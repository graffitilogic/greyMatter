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
        /// Save vocabulary data (replaces 35-minute SemanticStorageManager.StoreVocabularyAsync)
        /// </summary>
        public async Task SaveVocabularyAsync(Dictionary<string, TatoebaDataConverter.WordData> vocabulary)
        {
            Console.WriteLine($"💾 Saving {vocabulary.Count:N0} vocabulary entries...");
            var startTime = DateTime.UtcNow;
            
            // Convert to storage format
            var storageData = new Dictionary<string, object>();
            foreach (var (word, data) in vocabulary)
            {
                storageData[$"vocab/{word}"] = new
                {
                    Word = word,
                    Frequency = data.Frequency,
                    Contexts = data.SentenceContexts?.Take(10).ToList() ?? new List<string>(), // Limit context size
                    CooccurringWords = data.CooccurringWords?.Take(20).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, int>(),
                    LastUpdated = DateTime.UtcNow
                };
            }
            
            // Batch write to SSD (fast)
            await _storage.WriteBatchAsync(storageData);
            
            // Create vocabulary index for quick lookup
            var vocabularyIndex = vocabulary.Keys.ToDictionary(word => word, word => $"vocab/{word}");
            await _storage.WriteAsync($"session/{_sessionId}/vocabulary_index", vocabularyIndex);
            
            var elapsed = DateTime.UtcNow - startTime;
            Console.WriteLine($"✅ Vocabulary saved in {elapsed.TotalSeconds:F1}s (was 35+ minutes)");
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
        /// Load vocabulary data with smart caching
        /// </summary>
        public async Task<Dictionary<string, TatoebaDataConverter.WordData>> LoadVocabularyAsync()
        {
            Console.WriteLine("📚 Loading vocabulary data...");
            var startTime = DateTime.UtcNow;
            
            // Try to load vocabulary index first
            var vocabularyIndex = await _storage.ReadAsync<Dictionary<string, string>>($"session/{_sessionId}/vocabulary_index");
            if (vocabularyIndex == null)
            {
                Console.WriteLine("ℹ️ No vocabulary index found, creating empty vocabulary");
                return new Dictionary<string, TatoebaDataConverter.WordData>();
            }
            
            var vocabulary = new Dictionary<string, TatoebaDataConverter.WordData>();
            var loadTasks = new List<Task<(string Word, TatoebaDataConverter.WordData? Data)>>();
            
            foreach (var (word, storageKey) in vocabularyIndex.Take(5000)) // Limit for performance
            {
                loadTasks.Add(LoadSingleVocabularyItem(word, storageKey));
            }
            
            var results = await Task.WhenAll(loadTasks);
            
            foreach (var (word, data) in results)
            {
                if (data != null)
                {
                    vocabulary[word] = data;
                }
            }
            
            var elapsed = DateTime.UtcNow - startTime;
            Console.WriteLine($"✅ Loaded {vocabulary.Count:N0} vocabulary entries in {elapsed.TotalSeconds:F1}s");
            
            return vocabulary;
        }
        
        /// <summary>
        /// Load a single vocabulary item with error handling
        /// </summary>
        private async Task<(string Word, TatoebaDataConverter.WordData? Data)> LoadSingleVocabularyItem(string word, string storageKey)
        {
            try
            {
                var stored = await _storage.ReadAsync<dynamic>(storageKey);
                if (stored == null) return (word, null);
                
                // Convert from storage format
                var element = (JsonElement)stored;
                var data = new TatoebaDataConverter.WordData
                {
                    Word = element.GetProperty("Word").GetString() ?? word,
                    Frequency = element.TryGetProperty("Frequency", out var freq) ? freq.GetInt32() : 1,
                    SentenceContexts = element.TryGetProperty("Contexts", out var contexts) 
                        ? contexts.EnumerateArray().Select(c => c.GetString() ?? "").ToList()
                        : new List<string>(),
                    CooccurringWords = element.TryGetProperty("CooccurringWords", out var cooccur)
                        ? cooccur.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetInt32())
                        : new Dictionary<string, int>()
                };
                
                return (word, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to load vocabulary item '{word}': {ex.Message}");
                return (word, null);
            }
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
