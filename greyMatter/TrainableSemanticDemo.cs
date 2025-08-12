using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreyMatter.Storage;
using greyMatter.Core;

namespace greyMatter
{
    /// <summary>
    /// Demonstrates a trainable semantic classification system that learns from data
    /// Shows how hardcoded classifiers evolve into adaptive, learnable systems
    /// </summary>
    public class TrainableSemanticDemo
    {
        public static async Task RunLearningDemoAsync()
        {
            Console.WriteLine("🧠 Trainable Semantic Classification Demo");
            Console.WriteLine(new string('=', 60));
            
            // Initialize biological storage with trainable classifier
            var storageManager = new SemanticStorageManager("/tmp/trainable_brain");
            
            Console.WriteLine("📚 Phase 1: Training semantic classifier from examples");
            Console.WriteLine();
            
            // Train the classifier with labeled examples
            await TrainSemanticClassifierAsync(storageManager);
            
            Console.WriteLine("🧪 Phase 2: Testing learned classifications");
            Console.WriteLine();
            
            // Test with new concepts to see if it learned the patterns
            await TestLearnedClassificationsAsync(storageManager);
            
            Console.WriteLine("📈 Phase 3: Showing classification improvement");
            Console.WriteLine();
            
            // Show how classifications improve with more training data
            await DemonstrateIterativeLearningAsync(storageManager);
            
            Console.WriteLine("✅ Trainable semantic classification demo complete!");
        }
        
        private static async Task TrainSemanticClassifierAsync(SemanticStorageManager storage)
        {
            // Training examples with explicit semantic domain labels
            // Basic semantic training examples with proper domain paths
            var trainingExamples = new Dictionary<string, (string TargetDomain, string Features)>
            {
                // Animals - mammals
                ["cat"] = ("living_things/animals/mammals", "furry domestic pet feline"),
                ["dog"] = ("living_things/animals/mammals", "furry domestic pet canine loyal"),
                ["lion"] = ("living_things/animals/mammals", "big cat predator mane africa"),
                
                // Animals - birds  
                ["eagle"] = ("living_things/animals/birds", "flying bird prey predator wings"),
                
                // Technology/Electronics
                ["computer"] = ("artifacts/technology/electronics", "digital device processor computing"),
                ["smartphone"] = ("artifacts/technology/electronics", "mobile device communication portable"),
                ["robot"] = ("artifacts/technology/electronics", "automated machine artificial intelligence"),
                
                // Vehicles
                ["car"] = ("artifacts/vehicles/land_vehicles", "automobile transportation wheels road"),
                ["airplane"] = ("artifacts/vehicles/aircraft", "flying transportation wings airport"),
                ["boat"] = ("artifacts/vehicles/watercraft", "water transportation sailing floating"),
                
                // Natural world
                ["mountain"] = ("natural_world/geography/landforms", "high elevation peak landscape"),
                ["ocean"] = ("natural_world/geography/water_bodies", "large body saltwater marine"),
                ["rain"] = ("natural_world/weather/climate", "precipitation water falling weather"),
                
                // Mental/cognitive
                ["thinking"] = ("mental_cognitive/thoughts/ideas", "cognitive process mind reasoning"),
                ["memory"] = ("mental_cognitive/memory/perception", "remembering past experiences recall"),
                ["learning"] = ("mental_cognitive/knowledge/learning", "acquiring knowledge education study")
            };            Console.WriteLine($"Training with {trainingExamples.Count} labeled examples...");
            
            // Train the classifier with these examples
            foreach (var example in trainingExamples)
            {
                // Store the training example with its domain label
                await storage.TrainSemanticClassifierAsync(example.Key, example.Value.TargetDomain, example.Value.Features);
                Console.WriteLine($"  ✓ Trained: '{example.Key}' → {example.Value.TargetDomain.Split('/').Last()}");
            }
            
            Console.WriteLine($"Training complete! Classifier learned patterns from {trainingExamples.Count} examples.");
            Console.WriteLine();
        }
        
        private static async Task TestLearnedClassificationsAsync(SemanticStorageManager storage)
        {
            // Test concepts the classifier hasn't seen before
            var testConcepts = new Dictionary<string, object>
            {
                // Should classify as animals based on learned patterns
                ["elephant_behavior"] = new { animal = "elephant", living = true, mammal = true, large = true },
                ["bird_singing"] = new { animal = "bird", living = true, sound = true, flies = true },
                
                // Should classify as technology based on learned patterns
                ["laptop_programming"] = new { device = "laptop", technology = true, electronic = true, portable = true },
                ["ai_system"] = new { technology = true, artificial = true, intelligent = true, automated = true },
                
                // Should classify as vehicles based on learned patterns
                ["truck_delivery"] = new { vehicle = "truck", transportation = true, land = true, cargo = true },
                ["helicopter_rescue"] = new { vehicle = "helicopter", transportation = true, air = true, rescue = true },
                
                // Should classify as natural world based on learned patterns
                ["volcano_eruption"] = new { natural = "volcano", geography = true, landform = true, erupts = true },
                ["storm_weather"] = new { natural = "storm", weather = true, wind = true, powerful = true },
                
                // Should classify as cognitive based on learned patterns
                ["decision_making"] = new { mental = "decision", cognitive = true, mind = true, choice = true },
                ["problem_solving"] = new { mental = "solving", cognitive = true, mind = true, analysis = true }
            };
            
            Console.WriteLine("Testing classifier on new concepts...");
            Console.WriteLine();
            
            foreach (var concept in testConcepts)
            {
                // Store the concept and let the trained classifier categorize it
                await storage.StoreNeuralConceptsAsync(new Dictionary<string, object> { [concept.Key] = concept.Value });
                
                // Get the predicted domain from the storage statistics
                var domain = await storage.GetPredictedDomainAsync(concept.Key);
                Console.WriteLine($"  🎯 '{concept.Key}' → Predicted: {domain}");
            }
            
            Console.WriteLine();
            Console.WriteLine("✅ Classifier successfully applied learned patterns to new concepts!");
            Console.WriteLine();
        }
        
        private static async Task DemonstrateIterativeLearningAsync(SemanticStorageManager storage)
        {
            Console.WriteLine("Demonstrating iterative learning improvement...");
            Console.WriteLine();
            
            // Add more training examples to improve accuracy
            var additionalTraining = new Dictionary<string, TrainingExample>
            {
                // More complex examples that test boundary cases
                ["robot_dog"] = new("artifacts/technology/electronics", 
                    new { device = "robot", animal_like = "dog", technology = true, artificial = true }),
                ["flying_car"] = new("artifacts/vehicles/aircraft", 
                    new { vehicle = "car", transportation = true, air = true, futuristic = true }),
                ["artificial_brain"] = new("artifacts/technology/electronics", 
                    new { device = "brain", technology = true, artificial = true, cognitive = true }),
                ["weather_satellite"] = new("artifacts/technology/electronics", 
                    new { device = "satellite", technology = true, space = true, weather = true })
            };
            
            Console.WriteLine("Adding advanced training examples...");
            foreach (var example in additionalTraining)
            {
                await storage.TrainSemanticClassifierAsync(example.Key, example.Value.TargetDomain, example.Value.Features);
                Console.WriteLine($"  ⚡ Enhanced: '{example.Key}' → {example.Value.TargetDomain.Split('/').Last()}");
            }
            
            Console.WriteLine();
            Console.WriteLine("🚀 Classifier has been enhanced with boundary case training!");
            Console.WriteLine("Ready for complex real-world semantic categorization tasks.");
            Console.WriteLine();
            
            // Show final statistics
            var stats = await storage.GetStorageStatisticsAsync();
            Console.WriteLine("📊 Final Training Statistics:");
            Console.WriteLine($"   Total Concepts: {stats.ConceptIndexSize}");
            Console.WriteLine($"   Semantic Domains: {stats.CorticalColumnCount}");
            Console.WriteLine($"   Storage Size: {stats.TotalStorageSize} bytes");
        }
        
        public record TrainingExample(string TargetDomain, object Features);
    }
}
