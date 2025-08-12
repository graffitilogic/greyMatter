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
            Console.WriteLine("Testing pre-trained semantic classification...");
            
            // Initialize the semantic storage with pre-trained classifier
            // Use NAS paths for proper data storage
            var brainDataPath = "/Volumes/jarvis/brainData";
            var trainingDataRoot = "/Volumes/jarvis/trainData";
            var storageManager = new SemanticStorageManager(brainDataPath, trainingDataRoot);
            
            Console.WriteLine("✅ Pre-trained classifier ready. Testing...");
            
            // Test various examples (no training needed!)
            var testWords = new[] { "cat", "computer", "car", "dog", "robot", "airplane", "mountain" };
            
            foreach (var word in testWords)
            {
                var domain = await storageManager.GetPredictedDomainAsync(word);
                Console.WriteLine($"  '{word}' → {domain}");
            }
            
            Console.WriteLine("✅ Quick test complete!");
        }
    }
}
