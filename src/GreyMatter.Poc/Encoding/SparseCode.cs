using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Encoding;

/// <summary>
/// plan.md §4.2 stage 3 — a k-of-n sparse code.
///
/// Dimensions are stored sorted and distinct, so overlap is a merge rather than a
/// set intersection and the whole type stays allocation-light. The hash is a
/// stable 64-bit function of the active bit set: it is the ONLY identifier that
/// reaches disk (§4.3 guardrail — no strings in any persisted record), so it must
/// not depend on process, platform, or insertion order.
/// </summary>
public readonly struct SparseCode
{
    /// <summary>Active dimensions, ascending, distinct.</summary>
    public readonly int[] Dims;

    public SparseCode(int[] sortedDistinctDims) => Dims = sortedDistinctDims;

    public int K => Dims.Length;

    /// <summary>Raw overlap: how many dimensions two codes share.</summary>
    public int Overlap(in SparseCode other)
    {
        int i = 0, j = 0, n = 0;
        while (i < Dims.Length && j < other.Dims.Length)
        {
            int a = Dims[i], b = other.Dims[j];
            if (a == b) { n++; i++; j++; }
            else if (a < b) i++;
            else j++;
        }
        return n;
    }

    /// <summary>Overlap normalised by code size — 1.0 for identical codes.</summary>
    public float Similarity(in SparseCode other)
    {
        int denom = Math.Max(Dims.Length, other.Dims.Length);
        return denom == 0 ? 0f : (float)Overlap(other) / denom;
    }

    /// <summary>
    /// Rarity-weighted similarity — the §4.2 correction the P0 baseline demands.
    ///
    /// Plain overlap counts a dimension that appears in 90% of the vocabulary the
    /// same as one that appears in 2%, but only the second says anything about
    /// identity. P0 measured 1 generic dim (in &gt;90% of words) against 5
    /// discriminative ones (&lt;10%) and a median 27-of-32 overlap between a word
    /// and its nearest neighbour — so unweighted overlap is dominated by shared
    /// structure and barely discriminates at all.
    ///
    /// <paramref name="idf"/> supplies each dimension's inverse document frequency,
    /// i.e. how surprising it is for that dimension to be active.
    /// </summary>
    public float WeightedSimilarity(in SparseCode other, float[] idf)
    {
        int i = 0, j = 0;
        float shared = 0f, total = 0f;

        while (i < Dims.Length && j < other.Dims.Length)
        {
            int a = Dims[i], b = other.Dims[j];
            if (a == b) { shared += idf[a]; total += idf[a]; i++; j++; }
            else if (a < b) { total += idf[a]; i++; }
            else { total += idf[b]; j++; }
        }
        while (i < Dims.Length) total += idf[Dims[i++]];
        while (j < other.Dims.Length) total += idf[other.Dims[j++]];

        return total <= 0f ? 0f : shared / total;
    }

    /// <summary>
    /// Stable 64-bit hash of the active set. Order-independent by construction
    /// (dims are sorted) and identical across processes and platforms — it is the
    /// key under which engrams are stored and looked up.
    /// </summary>
    public ulong Hash()
    {
        ulong h = 0xCBF29CE484222325UL;
        for (int i = 0; i < Dims.Length; i++)
            h = Rng.Mix(h ^ (ulong)(uint)Dims[i]);
        return h;
    }

    public override string ToString() => $"[{string.Join(",", Dims)}]";

    /// <summary>
    /// Top-k dimensions of a dense vector by |magnitude|, index-ascending
    /// tie-break, returned sorted. Identical selection rule to the legacy
    /// sparsification the P0 baseline was measured with.
    /// </summary>
    public static SparseCode TopK(float[] dense, int k)
    {
        if (k >= dense.Length) k = dense.Length;

        // Bounded insertion into a k-sized buffer: a selection, not a full sort.
        Span<int> best = k <= 128 ? stackalloc int[k] : new int[k];
        Span<float> mag = k <= 128 ? stackalloc float[k] : new float[k];
        int found = 0;

        for (int d = 0; d < dense.Length; d++)
        {
            float m = Math.Abs(dense[d]);
            // Strictly-greater keeps the tie-break at "lowest index wins", because
            // lower indices are visited first and are never displaced by an equal.
            if (found == k && m <= mag[k - 1]) continue;

            int pos = found < k ? found : k - 1;
            while (pos > 0 && mag[pos - 1] < m)
            {
                mag[pos] = mag[pos - 1];
                best[pos] = best[pos - 1];
                pos--;
            }
            mag[pos] = m;
            best[pos] = d;
            if (found < k) found++;
        }

        var dims = new int[found];
        for (int i = 0; i < found; i++) dims[i] = best[i];
        Array.Sort(dims);
        return new SparseCode(dims);
    }
}

/// <summary>
/// Per-dimension document frequency over observed codes, and the inverse
/// weighting derived from it. Kept beside the codes rather than inside them
/// because it is a property of the vocabulary, not of any one word.
/// </summary>
public sealed class RarityTable
{
    private readonly int[] _documentFrequency;
    private readonly float[] _idf;
    private int _documents;
    private bool _dirty = true;

    public RarityTable(int dimensions)
    {
        _documentFrequency = new int[dimensions];
        _idf = new float[dimensions];
    }

    public int Documents => _documents;

    public void Observe(in SparseCode code)
    {
        for (int i = 0; i < code.Dims.Length; i++) _documentFrequency[code.Dims[i]]++;
        _documents++;
        _dirty = true;
    }

    /// <summary>
    /// idf(d) = ln(1 + N / (1 + df(d))). Smoothed so an unseen dimension gets the
    /// maximum weight rather than a division by zero, and a dimension present in
    /// every word tends toward — but never reaches — zero weight.
    /// </summary>
    public float[] Idf()
    {
        if (!_dirty) return _idf;
        for (int d = 0; d < _idf.Length; d++)
            _idf[d] = MathF.Log(1f + (float)_documents / (1 + _documentFrequency[d]));
        _dirty = false;
        return _idf;
    }

    public int DocumentFrequency(int dim) => _documentFrequency[dim];
}
