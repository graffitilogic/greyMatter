using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using GreyMatter.Core;
using GreyMatter.Storage;
using System.Threading;

namespace GreyMatter
{
    /// <summary>
    /// Enhanced language learning system for scaling to 5,000+ words with parallel processing
    /// </summary>
    public class EnhancedLanguageLearner
    {
        private readonly string _dataPath;
        private readonly string _brainPath;
        private readonly string _learnedWordsPath;
        private readonly SemanticStorageManager _storageManager;
        private readonly LearningSparseConceptEncoder _encoder;
        private Dictionary<string, TatoebaDataConverter.WordData> _wordDatabase;
        private Dictionary<string, Dictionary<string, int>> _cooccurrenceMatrix;
        private HashSet<string> _alreadyLearnedWords;
        private readonly int _maxConcurrency;

        public EnhancedLanguageLearner(string dataPath, string brainPath, int maxConcurrency = 4)
        {
            _dataPath = dataPath;
            _brainPath = brainPath;
            _learnedWordsPath = Path.Combine(brainPath, "learned_words.json");
            _storageManager = new SemanticStorageManager(brainPath);
            _encoder = new LearningSparseConceptEncoder(_storageManager);
            _wordDatabase = new Dictionary<string, TatoebaDataConverter.WordData>();
            _cooccurrenceMatrix = new Dictionary<string, Dictionary<string, int>>();
            _alreadyLearnedWords = new HashSet<string>();
            _maxConcurrency = maxConcurrency;
        }

        public async Task LearnVocabularyAtScaleAsync(int targetVocabularySize = 5000, int batchSize = 500)
        {
            Console.WriteLine("🚀 **ENHANCED LANGUAGE LEARNING SYSTEM - PHASE 4**");
            Console.WriteLine("=================================================");
            Console.WriteLine($"Target: {targetVocabularySize:N0} words | Batch Size: {batchSize} | Max Concurrency: {_maxConcurrency}");

            var stopwatch = Stopwatch.StartNew();

            // Step 1: Load learning data
            await LoadLearningDataAsync();

            // Step 2: Load previously learned words
            await LoadLearnedWordsAsync();

            // Step 3: Calculate learning plan
            var learningPlan = CalculateLearningPlan(targetVocabularySize, batchSize);
            Console.WriteLine($"\n📊 **LEARNING PLAN**");
            Console.WriteLine($"Current vocabulary: {_alreadyLearnedWords.Count:N0} words");
            Console.WriteLine($"New words to learn: {learningPlan.NewWordsToLearn:N0}");
            Console.WriteLine($"Reinforcement words: {learningPlan.ReinforcementWords:N0}");
            Console.WriteLine($"Total batches: {learningPlan.TotalBatches}");
            Console.WriteLine($"Estimated time: {learningPlan.EstimatedTimeMinutes} minutes");

            // Step 4: Execute learning in batches
            await ExecuteBatchLearningAsync(learningPlan, batchSize);

            // Step 5: Final validation and reporting
            await PerformFinalValidationAsync();

            stopwatch.Stop();
            Console.WriteLine($"\n⏱️ **LEARNING COMPLETE** - Total time: {stopwatch.Elapsed.TotalMinutes:F1} minutes");
            Console.WriteLine($"Final vocabulary size: {_alreadyLearnedWords.Count:N0} words");
            Console.WriteLine($"Learning rate: {(double)_alreadyLearnedWords.Count / stopwatch.Elapsed.TotalMinutes:F0} words/minute");
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

            // Load word database with progress reporting
            Console.Write("Loading word database...");
            var wordDbJson = await File.ReadAllTextAsync(wordDbPath);
            _wordDatabase = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, TatoebaDataConverter.WordData>>(wordDbJson)
                ?? new Dictionary<string, TatoebaDataConverter.WordData>();
            Console.WriteLine($" {_wordDatabase.Count:N0} words loaded");

            // Load co-occurrence matrix
            Console.Write("Loading co-occurrence matrix...");
            var cooccurrenceJson = await File.ReadAllTextAsync(cooccurrencePath);
            _cooccurrenceMatrix = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(cooccurrenceJson)
                ?? new Dictionary<string, Dictionary<string, int>>();
            Console.WriteLine($" {_cooccurrenceMatrix.Count:N0} entries loaded");

            // CRITICAL FIX: Learn from actual sentence data before processing words
            await LearnFromSentenceDataAsync();

            // Load existing vocabulary from storage manager
            await LoadExistingVocabularyAsync();

            await LoadLearnedWordsAsync();
        }

        private async Task LoadExistingVocabularyAsync()
        {
            try
            {
                Console.Write("Loading existing vocabulary from storage...");
                var existingVocabulary = await _storageManager.LoadVocabularyAsync();
                
                // Merge with word database, preferring existing data
                foreach (var kvp in existingVocabulary)
                {
                    if (!_wordDatabase.ContainsKey(kvp.Key))
                    {
                        // Convert WordInfo back to WordData format
                        var wordData = new TatoebaDataConverter.WordData
                        {
                            Word = kvp.Key,
                            Frequency = kvp.Value.Frequency,
                            SentenceContexts = new List<string>(),
                            CooccurringWords = new Dictionary<string, int>()
                        };
                        _wordDatabase[kvp.Key] = wordData;
                    }
                }
                
                Console.WriteLine($" {existingVocabulary.Count:N0} existing words loaded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to load existing vocabulary: {ex.Message}");
                // Continue without existing vocabulary - not a fatal error
            }
        }

        private LearningPlan CalculateLearningPlan(int targetVocabularySize, int batchSize)
        {
            var stopWords = new HashSet<string> {
                "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by",
                "is", "are", "was", "were", "be", "been", "being", "have", "has", "had", "do", "does",
                "did", "will", "would", "could", "should", "may", "might", "must", "can", "shall"
            };

            var allWords = _wordDatabase
                .Where(w => !stopWords.Contains(w.Key))
                .ToList();

            var newWords = allWords.Where(w => !_alreadyLearnedWords.Contains(w.Key)).ToList();
            var learnedWords = allWords.Where(w => _alreadyLearnedWords.Contains(w.Key)).ToList();

            var newWordsToLearn = Math.Min(newWords.Count, targetVocabularySize - _alreadyLearnedWords.Count);
            var reinforcementWords = Math.Min(learnedWords.Count, (int)(newWordsToLearn * 0.2)); // 20% reinforcement

            var totalWordsToProcess = newWordsToLearn + reinforcementWords;
            var totalBatches = (int)Math.Ceiling((double)totalWordsToProcess / batchSize);

            // Estimate time: ~2 seconds per word for processing
            var estimatedTimeMinutes = (totalWordsToProcess * 2.0) / 60.0;

            return new LearningPlan
            {
                NewWordsToLearn = newWordsToLearn,
                ReinforcementWords = reinforcementWords,
                TotalBatches = totalBatches,
                EstimatedTimeMinutes = estimatedTimeMinutes,
                NewWords = newWords,
                LearnedWords = learnedWords
            };
        }

        private async Task ExecuteBatchLearningAsync(LearningPlan plan, int batchSize)
        {
            Console.WriteLine("\n🔄 **EXECUTING BATCH LEARNING**");

            var semaphore = new SemaphoreSlim(_maxConcurrency);
            var tasks = new List<Task>();

            // Prepare word batches
            var allWordsToLearn = new List<string>();

            // Add new words
            var newWordsSample = SelectWeightedRandomWords(plan.NewWords, plan.NewWordsToLearn, new Random(42));
            allWordsToLearn.AddRange(newWordsSample);

            // Add reinforcement words
            if (plan.ReinforcementWords > 0)
            {
                var reinforcementSample = SelectWeightedRandomWords(plan.LearnedWords, plan.ReinforcementWords, new Random(123));
                allWordsToLearn.AddRange(reinforcementSample);
            }

            // Process in batches
            for (int batchIndex = 0; batchIndex < plan.TotalBatches; batchIndex++)
            {
                var batchWords = allWordsToLearn
                    .Skip(batchIndex * batchSize)
                    .Take(batchSize)
                    .ToList();

                if (!batchWords.Any()) break;

                var batchTask = ProcessBatchAsync(batchWords, batchIndex + 1, plan.TotalBatches, semaphore);
                tasks.Add(batchTask);
            }

            // Wait for all batches to complete
            await Task.WhenAll(tasks);

            Console.WriteLine($"\n✅ **BATCH LEARNING COMPLETE**");
            Console.WriteLine($"Processed {allWordsToLearn.Count:N0} words in {plan.TotalBatches} batches");
        }

        private async Task ProcessBatchAsync(List<string> words, int batchNumber, int totalBatches, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();

            try
            {
                Console.WriteLine($"📦 Processing batch {batchNumber}/{totalBatches} ({words.Count} words)...");

                // Learn word patterns for this batch
                await LearnWordPatternsBatchAsync(words);

                // Learn semantic relationships for this batch
                await LearnSemanticRelationshipsBatchAsync(words);

                Console.WriteLine($"✅ Batch {batchNumber}/{totalBatches} completed");
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task LearnWordPatternsBatchAsync(List<string> words)
        {
            // Add biological messiness - randomize learning order
            var random = new Random();
            words = words.OrderBy(x => random.Next()).ToList();
            
            foreach (var word in words)
            {
                if (_wordDatabase.TryGetValue(word, out var wordData))
                {
                    // BIOLOGICAL VARIABILITY: Add noise to frequency to simulate imperfect memory
                    var noisyFrequency = wordData.Frequency + random.Next(-10, 11);
                    noisyFrequency = Math.Max(1, noisyFrequency); // Ensure positive
                    
                    // BIOLOGICAL ENCODING: Use LearningSparseConceptEncoder for neural patterns
                    var sparsePattern = await _encoder.EncodeLearnedWordAsync(word);
                    
                    // Patterns are stored internally by the encoder
                    
                    // Convert WordData to WordInfo with biological variability
                    var wordInfo = new GreyMatter.Storage.WordInfo
                    {
                        Word = word,
                        Frequency = noisyFrequency, // Add biological noise
                        FirstSeen = DateTime.UtcNow.AddMinutes(random.Next(-60, 0)), // Variable "first seen" time
                        EstimatedType = GreyMatter.Storage.WordType.Unknown
                    };

                    // Store word in semantic storage with biological encoding
                    await _storageManager.SaveVocabularyWordAsync(word, wordInfo);

                    // Mark as learned
                    _alreadyLearnedWords.Add(word);
                    
                    // BIOLOGICAL FORGETTING: Random chance to "forget" some words
                    if (random.NextDouble() < 0.05) // 5% chance
                    {
                        _alreadyLearnedWords.Remove(word);
                        Console.WriteLine($"   🧠 Biological forgetting: temporarily forgot '{word}'");
                    }
                }
            }
        }

        private async Task LearnSemanticRelationshipsBatchAsync(List<string> words)
        {
            var conceptsToStore = new Dictionary<string, object>();

            foreach (var word in words)
            {
                if (_cooccurrenceMatrix.TryGetValue(word, out var cooccurrences))
                {
                    // Store co-occurrence relationships as neural concepts
                    conceptsToStore[$"cooccurrences_{word}"] = cooccurrences;
                }
            }

            if (conceptsToStore.Any())
            {
                await _storageManager.StoreNeuralConceptsAsync(conceptsToStore);
            }
        }

        private async Task PerformFinalValidationAsync()
        {
            Console.WriteLine("\n🔍 **FINAL VALIDATION**");

            // Save all learned knowledge
            await SaveLearnedKnowledgeAsync();
            await SaveLearnedWordsAsync();

            // Generate learning report
            Console.WriteLine("\n📊 **LEARNING REPORT**");
            Console.WriteLine($"Total words in database: {_wordDatabase.Count:N0}");
            Console.WriteLine($"Words learned: {_alreadyLearnedWords.Count:N0}");
            Console.WriteLine($"Learning coverage: {(double)_alreadyLearnedWords.Count / _wordDatabase.Count * 100:F1}%");

            // Test a few learned words
            var testWords = _alreadyLearnedWords.Take(5).ToList();
            Console.WriteLine("\n🧪 **SAMPLE LEARNED WORDS**");
            foreach (var word in testWords)
            {
                if (_wordDatabase.TryGetValue(word, out var data))
                {
                    var contextCount = data.SentenceContexts?.Count ?? 0;
                    var relationshipCount = data.CooccurringWords?.Count ?? 0;
                    Console.WriteLine($"- {word}: {contextCount} contexts, {relationshipCount} relationships");
                }
            }
        }

        private async Task LoadLearnedWordsAsync()
        {
            if (File.Exists(_learnedWordsPath))
            {
                var json = await File.ReadAllTextAsync(_learnedWordsPath);
                _alreadyLearnedWords = System.Text.Json.JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>();
            }
        }

        private async Task SaveLearnedKnowledgeAsync()
        {
            Console.WriteLine("💾 Saving learned knowledge to persistent storage...");
            
            try
            {
                // Save vocabulary data through storage manager
                foreach (var word in _alreadyLearnedWords)
                {
                    if (_wordDatabase.TryGetValue(word, out var wordData))
                    {
                        var wordInfo = new GreyMatter.Storage.WordInfo
                        {
                            Word = word,
                            Frequency = wordData.Frequency,
                            FirstSeen = DateTime.UtcNow,
                            EstimatedType = GreyMatter.Storage.WordType.Unknown
                        };
                        
                        await _storageManager.SaveVocabularyWordAsync(word, wordInfo);
                    }
                }
                
                // Save co-occurrence relationships as concepts
                if (_cooccurrenceMatrix.Any())
                {
                    var conceptsToSave = new Dictionary<string, (object Data, GreyMatter.Storage.ConceptType Type)>();
                    
                    foreach (var kvp in _cooccurrenceMatrix)
                    {
                        conceptsToSave[$"cooccurrences_{kvp.Key}"] = (kvp.Value, GreyMatter.Storage.ConceptType.SemanticRelation);
                    }
                    
                    await _storageManager.SaveConceptsBatchAsync(conceptsToSave);
                }
                
                Console.WriteLine($"✅ Saved {_alreadyLearnedWords.Count} learned words and their relationships");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to save learned knowledge: {ex.Message}");
                // Don't throw - saving failure shouldn't stop the learning process
            }
        }

        private async Task SaveLearnedWordsAsync()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_alreadyLearnedWords, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_learnedWordsPath, json);
        }

        private List<string> SelectWeightedRandomWords(
            List<KeyValuePair<string, TatoebaDataConverter.WordData>> words,
            int count,
            Random random)
        {
            if (words.Count <= count)
                return words.Select(w => w.Key).ToList();

            // Weight by frequency for more important words
            var weightedWords = words
                .Select(w => new { Word = w.Key, Weight = Math.Max(1, w.Value.Frequency) })
                .ToList();

            var selected = new List<string>();
            var totalWeight = weightedWords.Sum(w => w.Weight);

            for (int i = 0; i < count && weightedWords.Any(); i++)
            {
                var randomWeight = random.NextDouble() * totalWeight;
                var cumulativeWeight = 0.0;

                for (int j = 0; j < weightedWords.Count; j++)
                {
                    cumulativeWeight += weightedWords[j].Weight;
                    if (randomWeight <= cumulativeWeight)
                    {
                        selected.Add(weightedWords[j].Word);
                        totalWeight -= weightedWords[j].Weight;
                        weightedWords.RemoveAt(j);
                        break;
                    }
                }
            }

            return selected;
        }

        private async Task LearnFromSentenceDataAsync()
        {
            Console.Write("Learning patterns from sentence data...");

            // Collect all sentence contexts from word database
            var allSentences = new List<string>();
            foreach (var wordData in _wordDatabase.Values)
            {
                if (wordData.SentenceContexts != null)
                {
                    allSentences.AddRange(wordData.SentenceContexts);
                }
            }

            // Remove duplicates and limit for performance
            allSentences = allSentences.Distinct().Take(5000).ToList();

            if (allSentences.Any())
            {
                // Learn from actual sentence data
                await _encoder.LearnFromDataAsync(allSentences);
                Console.WriteLine($" Learned from {allSentences.Count} sentences");
            }
            else
            {
                Console.WriteLine(" No sentence data available for learning");
            }
        }

        private class LearningPlan
        {
            public int NewWordsToLearn { get; set; }
            public int ReinforcementWords { get; set; }
            public int TotalBatches { get; set; }
            public double EstimatedTimeMinutes { get; set; }
            public List<KeyValuePair<string, TatoebaDataConverter.WordData>> NewWords { get; set; } = new();
            public List<KeyValuePair<string, TatoebaDataConverter.WordData>> LearnedWords { get; set; } = new();
        }
    }
}
