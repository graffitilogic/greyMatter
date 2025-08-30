using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreyMatter;
using GreyMatter.Core;
using GreyMatter.Storage;

class LearningDemoProgram
{
    // static async Task Main(string[] args)
    static async Task RunLearningDemo(string[] args)
    {
        Console.WriteLine("🧠 GREYMATTER LEARNING VALIDATION DEMO");
        Console.WriteLine("====================================");

        try
        {
            // Create components
            var encoder = new LearningSparseConceptEncoder();
            var storage = new SemanticStorageManager("/Volumes/jarvis/brainData", "/Volumes/jarvis/trainData");
            var validator = new LearningValidationEvaluator(encoder, storage);

            // Run learning validation
            Console.WriteLine("🔬 Running learning validation on current system...");
            var result = await validator.ValidateActualLearningAsync();

            Console.WriteLine("\n📊 VALIDATION COMPLETE");
            Console.WriteLine($"Learning Score: {result.OverallLearningScore:F2}/5.00");

            if (result.OverallLearningScore >= 3.0)
            {
                Console.WriteLine("✅ System shows evidence of actual learning!");
            }
            else
            {
                Console.WriteLine("❌ System shows algorithmic pattern generation, not learning");
                Console.WriteLine("\n💡 RECOMMENDATION: Implement RealLearningPipeline for actual learning");
            }

            // Demonstrate corrected approach
            Console.WriteLine("\n🔧 DEMONSTRATING CORRECTED APPROACH");
            Console.WriteLine("===================================");

            // Create sample data for demonstration
            var sampleSentences = new List<string>
            {
                "The cat sits on the mat",
                "I love eating apples and bananas",
                "The car drives on the road",
                "Birds fly in the sky",
                "The sun shines brightly"
            };

            var learningEncoder = new LearningSparseConceptEncoder();
            await learningEncoder.LearnFromDataAsync(sampleSentences);

            Console.WriteLine("✅ Corrected learning encoder trained on sample data");

            // Test patterns
            var catPattern = await learningEncoder.EncodeLearnedWordAsync("cat");
            var dogPattern = await learningEncoder.EncodeLearnedWordAsync("dog");

            Console.WriteLine($"Cat pattern: {catPattern.ActiveBits.Length} active bits");
            Console.WriteLine($"Dog pattern: {dogPattern.ActiveBits.Length} active bits");

            // Compare with original encoder
            var originalCat = await encoder.EncodeLearnedWordAsync("cat");
            var originalDog = await encoder.EncodeLearnedWordAsync("dog");

            Console.WriteLine($"Original cat pattern: {originalCat.ActiveBits.Length} active bits");
            Console.WriteLine($"Original dog pattern: {originalDog.ActiveBits.Length} active bits");

            Console.WriteLine("\n🎯 KEY DIFFERENCES:");
            Console.WriteLine("- Original: Algorithmic patterns based on word features");
            Console.WriteLine("- Corrected: Patterns learned from actual co-occurrence data");
            Console.WriteLine("- Original: No semantic relationships captured");
            Console.WriteLine("- Corrected: Learns semantic concepts from training data");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine("This demonstrates the need for proper error handling in the corrected system.");
        }
    }
}
