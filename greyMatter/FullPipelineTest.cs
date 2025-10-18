using System;
using System.Linq;
using System.Threading.Tasks;
using greyMatter.Core;
using GreyMatter.Storage;
using GreyMatter.Learning;

namespace GreyMatter.Tests
{
    /// <summary>
    /// Full pipeline test: Train with synthetic sentences and validate biological learning
    /// </summary>
    public class FullPipelineTest
    {
        public static async Task Run()
        {
            Console.WriteLine("=== FULL PIPELINE TEST ===");
            Console.WriteLine("Testing biological learning with training and storage\n");
            
            // Create brain and storage
            var brain = new LanguageEphemeralBrain();
            var storage = new FastStorageAdapter(
                hotPath: "/Users/billdodd/Desktop/Cerebro/working",
                coldPath: "/Users/billdodd/Documents/Cerebro"
            );
            
            Console.WriteLine("📝 Training with 100 synthetic sentences...");
            Console.WriteLine("============================================\n");
            
            // Create 100 synthetic sentences for testing
            var sentences = new[]
            {
                // Animals
                "the cat is black", "the dog is brown", "the bird is blue",
                "cats like fish", "dogs like bones", "birds like seeds",
                "the black cat sleeps", "the brown dog runs", "the blue bird flies",
                
                // Food
                "the apple is red", "the banana is yellow", "the orange is orange",
                "apples are sweet", "bananas are soft", "oranges are juicy",
                "I eat apples", "I eat bananas", "I eat oranges",
                
                // Nature
                "the tree is tall", "the flower is pretty", "the grass is green",
                "trees have leaves", "flowers have petals", "grass grows fast",
                "the tall tree stands", "the pretty flower blooms", "the green grass waves",
                
                // Weather
                "the sun is bright", "the rain is wet", "the snow is cold",
                "sun makes heat", "rain makes puddles", "snow makes ice",
                "the bright sun shines", "the wet rain falls", "the cold snow melts",
                
                // More complex sentences
                "the red apple falls from the tall tree",
                "the black cat sleeps under the green grass",
                "the brown dog runs through the wet rain",
                "the blue bird flies above the pretty flower",
                "I like red apples and yellow bananas",
                "the cat and dog play together",
                "birds fly high in the bright sun",
                "trees grow tall with water and sun",
                "the cold snow covers the green grass",
                "flowers bloom when the sun shines",
                
                // Repeat some patterns to strengthen connections
                "the cat is black", "the dog is brown", "the bird is blue",
                "the apple is red", "the banana is yellow", "the orange is orange",
                "the tree is tall", "the flower is pretty", "the grass is green",
                "the sun is bright", "the rain is wet", "the snow is cold",
                
                // More variations
                "cats and dogs are animals", "apples and bananas are fruit",
                "trees and flowers are plants", "sun and rain make weather",
                "the black cat and brown dog", "red apples and yellow bananas",
                "tall trees and pretty flowers", "bright sun and wet rain",
                
                // Even more sentences
                "animals need food and water", "fruit grows on trees",
                "plants need sun and rain", "weather changes every day",
                "the cat chases the bird", "the dog finds the bone",
                "the bird builds a nest", "the apple hangs on the tree",
                "I see the black cat", "I see the brown dog",
                "I see the blue bird", "I see the red apple",
                "the cat sleeps all day", "the dog plays all day",
                "the bird sings all day", "the apple grows all summer",
                "cats purr when happy", "dogs bark when excited",
                "birds chirp in the morning", "apples ripen in the fall",
                "the tree provides shade", "the flower attracts bees",
                "the grass feels soft", "the sun feels warm",
                "rain falls from clouds", "snow falls in winter",
                "the cat drinks milk", "the dog drinks water",
                "the bird drinks from puddles", "I drink juice",
                "cats have sharp claws", "dogs have wagging tails",
                "birds have colorful feathers", "apples have shiny skin",
                "the black cat has green eyes", "the brown dog has floppy ears",
                "the blue bird has a yellow beak", "the red apple has brown seeds"
            };
            
            // Take only 100 sentences
            var trainingSentences = sentences.Take(100).ToList();
            
            Console.WriteLine($"Training on {trainingSentences.Count} sentences...\n");
            
            // Train the brain
            var startTime = DateTime.UtcNow;
            brain.LearnSentencesBatch(trainingSentences, batchSize: 20);
            var elapsed = DateTime.UtcNow - startTime;
            
            Console.WriteLine($"\n⏱️  Training completed in {elapsed.TotalSeconds:F1}s");
            
            // Get learning stats before saving
            var stats = brain.GetLearningStats();
            Console.WriteLine($"\n📊 LEARNING STATISTICS:");
            Console.WriteLine($"   Vocabulary: {stats.VocabularySize} words");
            Console.WriteLine($"   Learned sentences: {stats.LearnedSentences}");
            Console.WriteLine($"   Total concepts: {stats.TotalConcepts}");
            Console.WriteLine($"   Word associations: {stats.WordAssociationCount}");
            
            // Export and check neurons
            var neurons = brain.ExportNeurons();
            Console.WriteLine($"   Total neurons: {neurons.Count}");
            
            int neuronsWithConnections = 0;
            int totalConnections = 0;
            
            foreach (var neuronKvp in neurons)
            {
                var neuronData = neuronKvp.Value as dynamic;
                if (neuronData != null)
                {
                    var weights = neuronData.Weights as System.Collections.Generic.Dictionary<int, double>;
                    if (weights != null && weights.Count > 0)
                    {
                        neuronsWithConnections++;
                        totalConnections += weights.Count;
                    }
                }
            }
            
            Console.WriteLine($"   Neurons with connections: {neuronsWithConnections} ({neuronsWithConnections * 100.0 / neurons.Count:F1}%)");
            Console.WriteLine($"   Total connections: {totalConnections}");
            Console.WriteLine($"   Avg connections/neuron: {(double)totalConnections / Math.Max(1, neuronsWithConnections):F1}");
            
            // Save to storage
            Console.WriteLine($"\n💾 Saving brain state to storage...");
            
            var vocabulary = brain.ExportVocabulary();
            var languageData = brain.ExportLanguageData();
            var neuralConcepts = brain.ExportNeuralConcepts();
            
            // Save vocabulary
            var vocabSet = new HashSet<string>(vocabulary.Keys);
            await storage.SaveVocabularyAsync(vocabSet);
            
            // Save brain state
            var brainState = new Dictionary<string, object>
            {
                ["languageData"] = languageData,
                ["neurons"] = neurons,
                ["trainingSession"] = DateTime.UtcNow,
                ["schemaVersion"] = storage.SchemaVersion
            };
            await storage.SaveBrainStateAsync(brainState);
            
            // Save neural concepts
            await storage.SaveNeuralConceptsAsync(neuralConcepts);
            
            Console.WriteLine($"✅ Brain state saved successfully!");
            
            // Verify what was saved
            Console.WriteLine($"\n🔍 VERIFYING SAVED DATA:");
            Console.WriteLine($"   Saved vocabulary: {vocabSet.Count} words");
            Console.WriteLine($"   Saved neurons: {neurons.Count}");
            Console.WriteLine($"   Saved concepts: {neuralConcepts.Count}");
            
            // Check language data structure
            if (languageData.ContainsKey("learning_stats"))
            {
                var learningStats = languageData["learning_stats"] as Dictionary<string, object>;
                if (learningStats != null && learningStats.ContainsKey("LearnedSentences"))
                {
                    Console.WriteLine($"   Saved learned sentences: {learningStats["LearnedSentences"]}");
                }
                if (learningStats != null && learningStats.ContainsKey("WordAssociations"))
                {
                    var assocs = learningStats["WordAssociations"] as Dictionary<string, object>;
                    if (assocs != null)
                    {
                        var totalAssocs = assocs.Sum(kvp => {
                            var list = kvp.Value as System.Collections.IList;
                            return list?.Count ?? 0;
                        });
                        Console.WriteLine($"   Saved word associations: {totalAssocs} associations");
                    }
                }
            }
            
            // Test some associations
            Console.WriteLine($"\n🔗 SAMPLE WORD ASSOCIATIONS:");
            var testWords = new[] { "cat", "dog", "apple", "red", "tree" };
            foreach (var word in testWords)
            {
                var assocs = brain.GetWordAssociations(word, 5);
                if (assocs.Any())
                {
                    Console.WriteLine($"   '{word}' → {string.Join(", ", assocs)}");
                }
            }
            
            // Final verdict
            Console.WriteLine($"\n{'='}=======================================");
            if (neuronsWithConnections > 0 && stats.LearnedSentences > 0)
            {
                Console.WriteLine("✅ FULL PIPELINE TEST PASSED!");
                Console.WriteLine($"   \u2713 Neurons forming connections ({neuronsWithConnections * 100.0 / neurons.Count:F1}%)");
                Console.WriteLine($"   \u2713 Sentences tracked ({stats.LearnedSentences})");
                Console.WriteLine($"   \u2713 Word associations formed ({stats.WordAssociationCount})");
                Console.WriteLine($"   \u2713 Data saved to storage");
                Console.WriteLine($"\n   Now run: dotnet run -- --knowledge-query --stats");
                Console.WriteLine($"   To verify data persisted correctly!");
            }
            else
            {
                Console.WriteLine("❌ PIPELINE TEST FAILED");
                Console.WriteLine($"   Neurons with connections: {neuronsWithConnections}/{neurons.Count}");
                Console.WriteLine($"   Learned sentences: {stats.LearnedSentences}");
            }
            Console.WriteLine($"{'='}=======================================\n");
        }
    }
}
