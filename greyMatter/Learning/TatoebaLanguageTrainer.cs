using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using greyMatter.Core;
using GreyMatter.Learning;
using GreyMatter.Storage;
using CoreWordInfo = greyMatter.Core.WordInfo;
using StorageWordInfo = GreyMatter.Storage.WordInfo;

namespace greyMatter.Learning
{
    /// <summary>
    /// Enhanced Tatoeba trainer that uses LanguageEphemeralBrain for sentence structure learning
    /// Phase 1 of the language learning roadmap: Foundation sentence pattern learning
    /// </summary>
    public class TatoebaLanguageTrainer
    {
        private readonly TatoebaReader _reader;
        private readonly LanguageEphemeralBrain _brain;
        private readonly SemanticStorageManager _storageManager;
        private readonly string _dataPath;

        private Task? _backgroundSaveTask;
        private bool _isBackgroundSaveRunning;
        private readonly object _backgroundSaveLock = new object();

        public LanguageEphemeralBrain Brain => _brain;

        public TatoebaLanguageTrainer(string tatoebaDataPath)
        {
            _reader = new TatoebaReader();
            _dataPath = tatoebaDataPath;
            
            // Initialize biological storage manager
            var brainDataPath = "/Volumes/jarvis/brainData";
            var trainingDataRoot = "/Volumes/jarvis/trainData";
            _storageManager = new SemanticStorageManager(brainDataPath, trainingDataRoot);
            
            // Try to load existing brain state, create new if none exists
            _brain = LoadOrCreateBrainAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Load existing brain state or create a new one
        /// This ensures training sessions are cumulative rather than starting fresh
        /// </summary>
        private async Task<LanguageEphemeralBrain> LoadOrCreateBrainAsync()
        {
            if (_storageManager.HasExistingBrainState())
            {
                try
                {
                    Console.WriteLine("📂 Loading existing brain state using biological storage...");
                    var brain = await LoadExistingBrainAsync();
                    var stats = brain.GetLearningStats();
                    Console.WriteLine($"   ✅ Loaded brain with {stats.VocabularySize:N0} words, {stats.TotalConcepts:N0} concepts");
                    Console.WriteLine($"   📊 Training sessions: {stats.TrainingSessions}");
                    return brain;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️  Failed to load existing brain: {ex.Message}");
                    Console.WriteLine("   🔄 Creating fresh brain instead");
                }
            }
            else
            {
                Console.WriteLine("📂 No existing brain state found - creating fresh brain");
            }
            
            return new LanguageEphemeralBrain();
        }

        /// <summary>
        /// Load existing brain state from biological storage
        /// </summary>
        private async Task<LanguageEphemeralBrain> LoadExistingBrainAsync()
        {
            var brain = new LanguageEphemeralBrain();
            
            // Load vocabulary from biological storage
            var vocabulary = await _storageManager.LoadVocabularyAsync();
            if (vocabulary.Any())
            {
                Console.WriteLine($"   📚 Loading {vocabulary.Count:N0} vocabulary entries...");
                var coreVocabulary = ConvertToCoreVocabulary(vocabulary);
                brain.ImportVocabulary(coreVocabulary);
            }
            
            // Load language data (concepts, patterns, etc.)
            var languageData = await _storageManager.LoadLanguageDataAsync();
            if (languageData.Any())
            {
                Console.WriteLine($"   🧠 Loading {languageData.Count:N0} language concepts...");
                brain.ImportLanguageData(languageData);
            }
            
            // CRITICAL FIX: Load neurons from the persistent neuron pool
            Console.WriteLine("   🔄 Loading neurons from persistent storage...");
            var neurons = await _storageManager.LoadAllNeuronsAsync();
            if (neurons.Any())
            {
                Console.WriteLine($"   🧠 Loading {neurons.Count:N0} neurons from pool...");
                brain.ImportNeurons(neurons);
            }
            else
            {
                Console.WriteLine("   ℹ️  No neurons found in storage pool");
            }
            
            return brain;
        }
        
        /// <summary>
        /// Save current brain state to biological storage
        /// </summary>
        public async Task SaveBrainStateAsync()
        {
            var totalStartTime = DateTime.UtcNow;
            Console.WriteLine("💾 Saving brain state to biological storage...");
            
            try
            {
                // Export vocabulary and save
                var vocabStartTime = DateTime.UtcNow;
                var coreVocabulary = _brain.ExportVocabulary();
                var storageVocabulary = ConvertToStorageVocabulary(coreVocabulary);
                await _storageManager.StoreVocabularyAsync(storageVocabulary);
                var vocabElapsed = DateTime.UtcNow - vocabStartTime;
                Console.WriteLine($"   ✅ Saved {storageVocabulary.Count:N0} vocabulary entries ({vocabElapsed.TotalMilliseconds:F0}ms)");
                
                // Export language data and save
                var langStartTime = DateTime.UtcNow;
                var languageData = _brain.ExportLanguageData();
                await _storageManager.StoreLanguageDataAsync(languageData);
                var langElapsed = DateTime.UtcNow - langStartTime;
                Console.WriteLine($"   ✅ Saved {languageData.Count:N0} language data entries ({langElapsed.TotalMilliseconds:F0}ms)");
                
                // Export individual neural concepts for semantic domain categorization
                var conceptStartTime = DateTime.UtcNow;
                var neuralConcepts = _brain.ExportNeuralConcepts();
                Console.WriteLine($"   🧠 Saving {neuralConcepts.Count:N0} neural concepts...");
                
                // Add timeout protection for large concept sets
                var saveTask = _storageManager.StoreNeuralConceptsAsync(neuralConcepts);
                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(15)); // 15 minute timeout
                
                var completedTask = await Task.WhenAny(saveTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    Console.WriteLine("   ⚠️ Neural concept saving timed out, skipping for now");
                }
                else
                {
                    await saveTask; // Ensure we get any exceptions
                    var conceptElapsed = DateTime.UtcNow - conceptStartTime;
                    Console.WriteLine($"   ✅ Saved {neuralConcepts.Count:N0} neural concepts to semantic domains ({conceptElapsed.TotalMilliseconds:F0}ms)");
                }
                
                // CRITICAL FIX: Export neurons from brain and add to storage manager pool
                var neuronStartTime = DateTime.UtcNow;
                Console.WriteLine("   🧠 Exporting neurons from brain...");
                var brainNeurons = _brain.ExportNeurons(); // Get neurons from LanguageEphemeralBrain
                Console.WriteLine($"   📊 Found {brainNeurons.Count:N0} neurons to persist");
                
                // Add each neuron to the storage manager's pool
                foreach (var neuron in brainNeurons)
                {
                    await _storageManager.AddNeuronToPoolAsync(neuron.Key, neuron.Value);
                }
                var neuronElapsed = DateTime.UtcNow - neuronStartTime;
                Console.WriteLine($"   ✅ Added {brainNeurons.Count:N0} neurons to pool ({neuronElapsed.TotalMilliseconds:F0}ms)");
                
                // Flush neuron pool to disk
                var flushStartTime = DateTime.UtcNow;
                Console.WriteLine("   💾 Flushing neuron pool...");
                await _storageManager.FlushNeuronPoolAsync();
                var flushElapsed = DateTime.UtcNow - flushStartTime;
                Console.WriteLine($"   ✅ Saved neural network state ({flushElapsed.TotalMilliseconds:F0}ms)");
                
                // Get storage statistics
                var stats = await _storageManager.GetStorageStatisticsAsync();
                Console.WriteLine($"   📊 Total storage: {stats.TotalStorageSize / 1024 / 1024:F1} MB");
                Console.WriteLine($"   📊 Neuron pool: {stats.TotalNeuronsInPool:N0} neurons");
                
                var totalElapsed = DateTime.UtcNow - totalStartTime;
                Console.WriteLine($"💾 Brain state saved successfully! (Total: {totalElapsed.TotalMilliseconds:F0}ms)");
            }
            catch (Exception ex)
            {
                var totalElapsed = DateTime.UtcNow - totalStartTime;
                Console.WriteLine($"❌ Failed to save brain state: {ex.Message} (after {totalElapsed.TotalMilliseconds:F0}ms)");
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

            Console.WriteLine($"✅ Found {sentences.Count:N0} suitable learning sentences");
            
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
            Console.WriteLine($"\n✅ Vocabulary foundation complete!");
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
        /// </summary>
        public async Task SaveTrainedBrain(string savePath = "")
        {
            if (string.IsNullOrEmpty(savePath))
            {
                savePath = "/Volumes/jarvis/brainData";
            }

            Console.WriteLine($"\n💾 Saving trained language brain to: {savePath}");
            
            // Use the ScalePersistence system like in ScaleDemo
            var persistence = new ScalePersistence(savePath);
            await persistence.SaveBrain(_brain);
            
            Console.WriteLine("✅ Brain saved successfully!");
            
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
            Console.WriteLine($"\n✅ Sample vocabulary building complete!");
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
                
                Console.WriteLine($"   ✅ Block {blockNumber} complete in {blockElapsed:mm\\:ss}");
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
            Console.WriteLine($"\n✅ Random sample training complete!");
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

            Console.WriteLine($"   ✅ Collected {sentences.Count:N0} suitable sentences");
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

        private Dictionary<string, CoreWordInfo> ConvertToCoreVocabulary(Dictionary<string, StorageWordInfo> storageVocab)
        {
            return storageVocab.ToDictionary(
                kvp => kvp.Key,
                kvp => new CoreWordInfo
                {
                    Word = kvp.Value.Word,
                    Frequency = kvp.Value.Frequency,
                    FirstSeen = kvp.Value.FirstSeen,
                    EstimatedType = (greyMatter.Core.WordType)(int)kvp.Value.EstimatedType
                });
        }

        private Dictionary<string, StorageWordInfo> ConvertToStorageVocabulary(Dictionary<string, CoreWordInfo> coreVocab)
        {
            return coreVocab.ToDictionary(
                kvp => kvp.Key,
                kvp => new StorageWordInfo
                {
                    Word = kvp.Value.Word,
                    Frequency = kvp.Value.Frequency,
                    FirstSeen = kvp.Value.FirstSeen,
                    EstimatedType = (GreyMatter.Storage.WordType)(int)kvp.Value.EstimatedType
                });
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
        }
    }
}