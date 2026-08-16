using GreyMatter.Poc.Substrate;
using Xunit;

namespace GreyMatter.Poc.Tests;

public class NeuronPoolTests
{
    [Fact]
    public void Materialize_AssignsSlotsAndFindsThemBack()
    {
        var pool = new NeuronPool(16);
        int a = pool.Materialize(900_000);
        int b = pool.Materialize(7);

        Assert.Equal(2, pool.Count);
        Assert.Equal(a, pool.Find(900_000));
        Assert.Equal(b, pool.Find(7));
        Assert.Equal(900_000u, pool.VirtualId[a]);
    }

    [Fact]
    public void Find_ReturnsMinusOneForAbsentNeuron()
    {
        var pool = new NeuronPool(16);
        pool.Materialize(1);
        Assert.Equal(-1, pool.Find(2));
        Assert.False(pool.IsResident(2));
    }

    [Fact]
    public void Materialize_IsIdempotentForAResidentNeuron()
    {
        var pool = new NeuronPool(16);
        int first = pool.Materialize(42);
        int second = pool.Materialize(42);
        Assert.Equal(first, second);
        Assert.Equal(1, pool.Count);
        Assert.Equal(1, pool.TotalMaterialized);
    }

    [Fact]
    public void WorkingSetNeverExceedsCapacity()
    {
        // The P4 gate depends on this invariant holding under pressure.
        var pool = new NeuronPool(64);
        for (uint i = 0; i < 5000; i++)
        {
            pool.AdvanceTick();
            pool.Materialize(i);
            Assert.True(pool.Count <= 64, $"count {pool.Count} exceeded capacity at i={i}");
        }
        Assert.True(pool.HighWaterMark <= 64);
        Assert.True(pool.TotalEvicted > 0, "expected eviction under pressure");
    }

    [Fact]
    public void Eviction_KeepsRecentlyTouchedNeuronsAndDropsStaleOnes()
    {
        var pool = new NeuronPool(32);

        // Fill with 32 neurons, all at tick 1.
        pool.AdvanceTick();
        for (uint i = 0; i < 32; i++) pool.Materialize(i);

        // Keep the first eight alive across several ticks; let the rest go stale.
        for (int t = 0; t < 5; t++)
        {
            pool.AdvanceTick();
            for (uint i = 0; i < 8; i++) pool.Touch(pool.Find(i));
        }

        // Force eviction.
        pool.AdvanceTick();
        pool.Materialize(1000);

        for (uint i = 0; i < 8; i++)
            Assert.True(pool.IsResident(i), $"recently-touched neuron {i} was evicted");
        Assert.True(pool.IsResident(1000));
    }

    [Fact]
    public void Eviction_NotifiesTheCallerForEachEvictedSlot()
    {
        // §4.4 step 6: consolidation happens on eviction, so a missed notification
        // is silently lost learning.
        var pool = new NeuronPool(16);
        var evicted = new List<uint>();

        pool.AdvanceTick();
        for (uint i = 0; i < 16; i++) pool.Materialize(i);

        pool.AdvanceTick();
        pool.Materialize(999, slot => evicted.Add(pool.VirtualId[slot]));

        Assert.NotEmpty(evicted);
        Assert.All(evicted, v => Assert.True(v < 16));
        Assert.Equal(evicted.Count, (int)pool.TotalEvicted);
    }

    [Fact]
    public void Compaction_KeepsTheHashConsistentWithMovedSlots()
    {
        // A stale hash entry after compaction would silently route activation to
        // the wrong neuron — corruption that produces plausible-looking numbers.
        var pool = new NeuronPool(64);
        pool.AdvanceTick();
        for (uint i = 0; i < 64; i++) pool.Materialize(i);

        // Scopes of 8 across many ticks, so churn forces repeated compaction
        // without ever asking for a scope wider than the pool.
        for (uint round = 0; round < 40; round++)
        {
            pool.AdvanceTick();
            for (uint i = 0; i < 8; i++) pool.Materialize(1000 + round * 8 + i);

            for (int slot = 0; slot < pool.Count; slot++)
                Assert.Equal(slot, pool.Find(pool.VirtualId[slot]));
        }

        Assert.True(pool.TotalEvicted > 0, "expected compaction to have run");
    }

    [Fact]
    public void SlotMovedCallback_FiresForEveryCompactionMove()
    {
        var pool = new NeuronPool(32);
        var moves = new List<(int from, int to)>();
        pool.OnSlotMoved = (f, t) => moves.Add((f, t));

        pool.AdvanceTick();
        for (uint i = 0; i < 32; i++) pool.Materialize(i);
        pool.AdvanceTick();
        pool.Materialize(500);

        Assert.All(moves, m => Assert.True(m.to < m.from, "compaction should only move slots downward"));
    }

    /// <summary>
    /// A scope wider than the whole working set is a configuration error, and it
    /// must fail loudly. Silently evicting a current-tick neuron would hand the
    /// caller a slot index that is invalid the instant it is returned — corruption
    /// that surfaces much later as inexplicably truncated recall.
    /// </summary>
    [Fact]
    public void AScopeWiderThanTheWorkingSetFailsLoudly()
    {
        var pool = new NeuronPool(32);
        pool.AdvanceTick();

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            for (uint i = 0; i < 100; i++) pool.Materialize(i);
        });
        Assert.Contains("exceeds WorkingSetMax", ex.Message);
    }

    [Fact]
    public void CurrentTickNeuronsAreNeverEvicted()
    {
        var pool = new NeuronPool(32);

        pool.AdvanceTick();
        for (uint i = 0; i < 32; i++) pool.Materialize(i);   // all stale after the next tick

        pool.AdvanceTick();
        var thisScope = new List<uint>();
        for (uint i = 100; i < 120; i++)
        {
            pool.Materialize(i);
            thisScope.Add(i);
            // Everything materialized this tick must still be addressable.
            foreach (var v in thisScope)
                Assert.True(pool.IsResident(v), $"neuron {v} from the current scope was evicted");
        }
    }

    [Fact]
    public void Clear_ResetsResidencyWithoutLeavingStaleHashEntries()
    {
        var pool = new NeuronPool(16);
        pool.Materialize(5);
        pool.Clear();
        Assert.Equal(0, pool.Count);
        Assert.Equal(-1, pool.Find(5));
    }
}
