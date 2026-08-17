using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Runtime;

/// <summary>
/// plan.md §4.4 steps 2–4 and 7 — propagation with k-WTA inhibition, and the
/// readout recall is measured from.
///
/// The k-WTA step is the interneuron-inhibition LEGO: every step keeps only the
/// <c>ActivationWidth</c> strongest and silences the rest, expressed as a bounded
/// partial selection rather than a sort (§7).
///
/// Neurons reachable by synapse but not resident are regenerated on demand; past
/// the working-set cap the cascade TRUNCATES. That truncation is the
/// accuracy-for-scale trade the whole project is about, so it is counted and
/// reported rather than hidden.
/// </summary>
public sealed class Cascade
{
    private readonly Config _cfg;
    private readonly ActivationScope _scope;

    private readonly uint[] _members;
    private readonly int[] _active;
    private readonly int[] _winners;
    private readonly float[] _winnerScores;
    private readonly byte[] _hop;
    private readonly int[] _deliveredBy;
    private readonly float[] _massByHop = new float[3];
    private readonly float[] _driveByPopulation = new float[3];
    private readonly int[] _winnersByHop = new int[3];

    public long Truncations { get; private set; }
    public long Regenerations { get; private set; }

    public Cascade(Config cfg, ActivationScope scope)
    {
        _cfg = cfg;
        _scope = scope;
        _members = new uint[Assembly.Size(cfg.Sparsity)];
        _active = new int[Math.Max(cfg.ActivationWidth * 4, _members.Length * 2)];
        _winners = new int[cfg.ActivationWidth];
        _winnerScores = new float[cfg.ActivationWidth];
        _hop = new byte[_active.Length];
        _deliveredBy = new int[_active.Length];
    }

    public ReadOnlySpan<int> Winners(int count) => _winners.AsSpan(0, count);
    public ReadOnlySpan<float> WinnerScores(int count) => _winnerScores.AsSpan(0, count);

    public sealed record Readout(int WinnerCount, float TotalMass, float MeanWinnerMass,
                                 float SelfMass, int Materialized, int Truncated)
    {
        /// <summary>P7.0 — mass by hop: [0] the cue's own drive, [1] one synapse away, [2] two or more.</summary>
        public float[] MassByHop { get; init; } = new float[3];

        /// <summary>
        /// P7.0 — drive INJECTED by each synapse population, summed at delivery.
        ///
        /// Different units from <see cref="MassByHop"/> deliberately. Classifying a
        /// surviving winner by "the population that delivered it" undercounts
        /// catastrophically: an assembly member starts at hop 0 and is then topped up
        /// by within-assembly edges, so the drive those edges contribute is real but
        /// belongs to a node that is still hop 0. The first version of this metric
        /// reported 0.0 for every population as a result. Measuring at the point of
        /// delivery attributes it correctly.
        /// </summary>
        public float[] DriveByPopulation { get; init; } = new float[3];

        /// <summary>P7.0 — how many k-WTA winners were at each hop.</summary>
        public int[] WinnersByHop { get; init; } = new int[3];
    }

    /// <summary>The cue's assembly members from the last Run — needed to classify edge provenance.</summary>
    public ReadOnlySpan<uint> LastCueMembers(int count) => _members.AsSpan(0, count);
    public int LastMemberCount { get; private set; }

    /// <summary>
    /// Run one cue to completion. Returns the readout; the winners of the final
    /// step remain available via <see cref="Winners"/> for the learning pass.
    /// </summary>
    public Readout Run(in SparseCode code, bool learningMode)
    {
        _scope.AdvanceTick();
        var pool = _scope.Pool;
        var synapses = _scope.Synapses;

        // ── Materialize the cue's assembly ──────────────────────────────────
        int memberCount = Assembly.Members(code, _cfg.BaselineNeuronCount, _members);
        LastMemberCount = memberCount;
        int truncatedHere = 0;

        // P7.0 attribution. hopOf[slot-index-into-_active] records how many synapse
        // steps from the cue a neuron was first reached; deliveredBy records which
        // synapse population delivered its most recent input.
        Array.Clear(_massByHop);
        Array.Clear(_driveByPopulation);
        Array.Clear(_winnersByHop);

        for (int i = 0; i < memberCount; i++)
        {
            // Ask the pool to make room rather than pre-testing whether it is full.
            // Pre-testing `Count >= Capacity` refused EVERY materialization once the
            // pool first filled, freezing the working set at exactly WorkingSetMax
            // for the rest of the run — the evict/regenerate cycle, which is the
            // entire premise of the project, never ran.
            if (_scope.TryMaterialize(_members[i]) < 0)
            {
                truncatedHere++;
                Truncations++;
                continue;
            }
            Regenerations++;
        }

        // Resolve slots only after every materialization — eviction compacts the
        // pool and moves survivors, so indices captured earlier are stale.
        int activeCount = 0;
        for (int i = 0; i < memberCount && activeCount < _active.Length; i++)
        {
            int slot = pool.Find(_members[i]);
            if (slot < 0) continue;
            _hop[activeCount] = 0;
            _deliveredBy[activeCount] = -1;          // hop 0 arrives by no synapse
            _active[activeCount++] = slot;

            // Drive from the cue: assembly members start at full activation.
            pool.Potential[slot] = 1f;
        }

        float selfMass = 0;
        int winnerCount = 0;

        // ── Propagate ───────────────────────────────────────────────────────
        for (int step = 0; step < _cfg.ActivationDepth; step++)
        {
            for (int i = 0; i < activeCount; i++)
            {
                int slot = _active[i];
                float drive = pool.Potential[slot];
                if (drive < pool.Threshold[slot] * 0.5f) continue;

                int start = synapses.SegmentStart(slot);
                int degree = synapses.Degree[slot];
                int end = start + degree;

                // Activation is CONSERVED, not multiplied: a neuron distributes its
                // drive across its out-synapses rather than sending the full amount
                // down each one. Without this a neuron with 32 synapses emits 32×
                // what it received, mass grows ~32× per step, and by depth 4 every
                // cue saturates whatever ceiling exists — measured at 3.3e10 for
                // trained and control alike, AUC exactly 0.500. Dividing by degree
                // makes retained mass a statement about synaptic STRUCTURE (how much
                // a scope keeps circulating) rather than about out-degree.
                float share = drive / degree;

                int sourceHop = _hop[i];

                for (int s = start; s < end; s++)
                {
                    int target = pool.Find(synapses.Target[s]);
                    if (target < 0) continue;   // evicted; the cascade cannot follow

                    float contribution = synapses.Weight[s] * share;
                    _driveByPopulation[synapses.Population[s]] += contribution;

                    float v = pool.Potential[target] + contribution;
                    pool.Potential[target] = v > MaxPotential ? MaxPotential : v;

                    // Reached neurons join the active set so the next step can
                    // propagate from them — this is what makes depth mean anything.
                    int existing = IndexOf(_active, activeCount, target);
                    if (existing < 0)
                    {
                        if (activeCount >= _active.Length) continue;
                        _hop[activeCount] = (byte)Math.Min(2, sourceHop + 1);
                        _deliveredBy[activeCount] = synapses.Population[s];
                        _active[activeCount++] = target;
                    }
                    else if (_hop[existing] > sourceHop + 1)
                    {
                        _hop[existing] = (byte)Math.Min(2, sourceHop + 1);
                        _deliveredBy[existing] = synapses.Population[s];
                    }
                }
            }

            winnerCount = SelectTopK(pool, _active, activeCount, _winners, _winnerScores,
                                     _hop, _cfg.PropagatedWinnerQuota);

            for (int i = 0; i < activeCount; i++) pool.Potential[_active[i]] = 0f;
            for (int i = 0; i < winnerCount; i++)
            {
                int slot = _winners[i];
                pool.Potential[slot] = _winnerScores[i];
                pool.Fatigue[slot] += 0.01f;
                pool.Familiarity[slot] = MathF.Min(1f, pool.Familiarity[slot] + 0.001f);
                pool.Touch(slot);
            }
        }

        // ── Readout ─────────────────────────────────────────────────────────
        //
        // Total mass surviving k-WTA after ActivationDepth steps. An untrained
        // assembly has no synapses, so its members receive nothing beyond the
        // initial drive and mass decays to the cue itself; a trained one has
        // learned lateral weights that carry mass forward. That difference IS the
        // recall signal, and it is why a trained and an untrained word can have
        // equally valid assemblies and still be distinguishable.
        float total = 0;
        for (int i = 0; i < winnerCount; i++) total += _winnerScores[i];

        // Attribute the surviving mass. A winner's hop and delivering population
        // are looked up from the active set it came from.
        for (int i = 0; i < winnerCount; i++)
        {
            int idx = IndexOf(_active, activeCount, _winners[i]);
            if (idx < 0) continue;
            _massByHop[_hop[idx]] += _winnerScores[i];
            _winnersByHop[_hop[idx]]++;
        }

        // Mass that landed back on the cue's own members, as a separate diagnostic.
        for (int i = 0; i < winnerCount; i++)
        {
            uint vid = pool.VirtualId[_winners[i]];
            for (int m = 0; m < memberCount; m++)
                if (_members[m] == vid) { selfMass += _winnerScores[i]; break; }
        }

        // Leave the pool electrically clean.
        //
        // Not doing this was a real defect: a cue's winners kept their potential
        // after Run returned, and Materialize only zeroes NEWLY resident neurons.
        // So potential accumulated across every cue in a run, grew without bound,
        // and reached Infinity → NaN. The recall eval reported "trained mass NaN"
        // and AUC 0.000 — which reads like a null result and is in fact arithmetic
        // overflow. Residual charge from the previous cue is also a correctness
        // problem in its own right: it makes a probe depend on what was probed
        // before it.
        for (int i = 0; i < activeCount; i++) pool.Potential[_active[i]] = 0f;

        return new Readout(winnerCount, total,
                           winnerCount > 0 ? total / winnerCount : 0f,
                           selfMass, memberCount - truncatedHere, truncatedHere)
        {
            MassByHop = (float[])_massByHop.Clone(),
            DriveByPopulation = (float[])_driveByPopulation.Clone(),
            WinnersByHop = (int[])_winnersByHop.Clone()
        };
    }

    /// <summary>Ceiling on a single neuron's potential; see the clamp in Run.</summary>
    private const float MaxPotential = 1e9f;

    private static int IndexOf(int[] arr, int count, int value)
    {
        for (int i = 0; i < count; i++) if (arr[i] == value) return i;
        return -1;
    }

    /// <summary>
    /// Bounded partial selection — a reduction, not a sort of the scope (§7).
    ///
    /// With <paramref name="propagatedQuota"/> &gt; 0 this runs as TWO selections:
    /// an open contest for (k − quota) slots, and a contest restricted to propagated
    /// (hop ≥ 1) neurons for the reserved remainder. Reserved slots left unfilled are
    /// returned to the open pool, so the quota never wastes capacity.
    ///
    /// The open contest is unrestricted rather than members-only on purpose: a
    /// propagated neuron strong enough to beat an assembly member on raw potential
    /// should win on merit, and the quota is a floor on propagated representation,
    /// not a cap on it.
    /// </summary>
    private static int SelectTopK(NeuronPool pool, int[] slots, int slotCount,
                                  int[] winners, float[] scores,
                                  byte[] hop, int propagatedQuota)
    {
        int k = winners.Length;
        if (propagatedQuota <= 0)
            return Select(pool, slots, slotCount, winners, scores, 0, k, hop, propagatedOnly: false);

        int quota = Math.Min(propagatedQuota, k);
        int openSlots = k - quota;

        int found = Select(pool, slots, slotCount, winners, scores, 0, openSlots, hop, propagatedOnly: false);

        // Reserved contest: propagated only, excluding anything already selected.
        int reserved = SelectExcluding(pool, slots, slotCount, winners, scores, found, quota, hop);
        found += reserved;

        // Hand unfilled reserved slots back to the open pool.
        int unused = quota - reserved;
        if (unused > 0)
            found += SelectExcluding(pool, slots, slotCount, winners, scores, found, unused, hop: null);

        return found;
    }

    private static int Select(NeuronPool pool, int[] slots, int slotCount,
                              int[] winners, float[] scores, int offset, int capacity,
                              byte[]? hop, bool propagatedOnly)
    {
        if (capacity <= 0) return 0;
        int found = 0;
        for (int i = 0; i < slotCount; i++)
        {
            if (propagatedOnly && hop is not null && hop[i] == 0) continue;

            int slot = slots[i];
            float v = pool.Potential[slot];
            if (v <= 0f) continue;
            if (found == capacity && v <= scores[offset + capacity - 1]) continue;

            int pos = offset + (found < capacity ? found : capacity - 1);
            while (pos > offset && scores[pos - 1] < v)
            {
                scores[pos] = scores[pos - 1];
                winners[pos] = winners[pos - 1];
                pos--;
            }
            scores[pos] = v;
            winners[pos] = slot;
            if (found < capacity) found++;
        }
        return found;
    }

    /// Same selection, skipping slots already chosen. <paramref name="hop"/> non-null
    /// restricts the contest to propagated neurons.
    private static int SelectExcluding(NeuronPool pool, int[] slots, int slotCount,
                                       int[] winners, float[] scores, int chosen, int capacity,
                                       byte[]? hop)
    {
        if (capacity <= 0) return 0;
        int found = 0;
        for (int i = 0; i < slotCount; i++)
        {
            if (hop is not null && hop[i] == 0) continue;

            int slot = slots[i];
            float v = pool.Potential[slot];
            if (v <= 0f) continue;

            bool already = false;
            for (int c = 0; c < chosen; c++) if (winners[c] == slot) { already = true; break; }
            if (already) continue;

            if (found == capacity && v <= scores[chosen + capacity - 1]) continue;

            int pos = chosen + (found < capacity ? found : capacity - 1);
            while (pos > chosen && scores[pos - 1] < v)
            {
                scores[pos] = scores[pos - 1];
                winners[pos] = winners[pos - 1];
                pos--;
            }
            scores[pos] = v;
            winners[pos] = slot;
            if (found < capacity) found++;
        }
        return found;
    }
}
