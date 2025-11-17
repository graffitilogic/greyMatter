using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using greyMatter.Core;
using GreyMatter.Storage;

namespace GreyMatter.Core
{
    /// <summary>
    /// Production-grade 24/7 training service with checkpoint rehydration,
    /// validation cycles, and NAS archival.
    /// 
    /// Features:
    /// - Uses Cerebro for procedural generation and lazy loading
    /// - Loads from latest checkpoint on startup
    /// - Hourly checkpoint saves (lightweight cluster metadata)
    /// - 6-hour validation cycles
    /// - Daily NAS archival
    /// - Control via JSON file (pause/resume/stop)
    /// - Graceful shutdown with state preservation
    /// - Constant memory usage (no growth over time)
    /// </summary>
    public class ProductionTrainingService
    {
        private readonly ProductionStorageManager _storage;
        private readonly Cerebro _cerebro;
        private readonly EnhancedBrainStorage _brainStorage;
        private readonly TrainingDataProvider _dataProvider;
        private readonly LLMTeacher? _llmTeacher;
        private readonly string _datasetKey;
        private readonly string _controlFilePath;
        private readonly bool _useLLMTeacher;
        private readonly bool _useProgressiveCurriculum;
        
        // Configuration
        private readonly int _checkpointIntervalMinutes;
        private readonly int _validationIntervalHours;
        private readonly int _nasArchiveIntervalHours;
        
        // Dynamic curriculum state
        private TrainingPhase? _currentPhase = null;
        private long _lastCurriculumCheck = 0;
        private const int CURRICULUM_CHECK_INTERVAL = 100; // Check every 100 sentences
        
        // Training data management
        private List<string> _trainingSentences = new();
        private int _sentenceIndex = 0;
        private int _batchNumber = 0; // Track which batch we're on for shuffling
        private Random _random = new Random();
        
        // State
        private bool _isRunning = false;
        private bool _isPaused = false;
        private CancellationTokenSource? _cancellationTokenSource;
        private DateTime _startTime;
        private DateTime _lastCheckpoint;
        private DateTime _lastValidation;
        private DateTime _lastNASArchive;
        
        // Background tasks
        private Task? _trainingTask;
        private Task? _maintenanceTask;
        private Task? _controlTask;
        
        // Statistics
        private long _totalSentencesProcessed = 0;
        private long _sessionSentencesProcessed = 0;
        private int _checkpointsSaved = 0;
        private int _validationsPassed = 0;
        private int _validationsFailed = 0;

        public ProductionTrainingService(
            string? datasetKey = null,
            ProductionStorageManager? storage = null,
            TrainingDataProvider? dataProvider = null,
            LLMTeacher? llmTeacher = null,
            bool useLLMTeacher = false,
            bool useProgressiveCurriculum = true,
            int checkpointIntervalMinutes = 10, // Changed from 60 to 10 for safer persistence
            int validationIntervalHours = 6,
            int nasArchiveIntervalHours = 24,
            bool enableAttention = true,
            bool enableEpisodicMemory = true)
        {
            _datasetKey = datasetKey ?? "tatoeba_small";
            
            // Use NAS for persistent storage (not SSD!)
            var nasStoragePath = "/Volumes/jarvis/brainData";
            _storage = storage ?? new ProductionStorageManager(nasStoragePath);
            _dataProvider = dataProvider ?? new TrainingDataProvider("/Volumes/jarvis/trainData");
            
            _llmTeacher = llmTeacher;
            _useLLMTeacher = useLLMTeacher && llmTeacher != null;
            _useProgressiveCurriculum = useProgressiveCurriculum;
            _checkpointIntervalMinutes = checkpointIntervalMinutes;
            _validationIntervalHours = validationIntervalHours;
            _nasArchiveIntervalHours = nasArchiveIntervalHours;
            
            _controlFilePath = Path.Combine(_storage.GetLivePath(""), "training_control.json");
            
            // Initialize Cerebro with EnhancedBrainStorage on NAS
            _brainStorage = new EnhancedBrainStorage(nasStoragePath);
            _cerebro = new Cerebro(nasStoragePath);
            
            Console.WriteLine("🏭 Production Training Service initialized with Cerebro");
            Console.WriteLine($"   Brain storage: {nasStoragePath}");
            Console.WriteLine($"   Dataset: {_datasetKey}");
            Console.WriteLine($"   LLM Teacher: {(_useLLMTeacher ? "Enabled" : "Disabled")}");
            Console.WriteLine($"   Progressive Curriculum: {_useProgressiveCurriculum}");
            Console.WriteLine($"   Checkpoint interval: {_checkpointIntervalMinutes} minutes");
            Console.WriteLine($"   Validation interval: {_validationIntervalHours} hours");
            Console.WriteLine($"   NAS archive interval: {_nasArchiveIntervalHours} hours");
            Console.WriteLine($"   Attention: {enableAttention}, Episodic: {enableEpisodicMemory}");
        }

        /// <summary>
        /// Start production training service
        /// </summary>
        public async Task StartAsync()
        {
            if (_isRunning)
            {
                Console.WriteLine("⚠️  Service already running");
                return;
            }

            Console.WriteLine("\n" + "═".PadRight(80, '═'));
            Console.WriteLine("PRODUCTION TRAINING SERVICE - STARTING");
            Console.WriteLine("═".PadRight(80, '═') + "\n");

            _isRunning = true;
            _startTime = DateTime.Now;
            _lastCheckpoint = _startTime;
            _lastValidation = _startTime;
            _lastNASArchive = _startTime;
            _cancellationTokenSource = new CancellationTokenSource();

            // Initialize Cerebro
            Console.WriteLine("🧠 Initializing Cerebro...");
            await _cerebro.InitializeAsync();
            
            // Load latest checkpoint if available
            await LoadLatestCheckpointAsync();

            // Start background tasks (use Task.Run to ensure they truly run in background)
            _trainingTask = Task.Run(() => RunTrainingLoopAsync(_cancellationTokenSource.Token));
            _maintenanceTask = Task.Run(() => RunMaintenanceLoopAsync(_cancellationTokenSource.Token));
            _controlTask = Task.Run(() => MonitorControlFileAsync(_cancellationTokenSource.Token));

            Console.WriteLine("✅ Production training service started");
            Console.WriteLine($"   Control file: {_controlFilePath}");
            Console.WriteLine($"   Start time: {_startTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();
            
            // Return immediately - tasks run in background
        }

        /// <summary>
        /// Main training loop - processes data continuously with dynamic curriculum
        /// </summary>
        private async Task RunTrainingLoopAsync(CancellationToken cancellationToken)
        {
            // Initial data load
            _trainingSentences = LoadTrainingData();
            _sentenceIndex = 0;
            var lastProgressUpdate = DateTime.Now;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (_isPaused)
                {
                    await Task.Delay(1000, cancellationToken);
                    continue;
                }

                try
                {
                    // Check if we need to reload data (reached end of batch)
                    if (_sentenceIndex >= _trainingSentences.Count)
                    {
                        Console.WriteLine($"\n📚 Batch exhausted ({_sentenceIndex} sentences processed)");
                        Console.WriteLine($"   Reloading fresh training batch...");
                        ReloadTrainingData();
                    }
                    
                    // Process next sentence
                    var sentence = _trainingSentences[_sentenceIndex];
                    
                    // Extract features from sentence (simple word-based features)
                    var features = ExtractFeatures(sentence);
                    
                    // Learn each word as a concept with Cerebro
                    var words = sentence.ToLower()
                        .Split(new[] { ' ', '.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']' }, 
                               StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var word in words)
                    {
                        if (word.Length > 1) // Skip single characters
                        {
                            await _cerebro.LearnConceptAsync(word, features);
                        }
                    }
                    
                    _totalSentencesProcessed++;
                    _sessionSentencesProcessed++;
                    _sentenceIndex++;

                    // Check curriculum advancement every N sentences
                    if (_useProgressiveCurriculum && 
                        _totalSentencesProcessed - _lastCurriculumCheck >= CURRICULUM_CHECK_INTERVAL)
                    {
                        CheckAndAdvanceCurriculum();
                        _lastCurriculumCheck = _totalSentencesProcessed;
                    }

                    // Progress update every 10 seconds
                    if ((DateTime.Now - lastProgressUpdate).TotalSeconds >= 10)
                    {
                        var rate = _sessionSentencesProcessed / (DateTime.Now - _startTime).TotalSeconds;
                        var stats = await _cerebro.GetStatsAsync();
                        Console.WriteLine($"📊 Training: {_totalSentencesProcessed:N0} total | " +
                                        $"{_sessionSentencesProcessed:N0} session | " +
                                        $"{rate:F1} sent/sec | " +
                                        $"Clusters: {stats.TotalClusters:N0} | " +
                                        $"Neurons: {stats.TotalNeuronsCreated:N0}");
                        lastProgressUpdate = DateTime.Now;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Training error: {ex.Message}");
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Check if curriculum should advance and reload data if needed
        /// </summary>
        private void CheckAndAdvanceCurriculum()
        {
            var curriculum = _dataProvider.GetProgressiveCurriculum();
            var newPhase = curriculum.GetPhaseForSentenceCount(_totalSentencesProcessed);

            // Only reload if phase actually changed (not just checking)
            if (newPhase.Name != _currentPhase?.Name)
            {
                Console.WriteLine($"\n🎓 CURRICULUM ADVANCING");
                Console.WriteLine($"   From: {_currentPhase?.Name ?? "Initial"}");
                Console.WriteLine($"   To: {newPhase.Name}");
                Console.WriteLine($"   Sentences: {_totalSentencesProcessed:N0}");
                
                _currentPhase = newPhase;
                ReloadTrainingData();
            }
        }

        /// <summary>
        /// Reload training data - either for curriculum change or batch exhaustion
        /// Shuffles data each time to provide variety even within same dataset
        /// </summary>
        private void ReloadTrainingData()
        {
            try
            {
                // Determine which dataset to load
                string datasetName;
                if (_currentPhase != null)
                {
                    datasetName = _currentPhase.DatasetKey;
                    Console.WriteLine($"   Loading curriculum dataset: {datasetName}");
                }
                else
                {
                    // Use sentence count to determine phase
                    var curriculum = _dataProvider.GetProgressiveCurriculum();
                    var phase = curriculum.GetPhaseForSentenceCount(_totalSentencesProcessed);
                    datasetName = phase.DatasetKey;
                    Console.WriteLine($"   Loading dataset by count: {datasetName}");
                }

                // Load fresh batch (with shuffling for variety!)
                _batchNumber++;
                var sentences = _dataProvider.LoadSentences(
                    datasetName, 
                    maxSentences: 5000,
                    shuffle: true  // SHUFFLE each batch for variety!
                );
                var sentenceList = sentences.ToList();
                
                if (sentenceList.Any())
                {
                    _trainingSentences = sentenceList;
                    _sentenceIndex = 0;
                    Console.WriteLine($"✅ Loaded {sentenceList.Count:N0} fresh sentences from '{datasetName}' (batch #{_batchNumber}, shuffled)");
                }
                else
                {
                    Console.WriteLine($"⚠️  No sentences loaded from '{datasetName}', keeping current batch");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Failed to reload training data: {ex.Message}");
                Console.WriteLine($"   Continuing with current batch");
            }
        }

        /// <summary>
        /// Maintenance loop - handles checkpoints, validation, archival
        /// </summary>
        private async Task RunMaintenanceLoopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("🔧 Maintenance loop started!");
            var iteration = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(60000, cancellationToken); // Check every minute
                    iteration++;

                    // Heartbeat every 10 minutes to confirm loop is running
                    if (iteration % 10 == 0)
                    {
                        Console.WriteLine($"💓 Maintenance loop heartbeat (iteration {iteration})");
                    }

                    if (_isPaused)
                        continue;

                    // Checkpoint if needed
                    var minutesSinceCheckpoint = (DateTime.Now - _lastCheckpoint).TotalMinutes;
                    if (minutesSinceCheckpoint >= _checkpointIntervalMinutes)
                    {
                        Console.WriteLine($"🔔 Checkpoint interval reached ({minutesSinceCheckpoint:F1} min >= {_checkpointIntervalMinutes} min)");
                        await SaveCheckpointAsync("hourly");
                    }

                    // Validation if needed
                    if ((DateTime.Now - _lastValidation).TotalHours >= _validationIntervalHours)
                    {
                        await RunValidationAsync();
                    }

                    // NAS archival if needed
                    if ((DateTime.Now - _lastNASArchive).TotalHours >= _nasArchiveIntervalHours)
                    {
                        await ArchiveToNASAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation - service is stopping
                    Console.WriteLine("🛑 Maintenance loop stopping (cancellation requested)");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Maintenance error occurred: {ex.Message}");
                    Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Monitor control file for pause/resume/stop commands
        /// </summary>
        private async Task MonitorControlFileAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(5000, cancellationToken); // Check every 5 seconds

                    if (File.Exists(_controlFilePath))
                    {
                        var json = await File.ReadAllTextAsync(_controlFilePath, cancellationToken);
                        var control = JsonSerializer.Deserialize<TrainingControl>(json);

                        if (control?.Command == "pause" && !_isPaused)
                        {
                            Console.WriteLine("\n⏸️  PAUSING training (control file command)");
                            _isPaused = true;
                            await SaveCheckpointAsync("pause");
                        }
                        else if (control?.Command == "resume" && _isPaused)
                        {
                            Console.WriteLine("\n▶️  RESUMING training (control file command)");
                            _isPaused = false;
                        }
                        else if (control?.Command == "stop")
                        {
                            Console.WriteLine("\n🛑 STOPPING training (control file command)");
                            await StopAsync();
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore control file errors
                }
            }
        }

        /// <summary>
        /// Load latest checkpoint and restore state
        /// </summary>
        private async Task LoadLatestCheckpointAsync()
        {
            try
            {
                var checkpoint = await _storage.LoadLatestCheckpointAsync();
                if (checkpoint == null)
                {
                    Console.WriteLine("ℹ️  No checkpoint found - starting fresh");
                    return;
                }

                Console.WriteLine($"🔄 Loading checkpoint from {checkpoint.Timestamp:yyyy-MM-dd HH:mm:ss}");
                
                // Cerebro loads its own state during InitializeAsync()
                // Just restore training progress
                _totalSentencesProcessed = checkpoint.SentencesProcessed;
                
                var stats = await _cerebro.GetStatsAsync();
                Console.WriteLine($"✅ Restored state:");
                Console.WriteLine($"   Clusters: {stats.TotalClusters:N0}");
                Console.WriteLine($"   Neurons: {stats.TotalNeuronsCreated:N0}");
                Console.WriteLine($"   Synapses: {stats.TotalSynapses:N0}");
                Console.WriteLine($"   Sentences: {_totalSentencesProcessed:N0}");
                Console.WriteLine($"   Training hours: {checkpoint.TrainingHours:F1}");
                Console.WriteLine($"   Storage: {stats.StorageSizeFormatted}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Failed to load checkpoint: {ex.Message}");
                Console.WriteLine("Starting with fresh state");
            }
        }

        /// <summary>
        /// Save checkpoint with current state
        /// </summary>
        private async Task SaveCheckpointAsync(string reason)
        {
            try
            {
                Console.WriteLine($"\n💾 Saving checkpoint ({reason})...");

                // Save Cerebro state (lightweight cluster metadata)
                await _cerebro.SaveAsync();
                
                var stats = await _cerebro.GetStatsAsync();

                // Save checkpoint metadata
                var checkpoint = new BrainCheckpoint
                {
                    Timestamp = DateTime.Now,
                    SentencesProcessed = _totalSentencesProcessed,
                    VocabularySize = stats.TotalNeuronsCreated, // Use total neurons as proxy for vocabulary
                    SynapseCount = stats.TotalSynapses,
                    TrainingHours = (DateTime.Now - _startTime).TotalHours,
                    AverageTrainingRate = _sessionSentencesProcessed / Math.Max(1, (DateTime.Now - _startTime).TotalSeconds),
                    MemoryUsageGB = GC.GetTotalMemory(false) / (1024.0 * 1024.0 * 1024.0)
                };

                await _storage.SaveCheckpointAsync(checkpoint);

                // Log metrics
                await _storage.LogMetricAsync("sentences_processed", _totalSentencesProcessed);
                await _storage.LogMetricAsync("total_neurons", stats.TotalNeuronsCreated);
                await _storage.LogMetricAsync("total_clusters", stats.TotalClusters);
                await _storage.LogMetricAsync("total_synapses", stats.TotalSynapses);
                await _storage.LogMetricAsync("memory_usage_gb", checkpoint.MemoryUsageGB);

                _lastCheckpoint = DateTime.Now;
                _checkpointsSaved++;

                // Memory management: Force GC after checkpoint
                var beforeGC = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                var afterGC = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
                var freedMB = beforeGC - afterGC;

                Console.WriteLine($"✅ Checkpoint saved: {checkpoint.Timestamp:HH:mm:ss}");
                Console.WriteLine($"   Total checkpoints: {_checkpointsSaved}");
                Console.WriteLine($"   Storage size: {stats.StorageSizeFormatted}");
                Console.WriteLine($"   Memory: {afterGC:F1} MB (freed {freedMB:F1} MB)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to save checkpoint: {ex.Message}");
            }
        }

        /// <summary>
        /// Run validation suite to check knowledge quality
        /// </summary>
        private async Task RunValidationAsync()
        {
            try
            {
                Console.WriteLine("\n" + "═".PadRight(80, '═'));
                Console.WriteLine("VALIDATION CYCLE - Testing Knowledge Quality");
                Console.WriteLine("═".PadRight(80, '═') + "\n");

                _lastValidation = DateTime.Now;

                // Test 1: Cluster/Neuron growth
                var stats = await _cerebro.GetStatsAsync();
                var retentionPass = stats.TotalNeuronsCreated > 0;
                Console.WriteLine($"✓ Neural growth: {stats.TotalNeuronsCreated:N0} neurons, {stats.TotalClusters:N0} clusters ({(retentionPass ? "PASS" : "FAIL")})");

                // Test 2: Learning rate
                var rate = _sessionSentencesProcessed / Math.Max(1, (DateTime.Now - _startTime).TotalSeconds);
                var ratePass = rate > 0.1; // At least 0.1 sentences/sec
                Console.WriteLine($"✓ Learning rate: {rate:F2} sent/sec ({(ratePass ? "PASS" : "FAIL")})");

                // Test 3: Memory usage
                var memoryGB = GC.GetTotalMemory(false) / (1024.0 * 1024.0 * 1024.0);
                var memoryPass = memoryGB < 4.0; // Less than 4GB
                Console.WriteLine($"✓ Memory usage: {memoryGB:F2} GB ({(memoryPass ? "PASS" : "FAIL")})");

                // Overall validation result
                var validationPassed = retentionPass && ratePass && memoryPass;
                
                if (validationPassed)
                {
                    _validationsPassed++;
                    Console.WriteLine($"\n✅ VALIDATION PASSED ({_validationsPassed} total)");
                }
                else
                {
                    _validationsFailed++;
                    Console.WriteLine($"\n❌ VALIDATION FAILED ({_validationsFailed} total)");
                }

                Console.WriteLine("═".PadRight(80, '═') + "\n");

                // Save checkpoint after validation
                await SaveCheckpointAsync("validation");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Validation error: {ex.Message}");
                _validationsFailed++;
            }
        }

        /// <summary>
        /// Archive current state to NAS
        /// </summary>
        private async Task ArchiveToNASAsync()
        {
            try
            {
                Console.WriteLine("\n📦 Archiving to NAS...");
                
                var success = await _storage.ArchiveToNASAsync();
                
                if (success)
                {
                    _lastNASArchive = DateTime.Now;
                    Console.WriteLine("✅ NAS archive complete");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  NAS archive failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop the training service gracefully
        /// </summary>
        public async Task StopAsync()
        {
            if (!_isRunning)
                return;

            Console.WriteLine("\n🛑 Stopping production training service...");

            // Signal cancellation
            _cancellationTokenSource?.Cancel();
            
            // Wait for all tasks to complete
            try
            {
                var tasksToWait = new List<Task>();
                if (_trainingTask != null) tasksToWait.Add(_trainingTask);
                if (_maintenanceTask != null) tasksToWait.Add(_maintenanceTask);
                if (_controlTask != null) tasksToWait.Add(_controlTask);
                
                if (tasksToWait.Count > 0)
                {
                    await Task.WhenAll(tasksToWait);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when tasks are cancelled
            }

            // Save final checkpoint
            await SaveCheckpointAsync("shutdown");

            _isRunning = false;
            
            Console.WriteLine("✅ Service stopped gracefully");
        }

        /// <summary>
        /// Load training data using NAS-based provider with progressive curriculum
        /// </summary>
        private List<string> LoadTrainingData()
        {
            try
            {
                // Get current curriculum phase based on sentences processed
                TrainingPhase? currentPhase = null;
                if (_useProgressiveCurriculum)
                {
                    var curriculum = _dataProvider.GetProgressiveCurriculum(_datasetKey);
                    currentPhase = curriculum.GetPhaseForSentenceCount(_totalSentencesProcessed);
                    
                    Console.WriteLine($"\n📖 Curriculum Phase: {currentPhase.Name}");
                    Console.WriteLine($"   {currentPhase.Description}");
                }

                // Load sentences from NAS
                var datasetToLoad = currentPhase?.DatasetKey ?? _datasetKey;
                Console.WriteLine($"📂 Loading dataset: {datasetToLoad}");
                
                var sentences = currentPhase != null
                    ? _dataProvider.LoadSentences(
                        currentPhase.DatasetKey,
                        maxSentences: currentPhase.MaxSentences,
                        minWordCount: currentPhase.MinWordCount,
                        maxWordCount: currentPhase.MaxWordCount,
                        shuffle: true).ToList()
                    : _dataProvider.LoadSentences(_datasetKey, shuffle: true).ToList();

                if (sentences.Count == 0)
                {
                    Console.WriteLine($"❌ ERROR: No sentences loaded from dataset '{datasetToLoad}'");
                    Console.WriteLine($"   Dataset should be at NAS: /Volumes/jarvis/trainData");
                    throw new InvalidOperationException($"Failed to load dataset '{datasetToLoad}' - no training data available. Refusing to fall back to toy data.");
                }

                return sentences;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FATAL ERROR loading training data: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                throw;  // Don't fall back to toy data - this is a real error that needs to be fixed
            }
        }

        /// <summary>
        /// Get current service statistics
        /// </summary>
        public ProductionTrainingStats GetStats()
        {
            // Get Cerebro stats synchronously (GetStatsAsync can't be called from sync method)
            var cerebroStats = _cerebro.GetStatsAsync().Result;
            
            return new ProductionTrainingStats
            {
                IsRunning = _isRunning,
                IsPaused = _isPaused,
                Uptime = _isRunning ? DateTime.Now - _startTime : TimeSpan.Zero,
                TotalSentencesProcessed = _totalSentencesProcessed,
                SessionSentencesProcessed = _sessionSentencesProcessed,
                VocabularySize = cerebroStats.TotalNeuronsCreated, // Use neurons as proxy for vocabulary
                CheckpointsSaved = _checkpointsSaved,
                ValidationsPassed = _validationsPassed,
                ValidationsFailed = _validationsFailed,
                LastCheckpoint = _lastCheckpoint,
                LastValidation = _lastValidation,
                LastNASArchive = _lastNASArchive
            };
        }

        /// <summary>
        /// Extract simple features from a sentence for Cerebro learning
        /// </summary>
        private Dictionary<string, double> ExtractFeatures(string sentence)
        {
            var features = new Dictionary<string, double>();
            var lowerSentence = sentence.ToLower();
            
            // Basic features
            features["length"] = sentence.Length / 100.0; // Normalized
            features["words"] = sentence.Split(' ').Length / 20.0; // Normalized
            
            // Character type features
            features["hasUpper"] = sentence.Any(char.IsUpper) ? 1.0 : 0.0;
            features["hasDigit"] = sentence.Any(char.IsDigit) ? 1.0 : 0.0;
            features["hasPunctuation"] = sentence.Any(c => char.IsPunctuation(c)) ? 1.0 : 0.0;
            
            // Simple position encoding (first few characters as features)
            for (int i = 0; i < Math.Min(5, sentence.Length); i++)
            {
                features[$"pos_{i}"] = (double)char.ToLower(sentence[i]) / 128.0;
            }
            
            return features;
        }
    }

    /// <summary>
    /// Training control commands (pause/resume/stop)
    /// </summary>
    public class TrainingControl
    {
        public string Command { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Reason { get; set; } = "";
    }

    /// <summary>
    /// Production training statistics
    /// </summary>
    public class ProductionTrainingStats
    {
        public bool IsRunning { get; set; }
        public bool IsPaused { get; set; }
        public TimeSpan Uptime { get; set; }
        public long TotalSentencesProcessed { get; set; }
        public long SessionSentencesProcessed { get; set; }
        public int VocabularySize { get; set; }
        public int CheckpointsSaved { get; set; }
        public int ValidationsPassed { get; set; }
        public int ValidationsFailed { get; set; }
        public DateTime LastCheckpoint { get; set; }
        public DateTime LastValidation { get; set; }
        public DateTime LastNASArchive { get; set; }
    }
}
