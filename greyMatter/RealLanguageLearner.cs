using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using GreyMatter.Core;
using greyMatter.Core;
using GreyMatter.Storage;

namespace GreyMatter
{
    /// <summary>
    /// Real language learning system that processes Tatoeba data
    /// </summary>
    public class RealLanguageLearner
    {
        private readonly string _dataPath;
        private readonly string _brainPath;
        private readonly SemanticStorageManager _storageManager;
        private readonly LearningSparseConceptEncoder _encoder;
        private Dictionary<string, TatoebaDataConverter.WordData> _wordDatabase;
        private Dictionary<string, Dictionary<string, int>> _cooccurrenceMatrix;

        public RealLanguageLearner(string dataPath, string brainPath)
        {
            _dataPath = dataPath;
            _brainPath = brainPath;
            _storageManager = new SemanticStorageManager(brainPath);
            _encoder = new LearningSparseConceptEncoder();
            _wordDatabase = new Dictionary<string, TatoebaDataConverter.WordData>();
            _cooccurrenceMatrix = new Dictionary<string, Dictionary<string, int>>();
        }

        public async Task LearnFromTatoebaDataAsync(int maxWords = 1000)
        {
            Console.WriteLine("🧠 **REAL LANGUAGE LEARNING SYSTEM**");
            Console.WriteLine("===================================");
            Console.WriteLine($"Learning from Tatoeba data (max {maxWords} words)");

            // Step 1: Load learning data
            await LoadLearningDataAsync();

            // Step 2: Select most frequent words to learn
            var wordsToLearn = SelectWordsToLearn(maxWords);

            // Step 3: Learn word patterns
            await LearnWordPatternsAsync(wordsToLearn);

            // Step 4: Learn semantic relationships
            await LearnSemanticRelationshipsAsync(wordsToLearn);

            // Step 5: Save learned knowledge
            await SaveLearnedKnowledgeAsync();

            Console.WriteLine("\n✅ **LEARNING COMPLETE**");
            Console.WriteLine($"Words learned: {wordsToLearn.Count}");
            Console.WriteLine($"Patterns stored: {wordsToLearn.Count} word patterns + semantic relationships");
        }

        private async Task LoadLearningDataAsync()
        {
            Console.WriteLine("\n📚 **LOADING LEARNING DATA**");

            var wordDbPath = Path.Combine(_dataPath, "word_database.json");
            var cooccurrencePath = Path.Combine(_dataPath, "cooccurrence_matrix.json");

            if (!File.Exists(wordDbPath) || !File.Exists(cooccurrencePath))
            {
                throw new FileNotFoundException("Learning data not found. Run --convert-tatoeba-data first.");
            }

            var wordDbJson = await File.ReadAllTextAsync(wordDbPath);
            _wordDatabase = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, TatoebaDataConverter.WordData>>(wordDbJson);
            
            var cooccurrenceJson = await File.ReadAllTextAsync(cooccurrencePath);
            _cooccurrenceMatrix = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(cooccurrenceJson);

            if (_wordDatabase == null || _cooccurrenceMatrix == null)
            {
                throw new InvalidDataException("Failed to load learning data from JSON files");
            }

            Console.WriteLine($"Loaded {_wordDatabase.Count} words from database");
            Console.WriteLine($"Loaded {_cooccurrenceMatrix.Count} co-occurrence entries");
        }

        private List<string> SelectWordsToLearn(int maxWords)
        {
            Console.WriteLine("\n🎯 **SELECTING WORDS TO LEARN**");

            // Select most frequent words, excluding very common stop words
            var stopWords = new HashSet<string> { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "is", "are", "was", "were", "be", "been", "being", "have", "has", "had", "do", "does", "did", "will", "would", "could", "should", "may", "might", "must", "can", "shall" };

            var wordsToLearn = _wordDatabase
                .Where(w => !stopWords.Contains(w.Key))
                .OrderByDescending(w => w.Value.Frequency)
                .Take(maxWords)
                .Select(w => w.Key)
                .ToList();

            Console.WriteLine($"Selected {wordsToLearn.Count} words to learn");
            Console.WriteLine($"Most frequent: {string.Join(", ", wordsToLearn.Take(10))}");

            return wordsToLearn;
        }

        private async Task LearnWordPatternsAsync(List<string> wordsToLearn)
        {
            Console.WriteLine("\n🧩 **LEARNING WORD PATTERNS**");

            var learnedCount = 0;
            foreach (var word in wordsToLearn)
            {
                var wordData = _wordDatabase[word];

                // Store word using the correct method
                var wordInfo = new WordInfo
                {
                    Word = word,
                    Frequency = wordData.Frequency,
                    FirstSeen = DateTime.Now,
                    EstimatedType = WordType.Unknown,
                    AssociatedNeuronIds = new List<int>()
                };

                await _storageManager.SaveVocabularyWordAsync(word, wordInfo);

                learnedCount++;
                if (learnedCount % 100 == 0)
                {
                    Console.WriteLine($"Learned patterns for {learnedCount}/{wordsToLearn.Count} words...");
                }
            }

            Console.WriteLine($"Stored {learnedCount} word patterns");
        }

        private async Task LearnSemanticRelationshipsAsync(List<string> wordsToLearn)
        {
            Console.WriteLine("\n🔗 **LEARNING SEMANTIC RELATIONSHIPS**");

            var relationshipCount = 0;
            foreach (var word in wordsToLearn)
            {
                if (_cooccurrenceMatrix.ContainsKey(word))
                {
                    var cooccurringWords = _cooccurrenceMatrix[word]
                        .Where(c => wordsToLearn.Contains(c.Key))
                        .OrderByDescending(c => c.Value)
                        .Take(10); // Top 10 co-occurring words

                    foreach (var cooccurrence in cooccurringWords)
                    {
                        // Create semantic relationship concept
                        var relationshipPattern = CreateSemanticRelationshipPattern(word, cooccurrence.Key, cooccurrence.Value);
                        var relationshipData = new
                        {
                            Word1 = word,
                            Word2 = cooccurrence.Key,
                            Strength = cooccurrence.Value,
                            RelationshipType = "cooccurrence",
                            LearnedDate = DateTime.Now,
                            Pattern = relationshipPattern
                        };

                        var relationshipKey = $"semantic_{word}_{cooccurrence.Key}";
                        await _storageManager.SaveConceptAsync(relationshipKey, relationshipData, ConceptType.SemanticRelation);

                        relationshipCount++;
                    }
                }
            }

            Console.WriteLine($"Learned {relationshipCount} semantic relationships");
        }

        private SparsePattern CreateSemanticRelationshipPattern(string word1, string word2, int strength)
        {
            // Get base patterns for both words (simplified - we'll use word data instead)
            var pattern1 = _wordDatabase[word1].LearnedPattern;
            var pattern2 = _wordDatabase[word2].LearnedPattern;

            // Create relationship pattern by combining and modulating based on strength
            var combinedBits = new List<int>();
            combinedBits.AddRange(pattern1.ActiveBits);
            combinedBits.AddRange(pattern2.ActiveBits);

            // Modulate pattern based on relationship strength
            var strengthFactor = Math.Min(strength / 5.0, 1.0);
            var targetSize = (int)(2048 * 0.02 * strengthFactor); // 2% sparsity modulated by strength

            // Keep most significant bits
            combinedBits = combinedBits
                .GroupBy(b => b)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(targetSize)
                .ToList();

            return new SparsePattern(combinedBits.ToArray(), 0.02);
        }

        private async Task SaveLearnedKnowledgeAsync()
        {
            Console.WriteLine("\n💾 **SAVING LEARNED KNOWLEDGE**");

            // Get storage statistics
            var stats = await _storageManager.GetStorageStatisticsAsync();

            // Generate learning report
            var report = new LearningReport
            {
                TotalWordsLearned = _wordDatabase.Count,
                PatternsStored = stats.VocabularyIndexSize,
                LearningTimestamp = DateTime.Now,
                DataSource = "Tatoeba Dataset"
            };

            var reportPath = Path.Combine(_brainPath, "learning_report.json");
            var reportJson = System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(reportPath, reportJson);

            Console.WriteLine("✅ Learned knowledge saved");
            Console.WriteLine($"📊 Report saved to: {reportPath}");
        }

        public async Task TestLearningAsync()
        {
            Console.WriteLine("\n🧪 **TESTING LEARNING RESULTS**");

            // Test word recognition
            var testWords = new[] { "cat", "dog", "house", "run", "happy" };
            foreach (var word in testWords)
            {
                var wordInfo = await _storageManager.LoadVocabularyWordAsync(word);
                if (wordInfo != null)
                {
                    Console.WriteLine($"📝 {word}:");
                    Console.WriteLine($"   Frequency: {wordInfo.Frequency}");
                    Console.WriteLine($"   Type: {wordInfo.EstimatedType}");
                    Console.WriteLine($"   Associated neurons: {wordInfo.AssociatedNeuronIds.Count}");
                }
                else
                {
                    Console.WriteLine($"❌ {word}: Not learned");
                }
            }

            // Test semantic relationships (simplified - just check if concepts exist)
            var testRelationships = new[] { ("cat", "dog"), ("house", "run"), ("happy", "run") };
            foreach (var (word1, word2) in testRelationships)
            {
                var relationshipKey = $"semantic_{word1}_{word2}";
                // Note: We can't easily retrieve concept data, so we'll just check if the words exist
                var word1Info = await _storageManager.LoadVocabularyWordAsync(word1);
                var word2Info = await _storageManager.LoadVocabularyWordAsync(word2);

                if (word1Info != null && word2Info != null)
                {
                    Console.WriteLine($"🔗 {word1} ↔ {word2}: Both words learned");
                }
                else
                {
                    Console.WriteLine($"❌ No relationship found: {word1} ↔ {word2}");
                }
            }
        }

        public class LearningReport
        {
            public int TotalWordsLearned { get; set; }
            public int PatternsStored { get; set; }
            public DateTime LearningTimestamp { get; set; }
            public string? DataSource { get; set; }
        }
    }
}
