using System.Text.Json;
using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Runtime;

namespace GreyMatter.Poc.Eval;

/// <summary>Read-only graph reachability and runtime witness observation, not a new readout.</summary>
public static class RouteReview
{
    public sealed record Search(int[] Distance,int[] Parent);
    public sealed record Node(uint Id,int Distance,int FirstPositive,int FirstSelected,int FirstEmitted,float MaxPotential);
    public sealed record Query(int Chain,double[] Scores,double[] ReachableFraction,int[] MinDistance,
        int CueTargetOverlap,Node[] Witness,float[] WitnessWeights);
    public sealed record Arm(string Name,Query[] Queries);

    public static Search BreadthFirst(ActivationScope scope,uint[] roots,int maxDepth)
    {
        if(maxDepth<0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
        int n=scope.Pool.Count;
        var distance=Enumerable.Repeat(-1,n).ToArray();var parent=Enumerable.Repeat(-1,n).ToArray();
        var queue=new int[n];int head=0,tail=0;
        foreach(uint id in roots.Distinct().OrderBy(x=>x))
        {
            int slot=scope.Pool.Find(id);if(slot<0) throw new InvalidOperationException("Nonresident root");
            distance[slot]=0;queue[tail++]=slot;
        }
        while(head<tail)
        {
            int slot=queue[head++];if(distance[slot]>=maxDepth) continue;
            int start=scope.Synapses.SegmentStart(slot);
            for(int e=start;e<start+scope.Synapses.Degree[slot];e++)
            {
                if(scope.Synapses.Weight[e]<=0) continue;
                int target=scope.Pool.Find(scope.Synapses.Target[e]);
                if(target<0) throw new InvalidOperationException("Snapshot has a nonresident positive target");
                if(distance[target]>=0) continue;
                distance[target]=distance[slot]+1;parent[target]=slot;queue[tail++]=target;
            }
        }
        return new(distance,parent);
    }

    public static int Run(Args args)
    {
        string directory=args.Value("--snapshot-directory",null)??throw new ArgumentException("--snapshot-directory required");
        string output=args.Value("--output",null)??throw new ArgumentException("--output required");
        if(File.Exists(output)) throw new ArgumentException("Refusing overwrite");
        using var prior=JsonDocument.Parse(File.ReadAllText(Path.Combine(directory,"time.json")));
        var arms=new List<Arm>();
        foreach(string name in new[]{"untrained","learned","shuffled"})
        {
            var loaded=TravelTimeReview.Load(Path.Combine(directory,$"{name}-recall.bin"));
            using var scope=loaded.scope;var cfg=loaded.cfg;cfg.ActivationDepth=12;
            var raw=JsonSerializer.Deserialize<int[][][]>(File.ReadAllText(Path.Combine(directory,$"{name}-evaluation-codes.json")))!;
            var codes=raw.Select(c=>c.Select(d=>new SparseCode(d)).ToArray()).ToArray();
            var members=codes.Select(c=>c.Select(code=>Assembly.Members(code,cfg.BaselineNeuronCount).Distinct().ToArray()).ToArray()).ToArray();
            var reference=prior.RootElement.GetProperty("Results").EnumerateArray().Single(a=>a.GetProperty("Arm").GetString()==name);
            var queries=new List<Query>();
            var fam=scope.Pool.Familiarity.Take(scope.Pool.Count).ToArray();
            var fatigue=scope.Pool.Fatigue.Take(scope.Pool.Count).ToArray();
            for(int chain=0;chain<32;chain++)
            {
                var graph=BreadthFirst(scope,members[chain][0],12);
                int n=scope.Pool.Count;
                var positive=new int[n];var selected=new int[n];var emitted=new int[n];var maxima=new float[n];
                var run=new Cascade(cfg,scope);
                run.SourceObserver=(step,id,drive,below)=>
                {int slot=scope.Pool.Find(id);if(!below && drive>0 && emitted[slot]==0) emitted[slot]=step+1;};
                run.SelectionObserver=(step,id,mass,won)=>
                {
                    int slot=scope.Pool.Find(id);maxima[slot]=Math.Max(maxima[slot],mass);
                    if(mass>0 && positive[slot]==0) positive[slot]=step+1;
                    if(mass>0 && won && selected[slot]==0) selected[slot]=step+1;
                };
                run.Run(codes[chain][0],false);
                var scores=members.Select(c=>c[4].Sum(id=>(double)run.DeliveredDrive[scope.Pool.Find(id)])).ToArray();
                var expected=reference.GetProperty("Queries").EnumerateArray().Single(q=>q.GetProperty("Chain").GetInt32()==chain && q.GetProperty("Ticks").GetInt32()==12)
                    .GetProperty("Scores").EnumerateArray().Select(x=>x.GetDouble()).ToArray();
                if(!scores.SequenceEqual(expected)) throw new InvalidOperationException("T2 replay mismatch");
                var fractions=members.Select(c=>(double)c[4].Count(id=>graph.Distance[scope.Pool.Find(id)]>=0)/c[4].Length).ToArray();
                var distances=members.Select(c=>c[4].Select(id=>graph.Distance[scope.Pool.Find(id)]).Where(d=>d>=0).DefaultIfEmpty(-1).Min()).ToArray();
                // A deterministic shortest positive witness, not necessarily the route
                // the runtime used, nor representative of every path to this assembly.
                int target=members[chain][4].Select(scope.Pool.Find).Where(s=>graph.Distance[s]>0)
                    .OrderBy(s=>graph.Distance[s]).ThenBy(s=>scope.Pool.VirtualId[s]).DefaultIfEmpty(-1).First();
                var path=new List<int>();
                while(target>=0){path.Add(target);target=graph.Parent[target];}path.Reverse();
                var nodes=path.Select(s=>new Node(scope.Pool.VirtualId[s],graph.Distance[s],positive[s],selected[s],emitted[s],maxima[s])).ToArray();
                var weights=new List<float>();
                for(int i=1;i<path.Count;i++)
                {
                    int from=path[i-1],start=scope.Synapses.SegmentStart(from);
                    int edge=Enumerable.Range(start,scope.Synapses.Degree[from]).Single(e=>scope.Synapses.Target[e]==scope.Pool.VirtualId[path[i]]);
                    weights.Add(scope.Synapses.Weight[edge]);
                }
                queries.Add(new(chain,scores,fractions,distances,members[chain][0].Intersect(members[chain][4]).Count(),nodes,weights.ToArray()));
            }
            if(!fam.SequenceEqual(scope.Pool.Familiarity.Take(scope.Pool.Count)) || !fatigue.SequenceEqual(scope.Pool.Fatigue.Take(scope.Pool.Count))) throw new InvalidOperationException("Read-only violation");
            arms.Add(new(name,queries.ToArray()));
            Console.WriteLine($"{name}: {queries.Count} queries exactly reproduce T2; {queries.Count(q=>q.Witness.Length>0)} positive correct-candidate witnesses");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllText(output,JsonSerializer.Serialize(new{Verdict="DIAGNOSTIC_ONLY",Command=args.CommandLine,Arms=arms},new JsonSerializerOptions{WriteIndented=true}));
        return 0;
    }
}
