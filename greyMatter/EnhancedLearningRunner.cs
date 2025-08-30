using System;
using System.Threading.Tasks;
using System.IO;
using GreyMatter.Core;

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

            // Use proper NAS configuration
            var config = new CerebroConfiguration();
            config.ValidateAndSetup();
            
            var dataPath = Path.Combine(config.TrainingDataRoot, "learning_data");
            var brainPath = config.BrainDataPath;

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

            // Initialize Cerebro brain with existing data
            var cerebro = new GreyMatter.Core.Cerebro(brainPath);
            await cerebro.InitializeAsync();
            Console.WriteLine("✅ Cerebro brain initialized with existing data");

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
