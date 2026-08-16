using System.Diagnostics;

namespace GreyMatter.Poc.Substrate;

/// <summary>
/// plan.md P1 gate — the substrate microbenchmark, not a new experiment.
///
/// One cycle = materialize a scope of <c>--scope</c> neurons drawn from a
/// <c>BaselineNeuronCount</c>-wide virtual space, run <c>ActivationDepth</c>
/// propagation steps with k-WTA over the resident set, apply Hebbian coactivation,
/// then evict the scope.
///
/// The propagation here is a deliberately minimal kernel written directly against
/// the pool and store arrays. It is NOT <c>Runtime/Cascade</c> — that is P4's
/// component with its own semantics. This exists to load the substrate with a
/// propagation-SHAPED workload so the gate measures what it claims to measure.
/// </summary>
public static class SubstrateBench
{
    public sealed record Result(
        int Cycles, int ScopeSize, double Seconds, double CyclesPerSecond,
        int Gen0, int Gen1, int Gen2, long AllocatedBytes,
        long Synapses, int HighWaterMark, long Materialized, long Evicted,
        long Created, long Strengthened, long Displaced, long Declined);

    public static Result Run(Config cfg, int cycles, int scopeSize, bool quiet = false)
    {
        var pool = new NeuronPool(cfg.WorkingSetMax);
        var synapses = new SynapseStore(cfg.WorkingSetMax, cfg.SynapseCapPerNeuron);
        pool.OnSlotMoved = synapses.MoveSlot;

        // Scratch, allocated once. Anything allocated inside the loop would show
        // up as gen0 pressure and defeat the point of the gate.
        var scope = new uint[scopeSize];
        var slots = new int[scopeSize];
        var winners = new int[Math.Min(cfg.ActivationWidth, scopeSize)];
        var winnerScores = new float[winners.Length];

        // Bound the eviction callback exactly once. A method group or lambda
        // written at the call site allocates a delegate on every invocation,
        // which is gen0 pressure the gate is specifically measuring for.
        Action<int> onEvict = synapses.ClearSlot;

        // Warm up: JIT the loop bodies and settle the pool before measuring.
        int warmup = Math.Min(50, Math.Max(1, cycles / 10));
        for (int c = 0; c < warmup; c++) Cycle(cfg, pool, synapses, scope, slots, winners, winnerScores, (uint)c, onEvict);

        var gen0 = GC.CollectionCount(0);
        var gen1 = GC.CollectionCount(1);
        var gen2 = GC.CollectionCount(2);
        var alloc0 = GC.GetAllocatedBytesForCurrentThread();

        var sw = Stopwatch.StartNew();
        for (int c = 0; c < cycles; c++)
        {
            Cycle(cfg, pool, synapses, scope, slots, winners, winnerScores, (uint)(warmup + c), onEvict);
            if (!quiet && cycles >= 1000 && (c + 1) % (cycles / 10) == 0)
                Console.WriteLine($"   {c + 1,8:N0}/{cycles:N0} cycles   {(c + 1) / sw.Elapsed.TotalSeconds,8:F1}/s   " +
                                  $"resident {pool.Count:N0}   synapses {synapses.TotalSynapses:N0}");
        }
        sw.Stop();

        return new Result(
            cycles, scopeSize, sw.Elapsed.TotalSeconds, cycles / sw.Elapsed.TotalSeconds,
            GC.CollectionCount(0) - gen0, GC.CollectionCount(1) - gen1, GC.CollectionCount(2) - gen2,
            GC.GetAllocatedBytesForCurrentThread() - alloc0,
            synapses.TotalSynapses, pool.HighWaterMark, pool.TotalMaterialized, pool.TotalEvicted,
            synapses.Created, synapses.Strengthened, synapses.Displaced, synapses.Declined);
    }

    /// <summary>One full materialize → propagate → learn → evict cycle.</summary>
    private static void Cycle(Config cfg, NeuronPool pool, SynapseStore syn,
                              uint[] scope, int[] slots, int[] winners, float[] winnerScores, uint cycleId,
                              Action<int> onEvict)
    {
        pool.AdvanceTick();
        uint space = (uint)cfg.BaselineNeuronCount;

        // ── 1. Materialize the scope ────────────────────────────────────────
        // Cue-driven in the real runtime; here a counter-based draw, so the same
        // seed and cycle index always select the same neurons (rule 8).
        // Two passes, and the split is load-bearing: materializing can trigger a
        // batch eviction, which COMPACTS the pool and moves surviving neurons to
        // new slots. Any slot index captured before that point is stale. Resolving
        // slots only after every materialization is done is what makes the indices
        // valid. (Neurons materialized this cycle carry the current tick, which is
        // the maximum, so eviction can never take one of them.)
        for (int i = 0; i < scope.Length; i++)
        {
            uint vid = Rng.NextUInt(cfg.Seed, Rng.Purpose.Benchmark, cycleId, space, (uint)i);
            scope[i] = vid;
            pool.Materialize(vid, onEvict);
        }

        for (int i = 0; i < scope.Length; i++)
        {
            int slot = pool.Find(scope[i]);
            slots[i] = slot;

            // Regeneration stand-in: a neuron's initial state is a pure function
            // of its virtual id, exactly as recipes will make it in P3.
            pool.Potential[slot] = Rng.NextFloat(cfg.Seed, Rng.Purpose.NeuronSeed, scope[i]);
            pool.Threshold[slot] = 0.5f + 0.25f * Rng.NextFloat(cfg.Seed, Rng.Purpose.ReceptiveField, scope[i]);
        }

        // ── 2. Propagate, ActivationDepth steps, k-WTA each step ────────────
        for (int step = 0; step < cfg.ActivationDepth; step++)
        {
            // Integrate along existing synapses. Flat scan of each segment; the
            // target lookup is a hash probe, which is the one non-contiguous
            // access in the cycle and the thing the CUDA port will have to batch.
            for (int i = 0; i < slots.Length; i++)
            {
                int slot = slots[i];
                if (pool.Potential[slot] < pool.Threshold[slot]) continue;

                int start = syn.SegmentStart(slot);
                int end = start + syn.Degree[slot];
                float drive = pool.Potential[slot];
                for (int s = start; s < end; s++)
                {
                    int tslot = pool.Find(syn.Target[s]);
                    if (tslot >= 0) pool.Potential[tslot] += syn.Weight[s] * drive;
                }
            }

            // k-WTA inhibition: keep the ActivationWidth strongest, silence the
            // rest. A partial selection, expressed as a bounded insertion into a
            // small array — a reduction, not a sort of the whole scope (§7).
            int found = SelectTopK(pool, slots, winners, winnerScores);

            for (int i = 0; i < slots.Length; i++) pool.Potential[slots[i]] = 0f;
            for (int i = 0; i < found; i++)
            {
                int slot = winners[i];
                pool.Potential[slot] = winnerScores[i];
                pool.Fatigue[slot] += 0.01f;
                pool.Familiarity[slot] += 0.001f;
                pool.Touch(slot);
            }

            // ── 3. Learn: Hebbian coactivation among this step's winners ─────
            for (int i = 0; i < found; i++)
                for (int j = 0; j < found; j++)
                {
                    if (i == j) continue;
                    syn.RecordCoactivation(winners[i], pool.VirtualId[winners[i]], pool.VirtualId[winners[j]],
                                           Normalize(winnerScores[i]), Normalize(winnerScores[j]));
                }
        }

        // ── 4. Decay, so synapses that stop being reinforced die ────────────
        if (cycleId % 100 == 99) syn.ApplyDecay(pool.Count);
    }

    /// Activations are clamped into 0..1 before Hebbian use — the legacy rule
    /// assumes normalized activations and the creation threshold is calibrated
    /// for that range.
    private static float Normalize(float x) => x <= 0f ? 0f : (x >= 1f ? 1f : x);

    /// <summary>
    /// Bounded partial selection of the k strongest potentials. O(n·k) worst case
    /// with k = ActivationWidth, no allocation, no sort of the full scope.
    /// </summary>
    private static int SelectTopK(NeuronPool pool, int[] slots, int[] winners, float[] scores)
    {
        int k = winners.Length;
        int found = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            int slot = slots[i];
            float v = pool.Potential[slot];
            if (found == k && v <= scores[k - 1]) continue;

            int pos = found < k ? found : k - 1;
            while (pos > 0 && scores[pos - 1] < v)
            {
                scores[pos] = scores[pos - 1];
                winners[pos] = winners[pos - 1];
                pos--;
            }
            scores[pos] = v;
            winners[pos] = slot;
            if (found < k) found++;
        }
        return found;
    }
}
