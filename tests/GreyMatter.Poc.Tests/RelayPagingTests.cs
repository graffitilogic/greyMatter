using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Storage;
using GreyMatter.Poc.Substrate;
using Xunit;

namespace GreyMatter.Poc.Tests;
public sealed class RelayPagingTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "gm-r3-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
    private static void Wire(IRelayRecords store, uint id, params uint[] targets)
    {
        var syn = new SynapseStore(1, 32); syn.Hydrate(0, targets, targets.Select(_ => 1f).ToArray());
        var b = new byte[RelayRecord.Bytes]; RelayRecord.Encode(id, syn, b); store.Write(id, b);
    }
    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public void FanInCycleAndEvictedSourcesMatchResidentWithImmutableRecords(int slots)
    {
        using var resident = new ResidentRelayRecords(100);
        using var disk = new DiskRelayRecords(_root, 100, DiskRelayRecords.FixedCharge + slots * DiskRelayRecords.SlotCharge);
        foreach (var s in new IRelayRecords[] { resident, disk })
        { Wire(s, 1, 3, 2); Wire(s, 2, 4); Wire(s, 3, 4); Wire(s, 4, 1); Wire(s, 5); }
        disk.Flush(); disk.ClearCache(); long writes = disk.BytesWritten;
        var a = new StoredRelayRecall(resident, 2, 4, 100000);
        var b = new StoredRelayRecall(disk, 2, 4, 100000);
        a.Run(new uint[] { 1 }, 4); b.Run(new uint[] { 1 }, 4);
        Assert.Equal(1, b.Value(4)); Assert.Equal(1, b.Value(1));
        for (uint i = 0; i < 6; i++) Assert.Equal(a.Value(i), b.Value(i));
        for (int t = 0; t < 4; t++) Assert.Equal(a.Step(t), b.Step(t));
        Assert.Equal(writes, disk.BytesWritten); Assert.Equal(0, b.Truncations);
        if (slots == 1) Assert.True(disk.Evictions > 10);
        b.Run(new uint[] { 5 }, 4); Assert.Equal(0, b.DeliveredCount);
        b.Run(new uint[] { 1 }, 4); for (uint i = 0; i < 6; i++) Assert.Equal(a.Value(i), b.Value(i));
    }
    [Fact]
    public void WinnerTieUsesIdNotDiskOrStoredEdgeOrderAndThresholdIsUnchanged()
    {
        using var disk = new DiskRelayRecords(_root, 100, DiskRelayRecords.FixedCharge + DiskRelayRecords.SlotCharge);
        Wire(disk, 1, 3, 2); Wire(disk, 2, 4); Wire(disk, 3, 5); Wire(disk, 4); Wire(disk, 5);
        var r = new StoredRelayRecall(disk, 1, 2, 100000); r.Run(new uint[] { 1 }, 2);
        Assert.Equal(.5, r.Value(4)); Assert.Equal(0, r.Value(5)); Assert.Equal(1, r.Step(0).Winners);
        Wire(disk, 1, 2, 3, 4); r.Run(new uint[] { 1 }, 2);
        Assert.Equal(0, r.Step(1).Emitting); Assert.Equal(0, r.Value(5));
    }
    [Fact]
    public void MissingLearnedTargetFailsButUnseenCueIsEmpty()
    {
        using var resident = new ResidentRelayRecords(100); Wire(resident, 1, 2);
        var r = new StoredRelayRecall(resident, 8, 4, 100000);
        Assert.Throws<InvalidDataException>(() => r.Run(new uint[] { 1 }, 4));
        r.Run(new uint[] { 90 }, 4); Assert.Equal(0, r.DeliveredCount);
    }
    [Fact]
    public void InsufficientScratchAndExcessDepthAreRefusedExplicitly()
    {
        using var resident = new ResidentRelayRecords(100);
        Assert.Throws<ArgumentException>(() => new StoredRelayRecall(resident, 256, 4, 4096));
        var r = new StoredRelayRecall(resident, 1, 2, 100000);
        Assert.Throws<ArgumentOutOfRangeException>(() => r.Run(new uint[] { 1 }, 3));
        Assert.Throws<ArgumentException>(() => r.Run(new uint[] { 1, 1 }, 2));
    }
    [Fact]
    public void ReadTraceReportsActualVictimAndColdCacheReload()
    {
        using var disk = new DiskRelayRecords(_root, 100, DiskRelayRecords.FixedCharge + DiskRelayRecords.SlotCharge);
        Wire(disk, 1, 2); Wire(disk, 2); disk.Flush(); disk.ClearCache();
        var events = new DiskRelayRecords.Access[4]; int count = 0;
        disk.ObserveRead = e => events[count++] = e;
        var record = new byte[RelayRecord.Bytes];
        disk.Read(1, record); disk.Read(2, record); disk.Read(2, record); disk.Read(1, record);
        Assert.Null(events[0].EvictedId); Assert.Equal((uint)1, events[1].EvictedId);
        Assert.True(events[2].Hit); Assert.Equal((uint)2, events[3].EvictedId);
        Assert.Equal((uint)1, events[3].Id); Assert.True(events[3].Loaded);
    }
}
