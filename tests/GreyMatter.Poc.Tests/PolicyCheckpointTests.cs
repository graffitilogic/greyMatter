using System.Buffers.Binary;
using System.Security.Cryptography;
using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Storage;
using Xunit;

namespace GreyMatter.Poc.Tests;
public sealed class PolicyCheckpointTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "gm-policy-" + Guid.NewGuid().ToString("N"));
    private const long Budget = DiskRelayRecords.FixedCharge + DiskRelayRecords.SlotCharge;
    private string P(string name) => Path.Combine(_root, name);
    public PolicyCheckpointTests() => Directory.CreateDirectory(_root);
    public void Dispose() => Directory.Delete(_root, true);
    private static void Teach(StoredRelayLearning learner, uint source, uint target)
    { learner.ObserveMembers(new[] { source }); learner.ObserveMembers(new[] { target }); learner.EndSequence(); }
    private static void Same(IRelayRecords a, IRelayRecords b)
    {
        byte[] x = new byte[RelayRecord.Bytes], y = new byte[RelayRecord.Bytes];
        for (uint id = 0; id < a.IdLimit; id++) { Assert.Equal(a.Read(id, x), b.Read(id, y)); Assert.Equal(x, y); }
    }
    [Fact]
    public void DirtyEvictionAndMidSequenceRestartRecoverPolicyWithoutFlags()
    {
        using var resident = new ResidentRelayRecords(256);
        var reference = new StoredRelayLearning(resident) { SourceLocalForgetting = true };
        RelayTrainingState expected;
        using (var disk = new DiskRelayRecords(P("work"), 256, Budget))
        {
            var learner = new StoredRelayLearning(disk) { SourceLocalForgetting = true };
            for (int i = 0; i < 1100; i++) foreach (var l in new[] { reference, learner }) Teach(l, (uint)(i % 80), (uint)(100 + i % 90));
            foreach (var l in new[] { reference, learner }) l.ObserveMembers(new uint[] { 7, 8 });
            expected = learner.Capture(); RelayCheckpoint.Publish(disk, P("snapshot"), expected);
            Assert.True(disk.Evictions > 0); Same(resident, disk);
        }
        var saved = RelayCheckpoint.Restore(P("snapshot"), P("resume"), Budget);
        using var restored = saved.Store;
        Assert.Equal(RelayLearningPolicy.SourceLocal, saved.State.Policy);
        var resumed = new StoredRelayLearning(restored, saved.State);
        Assert.True(resumed.SourceLocalForgetting);
        Assert.Throws<InvalidOperationException>(() => new StoredRelayLearning(restored, saved.State) { SourceLocalForgetting = false });
        Assert.Throws<InvalidOperationException>(() => new StoredRelayLearning(restored, saved.State) { DisableDecayForDiagnostic = true });
        foreach (var l in new[] { reference, resumed }) { l.ObserveMembers(new uint[] { 200, 201 }); l.EndSequence(); }
        for (int i = 0; i < 1100; i++) foreach (var l in new[] { reference, resumed }) Teach(l, (uint)(i % 80), (uint)(120 + i % 90));
        Same(resident, restored); Assert.Equal(reference.Updates, resumed.Updates); Assert.Equal(reference.Episodes, resumed.Episodes);
        Assert.Equal(reference.Capture().Policy, resumed.Capture().Policy);
    }
    [Theory]
    [InlineData(RelayCheckpoint.Stage.DuringGenerationCopy)]
    [InlineData(RelayCheckpoint.Stage.BeforeManifestReplace)]
    [InlineData(RelayCheckpoint.Stage.ManifestReplaced)]
    public void InterruptedPublicationKeepsCompletePolicyGeneration(RelayCheckpoint.Stage stop)
    {
        using var disk = new DiskRelayRecords(P("work"), 256, Budget);
        var learner = new StoredRelayLearning(disk) { SourceLocalForgetting = true };
        Teach(learner, 1, 2); RelayCheckpoint.Publish(disk, P("snapshot"), learner.Capture());
        Teach(learner, 1, 3);
        Assert.Throws<IOException>(() => RelayCheckpoint.Publish(disk, P("snapshot"), learner.Capture(), s => { if (s == stop) throw new IOException("injected"); }));
        var saved = RelayCheckpoint.OpenReadOnly(P("snapshot"), Budget); using var store = saved.Store;
        Assert.Equal(RelayLearningPolicy.SourceLocal, saved.State.Policy);
        Assert.Equal(stop == RelayCheckpoint.Stage.ManifestReplaced ? 2 : 1, saved.State.Episodes);
        Assert.Throws<InvalidOperationException>(() => store.Write(1, new byte[RelayRecord.Bytes]));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CorruptOrUnknownPolicyIsRefused(bool validChecksum)
    {
        using var disk = new DiskRelayRecords(P("work"), 256, Budget);
        var learner = new StoredRelayLearning(disk) { SourceLocalForgetting = true };
        Teach(learner, 1, 2); RelayCheckpoint.Publish(disk, P("snapshot"), learner.Capture());
        string statePath = Path.Combine(P("snapshot"), "00000000000000000001", "state.bin");
        byte[] state = File.ReadAllBytes(statePath); BinaryPrimitives.WriteInt32LittleEndian(state.AsSpan(112), 999); File.WriteAllBytes(statePath, state);
        if (validChecksum)
        {
            string manifestPath = Path.Combine(P("snapshot"), "manifest.bin"); byte[] manifest = File.ReadAllBytes(manifestPath);
            SHA256.HashData(state).CopyTo(manifest, 8); File.WriteAllBytes(manifestPath, manifest);
        }
        Assert.ThrowsAny<Exception>(() => RelayCheckpoint.OpenReadOnly(P("snapshot"), Budget));
    }
    [Fact]
    public void LegacyRemainsGlobalAndDeferredRejectsDifferentPolicyBeforePublishing()
    {
        using var disk = new DiskRelayRecords(P("work"), 256, Budget);
        var learner = new StoredRelayLearning(disk); Teach(learner, 1, 2);
        RelayCheckpoint.Publish(disk, P("legacy"), learner.Capture());
        var saved = RelayCheckpoint.OpenReadOnly(P("legacy"), Budget); using var read = saved.Store;
        Assert.Equal(RelayLearningPolicy.Global, saved.State.Policy);
        Assert.Throws<InvalidOperationException>(() => new StoredRelayLearning(read, saved.State) { SourceLocalForgetting = true });
        using var deferred = new DeferredRelayRecords(P("deferred"), 256, Budget + DeferredRelayRecords.ExtraReservedBytes);
        Assert.Throws<ArgumentException>(() => DeferredRelayCheckpoint.Publish(deferred, P("invalid"), learner.Capture() with { Policy = RelayLearningPolicy.SourceLocal }));
        Assert.False(Directory.Exists(P("invalid")));
    }
}
