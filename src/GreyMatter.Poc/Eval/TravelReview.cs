using System.Text.Json;
using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Pipeline;
using GreyMatter.Poc.Runtime;

namespace GreyMatter.Poc.Eval;

/// <summary>Frozen-model, observation-only review. No counterfactual inference or gate.</summary>
public static class TravelReview
{
    public sealed record Cell(int Step, int Position, int Positive, double Mass,
        int Selected, double SelectedMass, int Eligible);
    public sealed record Link(int Chain, int Position, int Edges, int Sources, int Targets, int BridgeMembers);

    public static int Run(Args args)
    {
        string output=args.Value("--output",null)??throw new ArgumentException("--output required");
        if(File.Exists(output)) throw new ArgumentException("Refusing to overwrite evidence");
        var cfg=new Config { Seed=100,WorkingSetMax=50_000,SequenceUsesCueMembers=true };
        var data=RecoveryLearning.Generate(100);
        var encoder=new ContextEncoder(cfg);
        Trainer.AccumulateContext(encoder,data.Episodes);
        var codes=data.Symbols.Select(c=>c.Select(encoder.Encode).ToArray()).ToArray();
        using var scope=new ActivationScope(cfg);
        var train=new Trainer(cfg,scope,encoder).Run(data.Episodes,quiet:true);
        var members=codes.Select(c=>c.Select(code=>Assembly.Members(code,cfg.BaselineNeuronCount).Distinct().ToArray()).ToArray()).ToArray();
        foreach(uint id in members.SelectMany(c=>c).SelectMany(c=>c).Distinct()) scope.Materialize(id);
        if(train.Truncations!=0 || scope.Pool.Count>=cfg.WorkingSetMax) throw new InvalidOperationException("Not fully resident");
        var links=new List<Link>();
        (int count,HashSet<uint> sources,HashSet<uint> targets) Edges(uint[] from,uint[] to)
        {
            var targets=to.ToHashSet(); var src=new HashSet<uint>(); var dst=new HashSet<uint>(); int count=0;
            foreach(uint id in from)
            {
                int slot=scope.Pool.Find(id), start=scope.Synapses.SegmentStart(slot);
                for(int s=start;s<start+scope.Synapses.Degree[slot];s++)
                    if(scope.Synapses.Weight[s]>0 && targets.Contains(scope.Synapses.Target[s]))
                    { count++; src.Add(id);dst.Add(scope.Synapses.Target[s]); }
            }
            return(count,src,dst);
        }
        for(int c=0;c<32;c++) for(int p=0;p<4;p++)
        {
            var e=Edges(members[c][p],members[c][p+1]);
            int bridge=p<3?e.targets.Intersect(Edges(members[c][p+1],members[c][p+2]).sources).Count():0;
            links.Add(new(c,p,e.count,e.sources.Count,e.targets.Count,bridge));
        }
        var fam=scope.Pool.Familiarity.Take(scope.Pool.Count).ToArray();
        var fatigue=scope.Pool.Fatigue.Take(scope.Pool.Count).ToArray();
        var queries=new List<object>();
        for(int c=0;c<32;c++)
        {
            var cells=new Dictionary<(int step,int pos),Cell>();
            var run=new Cascade(cfg,scope);
            run.SelectionObserver=(step,id,mass,selected)=>
            {
                if(mass<=0) return;
                for(int p=0;p<5;p++) if(members[c][p].Contains(id))
                {
                    var key=(step,p);
                    var old=cells.GetValueOrDefault(key)??new Cell(step+1,p,0,0,0,0,0);
                    cells[key]=old with { Positive=old.Positive+1,Mass=old.Mass+mass,
                        Selected=old.Selected+(selected?1:0),SelectedMass=old.SelectedMass+(selected?mass:0),
                        Eligible=old.Eligible+(selected && mass>=.5f?1:0) };
                }
            };
            run.Run(codes[c][0],false);
            var observed=run.DeliveredDrive.ToArray();
            run.SelectionObserver=null;run.Run(codes[c][0],false);
            if(!observed.SequenceEqual(run.DeliveredDrive.ToArray())) throw new InvalidOperationException("Observer changes answer");
            var scores=members.Select(chain=>chain[4].Sum(id=>(double)observed[scope.Pool.Find(id)])).ToArray();
            queries.Add(new { Chain=c,Scores=scores,Cells=cells.Values.OrderBy(x=>x.Step).ThenBy(x=>x.Position).ToArray() });
        }
        if(!fam.SequenceEqual(scope.Pool.Familiarity.Take(scope.Pool.Count)) || !fatigue.SequenceEqual(scope.Pool.Fatigue.Take(scope.Pool.Count)))
            throw new InvalidOperationException("Recall mutated state");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllText(output,JsonSerializer.Serialize(new { Command=args.CommandLine,Config=cfg,Train=train,Links=links,Queries=queries,
            ObserverEquality=true,ReadOnly=true,Verdict="DIAGNOSTIC_ONLY"},new JsonSerializerOptions {WriteIndented=true}));
        Console.WriteLine($"DIAGNOSTIC_ONLY: {output}; observer equality and read-only checks pass");
        return 0;
    }
}
