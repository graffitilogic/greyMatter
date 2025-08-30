using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using greyMatter.Core;
using MessagePack;

namespace GreyMatter.Storage
{
    /// <summary>
    /// High-performance binary serialization for neural data structures using MessagePack
    /// Replaces JSON serialization with compact binary format for better performance
    /// </summary>
    public class BinaryStorageManager
    {
        private readonly string _brainDataPath;
        private readonly string _neuronPoolPath;

        public BinaryStorageManager(string brainDataPath)
        {
            _brainDataPath = brainDataPath;
            _neuronPoolPath = Path.Combine(_brainDataPath, "neuron_pool_msgpack");

            // Ensure directories exist
            Directory.CreateDirectory(_neuronPoolPath);
        }

        /// <summary>
        /// Serialize a list of neurons to MessagePack format
        /// </summary>
        public async Task SaveNeuronsBinaryAsync(List<SharedNeuron> neurons, string filename)
        {
            var filePath = Path.Combine(_neuronPoolPath, filename + ".msgpack");

            await Task.Run(() =>
            {
                var bytes = MessagePackSerializer.Serialize(neurons);
                File.WriteAllBytes(filePath, bytes);
            });
        }

        /// <summary>
        /// Deserialize neurons from MessagePack format
        /// </summary>
        public async Task<List<SharedNeuron>> LoadNeuronsBinaryAsync(string filename)
        {
            var filePath = Path.Combine(_neuronPoolPath, filename + ".msgpack");

            if (!File.Exists(filePath))
                return new List<SharedNeuron>();

            return await Task.Run(() =>
            {
                var bytes = File.ReadAllBytes(filePath);
                return MessagePackSerializer.Deserialize<List<SharedNeuron>>(bytes);
            });
        }

        /// <summary>
        /// Serialize vocabulary cluster to MessagePack format
        /// </summary>
        public async Task SaveVocabularyClusterBinaryAsync(VocabularyCluster cluster, string filename)
        {
            var filePath = Path.Combine(_neuronPoolPath, "vocab_" + filename + ".msgpack");

            await Task.Run(() =>
            {
                var bytes = MessagePackSerializer.Serialize(cluster);
                File.WriteAllBytes(filePath, bytes);
            });
        }

        /// <summary>
        /// Deserialize vocabulary cluster from MessagePack format
        /// </summary>
        public async Task<VocabularyCluster> LoadVocabularyClusterBinaryAsync(string filename)
        {
            var filePath = Path.Combine(_neuronPoolPath, "vocab_" + filename + ".msgpack");

            if (!File.Exists(filePath))
                return new VocabularyCluster();

            return await Task.Run(() =>
            {
                var bytes = File.ReadAllBytes(filePath);
                return MessagePackSerializer.Deserialize<VocabularyCluster>(bytes);
            });
        }

        /// <summary>
        /// Serialize concept cluster to MessagePack format
        /// </summary>
        public async Task SaveConceptClusterBinaryAsync(ConceptCluster cluster, string filename)
        {
            var filePath = Path.Combine(_neuronPoolPath, "concept_" + filename + ".msgpack");

            await Task.Run(() =>
            {
                var bytes = MessagePackSerializer.Serialize(cluster);
                File.WriteAllBytes(filePath, bytes);
            });
        }

        /// <summary>
        /// Deserialize concept cluster from MessagePack format
        /// </summary>
        public async Task<ConceptCluster> LoadConceptClusterBinaryAsync(string filename)
        {
            var filePath = Path.Combine(_neuronPoolPath, "concept_" + filename + ".msgpack");

            if (!File.Exists(filePath))
                return new ConceptCluster();

            return await Task.Run(() =>
            {
                var bytes = File.ReadAllBytes(filePath);
                return MessagePackSerializer.Deserialize<ConceptCluster>(bytes);
            });
        }

        /// <summary>
        /// Get file size comparison between JSON and binary formats
        /// </summary>
        public async Task<StorageComparison> CompareStorageSizesAsync(List<SharedNeuron> neurons, string filename)
        {
            var comparison = new StorageComparison { Filename = filename };

            // Save as binary
            await SaveNeuronsBinaryAsync(neurons, filename + "_binary");
            var binaryFile = Path.Combine(_neuronPoolPath, filename + "_binary.msgpack");
            comparison.BinarySize = new FileInfo(binaryFile).Length;

            // Save as JSON for comparison
            var jsonFile = Path.Combine(_neuronPoolPath, filename + "_json.json");
            var json = System.Text.Json.JsonSerializer.Serialize(neurons, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(jsonFile, json);
            comparison.JsonSize = new FileInfo(jsonFile).Length;

            comparison.CompressionRatio = (double)comparison.BinarySize / comparison.JsonSize;
            comparison.SpaceSaved = comparison.JsonSize - comparison.BinarySize;

            return comparison;
        }

        /// <summary>
        /// Performance test for serialization speeds
        /// </summary>
        public async Task<PerformanceResults> TestSerializationPerformanceAsync(List<SharedNeuron> neurons, int iterations = 10)
        {
            var results = new PerformanceResults();

            // Test binary serialization
            var binaryTimes = new List<long>();
            for (int i = 0; i < iterations; i++)
            {
                var start = DateTime.UtcNow.Ticks;
                await SaveNeuronsBinaryAsync(neurons, $"perf_test_binary_{i}");
                var end = DateTime.UtcNow.Ticks;
                binaryTimes.Add(end - start);
            }
            results.BinarySerializationTime = TimeSpan.FromTicks((long)binaryTimes.Average());

            // Test JSON serialization
            var jsonTimes = new List<long>();
            for (int i = 0; i < iterations; i++)
            {
                var start = DateTime.UtcNow.Ticks;
                var json = System.Text.Json.JsonSerializer.Serialize(neurons, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(Path.Combine(_neuronPoolPath, $"perf_test_json_{i}.json"), json);
                var end = DateTime.UtcNow.Ticks;
                jsonTimes.Add(end - start);
            }
            results.JsonSerializationTime = TimeSpan.FromTicks((long)jsonTimes.Average());

            results.PerformanceImprovement = results.JsonSerializationTime.TotalMilliseconds / results.BinarySerializationTime.TotalMilliseconds;

            return results;
        }

        /// <summary>
        /// Clean up test files
        /// </summary>
        public void CleanupTestFiles()
        {
            try
            {
                var testFiles = Directory.GetFiles(_neuronPoolPath, "perf_test_*");
                foreach (var file in testFiles)
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not cleanup test files: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Storage size comparison results
    /// </summary>
    public class StorageComparison
    {
        public string Filename { get; set; } = string.Empty;
        public long JsonSize { get; set; }
        public long BinarySize { get; set; }
        public double CompressionRatio { get; set; }
        public long SpaceSaved { get; set; }

        public override string ToString()
        {
            return $"{Filename}: JSON={JsonSize:N0}B, Binary={BinarySize:N0}B, Ratio={CompressionRatio:F2}x, Saved={SpaceSaved:N0}B";
        }
    }

    /// <summary>
    /// Performance test results
    /// </summary>
    public class PerformanceResults
    {
        public TimeSpan BinarySerializationTime { get; set; }
        public TimeSpan JsonSerializationTime { get; set; }
        public double PerformanceImprovement { get; set; }

        public override string ToString()
        {
            return $"Binary: {BinarySerializationTime.TotalMilliseconds:F2}ms, JSON: {JsonSerializationTime.TotalMilliseconds:F2}ms, Improvement: {PerformanceImprovement:F1}x";
        }
    }
}
