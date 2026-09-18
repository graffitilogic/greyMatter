using System.Buffers.Binary;
using System.Text.Json;
using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Storage;

namespace GreyMatter.Poc.Eval;

/// <summary>Registered cache-pressure experiment. Native I/O is macOS-only; no cold-disk claim.</summary>
public static class PackedResourceEval
{
    public const long OriginalBaseline = 53166080, ScratchAllowance = 4L * 1024 * 1024;
    public sealed record Memory(ulong PeakRss, ulong PeakFootprint, long PeakManaged, long Allocated);
    public sealed record Probe(int Index, int Chain, int Hops, int Correct, double[] Scores, double Top1, double Milliseconds);
    public sealed record Traffic(long DataRead, long DataWritten, long IndexRead, long IndexWritten, long Evictions, long DirtyWrites);
    public sealed record Pass(string Name, Probe[] Queries, double Seconds, Traffic Logical, MacFileIo.Usage KernelDelta);
    private sealed class Watch : IDisposable
    {
        private readonly object _sync = new(); private readonly Timer _timer;
        private ulong _rss, _footprint; private long _managed; private Exception? _error;
        public Watch() { Sample(); _timer = new Timer(_ => Sample(), null, 20, 20); }
        private void Sample()
        {
            lock (_sync)
            {
                try { var u = MacFileIo.Sample(); _rss = Math.Max(_rss, u.ResidentBytes); _footprint = Math.Max(_footprint, u.PhysicalFootprintBytes); _managed = Math.Max(_managed, GC.GetTotalMemory(false)); }
                catch (Exception e) { _error = e; }
            }
        }
        public Memory Finish()
        {
            Sample(); lock (_sync)
            {
                if (_error != null) throw new InvalidOperationException("Native resource sampling failed", _error);
                return new(_rss, _footprint, _managed, GC.GetTotalAllocatedBytes());
            }
        }
        public void Dispose() => _timer.Dispose();
    }
    private static Traffic Counters(PackedRelayRecords? store) => store == null ? new(0, 0, 0, 0, 0, 0) :
        new(store.BytesRead, store.BytesWritten, store.IndexBytesRead, store.IndexBytesWritten, store.Evictions, store.DirtyWrites);
    private static Traffic Subtract(Traffic a, Traffic b) => new(a.DataRead-b.DataRead,a.DataWritten-b.DataWritten,
        a.IndexRead-b.IndexRead,a.IndexWritten-b.IndexWritten,a.Evictions-b.Evictions,a.DirtyWrites-b.DirtyWrites);
    private static MacFileIo.Usage Difference(MacFileIo.Usage a, MacFileIo.Usage b) => new(
        checked(a.DiskReadBytes-b.DiskReadBytes), checked(a.DiskWrittenBytes-b.DiskWrittenBytes), a.ResidentBytes, a.PhysicalFootprintBytes);

    public static int Run(Args args)
    {
        string action = args.Value("--action", "query")!, backend = args.Value("--backend", "paged")!, io = args.Value("--io", "buffered")!;
        if (!new[] { "baseline", "train", "query" }.Contains(action) || !new[] { "paged", "resident" }.Contains(backend) || !new[] { "buffered", "nocache" }.Contains(io))
            throw new ArgumentException("Unregistered action/backend/I/O");
        int seed = int.Parse(args.Value("--seed", "100")!), mib = int.Parse(args.Value("--budget-mib", "128")!);
        if ((seed != 100 && seed != 201) || (mib != 128 && mib != 256)) throw new ArgumentException("Registered seed/budget");
        if (io == "nocache" && (action != "query" || backend != "paged")) throw new ArgumentException("No-cache is a paged read-only query condition");
        int chains = int.Parse(args.Value("--chains", seed == 100 ? "32" : "32768")!);
        if (seed == 100 ? chains != 32 : chains != 32768 && chains != 131072)
            throw new ArgumentException("Registered chain count");
        long budget = (long)mib * 1024 * 1024, cacheBudget = budget - ScratchAllowance;
        string output = Path.GetFullPath(args.Value("--output", null) ?? throw new ArgumentException("--output required"));
        if (File.Exists(output)) throw new IOException("Refusing result overwrite"); Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        using var memory = new Watch(); var total = System.Diagnostics.Stopwatch.StartNew();
        if (action == "baseline")
        {
            using var tiny = new ResidentRelayRecords(16); var l = new StoredRelayLearning(tiny);
            l.ObserveMembers(new uint[] { 1 }); l.ObserveMembers(new uint[] { 2 }); l.EndSequence();
            new StoredRelayRecall(tiny,256,4,ScratchAllowance).Run(new uint[] { 1 },1); Thread.Sleep(100);
            Save(new { Action = action, Memory = memory.Finish(), OriginalBaseline,
                Note = "Diagnostic empty runtime baseline; does not replace registered B" }); return 0;
        }
        string root = Path.GetFullPath(args.Value("--model-root", null) ?? throw new ArgumentException("--model-root required"));
        string inputHash = PolicyIntegrationEval.InputHash(seed, chains);
        if (action == "train")
        {
            using var store = new PackedRelayRecords(Path.Combine(root, "working"), CapacityEval.Space, cacheBudget);
            var learner = new StoredRelayLearning(store) { SourceLocalForgetting = true };
            var nativeBefore = MacFileIo.Sample(); var clock = System.Diagnostics.Stopwatch.StartNew();
            PolicyIntegrationEval.Train(store, learner, seed, chains, false); clock.Stop();
            Traffic trainingIo = Counters(store); var trainingNative = Difference(MacFileIo.Sample(),nativeBefore);
            double trainingSeconds = clock.Elapsed.TotalSeconds;
            clock.Restart(); PackedRelayCheckpoint.Publish(store, Path.Combine(root,"complete"), learner.Capture()); clock.Stop();
            double publicationSeconds = clock.Elapsed.TotalSeconds; long edges = 0; var bytes = new byte[RelayRecord.Bytes];
            store.VisitPresent(id => { store.Read(id,bytes); edges += BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(4)); });
            string snapshotHash = PolicyIntegrationEval.PhysicalHash(Path.Combine(root,"complete"));
            Save(new { Action=action, Seed=seed, Chains=chains, BudgetBytes=budget, CacheBudgetBytes=cacheBudget, store.ReservedBytes,
                InputHash=inputHash, State=learner.Capture(), Records=store.Count, Edges=edges, PayloadBytes=store.Count*RelayRecord.Bytes,
                store.IndexCapacity, TrainingSeconds=trainingSeconds, PublicationSeconds=publicationSeconds,
                TrainingIo=trainingIo, TrainingNative=trainingNative, SnapshotHash=snapshotHash,
                Memory=memory.Finish(), TotalSeconds=total.Elapsed.TotalSeconds }); return 0;
        }
        string expected = args.Value("--expected-hash",null) ?? throw new ArgumentException("--expected-hash from completed training required");
        string snapshot = Path.Combine(root,"complete"); var startup = System.Diagnostics.Stopwatch.StartNew();
        var saved = PackedRelayCheckpoint.OpenReadOnly(snapshot,backend == "paged" ? cacheBudget : PackedRelayRecords.FixedCharge + 8*PackedRelayRecords.SlotCharge);
        using var source = saved.Store;
        if (saved.State.Policy != RelayLearningPolicy.SourceLocal || saved.State.Episodes != chains*4*16) throw new InvalidDataException("Wrong model/state");
        using var resident = backend == "resident" ? new ResidentRelayRecords(CapacityEval.Space) : null;
        long heapBefore = GC.GetTotalMemory(true), residentBytes = 0;
        if (resident != null)
        {
            var bytes = new byte[RelayRecord.Bytes]; source.VisitPresent(id => { source.Read(id,bytes); resident.Write(id,bytes); });
            residentBytes = GC.GetTotalMemory(true) - heapBefore;
        }
        else if (io == "nocache") source.SetReadNoCache(true);
        IRelayRecords records = resident ?? (IRelayRecords)source;
        var runtime = new StoredRelayRecall(records,256,4,ScratchAllowance); var input = new CapacityEval.Input(seed);
        startup.Stop(); double startupSeconds = startup.Elapsed.TotalSeconds;
        var passes = new List<Pass>();
        foreach (bool reverse in new[] { false,true })
        {
            var trafficBefore = Counters(resident == null ? source : null); var nativeBefore = MacFileIo.Sample();
            var timer = System.Diagnostics.Stopwatch.StartNew(); var queries = new List<Probe>();
            int count = Math.Min(64,chains)*2;
            for (int at = 0; at < count; at++)
            {
                int index = reverse ? count-1-at : at, q=index/2, chain=(seed+q*(chains/64+1))%chains;
                int hops=index%2==0 ? 1 : 2+q%3, correct=q%32;
                var elapsed = System.Diagnostics.Stopwatch.StartNew(); runtime.Run(input.Members(chain*5),hops);
                var scores = new double[32];
                for (int c=0;c<32;c++)
                {
                    int candidate=(chain+(c-correct+32)%32)%chains;
                    foreach(uint id in input.Members(candidate*5+hops)) scores[c]+=runtime.Value(id);
                }
                elapsed.Stop(); queries.Add(new(index,chain,hops,correct,scores,RecoveryLearning.ScoreRank(scores,correct).Top1,elapsed.Elapsed.TotalMilliseconds));
            }
            timer.Stop(); var nativeAfter=MacFileIo.Sample();
            passes.Add(new(reverse ? "repeat-reverse" : "first-forward",queries.ToArray(),timer.Elapsed.TotalSeconds,
                Subtract(Counters(resident == null ? source : null),trafficBefore),Difference(nativeAfter,nativeBefore)));
        }
        bool immutable=PolicyIntegrationEval.PhysicalHash(snapshot)==expected;
        Save(new { Action=action, Seed=seed, Chains=chains, Backend=backend, Io=io, BudgetBytes=budget, CacheBudgetBytes=cacheBudget,
            StoreReservedBytes=source.ReservedBytes, TraversalReservedBytes=runtime.ReservedBytes, ResidentManagedDeltaBytes=residentBytes,
            Records=source.Count, PayloadBytes=source.Count*RelayRecord.Bytes, InputHash=inputHash, State=saved.State,
            StartupSeconds=startupSeconds, Passes=passes, Immutable=immutable, SnapshotHash=expected,
            Memory=memory.Finish(), TotalSeconds=total.Elapsed.TotalSeconds,
            CacheNote="First: empty application cache (resident preloaded); repeat: retained caches. OS/hardware not purged. F_NOCACHE advisory only." });
        return immutable && passes.All(p=>p.Logical.DataWritten==0 && p.Logical.IndexWritten==0) ? 0 : 1;
        void Save(object result) { File.WriteAllText(output,JsonSerializer.Serialize(result,new JsonSerializerOptions { WriteIndented=true })); Console.WriteLine($"{action} seed{seed} {mib}MiB {backend}/{io} complete"); }
    }
}
