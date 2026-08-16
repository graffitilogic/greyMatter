namespace GreyMatter.Poc.Substrate;

/// <summary>
/// plan.md §4.1 — CSR-style bounded adjacency over the materialized set.
///
/// Each resident slot owns a fixed segment of <c>CapPerNeuron</c> entries at
/// <c>[slot*cap, slot*cap + Degree[slot])</c>. Fixed stride rather than true CSR
/// offsets because the cap makes segments uniform, which removes the offset
/// array entirely and makes the layout a plain 2D matrix — the friendliest
/// possible shape for a kernel.
///
/// Targets are stored as VIRTUAL ids, not slots. A synapse must outlive eviction
/// of its target (that is the entire point of a virtual space larger than RAM),
/// and virtual ids are also what gets persisted, so no translation is needed on
/// the save path.
///
/// The learning rules — Hebbian update, creation threshold, birth weight, decay,
/// prune, and competitive displacement at the degree cap — are ported from legacy
/// <c>SparseSynapticGraph</c> with their semantics intact. Only the data layout
/// changes (§1.2: "the logic is sound; the data layout is not").
/// </summary>
public sealed class SynapseStore
{
    public readonly uint[] Target;
    public readonly float[] Weight;
    public readonly int[] Degree;

    public int CapPerNeuron { get; }
    public int SlotCapacity { get; }

    // Ported legacy constants. Changing these changes learning, not performance.
    public float LearningRate { get; init; } = 0.01f;
    public float MinWeight { get; init; } = 0.0f;
    public float MaxWeight { get; init; } = 1.0f;
    public float PruneThreshold { get; init; } = 0.1f;
    public float CreationProductThreshold { get; init; } = 0.15f;

    // §6.1 rule 3 in spirit: instrument the DECISION, not just the aggregate.
    // "declined" and "displaced" are the two arms of synaptic competition and
    // telling them apart is what makes the counter worth keeping.
    public long Created { get; private set; }
    public long Strengthened { get; private set; }
    public long Displaced { get; private set; }
    public long Declined { get; private set; }
    public long Pruned { get; private set; }

    public SynapseStore(int slotCapacity, int capPerNeuron)
    {
        if (slotCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(slotCapacity));
        if (capPerNeuron <= 0) throw new ArgumentOutOfRangeException(nameof(capPerNeuron));

        long entries = (long)slotCapacity * capPerNeuron;
        if (entries > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(slotCapacity),
                $"slotCapacity × capPerNeuron = {entries:N0} exceeds the maximum array length");

        SlotCapacity = slotCapacity;
        CapPerNeuron = capPerNeuron;
        Target = new uint[entries];
        Weight = new float[entries];
        Degree = new int[slotCapacity];
    }

    public int SegmentStart(int slot) => slot * CapPerNeuron;

    public long TotalSynapses
    {
        get { long n = 0; for (int i = 0; i < Degree.Length; i++) n += Degree[i]; return n; }
    }

    /// <summary>Weight of slot→targetVirtualId, or 0 when no such synapse exists.</summary>
    public float GetWeight(int slot, uint targetVirtualId)
    {
        int start = SegmentStart(slot);
        int end = start + Degree[slot];
        for (int i = start; i < end; i++)
            if (Target[i] == targetVirtualId) return Weight[i];
        return 0f;
    }

    /// <summary>
    /// Hebbian coactivation: Δw = η·a_src·a_tgt, clamped.
    ///
    /// Strengthening an existing synapse is always allowed — reinforcement is
    /// free. Creating a new one is not: both parties must be meaningfully active,
    /// and the synapse is born just above the prune line so a single decay pass
    /// kills it unless it is reinforced. Persistence is earned by repetition.
    /// </summary>
    public void RecordCoactivation(int slot, uint sourceVirtualId, uint targetVirtualId,
                                   float sourceActivation, float targetActivation)
    {
        if (sourceVirtualId == targetVirtualId) return;

        int start = SegmentStart(slot);
        int degree = Degree[slot];
        int end = start + degree;

        float delta = LearningRate * sourceActivation * targetActivation;

        for (int i = start; i < end; i++)
        {
            if (Target[i] != targetVirtualId) continue;
            Weight[i] = Math.Clamp(Weight[i] + delta, MinWeight, MaxWeight);
            Strengthened++;
            return;
        }

        if (sourceActivation * targetActivation < CreationProductThreshold) return;

        float birthWeight = Math.Clamp(PruneThreshold + delta, MinWeight, MaxWeight);

        if (degree >= CapPerNeuron)
        {
            // Competitive displacement, not first-come-first-served. Refusing
            // every candidate once the cap is hit means whichever partners arrived
            // first hold their slots forever — legacy P5.5 measured 18.4M creations
            // blocked and FEWER reachable pairs from 40× more data. Decay pulls
            // unreinforced synapses toward the prune line, so a slot held by a
            // dying connection loses it to a fresh one while a reinforced slot
            // sits safely above birthWeight. Displacement targets exactly the
            // connections that stopped earning their place.
            int weakest = -1;
            float weakestW = float.MaxValue;
            for (int i = start; i < end; i++)
                if (Weight[i] < weakestW) { weakestW = Weight[i]; weakest = i; }

            if (weakest < 0 || weakestW >= birthWeight) { Declined++; return; }

            Target[weakest] = targetVirtualId;
            Weight[weakest] = birthWeight;
            Displaced++;
            return;
        }

        Target[end] = targetVirtualId;
        Weight[end] = birthWeight;
        Degree[slot] = degree + 1;
        Created++;
    }

    /// <summary>
    /// Long-term depression — weakens an existing synapse. Never creates one:
    /// depressing a connection that does not exist is not a meaningful operation.
    /// </summary>
    public void Depress(int slot, uint targetVirtualId, float amount)
    {
        int start = SegmentStart(slot);
        int end = start + Degree[slot];
        for (int i = start; i < end; i++)
            if (Target[i] == targetVirtualId)
            {
                Weight[i] = Math.Clamp(Weight[i] - amount, MinWeight, MaxWeight);
                return;
            }
    }

    /// <summary>
    /// Decay every synapse of every resident slot, removing those that fall below
    /// the prune line. One flat pass; compaction within a segment is a swap with
    /// the segment's last live entry, so order within a segment is not stable
    /// (nothing depends on it, and stability would cost a shift per removal).
    /// </summary>
    public int ApplyDecay(int liveSlots, float decayFactor = 0.99f)
    {
        int removed = 0;
        for (int slot = 0; slot < liveSlots; slot++)
        {
            int start = SegmentStart(slot);
            int degree = Degree[slot];
            for (int i = start + degree - 1; i >= start; i--)
            {
                float w = Weight[i] * decayFactor;
                if (w < PruneThreshold)
                {
                    int last = start + degree - 1;
                    Target[i] = Target[last];
                    Weight[i] = Weight[last];
                    degree--;
                    removed++;
                }
                else Weight[i] = w;
            }
            Degree[slot] = degree;
        }
        Pruned += removed;
        return removed;
    }

    /// <summary>Remove synapses below the prune threshold without decaying the rest.</summary>
    public int PruneWeakSynapses(int liveSlots)
    {
        int removed = 0;
        for (int slot = 0; slot < liveSlots; slot++)
        {
            int start = SegmentStart(slot);
            int degree = Degree[slot];
            for (int i = start + degree - 1; i >= start; i--)
            {
                if (Weight[i] >= PruneThreshold) continue;
                int last = start + degree - 1;
                Target[i] = Target[last];
                Weight[i] = Weight[last];
                degree--;
                removed++;
            }
            Degree[slot] = degree;
        }
        Pruned += removed;
        return removed;
    }

    /// <summary>Move a slot's whole segment. Called by NeuronPool compaction.</summary>
    public void MoveSlot(int from, int to)
    {
        int degree = Degree[from];
        Array.Copy(Target, SegmentStart(from), Target, SegmentStart(to), degree);
        Array.Copy(Weight, SegmentStart(from), Weight, SegmentStart(to), degree);
        Degree[to] = degree;
        Degree[from] = 0;
    }

    /// <summary>Drop a slot's synapses — called when its neuron is evicted.</summary>
    public void ClearSlot(int slot) => Degree[slot] = 0;

    public void Clear()
    {
        Array.Clear(Degree);
        Created = Strengthened = Displaced = Declined = Pruned = 0;
    }
}
