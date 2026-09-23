using System.Buffers.Binary;
using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Storage;
using GreyMatter.Poc.Substrate;
using Xunit;

namespace GreyMatter.Poc.Tests;

/// <summary>Item 5: checksums are verified on disk load and sealed on disk write, never per cache access; corruption still fails closed.</summary>
public sealed class DiskBoundaryChecksumTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "gm-seal-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
    private static byte[] Unsealed(uint id, uint target, float weight)
    {
        var syn = new SynapseStore(1, 32); syn.Degree[0] = 1; syn.Target[0] = target; syn.Weight[0] = weight; syn.Population[0] = (byte)SynapsePopulation.CrossCue;
        var b = new byte[RelayRecord.Bytes]; RelayRecord.Encode(id, syn, b, seal: false); return b;
    }

    [Fact]
    public void PackedStoreAcceptsUnsealedWritesAndSealsExactlyAtWriteback()
    {
        string dir = Path.Combine(_root, "w");
        var unsealed = Unsealed(7, 9, .5f);
        Assert.Throws<InvalidDataException>(() => RelayRecord.Validate(7, unsealed));           // not yet sealed
        using (var store = new PackedRelayRecords(dir, 1000, PackedRelayCheckpoint.CopyBudget))
        {
            Assert.True(store.SealsAtDiskBoundary);
            store.Write(7, unsealed);                                                                  // no per-write hash
            var copy = new byte[RelayRecord.Bytes]; Assert.True(store.Read(7, copy)); Assert.Equal(unsealed, copy);
            Assert.True(store.Contains(7)); Assert.False(store.Contains(8));
            store.Flush();                                                                             // sealed in place
            Assert.True(store.Read(7, copy)); RelayRecord.Validate(7, copy);
        }
        using (var reopened = new PackedRelayRecords(dir, 1000, PackedRelayCheckpoint.CopyBudget, true))
        { var copy = new byte[RelayRecord.Bytes]; Assert.True(reopened.Read(7, copy)); RelayRecord.Validate(7, copy); }
    }

    [Fact]
    public void CorruptedRecordOnDiskStillFailsClosedOnLoad()
    {
        string dir = Path.Combine(_root, "c");
        using (var store = new PackedRelayRecords(dir, 1000, PackedRelayCheckpoint.CopyBudget)) { store.Write(7, Unsealed(7, 9, .5f)); store.Flush(); }
        string records = Path.Combine(dir, "records.bin");
        var bytes = File.ReadAllBytes(records); bytes[12] ^= 0x01; File.WriteAllBytes(records, bytes);   // flip a bit in the weight
        using var reopened = new PackedRelayRecords(dir, 1000, PackedRelayCheckpoint.CopyBudget, true);
        var copy = new byte[RelayRecord.Bytes];
        Assert.Throws<InvalidDataException>(() => reopened.Read(7, copy));
    }

    [Fact]
    public void ReferenceStoreStillValidatesEveryWriteAndLearnerRoundTripsThroughBothStores()
    {
        using var resident = new ResidentRelayRecords(1000);
        Assert.False(((IRelayRecords)resident).SealsAtDiskBoundary);
        Assert.Throws<InvalidDataException>(() => resident.Write(7, Unsealed(7, 9, .5f)));
        // Same learning through the sealing store and the reference store produces identical sealed records.
        using var packed = new PackedRelayRecords(Path.Combine(_root, "l"), 1000, PackedRelayCheckpoint.CopyBudget);
        foreach (var (store, name) in new (IRelayRecords, string)[] { (resident, "r"), (packed, "p") })
        {
            var learner = new StoredRelayLearning(store) { CountBaseline = true };
            for (int i = 0; i < 40; i++) { learner.ObserveMembers(new uint[] { 1 }); learner.ObserveMembers(new uint[] { 2 + (uint)(i % 3) }); learner.EndSequence(); }
        }
        packed.Flush();
        var a = new byte[RelayRecord.Bytes]; var b = new byte[RelayRecord.Bytes];
        foreach (uint id in new uint[] { 1, 2, 3, 4 }) { Assert.True(resident.Read(id, a)); Assert.True(packed.Read(id, b)); Assert.Equal(a, b); RelayRecord.Validate(id, b); }
    }
}
