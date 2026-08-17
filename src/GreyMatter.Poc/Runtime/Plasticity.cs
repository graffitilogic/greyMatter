using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Runtime;

/// <summary>
/// plan.md §4.4 step 5 — learning.
///
/// Two timescales, both ported in semantics from the legacy tree:
///
///   • **Within a cue**: Hebbian coactivation among this step's k-WTA winners.
///     Symmetric and time-blind, which is correct for "these things co-occurred".
///   • **Across cues**: a short temporal trace links the previous cue's winners to
///     this one's, directed. This is the legacy P4.2 idea — spike-timing STDP needs
///     a millisecond clock the engine does not have, but word order within a
///     sentence IS a real temporal axis and it was going unused. Without the
///     directed trace the graph cannot tell "cat"→"sat" from "sat"→"cat", and every
///     order experiment is answering a question the representation cannot express.
///
/// Neuron-level learning nudges receptive-field weights toward the input, in the
/// STM arrays; those consolidate into recipe deviations on eviction, not here.
/// </summary>
public sealed class Plasticity
{
    private readonly Config _cfg;
    private readonly ActivationScope _scope;

    /// <summary>Previous cue's winners, as VIRTUAL ids — slots move under compaction.</summary>
    private readonly uint[] _trace;
    private int _traceCount;
    private readonly HashSet<uint> _cueMembers = new();

    public float NeuronLearningRate { get; init; } = 0.05f;

    /// <summary>
    /// Cross-cue edges are born weaker than within-cue ones. Sequence evidence is
    /// one observation of two things being adjacent; co-activation within a cue is
    /// the same pattern re-presented. Weighting them equally lets sentence-order
    /// noise swamp the co-occurrence signal.
    /// </summary>
    public float SequenceStrength { get; init; } = 0.5f;

    public long WithinCueUpdates { get; private set; }
    public long SequenceUpdates { get; private set; }

    public Plasticity(Config cfg, ActivationScope scope)
    {
        _cfg = cfg;
        _scope = scope;
        _trace = new uint[cfg.ActivationWidth];
    }

    /// <summary>
    /// Learn from one cue's winners. <paramref name="winners"/> are slots and
    /// <paramref name="scores"/> their activation, both from the cascade's final step.
    /// </summary>
    /// <param name="cueMembers">
    /// Virtual ids of the cue's own assembly. Needed to classify each within-cue
    /// edge as WithinAssembly (both endpoints in the cue's assembly — encodes only
    /// "this cue fired") or CrossAssembly (at least one endpoint reached by
    /// propagation). That distinction is A.1 hypothesis 1 and cannot be measured
    /// without it.
    /// </param>
    public void Learn(ReadOnlySpan<int> winners, ReadOnlySpan<float> scores,
                      ReadOnlySpan<uint> cueMembers = default)
    {
        var pool = _scope.Pool;
        var synapses = _scope.Synapses;

        // Normalise activations into 0..1 — the ported creation threshold is
        // calibrated for that range, and raw cascade mass is unbounded.
        float max = 0f;
        for (int i = 0; i < scores.Length; i++) if (scores[i] > max) max = scores[i];
        if (max <= 0f) { EndCue(winners, pool); return; }
        float inv = 1f / max;

        // Membership lookup for the cue's assembly, rebuilt per cue. Small (256)
        // and only touched on the learning path, not the propagation path.
        _cueMembers.Clear();
        for (int i = 0; i < cueMembers.Length; i++) _cueMembers.Add(cueMembers[i]);

        // ── Within-cue Hebbian coactivation ─────────────────────────────────
        for (int i = 0; i < winners.Length; i++)
        {
            int si = winners[i];
            uint vi = pool.VirtualId[si];
            float ai = scores[i] * inv;

            bool sourceInAssembly = _cueMembers.Count == 0 || _cueMembers.Contains(vi);

            for (int j = 0; j < winners.Length; j++)
            {
                if (i == j) continue;
                uint vj = pool.VirtualId[winners[j]];
                var population = sourceInAssembly && _cueMembers.Contains(vj)
                    ? SynapsePopulation.WithinAssembly
                    : SynapsePopulation.CrossAssembly;

                synapses.RecordCoactivation(si, vi, vj, ai, scores[j] * inv, population);
                WithinCueUpdates++;
            }

            // ── Neuron-level: nudge the receptive field toward the input ────
            // Ported from ReinforceTowardInput. Accumulates in STM; consolidates
            // to recipe deviations on eviction.
            var weights = _scope.Weights(si);
            for (int d = 0; d < weights.Length; d++)
                if (weights[d] != 0f) weights[d] += NeuronLearningRate * ai * weights[d] * 0.01f;
            _scope.MarkDirty(si);
        }

        // ── Cross-cue directed trace (sequence) ─────────────────────────────
        for (int p = 0; p < _traceCount; p++)
        {
            int pre = pool.Find(_trace[p]);
            if (pre < 0) continue;   // evicted since the previous cue

            for (int i = 0; i < winners.Length; i++)
            {
                // Directed: previous → current only. The asymmetry IS the order
                // information; wiring both ways would erase it.
                synapses.RecordCoactivation(pre, _trace[p], pool.VirtualId[winners[i]],
                                            SequenceStrength, scores[i] * inv,
                                            SynapsePopulation.CrossCue);
                SequenceUpdates++;
            }
        }

        EndCue(winners, pool);
    }

    private void EndCue(ReadOnlySpan<int> winners, NeuronPool pool)
    {
        _traceCount = Math.Min(winners.Length, _trace.Length);
        for (int i = 0; i < _traceCount; i++) _trace[i] = pool.VirtualId[winners[i]];
    }

    /// <summary>
    /// Break the temporal trace at a sentence boundary. Without this the last word
    /// of one sentence wires to the first of the next, manufacturing bigrams the
    /// corpus never contained — and those false bigrams would then be scored
    /// against real corpus statistics in the order eval.
    /// </summary>
    public void EndSequence() => _traceCount = 0;

    /// <summary>Periodic decay so synapses that stop being reinforced die (§4.1).</summary>
    public int Decay(float factor = 0.99f) => _scope.Synapses.ApplyDecay(_scope.Pool.Count, factor);
}
