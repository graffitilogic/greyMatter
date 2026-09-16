using System.Buffers.Binary;
using System.Security.Cryptography;

namespace GreyMatter.Poc.Storage;

/// <summary>Version-2 publication; old generations remain untouched. Single writer.</summary>
public static class DeferredRelayCheckpoint
{
    public const long CopyBudget = DeferredRelayRecords.ExtraReservedBytes + DiskRelayRecords.FixedCharge + DiskRelayRecords.SlotCharge;
    private static void Copy(DeferredRelayRecords source, DeferredRelayRecords target, Action<RelayCheckpoint.Stage>? fault = null)
    {
        var record = new byte[RelayRecord.Bytes];
        source.VisitPresent(id =>
        {
            if (!source.Read(id, record)) throw new InvalidDataException("Missing source record");
            target.Write(id, record); // aged canonical view stamped at the same global epoch
            fault?.Invoke(RelayCheckpoint.Stage.DuringGenerationCopy);
        });
        target.Flush();
    }
    public static void Publish(DeferredRelayRecords source, string root, RelayTrainingState state, Action<RelayCheckpoint.Stage>? fault = null)
    {
        _ = RelayCheckpoint.State(source.IdLimit, state, new byte[32], 2, source.Epoch);
        source.Flush(); Directory.CreateDirectory(root);
        string manifest = Path.Combine(root, "manifest.bin");
        ulong generation = File.Exists(manifest) ? BinaryPrimitives.ReadUInt64LittleEndian(RelayCheckpoint.SmallFile(manifest, 40)) + 1 : 1;
        while (Directory.Exists(RelayCheckpoint.Generation(root, generation))) generation++;
        string directory = RelayCheckpoint.Generation(root, generation);
        using (var target = new DeferredRelayRecords(directory, source.IdLimit, CopyBudget, false, source.Epoch)) Copy(source, target, fault);
        byte[] metadata = RelayCheckpoint.State(source.IdLimit, state,
            RelayCheckpoint.HashFile(Path.Combine(directory, "base", "presence.bin")), 2, source.Epoch);
        RelayCheckpoint.DurableWrite(Path.Combine(directory, "state.bin"), metadata);
        RelayCheckpoint.SyncDirectory(Path.Combine(directory, "base")); RelayCheckpoint.SyncDirectory(directory); RelayCheckpoint.SyncDirectory(root);
        fault?.Invoke(RelayCheckpoint.Stage.GenerationFlushed);
        var pointer = new byte[40]; BinaryPrimitives.WriteUInt64LittleEndian(pointer, generation); SHA256.HashData(metadata, pointer.AsSpan(8));
        string pending = Path.Combine(root, $"{generation:D20}.pending"); RelayCheckpoint.DurableWrite(pending, pointer);
        fault?.Invoke(RelayCheckpoint.Stage.BeforeManifestReplace);
        File.Move(pending, manifest, true); RelayCheckpoint.SyncDirectory(root); fault?.Invoke(RelayCheckpoint.Stage.ManifestReplaced);
    }
    public static (DeferredRelayRecords Store, RelayTrainingState State) OpenReadOnly(string root, long budgetBytes)
    {
        byte[] pointer = RelayCheckpoint.SmallFile(Path.Combine(root, "manifest.bin"), 40);
        string directory = RelayCheckpoint.Generation(root, BinaryPrimitives.ReadUInt64LittleEndian(pointer));
        byte[] b = RelayCheckpoint.SmallFile(Path.Combine(directory, "state.bin"), RelayCheckpoint.StateBytes);
        if (!SHA256.HashData(b).AsSpan().SequenceEqual(pointer.AsSpan(8)) || BinaryPrimitives.ReadInt32LittleEndian(b) != 2 ||
            BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(4)) != 1 || BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(8)) != 1)
            throw new InvalidDataException("Version-2 snapshot metadata");
        if (!RelayCheckpoint.HashFile(Path.Combine(directory, "base", "presence.bin")).AsSpan().SequenceEqual(b.AsSpan(80, 32)))
            throw new InvalidDataException("Version-2 presence checksum");
        uint limit = BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(12)); ulong epoch = BinaryPrimitives.ReadUInt64LittleEndian(b.AsSpan(112));
        int count = BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(40));
        if (count < 0 || count > 8) throw new InvalidDataException("Previous cohort");
        uint[] previous = new uint[count]; for (int i = 0; i < count; i++) previous[i] = BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(44 + 4 * i));
        var state = new RelayTrainingState(BinaryPrimitives.ReadInt64LittleEndian(b.AsSpan(16)), BinaryPrimitives.ReadInt64LittleEndian(b.AsSpan(24)),
            BinaryPrimitives.ReadInt64LittleEndian(b.AsSpan(32)), previous);
        _ = RelayCheckpoint.State(limit, state, b.AsSpan(80, 32).ToArray(), 2, epoch);
        var store = new DeferredRelayRecords(directory, limit, budgetBytes, true);
        if (store.Epoch != epoch) { store.Dispose(); throw new InvalidDataException("Snapshot global epoch mismatch"); }
        return (store, state);
    }
    public static (DeferredRelayRecords Store, RelayTrainingState State) Restore(string root, string workspace, long budgetBytes)
    {
        var saved = OpenReadOnly(root, CopyBudget); using var source = saved.Store;
        var target = new DeferredRelayRecords(workspace, source.IdLimit, budgetBytes, false, source.Epoch);
        try { Copy(source, target); return (target, saved.State); }
        catch { target.Dispose(); throw; }
    }
}
