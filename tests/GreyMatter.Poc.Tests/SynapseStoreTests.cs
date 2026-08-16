using GreyMatter.Poc.Substrate;
using Xunit;

namespace GreyMatter.Poc.Tests;

public class SynapseStoreTests
{
    private static SynapseStore Store(int cap = 4) => new(slotCapacity: 8, capPerNeuron: cap);

    [Fact]
    public void NewSynapse_RequiresBothPartiesMeaningfullyActive()
    {
        var s = Store();
        // product 0.1 × 0.1 = 0.01, below CreationProductThreshold 0.15
        s.RecordCoactivation(0, 100, 200, 0.1f, 0.1f);
        Assert.Equal(0, s.Degree[0]);
        Assert.Equal(0, s.Created);

        s.RecordCoactivation(0, 100, 200, 0.9f, 0.9f);
        Assert.Equal(1, s.Degree[0]);
        Assert.Equal(1, s.Created);
    }

    [Fact]
    public void NewSynapse_IsBornJustAboveThePruneLine()
    {
        // Legacy semantics: one decay pass kills it unless reinforced.
        var s = Store();
        s.RecordCoactivation(0, 100, 200, 1f, 1f);
        var w = s.GetWeight(0, 200);
        Assert.Equal(s.PruneThreshold + s.LearningRate, w, 5);

        s.ApplyDecay(liveSlots: 1, decayFactor: 0.9f);
        Assert.Equal(0, s.Degree[0]);
    }

    [Fact]
    public void ReinforcementLetsASynapseSurviveDecay()
    {
        var s = Store();
        for (int i = 0; i < 50; i++) s.RecordCoactivation(0, 100, 200, 1f, 1f);
        s.ApplyDecay(liveSlots: 1, decayFactor: 0.9f);
        Assert.Equal(1, s.Degree[0]);
    }

    [Fact]
    public void StrengtheningAnExistingSynapseIsAlwaysAllowed()
    {
        var s = Store();
        s.RecordCoactivation(0, 100, 200, 1f, 1f);
        var before = s.GetWeight(0, 200);

        // Product below the creation threshold — but this synapse already exists.
        s.RecordCoactivation(0, 100, 200, 0.1f, 0.1f);
        Assert.True(s.GetWeight(0, 200) > before);
        Assert.Equal(1, s.Created);
        Assert.Equal(1, s.Strengthened);
    }

    [Fact]
    public void Weight_IsClampedToMax()
    {
        var s = Store();
        for (int i = 0; i < 10_000; i++) s.RecordCoactivation(0, 100, 200, 1f, 1f);
        Assert.Equal(s.MaxWeight, s.GetWeight(0, 200), 5);
    }

    [Fact]
    public void SelfConnectionsAreRejected()
    {
        var s = Store();
        s.RecordCoactivation(0, 100, 100, 1f, 1f);
        Assert.Equal(0, s.Degree[0]);
    }

    [Fact]
    public void DegreeNeverExceedsTheCap()
    {
        var s = Store(cap: 4);
        for (uint t = 200; t < 260; t++) s.RecordCoactivation(0, 100, t, 1f, 1f);
        Assert.Equal(4, s.Degree[0]);
    }

    [Fact]
    public void AtTheCap_AStrongerCandidateDisplacesTheWeakestSynapse()
    {
        var s = Store(cap: 2);

        // Two synapses, one heavily reinforced and one fresh.
        for (int i = 0; i < 50; i++) s.RecordCoactivation(0, 100, 200, 1f, 1f);
        s.RecordCoactivation(0, 100, 201, 1f, 1f);
        Assert.Equal(2, s.Degree[0]);

        // Decay the fresh one below a new birth weight without killing it.
        s.ApplyDecay(liveSlots: 1, decayFactor: 0.999f);

        s.RecordCoactivation(0, 100, 202, 1f, 1f);

        Assert.Equal(2, s.Degree[0]);
        Assert.True(s.GetWeight(0, 200) > 0, "the reinforced synapse must survive competition");
        Assert.Equal(1, s.Displaced);
        Assert.Equal(0, s.Declined);
        Assert.True(s.GetWeight(0, 202) > 0, "the stronger candidate should have taken the slot");
    }

    [Fact]
    public void AtTheCap_AWeakerCandidateIsDeclinedRatherThanChurning()
    {
        var s = Store(cap: 2);
        for (int i = 0; i < 50; i++)
        {
            s.RecordCoactivation(0, 100, 200, 1f, 1f);
            s.RecordCoactivation(0, 100, 201, 1f, 1f);
        }

        s.RecordCoactivation(0, 100, 202, 1f, 1f);

        Assert.Equal(1, s.Declined);
        Assert.Equal(0, s.Displaced);
        Assert.Equal(0f, s.GetWeight(0, 202));
    }

    [Fact]
    public void Depress_WeakensButNeverCreates()
    {
        var s = Store();
        s.Depress(0, 200, 0.5f);
        Assert.Equal(0, s.Degree[0]);

        s.RecordCoactivation(0, 100, 200, 1f, 1f);
        var before = s.GetWeight(0, 200);
        s.Depress(0, 200, 0.05f);
        Assert.Equal(before - 0.05f, s.GetWeight(0, 200), 5);
    }

    [Fact]
    public void Prune_RemovesOnlySubThresholdSynapses()
    {
        var s = Store(cap: 4);
        for (int i = 0; i < 80; i++) s.RecordCoactivation(0, 100, 200, 1f, 1f);   // strong
        s.RecordCoactivation(0, 100, 201, 1f, 1f);                                 // fresh
        s.Depress(0, 201, 0.05f);                                                  // push below the line

        Assert.Equal(1, s.PruneWeakSynapses(liveSlots: 1));
        Assert.Equal(1, s.Degree[0]);
        Assert.True(s.GetWeight(0, 200) > 0);
    }

    [Fact]
    public void MoveSlot_TransfersTheWholeSegmentAndVacatesTheSource()
    {
        var s = Store(cap: 4);
        for (int i = 0; i < 30; i++) s.RecordCoactivation(3, 100, 200, 1f, 1f);
        var w = s.GetWeight(3, 200);

        s.MoveSlot(3, 1);

        Assert.Equal(0, s.Degree[3]);
        Assert.Equal(1, s.Degree[1]);
        Assert.Equal(w, s.GetWeight(1, 200), 6);
    }

    [Fact]
    public void ConstructorRejectsAnOverflowingCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SynapseStore(200_000_000, 32));
    }
}
