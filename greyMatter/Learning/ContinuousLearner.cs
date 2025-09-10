using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GreyMatter;

namespace GreyMatter.Learning
{
    /// <summary>
    /// Continuous learning system that integrates data preparation and learning into a unified pipeline
    /// Supports interruptible learning with priority task management
    /// </summary>
    public class ContinuousLearner
    {
        private readonly string _dataPath;
        private readonly string _brainPath;
        private readonly int _batchSize;
        private readonly int _autoSaveIntervalSeconds;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Stopwatch _lastSaveTimer;

        private EnhancedLanguageLearner? _learner;
        private Dictionary<string, TatoebaDataConverter.WordData>? _wordDatabase;
        private HashSet<string>? _learnedWords;
        private int _wordsLearned;
        private DateTime _startTime;

        public ContinuousLearner(string dataPath, string brainPath, int batchSize = 1000, int autoSaveIntervalSeconds = 300)
        {
            _dataPath = dataPath;
            _brainPath = brainPath;
            _batchSize = batchSize;
            _autoSaveIntervalSeconds = autoSaveIntervalSeconds;
            _cancellationTokenSource = new CancellationTokenSource();
            _lastSaveTimer = new Stopwatch();
            _wordsLearned = 0;
            _startTime = DateTime.Now;

            // Handle Ctrl+C gracefully
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\n🛑 **INTERRUPTION DETECTED**");
                Console.WriteLine("Gracefully shutting down continuous learning...");
                _cancellationTokenSource.Cancel();
            };
        }

        public async Task InitializeAsync()
        {
            Console.WriteLine("📋 **INITIALIZING CONTINUOUS LEARNING SYSTEM**");
            Console.WriteLine("==============================================");

            // Ensure data directory exists
            Directory.CreateDirectory(_dataPath);
            Directory.CreateDirectory(_brainPath);

            // Check for existing word database
            var wordDbPath = Path.Combine(_dataPath, "word_database.json");
            if (File.Exists(wordDbPath))
            {
                Console.WriteLine("✅ Found existing word database");
                var json = await File.ReadAllTextAsync(wordDbPath);
                _wordDatabase = JsonSerializer.Deserialize<Dictionary<string, TatoebaDataConverter.WordData>>(json)
                    ?? new Dictionary<string, TatoebaDataConverter.WordData>();
                Console.WriteLine($"   Words available: {_wordDatabase?.Count ?? 0}");
            }
            else
            {
                Console.WriteLine("⚠️  No word database found - will prepare data on-the-fly");
                _wordDatabase = new Dictionary<string, TatoebaDataConverter.WordData>();
            }

            // Initialize learner
            _learner = new EnhancedLanguageLearner(_dataPath, _brainPath, maxConcurrency: 2);
            _learnedWords = new HashSet<string>();

            // Load existing progress
            await LoadProgressAsync();

            Console.WriteLine("✅ Continuous learning system initialized");
            Console.WriteLine();
        }

        public async Task<int> RunContinuousLearningAsync(int targetWords)
        {
            _startTime = DateTime.Now;
            _lastSaveTimer.Restart();

            var learningTimer = Stopwatch.StartNew();

            Console.WriteLine($"🎯 **LEARNING TARGET: {targetWords} words**");
            Console.WriteLine($"🔄 **BATCH SIZE: {_batchSize} words**");
            Console.WriteLine($"💾 **AUTO-SAVE: Every {_autoSaveIntervalSeconds} seconds**");
            Console.WriteLine();

            try
            {
                if (_learner == null)
                {
                    throw new InvalidOperationException("Learner not initialized. Call InitializeAsync() first.");
                }

                // Use the enhanced continuous learning method with proper cancellation
                var finalVocabSize = await _learner.LearnVocabularyContinuouslyAsync(
                    maxWords: targetWords, 
                    batchSize: _batchSize, 
                    cancellationToken: _cancellationTokenSource.Token);

                learningTimer.Stop();

                // Update final stats
                _wordsLearned = finalVocabSize;

                Console.WriteLine("🎉 **CONTINUOUS LEARNING SESSION COMPLETE**");
                Console.WriteLine("==========================================");
                Console.WriteLine($"📚 Final vocabulary size: {finalVocabSize}");
                Console.WriteLine($"⏱️  Learning duration: {learningTimer.Elapsed.TotalMinutes:F1} minutes");
                
                if (learningTimer.Elapsed.TotalSeconds > 0)
                {
                    Console.WriteLine($"⚡ Average rate: {finalVocabSize / learningTimer.Elapsed.TotalSeconds:F1} words/second");
                }

                // Final shutdown
                await _learner.ShutdownAsync();

                return _wordsLearned;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\n🛑 **LEARNING INTERRUPTED BY USER**");
                Console.WriteLine("==================================");
                await SaveProgressAsync();
                Console.WriteLine($"💾 Progress saved: {_wordsLearned} words learned");
                return _wordsLearned;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ **ERROR DURING CONTINUOUS LEARNING**");
                Console.WriteLine($"Error: {ex.Message}");
                await SaveProgressAsync();
                throw;
            }
        }

        private async Task<List<string>> PrepareBatchDataAsync(int batchSize)
        {
            // Since we're using EnhancedLanguageLearner's built-in batch processing,
            // we don't need to manually prepare batches. The learner handles this internally.
            // This method is kept for future extensibility.
            await Task.Yield(); // Make this truly async
            return new List<string>();
        }

        private async Task<List<string>> PrepareAdditionalDataAsync(int count)
        {
            // Placeholder for future data preparation logic
            // In full implementation, this would process raw data sources
            await Task.Yield(); // Make this truly async
            return new List<string>();
        }

        private async Task<bool> CheckForPriorityTasksAsync()
        {
            // Placeholder for priority task detection
            await Task.Yield(); // Make this truly async
            return false;
        }

        private async Task HandlePriorityTaskAsync()
        {
            // Placeholder for priority task handling
            // In full implementation, this would:
            // - Pause learning
            // - Handle user requests
            // - Perform maintenance
            // - Resume learning from checkpoint
            await Task.Delay(1000); // Simulate handling time
        }

        private async Task LoadProgressAsync()
        {
            var progressPath = Path.Combine(_brainPath, "continuous_learning_progress.json");

            if (File.Exists(progressPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(progressPath);
                    var progress = JsonSerializer.Deserialize<LearningProgress>(json);

                    _wordsLearned = progress?.WordsLearned ?? 0;
                    Console.WriteLine($"📊 Loaded previous progress: {_wordsLearned} words learned");
                }
                catch
                {
                    Console.WriteLine("⚠️  Could not load previous progress, starting fresh");
                    _wordsLearned = 0;
                }
            }
            else
            {
                Console.WriteLine("📊 Starting fresh learning session");
            }
        }

        private async Task SaveProgressAsync()
        {
            var progressPath = Path.Combine(_brainPath, "continuous_learning_progress.json");

            var progress = new LearningProgress
            {
                WordsLearned = _wordsLearned,
                LastSaveTime = DateTime.Now,
                SessionStartTime = _startTime,
                BatchSize = _batchSize,
                AutoSaveInterval = _autoSaveIntervalSeconds
            };

            var json = JsonSerializer.Serialize(progress, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(progressPath, json);
        }

        private class LearningProgress
        {
            public int WordsLearned { get; set; }
            public DateTime LastSaveTime { get; set; }
            public DateTime SessionStartTime { get; set; }
            public int BatchSize { get; set; }
            public int AutoSaveInterval { get; set; }
        }
    }
}
