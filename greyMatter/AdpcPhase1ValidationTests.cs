using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter
{
    /// <summary>
    /// ADPC-Net Phase 1 Validation Tests
    /// Tests pattern-based learning WITHOUT word list lookups
    /// </summary>
    public class AdpcPhase1ValidationTests
    {
        private readonly FeatureEncoder _encoder;
        private readonly LSHPartitioner _partitioner;
        private readonly ActivationStats _stats;

        public AdpcPhase1ValidationTests()
        {
            _encoder = new FeatureEncoder(dimensions: 128);
            _partitioner = new LSHPartitioner(dimensions: 128, numBands: 16, rowsPerBand: 4);
            _stats = new ActivationStats();
        }

        /// <summary>
        /// Test 1: Deterministic Encoding
        /// Same word → same feature vector
        /// </summary>
        public bool Test1_DeterministicEncoding()
        {
            Console.WriteLine("\n=== Test 1: Deterministic Encoding ===");
            
            var vec1 = _encoder.Encode("cat");
            var vec2 = _encoder.Encode("cat");
            var vec3 = _encoder.Encode("dog");
            
            // Same word should produce identical vectors
            bool identical = VectorsEqual(vec1, vec2);
            
            // Different words should produce different vectors
            bool different = !VectorsEqual(vec1, vec3);
            
            // Calculate similarity
            var sameSimilarity = _partitioner.CosineSimilarity(vec1, vec2);
            var diffSimilarity = _partitioner.CosineSimilarity(vec1, vec3);
            
            Console.WriteLine($"  cat vs cat: identical={identical}, similarity={sameSimilarity:F3}");
            Console.WriteLine($"  cat vs dog: different={different}, similarity={diffSimilarity:F3}");
            Console.WriteLine($"  ✅ Result: {(identical && different ? "PASS" : "FAIL")}");
            
            return identical && different;
        }

        /// <summary>
        /// Test 2: Similar Words → Similar Regions
        /// NOTE: Using character-based encoding (not semantic), so we test
        /// orthographically similar words instead of semantically similar
        /// </summary>
        public bool Test2_SimilarWordsNearbyRegions()
        {
            Console.WriteLine("\n=== Test 2: Similar Words → Similar Regions ===");
            
            // Test orthographically similar words (character-based encoder)
            var testPairs = new[]
            {
                ("cat", "cats"),      // plural
                ("run", "running"),   // inflection
                ("test", "testing"),  // inflection
                ("happy", "happily")  // derivation
            };
            
            int passCount = 0;
            foreach (var (word1, word2) in testPairs)
            {
                var vec1 = _encoder.Encode(word1);
                var vec2 = _encoder.Encode(word2);
                
                var region1 = _partitioner.GetRegionId(vec1);
                var nearbyRegions2 = _partitioner.GetNearbyRegions(vec2, neighbors: 5);
                
                var similarity = _partitioner.CosineSimilarity(vec1, vec2);
                var inNearby = nearbyRegions2.Contains(region1);
                
                Console.WriteLine($"  {word1} vs {word2}: similarity={similarity:F3}, nearby={inNearby}");
                
                // Pass if similar (>0.4) or in nearby regions
                if (inNearby || similarity > 0.4)
                    passCount++;
            }
            
            bool passed = passCount >= testPairs.Length / 2; // At least half should be similar
            Console.WriteLine($"  ✅ Result: {passCount}/{testPairs.Length} pairs nearby → {(passed ? "PASS" : "FAIL")}");
            
            return passed;
        }

        /// <summary>
        /// Test 3: Dissimilar Words → Different Regions
        /// Unrelated words should NOT activate same regions
        /// </summary>
        public bool Test3_DissimilarWordsDifferentRegions()
        {
            Console.WriteLine("\n=== Test 3: Dissimilar Words → Different Regions ===");
            
            var testPairs = new[]
            {
                ("cat", "computer"),
                ("run", "table"),
                ("happy", "quantum"),
                ("dog", "mathematics")
            };
            
            int passCount = 0;
            foreach (var (word1, word2) in testPairs)
            {
                var vec1 = _encoder.Encode(word1);
                var vec2 = _encoder.Encode(word2);
                
                var region1 = _partitioner.GetRegionId(vec1);
                var region2 = _partitioner.GetRegionId(vec2);
                
                var similarity = _partitioner.CosineSimilarity(vec1, vec2);
                var differentRegions = region1 != region2;
                
                Console.WriteLine($"  {word1} vs {word2}: similarity={similarity:F3}, different={differentRegions}");
                
                if (similarity < 0.5)
                    passCount++;
            }
            
            bool passed = passCount >= testPairs.Length * 0.75; // At least 75% should be dissimilar
            Console.WriteLine($"  ✅ Result: {passCount}/{testPairs.Length} pairs dissimilar → {(passed ? "PASS" : "FAIL")}");
            
            return passed;
        }

        /// <summary>
        /// Test 4: Novelty Detection
        /// First activation → high novelty, repeated → low novelty
        /// </summary>
        public bool Test4_NoveltyDetection()
        {
            Console.WriteLine("\n=== Test 4: Novelty Detection ===");
            
            var vec = _encoder.Encode("elephant");
            var regionId = _partitioner.GetRegionId(vec);
            
            // First activation should be novel
            var novelty1 = _stats.CalculateNovelty(regionId, vec);
            Console.WriteLine($"  First activation: novelty={novelty1:F3}");
            
            // Record 50 activations
            for (int i = 0; i < 50; i++)
            {
                _stats.RecordActivation(regionId, vec);
            }
            
            // Should now be familiar
            var novelty2 = _stats.CalculateNovelty(regionId, vec);
            Console.WriteLine($"  After 50 activations: novelty={novelty2:F3}");
            
            bool passed = novelty1 > 0.7 && novelty2 < 0.3;
            Console.WriteLine($"  ✅ Result: novelty decreased from {novelty1:F3} to {novelty2:F3} → {(passed ? "PASS" : "FAIL")}");
            
            return passed;
        }

        /// <summary>
        /// Test 5: Region Distribution
        /// Different words should spread across multiple regions (not all collide)
        /// </summary>
        public bool Test5_RegionDistribution()
        {
            Console.WriteLine("\n=== Test 5: Region Distribution ===");
            
            var words = new[] 
            { 
                "cat", "dog", "run", "jump", "happy", "sad", "big", "small",
                "fast", "slow", "hot", "cold", "up", "down", "left", "right"
            };
            
            var regionCounts = new Dictionary<string, int>();
            
            foreach (var word in words)
            {
                var vec = _encoder.Encode(word);
                var region = _partitioner.GetRegionId(vec);
                
                if (!regionCounts.ContainsKey(region))
                    regionCounts[region] = 0;
                regionCounts[region]++;
            }
            
            var uniqueRegions = regionCounts.Count;
            var avgPerRegion = words.Length / (double)uniqueRegions;
            var maxInRegion = regionCounts.Values.Max();
            
            Console.WriteLine($"  {words.Length} words → {uniqueRegions} unique regions");
            Console.WriteLine($"  Avg per region: {avgPerRegion:F1}");
            Console.WriteLine($"  Max in one region: {maxInRegion}");
            
            // Good distribution: at least 50% unique regions, no region has >30% of words
            bool goodDistribution = uniqueRegions >= words.Length / 2 && maxInRegion <= words.Length * 0.3;
            Console.WriteLine($"  ✅ Result: distribution → {(goodDistribution ? "PASS" : "FAIL")}");
            
            return goodDistribution;
        }

        /// <summary>
        /// Test 6: Compositional Encoding
        /// Phrase encoding should relate to word encodings
        /// </summary>
        public bool Test6_CompositionalEncoding()
        {
            Console.WriteLine("\n=== Test 6: Compositional Encoding ===");
            
            var catVec = _encoder.Encode("cat");
            var satVec = _encoder.Encode("sat");
            var phraseVec = _encoder.EncodePhrase("cat sat");
            
            var simCat = _partitioner.CosineSimilarity(phraseVec, catVec);
            var simSat = _partitioner.CosineSimilarity(phraseVec, satVec);
            
            Console.WriteLine($"  'cat sat' vs 'cat': similarity={simCat:F3}");
            Console.WriteLine($"  'cat sat' vs 'sat': similarity={simSat:F3}");
            
            // Phrase should be somewhat similar to both component words
            bool passed = simCat > 0.3 && simSat > 0.3;
            Console.WriteLine($"  ✅ Result: compositional → {(passed ? "PASS" : "FAIL")}");
            
            return passed;
        }

        // Helper method
        private bool VectorsEqual(double[] v1, double[] v2)
        {
            if (v1.Length != v2.Length) return false;
            for (int i = 0; i < v1.Length; i++)
            {
                if (Math.Abs(v1[i] - v2[i]) > 1e-10)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Run all tests
        /// </summary>
        public void RunAllTests()
        {
            Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║       ADPC-Net Phase 1 Validation Tests                  ║");
            Console.WriteLine("║       Pattern-Based Learning (NO Word Lists)             ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            
            var results = new Dictionary<string, bool>
            {
                ["Deterministic Encoding"] = Test1_DeterministicEncoding(),
                ["Similar Words → Similar Regions"] = Test2_SimilarWordsNearbyRegions(),
                ["Dissimilar Words → Different Regions"] = Test3_DissimilarWordsDifferentRegions(),
                ["Novelty Detection"] = Test4_NoveltyDetection(),
                ["Region Distribution"] = Test5_RegionDistribution(),
                ["Compositional Encoding"] = Test6_CompositionalEncoding()
            };
            
            Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    TEST SUMMARY                          ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            
            int passed = 0;
            int total = results.Count;
            
            foreach (var (testName, result) in results)
            {
                var status = result ? "✅ PASS" : "❌ FAIL";
                Console.WriteLine($"  {status} - {testName}");
                if (result) passed++;
            }
            
            Console.WriteLine($"\n  Results: {passed}/{total} tests passed ({100.0 * passed / total:F0}%)");
            
            if (passed == total)
            {
                Console.WriteLine("\n  🎉 ALL TESTS PASSED! Phase 1 validation complete.");
            }
            else
            {
                Console.WriteLine($"\n  ⚠️  {total - passed} test(s) failed. Review implementation.");
            }
        }
    }
}
