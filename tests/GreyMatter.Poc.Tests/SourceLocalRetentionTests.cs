using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Storage;
using GreyMatter.Poc.Substrate;
using Xunit;

namespace GreyMatter.Poc.Tests;
public sealed class SourceLocalRetentionTests
{
    private static void Teach(StoredRelayLearning learner, uint a, uint b)
    { learner.ObserveMembers(new[] { a }); learner.ObserveMembers(new[] { b }); learner.EndSequence(); }
    private static float? Weight(IRelayRecords records, uint from, uint to)
    {
        var buffer = new byte[RelayRecord.Bytes]; if (!records.Read(from, buffer)) return null;
        var syn = new SynapseStore(1, 32); RelayRecord.Decode(from, buffer, syn);
        for (int i = 0; i < syn.Degree[0]; i++) if (syn.Target[i] == to) return syn.Weight[i];
        return null;
    }
    [Fact]
    public void DormantSourceSurvivesAndCompetingEvidenceRemovesOldEdge()
    {
        using var records = new ResidentRelayRecords(100);
        var learner = new StoredRelayLearning(records) { SourceLocalForgetting = true };
        for (int i = 0; i < 16; i++) Teach(learner, 1, 2);
        float before = Weight(records, 1, 2)!.Value;
        for (int i = 0; i < 10000; i++) Teach(learner, 10, 11);
        Assert.Equal(before, Weight(records, 1, 2));
        for (int i = 0; i < 64; i++) Teach(learner, 1, 3);
        Assert.Null(Weight(records, 1, 2)); Assert.True(Weight(records, 1, 3) > .1f);
        Assert.Throws<InvalidOperationException>(() => learner.Decay());
    }
    [Fact]
    public void AlternatingSuccessorsRemainAndExplicitContinuationIsExact()
    {
        using var a = new ResidentRelayRecords(100); using var b = new ResidentRelayRecords(100);
        var learner = new StoredRelayLearning(a) { SourceLocalForgetting = true };
        for (int i = 0; i < 16; i++) Teach(learner, 1, 2);
        learner.ObserveMembers(new uint[] { 1 });
        var buffer = new byte[RelayRecord.Bytes]; a.VisitPresent(id => { a.Read(id, buffer); b.Write(id, buffer); });
        var resumed = new StoredRelayLearning(b, learner.Capture()) { SourceLocalForgetting = true };
        foreach (var run in new[] { learner, resumed })
        {
            run.ObserveMembers(new uint[] { 3 }); run.EndSequence();
            for (int i = 0; i < 16; i++) { Teach(run, 1, 2); Teach(run, 1, 3); }
        }
        Assert.True(Weight(a, 1, 2) > .1f); Assert.True(Weight(a, 1, 3) > .1f);
        var other = new byte[RelayRecord.Bytes]; a.VisitPresent(id => { a.Read(id, buffer); b.Read(id, other); Assert.Equal(buffer, other); });
    }
    [Fact]
    public void SelectivePruningKeepsObservedTargetsAndPopulationAligned()
    {
        var syn = new SynapseStore(1, 32);
        syn.Hydrate(0, new uint[] { 2, 3, 4, 5 }, new float[] { .1f, .1f, .2f, .1f });
        syn.Population[1] = (byte)SynapsePopulation.CrossCue;
        Assert.Equal(2, syn.DecayUnobserved(0, new uint[] { 3 }));
        Assert.Equal(2, syn.Degree[0]);
        int observed = Array.IndexOf(syn.Target, 3u, 0, syn.Degree[0]);
        Assert.Equal(.1f, syn.Weight[observed]); Assert.Equal((byte)SynapsePopulation.CrossCue, syn.Population[observed]);
        int other = Array.IndexOf(syn.Target, 4u, 0, syn.Degree[0]); Assert.Equal(.198f, syn.Weight[other], 6);
    }
}
