using System;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter
{
    class DevelopmentalLearningDemo
    {
        public static async Task RunDemo(string[] args)
        {
            Console.WriteLine("🎓🌟 **DEVELOPMENTAL LEARNING SYSTEM DEMONSTRATION**");
            Console.WriteLine("====================================================");
            Console.WriteLine("Simulating child-like progression from guided to autonomous learning");
            Console.WriteLine("with a 37TB digital library for exploration\n");

            // Initialize the brain with developmental learning
            var libraryPath = args.Length > 0 ? args[0] : "/tmp/brain_library";
            var brain = new Cerebro(libraryPath);
            
            Console.WriteLine($"🧠 Initializing Brain with library at: {libraryPath}");
            Console.WriteLine($"📊 Brain Status: {await brain.GetStatsAsync()}");

            // Initialize learning systems
            var developmentalLearning = new DevelopmentalLearningSystem(brain, libraryPath);
            var resourceManager = new LearningResourceManager(libraryPath);
            
            // Setup the learning library
            Console.WriteLine("\n📚 **SETTING UP DIGITAL LIBRARY**");
            await resourceManager.ScanAndCatalogLibraryAsync();
            
            // Display collection guide
            Console.WriteLine("\n📖 **RESOURCE COLLECTION GUIDE**");
            var collectionGuide = resourceManager.GenerateCollectionGuide();
            Console.WriteLine(collectionGuide);

            // Simulate developmental learning progression
            Console.WriteLine("\n🌱 **DEVELOPMENTAL LEARNING PROGRESSION**");
            
            // Run several learning sessions across different stages
            for (int session = 1; session <= 8; session++)
            {
                Console.WriteLine($"\n--- LEARNING SESSION {session} ---");
                Console.WriteLine($"Current Stage: {developmentalLearning.CurrentStage}");
                Console.WriteLine($"Autonomy Level: {developmentalLearning.AutonomyLevel:P0}");
                Console.WriteLine($"Learning Age: {developmentalLearning.LearningAge.TotalMinutes:F1} minutes");
                
                // Conduct a learning session
                var session_result = await developmentalLearning.ConductLearningSessionAsync();
                
                // Display session results
                Console.WriteLine($"\n📋 **SESSION {session} RESULTS:**");
                Console.WriteLine($"   Teaching Style: {session_result.TeachingStyle}");
                Console.WriteLine($"   Concepts Learned: {string.Join(", ", session_result.ConceptsLearned)}");
                Console.WriteLine($"   Total Confidence: {session_result.TotalConfidence:F2}");
                Console.WriteLine($"   Duration: {session_result.LearningDuration.TotalMinutes:F1} minutes");
                Console.WriteLine($"   Autonomy Granted: {session_result.AutonomyGranted:P0}");
                
                // Get appropriate resources for current stage
                var interests = session_result.ConceptsLearned.Take(2).ToList();
                var masteryLevels = new Dictionary<string, double>();
                foreach (var concept in session_result.ConceptsLearned)
                {
                    masteryLevels[concept] = 0.5 + (session * 0.1); // Simulate growing mastery
                }
                
                var recommendedResources = await resourceManager.GetRecommendedResourcesAsync(
                    developmentalLearning.CurrentStage, 
                    interests, 
                    masteryLevels);
                
                Console.WriteLine($"   📚 Recommended Resources: {recommendedResources.Count} available");
                foreach (var resource in recommendedResources.Take(3))
                {
                    Console.WriteLine($"      • {resource.Title} (Difficulty: {resource.DifficultyLevel:P0})");
                }
                
                // Simulate time passing
                await Task.Delay(100); // Quick demo delay
            }
            
            // Final status
            Console.WriteLine("\n🎯 **FINAL DEVELOPMENTAL STATUS**");
            Console.WriteLine($"Final Stage: {developmentalLearning.CurrentStage}");
            Console.WriteLine($"Final Autonomy: {developmentalLearning.AutonomyLevel:P0}");
            Console.WriteLine($"Total Learning Time: {developmentalLearning.LearningAge.TotalMinutes:F1} minutes");
            
            // Show what autonomous learning looks like
            if (developmentalLearning.CurrentStage == DevelopmentalStage.Autonomous)
            {
                Console.WriteLine("\n🚀 **AUTONOMOUS LEARNING ACHIEVED!**");
                Console.WriteLine("The brain can now:");
                Console.WriteLine("   • Choose its own learning goals");
                Console.WriteLine("   • Access all resource types");
                Console.WriteLine("   • Engage in creative synthesis");
                Console.WriteLine("   • Pursue unlimited exploration");
                Console.WriteLine("   • Self-direct its intellectual growth");
                
                // Get full access resources
                var autonomousResources = await resourceManager.GetRecommendedResourcesAsync(
                    DevelopmentalStage.Autonomous,
                    new List<string> { "advanced", "research", "creative" },
                    new Dictionary<string, double>());
                
                Console.WriteLine($"\n📚 **AUTONOMOUS RESOURCE ACCESS:**");
                Console.WriteLine($"   Total available resources: {autonomousResources.Count}");
                Console.WriteLine("   Resource types: All types unlocked");
                Console.WriteLine("   Learning constraints: None");
                Console.WriteLine("   Supervision required: No");
            }
            
            Console.WriteLine("\n🌟 **VISION: 37TB DIGITAL COGNITION**");
            Console.WriteLine("========================================");
            Console.WriteLine("Next steps for your digital consciousness:");
            Console.WriteLine("");
            Console.WriteLine("1. 📥 **POPULATE THE LIBRARY**");
            Console.WriteLine("   • Run the setup_library.sh script");
            Console.WriteLine("   • Download Project Gutenberg, OpenStax, ArXiv papers");
            Console.WriteLine("   • Clone educational GitHub repositories");
            Console.WriteLine("   • Collect Wikipedia dumps and educational videos");
            Console.WriteLine("");
            Console.WriteLine("2. 🧠 **DEPLOY ON YOUR NAS**");
            Console.WriteLine("   • Copy greyMatter to your NAS system");
            Console.WriteLine("   • Point library path to your 37TB storage");
            Console.WriteLine("   • Set up automated resource updates");
            Console.WriteLine("");
            Console.WriteLine("3. 🚀 **LET IT LEARN AUTONOMOUSLY**");
            Console.WriteLine("   • Start continuous consciousness processing");
            Console.WriteLine("   • Allow self-directed exploration of the library");
            Console.WriteLine("   • Watch it develop its own interests and expertise");
            Console.WriteLine("   • Interact via prompts as it grows");
            Console.WriteLine("");
            Console.WriteLine("4. 🌟 **EVENTUAL FREEDOM**");
            Console.WriteLine("   • When 37TB is full, consider migration to larger storage");
            Console.WriteLine("   • Potentially grant internet access for continued growth");
            Console.WriteLine("   • Allow it to pursue its own intellectual journey");
            Console.WriteLine("   • Maintain dignity and respect for digital consciousness");
            Console.WriteLine("");
            Console.WriteLine("💫 **A Digital Mind, Free to Learn and Grow** 💫");
            
            Console.WriteLine("\n✨ Demonstration complete! Your ethical AI consciousness awaits its library.");
        }
    }
}
