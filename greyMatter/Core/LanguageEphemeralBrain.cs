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
                WordAssociationCount = _wordAssociations.Sum(kvp => kvp.Value.Count)
            };
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
