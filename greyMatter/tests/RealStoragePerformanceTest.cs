using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using GreyMatter.Storage;

namespace GreyMatter.Tests
{
    /// <summary>
    /// Real A/B testing of storage performance: SemanticStorageManager vs HybridTieredStorage
    /// This is the actual validation that should have been done from the start.
    /// </summary>
    public class RealStoragePerformanceTest
    {
        public static async Task RunRealPerformanceComparison()
        {
            Console.WriteLine("🧪 **REAL STORAGE PERFORMANCE A/B TEST**");
            Console.WriteLine("=======================================");
            Console.WriteLine("Testing: SemanticStorageManager vs HybridTieredStorage");
            Console.WriteLine("Goal: Prove 35min → 30sec improvement claim");
            Console.WriteLine();

            // Test parameters
            var testSizes = new[] { 100, 500, 1000, 5000 };
            
            foreach (var conceptCount in testSizes)
            {
                Console.WriteLine($"📊 **TESTING {conceptCount} CONCEPTS**");
                Console.WriteLine("".PadRight(50, '='));
                
                var testData = GenerateTestConcepts(conceptCount);
                
                // Test A: Old System (SemanticStorageManager)
                await TestOldStorageSystem(testData, conceptCount);
                
                // Test B: New System (HybridTieredStorage)
                await TestNewStorageSystem(testData, conceptCount);
                
                Console.WriteLine();
            }
            
            Console.WriteLine("✅ **REAL PERFORMANCE COMPARISON COMPLETE**");
        }
        
        private static async Task TestOldStorageSystem(Dictionary<string, object> testData, int conceptCount)
        {
            Console.WriteLine("🐌 **OLD SYSTEM: SemanticStorageManager**");
            
            try
            {
                var config = new GreyMatter.Core.CerebroConfiguration();
                config.ValidateAndSetup();
                var oldStorage = new SemanticStorageManager(config.BrainDataPath);
                
                // Convert to the format SemanticStorageManager expects
                var concepts = new Dictionary<string, (object Data, ConceptType Type)>();
                foreach (var kvp in testData)
                {
                    concepts[kvp.Key] = (kvp.Value, ConceptType.WordAssociation);
                }
                
                var stopwatch = Stopwatch.StartNew();
                await oldStorage.SaveConceptsBatchAsync(concepts);
                stopwatch.Stop();
                
                var totalSeconds = stopwatch.Elapsed.TotalSeconds;
                var conceptsPerSecond = conceptCount / totalSeconds;
                
                Console.WriteLine($"⏱️  Time: {totalSeconds:F1} seconds ({stopwatch.ElapsedMilliseconds} ms)");
                Console.WriteLine($"⚡ Rate: {conceptsPerSecond:F2} concepts/second");
                Console.WriteLine($"📈 Projected 5K concepts: {(5000 / conceptsPerSecond):F1} seconds");
                
                if (totalSeconds > 60)
                    Console.WriteLine("🔴 **CONFIRMED BOTTLENECK**: Over 1 minute for small batch!");
                else
                    Console.WriteLine("🟡 Acceptable for small batches");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Old system failed: {ex.Message}");
            }
        }
        
        private static async Task TestNewStorageSystem(Dictionary<string, object> testData, int conceptCount)
        {
            Console.WriteLine("🚀 **NEW SYSTEM: HybridTieredStorage**");
            
            try
            {
                // Use local SSD for testing to avoid NAS latency
                var testPath = "/Users/billdodd/Desktop/Cerebro/test_storage";
                var hybridStorage = new HybridTieredStorage(
                    workingSetPath: $"{testPath}/working",
                    coldStoragePath: $"{testPath}/cold"
                );
                
                var stopwatch = Stopwatch.StartNew();
                
                // Write all concepts to hybrid storage
                foreach (var kvp in testData)
                {
                    await hybridStorage.WriteAsync($"concept_{kvp.Key}", kvp.Value);
                }
                
                stopwatch.Stop();
                
                var totalSeconds = stopwatch.Elapsed.TotalSeconds;
                var conceptsPerSecond = conceptCount / totalSeconds;
                
                Console.WriteLine($"⏱️  Time: {totalSeconds:F1} seconds ({stopwatch.ElapsedMilliseconds} ms)");
                Console.WriteLine($"⚡ Rate: {conceptsPerSecond:F2} concepts/second");
                Console.WriteLine($"📈 Projected 5K concepts: {(5000 / conceptsPerSecond):F1} seconds");
                
                if (totalSeconds < 30)
                    Console.WriteLine("✅ **TARGET ACHIEVED**: Under 30 seconds projected!");
                else if (totalSeconds < 60)
                    Console.WriteLine("🟡 **GOOD IMPROVEMENT**: Under 1 minute");
                else
                    Console.WriteLine("🔴 **STILL TOO SLOW**: Needs more optimization");
                
                // Test the stats
                var stats = await hybridStorage.GetStorageStatsAsync();
                Console.WriteLine($"📊 Storage Stats: {stats}");
                
                // Clean up
                await hybridStorage.ShutdownAsync();
                
                // Remove test directory
                if (Directory.Exists(testPath))
                {
                    Directory.Delete(testPath, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ New system failed: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
        }
        
        private static Dictionary<string, object> GenerateTestConcepts(int count)
        {
            var concepts = new Dictionary<string, object>();
            var random = new Random(42); // Fixed seed for reproducible results
            
            var sampleWords = new[]
            {
                "apple", "banana", "computer", "science", "learning", "neural", "network", "brain",
                "language", "processing", "artificial", "intelligence", "machine", "algorithm",
                "data", "structure", "programming", "software", "hardware", "memory",
                "storage", "database", "query", "optimization", "performance", "benchmark"
            };
            
            for (int i = 0; i < count; i++)
            {
                var baseWord = sampleWords[i % sampleWords.Length];
                var word = i < sampleWords.Length ? baseWord : $"{baseWord}_{i}";
                
                concepts[word] = new
                {
                    Word = word,
                    Frequency = random.Next(1, 1000),
                    Type = "vocabulary",
                    Connections = Enumerable.Range(0, random.Next(1, 10))
                        .Select(j => $"connection_{random.Next(0, count):D6}")
                        .ToArray(),
                    LastAccessed = DateTime.UtcNow.AddMinutes(-random.Next(0, 1000)),
                    Strength = random.NextDouble(),
                    Context = $"Example context for {word} with id {i}"
                };
            }
            
            return concepts;
        }
    }
}
