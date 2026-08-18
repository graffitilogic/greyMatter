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

    /// <summary>
    /// P7.1b — how many of a neuron's <see cref="SynapseCapPerNeuron"/> slots
    /// within-assembly edges may occupy. The remainder is reserved for
    /// cross-assembly and cross-cue edges.
    ///
    /// P7.0.1 measured within-assembly at 99.9% of live slots with the segment
    /// 100.0% full, and exactly zero cross-assembly synapses ever created. P7.1a then
    /// showed that unsaturating k-WTA raises cross-assembly PROPOSALS from 17,410 to
    /// 420 million while still creating zero, because a candidate born at 0.11 cannot
    /// displace an incumbent in a full segment. Proposals are not the bottleneck;
    /// slots are.
    ///
    /// A value ≥ SynapseCapPerNeuron reproduces pre-P7.1 behaviour exactly.
    /// </summary>
    public int WithinAssemblyCap { get; set; } = 8;   // P7.1 adopted default; >= SynapseCapPerNeuron = pre-P7.1

    /// <summary>
    /// P7.2 — heterosynaptic erosion rate. See SynapseStore.ContestErosion.
    /// 0 reproduces pre-P7.2 behaviour.
    /// </summary>
    public double ContestErosion { get; set; }

    /// <summary>
    /// P8c — base-rate depression coefficient λ in Δw = η·a_s·a_t − λ·a_s·rate(t).
    ///
    /// Hebbian coactivation accumulates weight in proportion to count(s,t), so a
    /// frequent successor wins every ranking simply by entering more coactivation
    /// events. PMI needs count(s,t) divided by count(t); this subtracts the target's
    /// marginal rate so the rule measures covariance rather than co-occurrence.
    ///
    /// 0 reproduces pre-P8c behaviour.
    /// </summary>
    public double BaseRateDepression { get; set; }

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
    public int PropagatedWinnerQuota { get; set; } = 64;   // P7.1 adopted default; 0 = pre-P7.1

    /// <summary>
    /// P8a — fraction of an assembly's members drawn from its code's ACTIVE DIMS
    /// (therefore shared with any code containing those dims) rather than from its
    /// code hash (therefore private).
    ///
    /// P7.2.8 measured the problem this addresses: `to → be`, one of the most
    /// frequent bigrams in English, has 7,164 synapses across 256 fully-resident
    /// members and ZERO edges into `be`'s assembly. With hash-disjoint assemblies,
    /// whether two words have any synaptic path at all is close to a lottery, and
    /// Hebbian learning cannot encode an association across neuron sets that never
    /// form an edge.
    ///
    /// 0 reproduces current behaviour exactly. 1 reproduces the P4.3 defect-4 failure
    /// mode, where every word shared ~216 of 256 members and no word was ever
    /// untrained — which is why this is a dial and not a switch.
    /// </summary>
    public double AssemblyOverlap { get; set; }

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

    /// <summary>
    /// A copy for a single experiment arm. MemberwiseClone rather than a hand-written
    /// member list: every eval that sweeps a parameter needs one of these, and five
    /// hand-written copies had already silently dropped two newly-added fields
    /// (AssemblyOverlap, BaseRateDepression), which makes a sweep quietly measure the
    /// default instead of the swept value. Every field is a value type or string, so
    /// a shallow copy is a complete one.
    /// </summary>
    public Config Clone() => (Config)MemberwiseClone();

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
