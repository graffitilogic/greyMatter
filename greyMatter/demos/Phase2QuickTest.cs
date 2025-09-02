using System;
using System.Threading.Tasks;
using GreyMatter.Core;
using greyMatter.Core;

namespace GreyMatter
{
    /// <summary>
    /// Quick Phase 2 Integration Test
    /// </summary>
    public class Phase2QuickTest
    {
        public static async Task RunQuickTestAsync()
        {
            Console.WriteLine("🚀 **PHASE 2 INTEGRATION QUICK TEST**");
            Console.WriteLine("====================================");

            try
            {
                // Configure paths
                var projectRoot = AppDomain.CurrentDomain.BaseDirectory;
                
                // Use proper NAS configuration instead of hardcoded local paths
                var config = new CerebroConfiguration();
                config.ValidateAndSetup();
                
                var dataPath = Path.Combine(config.TrainingDataRoot, "learning_data");
                var brainPath = config.BrainDataPath;

                Console.WriteLine($"📁 Data Path: {dataPath}");
                Console.WriteLine($"🧠 Brain Path: {brainPath}");

                // Create integration system
                var integration = new LanguageIntegrationSystem(dataPath, brainPath);

                // First, run the integrated learning pipeline to train the system
                Console.WriteLine("\n🚀 **INITIALIZING INTEGRATED LEARNING SYSTEM**");
                await integration.IntegratedLearningPipelineAsync(maxWords: 500, maxSentences: 1000);

                // Run quick linguistic analysis test
                await RunLinguisticAnalysisTestAsync(integration);

                // Run vocabulary integration test
                await RunVocabularyIntegrationTestAsync(integration);

                Console.WriteLine("\n✅ **PHASE 2 QUICK TEST COMPLETE**");
                Console.WriteLine($"📊 Vocabulary Size: {integration.VocabularySize}");
                Console.WriteLine($"📝 Learned Sentences: {integration.LearnedSentences}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static async Task RunLinguisticAnalysisTestAsync(LanguageIntegrationSystem integration)
        {
            Console.WriteLine("\n🔬 **LINGUISTIC ANALYSIS TEST**");

            var testSentences = new[]
            {
                "The cat sits on the mat",
                "I love eating apples",
                "She runs in the park",
                "The dog chases the ball",
                "We study computer science"
            };

            // Use the integrated system's SVO extraction test
            var accuracy = await integration.TestSVOExtractionAccuracyAsync(testSentences);
            
            Console.WriteLine($"🎯 **SVO EXTRACTION ACCURACY: {accuracy:F1}%**");
            
            // Show detailed results
            var sentenceAnalyzer = new SentenceStructureAnalyzer();
            foreach (var sentence in testSentences)
            {
                var structure = sentenceAnalyzer.AnalyzeSentence(sentence);
                if (structure != null && structure.HasCompleteStructure)
                {
                    Console.WriteLine($"   ✅ {sentence}");
                    Console.WriteLine($"      {structure}");
                }
                else
                {
                    Console.WriteLine($"   ⚠️ {sentence} (partial analysis)");
                    if (structure != null)
                    {
                        Console.WriteLine($"      {structure}");
                    }
                }
            }
        }

        private static async Task RunVocabularyIntegrationTestAsync(LanguageIntegrationSystem integration)
        {
            Console.WriteLine("\n📚 **VOCABULARY INTEGRATION TEST**");

            // Test word associations
            var testWords = new[] { "cat", "dog", "apple", "run", "study" };
            foreach (var word in testWords)
            {
                var associations = integration.LanguageBrain.GetWordAssociations(word, 3);
                if (associations.Any())
                {
                    Console.WriteLine($"   {word} → {string.Join(", ", associations)}");
                }
                else
                {
                    Console.WriteLine($"   {word} → (no associations learned yet)");
                }
            }

            // Test sentence completion
            var incompleteSentences = new[]
            {
                "The cat sits on the _",
                "I love eating _",
                "She _ in the park"
            };

            foreach (var sentence in incompleteSentences)
            {
                var predictions = integration.LanguageBrain.PredictMissingWord(sentence, 2);
                Console.WriteLine($"   '{sentence}' → {string.Join(", ", predictions)}");
            }
        }
    }
}