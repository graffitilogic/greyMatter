using System.Diagnostics;
using System.Text.Json;
using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Pipeline;
using GreyMatter.Poc.Runtime;

namespace GreyMatter.Poc.Eval;

/// <summary>One fixed alternative simulation budget; no learning gate or parameter search.</summary>
public static class TravelTimeReview
{
    public sealed record Row(int Chain,int Ticks,double[] Scores,RecoveryLearning.Rank Rank,double Milliseconds);
    public sealed record Result(string Arm,Row[] Queries,string Snapshot,bool RoundTripExact,Config Config);

    public static int Run(Args args)
    {
        string output=args.Value("--output",null)??throw new ArgumentException("--output required");
        if(File.Exists(output)) throw new ArgumentException("Refusing to overwrite results");
        var directory=Path.GetDirectoryName(Path.GetFullPath(output))!;
        Directory.CreateDirectory(directory);
        var data=RecoveryLearning.Generate(100);
        var results=new List<Result>();
        foreach(string name in new[]{"untrained","learned","shuffled"})
        {
            var cfg=new Config { Seed=100,WorkingSetMax=50_000,SequenceUsesCueMembers=true };
            string snapshot=Path.Combine(directory,$"{name}-recall.bin");
            if(File.Exists(snapshot)) throw new ArgumentException($"Refusing to overwrite {snapshot}");
            var episodes=name=="shuffled"?RecoveryLearning.NullEpisodes(data,100):data.Episodes;
            var encoder=new ContextEncoder(cfg); Trainer.AccumulateContext(encoder,episodes);
            var codes=data.Symbols.Select(c=>c.Select(encoder.Encode).ToArray()).ToArray();
            if(codes.SelectMany(c=>c).Select(c=>c.Hash()).Distinct().Count()!=160) throw new InvalidOperationException("Code collision");
            using var scope=new ActivationScope(cfg);
            if(name!="untrained")
            {
                var stats=new Trainer(cfg,scope,encoder).Run(episodes,quiet:true);
                if(stats.Truncations!=0) throw new InvalidOperationException("Training truncation");
            }
            foreach(var id in codes.SelectMany(c=>c).SelectMany(c=>Assembly.Members(c,cfg.BaselineNeuronCount)).Distinct()) scope.Materialize(id);
            Save(snapshot,cfg,scope);
            // Evaluation metadata, outside the numerical model snapshot. It supplies
            // frozen inputs/candidates and is not a lookup table used by propagation.
            File.WriteAllText(Path.Combine(directory,$"{name}-evaluation-codes.json"),
                JsonSerializer.Serialize(codes.Select(chain=>chain.Select(code=>code.Dims).ToArray()).ToArray()));
            var rows=Score(cfg,scope,codes);
            var loaded=Load(snapshot);
            using var replay=loaded.scope;
            var repeated=Score(loaded.cfg,replay,codes);
            if(!rows.Zip(repeated).All(pair=>pair.First.Scores.SequenceEqual(pair.Second.Scores)))
                throw new InvalidOperationException("Snapshot replay differs");
            foreach(int ticks in new[]{4,12})
            {
                var group=rows.Where(r=>r.Ticks==ticks).ToArray();
                Console.WriteLine($"arm={name} ticks={ticks} top1={group.Average(r=>r.Rank.Top1):F6} MRR={group.Average(r=>r.Rank.ReciprocalRank):F6} meanMs={group.Average(r=>r.Milliseconds):F3}");
            }
            results.Add(new(name,rows,snapshot,true,cfg));
        }
        File.WriteAllText(output,JsonSerializer.Serialize(new{Verdict="DIAGNOSTIC_ONLY",Command=args.CommandLine,Results=results},new JsonSerializerOptions{WriteIndented=true}));
        return 0;
    }

    private static Row[] Score(Config cfg,ActivationScope scope,SparseCode[][] codes)
    {
        var members=codes.Select(c=>c.Select(code=>Assembly.Members(code,cfg.BaselineNeuronCount).Distinct().ToArray()).ToArray()).ToArray();
        var fam=scope.Pool.Familiarity.Take(scope.Pool.Count).ToArray();
        var fatigue=scope.Pool.Fatigue.Take(scope.Pool.Count).ToArray();
        var rows=new List<Row>();
        foreach(int ticks in new[]{4,12})
        {
            cfg.ActivationDepth=ticks;
            var cascade=new Cascade(cfg,scope);
            for(int chain=0;chain<32;chain++)
            {
                var sw=Stopwatch.StartNew();
                var readout=cascade.Run(codes[chain][0],false);
                if(readout.Truncated!=0) throw new InvalidOperationException("Recall truncated");
                var scores=members.Select(c=>c[4].Sum(id=>(double)cascade.DeliveredDrive[scope.Pool.Find(id)])).ToArray();
                sw.Stop();
                rows.Add(new(chain,ticks,scores,RecoveryLearning.ScoreRank(scores,chain),sw.Elapsed.TotalMilliseconds));
            }
        }
        if(!fam.SequenceEqual(scope.Pool.Familiarity.Take(scope.Pool.Count)) || !fatigue.SequenceEqual(scope.Pool.Fatigue.Take(scope.Pool.Count)))
            throw new InvalidOperationException("Recall changed familiarity/fatigue");
        cfg.ActivationDepth=4;
        return rows.ToArray();
    }

    // Recall-only numerical graph format: not a training checkpoint or a pageable store.
    public static void Save(string path,Config cfg,ActivationScope scope)
    {
        using var w=new BinaryWriter(new FileStream(path,FileMode.CreateNew));
        w.Write(0x54325331); w.Write(cfg.BaselineNeuronCount);w.Write(cfg.WorkingSetMax);
        w.Write(cfg.SynapseCapPerNeuron);w.Write(cfg.Sparsity);w.Write(cfg.ActivationWidth);
        w.Write(cfg.PropagatedWinnerQuota);w.Write(cfg.Seed);w.Write(scope.Pool.Count);
        for(int slot=0;slot<scope.Pool.Count;slot++)
        {
            w.Write(scope.Pool.VirtualId[slot]);w.Write(scope.Pool.Threshold[slot]);
            w.Write(scope.Pool.Familiarity[slot]);w.Write(scope.Pool.Fatigue[slot]);
            int n=scope.Synapses.Degree[slot],start=scope.Synapses.SegmentStart(slot);w.Write(n);
            for(int i=start;i<start+n;i++)
            {w.Write(scope.Synapses.Target[i]);w.Write(scope.Synapses.Weight[i]);w.Write(scope.Synapses.Population[i]);}
        }
    }

    public static (Config cfg,ActivationScope scope) Load(string path)
    {
        using var r=new BinaryReader(File.OpenRead(path));
        if(r.ReadInt32()!=0x54325331) throw new InvalidDataException("Snapshot version");
        var cfg=new Config {BaselineNeuronCount=r.ReadInt32(),WorkingSetMax=r.ReadInt32(),
            SynapseCapPerNeuron=r.ReadInt32(),Sparsity=r.ReadInt32(),ActivationWidth=r.ReadInt32(),
            PropagatedWinnerQuota=r.ReadInt32(),Seed=r.ReadInt32()};
        int count=r.ReadInt32();
        if(count<0 || count>cfg.WorkingSetMax || cfg.WorkingSetMax>1_000_000) throw new InvalidDataException("Snapshot size");
        var scope=new ActivationScope(cfg);
        try
        {
            for(int i=0;i<count;i++)
            {
                int slot=scope.Materialize(r.ReadUInt32());
                scope.Pool.Threshold[slot]=r.ReadSingle();scope.Pool.Familiarity[slot]=r.ReadSingle();scope.Pool.Fatigue[slot]=r.ReadSingle();
                int n=r.ReadInt32();if(n<0||n>cfg.SynapseCapPerNeuron) throw new InvalidDataException("Degree");
                var targets=new uint[n];var weights=new float[n];var populations=new byte[n];
                for(int j=0;j<n;j++){targets[j]=r.ReadUInt32();weights[j]=r.ReadSingle();populations[j]=r.ReadByte();}
                scope.Synapses.Hydrate(slot,targets,weights,populations);
            }
            if(r.BaseStream.Position!=r.BaseStream.Length) throw new InvalidDataException("Trailing data");
            return(cfg,scope);
        }
        catch {scope.Dispose();throw;}
    }
}
