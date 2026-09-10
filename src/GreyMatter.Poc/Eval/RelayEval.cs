using System.Security.Cryptography;
using System.Text.Json;
using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Pipeline;
using GreyMatter.Poc.Runtime;

namespace GreyMatter.Poc.Eval;

public static class RelayEval
{
    public sealed record Query(int Chain,int Start,int Hops,double[] Scores,RecoveryLearning.Rank Rank,AssemblyRelay.Step[] Steps);
    public sealed record Arm(string Name,Query[] Queries,int Links,int Bridges,int UniqueNeurons,int Collisions,
        long Synapses,long Updates,bool SnapshotExact,string Snapshot);
    public sealed record Seed(int Value,string CorpusSha256,string NullSha256,Config Config,Arm[] Arms);

    private static Query[] Score(AssemblyRelay model,Config cfg,RecoveryLearning.Data data,SparseCode[][] codes)
    {
        var members=codes.Select(c=>c.Select(code=>AssemblyRelay.Members(code,cfg.BaselineNeuronCount)).ToArray()).ToArray();
        return data.Queries.Select(q=>
        {
            var r=model.Run(codes[q.Chain][q.Start],q.Hops);
            var scores=members.Select(c=>c[q.Start+q.Hops].Sum(id=>r.Delivered.GetValueOrDefault(id))).ToArray();
            return new Query(q.Chain,q.Start,q.Hops,scores,RecoveryLearning.ScoreRank(scores,q.Chain),r.Steps);
        }).ToArray();
    }

    private static Arm Learn(Config cfg,RecoveryLearning.Data data,string[] episodes,string name,string directory)
    {
        var encoder=new ContextEncoder(cfg);Trainer.AccumulateContext(encoder,episodes);
        var codes=data.Symbols.Select(c=>c.Select(encoder.Encode).ToArray()).ToArray();
        if(codes.SelectMany(c=>c).Select(c=>c.Hash()).Distinct().Count()!=160) throw new InvalidOperationException("Encoder collision");
        using var scope=new ActivationScope(cfg);var model=new AssemblyRelay(cfg,scope);
        if(name!="untrained")
        {
            int count=0;
            foreach(string episode in episodes)
            {
                foreach(string token in Corpus.Tokenize(episode)) model.Observe(encoder.Encode(token));
                model.EndSequence();count++;
                if(count%500==0) scope.Synapses.ApplyDecay(scope.Pool.Count,.99f);
            }
        }
        var members=codes.Select(c=>c.Select(code=>model.Prepare(code)).ToArray()).ToArray();
        if(scope.Pool.TotalEvicted!=0) throw new InvalidOperationException("A1 must remain resident");
        HashSet<uint> Targets(uint[] from,uint[] to)
        {
            var wanted=to.ToHashSet();var found=new HashSet<uint>();
            foreach(uint id in from)
            {
                int slot=scope.Pool.Find(id),start=scope.Synapses.SegmentStart(slot);
                for(int e=start;e<start+scope.Synapses.Degree[slot];e++)
                    if(scope.Synapses.Weight[e]>0 && wanted.Contains(scope.Synapses.Target[e])) found.Add(scope.Synapses.Target[e]);
            }
            return found;
        }
        int links=0,bridges=0;
        for(int chain=0;chain<32;chain++) for(int p=0;p<4;p++)
        {
            var incoming=Targets(members[chain][p],members[chain][p+1]);if(incoming.Count>0) links++;
            if(p<3 && incoming.Any(id=>Targets(new[]{id},members[chain][p+2]).Count>0)) bridges++;
        }
        var scores=Score(model,cfg,data,codes);
        Directory.CreateDirectory(directory);
        string snapshot=Path.Combine(directory,$"{name}-relay-recall.bin");
        TravelTimeReview.Save(snapshot,cfg,scope);
        File.WriteAllText(Path.Combine(directory,$"{name}-evaluation-codes.json"),JsonSerializer.Serialize(codes.Select(c=>c.Select(code=>code.Dims).ToArray()).ToArray()));
        var restored=TravelTimeReview.Load(snapshot);using var loaded=restored.scope;
        var replay=Score(new AssemblyRelay(restored.cfg,loaded),restored.cfg,data,codes);
        if(!scores.Zip(replay).All(p=>p.First.Scores.SequenceEqual(p.Second.Scores))) throw new InvalidOperationException("Relay snapshot mismatch");
        int total=members.SelectMany(c=>c).Sum(c=>c.Length),unique=members.SelectMany(c=>c).SelectMany(c=>c).Distinct().Count();
        return new(name,scores,links,bridges,unique,total-unique,scope.Synapses.TotalSynapses,model.Updates,true,snapshot);
    }

    public static int Run(Args args)
    {
        var seeds=(args.Value("--seeds","100")!).Split(',').Select(int.Parse).ToArray();
        bool final=seeds.SequenceEqual(new[]{201,202,203,204,205});
        if(!final&&!seeds.SequenceEqual(new[]{100})) throw new ArgumentException("Registered seeds: 100 or 201,202,203,204,205");
        string output=args.Value("--output",null)??throw new ArgumentException("--output required");
        if(File.Exists(output)) throw new ArgumentException("Refusing overwrite");
        string directory=Path.GetDirectoryName(Path.GetFullPath(output))!;
        var results=new List<Seed>();
        foreach(int seed in seeds)
        {
            var cfg=new Config {Seed=seed,WorkingSetMax=50_000};var data=RecoveryLearning.Generate(seed);var nulls=RecoveryLearning.NullEpisodes(data,seed);
            var arms=new List<Arm>();
            var baseline=RecoveryLearning.Baseline(data);
            arms.Add(new("transition-baseline",baseline.Queries.Select(q=>new Query(q.Chain,q.Start,q.Hops,q.Scores,q.Rank,Array.Empty<AssemblyRelay.Step>())).ToArray(),0,0,0,0,0,0,false,""));
            foreach(string name in new[]{"untrained","learned","shuffled"})
            {
                var a=Learn(cfg,data,name=="shuffled"?nulls:data.Episodes,name,Path.Combine(directory,$"seed-{seed}"));arms.Add(a);
                Console.WriteLine($"seed={seed} arm={name} direct={a.Queries.Where(q=>q.Hops==1).Average(q=>q.Rank.Top1):F6} composed={a.Queries.Where(q=>q.Hops>1).Average(q=>q.Rank.Top1):F6} links={a.Links}/128 bridges={a.Bridges}/96 neurons={a.UniqueNeurons} collisions={a.Collisions} synapses={a.Synapses}");
            }
            string Hash(string[] lines)=>Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join('\n',lines))));
            results.Add(new(seed,Hash(data.Episodes),Hash(nulls),cfg,arms.ToArray()));
        }
        bool connectivity=results.All(s=>s.Arms.Single(a=>a.Name=="learned") is {Links:128,Bridges:96});
        bool quality=true;
        foreach(bool direct in new[]{true,false})
        {
            double[] Values(string name)=>results.Select(s=>s.Arms.Single(a=>a.Name==name).Queries.Where(q=>(q.Hops==1)==direct).Average(q=>q.Rank.Top1)).ToArray();
            var learned=Values("learned");quality &= learned.Average()>=.8 && learned.Min()>=.7 && learned.Average()-Values("untrained").Average()>=.2 && learned.Average()-Values("shuffled").Average()>=.2;
        }
        bool transmission=results.All(s=>s.Arms.Single(a=>a.Name=="learned").Queries.Where(q=>q.Hops>1).All(q=>q.Scores[q.Chain]>0 && q.Steps.All(t=>t.Emitting>0)));
        string verdict=final?(connectivity&&quality&&transmission?"A1_PASS":"A1_FAIL"):"DEVELOPMENT_ONLY";
        Directory.CreateDirectory(directory);
        File.WriteAllText(output,JsonSerializer.Serialize(new{Verdict=verdict,Runtime="AssemblyRelay",CohortSize=8,Connectivity=connectivity,Transmission=transmission,Quality=quality,Results=results},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"{verdict} connectivity={connectivity} transmission={transmission} quality={quality}");
        return final&&verdict=="A1_FAIL"?1:0;
    }
}
