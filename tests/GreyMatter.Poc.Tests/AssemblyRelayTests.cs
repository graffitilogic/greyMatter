using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Runtime;
using Xunit;

namespace GreyMatter.Poc.Tests;
public class AssemblyRelayTests
{
    private static readonly SparseCode A=new(new[]{11});
    private static readonly SparseCode B=new(new[]{21});
    private static readonly SparseCode C=new(new[]{31});
    private static Config Cfg()=>new(){WorkingSetMax=64,Sparsity=1,ActivationWidth=32};

    [Fact]
    public void ObservedPairsComposeThroughTheSameCohortWithoutAnEndpointTrainingPair()
    {
        var cfg=Cfg();using var scope=new ActivationScope(cfg);var relay=new AssemblyRelay(cfg,scope);
        relay.Observe(A);relay.Observe(B);relay.EndSequence();
        relay.Observe(B);relay.Observe(C);relay.EndSequence();
        double Score(AssemblyRelay.Readout r,SparseCode code)=>AssemblyRelay.Members(code,cfg.BaselineNeuronCount).Sum(id=>r.Delivered.GetValueOrDefault(id));
        Assert.Equal(0,Score(relay.Run(A,1),C));
        var two=relay.Run(A,2);
        Assert.Equal(8,Score(two,C),6);
        Assert.Equal(0,Score(two,A)); // sources are not retained or fabricated as deliveries
        Assert.All(two.Steps,t=>Assert.Equal(t.InputMass,t.DeliveredMass,6));
        Assert.All(two.Steps,t=>Assert.Equal(8,t.Emitting));
        Assert.Equal(128,scope.Synapses.TotalSynapses);
    }

    [Fact]
    public void UntrainedCohortsDeliverNothingAndBoundariesDoNotCreateAssociations()
    {
        var cfg=Cfg();using var scope=new ActivationScope(cfg);var relay=new AssemblyRelay(cfg,scope);
        relay.Prepare(A);relay.Prepare(B);relay.Prepare(C);
        Assert.Empty(relay.Run(A,4).Delivered);
        relay.Observe(A);relay.Observe(B);relay.EndSequence();relay.Observe(C);
        var r=relay.Run(A,2);
        Assert.All(AssemblyRelay.Members(C,cfg.BaselineNeuronCount),id=>Assert.Equal(0,r.Delivered.GetValueOrDefault(id)));
        Assert.Empty(relay.Run(C,2).Delivered);
    }

    [Fact]
    public void LearnedRelativeWeightsRankRepeatedAssociationsAndQueriesDoNotLearn()
    {
        var cfg=Cfg();using var scope=new ActivationScope(cfg);var relay=new AssemblyRelay(cfg,scope);
        relay.Observe(A);relay.Observe(C);relay.EndSequence();
        for(int i=0;i<4;i++){relay.Observe(A);relay.Observe(B);relay.EndSequence();}
        var weights=scope.Synapses.Weight.ToArray();var fam=scope.Pool.Familiarity.ToArray();
        var r=relay.Run(A,1);
        double Score(SparseCode code)=>AssemblyRelay.Members(code,cfg.BaselineNeuronCount).Sum(id=>r.Delivered.GetValueOrDefault(id));
        Assert.True(Score(B)>Score(C));
        relay.Run(C,4);var again=relay.Run(A,1);
        Assert.Equal(r.Delivered.OrderBy(kv=>kv.Key),again.Delivered.OrderBy(kv=>kv.Key));
        Assert.Equal(weights,scope.Synapses.Weight);Assert.Equal(fam,scope.Pool.Familiarity);
    }
}
