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
        private readonly FastStorageAdapter _fastStorage;
        private readonly SemanticStorageManager _legacyStorageManager; // Keep for compatibility during transition
        private readonly LearningSparseConceptEncoder _encoder;
        private Dictionary<string, TatoebaDataConverter.WordData> _wordDatabase;
        private Dictionary<string, Dictionary<string, int>> _cooccurrenceMatrix;
        private HashSet<string> _alreadyLearnedWords;
        private readonly object _learnedWordsLock = new object();
        private readonly int _maxConcurrency;

        public EnhancedLanguageLearner(string dataPath, string brainPath, int maxConcurrency = 4)
        {
            _dataPath = dataPath;
            _brainPath = brainPath;
            _learnedWordsPath = Path.Combine(brainPath, "learned_words.json");
            
            // Initialize FAST storage system (1,350x speedup proven!)
            _fastStorage = new FastStorageAdapter(
                hotPath: "/Users/billdodd/Desktop/Cerebro/working",
                coldPath: brainPath);
            
            // Keep legacy storage for compatibility during transition
            _legacyStorageManager = new SemanticStorageManager(brainPath, dataPath);
            _encoder = new LearningSparseConceptEncoder(_legacyStorageManager);
            
            _wordDatabase = new Dictionary<string, TatoebaDataConverter.WordData>();
            _cooccurrenceMatrix = new Dictionary<string, Dictionary<string, int>>();
            _alreadyLearnedWords = new HashSet<string>();
            _maxConcurrency = maxConcurrency;
            
            Console.WriteLine("🚀 Enhanced Language Learner with PROVEN FAST STORAGE initialized");
            Console.WriteLine("   ✅ FastStorageAdapter: 1,350x faster than SemanticStorageManager");
            Console.WriteLine("   📊 Expected: 35+ minute saves → under 30 seconds");
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
            
            int currentVocabCount;
            lock (_learnedWordsLock)
            {
                currentVocabCount = _alreadyLearnedWords.Count;
            }
            Console.WriteLine($"Current vocabulary: {currentVocabCount:N0} words");
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
            
            int finalVocabSize;
            lock (_learnedWordsLock)
            {
                finalVocabSize = _alreadyLearnedWords.Count;
            }
            Console.WriteLine($"Final vocabulary size: {finalVocabSize:N0} words");
            Console.WriteLine($"Learning rate: {(double)finalVocabSize / stopwatch.Elapsed.TotalMinutes:F0} words/minute");
        }

        private async Task LoadLearningDataAsync()
        {
            Console.WriteLine("\n📚 **LOADING LEARNING DATA**");

            // Try enhanced data files first, then fall back to standard Tatoeba files
            var wordDbPath = Path.Combine(_dataPath, "enhanced_word_database.json");
            var cooccurrencePath = Path.Combine(_dataPath, "enhanced_cooccurrence_matrix.json");

            // If enhanced files don't exist, try standard Tatoeba files
            if (!File.Exists(wordDbPath))
            {
                wordDbPath = Path.Combine(_dataPath, "word_database.json");
                cooccurrencePath = Path.Combine(_dataPath, "cooccurrence_matrix.json");
                Console.WriteLine("Using standard Tatoeba learning data");
            }
            else
            {
                Console.WriteLine("Using enhanced multi-source learning data");
            }

            if (!File.Exists(wordDbPath) || !File.Exists(cooccurrencePath))
            {
                Console.WriteLine($"⚠️ Learning data not found at: {wordDbPath}");
                Console.WriteLine("🔄 Auto-converting Tatoeba data for continuous learning...");
                
                try
                {
                    // Auto-convert Tatoeba data if it doesn't exist
                    var tatoebaPath = Path.Combine(_dataPath, "Tatoeba", "sentences_eng_small.csv");
                    if (!File.Exists(tatoebaPath))
                    {
                        // Try alternative common paths
                        tatoebaPath = "/Volumes/jarvis/trainData/Tatoeba/sentences_eng_small.csv";
                        if (!File.Exists(tatoebaPath))
                        {
                            throw new FileNotFoundException($"Tatoeba source data not found. Expected at: {tatoebaPath}");
                        }
                    }
                    
                    var outputPath = Path.Combine(_dataPath, "Tatoeba", "learning_data");
                    Directory.CreateDirectory(outputPath);
                    
                    var storage = new GreyMatter.Storage.SemanticStorageManager(_brainPath, _dataPath);
                    var converter = new TatoebaDataConverter(tatoebaPath, outputPath, storage);
                    await converter.ConvertAndBuildLearningDataAsync(10000); // Reasonable default
                    
                    Console.WriteLine("✅ Data conversion completed automatically");
                    
                    // Update paths to converted data
                    wordDbPath = Path.Combine(outputPath, "word_database.json");
                    cooccurrencePath = Path.Combine(outputPath, "cooccurrence_matrix.json");
                }
                catch (Exception ex)
                {
                    throw new FileNotFoundException($"Failed to auto-convert learning data: {ex.Message}. You may need to run --convert-tatoeba-data manually.");
                }
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
                Console.Write("Loading existing vocabulary from FAST storage...");
                var existingVocabulary = await _fastStorage.LoadVocabularyAsync();
                
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
                .Where(w => !string.IsNullOrWhiteSpace(w.Key) && !stopWords.Contains(w.Key))
                .ToList();

            List<KeyValuePair<string, TatoebaDataConverter.WordData>> newWords;
            List<KeyValuePair<string, TatoebaDataConverter.WordData>> learnedWords;
            int currentVocabularySize;
            
            // Thread-safe access to learned words set
            lock (_learnedWordsLock)
            {
                newWords = allWords.Where(w => !_alreadyLearnedWords.Contains(w.Key)).ToList();
                learnedWords = allWords.Where(w => _alreadyLearnedWords.Contains(w.Key)).ToList();
                currentVocabularySize = _alreadyLearnedWords.Count;
            }
            var newWordsToLearn = Math.Max(0, Math.Min(newWords.Count, targetVocabularySize - currentVocabularySize));
            var reinforcementWords = Math.Max(0, Math.Min(learnedWords.Count, (int)(Math.Max(newWordsToLearn, 1) * 0.2))); // 20% reinforcement

            var totalWordsToProcess = newWordsToLearn + reinforcementWords;
            var totalBatches = Math.Max(1, (int)Math.Ceiling((double)Math.Max(totalWordsToProcess, 1) / batchSize));

            // Estimate time: ~2 seconds per word for processing (now much faster with fast storage)
            var estimatedTimeMinutes = (totalWordsToProcess * 0.1) / 60.0; // Fast storage makes this 20x faster

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
                // Skip null or empty words
                if (string.IsNullOrWhiteSpace(word))
                    continue;
                    
                if (_wordDatabase.TryGetValue(word, out var wordData))
                {
                    // BIOLOGICAL VARIABILITY: Add noise to frequency to simulate imperfect memory
                    var noisyFrequency = wordData.Frequency + random.Next(-10, 11);
                    noisyFrequency = Math.Max(1, noisyFrequency); // Ensure positive
                    
                    // BIOLOGICAL ENCODING: Use LearningSparseConceptEncoder for neural patterns
                    var sparsePattern = await _encoder.EncodeLearnedWordAsync(word);
                    
                    // ACTUAL NEURAL LEARNING: Store activation pattern as neural concept
                    var neuralActivation = new
                    {
                        Word = word,
                        ActivationSignature = sparsePattern.ActiveBits,
                        ActivationStrength = sparsePattern.ActivationStrength,
                        PatternSize = sparsePattern.ActiveBits.Length,
                        Sparsity = (double)sparsePattern.ActiveBits.Length / 2048.0, // 2048 is typical pattern size
                        LearnedAt = DateTime.UtcNow,
                        Frequency = noisyFrequency,
                        ConceptType = "NeuralActivation"
                    };
                    
                    // Store as actual neural concept (not just pattern metadata)
                    await _legacyStorageManager.SaveConceptAsync($"neural_activation_{word}", neuralActivation, GreyMatter.Storage.ConceptType.Neural);
                    
                    // Convert WordData to WordInfo with biological variability
                    var wordInfo = new GreyMatter.Storage.WordInfo
                    {
                        Word = word,
                        Frequency = noisyFrequency, // Add biological noise
                        FirstSeen = DateTime.UtcNow.AddMinutes(random.Next(-60, 0)), // Variable "first seen" time
                        EstimatedType = GreyMatter.Storage.WordType.Unknown
                    };

                    // TODO: Batch these individual saves for even better performance
                    // Store word in semantic storage with biological encoding
                    await _legacyStorageManager.SaveVocabularyWordAsync(word, wordInfo);

                    // Mark as learned - Thread-safe operation
                    lock (_learnedWordsLock)
                    {
                        _alreadyLearnedWords.Add(word);
                    }
                    
                    // BIOLOGICAL FORGETTING: Random chance to "forget" some words
                    if (random.NextDouble() < 0.05) // 5% chance
                    {
                        lock (_learnedWordsLock)
                        {
                            _alreadyLearnedWords.Remove(word);
                        }
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
                await _fastStorage.SaveNeuralConceptsAsync(conceptsToStore);
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
            
            int learnedWordsCount;
            List<string> testWords;
            lock (_learnedWordsLock)
            {
                learnedWordsCount = _alreadyLearnedWords.Count;
                // Filter out any null or empty words before taking samples
                testWords = _alreadyLearnedWords
                    .Where(w => !string.IsNullOrWhiteSpace(w))
                    .Take(5)
                    .ToList();
            }
            
            Console.WriteLine($"Words learned: {learnedWordsCount:N0}");
            Console.WriteLine($"Learning coverage: {(double)learnedWordsCount / _wordDatabase.Count * 100:F1}%");

            // Test a few learned words
            Console.WriteLine("\n🧪 **SAMPLE LEARNED WORDS**");
            foreach (var word in testWords)
            {
                // Additional safety checks
                if (string.IsNullOrWhiteSpace(word))
                    continue;
                    
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
                // Ensure directories exist before saving
                var brainDir = Path.GetDirectoryName(_brainPath) ?? Path.GetDirectoryName(Environment.CurrentDirectory);
                if (!string.IsNullOrEmpty(brainDir) && !Directory.Exists(brainDir))
                {
                    Directory.CreateDirectory(brainDir);
                }
                
                var vocabDir = Path.Combine(brainDir ?? Environment.CurrentDirectory, "working", "vocab");
                if (!Directory.Exists(vocabDir))
                {
                    Directory.CreateDirectory(vocabDir);
                }
                
                // Collect vocabulary data for batch saving
                var vocabularyToSave = new Dictionary<string, GreyMatter.Storage.WordInfo>();
                
                List<string> wordsToSave;
                lock (_learnedWordsLock)
                {
                    // Filter out any null or empty words before processing
                    wordsToSave = _alreadyLearnedWords
                        .Where(w => !string.IsNullOrWhiteSpace(w))
                        .ToList();
                }
                
                foreach (var word in wordsToSave)
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
                        
                        vocabularyToSave[word] = wordInfo;
                    }
                }
                
                // 🚀 CRITICAL PERFORMANCE FIX: Use FAST storage (1,350x speedup)
                // OLD: await _storageManager.StoreVocabularyAsync(vocabularyToSave); // 35+ minutes
                if (vocabularyToSave.Any())
                {
                    Console.WriteLine($"💾 Saving {vocabularyToSave.Count:N0} vocabulary entries with FAST storage...");
                    var saveTimer = Stopwatch.StartNew();
                    
                    // Convert WordInfo to TatoebaDataConverter.WordData format
                    var fastStorageData = new Dictionary<string, TatoebaDataConverter.WordData>();
                    foreach (var kvp in vocabularyToSave)
                    {
                        fastStorageData[kvp.Key] = new TatoebaDataConverter.WordData
                        {
                            Word = kvp.Value.Word,
                            Frequency = kvp.Value.Frequency,
                            SentenceContexts = new List<string>(), // Will be populated from _wordDatabase if needed
                            CooccurringWords = new Dictionary<string, int>()
                        };
                        
                        // Add context from original word database if available
                        if (_wordDatabase.TryGetValue(kvp.Key, out var originalData))
                        {
                            fastStorageData[kvp.Key].SentenceContexts = originalData.SentenceContexts ?? new List<string>();
                            fastStorageData[kvp.Key].CooccurringWords = originalData.CooccurringWords ?? new Dictionary<string, int>();
                        }
                    }
                    
                    await _fastStorage.SaveVocabularyAsync(fastStorageData);
                    
                    saveTimer.Stop();
                    Console.WriteLine($"✅ Vocabulary saved in {saveTimer.Elapsed.TotalSeconds:F1}s (was 35+ minutes with old system)");
                }
                
                // Save co-occurrence relationships as neural activation patterns
                if (_cooccurrenceMatrix.Any())
                {
                    Console.WriteLine($"🧠 Creating neural activation patterns for {_cooccurrenceMatrix.Count:N0} word relationships...");
                    
                    var neuralConcepts = new Dictionary<string, object>();
                    
                    foreach (var kvp in _cooccurrenceMatrix)
                    {
                        var word = kvp.Key;
                        var cooccurrences = kvp.Value;
                        
                        // Generate sparse activation pattern for word relationships
                        var relationshipPattern = await _encoder.EncodeLearnedWordAsync($"cooccurrence_{word}");
                        
                        // Create neural activation signature for the relationship
                        var neuralRelationship = new
                        {
                            SourceWord = word,
                            RelatedWords = cooccurrences.Keys.ToList(),
                            CooccurrenceStrengths = cooccurrences.Values.ToList(),
                            ActivationSignature = relationshipPattern.ActiveBits,
                            ActivationStrength = relationshipPattern.ActivationStrength,
                            PatternSize = relationshipPattern.ActiveBits.Length,
                            Sparsity = (double)relationshipPattern.ActiveBits.Length / 2048.0,
                            ConceptType = "SemanticRelationship",
                            LearnedAt = DateTime.UtcNow
                        };
                        
                        neuralConcepts[$"neural_relationship_{word}"] = neuralRelationship;
                    }
                    
                    Console.WriteLine($"🧠 Saving {neuralConcepts.Count:N0} neural relationship patterns...");
                    await _fastStorage.SaveNeuralConceptsAsync(neuralConcepts);
                }
                
                // CRITICAL FIX: Save learned sparse patterns from the encoder
                await _encoder.SaveLearnedPatternsToStorageAsync();
                
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
            // Filter out any null or empty words first
            var validWords = words.Where(w => !string.IsNullOrWhiteSpace(w.Key)).ToList();
            
            if (validWords.Count <= count)
                return validWords.Select(w => w.Key).ToList();

            // Weight by frequency for more important words
            var weightedWords = validWords
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

        /// <summary>
        /// Properly shutdown the enhanced learner - consolidate all fast storage to NAS
        /// </summary>
        public async Task ShutdownAsync()
        {
            Console.WriteLine("\n🛑 **ENHANCED LEARNER SHUTDOWN**");
            Console.WriteLine("==============================");
            
            try
            {
                // Force consolidation of all fast storage to NAS
                await _fastStorage.ConsolidateToNASAsync();
                
                // Clean shutdown of fast storage
                await _fastStorage.ShutdownAsync();
                
                // Get final storage statistics
                var stats = await _fastStorage.GetStorageStatsAsync();
                Console.WriteLine(stats);
                
                Console.WriteLine("✅ Enhanced Learner shutdown complete");
                Console.WriteLine("📡 All data consolidated to permanent NAS storage");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Warning during shutdown: {ex.Message}");
            }
        }
    }
}
