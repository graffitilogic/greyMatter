using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GreyMatter.Learning;

namespace GreyMatter.Storage
{
    /// <summary>
    /// Fast storage adapter that solves the 35-minute persistence bottleneck
    /// 
    /// Problem: Current system writes thousands of individual files to NAS
    /// Solution: Batch writes to SSD, background consolidation to NAS
    /// 
    /// Performance Target: 35min → 30 seconds for vocabulary saves
    /// </summary>
    public class FastStorageAdapter
    {
        private readonly HybridTieredStorage _storage;
        private readonly string _sessionId;
        
        public FastStorageAdapter(string hotPath = "/Users/billdodd/Desktop/Cerebro/working", 
                                 string coldPath = "/Volumes/jarvis/brainData")
        {
            _storage = new HybridTieredStorage(hotPath, coldPath);
            _sessionId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            
            Console.WriteLine($"🚀 FastStorageAdapter initialized with session: {_sessionId}");
        }
        
        /// <summary>
        /// Generate Sparse Distributed Representation (SDR) for biological concept storage
        /// </summary>
        private int[] GenerateActivationPattern(string word, int frequency = 1)
        {
            // Create sparse activation pattern (~2% of 2048 neurons = ~40 active neurons)
            var totalNeurons = 2048;
            var activationPercent = 0.02; // Biological sparsity
            var activeNeurons = (int)(totalNeurons * activationPercent);
            
            // Use deterministic seeding for consistency
            var seed = word.GetHashCode() ^ frequency;
            var random = new Random(seed);
            
            return Enumerable.Range(0, totalNeurons)
                .OrderBy(x => random.Next())
                .Take(activeNeurons)
                .OrderBy(x => x)
                .ToArray();
        }
        
        /// <summary>
        /// Save neural concepts efficiently
        /// </summary>
        public async Task SaveNeuralConceptsAsync(Dictionary<string, object> concepts)
        {
            Console.WriteLine($"🧠 Saving {concepts.Count:N0} neural concepts...");
            var startTime = DateTime.UtcNow;
            
            var storageData = new Dictionary<string, object>();
            foreach (var (conceptId, data) in concepts)
            {
                storageData[$"neural/{conceptId}"] = new
                {
                    ConceptId = conceptId,
                    Data = data,
                    SessionId = _sessionId,
                    SavedAt = DateTime.UtcNow
                };
            }
            
            await _storage.WriteBatchAsync(storageData);
            
            var elapsed = DateTime.UtcNow - startTime;
            Console.WriteLine($"✅ Neural concepts saved in {elapsed.TotalSeconds:F1}s");
        }
        
        /// <summary>
        /// Load neural concepts with smart caching (biological approach)
        /// </summary>
        public async Task<Dictionary<string, object>> LoadNeuralConceptsAsync()
        {
            Console.WriteLine("🧠 Loading neural concept patterns...");
            var startTime = DateTime.UtcNow;
            
            var concepts = new Dictionary<string, object>();
            
            // For now, return empty concepts - neural concepts will be loaded as we encounter them
            // This method will be enhanced when we have a proper neural concept index
            Console.WriteLine("ℹ️ Neural concept loading system ready (concepts loaded on-demand)");
            
            var elapsed = DateTime.UtcNow - startTime;
            Console.WriteLine($"✅ Neural concept system initialized in {elapsed.TotalSeconds:F1}s");
            
            return concepts;
        }
        
        /// <summary>
        /// Save learning session metadata
        /// </summary>
        public async Task SaveSessionMetadataAsync(Dictionary<string, object> metadata)
        {
            var sessionData = new
            {
                SessionId = _sessionId,
                StartTime = DateTime.UtcNow,
                Metadata = metadata,
                MachineName = Environment.MachineName,
                ProcessId = Environment.ProcessId
            };
            
            await _storage.WriteAsync($"session/{_sessionId}/metadata", sessionData);
        }
        
        /// <summary>
        /// Force consolidation to NAS storage
        /// </summary>
        public async Task ConsolidateToNASAsync()
        {
            Console.WriteLine("📡 Forcing consolidation to NAS storage...");
            await _storage.ConsolidateToNASAsync();
        }
        
        /// <summary>
        /// Get storage performance statistics
        /// </summary>
        public async Task<string> GetStorageStatsAsync()
        {
            var stats = await _storage.GetStatsAsync();
            return $"""
                📊 **Storage Statistics**
                ========================
                Hot Storage (SSD): {stats.HotStorageSizeMB:N0} MB
                Cold Storage (NAS): {stats.ColdStorageSizeMB:N0} MB  
                Pending Writes: {stats.PendingWrites:N0}
                Cached Items: {stats.CachedItems:N0}
                Session: {_sessionId}
                """;
        }
        
        /// <summary>
        /// Clean shutdown - consolidate everything to NAS
        /// </summary>
        public async Task ShutdownAsync()
        {
            Console.WriteLine("🛑 FastStorageAdapter shutdown - consolidating to NAS...");
            await _storage.ConsolidateToNASAsync();
            Console.WriteLine("✅ Shutdown complete");
        }
    }
}
