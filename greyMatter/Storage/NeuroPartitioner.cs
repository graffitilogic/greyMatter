using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter.Storage
{
    /// <summary>
    /// Neurobiologically-inspired partitioning system that uses multi-modal analysis
    /// of neuron properties to determine optimal storage organization with hierarchical learning support
    /// </summary>
    public class NeuroPartitioner
    {
        private readonly Random _random = new();
        private readonly ConceptDependencyGraph _dependencyGraph = new();

        /// <summary>
        /// Calculate optimal partition path using multi-modal neurobiological analysis
        /// Enhanced with knowledge layer hierarchy
        /// </summary>
        public async Task<LayeredPartitionPath> CalculateOptimalLayeredPartition(HybridNeuron neuron, BrainContext context)
        {
            // Determine knowledge layer based on concept complexity and dependencies
            var knowledgeLayer = DetermineKnowledgeLayer(neuron);
            
            // Check if concept has prerequisites and scaffolding requirements
            var scaffoldingNeeded = RequiresScaffolding(neuron, context);
            
            // Primary partition by knowledge layer
            var layerPath = $"layer_{(int)knowledgeLayer}_{knowledgeLayer.ToString().ToLower()}";
            
            // Secondary partition by functional role
            var functionalRole = CalculateFunctionalRole(neuron);
            
            // Tertiary partition by plasticity state (influenced by knowledge layer)
            var plasticityState = CalculatePlasticityState(neuron, knowledgeLayer);
            
            // Quaternary partition by network topology
            var topologyRole = CalculateTopologyRole(neuron);
            
            // Quinary partition by temporal dynamics
            var temporalSignature = CalculateTemporalSignature(neuron);

            var partitionPath = new LayeredPartitionPath
            {
                KnowledgeLayer = knowledgeLayer,
                Primary = layerPath,
                Secondary = functionalRole,
                Tertiary = plasticityState,
                Quaternary = topologyRole,
                FullPath = $"{layerPath}/{functionalRole}/{plasticityState}/{topologyRole}/{temporalSignature}",
                ConceptComplexity = CalculateConceptComplexity(neuron),
                DependentConcepts = GetDependentConcepts(neuron),
                RequiresScaffolding = scaffoldingNeeded
            };

            return await Task.FromResult(partitionPath);
        }

        /// <summary>
        /// Legacy method for backward compatibility
        /// </summary>
        public async Task<PartitionPath> CalculateOptimalPartition(HybridNeuron neuron, BrainContext context)
        {
            var layeredPath = await CalculateOptimalLayeredPartition(neuron, context);
            
            return new PartitionPath
            {
                Primary = layeredPath.Secondary, // Use functional role as primary
                Secondary = layeredPath.Tertiary,
                Tertiary = layeredPath.Quaternary,
                Quaternary = "temporal/recent_moderate", // Default temporal
                FullPath = $"{layeredPath.Secondary}/{layeredPath.Tertiary}/{layeredPath.Quaternary}/temporal/recent_moderate"
            };
        }

        /// <summary>
        /// Determine the appropriate knowledge layer for a neuron's concepts
        /// </summary>
        private KnowledgeLayer DetermineKnowledgeLayer(HybridNeuron neuron)
        {
            if (!neuron.AssociatedConcepts.Any())
            {
                return KnowledgeLayer.SensoryPrimitives; // Default for new neurons
            }

            var maxLayer = KnowledgeLayer.SensoryPrimitives;
            
            foreach (var concept in neuron.AssociatedConcepts)
            {
                var conceptNode = _dependencyGraph.GetConcept(concept);
                if (conceptNode != null && (int)conceptNode.Layer > (int)maxLayer)
                {
                    maxLayer = conceptNode.Layer;
                }
                else
                {
                    // Heuristic classification for unknown concepts
                    var inferredLayer = InferKnowledgeLayer(concept);
                    if ((int)inferredLayer > (int)maxLayer)
                    {
                        maxLayer = inferredLayer;
                    }
                }
            }

            return maxLayer;
        }

        /// <summary>
        /// Infer knowledge layer from concept name patterns
        /// </summary>
        private KnowledgeLayer InferKnowledgeLayer(string concept)
        {
            var lowerConcept = concept.ToLowerInvariant();
            
            // Sensory primitives: basic colors, shapes, textures
            if (lowerConcept.Contains("red") || lowerConcept.Contains("blue") || lowerConcept.Contains("green") ||
                lowerConcept.Contains("circle") || lowerConcept.Contains("square") || lowerConcept.Contains("triangle") ||
                lowerConcept.Contains("smooth") || lowerConcept.Contains("rough") || lowerConcept.Contains("hard"))
            {
                return KnowledgeLayer.SensoryPrimitives;
            }
            
            // Concept associations: object-property relationships
            if (lowerConcept.Contains("apple") || lowerConcept.Contains("ball") || lowerConcept.Contains("toy") ||
                lowerConcept.Contains("food") || lowerConcept.Contains("animal"))
            {
                return KnowledgeLayer.ConceptAssociations;
            }
            
            // Relational understanding: comparisons, relationships
            if (lowerConcept.Contains("bigger") || lowerConcept.Contains("same") || lowerConcept.Contains("different") ||
                lowerConcept.Contains("more") || lowerConcept.Contains("less") || lowerConcept.Contains("similar"))
            {
                return KnowledgeLayer.RelationalUnderstanding;
            }
            
            // Abstract concepts: emotions, social concepts, complex categories
            if (lowerConcept.Contains("happy") || lowerConcept.Contains("sad") || lowerConcept.Contains("friendship") ||
                lowerConcept.Contains("justice") || lowerConcept.Contains("beauty") || lowerConcept.Contains("love"))
            {
                return KnowledgeLayer.AbstractConcepts;
            }
            
            // Complex reasoning: scientific, mathematical, philosophical
            if (lowerConcept.Contains("algorithm") || lowerConcept.Contains("hypothesis") || lowerConcept.Contains("theorem") ||
                lowerConcept.Contains("consciousness") || lowerConcept.Contains("quantum") || lowerConcept.Contains("recursion"))
            {
                return KnowledgeLayer.ComplexReasoning;
            }
            
            // Default to concept associations for unknown patterns
            return KnowledgeLayer.ConceptAssociations;
        }

        /// <summary>
        /// Check if learning this concept requires scaffolding from prerequisite concepts
        /// </summary>
        private bool RequiresScaffolding(HybridNeuron neuron, BrainContext context)
        {
            foreach (var concept in neuron.AssociatedConcepts)
            {
                var conceptNode = _dependencyGraph.GetConcept(concept);
                if (conceptNode != null && conceptNode.Prerequisites.Any())
                {
                    return true; // Has prerequisites, needs scaffolding
                }
            }
            
            return false;
        }

        /// <summary>
        /// Calculate concept complexity based on abstraction level and dependencies
        /// </summary>
        private double CalculateConceptComplexity(HybridNeuron neuron)
        {
            if (!neuron.AssociatedConcepts.Any())
            {
                return 0.1; // Simple for new neurons
            }

            var totalComplexity = 0.0;
            var conceptCount = 0;

            foreach (var concept in neuron.AssociatedConcepts)
            {
                var conceptNode = _dependencyGraph.GetConcept(concept);
                if (conceptNode != null)
                {
                    totalComplexity += conceptNode.ConceptComplexity;
                    conceptCount++;
                }
                else
                {
                    // Estimate complexity based on knowledge layer
                    var layer = InferKnowledgeLayer(concept);
                    totalComplexity += (int)layer * 0.2 + 0.1;
                    conceptCount++;
                }
            }

            return conceptCount > 0 ? totalComplexity / conceptCount : 0.1;
        }

        /// <summary>
        /// Get concepts that depend on this neuron's concepts
        /// </summary>
        private string[] GetDependentConcepts(HybridNeuron neuron)
        {
            var dependents = new HashSet<string>();
            
            foreach (var concept in neuron.AssociatedConcepts)
            {
                var conceptNode = _dependencyGraph.GetConcept(concept);
                if (conceptNode != null)
                {
                    dependents.UnionWith(conceptNode.EnabledConcepts);
                }
            }
            
            return dependents.ToArray();
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
        /// Enhanced with knowledge layer influence
        /// </summary>
        private string CalculatePlasticityState(HybridNeuron neuron, KnowledgeLayer knowledgeLayer)
        {
            var learningRate = neuron.LearningRate;
            var fatigue = neuron.Fatigue;
            var adaptationLevel = CalculateAdaptationLevel(neuron);

            // Adjust plasticity classification based on knowledge layer
            if (knowledgeLayer >= KnowledgeLayer.AbstractConcepts && adaptationLevel > 0.8)
                return "plasticity/high_abstract_adaptive";
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
    /// Represents a layered hierarchical partition path for neuron storage
    /// </summary>
    public class LayeredPartitionPath : PartitionPath
    {
        public KnowledgeLayer KnowledgeLayer { get; set; }
        public double ConceptComplexity { get; set; }
        public string[] DependentConcepts { get; set; } = Array.Empty<string>();
        public bool RequiresScaffolding { get; set; }
    }

    /// <summary>
    /// Enum for hierarchical knowledge layers
    /// </summary>
    public enum KnowledgeLayer
    {
        SensoryPrimitives = 1,
        ConceptAssociations = 2,
        RelationalUnderstanding = 3,
        AbstractConcepts = 4,
        ComplexReasoning = 5
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
