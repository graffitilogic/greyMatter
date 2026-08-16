namespace GreyMatter.Poc.Substrate;

/// <summary>
/// plan.md §4.1 / §7 — counter-based, seeded, splittable.
///
/// Deliberately NOT <c>System.Random</c>. A stateful stream makes every draw
/// depend on how many draws came before it, which makes results depend on
/// iteration order — the exact class of noise rule 8 exists to eliminate, and
/// the reason the legacy harness could not trust a single run.
///
/// Here a value is a pure function of (seed, purpose, id, counter). Nothing
/// carries state between draws, so a GPU thread computing neuron 900,000's
/// receptive field gets the same bits regardless of what any other thread did,
/// or whether it ran at all. This is the property that makes the P7c port a
/// translation rather than a rewrite.
/// </summary>
public static class Rng
{
    /// <summary>Purpose tags keep independent uses of the same id from correlating.</summary>
    public enum Purpose : ulong
    {
        ReceptiveField = 0x9E3779B97F4A7C15,
        NeuronSeed     = 0xBF58476D1CE4E5B9,
        Synapse        = 0x94D049BB133111EB,
        Projection     = 0xD6E8FEB86659FD93,
        Benchmark      = 0xA24BAED4963EE407
    }

    /// <summary>SplitMix64 finalizer — the mixing step, used here as the whole generator.</summary>
    public static ulong Mix(ulong x)
    {
        x += 0x9E3779B97F4A7C15UL;
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        return x ^ (x >> 31);
    }

    public static ulong Bits(int seed, Purpose purpose, uint id, uint counter = 0) =>
        Mix((ulong)(uint)seed ^ (ulong)purpose ^ Mix(((ulong)id << 32) | counter));

    /// <summary>Uniform in [0,1). float32, per §7 — no doubles in hot state.</summary>
    public static float NextFloat(int seed, Purpose purpose, uint id, uint counter = 0) =>
        (Bits(seed, purpose, id, counter) >> 40) * (1.0f / 16777216.0f);   // 24 bits

    /// <summary>Uniform in [-1,1).</summary>
    public static float NextSigned(int seed, Purpose purpose, uint id, uint counter = 0) =>
        NextFloat(seed, purpose, id, counter) * 2.0f - 1.0f;

    /// <summary>Uniform integer in [0, bound). bound must be positive.</summary>
    public static uint NextUInt(int seed, Purpose purpose, uint id, uint bound, uint counter = 0) =>
        (uint)((Bits(seed, purpose, id, counter) >> 32) % bound);

    /// <summary>
    /// Deterministic in-place shuffle. Takes the seed explicitly so a caller can
    /// never accidentally shuffle from ambient state (rule 8).
    /// </summary>
    public static void Shuffle<T>(IList<T> items, int seed, Purpose purpose = Purpose.Benchmark)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = (int)NextUInt(seed, purpose, (uint)i, (uint)(i + 1));
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
