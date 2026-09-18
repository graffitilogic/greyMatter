using System.Buffers.Binary;
using System.Security.Cryptography;
using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Storage;
using GreyMatter.Poc.Substrate;
using Xunit;

namespace GreyMatter.Poc.Tests;
public sealed class PackedRelayTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "gm-packed-" + Guid.NewGuid().ToString("N"));
    private const long Budget = PackedRelayRecords.FixedCharge + PackedRelayRecords.SlotCharge;
    private string P(string path) => Path.Combine(_root, path);
    public PackedRelayTests() => Directory.CreateDirectory(_root);
    public void Dispose() => Directory.Delete(_root, true);
    private static byte[] Record(uint id, float weight)
    {
        var syn = new SynapseStore(1, 32); syn.Hydrate(0, new uint[] { id + 1 }, new[] { weight });
        var bytes = new byte[RelayRecord.Bytes]; RelayRecord.Encode(id, syn, bytes); return bytes;
    }
    [Fact]
    public void NativeReadPolicyPreservesFrozenRecordsAndReportsResources()
    {
        if (!OperatingSystem.IsMacOS()) return;
        var before = MacFileIo.Sample(); Assert.True(before.ResidentBytes > 0);
        using (var store = new PackedRelayRecords(P("native-work"), 1024, Budget))
        {
            Assert.Throws<InvalidOperationException>(() => store.SetReadNoCache(true));
            for (uint id = 0; id < 100; id++) store.Write(id, Record(id, .2f));
            PackedRelayCheckpoint.Publish(store, P("native-snapshot"), RelayTrainingState.Empty);
        }
        string hash = GreyMatter.Poc.Eval.PolicyIntegrationEval.PhysicalHash(P("native-snapshot"));
        var saved = PackedRelayCheckpoint.OpenReadOnly(P("native-snapshot"), Budget);
        using var read = saved.Store; var bytes = new byte[RelayRecord.Bytes];
        foreach (bool enabled in new[] { true, false })
        {
            read.SetReadNoCache(enabled); read.ClearCache();
            for (uint id = 0; id < 100; id++) { Assert.True(read.Read(id, bytes)); Assert.Equal(Record(id, .2f), bytes); }
        }
        Assert.Equal(0, read.BytesWritten); Assert.Equal(0, read.IndexBytesWritten);
        Assert.Equal(hash, GreyMatter.Poc.Eval.PolicyIntegrationEval.PhysicalHash(P("native-snapshot")));
        var after = MacFileIo.Sample(); Assert.True(after.DiskReadBytes >= before.DiskReadBytes);
        Assert.True(after.DiskWrittenBytes >= before.DiskWrittenBytes);
    }
    [Fact]
    public void CollisionsGrowthZeroIdAndUpdatesUseOnlyOccupiedRecords()
    {
        using var store = new PackedRelayRecords(P("work"), uint.MaxValue, Budget);
        var bytes = new byte[RelayRecord.Bytes];
        for (int i = 0; i < 3000; i++) store.Write((uint)i * 64, Record((uint)i * 64, .2f));
        Assert.True(store.Rehashes >= 4); Assert.True(store.Evictions > 0); Assert.Equal(Budget, store.ReservedBytes);
        for (int i = 2999; i >= 0; i--) { uint id = (uint)i * 64; Assert.True(store.Read(id, bytes)); Assert.Equal(Record(id, .2f), bytes); store.Write(id, Record(id, .3f)); }
        Assert.False(store.Read(17, bytes)); Assert.All(bytes, x => Assert.Equal((byte)0, x));
        store.Flush(); Assert.Equal(3000 * RelayRecord.Bytes, new FileInfo(Path.Combine(P("work"), "records.bin")).Length);
        var ids = new HashSet<uint>(); store.VisitPresent(id => { Assert.True(ids.Add(id)); Assert.True(store.Read(id, bytes)); Assert.Equal(Record(id, .3f), bytes); });
        Assert.Equal(3000, ids.Count); Assert.Equal(8192, store.IndexCapacity);
        using var old = new DiskRelayRecords(P("empty-old"), 32, DiskRelayRecords.FixedCharge + DiskRelayRecords.SlotCharge);
        RelayCheckpoint.Publish(old, P("old"), RelayTrainingState.Empty);
        Assert.ThrowsAny<Exception>(() => PackedRelayCheckpoint.OpenReadOnly(P("old"), Budget));
    }
    [Theory]
    [InlineData(RelayLearningPolicy.SourceLocal)]
    [InlineData(RelayLearningPolicy.Global)]
    [InlineData(RelayLearningPolicy.NoDecayDiagnostic)]
    public void LearningWithDirtyEvictionAndMidSequenceRestartIsExact(RelayLearningPolicy policy)
    {
        using var reference = new ResidentRelayRecords(1024); var a = new StoredRelayLearning(reference) { SourceLocalForgetting = policy == RelayLearningPolicy.SourceLocal, DisableDecayForDiagnostic = policy == RelayLearningPolicy.NoDecayDiagnostic };
        using (var store = new PackedRelayRecords(P("work"), 1024, Budget))
        {
            var b = new StoredRelayLearning(store) { SourceLocalForgetting = policy == RelayLearningPolicy.SourceLocal, DisableDecayForDiagnostic = policy == RelayLearningPolicy.NoDecayDiagnostic };
            for (int i = 0; i < 1500; i++) foreach (var learner in new[] { a, b }) Teach(learner, (uint)(i % 250), (uint)(300 + i % 300));
            foreach (var learner in new[] { a, b }) learner.ObserveMembers(new uint[] { 700, 701 });
            PackedRelayCheckpoint.Publish(store, P("snapshot"), b.Capture());
        }
        var saved = PackedRelayCheckpoint.Restore(P("snapshot"), P("resume"), Budget); using var resumed = saved.Store;
        var c = new StoredRelayLearning(resumed, saved.State); Assert.Equal(policy, c.Policy);
        foreach (var learner in new[] { a, c }) { learner.ObserveMembers(new uint[] { 702, 703 }); learner.EndSequence(); }
        for (int i = 0; i < 1500; i++) foreach (var learner in new[] { a, c }) Teach(learner, (uint)(i % 250), (uint)(350 + i % 300));
        byte[] x = new byte[RelayRecord.Bytes], y = new byte[RelayRecord.Bytes];
        for (uint id = 0; id < 1024; id++) { Assert.Equal(reference.Read(id, x), resumed.Read(id, y)); Assert.Equal(x, y); }
        Assert.Equal(a.Updates, c.Updates); Assert.Equal(a.Episodes, c.Episodes);
        Assert.ThrowsAny<Exception>(() => RelayCheckpoint.OpenReadOnly(P("snapshot"), DiskRelayRecords.FixedCharge + DiskRelayRecords.SlotCharge));
    }
    private static void Teach(StoredRelayLearning learner, uint a, uint b)
    { learner.ObserveMembers(new[] { a }); learner.ObserveMembers(new[] { b }); learner.EndSequence(); }
    [Theory]
    [InlineData(RelayCheckpoint.Stage.DuringGenerationCopy)]
    [InlineData(RelayCheckpoint.Stage.BeforeManifestReplace)]
    [InlineData(RelayCheckpoint.Stage.ManifestReplaced)]
    public void InterruptedPublicationKeepsCompleteOldOrNewModel(RelayCheckpoint.Stage stage)
    {
        using var store = new PackedRelayRecords(P("work"), 1024, Budget);
        var learner = new StoredRelayLearning(store) { SourceLocalForgetting = true }; Teach(learner, 1, 2);
        PackedRelayCheckpoint.Publish(store, P("snapshot"), learner.Capture()); Teach(learner, 1, 3);
        Assert.Throws<IOException>(() => PackedRelayCheckpoint.Publish(store, P("snapshot"), learner.Capture(), s => { if (s == stage) throw new IOException("injected"); }));
        var saved = PackedRelayCheckpoint.OpenReadOnly(P("snapshot"), Budget); using var read = saved.Store;
        Assert.Equal(RelayLearningPolicy.SourceLocal, saved.State.Policy);
        Assert.Equal(stage == RelayCheckpoint.Stage.ManifestReplaced ? 2 : 1, saved.State.Episodes);
        Assert.Throws<InvalidOperationException>(() => read.Write(1, Record(1, .2f)));
        byte[] bytes = new byte[RelayRecord.Bytes]; Assert.True(read.Read(1, bytes));
        Assert.Equal(stage == RelayCheckpoint.Stage.ManifestReplaced ? 2 : 1, BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(4)));
    }
    [Theory]
    [InlineData("index.bin")]
    [InlineData("format.bin")]
    [InlineData("records.bin")]
    public void CorruptionIsRefused(string file)
    {
        using (var store = new PackedRelayRecords(P("work"), 1024, Budget))
        { store.Write(0, Record(0, .2f)); PackedRelayCheckpoint.Publish(store, P("snapshot"), RelayTrainingState.Empty); }
        string path = Path.Combine(P("snapshot"), "00000000000000000001", file);
        var bytes = File.ReadAllBytes(path); bytes[0] ^= 0x7f; File.WriteAllBytes(path, bytes);
        Assert.ThrowsAny<Exception>(() =>
        {
            var saved = PackedRelayCheckpoint.OpenReadOnly(P("snapshot"), Budget); using var read = saved.Store;
            read.Read(0, new byte[RelayRecord.Bytes]);
        });
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChecksummedOutOfRangeOrAliasedPointerIsRefused(bool alias)
    {
        using (var store = new PackedRelayRecords(P("work"), 1024, Budget))
        { store.Write(0, Record(0, .2f)); PackedRelayCheckpoint.Publish(store, P("snapshot"), RelayTrainingState.Empty); }
        string directory = Path.Combine(P("snapshot"), "00000000000000000001");
        byte[] index = File.ReadAllBytes(Path.Combine(directory, "index.bin"));
        BinaryPrimitives.WriteUInt32LittleEndian(index, alias ? 64u : 0u);
        BinaryPrimitives.WriteUInt64LittleEndian(index.AsSpan(8), alias ? 1UL : 2UL);
        File.WriteAllBytes(Path.Combine(directory, "index.bin"), index);
        byte[] state = File.ReadAllBytes(Path.Combine(directory, "state.bin"));
        PackedRelayCheckpoint.IndexHash(directory).CopyTo(state, 80); File.WriteAllBytes(Path.Combine(directory, "state.bin"), state);
        string manifestPath = Path.Combine(P("snapshot"), "manifest.bin"); byte[] pointer = File.ReadAllBytes(manifestPath);
        SHA256.HashData(state).CopyTo(pointer, 8); File.WriteAllBytes(manifestPath, pointer);
        var saved = PackedRelayCheckpoint.OpenReadOnly(P("snapshot"), Budget); using var read = saved.Store;
        Assert.Throws<InvalidDataException>(() => read.Read(alias ? 64u : 0u, new byte[RelayRecord.Bytes]));
    }

}
