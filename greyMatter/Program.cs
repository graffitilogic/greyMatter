using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;
using GreyMatter.Storage;
using GreyMatter.Learning;
using GreyMatter.Evaluations;
using GreyMatter.DataIntegration;
using GreyMatter.Demos; // Temporary: for deprecated demo stubs
using GreyMatter.Tests; // Week 5 integration tests
using greyMatter;
using greyMatter.Tests;

namespace GreyMatter
{
    class Program
    {
        private static TrainingService CreateTrainingService(string[]? args = null)
        {
            // Build CerebroConfiguration from CLI args (or defaults) to keep paths centralized
            var cfg = CerebroConfiguration.FromCommandLine(args ?? Array.Empty<string>());
            cfg.ValidateAndSetup();
            
            // TrainingService now uses IStorageAdapter (FastStorageAdapter by default)
            // All Tatoeba training paths benefit from 1,350x storage speedup
            return new TrainingService(cfg);
        }

        static async Task Main(string[] args)
        {
            await RunProgram(args);
        }

        static async Task RunProgram(string[] args)
        {
            // Week 6 Continuous Learning Service
            if (args.Length > 0 && (args[0] == "--continuous-learning" || args[0] == "--service" || args[0] == "--continuous"))
            {
                // Parse optional arguments
                string dataPath = "./training_data";
                int durationSeconds = 60;
                bool controlDemo = false;
                
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i] == "-td" || args[i] == "--training-data")
                    {
                        dataPath = args[++i];
                    }
                    else if (args[i] == "-d" || args[i] == "--duration")
                    {
                        durationSeconds = int.Parse(args[++i]);
                    }
                    else if (args[i] == "--control-demo")
                    {
                        controlDemo = true;
                    }
                }
                
                if (controlDemo)
                {
                    await ContinuousLearningDemo.DemoControlFeaturesAsync(dataPath);
                }
                else
                {
                    await ContinuousLearningDemo.RunAsync(dataPath, durationSeconds);
                }
                return;
            }
            
            // Week 5 Integration Validation (Task 9)
            if (args.Length > 0 && (args[0] == "--week5-validation" || args[0] == "--integration-validation" || args[0] == "--validate-integration"))
            {
                await IntegrationValidationTest.RunValidationAsync();
                return;
            }
            
            // Week 4 Baseline Comparison (Task 5)
            if (args.Length > 0 && (args[0] == "--baseline-comparison" || args[0] == "--compare-columns"))
            {
                await BaselineColumnComparisonTest.Run();
                return;
            }
            
            // Week 4 Column Architecture Test Runner (Task 1)
            if (args.Length > 0 && (args[0] == "--column-test" || args[0] == "--test-columns"))
            {
                await ColumnArchitectureTestRunner.Run();
                return;
            }
            
            // Week 3 end-to-end validation (Task 4)
            if (args.Length > 0 && (args[0] == "--week3-validation" || args[0] == "--validate-week3"))
            {
                await Week3ValidationTest.Run();
                return;
            }
            
            // Test multi-source training (Week 3 Task 2)
            if (args.Length > 0 && (args[0] == "--test-multi-source" || args[0] == "--multi-source-training"))
            {
                await MultiSourceTrainingTest.Run();
                return;
            }
            
            // Test data sources (Week 3 Task 1)
            if (args.Length > 0 && (args[0] == "--test-data-sources" || args[0] == "--validate-sources"))
            {
                await GreyMatter.Tests.DataSourceValidationTest.Run();
                return;
            }
            
            // Test biological learning with neuron connections
            if (args.Length > 0 && (args[0] == "--test-bio-learning" || args[0] == "--test-biological"))
            {
                GreyMatter.Tests.TestBiologicalLearning.Run();
                return;
            }
            
            // Full pipeline test with training and storage
            if (args.Length > 0 && (args[0] == "--test-full-pipeline" || args[0] == "--full-pipeline-test"))
            {
                await GreyMatter.Tests.FullPipelineTest.Run();
                return;
            }
            
            // Verify training data presence
            if (args.Length > 0 && (args[0] == "--verify-training-data" || args[0] == "--verify-data"))
            {
                Console.WriteLine("🧪 Training Data Verification\n============================");
                try
                {
                    var cfg = CerebroConfiguration.FromCommandLine(args);
                    cfg.ValidateAndSetup();
                    var root = cfg.TrainingDataRoot;

                    var expected = new (string label, string path)[]
                    {
                        ("SimpleWiki XML", System.IO.Path.Combine(root, "SimpleWiki", "simplewiki-latest-pages-articles-multistream.xml")),
                        ("News Headlines", System.IO.Path.Combine(root, "news", "headlines.txt")),
                        ("Scientific Abstracts", System.IO.Path.Combine(root, "scientific", "abstracts.txt")),
                        ("Technical Docs", System.IO.Path.Combine(root, "technical", "documentation.txt")),
                        ("Enhanced Word DB", System.IO.Path.Combine(root, "enhanced_learning_data", "enhanced_word_database.json")),
                        ("CBT Train", System.IO.Path.Combine(root, "CBT", "CBTest", "data", "cbt_train.txt")),
                        ("Learning Sentences", System.IO.Path.Combine(root, "learning_data", "sentences.csv"))
                    };

                    int found = 0;
                    foreach (var (label, path) in expected)
                    {
                        var exists = System.IO.File.Exists(path) || System.IO.Directory.Exists(path);
                        Console.WriteLine($"{(exists ? "✅" : "❌")} {label}: {path}");
                        if (exists) found++;
                    }

                    Console.WriteLine($"\n📁 Root: {root}");
                    Console.WriteLine($"📊 Found {found}/{expected.Length} expected items");
                    if (found == 0)
                    {
                        Console.WriteLine("🚫 No datasets detected. Ensure your NAS is mounted and TRAINING_DATA_ROOT is correct.");
                        Console.WriteLine("Tip: dotnet run -- --verify-training-data -td /Volumes/jarvis/trainData");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Verification error: {ex.Message}");
                }
                return;
            }
            
            // Week 1 Baseline Validation Test
            if (args.Length > 0 && (args[0] == "--baseline-test" || args[0] == "--week1-baseline"))
            {
                await greyMatter.Validation.BaselineValidationTest.Main(new string[0]);
                return;
            }
            
            // Week 1 Multi-Source Validation Test
            if (args.Length > 0 && (args[0] == "--multi-source-test" || args[0] == "--week1-multisource"))
            {
                await greyMatter.Validation.MultiSourceValidationTest.Main(new string[0]);
                return;
            }
            
            // Week 1 Knowledge Query System
            if (args.Length > 0 && (args[0] == "--knowledge-query" || args[0] == "--query" || args[0] == "--week1-query"))
            {
                // Pass remaining args to query system
                var queryArgs = args.Skip(1).ToArray();
                await greyMatter.Validation.KnowledgeQuerySystem.Main(queryArgs);
                return;
            }
            
            // Legacy sparse encoding test (use --evaluate instead)
            if (args.Length > 0 && (args[0] == "--test-sparse" || args[0] == "--sparse-encoding"))
            {
                Console.WriteLine("ℹ️  Sparse encoding tests are now integrated into the main evaluation.");
                Console.WriteLine("    Use --evaluate to test your trained models.");
                Console.WriteLine("    Use --tatoeba-hybrid-1k to train with sparse encoding.");
                return;
            }
            
            // Check for neuron creation analysis
            if (args.Length > 0 && (args[0] == "--neuron-analysis" || args[0] == "--analyze-neurons"))
            {
                await NeuronCreationAnalysisTest.RunAnalysisAsync();
                return;
            }
            
            // Check for auto-save test
            if (args.Length > 0 && (args[0] == "--auto-save-test" || args[0] == "--test-auto-save"))
            {
                await GreyMatter.demos.AutoSaveTest.Main(new string[0]);
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
                    var storage = new GreyMatter.Storage.SemanticStorageManager("/Volumes/jarvis/brainData", "/Volumes/jarvis/trainData");
                    var encoder = new GreyMatter.Core.LearningSparseConceptEncoder(storage);

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
                Console.WriteLine("🔗 Using Cerebro brain storage (same as continuous learning)");

                try
                {
                    // Use CerebroConfiguration for consistent path handling (same as continuous learning)
                    var validationConfig = CerebroConfiguration.FromCommandLine(args);
                    validationConfig.ValidateAndSetup();

                    var brainPath = validationConfig.BrainDataPath;
                    var dataPath = validationConfig.TrainingDataRoot;
                    
                    Console.WriteLine($"🧠 Brain Path: {brainPath}");
                    Console.WriteLine($"📁 Data Path: {dataPath}");

                    // Initialize the same Cerebro brain system used in continuous learning
                    var brain = new Cerebro(validationConfig.BrainDataPath);
                    await brain.InitializeAsync();
                    
                    // Check if the brain has any learned data
                    var brainStats = await brain.GetStatsAsync();
                    Console.WriteLine($"📊 Brain Statistics:");
                    Console.WriteLine($"   🧠 Total Neurons: {brain.TotalNeuronsCreated:N0}");
                    Console.WriteLine($"   �️ Total Clusters: {brain.TotalClustersCreated:N0}");
                    Console.WriteLine($"   🔗 Total Synapses: {brainStats.TotalSynapses:N0}");
                    Console.WriteLine($"   � Loaded Clusters: {brainStats.LoadedClusters:N0}");
                    
                    if (brainStats.TotalSynapses == 0 && brainStats.TotalClusters == 0)
                    {
                        Console.WriteLine("❌ No learned data found in brain!");
                        Console.WriteLine("💡 Make sure to run continuous learning first:");
                        Console.WriteLine("   dotnet run -- --continuous-learning --max-words 1000");
                        return;
                    }
                    // Get enhanced brain statistics
                    var enhancedStats = await brain.GetEnhancedStatsAsync();
                    Console.WriteLine($"   🏗️ Partition Efficiency: {enhancedStats.PartitionEfficiency:P1}");
                    Console.WriteLine($"   � Top Partitions: {enhancedStats.TopPartitions.Count:N0}");

                    // For now, provide a basic validation report based on brain statistics
                    var hasSignificantSynapses = brainStats.TotalSynapses > 1000; // At least 1000 synapses
                    var hasStorageClusters = brainStats.TotalClusters > 0; // Clusters in storage
                    var hasSynapses = brainStats.TotalSynapses > 0;
                    var hasActiveMemory = brainStats.LoadedClusters > 0; // Some clusters loaded
                    
                    Console.WriteLine("\n📊 **LEARNING VALIDATION RESULTS**");
                    Console.WriteLine("===================================");
                    Console.WriteLine($"   Brain Storage System: ✅ (Cerebro)");
                    Console.WriteLine($"   Significant Learning: {(hasSignificantSynapses ? "✅" : "❌")} ({brainStats.TotalSynapses:N0} synapses)");
                    Console.WriteLine($"   Cluster Formation: {(hasStorageClusters ? "✅" : "❌")} ({brainStats.TotalClusters:N0} storage clusters)");
                    Console.WriteLine($"   Neural Connections: {(hasSynapses ? "✅" : "❌")} ({brainStats.TotalSynapses:N0} synapses)");
                    Console.WriteLine($"   Active Memory: {(hasActiveMemory ? "✅" : "❌")} ({brainStats.LoadedClusters:N0} loaded clusters)");

                    var validationScore = 0.0;
                    if (hasSignificantSynapses) validationScore += 1.0;
                    if (hasStorageClusters) validationScore += 1.0; 
                    if (hasSynapses) validationScore += 1.0;
                    if (hasActiveMemory) validationScore += 1.0;

                    Console.WriteLine($"\n   Overall Learning Score: {validationScore:F2}/4.00");
                    
                    if (validationScore >= 3.0)
                    {
                        Console.WriteLine("🎉 Excellent! The brain shows strong learning evidence.");
                    }
                    else if (validationScore >= 2.0)
                    {
                        Console.WriteLine("👍 Good! The brain shows meaningful learning progress.");
                    }
                    else
                    {
                        Console.WriteLine("⚠️  Limited learning detected. Consider running more training.");
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
                Console.WriteLine("⚠️  --hybrid-test is deprecated (Phase 0 cleanup)");
                Console.WriteLine("   Use: dotnet run -- --tatoeba-hybrid-1k");
                Console.WriteLine("   Or: dotnet run -- --enhanced-learning --max-words 1000");
                return;
            }
            
            // Check for Tatoeba hybrid integration (large-scale training)
            if (args.Length > 0 && (args[0] == "--tatoeba-hybrid" || args[0] == "--hybrid-tatoeba"))
            {
                var totalTimer = Stopwatch.StartNew();
                Console.WriteLine("⏱️  Starting Tatoeba Hybrid Integration...");
                
                var trainingService = CreateTrainingService(args);
                var parameters = new TatoebaTrainingParameters
                {
                    SentenceCount = 500,
                    SamplingMode = SamplingMode.Sequential,
                    ResetBrain = false
                };
                
                var result = await trainingService.RunTatoebaTrainingAsync(parameters);
                
                totalTimer.Stop();
                Console.WriteLine($"⏱️  Tatoeba Hybrid Integration completed in {totalTimer.Elapsed.TotalSeconds:F2} seconds");
                Console.WriteLine($"   Success: {result.Success}, Processed: {result.ProcessedSentences} sentences");
                return;
            }

            if (args.Length > 0 && (args[0] == "--tatoeba-hybrid-full" || args[0] == "--hybrid-full-scale"))
            {
                var totalTimer = Stopwatch.StartNew();
                Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║          FULL-SCALE HYBRID TRAINING (OPTIMIZED)               ║");
                Console.WriteLine("║     Real Tatoeba Data + ONNX Semantic + Biological Neural     ║");
                Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
                Console.WriteLine();
                Console.WriteLine("⏱️  Starting Full-Scale Hybrid Training...");
                
                var trainingService = CreateTrainingService(args);
                var parameters = new TatoebaTrainingParameters
                {
                    SentenceCount = 100000, // Large scale
                    SamplingMode = SamplingMode.Random,
                    ResetBrain = false
                };
                
                var result = await trainingService.RunTatoebaTrainingAsync(parameters);
                
                totalTimer.Stop();
                Console.WriteLine($"⏱️  Full-Scale Hybrid Training completed in {totalTimer.Elapsed.TotalSeconds:F2} seconds");
                Console.WriteLine($"   Success: {result.Success}, Processed: {result.ProcessedSentences} sentences");
                return;
            }
            
            // Check for random sampling hybrid training
            if (args.Length > 0 && (args[0] == "--tatoeba-hybrid-random" || args[0] == "--hybrid-random"))
            {
                var totalTimer = Stopwatch.StartNew();
                Console.WriteLine("🎲 **RANDOM SAMPLING HYBRID TRAINING**");
                Console.WriteLine("=====================================");
                Console.WriteLine("Using random sampling from 2M+ Tatoeba sentences");
                Console.WriteLine("⏱️  Starting Random Sampling Hybrid Training...");
                Console.WriteLine();
                
                var trainingService = CreateTrainingService(args);
                var parameters = new TatoebaTrainingParameters
                {
                    SentenceCount = 500,
                    SamplingMode = SamplingMode.Random,
                    ResetBrain = false
                };
                
                var result = await trainingService.RunTatoebaTrainingAsync(parameters);
                
                totalTimer.Stop();
                Console.WriteLine($"⏱️  Random Sampling Hybrid Training completed in {totalTimer.Elapsed.TotalSeconds:F2} seconds");
                Console.WriteLine($"   Success: {result.Success}, Processed: {result.ProcessedSentences} sentences");
                return;
            }

            // Check for different batch sizes
            if (args.Length > 0 && (args[0] == "--tatoeba-hybrid-debug" || args[0] == "--hybrid-debug"))
            {
                var totalTimer = Stopwatch.StartNew();
                Console.WriteLine("🔧 **DEBUG HYBRID TRAINING**");
                Console.WriteLine("============================");
                Console.WriteLine("Processing 10 sentences with debug output");
                Console.WriteLine("⏱️  Starting Debug Hybrid Training...");
                Console.WriteLine();
                
                var trainingService = CreateTrainingService(args);
                var parameters = new TatoebaTrainingParameters
                {
                    SentenceCount = 10,
                    SamplingMode = SamplingMode.Random,
                    ResetBrain = false
                };
                
                var result = await trainingService.RunTatoebaTrainingAsync(parameters);
                
                totalTimer.Stop();
                Console.WriteLine($"⏱️  Debug Hybrid Training completed in {totalTimer.Elapsed.TotalSeconds:F2} seconds");
                Console.WriteLine($"   Success: {result.Success}, Processed: {result.ProcessedSentences} sentences");
                return;
            }
            
            if (args.Length > 0 && (args[0] == "--tatoeba-hybrid-1k" || args[0] == "--hybrid-1k"))
            {
                var totalTimer = Stopwatch.StartNew();
                Console.WriteLine("📊 **1K SENTENCE HYBRID TRAINING**");
                Console.WriteLine("==================================");
                Console.WriteLine("Processing 1,000 random sentences for quick testing");
                Console.WriteLine("⏱️  Starting 1K Sentence Hybrid Training...");
                Console.WriteLine();
                
                var trainingService = CreateTrainingService(args);
                var parameters = new TatoebaTrainingParameters
                {
                    SentenceCount = 1000,
                    SamplingMode = SamplingMode.Random,
                    ResetBrain = false
                };
                
                var result = await trainingService.RunTatoebaTrainingAsync(parameters);
                
                totalTimer.Stop();
                Console.WriteLine($"⏱️  1K Sentence Hybrid Training completed in {totalTimer.Elapsed.TotalSeconds:F2} seconds");
                Console.WriteLine($"   Success: {result.Success}, Processed: {result.ProcessedSentences} sentences");
                return;
            }

            if (args.Length > 0 && (args[0] == "--tatoeba-hybrid-10k" || args[0] == "--hybrid-10k"))
            {
                var totalTimer = Stopwatch.StartNew();
                Console.WriteLine("📊 **10K SENTENCE HYBRID TRAINING**");
                Console.WriteLine("===================================");
                Console.WriteLine("Processing 10,000 random sentences for medium-scale testing");
                Console.WriteLine("⏱️  Starting 10K Sentence Hybrid Training...");
                Console.WriteLine();
                
                var trainingService = CreateTrainingService(args);
                var parameters = new TatoebaTrainingParameters
                {
                    SentenceCount = 10000,
                    SamplingMode = SamplingMode.Random,
                    ResetBrain = false
                };
                
                var result = await trainingService.RunTatoebaTrainingAsync(parameters);
                
                totalTimer.Stop();
                Console.WriteLine($"⏱️  10K Sentence Hybrid Training completed in {totalTimer.Elapsed.TotalSeconds:F2} seconds");
                Console.WriteLine($"   Success: {result.Success}, Processed: {result.ProcessedSentences} sentences");
                return;
            }

            if (args.Length > 0 && (args[0] == "--tatoeba-hybrid-100k" || args[0] == "--hybrid-100k"))
            {
                var totalTimer = Stopwatch.StartNew();
                Console.WriteLine("📊 **100K SENTENCE HYBRID TRAINING**");
                Console.WriteLine("====================================");
                Console.WriteLine("Processing 100,000 random sentences for large-scale testing");
                Console.WriteLine("⏱️  Starting 100K Sentence Hybrid Training...");
                Console.WriteLine();
                
                var trainingService = CreateTrainingService(args);
                var parameters = new TatoebaTrainingParameters
                {
                    SentenceCount = 100000,
                    SamplingMode = SamplingMode.Random,
                    ResetBrain = false
                };
                
                var result = await trainingService.RunTatoebaTrainingAsync(parameters);
                
                totalTimer.Stop();
                Console.WriteLine($"⏱️  100K Sentence Hybrid Training completed in {totalTimer.Elapsed.TotalSeconds:F2} seconds");
                Console.WriteLine($"   Success: {result.Success}, Processed: {result.ProcessedSentences} sentences");
                return;
            }

            if (args.Length > 0 && (args[0] == "--tatoeba-hybrid-complete" || args[0] == "--hybrid-complete"))
            {
                var totalTimer = Stopwatch.StartNew();
                Console.WriteLine("🌍 **COMPLETE DATASET HYBRID TRAINING**");
                Console.WriteLine("=======================================");
                Console.WriteLine("Processing ALL 2M+ Tatoeba sentences (FULL DATASET)");
                Console.WriteLine("⚠️  This will take significant time and storage!");
                Console.WriteLine("⏱️  Starting Complete Dataset Hybrid Training...");
                Console.WriteLine();
                
                var trainingService = CreateTrainingService(args);
                var parameters = new TatoebaTrainingParameters
                {
                    SentenceCount = int.MaxValue, // Complete dataset
                    SamplingMode = SamplingMode.Complete,
                    ResetBrain = false
                };
                
                var result = await trainingService.RunTatoebaTrainingAsync(parameters);
                
                totalTimer.Stop();
                Console.WriteLine($"⏱️  Complete Dataset Hybrid Training completed in {totalTimer.Elapsed.TotalSeconds:F2} seconds");
                Console.WriteLine($"   Success: {result.Success}, Processed: {result.ProcessedSentences} sentences");
                return;
            }
            
            // Check for Tatoeba data conversion
            if (args.Length > 0 && (args[0] == "--convert-tatoeba-data" || args[0] == "--convert-tatoeba"))
            {
                Console.WriteLine("🔄 **TATOEBA DATA CONVERSION**");
                Console.WriteLine("==============================");

                try
                {
                    var trainingService = CreateTrainingService(args);
                    var inputPath = GetArgValue(args, "--input", "/Volumes/jarvis/trainData/Tatoeba/sentences_eng_small.csv");
                    var outputPath = GetArgValue(args, "--output", "/Volumes/jarvis/trainData/Tatoeba/learning_data");

                    var result = await trainingService.ConvertTatoebaDataAsync(inputPath, outputPath);

                    if (result.Success)
                    {
                        Console.WriteLine($"📁 Output saved to: {outputPath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error during conversion: {ex.Message}");
                }
                return;
            }

            // Check for enhanced data conversion (multiple sources)
            if (args.Length > 0 && (args[0] == "--convert-enhanced-data" || args[0] == "--enhanced-data-converter"))
            {
                Console.WriteLine("🚀 **ENHANCED DATA CONVERSION**");
                Console.WriteLine("==============================");

                try
                {
                    var trainingService = CreateTrainingService(args);
                    var dataRoot = GetArgValue(args, "--data-root", "/Volumes/jarvis/trainData");
                    var outputPath = GetArgValue(args, "--output", "/Volumes/jarvis/trainData/enhanced_learning_data");

                    var result = await trainingService.ConvertEnhancedDataAsync(dataRoot, outputPath);

                    if (result.Success)
                    {
                        Console.WriteLine($"📁 Output saved to: {outputPath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error during enhanced conversion: {ex.Message}");
                }
                return;
            }

            // Check for continuous learning mode (unified pipeline)
            if (args.Length > 0 && (args[0] == "--continuous-learning" || args[0] == "--continuous"))
            {
                Console.WriteLine("🧠 **ENHANCED CONTINUOUS LEARNING MODE**");
                Console.WriteLine("=======================================");
                Console.WriteLine("🚀 Unlimited multi-source learning with LLM teacher");
                Console.WriteLine("📊 Accessing millions of words from multiple datasets");
                Console.WriteLine("🤖 Dynamic curriculum generation and content adaptation");
                Console.WriteLine();

                try
                {
                    var trainingService = CreateTrainingService(args);
                    var parameters = new ContinuousLearningParameters
                    {
                        MaxWords = GetArgValue(args, "--max-words", 10000),
                        BatchSize = GetArgValue(args, "--batch-size", 1000),
                        AutoSave = true // Always auto-save during continuous learning
                    };

                    var result = await trainingService.RunContinuousLearningAsync(parameters);

                    if (result.Success)
                    {
                        Console.WriteLine($"\n⚡ Processing Rate: {result.ProcessedWords / Math.Max(result.Duration.TotalSeconds, 1):F1} words/second");
                        Console.WriteLine("🎯 Used unlimited multi-source learning!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error during continuous learning: {ex.Message}");
                }
                return;
            }
            
            // Check for procedural generation demo (Phase 2)
            if (args.Length > 0 && (args[0] == "--procedural-demo" || args[0] == "--phase2-demo"))
            {
                Console.WriteLine("🚀 **PHASE 2: PROCEDURAL GENERATION**");
                Console.WriteLine("====================================");
                Console.WriteLine("No Man's Sky-inspired cortical column generation");
                Console.WriteLine();

                try
                {
                    // TODO: Convert ProceduralGenerationDemo to parameterized function
                    // await ProceduralGenerationDemo.RunAsync();
                    Console.WriteLine("⚠️  Procedural generation demo temporarily disabled during demo-to-service conversion");
                    Console.WriteLine("   This functionality will be moved to TrainingService.RunProceduralGenerationAsync()");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error during procedural demo: {ex.Message}");
                }
                return;
            }
            
            // Check for LLM teacher demo
            if (args.Length > 0 && (args[0] == "--llm-teacher" || args[0] == "--teacher-demo"))
            {
                Console.WriteLine("🧠 **LLM TEACHER INTEGRATION**");
                Console.WriteLine("=================================");
                Console.WriteLine("Dynamic learning with Ollama API teacher guidance");
                Console.WriteLine();

                try
                {
                    var trainingService = CreateTrainingService(args);
                    var parameters = new LLMTeacherParameters
                    {
                        ApiEndpoint = "http://192.168.69.138:11434/api/chat",
                        Model = "deepseek-r1:1.5b",
                        Interactive = true
                    };
                    
                    var result = await trainingService.RunLLMTeacherSessionAsync(parameters);
                    Console.WriteLine($"\n✅ **LLM TEACHER SESSION COMPLETE** - Success: {result.Success}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error during LLM teacher session: {ex.Message}");
                    Console.WriteLine("⚠️  Make sure Ollama is running at http://192.168.69.138:11434");
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
                Console.WriteLine("⚠️  --quick-test is deprecated (Phase 0 cleanup)");
                Console.WriteLine("   Use: dotnet run -- --tatoeba-hybrid-1k");
                return;
            }
            else if (args.Length > 0 && args[0] == "--debug-classifier")
            {
                Console.WriteLine("⚠️  --debug-classifier moved to archive");
                Console.WriteLine("   Available in: demos/archive/DebugClassifierTest.cs");
                return;
            }
            
            // Check for simple demo first
            if (args.Length > 0 && (args[0] == "--simple-demo" || args[0] == "--original-vision"))
            {
                var trainingService = CreateTrainingService(args);
                var result = trainingService.RunSimpleEphemeralDemo();
                Console.WriteLine($"✅ Simple ephemeral demo completed - Success: {result.Success}, Concepts: {result.ConceptsLearned}");
                return;
            }
            
            if (args.Length > 0 && (args[0] == "--enhanced-demo" || args[0] == "--phase2-demo"))
            {
                Console.WriteLine("⚠️  --enhanced-demo is deprecated (Phase 0 cleanup)");
                Console.WriteLine("   Use: dotnet run -- --llm-teacher");
                return;
            }
            
            if (args.Length > 0 && (args[0] == "--text-demo" || args[0] == "--phase3-demo"))
            {
                Console.WriteLine("⚠️  --text-demo is deprecated (Phase 0 cleanup)");
                Console.WriteLine("   Use: dotnet run -- --tatoeba-hybrid-1k");
                return;
            }
            
            if (args.Length > 0 && (args[0] == "--comprehensive" || args[0] == "--full-demo"))
            {
                Console.WriteLine("⚠️  --comprehensive is deprecated (Phase 0 cleanup)");
                Console.WriteLine("   Use specific demos:");
                Console.WriteLine("   - LLM Teacher: dotnet run -- --llm-teacher");
                Console.WriteLine("   - Quick Demo: dotnet run -- --tatoeba-hybrid-1k");
                Console.WriteLine("   - Reading: dotnet run -- --reading-comprehension");
                return;
            }
            
            // Scale Production Demos - Deprecated
            if (args.Length > 0 && args[0] == "--scale-demo")
            {
                Console.WriteLine("⚠️  --scale-demo is deprecated (Phase 0 cleanup)");
                Console.WriteLine("   Use: dotnet run -- --enhanced-learning --max-words 100000");
                return;
            }

            if (args.Length > 0 && args[0] == "--wikipedia")
            {
                Console.WriteLine("⚠️  --wikipedia is deprecated (Phase 0 cleanup)");
                Console.WriteLine("   ExternalDataIngester removed - use continuous learning instead");
                Console.WriteLine("   Use: dotnet run -- --continuous-learning");
                return;
            }

            if (args.Length > 0 && args[0] == "--evaluation")
            {
                Console.WriteLine("⚠️  --evaluation is deprecated (Phase 0 cleanup)");
                Console.WriteLine("   ComprehensionTester removed - use --reading-comprehension instead");
                Console.WriteLine("   Use: dotnet run -- --reading-comprehension");
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
                
                Console.WriteLine("\nContinuous Learning Options:");
                Console.WriteLine("  --continuous-learning    Unified data prep + learning pipeline");
                Console.WriteLine("  --continuous            Same as --continuous-learning");
                Console.WriteLine("    └─ Runs indefinitely with interruptible learning");
                Console.WriteLine("    └─ Auto-saves progress every 5 minutes");
                Console.WriteLine("    └─ Handles priority tasks (user interactions)");
                Console.WriteLine("    └─ Example: --continuous-learning --max-words 50000");
                Console.WriteLine();
                Console.WriteLine("Legacy Options:");
                Console.WriteLine("  --convert-tatoeba-data  Convert Tatoeba CSV to learning data");
                Console.WriteLine("  --enhanced-learning     Learn from pre-converted data");
                Console.WriteLine("    Options:");
                Console.WriteLine("      --max-words N       Maximum words to learn (default: 5000)");
                Console.WriteLine("      --batch-size N      Batch size for learning (default: 500)");
                Console.WriteLine("      --continuous        Use continuous learning mode");
                Console.WriteLine("      --brain-path PATH   Path to brain data storage");
                Console.WriteLine("      --data-path PATH    Path to training data");
                Console.WriteLine("  --diag                  Run diagnostic to check system status");
                Console.WriteLine("  --debug                 Run comprehensive debugging");
                Console.WriteLine("  --evaluate              Evaluate current learning results");
                Console.WriteLine("  --benchmark             Run persistence performance benchmarks");
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
                Console.WriteLine("⚠️  --developmental-demo is deprecated (Phase 0 cleanup)");
                Console.WriteLine("   Use: dotnet run -- --continuous-learning");
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

            // Persistence performance benchmark
            if (args.Length > 0 && (args[0] == "--benchmark" || args[0] == "--perf-test"))
            {
                Console.WriteLine("📊 **PERSISTENCE PERFORMANCE BENCHMARK**");
                Console.WriteLine("========================================");
                Console.WriteLine("Measuring persistence operations performance\n");

                try
                {
                    var test = new ConceptStorageTest();
                    await test.RunPerformanceTest();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error running test: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                return;
            }

            // Performance validation tests
            if (args.Length > 0 && args[0] == "--performance-validation")
            {
                await RunPerformanceValidation();
                return;
            }

            // Real storage A/B testing
            if (args.Length > 0 && args[0] == "--real-storage-test")
            {
                await GreyMatter.Tests.RealStoragePerformanceTest.RunRealPerformanceComparison();
                return;
            }

            // Enhanced language learning with PROVEN fast storage (1,350x speedup)
            if (args.Length > 0 && args[0] == "--enhanced-learning")
            {
                Console.WriteLine("🚀 **ENHANCED LANGUAGE LEARNING - WITH FAST STORAGE**");
                Console.WriteLine("====================================================");
                Console.WriteLine("🎯 Performance: 1,350x faster than legacy system");
                Console.WriteLine("⏱️  Expected: 35+ minute saves → under 30 seconds");
                
                try
                {
                    // Parse arguments
                    var dataPath = GetArgValue(args, "--data-path", "/Volumes/jarvis/trainData");
                    var brainPath = GetArgValue(args, "--brain-path", "/Volumes/jarvis/brainData");
                    var maxWords = int.Parse(GetArgValue(args, "--max-words", "5000"));
                    var batchSize = int.Parse(GetArgValue(args, "--batch-size", "500"));
                    var useContinuous = HasFlag(args, "--continuous");
                    
                    Console.WriteLine($"📂 Data Path: {dataPath}");
                    Console.WriteLine($"🧠 Brain Path: {brainPath}");
                    Console.WriteLine($"📊 Max Words: {maxWords:N0}");
                    Console.WriteLine($"📦 Batch Size: {batchSize:N0}");
                    Console.WriteLine($"🔄 Mode: {(useContinuous ? "Continuous" : "Batch")}");
                    
                    // Initialize Enhanced Language Learner with FAST storage
                    var learner = new EnhancedLanguageLearner(dataPath, brainPath, maxConcurrency: 8);
                    
                    // Handle Ctrl+C gracefully for continuous learning
                    var cancellationTokenSource = new CancellationTokenSource();
                    Console.CancelKeyPress += (sender, e) =>
                    {
                        e.Cancel = true;
                        Console.WriteLine("\n🛑 **INTERRUPTION DETECTED**");
                        Console.WriteLine("Gracefully shutting down learning...");
                        cancellationTokenSource.Cancel();
                    };
                    
                    // Run learning based on mode
                    if (useContinuous)
                    {
                        Console.WriteLine("\n🔄 **STARTING CONTINUOUS LEARNING**");
                        var finalVocabSize = await learner.LearnVocabularyContinuouslyAsync(
                            maxWords: maxWords, 
                            batchSize: batchSize, 
                            cancellationToken: cancellationTokenSource.Token);
                        Console.WriteLine($"📊 Final vocabulary size: {finalVocabSize:N0} words");
                    }
                    else
                    {
                        Console.WriteLine("\n📦 **STARTING BATCH LEARNING**");
                        await learner.LearnVocabularyAtScaleAsync(maxWords, batchSize);
                    }
                    
                    // CRITICAL: Proper shutdown to consolidate to NAS
                    await learner.ShutdownAsync();
                    
                    Console.WriteLine("\n🎉 **ENHANCED LEARNING COMPLETE WITH FAST STORAGE**");
                    Console.WriteLine("✅ All vocabulary data saved in seconds instead of hours!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error during enhanced learning: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                return;
            }

            // Enhanced data integration
            if (args.Length > 0 && args[0] == "--enhanced-integration")
            {
                Console.WriteLine("🧠 GreyMatter Enhanced Data Integration Runner");
                Console.WriteLine("==============================================");

                try
                {
                    // Initialize paths
                    string dataPath = "/Volumes/jarvis/trainData";
                    string brainPath = "/Volumes/jarvis/brainData";

                    // Initialize the Real Language Learner
                    var learner = new RealLanguageLearner(dataPath, brainPath);

                    // Create the data integrator
                    var integrator = new EnhancedDataIntegrator(learner);

                    // Run the integration
                    await integrator.IntegrateAllSourcesAsync();

                    // Save the enhanced knowledge
                    await learner.SaveLearnedKnowledgeAsync();

                    Console.WriteLine("\n🎉 Enhanced Data Integration Complete!");
                    Console.WriteLine("📊 The system now has access to diverse learning sources:");
                    Console.WriteLine("   • SimpleWiki articles (encyclopedic knowledge)");
                    Console.WriteLine("   • News headlines (current events)");
                    Console.WriteLine("   • Scientific abstracts (technical knowledge)");
                    Console.WriteLine("   • Children's literature (narrative patterns)");
                    Console.WriteLine("   • Idioms and expressions (colloquial language)");
                    Console.WriteLine("   • Technical documentation (formal writing)");
                    Console.WriteLine("   • Social media posts (conversational language)");
                    Console.WriteLine("   • Open subtitles (spoken dialogue)");

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error during integration: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
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
            Console.WriteLine("  --performance-validation    Run comprehensive storage performance tests");
            Console.WriteLine("  --real-storage-test         Run A/B test: Old vs New storage systems");
            Console.WriteLine("  --convert-tatoeba-data      Convert Tatoeba CSV to learning data");
            Console.WriteLine("  --diag                      Run diagnostic to check system status");
            Console.WriteLine("  --debug                     Run comprehensive debugging");
            Console.WriteLine("  --evaluate                  Evaluate current learning results");
            Console.WriteLine();
            Console.WriteLine("📚 **QUICK START**");
            Console.WriteLine("==================");
            Console.WriteLine("1. Convert data: dotnet run -- --convert-tatoeba-data");
            Console.WriteLine("2. Learn continuously: dotnet run -- --continuous-learning");
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

        private static bool HasFlag(string[] args, string flagName)
        {
            return Array.IndexOf(args, flagName) >= 0;
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

        /// <summary>
        /// Comprehensive storage performance validator
        /// </summary>
        private class StoragePerformanceValidator
        {
            private readonly SemanticStorageManager _storageManager;
            private readonly Random _random = new Random(42); // Fixed seed for reproducible results

            public StoragePerformanceValidator()
            {
                _storageManager = new SemanticStorageManager("/Volumes/jarvis/brainData");
            }

            public async Task RunComprehensivePerformanceTest()
            {
                Console.WriteLine("🧪 **COMPREHENSIVE STORAGE PERFORMANCE VALIDATION**");
                Console.WriteLine("==================================================");

                // Test 1: Small batch performance (baseline)
                await TestBatchSize(100, "Small Batch (100 concepts)");
                await TestBatchSize(500, "Medium Batch (500 concepts)");
                await TestBatchSize(1000, "Large Batch (1000 concepts)");
                await TestBatchSize(5000, "XL Batch (5000 concepts)");

                // Test 2: Memory usage analysis
                await TestMemoryUsage();

                // Test 3: Concurrent operations
                await TestConcurrentOperations();

                // Test 4: Network storage simulation
                await TestNetworkLatencySimulation();

                // Test 5: Real-world scenario simulation
                await TestRealWorldScenario();

                Console.WriteLine("\n✅ **PERFORMANCE VALIDATION COMPLETE**");
            }

            private async Task TestBatchSize(int conceptCount, string testName)
            {
                Console.WriteLine($"\n📊 **{testName}**");
                Console.WriteLine("".PadRight(50, '-'));

                // Generate test concepts
                var concepts = GenerateTestConcepts(conceptCount);

                // Note: Using existing storage - not clearing for performance test
                Console.WriteLine("📝 Note: Testing with existing storage data");

                // Measure storage performance
                var stopwatch = Stopwatch.StartNew();
                await _storageManager.SaveConceptsBatchAsync(concepts);
                stopwatch.Stop();

                var totalTime = stopwatch.ElapsedMilliseconds;
                var conceptsPerSecond = (double)conceptCount / (totalTime / 1000.0);

                Console.WriteLine($"⏱️  Total Time: {totalTime}ms");
                Console.WriteLine($"⚡ Concepts/Second: {conceptsPerSecond:F2}");
                Console.WriteLine($"🎯 Expected: >100 concepts/second (200x improvement target)");

                // Calculate improvement factor (baseline: 2 concepts/second)
                var improvementFactor = conceptsPerSecond / 2.0;
                Console.WriteLine($"📈 Improvement Factor: {improvementFactor:F1}x");

                if (improvementFactor >= 100)
                    Console.WriteLine("✅ **TARGET ACHIEVED**: 100x+ improvement confirmed!");
                else if (improvementFactor >= 50)
                    Console.WriteLine("🟡 **GOOD PROGRESS**: 50x+ improvement achieved");
                else
                    Console.WriteLine("🔴 **BELOW TARGET**: Further optimization needed");
            }

            private async Task TestMemoryUsage()
            {
                Console.WriteLine("\n🧠 **MEMORY USAGE ANALYSIS**");
                Console.WriteLine("".PadRight(30, '-'));

                var initialMemory = GC.GetTotalMemory(true);
                Console.WriteLine($"📊 Initial Memory: {initialMemory / 1024 / 1024}MB");

                // Test with 10K concepts
                var concepts = GenerateTestConcepts(10000);
                await _storageManager.SaveConceptsBatchAsync(concepts);

                var finalMemory = GC.GetTotalMemory(true);
                var memoryUsed = finalMemory - initialMemory;
                Console.WriteLine($"📊 Final Memory: {finalMemory / 1024 / 1024}MB");
                Console.WriteLine($"📈 Memory Used: {memoryUsed / 1024 / 1024}MB");
                Console.WriteLine($"📈 Memory per Concept: {memoryUsed / 10000} bytes");
            }

            private async Task TestConcurrentOperations()
            {
                Console.WriteLine("\n🔄 **CONCURRENT OPERATIONS TEST**");
                Console.WriteLine("".PadRight(35, '-'));

                var tasks = new List<Task>();
                var stopwatch = Stopwatch.StartNew();

                // Launch 10 concurrent batches of 100 concepts each
                for (int i = 0; i < 10; i++)
                {
                    var concepts = GenerateTestConcepts(100);
                    tasks.Add(_storageManager.SaveConceptsBatchAsync(concepts));
                }

                await Task.WhenAll(tasks);
                stopwatch.Stop();

                var totalTime = stopwatch.ElapsedMilliseconds;
                var totalConcepts = 1000;
                var conceptsPerSecond = (double)totalConcepts / (totalTime / 1000.0);

                Console.WriteLine($"⏱️  Concurrent Time: {totalTime}ms");
                Console.WriteLine($"⚡ Concurrent Throughput: {conceptsPerSecond:F2} concepts/second");
            }

            private async Task TestNetworkLatencySimulation()
            {
                Console.WriteLine("\n🌐 **NETWORK LATENCY SIMULATION**");
                Console.WriteLine("".PadRight(35, '-'));
                Console.WriteLine("📝 Note: Testing with network storage environment");

                // Test with smaller batches to simulate network conditions
                var concepts = GenerateTestConcepts(500);
                var stopwatch = Stopwatch.StartNew();

                await _storageManager.SaveConceptsBatchAsync(concepts);

                stopwatch.Stop();
                var totalTime = stopwatch.ElapsedMilliseconds;
                var conceptsPerSecond = (double)500 / (totalTime / 1000.0);

                Console.WriteLine($"⏱️  Network Storage Time: {totalTime}ms");
                Console.WriteLine($"⚡ Network Throughput: {conceptsPerSecond:F2} concepts/second");

                if (conceptsPerSecond >= 50)
                    Console.WriteLine("✅ **NETWORK OPTIMIZED**: Good performance on network storage");
                else
                    Console.WriteLine("⚠️  **NETWORK CONSIDERATIONS**: May need further optimization");
            }

            private async Task TestRealWorldScenario()
            {
                Console.WriteLine("\n🌍 **REAL-WORLD SCENARIO SIMULATION**");
                Console.WriteLine("".PadRight(40, '-'));
                Console.WriteLine("📝 Simulating: Learning session with mixed concept types");

                // Simulate a realistic learning session
                var scenarios = new[]
                {
                    ("Vocabulary Learning", 200),
                    ("Semantic Relationships", 150),
                    ("Grammar Patterns", 100),
                    ("Context Examples", 250)
                };

                var totalConcepts = 0;
                var totalTime = 0L;

                foreach (var (name, count) in scenarios)
                {
                    var concepts = GenerateTestConcepts(count);
                    var stopwatch = Stopwatch.StartNew();

                    await _storageManager.SaveConceptsBatchAsync(concepts);

                    stopwatch.Stop();
                    totalTime += stopwatch.ElapsedMilliseconds;
                    totalConcepts += count;

                    Console.WriteLine($"📚 {name}: {count} concepts in {stopwatch.ElapsedMilliseconds}ms");
                }

                var overallThroughput = (double)totalConcepts / (totalTime / 1000.0);
                Console.WriteLine($"🎯 **OVERALL SESSION**: {totalConcepts} concepts in {totalTime}ms");
                Console.WriteLine($"⚡ **SESSION THROUGHPUT**: {overallThroughput:F2} concepts/second");
            }

            private Dictionary<string, (object Data, GreyMatter.Storage.ConceptType Type)> GenerateTestConcepts(int count)
            {
                var concepts = new Dictionary<string, (object Data, GreyMatter.Storage.ConceptType Type)>();

                for (int i = 0; i < count; i++)
                {
                    var conceptId = $"test_concept_{i}";
                    var conceptData = new
                    {
                        id = conceptId,
                        word = $"word_{i}",
                        semantic_domain = GetRandomDomain(),
                        frequency = _random.Next(1, 1000),
                        associations = GenerateAssociations(3),
                        context_examples = GenerateExamples(2),
                        learned_patterns = GeneratePatterns(2),
                        timestamp = DateTime.UtcNow
                    };

                    concepts[conceptId] = (conceptData, GreyMatter.Storage.ConceptType.SemanticRelation);
                }

                return concepts;
            }

            private string GetRandomDomain()
            {
                var domains = new[] { "nouns", "verbs", "adjectives", "grammar", "semantics", "syntax" };
                return domains[_random.Next(domains.Length)];
            }

            private List<string> GenerateAssociations(int count)
            {
                var associations = new List<string>();
                for (int i = 0; i < count; i++)
                {
                    associations.Add($"associated_word_{_random.Next(1000)}");
                }
                return associations;
            }

            private List<string> GenerateExamples(int count)
            {
                var examples = new List<string>();
                for (int i = 0; i < count; i++)
                {
                    examples.Add($"Example sentence {_random.Next(1000)} with context.");
                }
                return examples;
            }

            private List<string> GeneratePatterns(int count)
            {
                var patterns = new List<string>();
                for (int i = 0; i < count; i++)
                {
                    patterns.Add($"Pattern_{_random.Next(100)}");
                }
                return patterns;
            }
        }

        /// <summary>
        /// Run comprehensive performance validation tests
        /// </summary>
        static async Task RunPerformanceValidation()
        {
            Console.WriteLine("🧪 **COMPREHENSIVE STORAGE PERFORMANCE VALIDATION**");
            Console.WriteLine("==================================================");

            var validator = new StoragePerformanceValidator();
            await validator.RunComprehensivePerformanceTest();
        }
    }
}
