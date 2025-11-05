using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreyMatter.Core;
using greyMatter.Core;

namespace GreyMatter.Demos
{
    /// <summary>
    /// Demo showing integrated column-brain training in action.
    /// Demonstrates bidirectional communication:
    /// - Columns detect patterns → trigger brain learning
    /// - Brain knowledge → guides column processing
    /// </summary>
    public class IntegratedTrainingDemo
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("\n" + "═".PadRight(80, '═'));
            Console.WriteLine("INTEGRATED TRAINING DEMO");
            Console.WriteLine("Biologically-Aligned Column-Brain Architecture");
            Console.WriteLine("═".PadRight(80, '═') + "\n");

            // Test sentences showcasing different learning scenarios
            var testSentences = new[]
            {
                "The cat sits on the mat",
                "The dog runs in the park",
                "A bird flies over the tree",
                "The cat chases the bird",
                "The dog and cat are friends",
                "Birds fly high in the sky",
                "The park has many trees",
                "Cats and dogs live together",
                "The mat is soft and warm",
                "Trees grow tall in parks"
            };

            // Test 1: Traditional learning only
            Console.WriteLine("📖 TEST 1: Traditional Learning Only");
            Console.WriteLine("─".PadRight(80, '─'));
            await RunTrainingMode(testSentences, 
                enableColumns: false, 
                enableTraditional: true, 
                enableIntegration: false);

            Console.WriteLine("\n");

            // Test 2: Columns only (no brain learning)
            Console.WriteLine("🏛️  TEST 2: Column Processing Only");
            Console.WriteLine("─".PadRight(80, '─'));
            await RunTrainingMode(testSentences, 
                enableColumns: true, 
                enableTraditional: false, 
                enableIntegration: false);

            Console.WriteLine("\n");

            // Test 3: Integrated (both systems working together)
            Console.WriteLine("🧠 TEST 3: Integrated Column-Brain Architecture");
            Console.WriteLine("─".PadRight(80, '─'));
            await RunTrainingMode(testSentences, 
                enableColumns: true, 
                enableTraditional: true, 
                enableIntegration: true);

            Console.WriteLine("\n" + "═".PadRight(80, '═'));
            Console.WriteLine("DEMO COMPLETE");
            Console.WriteLine("═".PadRight(80, '═') + "\n");
        }

        private static async Task RunTrainingMode(
            string[] sentences,
            bool enableColumns,
            bool enableTraditional,
            bool enableIntegration)
        {
            var startTime = DateTime.Now;

            // Create brain and trainer
            var brain = new LanguageEphemeralBrain();
            var trainer = new IntegratedTrainer(
                brain,
                enableColumnProcessing: enableColumns,
                enableTraditionalLearning: enableTraditional,
                enableIntegration: enableIntegration
            );

            Console.WriteLine($"⚙️  Configuration:");
            Console.WriteLine($"   Columns: {(enableColumns ? "✅" : "❌")}");
            Console.WriteLine($"   Traditional: {(enableTraditional ? "✅" : "❌")}");
            Console.WriteLine($"   Integration: {(enableIntegration ? "✅" : "❌")}");
            Console.WriteLine();

            // Train on sentences
            foreach (var sentence in sentences)
            {
                await trainer.TrainOnSentenceAsync(sentence);
            }

            var elapsed = DateTime.Now - startTime;

            // Print statistics
            Console.WriteLine($"⏱️  Training Time: {elapsed.TotalMilliseconds:F0}ms");
            
            var stats = trainer.GetStats();
            Console.WriteLine($"📊 Results:");
            Console.WriteLine($"   Sentences: {stats.SentencesProcessed}");
            Console.WriteLine($"   Vocabulary: {stats.VocabularySize} words");
            
            if (enableColumns)
            {
                Console.WriteLine($"   Columns: {stats.TotalColumns}");
            }
            
            if (enableIntegration && stats.IntegrationStats != null)
            {
                var iStats = stats.IntegrationStats;
                Console.WriteLine($"   🔗 Integration:");
                Console.WriteLine($"      Column→Brain: {iStats.PatternsNotified} patterns, {iStats.LearningTriggersTotal} triggers");
                Console.WriteLine($"      Brain→Column: {iStats.KnowledgeQueriesTotal} queries ({iStats.KnowledgeHits} hits)");
                Console.WriteLine($"      Synergy: {iStats.KnowledgeUtilizationRate:P0} knowledge use");
            }

            if (stats.PatternDetectionStats != null)
            {
                var pStats = stats.PatternDetectionStats;
                Console.WriteLine($"   🎯 Pattern Detection:");
                Console.WriteLine($"      {pStats.UniqueConcepts} concepts, {pStats.ConsensusEventsReached} consensus");
                Console.WriteLine($"      {pStats.CoActivationsFound} co-activations, {pStats.ActiveClusters} clusters");
            }

            // Test brain knowledge
            if (enableTraditional)
            {
                Console.WriteLine($"\n   📚 Brain Knowledge Test:");
                var testWords = new[] { "cat", "dog", "bird", "park", "tree" };
                var iBrain = brain as IIntegratedBrain;
                if (iBrain != null)
                {
                    foreach (var word in testWords)
                    {
                        var known = iBrain.IsKnownWord(word);
                        Console.WriteLine($"      {word}: {(known ? "✅" : "❌")}");
                    }
                }
            }
        }
    }
}
