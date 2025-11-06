using System;
using System.Collections.Generic;
using System.Linq;
using greyMatter.Core;

namespace greyMatter.Core
{
    /// <summary>
    /// Language-specialized ephemeral brain that extends SimpleEphemeralBrain
    /// with vocabulary networks, sentence structure understanding, and semantic relationships.
    /// 
    /// NOW IMPLEMENTS IIntegratedBrain for bidirectional column-brain communication:
    /// - Columns can trigger learning through pattern detection
    /// - Columns can query existing knowledge to guide processing
    /// - Integration enables biologically-aligned cognitive architecture
    /// </summary>
    public class LanguageEphemeralBrain : SimpleEphemeralBrain, IIntegratedBrain
    {
        private readonly VocabularyNetwork _vocabulary;
        private readonly SentenceStructureAnalyzer _structureAnalyzer;
        private readonly Dictionary<string, int> _wordFrequencies;
        private readonly Dictionary<string, HashSet<string>> _wordAssociations;
        
        // Integration tracking
        private readonly IntegrationStats _integrationStats;
        private readonly Dictionary<string, ConceptKnowledge> _knowledgeCache;
        
        public VocabularyNetwork Vocabulary => _vocabulary;
        public int VocabularySize => _vocabulary.WordCount;
        public int LearnedSentences { get; private set; }

        public LanguageEphemeralBrain() : base()
        {
            _vocabulary = new VocabularyNetwork();
            _structureAnalyzer = new SentenceStructureAnalyzer();
            _wordFrequencies = new Dictionary<string, int>();
            _wordAssociations = new Dictionary<string, HashSet<string>>();
            
            // Initialize integration support
            _integrationStats = new IntegrationStats
            {
                IntegrationStarted = DateTime.Now,
                LastActivity = DateTime.Now
            };
            _knowledgeCache = new Dictionary<string, ConceptKnowledge>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Learn from a sentence, extracting vocabulary, structure, and semantic relationships
        /// </summary>
        public void LearnSentence(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence)) return;

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            
            // Analyze sentence structure
            var structure = _structureAnalyzer.AnalyzeSentence(sentence);
            if (structure == null) return;

            // Extract and learn vocabulary
            foreach (var word in structure.Words)
            {
                LearnWord(word, timestamp);
            }

            // Build word associations from sentence context
            BuildWordAssociations(structure.Words);

            // Create concept for sentence structure (silently)
            var sentenceConcept = $"sentence_{timestamp}";
            LearnSilently(sentenceConcept);

            // Create relationships between words in sentence (silently)
            CreateWordRelationships(structure, sentenceConcept);

            LearnedSentences++;
        }

        /// <summary>
        /// Learn multiple sentences in batch with progress reporting
        /// </summary>
        public void LearnSentencesBatch(IEnumerable<string> sentences, int batchSize = 100)
        {
            var sentenceList = sentences.ToList();
            var totalSentences = sentenceList.Count;
            var processed = 0;
            var startTime = DateTime.Now;

            Console.WriteLine($"Starting to learn {totalSentences:N0} sentences in batches of {batchSize}...");

            foreach (var batch in sentenceList.Chunk(batchSize))
            {
                foreach (var sentence in batch)
                {
                    LearnSentence(sentence);
                    processed++;
                }

                // Progress report
                var elapsed = DateTime.Now - startTime;
                var rate = processed / elapsed.TotalSeconds;
                var eta = TimeSpan.FromSeconds((totalSentences - processed) / Math.Max(rate, 1));
                
                Console.WriteLine($"Processed {processed:N0}/{totalSentences:N0} sentences " +
                                $"({processed * 100.0 / totalSentences:F1}%) " +
                                $"- Rate: {rate:F1}/sec - ETA: {eta:mm\\:ss} " +
                                $"- Vocab: {VocabularySize:N0} words");
            }

            var finalElapsed = DateTime.Now - startTime;
            Console.WriteLine($"Completed learning {totalSentences:N0} sentences in {finalElapsed:mm\\:ss}");
            Console.WriteLine($"Final vocabulary size: {VocabularySize:N0} words");
            Console.WriteLine($"Average rate: {totalSentences / finalElapsed.TotalSeconds:F1} sentences/sec");
        }

        /// <summary>
        /// Find words associated with a given word based on learned context
        /// </summary>
        public List<string> GetWordAssociations(string word, int maxResults = 10)
        {
            word = word.ToLower();
            if (!_wordAssociations.ContainsKey(word))
                return new List<string>();

            return _wordAssociations[word]
                .OrderByDescending(w => GetWordStrength(w))
                .Take(maxResults)
                .ToList();
        }

        /// <summary>
        /// Get the most frequent words learned
        /// </summary>
        public List<(string word, int frequency)> GetTopWords(int count = 100)
        {
            return _wordFrequencies
                .OrderByDescending(kvp => kvp.Value)
                .Take(count)
                .Select(kvp => (kvp.Key, kvp.Value))
                .ToList();
        }

        /// <summary>
        /// Predict missing words in a sentence based on learned patterns
        /// </summary>
        public List<string> PredictMissingWord(string sentenceWithBlank, int maxPredictions = 5)
        {
            // Simple context-based prediction using word associations
            var words = sentenceWithBlank.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w != "_" && !string.IsNullOrWhiteSpace(w))
                .Select(w => w.ToLower().Trim('.', ',', '!', '?'))
                .ToList();

            var candidates = new Dictionary<string, double>();

            foreach (var contextWord in words)
            {
                if (_wordAssociations.ContainsKey(contextWord))
                {
                    foreach (var associatedWord in _wordAssociations[contextWord])
                    {
                        var strength = GetWordStrength(associatedWord);
                        candidates[associatedWord] = candidates.GetValueOrDefault(associatedWord) + strength;
                    }
                }
            }

            return candidates
                .OrderByDescending(kvp => kvp.Value)
                .Take(maxPredictions)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        private void LearnWord(string word, string timestamp)
        {
            word = word.ToLower();
            _vocabulary.AddWord(word);
            _wordFrequencies[word] = _wordFrequencies.GetValueOrDefault(word) + 1;

            // Create neural concept for the word (silently)
            var wordConcept = $"word_{word}_{timestamp}";
            LearnSilently(wordConcept);
        }

        private void BuildWordAssociations(List<string> wordsInSentence)
        {
            // Create associations between all words that appear in the same sentence
            for (int i = 0; i < wordsInSentence.Count; i++)
            {
                var word1 = wordsInSentence[i].ToLower();
                
                if (!_wordAssociations.ContainsKey(word1))
                    _wordAssociations[word1] = new HashSet<string>();

                // Associate with nearby words (window of 3)
                for (int j = Math.Max(0, i - 3); j < Math.Min(wordsInSentence.Count, i + 4); j++)
                {
                    if (i != j)
                    {
                        var word2 = wordsInSentence[j].ToLower();
                        _wordAssociations[word1].Add(word2);
                    }
                }
            }
        }

        private void CreateWordRelationships(SentenceStructure structure, string sentenceConcept)
        {
            // Create neural relationships between sentence components (silently)
            if (!string.IsNullOrEmpty(structure.Subject))
            {
                LearnSilently($"subject_relation_{structure.Subject}_{sentenceConcept}");
            }
            
            if (!string.IsNullOrEmpty(structure.Verb))
            {
                LearnSilently($"verb_relation_{structure.Verb}_{sentenceConcept}");
            }
            
            if (!string.IsNullOrEmpty(structure.Object))
            {
                LearnSilently($"object_relation_{structure.Object}_{sentenceConcept}");
            }
        }

        private double GetWordStrength(string word)
        {
            // Simple strength calculation based on frequency and connections
            var frequency = _wordFrequencies.GetValueOrDefault(word, 0);
            var connections = _wordAssociations.GetValueOrDefault(word, new HashSet<string>()).Count;
            return frequency * 0.7 + connections * 0.3;
        }

        /// <summary>
        /// Get learning statistics for progress tracking
        /// </summary>
        public LanguageLearningStats GetLearningStats()
        {
            return new LanguageLearningStats
            {
                VocabularySize = VocabularySize,
                LearnedSentences = LearnedSentences,
                TotalConcepts = GetActiveClusters().Count(),
                AverageWordFrequency = _wordFrequencies.Count > 0 ? _wordFrequencies.Values.Average() : 0,
                MostFrequentWords = GetTopWords(10),
                WordAssociationCount = _wordAssociations.Sum(kvp => kvp.Value.Count),
                TrainingSessions = GetActiveClusters().Count() // Placeholder until we track sessions properly
            };
        }
        
        /// <summary>
        /// Import vocabulary from biological storage
        /// </summary>
        public void ImportVocabulary(Dictionary<string, WordInfo> vocabulary)
        {
            foreach (var kvp in vocabulary)
            {
                var word = kvp.Key;
                var wordInfo = kvp.Value;
                
                // Add to vocabulary network
                _vocabulary.AddWord(word);
                
                // Update word frequencies
                _wordFrequencies[word] = wordInfo.Frequency;
                
                // Learn the word concept silently to avoid triggering full learning pipeline
                LearnSilently($"word_{word}");
            }
        }
        
        /// <summary>
        /// Import language data (concepts, patterns, etc.) from biological storage
        /// </summary>
        public void ImportLanguageData(Dictionary<string, object> languageData)
        {
            foreach (var kvp in languageData)
            {
                var dataType = kvp.Key;
                var data = kvp.Value;
                
                // Restore learning statistics if available
                if (dataType == "learning_stats" && data is Dictionary<string, object> stats)
                {
                    if (stats.ContainsKey("LearnedSentences") && stats["LearnedSentences"] is int sentences)
                    {
                        LearnedSentences = sentences;
                        Console.WriteLine($"   📊 Restored sentence count: {sentences:N0}");
                    }
                    if (stats.ContainsKey("WordAssociations") && stats["WordAssociations"] is Dictionary<string, object> assocData)
                    {
                        foreach (var assoc in assocData)
                        {
                            if (assoc.Value is List<object> wordList)
                            {
                                var words = wordList.Select(w => w.ToString()).Where(w => !string.IsNullOrEmpty(w)).ToHashSet()!;
                                _wordAssociations[assoc.Key] = words;
                            }
                        }
                        Console.WriteLine($"   🔗 Restored {_wordAssociations.Sum(kvp => kvp.Value.Count):N0} word associations");
                    }
                }
                else
                {
                    // Learn language concepts silently to restore brain state
                    LearnSilently($"language_data_{dataType}");
                    
                    // If this is sentence pattern data, we could reconstruct patterns
                    if (dataType.Contains("sentence_patterns") && data is Dictionary<string, object> patterns)
                    {
                        foreach (var pattern in patterns.Keys)
                        {
                            LearnSilently($"sentence_pattern_{pattern}");
                        }
                    }
                    
                    // If this is word association data, restore associations (legacy support)
                    if (dataType.Contains("word_associations") && data is Dictionary<string, object> associations)
                    {
                        foreach (var assoc in associations)
                        {
                            if (assoc.Value is IEnumerable<string> words)
                            {
                                var wordSet = new HashSet<string>(words);
                                _wordAssociations[assoc.Key] = wordSet;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Import neurons from biological storage back into the brain
        /// This restores the neural network state from persistent storage
        /// </summary>
        public void ImportNeurons(Dictionary<int, object> neurons)
        {
            foreach (var kvp in neurons)
            {
                var neuronId = kvp.Key;
                var neuronData = kvp.Value;
                
                try
                {
                    // Extract neuron information using reflection or dynamic access
                    var neuronType = neuronData.GetType();
                    
                    // Get ActiveConcepts if available
                    var activeConceptsProp = neuronType.GetProperty("ActiveConcepts");
                    HashSet<string> activeConcepts = new HashSet<string>();
                    
                    if (activeConceptsProp != null && activeConceptsProp.GetValue(neuronData) is HashSet<string> concepts)
                    {
                        activeConcepts = concepts;
                    }
                    
                    // Get ActivationCount if available
                    var activationCountProp = neuronType.GetProperty("ActivationCount");
                    int activationCount = 1;
                    
                    if (activationCountProp != null && activationCountProp.GetValue(neuronData) is int count)
                    {
                        activationCount = count;
                    }
                    
                    // Restore the neuron's concepts by triggering concept learning
                    foreach (var concept in activeConcepts)
                    {
                        if (!string.IsNullOrEmpty(concept))
                        {
                            // Silently restore the concept to rebuild neural structure
                            LearnSilently(concept);
                            
                            // Track restored sentences if this appears to be sentence data
                            if (concept.StartsWith("sentence_") || concept.Contains("_sent_"))
                            {
                                LearnedSentences++;
                            }
                        }
                    }
                    
                    // If we can determine word associations from the neuron, restore them
                    if (activeConcepts.Count >= 2)
                    {
                        var conceptList = activeConcepts.ToList();
                        for (int i = 0; i < conceptList.Count; i++)
                        {
                            var concept1 = conceptList[i];
                            
                            // Create word associations between concepts if they look like words
                            if (IsWordConcept(concept1))
                            {
                                var word1 = ExtractWordFromConcept(concept1);
                                if (!_wordAssociations.ContainsKey(word1))
                                {
                                    _wordAssociations[word1] = new HashSet<string>();
                                }
                                
                                // Associate with other word concepts in this neuron
                                for (int j = i + 1; j < conceptList.Count; j++)
                                {
                                    var concept2 = conceptList[j];
                                    if (IsWordConcept(concept2))
                                    {
                                        var word2 = ExtractWordFromConcept(concept2);
                                        _wordAssociations[word1].Add(word2);
                                        
                                        if (!_wordAssociations.ContainsKey(word2))
                                        {
                                            _wordAssociations[word2] = new HashSet<string>();
                                        }
                                        _wordAssociations[word2].Add(word1);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️  Failed to import neuron {neuronId}: {ex.Message}");
                }
            }
            
            Console.WriteLine($"   🧠 Imported {neurons.Count:N0} neurons into brain structure");
        }

        /// <summary>
        /// Check if a concept string represents a word
        /// </summary>
        private bool IsWordConcept(string concept)
        {
            return concept.StartsWith("word_") || 
                   concept.StartsWith("vocab_") ||
                   (!concept.Contains("_") && concept.Length > 1 && concept.Length < 20);
        }

        /// <summary>
        /// Extract the actual word from a word concept string
        /// </summary>
        private string ExtractWordFromConcept(string concept)
        {
            if (concept.StartsWith("word_"))
                return concept.Substring(5);
            if (concept.StartsWith("vocab_"))
                return concept.Substring(6);
            return concept;
        }
        
        /// <summary>
        /// Export current brain state for biological storage
        /// </summary>
        public Dictionary<string, WordInfo> ExportVocabulary()
        {
            var vocabulary = new Dictionary<string, WordInfo>();
            
            foreach (var kvp in _wordFrequencies)
            {
                var word = kvp.Key;
                var frequency = kvp.Value;
                
                vocabulary[word] = new WordInfo
                {
                    Word = word,
                    Frequency = frequency,
                    FirstSeen = DateTime.UtcNow, // We don't track this yet, so use current time
                    EstimatedType = EstimateWordType(word)
                };
            }
            
            return vocabulary;
        }
        
        /// <summary>
        /// Export language data for biological storage
        /// </summary>
        public Dictionary<string, object> ExportLanguageData()
        {
            var languageData = new Dictionary<string, object>();
            
            // Export learning statistics for proper state restoration
            var learningStats = new Dictionary<string, object>
            {
                ["LearnedSentences"] = LearnedSentences,
                ["WordAssociations"] = _wordAssociations.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToList() as object
                )
            };
            languageData["learning_stats"] = learningStats;
            
            // Export word associations (legacy support)
            languageData["word_associations"] = _wordAssociations.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToList() as object
            );
            
            // Export sentence patterns (basic implementation)
            var sentencePatterns = new Dictionary<string, object>();
            foreach (var clusterKvp in GetActiveClusters())
            {
                var cluster = clusterKvp.Value;
                if (clusterKvp.Key.Contains("sentence"))
                {
                    sentencePatterns[clusterKvp.Key] = new
                    {
                        CreatedAt = DateTime.UtcNow,
                        NeuronCount = cluster.ActiveNeurons.Count,
                        Strength = cluster.ActivationLevel
                    };
                }
            }
            languageData["sentence_patterns"] = sentencePatterns;
            
            return languageData;
        }

        /// <summary>
        /// Export individual neural concepts for semantic domain storage
        /// This enables the biological storage manager to categorize concepts
        /// into Huth's hierarchical semantic domain architecture
        /// </summary>
        public Dictionary<string, object> ExportNeuralConcepts()
        {
            var concepts = new Dictionary<string, object>();
            
            // Export all active neural clusters as individual concepts
            foreach (var clusterKvp in GetActiveClusters())
            {
                var clusterId = clusterKvp.Key;
                var cluster = clusterKvp.Value;
                
                // Create concept data structure
                var conceptData = new
                {
                    Id = clusterId,
                    Type = DetermineConceptType(clusterId),
                    CreatedAt = DateTime.UtcNow,
                    NeuronCount = cluster.ActiveNeurons.Count,
                    ActivationLevel = cluster.ActivationLevel,
                    ActiveNeurons = cluster.ActiveNeurons.ToList(),
                    LastActivated = DateTime.UtcNow,
                    ActivationHistory = new List<DateTime> { DateTime.UtcNow },
                    AssociatedWords = GetWordsForCluster(clusterId),
                    Context = GetClusterContext(clusterId)
                };
                
                concepts[clusterId] = conceptData;
            }
            
            return concepts;
        }

        /// <summary>
        /// Export all neurons for persistence to storage manager
        /// This enables proper neuron transfer to the SemanticStorageManager pool
        /// </summary>
        public Dictionary<int, object> ExportNeurons()
        {
            var neurons = new Dictionary<int, object>();
            var neuronId = 0;
            
            // Export neurons from all active clusters
            foreach (var clusterKvp in GetActiveClusters())
            {
                var cluster = clusterKvp.Value;
                
                foreach (var neuron in cluster.ActiveNeurons)
                {
                    var neuronData = new
                    {
                        Id = neuronId,
                        ClusterId = clusterKvp.Key,
                        Type = "language", // All neurons in LanguageEphemeralBrain are language neurons
                        Weights = neuron.Weights.ToDictionary(kvp => kvp.Key, kvp => kvp.Value), // Export actual connection weights
                        ActiveConcepts = new HashSet<string> { clusterKvp.Key },
                        LastActivated = DateTime.UtcNow,
                        ActivationCount = neuron.ActivationCount,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    neurons[neuronId] = neuronData;
                    neuronId++;
                }
            }
            
            return neurons;
        }

        /// <summary>
        /// Determine the type of concept based on cluster ID and context
        /// </summary>
        private string DetermineConceptType(string clusterId)
        {
            if (clusterId.Contains("word_") || clusterId.Contains("vocab_"))
                return "vocabulary";
            if (clusterId.Contains("sentence_") || clusterId.Contains("phrase_"))
                return "linguistic";
            if (clusterId.Contains("pattern_") || clusterId.Contains("grammar_"))
                return "syntactic";
            if (clusterId.Contains("semantic_") || clusterId.Contains("meaning_"))
                return "semantic";
            if (clusterId.Contains("association_") || clusterId.Contains("relation_"))
                return "associative";
            
            return "general";
        }

        /// <summary>
        /// Get words associated with a neural cluster
        /// </summary>
        private List<string> GetWordsForCluster(string clusterId)
        {
            var words = new List<string>();
            
            // Check word associations for this cluster
            foreach (var wordAssoc in _wordAssociations)
            {
                if (wordAssoc.Value.Any(assoc => assoc.Contains(clusterId)))
                {
                    words.Add(wordAssoc.Key);
                }
            }
            
            return words;
        }

        /// <summary>
        /// Get contextual information for a cluster
        /// </summary>
        private Dictionary<string, object> GetClusterContext(string clusterId)
        {
            var context = new Dictionary<string, object>();
            
            // Add frequency information
            var associatedWords = GetWordsForCluster(clusterId);
            if (associatedWords.Any())
            {
                var avgFrequency = associatedWords.Average(word => 
                    _vocabulary.GetWordInfo(word)?.Frequency ?? 0);
                context["average_word_frequency"] = avgFrequency;
                context["word_count"] = associatedWords.Count;
            }
            
            // Add activation patterns
            context["cluster_type"] = DetermineConceptType(clusterId);
            context["creation_time"] = DateTime.UtcNow;
            
            return context;
        }
        
        /// <summary>
        /// Simple word type estimation (placeholder)
        /// </summary>
        private WordType EstimateWordType(string word)
        {
            // Very basic estimation - would need proper NLP in production
            if (word.EndsWith("ing") || word.EndsWith("ed")) return WordType.Verb;
            if (word.EndsWith("ly")) return WordType.Adverb;
            if (word.EndsWith("tion") || word.EndsWith("ness")) return WordType.Noun;
            return WordType.Unknown;
        }
        
        // ═══════════════════════════════════════════════════════════════════════
        // IIntegratedBrain Implementation - Column-Brain Integration
        // ═══════════════════════════════════════════════════════════════════════
        
        #region Column → Brain Communication (Pattern-triggered Learning)
        
        /// <summary>
        /// Notify brain of significant pattern detected in column activity.
        /// Triggers Hebbian learning for novel concepts or reinforces existing ones.
        /// </summary>
        public async Task NotifyColumnPatternAsync(ColumnPattern pattern)
        {
            _integrationStats.PatternsNotified++;
            _integrationStats.LastActivity = DateTime.Now;
            
            try
            {
                // Learn the primary concept (silently for continuous operation)
                if (!string.IsNullOrWhiteSpace(pattern.PrimaryConcept))
                {
                    LearnSilently(pattern.PrimaryConcept);
                    _integrationStats.LearningTriggersTotal++;
                }
                
                // Form associations between related concepts
                for (int i = 0; i < pattern.RelatedConcepts.Count; i++)
                {
                    for (int j = i + 1; j < pattern.RelatedConcepts.Count; j++)
                    {
                        await StrengthenAssociationAsync(
                            pattern.RelatedConcepts[i],
                            pattern.RelatedConcepts[j],
                            pattern.Confidence
                        );
                    }
                }
                
                // Learn from context if provided
                if (!string.IsNullOrWhiteSpace(pattern.Context))
                {
                    LearnSentence(pattern.Context);
                }
                
                // Invalidate knowledge cache for updated concepts
                _knowledgeCache.Remove(pattern.PrimaryConcept);
                foreach (var concept in pattern.RelatedConcepts)
                {
                    _knowledgeCache.Remove(concept);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Error processing column pattern: {ex.Message}");
            }
            
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Strengthen association between two concepts based on column co-activation.
        /// Implements spike-timing dependent plasticity principle.
        /// </summary>
        public async Task StrengthenAssociationAsync(string concept1, string concept2, double strength)
        {
            if (string.IsNullOrWhiteSpace(concept1) || string.IsNullOrWhiteSpace(concept2))
                return;
                
            _integrationStats.AssociationsStrengthened++;
            _integrationStats.LastActivity = DateTime.Now;
            
            try
            {
                // Build word association (bidirectional)
                BuildWordAssociation(concept1, concept2);
                BuildWordAssociation(concept2, concept1);
                
                // Strengthen neural connections if both concepts have neurons
                var neurons1 = GetNeuronsForConcept(concept1);
                var neurons2 = GetNeuronsForConcept(concept2);
                
                if (neurons1.Any() && neurons2.Any())
                {
                    // Create Hebbian connections between the neuron groups
                    foreach (var n1 in neurons1.Take(5)) // Limit connections for efficiency
                    {
                        foreach (var n2 in neurons2.Take(5))
                        {
                            StrengthenConnection(n1, n2, strength);
                        }
                    }
                }
                
                // Invalidate cache
                _knowledgeCache.Remove(concept1);
                _knowledgeCache.Remove(concept2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Error strengthening association: {ex.Message}");
            }
            
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Register word from column consensus.
        /// Adds to vocabulary if novel, reinforces if known.
        /// </summary>
        public async Task RegisterWordFromColumnsAsync(string word, double confidence)
        {
            if (string.IsNullOrWhiteSpace(word))
                return;
                
            _integrationStats.WordsRegisteredFromColumns++;
            _integrationStats.LastActivity = DateTime.Now;
            
            try
            {
                // Add to vocabulary
                _vocabulary.AddWord(word);
                
                // Track frequency
                if (!_wordFrequencies.ContainsKey(word))
                    _wordFrequencies[word] = 0;
                _wordFrequencies[word]++;
                
                // If high confidence, also trigger learning
                if (confidence >= 0.7)
                {
                    Learn(word);
                    _integrationStats.LearningTriggersTotal++;
                }
                
                // Invalidate cache
                _knowledgeCache.Remove(word);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Error registering word from columns: {ex.Message}");
            }
            
            await Task.CompletedTask;
        }
        
        #endregion
        
        #region Brain → Column Communication (Knowledge-guided Processing)
        
        /// <summary>
        /// Query existing knowledge about a word/concept.
        /// Returns familiarity score, frequency, and related concepts.
        /// </summary>
        public async Task<ConceptKnowledge> QueryKnowledgeAsync(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return new ConceptKnowledge { Concept = word };
                
            _integrationStats.KnowledgeQueriesTotal++;
            _integrationStats.LastActivity = DateTime.Now;
            
            // Check cache first
            if (_knowledgeCache.TryGetValue(word, out var cached))
            {
                _integrationStats.KnowledgeHits++;
                return cached;
            }
            
            try
            {
                var knowledge = new ConceptKnowledge
                {
                    Concept = word,
                    Familiarity = await GetWordFamiliarityAsync(word),
                    Frequency = _wordFrequencies.GetValueOrDefault(word, 0),
                    LastSeen = DateTime.Now,
                    Associations = GetWordAssociations(word, 10)
                        .Select(w => (w, 0.8)) // Default strength
                        .ToList(),
                    NeuronIds = GetNeuronsForConcept(word),
                    ConnectionStrength = CalculateConnectionStrength(word)
                };
                
                // Cache the result
                _knowledgeCache[word] = knowledge;
                
                if (knowledge.Familiarity > 0)
                    _integrationStats.KnowledgeHits++;
                else
                    _integrationStats.KnowledgeMisses++;
                
                return knowledge;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Error querying knowledge: {ex.Message}");
                _integrationStats.KnowledgeMisses++;
                return new ConceptKnowledge { Concept = word };
            }
        }
        
        /// <summary>
        /// Get related concepts through learned associations.
        /// Used by columns for context-aware message routing.
        /// </summary>
        public async Task<List<string>> GetRelatedConceptsAsync(string word, int maxResults = 10)
        {
            if (string.IsNullOrWhiteSpace(word))
                return new List<string>();
                
            _integrationStats.RelatedConceptsRequests++;
            _integrationStats.LastActivity = DateTime.Now;
            
            try
            {
                return GetWordAssociations(word, maxResults);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Error getting related concepts: {ex.Message}");
                return new List<string>();
            }
        }
        
        /// <summary>
        /// Get familiarity score for a word (0.0 = unknown, 1.0 = very familiar).
        /// Based on frequency, recency, and connection strength.
        /// </summary>
        public async Task<double> GetWordFamiliarityAsync(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return 0.0;
                
            _integrationStats.FamiliarityChecks++;
            _integrationStats.LastActivity = DateTime.Now;
            
            try
            {
                // Check if word is in vocabulary
                if (!_vocabulary.IsKnownWord(word))
                    return 0.0;
                
                // Calculate familiarity based on multiple factors
                double frequency = _wordFrequencies.GetValueOrDefault(word, 0);
                double maxFrequency = _wordFrequencies.Values.DefaultIfEmpty(1).Max();
                double frequencyScore = maxFrequency > 0 ? frequency / maxFrequency : 0.0;
                
                // Connection strength
                double connectionScore = CalculateConnectionStrength(word);
                
                // Association richness
                int associationCount = GetWordAssociations(word, 100).Count;
                double associationScore = Math.Min(associationCount / 10.0, 1.0);
                
                // Weighted combination
                double familiarity = (frequencyScore * 0.4) + 
                                   (connectionScore * 0.4) + 
                                   (associationScore * 0.2);
                
                return Math.Min(familiarity, 1.0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Error calculating familiarity: {ex.Message}");
                return 0.0;
            }
        }
        
        /// <summary>
        /// Fast check if word exists in vocabulary.
        /// </summary>
        public bool IsKnownWord(string word)
        {
            return _vocabulary.IsKnownWord(word);
        }
        
        #endregion
        
        #region Integration Statistics
        
        /// <summary>
        /// Get statistics about integration activity.
        /// </summary>
        public IntegrationStats GetIntegrationStats()
        {
            return _integrationStats;
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Build word association (internal helper).
        /// </summary>
        private void BuildWordAssociation(string word1, string word2)
        {
            if (!_wordAssociations.ContainsKey(word1))
                _wordAssociations[word1] = new HashSet<string>();
            _wordAssociations[word1].Add(word2);
        }
        
        /// <summary>
        /// Calculate connection strength for a word based on neural connections.
        /// </summary>
        private double CalculateConnectionStrength(string word)
        {
            var neurons = GetNeuronsForConcept(word);
            if (!neurons.Any())
                return 0.0;
            
            // Average activation strength of clusters containing these neurons
            double totalActivation = 0.0;
            int count = 0;
            
            foreach (var neuronId in neurons.Take(10))
            {
                var cluster = GetActiveClusters().Values
                    .FirstOrDefault(c => c.ActiveNeurons.Any(n => n.Id == neuronId));
                    
                if (cluster != null)
                {
                    totalActivation += cluster.ActivationLevel;
                    count++;
                }
            }
            
            return count > 0 ? totalActivation / count : 0.0;
        }
        
        /// <summary>
        /// Get neuron IDs associated with a concept.
        /// </summary>
        private List<int> GetNeuronsForConcept(string concept)
        {
            var neurons = new List<int>();
            
            foreach (var cluster in GetActiveClusters().Values)
            {
                if (cluster.Concept.Contains(concept, StringComparison.OrdinalIgnoreCase))
                {
                    neurons.AddRange(cluster.ActiveNeurons.Select(n => n.Id));
                }
            }
            
            return neurons.Distinct().ToList();
        }
        
        /// <summary>
        /// Strengthen connection between two neurons (Hebbian learning).
        /// </summary>
        private void StrengthenConnection(int neuronId1, int neuronId2, double strength)
        {
            // This would integrate with the neural connection system
            // For now, we're using the existing Learn() and association mechanisms
            // In future: direct synaptic weight manipulation
        }
        
        #endregion
    }

    /// <summary>
    /// Statistics about language learning progress
    /// </summary>
    public class LanguageLearningStats
    {
        public int VocabularySize { get; set; }
        public int LearnedSentences { get; set; }
        public int TotalConcepts { get; set; }
        public double AverageWordFrequency { get; set; }
        public List<(string word, int frequency)> MostFrequentWords { get; set; } = new();
        public int WordAssociationCount { get; set; }
        public int TrainingSessions { get; set; }
    }

    /// <summary>
    /// Extension methods for supporting language learning operations
    /// </summary>
    public static class LanguageLearningExtensions
    {
        /// <summary>
        /// Chunk an enumerable into batches of specified size
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int size)
        {
            if (size <= 0) throw new ArgumentException("Chunk size must be greater than 0", nameof(size));
            
            var list = source.ToList();
            for (int i = 0; i < list.Count; i += size)
            {
                yield return list.Skip(i).Take(size);
            }
        }
    }
}
