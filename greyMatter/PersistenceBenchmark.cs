using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;
using GreyMatter.Storage;
using greyMatter.Core;

namespace GreyMatter
{
    /// <summary>
    /// Comprehensive performance benchmarking for persistence operations
    /// </summary>
    public class PersistenceBenchmark
    {
        private readonly SemanticStorageManager _storageManager;
        private readonly string _benchmarkResultsPath;

        public PersistenceBenchmark(SemanticStorageManager storageManager)
        {
            _storageManager = storageManager;
            _benchmarkResultsPath = "/Volumes/jarvis/brainData/benchmark_results.json";
        }

        public async Task RunComprehensiveBenchmarkAsync()
        {
            Console.WriteLine("🏃 **PERSISTENCE PERFORMANCE BENCHMARK**");
            Console.WriteLine("======================================");

            var results = new Dictionary<string, BenchmarkResult>();

            // 1. Vocabulary Index Operations
            results["VocabularyIndex_Save"] = await BenchmarkVocabularyIndexSaveAsync();
            results["VocabularyIndex_Load"] = await BenchmarkVocabularyIndexLoadAsync();

            // 2. Concept Index Operations
            results["ConceptIndex_Save"] = await BenchmarkConceptIndexSaveAsync();
            results["ConceptIndex_Load"] = await BenchmarkConceptIndexLoadAsync();

            // 3. Word Storage Operations
            results["WordStorage_Batch"] = await BenchmarkWordStorageBatchAsync();
            results["WordStorage_Individual"] = await BenchmarkWordStorageIndividualAsync();

            // 4. Concept Storage Operations
            results["ConceptStorage_Batch"] = await BenchmarkConceptStorageBatchAsync();

            // 5. File System Operations
            results["FileSystem_ReadWrite"] = await BenchmarkFileSystemOperationsAsync();

            // 6. Memory Usage Analysis
            results["Memory_Usage"] = BenchmarkMemoryUsageAsync();

            // Save results
            await SaveBenchmarkResultsAsync(results);

            // Display results
            DisplayBenchmarkResults(results);

            // Generate recommendations
            GenerateOptimizationRecommendations(results);
        }

        private async Task<BenchmarkResult> BenchmarkVocabularyIndexSaveAsync()
        {
            Console.WriteLine("\n📝 Benchmarking Vocabulary Index Save...");

            var stopwatch = Stopwatch.StartNew();

            // Create a large vocabulary index for testing
            var testVocabulary = new Dictionary<string, string>();
            for (int i = 0; i < 10000; i++)
            {
                testVocabulary[$"word_{i}"] = $"/path/to/cluster_{i % 100}/word_{i}.json";
            }

            // Measure save operation
            var originalIndex = _storageManager.GetType()
                .GetField("_vocabularyIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_storageManager) as Dictionary<string, string>;

            if (originalIndex != null)
            {
                var backup = new Dictionary<string, string>(originalIndex);
                originalIndex.Clear();
                originalIndex.AddRange(testVocabulary);

                // Save operation
                var saveMethod = _storageManager.GetType()
                    .GetMethod("SaveVocabularyIndexAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                await (Task)saveMethod?.Invoke(_storageManager, null)!;

                // Restore original
                originalIndex.Clear();
                originalIndex.AddRange(backup);
            }

            stopwatch.Stop();

            return new BenchmarkResult
            {
                Operation = "VocabularyIndex_Save",
                DurationMs = stopwatch.ElapsedMilliseconds,
                ItemCount = testVocabulary.Count,
                DataSize = CalculateDictionarySize(testVocabulary)
            };
        }

        private async Task<BenchmarkResult> BenchmarkVocabularyIndexLoadAsync()
        {
            Console.WriteLine("📖 Benchmarking Vocabulary Index Load...");

            var stopwatch = Stopwatch.StartNew();

            // Load operation
            var loadMethod = _storageManager.GetType()
                .GetMethod("LoadVocabularyIndexAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)loadMethod?.Invoke(_storageManager, null)!;

            stopwatch.Stop();

            var vocabularyIndex = _storageManager.GetType()
                .GetField("_vocabularyIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_storageManager) as Dictionary<string, string>;

            return new BenchmarkResult
            {
                Operation = "VocabularyIndex_Load",
                DurationMs = stopwatch.ElapsedMilliseconds,
                ItemCount = vocabularyIndex?.Count ?? 0,
                DataSize = CalculateDictionarySize(vocabularyIndex)
            };
        }

        private async Task<BenchmarkResult> BenchmarkConceptIndexSaveAsync()
        {
            Console.WriteLine("💾 Benchmarking Concept Index Save...");

            var stopwatch = Stopwatch.StartNew();

            // Create test concept index
            var testConcepts = new Dictionary<string, ConceptIndexEntry>();
            for (int i = 0; i < 5000; i++)
            {
                testConcepts[$"concept_{i}"] = new ConceptIndexEntry
                {
                    ClusterPath = $"/path/to/concept_cluster_{i % 50}/concept_{i}.json",
                    ConceptType = ConceptType.SemanticRelation,
                    LastAccessed = DateTime.Now
                };
            }

            var originalIndex = _storageManager.GetType()
                .GetField("_conceptIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_storageManager) as Dictionary<string, ConceptIndexEntry>;

            if (originalIndex != null)
            {
                var backup = new Dictionary<string, ConceptIndexEntry>(originalIndex);
                originalIndex.Clear();
                originalIndex.AddRange(testConcepts);

                var saveMethod = _storageManager.GetType()
                    .GetMethod("SaveConceptIndexAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                await (Task)saveMethod?.Invoke(_storageManager, null)!;

                originalIndex.Clear();
                originalIndex.AddRange(backup);
            }

            stopwatch.Stop();

            return new BenchmarkResult
            {
                Operation = "ConceptIndex_Save",
                DurationMs = stopwatch.ElapsedMilliseconds,
                ItemCount = testConcepts.Count,
                DataSize = CalculateConceptIndexSize(testConcepts)
            };
        }

        private async Task<BenchmarkResult> BenchmarkConceptIndexLoadAsync()
        {
            Console.WriteLine("📂 Benchmarking Concept Index Load...");

            var stopwatch = Stopwatch.StartNew();

            var loadMethod = _storageManager.GetType()
                .GetMethod("LoadConceptIndexAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)loadMethod?.Invoke(_storageManager, null)!;

            stopwatch.Stop();

            var conceptIndex = _storageManager.GetType()
                .GetField("_conceptIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_storageManager) as Dictionary<string, ConceptIndexEntry>;

            return new BenchmarkResult
            {
                Operation = "ConceptIndex_Load",
                DurationMs = stopwatch.ElapsedMilliseconds,
                ItemCount = conceptIndex?.Count ?? 0,
                DataSize = CalculateConceptIndexSize(conceptIndex)
            };
        }

        private async Task<BenchmarkResult> BenchmarkWordStorageBatchAsync()
        {
            Console.WriteLine("📚 Benchmarking Batch Word Storage...");

            var stopwatch = Stopwatch.StartNew();

            // Create test words
            var testWords = new Dictionary<string, WordInfo>();
            for (int i = 0; i < 1000; i++)
            {
                testWords[$"test_word_{i}"] = new WordInfo
                {
                    Word = $"test_word_{i}",
                    Frequency = i,
                    FirstSeen = DateTime.Now,
                    EstimatedType = WordType.Noun,
                    AssociatedNeuronIds = new List<int> { i, i + 1 }
                };
            }

            await _storageManager.StoreVocabularyAsync(testWords);

            stopwatch.Stop();

            return new BenchmarkResult
            {
                Operation = "WordStorage_Batch",
                DurationMs = stopwatch.ElapsedMilliseconds,
                ItemCount = testWords.Count,
                DataSize = CalculateWordInfoSize(testWords)
            };
        }

        private async Task<BenchmarkResult> BenchmarkWordStorageIndividualAsync()
        {
            Console.WriteLine("🔤 Benchmarking Individual Word Storage...");

            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < 100; i++)
            {
                var wordInfo = new WordInfo
                {
                    Word = $"individual_word_{i}",
                    Frequency = i,
                    FirstSeen = DateTime.Now,
                    EstimatedType = WordType.Noun,
                    AssociatedNeuronIds = new List<int> { i }
                };

                await _storageManager.SaveVocabularyWordAsync($"individual_word_{i}", wordInfo);
            }

            stopwatch.Stop();

            return new BenchmarkResult
            {
                Operation = "WordStorage_Individual",
                DurationMs = stopwatch.ElapsedMilliseconds,
                ItemCount = 100,
                DataSize = 100 * 1024 // Rough estimate
            };
        }

        private async Task<BenchmarkResult> BenchmarkConceptStorageBatchAsync()
        {
            Console.WriteLine("🧠 Benchmarking Concept Storage...");

            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < 500; i++)
            {
                var conceptData = new
                {
                    Id = $"concept_{i}",
                    Type = "semantic_concept",
                    Features = new Dictionary<string, double>
                    {
                        ["feature1"] = i * 0.1,
                        ["feature2"] = i * 0.2,
                        ["feature3"] = i * 0.3
                    }
                };

                await _storageManager.SaveConceptAsync($"concept_{i}", conceptData, ConceptType.SemanticRelation);
            }

            stopwatch.Stop();

            return new BenchmarkResult
            {
                Operation = "ConceptStorage_Batch",
                DurationMs = stopwatch.ElapsedMilliseconds,
                ItemCount = 500,
                DataSize = 500 * 512 // Rough estimate
            };
        }

        private async Task<BenchmarkResult> BenchmarkFileSystemOperationsAsync()
        {
            Console.WriteLine("💿 Benchmarking File System Operations...");

            var stopwatch = Stopwatch.StartNew();

            var testPath = "/Volumes/jarvis/brainData/benchmark_test.json";
            var testData = new { TestData = "benchmark_test", Size = 1024 };

            // Write operation
            var json = System.Text.Json.JsonSerializer.Serialize(testData);
            await File.WriteAllTextAsync(testPath, json);

            // Read operation
            var readJson = await File.ReadAllTextAsync(testPath);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<object>(readJson);

            // Cleanup
            if (File.Exists(testPath))
                File.Delete(testPath);

            stopwatch.Stop();

            return new BenchmarkResult
            {
                Operation = "FileSystem_ReadWrite",
                DurationMs = stopwatch.ElapsedMilliseconds,
                ItemCount = 1,
                DataSize = json.Length
            };
        }

        private BenchmarkResult BenchmarkMemoryUsageAsync()
        {
            Console.WriteLine("🧠 Benchmarking Memory Usage...");

            var beforeMemory = GC.GetTotalMemory(false);

            // Create large data structures
            var largeVocabulary = new Dictionary<string, string>();
            for (int i = 0; i < 50000; i++)
            {
                largeVocabulary[$"memory_test_word_{i}"] = $"/very/long/path/to/cluster_{i % 1000}/memory_test_word_{i}.json";
            }

            var afterMemory = GC.GetTotalMemory(false);
            var memoryUsed = afterMemory - beforeMemory;

            // Force garbage collection
            GC.Collect();
            var afterGcMemory = GC.GetTotalMemory(true);

            return new BenchmarkResult
            {
                Operation = "Memory_Usage",
                DurationMs = 0, // Not a timing benchmark
                ItemCount = largeVocabulary.Count,
                DataSize = memoryUsed,
                AdditionalMetrics = new Dictionary<string, object>
                {
                    ["MemoryUsed"] = memoryUsed,
                    ["MemoryAfterGC"] = afterGcMemory,
                    ["MemoryEfficiency"] = (double)largeVocabulary.Count / memoryUsed * 1024 // Items per KB
                }
            };
        }

        private async Task SaveBenchmarkResultsAsync(Dictionary<string, BenchmarkResult> results)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(results, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_benchmarkResultsPath, json);
        }

        private void DisplayBenchmarkResults(Dictionary<string, BenchmarkResult> results)
        {
            Console.WriteLine("\n📊 **BENCHMARK RESULTS**");
            Console.WriteLine("======================");

            foreach (var result in results.OrderBy(r => r.Value.DurationMs))
            {
                var r = result.Value;
                Console.WriteLine($"{r.Operation,-25} | {r.DurationMs,6}ms | {r.ItemCount,5} items | {FormatBytes(r.DataSize),8}");
            }

            // Calculate totals
            var totalTime = results.Sum(r => r.Value.DurationMs);
            var totalItems = results.Sum(r => r.Value.ItemCount);
            var totalSize = results.Sum(r => r.Value.DataSize);

            Console.WriteLine("".PadRight(60, '-'));
            Console.WriteLine($"{"TOTALS",-25} | {totalTime,6}ms | {totalItems,5} items | {FormatBytes(totalSize),8}");
        }

        private void GenerateOptimizationRecommendations(Dictionary<string, BenchmarkResult> results)
        {
            Console.WriteLine("\n🎯 **OPTIMIZATION RECOMMENDATIONS**");
            Console.WriteLine("=================================");

            var slowestOperations = results.OrderByDescending(r => r.Value.DurationMs).Take(3);
            var fastestOperations = results.OrderBy(r => r.Value.DurationMs).Take(3);

            Console.WriteLine("🐌 Slowest Operations (Potential Bottlenecks):");
            foreach (var op in slowestOperations)
            {
                var r = op.Value;
                var itemsPerSecond = r.ItemCount / (r.DurationMs / 1000.0);
                Console.WriteLine($"  • {r.Operation}: {r.DurationMs}ms ({itemsPerSecond:F1} items/sec)");
            }

            Console.WriteLine("\n🚀 Fastest Operations (Good Performance):");
            foreach (var op in fastestOperations)
            {
                var r = op.Value;
                var itemsPerSecond = r.ItemCount / (r.DurationMs / 1000.0);
                Console.WriteLine($"  • {r.Operation}: {r.DurationMs}ms ({itemsPerSecond:F1} items/sec)");
            }

            // Specific recommendations
            Console.WriteLine("\n💡 Specific Recommendations:");

            var vocabSave = results.GetValueOrDefault("VocabularyIndex_Save");
            var vocabLoad = results.GetValueOrDefault("VocabularyIndex_Load");
            var batchWord = results.GetValueOrDefault("WordStorage_Batch");
            var individualWord = results.GetValueOrDefault("WordStorage_Individual");

            if (vocabSave != null && vocabLoad != null)
            {
                var saveLoadRatio = (double)vocabSave.DurationMs / vocabLoad.DurationMs;
                if (saveLoadRatio > 2.0)
                {
                    Console.WriteLine($"  • Vocabulary save is {saveLoadRatio:F1}x slower than load - consider async buffering");
                }
            }

            if (batchWord != null && individualWord != null)
            {
                var batchEfficiency = (double)individualWord.DurationMs / batchWord.DurationMs;
                if (batchEfficiency > 5.0)
                {
                    Console.WriteLine($"  • Batch operations are {batchEfficiency:F1}x faster - prioritize batching");
                }
            }

            var memoryResult = results.GetValueOrDefault("Memory_Usage");
            if (memoryResult?.AdditionalMetrics != null)
            {
                var efficiency = (double)memoryResult.AdditionalMetrics["MemoryEfficiency"];
                Console.WriteLine($"  • Memory efficiency: {efficiency:F1} items per KB");
                if (efficiency < 10)
                {
                    Console.WriteLine("  • Consider memory pooling for large dictionaries");
                }
            }
        }

        private long CalculateDictionarySize(Dictionary<string, string>? dict)
        {
            if (dict == null) return 0;
            return dict.Sum(kvp => kvp.Key.Length + kvp.Value.Length) * 2; // Rough UTF-16 estimate
        }

        private long CalculateConceptIndexSize(Dictionary<string, ConceptIndexEntry>? dict)
        {
            if (dict == null) return 0;
            return dict.Count * 256; // Rough estimate per entry
        }

        private long CalculateWordInfoSize(Dictionary<string, WordInfo> dict)
        {
            return dict.Count * 1024; // Rough estimate per WordInfo
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int suffixIndex = 0;
            double size = bytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:F1}{suffixes[suffixIndex]}";
        }

        public class BenchmarkResult
        {
            public string Operation { get; set; } = "";
            public long DurationMs { get; set; }
            public int ItemCount { get; set; }
            public long DataSize { get; set; }
            public Dictionary<string, object>? AdditionalMetrics { get; set; }
        }
    }

    public static class DictionaryExtensions
    {
        public static void AddRange<TKey, TValue>(this Dictionary<TKey, TValue> dict, IEnumerable<KeyValuePair<TKey, TValue>> items)
            where TKey : notnull
        {
            foreach (var item in items)
            {
                dict[item.Key] = item.Value;
            }
        }
    }
}
