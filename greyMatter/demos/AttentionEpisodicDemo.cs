using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;
using greyMatter.Core;

namespace GreyMatter.Demos
{
    /// <summary>
    /// Demo showcasing Week 7 advanced integration features:
    /// - Attention mechanisms for selective pattern processing
    /// - Episodic memory for learning event tracking
    /// - Integration with continuous learning service
    /// </summary>
    public class AttentionEpisodicDemo
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  WEEK 7: Attention & Episodic Memory Integration Demo         ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            // Create brain
            var brain = new LanguageEphemeralBrain();
            
            Console.WriteLine("🧠 Brain created\n");

            // Test data
            var testSentences = new List<string>
            {
                // Novel sentences (high novelty)
                "The quick brown fox jumps over the lazy dog",
                "Machine learning algorithms process vast amounts of data",
                "Neural networks learn patterns through backpropagation",
                
                // Repeated concepts (low novelty after first)
                "The fox jumps over obstacles",
                "Machine learning improves with more data",
                "Neural networks require training data",
                
                // More novel concepts
                "Quantum computing leverages superposition",
                "Episodic memory stores temporal events",
                "Attention mechanisms focus on salient patterns",
                
                // Mixed novelty
                "The neural networks use machine learning",
                "Quantum algorithms process data efficiently"
            };

            await RunScenario1_BaselineNoAttention(brain, testSentences);
            await RunScenario2_AttentionEnabled(brain, testSentences);
            await RunScenario3_AttentionPlusEpisodicMemory(brain, testSentences);
            await RunScenario4_AttentionTuning(brain, testSentences);

            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine("Demo Complete!");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");
        }

        /// <summary>
        /// Scenario 1: Baseline - no attention, no episodic memory
        /// </summary>
        static async Task RunScenario1_BaselineNoAttention(LanguageEphemeralBrain brain, List<string> sentences)
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("SCENARIO 1: Baseline (No Attention, No Episodic Memory)");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            var trainer = new IntegratedTrainer(
                brain,
                enableColumnProcessing: true,
                enableTraditionalLearning: true,
                enableIntegration: true,
                enableAttention: false,
                enableEpisodicMemory: false
            );

            Console.WriteLine("Training on {0} sentences...\n", sentences.Count);
            
            var startTime = DateTime.Now;
            
            foreach (var sentence in sentences.Take(5))
            {
                await trainer.TrainOnSentenceAsync(sentence);
            }
            
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;

            Console.WriteLine("\n📊 Baseline Results:");
            Console.WriteLine($"  Time: {elapsed:F1}ms");
            Console.WriteLine($"  Avg: {elapsed/5:F1}ms per sentence");
            
            trainer.PrintStats();
        }

        /// <summary>
        /// Scenario 2: Attention enabled (novelty-focused)
        /// </summary>
        static async Task RunScenario2_AttentionEnabled(LanguageEphemeralBrain brain, List<string> sentences)
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine("SCENARIO 2: Attention Enabled (Novelty-Focused)");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            var trainer = new IntegratedTrainer(
                brain,
                enableColumnProcessing: true,
                enableTraditionalLearning: true,
                enableIntegration: true,
                enableAttention: true,
                enableEpisodicMemory: false,
                attentionThreshold: 0.5, // Medium threshold
                attentionConfig: GreyMatter.Core.AttentionConfiguration.NoveltyFocused
            );

            Console.WriteLine("Training on {0} sentences with attention...\n", sentences.Count);
            Console.WriteLine("Attention Config: Novelty-Focused (60% novelty weight)");
            Console.WriteLine("Attention Threshold: 0.5 (patterns below this are skipped)\n");
            
            var startTime = DateTime.Now;
            
            foreach (var sentence in sentences.Take(5))
            {
                await trainer.TrainOnSentenceAsync(sentence);
            }
            
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;

            Console.WriteLine("\n📊 Attention Results:");
            Console.WriteLine($"  Time: {elapsed:F1}ms");
            Console.WriteLine($"  Avg: {elapsed/5:F1}ms per sentence");
            
            trainer.PrintStats();
            
            Console.WriteLine("💡 Hypothesis: Attention should reduce patterns processed");
            Console.WriteLine("   by focusing only on novel/uncertain patterns.\n");
        }

        /// <summary>
        /// Scenario 3: Attention + Episodic Memory
        /// </summary>
        static async Task RunScenario3_AttentionPlusEpisodicMemory(LanguageEphemeralBrain brain, List<string> sentences)
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine("SCENARIO 3: Attention + Episodic Memory");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            var trainer = new IntegratedTrainer(
                brain,
                enableColumnProcessing: true,
                enableTraditionalLearning: true,
                enableIntegration: true,
                enableAttention: true,
                enableEpisodicMemory: true,
                attentionThreshold: 0.5,
                attentionConfig: GreyMatter.Core.AttentionConfiguration.Default,
                episodicMemoryPath: "./demo_episodic_memory"
            );

            Console.WriteLine("Training on all {0} sentences with attention + episodic memory...\n", sentences.Count);
            Console.WriteLine("Attention Config: Default (balanced)");
            Console.WriteLine("Episodic Memory: Enabled\n");
            
            var startTime = DateTime.Now;
            
            foreach (var sentence in sentences)
            {
                await trainer.TrainOnSentenceAsync(sentence);
                
                // Show progress
                if (sentences.IndexOf(sentence) % 3 == 0)
                {
                    Console.WriteLine($"  [{sentences.IndexOf(sentence)+1}/{sentences.Count}] {sentence.Substring(0, Math.Min(50, sentence.Length))}...");
                }
            }
            
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;

            Console.WriteLine($"\n📊 Combined System Results:");
            Console.WriteLine($"  Time: {elapsed:F1}ms");
            Console.WriteLine($"  Avg: {elapsed/sentences.Count:F1}ms per sentence");
            
            trainer.PrintStats();
            
            Console.WriteLine("💡 Complete system: Attention focuses learning, episodic memory");
            Console.WriteLine("   records events for future context retrieval.\n");
        }

        /// <summary>
        /// Scenario 4: Attention tuning comparison
        /// </summary>
        static async Task RunScenario4_AttentionTuning(LanguageEphemeralBrain brain, List<string> sentences)
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine("SCENARIO 4: Attention Threshold Tuning");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            var thresholds = new[] { 0.3, 0.5, 0.7 };
            var results = new List<(double threshold, double timeMs, int patternsSkipped)>();

            foreach (var threshold in thresholds)
            {
                Console.WriteLine($"Testing threshold: {threshold:F1}...");
                
                var trainer = new IntegratedTrainer(
                    brain,
                    enableColumnProcessing: true,
                    enableTraditionalLearning: true,
                    enableIntegration: true,
                    enableAttention: true,
                    enableEpisodicMemory: false,
                    attentionThreshold: threshold
                );

                var startTime = DateTime.Now;
                
                foreach (var sentence in sentences.Take(5))
                {
                    await trainer.TrainOnSentenceAsync(sentence);
                }
                
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                var stats = trainer.GetStats();
                
                results.Add((threshold, elapsed, stats.PatternsSkippedByAttention));
                
                Console.WriteLine($"  Time: {elapsed:F1}ms, Patterns skipped: {stats.PatternsSkippedByAttention}");
            }

            Console.WriteLine("\n📊 Threshold Comparison:");
            Console.WriteLine("  Threshold  |  Time (ms)  |  Patterns Skipped");
            Console.WriteLine("  -----------|-------------|------------------");
            foreach (var (threshold, timeMs, skipped) in results)
            {
                Console.WriteLine($"     {threshold:F1}     |   {timeMs,7:F1}   |      {skipped,3}");
            }
            
            Console.WriteLine("\n💡 Lower threshold = more patterns processed = slower");
            Console.WriteLine("   Higher threshold = fewer patterns processed = faster\n");
        }
    }
}
