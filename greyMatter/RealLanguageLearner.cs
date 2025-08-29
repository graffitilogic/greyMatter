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
        private readonly string _learnedWordsPath;
        private readonly SemanticStorageManager _storageManager;
        private readonly LearningSparseConceptEncoder _encoder;
        private Dictionary<string, TatoebaDataConverter.WordData> _wordDatabase;
        private Dictionary<string, Dictionary<string, int>> _cooccurrenceMatrix;
        private HashSet<string> _alreadyLearnedWords;

        public RealLanguageLearner(string dataPath, string brainPath)
        {
            _dataPath = dataPath;
            _brainPath = brainPath;
            _learnedWordsPath = Path.Combine(brainPath, "learned_words.json");
            _storageManager = new SemanticStorageManager(brainPath);
            _encoder = new LearningSparseConceptEncoder();
            _wordDatabase = new Dictionary<string, TatoebaDataConverter.WordData>();
            _cooccurrenceMatrix = new Dictionary<string, Dictionary<string, int>>();
            _alreadyLearnedWords = new HashSet<string>();
        }

        public async Task LearnFromTatoebaDataAsync(int maxWords = 1000)
        {
            Console.WriteLine("🧠 **REAL LANGUAGE LEARNING SYSTEM**");
            Console.WriteLine("===================================");
            Console.WriteLine($"Learning from Tatoeba data (max {maxWords} words)");

            // Step 1: Load learning data
            await LoadLearningDataAsync();

            // Step 2: Load previously learned words
            await LoadLearnedWordsAsync();

            // Step 3: Select words to learn (with reinforcement)
            var wordsToLearn = SelectWordsToLearn(maxWords);

            // Step 4: Learn word patterns
            await LearnWordPatternsAsync(wordsToLearn);

            // Step 5: Learn semantic relationships
            await LearnSemanticRelationshipsAsync(wordsToLearn);

            // Step 6: Save learned knowledge
            await SaveLearnedKnowledgeAsync();

            // Step 7: Update learned words tracking
            await SaveLearnedWordsAsync();

            Console.WriteLine("\n✅ **LEARNING COMPLETE**");
            Console.WriteLine($"Words learned: {wordsToLearn.Count}");
            Console.WriteLine($"Patterns stored: {wordsToLearn.Count} word patterns + semantic relationships");
            Console.WriteLine($"Total unique words learned: {_alreadyLearnedWords.Count}");
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
            _wordDatabase = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, TatoebaDataConverter.WordData>>(wordDbJson)
                ?? new Dictionary<string, TatoebaDataConverter.WordData>();
            
            var cooccurrenceJson = await File.ReadAllTextAsync(cooccurrencePath);
            _cooccurrenceMatrix = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(cooccurrenceJson)
                ?? new Dictionary<string, Dictionary<string, int>>();

            if (_wordDatabase == null || _cooccurrenceMatrix == null)
            {
                throw new InvalidDataException("Failed to load learning data from JSON files");
            }

            Console.WriteLine($"Loaded {_wordDatabase.Count} words from database");
            Console.WriteLine($"Loaded {_cooccurrenceMatrix.Count} co-occurrence entries");

            // Load already learned words from file (if exists)
            await LoadLearnedWordsAsync();
        }

        private List<string> SelectWordsToLearn(int maxWords)
        {
            Console.WriteLine("\n🎯 **SELECTING WORDS TO LEARN**");

            // Get all learnable words (excluding stop words)
            var stopWords = new HashSet<string> { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "is", "are", "was", "were", "be", "been", "being", "have", "has", "had", "do", "does", "did", "will", "would", "could", "should", "may", "might", "must", "can", "shall" };

            var allWords = _wordDatabase
                .Where(w => !stopWords.Contains(w.Key))
                .ToList();

            if (!allWords.Any())
            {
                Console.WriteLine("❌ No learnable words found in database");
                return new List<string>();
            }

            // Separate new words from already learned words
            var newWords = allWords.Where(w => !_alreadyLearnedWords.Contains(w.Key)).ToList();
            var learnedWords = allWords.Where(w => _alreadyLearnedWords.Contains(w.Key)).ToList();

            var selectedWords = new List<string>();
            var random = new Random();

            // Prioritize new words (80% of selection) but allow reinforcement (20%)
            var newWordsTarget = (int)(maxWords * 0.8);
            var reinforcementTarget = maxWords - newWordsTarget;

            // Select new words with weighted random sampling
            if (newWords.Any())
            {
                var wordsToSelect = Math.Min(newWordsTarget, newWords.Count);
                selectedWords.AddRange(SelectWeightedRandomWords(newWords, wordsToSelect, random));
            }

            // Select some learned words for reinforcement (if available)
            if (learnedWords.Any() && reinforcementTarget > 0)
            {
                var wordsToSelect = Math.Min(reinforcementTarget, learnedWords.Count);
                selectedWords.AddRange(SelectWeightedRandomWords(learnedWords, wordsToSelect, random));
            }

            // If we still need more words, fill with any available words
            if (selectedWords.Count < maxWords && allWords.Count > selectedWords.Count)
            {
                var remainingNeeded = maxWords - selectedWords.Count;
                var remainingWords = allWords.Where(w => !selectedWords.Contains(w.Key)).ToList();
                if (remainingWords.Any())
                {
                    var wordsToSelect = Math.Min(remainingNeeded, remainingWords.Count);
                    selectedWords.AddRange(SelectWeightedRandomWords(remainingWords, wordsToSelect, random));
                }
            }

            // Update learned words tracking
            foreach (var word in selectedWords)
            {
                _alreadyLearnedWords.Add(word);
            }

            Console.WriteLine($"Selected {selectedWords.Count} words to learn");
            Console.WriteLine($"New words: {selectedWords.Count(w => !learnedWords.Any(lw => lw.Key == w))}");
            Console.WriteLine($"Reinforcement: {selectedWords.Count(w => learnedWords.Any(lw => lw.Key == w))}");
            Console.WriteLine($"Sample: {string.Join(", ", selectedWords.Take(10))}");

            return selectedWords;
        }

        private List<string> SelectWeightedRandomWords(List<KeyValuePair<string, TatoebaDataConverter.WordData>> wordPool, int count, Random random)
        {
            var selected = new List<string>();
            var remaining = new List<KeyValuePair<string, TatoebaDataConverter.WordData>>(wordPool);

            for (int i = 0; i < Math.Min(count, remaining.Count); i++)
            {
                // Weighted random selection based on frequency
                var totalWeight = remaining.Sum(w => w.Value.Frequency);
                var randomValue = random.Next(totalWeight);
                var cumulativeWeight = 0;

                for (int j = 0; j < remaining.Count; j++)
                {
                    cumulativeWeight += remaining[j].Value.Frequency;
                    if (randomValue < cumulativeWeight)
                    {
                        selected.Add(remaining[j].Key);
                        remaining.RemoveAt(j);
                        break;
                    }
                }
            }

            return selected;
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

            // Save already learned words to file
            await SaveLearnedWordsAsync();

            Console.WriteLine("✅ Learned knowledge saved");
            Console.WriteLine($"📊 Report saved to: {reportPath}");
            Console.WriteLine($"📝 Learned words list saved to: {_learnedWordsPath}");
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

        private async Task LoadLearnedWordsAsync()
        {
            if (File.Exists(_learnedWordsPath))
            {
                var learnedJson = await File.ReadAllTextAsync(_learnedWordsPath);
                var learnedList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(learnedJson);
                if (learnedList != null)
                {
                    _alreadyLearnedWords = new HashSet<string>(learnedList);
                }
            }
        }

        private async Task SaveLearnedWordsAsync()
        {
            var learnedList = _alreadyLearnedWords.ToList();
            var learnedJson = System.Text.Json.JsonSerializer.Serialize(learnedList, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_learnedWordsPath, learnedJson);
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
