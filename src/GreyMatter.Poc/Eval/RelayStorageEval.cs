using System.Security.Cryptography;
using System.Text.Json;
using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Storage;

namespace GreyMatter.Poc.Eval;

/// <summary>R2 storage fixture. No association or large-capacity inference claim.</summary>
public static class RelayStorageEval
{
    private const long Budget = DiskRelayRecords.FixedCharge + 8 * DiskRelayRecords.SlotCharge;
    private static void First(StoredRelayLearning learner, int episode, int records) =>
        learner.ObserveMembers(new[] { (uint)(episode % records) });
    private static void Last(StoredRelayLearning learner, int episode, int records)
    {
        learner.ObserveMembers(new[] { (uint)(records + (episode % records + 17 * (episode / records % 3)) % records) });
        learner.EndSequence();
    }
    private static string Digest(IRelayRecords store, out int count)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> record = stackalloc byte[RelayRecord.Bytes]; count = 0;
        for (uint id = 0; id < store.IdLimit; id++) if (store.Read(id, record)) { hash.AppendData(record); count++; }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
    public static int Run(Args args)
    {
        int records = int.Parse(args.Value("--records", "128")!);
        if (records != 128 && records != 1024) throw new ArgumentException("Registered source counts: 128 or 1024");
        string output = Path.GetFullPath(args.Value("--output", null) ?? throw new ArgumentException("--output required"));
        string directory = Path.GetDirectoryName(output)!;
        if (Directory.Exists(directory)) throw new IOException("Use a fresh output directory");
        Directory.CreateDirectory(directory);
        string? checkpoint = args.Value("--checkpoint", null);
        int total = 6 * records, split = 3 * records + records / 2;
        uint limit = (uint)(2 * records + 16);
        using var reference = new ResidentRelayRecords(limit); var expected = new StoredRelayLearning(reference);
        for (int e = 0; e < total; e++) { First(expected, e, records); Last(expected, e, records); }
        DiskRelayRecords disk; StoredRelayLearning actual;
        if (checkpoint is null)
        {
            disk = new DiskRelayRecords(Path.Combine(directory, "working"), limit, Budget);
            actual = new StoredRelayLearning(disk);
            for (int e = 0; e < split; e++) { First(actual, e, records); Last(actual, e, records); }
            First(actual, split, records);
            RelayCheckpoint.Publish(disk, Path.Combine(directory, "checkpoint"), actual.Capture());
        }
        else
        {
            var restored = RelayCheckpoint.Restore(checkpoint, Path.Combine(directory, "working"), Budget);
            disk = restored.Store; actual = new StoredRelayLearning(disk, restored.State);
            if (disk.IdLimit != limit || restored.State.Episodes != split || restored.State.Observations != 2L * split + 1 ||
                !restored.State.Previous.SequenceEqual(new[] { (uint)(split % records) }))
                throw new InvalidDataException("Checkpoint does not match registered fixture position");
        }
        using (disk)
        {
            Last(actual, split, records);
            for (int e = split + 1; e < total; e++) { First(actual, e, records); Last(actual, e, records); }
            disk.Flush();
            string expectedHash = Digest(reference, out int expectedCount), actualHash = Digest(disk, out int actualCount);
            bool exact = expectedHash == actualHash && expectedCount == actualCount &&
                expected.Updates == actual.Updates && expected.Episodes == actual.Episodes &&
                expected.Observations == actual.Observations && actual.Capture().Previous.Length == 0;
            bool bounded = disk.ReservedBytes <= Budget && actualCount > disk.Slots && disk.Evictions > actualCount && disk.DirtyWrites > actualCount;
            var result = new { Verdict = exact && bounded ? "R2_STORAGE_PASS" : "R2_STORAGE_FAIL",
                Fixture = "numeric singleton cohorts; six passes; offset destinations 0/17/34; decay every 500 episodes",
                FreshProcessResume = checkpoint is not null, SourceRecords = records, ActualRecords = actualCount,
                AddressLimit = limit, RecordBytes = RelayRecord.Bytes, CacheBudgetBytes = Budget,
                CacheSlots = disk.Slots, disk.ReservedBytes, disk.Hits, disk.Misses, disk.Evictions,
                disk.DirtyWrites, disk.BytesRead, disk.BytesWritten, actual.Updates, actual.Episodes, actual.Observations,
                ExpectedSha256 = expectedHash, ActualSha256 = actualHash, Exact = exact, BoundedCache = bounded,
                LogicalRecordFileBytes = (long)limit * RelayRecord.Bytes, PresenceFileBytes = limit,
                PagingRecallTested = false, Encoder = "external frozen numeric input; no text encoder in this fixture" };
            File.WriteAllText(output, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine(JsonSerializer.Serialize(result));
            return exact && bounded ? 0 : 1;
        }
    }
}
