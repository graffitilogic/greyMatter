using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Runtime;

/// <summary>
/// plan.md §4.3 — "which virtual neurons an activation pattern recruits".
///
/// Membership is DERIVED from the code HASH, per §4.3's
/// <c>{ codeHash → uint[] memberNeuronIds }</c> — not stored and looked up, and
/// not assembled from per-dimension slices.
///
/// **The per-dimension scheme was tried first and is fatally wrong.** Giving each
/// active dimension a fixed slice of the virtual space makes assemblies overlap
/// wherever codes share a dimension, which sounds like desirable distributed
/// representation and is in fact catastrophic: with n=2048 and 8 neurons per
/// dimension the entire addressable space is 16,384 neurons, and 500 sentences of
/// Tatoeba touch 16,005 of them. Measured consequence — a held-out control word
/// shared **256 of 256** members with the trained cues. There is no such thing as
/// an untrained word under that scheme, because every neuron a word addresses was
/// trained by some other word sharing a dimension. The P4 recall gate returned
/// AUC 0.500 for exactly this reason.
///
/// Hash-derived membership gives each distinct code its own neurons. Similarity
/// between words is then carried where the plan puts it — by learned synapses
/// between co-occurring assemblies, and by the LSH index over codes (§4.3, "the
/// entire lookup scheme") — rather than by accidental address collisions.
///
/// Two properties this preserves:
///   • An unseen cue still gets a valid assembly; nothing need be stored first.
///     Materialization is regeneration, as §4.4 requires.
///   • A trained and an untrained word produce equally valid assemblies, and only
///     the SYNAPSES between members differ. That is what makes the recall gate a
///     test of learning rather than of storage.
/// </summary>
public static class Assembly
{
    /// <summary>Neurons recruited per active dimension's worth of code capacity.</summary>
    public const int NeuronsPerDim = 8;

    public static int Size(int sparsity) => sparsity * NeuronsPerDim;

    /// <summary>
    /// Member virtual-neuron ids for a code, written into <paramml name="members"/>.
    /// Returns the count written.
    ///
    /// Deterministic in (codeHash, j, baselineNeuronCount), and deliberately NOT in
    /// the run seed: assemblies must be stable across runs and processes, or a
    /// store written by one run is meaningless to the next.
    /// </summary>
    public static int Members(in SparseCode code, int baselineNeuronCount, uint[] members)
    {
        uint space = (uint)baselineNeuronCount;
        ulong hash = code.Hash();

        int want = Math.Min(members.Length, Size(code.K));
        for (uint j = 0; j < want; j++)
            members[j] = (uint)(Rng.Mix(hash ^ Rng.Mix(j)) % space);

        return want;
    }

    public static uint[] Members(in SparseCode code, int baselineNeuronCount)
    {
        var members = new uint[Size(code.K)];
        int n = Members(code, baselineNeuronCount, members);
        return n == members.Length ? members : members[..n];
    }
}
