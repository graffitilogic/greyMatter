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
    /// Background service for continuous learning that runs 24/7.
    /// Features:
    /// - Infinite learning loop with data source rotation
    /// - Auto-save checkpoints every N concepts
    /// - Graceful shutdown on signal
    /// - Crash recovery with state persistence
    /// - Progress tracking and monitoring
    /// </summary>
    public class ContinuousLearningService
    {
        private readonly IntegratedTrainer _trainer;
        private readonly LanguageEphemeralBrain _brain;
        private readonly ProductionStorageManager _storage;
        private readonly string _dataPath;
        private readonly string _statusFilePath;
        private readonly string _controlFilePath;
        
        // Configuration
        private readonly int _autoSaveInterval; // Save every N sentences
        private readonly int _batchSize;
        private readonly bool _useIntegration;
        
        // State
        private bool _isRunning = false;
        private bool _isPaused = false;
        private CancellationTokenSource? _cancellationTokenSource;
        private ServiceStatus _status;
        private DateTime _startTime;
        
        // Statistics
        private long _totalSentencesProcessed = 0;
        private long _totalVocabularyLearned = 0;
        private int _consecutiveSaveFailures = 0;
        private DateTime _lastSaveTime = DateTime.MinValue;
        private DateTime _lastStatusUpdate = DateTime.MinValue;

        public ContinuousLearningService(
            string dataPath,
            ProductionStorageManager? storage = null,
            int autoSaveInterval = 1000,
            int batchSize = 100,
            bool useIntegration = true,
            bool enableAttention = false,
            bool enableEpisodicMemory = false,
            double attentionThreshold = 0.4)
        {
            _dataPath = dataPath ?? throw new ArgumentNullException(nameof(dataPath));
            _autoSaveInterval = autoSaveInterval;
            _batchSize = batchSize;
            _useIntegration = useIntegration;
            
            // Use centralized production storage
            _storage = storage ?? new ProductionStorageManager();
            
            // Legacy status/control files in live directory (production storage doesn't need separate directory)
            _statusFilePath = Path.Combine(_storage.GetLivePath(""), "status.json");
            _controlFilePath = Path.Combine(_storage.GetLivePath(""), "control.json");
            
            // Initialize brain and trainer (Week 7: with attention & episodic memory)
            _brain = new LanguageEphemeralBrain();
            _trainer = new IntegratedTrainer(
                _brain,
                enableColumnProcessing: useIntegration,
                enableTraditionalLearning: true,
                enableIntegration: useIntegration,
                enableAttention: enableAttention,
                enableEpisodicMemory: enableEpisodicMemory,
                attentionThreshold: attentionThreshold,
                episodicMemoryPath: _storage.GetEpisodicMemoryPath()
            );
            
            // Log Week 7 features if enabled
            if (enableAttention)
            {
                Console.WriteLine($"✨ Attention system enabled (threshold: {attentionThreshold:F2})");
            }
            if (enableEpisodicMemory)
            {
                Console.WriteLine($"✨ Episodic memory enabled");
            }
            
            // Initialize status
            _status = new ServiceStatus
            {
                State = ServiceState.Stopped,
                StartTime = DateTime.MinValue,
                LastActivity = DateTime.MinValue,
                SentencesProcessed = 0,
                VocabularySize = 0,
                CurrentDataSource = "",
                Message = "Service initialized"
            };
            
            // Try to load previous state
            Task.Run(async () => await TryLoadCheckpointAsync()).Wait();
        }

        /// <summary>
        /// Start the continuous learning service
        /// </summary>
        public async Task StartAsync()
        {
            if (_isRunning)
            {
                Console.WriteLine("⚠️  Service already running");
                return;
            }
            
            _isRunning = true;
            _startTime = DateTime.Now;
            _cancellationTokenSource = new CancellationTokenSource();
            
            _status.State = ServiceState.Running;
            _status.StartTime = _startTime;
            _status.Message = "Service started";
            await UpdateStatusFileAsync();
            
            Console.WriteLine("🚀 Continuous Learning Service started");
            Console.WriteLine($"   Data path: {_dataPath}");
            Console.WriteLine($"   Auto-save interval: {_autoSaveInterval} sentences");
            Console.WriteLine($"   Batch size: {_batchSize}");
            Console.WriteLine($"   Integration: {(_useIntegration ? "enabled" : "disabled")}");
            Console.WriteLine($"   Status file: {_statusFilePath}");
            Console.WriteLine($"   Control file: {_controlFilePath}");
            Console.WriteLine();
            
            // Start the learning loop
            await RunLearningLoopAsync(_cancellationTokenSource.Token);
        }

        /// <summary>
        /// Stop the service gracefully
        /// </summary>
        public async Task StopAsync()
        {
            if (!_isRunning)
            {
                return;
            }
            
            Console.WriteLine("\n🛑 Stopping continuous learning service...");
            
            _cancellationTokenSource?.Cancel();
            _isRunning = false;
            
            // Save final state
            await SaveCheckpointAsync("shutdown");
            
            _status.State = ServiceState.Stopped;
            _status.Message = "Service stopped gracefully";
            await UpdateStatusFileAsync();
            
            Console.WriteLine("✅ Service stopped");
        }

        /// <summary>
        /// Main learning loop - runs continuously until stopped
        /// </summary>
        private async Task RunLearningLoopAsync(CancellationToken cancellationToken)
        {
            var dataSources = DiscoverDataSources();
            if (dataSources.Count == 0)
            {
                Console.WriteLine("❌ No data sources found in: " + _dataPath);
                _status.State = ServiceState.Error;
                _status.Message = "No data sources available";
                await UpdateStatusFileAsync();
                return;
            }
            
            Console.WriteLine($"📚 Discovered {dataSources.Count} data sources");
            foreach (var source in dataSources)
            {
                Console.WriteLine($"   - {source}");
            }
            Console.WriteLine();
            
            int currentSourceIndex = 0;
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Check control file for pause/resume/stop commands
                    await CheckControlFileAsync();
                    
                    if (_isPaused)
                    {
                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }
                    
                    // Rotate through data sources
                    var currentSource = dataSources[currentSourceIndex];
                    _status.CurrentDataSource = Path.GetFileName(currentSource);
                    
                    // Load and process a batch
                    var sentences = await LoadSentenceBatchAsync(currentSource, _batchSize);
                    if (sentences.Count == 0)
                    {
                        // Move to next source
                        currentSourceIndex = (currentSourceIndex + 1) % dataSources.Count;
                        continue;
                    }
                    
                    // Train on batch
                    foreach (var sentence in sentences)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;
                            
                        await _trainer.TrainOnSentenceAsync(sentence);
                        _totalSentencesProcessed++;
                        
                        // Auto-save checkpoint
                        if (_totalSentencesProcessed % _autoSaveInterval == 0)
                        {
                            await SaveCheckpointAsync("auto");
                        }
                    }
                    
                    // Update statistics
                    _totalVocabularyLearned = _brain.VocabularySize;
                    _status.SentencesProcessed = _totalSentencesProcessed;
                    _status.VocabularySize = _totalVocabularyLearned;
                    _status.LastActivity = DateTime.Now;
                    
                    // Update status file periodically (every 10 seconds)
                    if ((DateTime.Now - _lastStatusUpdate).TotalSeconds >= 10)
                    {
                        await UpdateStatusFileAsync();
                        _lastStatusUpdate = DateTime.Now;
                        
                        // Log progress
                        var elapsed = stopwatch.Elapsed;
                        var rate = _totalSentencesProcessed / elapsed.TotalSeconds;
                        Console.WriteLine($"📊 Progress: {_totalSentencesProcessed:N0} sentences, {_totalVocabularyLearned:N0} vocabulary ({rate:F1} sent/sec)");
                    }
                    
                    // Rotate to next source
                    currentSourceIndex = (currentSourceIndex + 1) % dataSources.Count;
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Service cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in learning loop: {ex.Message}");
                _status.State = ServiceState.Error;
                _status.Message = $"Error: {ex.Message}";
                await UpdateStatusFileAsync();
                
                // Save emergency checkpoint
                await SaveCheckpointAsync("crash");
                throw;
            }
        }

        /// <summary>
        /// Discover available data sources
        /// </summary>
        private List<string> DiscoverDataSources()
        {
            var sources = new List<string>();
            
            if (Directory.Exists(_dataPath))
            {
                // Look for .txt files
                sources.AddRange(Directory.GetFiles(_dataPath, "*.txt", SearchOption.AllDirectories));
                
                // Look for .tsv files (Tatoeba format)
                sources.AddRange(Directory.GetFiles(_dataPath, "*.tsv", SearchOption.AllDirectories));
            }
            
            return sources;
        }

        /// <summary>
        /// Load a batch of sentences from a data source
        /// </summary>
        private async Task<List<string>> LoadSentenceBatchAsync(string sourcePath, int count)
        {
            var sentences = new List<string>();
            
            try
            {
                var allLines = await File.ReadAllLinesAsync(sourcePath);
                
                // Skip lines we've already processed (simple approach: use random sampling)
                var random = new Random((int)(_totalSentencesProcessed % int.MaxValue));
                var sampled = allLines
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .OrderBy(x => random.Next())
                    .Take(count);
                
                foreach (var line in sampled)
                {
                    // Handle TSV format (Tatoeba)
                    if (sourcePath.EndsWith(".tsv"))
                    {
                        var parts = line.Split('\t');
                        if (parts.Length >= 2)
                        {
                            sentences.Add(parts[1].Trim());
                        }
                    }
                    else
                    {
                        sentences.Add(line.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Error loading from {sourcePath}: {ex.Message}");
            }
            
            return sentences;
        }

        /// <summary>
        /// Save a checkpoint of the current state
        /// </summary>
        private async Task SaveCheckpointAsync(string reason)
        {
            try
            {
                // Save vocabulary to live state
                await _storage.SaveLiveStateAsync("vocabulary.json", _brain.ExportVocabulary());
                
                // Save checkpoint with metadata using ProductionStorageManager
                var checkpoint = new BrainCheckpoint
                {
                    Timestamp = DateTime.Now,
                    SentencesProcessed = _totalSentencesProcessed,
                    VocabularySize = _brain.VocabularySize,
                    SynapseCount = 0, // TODO: Track if needed
                    TrainingHours = (DateTime.Now - _startTime).TotalHours,
                    AverageTrainingRate = _totalSentencesProcessed / Math.Max(1, (DateTime.Now - _startTime).TotalSeconds),
                    MemoryUsageGB = GC.GetTotalMemory(false) / (1024.0 * 1024.0 * 1024.0)
                };
                
                await _storage.SaveCheckpointAsync(checkpoint);
                
                // Log metrics
                await _storage.LogMetricAsync("sentences_processed", _totalSentencesProcessed);
                await _storage.LogMetricAsync("vocabulary_size", _brain.VocabularySize);
                
                _lastSaveTime = DateTime.Now;
                _consecutiveSaveFailures = 0;
                
                Console.WriteLine($"💾 Checkpoint saved: {checkpoint.Timestamp:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                _consecutiveSaveFailures++;
                Console.WriteLine($"⚠️  Failed to save checkpoint: {ex.Message}");
                
                if (_consecutiveSaveFailures >= 3)
                {
                    Console.WriteLine("❌ Multiple consecutive save failures - stopping service");
                    await StopAsync();
                }
            }
        }

        /// <summary>
        /// Try to load the most recent checkpoint
        /// </summary>
        private async Task TryLoadCheckpointAsync()
        {
            try
            {
                var latestCheckpoint = await _storage.LoadLatestCheckpointAsync();
                if (latestCheckpoint == null)
                {
                    Console.WriteLine("ℹ️  No checkpoint found - starting fresh");
                    return;
                }
                
                Console.WriteLine($"🔄 Loading checkpoint from {latestCheckpoint.Timestamp:yyyy-MM-dd HH:mm:ss}");
                
                // Load vocabulary separately from live state
                var vocab = await _storage.LoadLiveStateAsync<Dictionary<string, greyMatter.Core.WordInfo>>("vocabulary.json");
                if (vocab != null)
                {
                    _brain.ImportVocabulary(vocab);
                    _totalVocabularyLearned = vocab.Count;
                    _totalSentencesProcessed = latestCheckpoint.SentencesProcessed;
                    
                    Console.WriteLine($"✅ Restored {vocab.Count} words, {latestCheckpoint.SentencesProcessed} sentences processed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Failed to load checkpoint: {ex.Message}");
                Console.WriteLine("Starting with fresh state");
            }
        }

        /// <summary>
        /// Cleanup is now handled by ProductionStorageManager
        /// </summary>
        private void CleanupOldCheckpoints(int keepCount = 24)
        {
            // Cleanup now handled by ProductionStorageManager.CleanupOldCheckpoints()
        }

        /// <summary>
        /// Update the status JSON file for monitoring
        /// </summary>
        private async Task UpdateStatusFileAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_status, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_statusFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Could not update status file: {ex.Message}");
            }
        }

        /// <summary>
        /// Check control file for commands (pause/resume/stop)
        /// </summary>
        private async Task CheckControlFileAsync()
        {
            try
            {
                if (!File.Exists(_controlFilePath))
                    return;
                
                var json = await File.ReadAllTextAsync(_controlFilePath);
                var control = JsonSerializer.Deserialize<ControlCommand>(json);
                
                if (control == null)
                    return;
                
                switch (control.Command?.ToLower())
                {
                    case "pause":
                        if (!_isPaused)
                        {
                            _isPaused = true;
                            _status.State = ServiceState.Paused;
                            _status.Message = "Service paused by control file";
                            await UpdateStatusFileAsync();
                            Console.WriteLine("⏸️  Service paused");
                        }
                        break;
                        
                    case "resume":
                        if (_isPaused)
                        {
                            _isPaused = false;
                            _status.State = ServiceState.Running;
                            _status.Message = "Service resumed";
                            await UpdateStatusFileAsync();
                            Console.WriteLine("▶️  Service resumed");
                        }
                        break;
                        
                    case "stop":
                        Console.WriteLine("🛑 Stop command received from control file");
                        await StopAsync();
                        _cancellationTokenSource?.Cancel();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Error reading control file: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current service statistics
        /// </summary>
        public ServiceStatistics GetStatistics()
        {
            var uptime = _isRunning ? (DateTime.Now - _startTime) : TimeSpan.Zero;
            var rate = uptime.TotalSeconds > 0 ? _totalSentencesProcessed / uptime.TotalSeconds : 0;
            
            return new ServiceStatistics
            {
                IsRunning = _isRunning,
                IsPaused = _isPaused,
                Uptime = uptime,
                SentencesProcessed = _totalSentencesProcessed,
                VocabularySize = _totalVocabularyLearned,
                ProcessingRate = rate,
                LastSaveTime = _lastSaveTime,
                ConsecutiveSaveFailures = _consecutiveSaveFailures,
                CurrentState = _status.State
            };
        }
    }

    #region Support Classes

    public enum ServiceState
    {
        Stopped,
        Running,
        Paused,
        Error
    }

    public class ServiceStatus
    {
        public ServiceState State { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime LastActivity { get; set; }
        public long SentencesProcessed { get; set; }
        public long VocabularySize { get; set; }
        public string CurrentDataSource { get; set; } = "";
        public string Message { get; set; } = "";
    }

    public class ServiceCheckpoint
    {
        public DateTime Timestamp { get; set; }
        public string Reason { get; set; } = "";
        public long SentencesProcessed { get; set; }
        public long VocabularySize { get; set; }
        public string BrainStateFile { get; set; } = "";
        public double Uptime { get; set; }
    }

    public class ControlCommand
    {
        public string Command { get; set; } = ""; // pause, resume, stop
        public DateTime Timestamp { get; set; }
    }

    public class ServiceStatistics
    {
        public bool IsRunning { get; set; }
        public bool IsPaused { get; set; }
        public TimeSpan Uptime { get; set; }
        public long SentencesProcessed { get; set; }
        public long VocabularySize { get; set; }
        public double ProcessingRate { get; set; }
        public DateTime LastSaveTime { get; set; }
        public int ConsecutiveSaveFailures { get; set; }
        public ServiceState CurrentState { get; set; }
    }

    #endregion
}
