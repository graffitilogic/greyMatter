using GreyMatter.Poc.Substrate;

namespace GreyMatter.Poc.Engrams;

/// <summary>
/// plan.md §4.3 — the engram. A neuron is not stored; its RECIPE is.
///
/// Regeneration = decode the codebook prototype → derive the receptive field
/// deterministically from (seed, vqCode) → apply deviations. Only weights that
/// learning moved beyond <c>DeviationThreshold</c> occupy any bytes, so a
/// neuron's persisted size is a function of how much it actually learned, not of
/// how many inputs it has. That is the thesis, and it is what gives fidelity
/// something it can lose.
///
/// Legacy measured its predecessor at 1.9% procedural content — 4 bytes of VQ code
/// against ~208 bytes of verbatim weights — which is why its regeneration
/// experiment had no failure mode and returned 100% no matter what.
///
/// This is a mutable class rather than a record because it is the working form.
/// The persisted form is the struct-of-arrays layout in <see cref="EngramPartition"/>.
/// </summary>
public sealed class NeuronRecipe
{
    public uint Id;
    public ushort VqCode;
    public uint Seed;
    public float Familiarity;
    public ushort ActivationCount;

    /// <summary>Sorted by dim. Parallel arrays, matching the on-disk layout.</summary>
    public ushort[] DeviationDims = Array.Empty<ushort>();
    public float[] DeviationDeltas = Array.Empty<float>();

    public int DeviationCount => DeviationDims.Length;

    public NeuronRecipe() { }

    public NeuronRecipe(uint id, ushort vqCode, uint seed)
    {
        Id = id;
        VqCode = vqCode;
        Seed = seed;
    }
}

/// <summary>
/// Deterministic regeneration of a neuron's receptive field from its recipe.
/// Ported from <c>ProceduralReceptiveField</c>, with Guid identity replaced by
/// uint and doubles by float32 (§7).
/// </summary>
public static class Regeneration
{
    /// <summary>
    /// Scales codebook components into the weight range the activation model
    /// expects. Ported constant — changing it changes every regenerated weight and
    /// invalidates every stored deviation.
    /// </summary>
    public const float BaselineGain = 45.0f;

    /// <summary>
    /// Does this neuron listen to this input line?
    ///
    /// Drawn from the neuron's VQ PROTOTYPE, narrowed by an identity hash — not
    /// from identity alone. The legacy P3.4 regression is the reason: when fields
    /// became purely identity-determined, every neuron overlapped every cue by the
    /// same ~8 lines and AUC fell 0.91–0.99 → 0.837–0.933. Before that the field
    /// was implicitly HISTORY-determined and the history was doing the
    /// discriminating. History cannot be regenerated; a prototype can. So a neuron
    /// hears what it is for.
    /// </summary>
    public static bool Listens(uint neuronId, ushort vqCode, int dim, VqCodebook codebook, float significance = 0.4f)
    {
        float component = MathF.Abs(codebook.Component(vqCode, dim));
        float scale = codebook.Dim > 0 ? 1f / MathF.Sqrt(codebook.Dim) : 1f;

        // Prototype-significant dims are likely; the identity hash decides which of
        // them this particular neuron takes, so neurons sharing a code still differ.
        float probability = MathF.Min(0.95f, significance + component / (scale + 1e-6f) * 0.1f);
        return Rng.NextFloat(unchecked((int)neuronId), Rng.Purpose.ReceptiveField, (uint)dim) < probability;
    }

    /// <summary>
    /// Baseline weight for (neuron, dim), derived from the VQ prototype.
    /// Deterministic, so it never needs storing.
    /// </summary>
    public static float BaselineWeight(uint neuronId, ushort vqCode, int dim, VqCodebook codebook)
    {
        float component = codebook.Component(vqCode, dim);
        float aligned = MathF.Max(0, component);
        float opposed = MathF.Max(0, -component);
        float w = (aligned + opposed * 0.15f) * BaselineGain;

        // Identity jitter so neurons sharing a VQ code are not exact clones — the
        // legacy P1.6n failure mode.
        float jitter = 0.85f + 0.30f * Rng.NextFloat(unchecked((int)neuronId), Rng.Purpose.NeuronSeed, (uint)dim);
        return MathF.Max(0.5f, w * jitter);
    }

    /// <summary>
    /// Full regeneration: prototype + deviations, written into <paramref name="weights"/>.
    /// Dims the neuron does not listen to are left at zero.
    /// </summary>
    public static void Regenerate(NeuronRecipe recipe, VqCodebook codebook, float[] weights)
    {
        Array.Clear(weights);

        for (int d = 0; d < codebook.Dim && d < weights.Length; d++)
            if (Listens(recipe.Id, recipe.VqCode, d, codebook))
                weights[d] = BaselineWeight(recipe.Id, recipe.VqCode, d, codebook);

        for (int i = 0; i < recipe.DeviationDims.Length; i++)
        {
            int d = recipe.DeviationDims[i];
            if (d < weights.Length) weights[d] += recipe.DeviationDeltas[i];
        }
    }

    /// <summary>
    /// Consolidation (§4.4 step 6): diff learned weights against what regeneration
    /// would produce, and keep only what moved beyond the threshold. Unchanged
    /// neurons cost zero bytes.
    /// </summary>
    public static void Consolidate(NeuronRecipe recipe, VqCodebook codebook, float[] learnedWeights, float threshold)
    {
        var dims = new List<ushort>();
        var deltas = new List<float>();

        for (int d = 0; d < learnedWeights.Length && d < codebook.Dim; d++)
        {
            float baseline = Listens(recipe.Id, recipe.VqCode, d, codebook)
                ? BaselineWeight(recipe.Id, recipe.VqCode, d, codebook)
                : 0f;

            float delta = learnedWeights[d] - baseline;
            if (MathF.Abs(delta) < threshold) continue;

            dims.Add((ushort)d);
            deltas.Add(delta);
        }

        recipe.DeviationDims = dims.ToArray();
        recipe.DeviationDeltas = deltas.ToArray();
    }
}
