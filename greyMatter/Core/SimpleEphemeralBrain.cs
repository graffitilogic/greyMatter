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
        public void Learn(string concept, string[] relatedConcepts = null)
        {
            Console.WriteLine($"Learning: {concept}");
            
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
            cluster.Train();
            
            Console.WriteLine($"Cluster '{concept}' now has {cluster.ActiveNeurons.Count} neurons");
            LogSharedNeurons(concept);
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
    }

    /// <summary>
    /// A concept cluster that can activate/deactivate (FMRI-like behavior)
    /// Contains both private and shared neurons
    /// </summary>
    public class ConceptCluster
    {
        public string Concept { get; }
        public List<EphemeralNeuron> ActiveNeurons { get; private set; }
        public double ActivationLevel { get; private set; }
        
        public ConceptCluster(string concept, List<EphemeralNeuron> initialNeurons)
        {
            Concept = concept;
            ActiveNeurons = new List<EphemeralNeuron>(initialNeurons);
            ActivationLevel = 0.0;
        }

        public void Activate()
        {
            ActivationLevel = 1.0;
            foreach (var neuron in ActiveNeurons)
            {
                neuron.Activate();
            }
        }

        public void PartialActivate(int sharedNeuronCount)
        {
            // Activation level based on how many shared neurons are firing
            ActivationLevel = Math.Min(1.0, (double)sharedNeuronCount / ActiveNeurons.Count);
        }

        public void Deactivate()
        {
            ActivationLevel = 0.0;
            foreach (var neuron in ActiveNeurons)
            {
                neuron.Deactivate();
            }
        }

        public void Train()
        {
            // Simple training: just mark that learning occurred
            foreach (var neuron in ActiveNeurons)
            {
                neuron.Learn();
            }
        }

        public void AddSharedNeurons(List<EphemeralNeuron> sharedNeurons)
        {
            ActiveNeurons.AddRange(sharedNeurons);
        }
    }

    /// <summary>
    /// Simple neuron implementation focused on the ephemeral concept
    /// Can be activated/deactivated, can learn, has fatigue
    /// </summary>
    public class EphemeralNeuron
    {
        public int Id { get; }
        public bool IsActive { get; private set; }
        public double Strength { get; private set; }
        public double Fatigue { get; private set; }
        public int ActivationCount { get; private set; }

        public EphemeralNeuron(int id)
        {
            Id = id;
            IsActive = false;
            Strength = 0.5; // Start at medium strength
            Fatigue = 0.0;
            ActivationCount = 0;
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

        public void Learn()
        {
            if (IsActive)
            {
                Strength = Math.Min(1.0, Strength + 0.01); // Strengthen with use
            }
        }

        public override bool Equals(object obj)
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
        public string Concept { get; set; }
        public DateTime CreatedAt { get; set; }
        public int BaseSize { get; set; }
    }
}
