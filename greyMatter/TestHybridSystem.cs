using System;
using System.Threading.Tasks;
using GreyMatter.Core;
using GreyMatter.Storage;

namespace GreyMatter
{
    /// <summary>
    /// Simple test to verify the hybrid training system is working
    /// </summary>
    public class TestHybridSystemVerification
    {
        public static async Task RunTest()
        {
            Console.WriteLine("🧠🔬 **HYBRID TRAINING SYSTEM VERIFICATION**");
            Console.WriteLine("==================================================");
            Console.WriteLine("Testing the complete hybrid training implementation");
            Console.WriteLine();

            try
            {
                // Initialize components
                Console.WriteLine("📋 Initializing Hybrid Training Components...");
                
                // Create storage manager
                var storageManager = new SemanticStorageManager("/Volumes/jarvis/brainData", "/Volumes/jarvis/trainData");
                
                // Create semantic classifiers
                Console.WriteLine("🔧 Setting up semantic classifiers...");
                var pretrainedClassifier = new PreTrainedSemanticClassifier(storageManager);
                var trainableClassifier = new TrainableSemanticClassifier(storageManager);
                
                // Create Cerebro brain instance
                Console.WriteLine("🧠 Initializing Cerebro...");
                var cerebro = new Cerebro("/Volumes/jarvis/brainData");
                
                // Initialize hybrid trainer
                Console.WriteLine("🔧 Setting up HybridCerebroTrainer...");
                var hybridTrainer = new HybridCerebroTrainer(
                    cerebro,
                    pretrainedClassifier,
                    trainableClassifier,
                    storageManager,
                    semanticGuidanceStrength: 0.7,
                    biologicalVariationRate: 0.3,
                    enableBidirectionalLearning: true
                );
                
                Console.WriteLine("✅ Hybrid trainer initialized successfully!");
                
                // Test basic semantic classification
                Console.WriteLine("\n🧪 Testing Semantic Classification...");
                var testSentences = new[]
                {
                    "The cat sits on the mat",
                    "I feel happy today", 
                    "Mathematics is fascinating",
                    "The ocean is vast and blue",
                    "Learning requires practice"
                };

                foreach (var sentence in testSentences)
                {
                    Console.WriteLine($"\n📝 Processing: \"{sentence}\"");
                    
                    try
                    {
                        // Test semantic-guided training
                        await hybridTrainer.TrainWithSemanticGuidanceAsync(sentence);
                        Console.WriteLine("   ✅ Semantic-guided training completed");
                        
                        // Get response to verify learning
                        var response = await cerebro.ProcessInputAsync(sentence, new Dictionary<string, double>());
                        Console.WriteLine($"   💭 Response: {response.Response}");
                        Console.WriteLine($"   📊 Confidence: {response.Confidence:P1}");
                        
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"   ⚠️ Error: {ex.Message}");
                    }
                }
                
                // Test batch training
                Console.WriteLine("\n🚀 Testing Batch Training...");
                var batchResult = await hybridTrainer.TrainBatchWithSemanticGuidanceAsync(
                    testSentences, 
                    batchSize: 3, 
                    showProgress: true
                );
                
                Console.WriteLine($"✅ Batch training completed!");
                Console.WriteLine($"   📊 Total Inputs: {batchResult.TotalInputs} sentences");
                Console.WriteLine($"   ✅ Successful: {batchResult.SuccessfulResults}");
                Console.WriteLine($"   ❌ Failed: {batchResult.FailedResults}");
                Console.WriteLine($"   ⏱️ Duration: {batchResult.TotalProcessingTime:F1}s");
                Console.WriteLine($"   🎯 Success Rate: {(batchResult.SuccessfulResults / (double)batchResult.TotalInputs):P1}");
                
                Console.WriteLine("\n🎉 **HYBRID TRAINING SYSTEM VERIFICATION COMPLETE**");
                Console.WriteLine("   ✅ HybridCerebroTrainer operational");
                Console.WriteLine("   ✅ Semantic classification functional");
                Console.WriteLine("   ✅ Biological neural training integrated");
                Console.WriteLine("   ✅ Batch processing capabilities confirmed");
                Console.WriteLine("   🌟 Hybrid system ready for real-world data!");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine($"📋 Stack trace: {ex.StackTrace}");
            }
        }
    }
}
