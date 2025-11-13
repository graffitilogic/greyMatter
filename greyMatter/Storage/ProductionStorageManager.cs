using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace GreyMatter.Storage
{
    /// <summary>
    /// Production-grade storage manager with centralized paths and NAS archival
    /// NO MORE scattered ./demo_* directories - single source of truth
    /// </summary>
    public class ProductionStorageManager
    {
        // Centralized storage paths
        private readonly string _activeBrainPath;      // Fast SSD for active work
        private readonly string _archivePath;          // NAS for long-term storage
        private readonly string _checkpointPath;       // Hourly snapshots
        private readonly string _episodicPath;         // Episodic memory events
        private readonly string _metricsPath;          // Performance tracking
        
        // Public accessors for query systems
        public string ActiveBrainPath => _activeBrainPath;
        public string CheckpointPath => _checkpointPath;
        public string EpisodicPath => _episodicPath;
        public string MetricsPath => _metricsPath;
        
        // Configuration
        private readonly int _maxCheckpoints = 24;     // Keep last 24 hours
        private bool _enableNASArchive;                // Enable NAS archival (not readonly)
        
        public ProductionStorageManager(
            string activeBrainPath = "/Users/billdodd/Desktop/Cerebro/brainData",
            string nasArchivePath = "/Volumes/jarvis/brainData",
            bool enableNASArchive = true,
            int maxCheckpoints = 24)
        {
            _activeBrainPath = activeBrainPath;
            _archivePath = nasArchivePath;
            _enableNASArchive = enableNASArchive && Directory.Exists(nasArchivePath);
            _maxCheckpoints = maxCheckpoints;
            
            // Setup directory structure
            _checkpointPath = Path.Combine(_activeBrainPath, "checkpoints");
            _episodicPath = Path.Combine(_activeBrainPath, "episodic_memory");
            _metricsPath = Path.Combine(_activeBrainPath, "metrics");
            
            InitializeDirectories();
            
            Console.WriteLine("📁 Production Storage Manager Initialized");
            Console.WriteLine($"   Active brain data: {_activeBrainPath}");
            Console.WriteLine($"   NAS archive: {(_enableNASArchive ? _archivePath : "DISABLED")}");
            Console.WriteLine($"   Max checkpoints: {_maxCheckpoints}");
        }
        
        private void InitializeDirectories()
        {
            // Active brain data structure
            Directory.CreateDirectory(_activeBrainPath);
            Directory.CreateDirectory(Path.Combine(_activeBrainPath, "live"));
            Directory.CreateDirectory(_checkpointPath);
            Directory.CreateDirectory(_episodicPath);
            Directory.CreateDirectory(_metricsPath);
            
            // NAS archive structure (if available)
            if (_enableNASArchive)
            {
                try
                {
                    Directory.CreateDirectory(Path.Combine(_archivePath, "archives"));
                    Directory.CreateDirectory(Path.Combine(_archivePath, "training_logs"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  NAS not available: {ex.Message}");
                    _enableNASArchive = false;
                }
            }
        }
        
        #region Live State Management
        
        /// <summary>
        /// Get path for live brain data (vocabulary, synapses, etc.)
        /// </summary>
        public string GetLivePath(string filename)
        {
            return Path.Combine(_activeBrainPath, "live", filename);
        }
        
        /// <summary>
        /// Save live brain state (vocabulary, synapses, clusters)
        /// </summary>
        public async Task SaveLiveStateAsync<T>(string name, T data)
        {
            var path = GetLivePath($"{name}.json");
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions 
            { 
                WriteIndented = false  // Compact for speed
            });
            await File.WriteAllTextAsync(path, json);
        }
        
        /// <summary>
        /// Load live brain state
        /// </summary>
        public async Task<T?> LoadLiveStateAsync<T>(string name)
        {
            var path = GetLivePath($"{name}.json");
            if (!File.Exists(path))
                return default;
                
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<T>(json);
        }
        
        #endregion
        
        #region Checkpoint Management
        
        /// <summary>
        /// Save checkpoint with metadata
        /// </summary>
        public async Task<string> SaveCheckpointAsync(BrainCheckpoint checkpoint)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var checkpointDir = Path.Combine(_checkpointPath, timestamp);
            Directory.CreateDirectory(checkpointDir);
            
            // Save checkpoint data
            await File.WriteAllTextAsync(
                Path.Combine(checkpointDir, "vocabulary.json"),
                JsonSerializer.Serialize(checkpoint.Vocabulary)
            );
            
            await File.WriteAllTextAsync(
                Path.Combine(checkpointDir, "synapses.json"),
                JsonSerializer.Serialize(checkpoint.Synapses)
            );
            
            await File.WriteAllTextAsync(
                Path.Combine(checkpointDir, "clusters.json"),
                JsonSerializer.Serialize(checkpoint.Clusters)
            );
            
            // Save metadata
            var metadata = new CheckpointMetadata
            {
                Timestamp = checkpoint.Timestamp,
                SentencesProcessed = checkpoint.SentencesProcessed,
                VocabularySize = checkpoint.VocabularySize,
                SynapseCount = checkpoint.SynapseCount,
                TrainingHours = checkpoint.TrainingHours,
                AverageTrainingRate = checkpoint.AverageTrainingRate,
                MemoryUsageGB = checkpoint.MemoryUsageGB,
                CheckpointPath = checkpointDir
            };
            
            await File.WriteAllTextAsync(
                Path.Combine(checkpointDir, "metadata.json"),
                JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true })
            );
            
            // Update "latest" symlink (or marker file on Windows)
            var latestPath = Path.Combine(_checkpointPath, "latest.txt");
            await File.WriteAllTextAsync(latestPath, checkpointDir);
            
            Console.WriteLine($"💾 Checkpoint saved: {timestamp}");
            Console.WriteLine($"   Sentences: {checkpoint.SentencesProcessed:N0}");
            Console.WriteLine($"   Vocabulary: {checkpoint.VocabularySize:N0}");
            Console.WriteLine($"   Synapses: {checkpoint.SynapseCount:N0}");
            
            // Cleanup old checkpoints
            await CleanupOldCheckpointsAsync();
            
            return checkpointDir;
        }
        
        /// <summary>
        /// Load latest checkpoint
        /// </summary>
        public async Task<BrainCheckpoint?> LoadLatestCheckpointAsync()
        {
            var latestPath = Path.Combine(_checkpointPath, "latest.txt");
            if (!File.Exists(latestPath))
            {
                Console.WriteLine("ℹ️  No checkpoint found - starting fresh");
                return null;
            }
            
            var checkpointDir = (await File.ReadAllTextAsync(latestPath)).Trim();
            if (!Directory.Exists(checkpointDir))
            {
                Console.WriteLine("⚠️  Latest checkpoint directory missing - starting fresh");
                return null;
            }
            
            return await LoadCheckpointAsync(checkpointDir);
        }
        
        /// <summary>
        /// Load specific checkpoint
        /// </summary>
        public async Task<BrainCheckpoint?> LoadCheckpointAsync(string checkpointDir)
        {
            try
            {
                var metadataPath = Path.Combine(checkpointDir, "metadata.json");
                var metadata = JsonSerializer.Deserialize<CheckpointMetadata>(
                    await File.ReadAllTextAsync(metadataPath)
                );
                
                var checkpoint = new BrainCheckpoint
                {
                    Timestamp = metadata!.Timestamp,
                    SentencesProcessed = metadata.SentencesProcessed,
                    VocabularySize = metadata.VocabularySize,
                    SynapseCount = metadata.SynapseCount,
                    TrainingHours = metadata.TrainingHours,
                    AverageTrainingRate = metadata.AverageTrainingRate,
                    MemoryUsageGB = metadata.MemoryUsageGB,
                    
                    Vocabulary = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        await File.ReadAllTextAsync(Path.Combine(checkpointDir, "vocabulary.json"))
                    )!,
                    
                    Synapses = JsonSerializer.Deserialize<List<object>>(
                        await File.ReadAllTextAsync(Path.Combine(checkpointDir, "synapses.json"))
                    )!,
                    
                    Clusters = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        await File.ReadAllTextAsync(Path.Combine(checkpointDir, "clusters.json"))
                    )!
                };
                
                Console.WriteLine($"✅ Loaded checkpoint: {metadata.Timestamp:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"   Sentences: {metadata.SentencesProcessed:N0}");
                Console.WriteLine($"   Vocabulary: {metadata.VocabularySize:N0}");
                Console.WriteLine($"   Training time: {metadata.TrainingHours:F1} hours");
                
                return checkpoint;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to load checkpoint: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// List all available checkpoints
        /// </summary>
        public List<CheckpointInfo> ListCheckpoints()
        {
            var checkpoints = new List<CheckpointInfo>();
            
            foreach (var dir in Directory.GetDirectories(_checkpointPath)
                .OrderByDescending(d => d))
            {
                var metadataPath = Path.Combine(dir, "metadata.json");
                if (File.Exists(metadataPath))
                {
                    var metadata = JsonSerializer.Deserialize<CheckpointMetadata>(
                        File.ReadAllText(metadataPath)
                    );
                    
                    checkpoints.Add(new CheckpointInfo
                    {
                        Path = dir,
                        Timestamp = metadata!.Timestamp,
                        SentencesProcessed = metadata.SentencesProcessed,
                        VocabularySize = metadata.VocabularySize,
                        TrainingHours = metadata.TrainingHours
                    });
                }
            }
            
            return checkpoints;
        }
        
        private async Task CleanupOldCheckpointsAsync()
        {
            var checkpoints = Directory.GetDirectories(_checkpointPath)
                .Where(d => !d.EndsWith("latest.txt"))
                .OrderByDescending(d => d)
                .ToList();
            
            if (checkpoints.Count <= _maxCheckpoints)
                return;
            
            var toDelete = checkpoints.Skip(_maxCheckpoints).ToList();
            
            foreach (var dir in toDelete)
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Failed to delete old checkpoint {dir}: {ex.Message}");
                }
            }
            
            if (toDelete.Count > 0)
            {
                Console.WriteLine($"🧹 Cleaned up {toDelete.Count} old checkpoints");
            }
        }
        
        #endregion
        
        #region Episodic Memory
        
        /// <summary>
        /// Get path for episodic memory storage
        /// </summary>
        public string GetEpisodicMemoryPath()
        {
            return _episodicPath;
        }
        
        /// <summary>
        /// Append episodic event (append-only log)
        /// </summary>
        public async Task AppendEpisodicEventAsync(EpisodicEvent episodicEvent)
        {
            var logPath = Path.Combine(_episodicPath, "episodes.jsonl");
            var json = JsonSerializer.Serialize(episodicEvent);
            await File.AppendAllTextAsync(logPath, json + "\n");
        }
        
        #endregion
        
        #region Metrics
        
        /// <summary>
        /// Log performance metric
        /// </summary>
        public async Task LogMetricAsync(string metricName, double value, DateTime? timestamp = null)
        {
            var ts = timestamp ?? DateTime.Now;
            var csvPath = Path.Combine(_metricsPath, $"{metricName}.csv");
            
            // Create header if new file
            if (!File.Exists(csvPath))
            {
                await File.WriteAllTextAsync(csvPath, "timestamp,value\n");
            }
            
            await File.AppendAllTextAsync(csvPath, $"{ts:yyyy-MM-dd HH:mm:ss},{value}\n");
        }
        
        #endregion
        
        #region NAS Archival
        
        /// <summary>
        /// Archive current state to NAS
        /// </summary>
        public async Task<bool> ArchiveToNASAsync()
        {
            if (!_enableNASArchive)
            {
                Console.WriteLine("ℹ️  NAS archival disabled");
                return false;
            }
            
            try
            {
                var archiveDate = DateTime.Now.ToString("yyyy-MM-dd");
                var archiveDir = Path.Combine(_archivePath, "archives", archiveDate);
                
                Console.WriteLine($"📦 Archiving to NAS: {archiveDir}");
                
                // Create archive directory
                Directory.CreateDirectory(archiveDir);
                
                // Copy latest checkpoint
                var latestPath = Path.Combine(_checkpointPath, "latest.txt");
                if (File.Exists(latestPath))
                {
                    var latestCheckpoint = (await File.ReadAllTextAsync(latestPath)).Trim();
                    await CopyDirectoryAsync(latestCheckpoint, archiveDir);
                }
                
                // Copy live state
                var liveDir = Path.Combine(_activeBrainPath, "live");
                var archiveLiveDir = Path.Combine(archiveDir, "live");
                await CopyDirectoryAsync(liveDir, archiveLiveDir);
                
                // Copy metrics
                var metricsArchiveDir = Path.Combine(archiveDir, "metrics");
                await CopyDirectoryAsync(_metricsPath, metricsArchiveDir);
                
                Console.WriteLine($"✅ Archived to NAS: {archiveDir}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ NAS archive failed: {ex.Message}");
                return false;
            }
        }
        
        private async Task CopyDirectoryAsync(string sourceDir, string destDir)
        {
            if (!Directory.Exists(sourceDir))
                return;
                
            Directory.CreateDirectory(destDir);
            
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(destDir, fileName);
                File.Copy(file, destFile, overwrite: true);
            }
            
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                var destSubDir = Path.Combine(destDir, dirName);
                await CopyDirectoryAsync(dir, destSubDir);
            }
        }
        
        #endregion
        
        #region Migration from Old Demo Directories
        
        /// <summary>
        /// Migrate data from scattered demo directories to centralized storage
        /// </summary>
        public async Task MigrateFromDemoDirectoriesAsync()
        {
            Console.WriteLine("🔄 Migrating data from demo directories...");
            
            var demoDirectories = new[]
            {
                "./continuous_learning_demo",
                "./continuous_learning_week7",
                "./attention_showcase_memory",
                "./demo_episodic_memory",
                "./episodic_memory"
            };
            
            int filesMigrated = 0;
            
            foreach (var demoDir in demoDirectories)
            {
                if (!Directory.Exists(demoDir))
                    continue;
                    
                Console.WriteLine($"  Migrating from: {demoDir}");
                
                // Migrate checkpoints
                var checkpointDir = Path.Combine(demoDir, "checkpoints");
                if (Directory.Exists(checkpointDir))
                {
                    foreach (var checkpoint in Directory.GetDirectories(checkpointDir))
                    {
                        var destDir = Path.Combine(_checkpointPath, Path.GetFileName(checkpoint));
                        if (!Directory.Exists(destDir))
                        {
                            await CopyDirectoryAsync(checkpoint, destDir);
                            filesMigrated++;
                        }
                    }
                }
                
                // Migrate episodic memory
                var episodicFiles = Directory.GetFiles(demoDir, "episodes.jsonl", SearchOption.AllDirectories);
                foreach (var file in episodicFiles)
                {
                    var destFile = Path.Combine(_episodicPath, $"migrated_{Path.GetFileName(file)}");
                    if (!File.Exists(destFile))
                    {
                        File.Copy(file, destFile);
                        filesMigrated++;
                    }
                }
            }
            
            if (filesMigrated > 0)
            {
                Console.WriteLine($"✅ Migrated {filesMigrated} items from demo directories");
                Console.WriteLine("💡 You can now safely delete the old demo directories");
            }
            else
            {
                Console.WriteLine("ℹ️  No data found in demo directories");
            }
        }
        
        #endregion
    }
    
    #region Data Models
    
    public class BrainCheckpoint
    {
        public DateTime Timestamp { get; set; }
        public long SentencesProcessed { get; set; }
        public int VocabularySize { get; set; }
        public long SynapseCount { get; set; }
        public double TrainingHours { get; set; }
        public double AverageTrainingRate { get; set; }
        public double MemoryUsageGB { get; set; }
        
        public Dictionary<string, object> Vocabulary { get; set; } = new();
        public List<object> Synapses { get; set; } = new();
        public Dictionary<string, object> Clusters { get; set; } = new();
    }
    
    public class CheckpointMetadata
    {
        public DateTime Timestamp { get; set; }
        public long SentencesProcessed { get; set; }
        public int VocabularySize { get; set; }
        public long SynapseCount { get; set; }
        public double TrainingHours { get; set; }
        public double AverageTrainingRate { get; set; }
        public double MemoryUsageGB { get; set; }
        public string CheckpointPath { get; set; } = "";
    }
    
    public class CheckpointInfo
    {
        public string Path { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public long SentencesProcessed { get; set; }
        public int VocabularySize { get; set; }
        public double TrainingHours { get; set; }
    }
    
    public class EpisodicEvent
    {
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = "";
        public Dictionary<string, object> Context { get; set; } = new();
        public List<string> Participants { get; set; } = new();
        public double Importance { get; set; }
    }
    
    #endregion
}
