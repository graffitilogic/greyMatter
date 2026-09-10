using System.Buffers.Binary;
using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Storage;
using GreyMatter.Poc.Substrate;
using Xunit;

namespace GreyMatter.Poc.Tests;

public sealed class RelayStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "gm-r2-tests-" + Guid.NewGuid().ToString("N"));
    private string P(string name) => Path.Combine(_root, name);
    private const long Budget = DiskRelayRecords.FixedCharge + 2 * DiskRelayRecords.SlotCharge;
    public RelayStorageTests() => Directory.CreateDirectory(_root);
    public void Dispose() => Directory.Delete(_root, true);
    private static byte[] Record(uint id, uint target, int repeats = 1)
    {
        var syn = new SynapseStore(1, 32);
        for (int i = 0; i < repeats; i++) syn.RecordCoactivation(0, id, target, .5f, 1, SynapsePopulation.CrossCue);
        var b = new byte[RelayRecord.Bytes]; RelayRecord.Encode(id, syn, b); return b;
    }
    [Fact]
    public void DirtyEvictionRoundTripsAndRefusesTooSmallBudget()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiskRelayRecords(P("small"), 100, 4096));
        using var disk = new DiskRelayRecords(P("working"), 100, Budget);
        for (uint i = 0; i < 80; i++) disk.Write(i, Record(i, i + 1));
        var b = new byte[RelayRecord.Bytes];
        for (uint i = 0; i < 80; i++) { Assert.True(disk.Read(i, b)); Assert.Equal(Record(i, i + 1), b); }
        Assert.False(disk.Read(90, b)); Assert.All(b, x => Assert.Equal(0, x));
        Assert.True(disk.Evictions > 100); Assert.Equal(80, disk.DirtyWrites);
        Assert.True(disk.ReservedBytes <= Budget);
        Assert.Throws<ArgumentOutOfRangeException>(() => disk.Read(100, b));
    }
    [Theory]
    [InlineData(RelayCheckpoint.Stage.DuringGenerationCopy)]
    [InlineData(RelayCheckpoint.Stage.GenerationFlushed)]
    [InlineData(RelayCheckpoint.Stage.BeforeManifestReplace)]
    [InlineData(RelayCheckpoint.Stage.ManifestReplaced)]
    public void InterruptedPublicationHasOnlyOldOrNewCompleteGeneration(RelayCheckpoint.Stage stage)
    {
        using var disk = new DiskRelayRecords(P("working"), 100, Budget);
        disk.Write(3, Record(3, 4)); RelayCheckpoint.Publish(disk, P("saved"), RelayTrainingState.Empty);
        disk.Write(3, Record(3, 4, 8));
        Assert.Throws<IOException>(() => RelayCheckpoint.Publish(disk, P("saved"),
            new(8, 2, 3, new uint[] { 3 }), s => { if (s == stage) throw new IOException("Simulated termination"); }));
        var restored = RelayCheckpoint.Restore(P("saved"), P("restored"), Budget); using var store = restored.Store;
        var b = new byte[RelayRecord.Bytes]; Assert.True(store.Read(3, b));
        bool published = stage == RelayCheckpoint.Stage.ManifestReplaced;
        Assert.Equal(Record(3, 4, published ? 8 : 1), b);
        Assert.Equal(published ? 8 : 0, restored.State.Updates);
    }
    [Theory]
    [InlineData("records.bin", false)]
    [InlineData("records.bin", true)]
    [InlineData("presence.bin", false)]
    [InlineData("state.bin", false)]
    public void MissingOrCorruptCommittedLearnedDataIsNeverUnseen(string file, bool remove)
    {
        using var disk = new DiskRelayRecords(P("working"), 100, Budget);
        disk.Write(3, Record(3, 4)); RelayCheckpoint.Publish(disk, P("saved"), RelayTrainingState.Empty);
        string path = Path.Combine(P("saved"), "00000000000000000001", file);
        if (remove) File.Delete(path);
        else
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Write);
            stream.Position = file == "records.bin" ? 3 * RelayRecord.Bytes : file == "presence.bin" ? 3 : 0;
            stream.WriteByte(0);
        }
        if (remove) Assert.Throws<FileNotFoundException>(() => RelayCheckpoint.Restore(P("saved"), P("restored"), Budget));
        else Assert.Throws<InvalidDataException>(() => RelayCheckpoint.Restore(P("saved"), P("restored"), Budget));
    }
    [Fact]
    public void MidSequenceRestartAndDecayMatchUninterruptedLearning()
    {
        using var resident = new ResidentRelayRecords(128);
        var expected = new StoredRelayLearning(resident);
        using var disk = new DiskRelayRecords(P("working"), 128, Budget);
        var actual = new StoredRelayLearning(disk);
        for (int i = 0; i < 501; i++)
        {
            uint a = (uint)(i % 60), b = a + 60;
            expected.ObserveMembers(new[] { a }); actual.ObserveMembers(new[] { a });
            if (i == 250)
            {
                RelayCheckpoint.Publish(disk, P("saved"), actual.Capture());
                var restored = RelayCheckpoint.Restore(P("saved"), P("resumed"), Budget);
                using var resumed = restored.Store; var learner = new StoredRelayLearning(resumed, restored.State);
                for (int j = 250; j < 501; j++)
                {
                    uint x = (uint)(j % 60);
                    if (j != 250) learner.ObserveMembers(new[] { x });
                    learner.ObserveMembers(new[] { x + 60 }); learner.EndSequence();
                }
                RelayCheckpoint.Publish(resumed, P("finished"), learner.Capture());
            }
            expected.ObserveMembers(new[] { b }); actual.ObserveMembers(new[] { b });
            expected.EndSequence(); actual.EndSequence();
        }
        var final = RelayCheckpoint.Restore(P("finished"), P("final"), Budget); using var finalStore = final.Store;
        var left = new byte[RelayRecord.Bytes]; var right = new byte[RelayRecord.Bytes];
        for (uint i = 0; i < 128; i++)
        {
            Assert.Equal(resident.Read(i, left), disk.Read(i, right)); Assert.Equal(left, right);
            Assert.Equal(resident.Read(i, left), finalStore.Read(i, right)); Assert.Equal(left, right);
        }
        Assert.Equal(expected.Capture().Updates, final.State.Updates); Assert.Equal(501, final.State.Episodes);
    }
    [Fact]
    public void StoredLearningMatchesOriginalA1IncludingDegreePressureAndDecay()
    {
        var cfg = new Config { WorkingSetMax = 1024 };
        using var scope = new ActivationScope(cfg); var original = new AssemblyRelay(cfg, scope);
        using var store = new ResidentRelayRecords((uint)cfg.BaselineNeuronCount); var stored = new StoredRelayLearning(store);
        var root = new SparseCode(new[] { 11 });
        for (int i = 0; i < 501; i++)
        {
            var target = new SparseCode(new[] { 20 + i % 7 });
            original.Observe(root); original.Observe(target); original.EndSequence();
            stored.Observe(root); stored.Observe(target); stored.EndSequence();
            if ((i + 1) % 500 == 0) scope.Synapses.ApplyDecay(scope.Pool.Count, .99f);
        }
        var b = new byte[RelayRecord.Bytes]; var scratch = new SynapseStore(1, 32);
        foreach (var code in Enumerable.Range(20, 7).Select(x => new SparseCode(new[] { x })).Prepend(root))
            foreach (uint id in AssemblyRelay.Members(code, cfg.BaselineNeuronCount))
            {
                Assert.True(store.Read(id, b)); RelayRecord.Decode(id, b, scratch);
                int slot = scope.Pool.Find(id), start = scope.Synapses.SegmentStart(slot);
                Assert.Equal(scope.Synapses.Degree[slot], scratch.Degree[0]);
                for (int e = 0; e < scratch.Degree[0]; e++)
                {
                    Assert.Equal(scope.Synapses.Target[start + e], scratch.Target[e]);
                    Assert.Equal(scope.Synapses.Weight[start + e], scratch.Weight[e]);
                    Assert.Equal(scope.Synapses.Population[start + e], scratch.Population[e]);
                }
            }
        Assert.Equal(original.Updates, stored.Updates);
    }
}
