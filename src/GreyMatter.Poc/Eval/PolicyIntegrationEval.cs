using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Storage;
using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Eval;

/// <summary>Separate-process source-local training/continuation/recall workers.</summary>
public static class PolicyIntegrationEval
{
    private const long Budget = 256L * 1024 * 1024 - 4L * 1024 * 1024;
    private const long QueryBudget = DiskRelayRecords.FixedCharge + 8 * DiskRelayRecords.SlotCharge;
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    public sealed record Probe(int Chain, int Hops, int Correct, double[] Scores, double Top1, double Milliseconds);
    public sealed record Census(long Records, long Edges, long PayloadBytes, string Hash);
    public static int Run(Args args)
    {
        string action = args.Value("--action", "resident")!;
        if (!new[] { "resident", "prepare", "resume", "query" }.Contains(action)) throw new ArgumentException("Action");
        int seed = int.Parse(args.Value("--seed", "100")!);
        if (seed != 100 && seed != 201) throw new ArgumentException("Registered seeds100/201");
        int chains = seed == 100 ? 32 : 8192;
        string root = Path.GetFullPath(args.Value("--workspace-root", null) ?? throw new ArgumentException("--workspace-root required"));
        string output = Path.GetFullPath(args.Value("--output", null) ?? throw new ArgumentException("--output required"));
        if (File.Exists(output)) throw new IOException("Refusing result overwrite");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        string inputHash = InputHash(seed, chains);
        if (action == "resident")
        {
            using var records = new ResidentRelayRecords(CapacityEval.Space);
            var learner = new StoredRelayLearning(records) { SourceLocalForgetting = true };
            Train(records, learner, seed, chains, false);
            var model = Inspect(records); var probes = Query(records, seed, chains);
            Save(new { Action = action, Seed = seed, Chains = chains, InputHash = inputHash, State = learner.Capture(), Model = model,
                Probes = probes, Immutable = model == Inspect(records), Seconds = watch.Elapsed.TotalSeconds });
        }
        else if (action == "prepare")
        {
            using var records = new DiskRelayRecords(Path.Combine(root, "working"), CapacityEval.Space, Budget);
            var learner = new StoredRelayLearning(records) { SourceLocalForgetting = true };
            Train(records, learner, seed, chains, true);
            RelayCheckpoint.Publish(records, Path.Combine(root, "midpoint"), learner.Capture());
            Save(new { Action = action, Seed = seed, Chains = chains, InputHash = inputHash, State = learner.Capture(),
                Model = Inspect(records), records.Evictions, records.BytesRead, records.BytesWritten, Seconds = watch.Elapsed.TotalSeconds });
        }
        else if (action == "resume")
        {
            var saved = RelayCheckpoint.Restore(Path.Combine(root, "midpoint"), Path.Combine(root, "resumed"), Budget);
            using var records = saved.Store;
            if (saved.State.Policy != RelayLearningPolicy.SourceLocal || saved.State.Episodes != chains * 4 * 16 / 2 || saved.State.Previous.Length == 0)
                throw new InvalidDataException("Wrong continuation for registered worker");
            var learner = new StoredRelayLearning(records, saved.State); // Deliberately NO policy flags.
            Train(records, learner, seed, chains, false);
            RelayCheckpoint.Publish(records, Path.Combine(root, "complete"), learner.Capture());
            var model = Inspect(records); var probes = Query(records, seed, chains);
            Save(new { Action = action, Seed = seed, Chains = chains, InputHash = inputHash, State = learner.Capture(), Model = model,
                Probes = probes, Immutable = model == Inspect(records), records.Evictions, records.BytesRead, records.BytesWritten, Seconds = watch.Elapsed.TotalSeconds });
        }
        else
        {
            string snapshot = Path.Combine(root, "complete"); string before = PhysicalHash(snapshot);
            var saved = RelayCheckpoint.OpenReadOnly(snapshot, QueryBudget); using var records = saved.Store;
            if (saved.State.Policy != RelayLearningPolicy.SourceLocal) throw new InvalidDataException("Wrong saved policy");
            var model = Inspect(records); records.ClearCache(); long writes = records.BytesWritten, reads = records.BytesRead, evictions = records.Evictions;
            Probe[] probes = Query(records, seed, chains); long queryWrites = records.BytesWritten - writes, queryReads = records.BytesRead - reads;
            bool immutable = before == PhysicalHash(snapshot);
            Save(new { Action = action, Seed = seed, Chains = chains, InputHash = inputHash, State = saved.State, Model = model,
                Probes = probes, Immutable = immutable, QueryWrites = queryWrites, QueryReads = queryReads, QueryEvictions = records.Evictions - evictions, records.Evictions,
                records.ReservedBytes, PhysicalHash = before, Seconds = watch.Elapsed.TotalSeconds });
            if (!immutable || queryWrites != 0) return 1;
        }
        return 0;
        void Save(object value) { File.WriteAllText(output, JsonSerializer.Serialize(value, Json)); Console.WriteLine($"{action} seed{seed} complete: {output}"); }
    }
    private static (int From, int To) Pair(int seed, int chains, int episode)
    {
        int mask = chains * 4 * 16 - 1;
        uint odd = (uint)Rng.Mix((uint)seed) | 1u, offset = (uint)Rng.Mix((uint)seed ^ 0xb519u);
        int relation = (int)(unchecked((uint)episode * odd + offset) & (uint)mask) % (chains * 4);
        int from = relation / 4 * 5 + relation % 4; return (from, from + 1);
    }
    internal static string InputHash(int seed, int chains)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256); Span<byte> bytes = stackalloc byte[8];
        for (int i = 0; i < chains * 4 * 16; i++)
        { var p = Pair(seed, chains, i); BinaryPrimitives.WriteInt32LittleEndian(bytes, p.From); BinaryPrimitives.WriteInt32LittleEndian(bytes[4..], p.To); hash.AppendData(bytes); }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
    internal static void Train(IRelayRecords records, StoredRelayLearning learner, int seed, int chains, bool midpoint)
    {
        var input = new CapacityEval.Input(seed); int end = chains * 4 * 16 / (midpoint ? 2 : 1);
        bool pending = learner.Capture().Previous.Length != 0;
        for (int i = checked((int)learner.Episodes); i < end; i++)
        {
            var p = Pair(seed, chains, i);
            if (!pending) learner.ObserveMembers(input.Members(p.From));
            learner.ObserveMembers(input.Members(p.To)); learner.EndSequence(); pending = false;
        }
        if (midpoint) learner.ObserveMembers(input.Members(Pair(seed, chains, end).From));
        records.Flush();
    }
    internal static Probe[] Query(IRelayRecords records, int seed, int chains)
    {
        var input = new CapacityEval.Input(seed); var runtime = new StoredRelayRecall(records, 256, 4, 4 * 1024 * 1024);
        var rows = new List<Probe>();
        for (int q = 0; q < Math.Min(64, chains); q++) foreach (int hops in new[] { 1, 2 + q % 3 })
        {
            int chain = (seed + 127 * q) % chains, correct = q % 32;
            var time = System.Diagnostics.Stopwatch.StartNew(); runtime.Run(input.Members(chain * 5), hops);
            var scores = new double[32];
            for (int c = 0; c < 32; c++)
            {
                int candidate = (chain + (c - correct + 32) % 32) % chains;
                foreach (uint id in input.Members(candidate * 5 + hops)) scores[c] += runtime.Value(id);
            }
            rows.Add(new(chain, hops, correct, scores, RecoveryLearning.ScoreRank(scores, correct).Top1, time.Elapsed.TotalMilliseconds));
        }
        return rows.ToArray();
    }
    private static Census Inspect(IRelayRecords records)
    {
        long count = 0, edges = 0; var bytes = new byte[RelayRecord.Bytes];
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        records.VisitPresent(id => { records.Read(id, bytes); count++; edges += BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(4)); hash.AppendData(bytes); });
        return new(count, edges, count * RelayRecord.Bytes, Convert.ToHexString(hash.GetHashAndReset()));
    }
    internal static string PhysicalHash(string root)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (string path in Directory.GetFiles(root, "*.bin", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal))
            hash.AppendData(RelayCheckpoint.HashFile(path));
        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
