using System.Text.Json;

namespace GreyMatter.Poc;

/// <summary>
/// plan.md §4.5 — one flat record, JSON-loadable, every field CLI-overridable.
/// The scale sweep (P6) varies exactly these fields; nothing is hard-coded
/// anywhere else in the project.
/// </summary>
public sealed class Config
{
    // ── Virtual space and working set (§4.1) ──
    public int BaselineNeuronCount { get; set; } = 1_000_000;
    public int WorkingSetMax { get; set; } = 100_000;
    public int SynapseCapPerNeuron { get; set; } = 32;

    // ── Activation scope (§4.4) ──
    public int ActivationDepth { get; set; } = 4;
    public int ActivationWidth { get; set; } = 256;

    /// <summary>
    /// P7.1a — k-WTA slots reserved for PROPAGATED (hop ≥ 1) neurons.
    ///
    /// P7.0.4 measured the root cause of the association gap: assembly size
    /// (Sparsity × NeuronsPerDim = 256) exactly equals ActivationWidth (256), so the
    /// cue's own assembly starts at potential 1.0 and fills every winner slot —
    /// 4,064 hop-0 winners against 32 hop-1 winners across 16 cues. A propagated
    /// neuron essentially cannot win, so it never enters the Hebbian pairing, so no
    /// cross-assembly synapse is ever created (measured: exactly 0).
    ///
    /// This reserves slots that only propagated neurons may occupy. Unfilled reserved
    /// slots fall back to assembly members, so the quota costs nothing when there is
    /// nothing propagated to put in it.
    ///
    /// Default 0 reproduces pre-P7.1 behaviour exactly.
    /// </summary>
    public int PropagatedWinnerQuota { get; set; }

    // ── Encoding (§4.2) ──
    public int PatternSize { get; set; } = 2048;   // n
    public int Sparsity { get; set; } = 32;        // k
    public double ContextBlend { get; set; } = 0.5; // β; 0 ⇒ surface null model
    public int SurfaceDimensions { get; set; } = 128; // legacy FeatureEncoder width

    // ── Engrams (§4.3) ──
    public int VqCodebookSize { get; set; } = 512;

    /// <summary>
    /// Ported legacy default (<c>ProceduralReceptiveField.DefaultDeviationThreshold</c>
    /// = 1.0), as §4.5 requires. This is the persistence budget dial: raise it and
    /// fewer deviations persist (smaller, lossier); lower it and more do. Weights
    /// are O(45), so a threshold of 1.0 is roughly 2% of a typical weight.
    /// </summary>
    public double DeviationThreshold { get; set; } = 1.0;

    // ── Run control ──
    public int Seed { get; set; } = 12345;
    public string BrainDataPath { get; set; } = "/Volumes/jarvis/brainData_poc";
    public string TrainingDataRoot { get; set; } = "/Volumes/jarvis/trainData";
    public string Dataset { get; set; } = "tatoeba_small";

    public static Config Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return new Config();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Config>(json, JsonOpts) ?? new Config();
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOpts);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Apply --key value overrides for any property above, so an experiment's
    /// full configuration is always visible on the command line that produced it
    /// (rule 9: every number in RESULTS.md carries its command line).
    /// </summary>
    public void ApplyOverrides(Args args)
    {
        foreach (var prop in typeof(Config).GetProperties())
        {
            var flag = "--" + ToKebab(prop.Name);
            var raw = args.Value(flag, null);
            if (raw is null) continue;

            object value = prop.PropertyType switch
            {
                var t when t == typeof(int) => int.Parse(raw),
                var t when t == typeof(double) => double.Parse(raw),
                var t when t == typeof(string) => raw,
                _ => throw new NotSupportedException($"Config type {prop.PropertyType} not overridable")
            };
            prop.SetValue(this, value);
        }
    }

    internal static string ToKebab(string name)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i])) sb.Append('-');
            sb.Append(char.ToLowerInvariant(name[i]));
        }
        return sb.ToString();
    }
}
