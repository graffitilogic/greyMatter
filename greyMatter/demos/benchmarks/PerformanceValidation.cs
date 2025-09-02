using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Storage;

namespace greyMatter
{
    public class StoragePerformanceValidator
    {
        private readonly SemanticStorageManager _storageManager;
        private readonly Random _random = new Random(42); // Fixed seed for reproducible results

        public StoragePerformanceValidator()
        {
            var config = new GreyMatter.Core.CerebroConfiguration();
            config.ValidateAndSetup();
            _storageManager = new SemanticStorageManager(config.BrainDataPath);
        }

        public async Task RunComprehensivePerformanceTest()
        {
            Console.WriteLine("🧪 **COMPREHENSIVE STORAGE PERFORMANCE VALIDATION**");
            Console.WriteLine("==================================================");

            // Test 1: Small batch performance (baseline)
            await TestBatchSize(100, "Small Batch (100 concepts)");
            await TestBatchSize(500, "Medium Batch (500 concepts)");
            await TestBatchSize(1000, "Large Batch (1000 concepts)");
            await TestBatchSize(5000, "XL Batch (5000 concepts)");

            // Test 2: Memory usage analysis
            await TestMemoryUsage();

            // Test 3: Concurrent operations
            await TestConcurrentOperations();

            // Test 4: Network storage simulation
            await TestNetworkLatencySimulation();

            // Test 5: Real-world scenario simulation
            await TestRealWorldScenario();

            Console.WriteLine("\n✅ **PERFORMANCE VALIDATION COMPLETE**");
        }

        private async Task TestBatchSize(int conceptCount, string testName)
        {
            Console.WriteLine($"\n📊 **{testName}**");
            Console.WriteLine("".PadRight(50, '-'));

            // Generate test concepts
            var concepts = GenerateTestConcepts(conceptCount);

            // Note: Using existing storage - not clearing for performance test
            Console.WriteLine("📝 Note: Testing with existing storage data");

            // Measure storage performance
            var stopwatch = Stopwatch.StartNew();
            await _storageManager.SaveConceptsBatchAsync(concepts);
            stopwatch.Stop();

            var totalTime = stopwatch.ElapsedMilliseconds;
            var conceptsPerSecond = (double)conceptCount / (totalTime / 1000.0);

            Console.WriteLine($"⏱️  Total Time: {totalTime}ms");
            Console.WriteLine($"⚡ Concepts/Second: {conceptsPerSecond:F2}");
            Console.WriteLine($"🎯 Expected: >100 concepts/second (200x improvement target)");

            // Calculate improvement factor (baseline: 2 concepts/second)
            var improvementFactor = conceptsPerSecond / 2.0;
            Console.WriteLine($"📈 Improvement Factor: {improvementFactor:F1}x");

            if (improvementFactor >= 100)
                Console.WriteLine("✅ **TARGET ACHIEVED**: 100x+ improvement confirmed!");
            else if (improvementFactor >= 50)
                Console.WriteLine("🟡 **GOOD PROGRESS**: 50x+ improvement achieved");
            else
                Console.WriteLine("🔴 **BELOW TARGET**: Further optimization needed");
        }

        private async Task TestMemoryUsage()
        {
            Console.WriteLine("\n🧠 **MEMORY USAGE ANALYSIS**");
            Console.WriteLine("".PadRight(30, '-'));

            var initialMemory = GC.GetTotalMemory(true);
            Console.WriteLine($"📊 Initial Memory: {initialMemory / 1024 / 1024}MB");

            // Test with 10K concepts
            var concepts = GenerateTestConcepts(10000);
            await _storageManager.SaveConceptsBatchAsync(concepts);

            var finalMemory = GC.GetTotalMemory(true);
            var memoryUsed = finalMemory - initialMemory;
            Console.WriteLine($"📊 Final Memory: {finalMemory / 1024 / 1024}MB");
            Console.WriteLine($"📈 Memory Used: {memoryUsed / 1024 / 1024}MB");
            Console.WriteLine($"📈 Memory per Concept: {memoryUsed / 10000} bytes");
        }

        private async Task TestConcurrentOperations()
        {
            Console.WriteLine("\n🔄 **CONCURRENT OPERATIONS TEST**");
            Console.WriteLine("".PadRight(35, '-'));

            var tasks = new List<Task>();
            var stopwatch = Stopwatch.StartNew();

            // Launch 10 concurrent batches of 100 concepts each
            for (int i = 0; i < 10; i++)
            {
                var concepts = GenerateTestConcepts(100);
                tasks.Add(_storageManager.SaveConceptsBatchAsync(concepts));
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            var totalTime = stopwatch.ElapsedMilliseconds;
            var totalConcepts = 1000;
            var conceptsPerSecond = (double)totalConcepts / (totalTime / 1000.0);

            Console.WriteLine($"⏱️  Concurrent Time: {totalTime}ms");
            Console.WriteLine($"⚡ Concurrent Throughput: {conceptsPerSecond:F2} concepts/second");
        }

        private async Task TestNetworkLatencySimulation()
        {
            Console.WriteLine("\n🌐 **NETWORK LATENCY SIMULATION**");
            Console.WriteLine("".PadRight(35, '-'));
            Console.WriteLine("📝 Note: Testing with network storage environment");

            // Test with smaller batches to simulate network conditions
            var concepts = GenerateTestConcepts(500);
            var stopwatch = Stopwatch.StartNew();

            await _storageManager.SaveConceptsBatchAsync(concepts);

            stopwatch.Stop();
            var totalTime = stopwatch.ElapsedMilliseconds;
            var conceptsPerSecond = (double)500 / (totalTime / 1000.0);

            Console.WriteLine($"⏱️  Network Storage Time: {totalTime}ms");
            Console.WriteLine($"⚡ Network Throughput: {conceptsPerSecond:F2} concepts/second");

            if (conceptsPerSecond >= 50)
                Console.WriteLine("✅ **NETWORK OPTIMIZED**: Good performance on network storage");
            else
                Console.WriteLine("⚠️  **NETWORK CONSIDERATIONS**: May need further optimization");
        }

        private async Task TestRealWorldScenario()
        {
            Console.WriteLine("\n🌍 **REAL-WORLD SCENARIO SIMULATION**");
            Console.WriteLine("".PadRight(40, '-'));
            Console.WriteLine("📝 Simulating: Learning session with mixed concept types");

            // Simulate a realistic learning session
            var scenarios = new[]
            {
                ("Vocabulary Learning", 200),
                ("Semantic Relationships", 150),
                ("Grammar Patterns", 100),
                ("Context Examples", 250)
            };

            var totalConcepts = 0;
            var totalTime = 0L;

            foreach (var (name, count) in scenarios)
            {
                var concepts = GenerateTestConcepts(count);
                var stopwatch = Stopwatch.StartNew();

                await _storageManager.SaveConceptsBatchAsync(concepts);

                stopwatch.Stop();
                totalTime += stopwatch.ElapsedMilliseconds;
                totalConcepts += count;

                Console.WriteLine($"📚 {name}: {count} concepts in {stopwatch.ElapsedMilliseconds}ms");
            }

            var overallThroughput = (double)totalConcepts / (totalTime / 1000.0);
            Console.WriteLine($"🎯 **OVERALL SESSION**: {totalConcepts} concepts in {totalTime}ms");
            Console.WriteLine($"⚡ **SESSION THROUGHPUT**: {overallThroughput:F2} concepts/second");
        }

        private Dictionary<string, (object Data, GreyMatter.Storage.ConceptType Type)> GenerateTestConcepts(int count)
        {
            var concepts = new Dictionary<string, (object Data, GreyMatter.Storage.ConceptType Type)>();

            for (int i = 0; i < count; i++)
            {
                var conceptId = $"test_concept_{i}";
                var conceptData = new
                {
                    id = conceptId,
                    word = $"word_{i}",
                    semantic_domain = GetRandomDomain(),
                    frequency = _random.Next(1, 1000),
                    associations = GenerateAssociations(3),
                    context_examples = GenerateExamples(2),
                    learned_patterns = GeneratePatterns(2),
                    timestamp = DateTime.UtcNow
                };

                concepts[conceptId] = (conceptData, GreyMatter.Storage.ConceptType.SemanticRelation);
            }

            return concepts;
        }

        private string GetRandomDomain()
        {
            var domains = new[] { "nouns", "verbs", "adjectives", "grammar", "semantics", "syntax" };
            return domains[_random.Next(domains.Length)];
        }

        private List<string> GenerateAssociations(int count)
        {
            var associations = new List<string>();
            for (int i = 0; i < count; i++)
            {
                associations.Add($"associated_word_{_random.Next(1000)}");
            }
            return associations;
        }

        private List<string> GenerateExamples(int count)
        {
            var examples = new List<string>();
            for (int i = 0; i < count; i++)
            {
                examples.Add($"Example sentence {_random.Next(1000)} with context.");
            }
            return examples;
        }

        private List<string> GeneratePatterns(int count)
        {
            var patterns = new List<string>();
            for (int i = 0; i < count; i++)
            {
                patterns.Add($"Pattern_{_random.Next(100)}");
            }
            return patterns;
        }
    }

    public class Program
    {
        // public static async Task Main(string[] args)
        public static async Task RunPerformanceValidation(string[] args)
        {
            try
            {
                var validator = new StoragePerformanceValidator();
                await validator.RunComprehensivePerformanceTest();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ **PERFORMANCE TEST FAILED**: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
    }
}
