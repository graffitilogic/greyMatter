using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MessagePack;
using GreyMatter.Storage;

namespace greyMatter
{
    /// <summary>
    /// Comprehensive MessagePack performance demonstration
    /// Shows dramatic improvements over JSON serialization
    /// </summary>
    public class MessagePackPerformanceDemo
    {
        private readonly BinaryStorageManager _binaryManager;
        private readonly string _testDataPath;

        public MessagePackPerformanceDemo()
        {
            var config = new GreyMatter.Core.CerebroConfiguration();
            config.ValidateAndSetup();
            _testDataPath = config.BrainDataPath;
            _binaryManager = new BinaryStorageManager(_testDataPath);
        }

        /// <summary>
        /// Run comprehensive MessagePack performance test
        /// </summary>
        public async Task RunPerformanceTest()
        {
            Console.WriteLine("🚀 **MESSAGEPACK PERFORMANCE DEMO**");
            Console.WriteLine("==================================");
            Console.WriteLine("Demonstrating superior performance over JSON serialization\n");

            // Create comprehensive test data
            var testNeurons = CreateTestNeurons(2000);
            var testVocabulary = CreateTestVocabulary();
            var testConcepts = CreateTestConcepts();

            Console.WriteLine($"📊 Test Data Created:");
            Console.WriteLine($"   Neurons: {testNeurons.Count}");
            Console.WriteLine($"   Vocabulary Words: {testVocabulary.Words.Count}");
            Console.WriteLine($"   Concept Clusters: {testConcepts.Concepts.Count}\n");

            // Test MessagePack vs JSON performance
            await CompareSerializationMethods(testNeurons, testVocabulary, testConcepts);

            // Test file size comparison
            await CompareFileSizes(testNeurons);

            // Test real-world scenario
            await SimulateRealWorldUsage(testNeurons, testVocabulary);

            // Cleanup
            _binaryManager.CleanupTestFiles();
            Console.WriteLine("\n✅ Demo completed successfully!");
        }

        private async Task CompareSerializationMethods(List<SharedNeuron> neurons, VocabularyCluster vocab, ConceptCluster concepts)
        {
            Console.WriteLine("⚡ **SERIALIZATION PERFORMANCE COMPARISON**");
            Console.WriteLine("==========================================");

            const int iterations = 5;

            // MessagePack Performance
            var msgpackTimes = new List<double>();
            for (int i = 0; i < iterations; i++)
            {
                var start = Stopwatch.GetTimestamp();
                await _binaryManager.SaveNeuronsBinaryAsync(neurons, $"msgpack_test_{i}");
                var end = Stopwatch.GetTimestamp();
                msgpackTimes.Add((end - start) * 1000.0 / Stopwatch.Frequency);
            }
            var avgMsgpackTime = msgpackTimes.Average();

            // JSON Performance
            var jsonTimes = new List<double>();
            for (int i = 0; i < iterations; i++)
            {
                var start = Stopwatch.GetTimestamp();
                var json = System.Text.Json.JsonSerializer.Serialize(neurons);
                await File.WriteAllTextAsync(Path.Combine(_testDataPath, "neuron_pool_msgpack", $"json_test_{i}.json"), json);
                var end = Stopwatch.GetTimestamp();
                jsonTimes.Add((end - start) * 1000.0 / Stopwatch.Frequency);
            }
            var avgJsonTime = jsonTimes.Average();

            var improvement = avgJsonTime / avgMsgpackTime;

            Console.WriteLine($"📦 MessagePack: {avgMsgpackTime:F2}ms average");
            Console.WriteLine($"📄 JSON:        {avgJsonTime:F2}ms average");
            Console.WriteLine($"🚀 Improvement: {improvement:F1}x faster");
            Console.WriteLine($"💰 Time Saved:  {(avgJsonTime - avgMsgpackTime):F2}ms per operation\n");

            // Test deserialization
            Console.WriteLine("🔄 **DESERIALIZATION PERFORMANCE**");
            Console.WriteLine("=================================");

            var msgpackLoadTimes = new List<double>();
            for (int i = 0; i < iterations; i++)
            {
                var start = Stopwatch.GetTimestamp();
                var loaded = await _binaryManager.LoadNeuronsBinaryAsync($"msgpack_test_{i}");
                var end = Stopwatch.GetTimestamp();
                msgpackLoadTimes.Add((end - start) * 1000.0 / Stopwatch.Frequency);
            }
            var avgMsgpackLoadTime = msgpackLoadTimes.Average();

            var jsonLoadTimes = new List<double>();
            for (int i = 0; i < iterations; i++)
            {
                var start = Stopwatch.GetTimestamp();
                var json = await File.ReadAllTextAsync(Path.Combine(_testDataPath, "neuron_pool_msgpack", $"json_test_{i}.json"));
                var loaded = System.Text.Json.JsonSerializer.Deserialize<List<SharedNeuron>>(json);
                var end = Stopwatch.GetTimestamp();
                jsonLoadTimes.Add((end - start) * 1000.0 / Stopwatch.Frequency);
            }
            var avgJsonLoadTime = jsonLoadTimes.Average();

            var loadImprovement = avgJsonLoadTime / avgMsgpackLoadTime;

            Console.WriteLine($"📦 MessagePack Load: {avgMsgpackLoadTime:F2}ms average");
            Console.WriteLine($"📄 JSON Load:        {avgJsonLoadTime:F2}ms average");
            Console.WriteLine($"🚀 Load Improvement: {loadImprovement:F1}x faster");
        }

        private async Task CompareFileSizes(List<SharedNeuron> neurons)
        {
            Console.WriteLine("💾 **FILE SIZE COMPARISON**");
            Console.WriteLine("==========================");

            // Create files
            await _binaryManager.SaveNeuronsBinaryAsync(neurons, "size_test");
            var json = System.Text.Json.JsonSerializer.Serialize(neurons, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(_testDataPath, "neuron_pool_msgpack", "size_test.json"), json);

            // Get file sizes
            var msgpackFile = Path.Combine(_testDataPath, "neuron_pool_msgpack", "size_test.msgpack");
            var jsonFile = Path.Combine(_testDataPath, "neuron_pool_msgpack", "size_test.json");

            var msgpackSize = new FileInfo(msgpackFile).Length;
            var jsonSize = new FileInfo(jsonFile).Length;
            var compressionRatio = (double)msgpackSize / jsonSize;
            var spaceSaved = jsonSize - msgpackSize;

            Console.WriteLine($"📦 MessagePack Size: {msgpackSize:N0} bytes");
            Console.WriteLine($"📄 JSON Size:        {jsonSize:N0} bytes");
            Console.WriteLine($"🗜️  Compression:     {compressionRatio:P2} of original size");
            Console.WriteLine($"💾 Space Saved:      {spaceSaved:N0} bytes ({(spaceSaved * 100.0 / jsonSize):F1}%)\n");

            // Project system-wide savings
            var totalNeurons = 28572; // From diagnostic
            var estimatedJsonSize = totalNeurons * 1000L; // Rough estimate
            var estimatedMsgpackSize = (long)(estimatedJsonSize * compressionRatio);
            var totalSpaceSaved = estimatedJsonSize - estimatedMsgpackSize;

            Console.WriteLine("🔮 **SYSTEM-WIDE PROJECTIONS**");
            Console.WriteLine("=============================");
            Console.WriteLine($"Current JSON storage:     ~{estimatedJsonSize / 1024.0 / 1024.0:F1} MB");
            Console.WriteLine($"MessagePack storage:      ~{estimatedMsgpackSize / 1024.0 / 1024.0:F1} MB");
            Console.WriteLine($"Total space savings:      ~{totalSpaceSaved / 1024.0 / 1024.0:F1} MB");
            Console.WriteLine($"Storage efficiency:       {(1 - compressionRatio):P1} reduction");
        }

        private async Task SimulateRealWorldUsage(List<SharedNeuron> neurons, VocabularyCluster vocab)
        {
            Console.WriteLine("🌍 **REAL-WORLD USAGE SIMULATION**");
            Console.WriteLine("=================================");

            // Simulate batch processing scenario
            const int batchSize = 500;
            var batches = neurons.Chunk(batchSize).ToList();

            Console.WriteLine($"Processing {neurons.Count} neurons in {batches.Count} batches of {batchSize}...");

            var start = Stopwatch.StartNew();
            for (int i = 0; i < batches.Count; i++)
            {
                await _binaryManager.SaveNeuronsBinaryAsync(batches[i].ToList(), $"batch_{i}");
            }
            start.Stop();

            Console.WriteLine($"✅ Batch processing completed in {start.Elapsed.TotalMilliseconds:F2}ms");
            Console.WriteLine($"📊 Average time per batch: {start.Elapsed.TotalMilliseconds / batches.Count:F2}ms");
            Console.WriteLine($"⚡ Processing rate: {(neurons.Count / start.Elapsed.TotalSeconds):F0} neurons/second");

            // Test vocabulary operations
            var vocabStart = Stopwatch.StartNew();
            await _binaryManager.SaveVocabularyClusterBinaryAsync(vocab, "simulation_vocab");
            vocabStart.Stop();

            Console.WriteLine($"📚 Vocabulary saved in {vocabStart.Elapsed.TotalMilliseconds:F2}ms");
            Console.WriteLine($"   Words processed: {vocab.Words.Count}");
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
                    Data = $"Test neuron data {i} with some content {random.Next(1000)} and additional text to make it more realistic for performance testing purposes",
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

            var testWords = new[] { "neuron", "synapse", "cortex", "memory", "learning", "pattern", "network", "signal", "algorithm", "intelligence", "cognition", "plasticity" };

            foreach (var word in testWords)
            {
                cluster.Words[word] = new WordInfo
                {
                    Word = word,
                    Frequency = new Random().Next(1, 1000),
                    FirstSeen = DateTime.Now.AddDays(-new Random().Next(365)),
                    EstimatedType = word switch
                    {
                        "neuron" or "synapse" or "cortex" or "memory" or "network" or "signal" => WordType.Noun,
                        "learning" or "pattern" or "intelligence" or "cognition" or "plasticity" => WordType.Noun,
                        "algorithm" => WordType.Noun,
                        _ => WordType.Unknown
                    }
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

            var testConcepts = new[] { "neural_network", "machine_learning", "artificial_intelligence", "cognitive_science", "deep_learning", "neural_plasticity" };

            foreach (var concept in testConcepts)
            {
                cluster.Concepts[concept] = $"Test concept: {concept} with detailed description and metadata for performance testing";
            }

            return cluster;
        }
    }
}
