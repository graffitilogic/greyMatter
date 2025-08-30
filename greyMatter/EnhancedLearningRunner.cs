using System;
using System.Threading.Tasks;
using System.IO;

namespace GreyMatter
{
    /// <summary>
    /// Runner for the Enhanced Language Learning System (Phase 4)
    /// </summary>
    public class EnhancedLearningRunner
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 **ENHANCED LANGUAGE LEARNING RUNNER - PHASE 4**");
            Console.WriteLine("==================================================");

            // Configuration
            var dataPath = Path.Combine(Directory.GetCurrentDirectory(), "learning_datasets", "learning_data");
            var brainPath = Path.Combine(Directory.GetCurrentDirectory(), "brain_data");

            // Parse command line arguments
            var targetVocabularySize = 5000; // Default 5,000 words
            var batchSize = 500; // Default batch size
            var maxConcurrency = 4; // Default concurrency

            if (args.Length > 0 && int.TryParse(args[0], out var customTarget))
            {
                targetVocabularySize = customTarget;
            }

            if (args.Length > 1 && int.TryParse(args[1], out var customBatch))
            {
                batchSize = customBatch;
            }

            if (args.Length > 2 && int.TryParse(args[2], out var customConcurrency))
            {
                maxConcurrency = customConcurrency;
            }

            Console.WriteLine($"Configuration:");
            Console.WriteLine($"- Target Vocabulary: {targetVocabularySize:N0} words");
            Console.WriteLine($"- Batch Size: {batchSize} words");
            Console.WriteLine($"- Max Concurrency: {maxConcurrency} threads");
            Console.WriteLine($"- Data Path: {dataPath}");
            Console.WriteLine($"- Brain Path: {brainPath}");

            // Validate paths
            if (!Directory.Exists(dataPath))
            {
                Console.WriteLine($"❌ Data path not found: {dataPath}");
                Console.WriteLine("Please ensure learning data exists. Run TatoebaDataConverter first.");
                return;
            }

            if (!Directory.Exists(brainPath))
            {
                Directory.CreateDirectory(brainPath);
                Console.WriteLine($"📁 Created brain data directory: {brainPath}");
            }

            // Initialize enhanced learner
            var learner = new EnhancedLanguageLearner(dataPath, brainPath, maxConcurrency);

            try
            {
                // Execute learning
                await learner.LearnVocabularyAtScaleAsync(targetVocabularySize, batchSize);

                Console.WriteLine("\n🎉 **PHASE 4 VOCABULARY EXPANSION COMPLETE**");
                Console.WriteLine($"Successfully scaled vocabulary to {targetVocabularySize:N0}+ words!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ **LEARNING FAILED**");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
    }
}
