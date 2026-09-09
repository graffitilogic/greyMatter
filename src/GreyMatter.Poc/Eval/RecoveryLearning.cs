using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Pipeline;
using GreyMatter.Poc.Runtime;

namespace GreyMatter.Poc.Eval;

/// <summary>Balanced held-out traversal queries, separate from the model and its training.</summary>
public static class RecoveryLearning
{
    public sealed record Query(int Chain, int Start, int Hops);
    public sealed record Data(string[][] Symbols, string[] Episodes, Query[] Queries);
    public sealed record Rank(double Top1, double ReciprocalRank);
    public sealed record QueryScore(int Chain, int Start, int Hops, double[] Scores, Rank Rank);
    public sealed record Metrics(double Top1, double Mrr, int Queries);
    public sealed record Arm(string Name, Metrics Direct, Metrics Composed, QueryScore[] Queries,
        long Synapses, long SequenceUpdates, long Truncations, int Resident, int UniqueCodes);
    public sealed record SeedResult(int Seed, string CorpusSha256, string NullSha256, Config Config, Arm[] Arms);

    public static Data Generate(int seed)
    {
        var rng = new Random(seed);
        var seen = new HashSet<string>();
        string Label()
        {
            while (true)
            {
                var text = new string(Enumerable.Range(0,12).Select(_ => (char)('a' + rng.Next(26))).ToArray());
                if (seen.Add(text)) return text;
            }
        }
        var symbols = Enumerable.Range(0,32).Select(_ => Enumerable.Range(0,5).Select(_ => Label()).ToArray()).ToArray();
        var episodes = new List<string>();
        for (int repeat = 0; repeat < 16; repeat++)
            foreach (var chain in symbols)
                for (int p = 0; p < 4; p++) episodes.Add($"{chain[p]} {chain[p+1]}");
        var shuffled = episodes.ToArray(); Shuffle(shuffled,rng);
        var queries = new List<Query>();
        for (int c = 0; c < 32; c++)
        {
            for (int p = 0; p < 4; p++) queries.Add(new(c,p,1));
            for (int hops = 2; hops <= 4; hops++) queries.Add(new(c,0,hops));
            queries.Add(new(c,1,2));
        }
        return new(symbols,shuffled,queries.ToArray());
    }

    public static string[] NullEpisodes(Data data, int seed)
    {
        var tokens = data.Episodes.SelectMany(Corpus.Tokenize).ToArray();
        Shuffle(tokens,new Random(seed ^ 0x5a71));
        return Enumerable.Range(0,tokens.Length/2).Select(i => $"{tokens[2*i]} {tokens[2*i+1]}").ToArray();
    }

    private static void Shuffle<T>(T[] values, Random rng)
    {
        for (int i=values.Length-1;i>0;i--) { int j=rng.Next(i+1); (values[i],values[j])=(values[j],values[i]); }
    }

    public static Rank ScoreRank(double[] scores, int correct)
    {
        if (scores.Length == 0 || scores.Any(x => !double.IsFinite(x))) throw new InvalidOperationException("Invalid scores");
        double value = scores[correct];
        int above = scores.Count(x => x > value), tied = scores.Count(x => x == value);
        return new(above == 0 ? 1.0/tied : 0,
            Enumerable.Range(above+1,tied).Average(r => 1.0/r));
    }

    private static Arm Summarize(string name, QueryScore[] queries, long synapses=0,
        long updates=0,long truncations=0,int resident=0,int unique=0)
    {
        Metrics Aggregate(bool direct)
        {
            var set = queries.Where(q => (q.Hops==1)==direct).ToArray();
            return new(set.Average(q=>q.Rank.Top1),set.Average(q=>q.Rank.ReciprocalRank),set.Length);
        }
        return new(name,Aggregate(true),Aggregate(false),queries,synapses,updates,truncations,resident,unique);
    }

    private static Arm Model(Config config, Data data, string[] episodes, string name, bool learn)
    {
        var cfg=config.Clone();
        var encoder=new ContextEncoder(cfg);
        Trainer.AccumulateContext(encoder,episodes);
        var codes=data.Symbols.Select(c=>c.Select(encoder.Encode).ToArray()).ToArray();
        int unique=codes.SelectMany(c=>c).Select(c=>c.Hash()).Distinct().Count();
        if(unique!=160) throw new InvalidOperationException($"Refused: only {unique}/160 unique codes");
        using var scope=new ActivationScope(cfg);
        Trainer.Stats? stats=null;
        if(learn) stats=new Trainer(cfg,scope,encoder).Run(episodes,quiet:true);
        // Symmetric residency for ALL candidates and cues, independent of query labels.
        var members=codes.Select(c=>c.Select(code=>Assembly.Members(code,cfg.BaselineNeuronCount,cfg.AssemblyOverlap)).ToArray()).ToArray();
        foreach(var id in members.SelectMany(c=>c).SelectMany(m=>m).Distinct()) scope.Materialize(id);
        if(scope.Pool.Count>=cfg.WorkingSetMax || stats?.Truncations>0)
            throw new InvalidOperationException("Refused: resident prerequisite violated");
        var familiarity=scope.Pool.Familiarity.Take(scope.Pool.Count).ToArray();
        var fatigue=scope.Pool.Fatigue.Take(scope.Pool.Count).ToArray();
        var cascade=new Cascade(cfg,scope);
        var rows=new List<QueryScore>();
        foreach(var query in data.Queries)
        {
            cfg.ActivationDepth=query.Hops;
            var trace = new Dictionary<(int step,int position),(int count,int below,double drive)>();
            if(query.Chain==0 && query.Start==0 && query.Hops==4)
                cascade.SourceObserver=(step,id,drive,below)=>
                {
                    for(int p=0;p<5;p++) if(members[0][p].Contains(id))
                    {
                        var key=(step,p); var row=trace.GetValueOrDefault(key);
                        trace[key]=(row.count+1,row.below+(below?1:0),row.drive+drive);
                    }
                };
            var readout=cascade.Run(codes[query.Chain][query.Start],false);
            cascade.SourceObserver=null;
            foreach(var (key,row) in trace.OrderBy(kv=>kv.Key))
                Console.WriteLine($"TRACE arm={name} chain=0 hops=4 step={key.step+1} position={key.position} sources={row.count} below={row.below} drive={row.drive:F6}");
            if(readout.Truncated!=0) throw new InvalidOperationException("Refused: truncated recall");
            var scores=new double[32];
            for(int c=0;c<32;c++)
                foreach(var id in members[c][query.Start+query.Hops].Distinct())
                    scores[c]+=cascade.DeliveredDrive[scope.Pool.Find(id)];
            rows.Add(new(query.Chain,query.Start,query.Hops,scores,ScoreRank(scores,query.Chain)));
        }
        if(!familiarity.SequenceEqual(scope.Pool.Familiarity.Take(scope.Pool.Count)) ||
           !fatigue.SequenceEqual(scope.Pool.Fatigue.Take(scope.Pool.Count)))
            throw new InvalidOperationException("Refused: recall mutated model state");
        return Summarize(name,rows.ToArray(),scope.Synapses.TotalSynapses,stats?.SequenceUpdates??0,
            stats?.Truncations??0,scope.Pool.Count,unique);
    }

    private static Arm Baseline(Data data)
    {
        var counts=new Dictionary<string,Dictionary<string,double>>();
        foreach(var ep in data.Episodes)
        {
            var t=Corpus.Tokenize(ep);
            if(!counts.TryGetValue(t[0],out var next)) counts[t[0]]=next=new();
            next[t[1]]=next.GetValueOrDefault(t[1])+1;
        }
        var rows=new List<QueryScore>();
        foreach(var q in data.Queries)
        {
            var active=new Dictionary<string,double>{{data.Symbols[q.Chain][q.Start],1}};
            for(int h=0;h<q.Hops;h++)
            {
                var next=new Dictionary<string,double>();
                foreach(var (from,drive) in active)
                    if(counts.TryGetValue(from,out var edges))
                    {
                        var sum=edges.Values.Sum();
                        foreach(var (to,weight) in edges) next[to]=next.GetValueOrDefault(to)+drive*weight/sum;
                    }
                active=next;
            }
            var scores=data.Symbols.Select(c=>active.GetValueOrDefault(c[q.Start+q.Hops])).ToArray();
            rows.Add(new(q.Chain,q.Start,q.Hops,scores,ScoreRank(scores,q.Chain)));
        }
        return Summarize("transition-baseline",rows.ToArray());
    }

    public static int Run(Config original, Args args)
    {
        var seeds=(args.Value("--seeds", "100")!).Split(',').Select(int.Parse).ToArray();
        bool final=seeds.SequenceEqual(new[]{101,102,103,104,105});
        if(!final && !seeds.SequenceEqual(new[]{100})) throw new ArgumentException("Use --seeds 100 or --seeds 101,102,103,104,105");
        var output=args.Value("--output",null) ?? throw new ArgumentException("--output is required (new JSON artifact)");
        if(File.Exists(output)) throw new ArgumentException("Refusing to overwrite result artifact");
        Console.WriteLine($"COMMAND: {args.CommandLine}");
        var results=new List<SeedResult>();
        foreach(int seed in seeds)
        {
            var cfg=original.Clone(); cfg.Seed=seed; cfg.WorkingSetMax=50_000;
            var data=Generate(seed); var nulls=NullEpisodes(data,seed);
            var arms=new List<Arm>();
            void Add(Arm arm)
            {
                arms.Add(arm);
                Console.WriteLine($"seed={seed} arm={arm.Name} direct={arm.Direct.Top1:F6} composed={arm.Composed.Top1:F6} " +
                    $"MRR={arm.Direct.Mrr:F6}/{arm.Composed.Mrr:F6} synapses={arm.Synapses} updates={arm.SequenceUpdates} resident={arm.Resident}");
            }
            Console.WriteLine($"CONFIG: {cfg.ToJson()}");
            Add(Baseline(data));
            Add(Model(cfg,data,data.Episodes,"untrained",false));
            Add(Model(cfg,data,data.Episodes,"learned",true));
            Add(Model(cfg,data,nulls,"shuffled",true));
            string Hash(string[] lines)=>Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join('\n',lines))));
            results.Add(new(seed,Hash(data.Episodes),Hash(nulls),cfg,arms.ToArray()));
        }
        bool pass=true;
        foreach(bool direct in new[]{true,false})
        {
            double Metric(Arm a)=>direct?a.Direct.Top1:a.Composed.Top1;
            double[] Values(string name)=>results.Select(r=>Metric(r.Arms.Single(a=>a.Name==name))).ToArray();
            var learned=Values("learned");
            pass &= learned.Average()>=.8 && learned.Min()>=.7 &&
                learned.Average()-Values("untrained").Average()>=.2 && learned.Average()-Values("shuffled").Average()>=.2;
        }
        var verdict=final?(pass?"R1_PASS":"R1_FAIL"):"DEVELOPMENT_ONLY";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllText(output,JsonSerializer.Serialize(new{verdict,meetsNumericTargets=pass,results},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"VERDICT: {verdict}; targets met={pass}; artifact={output}");
        return final&&!pass?1:0;
    }
}
