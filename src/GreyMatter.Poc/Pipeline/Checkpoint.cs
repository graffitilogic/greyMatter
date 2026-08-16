using System.IO.Compression;
using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Engrams;
using GreyMatter.Poc.Runtime;
using MessagePack;

namespace GreyMatter.Poc.Pipeline;

/// <summary>
/// The checkpoint manifest. Binary and numeric like everything else in
/// <c>BrainDataPath</c> — no strings, and it is covered by <c>gm audit --strings</c>.
/// </summary>
[MessagePackObject]
public sealed class CheckpointManifest
{
    /// <summary>
    /// Bumped whenever the codebook changes. **This is the P3 finding made
    /// operational:** a recipe's deviations are meaningful only relative to the
    /// codebook version they were consolidated against. Loading partitions written
    /// under one codebook against a different one produced 2,555,679 fidelity
    /// violations with a max error of 49.7. Resume therefore refuses on mismatch
    /// rather than producing plausible-looking wrong weights.
    /// </summary>
    [Key(0)] public int CodebookVersion { get; set; }

    /// <summary>Flattened codebook — the authority a resumed run regenerates against.</summary>
    [Key(1)] public float[] Codebook { get; set; } = Array.Empty<float>();

    [Key(2)] public long SentencesConsumed { get; set; }
    [Key(3)] public int Seed { get; set; }
    [Key(4)] public int CodebookSize { get; set; }
    [Key(5)] public int SurfaceDimensions { get; set; }
    [Key(6)] public int Sparsity { get; set; }
    [Key(7)] public int BaselineNeuronCount { get; set; }
    [Key(8)] public int SynapseCapPerNeuron { get; set; }
    [Key(9)] public long RecipeCount { get; set; }
    [Key(10)] public long SynapseCount { get; set; }

    /// <summary>LSH entries, flattened as parallel arrays.</summary>
    [Key(11)] public uint[] LshBuckets { get; set; } = Array.Empty<uint>();
    [Key(12)] public ulong[] LshAssemblies { get; set; } = Array.Empty<ulong>();

    /// <summary>
    /// Virtual ids of the resident working set, in ascending last-active order.
    ///
    /// Residency is transient state, but it is not irrelevant: Hebbian wiring only
    /// happens between CO-RESIDENT neurons, so a run that resumes with an empty pool
    /// forms different synapses from one that never stopped. Measured without this:
    /// an interrupted run diverged from an uninterrupted one by 16% on both recipe
    /// and synapse counts.
    /// </summary>
    [Key(13)] public uint[] ResidentIds { get; set; } = Array.Empty<uint>();
}

/// <summary>
/// plan.md P5 — checkpoint and resume.
///
/// A checkpoint is the manifest plus the engram partitions the trainer has already
/// written. Resume reconstitutes the codebook FIRST (so every regeneration agrees
/// with what was consolidated), then loads recipes, then replays the corpus from
/// the sentence it left off at.
/// </summary>
public static class Checkpoint
{
    public const string ManifestName = "checkpoint.mpgz";

    private static readonly MessagePackSerializerOptions Options =
        MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.None);

    public static string PathFor(Config cfg) => Path.Combine(cfg.BrainDataPath, ManifestName);

    public static bool Exists(Config cfg) => File.Exists(PathFor(cfg));

    public static void Save(Config cfg, ActivationScope scope, LshIndex lsh, long sentencesConsumed,
                            int codebookVersion)
    {
        Directory.CreateDirectory(cfg.BrainDataPath);

        // Persist learned state before the manifest, so a crash between the two
        // leaves a manifest that under-claims rather than one that over-claims.
        long synapses = 0, recipes = 0;
        foreach (var r in scope.Recipes.Values)
            if (r.HasLearnedState) { recipes++; synapses += r.SynapseCount; }

        Trainer.Persist(cfg, scope, lsh);

        var entries = lsh.Entries().ToList();
        var manifest = new CheckpointManifest
        {
            CodebookVersion = codebookVersion,
            Codebook = scope.Codebook.Export(),
            SentencesConsumed = sentencesConsumed,
            Seed = cfg.Seed,
            CodebookSize = cfg.VqCodebookSize,
            SurfaceDimensions = cfg.SurfaceDimensions,
            Sparsity = cfg.Sparsity,
            BaselineNeuronCount = cfg.BaselineNeuronCount,
            SynapseCapPerNeuron = cfg.SynapseCapPerNeuron,
            RecipeCount = recipes,
            SynapseCount = synapses,
            LshBuckets = entries.Select(e => e.bucket).ToArray(),
            LshAssemblies = entries.Select(e => e.assembly).ToArray(),
            ResidentIds = ResidentInLruOrder(scope)
        };

        var target = PathFor(cfg);
        var temp = target + ".tmp";
        using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var gz = new GZipStream(fs, CompressionLevel.Optimal))
            MessagePackSerializer.Serialize(gz, manifest, Options);
        File.Move(temp, target, overwrite: true);
    }

    public static CheckpointManifest? Load(Config cfg)
    {
        var path = PathFor(cfg);
        if (!File.Exists(path)) return null;

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var gz = new GZipStream(fs, CompressionMode.Decompress);
        return MessagePackSerializer.Deserialize<CheckpointManifest>(gz, Options);
    }

    public sealed record Restored(ActivationScope Scope, LshIndex Lsh, long SentencesConsumed, int CodebookVersion);

    /// <summary>
    /// Reconstitute a run from its checkpoint. Refuses on any configuration
    /// mismatch that would make stored state mean something different — silently
    /// adapting would produce a brain that looks trained and is not.
    /// </summary>
    public static Restored Resume(Config cfg)
    {
        var manifest = Load(cfg)
            ?? throw new InvalidOperationException($"no checkpoint at {PathFor(cfg)}");

        Require(manifest.Seed, cfg.Seed, nameof(cfg.Seed));
        Require(manifest.CodebookSize, cfg.VqCodebookSize, nameof(cfg.VqCodebookSize));
        Require(manifest.SurfaceDimensions, cfg.SurfaceDimensions, nameof(cfg.SurfaceDimensions));
        Require(manifest.Sparsity, cfg.Sparsity, nameof(cfg.Sparsity));
        Require(manifest.BaselineNeuronCount, cfg.BaselineNeuronCount, nameof(cfg.BaselineNeuronCount));

        // The codebook is restored BEFORE any recipe is read, so every regeneration
        // in the resumed run uses the version the deviations were written against.
        var codebook = new VqCodebook(cfg.VqCodebookSize, cfg.SurfaceDimensions, cfg.Seed);
        codebook.Import(manifest.Codebook);

        var scope = new ActivationScope(cfg, codebook);
        var store = new EngramStore(cfg.BrainDataPath);
        foreach (var file in store.PartitionFiles())
        {
            var bucket = BucketOf(file);
            var partition = store.Load(bucket);
            if (partition is null) continue;
            foreach (var recipe in partition.Recipes()) scope.AdoptRecipe(recipe);
        }

        var lsh = new LshIndex(cfg.Seed);
        lsh.Load(manifest.LshBuckets.Zip(manifest.LshAssemblies));

        // Rebuild the working set in the order it was last active, so the resumed
        // run starts from the same residency — and therefore the same co-activation
        // opportunities — as the run it is continuing.
        foreach (var id in manifest.ResidentIds)
        {
            scope.AdvanceTick();
            if (scope.TryMaterialize(id) < 0) break;   // pool full; the rest were colder anyway
        }

        return new Restored(scope, lsh, manifest.SentencesConsumed, manifest.CodebookVersion);
    }

    private static void Require(int stored, int current, string name)
    {
        if (stored != current)
            throw new InvalidOperationException(
                $"checkpoint {name} is {stored} but the run is configured for {current}. " +
                "Stored recipes are only meaningful under the configuration that wrote them; " +
                "resume refuses rather than reinterpreting them.");
    }

    /// Ascending by last-active tick: the coldest first, so a smaller pool on
    /// resume keeps the warmest neurons.
    private static uint[] ResidentInLruOrder(ActivationScope scope)
    {
        var pool = scope.Pool;
        var ids = new (uint id, uint tick)[pool.Count];
        for (int i = 0; i < pool.Count; i++) ids[i] = (pool.VirtualId[i], pool.LastActiveTick[i]);
        Array.Sort(ids, (a, b) => a.tick.CompareTo(b.tick));
        return ids.Select(x => x.id).ToArray();
    }

    private static uint BucketOf(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);           // "p0a1b2c3"
        return Convert.ToUInt32(name[1..], 16);
    }
}
