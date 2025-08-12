using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreyMatter.Storage;

namespace GreyMatter
{
    /// <summary>
    /// Quick test to see if the classifier can recognize exact training examples
    /// </summary>
    public static class QuickClassifierTest
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("🔍 Quick Classifier Test");
            Console.WriteLine("Training then immediately testing classifier...");
            
            // Initialize the biological storage with trainable classifier
            var storageManager = new SemanticStorageManager("./test_data/brain_structure");
            
            // First train with a few examples
            Console.WriteLine("🧠 Training with basic examples...");
            await storageManager.TrainSemanticClassifierAsync("cat", "living_things/animals/mammals", "furry domestic pet");
            await storageManager.TrainSemanticClassifierAsync("computer", "artifacts/technology/electronics", "digital device");
            await storageManager.TrainSemanticClassifierAsync("car", "artifacts/vehicles/land_vehicles", "automobile");
            
            Console.WriteLine("✅ Training complete. Now testing...");
            
            // Test the same examples
            var testWords = new[] { "cat", "computer", "car", "dog", "robot" };
            
            foreach (var word in testWords)
            {
                var domain = await storageManager.GetPredictedDomainAsync(word);
                Console.WriteLine($"  '{word}' → {domain}");
            }
            
            Console.WriteLine("✅ Quick test complete!");
        }
    }
}
