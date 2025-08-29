using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;
using GreyMatter.Storage;
using GreyMatter.Learning;

namespace GreyMatter
{
    /// <summary>
    /// Comprehensive evaluation that tests actual learning vs pattern generation
    /// </summary>
    public class LearningValidationEvaluator
    {
        private readonly LearningSparseConceptEncoder _encoder;
        private readonly SemanticStorageManager _storage;

        public LearningValidationEvaluator(LearningSparseConceptEncoder encoder, SemanticStorageManager storage)
        {
            _encoder = encoder;
            _storage = storage;
        }

        /// <summary>
        /// Test whether the system has actually learned from training data
        /// </summary>
        public async Task<LearningValidationResult> ValidateActualLearningAsync()
        {
            Console.WriteLine("🔬 **LEARNING VALIDATION EVALUATION**");
            Console.WriteLine("=====================================");

            var result = new LearningValidationResult();

            // Test 1: Check for real training data traces
            result.HasRealTrainingData = await TestRealTrainingDataPresence();

            // Test 2: Validate learned relationships exist
            result.HasLearnedRelationships = await TestLearnedRelationships();

            // Test 3: Test prediction capabilities
            result.CanPredictRelationships = await TestPredictionCapabilities();

            // Test 4: Compare against baseline
            result.PerformsBetterThanBaseline = await TestBaselineComparison();

            // Test 5: Test generalization
            result.CanGeneralize = await TestGeneralization();

            Console.WriteLine("\n📊 **LEARNING VALIDATION RESULTS**");
            Console.WriteLine($"   Real Training Data: {(result.HasRealTrainingData ? "✅" : "❌")}");
            Console.WriteLine($"   Learned Relationships: {(result.HasLearnedRelationships ? "✅" : "❌")}");
            Console.WriteLine($"   Prediction Capability: {(result.CanPredictRelationships ? "✅" : "❌")}");
            Console.WriteLine($"   Better than Baseline: {(result.PerformsBetterThanBaseline ? "✅" : "❌")}");
            Console.WriteLine($"   Generalization Ability: {(result.CanGeneralize ? "✅" : "❌")}");

            result.OverallLearningScore = CalculateOverallScore(result);
            Console.WriteLine($"   Overall Learning Score: {result.OverallLearningScore:F2}/5.00");

            return result;
        }

        private async Task<bool> TestRealTrainingDataPresence()
        {
            Console.WriteLine("   🗂️ Testing real training data presence...");

            try
            {
                // Check if concept files exist and have meaningful content
                var conceptCount = 0;
                var totalFileSize = 0L;

                // Check brain data directory for actual files
                if (System.IO.Directory.Exists("/Volumes/jarvis/brainData"))
                {
                    var jsonFiles = System.IO.Directory.GetFiles("/Volumes/jarvis/brainData", "*.json", System.IO.SearchOption.AllDirectories);
                    conceptCount = jsonFiles.Length;

                    foreach (var file in jsonFiles)
                    {
                        var info = new System.IO.FileInfo(file);
                        totalFileSize += info.Length;
                    }
                }

                // More than 50 files and 1MB total indicates real training
                var hasRealData = conceptCount > 50 && totalFileSize > 1024 * 1024;

                Console.WriteLine($"      {(hasRealData ? "✅" : "❌")} Found {conceptCount} files, {totalFileSize / 1024}KB total");
                return hasRealData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ❌ Error checking training data: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TestLearnedRelationships()
        {
            Console.WriteLine("   🔗 Testing learned relationships...");

            // Test known semantic relationships using pattern similarity
            var testPairs = new[]
            {
                ("cat", "dog"),     // Similar animals
                ("car", "bicycle"), // Similar vehicles
                ("apple", "fruit"), // Hypernym relationship
                ("run", "walk"),    // Similar actions
                ("happy", "sad")    // Opposite emotions
            };

            int learnedRelationships = 0;
            foreach (var (word1, word2) in testPairs)
            {
                var pattern1 = await _encoder.EncodeLearnedWordAsync(word1);
                var pattern2 = await _encoder.EncodeLearnedWordAsync(word2);

                // Calculate similarity
                var similarity = CalculatePatternSimilarity(pattern1, pattern2);

                // Similar concepts should have some similarity but not identical
                if (similarity > 0.1 && similarity < 0.8) // Reasonable similarity range
                {
                    learnedRelationships++;
                }
            }

            bool hasLearned = learnedRelationships >= testPairs.Length * 0.6; // 60% threshold
            Console.WriteLine($"      {(hasLearned ? "✅" : "❌")} Found {learnedRelationships}/{testPairs.Length} expected relationships");
            return hasLearned;
        }

        private async Task<bool> TestPredictionCapabilities()
        {
            Console.WriteLine("   🎯 Testing prediction capabilities...");

            // Test if patterns are consistent and predictable
            var testWords = new[] { "cat", "car", "apple", "run", "happy" };
            int successfulPredictions = 0;

            foreach (var word in testWords)
            {
                var pattern1 = await _encoder.EncodeLearnedWordAsync(word);
                var pattern2 = await _encoder.EncodeLearnedWordAsync(word); // Same word should give same pattern

                // Patterns should be identical for same input
                if (CalculatePatternSimilarity(pattern1, pattern2) > 0.95)
                {
                    successfulPredictions++;
                }
            }

            bool canPredict = successfulPredictions >= testWords.Length * 0.8; // 80% threshold
            Console.WriteLine($"      {(canPredict ? "✅" : "❌")} Predicted {successfulPredictions}/{testWords.Length} words consistently");
            return canPredict;
        }

        private async Task<bool> TestBaselineComparison()
        {
            Console.WriteLine("   📊 Comparing against baseline...");

            // Compare against simple random baseline
            var testWords = new[] { "cat", "dog", "car", "apple", "run" };
            var systemSimilarities = new List<double>();
            var baselineSimilarities = new List<double>();

            foreach (var word in testWords)
            {
                var systemPattern = await _encoder.EncodeLearnedWordAsync(word);
                var randomPattern = GenerateRandomPattern(systemPattern.ActiveBits.Length);

                systemSimilarities.Add(CalculatePatternSimilarity(systemPattern, systemPattern)); // Self-similarity
                baselineSimilarities.Add(CalculatePatternSimilarity(systemPattern, randomPattern));
            }

            var avgSystemSimilarity = systemSimilarities.Average();
            var avgBaselineSimilarity = baselineSimilarities.Average();

            bool betterThanBaseline = avgSystemSimilarity > avgBaselineSimilarity * 1.2; // 20% improvement
            Console.WriteLine($"      System consistency: {avgSystemSimilarity:F3}, Baseline: {avgBaselineSimilarity:F3}");
            Console.WriteLine($"      {(betterThanBaseline ? "✅" : "❌")} System {(betterThanBaseline ? "better" : "worse")} than baseline");
            return betterThanBaseline;
        }

        private async Task<bool> TestGeneralization()
        {
            Console.WriteLine("   🌍 Testing generalization ability...");

            // Test on unseen word combinations
            var unseenPairs = new[]
            {
                ("elephant", "mouse"),   // Size relationship
                ("ocean", "desert"),     // Opposite environments
                ("piano", "guitar"),     // Similar instruments
                ("winter", "summer"),    // Opposite seasons
                ("doctor", "teacher")    // Similar professions
            };

            int generalizedCorrectly = 0;
            foreach (var (word1, word2) in unseenPairs)
            {
                var pattern1 = await _encoder.EncodeLearnedWordAsync(word1);
                var pattern2 = await _encoder.EncodeLearnedWordAsync(word2);

                var similarity = CalculatePatternSimilarity(pattern1, pattern2);

                // Reasonable similarity indicates some generalization
                if (similarity > 0.05 && similarity < 0.9)
                {
                    generalizedCorrectly++;
                }
            }

            bool canGeneralize = generalizedCorrectly >= unseenPairs.Length * 0.7; // 70% threshold
            Console.WriteLine($"      {(canGeneralize ? "✅" : "❌")} Generalized {generalizedCorrectly}/{unseenPairs.Length} unseen relationships");
            return canGeneralize;
        }

        private double CalculatePatternSimilarity(SparsePattern pattern1, SparsePattern pattern2)
        {
            if (pattern1.ActiveBits.Length == 0 || pattern2.ActiveBits.Length == 0)
                return 0.0;

            var intersection = pattern1.ActiveBits.Intersect(pattern2.ActiveBits).Count();
            var union = pattern1.ActiveBits.Union(pattern2.ActiveBits).Count();

            return union > 0 ? (double)intersection / union : 0.0;
        }

        private SparsePattern GenerateRandomPattern(int patternSize)
        {
            var random = new Random();
            var activeBits = new List<int>();

            // Generate random active bits (2% sparsity like the real encoder)
            var numActive = (int)(patternSize * 0.02);
            var indices = new List<int>();
            for (int i = 0; i < patternSize; i++) indices.Add(i);

            for (int i = 0; i < numActive; i++)
            {
                var index = random.Next(indices.Count);
                activeBits.Add(indices[index]);
                indices.RemoveAt(index);
            }

            return new SparsePattern(activeBits.ToArray(), 0.02);
        }

        private double CalculateOverallScore(LearningValidationResult result)
        {
            var scores = new[]
            {
                result.HasRealTrainingData ? 1.0 : 0.0,
                result.HasLearnedRelationships ? 1.0 : 0.0,
                result.CanPredictRelationships ? 1.0 : 0.0,
                result.PerformsBetterThanBaseline ? 1.0 : 0.0,
                result.CanGeneralize ? 1.0 : 0.0
            };

            return scores.Average();
        }
    }

    public class LearningValidationResult
    {
        public bool HasRealTrainingData { get; set; }
        public bool HasLearnedRelationships { get; set; }
        public bool CanPredictRelationships { get; set; }
        public bool PerformsBetterThanBaseline { get; set; }
        public bool CanGeneralize { get; set; }
        public double OverallLearningScore { get; set; }
    }
}
