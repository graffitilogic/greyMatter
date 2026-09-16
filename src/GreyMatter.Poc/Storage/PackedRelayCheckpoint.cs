using System.Buffers.Binary;
using System.Security.Cryptography;

namespace GreyMatter.Poc.Storage;

/// <summary>Version4 packed publication. Metadata binds format/index; records self-checksum.</summary>
public static class PackedRelayCheckpoint
{
    public const long CopyBudget = PackedRelayRecords.FixedCharge + PackedRelayRecords.SlotCharge;
    internal static byte[] IndexHash(string directory)
    {
        Span<byte> hashes = stackalloc byte[64];
        RelayCheckpoint.HashFile(Path.Combine(directory, "format.bin")).CopyTo(hashes);
        RelayCheckpoint.HashFile(Path.Combine(directory, "index.bin")).CopyTo(hashes[32..]);
        return SHA256.HashData(hashes);
    }
    private static void Copy(PackedRelayRecords source, PackedRelayRecords target, Action<RelayCheckpoint.Stage>? fault = null)
    {
        var record = new byte[RelayRecord.Bytes];
        source.VisitPresent(id =>
        {
            if (!source.Read(id, record)) throw new InvalidDataException("Missing packed source");
            target.Write(id, record); fault?.Invoke(RelayCheckpoint.Stage.DuringGenerationCopy);
        });
        target.Flush();
    }
    public static void Publish(PackedRelayRecords source, string root, RelayTrainingState state, Action<RelayCheckpoint.Stage>? fault = null)
    {
        _ = RelayCheckpoint.State(source.IdLimit, state, new byte[32], 4);
        source.Flush(); Directory.CreateDirectory(root); string manifest = Path.Combine(root, "manifest.bin");
        ulong generation = File.Exists(manifest) ? BinaryPrimitives.ReadUInt64LittleEndian(RelayCheckpoint.SmallFile(manifest, 40)) + 1 : 1;
        while (Directory.Exists(RelayCheckpoint.Generation(root, generation))) generation++;
        string directory = RelayCheckpoint.Generation(root, generation);
        using (var target = new PackedRelayRecords(directory, source.IdLimit, CopyBudget)) Copy(source, target, fault);
        byte[] metadata = RelayCheckpoint.State(source.IdLimit, state, IndexHash(directory), 4);
        RelayCheckpoint.DurableWrite(Path.Combine(directory, "state.bin"), metadata);
        RelayCheckpoint.SyncDirectory(directory); RelayCheckpoint.SyncDirectory(root); fault?.Invoke(RelayCheckpoint.Stage.GenerationFlushed);
        var pointer = new byte[40]; BinaryPrimitives.WriteUInt64LittleEndian(pointer, generation); SHA256.HashData(metadata, pointer.AsSpan(8));
        string pending = Path.Combine(root, $"{generation:D20}.pending"); RelayCheckpoint.DurableWrite(pending, pointer);
        fault?.Invoke(RelayCheckpoint.Stage.BeforeManifestReplace); File.Move(pending, manifest, true);
        RelayCheckpoint.SyncDirectory(root); fault?.Invoke(RelayCheckpoint.Stage.ManifestReplaced);
    }
    public static (PackedRelayRecords Store, RelayTrainingState State) OpenReadOnly(string root, long budget)
    {
        byte[] pointer = RelayCheckpoint.SmallFile(Path.Combine(root, "manifest.bin"), 40);
        string directory = RelayCheckpoint.Generation(root, BinaryPrimitives.ReadUInt64LittleEndian(pointer));
        byte[] metadata = RelayCheckpoint.SmallFile(Path.Combine(directory, "state.bin"), RelayCheckpoint.StateBytes);
        if (!SHA256.HashData(metadata).AsSpan().SequenceEqual(pointer.AsSpan(8)) || BinaryPrimitives.ReadInt32LittleEndian(metadata) != 4 ||
            BinaryPrimitives.ReadInt32LittleEndian(metadata.AsSpan(4)) != 1 || BinaryPrimitives.ReadInt32LittleEndian(metadata.AsSpan(8)) != 1)
            throw new InvalidDataException("Packed snapshot metadata/version");
        if (!IndexHash(directory).AsSpan().SequenceEqual(metadata.AsSpan(80, 32))) throw new InvalidDataException("Packed format/index checksum");
        uint limit = BinaryPrimitives.ReadUInt32LittleEndian(metadata.AsSpan(12)); int count = BinaryPrimitives.ReadInt32LittleEndian(metadata.AsSpan(40));
        if (count < 0 || count > 8) throw new InvalidDataException("Previous cohort count");
        uint[] previous = new uint[count]; for (int i = 0; i < count; i++) previous[i] = BinaryPrimitives.ReadUInt32LittleEndian(metadata.AsSpan(44 + i * 4));
        var state = new RelayTrainingState(BinaryPrimitives.ReadInt64LittleEndian(metadata.AsSpan(16)),
            BinaryPrimitives.ReadInt64LittleEndian(metadata.AsSpan(24)), BinaryPrimitives.ReadInt64LittleEndian(metadata.AsSpan(32)), previous,
            (RelayLearningPolicy)BinaryPrimitives.ReadInt32LittleEndian(metadata.AsSpan(112)));
        _ = RelayCheckpoint.State(limit, state, metadata.AsSpan(80, 32).ToArray(), 4);
        return (new PackedRelayRecords(directory, limit, budget, true), state);
    }
    public static (PackedRelayRecords Store, RelayTrainingState State) Restore(string root, string workspace, long budget)
    {
        var saved = OpenReadOnly(root, CopyBudget); using var source = saved.Store;
        var target = new PackedRelayRecords(workspace, source.IdLimit, budget);
        try { Copy(source, target); return (target, saved.State); }
        catch { target.Dispose(); throw; }
    }
}
