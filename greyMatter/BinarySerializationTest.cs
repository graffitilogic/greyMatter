using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using GreyMatter.Storage;

namespace greyMatter
{
    /// <summary>
    /// Test program to demonstrate binary serialization performance improvements
    /// </summary>
    public class BinarySerializationTest
    {
        private readonly BinaryStorageManager _binaryManager;
        private readonly string _testDataPath;

        public BinarySerializationTest()
        {
            var config = new GreyMatter.Core.CerebroConfiguration();
            config.ValidateAndSetup();
            _testDataPath = config.BrainDataPath;
            _binaryManager = new BinaryStorageManager(_testDataPath);
        }

        /// <summary>
        /// Run comprehensive binary serialization performance test
        /// </summary>
        public async Task RunPerformanceTest()
        {
            Console.WriteLine("🔬 **BINARY SERIALIZATION PERFORMANCE TEST**");
            Console.WriteLine("==============================================");

            // Create test data
            var testNeurons = CreateTestNeurons(1000);
            var testVocabulary = CreateTestVocabulary();
            var testConcepts = CreateTestConcepts();

            Console.WriteLine($"📊 Test Data: {testNeurons.Count} neurons, {testVocabulary.Words.Count} words, {testConcepts.Concepts.Count} concepts\n");

            // Test neuron serialization performance
            Console.WriteLine("🧠 **NEURON SERIALIZATION PERFORMANCE**");
            var neuronResults = await _binaryManager.TestSerializationPerformanceAsync(testNeurons, 5);
            Console.WriteLine($"   {neuronResults}");

            // Test storage size comparison
            Console.WriteLine("\n💾 **STORAGE SIZE COMPARISON**");
            var sizeComparison = await _binaryManager.CompareStorageSizesAsync(testNeurons, "test_neurons");
            Console.WriteLine($"   {sizeComparison}");

            // Test vocabulary serialization
            Console.WriteLine("\n📚 **VOCABULARY SERIALIZATION TEST**");
            var vocabStart = Stopwatch.StartNew();
            await _binaryManager.SaveVocabularyClusterBinaryAsync(testVocabulary, "test_vocab");
            vocabStart.Stop();
            Console.WriteLine($"   Binary save time: {vocabStart.Elapsed.TotalMilliseconds:F2}ms");

            var vocabLoadStart = Stopwatch.StartNew();
            var loadedVocab = await _binaryManager.LoadVocabularyClusterBinaryAsync("test_vocab");
            vocabLoadStart.Stop();
            Console.WriteLine($"   Binary load time: {vocabLoadStart.Elapsed.TotalMilliseconds:F2}ms");
            Console.WriteLine($"   Loaded {loadedVocab.Words.Count} words successfully");

            // Test concept serialization
            Console.WriteLine("\n🎯 **CONCEPT SERIALIZATION TEST**");
            var conceptStart = Stopwatch.StartNew();
            await _binaryManager.SaveConceptClusterBinaryAsync(testConcepts, "test_concepts");
            conceptStart.Stop();
            Console.WriteLine($"   Binary save time: {conceptStart.Elapsed.TotalMilliseconds:F2}ms");

            var conceptLoadStart = Stopwatch.StartNew();
            var loadedConcepts = await _binaryManager.LoadConceptClusterBinaryAsync("test_concepts");
            conceptLoadStart.Stop();
            Console.WriteLine($"   Binary load time: {conceptLoadStart.Elapsed.TotalMilliseconds:F2}ms");
            Console.WriteLine($"   Loaded {loadedConcepts.Concepts.Count} concepts successfully");

            // Calculate overall improvements
            Console.WriteLine("\n📈 **OVERALL PERFORMANCE SUMMARY**");
            Console.WriteLine($"   Serialization Speed: {neuronResults.PerformanceImprovement:F1}x faster");
            Console.WriteLine($"   Storage Efficiency: {sizeComparison.CompressionRatio:F2}x smaller");
            Console.WriteLine($"   Space Saved: {sizeComparison.SpaceSaved:N0} bytes per 1000 neurons");

            // Estimate system-wide improvements
            Console.WriteLine("\n🔮 **PROJECTED SYSTEM IMPROVEMENTS**");
            var totalNeurons = 28572; // From diagnostic
            var estimatedJsonSize = totalNeurons * 1000; // Rough estimate
            var estimatedBinarySize = (long)(estimatedJsonSize * sizeComparison.CompressionRatio);
            var totalSpaceSaved = estimatedJsonSize - estimatedBinarySize;

            Console.WriteLine($"   Current JSON storage: ~{estimatedJsonSize / 1024.0 / 1024.0:F1} MB");
            Console.WriteLine($"   Binary storage: ~{estimatedBinarySize / 1024.0 / 1024.0:F1} MB");
            Console.WriteLine($"   Total space savings: ~{totalSpaceSaved / 1024.0 / 1024.0:F1} MB");

            // Cleanup test files
            _binaryManager.CleanupTestFiles();
            Console.WriteLine("\n✅ Test files cleaned up");
        }

        /// <summary>
        /// Create test neurons for performance testing
        /// </summary>
        private List<SharedNeuron> CreateTestNeurons(int count)
        {
            var neurons = new List<SharedNeuron>();
            var random = new Random(42); // Deterministic for consistent results

            for (int i = 0; i < count; i++)
            {
                neurons.Add(new SharedNeuron
                {
                    Id = i,
                    Data = $"Test neuron data {i} with some content {random.Next(1000)}",
                    LastUsed = DateTime.Now.AddMinutes(-random.Next(1440)) // Random time in last 24h
                });
            }

            return neurons;
        }

        /// <summary>
        /// Create test vocabulary cluster
        /// </summary>
        private VocabularyCluster CreateTestVocabulary()
        {
            var cluster = new VocabularyCluster { LastModified = DateTime.Now };

            var testWords = new[] { "neuron", "synapse", "cortex", "memory", "learning", "pattern", "network", "signal" };

            foreach (var word in testWords)
            {
                cluster.Words[word] = new WordInfo
                {
                    Word = word,
                    Frequency = new Random().Next(1, 1000),
                    FirstSeen = DateTime.Now.AddDays(-new Random().Next(365)),
                    EstimatedType = WordType.Noun
                };
            }

            return cluster;
        }

        /// <summary>
        /// Create test concept cluster
        /// </summary>
        private ConceptCluster CreateTestConcepts()
        {
            var cluster = new ConceptCluster { LastModified = DateTime.Now };

            var testConcepts = new[] { "neural_network", "machine_learning", "artificial_intelligence", "cognitive_science" };

            foreach (var concept in testConcepts)
            {
                cluster.Concepts[concept] = new
                {
                    Name = concept,
                    Description = $"Test concept: {concept}",
                    RelatedTerms = new[] { "neuron", "synapse", "learning" },
                    Strength = new Random().NextDouble()
                };
            }

            return cluster;
        }

        // Commented out to avoid multiple entry points
        // /// <summary>
        // /// Entry point for binary serialization test
        // /// </summary>
        // public static async Task Main(string[] args)
        // {
        //     try
        //     {
        //         var test = new BinarySerializationTest();
        //         await test.RunPerformanceTest();
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"❌ Test failed: {ex.Message}");
        //         Console.WriteLine(ex.StackTrace);
        //     }
        //
        //     Console.WriteLine("\nPress any key to exit...");
        //     Console.ReadKey();
        // }
    }
}
