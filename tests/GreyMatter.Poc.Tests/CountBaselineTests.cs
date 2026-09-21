using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Storage;
using GreyMatter.Poc.Substrate;
using GreyMatter.Poc.Utility;
using Xunit;

namespace GreyMatter.Poc.Tests;

/// <summary>CB count baseline: counts, cap, least-count displacement, no decay, persistence. A baseline, not learning.</summary>
public sealed class CountBaselineTests
{
    private static (uint Target, float Weight)[] Edges(IRelayRecords records, uint id)
    {
        var buffer = new byte[RelayRecord.Bytes]; if (!records.Read(id, buffer)) return Array.Empty<(uint, float)>();
        var syn = new SynapseStore(1, 32); RelayRecord.Decode(id, buffer, syn);
        return Enumerable.Range(0, syn.Degree[0]).Select(i => (syn.Target[i], syn.Weight[i])).ToArray();
    }
    private static void Teach(StoredRelayLearning l, uint a, uint b) { l.ObserveMembers(new[] { a }); l.ObserveMembers(new[] { b }); l.EndSequence(); }

    [Fact]
    public void CountsAccumulateAndNeverDecay()
    {
        using var records = new ResidentRelayRecords(1000);
        var learner = new StoredRelayLearning(records) { CountBaseline = true };
        for (int i = 0; i < 7; i++) Teach(learner, 1, 2);
        for (int i = 0; i < 3000; i++) Teach(learner, 1, 3);   // would erase 1→2 under source-local
        var edges = Edges(records, 1).ToDictionary(e => e.Target, e => e.Weight);
        Assert.Equal(7f, edges[2]); Assert.Equal(3000f, edges[3]);
        Assert.Throws<InvalidOperationException>(() => learner.Decay());
        Assert.Equal(RelayLearningPolicy.CountBaseline, learner.Capture().Policy);
    }

    [Fact]
    public void ThirtyTwoSuccessorsFitAndLeastCountDisplacementOnlyEvictsCountOne()
    {
        using var records = new ResidentRelayRecords(1000);
        var learner = new StoredRelayLearning(records) { CountBaseline = true };
        for (uint t = 100; t < 132; t++) for (int i = 0; i < 2; i++) Teach(learner, 1, t);   // 32 successors, count 2 each
        Assert.Equal(32, Edges(records, 1).Length);
        Teach(learner, 1, 500);                       // every incumbent has count 2 → declined
        Assert.DoesNotContain(500u, Edges(records, 1).Select(e => e.Target));
        Assert.Equal(1, learner.CountDeclined);
        using var other = new ResidentRelayRecords(1000);
        var second = new StoredRelayLearning(other) { CountBaseline = true };
        for (uint t = 100; t < 132; t++) Teach(second, 1, t);   // count 1 each
        Teach(second, 1, 500);                        // displaces the lowest-index count-1 incumbent (100)
        var targets = Edges(other, 1).Select(e => e.Target).ToArray();
        Assert.Contains(500u, targets); Assert.DoesNotContain(100u, targets); Assert.Equal(32, targets.Length);
        Assert.Equal(1, second.CountDisplaced);
    }

    [Fact]
    public void TextModelUsesOneRecordPerTokenAndRanksByTransitionProbability()
    {
        string root = Path.Combine(Path.GetTempPath(), "gm-count-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            string source = Path.Combine(root, "s.txt");
            File.WriteAllText(source, "amber birch\namber birch\namber cedar\nbirch dogwood\n");
            string model = Path.Combine(root, "model");
            LocalModel.Train(source, "text", model, 201, 128, "count-baseline");
            var saved = LocalModel.Open(model, 128); using var store = saved.Store;
            Assert.Equal(RelayLearningPolicy.CountBaseline, saved.Policy);
            var recall = LocalModel.Query(store, 201, "amber", new[] { "birch", "cedar", "quartz" }, 1, saved.Policy);
            var score = recall.Results.ToDictionary(h => h.Candidate, h => h.Score);
            Assert.Equal(2d / 3, score["birch"], 6); Assert.Equal(1d / 3, score["cedar"], 6); Assert.Equal(0, score["quartz"]);
            var two = LocalModel.Query(store, 201, "amber", new[] { "dogwood" }, 2, saved.Policy);
            Assert.Equal(2d / 3, two.Results.Single().Score, 6);  // amber→birch (2/3) then birch→dogwood (1) via the .5 emit threshold
            long records = 0; store.VisitPresent(_ => records++);
            Assert.Equal(4, records);   // one record per distinct token, not eight
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void ValidatorKeepsRelayEdgesWithinUnitAndCountEdgesIntegral()
    {
        var syn = new SynapseStore(1, 32); var b = new byte[RelayRecord.Bytes];
        syn.Degree[0] = 1; syn.Target[0] = 9; syn.Weight[0] = 5f; syn.Population[0] = (byte)SynapsePopulation.CrossCue;
        RelayRecord.Encode(1, syn, b); Assert.Throws<InvalidDataException>(() => RelayRecord.Validate(1, b));   // relay edge > 1
        syn.Population[0] = RelayRecord.CountPopulation; RelayRecord.Encode(1, syn, b); RelayRecord.Validate(1, b);  // count edge 5 ok
        syn.Weight[0] = 2.5f; RelayRecord.Encode(1, syn, b); Assert.Throws<InvalidDataException>(() => RelayRecord.Validate(1, b)); // fractional count
        syn.Weight[0] = 1f; syn.Population[0] = 4; RelayRecord.Encode(1, syn, b); Assert.Throws<InvalidDataException>(() => RelayRecord.Validate(1, b)); // unknown provenance
    }
}
