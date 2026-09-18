using System.Diagnostics;
using System.Text.Json;
using GreyMatter.Poc.Storage;
using GreyMatter.Poc.Utility;
namespace GreyMatter.Poc.Eval;
public static class CloseoutProfileEval
{
    public static int Run(Args args)
    {
        string action=args.Value("--action",null)??throw new ArgumentException("action required");
        string root=args.Value("--model",null)??throw new ArgumentException("model required");
        string output=args.Value("--output",null)??throw new ArgumentException("output required");
        if(File.Exists(output))throw new IOException("Output exists");object attribution;object result;
        var nativeBefore=MacFileIo.Sample();int[] gcBefore=Enumerable.Range(0,3).Select(GC.CollectionCount).ToArray();
        if(action=="train")
        {
            string source=args.Value("--source",null)??throw new ArgumentException("source required");
            using(var profile=new CostProfile()){result=LocalModel.Train(source,"text",root,201,128);attribution=profile.Finish();}
        }
        else if(action=="score")
        {
            string query=args.Value("--queries",null)??throw new ArgumentException("queries required");
            var questions=JsonSerializer.Deserialize<LocalDataEval.QuerySet>(File.ReadAllText(query))??throw new InvalidDataException("Queries");
            var rows=new List<object>();
            using(var profile=new CostProfile())
            {
                var saved=LocalModel.Open(root,128);using var store=saved.Store;
                foreach(var q in questions.Questions)
                {
                    var answer=LocalModel.Query(store,saved.Description.Seed,q.Cue,q.Candidates,1);
                    rows.Add(new {q.Cue,Scores=answer.Results.Select(x=>x.Score).ToArray(),answer.DataRead,answer.IndexRead,answer.Evictions});
                }
                if(store.BytesWritten!=0||store.IndexBytesWritten!=0)throw new InvalidOperationException("Query wrote model");
                result=rows;attribution=profile.Finish();
            }
        }
        else throw new ArgumentException("train or score required");
        var nativeAfter=MacFileIo.Sample();
        File.WriteAllText(output,JsonSerializer.Serialize(new {Action=action,Result=result,Profile=attribution,
            DiskReadBytes=nativeAfter.DiskReadBytes-nativeBefore.DiskReadBytes,DiskWrittenBytes=nativeAfter.DiskWrittenBytes-nativeBefore.DiskWrittenBytes,
            GcCollections=Enumerable.Range(0,3).Select(i=>GC.CollectionCount(i)-gcBefore[i]).ToArray(),
            CurrentRss=nativeAfter.ResidentBytes,PhysicalFootprint=nativeAfter.PhysicalFootprintBytes,
            SnapshotHash=PolicyIntegrationEval.PhysicalHash(root),Notes="Exclusive synchronous method wall time; file API cost is not pure disk wait; instrumentation overhead included."},new JsonSerializerOptions{WriteIndented=true}));
        return 0;
    }
}
