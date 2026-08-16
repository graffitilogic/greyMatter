using GreyMatter.Poc;
using GreyMatter.Poc.Substrate;
using Xunit;

namespace GreyMatter.Poc.Tests;

/// <summary>
/// plan.md rule 8 and the P1 gate's determinism requirement: same seed ⇒
/// bit-identical state after a scripted workload.
///
/// This is the rule the legacy tree could not satisfy — `Guid.NewGuid()` cluster
/// ids made iteration order differ every run, which is why single-run
/// correlations were untrustworthy and every result needed repeats to say
/// anything. Determinism is what buys back the ability to read one run.
/// </summary>
public class DeterminismTests
{
    [Fact]
    public void Rng_IsAPureFunctionOfItsInputs()
    {
        Assert.Equal(Rng.Bits(7, Rng.Purpose.NeuronSeed, 12345, 9),
                     Rng.Bits(7, Rng.Purpose.NeuronSeed, 12345, 9));
    }

    [Fact]
    public void Rng_SeparatesPurposesForTheSameId()
    {
        // Independent uses of the same neuron id must not correlate.
        Assert.NotEqual(Rng.Bits(1, Rng.Purpose.NeuronSeed, 42),
                        Rng.Bits(1, Rng.Purpose.ReceptiveField, 42));
    }

    [Fact]
    public void Rng_DrawsDoNotDependOnHowManyCameBefore()
    {
        // The property System.Random cannot offer, and the reason for the port.
        var direct = Rng.NextFloat(3, Rng.Purpose.Synapse, 500, 2);
        for (int i = 0; i < 1000; i++) _ = Rng.NextFloat(3, Rng.Purpose.Synapse, (uint)i, 7);
        Assert.Equal(direct, Rng.NextFloat(3, Rng.Purpose.Synapse, 500, 2));
    }

    [Fact]
    public void Rng_FloatsStayInRange()
    {
        for (uint i = 0; i < 20_000; i++)
        {
            var f = Rng.NextFloat(11, Rng.Purpose.Projection, i);
            Assert.InRange(f, 0f, 0.9999999f);
            Assert.InRange(Rng.NextSigned(11, Rng.Purpose.Projection, i), -1f, 1f);
            Assert.InRange(Rng.NextUInt(11, Rng.Purpose.Projection, i, 10), 0u, 9u);
        }
    }

    [Fact]
    public void Rng_ShuffleIsReproducibleAndAPermutation()
    {
        var a = Enumerable.Range(0, 200).ToList();
        var b = Enumerable.Range(0, 200).ToList();
        Rng.Shuffle(a, seed: 99);
        Rng.Shuffle(b, seed: 99);

        Assert.Equal(a, b);
        Assert.Equal(Enumerable.Range(0, 200), a.OrderBy(x => x));
        Assert.NotEqual(Enumerable.Range(0, 200), a);
    }

    [Fact]
    public void SameSeed_ProducesBitIdenticalSubstrateState()
    {
        var cfg = Bench(seed: 4242);
        var a = SubstrateBench.Run(cfg, cycles: 300, scopeSize: 200, quiet: true);
        var b = SubstrateBench.Run(cfg, cycles: 300, scopeSize: 200, quiet: true);

        Assert.Equal(a.Synapses, b.Synapses);
        Assert.Equal(a.Created, b.Created);
        Assert.Equal(a.Strengthened, b.Strengthened);
        Assert.Equal(a.Displaced, b.Displaced);
        Assert.Equal(a.Declined, b.Declined);
        Assert.Equal(a.Materialized, b.Materialized);
        Assert.Equal(a.Evicted, b.Evicted);
        Assert.Equal(a.HighWaterMark, b.HighWaterMark);
    }

    [Fact]
    public void DifferentSeed_ProducesDifferentSubstrateState()
    {
        // Guards the inverse: a "deterministic" system that ignores its seed
        // would pass the test above trivially.
        var a = SubstrateBench.Run(Bench(seed: 1), cycles: 300, scopeSize: 200, quiet: true);
        var b = SubstrateBench.Run(Bench(seed: 2), cycles: 300, scopeSize: 200, quiet: true);
        Assert.NotEqual(a.Synapses, b.Synapses);
    }

    [Fact]
    public void WorkingSetCapIsRespectedUnderSustainedChurn()
    {
        var cfg = Bench(seed: 7);
        cfg.WorkingSetMax = 2_000;
        var r = SubstrateBench.Run(cfg, cycles: 500, scopeSize: 500, quiet: true);
        Assert.True(r.HighWaterMark <= cfg.WorkingSetMax);
        Assert.True(r.Evicted > 0, "sustained churn should force eviction");
    }

    private static Config Bench(int seed) => new()
    {
        Seed = seed,
        BaselineNeuronCount = 100_000,
        WorkingSetMax = 5_000,
        ActivationDepth = 4,
        ActivationWidth = 64,
        SynapseCapPerNeuron = 32
    };
}
