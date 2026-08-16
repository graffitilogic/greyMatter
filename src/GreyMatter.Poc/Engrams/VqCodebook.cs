using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Engrams;

/// <summary>
/// plan.md §4.3 — ported <c>VectorQuantizer</c>. The codebook is what makes a
/// recipe compact: a neuron is BORN as its prototype, and only the deviation from
/// that prototype needs storing.
///
/// Two departures from the legacy version, both required here:
///   • initialisation is seeded (<c>Random.Shared</c> in the legacy constructor
///     made the codebook — and therefore every regenerated weight — differ run to
///     run, which is rule 8's exact failure mode);
///   • float32 throughout, no per-code heap arrays (§7): the codebook is one flat
///     <c>float[size*dim]</c>.
///
/// EMA codebook learning is ported as-is: a code's centroid tracks the running
/// mean of what quantises to it, decayed, so the codebook adapts online during
/// training and is frozen per checkpoint.
/// </summary>
public sealed class VqCodebook
{
    private readonly float[] _codebook;      // [size * dim], row-major
    private readonly float[] _emaClusterSize;
    private readonly float[] _emaSum;        // [size * dim]
    private readonly int[] _usage;

    public int Size { get; }
    public int Dim { get; }
    public float EmaDecay { get; init; } = 0.99f;

    public long TotalEncodings { get; private set; }

    public VqCodebook(int size, int dim, int seed)
    {
        Size = size;
        Dim = dim;
        _codebook = new float[size * dim];
        _emaClusterSize = new float[size];
        _emaSum = new float[size * dim];
        _usage = new int[size];

        // Seeded, not Random.Shared — the codebook determines every regenerated
        // weight, so an unseeded codebook makes the whole store non-reproducible.
        for (int c = 0; c < size; c++)
            for (int d = 0; d < dim; d++)
                _codebook[c * dim + d] = 0.02f * Rng.NextSigned(seed, Rng.Purpose.ReceptiveField, (uint)c, (uint)d);
    }

    public ReadOnlySpan<float> Vector(int code) => _codebook.AsSpan(code * Dim, Dim);

    public float Component(int code, int dim) => _codebook[code * Dim + dim];

    public int UsageOf(int code) => _usage[code];

    /// <summary>Nearest codebook entry by squared euclidean distance. Ties go to the lower code.</summary>
    public ushort Quantize(ReadOnlySpan<float> embedding)
    {
        if (embedding.Length != Dim)
            throw new ArgumentException($"Expected {Dim}-dim embedding, got {embedding.Length}");

        int best = 0;
        float bestDist = float.MaxValue;
        for (int c = 0; c < Size; c++)
        {
            float dist = 0;
            int off = c * Dim;
            for (int d = 0; d < Dim; d++)
            {
                float diff = embedding[d] - _codebook[off + d];
                dist += diff * diff;
                if (dist >= bestDist) break;   // early out; distances only grow
            }
            if (dist < bestDist) { bestDist = dist; best = c; }
        }

        _usage[best]++;
        TotalEncodings++;
        return (ushort)best;
    }

    /// <summary>
    /// Quantize and update the codebook toward the embedding (online EMA).
    /// Training-mode only; recall must never mutate the codebook, or the store
    /// stops agreeing with what regeneration produces.
    /// </summary>
    public ushort QuantizeAndLearn(ReadOnlySpan<float> embedding)
    {
        var code = Quantize(embedding);

        _emaClusterSize[code] = EmaDecay * _emaClusterSize[code] + (1 - EmaDecay);
        int off = code * Dim;
        for (int d = 0; d < Dim; d++)
            _emaSum[off + d] = EmaDecay * _emaSum[off + d] + (1 - EmaDecay) * embedding[d];

        // Laplace-smoothed normalisation, so a code used once does not jump onto
        // its single observation.
        float n = _emaClusterSize[code];
        if (n > 1e-6f)
            for (int d = 0; d < Dim; d++)
                _codebook[off + d] = _emaSum[off + d] / n;

        return code;
    }

    public float[] Export() => (float[])_codebook.Clone();

    public void Import(float[] flat)
    {
        if (flat.Length != _codebook.Length)
            throw new ArgumentException($"Codebook size mismatch: expected {_codebook.Length}, got {flat.Length}");
        Array.Copy(flat, _codebook, flat.Length);
    }

    /// <summary>Fraction of codes ever used — a dead codebook silently collapses recipes together.</summary>
    public double Utilization()
    {
        int used = 0;
        for (int c = 0; c < Size; c++) if (_usage[c] > 0) used++;
        return (double)used / Size;
    }
}
