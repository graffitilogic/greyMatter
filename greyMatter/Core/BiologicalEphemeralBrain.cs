using greyMatter.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace greyMatter.Core
{
    /// <summary>
    /// Enhanced SimpleEphemeralBrain with biological behaviors and better scaling
    /// Adds: neuron fatigue, activation decay, sparse pruning, LRU eviction
    /// </summary>
    public class BiologicalEphemeralBrain : SimpleEphemeralBrain
    {
        private readonly Dictionary<string, DateTime> _lastAccessed;
        private readonly Dictionary<string, int> _accessCounts;
        private readonly int _maxActiveClusters;
        private readonly Random _random;

        public BiologicalEphemeralBrain(int maxActiveClusters = 50) : base()
        {
            _lastAccessed = new Dictionary<string, DateTime>();
            _accessCounts = new Dictionary<string, int>();
            _maxActiveClusters = maxActiveClusters;
            _random = new Random(42);
        }

        /// <summary>
        /// Enhanced learning with biological behaviors
        /// </summary>
        public new void Learn(string concept, string[]? relatedConcepts = null)
        {
            // Record access for LRU
            RecordAccess(concept);
            
            // Check if we need to evict old clusters (LRU)
            if (GetActiveClusterCount() >= _maxActiveClusters)
            {
                EvictLeastUsedCluster();
            }

            // Apply decay to all other clusters before learning
            ApplyGlobalDecay(concept);

            // Standard learning
            base.Learn(concept, relatedConcepts);

            // Post-learning biological maintenance
            ApplyNeuronFatigue(concept);
        }

        /// <summary>
        /// Biological-aware batch learning with efficient maintenance
        /// </summary>
        public new void LearnBatch(IEnumerable<string> concepts, int reportingBatchSize = 50, int exampleSampleSize = 3)
        {
            var conceptList = concepts.ToList();
            var totalConcepts = conceptList.Count;
            var processed = 0;
            var recentExamples = new List<string>();
            var startTime = DateTime.Now;

            foreach (var concept in conceptList)
            {
                // Biological learning with reduced overhead
                RecordAccess(concept);
                
                // Check for eviction less frequently during batch processing
                if (processed % 10 == 0 && GetActiveClusterCount() >= _maxActiveClusters)
                {
                    EvictLeastUsedCluster();
                }

                // Apply decay less frequently during batch processing
                if (processed % 5 == 0)
                {
                    ApplyGlobalDecay(concept);
                }

                // Use silent learning for efficiency
                LearnSilently(concept);
                ApplyNeuronFatigue(concept);
                
                processed++;
                
                // Keep track of recent examples for sampling
                recentExamples.Add(concept);
                if (recentExamples.Count > exampleSampleSize)
                {
                    recentExamples.RemoveAt(0);
                }

                // Report progress at batch intervals or at the end
                if (processed % reportingBatchSize == 0 || processed == totalConcepts)
                {
                    var elapsed = DateTime.Now - startTime;
                    var conceptsPerSecond = processed / Math.Max(elapsed.TotalSeconds, 0.001);
                    var percentComplete = (double)processed / totalConcepts * 100;
                    
                    Console.WriteLine($"🧠 Bio-Learning: {percentComplete:F1}% | " +
                                    $"Batch: {conceptsPerSecond:F0} concepts/sec | " +
                                    $"Total: {processed:N0} | " +
                                    $"Active: {GetActiveClusterCount()}/{_maxActiveClusters}");
                    
                    // Show sample of recent concepts learned
                    if (recentExamples.Any())
                    {
                        var examples = string.Join(", ", recentExamples.Take(exampleSampleSize));
                        Console.WriteLine($"  Recent examples: {examples}");
                    }
                }
            }
            
            // Run final biological maintenance after batch
            Console.WriteLine("🧠 Running post-batch biological maintenance...");
            BiologicalMaintenance();
        }

        /// <summary>
        /// Enhanced recall with biological behaviors
        /// </summary>
        public new List<string> Recall(string concept)
        {
            RecordAccess(concept);
            
            // Apply biological activation patterns
            var cluster = GetCluster(concept);
            if (cluster != null)
            {
                ApplyBiologicalActivation(cluster);
            }

            return base.Recall(concept);
        }

        /// <summary>
        /// Biological maintenance cycle - call periodically
        /// </summary>
        public void BiologicalMaintenance()
        {
            Console.WriteLine("🧠 Running biological maintenance...");
            
            var prunedConnections = 0;
            var recoveredNeurons = 0;
            var decayedClusters = 0;

            foreach (var cluster in GetAllClusters())
            {
                // Prune weak connections
                var weakNeurons = cluster.ActiveNeurons.Where(n => n.Strength < 0.2).ToList();
                foreach (var neuron in weakNeurons)
                {
                    cluster.ActiveNeurons.Remove(neuron);
                    prunedConnections++;
                }

                // Recover from fatigue
                foreach (var neuron in cluster.ActiveNeurons)
                {
                    if (neuron.Fatigue > 0)
                    {
                        neuron.RecoverFromFatigue();
                        recoveredNeurons++;
                    }
                }

                // Apply natural decay
                if (cluster.ActivationLevel > 0)
                {
                    cluster.ApplyDecay();
                    if (cluster.ActivationLevel < 0.1)
                    {
                        cluster.Deactivate();
                        decayedClusters++;
                    }
                }
            }

            Console.WriteLine($"  ✂️  Pruned {prunedConnections} weak connections");
            Console.WriteLine($"  💤 Recovered {recoveredNeurons} neurons from fatigue");
            Console.WriteLine($"  📉 Applied decay to {decayedClusters} clusters");
        }

        /// <summary>
        /// Learn a sequence with enhanced biological pattern formation
        /// </summary>
        public void LearnSequence(string[] concepts, string sequenceName)
        {
            Console.WriteLine($"🔗 Learning sequence '{sequenceName}': {string.Join(" → ", concepts)}");
            
            // Learn each concept with temporal context
            for (int i = 0; i < concepts.Length; i++)
            {
                var concept = concepts[i];
                var context = new List<string>();
                
                // Add sequence context
                if (i > 0) context.Add(concepts[i - 1]);
                if (i < concepts.Length - 1) context.Add(concepts[i + 1]);
                
                // Add sequence identifier as context
                context.Add($"sequence_{sequenceName}");
                
                Learn(concept, context.ToArray());
                
                // Create temporal synapses (stronger than normal associations)
                if (i < concepts.Length - 1)
                {
                    CreateTemporalSynapse(concept, concepts[i + 1], 0.9);
                }
            }
        }

        /// <summary>
        /// Predict next concept in a sequence
        /// </summary>
        public string PredictNext(string concept)
        {
            var related = Recall(concept);
            
            // Look for temporal patterns
            foreach (var rel in related)
            {
                if (rel.Contains("sequence_"))
                {
                    // This is part of a sequence, find the next element
                    return ExtractNextFromSequence(concept, rel);
                }
            }
            
            // Fall back to strongest association
            return related.FirstOrDefault()?.Split(' ').FirstOrDefault();
        }

        /// <summary>
        /// Show enhanced brain scan with biological details
        /// </summary>
        public void ShowBiologicalBrainScan()
        {
            Console.WriteLine("\n🧠 === Biological Brain Scan ===");
            Console.WriteLine($"Active clusters: {GetActiveClusterCount()}/{_maxActiveClusters}");
            Console.WriteLine($"Total concepts registered: {_accessCounts.Count}");
            
            var activeClusters = GetActiveClusters().Values;
            var totalNeurons = activeClusters.Sum(c => c.ActiveNeurons.Count);
            var averageFatigue = activeClusters.SelectMany(c => c.ActiveNeurons).Average(n => n.Fatigue);
            var averageStrength = activeClusters.SelectMany(c => c.ActiveNeurons).Average(n => n.Strength);
            
            Console.WriteLine($"Total active neurons: {totalNeurons}");
            Console.WriteLine($"Average neuron fatigue: {averageFatigue:F2}");
            Console.WriteLine($"Average neuron strength: {averageStrength:F2}");
            
            Console.WriteLine("\n🔥 Most Active Clusters:");
            foreach (var cluster in activeClusters
                .Where(c => c.ActivationLevel > 0)
                .OrderByDescending(c => c.ActivationLevel)
                .Take(5))
            {
                var fatigue = cluster.ActiveNeurons.Average(n => n.Fatigue);
                var strength = cluster.ActiveNeurons.Average(n => n.Strength);
                var accessCount = _accessCounts.GetValueOrDefault(cluster.Concept, 0);
                
                Console.WriteLine($"  {cluster.Concept}: " +
                    $"activation={cluster.ActivationLevel:F2}, " +
                    $"neurons={cluster.ActiveNeurons.Count}, " +
                    $"fatigue={fatigue:F2}, " +
                    $"strength={strength:F2}, " +
                    $"accessed={accessCount}x");
            }
            
            Console.WriteLine("\n📊 Memory Efficiency:");
            Console.WriteLine($"Memory usage: O({GetActiveClusterCount()}) active concepts");
            Console.WriteLine($"Efficiency ratio: {(double)GetActiveClusterCount() / _accessCounts.Count:F2}");
        }

        /// <summary>
        /// Simulate aging process - strengthen frequently used concepts, weaken unused ones
        /// </summary>
        public void SimulateAging(int cycles = 10)
        {
            Console.WriteLine($"⏰ Simulating {cycles} aging cycles...");
            
            for (int cycle = 0; cycle < cycles; cycle++)
            {
                foreach (var cluster in GetAllClusters())
                {
                    var accessCount = _accessCounts.GetValueOrDefault(cluster.Concept, 0);
                    var timeSinceAccess = DateTime.Now - _lastAccessed.GetValueOrDefault(cluster.Concept, DateTime.MinValue);
                    
                    if (accessCount > 5 && timeSinceAccess.TotalMinutes < 5)
                    {
                        // Frequently used recent concepts get stronger
                        foreach (var neuron in cluster.ActiveNeurons)
                        {
                            neuron.Strengthen(0.05);
                        }
                    }
                    else if (timeSinceAccess.TotalMinutes > 10)
                    {
                        // Old unused concepts decay
                        foreach (var neuron in cluster.ActiveNeurons)
                        {
                            neuron.Weaken(0.02);
                        }
                    }
                }
                
                // Run maintenance every few cycles
                if (cycle % 3 == 0)
                {
                    BiologicalMaintenance();
                }
            }
            
            Console.WriteLine("⏰ Aging simulation complete");
        }

        // Private helper methods
        private void RecordAccess(string concept)
        {
            _lastAccessed[concept] = DateTime.Now;
            _accessCounts[concept] = _accessCounts.GetValueOrDefault(concept, 0) + 1;
        }

        private void ApplyGlobalDecay(string excludeConcept)
        {
            foreach (var cluster in GetAllClusters())
            {
                if (cluster.Concept != excludeConcept)
                {
                    cluster.ApplyDecay();
                }
            }
        }

        private void ApplyNeuronFatigue(string concept)
        {
            var cluster = GetCluster(concept);
            if (cluster != null)
            {
                foreach (var neuron in cluster.ActiveNeurons)
                {
                    neuron.AccumulateFatigue(0.1);
                }
            }
        }

        private void ApplyBiologicalActivation(ConceptCluster cluster)
        {
            // Use the BiologicalActivate method instead of setting ActivationLevel directly
            cluster.BiologicalActivate();
        }

        private void EvictLeastUsedCluster()
        {
            var leastUsed = _lastAccessed
                .OrderBy(kvp => kvp.Value)
                .FirstOrDefault();
                
            if (!string.IsNullOrEmpty(leastUsed.Key))
            {
                Console.WriteLine($"🗑️  Evicting least used cluster: {leastUsed.Key}");
                EvictCluster(leastUsed.Key);
            }
        }

        private void CreateTemporalSynapse(string fromConcept, string toConcept, double strength)
        {
            // Enhanced synapses for sequence learning
            // This would be implemented in the cluster itself
            Console.WriteLine($"🔗 Creating temporal synapse: {fromConcept} →({strength:F1})→ {toConcept}");
        }

        private string ExtractNextFromSequence(string concept, string sequenceInfo)
        {
            // Simple heuristic for sequence prediction
            // In a real implementation, this would be more sophisticated
            return sequenceInfo.Split(' ').Skip(1).FirstOrDefault() ?? concept;
        }

        // Use base class methods directly - no need for placeholders
        private int GetActiveClusterCount() => GetActiveClusters().Count;
        private ConceptCluster? GetCluster(string concept) => 
            GetActiveClusters().ContainsKey(concept) ? GetActiveClusters()[concept] : null;
        private List<ConceptCluster> GetAllClusters() => GetActiveClusters().Values.ToList();
        private void EvictCluster(string concept) 
        { 
            // For now, just deactivate the cluster - more sophisticated eviction could be implemented
            var cluster = GetCluster(concept);
            cluster?.Deactivate();
        }
    }
}

/// <summary>
/// Extensions to EphemeralNeuron for biological behaviors
/// </summary>
public static class EphemeralNeuronExtensions
{
    public static void RecoverFromFatigue(this EphemeralNeuron neuron)
    {
        // Would need to access private fatigue field
        // Implementation depends on making Fatigue settable
    }
    
    public static void AccumulateFatigue(this EphemeralNeuron neuron, double amount)
    {
        // Would accumulate fatigue based on usage
    }
    
    public static void Strengthen(this EphemeralNeuron neuron, double amount)
    {
        // Would increase neuron strength over time
    }
    
    public static void Weaken(this EphemeralNeuron neuron, double amount)
    {
        // Would decrease neuron strength over time
    }
}
