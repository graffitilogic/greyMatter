using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core; // For MessageBus, WorkingMemory, ProceduralCorticalColumn, etc.
using greyMatter.Core;  // For IIntegratedBrain, LanguageEphemeralBrain
using GreyMatter.Storage;

namespace greyMatter.Core
{
    /// <summary>
    /// Unified training system that integrates column processing with traditional learning.
    /// Implements biologically-aligned architecture where:
    /// - Columns provide dynamic pattern recognition (cortical processing)
    /// - Brain provides persistent learning and memory (synaptic consolidation)
    /// - Attention system focuses on salient patterns (selective attention)
    /// - Episodic memory records learning events for context (hippocampus)
    /// - All systems communicate bidirectionally for optimal learning
    /// </summary>
    public class IntegratedTrainer
    {
        private readonly LanguageEphemeralBrain _brain;
        private readonly ProceduralCorticalColumnGenerator? _columnGenerator;
        private readonly MessageBus? _messageBus;
        private readonly ColumnPatternDetector? _patternDetector;
        private readonly WorkingMemory? _workingMemory;
        
        // Week 7: Advanced integration systems
        private readonly AttentionWeightCalculator? _attentionCalculator;
        private readonly EpisodicMemorySystem? _episodicMemory;

        // Generated columns by type
        private readonly Dictionary<string, List<ProceduralCorticalColumn>>? _columnsByType;
        
        // Training configuration
        private readonly bool _enableColumnProcessing;
        private readonly bool _enableTraditionalLearning;
        private readonly bool _enableIntegration;
        private readonly bool _enableAttention;
        private readonly bool _enableEpisodicMemory;
        private readonly double _attentionThreshold; // Only process patterns above this threshold
        
        // Statistics
        private int _sentencesProcessed = 0;
        private int _wordsLearned = 0;
        private int _patternsDetected = 0;
        private int _integrationTriggers = 0;
        private int _patternsSkippedByAttention = 0; // Attention optimization
        private int _episodesRecorded = 0;
        
        // Performance profiling
        private double _traditionalLearningMs = 0;
        private double _columnProcessingMs = 0;
        private double _patternDetectionMs = 0;
        private double _familiarityCheckMs = 0;
        private double _attentionCalculationMs = 0;
        private double _episodicMemoryMs = 0;

        public IntegratedTrainer(
            LanguageEphemeralBrain brain,
            bool enableColumnProcessing = true,
            bool enableTraditionalLearning = true,
            bool enableIntegration = true,
            bool enableAttention = false,
            bool enableEpisodicMemory = false,
            double attentionThreshold = 0.4,
            AttentionConfiguration? attentionConfig = null,
            string episodicMemoryPath = "./episodic_memory")
        {
            _brain = brain ?? throw new ArgumentNullException(nameof(brain));
            
            _enableColumnProcessing = enableColumnProcessing;
            _enableTraditionalLearning = enableTraditionalLearning;
            _enableIntegration = enableIntegration;
            _enableAttention = enableAttention;
            _enableEpisodicMemory = enableEpisodicMemory;
            _attentionThreshold = attentionThreshold;
            _attentionThreshold = attentionThreshold;
            
            // Initialize attention system if enabled
            if (_enableAttention)
            {
                _attentionCalculator = new AttentionWeightCalculator(attentionConfig);
                Console.WriteLine("✅ Attention system enabled (threshold: {0:F2})", _attentionThreshold);
            }
            
            // Initialize episodic memory if enabled
            if (_enableEpisodicMemory)
            {
                var storageManager = new SemanticStorageManager(episodicMemoryPath);
                _episodicMemory = new EpisodicMemorySystem(storageManager);
                Console.WriteLine("✅ Episodic memory enabled (path: {0})", episodicMemoryPath);
            }
            
            // Initialize column infrastructure if needed
            if (_enableColumnProcessing)
            {
                _columnGenerator = new ProceduralCorticalColumnGenerator();
                _messageBus = new MessageBus();
                _workingMemory = new WorkingMemory();
                _columnsByType = new Dictionary<string, List<ProceduralCorticalColumn>>();
                
                if (_enableIntegration)
                {
                    var iBrain = _brain as IIntegratedBrain;
                    if (iBrain == null)
                    {
                        throw new InvalidOperationException("Brain must implement IIntegratedBrain for integration");
                    }
                    _patternDetector = new ColumnPatternDetector(_messageBus!, iBrain);
                }
                
                InitializeColumns().Wait();
            }
        }

        /// <summary>
        /// Initialize cortical columns for different processing types
        /// </summary>
        private async Task InitializeColumns()
        {
            var columnTypes = new[] { "phonetic", "semantic", "syntactic", "contextual" };
            
            foreach (var type in columnTypes)
            {
                _columnsByType![type] = new List<ProceduralCorticalColumn>();
                
                // OPTIMIZATION: Use only 1 column per type instead of 3
                // (reduces message passing overhead while maintaining functionality)
                for (int i = 0; i < 1; i++)
                {
                    var coordinates = new SemanticCoordinates
                    {
                        Domain = type,
                        Topic = $"{type}_processing",
                        Context = "integrated_training"
                    };
                    
                    var requirements = new TaskRequirements
                    {
                        Complexity = "medium",
                        Precision = "high",
                        ExpectedLoad = 1000
                    };
                    
                    var column = await _columnGenerator!.GenerateColumnAsync(type, coordinates, requirements);
                    column.MessageBus = _messageBus;
                    column.WorkingMemory = _workingMemory;
                    
                    // Wire up brain integration if enabled
                    if (_enableIntegration)
                    {
                        column.Brain = _brain as IIntegratedBrain;
                    }
                    
                    _columnsByType[type].Add(column);
                    _messageBus!.RegisterColumn(column.Id);
                }
            }
            
            Console.WriteLine($"✅ Initialized {_columnsByType!.Sum(kvp => kvp.Value.Count)} columns across {columnTypes.Length} types");
        }

        /// <summary>
        /// Train on a single sentence using integrated approach with attention and episodic memory
        /// </summary>
        public async Task TrainOnSentenceAsync(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence))
                return;

            _sentencesProcessed++;
            var wordsInSentence = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Phase 1: Traditional learning (if enabled)
            if (_enableTraditionalLearning)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                _brain.LearnSentence(sentence);
                sw.Stop();
                _traditionalLearningMs += sw.Elapsed.TotalMilliseconds;
                _wordsLearned += wordsInSentence.Length;
            }

            // Phase 2: Column processing (if enabled)
            List<ColumnPattern>? patterns = null;
            if (_enableColumnProcessing)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                await ProcessThroughColumnsAsync(sentence);
                sw.Stop();
                _columnProcessingMs += sw.Elapsed.TotalMilliseconds;
                
                // Phase 3: Pattern detection and integration (if enabled)
                if (_enableIntegration && _patternDetector != null)
                {
                    sw.Restart();
                    patterns = await _patternDetector.AnalyzeRecentActivityAsync(TimeSpan.FromSeconds(5));
                    sw.Stop();
                    _patternDetectionMs += sw.Elapsed.TotalMilliseconds;
                    _patternsDetected += patterns.Count;
                    
                    // Phase 4: Attention-guided processing (Week 7)
                    if (_enableAttention && _attentionCalculator != null && patterns.Any())
                    {
                        sw.Restart();
                        var processedPatterns = ProcessPatternsWithAttention(patterns);
                        sw.Stop();
                        _attentionCalculationMs += sw.Elapsed.TotalMilliseconds;
                        
                        _integrationTriggers += processedPatterns.Count;
                        _patternsSkippedByAttention += (patterns.Count - processedPatterns.Count);
                    }
                    else
                    {
                        // No attention - process all high-confidence patterns
                        _integrationTriggers += patterns.Count(p => p.Confidence >= 0.7);
                    }
                }
            }

            // Phase 5: Episodic memory recording (Week 7)
            if (_enableEpisodicMemory && _episodicMemory != null)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                await RecordLearningEpisodeAsync(sentence, wordsInSentence.ToList(), patterns);
                sw.Stop();
                _episodicMemoryMs += sw.Elapsed.TotalMilliseconds;
                _episodesRecorded++;
            }
        }

        /// <summary>
        /// Process sentence through cortical columns with brain guidance
        /// </summary>
        private async Task ProcessThroughColumnsAsync(string sentence)
        {
            var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            // OPTIMIZATION: Cache familiarity for all words at sentence level
            // instead of querying for each word in each column type
            var wordFamiliarity = new Dictionary<string, double>();
            if (_enableIntegration && _brain is IIntegratedBrain iBrain)
            {
                foreach (var word in words.Distinct())
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    wordFamiliarity[word] = await iBrain.GetWordFamiliarityAsync(word);
                    sw.Stop();
                    _familiarityCheckMs += sw.Elapsed.TotalMilliseconds;
                }
            }
            
            foreach (var word in words)
            {
                // Phonetic processing
                await ProcessWordThroughColumnType(word, "phonetic", wordFamiliarity);
                
                // Semantic processing (with brain knowledge integration)
                await ProcessWordThroughColumnType(word, "semantic", wordFamiliarity);
                
                // Syntactic processing
                await ProcessWordThroughColumnType(word, "syntactic", wordFamiliarity);
            }
            
            // Contextual processing for whole sentence
            await ProcessSentenceThroughColumnType(sentence, "contextual");
        }

        /// <summary>
        /// Process a word through columns of a specific type
        /// </summary>
        private async Task ProcessWordThroughColumnType(string word, string columnType, Dictionary<string, double> wordFamiliarity)
        {
            if (!_columnsByType!.TryGetValue(columnType, out var columns))
                return;

            foreach (var column in columns)
            {
                // Get cached familiarity (optimization - avoids repeated async calls)
                double knowledgeBoost = 1.0;
                if (_enableIntegration && wordFamiliarity.TryGetValue(word, out var familiarity))
                {
                    knowledgeBoost = 1.0 + (familiarity * 0.5); // Up to 50% boost
                }

                // Create sparse pattern for the word
                var pattern = CreatePatternForWord(word, columnType);
                
                // Send message with knowledge-boosted strength
                await column.SendMessageAsync(
                    receiverId: $"{columnType}_receiver",
                    type: MessageType.Excitatory,
                    payload: pattern,
                    strength: 0.8 * knowledgeBoost,
                    concept: word
                );

                //  OPTIMIZATION: Pattern detection runs periodically, not per message
                // Track pattern for potential learning trigger
                // if (_enableIntegration)
                // {
                //     await column.TrackAndNotifyPatternAsync(
                //         concept: word,
                //         relatedConcepts: new List<string>(),
                //         confidence: 0.7 * knowledgeBoost
                //     );
                // }
            }
        }

        /// <summary>
        /// Process sentence context through contextual columns
        /// </summary>
        private async Task ProcessSentenceThroughColumnType(string sentence, string columnType)
        {
            if (!_columnsByType!.TryGetValue(columnType, out var columns))
                return;

            foreach (var column in columns)
            {
                var pattern = CreatePatternForSentence(sentence, columnType);
                
                await column.SendMessageAsync(
                    receiverId: $"{columnType}_context",
                    type: MessageType.Forward,
                    payload: pattern,
                    strength: 1.0,
                    concept: sentence.Split(' ').FirstOrDefault() ?? ""
                );
            }
        }

        /// <summary>
        /// Create sparse pattern for a word (placeholder implementation)
        /// </summary>
        private SparsePattern CreatePatternForWord(string word, string columnType)
        {
            // Hash the word to create a reproducible pattern
            var hash = (word.GetHashCode() ^ columnType.GetHashCode()) & 0x7FFFFFFF;
            var random = new Random(hash);
            
            // Generate sparse active bits
            var activeBits = Enumerable.Range(0, 20)
                .Select(_ => random.Next(0, 2048))
                .Distinct()
                .OrderBy(x => x)
                .ToArray();
            
            return new SparsePattern(activeBits, 0.8);
        }

        /// <summary>
        /// Create sparse pattern for a sentence
        /// </summary>
        private SparsePattern CreatePatternForSentence(string sentence, string columnType)
        {
            var hash = (sentence.GetHashCode() ^ columnType.GetHashCode()) & 0x7FFFFFFF;
            var random = new Random(hash);
            
            var activeBits = Enumerable.Range(0, 30)
                .Select(_ => random.Next(0, 2048))
                .Distinct()
                .OrderBy(x => x)
                .ToArray();
            
            return new SparsePattern(activeBits, 0.9);
        }

        /// <summary>
        /// Week 7: Process patterns with attention weighting
        /// Only process patterns that deserve attention (novel, uncertain, progressing)
        /// </summary>
        private List<greyMatter.Core.ColumnPattern> ProcessPatternsWithAttention(List<greyMatter.Core.ColumnPattern> patterns)
        {
            if (_attentionCalculator == null)
                return patterns;

            var processedPatterns = new List<greyMatter.Core.ColumnPattern>();

            foreach (var pattern in patterns)
            {
                // Convert to attention system's AttentionPattern
                var attentionPattern = new GreyMatter.Core.AttentionPattern
                {
                    PatternType = pattern.Type.ToString(),
                    Confidence = pattern.Confidence
                };
                
                // Use context as column identifier for attention calculation
                var columnId = !string.IsNullOrEmpty(pattern.Context) ? pattern.Context : pattern.PrimaryConcept;
                var attentionWeight = _attentionCalculator.CalculateWeight(columnId, attentionPattern);
                
                if (attentionWeight >= _attentionThreshold)
                {
                    processedPatterns.Add(pattern);
                }
                else
                {
                    // Pattern skipped by attention system
                }
            }

            return processedPatterns;
        }

        /// <summary>
        /// Week 7: Record learning episode in episodic memory
        /// </summary>
        private async Task RecordLearningEpisodeAsync(
            string sentence, 
            List<string> words, 
            List<greyMatter.Core.ColumnPattern>? patterns)
        {
            if (_episodicMemory == null)
                return;

            // Extract concepts learned
            var conceptsLearned = words;
            
            // Calculate confidence from patterns if available
            var confidence = patterns != null && patterns.Any() 
                ? patterns.Average(p => p.Confidence) 
                : 0.7;

            // Prepare context
            var context = new Dictionary<string, object>
            {
                ["sentence"] = sentence,
                ["word_count"] = words.Count,
                ["has_patterns"] = patterns != null && patterns.Any()
            };

            if (patterns != null && patterns.Any())
            {
                context["pattern_count"] = patterns.Count;
                context["pattern_types"] = string.Join(", ", patterns.Select(p => p.Type.ToString()).Distinct());
            }

            // Record the episode
            await _episodicMemory.RecordEventAsync(
                eventId: $"learn_{_episodesRecorded}",
                description: $"Learned sentence: {sentence.Substring(0, Math.Min(50, sentence.Length))}...",
                context: context,
                timestamp: DateTime.Now,
                participants: conceptsLearned,
                location: "integrated_trainer"
            );
        }

        /// <summary>
        /// Train on a batch of sentences
        /// </summary>
        public async Task TrainOnBatchAsync(IEnumerable<string> sentences, int batchSize = 100)
        {
            var batch = new List<string>();
            
            foreach (var sentence in sentences)
            {
                batch.Add(sentence);
                
                if (batch.Count >= batchSize)
                {
                    foreach (var s in batch)
                    {
                        await TrainOnSentenceAsync(s);
                    }
                    batch.Clear();
                    
                    // Progress update
                    Console.WriteLine($"  Processed {_sentencesProcessed} sentences...");
                }
            }
            
            // Process remaining
            foreach (var s in batch)
            {
                await TrainOnSentenceAsync(s);
            }
        }

        /// <summary>
        /// Get comprehensive training statistics
        /// </summary>
        public IntegratedTrainingStats GetStats()
        {
            var stats = new IntegratedTrainingStats
            {
                SentencesProcessed = _sentencesProcessed,
                WordsLearned = _wordsLearned,
                VocabularySize = _brain.VocabularySize,
                
                // Brain stats
                IntegrationStats = _enableIntegration ? (_brain as IIntegratedBrain)?.GetIntegrationStats() : null,
                
                // Column stats
                TotalColumns = _enableColumnProcessing ? _columnsByType!.Sum(kvp => kvp.Value.Count) : 0,
                
                // Pattern detection stats
                PatternsDetected = _patternsDetected,
                IntegrationTriggers = _integrationTriggers,
                PatternDetectionStats = (_enableIntegration && _patternDetector != null) 
                    ? _patternDetector.GetStats() 
                    : null,
                
                // Week 7: Attention stats
                AttentionEnabled = _enableAttention,
                PatternsSkippedByAttention = _patternsSkippedByAttention,
                AttentionStatistics = _enableAttention && _attentionCalculator != null
                    ? _attentionCalculator.GetStatistics()
                    : null,
                    
                // Week 7: Episodic memory stats
                EpisodicMemoryEnabled = _enableEpisodicMemory,
                EpisodesRecorded = _episodesRecorded,
                
                // Configuration
                ColumnProcessingEnabled = _enableColumnProcessing,
                TraditionalLearningEnabled = _enableTraditionalLearning,
                IntegrationEnabled = _enableIntegration
            };

            return stats;
        }

        /// <summary>
        /// Print comprehensive statistics
        /// </summary>
        public void PrintStats()
        {
            var stats = GetStats();
            
            Console.WriteLine("\n" + "═".PadRight(80, '═'));
            Console.WriteLine("INTEGRATED TRAINING STATISTICS (Week 7)");
            Console.WriteLine("═".PadRight(80, '═'));
            
            Console.WriteLine($"\n📊 Training Progress:");
            Console.WriteLine($"  Sentences: {stats.SentencesProcessed}");
            Console.WriteLine($"  Words: {stats.WordsLearned}");
            Console.WriteLine($"  Vocabulary: {stats.VocabularySize}");
            
            Console.WriteLine($"\n🧠 Configuration:");
            Console.WriteLine($"  Column Processing: {(stats.ColumnProcessingEnabled ? "✅" : "❌")}");
            Console.WriteLine($"  Traditional Learning: {(stats.TraditionalLearningEnabled ? "✅" : "❌")}");
            Console.WriteLine($"  Integration: {(stats.IntegrationEnabled ? "✅" : "❌")}");
            Console.WriteLine($"  Attention: {(stats.AttentionEnabled ? "✅" : "❌")}");
            Console.WriteLine($"  Episodic Memory: {(stats.EpisodicMemoryEnabled ? "✅" : "❌")}");
            
            // Performance breakdown
            if (_sentencesProcessed > 0)
            {
                var totalMs = _traditionalLearningMs + _columnProcessingMs + _patternDetectionMs + 
                              _attentionCalculationMs + _episodicMemoryMs;
                Console.WriteLine($"\n⏱️  Performance Breakdown:");
                Console.WriteLine($"  Traditional learning: {_traditionalLearningMs:F1}ms ({(_traditionalLearningMs/totalMs*100):F1}%)");
                Console.WriteLine($"  Column processing: {_columnProcessingMs:F1}ms ({(_columnProcessingMs/totalMs*100):F1}%)");
                Console.WriteLine($"    - Familiarity checks: {_familiarityCheckMs:F1}ms ({(_familiarityCheckMs/_columnProcessingMs*100):F1}% of column time)");
                Console.WriteLine($"  Pattern detection: {_patternDetectionMs:F1}ms ({(_patternDetectionMs/totalMs*100):F1}%)");
                if (_enableAttention)
                {
                    Console.WriteLine($"  Attention calculation: {_attentionCalculationMs:F1}ms ({(_attentionCalculationMs/totalMs*100):F1}%)");
                }
                if (_enableEpisodicMemory)
                {
                    Console.WriteLine($"  Episodic memory: {_episodicMemoryMs:F1}ms ({(_episodicMemoryMs/totalMs*100):F1}%)");
                }
                Console.WriteLine($"  Total: {totalMs:F1}ms");
            }
            
            if (stats.IntegrationEnabled && stats.IntegrationStats != null)
            {
                Console.WriteLine($"\n🔗 Integration Activity:");
                Console.WriteLine($"  {stats.IntegrationStats}");
            }
            
            if (stats.PatternDetectionStats != null)
            {
                Console.WriteLine($"\n🎯 Pattern Detection:");
                Console.WriteLine($"  {stats.PatternDetectionStats}");
            }
            
            // Week 7: Attention statistics
            if (stats.AttentionEnabled && stats.AttentionStatistics != null)
            {
                Console.WriteLine($"\n👁️  Attention System:");
                Console.WriteLine($"  Patterns processed: {stats.PatternsDetected}");
                Console.WriteLine($"  Patterns skipped: {stats.PatternsSkippedByAttention} " +
                    $"({(stats.PatternsDetected > 0 ? (stats.PatternsSkippedByAttention*100.0/stats.PatternsDetected) : 0):F1}%)");
                Console.WriteLine($"  Total columns: {stats.AttentionStatistics.TotalColumns}");
                Console.WriteLine($"  Active columns: {stats.AttentionStatistics.ActiveColumns}");
                Console.WriteLine($"  Average weight: {stats.AttentionStatistics.AverageWeight:F3}");
            }
            
            // Week 7: Episodic memory statistics
            if (stats.EpisodicMemoryEnabled)
            {
                Console.WriteLine($"\n📚 Episodic Memory:");
                Console.WriteLine($"  Episodes recorded: {stats.EpisodesRecorded}");
            }
            
            Console.WriteLine("\n" + "═".PadRight(80, '═') + "\n");
        }
    }

    /// <summary>
    /// Comprehensive statistics for integrated training
    /// </summary>
    public class IntegratedTrainingStats
    {
        // Training metrics
        public int SentencesProcessed { get; set; }
        public int WordsLearned { get; set; }
        public int VocabularySize { get; set; }
        
        // Integration statistics
        public IntegrationStats? IntegrationStats { get; set; }
        
        // Column statistics
        public int TotalColumns { get; set; }
        
        // Pattern detection
        public int PatternsDetected { get; set; }
        public int IntegrationTriggers { get; set; }
        public PatternDetectionStats? PatternDetectionStats { get; set; }
        
        // Week 7: Attention statistics
        public bool AttentionEnabled { get; set; }
        public int PatternsSkippedByAttention { get; set; }
        public GreyMatter.Core.AttentionStatistics? AttentionStatistics { get; set; }
        
        // Week 7: Episodic memory statistics
        public bool EpisodicMemoryEnabled { get; set; }
        public int EpisodesRecorded { get; set; }
        
        // Configuration
        public bool ColumnProcessingEnabled { get; set; }
        public bool TraditionalLearningEnabled { get; set; }
        public bool IntegrationEnabled { get; set; }
    }
}
