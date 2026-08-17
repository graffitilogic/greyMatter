using System.IO.Compression;
using MessagePack;

namespace GreyMatter.Poc.Engrams;

/// <summary>
/// A partition's worth of recipes, in STRUCT-OF-ARRAYS form.
///
/// Array-of-objects would be the obvious shape and is the wrong one twice over:
/// it repeats MessagePack's per-object framing for every neuron, and it does not
/// match the in-memory layout the runtime and any future kernel use (§7). Parallel
/// arrays also compress far better, because each array is homogeneous.
///
/// Deviations are stored CSR-style — one flat dim array, one flat delta array, and
/// per-neuron offsets — for the same reason <see cref="Substrate.SynapseStore"/>
/// is: it is one allocation, not one per neuron.
///
/// **No strings anywhere.** Every field is a numeric id, code, or hash. This is the
/// §4.3 guardrail, and <c>gm audit --strings</c> verifies it against the bytes
/// actually written rather than against intent.
/// </summary>
[MessagePackObject]
public sealed class EngramPartition
{
    [Key(0)] public uint Bucket { get; set; }
    [Key(1)] public uint[] Ids { get; set; } = Array.Empty<uint>();
    [Key(2)] public ushort[] VqCodes { get; set; } = Array.Empty<ushort>();
    [Key(3)] public uint[] Seeds { get; set; } = Array.Empty<uint>();
    [Key(4)] public float[] Familiarity { get; set; } = Array.Empty<float>();
    [Key(5)] public ushort[] ActivationCounts { get; set; } = Array.Empty<ushort>();

    /// <summary>Length Ids.Length + 1; neuron i owns [Offsets[i], Offsets[i+1]).</summary>
    [Key(6)] public int[] DeviationOffsets { get; set; } = new[] { 0 };
    [Key(7)] public ushort[] DeviationDims { get; set; } = Array.Empty<ushort>();
    [Key(8)] public float[] DeviationDeltas { get; set; } = Array.Empty<float>();

    /// <summary>Assembly recipes: which virtual neurons a code pattern recruits.</summary>
    [Key(9)] public ulong[] AssemblyHashes { get; set; } = Array.Empty<ulong>();
    [Key(10)] public int[] AssemblyOffsets { get; set; } = new[] { 0 };

    /// <summary>Member ids, sorted and DELTA-ENCODED — sorted uints delta far smaller than raw.</summary>
    [Key(11)] public uint[] AssemblyMembers { get; set; } = Array.Empty<uint>();

    /// <summary>
    /// Out-synapses, CSR over the same recipe ordering as DeviationOffsets.
    /// Neuron i owns [SynapseOffsets[i], SynapseOffsets[i+1]).
    /// </summary>
    [Key(12)] public int[] SynapseOffsets { get; set; } = new[] { 0 };
    [Key(13)] public uint[] SynapseTargets { get; set; } = Array.Empty<uint>();
    [Key(14)] public float[] SynapseWeights { get; set; } = Array.Empty<float>();
    [Key(15)] public byte[] SynapsePopulations { get; set; } = Array.Empty<byte>();

    [IgnoreMember] public int RecipeCount => Ids.Length;

    public static EngramPartition FromRecipes(uint bucket, IReadOnlyList<NeuronRecipe> recipes)
    {
        int n = recipes.Count;
        var p = new EngramPartition
        {
            Bucket = bucket,
            Ids = new uint[n],
            VqCodes = new ushort[n],
            Seeds = new uint[n],
            Familiarity = new float[n],
            ActivationCounts = new ushort[n],
            DeviationOffsets = new int[n + 1]
        };

        int total = 0, synTotal = 0;
        foreach (var r in recipes) { total += r.DeviationCount; synTotal += r.SynapseCount; }
        p.DeviationDims = new ushort[total];
        p.DeviationDeltas = new float[total];
        p.SynapseOffsets = new int[n + 1];
        p.SynapseTargets = new uint[synTotal];
        p.SynapseWeights = new float[synTotal];
        p.SynapsePopulations = new byte[synTotal];

        int off = 0, synOff = 0;
        for (int i = 0; i < n; i++)
        {
            var r = recipes[i];
            p.Ids[i] = r.Id;
            p.VqCodes[i] = r.VqCode;
            p.Seeds[i] = r.Seed;
            p.Familiarity[i] = r.Familiarity;
            p.ActivationCounts[i] = r.ActivationCount;
            p.DeviationOffsets[i] = off;

            Array.Copy(r.DeviationDims, 0, p.DeviationDims, off, r.DeviationCount);
            Array.Copy(r.DeviationDeltas, 0, p.DeviationDeltas, off, r.DeviationCount);
            off += r.DeviationCount;

            p.SynapseOffsets[i] = synOff;
            Array.Copy(r.SynapseTargets, 0, p.SynapseTargets, synOff, r.SynapseCount);
            Array.Copy(r.SynapseWeights, 0, p.SynapseWeights, synOff, r.SynapseCount);
            if (r.SynapsePopulations.Length >= r.SynapseCount)
                Array.Copy(r.SynapsePopulations, 0, p.SynapsePopulations, synOff, r.SynapseCount);
            synOff += r.SynapseCount;
        }
        p.DeviationOffsets[n] = off;
        p.SynapseOffsets[n] = synOff;
        return p;
    }

    public NeuronRecipe RecipeAt(int i)
    {
        int start = DeviationOffsets[i], end = DeviationOffsets[i + 1], len = end - start;
        var r = new NeuronRecipe(Ids[i], VqCodes[i], Seeds[i])
        {
            Familiarity = Familiarity[i],
            ActivationCount = ActivationCounts[i],
            DeviationDims = new ushort[len],
            DeviationDeltas = new float[len]
        };
        Array.Copy(DeviationDims, start, r.DeviationDims, 0, len);
        Array.Copy(DeviationDeltas, start, r.DeviationDeltas, 0, len);

        int sStart = SynapseOffsets[i], sLen = SynapseOffsets[i + 1] - sStart;
        r.SynapseTargets = new uint[sLen];
        r.SynapseWeights = new float[sLen];
        Array.Copy(SynapseTargets, sStart, r.SynapseTargets, 0, sLen);
        Array.Copy(SynapseWeights, sStart, r.SynapseWeights, 0, sLen);
        r.SynapsePopulations = new byte[sLen];
        if (SynapsePopulations.Length >= sStart + sLen)
            Array.Copy(SynapsePopulations, sStart, r.SynapsePopulations, 0, sLen);
        return r;
    }

    public IEnumerable<NeuronRecipe> Recipes()
    {
        for (int i = 0; i < Ids.Length; i++) yield return RecipeAt(i);
    }

    /// <summary>Set assembly members, delta-encoding each sorted id list in place.</summary>
    public void SetAssemblies(IReadOnlyList<(ulong hash, uint[] members)> assemblies)
    {
        AssemblyHashes = new ulong[assemblies.Count];
        AssemblyOffsets = new int[assemblies.Count + 1];
        int total = 0;
        foreach (var (_, m) in assemblies) total += m.Length;
        AssemblyMembers = new uint[total];

        int off = 0;
        for (int i = 0; i < assemblies.Count; i++)
        {
            var (hash, members) = assemblies[i];
            AssemblyHashes[i] = hash;
            AssemblyOffsets[i] = off;

            var sorted = (uint[])members.Clone();
            Array.Sort(sorted);
            uint prev = 0;
            for (int j = 0; j < sorted.Length; j++)
            {
                AssemblyMembers[off + j] = sorted[j] - prev;   // delta
                prev = sorted[j];
            }
            off += sorted.Length;
        }
        AssemblyOffsets[assemblies.Count] = off;
    }

    public uint[] AssemblyMembersAt(int i)
    {
        int start = AssemblyOffsets[i], end = AssemblyOffsets[i + 1];
        var result = new uint[end - start];
        uint running = 0;
        for (int j = 0; j < result.Length; j++)
        {
            running += AssemblyMembers[start + j];
            result[j] = running;
        }
        return result;
    }
}

/// <summary>
/// plan.md §4.3 — MessagePack partitions on <c>BrainDataPath</c>, partitioned by
/// LSH bucket, gzipped, written atomically (temp + rename, as the legacy storage
/// does). Append and compact; nothing readable ever reaches disk.
/// </summary>
public sealed class EngramStore
{
    private readonly string _root;
    private static readonly MessagePackSerializerOptions Options =
        MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.None);

    public EngramStore(string brainDataPath)
    {
        _root = brainDataPath;
        Directory.CreateDirectory(_root);
    }

    public string Root => _root;

    /// <summary>Partition filenames are the bucket id in hex — a number, not a concept.</summary>
    public string PathFor(uint bucket) => Path.Combine(_root, $"p{bucket:x8}.mpgz");

    public IEnumerable<string> PartitionFiles() =>
        Directory.Exists(_root)
            ? Directory.GetFiles(_root, "p*.mpgz").OrderBy(f => f, StringComparer.Ordinal)
            : Enumerable.Empty<string>();

    /// <summary>
    /// Atomic write: serialize to a temp file in the same directory, flush, then
    /// rename. A crash mid-write leaves either the old partition or the new one,
    /// never a half-written one.
    /// </summary>
    public void Save(EngramPartition partition)
    {
        var target = PathFor(partition.Bucket);
        var temp = target + ".tmp";

        using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var gz = new GZipStream(fs, CompressionLevel.Optimal))
        {
            MessagePackSerializer.Serialize(gz, partition, Options);
        }

        File.Move(temp, target, overwrite: true);
    }

    public EngramPartition? Load(uint bucket)
    {
        var path = PathFor(bucket);
        if (!File.Exists(path)) return null;

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var gz = new GZipStream(fs, CompressionMode.Decompress);
        return MessagePackSerializer.Deserialize<EngramPartition>(gz, Options);
    }

    /// <summary>
    /// Append recipes to a bucket, replacing any recipe whose id already exists.
    /// Read-modify-write of one partition, not of the store.
    /// </summary>
    public void Append(uint bucket, IReadOnlyList<NeuronRecipe> recipes)
    {
        var existing = Load(bucket);
        var merged = new Dictionary<uint, NeuronRecipe>();

        if (existing is not null)
            foreach (var r in existing.Recipes()) merged[r.Id] = r;
        foreach (var r in recipes) merged[r.Id] = r;

        var ordered = merged.Values.OrderBy(r => r.Id).ToList();
        var partition = EngramPartition.FromRecipes(bucket, ordered);

        if (existing is not null)
        {
            partition.AssemblyHashes = existing.AssemblyHashes;
            partition.AssemblyOffsets = existing.AssemblyOffsets;
            partition.AssemblyMembers = existing.AssemblyMembers;
        }
        Save(partition);
    }

    public long TotalBytes() => PartitionFiles().Sum(f => new FileInfo(f).Length);

    /// <summary>
    /// Recipes, deviations and synapses across the whole store. Store-level totals,
    /// not resident ones — a resumed run has different residency but should hold the
    /// same learned state, so this is the metric equivalence is judged on.
    /// </summary>
    public (int recipes, long deviations, long synapses) Totals()
    {
        int recipes = 0;
        long deviations = 0, synapses = 0;
        foreach (var f in PartitionFiles())
        {
            using var fs = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var gz = new GZipStream(fs, CompressionMode.Decompress);
            var p = MessagePackSerializer.Deserialize<EngramPartition>(gz, Options);
            recipes += p.RecipeCount;
            deviations += p.DeviationDims.Length;
            synapses += p.SynapseTargets.Length;
        }
        return (recipes, deviations, synapses);
    }

    public int TotalRecipes() => Totals().recipes;

    public void DeleteAll()
    {
        foreach (var f in PartitionFiles()) File.Delete(f);
    }
}
