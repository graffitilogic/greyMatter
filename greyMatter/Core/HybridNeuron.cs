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
        public HashSet<string> AssociatedConcepts { get; private set; } = new();
        public double ImportanceScore { get; private set; } = 0.0;

        // --- New: Short-term learning (STM) buffers and salience tracking ---
        // Accumulates transient updates which can be consolidated into LTM
        public Dictionary<Guid, double> StmWeightDeltas { get; private set; } = new();
        public double StmBiasDelta { get; private set; } = 0.0;
        public double StmSalience { get; private set; } = 0.0; // magnitude-based salience
        public DateTime LastTagTime { get; private set; } = DateTime.MinValue;
        public bool HasPendingStm => StmWeightDeltas.Count > 0 || Math.Abs(StmBiasDelta) > 1e-9;

        public HybridNeuron(string conceptTag = "")
        {
            ConceptTag = conceptTag;
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
                Bias += StmBiasDelta;
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
                    InputWeights[id] += d;

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
                InputWeights[targetNeuronId] = initialWeight;
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
            AssociatedConcepts.Add(concept.ToLowerInvariant());
            UpdateConceptTag();
        }

        private void UpdateConceptTag()
        {
            if (AssociatedConcepts.Any())
                ConceptTag = string.Join(",", AssociatedConcepts.Take(3));
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
        public NeuronSnapshot CreateSnapshot()
        {
            return new NeuronSnapshot
            {
                Id = Id,
                ConceptTag = ConceptTag,
                AssociatedConcepts = AssociatedConcepts.ToList(),
                ImportanceScore = ImportanceScore,
                ActivationCount = ActivationCount,
                LastUsed = LastUsed,
                InputWeights = InputWeights.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                Bias = Bias,
                Threshold = Threshold,
                LearningRate = LearningRate
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
                ImportanceScore = snapshot.ImportanceScore
            };
            
            neuron.InputWeights = snapshot.InputWeights.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            neuron.AssociatedConcepts = snapshot.AssociatedConcepts.ToHashSet();
            
            return neuron;
        }
    }

    /// <summary>
    /// Lightweight snapshot for persistence
    /// </summary>
    public class NeuronSnapshot
    {
        public Guid Id { get; set; }
        public string ConceptTag { get; set; } = "";
        public List<string> AssociatedConcepts { get; set; } = new();
        public double ImportanceScore { get; set; }
        public int ActivationCount { get; set; }
        public DateTime LastUsed { get; set; }
        public Dictionary<Guid, double> InputWeights { get; set; } = new();
        public double Bias { get; set; }
        public double Threshold { get; set; }
        public double LearningRate { get; set; }
    }
}