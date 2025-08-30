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
    /// Simple runner for the Learning Validation Evaluator
    /// </summary>
    public class LearningValidationTestRunner
    {
        // static async Task Main(string[] args)
        static async Task RunLearningValidationTest(string[] args)
        {
            Console.WriteLine("🧪 **LEARNING VALIDATION TEST RUNNER**");
            Console.WriteLine("=====================================");

            try
            {
                // Initialize components
                var encoder = new LearningSparseConceptEncoder();
                var storage = new SemanticStorageManager("/Volumes/jarvis/brainData");

                // Create validator
                var validator = new LearningValidationEvaluator(encoder, storage);

                // Run validation
                var result = await validator.ValidateActualLearningAsync();

                // Display results
                Console.WriteLine("\n🎯 **VALIDATION SUMMARY**");
                Console.WriteLine($"Real Training Data: {result.HasRealTrainingData}");
                Console.WriteLine($"Learned Relationships: {result.HasLearnedRelationships}");
                Console.WriteLine($"Prediction Capabilities: {result.CanPredictRelationships}");
                Console.WriteLine($"Better Than Baseline: {result.PerformsBetterThanBaseline}");
                Console.WriteLine($"Generalization: {result.CanGeneralize}");
                Console.WriteLine($"Overall Score: {result.OverallLearningScore:P2}");

                if (result.OverallLearningScore > 0.7)
                {
                    Console.WriteLine("✅ **EXCELLENT**: System demonstrates real learning capabilities!");
                }
                else if (result.OverallLearningScore > 0.4)
                {
                    Console.WriteLine("⚠️  **MODERATE**: Some learning detected, but needs improvement");
                }
                else
                {
                    Console.WriteLine("❌ **POOR**: System shows algorithmic pattern generation, not learning");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error running validation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
