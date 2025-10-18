using System;
using System.Collections.Generic;
using System.Linq;

namespace greyMatter.Core
{
    /// <summary>
    /// Simple implementation of the original ephemeral brain concept:
    /// - Clusters activate on-demand (FMRI-like)
    /// - Neurons are shared between related concepts (Venn diagram overlap)
    /// - Memory usage scales with active concepts only
    /// - No complex persistence or capacity management
    /// </summary>
    public class SimpleEphemeralBrain
    {
        private readonly SharedNeuronPool _neuronPool;
        private readonly Dictionary<string, ConceptCluster> _activeClusters;
        private readonly Dictionary<string, ClusterMetadata> _conceptRegistry;
        private readonly Random _random;

        public SimpleEphemeralBrain()
        {
            _neuronPool = new SharedNeuronPool();
            _activeClusters = new Dictionary<string, ConceptCluster>();
            _conceptRegistry = new Dictionary<string, ClusterMetadata>();
            _random = new Random(42); // Deterministic for debugging
        }

        /// <summary>
        /// Learn a simple association: "this is red", "this is an apple"
        /// Creates or activates clusters and shares neurons between related concepts
        /// </summary>
        public void Learn(string concept, string[]? relatedConcepts = null)
        {
            Console.WriteLine($"Learning: {concept}");
            LearnInternal(concept, relatedConcepts);
            
            // Show detailed output for verbose learning
            var cluster = _activeClusters[concept];
            Console.WriteLine($"Cluster '{concept}' now has {cluster.ActiveNeurons.Count} neurons");
            LogSharedNeurons(concept);
        }

        /// <summary>
        /// Learn silently without console output - for batch processing
        /// </summary>
        public void LearnSilently(string concept, string[]? relatedConcepts = null)
        {
            LearnInternal(concept, relatedConcepts);
        }

        /// <summary>
        /// Learn a batch of concepts with rolled-up console output
        /// </summary>
        public void LearnBatch(IEnumerable<string> concepts, int reportingBatchSize = 50, int exampleSampleSize = 3)
        {
            var conceptList = concepts.ToList();
            var totalConcepts = conceptList.Count;
            var processed = 0;
            var recentExamples = new List<string>();
            var startTime = DateTime.Now;

            foreach (var concept in conceptList)
            {
                LearnInternal(concept);
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
                    
                    Console.WriteLine($"Learning: {percentComplete:F1}% | " +
                                    $"Batch: {conceptsPerSecond:F0} concepts/sec | " +
                                    $"Total: {processed:N0}");
                    
                    // Show sample of recent concepts learned
                    if (recentExamples.Any())
                    {
                        var examples = string.Join(", ", recentExamples.Take(exampleSampleSize));
                        Console.WriteLine($"  Recent examples: {examples}");
                    }
                }
            }
        }

        private void LearnInternal(string concept, string[]? relatedConcepts = null)
        {
            // Get or create cluster for this concept
            var cluster = GetOrCreateCluster(concept);
            
            // If this concept is related to others, share neurons
            if (relatedConcepts != null)
            {
                foreach (var related in relatedConcepts)
                {
                    var relatedCluster = GetOrCreateCluster(related);
                    ShareNeuronsBetweenClusters(cluster, relatedCluster, concept, related);
                }
            }
            
            // Activate and train the cluster
            cluster.Activate();
            cluster.Learn();
        }

        /// <summary>
        /// Recall: given a concept, what related concepts are activated?
        /// This demonstrates the FMRI-like activation spreading
        /// </summary>
        public List<string> Recall(string concept)
        {
            Console.WriteLine($"Recalling: {concept}");
            
            if (!_activeClusters.ContainsKey(concept))
            {
                Console.WriteLine($"Concept '{concept}' not found in active clusters");
                return new List<string>();
            }

            var cluster = _activeClusters[concept];
            cluster.Activate();
            
            // Find other clusters that share neurons with this one
            var relatedConcepts = new List<string>();
            
            foreach (var otherConcept in _activeClusters.Keys.Where(k => k != concept))
            {
                var otherCluster = _activeClusters[otherConcept];
                var sharedCount = cluster.ActiveNeurons.Intersect(otherCluster.ActiveNeurons).Count();
                
                if (sharedCount > 0)
                {
                    relatedConcepts.Add($"{otherConcept} ({sharedCount} shared)");
                    // Activate related cluster based on shared neurons
                    otherCluster.PartialActivate(sharedCount);
                }
            }
            
            Console.WriteLine($"Related concepts: {string.Join(", ", relatedConcepts)}");
            return relatedConcepts;
        }

        /// <summary>
        /// Show what's currently "thinking" - which clusters are active
        /// This is the FMRI-like visualization concept
        /// </summary>
        public void ShowBrainScan()
        {
            Console.WriteLine("\n=== Brain Scan (Active Clusters) ===");
            
            foreach (var cluster in _activeClusters.Values.Where(c => c.ActivationLevel > 0))
            {
                var sharedInfo = GetSharedNeuronInfo(cluster.Concept);
                Console.WriteLine($"{cluster.Concept}: {cluster.ActivationLevel:F2} activation, " +
                                $"{cluster.ActiveNeurons.Count} neurons, {sharedInfo}");
            }
            
            Console.WriteLine($"Total neurons in pool: {_neuronPool.TotalNeurons}");
            Console.WriteLine($"Active concepts: {_activeClusters.Count}");
        }

        private ConceptCluster GetOrCreateCluster(string concept)
        {
            if (_activeClusters.ContainsKey(concept))
            {
                return _activeClusters[concept];
            }

            // Create new cluster with procedurally generated neurons
            var baseSize = 50 + _random.Next(50); // 50-100 neurons per cluster
            var neurons = _neuronPool.GetNeurons(baseSize);
            
            var cluster = new ConceptCluster(concept, neurons);
            _activeClusters[concept] = cluster;
            
            // Register metadata for persistence (simple version)
            _conceptRegistry[concept] = new ClusterMetadata 
            { 
                Concept = concept, 
                CreatedAt = DateTime.Now,
                BaseSize = baseSize
            };
            
            return cluster;
        }

        private void ShareNeuronsBetweenClusters(ConceptCluster cluster1, ConceptCluster cluster2, 
                                               string concept1, string concept2)
        {
            // Simple sharing: related concepts share 20-40% of their neurons
            var sharePercentage = 0.2 + _random.NextDouble() * 0.2; // 20-40%
            var neuronsToShare = (int)(Math.Min(cluster1.ActiveNeurons.Count, cluster2.ActiveNeurons.Count) * sharePercentage);
            
            var sharedNeurons = _neuronPool.GetSharedNeurons(neuronsToShare);
            
            // Add shared neurons to both clusters
            cluster1.AddSharedNeurons(sharedNeurons);
            cluster2.AddSharedNeurons(sharedNeurons);
            
            Console.WriteLine($"Sharing {neuronsToShare} neurons between '{concept1}' and '{concept2}'");
            
            // Create bidirectional connections between neurons in related concepts
            // This implements the biological "concepts that co-occur wire together" principle
            CreateInterClusterConnections(cluster1, cluster2);
        }
        
        /// <summary>
        /// Create connections between neurons in related concept clusters
        /// Implements biological learning: related concepts have connected neurons
        /// </summary>
        private void CreateInterClusterConnections(ConceptCluster cluster1, ConceptCluster cluster2)
        {
            // Sample a few neurons from each cluster to connect (not all combinations - too many)
            var connectionsPerNeuron = 3; // Each neuron connects to ~3 neurons in related cluster
            
            foreach (var neuron1 in cluster1.ActiveNeurons.Take(10)) // Sample first 10
            {
                var targetNeurons = cluster2.ActiveNeurons
                    .OrderBy(x => _random.Next())
                    .Take(connectionsPerNeuron);
                    
                foreach (var neuron2 in targetNeurons)
                {
                    // Create bidirectional weak connections
                    var initialWeight = 0.1 + _random.NextDouble() * 0.1; // 0.1-0.2 initial weight
                    neuron1.ConnectTo(neuron2.Id, initialWeight);
                    neuron2.ConnectTo(neuron1.Id, initialWeight);
                }
            }
        }

        private void LogSharedNeurons(string concept)
        {
            if (!_activeClusters.ContainsKey(concept)) return;
            
            var cluster = _activeClusters[concept];
            var sharedCount = 0;
            
            foreach (var otherConcept in _activeClusters.Keys.Where(k => k != concept))
            {
                var otherCluster = _activeClusters[otherConcept];
                var shared = cluster.ActiveNeurons.Intersect(otherCluster.ActiveNeurons).Count();
                if (shared > 0)
                {
                    sharedCount += shared;
                    Console.WriteLine($"  Shares {shared} neurons with '{otherConcept}'");
                }
            }
            
            if (sharedCount == 0)
            {
                Console.WriteLine($"  No shared neurons (isolated concept)");
            }
        }

        private string GetSharedNeuronInfo(string concept)
        {
            if (!_activeClusters.ContainsKey(concept)) return "no sharing info";
            
            var cluster = _activeClusters[concept];
            var sharedConnections = new List<string>();
            
            foreach (var otherConcept in _activeClusters.Keys.Where(k => k != concept))
            {
                var otherCluster = _activeClusters[otherConcept];
                var shared = cluster.ActiveNeurons.Intersect(otherCluster.ActiveNeurons).Count();
                if (shared > 0)
                {
                    sharedConnections.Add($"{shared} with {otherConcept}");
                }
            }
            
            return sharedConnections.Any() ? $"shares: {string.Join(", ", sharedConnections)}" : "isolated";
        }

        /// <summary>
        /// Demonstrate memory efficiency: only active concepts use memory
        /// </summary>
        public void ShowMemoryEfficiency()
        {
            Console.WriteLine("\n=== Memory Efficiency Demo ===");
            Console.WriteLine($"Concepts registered: {_conceptRegistry.Count}");
            Console.WriteLine($"Concepts active in memory: {_activeClusters.Count}");
            Console.WriteLine($"Total neurons allocated: {_neuronPool.TotalNeurons}");
            Console.WriteLine($"Memory scaling: O(active_concepts) not O(total_concepts)");
        }

        /// <summary>
        /// Get all active clusters for persistence
        /// </summary>
        public IReadOnlyDictionary<string, ConceptCluster> GetActiveClusters()
        {
            return _activeClusters;
        }

        /// <summary>
        /// Get the neuron pool for persistence
        /// </summary>
        public SharedNeuronPool GetNeuronPool()
        {
            return _neuronPool;
        }

        /// <summary>
        /// Restore clusters from persistence
        /// </summary>
        public void RestoreClusters(Dictionary<string, ConceptCluster> clusters)
        {
            _activeClusters.Clear();
            foreach (var kvp in clusters)
            {
                _activeClusters[kvp.Key] = kvp.Value;
                
                // Also register in the concept registry
                if (!_conceptRegistry.ContainsKey(kvp.Key))
                {
                    _conceptRegistry[kvp.Key] = new ClusterMetadata 
                    { 
                        Concept = kvp.Key,
                        CreatedAt = DateTime.UtcNow,
                        LastAccessed = DateTime.UtcNow,
                        AccessCount = 1,
                        BaseSize = kvp.Value.ActiveNeurons.Count,
                        AverageStrength = 1.0
                    };
                }
            }
        }

        /// <summary>
        /// Get comprehensive statistics about brain state and memory usage
        /// </summary>
        public BrainMemoryStats GetMemoryStats()
        {
            return new BrainMemoryStats
            {
                ConceptsRegistered = _conceptRegistry.Count,
                ActiveConcepts = _activeClusters.Count,
                TotalNeurons = _neuronPool.GetTotalNeuronCount(),
                ActiveNeurons = _neuronPool.GetActiveNeuronCount(),
                SharedConnections = _neuronPool.GetSharedConnectionCount(),
                MemoryEfficiency = (double)_conceptRegistry.Count / Math.Max(1, _neuronPool.GetTotalNeuronCount())
            };
        }
    }

    /// <summary>
    /// Statistics about brain memory usage and efficiency
    /// </summary>
    public class BrainMemoryStats
    {
        public int ConceptsRegistered { get; set; }
        public int ActiveConcepts { get; set; }
        public int TotalNeurons { get; set; }
        public int ActiveNeurons { get; set; }
        public int SharedConnections { get; set; }
        public double MemoryEfficiency { get; set; }
    }

    /// <summary>
    /// Manages the shared pool of neurons - the key to memory efficiency
    /// Neurons are allocated on-demand and can be shared between clusters
    /// </summary>
    public class SharedNeuronPool
    {
        private readonly List<EphemeralNeuron> _allNeurons;
        private int _nextNeuronId;

        public SharedNeuronPool()
        {
            _allNeurons = new List<EphemeralNeuron>();
            _nextNeuronId = 1;
        }

        public int TotalNeurons => _allNeurons.Count;

        public List<EphemeralNeuron> GetNeurons(int count)
        {
            var neurons = new List<EphemeralNeuron>();
            
            for (int i = 0; i < count; i++)
            {
                var neuron = new EphemeralNeuron(_nextNeuronId++);
                _allNeurons.Add(neuron);
                neurons.Add(neuron);
            }
            
            return neurons;
        }

        public List<EphemeralNeuron> GetSharedNeurons(int count)
        {
            // For simplicity, create new shared neurons
            // In a real implementation, you might reuse existing neurons
            return GetNeurons(count);
        }

        public int GetTotalNeuronCount()
        {
            return _allNeurons.Count;
        }

        public int GetActiveNeuronCount()
        {
            // For this simple implementation, all neurons are considered active
            return _allNeurons.Count;
        }

        public int GetSharedConnectionCount()
        {
            // Count neurons that appear in multiple clusters
            // This is a simplified implementation
            return _allNeurons.Count / 10; // Rough estimate for demo
        }
    }

    /// <summary>
    /// A concept cluster that can activate/deactivate (FMRI-like behavior)
    /// Contains both private and shared neurons
    /// </summary>
    public class ConceptCluster
    {
        public string Concept { get; }
        public List<EphemeralNeuron> ActiveNeurons { get; protected set; }
        public double ActivationLevel { get; protected set; }
        
        public ConceptCluster(string concept, List<EphemeralNeuron> initialNeurons)
        {
            Concept = concept;
            ActiveNeurons = new List<EphemeralNeuron>(initialNeurons);
            ActivationLevel = 0.0;
        }

        public virtual void Activate()
        {
            ActivationLevel = 1.0;
            foreach (var neuron in ActiveNeurons)
            {
                neuron.Activate();
            }
        }

        public virtual void BiologicalActivate()
        {
            // Enhanced activation considering fatigue
            var availableNeurons = ActiveNeurons.Where(n => n.Fatigue < 0.8).ToList();
            ActivationLevel = (double)availableNeurons.Count / ActiveNeurons.Count;
            
            foreach (var neuron in availableNeurons)
            {
                neuron.Activate();
            }
        }

        public virtual void PartialActivate(int sharedNeuronCount)
        {
            // Activation level based on how many shared neurons are firing
            ActivationLevel = Math.Min(1.0, (double)sharedNeuronCount / ActiveNeurons.Count);
        }

        public virtual void Deactivate()
        {
            ActivationLevel = 0.0;
            foreach (var neuron in ActiveNeurons)
            {
                neuron.Deactivate();
            }
        }

        public virtual void Learn()
        {
            // Hebbian learning: neurons in this cluster learn from each other
            // Pass all active neurons so they can form connections
            foreach (var neuron in ActiveNeurons.Where(n => n.IsActive))
            {
                neuron.Learn(ActiveNeurons); // Pass all neurons for Hebbian learning
            }
        }

        public virtual void AddSharedNeurons(List<EphemeralNeuron> sharedNeurons)
        {
            ActiveNeurons.AddRange(sharedNeurons);
        }

        public virtual void RemoveNeuron(EphemeralNeuron neuron)
        {
            ActiveNeurons.Remove(neuron);
        }

        public virtual void RestoreFromMetadata(ClusterMetadata metadata)
        {
            // Simple restoration - can be enhanced
        }

        public virtual void ApplyDecay()
        {
            ActivationLevel *= 0.95; // Gradual decay
        }

        public virtual void CreateSequenceConnection(ConceptCluster targetCluster)
        {
            // Base implementation - can be overridden
        }

        public double AverageStrength => ActiveNeurons.Count > 0 ? ActiveNeurons.Average(n => n.Strength) : 0.0;
        public double AverageFatigue => ActiveNeurons.Count > 0 ? ActiveNeurons.Average(n => n.Fatigue) : 0.0;
    }

    /// <summary>
    /// Simple neuron implementation focused on the ephemeral concept
    /// Can be activated/deactivated, can learn, has fatigue
    /// Now includes connection weights for biological learning
    /// </summary>
    public class EphemeralNeuron
    {
        public int Id { get; }
        public bool IsActive { get; private set; }
        public double Strength { get; private set; }
        public double Fatigue { get; private set; }
        public int ActivationCount { get; private set; }
        
        /// <summary>
        /// Connection weights to other neurons (neuron ID -> weight)
        /// Implements Hebbian learning: "neurons that fire together, wire together"
        /// </summary>
        public Dictionary<int, double> Weights { get; } = new Dictionary<int, double>();

        public EphemeralNeuron(int id)
        {
            Id = id;
            IsActive = false;
            Strength = 0.5; // Start at medium strength
            Fatigue = 0.0;
            ActivationCount = 0;
        }
        
        /// <summary>
        /// Create a connection to another neuron
        /// </summary>
        public void ConnectTo(int targetNeuronId, double initialWeight = 0.1)
        {
            if (targetNeuronId == Id) return; // Don't connect to self
            
            if (!Weights.ContainsKey(targetNeuronId))
            {
                Weights[targetNeuronId] = initialWeight;
            }
        }
        
        /// <summary>
        /// Strengthen (or weaken) connection to another neuron
        /// Implements Hebbian learning rule
        /// </summary>
        public void StrengthenConnection(int targetNeuronId, double delta)
        {
            if (Weights.ContainsKey(targetNeuronId))
            {
                Weights[targetNeuronId] = Math.Clamp(Weights[targetNeuronId] + delta, 0.0, 1.0);
                
                // Prune very weak connections
                if (Weights[targetNeuronId] < 0.01)
                {
                    Weights.Remove(targetNeuronId);
                }
            }
        }

        public void Activate()
        {
            if (Fatigue < 0.8) // Don't activate if too fatigued
            {
                IsActive = true;
                ActivationCount++;
                Fatigue += 0.1; // Accumulate fatigue
            }
        }

        public void Deactivate()
        {
            IsActive = false;
            Fatigue = Math.Max(0, Fatigue - 0.05); // Recover from fatigue
        }

        /// <summary>
        /// Learn by strengthening connections to co-active neurons
        /// Implements Hebbian learning: neurons that fire together wire together
        /// </summary>
        public void Learn(IEnumerable<EphemeralNeuron>? coActiveNeurons = null)
        {
            if (IsActive)
            {
                Strength = Math.Min(1.0, Strength + 0.01); // Strengthen with use
                
                // Hebbian learning: strengthen connections to neurons that are active at the same time
                if (coActiveNeurons != null)
                {
                    foreach (var otherNeuron in coActiveNeurons)
                    {
                        if (otherNeuron.Id != Id && otherNeuron.IsActive)
                        {
                            // Create connection if doesn't exist
                            if (!Weights.ContainsKey(otherNeuron.Id))
                            {
                                Weights[otherNeuron.Id] = 0.05; // Initial weak connection
                            }
                            
                            // Strengthen existing connection
                            StrengthenConnection(otherNeuron.Id, 0.01); // Small increment with each co-activation
                        }
                    }
                }
            }
        }

        public override bool Equals(object? obj)
        {
            return obj is EphemeralNeuron other && Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    /// <summary>
    /// Simple metadata for concepts - much lighter than current system
    /// </summary>
    public class ClusterMetadata
    {
        public required string Concept { get; set; }
        public DateTime CreatedAt { get; set; }
        public int BaseSize { get; set; }
        public DateTime LastAccessed { get; set; }
        public int AccessCount { get; set; }
        public double AverageStrength { get; set; }
    }
}
