using GreyMatter.Poc.Encoding;
using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Engrams;

/// <summary>
/// plan.md P3 gate instrument: populate a store with N recipes that have been
/// through a learning-shaped perturbation, then measure bytes/neuron and
/// regeneration fidelity ACROSS A SAVE/LOAD ROUNDTRIP.
///
/// Two things this deliberately avoids, both of which made the legacy
/// regeneration experiment vacuous ("100% fidelity no matter what"):
///
///   • **A knob that dictates the answer.** The first version perturbed a fixed
///     FRACTION of dims by an amount far above threshold, so deviations-per-neuron
///     was exactly `fraction × dims` and the storage measurement was a restatement
///     of the input. Here every listened dim receives a small drift and the
///     THRESHOLD decides what persists, so deviation count is a measured
///     consequence and `DeviationThreshold` becomes the real fidelity-vs-storage
///     dial §4.3 says it is.
///   • **Checking fidelity in memory.** Consolidate drops sub-threshold deltas, so
///     an in-memory regenerate-and-compare cannot exceed the threshold by
///     construction. Reloading from disk first makes the check able to fail: it
///     now tests that serialization, gzip, delta encoding, and the determinism of
///     Listens()/BaselineWeight() all agree.
/// </summary>
public static class StoreBench
{
    public sealed record Result(
        int Recipes, long Bytes, double BytesPerNeuron,
        int Partitions, double MeanDeviationsPerNeuron, int MaxDeviations,
        int FidelityChecked, int FidelityViolations, double MaxAbsError,
        double CodebookUtilization);

    /// <param name="driftScale">
    /// Standard deviation of the per-dim weight drift, in weight units (weights are
    /// O(45) after <c>BaselineGain</c>). Learning moves a neuron by a lot on a few
    /// lines and barely at all on most; a sum of uniforms gives that shape.
    /// </param>
    public static Result Run(Config cfg, int recipeCount, double driftScale = 1.0, bool quiet = false)
    {
        var store = new EngramStore(cfg.BrainDataPath);
        store.DeleteAll();

        var codebook = new VqCodebook(cfg.VqCodebookSize, cfg.SurfaceDimensions, cfg.Seed);
        var lsh = new LshIndex(cfg.Seed);
        float threshold = (float)cfg.DeviationThreshold;

        var byBucket = new Dictionary<uint, List<NeuronRecipe>>();
        var learned = new float[cfg.SurfaceDimensions];
        var embedding = new float[cfg.SurfaceDimensions];

        // Keep the learned weights so fidelity can be checked against them AFTER
        // the store has been written and read back.
        var learnedByNeuron = new Dictionary<uint, float[]>(recipeCount);

        long totalDeviations = 0;
        int maxDeviations = 0;

        // ── Pass 1: train the codebook, then FREEZE it ──────────────────────
        //
        // This split is not tidiness, it is correctness, and the first version got
        // it wrong: it called QuantizeAndLearn while consolidating, so each recipe's
        // deviations were computed against the codebook as it stood at that moment,
        // while regeneration later used the FINAL codebook. Every prototype had
        // moved underneath its own recipes. Measured cost: 2,555,679 fidelity
        // violations out of 12.8M weights, max error 49.7 against a threshold of 1.0.
        //
        // A recipe is only meaningful relative to the codebook version it was
        // consolidated against — which is exactly why §4.3 says the codebook is
        // "trained online during P4, frozen per checkpoint". Freezing IS the
        // mechanism that makes stored deviations valid.
        if (!quiet) Console.WriteLine("   pass 1: training the codebook…");
        for (uint id = 0; id < recipeCount; id++)
        {
            for (int d = 0; d < embedding.Length; d++)
                embedding[d] = Rng.NextSigned(cfg.Seed, Rng.Purpose.NeuronSeed, id, (uint)d);
            codebook.QuantizeAndLearn(embedding);
        }

        // ── Pass 2: codebook frozen — generate, drift, consolidate ──────────
        if (!quiet) Console.WriteLine("   pass 2: building recipes against the frozen codebook…");
        for (uint id = 0; id < recipeCount; id++)
        {
            for (int d = 0; d < embedding.Length; d++)
                embedding[d] = Rng.NextSigned(cfg.Seed, Rng.Purpose.NeuronSeed, id, (uint)d);
            var code = codebook.Quantize(embedding);   // no learning

            var recipe = new NeuronRecipe(id, code, (uint)Rng.Bits(cfg.Seed, Rng.Purpose.NeuronSeed, id))
            {
                Familiarity = Rng.NextFloat(cfg.Seed, Rng.Purpose.Benchmark, id),
                ActivationCount = (ushort)Rng.NextUInt(cfg.Seed, Rng.Purpose.Benchmark, id, 1000, 1)
            };

            // Regenerate the prototype, then let learning drift it.
            Regeneration.Regenerate(recipe, codebook, learned);
            for (int d = 0; d < learned.Length; d++)
            {
                if (learned[d] == 0f) continue;   // not a line this neuron listens to

                // Sum of three signed uniforms ≈ bell-shaped: most dims barely move,
                // the tails move far. The threshold, not a fraction, decides which
                // of these survive into storage.
                float drift = 0;
                for (uint t = 0; t < 3; t++)
                    drift += Rng.NextSigned(cfg.Seed, Rng.Purpose.Synapse, id, (uint)(d * 8 + t));
                learned[d] += (float)driftScale * drift;
            }

            Regeneration.Consolidate(recipe, codebook, learned, threshold);
            totalDeviations += recipe.DeviationCount;
            if (recipe.DeviationCount > maxDeviations) maxDeviations = recipe.DeviationCount;

            learnedByNeuron[id] = (float[])learned.Clone();

            uint bucket = lsh.PrimaryBucket(SparseCode.TopK(learned, cfg.Sparsity));
            if (!byBucket.TryGetValue(bucket, out var list)) byBucket[bucket] = list = new List<NeuronRecipe>();
            list.Add(recipe);

            if (!quiet && (id + 1) % 20000 == 0)
                Console.WriteLine($"   built {id + 1:N0}/{recipeCount:N0} recipes   {byBucket.Count:N0} partitions");
        }

        foreach (var (bucket, list) in byBucket.OrderBy(kv => kv.Key))
            store.Save(EngramPartition.FromRecipes(bucket, list));

        // ── Fidelity, through disk ──────────────────────────────────────────
        if (!quiet) Console.WriteLine("   verifying fidelity from reloaded partitions…");
        int violations = 0, checkedCount = 0;
        double maxAbsError = 0;
        var regenerated = new float[cfg.SurfaceDimensions];

        foreach (var bucket in byBucket.Keys.OrderBy(k => k))
        {
            var partition = store.Load(bucket)
                ?? throw new InvalidOperationException($"partition {bucket:x8} vanished after save");

            foreach (var recipe in partition.Recipes())
            {
                Regeneration.Regenerate(recipe, codebook, regenerated);
                var truth = learnedByNeuron[recipe.Id];
                for (int d = 0; d < truth.Length; d++)
                {
                    checkedCount++;
                    double err = Math.Abs(truth[d] - regenerated[d]);
                    if (err > maxAbsError) maxAbsError = err;
                    if (err > threshold) violations++;
                }
            }
        }

        long bytes = store.TotalBytes();
        return new Result(
            recipeCount, bytes, (double)bytes / recipeCount,
            byBucket.Count, (double)totalDeviations / recipeCount, maxDeviations,
            checkedCount, violations, maxAbsError,
            codebook.Utilization());
    }
}
