using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using GreyMatter.Poc.Utility;
using GreyMatter.Poc.Storage;

namespace GreyMatter.Poc.Eval;
public static class LocalDataEval
{
    public sealed record Question(string Cue,string[] Candidates,int Answer,long[] Frequency,long[] Cooccurrence);
    public sealed record QuerySet(Question[] Questions);
    private static readonly JsonSerializerOptions Json=new(){WriteIndented=true};
    private static string H(string s)=>Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(s)));
    public static int Run(Args args)
    {
        string action=args.Value("--action","prepare")!;
        if(action=="prepare") return Prepare(args.Value("--source",null)??throw new ArgumentException("source required"),args.Value("--output",null)??throw new ArgumentException("output directory required"));
        if(action!="score") throw new ArgumentException("Action must be prepare or score");
        string output=args.Value("--output",null)??throw new ArgumentException("output required");if(File.Exists(output))throw new IOException("Result exists");
        string root=args.Value("--model",null)??throw new ArgumentException("model required");
        string queryFile=args.Value("--queries",null)??throw new ArgumentException("queries required");
        if(new FileInfo(queryFile).Length>8*1024*1024)throw new ArgumentException("Evaluation query file exceeds 8MiB");
        var set=JsonSerializer.Deserialize<QuerySet>(File.ReadAllText(queryFile))??throw new InvalidDataException("Queries");
        if(set.Questions.Length<1 || set.Questions.Length>128)throw new ArgumentException("Query count");
        string backend=args.Value("--backend","paged")!;if(backend!="paged"&&backend!="resident")throw new ArgumentException("Backend");
        string before=PolicyIntegrationEval.PhysicalHash(root);var saved=LocalModel.Open(root,128);using var store=saved.Store;
        using var resident=backend=="resident"?new ResidentRelayRecords(LocalText.Space):null;
        if(resident!=null){var buffer=new byte[RelayRecord.Bytes];store.VisitPresent(id=>{store.Read(id,buffer);resident.Write(id,buffer);});}
        IRelayRecords records=resident??(IRelayRecords)store;var rows=new List<object>();var timer=Stopwatch.StartNew();
        foreach(var q in set.Questions)
        {
            if(q.Candidates.Length!=32||q.Answer<0||q.Answer>=32)throw new InvalidDataException("Query shape");
            var clock=Stopwatch.StartNew();var result=LocalModel.Query(records,saved.Description.Seed,q.Cue,q.Candidates,1,saved.Policy);clock.Stop();
            rows.Add(new {q.Cue,Scores=result.Results.Select(h=>h.Score).ToArray(),Milliseconds=clock.Elapsed.TotalMilliseconds,
                result.DataRead,result.IndexRead,result.Evictions,result.Steps,result.Delivered});
        }
        timer.Stop();bool immutable=before==PolicyIntegrationEval.PhysicalHash(root);
        File.WriteAllText(output,JsonSerializer.Serialize(new {Backend=backend,Seed=saved.Description.Seed,Queries=rows,Seconds=timer.Elapsed.TotalSeconds,
            Immutable=immutable,DataWritten=store.BytesWritten,IndexWritten=store.IndexBytesWritten,SnapshotHash=before,
            QueryHash=Convert.ToHexString(LocalModel.Hash(queryFile)),store.ReservedBytes,ManagedBytes=GC.GetTotalMemory(false),
            PeakRss=LocalModelCli.PeakRssBytes(),CacheNote="Snapshot checksums warm OS cache; resident preloaded; no cold-disk claim"},Json));
        return immutable&&store.BytesWritten==0&&store.IndexBytesWritten==0?0:1;
    }
    private static int Prepare(string source,string output)
    {
        if(Directory.Exists(output))throw new IOException("Preparation directory exists");
        string hash=Convert.ToHexString(LocalModel.Hash(source));
        if(hash!="311C619DD0B8C7CA3B61F0FD91643721D827245EBA16FA1BD2B8DDFE540A9E09")throw new InvalidDataException("Source differs from registered checksum");
        var seen=new HashSet<string>(StringComparer.Ordinal);var train=new List<string>();var test=new List<string>();int total=0;
        foreach(var words in LocalText.Sentences(source,"tatoeba"))
        {
            total++;string normalized=string.Join(' ',words);if(!seen.Add(normalized))continue;
            byte[] digest=Convert.FromHexString(H(normalized));
            (BinaryPrimitives.ReadUInt32LittleEndian(digest)%5==0?test:train).Add(normalized);
        }
        var freq=new Dictionary<string,long>(StringComparer.Ordinal);var pairs=new Dictionary<(string,string),long>();
        foreach(string sentence in train){var words=sentence.Split(' ');foreach(string w in words)freq[w]=freq.GetValueOrDefault(w)+1;
            for(int i=1;i<words.Length;i++){var pair=(words[i-1],words[i]);pairs[pair]=pairs.GetValueOrDefault(pair)+1;}}
        var held=new Dictionary<string,HashSet<string>>(StringComparer.Ordinal);
        foreach(string sentence in test){var words=sentence.Split(' ');for(int i=1;i<words.Length;i++){
            if(!held.TryGetValue(words[i-1],out var targets))held[words[i-1]]=targets=new(StringComparer.Ordinal);targets.Add(words[i]);}}
        var vocab=freq.Where(kv=>kv.Value>=5).Select(kv=>kv.Key).ToArray();var eligible=new List<Question>();int insufficient=0;
        foreach(string cue in held.Keys.OrderBy(H,StringComparer.Ordinal))
        {
            if(freq.GetValueOrDefault(cue)<5)continue;
            var targets=held[cue].Where(t=>t!=cue&&freq.GetValueOrDefault(t)>=5&&pairs.GetValueOrDefault((cue,t))>=3).OrderBy(t=>H(cue+"|"+t),StringComparer.Ordinal).ToArray();
            if(targets.Length==0)continue;string positive=targets[0];int bin=(int)Math.Log2(freq[positive]);
            var negative=vocab.Where(t=>t!=cue&&!held[cue].Contains(t)&&(int)Math.Log2(freq[t])==bin).OrderBy(t=>H(cue+"|"+t),StringComparer.Ordinal).Take(31).ToArray();
            if(negative.Length<31){insufficient++;continue;}
            var candidates=negative.Append(positive).OrderBy(t=>H("candidate|"+cue+"|"+t),StringComparer.Ordinal).ToArray();
            eligible.Add(new(cue,candidates,Array.IndexOf(candidates,positive),candidates.Select(t=>freq[t]).ToArray(),candidates.Select(t=>pairs.GetValueOrDefault((cue,t))).ToArray()));
        }
        if(eligible.Count<100)throw new InvalidDataException($"Only {eligible.Count} supported queries; require100");
        if(hash!=Convert.ToHexString(LocalModel.Hash(source)))throw new IOException("Source changed");
        Directory.CreateDirectory(output);File.WriteAllLines(Path.Combine(output,"train.txt"),train);File.WriteAllLines(Path.Combine(output,"heldout.txt"),test);
        File.WriteAllText(Path.Combine(output,"queries.json"),JsonSerializer.Serialize(new QuerySet(eligible.Take(128).ToArray()),Json));
        File.WriteAllText(Path.Combine(output,"manifest.json"),JsonSerializer.Serialize(new {SourceHash=hash,EnglishSentences=total,Unique=seen.Count,
            Duplicates=total-seen.Count,TrainSentences=train.Count,HeldoutSentences=test.Count,TrainingTokens=freq.Values.Sum(),Vocabulary=freq.Count,
            IdentityCollisionGroups=freq.Keys.GroupBy(LocalText.Identity).Count(g=>g.Count()>1),Eligible=eligible.Count,InsufficientDistractors=insufficient,
            Selected=Math.Min(128,eligible.Count),TrainHash=Convert.ToHexString(LocalModel.Hash(Path.Combine(output,"train.txt"))),
            HeldoutHash=Convert.ToHexString(LocalModel.Hash(Path.Combine(output,"heldout.txt"))),QueryHash=Convert.ToHexString(LocalModel.Hash(Path.Combine(output,"queries.json")))},Json));
        Console.WriteLine(File.ReadAllText(Path.Combine(output,"manifest.json")));return 0;
    }
}
