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

    // ── Encoding (§4.2) ──
    public int PatternSize { get; set; } = 2048;   // n
    public int Sparsity { get; set; } = 32;        // k
    public double ContextBlend { get; set; } = 0.5; // β; 0 ⇒ surface null model
    public int SurfaceDimensions { get; set; } = 128; // legacy FeatureEncoder width

    // ── Engrams (§4.3) ──
    public int VqCodebookSize { get; set; } = 512;
    public double DeviationThreshold { get; set; } = 0.01;

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
