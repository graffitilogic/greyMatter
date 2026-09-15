using System.Security.Cryptography;
using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Storage;
using GreyMatter.Poc.Substrate;
using Xunit;

namespace GreyMatter.Poc.Tests;
public sealed class DeferredDecayTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "gm-deferred-" + Guid.NewGuid().ToString("N"));
    private string P(string name) => Path.Combine(_root, name);
    private const long Budget = DeferredRelayRecords.ExtraReservedBytes + DiskRelayRecords.FixedCharge + DiskRelayRecords.SlotCharge;
    public DeferredDecayTests() => Directory.CreateDirectory(_root);
    public void Dispose() => Directory.Delete(_root, true);
    private static void Same(IRelayRecords a, IRelayRecords b)
    {
        var x = new byte[RelayRecord.Bytes]; var y = new byte[RelayRecord.Bytes];
        for (uint id = 0; id < a.IdLimit; id++) { Assert.Equal(a.Read(id, x), b.Read(id, y)); Assert.Equal(x, y); }
    }
    private static void Wire(IRelayRecords r, uint id, uint[] targets, float[] weights)
    {
        var syn = new SynapseStore(1, 32); syn.Hydrate(0, targets, weights);
        var bytes = new byte[RelayRecord.Bytes]; RelayRecord.Encode(id, syn, bytes); r.Write(id, bytes);
    }
    [Fact]
    public void SkippedEpochsReproduceEveryFloatAndPruneSwapWithoutWritingOnRead()
    {
        using var eager = new ResidentRelayRecords(8); using var deferred = new DeferredRelayRecords(P("working"), 8, Budget);
        foreach (var r in new IRelayRecords[] { eager, deferred })
        {
            Wire(r, 1, new uint[] { 2, 3, 4 }, new[] { .105f, 1f, .101f });
            foreach (uint id in new uint[] { 2, 3, 4 }) Wire(r, id, Array.Empty<uint>(), Array.Empty<float>());
        }
        var a = new StoredRelayLearning(eager); var b = new StoredRelayLearning(deferred);
        for (int i = 0; i < 600; i++) { a.Decay(); b.Decay(); if (i == 1 || i == 99 || i == 229) Same(eager, deferred); }
        deferred.Flush();
        var before = Directory.GetFiles(P("working"), "*.bin", SearchOption.AllDirectories).ToDictionary(p => p, p => SHA256.HashData(File.ReadAllBytes(p)));
        Same(eager, deferred); Same(eager, deferred);
        foreach (var kv in before) Assert.Equal(kv.Value, SHA256.HashData(File.ReadAllBytes(kv.Key)));
        // New records do not inherit centuries of decay before their creation.
        Wire(eager, 5, new uint[] { 2 }, new[] { .105f }); Wire(deferred, 5, new uint[] { 2 }, new[] { .105f }); Same(eager, deferred);
    }
    [Fact]
    public void LearningDegreePressureAndMidSequenceRestartMatchEager()
    {
        using var eager = new ResidentRelayRecords(128); using var first = new DeferredRelayRecords(P("first"), 128, Budget);
        var a = new StoredRelayLearning(eager); var b = new StoredRelayLearning(first);
        for (int i = 0; i < 751; i++)
        {
            a.ObserveMembers(new uint[] { 1 }); b.ObserveMembers(new uint[] { 1 });
            a.ObserveMembers(new[] { (uint)(2 + i % 70) }); b.ObserveMembers(new[] { (uint)(2 + i % 70) }); a.EndSequence(); b.EndSequence();
        }
        a.ObserveMembers(new uint[] { 1 }); b.ObserveMembers(new uint[] { 1 });
        DeferredRelayCheckpoint.Publish(first, P("snapshot"), b.Capture());
        var restored = DeferredRelayCheckpoint.Restore(P("snapshot"), P("restored"), Budget); using var second = restored.Store;
        b = new StoredRelayLearning(second, restored.State);
        a.ObserveMembers(new uint[] { 97 }); b.ObserveMembers(new uint[] { 97 }); a.EndSequence(); b.EndSequence();
        for (int i = 0; i < 1501; i++)
        {
            a.ObserveMembers(new uint[] { 1 }); b.ObserveMembers(new uint[] { 1 });
            a.ObserveMembers(new[] { (uint)(2 + i % 70) }); b.ObserveMembers(new[] { (uint)(2 + i % 70) }); a.EndSequence(); b.EndSequence();
        }
        Same(eager, second); Assert.Equal(a.Updates, b.Updates); Assert.Equal(a.Episodes, b.Episodes);
        Assert.Equal((ulong)(b.Episodes / 500), second.Epoch);
        Assert.Throws<InvalidDataException>(() => RelayCheckpoint.Restore(P("snapshot"), P("wrong"), 10000));
    }
    [Theory]
    [InlineData(RelayCheckpoint.Stage.DuringGenerationCopy)]
    [InlineData(RelayCheckpoint.Stage.BeforeManifestReplace)]
    [InlineData(RelayCheckpoint.Stage.ManifestReplaced)]
    public void PublicationKeepsCompleteEpochAndModelTogether(RelayCheckpoint.Stage stop)
    {
        using var store = new DeferredRelayRecords(P("working"), 8, Budget);
        Wire(store, 1, new uint[] { 2 }, new[] { 1f }); Wire(store, 2, Array.Empty<uint>(), Array.Empty<float>());
        DeferredRelayCheckpoint.Publish(store, P("saved"), RelayTrainingState.Empty);
        for (int i = 0; i < 100; i++) store.AdvanceDecay();
        Assert.Throws<IOException>(() => DeferredRelayCheckpoint.Publish(store, P("saved"), RelayTrainingState.Empty,
            stage => { if (stage == stop) throw new IOException("Simulated interruption"); }));
        var loaded = DeferredRelayCheckpoint.OpenReadOnly(P("saved"), Budget); using var read = loaded.Store;
        Assert.Equal(stop == RelayCheckpoint.Stage.ManifestReplaced ? 100ul : 0ul, read.Epoch);
        Assert.Throws<InvalidOperationException>(() => read.AdvanceDecay());
        var r = new StoredRelayRecall(read, 8, 4, 100000); r.Run(new uint[] { 1 }, 1); Assert.Equal(1, r.Value(2));
    }
    [Theory]
    [InlineData("epochs.bin")]
    [InlineData("format.bin")]
    public void CorruptEpochsNeverBecomeFreshBaseline(string file)
    {
        using (var store = new DeferredRelayRecords(P("working"), 8, Budget))
        { Wire(store, 1, new uint[] { 2 }, new[] { 1f }); store.Flush(); }
        using (var f = new FileStream(P("working/" + file), FileMode.Open, FileAccess.Write))
        { f.Position = file == "epochs.bin" ? DeferredRelayRecords.EpochRecordBytes : 8; f.WriteByte(255); }
        if (file == "format.bin") Assert.Throws<InvalidDataException>(() => new DeferredRelayRecords(P("working"), 8, Budget, true));
        else
        {
            using var read = new DeferredRelayRecords(P("working"), 8, Budget, true);
            Assert.Throws<InvalidDataException>(() => read.Read(1, new byte[RelayRecord.Bytes]));
        }
    }
}
