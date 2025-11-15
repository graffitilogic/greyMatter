using System;
using System.Collections.Generic;
using System.Linq;
using GreyMatter.Core;

namespace GreyMatter.Validation
{
    /// <summary>
    /// ADPC-Net Phase 4: Validation tests for VQ-VAE codebook
    /// 
    /// Phase 4 Objectives:
    /// - Replace fixed LSH with learned vector quantization
    /// - Codebook adapts to data distribution
    /// - Better feature space packing than random hashing
    /// - Perplexity tracking ensures codebook utilization
    /// 
    /// Success Criteria:
    /// ✅ Codebook learns from data (not random)
    /// ✅ Similar inputs map to same/nearby codes
    /// ✅ Perplexity indicates good codebook usage
    /// ✅ EMA updates stabilize codebook
    /// ✅ Export/import preserves learned structure
    /// ✅ Commitment loss prevents encoder drift
    /// </summary>
    public class AdpcPhase4ValidationTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ADPC-Net Phase 4 Validation Tests ===");
            Console.WriteLine("Testing VQ-VAE codebook learning\n");

            int passed = 0;
            int total = 0;

            // Test 1: Codebook initialization
            total++;
            if (TestCodebookInitialization())
            {
                Console.WriteLine("✅ Test 1 PASSED: Codebook initialized correctly");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ Test 1 FAILED: Codebook initialization broken");
            }

            // Test 2: Quantization consistency
            total++;
            if (TestQuantizationConsistency())
            {
                Console.WriteLine("✅ Test 2 PASSED: Same input → same code (deterministic)");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ Test 2 FAILED: Quantization not deterministic");
            }

            // Test 3: Similar inputs cluster together
            total++;
            if (TestSimilarInputsClustering())
            {
                Console.WriteLine("✅ Test 3 PASSED: Similar inputs → nearby codes");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ Test 3 FAILED: Clustering not working");
            }

            // Test 4: EMA learning
            total++;
            if (TestEMALearning())
            {
                Console.WriteLine("✅ Test 4 PASSED: Codebook learns from data");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ Test 4 FAILED: Codebook not learning");
            }

            // Test 5: Perplexity tracking
            total++;
            if (TestPerplexityTracking())
            {
                Console.WriteLine("✅ Test 5 PASSED: Perplexity indicates codebook usage");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ Test 5 FAILED: Perplexity broken");
            }

            // Test 6: Export/import
            total++;
            if (TestExportImport())
            {
                Console.WriteLine("✅ Test 6 PASSED: Codebook persists correctly");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ Test 6 FAILED: Export/import broken");
            }

            Console.WriteLine($"\n=== Results: {passed}/{total} tests passed ({(100.0 * passed / total):F1}%) ===");
            
            if (passed == total)
            {
                Console.WriteLine("🎉 Phase 4: COMPLETE - All tests passing!");
                Console.WriteLine("\nNext: Phase 5 (Training dynamics) or production integration");
            }
            else
            {
                Console.WriteLine($"⚠️  Phase 4: {total - passed} test(s) still failing");
            }
        }

        /// <summary>
        /// Test 1: Verify codebook is initialized with random small values
        /// Success: All codebook vectors non-zero, small magnitude
        /// </summary>
        private static bool TestCodebookInitialization()
        {
            var vq = new VectorQuantizer(
                codebookSize: 64,
                embeddingDim: 16
            );

            // Get a few codebook vectors and check they're initialized
            bool allNonZero = true;
            bool allSmall = true;

            for (int i = 0; i < 10; i++)
            {
                var vec = vq.GetCodebookVector(i);
                
                // Check at least one element is non-zero
                bool hasNonZero = vec.Any(v => Math.Abs(v) > 0.0001f);
                if (!hasNonZero)
                    allNonZero = false;
                
                // Check all elements are small (< 0.1)
                bool allSmallVals = vec.All(v => Math.Abs(v) < 0.1f);
                if (!allSmallVals)
                    allSmall = false;
            }

            Console.WriteLine($"  All non-zero: {allNonZero}, All small: {allSmall}");

            return allNonZero && allSmall;
        }

        /// <summary>
        /// Test 2: Verify same input always maps to same code
        /// Success: Deterministic quantization
        /// </summary>
        private static bool TestQuantizationConsistency()
        {
            var vq = new VectorQuantizer(
                codebookSize: 128,
                embeddingDim: 32
            );

            // Create a random embedding
            var embedding = new float[32];
            for (int i = 0; i < 32; i++)
                embedding[i] = (float)Random.Shared.NextDouble();

            // Quantize multiple times
            int code1 = vq.Quantize(embedding);
            int code2 = vq.Quantize(embedding);
            int code3 = vq.Quantize(embedding);

            bool consistent = (code1 == code2) && (code2 == code3);

            Console.WriteLine($"  Codes: {code1}, {code2}, {code3} - Consistent: {consistent}");

            return consistent;
        }

        /// <summary>
        /// Test 3: Verify similar inputs map to same or nearby codes
        /// Success: Small perturbations → same code most of the time
        /// </summary>
        private static bool TestSimilarInputsClustering()
        {
            var vq = new VectorQuantizer(
                codebookSize: 256,
                embeddingDim: 64
            );

            // Create a base embedding
            var baseEmbedding = new float[64];
            for (int i = 0; i < 64; i++)
                baseEmbedding[i] = (float)Random.Shared.NextDouble();

            int baseCode = vq.Quantize(baseEmbedding);

            // Create 20 similar embeddings (small perturbations)
            int sameCodeCount = 0;
            for (int trial = 0; trial < 20; trial++)
            {
                var perturbed = new float[64];
                for (int i = 0; i < 64; i++)
                {
                    // Add small noise (±0.05)
                    perturbed[i] = baseEmbedding[i] + (float)(Random.Shared.NextDouble() * 0.1 - 0.05);
                }

                int perturbedCode = vq.Quantize(perturbed);
                if (perturbedCode == baseCode)
                    sameCodeCount++;
            }

            float sameCodeRate = sameCodeCount / 20.0f;
            bool goodClustering = sameCodeRate > 0.5f; // At least 50% map to same code

            Console.WriteLine($"  Same code rate: {sameCodeRate * 100:F1}% (should be > 50%)");

            return goodClustering;
        }

        /// <summary>
        /// Test 4: Verify codebook learns from data via EMA updates
        /// Success: Codebook vectors move toward data distribution
        /// </summary>
        private static bool TestEMALearning()
        {
            var vq = new VectorQuantizer(
                codebookSize: 128,
                embeddingDim: 32,
                emaDecay: 0.9f // Faster learning for testing
            );

            // Create a cluster of embeddings around a specific point
            var clusterCenter = new float[32];
            for (int i = 0; i < 32; i++)
                clusterCenter[i] = 0.5f; // All elements = 0.5

            // Find which code this cluster maps to initially
            int targetCode = vq.Quantize(clusterCenter);
            
            // Get initial codebook vector for the target code
            var initialVec = vq.GetCodebookVector(targetCode);

            // Train on 100 samples from this cluster
            for (int i = 0; i < 100; i++)
            {
                var sample = new float[32];
                for (int j = 0; j < 32; j++)
                {
                    // Add small noise around cluster center
                    sample[j] = clusterCenter[j] + (float)(Random.Shared.NextDouble() * 0.1 - 0.05);
                }
                
                // Update with EMA
                vq.QuantizeAndUpdate(sample);
            }

            // Get updated codebook vector for the target code
            var updatedVec = vq.GetCodebookVector(targetCode);

            // Check if codebook moved (at least one element changed significantly)
            bool codebookMoved = false;
            float maxChange = 0;
            for (int i = 0; i < 32; i++)
            {
                float change = Math.Abs(initialVec[i] - updatedVec[i]);
                if (change > maxChange)
                    maxChange = change;
                if (change > 0.001f)
                {
                    codebookMoved = true;
                }
            }

            Console.WriteLine($"  Codebook moved: {codebookMoved}, Max change: {maxChange:F6}");

            return codebookMoved;
        }

        /// <summary>
        /// Test 5: Verify perplexity calculation indicates codebook utilization
        /// Success: Perplexity between 1 and codebook_size
        /// </summary>
        private static bool TestPerplexityTracking()
        {
            var vq = new VectorQuantizer(
                codebookSize: 256,
                embeddingDim: 64
            );

            // Generate diverse embeddings to use multiple codes
            for (int i = 0; i < 1000; i++)
            {
                var embedding = new float[64];
                for (int j = 0; j < 64; j++)
                {
                    embedding[j] = (float)Random.Shared.NextDouble() * 2 - 1;
                }
                vq.Quantize(embedding);
            }

            var stats = vq.GetStats();

            bool validPerplexity = stats.Perplexity >= 1.0f && stats.Perplexity <= stats.CodebookSize;
            bool goodUtilization = stats.CodebookUtilization > 0.1f; // At least 10% of codes used
            bool hasActiveCodes = stats.ActiveCodes > 0;

            Console.WriteLine($"  Perplexity: {stats.Perplexity:F2}, Active codes: {stats.ActiveCodes}/{stats.CodebookSize}");
            Console.WriteLine($"  Utilization: {stats.CodebookUtilization * 100:F1}%");

            return validPerplexity && goodUtilization && hasActiveCodes;
        }

        /// <summary>
        /// Test 6: Verify export/import preserves codebook
        /// Success: Exported codebook matches when re-imported
        /// </summary>
        private static bool TestExportImport()
        {
            var vq1 = new VectorQuantizer(
                codebookSize: 128,
                embeddingDim: 32
            );

            // Train on some data
            for (int i = 0; i < 100; i++)
            {
                var embedding = new float[32];
                for (int j = 0; j < 32; j++)
                    embedding[j] = (float)Random.Shared.NextDouble();
                
                vq1.QuantizeAndUpdate(embedding);
            }

            // Export
            var snapshot = vq1.ExportCodebook();

            // Create new quantizer and import
            var vq2 = new VectorQuantizer(
                codebookSize: 128,
                embeddingDim: 32
            );
            vq2.ImportCodebook(snapshot);

            // Test that same embeddings map to same codes
            int matches = 0;
            for (int i = 0; i < 50; i++)
            {
                var embedding = new float[32];
                for (int j = 0; j < 32; j++)
                    embedding[j] = (float)Random.Shared.NextDouble();

                int code1 = vq1.Quantize(embedding);
                int code2 = vq2.Quantize(embedding);

                if (code1 == code2)
                    matches++;
            }

            float matchRate = matches / 50.0f;
            bool allMatch = matchRate == 1.0f;

            Console.WriteLine($"  Match rate: {matchRate * 100:F1}% (should be 100%)");

            return allMatch;
        }
    }
}
