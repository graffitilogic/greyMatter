using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Storage; // Import KnowledgeLayer from Storage namespace

namespace GreyMatter.Core
{
    /// <summary>
    /// Represents a concept node in the hierarchical knowledge graph
    /// </summary>
    public class ConceptNode
    {
        public string ConceptName { get; set; } = "";
        public KnowledgeLayer Layer { get; set; }
        public List<string> Prerequisites { get; set; } = new();
        public List<string> EnabledConcepts { get; set; } = new();
        public double MasteryThreshold { get; set; } = 0.7;
        public double CurrentMastery { get; set; } = 0.0;
        public DateTime FirstLearned { get; set; }
        public DateTime LastReinforced { get; set; }
        public int LearningAttempts { get; set; } = 0;
        public bool IsCriticalPeriodSensitive { get; set; }
        
        // Biological learning properties
        public double PlasticityLevel { get; set; } = 1.0;
        public double ConceptComplexity { get; set; } = 0.5;
        public List<string> SemanticNeighbors { get; set; } = new();
    }

    /// <summary>
    /// Enhanced partition path that includes knowledge layer information
    /// </summary>
    public class CoreLayeredPartitionPath : LayeredPartitionPath
    {
        public List<KnowledgeLayer> Prerequisites { get; set; } = new();
        
        public override string ToString()
        {
            return $"layer_{(int)KnowledgeLayer}_{KnowledgeLayer.ToString().ToLower()}/{FullPath}";
        }
    }

    /// <summary>
    /// Manages concept dependencies and learning prerequisites
    /// Implements scaffolding and zone of proximal development
    /// </summary>
    public class ConceptDependencyGraph
    {
        private readonly Dictionary<string, ConceptNode> _concepts = new();
        private readonly Dictionary<KnowledgeLayer, List<string>> _layerConcepts = new();
        
        public ConceptDependencyGraph()
        {
            InitializeFoundationalConcepts();
        }

        /// <summary>
        /// Check if a concept can be learned based on prerequisites
        /// </summary>
        public async Task<bool> CanLearnConcept(string concept, IBrainInterface brain)
        {
            if (!_concepts.TryGetValue(concept.ToLowerInvariant(), out var conceptNode))
            {
                // Unknown concept - can be learned as exploratory
                return true;
            }

            // Check critical period sensitivity
            if (conceptNode.IsCriticalPeriodSensitive)
            {
                var brainAge = await brain.GetBrainAgeAsync();
                if (!IsOptimalLearningWindow(conceptNode.Layer, brainAge))
                {
                    // Outside critical period - harder but still possible
                    conceptNode.MasteryThreshold *= 1.5; // Require higher mastery
                }
            }

            // Check if all prerequisites are sufficiently mastered
            foreach (var prerequisite in conceptNode.Prerequisites)
            {
                var masteryLevel = await brain.GetConceptMasteryLevelAsync(prerequisite);
                if (masteryLevel < conceptNode.MasteryThreshold)
                {
                    return false; // Not ready for this concept
                }
            }

            return true;
        }

        /// <summary>
        /// Get the optimal learning path for a concept
        /// </summary>
        public async Task<List<string>> GetLearningPath(string targetConcept, IBrainInterface brain)
        {
            var path = new List<string>();
            var visited = new HashSet<string>();
            
            await BuildLearningPathRecursive(targetConcept, path, visited, brain);
            
            return path;
        }

        /// <summary>
        /// Add a new concept to the dependency graph
        /// </summary>
        public void RegisterConcept(ConceptNode concept)
        {
            var key = concept.ConceptName.ToLowerInvariant();
            _concepts[key] = concept;
            
            if (!_layerConcepts.ContainsKey(concept.Layer))
            {
                _layerConcepts[concept.Layer] = new List<string>();
            }
            _layerConcepts[concept.Layer].Add(key);
        }

        /// <summary>
        /// Get all concepts at a specific knowledge layer
        /// </summary>
        public List<ConceptNode> GetConceptsAtLayer(KnowledgeLayer layer)
        {
            if (!_layerConcepts.TryGetValue(layer, out var conceptNames))
            {
                return new List<ConceptNode>();
            }

            return conceptNames
                .Where(name => _concepts.ContainsKey(name))
                .Select(name => _concepts[name])
                .ToList();
        }

        /// <summary>
        /// Update mastery level for a concept
        /// </summary>
        public void UpdateConceptMastery(string concept, double masteryLevel)
        {
            var key = concept.ToLowerInvariant();
            if (_concepts.TryGetValue(key, out var conceptNode))
            {
                conceptNode.CurrentMastery = masteryLevel;
                conceptNode.LastReinforced = DateTime.UtcNow;
                conceptNode.LearningAttempts++;
            }
        }

        /// <summary>
        /// Get concept node by name
        /// </summary>
        public ConceptNode? GetConcept(string concept)
        {
            _concepts.TryGetValue(concept.ToLowerInvariant(), out var conceptNode);
            return conceptNode;
        }

        private async Task BuildLearningPathRecursive(string concept, List<string> path, HashSet<string> visited, IBrainInterface brain)
        {
            var key = concept.ToLowerInvariant();
            if (visited.Contains(key) || !_concepts.TryGetValue(key, out var conceptNode))
            {
                return;
            }

            visited.Add(key);

            // First, ensure all prerequisites are in the path
            foreach (var prerequisite in conceptNode.Prerequisites)
            {
                var masteryLevel = await brain.GetConceptMasteryLevelAsync(prerequisite);
                if (masteryLevel < conceptNode.MasteryThreshold)
                {
                    await BuildLearningPathRecursive(prerequisite, path, visited, brain);
                }
            }

            // Then add this concept to the path
            if (!path.Contains(concept))
            {
                path.Add(concept);
            }
        }

        private bool IsOptimalLearningWindow(KnowledgeLayer layer, TimeSpan brainAge)
        {
            return layer switch
            {
                KnowledgeLayer.SensoryPrimitives => brainAge < TimeSpan.FromDays(30),
                KnowledgeLayer.ConceptAssociations => brainAge < TimeSpan.FromDays(90),
                KnowledgeLayer.RelationalUnderstanding => brainAge < TimeSpan.FromDays(180),
                KnowledgeLayer.AbstractConcepts => brainAge < TimeSpan.FromDays(365),
                _ => true // Complex reasoning can be learned throughout life
            };
        }

        private void InitializeFoundationalConcepts()
        {
            // Layer 0: Sensory Primitives
            RegisterConcept(new ConceptNode
            {
                ConceptName = "red",
                Layer = KnowledgeLayer.SensoryPrimitives,
                MasteryThreshold = 0.8,
                IsCriticalPeriodSensitive = true,
                ConceptComplexity = 0.1,
                SemanticNeighbors = new() { "color", "bright", "warm" }
            });

            RegisterConcept(new ConceptNode
            {
                ConceptName = "blue",
                Layer = KnowledgeLayer.SensoryPrimitives,
                MasteryThreshold = 0.8,
                IsCriticalPeriodSensitive = true,
                ConceptComplexity = 0.1,
                SemanticNeighbors = new() { "color", "cool", "sky" }
            });

            RegisterConcept(new ConceptNode
            {
                ConceptName = "circle",
                Layer = KnowledgeLayer.SensoryPrimitives,
                MasteryThreshold = 0.8,
                IsCriticalPeriodSensitive = true,
                ConceptComplexity = 0.2,
                SemanticNeighbors = new() { "shape", "round", "curved" }
            });

            RegisterConcept(new ConceptNode
            {
                ConceptName = "square",
                Layer = KnowledgeLayer.SensoryPrimitives,
                MasteryThreshold = 0.8,
                IsCriticalPeriodSensitive = true,
                ConceptComplexity = 0.2,
                SemanticNeighbors = new() { "shape", "angular", "four_sides" }
            });

            // Layer 1: Concept Associations
            RegisterConcept(new ConceptNode
            {
                ConceptName = "apple",
                Layer = KnowledgeLayer.ConceptAssociations,
                Prerequisites = new() { "red", "circle" },
                MasteryThreshold = 0.7,
                ConceptComplexity = 0.4,
                SemanticNeighbors = new() { "fruit", "food", "tree" }
            });

            RegisterConcept(new ConceptNode
            {
                ConceptName = "ball",
                Layer = KnowledgeLayer.ConceptAssociations,
                Prerequisites = new() { "circle", "blue" },
                MasteryThreshold = 0.7,
                ConceptComplexity = 0.4,
                SemanticNeighbors = new() { "toy", "play", "round" }
            });

            // Layer 2: Relational Understanding
            RegisterConcept(new ConceptNode
            {
                ConceptName = "bigger_than",
                Layer = KnowledgeLayer.RelationalUnderstanding,
                Prerequisites = new() { "circle", "square" },
                MasteryThreshold = 0.6,
                ConceptComplexity = 0.6,
                SemanticNeighbors = new() { "comparison", "size", "relationship" }
            });

            RegisterConcept(new ConceptNode
            {
                ConceptName = "same_color",
                Layer = KnowledgeLayer.RelationalUnderstanding,
                Prerequisites = new() { "red", "blue" },
                MasteryThreshold = 0.6,
                ConceptComplexity = 0.6,
                SemanticNeighbors = new() { "similarity", "matching", "classification" }
            });

            // Layer 3: Abstract Concepts
            RegisterConcept(new ConceptNode
            {
                ConceptName = "friendship",
                Layer = KnowledgeLayer.AbstractConcepts,
                Prerequisites = new() { "same_color", "bigger_than" },
                MasteryThreshold = 0.5,
                ConceptComplexity = 0.8,
                SemanticNeighbors = new() { "social", "relationship", "emotion" }
            });
        }
    }

    /// <summary>
    /// Interface for brain systems that support hierarchical learning
    /// </summary>
    public interface IBrainInterface
    {
        Task<double> GetConceptMasteryLevelAsync(string concept);
        Task<TimeSpan> GetBrainAgeAsync();
    }
}
