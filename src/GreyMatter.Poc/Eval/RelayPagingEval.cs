using System.Security.Cryptography;
using System.Text.Json;
using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Storage;
using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Eval;

public static class RelayPagingEval
{
    private const long ScratchBudget = 2 * 1024 * 1024;
    private sealed record Frozen(int Chain, int Start, int Hops, double[] Scores);
    private sealed record Query(int Chain, int Start, int Hops, double[] Scores, RecoveryLearning.Rank Rank,
        bool Exact, bool WithinTolerance, bool SameRanking, long Hits, long Loads, long Evictions, long BytesRead, int Delivered,
        AssemblyRelay.Step[] Steps);
    private sealed record Cell(int CacheSlots, string Order, long CacheReservedBytes, long ScratchReservedBytes,
        long Hits, long Loads, long Evictions, long BytesRead, long BytesWritten, int Truncations, Query[] Queries);
    private sealed record Seed(int Value, string SnapshotSha256, string CodesSha256, int Records,
        bool OriginalReplayExact, bool ResidentExact, bool Immutable, Dictionary<string,string> StoreChecksums,
        Cell[] Cells);
    private static string Hash(string path)
    { using var f = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(f)); }
    private static Dictionary<string,string> HashStore(string directory) =>
        new[] { "records.bin", "presence.bin" }.ToDictionary(n => n, n => Hash(Path.Combine(directory, n)));
    private static bool Close(double[] a, double[] b) => a.Zip(b).All(p =>
        Math.Abs(p.First - p.Second) <= 1e-6 + 1e-5 * Math.Abs(p.Second));
    private static bool Ranking(double[] a, double[] b)
    {
        for (int i = 0; i < a.Length; i++) for (int j = i + 1; j < a.Length; j++)
            if (a[i].CompareTo(a[j]) != b[i].CompareTo(b[j])) return false;
        return true;
    }
    // Stream the frozen graph once. Threshold/familiarity/fatigue are not read by A1
    // relay propagation; retain source snapshot hash and verify old-runtime replay.
    private static (Config Config, int Count) Import(string snapshot, IRelayRecords resident, IRelayRecords disk)
    {
        using var r = new BinaryReader(File.OpenRead(snapshot));
        if (r.ReadInt32() != 0x54325331) throw new InvalidDataException("A1 snapshot version");
        var cfg = new Config { BaselineNeuronCount = r.ReadInt32(), WorkingSetMax = r.ReadInt32(),
            SynapseCapPerNeuron = r.ReadInt32(), Sparsity = r.ReadInt32(), ActivationWidth = r.ReadInt32(),
            PropagatedWinnerQuota = r.ReadInt32(), Seed = r.ReadInt32() };
        if (cfg.SynapseCapPerNeuron != RelayRecord.Cap || cfg.BaselineNeuronCount != resident.IdLimit || cfg.ActivationWidth != 256)
            throw new InvalidDataException("Unsupported frozen model configuration");
        int count = r.ReadInt32(); if (count < 0 || count > cfg.WorkingSetMax) throw new InvalidDataException("Record count");
        var syn = new SynapseStore(1, RelayRecord.Cap); var buffer = new byte[RelayRecord.Bytes];
        for (int i = 0; i < count; i++)
        {
            uint id = r.ReadUInt32(); r.ReadSingle(); r.ReadSingle(); r.ReadSingle();
            int degree = r.ReadInt32(); if (degree < 0 || degree > RelayRecord.Cap) throw new InvalidDataException("Degree");
            if (resident.Read(id, buffer)) throw new InvalidDataException("Duplicate source");
            syn.Degree[0] = degree;
            for (int e = 0; e < degree; e++) { syn.Target[e] = r.ReadUInt32(); syn.Weight[e] = r.ReadSingle(); syn.Population[e] = r.ReadByte(); }
            RelayRecord.Encode(id, syn, buffer); resident.Write(id, buffer); disk.Write(id, buffer);
        }
        if (r.BaseStream.Position != r.BaseStream.Length) throw new InvalidDataException("Trailing data");
        disk.Flush(); return (cfg, count);
    }
    private static AssemblyRelay.Step[] Steps(StoredRelayRecall runtime) => Enumerable.Range(0, runtime.StepCount).Select(runtime.Step).ToArray();
    private static double[] Scores(StoredRelayRecall runtime, uint[][][] members, Frozen q) =>
        members.Select(c => c[q.Start + q.Hops].Sum(runtime.Value)).ToArray();

    public static int Run(Args args)
    {
        var seeds = args.Value("--seeds", "100")!.Split(',').Select(int.Parse).ToArray();
        bool final = seeds.SequenceEqual(new[] { 201, 202, 203, 204, 205 });
        if (!final && !seeds.SequenceEqual(new[] { 100 })) throw new ArgumentException("Registered seeds only");
        string output = Path.GetFullPath(args.Value("--output", null) ?? throw new ArgumentException("--output required"));
        string directory = Path.GetDirectoryName(output)!;
        if (Directory.Exists(directory)) throw new IOException("Use a fresh output directory");
        Directory.CreateDirectory(directory);
        string a1 = Path.GetFullPath("artifacts/recovery/a1");
        var checksums = JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(Path.Combine(a1, "snapshot-checksums.json")))!;
        using var recorded = JsonDocument.Parse(File.ReadAllText(Path.Combine(a1, final ? "final.json" : "dev.json")));
        var results = new List<Seed>(); bool pass = true;
        foreach (int seed in seeds)
        {
            var saved = recorded.RootElement.GetProperty("Results").EnumerateArray().Single(s => s.GetProperty("Value").GetInt32() == seed);
            var learned = saved.GetProperty("Arms").EnumerateArray().Single(a => a.GetProperty("Name").GetString() == "learned");
            var queries = learned.GetProperty("Queries").EnumerateArray().Select(q => new Frozen(q.GetProperty("Chain").GetInt32(),
                q.GetProperty("Start").GetInt32(), q.GetProperty("Hops").GetInt32(), q.GetProperty("Scores").EnumerateArray().Select(v => v.GetDouble()).ToArray())).ToArray();
            if (queries.Length != 256 || queries.Any(q => q.Scores.Length != 32 || q.Hops < 1 || q.Hops > 4))
                throw new InvalidDataException("Frozen query contract");
            string relative = $"seed-{seed}/learned-relay-recall.bin", snapshot = Path.Combine(a1, relative);
            string snapshotHash = Hash(snapshot);
            if (!snapshotHash.Equals(checksums[relative], StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("A1 snapshot hash changed");
            string codesPath = Path.Combine(a1, $"seed-{seed}/learned-evaluation-codes.json");
            var codes = JsonSerializer.Deserialize<int[][][]>(File.ReadAllText(codesPath))!.Select(c => c.Select(d => new SparseCode(d)).ToArray()).ToArray();
            uint limit = (uint)saved.GetProperty("Config").GetProperty("BaselineNeuronCount").GetInt32();
            string seedDir = Path.Combine(directory, $"seed-{seed}"); Directory.CreateDirectory(seedDir);
            string modelDir = Path.Combine(seedDir, "records");
            using var resident = new ResidentRelayRecords(limit);
            (Config Config, int Count) imported;
            using (var writer = new DiskRelayRecords(modelDir, limit, DiskRelayRecords.FixedCharge + 8 * DiskRelayRecords.SlotCharge))
                imported = Import(snapshot, resident, writer);
            var cfg = imported.Config;
            var members = codes.Select(c => c.Select(code => AssemblyRelay.Members(code, cfg.BaselineNeuronCount)).ToArray()).ToArray();
            bool originalExact = true, residentExact = true;
            var newResident = new StoredRelayRecall(resident, cfg.ActivationWidth, 4, ScratchBudget);
            var old = TravelTimeReview.Load(snapshot);
            using (old.scope)
            {
                var original = new AssemblyRelay(old.cfg, old.scope);
                foreach (var q in queries)
                {
                    var originalReadout = original.Run(codes[q.Chain][q.Start], q.Hops);
                    double[] scores = members.Select(c => c[q.Start + q.Hops].Sum(id => originalReadout.Delivered.GetValueOrDefault(id))).ToArray();
                    originalExact &= scores.SequenceEqual(q.Scores);
                    newResident.Run(members[q.Chain][q.Start], q.Hops);
                    residentExact &= Scores(newResident, members, q).SequenceEqual(q.Scores);
                    if (!Steps(newResident).SequenceEqual(originalReadout.Steps)) throw new InvalidDataException("Resident step trace changed");
                }
            }
            var before = HashStore(modelDir); var cells = new List<Cell>();
            foreach (int slots in new[] { 1, 8 }) foreach (bool reverse in new[] { false, true })
            {
                using var disk = new DiskRelayRecords(modelDir, limit, DiskRelayRecords.FixedCharge + slots * DiskRelayRecords.SlotCharge, existing: true);
                var runtime = new StoredRelayRecall(disk, cfg.ActivationWidth, 4, ScratchBudget); var rows = new List<Query>();
                foreach (var q in reverse ? queries.Reverse() : queries)
                {
                    long hits = disk.Hits, loads = disk.Misses, evictions = disk.Evictions, read = disk.BytesRead;
                    runtime.Run(members[q.Chain][q.Start], q.Hops); var scores = Scores(runtime, members, q);
                    var row = new Query(q.Chain, q.Start, q.Hops, scores, RecoveryLearning.ScoreRank(scores, q.Chain),
                        scores.SequenceEqual(q.Scores), Close(scores, q.Scores), Ranking(scores, q.Scores),
                        disk.Hits - hits, disk.Misses - loads, disk.Evictions - evictions, disk.BytesRead - read, runtime.DeliveredCount, Steps(runtime));
                    rows.Add(row); pass &= row.WithinTolerance && row.SameRanking;
                }
                cells.Add(new(slots, reverse ? "reverse" : "forward", disk.ReservedBytes, runtime.ReservedBytes,
                    disk.Hits, disk.Misses, disk.Evictions, disk.BytesRead, disk.BytesWritten, runtime.Truncations, rows.ToArray()));
                pass &= disk.Evictions > 0 && disk.Misses > 0 && disk.BytesWritten == 0;
                if (seed == seeds[0] && slots == 1 && !reverse)
                {
                    var trace = new DiskRelayRecords.Access[4096]; int count = 0;
                    disk.ClearCache(); disk.ObserveRead = e =>
                    { if (count == trace.Length) throw new InvalidOperationException("Trace budget exceeded"); trace[count++] = e; };
                    var q = queries.First(q => q.Hops == 4);
                    runtime.Run(members[q.Chain][q.Start], q.Hops); disk.ObserveRead = null;
                    File.WriteAllText(Path.Combine(seedDir, "four-hop-trace.json"), JsonSerializer.Serialize(new {
                        q.Chain, q.Start, q.Hops, Roots = members[q.Chain][q.Start],
                        ExpectedEndpoint = members[q.Chain][q.Start + q.Hops], Steps = Steps(runtime),
                        Scores = Scores(runtime, members, q), EventCapacity = trace.Length, EventCount = count,
                        Events = trace.Take(count).ToArray() }, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            var after = HashStore(modelDir); bool immutable = before.All(kv => after[kv.Key] == kv.Value) && Hash(snapshot) == snapshotHash;
            pass &= originalExact && residentExact && immutable;
            results.Add(new(seed, snapshotHash, Hash(codesPath), imported.Count, originalExact, residentExact, immutable, after, cells.ToArray()));
            Console.WriteLine($"seed={seed} originalExact={originalExact} residentExact={residentExact} immutable={immutable} pagedQueries={cells.Sum(c => c.Queries.Length)} exact={cells.All(c => c.Queries.All(q => q.Exact))} loads={cells.Sum(c => c.Loads)} evictions={cells.Sum(c => c.Evictions)}");
        }
        string verdict = pass ? (final ? "R3_PASS" : "R3_DEVELOPMENT_PASS") : "R3_FAIL";
        File.WriteAllText(output, JsonSerializer.Serialize(new { Verdict = verdict, Runtime = "StoredRelayRecall", ScratchBudgetBytes = ScratchBudget,
            ComparisonAbsTolerance = 1e-6, ComparisonRelTolerance = 1e-5, Results = results }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(verdict); return pass ? 0 : 1;
    }
}
