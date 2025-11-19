using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using greyMatter.Core;
using GreyMatter.Core;
using GreyMatter.Learning;
using GreyMatter.Storage;
using CoreWordInfo = greyMatter.Core.WordInfo;

namespace greyMatter.Learning
{
    /// <summary>
    /// Enhanced Tatoeba trainer that uses LanguageEphemeralBrain for sentence structure learning
    /// Phase 1 of the language learning roadmap: Foundation sentence pattern learning
    /// Migrated to use FastStorageAdapter for 1000x+ performance improvement
    /// Phase 2: Now supports column-based cognitive architecture
    /// </summary>
    public class TatoebaLanguageTrainer
    {
        private readonly TatoebaReader _reader;
        private readonly LanguageEphemeralBrain _brain;
        private readonly IStorageAdapter _storage;
        private readonly string _dataPath;

        private Task? _backgroundSaveTask;
        private bool _isBackgroundSaveRunning;
        private readonly object _backgroundSaveLock = new object();

        // Phase 2: Column-based cognitive architecture
        private ProceduralCorticalColumnGenerator? _columnGenerator;
        private WorkingMemory? _workingMemory;
        private MessageBus? _messageBus;
        private AttentionSystem? _attentionSystem;
        private ColumnBasedProcessor? _columnProcessor;

        public LanguageEphemeralBrain Brain => _brain;

        public TatoebaLanguageTrainer(string tatoebaDataPath, IStorageAdapter? storage = null)
        {
            _reader = new TatoebaReader();
            _dataPath = tatoebaDataPath;
            
            // Use provided storage or create FastStorageAdapter (1000x+ faster than legacy)
            _storage = storage ?? new FastStorageAdapter(
                hotPath: "/Users/billdodd/Desktop/Cerebro/working",
                coldPath: "/Volumes/jarvis/brainData"
            );
            
            // Try to load existing brain state, create new if none exists
            _brain = LoadOrCreateBrainAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Load existing brain state or create a new one
        /// This ensures training sessions are cumulative rather than starting fresh
        /// </summary>
        private async Task<LanguageEphemeralBrain> LoadOrCreateBrainAsync()
        {
            try
            {
                // Try to verify existing storage
                var hasData = await _storage.VerifyIntegrityAsync();
                if (hasData)
                {
                    Console.WriteLine("📂 Loading existing brain state from FastStorage...");
                    var brain = await LoadExistingBrainAsync();
                    var stats = brain.GetLearningStats();
                    Console.WriteLine($"    Loaded brain with {stats.VocabularySize:N0} words, {stats.TotalConcepts:N0} concepts");
                    Console.WriteLine($"   📊 Training sessions: {stats.TrainingSessions}");
                    return brain;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️  Failed to load existing brain: {ex.Message}");
                Console.WriteLine("   🔄 Creating fresh brain instead");
            }
            
            Console.WriteLine("📂 No existing brain state found - creating fresh brain");
            return new LanguageEphemeralBrain();
        }

        /// <summary>
        /// Load existing brain state from FastStorage
        /// </summary>
        private async Task<LanguageEphemeralBrain> LoadExistingBrainAsync()
        {
            var brain = new LanguageEphemeralBrain();
            
            // Load vocabulary (now simplified to HashSet<string>)
            var vocabularySet = await _storage.LoadVocabularyAsync();
            if (vocabularySet.Any())
            {
                Console.WriteLine($"   📚 Loading {vocabularySet.Count:N0} vocabulary words...");
                // Convert to core vocabulary format (Dictionary<string, WordInfo>)
                var coreVocabulary = vocabularySet.ToDictionary(
                    word => word,
                    word => new CoreWordInfo 
                    { 
                        Word = word, 
                        Frequency = 1,
                        FirstSeen = DateTime.UtcNow,
                        EstimatedType = greyMatter.Core.WordType.Unknown
                    }
                );
                brain.ImportVocabulary(coreVocabulary);
            }
            
            // Load brain state (contains language data and neurons)
            var brainState = await _storage.LoadBrainStateAsync();
            if (brainState.Any())
            {
                Console.WriteLine($"   🧠 Loading brain state with {brainState.Count:N0} components...");
                
                // Extract language data if present
                if (brainState.ContainsKey("languageData"))
                {
                    var languageData = brainState["languageData"] as Dictionary<string, object>;
                    if (languageData != null && languageData.Any())
                    {
                        Console.WriteLine($"   📝 Loading {languageData.Count:N0} language concepts...");
                        brain.ImportLanguageData(languageData);
                    }
                }
                
                // Extract neurons if present
                if (brainState.ContainsKey("neurons"))
                {
                    var neurons = brainState["neurons"] as Dictionary<int, object>;
                    if (neurons != null && neurons.Any())
                    {
                        Console.WriteLine($"   🧠 Loading {neurons.Count:N0} neurons...");
                        brain.ImportNeurons(neurons);
                    }
                }
            }
            
            // Load neural concepts
            var concepts = await _storage.LoadNeuralConceptsAsync();
            if (concepts.Any())
            {
                Console.WriteLine($"   💡 Loaded {concepts.Count:N0} neural concepts");
            }
            
            return brain;
        }
        
        /// <summary>
        /// Save current brain state using FastStorage (1000x+ faster)
        /// </summary>
        public async Task SaveBrainStateAsync()
        {
            var totalStartTime = DateTime.UtcNow;
            Console.WriteLine("💾 Saving brain state to FastStorage...");
            
            try
            {
                // Export and save vocabulary (simplified to HashSet<string>)
                var vocabStartTime = DateTime.UtcNow;
                var coreVocabulary = _brain.ExportVocabulary();
                var vocabularySet = new HashSet<string>(coreVocabulary.Keys);
                await _storage.SaveVocabularyAsync(vocabularySet);
                var vocabElapsed = DateTime.UtcNow - vocabStartTime;
                Console.WriteLine($"    Saved {vocabularySet.Count:N0} vocabulary words in {vocabElapsed.TotalSeconds:F1}s");
                
                // Export and save brain state (language data + neurons combined)
                var stateStartTime = DateTime.UtcNow;
                var languageData = _brain.ExportLanguageData();
                var neurons = _brain.ExportNeurons();
                
                var brainState = new Dictionary<string, object>
                {
                    ["languageData"] = languageData,
                    ["neurons"] = neurons,
                    ["trainingSession"] = DateTime.UtcNow,
                    ["schemaVersion"] = _storage.SchemaVersion
                };
                
                await _storage.SaveBrainStateAsync(brainState);
                var stateElapsed = DateTime.UtcNow - stateStartTime;
                Console.WriteLine($"    Saved brain state ({languageData.Count:N0} language concepts, {neurons.Count:N0} neurons) in {stateElapsed.TotalSeconds:F1}s");
                
                // Export and save neural concepts
                var conceptStartTime = DateTime.UtcNow;
                var neuralConcepts = _brain.ExportNeuralConcepts();
                Console.WriteLine($"   🧠 Saving {neuralConcepts.Count:N0} neural concepts...");
                await _storage.SaveNeuralConceptsAsync(neuralConcepts);
                var conceptElapsed = DateTime.UtcNow - conceptStartTime;
                Console.WriteLine($"    Saved {neuralConcepts.Count:N0} neural concepts in {conceptElapsed.TotalSeconds:F1}s");
                
                var totalElapsed = DateTime.UtcNow - totalStartTime;
                Console.WriteLine($"💾 Brain state saved successfully! Total: {totalElapsed.TotalSeconds:F1}s");
                Console.WriteLine($"   � FastStorage performance: {totalElapsed.TotalSeconds:F1}s (vs ~35min with legacy storage)");
            }
            catch (Exception ex)
            {
                var totalElapsed = DateTime.UtcNow - totalStartTime;
                Console.WriteLine($"❌ Failed to save brain state: {ex.Message} (after {totalElapsed.TotalSeconds:F1}s)");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Train on English sentences from Tatoeba dataset
        /// Implements Phase 1: Sentence Pattern Learning
        /// </summary>
        public async Task TrainOnEnglishSentencesAsync(int maxSentences = 10000, int batchSize = 100)
        {
            Console.WriteLine("=== Tatoeba Language Training (Phase 1) ===");
            Console.WriteLine("Learning sentence patterns from real English sentences\n");

            var sentencesPath = Path.Combine(_dataPath, "sentences_eng_small.csv");
            if (!File.Exists(sentencesPath))
            {
                sentencesPath = Path.Combine(_dataPath, "sentences.csv");
                if (!File.Exists(sentencesPath))
                {
                    Console.WriteLine($"❌ Tatoeba data not found at {_dataPath}");
                    Console.WriteLine("Expected: sentences_eng_small.csv or sentences.csv");
                    return;
                }
            }

            Console.WriteLine($"📖 Reading sentences from: {Path.GetFileName(sentencesPath)}");
            
            var sentences = _reader.ReadEnglishSentences(sentencesPath)
                .Where(s => IsGoodLearningSentence(s))
                .Take(maxSentences)
                .ToList();

            Console.WriteLine($" Found {sentences.Count:N0} suitable learning sentences");
            
            if (sentences.Count == 0)
            {
                Console.WriteLine("❌ No suitable sentences found for learning");
                return;
            }

            // Train the language brain
            var startTime = DateTime.Now;
            _brain.LearnSentencesBatch(sentences, batchSize);
            var elapsed = DateTime.Now - startTime;

            // Display learning results
            DisplayLearningResults(elapsed);
            
            // Save brain state to biological storage (background, non-blocking)
            await SaveBrainStateBackgroundAsync();
            
            // Test the learned capabilities
            TestLearnedCapabilities();
        }

        /// <summary>
        /// Train with focused vocabulary building (first 5000 most common words)
        /// </summary>
        public async Task TrainVocabularyFoundationAsync(int targetVocabularySize = 5000)
        {
            Console.WriteLine($"=== Building Vocabulary Foundation (Target: {targetVocabularySize:N0} words) ===\n");

            var sentencesPath = Path.Combine(_dataPath, "sentences_eng_small.csv");
            if (!File.Exists(sentencesPath))
            {
                sentencesPath = Path.Combine(_dataPath, "sentences.csv");
            }

            if (!File.Exists(sentencesPath))
            {
                Console.WriteLine($"❌ Tatoeba data not found at {_dataPath}");
                return;
            }

            var sentences = _reader.ReadEnglishSentences(sentencesPath)
                .Where(s => IsSimpleVocabularySentence(s))
                .ToList();

            Console.WriteLine($"Processing sentences to build vocabulary...");
            
            var processedSentences = 0;
            var startTime = DateTime.Now;

            foreach (var sentence in sentences)
            {
                _brain.LearnSentence(sentence);
                processedSentences++;

                // Check if we've reached target vocabulary size
                if (_brain.VocabularySize >= targetVocabularySize)
                {
                    break;
                }

                // Progress update every 1000 sentences
                if (processedSentences % 1000 == 0)
                {
                    var elapsed = DateTime.Now - startTime;
                    Console.WriteLine($"Processed {processedSentences:N0} sentences, " +
                                    $"vocabulary: {_brain.VocabularySize:N0} words, " +
                                    $"rate: {processedSentences / elapsed.TotalSeconds:F1}/sec");
                }
            }

            var finalElapsed = DateTime.Now - startTime;
            Console.WriteLine($"\n Vocabulary foundation complete!");
            Console.WriteLine($"📊 Final vocabulary size: {_brain.VocabularySize:N0} words");
            Console.WriteLine($"📊 Processed {processedSentences:N0} sentences in {finalElapsed:mm\\:ss}");
            
            // Save brain state to biological storage
            await SaveBrainStateBackgroundAsync();
            
            DisplayTopWords();
        }

        /// <summary>
        /// Test various learned capabilities and display results
        /// </summary>
        public void TestLearnedCapabilities()
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("🧪 Testing Learned Capabilities");
            Console.WriteLine(new string('=', 60));

            // Test 1: Word prediction
            TestWordPrediction();
            
            // Test 2: Word associations
            TestWordAssociations();
            
            // Test 3: Vocabulary knowledge
            TestVocabularyKnowledge();
        }

        private void TestWordPrediction()
        {
            Console.WriteLine("\n🔮 Word Prediction Test:");
            var testSentences = new[]
            {
                "The cat _ on the mat",
                "I like to _ apples", 
                "The big dog _ fast",
                "She _ a book",
                "The sun _ bright"
            };

            foreach (var testSentence in testSentences)
            {
                var predictions = _brain.PredictMissingWord(testSentence, 3);
                Console.WriteLine($"  '{testSentence}' → {string.Join(", ", predictions)}");
            }
        }

        private void TestWordAssociations()
        {
            Console.WriteLine("\n🔗 Word Association Test:");
            var testWords = new[] { "cat", "run", "apple", "big", "happy" };

            foreach (var word in testWords)
            {
                var associations = _brain.GetWordAssociations(word, 5);
                if (associations.Any())
                {
                    Console.WriteLine($"  '{word}' → {string.Join(", ", associations)}");
                }
            }
        }

        private void TestVocabularyKnowledge()
        {
            Console.WriteLine("\n📚 Vocabulary Knowledge Test:");
            var stats = _brain.GetLearningStats();
            
            Console.WriteLine($"  Total vocabulary: {stats.VocabularySize:N0} words");
            Console.WriteLine($"  Sentences learned: {stats.LearnedSentences:N0}");
            Console.WriteLine($"  Neural concepts: {stats.TotalConcepts:N0}");
            Console.WriteLine($"  Word associations: {stats.WordAssociationCount:N0}");
            Console.WriteLine($"  Avg word frequency: {stats.AverageWordFrequency:F1}");
        }

        private void DisplayLearningResults(TimeSpan elapsed)
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("📊 Learning Results");
            Console.WriteLine(new string('=', 60));

            var stats = _brain.GetLearningStats();
            
            Console.WriteLine($"⏱️  Training time: {elapsed:mm\\:ss}");
            Console.WriteLine($"📝 Sentences learned: {stats.LearnedSentences:N0}");
            Console.WriteLine($"📚 Vocabulary size: {stats.VocabularySize:N0} words");
            Console.WriteLine($"🧠 Neural concepts: {stats.TotalConcepts:N0}");
            Console.WriteLine($"🔗 Word associations: {stats.WordAssociationCount:N0}");
            Console.WriteLine($"⚡ Learning rate: {stats.LearnedSentences / elapsed.TotalSeconds:F1} sentences/sec");
        }

        private void DisplayTopWords()
        {
            Console.WriteLine("\n📈 Most Frequent Words:");
            var topWords = _brain.GetTopWords(20);
            
            for (int i = 0; i < Math.Min(20, topWords.Count); i++)
            {
                var (word, frequency) = topWords[i];
                Console.WriteLine($"  {i + 1,2}. {word,-12} ({frequency:N0} times)");
            }
        }

        /// <summary>
        /// Filter for good learning sentences (not too short, not too complex)
        /// </summary>
        private bool IsGoodLearningSentence(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence)) return false;
            
            var wordCount = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            
            // Good learning sentences: 3-15 words, basic punctuation only
            return wordCount >= 3 && 
                   wordCount <= 15 && 
                   !sentence.Contains('"') && 
                   !sentence.Contains('(') && 
                   !sentence.Contains('[') &&
                   !sentence.Contains('{') &&
                   sentence.Length < 100; // Not too long
        }

        /// <summary>
        /// Filter for simple vocabulary building sentences
        /// </summary>
        private bool IsSimpleVocabularySentence(string sentence)
        {
            if (!IsGoodLearningSentence(sentence)) return false;
            
            var wordCount = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            
            // Even simpler sentences for vocabulary building
            return wordCount >= 3 && 
                   wordCount <= 10 && 
                   !sentence.Contains(':') && 
                   !sentence.Contains(';') &&
                   sentence.Length < 80;
        }

        /// <summary>
        /// Save the trained brain to NAS storage
        /// NOTE: Deprecated - ScalePersistence removed in Phase 0 cleanup
        /// Use SaveBrainStateAsync() instead
        /// </summary>
        [Obsolete("Use SaveBrainStateAsync() instead - ScalePersistence removed")]
        public async Task SaveTrainedBrain(string savePath = "")
        {
            Console.WriteLine($"⚠️  SaveTrainedBrain is deprecated. Using SaveBrainStateAsync() instead.");
            await SaveBrainStateAsync();
            
            /* ORIGINAL CODE - Commented out during Phase 0 cleanup
            if (string.IsNullOrEmpty(savePath))
            {
                savePath = "/Volumes/jarvis/brainData";
            }

            Console.WriteLine($"\n💾 Saving trained language brain to: {savePath}");
            
            // Use the ScalePersistence system like in ScaleDemo
            var persistence = new ScalePersistence(savePath);
            await persistence.SaveBrain(_brain);
            
            Console.WriteLine(" Brain saved successfully!");
            */
            
            // Also save vocabulary statistics
            var stats = _brain.GetLearningStats();
            var statsPath = Path.Combine(savePath, "language_stats.txt");
            
            var statsContent = $"Language Learning Statistics\n" +
                             $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n" +
                             $"Vocabulary Size: {stats.VocabularySize:N0} words\n" +
                             $"Sentences Learned: {stats.LearnedSentences:N0}\n" +
                             $"Neural Concepts: {stats.TotalConcepts:N0}\n" +
                             $"Word Associations: {stats.WordAssociationCount:N0}\n" +
                             $"Average Word Frequency: {stats.AverageWordFrequency:F2}\n\n" +
                             $"Top 20 Most Frequent Words:\n";
            
            var topWords = _brain.GetTopWords(20);
            for (int i = 0; i < topWords.Count; i++)
            {
                var (word, frequency) = topWords[i];
                statsContent += $"{i + 1,2}. {word,-15} {frequency:N0}\n";
            }

            await File.WriteAllTextAsync(statsPath, statsContent);
            Console.WriteLine($"📊 Language statistics saved to: language_stats.txt");
        }

        /// <summary>
        /// Train vocabulary from a random sample of sentences
        /// </summary>
        public async Task TrainVocabularyFoundationWithSample(int targetVocabulary, int startPosition, int sampleSize)
        {
            Console.WriteLine($"=== Random Sample Vocabulary Building ===");
            Console.WriteLine($"Target: {targetVocabulary:N0} words from position {startPosition:N0}\n");

            var sentences = await GetRandomSampleSentences(startPosition, Math.Min(sampleSize, targetVocabulary * 4));
            
            Console.WriteLine($"Processing {sentences.Count:N0} sentences for vocabulary building...");
            
            var processedSentences = 0;
            var startTime = DateTime.Now;

            foreach (var sentence in sentences)
            {
                _brain.LearnSentence(sentence);
                processedSentences++;

                // Check if we've reached target vocabulary size
                if (_brain.VocabularySize >= targetVocabulary)
                {
                    break;
                }

                // Progress update every 500 sentences
                if (processedSentences % 500 == 0)
                {
                    var elapsed = DateTime.Now - startTime;
                    Console.WriteLine($"   Processed {processedSentences:N0} sentences, " +
                                    $"vocabulary: {_brain.VocabularySize:N0} words, " +
                                    $"rate: {processedSentences / elapsed.TotalSeconds:F1}/sec");
                }
            }

            var finalElapsed = DateTime.Now - startTime;
            Console.WriteLine($"\n Sample vocabulary building complete!");
            Console.WriteLine($"📊 Final vocabulary size: {_brain.VocabularySize:N0} words");
            Console.WriteLine($"📊 Processed {processedSentences:N0} sentences in {finalElapsed:mm\\:ss}");
        }

        /// <summary>
        /// Train with random sample using block-based processing for storage monitoring
        /// </summary>
        public async Task TrainWithRandomSample(int startPosition, int sampleSize, int blockSize)
        {
            Console.WriteLine($"=== Random Sample Block Training ===");
            Console.WriteLine($"Sample: {sampleSize:N0} sentences from position {startPosition:N0}");
            Console.WriteLine($"Block size: {blockSize:N0} sentences per block\n");

            var sentences = await GetRandomSampleSentences(startPosition, sampleSize);
            var totalSentences = sentences.Count;
            var processed = 0;
            var blockNumber = 1;
            var startTime = DateTime.Now;

            Console.WriteLine($"Starting block-based training on {totalSentences:N0} sentences...");

                            foreach (var batch in System.Linq.Enumerable.Chunk(sentences, blockSize))
            {
                var blockStart = DateTime.Now;
                var batchSize = batch.Count();
                
                Console.WriteLine($"\n📦 Block {blockNumber}: Processing {batchSize:N0} sentences...");
                
                // Get initial stats for this block
                var initialStats = _brain.GetLearningStats();
                
                foreach (var sentence in batch)
                {
                    _brain.LearnSentence(sentence);
                    processed++;
                }

                var blockElapsed = DateTime.Now - blockStart;
                var totalElapsed = DateTime.Now - startTime;
                var finalStats = _brain.GetLearningStats();
                
                // Block progress report
                var rate = batchSize / blockElapsed.TotalSeconds;
                var eta = TimeSpan.FromSeconds((totalSentences - processed) / Math.Max(rate, 1));
                var conceptGrowth = finalStats.TotalConcepts - initialStats.TotalConcepts;
                var vocabGrowth = finalStats.VocabularySize - initialStats.VocabularySize;
                
                Console.WriteLine($"    Block {blockNumber} complete in {blockElapsed:mm\\:ss}");
                Console.WriteLine($"   📈 Progress: {processed:N0}/{totalSentences:N0} ({processed * 100.0 / totalSentences:F1}%)");
                Console.WriteLine($"   ⚡ Rate: {rate:F1} sentences/sec - ETA: {eta:mm\\:ss}");
                Console.WriteLine($"   🧠 Concept growth: +{conceptGrowth:N0} (total: {finalStats.TotalConcepts:N0})");
                Console.WriteLine($"   📚 Vocab growth: +{vocabGrowth:N0} (total: {finalStats.VocabularySize:N0})");
                
                blockNumber++;
                
                // Storage monitoring after each block
                if (blockNumber % 5 == 0) // Every 5 blocks
                {
                    MonitorStorageGrowth(blockNumber, finalStats);
                }
            }

            var finalElapsed = DateTime.Now - startTime;
            Console.WriteLine($"\n Random sample training complete!");
            Console.WriteLine($"📊 Processed {totalSentences:N0} sentences in {finalElapsed:mm\\:ss}");
            Console.WriteLine($"📊 Final vocabulary: {_brain.VocabularySize:N0} words");
            Console.WriteLine($"📊 Total concepts: {_brain.GetLearningStats().TotalConcepts:N0}");
        }

        /// <summary>
        /// Get a random sample of sentences from the dataset
        /// </summary>
        private async Task<List<string>> GetRandomSampleSentences(int startPosition, int sampleSize)
        {
            var sentences = new List<string>();
            var sentencesPath = Path.Combine(_dataPath, "sentences_eng_small.csv");
            if (!File.Exists(sentencesPath))
            {
                sentencesPath = Path.Combine(_dataPath, "sentences.csv");
            }

            if (!File.Exists(sentencesPath))
            {
                throw new FileNotFoundException($"Tatoeba dataset not found at {_dataPath}");
            }

            Console.WriteLine($"📖 Reading sample from: {Path.GetFileName(sentencesPath)}");
            Console.WriteLine($"   Start position: {startPosition:N0}");
            Console.WriteLine($"   Sample size: {sampleSize:N0}");

            using var fs = new FileStream(sentencesPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);

            var currentPosition = 0;
            var collected = 0;
            string? line;

            // Skip to start position
            while ((line = await sr.ReadLineAsync()) != null && currentPosition < startPosition)
            {
                var parts = line.Split('\t');
                if (parts.Length >= 2 && parts[1].Equals("eng", StringComparison.OrdinalIgnoreCase))
                {
                    currentPosition++;
                }
            }

            Console.WriteLine($"   📍 Positioned at sentence {currentPosition:N0}");

            // Collect sample
            while ((line = await sr.ReadLineAsync()) != null && collected < sampleSize)
            {
                var parts = line.Split('\t');
                if (parts.Length >= 3 && parts[1].Equals("eng", StringComparison.OrdinalIgnoreCase))
                {
                    var text = parts[2].Trim();
                    if (IsGoodLearningSentence(text))
                    {
                        sentences.Add(text);
                        collected++;
                    }
                    currentPosition++;
                }

                // Progress indicator for large samples
                if (collected % 10000 == 0 && collected > 0)
                {
                    Console.WriteLine($"   📥 Collected: {collected:N0}/{sampleSize:N0} sentences...");
                }
            }

            Console.WriteLine($"    Collected {sentences.Count:N0} suitable sentences");
            return sentences;
        }

        /// <summary>
        /// Monitor storage growth patterns during training
        /// </summary>
        private void MonitorStorageGrowth(int blockNumber, LanguageLearningStats stats)
        {
            Console.WriteLine($"\n💾 Storage Monitoring (Block {blockNumber}):");
            
            // Estimate current memory usage
            var estimatedBytesPerConcept = 150;
            var estimatedStorageSize = stats.TotalConcepts * estimatedBytesPerConcept;
            
            Console.WriteLine($"   📊 Neural concepts: {stats.TotalConcepts:N0}");
            Console.WriteLine($"   📊 Vocabulary size: {stats.VocabularySize:N0}");
            Console.WriteLine($"   📊 Associations: {stats.WordAssociationCount:N0}");
            Console.WriteLine($"   💾 Estimated storage: {FormatBytes(estimatedStorageSize)}");
            
            // Check for potential issues
            if (stats.TotalConcepts > 50000)
            {
                Console.WriteLine($"   ⚠️  High concept count - consider partitioning");
            }
            
            if (estimatedStorageSize > 50_000_000) // 50MB
            {
                Console.WriteLine($"   ⚠️  Large storage size - monitor disk space");
            }
            
            var conceptsPerSentence = stats.TotalConcepts / (double)Math.Max(stats.LearnedSentences, 1);
            if (conceptsPerSentence > 20)
            {
                Console.WriteLine($"   ⚠️  High concept-to-sentence ratio: {conceptsPerSentence:F1}");
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double size = bytes;
            int order = 0;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Save brain state in the background (non-blocking)
        /// This allows training to continue while saving happens asynchronously
        /// </summary>
        public async Task SaveBrainStateBackgroundAsync()
        {
            // Use the existing background save infrastructure
            lock (_backgroundSaveLock)
            {
                if (_isBackgroundSaveRunning)
                {
                    Console.WriteLine("   ⏳ Background save already in progress, skipping...");
                    return;
                }
                _isBackgroundSaveRunning = true;
            }

            // Start background save task
            _backgroundSaveTask = Task.Run(async () =>
            {
                try
                {
                    await SaveBrainStateAsync();
                }
                finally
                {
                    lock (_backgroundSaveLock)
                    {
                        _isBackgroundSaveRunning = false;
                    }
                }
            });

            Console.WriteLine("   🚀 Background save initiated (non-blocking)");
            await Task.CompletedTask;
        }

        // ============================================================================
        // PHASE 2: COLUMN-BASED COGNITIVE ARCHITECTURE INTEGRATION
        // ============================================================================

        /// <summary>
        /// Initialize the column-based cognitive architecture (Phase 2)
        /// Creates working memory, message bus, attention system, and column processor
        /// </summary>
        public void InitializeColumnArchitecture()
        {
            Console.WriteLine("\n🧠 === Initializing Column-Based Cognitive Architecture (Phase 2) ===");
            
            // Create cognitive systems with correct constructor calls
            _workingMemory = new WorkingMemory(1000, 0.1); // maxCapacity, defaultDecayRate
            _messageBus = new MessageBus(100, 1000); // maxInboxSize, maxHistorySize
            _attentionSystem = new AttentionSystem(0.1, 0.05); // baselineActivation, decayRate
            
            // AttentionSystem has default profiles pre-loaded (comprehension, production, listening, reading, recall, learning)
            // Set initial profile to "learning"
            _attentionSystem.SetProfile("learning");
            
            // Create column generator (no seed parameter - uses fixed seed internally)
            _columnGenerator = new ProceduralCorticalColumnGenerator(2048, 0.02); // basePatternSize, baseSparsity
            _columnProcessor = new ColumnBasedProcessor(
                _columnGenerator,
                _workingMemory,
                _messageBus,
                _attentionSystem
            );
            
            Console.WriteLine("    Working Memory initialized (capacity: 1000, decay: 0.1)");
            Console.WriteLine("    Message Bus initialized (max inbox: 100, history: 1000)");
            Console.WriteLine("    Attention System initialized with 6 default profiles");
            Console.WriteLine("    Active profile: learning");
            Console.WriteLine("    Column Generator initialized (pattern size: 2048, sparsity: 0.02)");
            Console.WriteLine("    Column Processor ready for training");
            Console.WriteLine("🧠 === Column Architecture Ready! ===\n");
        }

        /// <summary>
        /// Train using column-based cognitive architecture (Phase 2)
        /// This is the NEW approach that uses procedural columns, working memory, and attention
        /// </summary>
        public async Task TrainWithColumnArchitectureAsync(int maxSentences = 50, int batchSize = 10)
        {
            Console.WriteLine("\n🚀 === Column-Based Training (Phase 2 - EXPERIMENTAL) ===");
            Console.WriteLine($"Training with {maxSentences} sentences, batch size {batchSize}\n");

            // Initialize column architecture if not already done
            if (_columnProcessor == null)
            {
                InitializeColumnArchitecture();
            }

            // Read sentences
            var sentencesPath = Path.Combine(_dataPath, "sentences_eng_small.csv");
            if (!File.Exists(sentencesPath))
            {
                sentencesPath = Path.Combine(_dataPath, "sentences.csv");
                if (!File.Exists(sentencesPath))
                {
                    Console.WriteLine($"❌ Tatoeba data not found at {_dataPath}");
                    return;
                }
            }

            Console.WriteLine($"📖 Reading sentences from: {Path.GetFileName(sentencesPath)}");
            
            var sentences = _reader.ReadEnglishSentences(sentencesPath)
                .Where(s => IsGoodLearningSentence(s))
                .Take(maxSentences)
                .ToList();

            Console.WriteLine($" Found {sentences.Count:N0} suitable learning sentences\n");
            
            if (sentences.Count == 0)
            {
                Console.WriteLine("❌ No suitable sentences found for learning");
                return;
            }

            // Train through column pipeline
            var startTime = DateTime.Now;
            var totalPatternsLearned = 0;
            var totalMessagesSent = 0;
            var batchCount = 0;

            for (int i = 0; i < sentences.Count; i += batchSize)
            {
                var batch = sentences.Skip(i).Take(batchSize).ToList();
                batchCount++;
                
                Console.WriteLine($"📦 Batch {batchCount}/{(int)Math.Ceiling((double)sentences.Count / batchSize)}:");
                
                foreach (var sentence in batch)
                {
                    // Convert sentence to LanguageInput
                    var input = LanguageInput.FromSentence(sentence, language: "en", domain: "general");
                    
                    // Process through column pipeline
                    var result = await _columnProcessor!.ProcessInputAsync(input);
                    
                    totalPatternsLearned += result.TotalPatternsLearned;
                    totalMessagesSent += result.MessagesSent;
                    
                    Console.WriteLine($"   📝 \"{sentence.Substring(0, Math.Min(50, sentence.Length))}...\"");
                    Console.WriteLine($"      → {result.ProcessingSteps.Count} steps, {result.TotalPatternsLearned} patterns, {result.MessagesSent} messages");
                }
                
                // Apply decay after each batch
                _columnProcessor.ApplyDecay();
                
                // Show batch statistics
                var stats = _columnProcessor.GetStats();
                Console.WriteLine($"   📊 Batch stats:");
                Console.WriteLine($"      • Active columns: {stats.ActiveColumns}");
                Console.WriteLine($"      • Working memory: {stats.WorkingMemoryStats?.TotalPatterns ?? 0} patterns");
                Console.WriteLine($"      • Message bus: {stats.MessageBusStats?.TotalMessagesSent ?? 0} total messages");
                Console.WriteLine($"      • Attention: Top focus = {GetTopAttentionFocus(stats)}");
                Console.WriteLine();
            }

            var elapsed = DateTime.Now - startTime;

            // Display final results
            Console.WriteLine("🎯 === Column-Based Training Complete ===");
            Console.WriteLine($"⏱️  Time: {elapsed.TotalSeconds:F2}s ({(sentences.Count / elapsed.TotalSeconds):F1} sentences/sec)");
            Console.WriteLine($"📊 Total patterns learned: {totalPatternsLearned:N0}");
            Console.WriteLine($"💬 Total messages sent: {totalMessagesSent:N0}");
            Console.WriteLine($"🧠 Working memory size: {_workingMemory?.Count ?? 0:N0}");
            Console.WriteLine($"📮 Message bus stats: {(_messageBus != null ? _messageBus.GetStats().TotalMessagesSent : 0):N0} messages");
            
            var finalStats = _columnProcessor!.GetStats();
            Console.WriteLine($"🎯 Final attention distribution:");
            if (finalStats.AttentionStats != null)
            {
                Console.WriteLine($"   • Active profile: {finalStats.AttentionStats.ActiveProfile}");
                Console.WriteLine($"   • Attended columns: {finalStats.AttentionStats.AttendedColumns}");
                Console.WriteLine($"   • Average activation: {finalStats.AttentionStats.AverageActivation:F3}");
                Console.WriteLine($"   • Highest activation: {finalStats.AttentionStats.HighestActivation:F3}");
            }
            
            Console.WriteLine("\n Phase 2 column-based training complete!");
            Console.WriteLine("   Compare this to the traditional LanguageEphemeralBrain approach");
            Console.WriteLine("   to see if emergence happens from column interactions.\n");
        }

        /// <summary>
        /// Get the top attention focus from processor stats
        /// </summary>
        private string GetTopAttentionFocus(ProcessorStats stats)
        {
            if (stats.AttentionStats == null)
            {
                return "None";
            }

            return $"{stats.AttentionStats.ActiveProfile} (attended: {stats.AttentionStats.AttendedColumns})";
        }
    }
}