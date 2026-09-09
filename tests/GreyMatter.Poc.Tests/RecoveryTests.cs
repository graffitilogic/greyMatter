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

    [Fact]
    public void SelectionObserverReportsBeforeClearWithoutChangingDrive()
    {
        var cfg=new GreyMatter.Poc.Config { Sparsity=1,WorkingSetMax=64,ActivationWidth=32,ActivationDepth=1 };
        using var scope=new GreyMatter.Poc.Runtime.ActivationScope(cfg);
        var code=new GreyMatter.Poc.Encoding.SparseCode(new[]{7});
        var run=new GreyMatter.Poc.Runtime.Cascade(cfg,scope);
        int positive=0,selected=0;
        run.SelectionObserver=(step,id,mass,won)=>{ if(mass>0) positive++; if(won) selected++; };
        var a=run.Run(code,false); var drives=run.DeliveredDrive.ToArray();
        run.SelectionObserver=null; var b=run.Run(code,false);
        Assert.Equal(8,positive); Assert.Equal(8,selected);
        Assert.Equal(a.TotalMass,b.TotalMass); Assert.Equal(drives,run.DeliveredDrive.ToArray());
    }

    [Fact]
    public void RecallSnapshotPreservesGraphAndRefusesOverwrite()
    {
        string path=Path.Combine(Path.GetTempPath(),$"gm-t2-{Guid.NewGuid():N}.bin");
        try
        {
            var cfg=new GreyMatter.Poc.Config {WorkingSetMax=64,Sparsity=1,ActivationWidth=16};
            using var scope=new GreyMatter.Poc.Runtime.ActivationScope(cfg);
            int slot=scope.Materialize(123);scope.Materialize(456);
            scope.Synapses.Hydrate(slot,new uint[]{456},new[]{.375f},new byte[]{2});
            scope.Pool.Familiarity[slot]=.25f;
            TravelTimeReview.Save(path,cfg,scope);
            Assert.Throws<IOException>(()=>TravelTimeReview.Save(path,cfg,scope));
            var loaded=TravelTimeReview.Load(path);using var other=loaded.scope;
            int restored=other.Pool.Find(123);
            Assert.Equal(.25f,other.Pool.Familiarity[restored]);
            int start=other.Synapses.SegmentStart(restored);
            Assert.Equal(456u,other.Synapses.Target[start]);Assert.Equal(.375f,other.Synapses.Weight[start]);
            Assert.Equal((byte)2,other.Synapses.Population[start]);
        }
        finally {if(File.Exists(path)) File.Delete(path);}
    }

    [Fact]
    public void FrozenGraphSearchHonorsDirectionDepthCyclesAndUnreachableNodes()
    {
        var cfg=new GreyMatter.Poc.Config {WorkingSetMax=64};
        using var scope=new GreyMatter.Poc.Runtime.ActivationScope(cfg);
        foreach(uint id in new uint[]{1,2,3,4}) scope.Materialize(id);
        scope.Synapses.Hydrate(scope.Pool.Find(1),new uint[]{2},new[]{.2f});
        scope.Synapses.Hydrate(scope.Pool.Find(2),new uint[]{3},new[]{.2f});
        scope.Synapses.Hydrate(scope.Pool.Find(3),new uint[]{1},new[]{.2f});
        var shortSearch=RouteReview.BreadthFirst(scope,new uint[]{1},1);
        Assert.Equal(-1,shortSearch.Distance[scope.Pool.Find(3)]);
        var all=RouteReview.BreadthFirst(scope,new uint[]{1,1},12);
        Assert.Equal(2,all.Distance[scope.Pool.Find(3)]);
        Assert.Equal(-1,all.Distance[scope.Pool.Find(4)]);
        Assert.Equal(scope.Pool.Find(2),all.Parent[scope.Pool.Find(3)]);
    }

    [Fact]
    public void DeliveredDriveCarriesNothingFromAnEarlierQueryAcrossEviction()
    {
        // An UNWIRED cue has no outgoing synapses, so it can deliver exactly zero
        // drive however the pool churned before it.
        var cfg=new GreyMatter.Poc.Config { Sparsity=1,WorkingSetMax=256,ActivationWidth=16,ActivationDepth=2 };
        using var scope=new GreyMatter.Poc.Runtime.ActivationScope(cfg);
        for(uint id=1;id<=240;id++) scope.Materialize(id);
        var wired=new GreyMatter.Poc.Encoding.SparseCode(new[]{7});
        var members=GreyMatter.Poc.Runtime.Assembly.Members(wired,cfg.BaselineNeuronCount);
        foreach(uint id in members) scope.Materialize(id);
        for(int i=0;i<members.Length-1;i++)
            scope.Synapses.Hydrate(scope.Pool.Find(members[i]),new[]{members[i+1]},new[]{1f});
        var run=new GreyMatter.Poc.Runtime.Cascade(cfg,scope);
        Assert.True(run.Run(wired,false).TotalMass>0);
        int high=scope.Pool.Count;

        // Churn with unrelated neurons until an eviction batch has dropped Count
        // below the point the wired query wrote to.
        scope.Pool.AdvanceTick();
        for(uint id=1000;id<1400 && scope.Pool.Count>=high-4;id++) scope.Materialize(id);
        int low=scope.Pool.Count;

        var unwired=new GreyMatter.Poc.Encoding.SparseCode(new[]{9});
        run.Run(unwired,false);
        double total=0;
        for(int slot=0;slot<scope.Pool.Count;slot++) total+=run.DeliveredDrive[slot];
        Assert.True(scope.Pool.TotalEvicted>0,"scenario requires eviction");
        Assert.Equal(0d,total);
    }
}
