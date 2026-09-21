using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Storage;
using GreyMatter.Poc.Substrate;
using GreyMatter.Poc.Utility;
using Xunit;

namespace GreyMatter.Poc.Tests;

/// <summary>D1 sparse relay: connectivity, capacity, persistence. No quality claim.</summary>
public sealed class SparseRelayTests
{
    private static uint[] Cohort(uint first) => Enumerable.Range(0, 8).Select(i => first + (uint)i).ToArray();
    private static int Degree(IRelayRecords records, uint id)
    {
        var buffer = new byte[RelayRecord.Bytes]; if (!records.Read(id, buffer)) return 0;
        var syn = new SynapseStore(1, 32); RelayRecord.Decode(id, buffer, syn); return syn.Degree[0];
    }
    private static HashSet<uint> Targets(IRelayRecords records, uint id)
    {
        var buffer = new byte[RelayRecord.Bytes]; var set = new HashSet<uint>(); if (!records.Read(id, buffer)) return set;
        var syn = new SynapseStore(1, 32); RelayRecord.Decode(id, buffer, syn);
        for (int i = 0; i < syn.Degree[0]; i++) set.Add(syn.Target[i]); return set;
    }

    [Fact]
    public void SubsetIsDeterministicDistinctInsideCohortAndFullForSmallCohorts()
    {
        var cohort = Cohort(1000); Span<uint> a = stackalloc uint[2], b = stackalloc uint[2];
        Assert.Equal(2, StoredRelayLearning.SparseTargets(7, cohort, a));
        Assert.Equal(2, StoredRelayLearning.SparseTargets(7, cohort, b));
        Assert.Equal(a[0], b[0]); Assert.Equal(a[1], b[1]); Assert.NotEqual(a[0], a[1]);
        Assert.Contains(a[0], cohort); Assert.Contains(a[1], cohort);
        // Different source members spread over the cohort rather than collapsing onto one pair.
        var union = new HashSet<uint>();
        for (uint source = 0; source < 8; source++) { StoredRelayLearning.SparseTargets(source, cohort, a); union.Add(a[0]); union.Add(a[1]); }
        Assert.True(union.Count >= 4);
        Span<uint> one = stackalloc uint[2];
        Assert.Equal(1, StoredRelayLearning.SparseTargets(7, new uint[] { 5 }, one)); Assert.Equal(5u, one[0]);
    }

    [Fact]
    public void SparseWiringSpendsTwoSlotsPerSuccessorInsteadOfEight()
    {
        // Four successors sit inside every limit, so both policies retain all four; the
        // difference is slot cost: 4x8=32 (segment full) versus 4x2=8.
        (int reached, int degree) Teach(bool sparse)
        {
            using var records = new ResidentRelayRecords(100_000);
            var learner = new StoredRelayLearning(records) { SourceLocalForgetting = !sparse, SparseRelayConnectivity = sparse };
            var source = Cohort(10);
            for (uint k = 0; k < 4; k++) { learner.ObserveMembers(source); learner.ObserveMembers(Cohort(1000 + 100 * k)); learner.EndSequence(); }
            var reached = new HashSet<uint>();
            foreach (uint member in source) foreach (uint t in Targets(records, member)) reached.Add((t - 1000) / 100);
            return (reached.Count, source.Max(m => Degree(records, m)));
        }
        Assert.Equal((4, 32), Teach(false));
        Assert.Equal((4, 8), Teach(true));
    }

    [Fact]
    public void ForgettingNotSlotsBindsAboveSixInterleavedSuccessors()
    {
        // Registered D1 finding: with m successors interleaved, an edge's steady-state
        // weight is .005/(1-.99^(m-1)), below the .1 prune line once m >= 7. Sparse wiring
        // frees slots but cannot raise this ceiling; both policies stay at or below six.
        int Retained(bool sparse)
        {
            using var records = new ResidentRelayRecords(100_000);
            var learner = new StoredRelayLearning(records) { SourceLocalForgetting = !sparse, SparseRelayConnectivity = sparse };
            var source = Cohort(10);
            for (int round = 0; round < 8; round++)
                for (uint k = 0; k < 16; k++) { learner.ObserveMembers(source); learner.ObserveMembers(Cohort(1000 + 100 * k)); learner.EndSequence(); }
            var reached = new HashSet<uint>();
            foreach (uint member in source) foreach (uint t in Targets(records, member)) reached.Add((t - 1000) / 100);
            Assert.All(source, m => Assert.True(Degree(records, m) <= 32));
            return reached.Count;
        }
        Assert.InRange(Retained(false), 1, 6);
        Assert.InRange(Retained(true), 1, 6);
    }

    [Fact]
    public void GlobalDecayIsRefusedAndPolicyCannotBeOverridden()
    {
        using var records = new ResidentRelayRecords(1000);
        var learner = new StoredRelayLearning(records) { SparseRelayConnectivity = true };
        Assert.Throws<InvalidOperationException>(() => learner.Decay());
        Assert.Equal(RelayLearningPolicy.SparseRelay, learner.Capture().Policy);
        Assert.Throws<InvalidOperationException>(() => new StoredRelayLearning(records, learner.Capture()) { SourceLocalForgetting = true });
        Assert.Throws<InvalidOperationException>(() => new StoredRelayLearning(records, learner.Capture()) { SparseRelayConnectivity = false });
    }

    [Fact]
    public void TextModelPersistsSparsePolicyAndProbeReportsIt()
    {
        string root = Path.Combine(Path.GetTempPath(), "gm-sparse-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            string source = Path.Combine(root, "s.txt"); File.WriteAllText(source, "amber birch\nbirch cedar\ncedar dogwood\n");
            string model = Path.Combine(root, "model");
            LocalModel.Train(source, "text", model, 201, 128, "sparse-relay");
            var saved = LocalModel.Open(model, 128); using var store = saved.Store;
            Assert.Equal(RelayLearningPolicy.SparseRelay, saved.Policy);
            var recall = LocalModel.Query(store, 201, "amber", new[] { "dogwood", "quartz" }, 3);
            Assert.True(recall.Results.Single(h => h.Candidate == "dogwood").Score > 0);
            Assert.Equal(0, recall.Results.Single(h => h.Candidate == "quartz").Score);
            Assert.Throws<ArgumentException>(() => LocalModel.Train(source, "text", model + "-bad", 201, 128, "global"));
        }
        finally { Directory.Delete(root, true); }
    }
}
