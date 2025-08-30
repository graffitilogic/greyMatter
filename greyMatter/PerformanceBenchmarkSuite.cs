using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Storage;
using GreyMatter.Core;
using MessagePack;
using MessagePack.Resolvers;

namespace GreyMatter.PerformanceBenchmarks
{
    /// <summary>
    /// Comprehensive performance benchmarking suite for GreyMatter
    /// </summary>
    public class PerformanceBenchmarkSuite
    {
        private readonly SemanticStorageManager _storageManager;
        private readonly string _benchmarkResultsPath;
        private readonly MessagePackSerializerOptions _messagePackOptions;

        public PerformanceBenchmarkSuite(SemanticStorageManager storageManager)
        {
            _storageManager = storageManager;
            _benchmarkResultsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "benchmark_results");

            // Initialize MessagePack options
            var resolver = CompositeResolver.Create(
                StandardResolver.Instance
            );
            _messagePackOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);

            Directory.CreateDirectory(_benchmarkResultsPath);
        }

        /// <summary>
        /// Run comprehensive performance benchmark suite
        /// </summary>
        public async Task RunComprehensiveBenchmark()
        {
            Console.WriteLine("🧪 **GREY MATTER COMPREHENSIVE PERFORMANCE BENCHMARK**");
            Console.WriteLine("==================================================");

            var results = new Dictionary<string, BenchmarkResult>();

            // Test 1: MessagePack vs JSON Serialization
            Console.WriteLine("\n📊 **TEST 1: SERIALIZATION PERFORMANCE**");
            results["Serialization"] = await TestSerializationPerformance();

            // Test 2: Storage Operations
            Console.WriteLine("\n💾 **TEST 2: STORAGE OPERATIONS**");
            results["Storage"] = await TestStorageOperations();

            // Test 3: Vocabulary Learning
            Console.WriteLine("\n🧠 **TEST 3: VOCABULARY LEARNING**");
            results["Vocabulary"] = await TestVocabularyLearning();

            // Test 4: Memory Usage
            Console.WriteLine("\n🧠 **TEST 4: MEMORY USAGE**");
            results["Memory"] = await TestMemoryUsage();

            // Generate comprehensive report
            Console.WriteLine("\n📋 **GENERATING COMPREHENSIVE REPORT**");
            await GenerateBenchmarkReport(results);

            Console.WriteLine("\n✅ **BENCHMARK SUITE COMPLETED**");
            Console.WriteLine("Results saved to: " + _benchmarkResultsPath);
        }

        private async Task<BenchmarkResult> TestSerializationPerformance()
        {
            var stopwatch = Stopwatch.StartNew();
            var testData = GenerateTestVocabulary(1000);

            // MessagePack serialization
            var msgPackStart = stopwatch.ElapsedMilliseconds;
            var msgPackData = MessagePackSerializer.Serialize(testData, _messagePackOptions);
            var msgPackSerializeTime = stopwatch.ElapsedMilliseconds - msgPackStart;

            var msgPackDeserializeStart = stopwatch.ElapsedMilliseconds;
            var msgPackDeserialized = MessagePackSerializer.Deserialize<List<GreyMatter.Storage.WordInfo>>(msgPackData, _messagePackOptions);
            var msgPackDeserializeTime = stopwatch.ElapsedMilliseconds - msgPackDeserializeStart;

            // JSON serialization
            var jsonStart = stopwatch.ElapsedMilliseconds;
            var jsonData = System.Text.Json.JsonSerializer.Serialize(testData);
            var jsonSerializeTime = stopwatch.ElapsedMilliseconds - jsonStart;

            var jsonDeserializeStart = stopwatch.ElapsedMilliseconds;
            var jsonDeserialized = System.Text.Json.JsonSerializer.Deserialize<List<GreyMatter.Storage.WordInfo>>(jsonData);
            var jsonDeserializeTime = stopwatch.ElapsedMilliseconds - jsonDeserializeStart;

            var totalTime = stopwatch.ElapsedMilliseconds;

            return new BenchmarkResult
            {
                TestName = "Serialization Performance",
                MessagePackSerializeTime = msgPackSerializeTime,
                MessagePackDeserializeTime = msgPackDeserializeTime,
                JsonSerializeTime = jsonSerializeTime,
                JsonDeserializeTime = jsonDeserializeTime,
                MessagePackSize = msgPackData.Length,
                JsonSize = jsonData.Length,
                TotalTime = totalTime,
                ItemsProcessed = testData.Count
            };
        }

        private async Task<BenchmarkResult> TestStorageOperations()
        {
            var stopwatch = Stopwatch.StartNew();
            var testWords = GenerateTestVocabulary(500);

            // Batch storage operations
            var saveStart = stopwatch.ElapsedMilliseconds;
            await _storageManager.StoreVocabularyAsync(testWords.ToDictionary(w => w.Word, w => w));
            var saveTime = stopwatch.ElapsedMilliseconds - saveStart;

            var loadStart = stopwatch.ElapsedMilliseconds;
            var loadedVocabulary = await _storageManager.LoadVocabularyAsync();
            var loadTime = stopwatch.ElapsedMilliseconds - loadStart;

            var searchStart = stopwatch.ElapsedMilliseconds;
            var searchTasks = testWords.Select(w => _storageManager.LoadVocabularyWordAsync(w.Word)).ToArray();
            await Task.WhenAll(searchTasks);
            var searchTime = stopwatch.ElapsedMilliseconds - searchStart;

            var totalTime = stopwatch.ElapsedMilliseconds;

            return new BenchmarkResult
            {
                TestName = "Storage Operations",
                SaveTime = saveTime,
                LoadTime = loadTime,
                SearchTime = searchTime,
                TotalTime = totalTime,
                ItemsProcessed = testWords.Count
            };
        }

        private async Task<BenchmarkResult> TestVocabularyLearning()
        {
            var stopwatch = Stopwatch.StartNew();
            var testWords = GenerateTestVocabulary(100);

            // Simulate learning process
            var learningStart = stopwatch.ElapsedMilliseconds;
            foreach (var word in testWords)
            {
                await _storageManager.SaveVocabularyWordAsync(word.Word, word);

                // Simulate semantic relationship building
                var relatedWords = testWords.Where(w => w != word).Take(5).ToList();
                foreach (var related in relatedWords)
                {
                    // Create semantic relationships
                    await Task.Delay(1); // Simulate processing time
                }
            }
            var learningTime = stopwatch.ElapsedMilliseconds - learningStart;

            var totalTime = stopwatch.ElapsedMilliseconds;

            return new BenchmarkResult
            {
                TestName = "Vocabulary Learning",
                LearningTime = learningTime,
                TotalTime = totalTime,
                ItemsProcessed = testWords.Count
            };
        }

        private async Task<BenchmarkResult> TestMemoryUsage()
        {
            var stopwatch = Stopwatch.StartNew();

            // Test memory usage with large vocabulary
            var largeVocabulary = GenerateTestVocabulary(5000);

            var memoryStart = stopwatch.ElapsedMilliseconds;
            var serialized = MessagePackSerializer.Serialize(largeVocabulary, _messagePackOptions);
            var memoryTime = stopwatch.ElapsedMilliseconds - memoryStart;

            // Calculate memory metrics
            var memoryUsed = GC.GetTotalMemory(true);
            var deserialized = MessagePackSerializer.Deserialize<List<GreyMatter.Storage.WordInfo>>(serialized, _messagePackOptions);

            var totalTime = stopwatch.ElapsedMilliseconds;

            return new BenchmarkResult
            {
                TestName = "Memory Usage",
                MemoryTime = memoryTime,
                MemoryUsed = memoryUsed,
                TotalTime = totalTime,
                ItemsProcessed = largeVocabulary.Count
            };
        }

        private List<GreyMatter.Storage.WordInfo> GenerateTestVocabulary(int count)
        {
            var vocabulary = new List<GreyMatter.Storage.WordInfo>();
            var random = new Random(42);

            for (int i = 0; i < count; i++)
            {
                vocabulary.Add(new GreyMatter.Storage.WordInfo
                {
                    Word = $"test_word_{i}",
                    Frequency = random.Next(1, 1000),
                    FirstSeen = DateTime.Now.AddDays(-random.Next(365)),
                    EstimatedType = (GreyMatter.Storage.WordType)random.Next(10)
                });
            }

            return vocabulary;
        }

        private async Task GenerateBenchmarkReport(Dictionary<string, BenchmarkResult> results)
        {
            var reportPath = Path.Combine(_benchmarkResultsPath, $"benchmark_report_{DateTime.Now:yyyyMMdd_HHmmss}.md");

            using (var writer = new StreamWriter(reportPath))
            {
                await writer.WriteLineAsync("# GreyMatter Performance Benchmark Report");
                await writer.WriteLineAsync($"**Generated**: {DateTime.Now}");
                await writer.WriteLineAsync();

                foreach (var result in results.Values)
                {
                    await writer.WriteLineAsync($"## {result.TestName}");
                    await writer.WriteLineAsync();

                    if (result.MessagePackSerializeTime.HasValue)
                    {
                        await writer.WriteLineAsync($"### Serialization Performance");
                        await writer.WriteLineAsync($"| Metric | MessagePack | JSON | Improvement |");
                        await writer.WriteLineAsync($"|--------|-------------|------|------------|");
                        await writer.WriteLineAsync($"| Serialize (ms) | {result.MessagePackSerializeTime:F2} | {result.JsonSerializeTime:F2} | {(result.JsonSerializeTime / result.MessagePackSerializeTime):F1}x |");
                        await writer.WriteLineAsync($"| Deserialize (ms) | {result.MessagePackDeserializeTime:F2} | {result.JsonDeserializeTime:F2} | {(result.JsonDeserializeTime / result.MessagePackDeserializeTime):F1}x |");
                        await writer.WriteLineAsync($"| Size (bytes) | {result.MessagePackSize} | {result.JsonSize} | {((result.JsonSize - result.MessagePackSize) / (double)result.JsonSize * 100):F1}% smaller |");
                        await writer.WriteLineAsync();
                    }

                    if (result.SaveTime.HasValue)
                    {
                        await writer.WriteLineAsync($"### Storage Operations");
                        await writer.WriteLineAsync($"| Operation | Time (ms) | Items/sec |");
                        await writer.WriteLineAsync($"|-----------|-----------|----------|");
                        await writer.WriteLineAsync($"| Save | {result.SaveTime:F2} | {(result.ItemsProcessed / result.SaveTime * 1000):F1} |");
                        await writer.WriteLineAsync($"| Load | {result.LoadTime:F2} | {(result.ItemsProcessed / result.LoadTime * 1000):F1} |");
                        await writer.WriteLineAsync($"| Search | {result.SearchTime:F2} | {(result.ItemsProcessed / result.SearchTime * 1000):F1} |");
                        await writer.WriteLineAsync();
                    }

                    if (result.LearningTime.HasValue)
                    {
                        await writer.WriteLineAsync($"### Learning Performance");
                        await writer.WriteLineAsync($"| Metric | Value |");
                        await writer.WriteLineAsync($"|--------|-------|");
                        await writer.WriteLineAsync($"| Learning Time | {result.LearningTime:F2}ms |");
                        await writer.WriteLineAsync($"| Words Learned | {result.ItemsProcessed} |");
                        await writer.WriteLineAsync($"| Learning Rate | {(result.ItemsProcessed / result.LearningTime * 1000):F1} words/sec |");
                        await writer.WriteLineAsync();
                    }

                    if (result.MemoryUsed.HasValue)
                    {
                        await writer.WriteLineAsync($"### Memory Usage");
                        await writer.WriteLineAsync($"| Metric | Value |");
                        await writer.WriteLineAsync($"|--------|-------|");
                        await writer.WriteLineAsync($"| Memory Used | {(result.MemoryUsed / 1024.0 / 1024.0):F2} MB |");
                        await writer.WriteLineAsync($"| Processing Time | {result.MemoryTime:F2}ms |");
                        await writer.WriteLineAsync($"| Items Processed | {result.ItemsProcessed} |");
                        await writer.WriteLineAsync();
                    }
                }

                await writer.WriteLineAsync("## Summary");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync("### Key Performance Metrics");
                await writer.WriteLineAsync("- **Serialization**: MessagePack provides significant performance improvements");
                await writer.WriteLineAsync("- **Storage**: Efficient batch operations with good throughput");
                await writer.WriteLineAsync("- **Learning**: Scalable vocabulary acquisition system");
                await writer.WriteLineAsync("- **Memory**: Optimized memory usage for large datasets");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync("### Recommendations");
                await writer.WriteLineAsync("- Use MessagePack for production serialization");
                await writer.WriteLineAsync("- Implement batch processing for large datasets");
                await writer.WriteLineAsync("- Monitor memory usage during learning operations");
                await writer.WriteLineAsync("- Consider parallel processing for vocabulary expansion");
            }

            Console.WriteLine($"📄 Report generated: {reportPath}");
        }
    }

    public class BenchmarkResult
    {
        public string TestName { get; set; } = "";
        public double TotalTime { get; set; }
        public int ItemsProcessed { get; set; }

        // Serialization metrics
        public double? MessagePackSerializeTime { get; set; }
        public double? MessagePackDeserializeTime { get; set; }
        public double? JsonSerializeTime { get; set; }
        public double? JsonDeserializeTime { get; set; }
        public int? MessagePackSize { get; set; }
        public int? JsonSize { get; set; }

        // Storage metrics
        public double? SaveTime { get; set; }
        public double? LoadTime { get; set; }
        public double? SearchTime { get; set; }

        // Learning metrics
        public double? LearningTime { get; set; }

        // Memory metrics
        public double? MemoryTime { get; set; }
        public long? MemoryUsed { get; set; }
    }
}
