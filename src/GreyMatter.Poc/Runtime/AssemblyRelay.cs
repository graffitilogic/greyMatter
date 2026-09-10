using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Runtime;

/// <summary>
/// Experimental protected routing channel. The same procedural cohort receives and
/// emits learned associations. No within-cue edges compete for this channel's slots.
/// This is a fully resident reference, not a pageable runtime or a default replacement.
/// </summary>
public sealed class AssemblyRelay
{
    public const int CohortSize = 8;
    private readonly Config _cfg;
    private readonly ActivationScope _scope;
    private uint[] _previous = Array.Empty<uint>();
    public long Updates { get; private set; }
    public sealed record Step(int Tick,int Sources,int Emitting,int Reached,int Winners,double InputMass,double DeliveredMass);
    public sealed record Readout(Dictionary<uint,double> Delivered,Step[] Steps);

    public AssemblyRelay(Config cfg,ActivationScope scope) { _cfg=cfg;_scope=scope; }
    public static uint[] Members(in SparseCode code,int space) =>
        Assembly.Members(code,space).Take(CohortSize).Distinct().ToArray();

    public uint[] Prepare(in SparseCode code)
    {
        var members=Members(code,_cfg.BaselineNeuronCount);
        foreach(uint id in members) _scope.Materialize(id);
        return members;
    }

    public void Observe(in SparseCode code)
    {
        _scope.AdvanceTick();
        var current=Prepare(code);
        foreach(uint source in _previous)
        {
            int slot=_scope.Pool.Find(source);
            if(slot<0) throw new InvalidOperationException("Relay learning requires a resident previous cohort");
            foreach(uint target in current)
            {
                _scope.Synapses.RecordCoactivation(slot,source,target,.5f,1f,SynapsePopulation.CrossCue);
                Updates++;
            }
        }
        _previous=current;
    }

    public void EndSequence() => _previous=Array.Empty<uint>();

    public Readout Run(in SparseCode cue,int ticks)
    {
        if(ticks<1) throw new ArgumentOutOfRangeException(nameof(ticks));
        var active=Members(cue,_cfg.BaselineNeuronCount).ToDictionary(id=>id,_=>1f);
        var delivered=new Dictionary<uint,double>();
        var trace=new List<Step>();
        for(int step=0;step<ticks;step++)
        {
            var incoming=new Dictionary<uint,float>();int emitting=0;
            foreach(var (id,drive) in active.OrderBy(kv=>kv.Key))
            {
                if(drive<.5f) continue;
                int slot=_scope.Pool.Find(id);
                if(slot<0) throw new InvalidOperationException("Nonresident relay target: paging is not implemented");
                int start=_scope.Synapses.SegmentStart(slot),end=start+_scope.Synapses.Degree[slot];
                double sum=0;
                for(int e=start;e<end;e++) sum+=Math.Max(0,_scope.Synapses.Weight[e]);
                if(sum<=0) continue;
                emitting++;
                for(int e=start;e<end;e++)
                {
                    float weight=_scope.Synapses.Weight[e];if(weight<=0) continue;
                    uint target=_scope.Synapses.Target[e];
                    if(_scope.Pool.Find(target)<0) throw new InvalidOperationException("Nonresident relay edge");
                    float value=(float)(drive*weight/sum);
                    incoming[target]=incoming.GetValueOrDefault(target)+value;
                    delivered[target]=delivered.GetValueOrDefault(target)+value;
                }
            }
            var next=incoming.OrderByDescending(kv=>kv.Value).ThenBy(kv=>kv.Key)
                .Take(_cfg.ActivationWidth).ToDictionary(kv=>kv.Key,kv=>kv.Value);
            trace.Add(new(step+1,active.Count,emitting,incoming.Count,next.Count,
                active.Values.Sum(x=>(double)x),incoming.Values.Sum(x=>(double)x)));
            active=next;
        }
        return new(delivered,trace.ToArray());
    }
}
