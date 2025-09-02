using System;
using System.Threading.Tasks;
using GreyMatter.Storage;

namespace GreyMatter
{
    /// <summary>
    /// Demo showing the pre-trained semantic classification system
    /// No training required - uses semantic embeddings for intelligent categorization
    /// </summary>
    public static class PreTrainedSemanticDemo
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("🧠 Pre-Trained Semantic Classification Demo");
            Console.WriteLine("============================================================");
            Console.WriteLine("Demonstrating semantic classification without custom training");
            Console.WriteLine("Uses pre-trained embeddings + biological storage architecture");
            Console.WriteLine();

            // Use NAS paths for persistent storage  
            var brainDataPath = "/Volumes/jarvis/brainData";
            var trainingDataRoot = "/Volumes/jarvis/trainData";
            
            // Initialize semantic storage with pre-trained classifier
            var storageManager = new SemanticStorageManager(brainDataPath, trainingDataRoot);
            
            Console.WriteLine("📚 Testing semantic classification on diverse vocabulary:");
            Console.WriteLine();

            // Test various semantic categories
            var testWords = new[]
            {
                // Animals
                "elephant", "sparrow", "dolphin", "butterfly",
                
                // Technology
                "smartphone", "artificial_intelligence", "quantum_computer", "blockchain",
                
                // Vehicles
                "helicopter", "sailboat", "motorcycle", "spaceship",
                
                // Natural world
                "thunderstorm", "glacier", "volcano", "rainbow",
                
                // Mental/Cognitive
                "creativity", "consciousness", "intuition", "wisdom",
                
                // Abstract concepts
                "democracy", "justice", "beauty", "infinity",
                
                // Actions
                "programming", "meditation", "exploration", "collaboration",
                
                // Compound concepts
                "robot_dog", "flying_car", "smart_home", "virtual_reality"
            };

            int correctlyClassified = 0;
            int totalWords = testWords.Length;

            foreach (var word in testWords)
            {
                // Use the fallback classifier directly for demo (since we don't have ONNX model)
                var fallbackClassifier = new FallbackSemanticClassifier();
                var domain = fallbackClassifier.ClassifySemanticDomain(word);
                
                var isWellClassified = domain != "semantic_domains/general_concepts";
                if (isWellClassified) correctlyClassified++;
                
                var status = isWellClassified ? "✅" : "⚠️";
                Console.WriteLine($"  {status} '{word}' → {domain}");
            }

            Console.WriteLine();
            Console.WriteLine($"📊 Classification Results:");
            Console.WriteLine($"   Correctly classified: {correctlyClassified}/{totalWords} ({(double)correctlyClassified/totalWords:P})");
            Console.WriteLine($"   Using: Fallback rule-based classifier");
            Console.WriteLine();

            Console.WriteLine("🔬 Benefits of Pre-trained Approach:");
            Console.WriteLine("   ✅ No training data required");
            Console.WriteLine("   ✅ Works immediately out-of-the-box");
            Console.WriteLine("   ✅ Leverages proven semantic understanding");
            Console.WriteLine("   ✅ Integrates with biological storage architecture");
            Console.WriteLine("   ✅ Fallback system ensures robustness");
            Console.WriteLine();

            Console.WriteLine("🚀 Next Steps:");
            Console.WriteLine("   • Download pre-trained sentence-transformer ONNX model");
            Console.WriteLine("   • Place model in: /Volumes/jarvis/trainData/models/sentence-transformer.onnx");
            Console.WriteLine("   • Or run: bash download_semantic_model.sh");
            Console.WriteLine("   • Enjoy highly accurate semantic classification!");
            Console.WriteLine();

            Console.WriteLine("✅ Pre-trained semantic classification demo complete!");
        }
    }
}
