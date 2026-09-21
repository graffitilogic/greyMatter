using System.Buffers.Binary;
using System.Security.Cryptography;
using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Storage;

namespace GreyMatter.Poc.Utility;

/// <summary>Immutable named model. Text labels/source paths never enter its numeric files.</summary>
public static class LocalModel
{
    public const long AdapterAndScratch = 8L*1024*1024;
    public sealed record Description(int Seed, long Sentences, long Tokens, long SourceBytes, string SourceSha256);
    public sealed record Hit(string Candidate, double Score);
    public sealed record Recall(Hit[] Results, long StoreReservedBytes, long TraversalReservedBytes,
        long DataRead, long IndexRead, long Evictions, int Steps, int Delivered, string TiePolicy);
    public static long CacheBudget(int mib)
    {
        if (mib != 128 && mib != 256) throw new ArgumentException("Memory budget must be 128 or 256 MiB");
        return (long)mib*1024*1024-AdapterAndScratch;
    }
    public static RelayLearningPolicy ParsePolicy(string policy) => policy switch
    {
        "source-local" => RelayLearningPolicy.SourceLocal,
        "sparse-relay" => RelayLearningPolicy.SparseRelay,   // D1 experiment; explicit, never default
        "count-baseline" => RelayLearningPolicy.CountBaseline, // CB baseline; one record per token, counts, no decay
        _ => throw new ArgumentException("Learning policy must be source-local, sparse-relay or count-baseline")
    };
    public static Description Train(string source, string format, string model, int seed, int budgetMiB, string policy = "source-local")
    {
        var learningPolicy = ParsePolicy(policy);
        long budget = CacheBudget(budgetMiB); model = Path.GetFullPath(model);
        if (Directory.Exists(model) || File.Exists(model)) throw new IOException("Refusing to overwrite a model");
        byte[] before = Hash(source); long sourceBytes = new FileInfo(source).Length;
        string staging = model+".building-"+Guid.NewGuid().ToString("N"); Directory.CreateDirectory(staging);
        try
        {
            long sentences = 0, tokens = 0;
            using (var store = new PackedRelayRecords(Path.Combine(staging,"working"),LocalText.Space,budget))
            {
                var learner = new StoredRelayLearning(store) { SourceLocalForgetting = learningPolicy == RelayLearningPolicy.SourceLocal,
                    SparseRelayConnectivity = learningPolicy == RelayLearningPolicy.SparseRelay, CountBaseline = learningPolicy == RelayLearningPolicy.CountBaseline };
                foreach (var words in LocalText.Sentences(source,format))
                {
                    foreach (string word in words) { learner.ObserveMembers(Cohort(word,seed,learningPolicy)); tokens++; }
                    learner.EndSequence(); sentences++;
                }
                if (tokens == 0) throw new InvalidDataException("Source has no supported text");
                if (!before.AsSpan().SequenceEqual(Hash(source))) throw new IOException("Source changed during learning");
                PackedRelayCheckpoint.Publish(store,Path.Combine(staging,"snapshot"),learner.Capture());
            }
            Directory.Delete(Path.Combine(staging,"working"),true);
            var description = new Description(seed,sentences,tokens,sourceBytes,Convert.ToHexString(before));
            WriteDescription(staging,description,format); RelayCheckpoint.SyncDirectory(staging);
            Directory.Move(staging,model); RelayCheckpoint.SyncDirectory(Path.GetDirectoryName(model)!);
            return description;
        }
        catch { if (Directory.Exists(staging)) Directory.Delete(staging,true); throw; }
    }
    internal static byte[] Hash(string path) { using var attribution = Eval.CostProfile.Enter(Eval.CostProfile.Kind.SourceHash); using var stream = File.OpenRead(path); return SHA256.HashData(stream); }
    private static void WriteDescription(string root, Description d, string format)
    {
        var b = new byte[192];
        BinaryPrimitives.WriteUInt32LittleEndian(b,0x31544D47); BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(4),1);
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(8),d.Seed); BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(12),LocalText.Space);
        foreach(int at in new[]{16,20,24,28}) BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(at),1);
        BinaryPrimitives.WriteInt64LittleEndian(b.AsSpan(32),d.Sentences); BinaryPrimitives.WriteInt64LittleEndian(b.AsSpan(40),d.Tokens);
        BinaryPrimitives.WriteInt64LittleEndian(b.AsSpan(48),d.SourceBytes); Convert.FromHexString(d.SourceSha256).CopyTo(b,56);
        Hash(Path.Combine(root,"snapshot","manifest.bin")).CopyTo(b,88);
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(120),format=="text"?1:2);
        SHA256.HashData(b.AsSpan(0,160),b.AsSpan(160)); RelayCheckpoint.DurableWrite(Path.Combine(root,"text.bin"),b);
    }
    public static Description Describe(string root)
    {
        var b = RelayCheckpoint.SmallFile(Path.Combine(root,"text.bin"),192);
        if (!SHA256.HashData(b.AsSpan(0,160)).AsSpan().SequenceEqual(b.AsSpan(160)) || BinaryPrimitives.ReadUInt32LittleEndian(b)!=0x31544D47 ||
            BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(4))!=1 || BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(12))!=LocalText.Space ||
            new[]{16,20,24,28}.Any(at=>BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(at))!=1) ||
            !new[]{1,2}.Contains(BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(120))) || b.AsSpan(124,36).IndexOfAnyExcept((byte)0)>=0 ||
            !Hash(Path.Combine(root,"snapshot","manifest.bin")).AsSpan().SequenceEqual(b.AsSpan(88,32)))
            throw new InvalidDataException("Text model version/configuration/checksum");
        var d = new Description(BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(8)),BinaryPrimitives.ReadInt64LittleEndian(b.AsSpan(32)),
            BinaryPrimitives.ReadInt64LittleEndian(b.AsSpan(40)),BinaryPrimitives.ReadInt64LittleEndian(b.AsSpan(48)),Convert.ToHexString(b.AsSpan(56,32)));
        if (d.Sentences<1 || d.Tokens<d.Sentences || d.SourceBytes<1) throw new InvalidDataException("Text model counts"); return d;
    }
    /// <summary>Policy identity comes from the checkpoint metadata, never from a caller flag; probe reports it.</summary>
    public static (PackedRelayRecords Store, Description Description, RelayLearningPolicy Policy) Open(string root, int budgetMiB)
    {
        var description=Describe(root); var saved=PackedRelayCheckpoint.OpenReadOnly(Path.Combine(root,"snapshot"),CacheBudget(budgetMiB));
        bool textPolicy = saved.State.Policy is RelayLearningPolicy.SourceLocal or RelayLearningPolicy.SparseRelay or RelayLearningPolicy.CountBaseline;
        if (saved.Store.IdLimit!=LocalText.Space || !textPolicy || saved.State.Previous.Length!=0 ||
            saved.State.Episodes!=description.Sentences || saved.State.Observations!=description.Tokens)
        { saved.Store.Dispose(); throw new InvalidDataException("Text model training contract mismatch"); }
        return (saved.Store,description,saved.State.Policy);
    }
    /// <summary>CB uses one record per token (its first cohort member); relay policies use the whole cohort.</summary>
    internal static uint[] Cohort(string canonical, int seed, RelayLearningPolicy policy)
    {
        var members = LocalText.Members(canonical, seed);
        return policy == RelayLearningPolicy.CountBaseline ? members[..1] : members;
    }
    public static Recall Query(IRelayRecords records, int seed, string cue, string[] candidates, int hops, RelayLearningPolicy policy = RelayLearningPolicy.SourceLocal)
    {
        if (hops<1 || hops>4 || candidates.Length<1 || candidates.Length>LocalText.MaxCandidates) throw new ArgumentException("Query bounds");
        cue=LocalText.Token(cue); candidates=candidates.Select(LocalText.Token).ToArray();
        if (candidates.Distinct(StringComparer.Ordinal).Count()!=candidates.Length) throw new ArgumentException("Duplicate candidates");
        var store=records as PackedRelayRecords; long reads=store?.BytesRead??0,index=store?.IndexBytesRead??0,evictions=store?.Evictions??0;
        var runtime=new StoredRelayRecall(records,256,4,4L*1024*1024); runtime.Run(Cohort(cue,seed,policy),hops);
        var hits=candidates.Select(c=>new Hit(c,Cohort(c,seed,policy).Sum(id=>runtime.Value(id)))).ToArray();
        return new(hits,store?.ReservedBytes??0,runtime.ReservedBytes,(store?.BytesRead??0)-reads,(store?.IndexBytesRead??0)-index,
            (store?.Evictions??0)-evictions,runtime.StepCount,runtime.DeliveredCount,"Equal scores remain ties; display uses ordinal label order");
    }
    public static object Audit(string root, int budgetMiB)
    {
        var allowed=new HashSet<string>(StringComparer.Ordinal){"text.bin","snapshot/manifest.bin","snapshot/00000000000000000001/state.bin",
            "snapshot/00000000000000000001/format.bin","snapshot/00000000000000000001/index.bin","snapshot/00000000000000000001/records.bin"};
        var actual=Directory.GetFiles(root,"*",SearchOption.AllDirectories).Select(p=>Path.GetRelativePath(root,p)).ToHashSet(StringComparer.Ordinal);
        if (!actual.SetEquals(allowed)) throw new InvalidDataException("Unexpected/missing model files (external labels do not belong in the model)");
        var saved=Open(root,budgetMiB); using var store=saved.Store; long count=0,edges=0; var b=new byte[RelayRecord.Bytes];
        store.VisitPresent(id=>
        {
            if (!store.Read(id,b)) throw new InvalidDataException("Missing record"); RelayRecord.Validate(id,b);
            int degree=BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(4));
            for(int e=0;e<degree;e++) if(BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(8+9*e))>=LocalText.Space) throw new InvalidDataException("Target outside space");
            if (b.AsSpan(8+9*degree,RelayRecord.PayloadBytes-8-9*degree).IndexOfAnyExcept((byte)0)>=0) throw new InvalidDataException("Nonzero record padding");
            count++;edges+=degree;
        });
        return new { NumericSchema=true, Records=count, Edges=edges, Saved=saved.Description, Policy=saved.Policy.ToString(),
            Note="Structural audit; not proof that learned numeric state cannot encode semantic information" };
    }
}
