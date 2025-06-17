using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter.Storage
{
    /// <summary>
    /// Neurobiologically-inspired partitioning system that uses multi-modal analysis
    /// of neuron properties to determine optimal storage organization
    /// </summary>
    public class NeuroPartitioner
    {
        private readonly Random _random = new();

        /// <summary>
        /// Calculate optimal partition path using multi-modal neurobiological analysis
        /// </summary>
        public async Task<PartitionPath> CalculateOptimalPartition(HybridNeuron neuron, BrainContext context)
        {
            // Primary partition by functional role
            var functionalRole = CalculateFunctionalRole(neuron);
            
            // Secondary partition by plasticity state  
            var plasticityState = CalculatePlasticityState(neuron);
            
            // Tertiary partition by network topology
            var topologyRole = CalculateTopologyRole(neuron);
            
            // Quaternary partition by temporal dynamics
            var temporalSignature = CalculateTemporalSignature(neuron);

            var partitionPath = new PartitionPath
            {
                Primary = functionalRole,
                Secondary = plasticityState,
                Tertiary = topologyRole,
                Quaternary = temporalSignature,
                FullPath = $"{functionalRole}/{plasticityState}/{topologyRole}/{temporalSignature}"
            };

            return await Task.FromResult(partitionPath);
        }

        /// <summary>
        /// Analyze neuron's functional role based on concept associations and processing patterns
        /// </summary>
        private string CalculateFunctionalRole(HybridNeuron neuron)
        {
            var concepts = neuron.AssociatedConcepts.ToList();
            
            // Analyze concept patterns to determine functional domain
            var sensoryKeywords = new[] { "red", "blue", "green", "color", "visual", "audio", "touch" };
            var motorKeywords = new[] { "move", "action", "motor", "control", "execute" };
            var memoryKeywords = new[] { "remember", "recall", "episodic", "semantic" };
            var associationKeywords = new[] { "relate", "connect", "associate", "concept" };

            var sensoryScore = concepts.Count(c => sensoryKeywords.Any(k => c.Contains(k)));
            var motorScore = concepts.Count(c => motorKeywords.Any(k => c.Contains(k)));
            var memoryScore = concepts.Count(c => memoryKeywords.Any(k => c.Contains(k)));
            var associationScore = concepts.Count(c => associationKeywords.Any(k => c.Contains(k)));

            // Determine primary functional role
            var maxScore = Math.Max(Math.Max(sensoryScore, motorScore), Math.Max(memoryScore, associationScore));
            
            if (maxScore == 0)
                return "functional/general";
            
            if (sensoryScore == maxScore)
                return "functional/sensory";
            if (motorScore == maxScore)
                return "functional/motor";
            if (memoryScore == maxScore)
                return "functional/memory";
            if (associationScore == maxScore)
                return "functional/association";
                
            return "functional/general";
        }

        /// <summary>
        /// Analyze neuron's plasticity state based on learning rate, fatigue, and adaptation
        /// </summary>
        private string CalculatePlasticityState(HybridNeuron neuron)
        {
            var learningRate = neuron.LearningRate;
            var fatigue = neuron.Fatigue;
            var adaptationLevel = CalculateAdaptationLevel(neuron);

            // Classify plasticity based on biological criteria
            if (learningRate > 0.15 && fatigue < 0.3)
                return "plasticity/high_adaptive";
            if (learningRate > 0.08 && fatigue < 0.6)
                return "plasticity/moderate_plastic";
            if (fatigue > 0.7)
                return "plasticity/low_fatigued";
            if (adaptationLevel > 0.8)
                return "plasticity/stable_mature";
                
            return "plasticity/baseline";
        }

        /// <summary>
        /// Analyze neuron's role in network topology
        /// </summary>
        private string CalculateTopologyRole(HybridNeuron neuron)
        {
            var connectionCount = neuron.InputWeights.Count + neuron.OutputConnections.Count;
            var weightVariance = CalculateWeightVariance(neuron.InputWeights.Values);
            var importanceScore = neuron.ImportanceScore;

            // Classify network topology role
            if (connectionCount > 20 && importanceScore > 0.5)
                return "topology/hub";
            if (connectionCount > 10 && weightVariance > 0.5)
                return "topology/bridge";
            if (connectionCount < 5 && neuron.AssociatedConcepts.Count > 0)
                return "topology/specialized";
            if (connectionCount >= 5 && connectionCount <= 20)
                return "topology/modular";
                
            return "topology/peripheral";
        }

        /// <summary>
        /// Analyze neuron's temporal activation patterns
        /// </summary>
        private string CalculateTemporalSignature(HybridNeuron neuron)
        {
            var daysSinceCreation = (DateTime.UtcNow - neuron.CreatedAt).TotalDays;
            var daysSinceLastUse = (DateTime.UtcNow - neuron.LastUsed).TotalDays;
            var activationFrequency = neuron.ActivationCount / Math.Max(daysSinceCreation, 1.0);

            // Classify temporal patterns
            if (daysSinceLastUse < 1.0 && activationFrequency > 5.0)
                return "temporal/active_frequent";
            if (daysSinceLastUse < 7.0 && activationFrequency > 1.0)
                return "temporal/recent_moderate";
            if (daysSinceLastUse < 30.0)
                return "temporal/archived_recent";
            if (daysSinceCreation > 30.0 && neuron.ImportanceScore > 0.3)
                return "temporal/consolidated_important";
                
            return "temporal/dormant";
        }

        /// <summary>
        /// Calculate adaptation level based on threshold changes and usage patterns
        /// </summary>
        private double CalculateAdaptationLevel(HybridNeuron neuron)
        {
            // Higher adaptation means more stable/mature connections
            var usageStability = Math.Min(1.0, neuron.ActivationCount / 100.0);
            var connectionStability = Math.Min(1.0, neuron.InputWeights.Count / 50.0);
            var importanceStability = Math.Min(1.0, neuron.ImportanceScore);
            
            return (usageStability + connectionStability + importanceStability) / 3.0;
        }

        /// <summary>
        /// Calculate variance in synaptic weights
        /// </summary>
        private double CalculateWeightVariance(IEnumerable<double> weights)
        {
            var weightList = weights.ToList();
            if (!weightList.Any()) return 0.0;
            
            var mean = weightList.Average();
            var variance = weightList.Sum(w => Math.Pow(w - mean, 2)) / weightList.Count;
            
            return Math.Sqrt(variance);
        }
    }

    /// <summary>
    /// Represents a hierarchical partition path for neuron storage
    /// </summary>
    public class PartitionPath
    {
        public string Primary { get; set; } = "";
        public string Secondary { get; set; } = "";
        public string Tertiary { get; set; } = "";
        public string Quaternary { get; set; } = "";
        public string FullPath { get; set; } = "";
        
        public override string ToString() => FullPath;
    }

    /// <summary>
    /// Brain context for partitioning decisions
    /// </summary>
    public class BrainContext
    {
        public Dictionary<Guid, HybridNeuron> AllNeurons { get; set; } = new();
        public Dictionary<string, HashSet<string>> ConceptRelationships { get; set; } = new();
        public DateTime AnalysisTime { get; set; } = DateTime.UtcNow;
    }
}
