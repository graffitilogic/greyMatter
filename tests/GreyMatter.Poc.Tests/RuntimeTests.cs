using GreyMatter.Poc;
using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Pipeline;
using GreyMatter.Poc.Runtime;
using Xunit;

namespace GreyMatter.Poc.Tests;

public class RuntimeTests
{
    private static Config Cfg(int workingSet = 20_000) => new()
    {
        BaselineNeuronCount = 1_000_000,
        WorkingSetMax = workingSet,
        ActivationDepth = 4,
        ActivationWidth = 256,
        Seed = 12345
    };

    private static ContextEncoder TrainedEncoder(Config cfg)
    {
        var enc = new ContextEncoder(cfg);
        Trainer.AccumulateContext(enc, LocalSample.Sentences);
        return enc;
    }

    // ── Assembly ────────────────────────────────────────────────────────────

    [Fact]
    public void Assembly_IsDeterministicForACode()
    {
        var code = new SparseCode(new[] { 1, 5, 9 });
        Assert.Equal(Assembly.Members(code, 1_000_000), Assembly.Members(code, 1_000_000));
    }

    [Fact]
    public void Assembly_DoesNotDependOnTheRunSeed()
    {
        // Assemblies must be stable across runs and processes, or a store written
        // by one run is meaningless to the next.
        var code = new SparseCode(new[] { 3, 7 });
        var a = Assembly.Members(code, 500_000);
        var b = Assembly.Members(code, 500_000);
        Assert.Equal(a, b);
    }

    /// <summary>
    /// The defect that made the first P4 gate return AUC 0.500: per-dimension
    /// membership meant codes sharing a dim shared neurons, so a held-out control
    /// shared 256 of 256 members with the trained cues and no word was ever
    /// genuinely untrained. Hash-derived membership must keep distinct codes apart.
    /// </summary>
    [Fact]
    public void DistinctCodesRecruitLargelyDistinctNeurons()
    {
        var a = Assembly.Members(new SparseCode(Enumerable.Range(0, 32).ToArray()), 1_000_000).ToHashSet();

        // One dimension different — under the old scheme this shared 31/32 slices.
        var nearly = Enumerable.Range(0, 32).Select(i => i == 31 ? 99 : i).ToArray();
        var b = Assembly.Members(new SparseCode(nearly), 1_000_000);

        int shared = b.Count(a.Contains);
        Assert.True(shared < b.Length / 10,
            $"expected near-disjoint assemblies, {shared}/{b.Length} shared");
    }

    [Fact]
    public void AssemblyMembersStayInsideTheVirtualSpace()
    {
        foreach (var m in Assembly.Members(new SparseCode(new[] { 1, 2, 3 }), 1000))
            Assert.InRange(m, 0u, 999u);
    }

    // ── Cascade ─────────────────────────────────────────────────────────────

    [Fact]
    public void UntrainedCascade_ReturnsOnlyTheCueDrive()
    {
        // With no synapses there is nothing to propagate, so mass is exactly the
        // assembly's initial drive. This is the untrained-brain ceiling, and it is
        // what makes the recall lift interpretable.
        var cfg = Cfg();
        using var scope = new ActivationScope(cfg);
        var cascade = new Cascade(cfg, scope);

        var readout = cascade.Run(new SparseCode(Enumerable.Range(0, 32).ToArray()), learningMode: false);
        Assert.Equal(Assembly.Size(32), readout.TotalMass, 1);
        Assert.Equal(0, readout.Truncated);
    }

    /// <summary>
    /// Potentials must not survive a cascade. When they did, charge accumulated
    /// across every cue in a run, overflowed to Infinity, and the recall eval
    /// reported "trained mass NaN" and AUC 0.000 — arithmetic overflow wearing the
    /// costume of a null result.
    /// </summary>
    [Fact]
    public void RepeatedProbesDoNotAccumulateCharge()
    {
        var cfg = Cfg();
        using var scope = new ActivationScope(cfg);
        var cascade = new Cascade(cfg, scope);
        var code = new SparseCode(Enumerable.Range(0, 32).ToArray());

        var first = cascade.Run(code, learningMode: false).TotalMass;
        for (int i = 0; i < 200; i++) cascade.Run(code, learningMode: false);
        var last = cascade.Run(code, learningMode: false).TotalMass;

        Assert.Equal(first, last, 3);
        Assert.False(float.IsNaN(last) || float.IsInfinity(last));
    }

    [Fact]
    public void ProbeResultDoesNotDependOnWhatWasProbedBefore()
    {
        var cfg = Cfg();
        using var scope = new ActivationScope(cfg);
        var cascade = new Cascade(cfg, scope);

        var a = new SparseCode(Enumerable.Range(0, 32).ToArray());
        var b = new SparseCode(Enumerable.Range(100, 32).ToArray());

        var aAlone = cascade.Run(a, learningMode: false).TotalMass;
        cascade.Run(b, learningMode: false);
        var aAfterB = cascade.Run(a, learningMode: false).TotalMass;

        Assert.Equal(aAlone, aAfterB, 3);
    }

    [Fact]
    public void MassStaysFiniteUnderHeavyTraining()
    {
        var cfg = Cfg();
        var encoder = TrainedEncoder(cfg);
        using var scope = new ActivationScope(cfg);
        var trainer = new Trainer(cfg, scope, encoder);
        trainer.Run(Enumerable.Repeat(LocalSample.Sentences, 6).SelectMany(x => x), quiet: true);

        var cascade = new Cascade(cfg, scope);
        var mass = cascade.Run(encoder.Encode("water"), learningMode: false).TotalMass;
        Assert.False(float.IsNaN(mass) || float.IsInfinity(mass), $"mass was {mass}");
    }

    // ── Learning ────────────────────────────────────────────────────────────

    [Fact]
    public void TrainingCreatesSynapses()
    {
        var cfg = Cfg();
        var encoder = TrainedEncoder(cfg);
        using var scope = new ActivationScope(cfg);
        var trainer = new Trainer(cfg, scope, encoder);
        var stats = trainer.Run(LocalSample.Sentences, quiet: true);

        Assert.True(stats.Synapses > 0, "training produced no synapses");
    }

    /// <summary>
    /// Consolidation must not destroy the graph. It did: ClearSlot lived inside
    /// ConsolidateSlot, so ConsolidateAll — which runs at checkpoint and shutdown on
    /// RESIDENT neurons — wiped every synapse. 241M Hebbian updates reported 0
    /// synapses.
    /// </summary>
    [Fact]
    public void ConsolidationPreservesResidentSynapses()
    {
        var cfg = Cfg();
        var encoder = TrainedEncoder(cfg);
        using var scope = new ActivationScope(cfg);
        var trainer = new Trainer(cfg, scope, encoder);
        trainer.Run(LocalSample.Sentences, quiet: true);

        var before = scope.Synapses.TotalSynapses;
        scope.ConsolidateAll();
        Assert.Equal(before, scope.Synapses.TotalSynapses);
    }

    [Fact]
    public void TrainedCueOutscoresAnUntrainedOne()
    {
        var cfg = Cfg();
        var encoder = TrainedEncoder(cfg);
        using var scope = new ActivationScope(cfg);
        var trainer = new Trainer(cfg, scope, encoder) { HeldOut = new HashSet<string> { "water" } };
        trainer.Run(LocalSample.Sentences, quiet: true);

        var cascade = new Cascade(cfg, scope);
        var trained = cascade.Run(encoder.Encode("sleeps"), learningMode: false).TotalMass;
        var held = cascade.Run(encoder.Encode("water"), learningMode: false).TotalMass;

        Assert.True(trained > held, $"trained {trained:F1} should beat held-out {held:F1}");
    }

    [Fact]
    public void HeldOutWordsAreSkippedByTheTrainer()
    {
        var cfg = Cfg();
        var encoder = TrainedEncoder(cfg);
        using var scope = new ActivationScope(cfg);
        var trainer = new Trainer(cfg, scope, encoder) { HeldOut = new HashSet<string> { "the", "water" } };
        var stats = trainer.Run(LocalSample.Sentences, quiet: true);

        Assert.True(stats.Skipped > 0);
    }

    [Fact]
    public void EndSequence_BreaksTheTemporalTrace()
    {
        // Without this the last word of one sentence wires to the first of the next,
        // manufacturing bigrams the corpus never contained.
        var cfg = Cfg();
        var encoder = TrainedEncoder(cfg);
        using var scope = new ActivationScope(cfg);
        var plasticity = new Plasticity(cfg, scope);
        var cascade = new Cascade(cfg, scope);

        var r = cascade.Run(encoder.Encode("water"), learningMode: true);
        plasticity.Learn(cascade.Winners(r.WinnerCount), cascade.WinnerScores(r.WinnerCount));
        var afterFirst = plasticity.SequenceUpdates;

        plasticity.EndSequence();

        r = cascade.Run(encoder.Encode("time"), learningMode: true);
        plasticity.Learn(cascade.Winners(r.WinnerCount), cascade.WinnerScores(r.WinnerCount));

        Assert.Equal(afterFirst, plasticity.SequenceUpdates);
    }

    [Fact]
    public void WorkingSetIsRespectedUnderTrainingPressure()
    {
        var cfg = Cfg(workingSet: 3_000);
        var encoder = TrainedEncoder(cfg);
        using var scope = new ActivationScope(cfg);
        var trainer = new Trainer(cfg, scope, encoder);
        var stats = trainer.Run(Enumerable.Repeat(LocalSample.Sentences, 3).SelectMany(x => x), quiet: true);

        Assert.True(stats.WorkingSetHighWater <= cfg.WorkingSetMax);
    }

    [Fact]
    public void TrainingIsReproducibleForAGivenSeed()
    {
        static long Synapses()
        {
            var cfg = Cfg();
            var encoder = TrainedEncoder(cfg);
            using var scope = new ActivationScope(cfg);
            return new Trainer(cfg, scope, encoder).Run(LocalSample.Sentences, quiet: true).Synapses;
        }
        Assert.Equal(Synapses(), Synapses());
    }

    [Fact]
    public void UnvisitedNeuronsStillHaveARegenerableRecipe()
    {
        // A virtual space larger than RAM means most neurons have never been seen.
        var cfg = Cfg();
        using var scope = new ActivationScope(cfg);
        var recipe = scope.RecipeFor(987_654);
        Assert.Equal(987_654u, recipe.Id);
        Assert.Equal(recipe.VqCode, scope.RecipeFor(987_654).VqCode);
    }
}
