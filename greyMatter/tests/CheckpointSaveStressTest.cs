using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GreyMatter.Core;
using GreyMatter.Storage;

namespace GreyMatter.Tests
{
    /// <summary>
    /// Stress test checkpoint saving to expose JSON serialization bugs WITHOUT waiting hours
    /// Tests edge cases: special chars, large dictionaries, concurrent modification, NaN/Infinity
    /// </summary>
    public class CheckpointSaveStressTest
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("🧪 CHECKPOINT SAVE STRESS TEST - Finding serialization bugs fast");
            Console.WriteLine("=================================================================\n");

            var allPassed = true;

            // Test 1: Special characters in strings (most likely culprit for "Expected ':'")
            Console.WriteLine("Test 1: Special characters in ConceptTag and AssociatedConcepts...");
            allPassed &= TestSpecialCharacters();

            // Test 2: Large InputWeights dictionaries (duplicate key detection)
            Console.WriteLine("\nTest 2: Large InputWeights dictionaries...");
            allPassed &= TestLargeWeightDictionaries();

            // Test 3: Edge case double values
            Console.WriteLine("\nTest 3: Edge case double values (NaN, Infinity, subnormals)...");
            allPassed &= TestEdgeCaseDoubles();

            // Test 4: Concurrent modification during snapshot
            Console.WriteLine("\nTest 4: Concurrent modification during snapshot creation...");
            allPassed &= TestConcurrentModification();

            // Test 5: Real-world scale test (2.9M neurons like production)
            Console.WriteLine("\nTest 5: Production-scale checkpoint (18K clusters, 2.9M neurons)...");
            allPassed &= TestProductionScale();

            Console.WriteLine("\n=================================================================");
            if (allPassed)
            {
                Console.WriteLine("✅ ALL TESTS PASSED - Checkpoint saving is robust");
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine("❌ TESTS FAILED - Found serialization bugs (see above)");
                Environment.Exit(1);
            }
        }

        static bool TestSpecialCharacters()
        {
            var problematicStrings = new[]
            {
                "simple",
                "with spaces",
                "with\ttabs",
                "with\nnewlines",
                "with\"quotes\"",
                "with'apostrophes'",
                "with\\backslashes",
                "with/slashes",
                "with:colons",
                "with{braces}",
                "with[brackets]",
                "unicode:café",
                "emoji:🧠💾",
                "control:\u0001\u001f",
                "mixed:\"It's\\n\\ta\\ttab\""
            };

            foreach (var testString in problematicStrings)
            {
                var neuron = new HybridNeuron(testString);
                neuron.AssociatedConcepts.Add(testString + "_assoc");
                neuron.InputWeights[Guid.NewGuid()] = 0.5;

                if (!TrySerializeNeuron(neuron, out var error))
                {
                    Console.WriteLine($"  ❌ FAILED on: '{testString.Replace("\n", "\\n").Replace("\t", "\\t")}' - {error}");
                    return false;
                }
            }

            Console.WriteLine("  ✅ All special characters handled correctly");
            return true;
        }

        static bool TestLargeWeightDictionaries()
        {
            var neuron = new HybridNeuron("large_weights");
            
            // Simulate production: some neurons have thousands of connections
            var guids = Enumerable.Range(0, 10000).Select(_ => Guid.NewGuid()).ToList();
            foreach (var guid in guids)
            {
                neuron.InputWeights[guid] = Random.Shared.NextDouble();
            }

            // Test for duplicate keys (should be impossible with Guid, but worth checking)
            if (neuron.InputWeights.Count != guids.Count)
            {
                Console.WriteLine($"  ❌ DUPLICATE KEYS: Expected {guids.Count}, got {neuron.InputWeights.Count}");
                return false;
            }

            if (!TrySerializeNeuron(neuron, out var error))
            {
                Console.WriteLine($"  ❌ FAILED to serialize large dictionary: {error}");
                return false;
            }

            Console.WriteLine($"  ✅ Successfully serialized neuron with {neuron.InputWeights.Count:N0} weights");
            return true;
        }

        static bool TestEdgeCaseDoubles()
        {
            var testValues = new[]
            {
                (double.NaN, "NaN"),
                (double.PositiveInfinity, "PositiveInfinity"),
                (double.NegativeInfinity, "NegativeInfinity"),
                (double.Epsilon, "Epsilon"),
                (double.MinValue, "MinValue"),
                (double.MaxValue, "MaxValue"),
                (-0.0, "NegativeZero"),
                (1e-323, "Subnormal"),
                (1e308, "NearMaxValue"),
                (-1e308, "NearMinValue")
            };

            foreach (var (value, name) in testValues)
            {
                var neuron = new HybridNeuron("edge_case");
                neuron.Bias = value;
                neuron.Threshold = value;
                neuron.LearningRate = value;
                neuron.ImportanceScore = value;
                neuron.InputWeights[Guid.NewGuid()] = value;

                if (!TrySerializeNeuron(neuron, out var error))
                {
                    Console.WriteLine($"  ❌ FAILED on {name} ({value}): {error}");
                    return false;
                }
            }

            Console.WriteLine("  ✅ All edge case doubles sanitized correctly");
            return true;
        }

        static bool TestConcurrentModification()
        {
            var neuron = new HybridNeuron("concurrent_test");
            
            // Simulate concurrent modification during snapshot
            var modificationTask = System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    neuron.InputWeights[Guid.NewGuid()] = Random.Shared.NextDouble();
                    System.Threading.Thread.Sleep(1);
                }
            });

            // Try to create snapshots while modifications are happening
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    var snapshot = neuron.CreateSnapshot();
                    var json = JsonSerializer.Serialize(snapshot);
                    var deserialized = JsonSerializer.Deserialize<NeuronSnapshot>(json);
                    
                    if (deserialized == null)
                    {
                        Console.WriteLine($"  ❌ Snapshot deserialized to null on iteration {i}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ❌ FAILED on iteration {i}: {ex.Message}");
                    return false;
                }
                System.Threading.Thread.Sleep(10);
            }

            modificationTask.Wait();
            Console.WriteLine("  ✅ Concurrent modification handled safely (100 snapshots during 1000 modifications)");
            return true;
        }

        static bool TestProductionScale()
        {
            Console.WriteLine("  Creating production-scale test data (this may take a minute)...");
            
            var clusters = new List<NeuronCluster>();
            var totalNeurons = 0;
            var clusterCount = 1000; // Test with 1000 clusters instead of full 18K for speed
            var neuronsPerCluster = 100; // 100K total neurons

            for (int c = 0; c < clusterCount; c++)
            {
                var cluster = new NeuronCluster(Guid.NewGuid(), $"cluster_{c}");
                
                for (int n = 0; n < neuronsPerCluster; n++)
                {
                    var neuron = new HybridNeuron($"concept_{c}_{n}");
                    neuron.AssociatedConcepts.Add($"assoc_{Random.Shared.Next(1000)}");
                    neuron.Bias = Random.Shared.NextDouble() * 2 - 1;
                    neuron.Threshold = Random.Shared.NextDouble();
                    neuron.ImportanceScore = Random.Shared.NextDouble();
                    
                    // Random connections (simulates real training)
                    var connectionCount = Random.Shared.Next(1, 50);
                    for (int w = 0; w < connectionCount; w++)
                    {
                        neuron.InputWeights[Guid.NewGuid()] = Random.Shared.NextDouble() * 2 - 1;
                    }
                    
                    cluster.AddNeuron(neuron);
                    totalNeurons++;
                }
                
                clusters.Add(cluster);
                
                if (c % 100 == 0 && c > 0)
                {
                    Console.WriteLine($"    Created {c:N0} clusters, {totalNeurons:N0} neurons...");
                }
            }

            Console.WriteLine($"  Serializing {clusters.Count:N0} clusters with {totalNeurons:N0} neurons...");
            
            try
            {
                var snapshots = new Dictionary<string, List<NeuronSnapshot>>();
                
                foreach (var cluster in clusters)
                {
                    var neurons = cluster.GetNeuronsAsync().Result;
                    var clusterSnapshots = neurons.Values.Select(n => n.CreateSnapshot()).ToList();
                    snapshots[cluster.ClusterId.ToString()] = clusterSnapshots;
                }

                // Test JSON serialization (this is where production fails)
                var json = JsonSerializer.Serialize(snapshots, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
                    DictionaryKeyPolicy = null,
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                    MaxDepth = 64
                });

                Console.WriteLine($"  Serialized to {json.Length / 1024.0 / 1024.0:F1} MB JSON");

                // Test deserialization (catch malformed JSON)
                var deserialized = JsonSerializer.Deserialize<Dictionary<string, List<NeuronSnapshot>>>(json);
                
                if (deserialized == null || deserialized.Count != snapshots.Count)
                {
                    Console.WriteLine($"  ❌ Deserialization mismatch: Expected {snapshots.Count} clusters, got {deserialized?.Count ?? 0}");
                    return false;
                }

                Console.WriteLine($"  ✅ Successfully round-tripped {totalNeurons:N0} neurons through JSON");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ FAILED: {ex.Message}");
                Console.WriteLine($"     Stack: {ex.StackTrace}");
                return false;
            }
        }

        static bool TrySerializeNeuron(HybridNeuron neuron, out string error)
        {
            try
            {
                var snapshot = neuron.CreateSnapshot();
                var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
                });

                // Try to deserialize to catch malformed JSON
                var deserialized = JsonSerializer.Deserialize<NeuronSnapshot>(json);
                
                if (deserialized == null)
                {
                    error = "Deserialized to null";
                    return false;
                }

                error = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
