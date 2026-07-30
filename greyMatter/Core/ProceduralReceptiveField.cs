using System;
using System.Globalization;

namespace GreyMatter.Core
{
    /// <summary>
    /// P3 — the piece that makes procedural generation load-bearing.
    ///
    /// Until now a neuron's receptive-field SHAPE was generated (P1.7's
    /// deterministic sparse subset) but its WEIGHTS were persisted verbatim.
    /// Measurement put the result at 1.9% procedural content: 4 bytes of VQ code
    /// against ~208 bytes of explicitly stored weights. Nothing about recall
    /// depended on the VQ code, so the regeneration experiment had no failure mode
    /// and returned 100% no matter what.
    ///
    /// Here a neuron is BORN as its VQ prototype: baseline weights are derived from
    /// codebook[VqCode] restricted to the dims the neuron samples. Learning then
    /// moves it away from that prototype, and only the DEVIATION needs storing.
    ///
    /// Persisted size therefore becomes a function of how much a neuron actually
    /// learned, not of how many inputs it has — which is the thesis, and which
    /// finally gives fidelity something it can lose.
    /// </summary>
    public static class ProceduralReceptiveField
    {
        /// <summary>
        /// Scales codebook components into the weight range the activation model
        /// expects. Codebook vectors are unit-ish; weights need to be O(10) to
        /// produce the potentials the rest of the system is calibrated for.
        /// </summary>
        public const double BaselineGain = 45.0;

        /// <summary>
        /// Weight is stored only when it has drifted further than this from the
        /// generated baseline. This is the persistence budget dial: raise it and
        /// fewer deviations persist (smaller, lossier); lower it and more do.
        /// Sweeping it is how the fidelity-vs-storage curve gets plotted.
        /// </summary>
        public const double DefaultDeviationThreshold = 1.0;

        /// <summary>
        /// Baseline weight for (neuron, feature line), derived from the neuron's VQ
        /// prototype. Deterministic: same neuron + same code + same feature always
        /// yields the same value, so it never needs storing.
        ///
        /// `cf_{dim}_p` / `cf_{dim}_n` map onto codebook dimension `dim` — the
        /// neuron's preferred pattern read off directly. Context lines have no
        /// codebook dimension and fall back to an identity-derived value, which
        /// keeps them deterministic without pretending they encode the prototype.
        /// </summary>
        public static double GenerateBaselineWeight(Guid neuronId, string featureKey, float[]? codebookVector)
        {
            if (codebookVector != null && TryParseConceptDim(featureKey, out var dim, out var positive)
                && dim >= 0 && dim < codebookVector.Length)
            {
                var component = codebookVector[dim];
                // ON/OFF lines carry |v|; a neuron tuned to this prototype should
                // weight the matching polarity strongly and the opposite weakly.
                var aligned = positive ? Math.Max(0, component) : Math.Max(0, -component);
                var opposed = positive ? Math.Max(0, -component) : Math.Max(0, component);
                var w = (aligned * 1.0 + opposed * 0.15) * BaselineGain;

                // Small identity-derived jitter so neurons sharing a VQ code are not
                // exact clones — the P1.6n failure mode.
                var jitter = 0.85 + 0.30 * UnitHash(neuronId, featureKey);
                return Math.Max(0.5, w * jitter);
            }

            // Context lines: deterministic, modest, no prototype meaning.
            return 3.0 + 6.0 * UnitHash(neuronId, featureKey);
        }

        /// <summary>Parse "cf_{dim}_p" / "cf_{dim}_n".</summary>
        public static bool TryParseConceptDim(string featureKey, out int dim, out bool positive)
        {
            dim = -1; positive = true;
            if (featureKey == null || !featureKey.StartsWith("cf_", StringComparison.Ordinal)) return false;

            var lastUnderscore = featureKey.LastIndexOf('_');
            if (lastUnderscore <= 3) return false;

            var polarity = featureKey.AsSpan(lastUnderscore + 1);
            if (polarity.Length != 1) return false;
            positive = polarity[0] == 'p';
            if (!positive && polarity[0] != 'n') return false;

            var dimSpan = featureKey.AsSpan(3, lastUnderscore - 3);
            return int.TryParse(dimSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out dim);
        }

        /// <summary>Deterministic [0,1) from (neuron, feature) — FNV-1a + avalanche.</summary>
        public static double UnitHash(Guid neuronId, string featureKey)
        {
            unchecked
            {
                const uint fnvOffset = 2166136261, fnvPrime = 16777619;
                uint h = fnvOffset;
                Span<byte> bytes = stackalloc byte[16];
                neuronId.TryWriteBytes(bytes);
                foreach (var b in bytes) { h ^= b; h *= fnvPrime; }
                foreach (var c in featureKey) { h ^= (byte)c; h *= fnvPrime; h ^= (byte)(c >> 8); h *= fnvPrime; }
                h ^= h >> 16; h *= 0x85ebca6b;
                h ^= h >> 13; h *= 0xc2b2ae35;
                h ^= h >> 16;
                return h / (double)uint.MaxValue;
            }
        }
    }
}
