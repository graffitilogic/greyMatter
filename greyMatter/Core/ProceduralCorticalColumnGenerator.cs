using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GreyMatter.Core;
using greyMatter.Core; // For IIntegratedBrain and support classes

namespace GreyMatter.Core
{
    /// <summary>
    /// Procedural generation system for cortical columns - inspired by No Man's Sky
    /// Generates neural structures on-demand based on semantic coordinates and task requirements
    /// </summary>
    public class ProceduralCorticalColumnGenerator
    {
        private readonly Random _random;
        private readonly int _patternSize;
        private readonly double _sparsity;
        private readonly Dictionary<string, ProceduralColumnTemplate> _columnTemplates;
        private readonly Dictionary<string, DateTime> _columnAccessTimes;
        private readonly Dictionary<string, int> _columnUsageCounts;

        // Procedural generation parameters
        private const int MIN_COLUMN_SIZE = 512;
        private const int MAX_COLUMN_SIZE = 8192;
        private const double MIN_SPARSITY = 0.01;
        private const double MAX_SPARSITY = 0.05;

        public ProceduralCorticalColumnGenerator(int basePatternSize = 2048, double baseSparsity = 0.02)
        {
            _patternSize = basePatternSize;
            _sparsity = baseSparsity;
            _random = new Random(42); // Consistent seed for reproducibility
            _columnTemplates = new Dictionary<string, ProceduralColumnTemplate>();
            _columnAccessTimes = new Dictionary<string, DateTime>();
            _columnUsageCounts = new Dictionary<string, int>();

            InitializeColumnTemplates();
        }

        /// <summary>
        /// Initialize templates for different types of cortical columns
        /// </summary>
        private void InitializeColumnTemplates()
        {
            // Phonetic columns - sound-based patterns
            _columnTemplates["phonetic"] = new ProceduralColumnTemplate
            {
                ColumnType = "phonetic",
                BaseFeatures = new[] { "vowel", "consonant", "syllable", "phoneme", "stress" },
                GenerationRules = new[] { "sound_similarity", "articulation_patterns", "phonetic_distance" },
                ComplexityMultiplier = 1.2
            };

            // Semantic columns - meaning-based patterns
            _columnTemplates["semantic"] = new ProceduralColumnTemplate
            {
                ColumnType = "semantic",
                BaseFeatures = new[] { "category", "relationship", "property", "function", "context" },
                GenerationRules = new[] { "semantic_similarity", "concept_hierarchy", "association_strength" },
                ComplexityMultiplier = 1.5
            };

            // Syntactic columns - structure-based patterns
            _columnTemplates["syntactic"] = new ProceduralColumnTemplate
            {
                ColumnType = "syntactic",
                BaseFeatures = new[] { "word_order", "dependency", "phrase_type", "clause_structure", "grammar_rule" },
                GenerationRules = new[] { "syntactic_distance", "structural_patterns", "parsing_rules" },
                ComplexityMultiplier = 1.3
            };

            // Contextual columns - situation-based patterns
            _columnTemplates["contextual"] = new ProceduralColumnTemplate
            {
                ColumnType = "contextual",
                BaseFeatures = new[] { "domain", "register", "tone", "purpose", "audience" },
                GenerationRules = new[] { "context_similarity", "situational_patterns", "pragmatic_rules" },
                ComplexityMultiplier = 1.4
            };

            // Episodic columns - memory-based patterns
            _columnTemplates["episodic"] = new ProceduralColumnTemplate
            {
                ColumnType = "episodic",
                BaseFeatures = new[] { "temporal", "spatial", "emotional", "personal", "sequence" },
                GenerationRules = new[] { "temporal_proximity", "emotional_valence", "personal_relevance" },
                ComplexityMultiplier = 1.6
            };
        }

        /// <summary>
        /// Generate a cortical column based on semantic coordinates and task requirements
        /// Similar to how No Man's Sky generates planets based on coordinates
        /// </summary>
        public async Task<ProceduralCorticalColumn> GenerateColumnAsync(
            string columnType,
            SemanticCoordinates coordinates,
            TaskRequirements requirements)
        {
            var columnId = GenerateColumnId(columnType, coordinates);

            // Check if column already exists and is recent
            if (_columnAccessTimes.TryGetValue(columnId, out var lastAccess))
            {
                var age = DateTime.UtcNow - lastAccess;
                if (age < TimeSpan.FromMinutes(30)) // Reuse recent columns
                {
                    _columnUsageCounts[columnId]++;
                    return await LoadExistingColumnAsync(columnId);
                }
            }

            Console.WriteLine($"🔄 Procedurally generating cortical column: {columnId}");

            // Generate column based on template and coordinates
            var template = _columnTemplates[columnType];
            var column = await GenerateColumnFromTemplateAsync(template, coordinates, requirements);

            // Cache the generated column
            _columnAccessTimes[columnId] = DateTime.UtcNow;
            _columnUsageCounts[columnId] = 1;

            return column;
        }

        /// <summary>
        /// Generate column ID based on type and semantic coordinates
        /// </summary>
        private string GenerateColumnId(string columnType, SemanticCoordinates coordinates)
        {
            var coordinateString = $"{coordinates.Domain}:{coordinates.Topic}:{coordinates.Context}";
            var combined = $"{columnType}:{coordinateString}";
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
            return $"{columnType}_{BitConverter.ToString(hash).Replace("-", "").Substring(0, 16)}";
        }

        /// <summary>
        /// Generate a column from template using procedural rules
        /// </summary>
        private async Task<ProceduralCorticalColumn> GenerateColumnFromTemplateAsync(
            ProceduralColumnTemplate template,
            SemanticCoordinates coordinates,
            TaskRequirements requirements)
        {
            // Calculate column parameters based on coordinates and requirements
            var columnSize = CalculateColumnSize(coordinates, requirements);
            var sparsity = CalculateSparsity(coordinates, requirements);
            var complexity = CalculateComplexity(template, coordinates);

            // Generate neural patterns using template rules
            var patterns = await GenerateNeuralPatternsAsync(template, coordinates, columnSize, sparsity);

            // Create the procedural column
            var column = new ProceduralCorticalColumn
            {
                Id = GenerateColumnId(template.ColumnType, coordinates),
                Type = template.ColumnType,
                Coordinates = coordinates,
                Size = columnSize,
                Sparsity = sparsity,
                Complexity = complexity,
                NeuralPatterns = patterns,
                GenerationTime = DateTime.UtcNow,
                AccessCount = 0
            };

            return column;
        }

        /// <summary>
        /// Calculate optimal column size based on semantic coordinates and task requirements
        /// </summary>
        private int CalculateColumnSize(SemanticCoordinates coordinates, TaskRequirements requirements)
        {
            // Base size on semantic complexity and task demands
            var baseSize = _patternSize;

            // Scale based on domain complexity
            var domainMultiplier = coordinates.Domain switch
            {
                "technical" => 1.5,
                "scientific" => 1.4,
                "literary" => 1.3,
                "conversational" => 1.1,
                _ => 1.0
            };

            // Scale based on task requirements
            var taskMultiplier = requirements.Complexity switch
            {
                "high" => 1.8,
                "medium" => 1.4,
                "low" => 1.0,
                _ => 1.2
            };

            var calculatedSize = (int)(baseSize * domainMultiplier * taskMultiplier);
            return Math.Clamp(calculatedSize, MIN_COLUMN_SIZE, MAX_COLUMN_SIZE);
        }

        /// <summary>
        /// Calculate optimal sparsity based on coordinates and requirements
        /// </summary>
        private double CalculateSparsity(SemanticCoordinates coordinates, TaskRequirements requirements)
        {
            // Higher sparsity for complex domains, lower for simple ones
            var baseSparsity = coordinates.Domain switch
            {
                "technical" => 0.015,    // More precise, less overlap
                "scientific" => 0.018,   // Balanced precision
                "literary" => 0.025,     // More flexible, more overlap
                "conversational" => 0.030, // Very flexible
                _ => _sparsity
            };

            // Adjust based on task requirements
            if (requirements.Precision == "high")
                baseSparsity *= 0.8; // More precise = lower sparsity
            else if (requirements.Precision == "low")
                baseSparsity *= 1.2; // Less precise = higher sparsity

            return Math.Clamp(baseSparsity, MIN_SPARSITY, MAX_SPARSITY);
        }

        /// <summary>
        /// Calculate complexity multiplier for the column
        /// </summary>
        private double CalculateComplexity(ProceduralColumnTemplate template, SemanticCoordinates coordinates)
        {
            var baseComplexity = template.ComplexityMultiplier;

            // Increase complexity for abstract concepts
            if (coordinates.IsAbstract)
                baseComplexity *= 1.3;

            // Increase complexity for polysemous words
            if (coordinates.PolysemyCount > 1)
                baseComplexity *= (1.0 + (coordinates.PolysemyCount - 1) * 0.2);

            return baseComplexity;
        }

        /// <summary>
        /// Generate neural patterns using procedural rules
        /// </summary>
        private async Task<Dictionary<string, SparsePattern>> GenerateNeuralPatternsAsync(
            ProceduralColumnTemplate template,
            SemanticCoordinates coordinates,
            int columnSize,
            double sparsity)
        {
            var patterns = new Dictionary<string, SparsePattern>();

            // Generate patterns for each base feature
            foreach (var feature in template.BaseFeatures)
            {
                var featureKey = $"{coordinates.Topic}_{feature}";
                var pattern = await GenerateFeaturePatternAsync(featureKey, columnSize, sparsity, coordinates);
                patterns[feature] = pattern;
            }

            // Generate patterns for generation rules
            foreach (var rule in template.GenerationRules)
            {
                var ruleKey = $"{coordinates.Topic}_{rule}";
                var pattern = await GenerateRulePatternAsync(ruleKey, columnSize, sparsity, coordinates);
                patterns[rule] = pattern;
            }

            return patterns;
        }

        /// <summary>
        /// Generate a sparse pattern for a specific feature
        /// </summary>
        private async Task<SparsePattern> GenerateFeaturePatternAsync(
            string featureKey,
            int columnSize,
            double sparsity,
            SemanticCoordinates coordinates)
        {
            // Use coordinates to seed the random generation for consistency
            var seed = featureKey.GetHashCode() ^ coordinates.GetHashCode();
            var localRandom = new Random(seed);

            var activeBitCount = (int)(columnSize * sparsity);
            var activeBits = new HashSet<int>();

            // Generate active bits using coordinate-based distribution
            for (int i = 0; i < activeBitCount; i++)
            {
                var bit = localRandom.Next(columnSize);
                activeBits.Add(bit);
            }

            return new SparsePattern(activeBits.ToArray(), 1);
        }

        /// <summary>
        /// Generate a sparse pattern for a generation rule
        /// </summary>
        private async Task<SparsePattern> GenerateRulePatternAsync(
            string ruleKey,
            int columnSize,
            double sparsity,
            SemanticCoordinates coordinates)
        {
            // Rules generate more complex patterns
            var seed = ruleKey.GetHashCode() ^ coordinates.GetHashCode() ^ "rule".GetHashCode();
            var localRandom = new Random(seed);

            var activeBitCount = (int)(columnSize * sparsity * 1.5); // Rules are more complex
            var activeBits = new HashSet<int>();

            // Generate active bits with rule-based clustering
            var clusters = localRandom.Next(3, 8); // 3-7 clusters per rule
            for (int cluster = 0; cluster < clusters; cluster++)
            {
                var clusterCenter = localRandom.Next(columnSize);
                var clusterSize = localRandom.Next(5, 15);

                for (int i = 0; i < clusterSize; i++)
                {
                    var offset = localRandom.Next(-10, 11); // Spread around center
                    var bit = (clusterCenter + offset + columnSize) % columnSize;
                    activeBits.Add(bit);
                }
            }

            return new SparsePattern(activeBits.ToArray(), clusters);
        }

        /// <summary>
        /// Load an existing column (placeholder for persistence integration)
        /// </summary>
        private async Task<ProceduralCorticalColumn> LoadExistingColumnAsync(string columnId)
        {
            // This would load from persistent storage in a full implementation
            // For now, return a placeholder
            return new ProceduralCorticalColumn
            {
                Id = columnId,
                Type = "cached",
                Coordinates = new SemanticCoordinates(),
                Size = _patternSize,
                Sparsity = _sparsity,
                Complexity = 1.0,
                NeuralPatterns = new Dictionary<string, SparsePattern>(),
                GenerationTime = DateTime.UtcNow.AddMinutes(-10),
                AccessCount = _columnUsageCounts.GetValueOrDefault(columnId, 0)
            };
        }

        /// <summary>
        /// Get statistics about procedural generation
        /// </summary>
        public ProceduralGenerationStats GetStats()
        {
            return new ProceduralGenerationStats
            {
                TotalColumnsGenerated = _columnAccessTimes.Count,
                ActiveColumns = _columnUsageCounts.Count,
                AverageAccessCount = _columnUsageCounts.Values.Average(),
                MostRecentGeneration = _columnAccessTimes.Values.Max()
            };
        }
    }

    /// <summary>
    /// Template for procedural column generation
    /// </summary>
    public class ProceduralColumnTemplate
    {
        public string ColumnType { get; set; }
        public string[] BaseFeatures { get; set; }
        public string[] GenerationRules { get; set; }
        public double ComplexityMultiplier { get; set; }
    }

    /// <summary>
    /// Semantic coordinates for procedural generation (like No Man's Sky coordinates)
    /// </summary>
    public class SemanticCoordinates
    {
        public string Domain { get; set; } = "general";
        public string Topic { get; set; } = "";
        public string Context { get; set; } = "";
        public bool IsAbstract { get; set; } = false;
        public int PolysemyCount { get; set; } = 1;

        public override int GetHashCode()
        {
            return HashCode.Combine(Domain, Topic, Context, IsAbstract, PolysemyCount);
        }
    }

    /// <summary>
    /// Task requirements for column generation
    /// </summary>
    public class TaskRequirements
    {
        public string Complexity { get; set; } = "medium";
        public string Precision { get; set; } = "medium";
        public int ExpectedLoad { get; set; } = 1000;
    }

    /// <summary>
    /// Procedurally generated cortical column with working memory and messaging capability
    /// </summary>
    public class ProceduralCorticalColumn
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public SemanticCoordinates Coordinates { get; set; }
        public int Size { get; set; }
        public double Sparsity { get; set; }
        public double Complexity { get; set; }
        public Dictionary<string, SparsePattern> NeuralPatterns { get; set; }
        public DateTime GenerationTime { get; set; }
        public int AccessCount { get; set; }

        /// <summary>
        /// Working memory for this column (shared state)
        /// </summary>
        public WorkingMemory? WorkingMemory { get; set; }

        /// <summary>
        /// Reference to message bus for inter-column communication
        /// </summary>
        public MessageBus? MessageBus { get; set; }

        /// <summary>
        /// Reference to integrated brain for bidirectional communication
        /// Enables columns to trigger learning and query existing knowledge
        /// </summary>
        public IIntegratedBrain? Brain { get; set; }

        // Integration tracking
        private Dictionary<string, int> _patternFrequencies = new Dictionary<string, int>();
        private Dictionary<string, DateTime> _lastSeen = new Dictionary<string, DateTime>();
        private int _totalMessagesProcessed = 0;

        /// <summary>
        /// Process incoming messages from other columns
        /// </summary>
        public List<ColumnMessage> ProcessMessages(int maxMessages = 10)
        {
            if (MessageBus == null)
                return new List<ColumnMessage>();

            return MessageBus.GetMessages(Id, maxMessages);
        }

        /// <summary>
        /// Send a message to another column or column type
        /// Now with optional brain knowledge integration
        /// </summary>
        public async Task SendMessageAsync(string receiverId, MessageType type, SparsePattern payload, double strength = 1.0, string? concept = null)
        {
            if (MessageBus == null)
                return;

            // Query brain for knowledge about the pattern (if brain is connected and concept is provided)
            if (Brain != null && !string.IsNullOrWhiteSpace(concept))
            {
                var familiarity = await Brain.GetWordFamiliarityAsync(concept);
                
                // Boost strength for known concepts (knowledge-guided processing)
                if (familiarity > 0.5)
                {
                    strength *= (1.0 + familiarity * 0.5); // Up to 50% boost for familiar concepts
                }
            }

            var message = new ColumnMessage
            {
                SenderId = Id,
                ReceiverId = receiverId,
                Type = type,
                Payload = payload,
                Strength = strength
            };

            MessageBus.SendMessage(message);
            _totalMessagesProcessed++;
        }

        /// <summary>
        /// Send a message to another column or column type (synchronous version)
        /// </summary>
        public void SendMessage(string receiverId, MessageType type, SparsePattern payload, double strength = 1.0)
        {
            SendMessageAsync(receiverId, type, payload, strength, null).Wait();
        }

        /// <summary>
        /// Broadcast message to all columns of a specific type
        /// </summary>
        public void BroadcastToType(string columnType, MessageType type, SparsePattern payload, double strength = 1.0)
        {
            if (MessageBus == null)
                return;

            var message = new ColumnMessage
            {
                SenderId = Id,
                Type = type,
                Payload = payload,
                Strength = strength
            };

            MessageBus.Broadcast(columnType, message);
        }

        /// <summary>
        /// Track pattern and potentially notify brain of significant patterns
        /// Implements pattern detection for column-triggered learning
        /// </summary>
        public async Task TrackAndNotifyPatternAsync(string concept, List<string> relatedConcepts, double confidence)
        {
            if (Brain == null || string.IsNullOrWhiteSpace(concept))
                return;

            // Update tracking
            if (!_patternFrequencies.ContainsKey(concept))
                _patternFrequencies[concept] = 0;
            _patternFrequencies[concept]++;
            _lastSeen[concept] = DateTime.Now;

            // Determine if this is a significant pattern worth notifying brain about
            PatternType patternType = DeterminePatternType(concept, relatedConcepts);
            bool isSignificant = IsSignificantPattern(concept, confidence);

            if (isSignificant)
            {
                var pattern = new ColumnPattern
                {
                    PrimaryConcept = concept,
                    RelatedConcepts = relatedConcepts,
                    Confidence = confidence,
                    ColumnCount = 1, // Could track multiple columns in future
                    MessageCount = _patternFrequencies[concept],
                    Type = patternType,
                    DetectedAt = DateTime.Now
                };

                await Brain.NotifyColumnPatternAsync(pattern);
            }
        }

        /// <summary>
        /// Determine what type of pattern this represents
        /// </summary>
        private PatternType DeterminePatternType(string concept, List<string> relatedConcepts)
        {
            // Check if this is a novel concept
            if (!_patternFrequencies.ContainsKey(concept) || _patternFrequencies[concept] <= 2)
                return PatternType.NovelWord;

            // Check if this is reinforcement (repeated pattern)
            if (_patternFrequencies[concept] > 5)
                return PatternType.Reinforcement;

            // Check for new associations
            if (relatedConcepts.Count > 0)
                return PatternType.NewAssociation;

            // Default based on column type
            return Type switch
            {
                "syntactic" => PatternType.SyntacticStructure,
                "semantic" => PatternType.SemanticCluster,
                _ => PatternType.NewAssociation
            };
        }

        /// <summary>
        /// Determine if pattern is significant enough to notify brain
        /// </summary>
        private bool IsSignificantPattern(string concept, double confidence)
        {
            // Require minimum confidence
            if (confidence < 0.6)
                return false;

            // Novel patterns are always significant
            if (!_patternFrequencies.ContainsKey(concept))
                return true;

            // Repeated patterns are significant at certain thresholds
            int frequency = _patternFrequencies[concept];
            return frequency == 1 || // First occurrence
                   frequency == 3 || // Reinforcement threshold
                   frequency == 10 || // Strong reinforcement
                   frequency % 50 == 0; // Periodic updates for very common patterns
        }

        /// <summary>
        /// Query brain for related concepts to guide message routing
        /// Implements knowledge-guided column processing
        /// </summary>
        public async Task<List<string>> GetBrainRelatedConceptsAsync(string concept, int maxResults = 5)
        {
            if (Brain == null || string.IsNullOrWhiteSpace(concept))
                return new List<string>();

            return await Brain.GetRelatedConceptsAsync(concept, maxResults);
        }

        /// <summary>
        /// Check if brain knows about a concept (fast check)
        /// </summary>
        public bool IsBrainKnownConcept(string concept)
        {
            if (Brain == null || string.IsNullOrWhiteSpace(concept))
                return false;

            return Brain.IsKnownWord(concept);
        }

        /// <summary>
        /// Get full knowledge about a concept from brain
        /// </summary>
        public async Task<ConceptKnowledge?> GetBrainKnowledgeAsync(string concept)
        {
            if (Brain == null || string.IsNullOrWhiteSpace(concept))
                return null;

            return await Brain.QueryKnowledgeAsync(concept);
        }
    }

    /// <summary>
    /// Statistics for procedural generation system
    /// </summary>
    public class ProceduralGenerationStats
    {
        public int TotalColumnsGenerated { get; set; }
        public int ActiveColumns { get; set; }
        public double AverageAccessCount { get; set; }
        public DateTime MostRecentGeneration { get; set; }
    }
}
