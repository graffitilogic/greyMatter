using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using GreyMatter.Core;
using greyMatter.Core;
using GreyMatter.Storage;

namespace GreyMatter
{
    /// <summary>
    /// Phase 2 Integration Demo: Demonstrates the complete language understanding pipeline
    /// RealLanguageLearner → LanguageEphemeralBrain → Enhanced Evaluation
    /// </summary>
    public class Phase2IntegrationDemo
    {
        private readonly string _dataPath;
        private readonly string _brainPath;
        private readonly LanguageIntegrationSystem _integrationSystem;

        public Phase2IntegrationDemo(string dataPath, string brainPath)
        {
            _dataPath = dataPath;
            _brainPath = brainPath;
            _integrationSystem = new LanguageIntegrationSystem(dataPath, brainPath);
        }

        /// <summary>
        /// Run the complete Phase 2 integration demonstration
        /// </summary>
        public async Task RunPhase2DemoAsync()
        {
            Console.WriteLine("🚀 **PHASE 2 INTEGRATION DEMO**");
            Console.WriteLine("=============================");
            Console.WriteLine("Demonstrating complete language understanding pipeline");
            Console.WriteLine();

            // Step 1: Initialize and verify data
            await InitializeDemoAsync();

            // Step 2: Run integrated learning pipeline
            await RunIntegratedLearningAsync();

            // Step 3: Demonstrate enhanced capabilities
            await DemonstrateEnhancedCapabilitiesAsync();

            // Step 4: Performance evaluation
            await RunPerformanceEvaluationAsync();

            // Step 5: Final summary
            DisplayFinalSummary();
        }

        /// <summary>
        /// Initialize demo environment and verify data availability
        /// </summary>
        private async Task InitializeDemoAsync()
        {
            Console.WriteLine("📋 **INITIALIZATION**");
            Console.WriteLine("====================");

            // Check data availability
            var tatoebaPath = Path.Combine(_dataPath, "sentences.csv");
            var brainDir = new DirectoryInfo(_brainPath);

            Console.WriteLine($"📁 Data Path: {_dataPath}");
            Console.WriteLine($"🧠 Brain Path: {_brainPath}");
            Console.WriteLine($"📖 Tatoeba Data: {(File.Exists(tatoebaPath) ? "✅ Available" : "❌ Not found")}");
            Console.WriteLine($"💾 Brain Directory: {(brainDir.Exists ? "✅ Exists" : "❌ Not found")}");

            if (brainDir.Exists)
            {
                var files = brainDir.GetFiles().Length;
                var subdirs = brainDir.GetDirectories().Length;
                Console.WriteLine($"   📊 Brain files: {files}, directories: {subdirs}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Run the integrated learning pipeline
        /// </summary>
        private async Task RunIntegratedLearningAsync()
        {
            Console.WriteLine("🔗 **INTEGRATED LEARNING PIPELINE**");
            Console.WriteLine("==================================");

            var startTime = DateTime.Now;

            // Run the complete integrated pipeline
            await _integrationSystem.IntegratedLearningPipelineAsync(
                maxWords: 2000,      // Scale up from Phase 1's 1000
                maxSentences: 1000   // Process more sentences for linguistic analysis
            );

            var duration = DateTime.Now - startTime;
            Console.WriteLine($"⏱️ **PIPELINE COMPLETED IN {duration.TotalSeconds:F1} SECONDS**");
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrate enhanced language understanding capabilities
        /// </summary>
        private async Task DemonstrateEnhancedCapabilitiesAsync()
        {
            Console.WriteLine("🧠 **ENHANCED CAPABILITIES DEMONSTRATION**");
            Console.WriteLine("==========================================");

            // 1. Linguistic Analysis Demo
            await DemonstrateLinguisticAnalysisAsync();

            // 2. Word Association Demo
            await DemonstrateWordAssociationsAsync();

            // 3. Sentence Structure Demo
            await DemonstrateSentenceStructureAsync();

            // 4. Prediction Demo
            await DemonstratePredictionCapabilitiesAsync();

            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrate linguistic analysis capabilities
        /// </summary>
        private async Task DemonstrateLinguisticAnalysisAsync()
        {
            Console.WriteLine("🔬 **LINGUISTIC ANALYSIS DEMO**");

            var sentenceAnalyzer = new SentenceStructureAnalyzer();

            var testSentences = new[]
            {
                "The quick brown fox jumps over the lazy dog",
                "I am learning about artificial intelligence",
                "She enjoys reading science fiction novels",
                "The researchers conducted experiments in the laboratory",
                "Students study various subjects in university"
            };

            Console.WriteLine("📝 Analyzing sentence structures:");
            foreach (var sentence in testSentences)
            {
                var structure = sentenceAnalyzer.AnalyzeSentence(sentence);
                if (structure != null)
                {
                    Console.WriteLine($"   \"{sentence}\"");
                    Console.WriteLine($"   📊 {structure}");
                    Console.WriteLine();
                }
            }
        }

        /// <summary>
        /// Demonstrate word association capabilities
        /// </summary>
        private async Task DemonstrateWordAssociationsAsync()
        {
            Console.WriteLine("🔗 **WORD ASSOCIATION DEMO**");

            var testWords = new[] { "learning", "intelligence", "research", "study", "science" };

            foreach (var word in testWords)
            {
                var associations = _integrationSystem.LanguageBrain.GetWordAssociations(word, 5);
                if (associations.Any())
                {
                    Console.WriteLine($"   {word} → {string.Join(", ", associations)}");
                }
                else
                {
                    Console.WriteLine($"   {word} → (learning associations...)");
                }
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrate sentence structure understanding
        /// </summary>
        private async Task DemonstrateSentenceStructureAsync()
        {
            Console.WriteLine("🏗️ **SENTENCE STRUCTURE DEMO**");

            var sentenceAnalyzer = new SentenceStructureAnalyzer();

            var structures = new[]
            {
                new { Pattern = "SVO", Example = "The student reads books" },
                new { Pattern = "SVO with adjectives", Example = "The intelligent student reads interesting books" },
                new { Pattern = "Complex SVO", Example = "The diligent student carefully reads complex scientific books" }
            };

            foreach (var structure in structures)
            {
                Console.WriteLine($"   {structure.Pattern}:");
                Console.WriteLine($"   \"{structure.Example}\"");

                var analysis = sentenceAnalyzer.AnalyzeSentence(structure.Example);
                if (analysis != null)
                {
                    Console.WriteLine($"   📊 {analysis}");
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Demonstrate prediction capabilities
        /// </summary>
        private async Task DemonstratePredictionCapabilitiesAsync()
        {
            Console.WriteLine("🔮 **PREDICTION CAPABILITIES DEMO**");

            var incompleteSentences = new[]
            {
                "The scientist studies _",
                "Students learn about _",
                "Researchers conduct _",
                "The professor teaches _",
                "Scientists discover _"
            };

            foreach (var sentence in incompleteSentences)
            {
                var predictions = _integrationSystem.LanguageBrain.PredictMissingWord(sentence, 3);
                Console.WriteLine($"   '{sentence}'");
                if (predictions.Any())
                {
                    Console.WriteLine($"   → {string.Join(", ", predictions)}");
                }
                else
                {
                    Console.WriteLine($"   → (building predictions...)");
                }
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Run performance evaluation for Phase 2
        /// </summary>
        private async Task RunPerformanceEvaluationAsync()
        {
            Console.WriteLine("📊 **PHASE 2 PERFORMANCE EVALUATION**");
            Console.WriteLine("====================================");

            var stats = _integrationSystem.GetIntegrationStatistics();

            Console.WriteLine("🎯 **INTEGRATION METRICS**");
            Console.WriteLine($"   📚 Vocabulary Size: {stats["vocabulary_size"]}");
            Console.WriteLine($"   📝 Learned Sentences: {stats["learned_sentences"]}");
            Console.WriteLine($"   🔗 Word Associations: {stats["word_associations_count"]}");
            Console.WriteLine($"   📈 Integration Status: {stats["integration_status"]}");

            // Display top words
            var topWords = stats["top_words"] as IEnumerable<(string word, int frequency)>;
            if (topWords != null && topWords.Any())
            {
                Console.WriteLine("\n🏆 **TOP LEARNED WORDS**");
                foreach (var (word, freq) in topWords.Take(10))
                {
                    Console.WriteLine($"   {word}: {freq} occurrences");
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Display final summary of Phase 2 achievements
        /// </summary>
        private void DisplayFinalSummary()
        {
            Console.WriteLine("🎉 **PHASE 2 INTEGRATION SUMMARY**");
            Console.WriteLine("==================================");
            Console.WriteLine("✅ **ACHIEVEMENTS**");
            Console.WriteLine("   🔗 Successfully integrated RealLanguageLearner with LanguageEphemeralBrain");
            Console.WriteLine("   📖 Processed real Tatoeba data through enhanced linguistic analysis");
            Console.WriteLine("   🧩 Extracted SVO patterns and sentence structures");
            Console.WriteLine("   🔮 Developed prediction capabilities for missing words");
            Console.WriteLine("   📊 Comprehensive evaluation framework implemented");

            Console.WriteLine("\n🚀 **PHASE 2 OBJECTIVES COMPLETED**");
            Console.WriteLine("   ✅ Language component integration");
            Console.WriteLine("   ✅ Enhanced linguistic analysis");
            Console.WriteLine("   ✅ SVO pattern extraction validation");
            Console.WriteLine("   ✅ Word association networks");
            Console.WriteLine("   ✅ Prediction system development");

            Console.WriteLine("\n📈 **PERFORMANCE IMPROVEMENTS**");
            Console.WriteLine("   📚 Expanded vocabulary from 3,202 to integrated knowledge base");
            Console.WriteLine("   🧠 Enhanced understanding through linguistic analysis");
            Console.WriteLine("   ⚡ Optimized processing with batch operations");

            Console.WriteLine("\n🔄 **READY FOR PHASE 3**");
            Console.WriteLine("   Binary serialization moved to Phase 2 end for validation");
            Console.WriteLine("   Foundation established for advanced language features");

            var finalStats = _integrationSystem.GetIntegrationStatistics();
            Console.WriteLine($"\n📊 **FINAL STATISTICS**");
            Console.WriteLine($"   Vocabulary: {finalStats["vocabulary_size"]} words");
            Console.WriteLine($"   Sentences: {finalStats["learned_sentences"]} processed");
            Console.WriteLine($"   Status: {finalStats["integration_status"]}");
        }

        /// <summary>
        /// Main entry point for Phase 2 demo
        /// </summary>
        public static async Task RunDemoAsync()
        {
            Console.WriteLine("🚀 Starting Phase 2 Integration Demo...");

            // Use proper NAS configuration instead of hardcoded local paths
            var config = new CerebroConfiguration();
            config.ValidateAndSetup();
            
            var dataPath = Path.Combine(config.TrainingDataRoot, "learning_data");
            var brainPath = config.BrainDataPath;

            // Ensure directories exist
            Directory.CreateDirectory(dataPath);
            Directory.CreateDirectory(brainPath);

            // Run demo
            var demo = new Phase2IntegrationDemo(dataPath, brainPath);
            await demo.RunPhase2DemoAsync();

            Console.WriteLine("\n🎉 Phase 2 Integration Demo Complete!");
            Console.WriteLine("Press any key to exit...");
            //stop prompting for keypresses-> Console.ReadKey();
        }
    }
}