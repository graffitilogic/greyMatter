using System.Diagnostics;
using System.Text.Json;

namespace GreyMatter.Poc.Utility;
public static class LocalModelCli
{
    public static long? PeakRssBytes()
    { using var process=Process.GetCurrentProcess(); long value=process.PeakWorkingSet64; return value>0?value:null; }
    public static int Run(string[] argv)
    {
        var args=new Args(argv); string command=argv[0];
        var allowed=(command switch {
            "learn"=>new[]{"--model","--source","--format","--seed","--budget-mib"},
            "probe"=>new[]{"--model","--cue","--candidates","--hops","--budget-mib"},
            "audit"=>new[]{"--model","--budget-mib"}, _=>throw new ArgumentException("Unknown model command") }).ToHashSet();
        var seen=new HashSet<string>();
        for(int i=1;i<argv.Length;i+=2)
            if(!allowed.Contains(argv[i]) || !seen.Add(argv[i]) || i+1>=argv.Length || argv[i+1].StartsWith("--")) throw new ArgumentException("Unknown, duplicate, or missing model option: "+argv[i]);
        string Required(string key)=>args.Value(key,null)??throw new ArgumentException(key+" required");
        int budget=int.Parse(args.Value("--budget-mib","128")!); _=LocalModel.CacheBudget(budget);
        string model=Required("--model"); var clock=Stopwatch.StartNew(); object result;
        if(command=="learn") result=LocalModel.Train(Required("--source"),args.Value("--format","text")!,model,int.Parse(args.Value("--seed","201")!),budget);
        else if(command=="audit") result=LocalModel.Audit(model,budget);
        else
        {
            var candidates=LocalText.Candidates(Required("--candidates")); var saved=LocalModel.Open(model,budget); using var store=saved.Store;
            var recall=LocalModel.Query(store,saved.Description.Seed,Required("--cue"),candidates,int.Parse(args.Value("--hops","1")!));
            if(store.BytesWritten!=0 || store.IndexBytesWritten!=0) throw new InvalidOperationException("Recall mutated model");
            result=new { Kind="Closed-candidate learned retrieval",Recall=recall with { Results=recall.Results.OrderByDescending(x=>x.Score).ThenBy(x=>x.Candidate,StringComparer.Ordinal).ToArray() },
                Candidates=candidates.Length, NoOutput=recall.Results.All(x=>x.Score==0), Saved=saved.Description };
        }
        Console.WriteLine(JsonSerializer.Serialize(new { Command=command, Result=result,Seconds=clock.Elapsed.TotalSeconds,
            ManagedBytes=GC.GetTotalMemory(false),PeakRss=PeakRssBytes(),
            PeakRssNote="Null means unavailable from this runtime; use external native peak measurement",
            BudgetMiB=budget,AdapterAndScratchAllowance=LocalModel.AdapterAndScratch },new JsonSerializerOptions {WriteIndented=true}));
        return 0;
    }
}
