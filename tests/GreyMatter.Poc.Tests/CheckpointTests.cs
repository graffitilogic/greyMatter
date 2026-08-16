using GreyMatter.Poc;
using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Engrams;
using GreyMatter.Poc.Pipeline;
using GreyMatter.Poc.Runtime;
using Xunit;

namespace GreyMatter.Poc.Tests;

public class CheckpointTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gm_p5_" + Path.GetRandomFileName());

    private Config Cfg() => new()
    {
        BaselineNeuronCount = 200_000,
        WorkingSetMax = 8_000,
        ActivationDepth = 3,
        ActivationWidth = 64,
        Seed = 12345,
        BrainDataPath = _dir
    };

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private static ContextEncoder Encoder(Config cfg)
    {
        var e = new ContextEncoder(cfg);
        Trainer.AccumulateContext(e, LocalSample.Sentences);
        return e;
    }

    [Fact]
    public void CheckpointRoundtripsTheCodebookExactly()
    {
        var cfg = Cfg();
        using (var scope = new ActivationScope(cfg))
        {
            new Trainer(cfg, scope, Encoder(cfg)).Run(LocalSample.Sentences, quiet: true);
            Checkpoint.Save(cfg, scope, new LshIndex(cfg.Seed), 80, codebookVersion: 3);

            var manifest = Checkpoint.Load(cfg)!;
            Assert.Equal(3, manifest.CodebookVersion);
            Assert.Equal(80, manifest.SentencesConsumed);
            Assert.Equal(scope.Codebook.Export(), manifest.Codebook);
        }
    }

    [Fact]
    public void ResumeRestoresLearnedSynapses()
    {
        var cfg = Cfg();
        long saved;
        using (var scope = new ActivationScope(cfg))
        {
            new Trainer(cfg, scope, Encoder(cfg)).Run(LocalSample.Sentences, quiet: true);
            scope.ConsolidateAll();
            saved = scope.Recipes.Values.Sum(r => (long)r.SynapseCount);
            Checkpoint.Save(cfg, scope, new LshIndex(cfg.Seed), 80, 0);
        }

        Assert.True(saved > 0, "nothing was learned, so the test proves nothing");

        var restored = Checkpoint.Resume(cfg);
        using var resumed = restored.Scope;
        Assert.Equal(80, restored.SentencesConsumed);
        Assert.True(resumed.Recipes.Values.Sum(r => (long)r.SynapseCount) > 0,
            "resume restored no synapses — learning did not survive the checkpoint");
    }

    /// <summary>
    /// The P3 finding made operational: recipes are meaningful only under the
    /// configuration that wrote them, so resume refuses rather than reinterpreting.
    /// </summary>
    [Theory]
    [InlineData("seed")]
    [InlineData("codebook")]
    [InlineData("space")]
    public void ResumeRefusesOnConfigurationMismatch(string what)
    {
        var cfg = Cfg();
        using (var scope = new ActivationScope(cfg))
        {
            new Trainer(cfg, scope, Encoder(cfg)).Run(LocalSample.Sentences, quiet: true);
            Checkpoint.Save(cfg, scope, new LshIndex(cfg.Seed), 80, 0);
        }

        var changed = Cfg();
        switch (what)
        {
            case "seed": changed.Seed += 1; break;
            case "codebook": changed.VqCodebookSize *= 2; break;
            case "space": changed.BaselineNeuronCount *= 2; break;
        }

        var ex = Assert.Throws<InvalidOperationException>(() => Checkpoint.Resume(changed));
        Assert.Contains("resume refuses", ex.Message);
    }

    [Fact]
    public void ResumeWithoutACheckpointThrowsClearly()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Checkpoint.Resume(Cfg()));
        Assert.Contains("no checkpoint", ex.Message);
    }

    [Fact]
    public void CheckpointLeavesNoTempFile()
    {
        var cfg = Cfg();
        using var scope = new ActivationScope(cfg);
        Checkpoint.Save(cfg, scope, new LshIndex(cfg.Seed), 1, 0);
        Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
    }

    [Fact]
    public void CheckpointRecordsTheResidentWorkingSet()
    {
        var cfg = Cfg();
        using var scope = new ActivationScope(cfg);
        new Trainer(cfg, scope, Encoder(cfg)).Run(LocalSample.Sentences, quiet: true);
        Checkpoint.Save(cfg, scope, new LshIndex(cfg.Seed), 80, 0);

        var manifest = Checkpoint.Load(cfg)!;
        Assert.Equal(scope.Pool.Count, manifest.ResidentIds.Length);
    }

    [Fact]
    public void SynapsesSurviveAPartitionRoundtrip()
    {
        var recipe = new NeuronRecipe(7, 2, 3)
        {
            SynapseTargets = new uint[] { 900_000, 12, 4 },
            SynapseWeights = new[] { 0.5f, 0.25f, 0.75f }
        };

        var store = new EngramStore(_dir);
        store.Save(EngramPartition.FromRecipes(1, new List<NeuronRecipe> { recipe }));
        var back = store.Load(1)!.RecipeAt(0);

        Assert.Equal(recipe.SynapseTargets, back.SynapseTargets);
        Assert.Equal(recipe.SynapseWeights, back.SynapseWeights);
    }

    [Fact]
    public void HydrateTruncatesToTheConfiguredCap()
    {
        // A recipe written under a larger cap must load without corrupting the
        // neighbouring segment.
        var store = new Substrate.SynapseStore(slotCapacity: 4, capPerNeuron: 2);
        store.Hydrate(0, new uint[] { 1, 2, 3, 4, 5 }, new[] { 1f, 1f, 1f, 1f, 1f });

        Assert.Equal(2, store.Degree[0]);
        Assert.Equal(0, store.Degree[1]);
    }

    [Fact]
    public void RecipesWithOnlySynapsesArePersisted()
    {
        // Filtering persistence on deviations alone dropped every neuron that had
        // learned connections without its own field drifting past threshold —
        // which, at DeviationThreshold 1.0, is all of them.
        var cfg = Cfg();
        using var scope = new ActivationScope(cfg);
        new Trainer(cfg, scope, Encoder(cfg)).Run(LocalSample.Sentences, quiet: true);
        scope.ConsolidateAll();

        var dirty = scope.DirtyRecipes().ToList();
        Assert.NotEmpty(dirty);
        Assert.All(dirty, r => Assert.True(r.HasLearnedState));
        Assert.Contains(dirty, r => r.DeviationCount == 0 && r.SynapseCount > 0);
    }

    /// <summary>
    /// The premature-truncation defect: pre-testing <c>Count == Capacity</c> refused
    /// every materialization once the pool first filled, freezing the working set
    /// and preventing the evict/regenerate cycle from ever running.
    /// </summary>
    [Fact]
    public void WorkingSetTurnsOverRatherThanFreezingWhenFull()
    {
        var cfg = Cfg();
        cfg.WorkingSetMax = 2_000;
        using var scope = new ActivationScope(cfg);
        new Trainer(cfg, scope, Encoder(cfg)).Run(
            Enumerable.Repeat(LocalSample.Sentences, 3).SelectMany(x => x), quiet: true);

        Assert.True(scope.Pool.TotalEvicted > 0, "the pool never evicted — it froze when full");
        Assert.True(scope.Pool.TotalMaterialized > cfg.WorkingSetMax,
            $"only {scope.Pool.TotalMaterialized} materializations for a {cfg.WorkingSetMax} pool: " +
            "the working set never turned over");
    }
}
