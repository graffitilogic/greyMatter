using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreyMatter.Storage;

namespace GreyMatter
{
    /// <summary>
    /// Debug version to understand why classification is failing
    /// </summary>
    public static class DebugClassifierTest
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("🔧 Debug Classifier Test");
            Console.WriteLine("Training then testing with debug output...");
            
            // Create a simple test to validate the trainable classifier directly
            var storageManager = new GreyMatter.Storage.SemanticStorageManager("./test_data/brain", "./test_data/classifier");
            var classifier = new TrainableSemanticClassifier(storageManager);
            
            // Train with a simple example
            Console.WriteLine("🧠 Training with 'cat' -> 'living_things/animals/mammals'");
            var trainingData = new Dictionary<string, string>
            {
                ["cat"] = "living_things/animals/mammals"
            };
            await classifier.TrainFromExamplesAsync(trainingData);
            
            Console.WriteLine("✅ Training complete. Now testing 'cat'...");
            var result = classifier.ClassifySemanticDomain("cat");
            Console.WriteLine($"🎯 Result: {result}");
            
            // Test with variations
            var testWords = new[] { "cat", "cats", "feline", "dog", "unknown" };
            Console.WriteLine("\n🧪 Testing multiple variations:");
            foreach (var word in testWords)
            {
                var classification = classifier.ClassifySemanticDomain(word);
                Console.WriteLine($"  '{word}' → {classification}");
            }
            
            // Show training statistics
            var stats = classifier.GetTrainingStatistics();
            Console.WriteLine($"\n📊 Training Stats:");
            Console.WriteLine($"   Training Iterations: {stats.TrainingIterations}");
            Console.WriteLine($"   Domains With Examples: {stats.DomainsWithExamples}");
            Console.WriteLine($"   Total Examples: {stats.TotalExamples}");
            Console.WriteLine($"   Vocabulary Size: {stats.VocabularySize}");
            Console.WriteLine($"   Average Confidence: {stats.AverageConfidence:P}");
        }
    }
}
