using System;
using System.Collections.Generic;
using System.Linq;
using GreyMatter.Core;

namespace GreyMatter.Validation
{
    /// <summary>
    /// ADPC-Net Phase 3: Validation tests for sparse synaptic graph with Hebbian learning
    /// 
    /// Phase 3 Objectives:
    /// - Replace dense synapse storage with sparse graph
    /// - Implement Hebbian learning ("neurons that fire together, wire together")
    /// - Enable efficient synaptic pruning and decay
    /// - Maintain constant memory despite network growth
    /// 
    /// Success Criteria:
    /// ✅ Sparse storage (synapse count << neuron_count²)
    /// ✅ Hebbian strengthening (co-activation increases weights)
    /// ✅ Pruning removes weak connections
    /// ✅ Decay weakens unused connections
    /// ✅ Export/import maintains graph structure
    /// ✅ Memory scales linearly with synapses, not neurons
    /// </summary>
    public class AdpcPhase3ValidationTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ADPC-Net Phase 3 Validation Tests ===");
            Console.WriteLine("Testing sparse synaptic graph with Hebbian learning\n");

            int passed = 0;
            int total = 0;

            // Test 1: Sparse storage validation
            total++;
            if (TestSparseStorage())
            {
                Console.WriteLine("✅ Test 1 PASSED: Sparse storage (synapses << N²)");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ Test 1 FAILED: Sparse storage inefficient");
            }

            // Test 2: Hebbian strengthening
            total++;
            if (TestHebbianStrengthening())
            {
                Console.WriteLine("✅ Test 2 PASSED: Co-activation strengthens synapses");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ Test 2 FAILED: Hebbian learning not working");
            }

            // Test 3: Selective connectivity (not all-to-all)
            total++;
            if (TestSelectiveConnectivity())
            {
                Console.WriteLine("✅ Test 3 PASSED: Selective connectivity (not dense)");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ Test 3 FAILED: Graph is too dense");
            }

            // Test 4: Pruning weak synapses
            total++;
            if (TestSynapticPruning())
            {
                Console.WriteLine("✅ Test 4 PASSED: Weak synapses pruned correctly");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ Test 4 FAILED: Pruning not working");
            }

            // Test 5: Synaptic decay (forgetting)
            total++;
            if (TestSynapticDecay())
            {
                Console.WriteLine("✅ Test 5 PASSED: Unused synapses decay");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ Test 5 FAILED: Decay mechanism broken");
            }

            // Test 6: Export/Import persistence
            total++;
            if (TestExportImport())
            {
                Console.WriteLine("✅ Test 6 PASSED: Graph persists correctly");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ Test 6 FAILED: Export/import broken");
            }

            Console.WriteLine($"\n=== Results: {passed}/{total} tests passed ({(100.0 * passed / total):F1}%) ===");
            
            if (passed == total)
            {
                Console.WriteLine("🎉 Phase 3: COMPLETE - All tests passing!");
                Console.WriteLine("\nNext: Phase 4 (VQ-VAE codebook) or production training");
            }
            else
            {
                Console.WriteLine($"⚠️  Phase 3: {total - passed} test(s) still failing");
            }
        }

        /// <summary>
        /// Test 1: Verify sparse storage (synapse count << neuron_count²)
        /// Success: Sparsity > 90% (< 10% of possible connections)
        /// </summary>
        private static bool TestSparseStorage()
        {
            var graph = new SparseSynapticGraph(
                learningRate: 0.01f,
                minWeight: 0.0f,
                maxWeight: 1.0f,
                pruneThreshold: 0.1f
            );

            // Create co-activation patterns for 100 neurons
            var neurons = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
            
            // Record selective co-activations (small clusters)
            for (int i = 0; i < 1000; i++)
            {
                // Random cluster of 3-5 neurons
                int clusterSize = Random.Shared.Next(3, 6);
                var cluster = neurons.OrderBy(_ => Random.Shared.Next()).Take(clusterSize).ToList();
                
                var activations = cluster.Select(id => (id, activation: 0.8f)).ToList();
                graph.RecordCoactivationPattern(activations);
            }

            int synapseCount = graph.GetSynapseCount();
            int maxPossible = neurons.Count * neurons.Count; // N²
            double sparsity = 1.0 - ((double)synapseCount / maxPossible);

            Console.WriteLine($"  Neurons: {neurons.Count}, Synapses: {synapseCount}, Max Possible: {maxPossible}");
            Console.WriteLine($"  Sparsity: {sparsity * 100:F2}% (should be > 90%)");

            return sparsity > 0.90; // More than 90% sparse
        }

        /// <summary>
        /// Test 2: Verify Hebbian strengthening (repeated co-activation increases weight)
        /// Success: Weight increases with repetition, plateaus at max
        /// </summary>
        private static bool TestHebbianStrengthening()
        {
            var graph = new SparseSynapticGraph(
                learningRate: 0.1f, // Higher learning rate for faster testing
                minWeight: 0.0f,
                maxWeight: 1.0f,
                pruneThreshold: 0.01f
            );

            var neuron1 = Guid.NewGuid();
            var neuron2 = Guid.NewGuid();

            // Record initial weight (should be zero)
            float initialWeight = graph.GetSynapseWeight(neuron1, neuron2);
            
            // Record co-activation 20 times
            var weights = new List<float>();
            for (int i = 0; i < 20; i++)
            {
                graph.RecordCoactivation(neuron1, neuron2, 0.8f, 0.8f);
                float weight = graph.GetSynapseWeight(neuron1, neuron2);
                weights.Add(weight);
            }

            // Verify weight increases monotonically
            bool monotonic = true;
            for (int i = 1; i < weights.Count; i++)
            {
                if (weights[i] < weights[i - 1])
                {
                    monotonic = false;
                    break;
                }
            }

            // Verify final weight is significantly higher than initial
            float finalWeight = weights.Last();
            bool strengthened = finalWeight > initialWeight + 0.2f;

            Console.WriteLine($"  Initial: {initialWeight:F4}, Final: {finalWeight:F4}");
            Console.WriteLine($"  Weight increased: {strengthened}, Monotonic: {monotonic}");

            return strengthened && monotonic;
        }

        /// <summary>
        /// Test 3: Verify selective connectivity (not all-to-all)
        /// Success: Connectivity < 20% (only related neurons connect)
        /// </summary>
        private static bool TestSelectiveConnectivity()
        {
            var graph = new SparseSynapticGraph(
                learningRate: 0.01f,
                minWeight: 0.0f,
                maxWeight: 1.0f,
                pruneThreshold: 0.1f
            );

            // Create 50 neurons
            var neurons = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToList();
            
            // Record co-activations only within small groups (5 groups of 10)
            for (int group = 0; group < 5; group++)
            {
                var groupNeurons = neurons.Skip(group * 10).Take(10).ToList();
                
                // Record 100 patterns within this group
                for (int i = 0; i < 100; i++)
                {
                    var active = groupNeurons.OrderBy(_ => Random.Shared.Next()).Take(5).ToList();
                    var activations = active.Select(id => (id, activation: 0.8f)).ToList();
                    graph.RecordCoactivationPattern(activations);
                }
            }

            int synapseCount = graph.GetSynapseCount();
            int maxPossible = neurons.Count * neurons.Count;
            double connectivity = (double)synapseCount / maxPossible;

            Console.WriteLine($"  Connectivity: {connectivity * 100:F2}% (should be < 20%)");
            Console.WriteLine($"  Synapses: {synapseCount}, Max Possible: {maxPossible}");

            return connectivity < 0.20; // Less than 20% connectivity
        }

        /// <summary>
        /// Test 4: Verify synaptic pruning removes weak connections
        /// Success: Weak synapses removed, strong ones retained
        /// </summary>
        private static bool TestSynapticPruning()
        {
            var graph = new SparseSynapticGraph(
                learningRate: 0.2f, // Higher learning rate to create weak synapses
                minWeight: 0.0f,
                maxWeight: 1.0f,
                pruneThreshold: 0.15f // Prune anything below 0.15
            );

            var neurons = Enumerable.Range(0, 20).Select(_ => Guid.NewGuid()).ToList();
            
            // Create weak connections (single activation with moderate strength)
            // delta = 0.2 * 0.6 * 0.6 = 0.072 (below creation threshold of 0.1, but we'll strengthen existing)
            for (int i = 0; i < 10; i++)
            {
                var n1 = neurons[i];
                var n2 = neurons[i + 1];
                // First create connection with strong activation
                graph.RecordCoactivation(n1, n2, 0.8f, 0.8f); // Creates synapse with 0.128
                // Then let it weaken through lack of reinforcement
            }

            // Create strong connections (repeated activation)
            for (int i = 10; i < 15; i++)
            {
                var n1 = neurons[i];
                var n2 = neurons[i + 1];
                for (int rep = 0; rep < 10; rep++)
                {
                    graph.RecordCoactivation(n1, n2, 0.8f, 0.8f); // Strong activation
                }
            }

            int beforePrune = graph.GetSynapseCount();
            int pruned = graph.PruneWeakSynapses();
            int afterPrune = graph.GetSynapseCount();

            Console.WriteLine($"  Before: {beforePrune}, Pruned: {pruned}, After: {afterPrune}");
            if (beforePrune > 0)
                Console.WriteLine($"  Pruning removed {(100.0 * pruned / beforePrune):F1}% of synapses");

            // Should have pruned at least some weak synapses
            return pruned > 0 && afterPrune < beforePrune;
        }

        /// <summary>
        /// Test 5: Verify synaptic decay weakens unused connections
        /// Success: Weights decrease with decay, approaching minimum
        /// </summary>
        private static bool TestSynapticDecay()
        {
            var graph = new SparseSynapticGraph(
                learningRate: 0.1f,
                minWeight: 0.0f,
                maxWeight: 1.0f,
                pruneThreshold: 0.1f
            );

            var neuron1 = Guid.NewGuid();
            var neuron2 = Guid.NewGuid();

            // Build up strong connection
            for (int i = 0; i < 10; i++)
            {
                graph.RecordCoactivation(neuron1, neuron2, 0.8f, 0.8f);
            }

            float beforeDecay = graph.GetSynapseWeight(neuron1, neuron2);

            // Apply decay multiple times (simulating time passing without activation)
            for (int i = 0; i < 10; i++)
            {
                graph.ApplyDecay(0.95f); // 5% decay per step
            }

            float afterDecay = graph.GetSynapseWeight(neuron1, neuron2);

            Console.WriteLine($"  Before decay: {beforeDecay:F4}, After decay: {afterDecay:F4}");
            Console.WriteLine($"  Weight decreased: {beforeDecay > afterDecay}");

            // Weight should have decreased significantly
            return afterDecay < beforeDecay * 0.7f; // At least 30% reduction
        }

        /// <summary>
        /// Test 6: Verify export/import maintains graph structure
        /// Success: Exported graph matches when re-imported
        /// </summary>
        private static bool TestExportImport()
        {
            var graph1 = new SparseSynapticGraph(
                learningRate: 0.01f,
                minWeight: 0.0f,
                maxWeight: 1.0f,
                pruneThreshold: 0.1f
            );

            var neurons = Enumerable.Range(0, 30).Select(_ => Guid.NewGuid()).ToList();
            
            // Create some connections
            for (int i = 0; i < 100; i++)
            {
                int idx1 = Random.Shared.Next(neurons.Count);
                int idx2 = Random.Shared.Next(neurons.Count);
                if (idx1 != idx2)
                {
                    graph1.RecordCoactivation(neurons[idx1], neurons[idx2], 0.7f, 0.7f);
                }
            }

            // Export
            var exported = graph1.ExportSynapses();
            int originalCount = exported.Count;

            // Create new graph and import
            var graph2 = new SparseSynapticGraph(
                learningRate: 0.01f,
                minWeight: 0.0f,
                maxWeight: 1.0f,
                pruneThreshold: 0.1f
            );
            graph2.ImportSynapses(exported);

            // Verify counts match
            int importedCount = graph2.GetSynapseCount();
            bool countsMatch = originalCount == importedCount;

            // Verify some weights match
            int matchingWeights = 0;
            int checkedWeights = 0;
            foreach (var synapse in exported.Take(10))
            {
                checkedWeights++;
                float w1 = (float)synapse.Weight;
                float w2 = graph2.GetSynapseWeight(synapse.PresynapticNeuronId, synapse.PostsynapticNeuronId);
                if (Math.Abs(w1 - w2) < 0.001f)
                {
                    matchingWeights++;
                }
            }

            bool weightsMatch = matchingWeights == checkedWeights;

            Console.WriteLine($"  Original: {originalCount}, Imported: {importedCount}");
            Console.WriteLine($"  Counts match: {countsMatch}, Weights match: {weightsMatch}");

            return countsMatch && weightsMatch;
        }
    }
}
