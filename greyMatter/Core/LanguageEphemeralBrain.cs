using System;
using System.Collections.Generic;
using System.Linq;
using greyMatter.Core;

namespace greyMatter.Core
{
    /// <summary>
    /// Language-specialized ephemeral brain that extends SimpleEphemeralBrain
    /// with vocabulary networks, sentence structure understanding, and semantic relationships
    /// </summary>
    public class LanguageEphemeralBrain : SimpleEphemeralBrain
    {
        private readonly VocabularyNetwork _vocabulary;
        private readonly SentenceStructureAnalyzer _structureAnalyzer;
        private readonly Dictionary<string, int> _wordFrequencies;
        private readonly Dictionary<string, HashSet<string>> _wordAssociations;
        
        public VocabularyNetwork Vocabulary => _vocabulary;
        public int VocabularySize => _vocabulary.WordCount;
        public int LearnedSentences { get; private set; }

        public LanguageEphemeralBrain() : base()
        {
            _vocabulary = new VocabularyNetwork();
            _structureAnalyzer = new SentenceStructureAnalyzer();
            _wordFrequencies = new Dictionary<string, int>();
            _wordAssociations = new Dictionary<string, HashSet<string>>();
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
                
                // If this is word association data, restore associations
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
            
            // Export word associations
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
