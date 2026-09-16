using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Storage;

namespace GreyMatter.Poc.Eval;

/// <summary>R4 isolated-process workers. Synthetic numeric input; no text-encoder claims.</summary>
public static class CapacityEval
{
    public const int Space = 16_000_000, MaximumChains = 131072;
    private const int Repeats = 16;
    private const long ScratchAllowance = 4L * 1024 * 1024;
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    public sealed record Memory(long PeakRssBytes, long SampledManagedPeakBytes, long AllocatedBytes);
    private sealed class Watch : IDisposable
    {
        private readonly Process _process = Process.GetCurrentProcess();
        private readonly object _sync = new(); private readonly Timer _timer;
        private bool _closed; private long _rss, _heap;
        public Watch() { _timer = new Timer(_ => Sample(), null, 0, 10); }
        private void Sample()
        {
            lock (_sync)
            {
                if (_closed) return;
                _process.Refresh(); _rss = Math.Max(_rss, Math.Max(_process.WorkingSet64, _process.PeakWorkingSet64));
                _heap = Math.Max(_heap, Math.Max(GC.GetTotalMemory(false), GC.GetGCMemoryInfo().HeapSizeBytes));
            }
        }
        public Memory Finish() { Sample(); lock (_sync) return new(_rss, _heap, GC.GetTotalAllocatedBytes()); }
        public void Dispose() { lock (_sync) { _closed = true; _timer.Dispose(); _process.Dispose(); } }
    }
    // One dimension from each of 32 disjoint bins: sorted unique, stateless, no vocabulary cache.
    internal sealed class Input(int seed)
    {
        private readonly int[] _dims = new int[32]; private readonly uint[] _members = new uint[8];
        public ReadOnlySpan<uint> Members(int concept)
        {
            for (int d = 0; d < 32; d++) _dims[d] = 64 * d + (int)(Substrate.Rng.Bits(seed, Substrate.Rng.Purpose.Projection, (uint)concept, (uint)d) % 64);
            Runtime.Assembly.Members(new SparseCode(_dims), Space, _members);
            int n = 0;
            for (int i = 0; i < 8; i++)
            {
                bool duplicate = false; for (int j = 0; j < n; j++) duplicate |= _members[j] == _members[i];
                if (!duplicate) _members[n++] = _members[i];
            }
            return _members.AsSpan(0, n);
        }
    }
    private static void Warm()
    {
        // Fixed tiny model warms common numerical code; no workload/cache-sized allocation.
        using var records = new ResidentRelayRecords(Space); var input = new Input(100);
        var learning = new StoredRelayLearning(records);
        learning.ObserveMembers(input.Members(0)); learning.ObserveMembers(input.Members(1)); learning.EndSequence();
        var recall = new StoredRelayRecall(records, 256, 4, ScratchAllowance);
        recall.Run(input.Members(0), 4);
    }
    private static (long Records, long Edges, string Hash) Census(IRelayRecords records)
    {
        long count = 0, edges = 0; var buffer = new byte[RelayRecord.Bytes];
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        records.VisitPresent(id =>
        {
            if (!records.Read(id, buffer)) throw new InvalidDataException("Census missing record");
            count++; edges += BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(4)); hash.AppendData(buffer);
        });
        return (count, edges, Convert.ToHexString(hash.GetHashAndReset()));
    }
    private static void Write(string output, object value)
    { File.WriteAllText(output, JsonSerializer.Serialize(value, Json)); Console.WriteLine(JsonSerializer.Serialize(value)); }
    public static int Run(Args args)
    {
        string action = args.Value("--action", "development")!;
        string output = Path.GetFullPath(args.Value("--output", null) ?? throw new ArgumentException("--output required"));
        if (File.Exists(output)) throw new IOException("Refusing result overwrite");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        if (action == "assess") return Assess(args, output);
        int seed = int.Parse(args.Value("--seed", "100")!), chains = int.Parse(args.Value("--chains", "32")!);
        int mib = int.Parse(args.Value("--budget-mib", "256")!);
        if (mib != 256 && mib != 128) throw new ArgumentException("Registered budgets are 256/128 MiB");
        if (chains != 32 && chains != 8192 && chains != 32768 && chains != MaximumChains) throw new ArgumentException("Unregistered workload size");
        if (seed != 100 && (seed < 201 || seed > 205)) throw new ArgumentException("Unregistered seed");
        string decayMode = args.Value("--decay", "eager")!;
        if (decayMode != "eager" && decayMode != "deferred") throw new ArgumentException("Decay mode");
        bool deferredMode = decayMode == "deferred";
        bool retentionDiagnostic = args.Has("--retention-diagnostic"), disableDecay = args.Has("--disable-decay");
        if (disableDecay && !retentionDiagnostic) throw new ArgumentException("--disable-decay requires --retention-diagnostic");
        long budget = (long)mib * 1024 * 1024, cacheBudget = budget - ScratchAllowance;
        using var watch = new Watch(); Warm();
        if (action == "baseline")
        {
            Thread.Sleep(100); Write(output, new { Action = action, Memory = watch.Finish(),
                Description = "Fresh process, common runtime warmed on one tiny pair; no workload-sized model or record cache" }); return 0;
        }
        if (action == "train" || action == "development")
        {
            string model = Path.Combine(Path.GetDirectoryName(output)!, "model");
            using IRelayRecords records = deferredMode ? new DeferredRelayRecords(model, Space, cacheBudget) : new DiskRelayRecords(model, Space, cacheBudget);
            var deferred = records as DeferredRelayRecords; var disk = deferred?.Cache ?? (DiskRelayRecords)records;
            var input = new Input(seed); var learner = new StoredRelayLearning(records) { DisableDecayForDiagnostic = disableDecay };
            var trace = new List<object>();
            var tracked = new HashSet<int>(Enumerable.Range(0, 7).Select(q => (int)(((long)q * MaximumChains / 100 + seed) % MaximumChains) * 4));
            var traceBuffer = new byte[RelayRecord.Bytes];
            float? Weight(uint source, uint target)
            {
                if (!records.Read(source, traceBuffer)) return null;
                int degree = BinaryPrimitives.ReadInt32LittleEndian(traceBuffer.AsSpan(4));
                for (int e = 0; e < degree; e++)
                    if (BinaryPrimitives.ReadUInt32LittleEndian(traceBuffer.AsSpan(8 + 9 * e)) == target)
                        return BinaryPrimitives.ReadSingleLittleEndian(traceBuffer.AsSpan(12 + 9 * e));
                return null;
            }
            int episodes = checked(chains * 4 * Repeats), mask = episodes - 1;
            uint odd = (uint)Substrate.Rng.Mix((uint)seed) | 1u;
            uint offset = (uint)Substrate.Rng.Mix((uint)seed ^ 0xb519u);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            Span<byte> pair = stackalloc byte[8]; var elapsed = Stopwatch.StartNew();
            for (int i = 0; i < episodes; i++)
            {
                int index = (int)(unchecked((uint)i * odd + offset) & (uint)mask), relation = index % (chains * 4);
                int concept = relation / 4 * 5 + relation % 4;
                BinaryPrimitives.WriteInt32LittleEndian(pair, concept); BinaryPrimitives.WriteInt32LittleEndian(pair[4..], concept + 1); hash.AppendData(pair);
                bool observe = retentionDiagnostic && tracked.Contains(relation);
                uint sourceId = 0, targetId = 0; float? beforeWeight = null;
                if (observe) { sourceId = input.Members(concept)[0]; targetId = input.Members(concept + 1)[0]; beforeWeight = Weight(sourceId, targetId); }
                learner.ObserveMembers(input.Members(concept)); learner.ObserveMembers(input.Members(concept + 1)); learner.EndSequence();
                if (observe) trace.Add(new { Episode = i + 1, Relation = relation, Source = sourceId, Target = targetId,
                    Epoch = deferred?.Epoch ?? 0, Before = beforeWeight, After = Weight(sourceId, targetId) });
            }
            records.Flush(); elapsed.Stop();
            if (retentionDiagnostic) File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!, "retention-trace.json"), JsonSerializer.Serialize(new { DisableDecay = disableDecay, Rows = trace }, Json));
            long read = disk.BytesRead + (deferred?.EpochBytesRead ?? 0), written = disk.BytesWritten + (deferred?.EpochBytesWritten ?? 0);
            long agingTicks = deferred?.AgingTicks ?? 0, replaySteps = deferred?.ReplaySteps ?? 0;
            var census = Census(records);
            Write(output, new { Action = "train", Seed = seed, Chains = chains, Episodes = episodes, Model = model,
                IdLimit = Space, DegreeCap = 32, Width = 256, MaxTicks = 4, Repeats, BudgetBytes = budget, CacheBudgetBytes = cacheBudget,
                RetentionDiagnostic = retentionDiagnostic, DisableDecay = disableDecay,
                StorageVersion = deferredMode ? 2 : 1, Decay = decayMode, Epoch = deferred?.Epoch ?? 0,
                ReservedBytes = deferred?.ReservedBytes ?? disk.ReservedBytes, ScratchAllowance, ActualRecords = census.Records, ActualEdges = census.Edges,
                RecordPayloadBytes = census.Records * RelayRecord.Bytes, RecordSha256 = census.Hash,
                CorpusSha256 = Convert.ToHexString(hash.GetHashAndReset()), learner.Updates, learner.DecayVisits,
                TrainingSeconds = elapsed.Elapsed.TotalSeconds, DecaySeconds = learner.DecayElapsedTicks / (double)Stopwatch.Frequency,
                DecayRecordSeconds = learner.DecayRecordTicks / (double)Stopwatch.Frequency,
                BytesReadDuringTraining = read, BytesWrittenDuringTraining = written, disk.Evictions,
                AgingSecondsDuringTraining = agingTicks / (double)Stopwatch.Frequency, ReplayStepsDuringTraining = replaySteps,
                SnapshotEpochBytes = deferredMode ? census.Records * DeferredRelayRecords.EpochRecordBytes : 0,
                Memory = watch.Finish(), Encoder = "stateless numeric synthetic input; no text encoder", Result = "MEASURED_NOT_A_CAPACITY_PASS" });
            return 0;
        }
        if (action == "query")
        {
            string model = Path.GetFullPath(args.Value("--model", null) ?? throw new ArgumentException("--model required"));
            bool paged = args.Value("--backend", "paged") == "paged";
            long heapBefore = GC.GetTotalMemory(true); var startup = Stopwatch.StartNew();
            using IRelayRecords store = !paged ? new ResidentRelayRecords(Space) : deferredMode
                ? new DeferredRelayRecords(model, Space, cacheBudget, true) : new DiskRelayRecords(model, Space, cacheBudget, existing: true);
            if (!paged)
            {
                using IRelayRecords source = deferredMode ? new DeferredRelayRecords(model, Space, DeferredRelayCheckpoint.CopyBudget, true)
                    : new DiskRelayRecords(model, Space, DiskRelayRecords.FixedCharge + DiskRelayRecords.SlotCharge, existing: true);
                var record = new byte[RelayRecord.Bytes]; source.VisitPresent(id => { source.Read(id, record); store.Write(id, record); });
            }
            long residentHeapBytes = GC.GetTotalMemory(true) - heapBefore;
            var before = Census(store); var input = new Input(seed); var runtime = new StoredRelayRecall(store, 256, 4, ScratchAllowance);
            var deferred = store as DeferredRelayRecords; var disk = deferred?.Cache ?? store as DiskRelayRecords; disk?.ClearCache(); startup.Stop();
            long initialRead = (disk?.BytesRead ?? 0) + (deferred?.EpochBytesRead ?? 0), initialWrites = (disk?.BytesWritten ?? 0) + (deferred?.EpochBytesWritten ?? 0);
            var rows = new List<object>(); var scoresAll = new double[100][]; var tops = new double[100]; var mrr = new double[100];
            var times = new double[100]; int querySpace = chains == 32 ? 32 : MaximumChains, supported = 0;
            for (int q = 0; q < 100; q++)
            {
                int correctChain = (int)(((long)q * querySpace / 100 + seed) % querySpace);
                int hops = q < 50 ? 1 : 2 + q % 3, correct = q % 32;
                var elapsed = Stopwatch.StartNew(); runtime.Run(input.Members(correctChain * 5), hops);
                var scores = new double[32];
                for (int c = 0; c < 32; c++)
                {
                    int candidate = (correctChain + (c - correct + 32) % 32 * (querySpace / 32 * 2 + 1)) % querySpace;
                    foreach (uint id in input.Members(candidate * 5 + hops)) scores[c] += runtime.Value(id);
                }
                elapsed.Stop(); times[q] = elapsed.Elapsed.TotalMilliseconds; scoresAll[q] = scores;
                var rank = RecoveryLearning.ScoreRank(scores, correct);
                bool available = correctChain < chains; if (available) supported++;
                tops[q] = available ? rank.Top1 : 0; mrr[q] = available ? rank.ReciprocalRank : 0;
                rows.Add(new { Index = q, Chain = correctChain, Hops = hops, Correct = correct, Supported = available,
                    Scores = scores, Rank = rank, Top1Credit = tops[q], MrrCredit = mrr[q] });
            }
            long queryRead = (disk?.BytesRead ?? 0) + (deferred?.EpochBytesRead ?? 0) - initialRead,
                queryWrites = (disk?.BytesWritten ?? 0) + (deferred?.EpochBytesWritten ?? 0) - initialWrites;
            var after = Census(store); bool immutable = before.Hash == after.Hash;
            Write(output, new { Action = action, Decay = decayMode, StorageVersion = deferredMode ? 2 : 1, Seed = seed, Chains = chains, Backend = paged ? "paged" : "resident",
                BudgetBytes = budget, CacheBudgetBytes = cacheBudget, ScratchReservedBytes = runtime.ReservedBytes,
                ResidentManagedDeltaBytes = paged ? 0 : residentHeapBytes, ModelRecords = before.Records, ModelEdges = before.Edges,
                PayloadBytes = before.Records * RelayRecord.Bytes, RecordSha256 = before.Hash, Immutable = immutable,
                StartupSeconds = startup.Elapsed.TotalSeconds, P50Milliseconds = times.OrderBy(t => t).ElementAt(49),
                P95Milliseconds = times.OrderBy(t => t).ElementAt(94), ThroughputQueriesPerSecond = 100000 / times.Sum(),
                DirectTop1 = tops.Take(50).Average(), ComposedTop1 = tops.Skip(50).Average(), Mrr = mrr.Average(), SupportedQueries = supported, SupportedTop1 = supported == 0 ? 0 : tops.Sum() / supported,
                QueryBytesRead = queryRead, QueryBytesWritten = queryWrites, Scores = scoresAll, Queries = rows,
                Memory = watch.Finish(), Result = "MEASURED_NOT_A_CAPACITY_PASS" });
            return immutable && queryWrites == 0 ? 0 : 1;
        }
        throw new ArgumentException("Actions: baseline, development/train, query, assess");
    }
    private static int Assess(Args args, string output)
    {
        JsonElement Read(string flag) => JsonDocument.Parse(File.ReadAllText(args.Value(flag, null) ?? throw new ArgumentException(flag))).RootElement.Clone();
        JsonElement train = Read("--training"), resident = Read("--resident"), paged = Read("--paged"), baseline = Read("--baseline");
        double Number(JsonElement x, string field) => x.GetProperty(field).GetDouble();
        bool exact = resident.GetProperty("Scores").GetRawText() == paged.GetProperty("Scores").GetRawText();
        long Peak(string flag, JsonElement measured)
        {
            string? log = args.Value(flag, null);
            if (log is null) return measured.GetProperty("Memory").GetProperty("PeakRssBytes").GetInt64();
            string line = File.ReadLines(log).Single(l => l.Contains("maximum resident set size"));
            return long.Parse(line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]); // macOS time -l: bytes
        }
        long b = Peak("--baseline-time", baseline), trainPeak = Peak("--training-time", train), pagedPeak = Peak("--paged-time", paged);
        double trainingSeconds = Number(train, "TrainingSeconds"), decaySeconds = Number(train, "DecaySeconds"), visits = Number(train, "DecayVisits");
        double recordSeconds = Number(train, "DecayRecordSeconds");
        long fullEpisodes = (long)MaximumChains * 4 * Repeats;
        double expectedRecords = Space * (1 - Math.Exp(-(double)MaximumChains * 5 * 8 / Space));
        double estimatedVisits = .5 * expectedRecords * Math.Floor(fullEpisodes / 500.0);
        // Half the final occupancy is deliberately optimistic; a full cache in the smoke
        // understates the future eviction/I/O cost. This is an estimate, not measured scale.
        bool deferredForecast = train.TryGetProperty("StorageVersion", out var version) && version.GetInt32() == 2;
        double estimatedLargestSeconds = Math.Max(0, trainingSeconds - decaySeconds) * fullEpisodes / Number(train, "Episodes")
            + recordSeconds / Math.Max(1, visits) * estimatedVisits
            + Math.Max(0, decaySeconds - recordSeconds) * Math.Floor(fullEpisodes / 500.0) / Math.Floor(Number(train, "Episodes") / 500);
        if (deferredForecast) estimatedLargestSeconds = trainingSeconds * fullEpisodes / Number(train, "Episodes");
        bool overBudget = estimatedLargestSeconds > 3600;
        Write(output, new { Phase = "R4", Verdict = overBudget ? "R4_FULL_RUN_REQUIRES_TIME_AUTHORIZATION" : "R4_DEVELOPMENT_COMPLETE",
            BaselineRssBytes = b, DevelopmentExactScores = exact,
            TrainingPeakRssBytes = trainPeak, PagedPeakRssBytes = pagedPeak,
            DevelopmentRssWithinBound = pagedPeak <= b + 1.25 * Number(paged, "BudgetBytes") && trainPeak <= b + 1.25 * Number(train, "BudgetBytes"),
            DevelopmentDirectTop1 = Number(paged, "DirectTop1"), DevelopmentComposedTop1 = Number(paged, "ComposedTop1"),
            DevelopmentLatencyRatio = Number(paged, "P95Milliseconds") / Number(resident, "P95Milliseconds"),
            EstimatedLargestTrainingHours = estimatedLargestSeconds / 3600, EstimatedLargestRecords = expectedRecords,
            MeasuredCachedDecayMicrosecondsPerRecord = 1e6 * recordSeconds / Math.Max(1, visits),
            EstimatedLargestDecayVisits = estimatedVisits, RegisteredMBytes = 256L * 1024 * 1024,
            RequiredResidentBytes = 1024L * 1024 * 1024, FullGridRun = false, CapacityGatePassed = false,
            Explanation = deferredForecast
                ? "Deferred estimate is linear extrapolation from the tiny cached smoke, excluding increased catch-up age, cache misses and final census/checkpoint cost; insufficient by itself to authorize the full grid."
                : "Largest-training estimate uses half final occupancy, measured cached per-record decay cost, and separately scaled per-pass overhead; it is not a scale result or a hardware lower bound." });
        return exact ? 0 : 1;
    }
}
