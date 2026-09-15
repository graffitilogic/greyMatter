using GreyMatter.Poc.Storage;
using GreyMatter.Poc.Substrate;
using Xunit;

namespace GreyMatter.Poc.Tests;
public sealed class CapacityStorageTests
{
    [Fact]
    public void CollisionClustersSurviveRepeatedLruEvictionAndDirtyEnumeration()
    {
        string directory = Path.Combine(Path.GetTempPath(), "gm-r4-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var disk = new DiskRelayRecords(directory, 4096, DiskRelayRecords.FixedCharge + 3 * DiskRelayRecords.SlotCharge);
            using var expected = new ResidentRelayRecords(4096);
            var syn = new SynapseStore(1, 32); var bytes = new byte[RelayRecord.Bytes]; var actual = new byte[RelayRecord.Bytes];
            var rng = new Random(100);
            for (int i = 0; i < 2000; i++)
            {
                uint id = (uint)rng.Next(64) * 8; // same initial hash bucket, including wraparound clusters
                syn.Degree[0] = 1; syn.Target[0] = id + 1; syn.Weight[0] = (i % 99 + 1) / 100f;
                RelayRecord.Encode(id, syn, bytes); expected.Write(id, bytes); disk.Write(id, bytes);
                uint query = (uint)rng.Next(64) * 8;
                Assert.Equal(expected.Read(query, bytes), disk.Read(query, actual)); Assert.Equal(bytes, actual);
            }
            var visited = new List<uint>(); disk.VisitPresent(visited.Add);
            Assert.Equal(64, visited.Count); Assert.Equal(visited.OrderBy(x => x), visited);
            foreach (uint id in visited)
            { Assert.True(expected.Read(id, bytes)); Assert.True(disk.Read(id, actual)); Assert.Equal(bytes, actual); }
            Assert.True(disk.ReservedBytes <= disk.BudgetBytes);
        }
        finally { Directory.Delete(directory, true); }
    }
}
