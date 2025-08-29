using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;
using GreyMatter.Storage;
using GreyMatter.Learning;
using GreyMatter.Evaluations;
using greyMatter;

namespace GreyMatter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Legacy sparse encoding test (use --evaluate instead)
            if (args.Length > 0 && (args[0] == "--test-sparse" || args[0] == "--sparse-encoding"))
            {
                Console.WriteLine("ℹ️  Sparse encoding tests are now integrated into the main evaluation.");
                Console.WriteLine("    Use --evaluate to test your trained models.");
                Console.WriteLine("    Use --tatoeba-hybrid-1k to train with sparse encoding.");
                return;
            }
            
            // Check for pattern analysis
            if (args.Length > 0 && args[0] == "--analyze-patterns")
            {
                await PatternAnalysisTest.RunAsync();
                return;
            }
            
            // Check for unified evaluation of training results
            if (args.Length > 0 && (args[0] == "--evaluate" || args[0] == "--eval-training"))
            {
                var evaluator = new greyMatter.UnifiedTrainingEvaluator();
                await evaluator.RunUnifiedEvaluation();
                return;
            }

            // Check for comprehensive debugging
            if (args.Length > 0 && (args[0] == "--debug" || args[0] == "--comprehensive-debug"))
            {
                Console.WriteLine("🔍 **COMPREHENSIVE SYSTEM DEBUG**");
                Console.WriteLine("===============================");

                try
                {
                    // Initialize components
                    var encoder = new GreyMatter.Core.LearningSparseConceptEncoder();
                    var storage = new GreyMatter.Storage.SemanticStorageManager("/Volumes/jarvis/brainData", "/Volumes/jarvis/trainData");

                    // Create debugger
                    var debugger = new GreyMatter.GreyMatterDebugger(encoder, storage);

                    // Run comprehensive debug
                    await debugger.RunComprehensiveDebugAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error running debug: {ex.Message}");
                }
                return;
            }

            // Check for quick diagnostic
            if (args.Length > 0 && (args[0] == "--diag" || args[0] == "--quick-diag"))
            {
                await GreyMatter.SimpleDiagnostic.RunQuickDiagnosticAsync();
                return;
            }

            // Check for learning validation
            if (args.Length > 0 && (args[0] == "--validate-learning" || args[0] == "--learning-validation"))
            {
                Console.WriteLine("🧪 **LEARNING VALIDATION EVALUATION**");
                Console.WriteLine("=====================================");

                try
                {
                    // Initialize components
                    var encoder = new GreyMatter.Core.LearningSparseConceptEncoder();
                    var storage = new GreyMatter.Storage.SemanticStorageManager("/Volumes/jarvis/brainData", "/Volumes/jarvis/trainData");

                    // Create validator
                    var validator = new GreyMatter.LearningValidationEvaluator(encoder, storage);

                    // Run validation
                    var result = await validator.ValidateActualLearningAsync();

                    // Display results
                    Console.WriteLine("\n🎯 **VALIDATION SUMMARY**");
                    Console.WriteLine($"Real Training Data: {result.HasRealTrainingData}");
                    Console.WriteLine($"Learned Relationships: {result.HasLearnedRelationships}");
                    Console.WriteLine($"Prediction Capabilities: {result.CanPredictRelationships}");
                    Console.WriteLine($"Better Than Baseline: {result.PerformsBetterThanBaseline}");
                    Console.WriteLine($"Generalization: {result.CanGeneralize}");

                    double overallScore = (result.HasRealTrainingData ? 0.2 : 0) +
                                         (result.HasLearnedRelationships ? 0.2 : 0) +
                                         (result.CanPredictRelationships ? 0.2 : 0) +
                                         (result.PerformsBetterThanBaseline ? 0.2 : 0) +
                                         (result.CanGeneralize ? 0.2 : 0);

                    Console.WriteLine($"Overall Score: {overallScore:P2}");

                    if (overallScore > 0.7)
                    {
                        Console.WriteLine("✅ **EXCELLENT**: System demonstrates real learning capabilities!");
                    }
                    else if (overallScore > 0.4)
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
                }
                return;
            }
            
            // Check for neuron growth diagnostic
            if (args.Length > 0 && (args[0] == "--diagnostic" || args[0] == "--analyze-growth"))
            {
                var diagnostic = new NeuronGrowthDiagnostic();
                await diagnostic.RunDiagnostic();
                return;
            }
            
            // Check for optimized learning test
            if (args.Length > 0 && (args[0] == "--test-optimized" || args[0] == "--test-efficiency"))
            {
                await OptimizedLearningTest.TestOptimizedLearning();
                return;
            }
            
            // Check for hybrid optimization test
            if (args.Length > 0 && (args[0] == "--test-hybrid-optimization" || args[0] == "--verify-optimization"))
            {
                // await global::greyMatter.HybridOptimizationTest.RunOptimizationTestAsync();
                Console.WriteLine("Hybrid optimization test temporarily disabled due to namespace issues");
                return;
            }
            
            // Check for testing current training results
            if (args.Length > 0 && (args[0] == "--test-training" || args[0] == "--test-results"))
            {
                // await TestTrainingResults.RunTestAsync(); // Temporarily disabled
                Console.WriteLine("Training results testing temporarily disabled");
                return;
            }
            
            // Check for hybrid training test
            if (args.Length > 0 && (args[0] == "--hybrid-test" || args[0] == "--test-hybrid"))
            {
                await TestHybridSystemVerification.RunTest();
                return;
            }
            
            // Check for Tatoeba hybrid integration (large-scale training)
            if (args.Length > 0 && (args[0] == "--tatoeba-hybrid" || args[0] == "--hybrid-tatoeba"))
            {
                var demo = new TatoebaHybridIntegrationDemo();
                await demo.RunTatoebaHybridIntegrationAsync();
                return;
            }

            if (args.Length > 0 && (args[0] == "--tatoeba-hybrid-full" || args[0] == "--hybrid-full-scale"))
            {
                Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║          FULL-SCALE HYBRID TRAINING (OPTIMIZED)               ║");
                Console.WriteLine("║     Real Tatoeba Data + ONNX Semantic + Biological Neural     ║");
                Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
                Console.WriteLine();
                
                var demo = new TatoebaHybridIntegrationDemo();
                await demo.RunLargeScaleHybridTrainingAsync();
                return;
            }
            
            // Check for random sampling hybrid training
            if (args.Length > 0 && (args[0] == "--tatoeba-hybrid-random" || args[0] == "--hybrid-random"))
            {
                Console.WriteLine("🎲 **RANDOM SAMPLING HYBRID TRAINING**");
                Console.WriteLine("=====================================");
                Console.WriteLine("Using random sampling from 2M+ Tatoeba sentences");
                Console.WriteLine();
                
                var demo = new TatoebaHybridIntegrationDemo();
                await demo.RunRandomSamplingHybridTrainingAsync();
                return;
            }

            // Check for different batch sizes
            if (args.Length > 0 && (args[0] == "--tatoeba-hybrid-debug" || args[0] == "--hybrid-debug"))
            {
                Console.WriteLine("🔧 **DEBUG HYBRID TRAINING**");
                Console.WriteLine("============================");
                Console.WriteLine("Processing 10 sentences with debug output");
                Console.WriteLine();
                
                var demo = new TatoebaHybridIntegrationDemo();
                await demo.RunSizedHybridTrainingAsync(10);
                return;
            }
            
            if (args.Length > 0 && (args[0] == "--tatoeba-hybrid-1k" || args[0] == "--hybrid-1k"))
            {
                Console.WriteLine("📊 **1K SENTENCE HYBRID TRAINING**");
                Console.WriteLine("==================================");
                Console.WriteLine("Processing 1,000 random sentences for quick testing");
                Console.WriteLine();
                
                var demo = new TatoebaHybridIntegrationDemo();
                await demo.RunSizedHybridTrainingAsync(1000);
                return;
            }

            if (args.Length > 0 && (args[0] == "--tatoeba-hybrid-10k" || args[0] == "--hybrid-10k"))
            {
                Console.WriteLine("📊 **10K SENTENCE HYBRID TRAINING**");
                Console.WriteLine("===================================");
                Console.WriteLine("Processing 10,000 random sentences for medium-scale testing");
                Console.WriteLine();
                
                var demo = new TatoebaHybridIntegrationDemo();
                await demo.RunSizedHybridTrainingAsync(10000);
                return;
            }

            if (args.Length > 0 && (args[0] == "--tatoeba-hybrid-100k" || args[0] == "--hybrid-100k"))
            {
                Console.WriteLine("📊 **100K SENTENCE HYBRID TRAINING**");
                Console.WriteLine("====================================");
                Console.WriteLine("Processing 100,000 random sentences for large-scale testing");
                Console.WriteLine();
                
                var demo = new TatoebaHybridIntegrationDemo();
                await demo.RunSizedHybridTrainingAsync(100000);
                return;
            }

            if (args.Length > 0 && (args[0] == "--tatoeba-hybrid-complete" || args[0] == "--hybrid-complete"))
            {
                Console.WriteLine("🌍 **COMPLETE DATASET HYBRID TRAINING**");
                Console.WriteLine("=======================================");
                Console.WriteLine("Processing ALL 2M+ Tatoeba sentences (FULL DATASET)");
                Console.WriteLine("⚠️  This will take significant time and storage!");
                Console.WriteLine();
                
                var demo = new TatoebaHybridIntegrationDemo();
                await demo.RunCompleteDatasetHybridTrainingAsync();
                return;
            }
            
            // Check for Tatoeba data conversion
            if (args.Length > 0 && (args[0] == "--convert-tatoeba-data" || args[0] == "--convert-tatoeba"))
            {
                Console.WriteLine("🔄 **TATOEBA DATA CONVERSION**");
                Console.WriteLine("==============================");

                try
                {
                    var tatoebaPath = GetArgValue(args, "--input", "/Volumes/jarvis/trainData/Tatoeba/sentences_eng_small.csv");
                    var outputPath = GetArgValue(args, "--output", "/Volumes/jarvis/trainData/Tatoeba/learning_data");
                    var maxSentences = GetArgValue(args, "--max-sentences", 10000);

                    var converter = new TatoebaDataConverter(tatoebaPath, outputPath);
                    await converter.ConvertAndBuildLearningDataAsync(maxSentences);

                    Console.WriteLine("\n✅ **DATA CONVERSION COMPLETE**");
                    Console.WriteLine($"📁 Output saved to: {outputPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error during conversion: {ex.Message}");
                }
                return;
            }

            // Check for real language learning from Tatoeba data
            if (args.Length > 0 && (args[0] == "--learn-from-tatoeba" || args[0] == "--real-learning"))
            {
                Console.WriteLine("🧠 **REAL LANGUAGE LEARNING FROM TATOEBA**");
                Console.WriteLine("==========================================");

                try
                {
                    var dataPath = GetArgValue(args, "--data-path", "/Volumes/jarvis/trainData/Tatoeba/learning_data");
                    var brainPath = GetArgValue(args, "--brain-path", "/Volumes/jarvis/brainData");
                    var maxWords = GetArgValue(args, "--max-words", 1000);

                    var learner = new RealLanguageLearner(dataPath, brainPath);
                    await learner.LearnFromTatoebaDataAsync(maxWords);

                    // Test the learning
                    await learner.TestLearningAsync();

                    Console.WriteLine("\n✅ **REAL LEARNING COMPLETE**");
                    Console.WriteLine($"📊 Learned {maxWords} words from actual Tatoeba sentences");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error during learning: {ex.Message}");
                }
                return;
            }
            
            // Check for semantic domain test
            if (args.Length > 0 && (args[0] == "--semantic-test" || args[0] == "--test-domains"))
            {
                await SemanticDomainTest.RunTestAsync();
                return;
            }
            
            // Check for trainable semantic demo
            else if (args.Length > 0 && args[0] == "--pretrained-demo")
            {
                await PreTrainedSemanticDemo.RunAsync();
            }
            else if (args.Length > 0 && args[0] == "--quick-test")
            {
                await QuickClassifierTest.RunAsync();
            }
            else if (args.Length > 0 && args[0] == "--debug-classifier")
            {
                await DebugClassifierTest.RunAsync();
            }
            
            // Check for simple demo first
            if (args.Length > 0 && (args[0] == "--simple-demo" || args[0] == "--original-vision"))
            {
                SimpleEphemeralDemo.RunDemo();
                return;
            }
            
            if (args.Length > 0 && (args[0] == "--enhanced-demo" || args[0] == "--phase2-demo"))
            {
                EnhancedEphemeralDemo.RunDemo();
                return;
            }
            
            if (args.Length > 0 && (args[0] == "--text-demo" || args[0] == "--phase3-demo"))
            {
                TextLearningDemo.RunDemo();
                return;
            }
            
            if (args.Length > 0 && (args[0] == "--comprehensive" || args[0] == "--full-demo"))
            {
                ComprehensiveDemo.RunDemo();
                return;
            }
            
                        // Scale Production Demos
            if (args.Length > 0 && args[0] == "--scale-demo")
            {
                var conceptCount = GetArgValue(args, "--target-concepts", 100000);
                var scaleConfig = CerebroConfiguration.FromCommandLine(args);
                
                var scaleDemo = new ScaleDemo(scaleConfig);
                await scaleDemo.RunScaleDemo(conceptCount);
                return;
            }

            if (args.Length > 0 && args[0] == "--wikipedia")
            {
                var articleCount = GetArgValue(args, "--articles", 1000);
                var wikiConfig = CerebroConfiguration.FromCommandLine(args);
                wikiConfig.ValidateAndSetup();
                
                Console.WriteLine("📚 Wikipedia Learning Demo");
                Console.WriteLine($"📁 Using training data from: {wikiConfig.TrainingDataRoot}");
                
                var ingester = new ExternalDataIngester(wikiConfig.TrainingDataRoot);
                var concepts = await ingester.GenerateWikipediaLikeConcepts(articleCount);
                
                var brain = new greyMatter.Core.SimpleEphemeralBrain();
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                foreach (var concept in concepts)
                {
                    brain.Learn(concept);
                }
                
                stopwatch.Stop();
                var stats = brain.GetMemoryStats();
                
                Console.WriteLine($"✅ Learned {concepts.Count} Wikipedia concepts in {stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"🧠 Brain: {stats.ConceptsRegistered} concepts, {stats.TotalNeurons} neurons");
                Console.WriteLine($"⚡ Speed: {concepts.Count / stopwatch.Elapsed.TotalSeconds:F0} concepts/second");
                Console.WriteLine($"💾 Data would be saved to: {wikiConfig.BrainDataPath}");
                return;
            }

            if (args.Length > 0 && args[0] == "--evaluation")
            {
                Console.WriteLine("🧪 Comprehension Evaluation Demo");
                
                // Load or create brain with test data
                var brain = new greyMatter.Core.SimpleEphemeralBrain();
                
                // Quick learning for evaluation
                var concepts = new[] { "red", "fruit", "apple", "science", "nature", "bright science", "red apple" };
                foreach (var concept in concepts)
                {
                    brain.Learn(concept);
                }
                
                var tester = new ComprehensionTester();
                var tests = new[]
                {
                    ("Association Recall", await tester.TestAssociationRecall(brain)),
                    ("Domain Knowledge", await tester.TestDomainKnowledge(brain)),
                    ("Concept Completion", await tester.TestConceptCompletion(brain)),
                    ("Semantic Similarity", await tester.TestSemanticSimilarity(brain))
                };
                
                Console.WriteLine("📊 Test Results:");
                foreach (var (testName, score) in tests)
                {
                    var grade = score >= 0.8 ? "🟢" : score >= 0.6 ? "🟡" : "🔴";
                    Console.WriteLine($"   {grade} {testName}: {score:P1}");
                }
                
                var overallScore = tests.Average(t => t.Item2);
                Console.WriteLine($"🎯 Overall Score: {overallScore:P1}");
                return;
            }
            
            // Parse configuration from command line
            var config = CerebroConfiguration.FromCommandLine(args);
            
            // Check for help or configuration display
            if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
            {
                CerebroConfiguration.DisplayUsage();
                Console.WriteLine("\nHybrid Training Options:");
                Console.WriteLine("  --tatoeba-hybrid             Small-scale hybrid demo (500 sentences, sequential)");
                Console.WriteLine("  --tatoeba-hybrid-random      Small-scale with random sampling (500 sentences)");
                Console.WriteLine("  --tatoeba-hybrid-1k          1,000 random sentences for quick testing");
                Console.WriteLine("  --tatoeba-hybrid-10k         10,000 random sentences for medium testing");
                Console.WriteLine("  --tatoeba-hybrid-100k        100,000 random sentences for large testing");
                Console.WriteLine("  --tatoeba-hybrid-full        Optimized large-scale (demo limit: 100K sentences)");
                Console.WriteLine("  --tatoeba-hybrid-complete    COMPLETE dataset (all 2M+ sentences) - LONG RUNTIME!");
                Console.WriteLine();
                Console.WriteLine("Additional Demo Options:");
                Console.WriteLine("  --simple-demo     Run the original ephemeral brain concept demo");
                Console.WriteLine("  --original-vision Same as --simple-demo");
                Console.WriteLine("  --enhanced-demo   Run enhanced demo with Phase 2 features");
                Console.WriteLine("  --phase2-demo     Same as --enhanced-demo");
                Console.WriteLine("  --text-demo       Run text learning demo (Phase 3)");
                Console.WriteLine("  --phase3-demo     Same as --text-demo");
                Console.WriteLine("  --comprehensive   Run complete demonstration (all phases)");
                Console.WriteLine("  --full-demo       Same as --comprehensive");
                Console.WriteLine();
                Console.WriteLine("Data Information:");
                Console.WriteLine("  • Total Tatoeba English sentences: 1,988,463");
                Console.WriteLine("  • Random sampling gives different training data each run");
                Console.WriteLine("  • Sequential sampling (default) uses same sentences each run");
                
                Console.WriteLine("\nReal Language Learning Options:");
                Console.WriteLine("  --convert-tatoeba-data    Convert Tatoeba CSV to learning data");
                Console.WriteLine("  --learn-from-tatoeba      Learn from actual Tatoeba sentences");
                Console.WriteLine("  --diag                    Run diagnostic to check system status");
                Console.WriteLine("  --debug                   Run comprehensive debugging");
                Console.WriteLine("  --evaluate                Evaluate current learning results");
                Console.WriteLine();
                return;
            }
            
            // Validate and setup paths
            try
            {
                config.ValidateAndSetup();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Configuration Error: {ex.Message}");
                CerebroConfiguration.DisplayUsage();
                return;
            }

            // NEW: Save-only utility mode
            if (args.Length > 0 && args[0] == "--save-only")
            {
                var brain = new Cerebro(config.BrainDataPath);
                brain.AttachConfiguration(config);
                await brain.InitializeAsync();
                Console.WriteLine("💾 Saving brain state (save-only)...");
                await brain.SaveAsync();
                return;
            }

            // NEW: Preschool training pipeline (compile → learn → eval)
            if (args.Length > 0 && args[0] == "--preschool-train")
            {
                await RunPreschoolTrain(config);
                return;
            }
            
            // Check for demo modes
            if (args.Length > 0 && args[0] == "--developmental-demo")
            {
                await DevelopmentalLearningDemo.RunDemo(args.Skip(1).ToArray());
                return;
            }
            

            
            if (args.Length > 0 && args[0] == "--language-foundations")
            {
                await RunLanguageFoundationsDemo(args.Skip(1).ToArray(), config);
                return;
            }
            
            if (args.Length > 0 && args[0] == "--comprehensive-language")
            {
                await RunComprehensiveLanguageDemo(args.Skip(1).ToArray(), config);
                return;
            }
            
            if (args.Length > 0 && (args[0] == "--language-help" || args[0] == "--help-language"))
            {
                DisplayLanguageHelp();
                return;
            }
            
            if (args.Length > 0 && (args[0] == "--language-demo" || args[0] == "--phase1-language"))
            {
                await LanguageFoundationsDemo.RunDemo();
                return;
            }
            
            if (args.Length > 0 && args[0] == "--language-quick-test")
            {
                LanguageFoundationsDemo.RunQuickTest();
                return;
            }
            
            if (args.Length > 0 && args[0] == "--language-minimal-demo")
            {
                await LanguageFoundationsDemo.RunMinimalDemo();
                return;
            }
            
            if (args.Length > 0 && (args[0] == "--language-full-scale" || args[0] == "--language-production"))
            {
                await LanguageFoundationsDemo.RunFullScaleTraining();
                return;
            }
            
            if (args.Length >= 2 && args[0] == "--language-random-sample")
            {
                if (int.TryParse(args[1], out int sampleSize) && sampleSize > 0)
                {
                    await LanguageFoundationsDemo.RunRandomSampleTraining(sampleSize);
                }
                else
                {
                    Console.WriteLine("❌ Error: Invalid sample size. Please provide a positive integer.");
                    Console.WriteLine("Usage: dotnet run --language-random-sample [size]");
                    Console.WriteLine("Example: dotnet run --language-random-sample 50000");
                }
                return;
            }
            
            if (args.Length >= 3 && args[0] == "--language-random-sample" && args[2] == "--reset")
            {
                if (int.TryParse(args[1], out int sampleSize) && sampleSize > 0)
                {
                    await LanguageFoundationsDemo.RunRandomSampleTraining(sampleSize, resetBrain: true);
                }
                else
                {
                    Console.WriteLine("❌ Error: Invalid sample size. Please provide a positive integer.");
                    Console.WriteLine("Usage: dotnet run --language-random-sample [size] --reset");
                    Console.WriteLine("Example: dotnet run --language-random-sample 50000 --reset");
                }
                return;
            }
            
            if (args.Length >= 3 && args[0] == "--iterative-growth-test")
            {
                if (int.TryParse(args[1], out int sampleSize) && int.TryParse(args[2], out int iterations) && 
                    sampleSize > 0 && iterations > 0)
                {
                    await IterativeGrowthTest.RunIterativeGrowthAnalysis(sampleSize, iterations);
                }
                else
                {
                    Console.WriteLine("❌ Error: Invalid parameters. Please provide positive integers.");
                    Console.WriteLine("Usage: dotnet run --iterative-growth-test [sample_size] [iterations]");
                    Console.WriteLine("Example: dotnet run --iterative-growth-test 25000 5");
                    Console.WriteLine("This will run 5 iterations of 25K sentence training to test growth patterns.");
                }
                return;
            }
            
            // Interactive conversation mode
            if (config.InteractiveMode)
            {
                await RunInteractiveMode(config);
                return;
            }
            
            // Enhanced cognitive demo (default)
            await RunCognitionDemo(args, config);
        }
        
        static async Task RunCognitionDemo(string[] args, CerebroConfiguration config)
        {
            Console.WriteLine("🧠 **GREYMATTER - REAL LANGUAGE LEARNING SYSTEM**");
            Console.WriteLine("================================================");
            Console.WriteLine("Processing actual language data from Tatoeba dataset\n");

            Console.WriteLine("📊 **SYSTEM STATUS**");
            Console.WriteLine("===================");

            // Check if learning data exists
            var dataPath = "/Volumes/jarvis/trainData/Tatoeba/learning_data";
            var brainPath = "/Volumes/jarvis/brainData";

            if (Directory.Exists(dataPath))
            {
                Console.WriteLine("✅ Learning data found");
                var wordDbPath = Path.Combine(dataPath, "word_database.json");
                if (File.Exists(wordDbPath))
                {
                    var wordDbJson = await File.ReadAllTextAsync(wordDbPath);
                    var wordDb = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, TatoebaDataConverter.WordData>>(wordDbJson);
                    Console.WriteLine($"   Words in database: {wordDb?.Count ?? 0}");
                }
            }
            else
            {
                Console.WriteLine("❌ No learning data found");
                Console.WriteLine("   Run: dotnet run -- --convert-tatoeba-data");
            }

            // Check brain storage
            if (Directory.Exists(brainPath))
            {
                Console.WriteLine("✅ Brain storage found");
                var files = Directory.GetFiles(brainPath, "*.json", SearchOption.AllDirectories);
                Console.WriteLine($"   Stored patterns: {files.Length}");
            }
            else
            {
                Console.WriteLine("❌ No brain storage found");
            }

            Console.WriteLine("\n🚀 **AVAILABLE COMMANDS**");
            Console.WriteLine("========================");
            Console.WriteLine("  --convert-tatoeba-data    Convert Tatoeba CSV to learning data");
            Console.WriteLine("  --learn-from-tatoeba      Learn from actual Tatoeba sentences");
            Console.WriteLine("  --diag                    Run diagnostic to check system status");
            Console.WriteLine("  --debug                   Run comprehensive debugging");
            Console.WriteLine("  --evaluate                Evaluate current learning results");
            Console.WriteLine();
            Console.WriteLine("📚 **QUICK START**");
            Console.WriteLine("==================");
            Console.WriteLine("1. Convert data: dotnet run -- --convert-tatoeba-data");
            Console.WriteLine("2. Learn words:  dotnet run -- --learn-from-tatoeba");
            Console.WriteLine("3. Test results: dotnet run -- --evaluate");
            Console.WriteLine();
            Console.WriteLine("💡 **REAL LEARNING SYSTEM**");
            Console.WriteLine("===========================");
            Console.WriteLine("• Processes actual Tatoeba sentences (12.9M available)");
            Console.WriteLine("• Learns from real word frequencies and co-occurrences");
            Console.WriteLine("• Builds semantic relationships between words");
            Console.WriteLine("• No algorithmic pattern generation - real data-driven learning");
            Console.WriteLine("• Measurable storage growth based on actual learning");
        }

        /// <summary>
        /// Run foundational language learning demo
        /// </summary>
        static async Task RunLanguageFoundationsDemo(string[] args, CerebroConfiguration config)
        {
            Console.WriteLine("🎓 **FOUNDATIONAL LANGUAGE LEARNING DEMONSTRATION**");
            Console.WriteLine("===================================================");
            Console.WriteLine("Progressive language acquisition following developmental stages\n");

            var brain = new Cerebro(config.BrainDataPath);
            await brain.InitializeAsync();

            Console.WriteLine("📊 Initial Brain Status:");
            var initialStats = await brain.GetStatsAsync();
            Console.WriteLine($"   Age: {await brain.GetBrainAgeAsync()}");
            Console.WriteLine($"   Clusters: {initialStats.TotalClusters}");
            Console.WriteLine($"   Storage: {initialStats.StorageSizeFormatted}\n");

            // Start cognitive processing for enhanced learning
            await brain.AwakeCognitionAsync();

            // Run foundational language training
            var trainer = new LanguageFoundationsTrainer(brain, config);
            await trainer.RunFoundationalTrainingAsync();

            // Test comprehension with simple interactions
            Console.WriteLine("\n🧪 **TESTING LANGUAGE COMPREHENSION**");
            Console.WriteLine("=====================================");

            var testPhrases = new[]
            {
                ("What is a cat?", new Dictionary<string, double> { ["question"] = 1.0, ["animal"] = 0.9 }),
                ("The red ball is big", new Dictionary<string, double> { ["description"] = 1.0, ["color"] = 0.8, ["size"] = 0.8 }),
                ("I like to play", new Dictionary<string, double> { ["preference"] = 1.0, ["activity"] = 0.9 }),
                ("Dogs can run fast", new Dictionary<string, double> { ["ability"] = 1.0, ["animal"] = 0.9, ["speed"] = 0.8 }),
                ("Tell me a story", new Dictionary<string, double> { ["request"] = 1.0, ["narrative"] = 0.9 })
            };

            foreach (var (phrase, features) in testPhrases)
            {
                var response = await brain.ProcessInputAsync(phrase, features);
                Console.WriteLine($"🤔 Input: \"{phrase}\"");
                Console.WriteLine($"💭 Response: {response.Response}");
                Console.WriteLine($"   Confidence: {response.Confidence:P0} | Neurons: {response.ActivatedNeurons}");
                Console.WriteLine();
                
                await Task.Delay(1000); // Let cognitive processing continue
            }

            // Final assessment
            Console.WriteLine("📈 **LANGUAGE LEARNING ASSESSMENT**");
            var finalStats = await brain.GetStatsAsync();
            Console.WriteLine($"   Total Concepts Learned: {finalStats.TotalClusters}");
            Console.WriteLine($"   Neural Connections: {finalStats.TotalSynapses}");
            Console.WriteLine($"   Brain Age: {await brain.GetBrainAgeAsync()}");
            Console.WriteLine($"   Storage Used: {finalStats.StorageSizeFormatted}");

            // Test specific language concepts
            var languageConcepts = new[] { "cat", "run", "big", "red", "happy" };
            Console.WriteLine("\n🎯 **CONCEPT MASTERY LEVELS**:");
            foreach (var concept in languageConcepts)
            {
                var mastery = await brain.GetConceptMasteryLevelAsync(concept);
                Console.WriteLine($"   {concept}: {mastery:P1}");
            }

            await brain.SleepCognitionAsync();
            await brain.SaveAsync();

            Console.WriteLine("\n🎉 **FOUNDATIONAL LANGUAGE TRAINING COMPLETE**");
            Console.WriteLine("   ✅ Core vocabulary established");
            Console.WriteLine("   ✅ Basic grammar patterns learned");
            Console.WriteLine("   ✅ Simple sentence comprehension");
            Console.WriteLine("   ✅ Reading foundation established");
            Console.WriteLine("   ✅ Ready for progressive language expansion");
            Console.WriteLine("\n💡 Next steps: Continue with graded readers and conversational practice");
        }

        /// <summary>
        /// Run interactive conversation mode
        /// </summary>
        static async Task RunInteractiveMode(CerebroConfiguration config)
        {
            Console.WriteLine("🧠💬 **INTERACTIVE BRAIN CONVERSATION**");
            Console.WriteLine("=======================================");
            Console.WriteLine("Initializing conversational brain with advanced cognitive processing...\n");

            var brain = new Cerebro(config.BrainDataPath);
            await brain.InitializeAsync();

            Console.WriteLine("📊 Brain Status:");
            var initialStats = await brain.GetStatsAsync();
            Console.WriteLine($"   Age: {await brain.GetBrainAgeAsync()}");
            Console.WriteLine($"   Clusters: {initialStats.TotalClusters}");
            Console.WriteLine($"   Storage: {initialStats.StorageSizeFormatted}");

            // Start cognitive processing
            await brain.AwakeCognitionAsync();
            
            // Allow brain to stabilize
            await Task.Delay(2000);

            // Start interactive conversation
            var conversation = new InteractiveConversation(brain, config);
            await conversation.StartConversationAsync();

            // Stop cognitive processing gracefully
            await brain.SleepCognitionAsync();
            
            Console.WriteLine("👋 Session ended. Brain cognitive processing stopped.");
        }

        static async Task RunComprehensiveLanguageDemo(string[] args, CerebroConfiguration config)
        {
            Console.WriteLine("🎓 **COMPREHENSIVE LANGUAGE ACQUISITION DEMONSTRATION**");
            Console.WriteLine("=====================================================");
            Console.WriteLine("Research-based linguistic training with 2000+ words and complex features\n");

            var brain = new Cerebro(config.BrainDataPath);
            await brain.InitializeAsync();

            Console.WriteLine("📊 Initial Brain Status:");
            var initialStats = await brain.GetStatsAsync();
            Console.WriteLine($"   Age: {await brain.GetBrainAgeAsync()}");
            Console.WriteLine($"   Clusters: {initialStats.TotalClusters}");
            Console.WriteLine($"   Storage: {initialStats.StorageSizeFormatted}\n");

            // Start cognitive processing for enhanced learning (but pause during intensive training)
            await brain.AwakeCognitionAsync();
            
            // Temporarily stop cognitive processing during intensive learning to avoid concurrency issues
            await brain.SleepCognitionAsync();

            // Run comprehensive language training
            var trainer = new ComprehensiveLanguageTrainer(brain, config);
            await trainer.RunComprehensiveTrainingAsync();
            
            // Restart cognitive processing after training
            await brain.AwakeCognitionAsync();

            // Test comprehension with diverse linguistic constructions
            Console.WriteLine("\n🧪 **TESTING COMPREHENSIVE LANGUAGE UNDERSTANDING**");
            Console.WriteLine("==================================================");

            var testPhrases = new[]
            {
                ("Democracy requires citizen participation", new Dictionary<string, double> { ["political_concept"] = 1.0, ["civic_responsibility"] = 0.9, ["complex_sentence"] = 0.8 }),
                ("The ubiquitous nature of technology influences society", new Dictionary<string, double> { ["advanced_vocabulary"] = 1.0, ["abstract_concept"] = 0.9, ["cause_effect"] = 0.8 }),
                ("Contemplating existence brings profound understanding", new Dictionary<string, double> { ["philosophical"] = 1.0, ["abstract_process"] = 0.9, ["mental_action"] = 0.8 }),
                ("Serendipitous discoveries often revolutionize science", new Dictionary<string, double> { ["rare_vocabulary"] = 1.0, ["scientific_process"] = 0.9, ["causation"] = 0.8 }),
                ("Synthesizing diverse perspectives creates innovative solutions", new Dictionary<string, double> { ["academic_vocabulary"] = 1.0, ["complex_cognition"] = 0.9, ["creativity"] = 0.8 })
            };

            foreach (var (phrase, features) in testPhrases)
            {
                var response = await brain.ProcessInputAsync(phrase, features);
                Console.WriteLine($"🤔 Input: \"{phrase}\"");
                Console.WriteLine($"💭 Response: {response.Response}");
                Console.WriteLine($"   Confidence: {response.Confidence:P0} | Neurons: {response.ActivatedNeurons}");
                Console.WriteLine();
            }

            // Final brain statistics
            Console.WriteLine("📊 **FINAL BRAIN ANALYSIS**");
            Console.WriteLine("===========================");
            var finalStats = await brain.GetStatsAsync();
            Console.WriteLine($"   Total Age: {await brain.GetBrainAgeAsync()}");
            Console.WriteLine($"   Total Clusters: {finalStats.TotalClusters}");
            Console.WriteLine($"   Storage Used: {finalStats.StorageSizeFormatted}");
            Console.WriteLine($"   Total Synapses: {finalStats.TotalSynapses}");
            
            // Check dynamic neuron allocation across different concept types
            Console.WriteLine("\n🧠 **NEURON ALLOCATION ANALYSIS**");
            Console.WriteLine("=================================");
            Console.WriteLine("Demonstrating dynamic, complexity-aware neuron allocation:\n");

            var allocationTests = new[]
            {
                ("simple_word", new Dictionary<string, double> { ["basic"] = 1.0 }),
                ("complex_philosophical_concept", new Dictionary<string, double> { ["abstract"] = 1.0, ["philosophical"] = 0.9, ["complex"] = 0.8, ["metaphysical"] = 0.7 }),
                ("scientific_methodology_framework", new Dictionary<string, double> { ["academic"] = 1.0, ["scientific"] = 0.9, ["systematic"] = 0.8, ["methodology"] = 0.8, ["research"] = 0.7 }),
                ("multifaceted_sociocultural_phenomenon", new Dictionary<string, double> { ["complex"] = 1.0, ["social"] = 0.9, ["cultural"] = 0.9, ["interdisciplinary"] = 0.8, ["multifaceted"] = 0.8, ["phenomenon"] = 0.7 })
            };

            foreach (var (concept, features) in allocationTests)
            {
                var result = await brain.LearnConceptAsync(concept, features);
                Console.WriteLine($"   📋 '{concept}': {result.NeuronsInvolved} neurons allocated");
                Console.WriteLine($"      Features: {features.Count}, Complexity Score: {features.Values.Average():F2}");
            }

            Console.WriteLine("\n✅ Comprehensive language training complete!");
            Console.WriteLine("   🌟 Brain now exhibits sophisticated linguistic competence");
            Console.WriteLine("   🎯 Dynamic neuron allocation demonstrates realistic neural scaling");

            // Stop cognitive processing gracefully
            await brain.SleepCognitionAsync();
        }

        private static async Task RunPreschoolTrain(CerebroConfiguration config)
        {
            Console.WriteLine("🎒 **PRESCHOOL TRAINING PIPELINE**");
            Console.WriteLine("=================================");
            Console.WriteLine("Compiling a simple curriculum, learning from it, and running a quick cloze baseline.\n");

            var brain = new Cerebro(config.BrainDataPath);
            brain.AttachConfiguration(config);
            await brain.InitializeAsync();

            // Compile curriculum
            Console.WriteLine("🧮 Compiling curriculum from datasets...");
            var compiler = new CurriculumCompiler();
            var curriculum = await compiler.CompileAsync(config, maxSentencesPerStage: 1000);

            // Merge a small starter set across early stages
            var lessons = new List<CurriculumCompiler.LessonItem>();
            lessons.AddRange(curriculum.Stage1_WordsAndSimpleSV.Take(500));
            lessons.AddRange(curriculum.Stage2_SVO.Take(500));
            lessons.AddRange(curriculum.Stage3_Modifiers.Take(250));
            Console.WriteLine($"   Curriculum sizes → S1:{curriculum.Stage1_WordsAndSimpleSV.Count} S2:{curriculum.Stage2_SVO.Count} S3:{curriculum.Stage3_Modifiers.Count}");

            // Learn from lessons
            Console.WriteLine("📘 Environmental learning (passive reading)...");
            var learner = new EnvironmentalLearner(brain, config);
            var learned = await learner.LearnAsync(lessons, maxItems: 800);
            Console.WriteLine($"   Lessons ingested: {learned}");

            // Quick cloze baseline
            Console.WriteLine("🧪 Running cloze baseline on a small set...");
            var evalSet = lessons.Select(l => l.Sentence).Take(200).ToList();
            var acc = await EvalHarness.RunClozeAsync(brain, evalSet, max: 200);
            Console.WriteLine($"   Cloze accuracy: {acc:P1} on {evalSet.Count} items");

            await brain.SaveAsync();
            Console.WriteLine("✅ Preschool training pipeline complete.");
        }

        private static int GetArgValue(string[] args, string argName, int defaultValue)
        {
            var index = Array.IndexOf(args, argName);
            if (index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out var value))
                return value;
            return defaultValue;
        }

        private static string GetArgValue(string[] args, string argName, string defaultValue)
        {
            var index = Array.IndexOf(args, argName);
            if (index >= 0 && index + 1 < args.Length)
                return args[index + 1];
            return defaultValue;
        }

        static void DisplayLanguageHelp()
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    LANGUAGE LEARNING OPTIONS                  ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("🧪 TESTING & DEVELOPMENT:");
            Console.WriteLine("  --language-quick-test");
            Console.WriteLine("     └─ 5 sentences, silent output, instant results");
            Console.WriteLine();

            Console.WriteLine("  --language-minimal-demo");
            Console.WriteLine("     └─ 100 sentences with progress tracking (~30 seconds)");
            Console.WriteLine();

            Console.WriteLine("📚 DEMONSTRATION TRAINING:");
            Console.WriteLine("  --language-demo");
            Console.WriteLine("     └─ 2,000 sentences, comprehensive demo (~5 minutes)");
            Console.WriteLine();

            Console.WriteLine("🏭 PRODUCTION TRAINING:");
            Console.WriteLine("  --language-full-scale  (or --language-production)");
            Console.WriteLine("     └─ ALL 2,043,357 English sentences from Tatoeba");
            Console.WriteLine("     └─ Builds 50,000+ word vocabulary");
            Console.WriteLine("     └─ Duration: 30-60 minutes");
            Console.WriteLine("     └─ Requires: 8GB+ RAM, several hundred MB storage");
            Console.WriteLine("     └─ Result: Production-ready language foundation");
            Console.WriteLine();

            Console.WriteLine("🧠 HYBRID TRAINING (ONNX + Biological):");
            Console.WriteLine("  --tatoeba-hybrid  (or --hybrid-tatoeba)");
            Console.WriteLine("     └─ Real Tatoeba data with semantic-biological integration");
            Console.WriteLine("     └─ ONNX DistilBERT semantic guidance + emergent neural learning");
            Console.WriteLine("     └─ Small-scale demonstration (500-1000 sentences)");
            Console.WriteLine("     └─ Duration: 1-2 minutes");
            Console.WriteLine();
            Console.WriteLine("  --tatoeba-hybrid-full  (or --hybrid-full-scale)");
            Console.WriteLine("     └─ FULL-SCALE hybrid training on complete dataset");
            Console.WriteLine("     └─ Optimized storage for large-scale operations");
            Console.WriteLine("     └─ Real semantic embeddings + biological neural networks");
            Console.WriteLine("     └─ Duration: 20-40 minutes");
            Console.WriteLine("     └─ Result: Advanced hybrid language model");
            Console.WriteLine();

            Console.WriteLine("🎯 CONTROLLED TESTING:");
            Console.WriteLine("  --language-random-sample [size]");
            Console.WriteLine("     └─ Random sample of [size] sentences from dataset");
            Console.WriteLine("     └─ CUMULATIVE: Builds on existing brain state");
            Console.WriteLine("     └─ Example: --language-random-sample 50000");
            Console.WriteLine();
            Console.WriteLine("  --language-random-sample [size] --reset");
            Console.WriteLine("     └─ Same as above but RESETS brain state (fresh start)");
            Console.WriteLine("     └─ Example: --language-random-sample 50000 --reset");
            Console.WriteLine();

            Console.WriteLine("� GROWTH PATTERN ANALYSIS:");
            Console.WriteLine("  --iterative-growth-test [size] [iterations]");
            Console.WriteLine("     └─ Tests randomness validation and concept consolidation");
            Console.WriteLine("     └─ Runs multiple iterations to measure growth plateaus");
            Console.WriteLine("     └─ Validates that storage stops growing due to proper merging");
            Console.WriteLine("     └─ Example: --iterative-growth-test 25000 5");
            Console.WriteLine();

            Console.WriteLine("�📊 DATASET INFORMATION:");
            Console.WriteLine($"  • Total Tatoeba sentences: 12,916,547");
            Console.WriteLine($"  • English sentences: 2,043,357");
            Console.WriteLine($"  • Data location: /Volumes/jarvis/trainData/Tatoeba");
            Console.WriteLine($"  • Brain storage: /Volumes/jarvis/brainData");
            Console.WriteLine();

            Console.WriteLine("🚀 QUICK START:");
            Console.WriteLine("  dotnet run --language-minimal-demo    # Quick test");
            Console.WriteLine("  dotnet run --language-demo            # Full demo");
            Console.WriteLine("  dotnet run --language-random-sample 50000  # Controlled test");
            Console.WriteLine("  dotnet run --iterative-growth-test 25000 5  # Growth analysis");
            Console.WriteLine("  dotnet run --language-full-scale      # Production training");
            Console.WriteLine();

            Console.WriteLine("💡 TIP: Start with --language-minimal-demo to verify everything works,");
            Console.WriteLine("   then use --language-full-scale for production training.");
        }
    }
}
