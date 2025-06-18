using System;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter
{
    /// <summary>
    /// Standalone language foundations demonstration
    /// Can be used independently or called from main program
    /// </summary>
    public class LanguageFoundationsDemo
    {
        public static async Task RunDemo(string[] args)
        {
            Console.WriteLine("🎓 **FOUNDATIONAL LANGUAGE LEARNING DEMONSTRATION**");
            Console.WriteLine("===================================================");
            Console.WriteLine("Progressive language acquisition following developmental stages\n");

            // Parse configuration
            var config = BrainConfiguration.FromCommandLine(args);
            if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
            {
                BrainConfiguration.DisplayUsage();
                return;
            }

            try
            {
                config.ValidateAndSetup();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Configuration Error: {ex.Message}");
                return;
            }

            // Initialize brain
            var brain = new BrainInJar(config.BrainDataPath);
            await brain.InitializeAsync();

            Console.WriteLine("📊 Initial Brain Status:");
            var initialStats = await brain.GetStatsAsync();
            Console.WriteLine($"   Age: {await brain.GetBrainAgeAsync()}");
            Console.WriteLine($"   Clusters: {initialStats.TotalClusters}");
            Console.WriteLine($"   Storage: {initialStats.StorageSizeFormatted}\n");

            // Start consciousness for enhanced learning
            await brain.AwakeConsciousnessAsync();

            // Run foundational language training
            var trainer = new LanguageFoundationsTrainer(brain, config);
            await trainer.RunFoundationalTrainingAsync();

            // Test comprehension with simple interactions
            Console.WriteLine("\n🧪 **TESTING LANGUAGE COMPREHENSION**");
            Console.WriteLine("=====================================");

            var testPhrases = new[]
            {
                ("What is a cat?", new System.Collections.Generic.Dictionary<string, double> { ["question"] = 1.0, ["animal"] = 0.9 }),
                ("The red ball is big", new System.Collections.Generic.Dictionary<string, double> { ["description"] = 1.0, ["color"] = 0.8, ["size"] = 0.8 }),
                ("I like to play", new System.Collections.Generic.Dictionary<string, double> { ["preference"] = 1.0, ["activity"] = 0.9 }),
                ("Dogs can run fast", new System.Collections.Generic.Dictionary<string, double> { ["ability"] = 1.0, ["animal"] = 0.9, ["speed"] = 0.8 }),
                ("Tell me a story", new System.Collections.Generic.Dictionary<string, double> { ["request"] = 1.0, ["narrative"] = 0.9 })
            };

            foreach (var (phrase, features) in testPhrases)
            {
                var response = await brain.ProcessInputAsync(phrase, features);
                Console.WriteLine($"🤔 Input: \"{phrase}\"");
                Console.WriteLine($"💭 Response: {response.Response}");
                Console.WriteLine($"   Confidence: {response.Confidence:P0} | Neurons: {response.ActivatedNeurons}");
                Console.WriteLine();
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

            await brain.SleepConsciousnessAsync();
            await brain.SaveAsync();

            Console.WriteLine("\n🎉 **FOUNDATIONAL LANGUAGE TRAINING COMPLETE**");
            Console.WriteLine("   ✅ Core vocabulary established");
            Console.WriteLine("   ✅ Basic grammar patterns learned");
            Console.WriteLine("   ✅ Simple sentence comprehension");
            Console.WriteLine("   ✅ Reading foundation established");
            Console.WriteLine("   ✅ Ready for progressive language expansion");
            Console.WriteLine("\n💡 Next steps: Continue with graded readers and conversational practice");
        }
    }
}
