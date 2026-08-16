using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Engrams;

/// <summary>
/// plan.md §4.3 — "a lookup scheme to determine which recipes may aide specific
/// concepts". This is the entire scheme: a cue's sparse code → candidate buckets
/// → candidate assembly recipes.
///
/// Ported from <c>LSHPartitioner</c> in concept, rewritten in mechanism, for two
/// reasons that are not stylistic:
///
///   1. **Legacy emitted string region ids** ("A3F1_0092_…"), and those ids named
///      partitions on disk. §4.3's guardrail forbids strings in persisted records.
///      Buckets here are <c>uint</c>.
///   2. **Legacy hashed dense double vectors** via random projections. The things
///      being indexed now are k-of-n sparse codes, for which banded MinHash is the
///      correct family — it estimates Jaccard overlap, which IS the similarity
///      measure <see cref="SparseCode"/> defines.
///
/// Banding gives the usual tunable: two codes share a bucket if they agree on all
/// <c>RowsPerBand</c> hashes of any one band, so recall rises with
/// <c>Bands</c> and precision with <c>RowsPerBand</c>.
/// </summary>
public sealed class LshIndex
{
    public int Bands { get; }
    public int RowsPerBand { get; }
    private readonly int _seed;

    private readonly Dictionary<uint, List<ulong>> _buckets = new();

    public LshIndex(int seed, int bands = 16, int rowsPerBand = 2)
    {
        if (bands <= 0 || rowsPerBand <= 0) throw new ArgumentOutOfRangeException(nameof(bands));
        Bands = bands;
        RowsPerBand = rowsPerBand;
        _seed = seed;
    }

    public int BucketCount => _buckets.Count;

    /// <summary>
    /// MinHash under permutation <paramref name="h"/>: the smallest hashed active
    /// dimension. Two codes agree here with probability equal to their Jaccard
    /// similarity, which is what makes banding work.
    /// </summary>
    private uint MinHash(in SparseCode code, int h)
    {
        uint min = uint.MaxValue;
        for (int i = 0; i < code.Dims.Length; i++)
        {
            uint v = (uint)(Rng.Bits(_seed, Rng.Purpose.Projection, (uint)code.Dims[i], (uint)h) >> 32);
            if (v < min) min = v;
        }
        return min;
    }

    /// <summary>The bucket ids a code belongs to — one per band.</summary>
    public uint[] BucketsFor(in SparseCode code)
    {
        var result = new uint[Bands];
        for (int b = 0; b < Bands; b++)
        {
            ulong acc = 0xCBF29CE484222325UL ^ (ulong)b;
            for (int r = 0; r < RowsPerBand; r++)
                acc = Rng.Mix(acc ^ MinHash(code, b * RowsPerBand + r));
            result[b] = (uint)(acc >> 32);
        }
        return result;
    }

    /// <summary>Primary partition for a code — where its engram is stored.</summary>
    public uint PrimaryBucket(in SparseCode code) => BucketsFor(code)[0];

    public void Add(in SparseCode code, ulong assemblyHash)
    {
        foreach (var bucket in BucketsFor(code))
        {
            if (!_buckets.TryGetValue(bucket, out var list))
                _buckets[bucket] = list = new List<ulong>();
            if (!list.Contains(assemblyHash)) list.Add(assemblyHash);
        }
    }

    /// <summary>
    /// Candidate assemblies for a cue, in deterministic order. Candidates only —
    /// LSH over-retrieves by design and the caller scores what it gets.
    /// </summary>
    public List<ulong> Candidates(in SparseCode code, int limit = int.MaxValue)
    {
        var seen = new HashSet<ulong>();
        var result = new List<ulong>();
        foreach (var bucket in BucketsFor(code))
        {
            if (!_buckets.TryGetValue(bucket, out var list)) continue;
            foreach (var h in list)
            {
                if (!seen.Add(h)) continue;
                result.Add(h);
                if (result.Count >= limit) return result;
            }
        }
        return result;
    }

    public void Clear() => _buckets.Clear();

    /// <summary>Flat (bucket, assembly) pairs for persistence — no strings.</summary>
    public IEnumerable<(uint bucket, ulong assembly)> Entries()
    {
        foreach (var kv in _buckets.OrderBy(k => k.Key))
            foreach (var h in kv.Value)
                yield return (kv.Key, h);
    }

    public void Load(IEnumerable<(uint bucket, ulong assembly)> entries)
    {
        foreach (var (bucket, assembly) in entries)
        {
            if (!_buckets.TryGetValue(bucket, out var list))
                _buckets[bucket] = list = new List<ulong>();
            list.Add(assembly);
        }
    }
}
