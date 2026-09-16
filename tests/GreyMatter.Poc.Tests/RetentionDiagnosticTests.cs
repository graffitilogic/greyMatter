using GreyMatter.Poc.Runtime;
using GreyMatter.Poc.Storage;
using GreyMatter.Poc.Substrate;
using Xunit;

namespace GreyMatter.Poc.Tests;
public sealed class RetentionDiagnosticTests
{
    [Fact]
    public void UnrelatedEpisodesPruneBirthWhileDiagnosticPreservesIt()
    {
        using var normal = new ResidentRelayRecords(100);
        using var retained = new ResidentRelayRecords(100);
        var a = new StoredRelayLearning(normal);
        var b = new StoredRelayLearning(retained) { DisableDecayForDiagnostic = true };
        foreach (var learner in new[] { a, b })
        {
            learner.ObserveMembers(new uint[] { 1 }); learner.ObserveMembers(new uint[] { 2 }); learner.EndSequence();
            for (int i = 1; i < 2500; i++) learner.EndSequence();
        }
        var record = new byte[RelayRecord.Bytes]; var syn = new SynapseStore(1, 32);
        Assert.True(normal.Read(1, record)); RelayRecord.Decode(1, record, syn); Assert.Equal(0, syn.Degree[0]);
        Assert.True(retained.Read(1, record)); RelayRecord.Decode(1, record, syn); Assert.Equal(1, syn.Degree[0]);
        Assert.Equal(.105f, syn.Weight[0], 6);
        Assert.Equal(a.Updates, b.Updates); Assert.Equal(a.Episodes, b.Episodes);
    }
}
