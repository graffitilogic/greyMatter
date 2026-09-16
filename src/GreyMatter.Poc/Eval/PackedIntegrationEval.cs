using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Storage;

namespace GreyMatter.Poc.Eval;

/// <summary>Paired packed/direct storage experiment, same frozen training and queries.</summary>
public static class PackedIntegrationEval
{
    private const long Budget = 256L * 1024 * 1024 - 4L * 1024 * 1024;
    private const long QueryBudget = PackedRelayRecords.FixedCharge + 8 * PackedRelayRecords.SlotCharge;
    public static int Run(Args args)
    {
        string action = args.Value("--action", "prepare")!;
        if (!new[] { "prepare", "resume", "query" }.Contains(action)) throw new ArgumentException("Action");
        int seed = int.Parse(args.Value("--seed", "100")!); if (seed != 100 && seed != 201) throw new ArgumentException("Registered seeds100/201");
        int chains = seed == 100 ? 32 : 8192;
        string root = Path.GetFullPath(args.Value("--workspace-root", null) ?? throw new ArgumentException("--workspace-root required"));
        string referenceRoot = Path.GetFullPath(args.Value("--reference-root", null) ?? throw new ArgumentException("--reference-root required"));
        string output = Path.GetFullPath(args.Value("--output", null) ?? throw new ArgumentException("--output required"));
        if (File.Exists(output)) throw new IOException("Refusing result overwrite");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        string inputHash = PolicyIntegrationEval.InputHash(seed, chains);
        if (action == "prepare")
        {
            using var store = new PackedRelayRecords(Path.Combine(root, "working"), CapacityEval.Space, Budget);
            var learner = new StoredRelayLearning(store) { SourceLocalForgetting = true };
            PolicyIntegrationEval.Train(store, learner, seed, chains, true);
            PackedRelayCheckpoint.Publish(store, Path.Combine(root, "midpoint"), learner.Capture());
            var model = Compare(store, Path.Combine(referenceRoot, "midpoint"));
            Save(new { Action = action, Seed = seed, InputHash = inputHash, State = learner.Capture(), Model = model,
                store.Rehashes, store.Evictions, store.IndexCapacity, store.ReservedBytes, store.BytesRead, store.BytesWritten,
                store.IndexBytesRead, store.IndexBytesWritten, Seconds = watch.Elapsed.TotalSeconds });
        }
        else if (action == "resume")
        {
            var saved = PackedRelayCheckpoint.Restore(Path.Combine(root, "midpoint"), Path.Combine(root, "resumed"), Budget);
            using var store = saved.Store;
            if (saved.State.Policy != RelayLearningPolicy.SourceLocal || saved.State.Episodes != chains * 4 * 16 / 2 || saved.State.Previous.Length != 8)
                throw new InvalidDataException("Wrong integration continuation");
            var learner = new StoredRelayLearning(store, saved.State);
            PolicyIntegrationEval.Train(store, learner, seed, chains, false);
            PackedRelayCheckpoint.Publish(store, Path.Combine(root, "complete"), learner.Capture());
            var model = Compare(store, Path.Combine(referenceRoot, "complete"));
            var probes = PolicyIntegrationEval.Query(store, seed, chains);
            Save(new { Action = action, Seed = seed, InputHash = inputHash, State = learner.Capture(), Model = model, Probes = probes,
                store.Rehashes, store.Evictions, store.IndexCapacity, store.ReservedBytes, store.BytesRead, store.BytesWritten,
                store.IndexBytesRead, store.IndexBytesWritten, Seconds = watch.Elapsed.TotalSeconds });
        }
        else
        {
            string snapshot = Path.Combine(root, "complete"), before = PolicyIntegrationEval.PhysicalHash(snapshot);
            var saved = PackedRelayCheckpoint.OpenReadOnly(snapshot, QueryBudget); using var store = saved.Store;
            var model = Compare(store, Path.Combine(referenceRoot, "complete")); store.ClearCache();
            long read = store.BytesRead, written = store.BytesWritten, indexRead = store.IndexBytesRead, indexWritten = store.IndexBytesWritten,
                evictions = store.Evictions, pageHits = store.IndexPageHits, pageMisses = store.IndexPageMisses;
            var probes = PolicyIntegrationEval.Query(store, seed, chains);
            long queryRead = store.BytesRead - read, queryWritten = store.BytesWritten - written, queryIndexRead = store.IndexBytesRead - indexRead,
                queryIndexWritten = store.IndexBytesWritten - indexWritten;
            bool immutable = before == PolicyIntegrationEval.PhysicalHash(snapshot);
            Save(new { Action = action, Seed = seed, InputHash = inputHash, State = saved.State, Model = model, Probes = probes,
                Immutable = immutable, QueryReads = queryRead, QueryWrites = queryWritten, QueryIndexReads = queryIndexRead, QueryIndexWrites = queryIndexWritten,
                QueryEvictions = store.Evictions - evictions, QueryIndexHits = store.IndexPageHits - pageHits, QueryIndexMisses = store.IndexPageMisses - pageMisses,
                store.ReservedBytes, PhysicalHash = before, Seconds = watch.Elapsed.TotalSeconds });
            if (!immutable || queryWritten != 0 || queryIndexWritten != 0) return 1;
        }
        return 0;
        void Save(object value) { File.WriteAllText(output, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true })); Console.WriteLine($"packed {action} seed{seed} complete"); }
    }
    private static PolicyIntegrationEval.Census Compare(PackedRelayRecords packed, string referenceRoot)
    {
        packed.Flush(); var saved = RelayCheckpoint.OpenReadOnly(referenceRoot, DiskRelayRecords.FixedCharge + 8 * DiskRelayRecords.SlotCharge);
        using var reference = saved.Store; long count = 0, edges = 0;
        byte[] expected = new byte[RelayRecord.Bytes], actual = new byte[RelayRecord.Bytes];
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        // Reference enumerates sorted virtual IDs; packed birth order need not. No full ID list in RAM.
        reference.VisitPresent(id =>
        {
            reference.Read(id, expected);
            if (!packed.Read(id, actual) || !actual.AsSpan().SequenceEqual(expected)) throw new InvalidDataException($"Packed learned record differs at {id}");
            count++; edges += BinaryPrimitives.ReadInt32LittleEndian(actual.AsSpan(4)); hash.AppendData(actual);
        });
        if (packed.Count != count) throw new InvalidDataException("Packed record population differs");
        return new(count, edges, count * RelayRecord.Bytes, Convert.ToHexString(hash.GetHashAndReset()));
    }
}
