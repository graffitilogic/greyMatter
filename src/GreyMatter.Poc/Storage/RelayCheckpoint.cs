using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace GreyMatter.Poc.Storage;

/// <summary>Numeric, versioned, recall + learning continuation metadata; frozen-code input model.</summary>
public enum RelayLearningPolicy { Global = 0, SourceLocal = 1, NoDecayDiagnostic = 2 }

public sealed record RelayTrainingState(long Updates, long Episodes, long Observations, uint[] Previous, RelayLearningPolicy Policy = RelayLearningPolicy.Global)
{
    public static RelayTrainingState Empty => new(0, 0, 0, Array.Empty<uint>());
}

public static class RelayCheckpoint
{
    // v1: model 1 = protected AssemblyRelay, input 1 = caller-supplied frozen numeric code.
    public const int StateBytes = 128, CopyBufferBytes = 4096;
    public enum Stage { DuringGenerationCopy, GenerationFlushed, BeforeManifestReplace, ManifestReplaced }
    internal static byte[] State(uint limit, RelayTrainingState state, byte[] indexHash, int version = 1, ulong epoch = 0)
    {
        if (!Enum.IsDefined(state.Policy) || (version != 3 && version != 4 && state.Policy != RelayLearningPolicy.Global))
            throw new ArgumentException("Unsupported checkpoint learning policy");
        if (state.Previous.Length > 8 || state.Previous.Any(id => id >= limit) ||
            state.Updates < 0 || state.Episodes < 0 || state.Observations < 0)
            throw new ArgumentException("Invalid learning continuation");
        var b = new byte[StateBytes];
        BinaryPrimitives.WriteInt32LittleEndian(b, version);
        if (version == 3 || version == 4) BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(112), (int)state.Policy);
        if (version == 2) BinaryPrimitives.WriteUInt64LittleEndian(b.AsSpan(112), epoch);
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(4), 1);
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(8), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(12), limit);
        BinaryPrimitives.WriteInt64LittleEndian(b.AsSpan(16), state.Updates);
        BinaryPrimitives.WriteInt64LittleEndian(b.AsSpan(24), state.Episodes);
        BinaryPrimitives.WriteInt64LittleEndian(b.AsSpan(32), state.Observations);
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(40), state.Previous.Length);
        for (int i = 0; i < state.Previous.Length; i++) BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(44 + 4 * i), state.Previous[i]);
        indexHash.CopyTo(b, 80);
        return b;
    }
    internal static byte[] HashFile(string path)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1);
        Span<byte> buffer = stackalloc byte[CopyBufferBytes]; int n;
        while ((n = file.Read(buffer)) != 0) hash.AppendData(buffer[..n]);
        return hash.GetHashAndReset();
    }
    internal static byte[] SmallFile(string path, int length)
    {
        using var file = File.OpenRead(path);
        if (file.Length != length) throw new InvalidDataException("Invalid manifest/metadata size");
        var b = new byte[length]; file.ReadExactly(b); return b;
    }
    internal static void DurableWrite(string path, byte[] bytes)
    {
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1);
        file.Write(bytes); file.Flush(true);
    }
    [DllImport("libc", SetLastError = true)] private static extern int open(string path, int flags);
    [DllImport("libc", SetLastError = true)] private static extern int fsync(int fd);
    [DllImport("libc")] private static extern int close(int fd);
    internal static void SyncDirectory(string directory)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("Durable checkpoint publication requires POSIX directory fsync");
        int fd = open(directory, 0);
        if (fd < 0) throw new IOException("Cannot open checkpoint directory for sync");
        try { if (fsync(fd) != 0) throw new IOException("Cannot sync checkpoint directory"); }
        finally { close(fd); }
    }
    internal static string Generation(string root, ulong generation) => Path.Combine(root, $"{generation:D20}");

    // Sequential direct-offset scan: fixed 4 KiB presence buffer, one record, no list of IDs.
    private static void Copy(DiskRelayRecords source, DiskRelayRecords destination, Action<Stage>? fault = null)
    {
        source.Flush();
        using var index = new FileStream(Path.Combine(source.DirectoryPath, "presence.bin"), FileMode.Open,
            FileAccess.Read, FileShare.ReadWrite, 1);
        Span<byte> markers = stackalloc byte[CopyBufferBytes];
        Span<byte> record = stackalloc byte[RelayRecord.Bytes];
        uint first = 0;
        while (first < source.IdLimit)
        {
            int n = (int)Math.Min((uint)markers.Length, source.IdLimit - first);
            index.ReadExactly(markers[..n]);
            for (int j = 0; j < n; j++)
            {
                if (markers[j] == 0) continue;
                if (markers[j] != 1 || !source.Read(first + (uint)j, record)) throw new InvalidDataException("Presence mismatch");
                destination.Write(first + (uint)j, record);
                fault?.Invoke(Stage.DuringGenerationCopy);
            }
            first += (uint)n;
        }
        destination.Flush();
    }
    public static void Publish(DiskRelayRecords source, string root, RelayTrainingState state, Action<Stage>? fault = null)
    {
        int version = state.Policy == RelayLearningPolicy.Global ? 1 : 3;
        _ = State(source.IdLimit, state, new byte[32], version);
        Directory.CreateDirectory(root);
        string manifest = Path.Combine(root, "manifest.bin");
        ulong generation = File.Exists(manifest) ? BinaryPrimitives.ReadUInt64LittleEndian(SmallFile(manifest, 40)) + 1 : 1;
        while (Directory.Exists(Generation(root, generation))) generation++;
        string directory = Generation(root, generation);
        using (var target = new DiskRelayRecords(directory, source.IdLimit, DiskRelayRecords.FixedCharge + DiskRelayRecords.SlotCharge))
            Copy(source, target, fault);
        byte[] metadata = State(source.IdLimit, state, HashFile(Path.Combine(directory, "presence.bin")), version);
        DurableWrite(Path.Combine(directory, "state.bin"), metadata);
        SyncDirectory(directory); SyncDirectory(root);
        fault?.Invoke(Stage.GenerationFlushed);
        var pointer = new byte[40]; BinaryPrimitives.WriteUInt64LittleEndian(pointer, generation);
        SHA256.HashData(metadata, pointer.AsSpan(8));
        string pending = Path.Combine(root, $"{generation:D20}.pending");
        DurableWrite(pending, pointer);
        fault?.Invoke(Stage.BeforeManifestReplace);
        File.Move(pending, manifest, true);
        SyncDirectory(root);
        fault?.Invoke(Stage.ManifestReplaced);
    }
    public static (DiskRelayRecords Store, RelayTrainingState State) OpenReadOnly(string root, long budgetBytes)
    {
        byte[] pointer = SmallFile(Path.Combine(root, "manifest.bin"), 40);
        string directory = Generation(root, BinaryPrimitives.ReadUInt64LittleEndian(pointer));
        byte[] b = SmallFile(Path.Combine(directory, "state.bin"), StateBytes);
        if (!SHA256.HashData(b).AsSpan().SequenceEqual(pointer.AsSpan(8))) throw new InvalidDataException("Metadata checksum");
        int version = BinaryPrimitives.ReadInt32LittleEndian(b);
        if ((version != 1 && version != 3) || BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(4)) != 1 ||
            BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(8)) != 1) throw new InvalidDataException("Unsupported model/version/input kind");
        if (!HashFile(Path.Combine(directory, "presence.bin")).AsSpan().SequenceEqual(b.AsSpan(80, 32)))
            throw new InvalidDataException("Missing/corrupt presence index");
        uint limit = BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(12));
        int count = BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(40));
        if (count < 0 || count > 8) throw new InvalidDataException("Previous cohort count");
        var previous = new uint[count];
        for (int i = 0; i < count; i++) previous[i] = BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(44 + 4 * i));
        var state = new RelayTrainingState(BinaryPrimitives.ReadInt64LittleEndian(b.AsSpan(16)),
            BinaryPrimitives.ReadInt64LittleEndian(b.AsSpan(24)), BinaryPrimitives.ReadInt64LittleEndian(b.AsSpan(32)), previous,
            version == 3 ? (RelayLearningPolicy)BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(112)) : RelayLearningPolicy.Global);
        _ = State(limit, state, b.AsSpan(80, 32).ToArray(), version); // validate numeric continuation too
        return (new DiskRelayRecords(directory, limit, budgetBytes, existing: true), state);
    }
    public static (DiskRelayRecords Store, RelayTrainingState State) Restore(string root, string workspace, long budgetBytes)
    {
        var saved = OpenReadOnly(root, DiskRelayRecords.FixedCharge + DiskRelayRecords.SlotCharge);
        using var source = saved.Store; var state = saved.State;
        var target = new DiskRelayRecords(workspace, source.IdLimit, budgetBytes);
        try { Copy(source, target); return (target, state); }
        catch { target.Dispose(); throw; }
    }
}
