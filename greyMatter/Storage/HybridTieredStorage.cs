using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace GreyMatter.Storage
{
    /// <summary>
    /// Hybrid tiered storage system to solve the 35-minute persistence bottleneck
    /// 
    /// Strategy:
    /// - Write immediately to fast SSD storage (/Users/billdodd/Desktop/Cerebro/)
    /// - Background consolidation to NAS (/Volumes/jarvis/) in batches
    /// - Aggressive caching to minimize NAS reads
    /// </summary>
    public class HybridTieredStorage
    {
        private readonly string _workingSetPath;
        private readonly string _coldStoragePath;
        private readonly ConcurrentDictionary<string, object> _writeCache;
        private readonly ConcurrentQueue<(string key, object data)> _pendingWrites;
        private readonly ConcurrentDictionary<string, DateTime> _lastAccessed;
        private readonly Timer _consolidationTimer;
        private readonly SemaphoreSlim _writeSemaphore;
        
        public HybridTieredStorage(string workingSetPath = "/Users/billdodd/Desktop/Cerebro/working", 
                                  string coldStoragePath = "/Volumes/jarvis/brainData")
        {
            _workingSetPath = workingSetPath;
            _coldStoragePath = coldStoragePath;
            _writeCache = new ConcurrentDictionary<string, object>();
            _pendingWrites = new ConcurrentQueue<(string, object)>();
            _lastAccessed = new ConcurrentDictionary<string, DateTime>();
            _writeSemaphore = new SemaphoreSlim(10, 10); // Limit concurrent writes
            
            // Ensure directories exist
            Directory.CreateDirectory(_workingSetPath);
            Directory.CreateDirectory(_coldStoragePath);
            
            // Start background consolidation (every 30 minutes)
            _consolidationTimer = new Timer(BackgroundConsolidationLoop, null, 
                TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
        }
        
        /// <summary>
        /// Write data immediately to fast storage
        /// </summary>
        public async Task WriteAsync<T>(string key, T data)
        {
            await _writeSemaphore.WaitAsync();
            try
            {
                // Cache in memory
                _writeCache[key] = data;
                _lastAccessed[key] = DateTime.UtcNow;
                
                // Write to fast storage immediately
                var filePath = Path.Combine(_workingSetPath, $"{key}.json");
                
                // Ensure directory exists
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var jsonData = JsonSerializer.Serialize(data);
                await File.WriteAllTextAsync(filePath, jsonData);
                
                // Queue for background NAS consolidation
                _pendingWrites.Enqueue((key, data));
            }
            finally
            {
                _writeSemaphore.Release();
            }
        }
        
        /// <summary>
        /// Read data with caching
        /// </summary>
        public async Task<T> ReadAsync<T>(string key)
        {
            // Check cache first
            if (_writeCache.TryGetValue(key, out var cached))
            {
                _lastAccessed[key] = DateTime.UtcNow;
                return (T)cached;
            }
            
            // Try fast storage
            var fastPath = Path.Combine(_workingSetPath, $"{key}.json");
            if (File.Exists(fastPath))
            {
                var content = await File.ReadAllTextAsync(fastPath);
                var data = JsonSerializer.Deserialize<T>(content);
                _writeCache[key] = data;
                _lastAccessed[key] = DateTime.UtcNow;
                return data;
            }
            
            // Fall back to cold storage
            var coldPath = Path.Combine(_coldStoragePath, $"{key}.json");
            if (File.Exists(coldPath))
            {
                var content = await File.ReadAllTextAsync(coldPath);
                var data = JsonSerializer.Deserialize<T>(content);
                _writeCache[key] = data;
                _lastAccessed[key] = DateTime.UtcNow;
                return data;
            }
            
            throw new FileNotFoundException($"Key '{key}' not found in storage");
        }
        
        /// <summary>
        /// Background consolidation to NAS storage
        /// </summary>
        private async void BackgroundConsolidationLoop(object? state)
        {
            try
            {
                await ConsolidateToNASAsync();
                await CleanupHotStorageAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Background consolidation error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Consolidate pending writes to NAS storage
        /// </summary>
        public async Task ConsolidateToNASAsync()
        {
            var batch = new List<(string key, object data)>();
            
            // Collect pending writes
            while (_pendingWrites.TryDequeue(out var item) && batch.Count < 1000)
            {
                batch.Add(item);
            }
            
            if (batch.Count == 0) return;
            
            Console.WriteLine($"Consolidating {batch.Count} items to NAS...");
            
            // Batch write to NAS
            var tasks = new List<Task>();
            foreach (var (key, data) in batch)
            {
                tasks.Add(WriteToNASAsync(key, data));
                
                // Throttle to avoid overwhelming NAS
                if (tasks.Count >= 50)
                {
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                    await Task.Delay(100); // Brief pause
                }
            }
            
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }
        
        /// <summary>
        /// Write individual item to NAS
        /// </summary>
        private async Task WriteToNASAsync(string key, object data)
        {
            try
            {
                var filePath = Path.Combine(_coldStoragePath, $"{key}.json");
                
                // Ensure directory exists
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var jsonData = JsonSerializer.Serialize(data);
                await File.WriteAllTextAsync(filePath, jsonData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write {key} to NAS: {ex.Message}");
                // Re-queue for retry
                _pendingWrites.Enqueue((key, data));
            }
        }
        
        /// <summary>
        /// Clean up old items from hot storage
        /// </summary>
        public Task CleanupHotStorageAsync()
        {
            var cutoff = DateTime.UtcNow.AddHours(-24); // Keep 24 hours in hot storage
            var toRemove = new List<string>();
            
            foreach (var kvp in _lastAccessed)
            {
                if (kvp.Value < cutoff)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in toRemove)
            {
                _lastAccessed.TryRemove(key, out _);
                _writeCache.TryRemove(key, out _);
                
                var filePath = Path.Combine(_workingSetPath, $"{key}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            
            Console.WriteLine($"Cleaned up {toRemove.Count} old items from hot storage");
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Get storage statistics
        /// </summary>
        public Task<HybridStorageStats> GetStorageStatsAsync()
        {
            var stats = new HybridStorageStats
            {
                HotStorageSizeMB = GetDirectorySizeMB(_workingSetPath),
                ColdStorageSizeMB = GetDirectorySizeMB(_coldStoragePath),
                PendingWrites = _pendingWrites.Count,
                CachedItems = _lastAccessed.Count,
                LastConsolidation = DateTime.UtcNow // Approximate
            };
            return Task.FromResult(stats);
        }
        
        /// <summary>
        /// Get directory size in MB
        /// </summary>
        private long GetDirectorySizeMB(string path)
        {
            if (!Directory.Exists(path)) return 0;
            
            var dirInfo = new DirectoryInfo(path);
            long size = 0;
            
            foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                size += file.Length;
            }
            
            return size / (1024 * 1024);
        }
        
        /// <summary>
        /// Shutdown with final consolidation
        /// </summary>
        public async Task ShutdownAsync()
        {
            _consolidationTimer?.Dispose();
            await ConsolidateToNASAsync();
            _writeSemaphore?.Dispose();
        }

        /// <summary>
        /// Batch write multiple items efficiently
        /// </summary>
        public async Task WriteBatchAsync(Dictionary<string, object> items)
        {
            var tasks = new List<Task>();
            foreach (var (key, data) in items)
            {
                tasks.Add(WriteAsync(key, data));
            }
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Get storage performance statistics
        /// </summary>
        public async Task<HybridStorageStats> GetStatsAsync()
        {
            return await Task.FromResult(new HybridStorageStats
            {
                HotStorageSizeMB = GetDirectorySizeMB(_workingSetPath),
                ColdStorageSizeMB = GetDirectorySizeMB(_coldStoragePath),
                PendingWrites = _pendingWrites.Count,
                CachedItems = _writeCache.Count,
                LastConsolidation = DateTime.UtcNow
            });
        }
    }
    
    /// <summary>
    /// Storage statistics for hybrid system
    /// </summary>
    public class HybridStorageStats
    {
        public long HotStorageSizeMB { get; set; }
        public long ColdStorageSizeMB { get; set; }
        public int PendingWrites { get; set; }
        public int CachedItems { get; set; }
        public DateTime LastConsolidation { get; set; }
        
        public override string ToString()
        {
            return $"Hot Storage: {HotStorageSizeMB} MB, Cold Storage: {ColdStorageSizeMB} MB, " +
                   $"Pending: {PendingWrites}, Cached: {CachedItems}, Last Consolidation: {LastConsolidation:yyyy-MM-dd HH:mm:ss}";
        }
    }
}
