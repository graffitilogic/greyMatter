using GreyMatter.Poc.Eval;
using Xunit;
namespace GreyMatter.Poc.Tests;
public class RecoveryTests
{
    [Fact]
    public void DirectedReadOnlyTraversalFixtures()
    {
        var checks = RecoveryEval.Fixtures();
        Assert.True(checks.All(c => c.Passed), string.Join("\n", checks.Where(c => !c.Passed)));
    }

    [Fact]
    public void TiedScoresDoNotFavorTheAnswerOrCandidateOrder()
    {
        var tied = Enumerable.Repeat(0d,32).ToArray();
        for (int i=0;i<32;i++) Assert.Equal(1d/32, RecoveryLearning.ScoreRank(tied,i).Top1);
        var rank=RecoveryLearning.ScoreRank(new[]{2d,1d,1d,0d},1);
        Assert.Equal(0d,rank.Top1);
        Assert.Equal((1d/2+1d/3)/2,rank.ReciprocalRank,12);
    }

    [Fact]
    public void ComposedAnswersAreNeverDirectTrainingPairsAndCandidatesAreBalanced()
    {
        var data=RecoveryLearning.Generate(100);
        Assert.Equal(data.Episodes,RecoveryLearning.Generate(100).Episodes);
        var pairs=data.Episodes.ToHashSet();
        var counts=data.Episodes.SelectMany(x=>x.Split(' ')).GroupBy(x=>x).ToDictionary(g=>g.Key,g=>g.Count());
        Assert.Equal(128,data.Queries.Count(q=>q.Hops==1));
        Assert.Equal(128,data.Queries.Count(q=>q.Hops>1));
        foreach(var q in data.Queries)
        {
            if(q.Hops>1) Assert.DoesNotContain($"{data.Symbols[q.Chain][q.Start]} {data.Symbols[q.Chain][q.Start+q.Hops]}",pairs);
            Assert.Single(data.Symbols.Select(c=>counts[c[q.Start+q.Hops]]).Distinct());
        }
        var nullTokens=RecoveryLearning.NullEpisodes(data,100).SelectMany(x=>x.Split(' ')).OrderBy(x=>x);
        Assert.Equal(data.Episodes.SelectMany(x=>x.Split(' ')).OrderBy(x=>x),nullTokens);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExperimentalTraceCreditsObservedMembersAndRespectsSequenceBoundary(bool observed)
    {
        var cfg=new GreyMatter.Poc.Config { Sparsity=1, ActivationWidth=1, WorkingSetMax=64,
            SequenceUsesCueMembers=observed };
        using var scope=new GreyMatter.Poc.Runtime.ActivationScope(cfg);
        var before=Enumerable.Range(1,8).Select(i=>(uint)i).ToArray();
        var after=Enumerable.Range(9,8).Select(i=>(uint)i).ToArray();
        foreach(var id in before.Concat(after)) scope.Materialize(id);
        var learner=new GreyMatter.Poc.Runtime.Plasticity(cfg,scope);
        learner.Learn(new[]{scope.Pool.Find(1)},new[]{1f},before);
        learner.Learn(new[]{scope.Pool.Find(9)},new[]{1f},after);
        Assert.Equal(observed?64:1,learner.SequenceUpdates);
        int slot=scope.Pool.Find(2), start=scope.Synapses.SegmentStart(slot);
        var targets=scope.Synapses.Target.Skip(start).Take(scope.Synapses.Degree[slot]);
        Assert.Equal(observed,targets.Contains(10u));
        long updates=learner.SequenceUpdates;
        learner.EndSequence();
        learner.Learn(new[]{scope.Pool.Find(1)},new[]{1f},before);
        Assert.Equal(updates,learner.SequenceUpdates);
    }
}
