using System;
using System.Collections.Generic;
using System.Linq;

namespace GreyMatter.Core
{
    /// <summary>
    /// HybridNeuron: Splits the difference between Artificial and Biological neurons
    /// Features: Dynamic threshold, fatigue, state persistence, sparse connections
    /// </summary>
    public class HybridNeuron
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        
        // Biological-inspired properties
        public double RestingPotential { get; set; } = -70.0;
        public double Threshold { get; set; } = -69.0; // Very very sensitive threshold
        public double CurrentPotential { get; private set; }
        public double Fatigue { get; private set; } = 0.0;
        public DateTime LastActivation { get; private set; } = DateTime.MinValue;
        
        // Artificial network properties
        public double Bias { get; set; } = 0.0;
        public double LearningRate { get; set; } = 0.1; // Increased learning rate
        
        // State management
        public bool IsActive => CurrentPotential > Threshold;
        public bool IsExhausted => Fatigue > 0.8;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;
        public int ActivationCount { get; private set; } = 0;
        
        // Sparse connections - only store non-zero weights
        public Dictionary<Guid, double> InputWeights { get; private set; } = new();
        public HashSet<Guid> OutputConnections { get; private set; } = new();
        
        // Metadata for clustering and persistence
        public string ConceptTag { get; set; } = "";

        /// <summary>
        /// P4.5: the concept this neuron was ALLOCATED for — the first one
        /// associated, never overwritten. Distinct from AssociatedConcepts, which
        /// also accumulates the owning cluster's ConceptDomain (see
        /// NeuronCluster.AddNeuronAsync) and any other context the neuron meets.
        ///
        /// This exists because ConceptTag used to be a JOIN of up to three
        /// concepts, and procedural regeneration rebuilt AssociatedConcepts from
        /// that join as a single literal string ("the,pattern_a1b2c3"). Concept
        /// identity therefore did not survive a save/load round trip.
        /// </summary>
        public string PrimaryConcept { get; private set; } = "";

        // Case-insensitive: concepts are lowercased on association, but callers
        // (debugLabel, cue words) are not guaranteed to be.
        public HashSet<string> AssociatedConcepts { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        public double ImportanceScore { get; private set; } = 0.0;

        // New: Provisional flag (STM-only neuron not yet consolidated to LTM)
        public bool IsProvisional { get; set; } = false;
        
        // Phase 6B: VQ code for procedural regeneration (No Man's Sky principle)
        public int? VqCode { get; set; } = null;

        /// <summary>
        /// W6 — familiarity trace. Running mean of the `MatchQuality` values that
        /// actually caused this neuron to fire.
        ///
        /// Why not `ActivationCount`, which is already persisted and free:
        /// a count is a per-neuron CONSTANT. It shifts every cue's score by the
        /// same amount regardless of what is being probed, so trained and novel
        /// cues move together and d′ cannot change. That is precisely the null
        /// this experiment has to avoid. A discriminative trace must interact with
        /// the current input.
        ///
        /// Why this one can discriminate: `ReinforceTowardInput` applies a Kohonen
        /// step, so a neuron's weights are `prototype + drift toward the words that
        /// trained it`. A trained cue matches prototype AND drift and lands near
        /// this mean; a novel cue sharing the VQ code matches only the prototype
        /// and should land below it.
        ///
        /// Cost: 4 bytes/neuron, persisted. Unlike ActivationCount this is NEW
        /// state — it is not inside the existing 64 B floor.
        /// </summary>
        public float MeanFiringMatch { get; private set; } = 0f;

        /// Below this much history the trace is noise, so no adjustment is applied.
        public const int MinHistoryForFamiliarity = 5;

        /// <summary>
        /// W6 — recall-path score. `MatchQuality` is deliberately left untouched
        /// (ground rule 9: it is read in many places, including training).
        ///
        /// Penalises only inputs falling BELOW what this neuron habitually
        /// responds to. A trained cue sitting at its own historical mean is
        /// unaffected; a novel cue that merely shares the VQ prototype should sit
        /// under it and lose ground.
        ///
        /// Reachable null, stated in the pre-registration: if MeanFiringMatch is
        /// uniform across neurons, this is a constant shift, trained and novel
        /// cues are penalised equally, and d′ does not move.
        /// </summary>
        public double FamiliarityAdjustedMatch(
            Dictionary<Guid, double> inputs,
            HashSet<Guid>? featureInputIds = null,
            double lambda = 1.0,
            Action<double>? recordPenalty = null)
        {
            var match = MatchQuality(inputs, featureInputIds);
            if (ActivationCount < MinHistoryForFamiliarity || MeanFiringMatch <= 0)
            {
                recordPenalty?.Invoke(0.0);
                return match;
            }

            var penalty = Math.Max(0.0, MeanFiringMatch - match);
            recordPenalty?.Invoke(penalty);
            return Math.Max(0.0, match - lambda * penalty);
        }

        /// <summary>
        /// P3.4: how many feature lines existed the last time this neuron's
        /// receptive field was wired. A neuron's field is defined by its IDENTITY
        /// (which lines it samples), not by which words it happened to meet — so it
        /// must be re-wired whenever the vocabulary grows, or the in-memory field
        /// stays smaller than the one regeneration reconstructs.
        /// Not persisted: regeneration always rebuilds the complete field.
        /// </summary>
        public int LastWiredFeatureCount { get; set; } = 0;

        // --- New: Short-term learning (STM) buffers and salience tracking ---
        // Accumulates transient updates which can be consolidated into LTM
        public Dictionary<Guid, double> StmWeightDeltas { get; private set; } = new();
        public double StmBiasDelta { get; private set; } = 0.0;
        public double StmSalience { get; private set; } = 0.0; // magnitude-based salience
        public DateTime LastTagTime { get; private set; } = DateTime.MinValue;
        public bool HasPendingStm => StmWeightDeltas.Count > 0 || Math.Abs(StmBiasDelta) > 1e-9;

        public HybridNeuron(string conceptTag = "")
        {
            // Legacy data may carry a comma-joined tag; the allocation concept is
            // the first element. Harmless for well-formed single-token tags.
            ConceptTag = conceptTag?.Split(',')[0].Trim() ?? "";
            PrimaryConcept = ConceptTag.ToLowerInvariant();
            CurrentPotential = RestingPotential;
        }

        /// <summary>
        /// Process inputs and determine if neuron fires
        /// Combines weighted sum (ANN) with fatigue and dynamic threshold (BNN-inspired)
        /// </summary>
        public double ProcessInputs(Dictionary<Guid, double> inputs)
        {
            LastUsed = DateTime.UtcNow;
            
            // Calculate weighted sum of inputs
            double weightedSum = Bias;
            foreach (var input in inputs)
            {
                if (InputWeights.ContainsKey(input.Key))
                {
                    weightedSum += input.Value * InputWeights[input.Key];
                }
            }
            
            // Apply fatigue - tired neurons need stronger stimuli
            double adjustedThreshold = Threshold + (Fatigue * 10.0);
            
            // Update potential
            CurrentPotential = RestingPotential + weightedSum;
            
            // Check if firing
            if (CurrentPotential > adjustedThreshold && !IsExhausted)
            {
                Fire();
                return Activate(CurrentPotential);
            }
            
            // Gradual return to resting potential
            CurrentPotential = RestingPotential + (CurrentPotential - RestingPotential) * 0.9;
            
            return 0.0;
        }

        private void Fire()
        {
            LastActivation = DateTime.UtcNow;
            ActivationCount++;
            
            // Increase fatigue with each activation
            Fatigue = Math.Min(1.0, Fatigue + 0.1);
            
            // Update importance based on usage
            ImportanceScore = CalculateImportance();
        }

        private double Activate(double potential)
        {
            // Sigmoid-like activation but with biological constraints
            double normalizedPotential = (potential - RestingPotential) / (Threshold - RestingPotential);
            return Math.Tanh(normalizedPotential * 2.0); // Range: -1 to 1
        }

        /// <summary>
        /// Rest period - neurons recover fatigue over time
        /// </summary>
        public void Rest(TimeSpan timePassed)
        {
            // Fatigue recovery
            double recoveryRate = 0.1 * timePassed.TotalMinutes;
            Fatigue = Math.Max(0.0, Fatigue - recoveryRate);
            
            // Gradual return to resting potential
            CurrentPotential = RestingPotential;
        }

        /// <summary>
        /// P2.1 — how well does this input pattern match what this neuron is tuned to?
        ///
        /// Cosine similarity between the driven input lines and this neuron's weights
        /// over those same lines. Range [0,1] for non-negative inputs (guaranteed by
        /// the ON/OFF rectification in BuildTrainingFeatures).
        ///
        /// Replaces the raw weighted sum for recognition. The dot product alone
        /// measures MAGNITUDE, so with density-compensated weights (7.5–22.5) a cue
        /// driving 3 of a neuron's 8 inputs still summed to ~18 and saturated —
        /// which is why "qwertyuiop" scored 0.993, above every real word.
        /// Cosine measures ALIGNMENT: partial overlap yields a partial score.
        ///
        /// Only keys present in `inputs` participate, so synaptic entries in
        /// InputWeights (other neurons' IDs) are naturally excluded — they are never
        /// input lines.
        /// </summary>
        public double MatchQuality(Dictionary<Guid, double> inputs, HashSet<Guid>? featureInputIds = null)
        {
            if (inputs.Count == 0) return 0.0;

            double dot = 0, xNorm = 0;
            foreach (var input in inputs)
            {
                var x = input.Value;
                xNorm += x * x;
                if (InputWeights.TryGetValue(input.Key, out var w))
                    dot += x * w;
            }
            if (dot <= 0 || xNorm <= 0) return 0.0;

            // ||w|| must span the neuron's WHOLE receptive field, not just the part
            // that happens to overlap this cue.
            //
            // P2.1 BUG (fixed here): wNorm was accumulated only inside the loop
            // above, i.e. only over overlapping keys. A neuron with 8 input lines of
            // which a cue drove just 1 was normalised by that single weight and
            // scored as if it had matched perfectly. Every cue — word or gibberish —
            // therefore landed in a narrow 0.55–0.68 band and the discrimination
            // margin measured 0.024.
            //
            // With the full receptive field in the denominator, "how much of what I
            // listen for is actually present" becomes the quantity, and a cue that
            // drives different input lines scores near zero.
            double wNorm = 0;
            foreach (var kvp in InputWeights)
            {
                // Synapses share this dictionary with feature inputs; only feature
                // lines are part of the receptive field.
                if (featureInputIds != null && !featureInputIds.Contains(kvp.Key)) continue;
                wNorm += kvp.Value * kvp.Value;
            }
            if (wNorm <= 0) return 0.0;

            return Math.Clamp(dot / (Math.Sqrt(wNorm) * Math.Sqrt(xNorm)), 0.0, 1.0);
        }

        /// <summary>
        /// P2.1 — competitive Hebbian update (Kohonen form): move this neuron's
        /// weights toward the input pattern it just won, then rescale so total
        /// synaptic strength is conserved (Turrigiano synaptic scaling).
        ///
        /// This is the biological substitute for the supervised delta rule, which
        /// trained EVERY neuron toward a constant target of 0.8 and therefore had
        /// "respond to everything" as its fixed point. Here only winners learn
        /// (lateral inhibition), and scaling means a neuron cannot become dominant
        /// by growing weights — it can only become better MATCHED. Selectivity is
        /// the direct consequence: a neuron that wins for "the" moves toward "the"
        /// and is never moved toward "water".
        ///
        /// Recorded as STM deltas so the existing consolidation path still governs
        /// what becomes permanent.
        /// </summary>
        public void ReinforceTowardInput(Dictionary<Guid, double> inputs, double rate,
                                         double? firedAtMatch = null)
        {
            if (inputs.Count == 0) return;

            // Preserve the neuron's existing total strength over the driven lines
            double strengthBefore = 0;
            foreach (var input in inputs)
                if (InputWeights.TryGetValue(input.Key, out var w0)) strengthBefore += Math.Abs(w0);

            // Kohonen step toward the input, accumulated as STM
            double strengthAfter = 0;
            var proposed = new Dictionary<Guid, double>(inputs.Count);
            foreach (var input in inputs)
            {
                if (!InputWeights.TryGetValue(input.Key, out var w)) continue;
                var target = w + rate * (input.Value - w);
                proposed[input.Key] = target;
                strengthAfter += Math.Abs(target);
            }
            if (proposed.Count == 0 || strengthAfter <= 0) return;

            // Winning the competition IS firing: record it, so ActivationCount and
            // ImportanceScore keep tracking usage now that ProcessInputs is no
            // longer called on the training path.
            LastActivation = DateTime.UtcNow;
            ActivationCount++;
            Fatigue = Math.Min(1.0, Fatigue + 0.1);
            ImportanceScore = CalculateImportance();

            // W6: running mean of the match values that actually made this neuron
            // fire. Updated here because winning the competition IS firing.
            //
            // The caller MUST pass the match it already computed. Recomputing it
            // here as MatchQuality(inputs) would normalise ‖w‖ over all of
            // InputWeights — which mixes feature lines with synapses to other
            // neurons (the P1.6m trap) — while recall passes featureNeuronIds and
            // normalises over the receptive field only. The two scales differ, the
            // stored mean would sit systematically below every recall-time match,
            // the penalty would always be 0, and the experiment would return its
            // own null by construction rather than by measurement.
            if (firedAtMatch is double m)
            {
                MeanFiringMatch = ActivationCount <= 1
                    ? (float)m
                    : MeanFiringMatch + (float)((m - MeanFiringMatch) / ActivationCount);
            }

            // Synaptic scaling: conserve total strength across the receptive field
            var scale = strengthBefore > 0 ? strengthBefore / strengthAfter : 1.0;
            foreach (var kvp in proposed)
            {
                var scaled = kvp.Value * scale;
                var delta = SanitizeDouble(scaled - InputWeights[kvp.Key], 0.0, $"Neuron {Id} reinforce");
                if (Math.Abs(delta) <= 0) continue;

                if (!StmWeightDeltas.ContainsKey(kvp.Key)) StmWeightDeltas[kvp.Key] = 0.0;
                StmWeightDeltas[kvp.Key] += delta;
                StmSalience += Math.Abs(delta);
                LastTagTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Learn by adjusting connection weights
        /// Now defaults to recording Short-Term (STM) deltas; consolidation promotes to LTM.
        /// </summary>
        public void Learn(Guid inputNeuronId, double inputValue, double expectedOutput, double actualOutput)
        {
            LearnStm(inputNeuronId, inputValue, expectedOutput, actualOutput);
        }

        /// <summary>
        /// Record short-term delta and salience (eligibility trace style)
        /// </summary>
        public void LearnStm(Guid inputNeuronId, double inputValue, double expectedOutput, double actualOutput)
        {
            double error = expectedOutput - actualOutput;
            double delta = LearningRate * error * inputValue;
            
            // CRITICAL: Sanitize delta to prevent NaN/Infinity from propagating
            delta = SanitizeDouble(delta, 0.0, $"Neuron {Id} LearnStm delta");

            if (Math.Abs(delta) > 0)
            {
                if (!StmWeightDeltas.ContainsKey(inputNeuronId))
                    StmWeightDeltas[inputNeuronId] = 0.0;
                StmWeightDeltas[inputNeuronId] += delta;
                StmSalience += Math.Abs(delta);
                LastTagTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Consolidate STM into long-term weights if above epsilon; returns true if LTM changed.
        /// </summary>
        public bool ConsolidateToLtm(double epsilon = 1e-3)
        {
            bool changed = false;

            if (Math.Abs(StmBiasDelta) >= epsilon)
            {
                Bias = SanitizeDouble(Bias + StmBiasDelta, 0.0, $"Neuron {Id} bias update");
                StmBiasDelta = 0.0;
                changed = true;
            }

            if (StmWeightDeltas.Count > 0)
            {
                // Apply deltas and prune tiny weights
                foreach (var kvp in StmWeightDeltas)
                {
                    var id = kvp.Key;
                    var d = kvp.Value;
                    if (Math.Abs(d) < epsilon) continue;

                    if (!InputWeights.ContainsKey(id))
                        InputWeights[id] = 0.0;
                    
                    // CRITICAL: Sanitize before and after addition
                    var newWeight = SanitizeDouble(InputWeights[id] + d, 0.0, $"Neuron {Id} weight update");
                    InputWeights[id] = newWeight;

                    if (Math.Abs(InputWeights[id]) < 0.001)
                        InputWeights.Remove(id);

                    changed = true;
                }

                // Clear STM buffer and decay salience
                StmWeightDeltas.Clear();
                StmSalience *= 0.5; // decay remaining salience
            }

            return changed;
        }

        /// <summary>
        /// Add connection to another neuron
        /// </summary>
        public void ConnectTo(Guid targetNeuronId, double initialWeight = 0.1)
        {
            OutputConnections.Add(targetNeuronId);
            if (!InputWeights.ContainsKey(targetNeuronId))
                InputWeights[targetNeuronId] = SanitizeDouble(initialWeight, 0.1, $"Neuron {Id} ConnectTo");
        }

        /// <summary>
        /// Remove weak or unused connections
        /// </summary>
        public void PruneConnections(double weightThreshold = 0.001)
        {
            var weakConnections = InputWeights
                .Where(w => Math.Abs(w.Value) < weightThreshold)
                .Select(w => w.Key)
                .ToList();
            
            foreach (var connectionId in weakConnections)
            {
                InputWeights.Remove(connectionId);
                OutputConnections.Remove(connectionId);
            }
        }

        /// <summary>
        /// Associate this neuron with concepts for clustering
        /// </summary>
        public void AssociateConcept(string concept)
        {
            if (string.IsNullOrWhiteSpace(concept)) return;
            var normalized = concept.ToLowerInvariant();
            AssociatedConcepts.Add(normalized);

            // First association wins and is never revised. Previously this joined
            // the first three concepts with commas, which meant the owning
            // cluster's ConceptDomain ("pattern_a1b2c3") got baked into the tag
            // alongside the word — and the tag is the ONLY concept information
            // procedural regeneration has to work from.
            if (string.IsNullOrEmpty(PrimaryConcept))
                PrimaryConcept = normalized;

            UpdateConceptTag();
        }

        private void UpdateConceptTag()
        {
            if (!string.IsNullOrEmpty(PrimaryConcept))
                ConceptTag = PrimaryConcept;
        }

        private double CalculateImportance()
        {
            // Importance based on: usage frequency, connection count, concept associations
            double usageScore = Math.Log(ActivationCount + 1) / 10.0;
            double connectionScore = (InputWeights.Count + OutputConnections.Count) / 100.0;
            double conceptScore = AssociatedConcepts.Count / 10.0;
            double recentUsage = (DateTime.UtcNow - LastUsed).TotalDays > 7 ? 0.5 : 1.0;
            
            return (usageScore + connectionScore + conceptScore) * recentUsage;
        }

        /// <summary>
        /// Determine if this neuron should be persisted or can be garbage collected
        /// </summary>
        public bool ShouldPersist()
        {
            return ImportanceScore > 0.1 || 
                   ActivationCount > 10 || 
                   AssociatedConcepts.Any() ||
                   (DateTime.UtcNow - LastUsed).TotalDays < 1;
        }

        /// <summary>
        /// Create a lightweight representation for persistence
        /// </summary>
        // Aggressive sanitization: validates that a double is JSON-serializable
        private static double SanitizeDouble(double value, double defaultValue = 0.0, string context = "")
        {
            // Check for NaN or Infinity
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                Console.WriteLine($"[SANITIZE] {context}: NaN/Infinity detected, replacing with {defaultValue}");
                return defaultValue;
            }
            
            // Check for subnormal numbers (very close to zero, can cause JSON issues)
            if (Math.Abs(value) < double.Epsilon * 100)
            {
                if (value != 0.0)
                    Console.WriteLine($"[SANITIZE] {context}: Subnormal detected ({value:E}), replacing with 0.0");
                return 0.0;
            }
            
            // Check for extreme values that might overflow JSON
            if (Math.Abs(value) > 1e308)
            {
                Console.WriteLine($"[SANITIZE] {context}: Extreme value detected ({value:E}), replacing with {defaultValue}");
                return defaultValue;
            }
            
            // CRITICAL: Check for System.Text.Json edge cases
            // Certain bit patterns cause malformed JSON (e.g., numbers ending with quotes)
            var bits = BitConverter.DoubleToInt64Bits(value);
            
            // Check for signaling NaN (different bit pattern than quiet NaN)
            if ((bits & 0x7FF8000000000000) == 0x7FF0000000000000 && (bits & 0x0007FFFFFFFFFFFF) != 0)
            {
                Console.WriteLine($"❌ [SANITIZE] {context}: Signaling NaN detected (0x{bits:X16}), replacing with {defaultValue}");
                return defaultValue;
            }
            
            // CRITICAL: Test round-trip through JSON using Utf8JsonWriter to catch System.Text.Json edge cases
            // Some values pass the above checks but still fail JSON serialization
            try
            {
                // Use Utf8JsonWriter to match actual serialization path
                using var ms = new System.IO.MemoryStream();
                using (var writer = new System.Text.Json.Utf8JsonWriter(ms))
                {
                    writer.WriteNumberValue(value);
                }
                
                // Verify the JSON is valid by parsing it back
                ms.Position = 0;
                var jsonBytes = ms.ToArray();
                var jsonText = System.Text.Encoding.UTF8.GetString(jsonBytes);
                
                // Check for malformed JSON (numbers shouldn't contain quotes)
                if (jsonText.Contains('"'))
                {
                    Console.WriteLine($"❌ [SANITIZE] {context}: Value {value:E} (0x{bits:X16}) produced malformed JSON: '{jsonText}', replacing with {defaultValue}");
                    return defaultValue;
                }
                
                ms.Position = 0;
                using var doc = System.Text.Json.JsonDocument.Parse(ms);
                var roundTrip = doc.RootElement.GetDouble();
                
                // If round-trip produced NaN/Infinity, the value is invalid
                if (double.IsNaN(roundTrip) || double.IsInfinity(roundTrip))
                {
                    Console.WriteLine($"[SANITIZE] {context}: Value {value:E} failed JSON round-trip (became {roundTrip}), replacing with {defaultValue}");
                    return defaultValue;
                }
                
                return value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [SANITIZE] {context}: Value {value:E} (raw bits: {BitConverter.DoubleToInt64Bits(value):X16}) failed JSON serialization ({ex.Message}), replacing with {defaultValue}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Sanitize a string for safe JSON serialization
        /// Removes/replaces control characters and other problematic characters
        /// </summary>
        private static string SanitizeString(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            
            // Fast path: if string only contains safe ASCII printable chars, return as-is
            bool needsSanitization = false;
            foreach (char c in value)
            {
                if (c < 32 || c == 127)  // Control characters including DEL
                {
                    needsSanitization = true;
                    break;
                }
            }
            
            if (!needsSanitization) return value;
            
            // Slow path: rebuild string with safe characters
            var sb = new System.Text.StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (c < 32 || c == 127)
                {
                    // Replace control characters with space (including newline, tab, etc.)
                    // These cause JSON parsing errors when embedded in string values
                    sb.Append(' ');
                }
                else
                {
                    sb.Append(c);
                }
            }
            
            return sb.ToString();
        }

        public NeuronSnapshot CreateSnapshot()
        {
            // CRITICAL: Create defensive copy of InputWeights to prevent concurrent modification
            // Training threads can modify InputWeights during checkpoint save
            Dictionary<Guid, double> weightsCopy;
            lock (InputWeights)
            {
                weightsCopy = new Dictionary<Guid, double>(InputWeights);
            }
            
            // Sanitize weights to prevent JSON serialization errors (NaN, Infinity, subnormals, etc.)
            var sanitizedWeights = new Dictionary<Guid, double>();
            foreach (var kvp in weightsCopy)
            {
                sanitizedWeights[kvp.Key] = SanitizeDouble(kvp.Value, 0.0, $"Neuron {Id} weight {kvp.Key}");
            }
            
            // CRITICAL: Snapshot other fields atomically to prevent concurrent modification
            var conceptsCopy = AssociatedConcepts.ToList();
            var conceptTag = ConceptTag;
            var importance = ImportanceScore;
            var activationCount = ActivationCount;
            var lastUsed = LastUsed;
            var bias = Bias;
            var threshold = Threshold;
            var learningRate = LearningRate;
            var isProvisional = IsProvisional;
            var vqCode = VqCode; // Phase 6B: Capture VQ code for procedural regeneration
            
            // CRITICAL: Sanitize strings to prevent JSON serialization errors
            // Control characters, unescaped quotes, and invalid JSON chars cause parse failures
            var sanitizedConceptTag = SanitizeString(conceptTag);
            var sanitizedConcepts = conceptsCopy.Select(SanitizeString).ToList();
            
            return new NeuronSnapshot
            {
                Id = Id,
                ConceptTag = sanitizedConceptTag,
                AssociatedConcepts = sanitizedConcepts,
                ImportanceScore = SanitizeDouble(importance, 0.0, $"Neuron {Id} importance"),
                ActivationCount = activationCount,
                LastUsed = lastUsed,
                InputWeights = sanitizedWeights,
                Bias = SanitizeDouble(bias, 0.0, $"Neuron {Id} bias"),
                Threshold = SanitizeDouble(threshold, 0.5, $"Neuron {Id} threshold"),
                LearningRate = SanitizeDouble(learningRate, 0.01, $"Neuron {Id} learningRate"),
                IsProvisional = isProvisional,
                VqCode = vqCode, // Phase 6B: Store VQ code for procedural regeneration
                MeanFiringMatch = MeanFiringMatch // W6: familiarity trace (4 bytes)
            };
        }

        /// <summary>
        /// Restore from snapshot
        /// </summary>
        public static HybridNeuron FromSnapshot(NeuronSnapshot snapshot)
        {
            var neuron = new HybridNeuron(snapshot.ConceptTag)
            {
                Bias = snapshot.Bias,
                Threshold = snapshot.Threshold,
                LearningRate = snapshot.LearningRate,
                ActivationCount = snapshot.ActivationCount,
                LastUsed = snapshot.LastUsed,
                ImportanceScore = snapshot.ImportanceScore,
                IsProvisional = snapshot.IsProvisional,
                VqCode = snapshot.VqCode, // Phase 6B: Restore VQ code for procedural regeneration
                MeanFiringMatch = snapshot.MeanFiringMatch // W6: familiarity trace
            };
            // Ensure identity is preserved across loads
            neuron.Id = snapshot.Id;
            
            neuron.InputWeights = snapshot.InputWeights.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // P4.5: split any comma-joined legacy entries so concepts persisted
            // under the old tag scheme resolve to individual words on load,
            // rather than to one literal "the,pattern_a1b2c3" string that no
            // concept lookup can ever match.
            neuron.AssociatedConcepts = snapshot.AssociatedConcepts
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .SelectMany(c => c.Split(','))
                .Select(c => c.Trim().ToLowerInvariant())
                .Where(c => c.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return neuron;
        }
    }

    /// <summary>
    /// Lightweight snapshot for persistence
    /// </summary>
    [MessagePack.MessagePackObject]
    public class NeuronSnapshot
    {
        [MessagePack.Key(0)]
        public Guid Id { get; set; }
        [MessagePack.Key(1)]
        public string ConceptTag { get; set; } = "";
        [MessagePack.Key(2)]
        public List<string> AssociatedConcepts { get; set; } = new();
        [MessagePack.Key(3)]
        public double ImportanceScore { get; set; }
        [MessagePack.Key(4)]
        public int ActivationCount { get; set; }
        [MessagePack.Key(5)]
        public DateTime LastUsed { get; set; }
        [MessagePack.Key(6)]
        public Dictionary<Guid, double> InputWeights { get; set; } = new();
        [MessagePack.Key(7)]
        public double Bias { get; set; }
        [MessagePack.Key(8)]
        public double Threshold { get; set; }
        [MessagePack.Key(9)]
        public double LearningRate { get; set; }
        [MessagePack.Key(10)]
        public bool IsProvisional { get; set; } = false;
        [MessagePack.Key(11)]
        public int? VqCode { get; set; } = null; // Phase 6B: VQ code for procedural regeneration
        // W6: familiarity trace. New key appended — existing keys keep their
        // indices so already-persisted neurons deserialise unchanged (default 0,
        // which FamiliarityAdjustedMatch treats as "no history, no adjustment").
        [MessagePack.Key(12)]
        public float MeanFiringMatch { get; set; } = 0f;
    }
}