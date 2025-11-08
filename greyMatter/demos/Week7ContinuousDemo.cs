using System;
using System.IO;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter.Demos
{
    /// <summary>
    /// Demo for Week 7: Continuous learning with attention and episodic memory
    /// Shows the complete integrated system running in continuous mode
    /// </summary>
    public class Week7ContinuousDemo
    {
        public static async Task RunAsync(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  WEEK 7: Continuous Learning with Attention & Episodic Memory ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            // Parse arguments
            var dataPath = GetArg(args, "--data", "./test_data");
            var workDir = GetArg(args, "--work-dir", "./continuous_learning_week7");
            var duration = int.Parse(GetArg(args, "--duration", "60")); // 60 seconds default
            var enableAttention = HasFlag(args, "--attention");
            var enableEpisodicMemory = HasFlag(args, "--episodic");
            var attentionThreshold = double.Parse(GetArg(args, "--threshold", "0.5"));

            Console.WriteLine("📋 Configuration:");
            Console.WriteLine($"   Data path: {dataPath}");
            Console.WriteLine($"   Working directory: {workDir}");
            Console.WriteLine($"   Duration: {duration} seconds");
            Console.WriteLine($"   Attention: {(enableAttention ? "✅" : "❌")}");
            if (enableAttention)
            {
                Console.WriteLine($"   Attention threshold: {attentionThreshold:F2}");
            }
            Console.WriteLine($"   Episodic memory: {(enableEpisodicMemory ? "✅" : "❌")}\n");

            // Ensure test data exists
            EnsureTestData(dataPath);

            // Run comparison scenarios
            await RunComparisonDemo(dataPath, workDir, duration, attentionThreshold);

            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine("Demo Complete!");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");
        }

        /// <summary>
        /// Run all three scenarios for comparison
        /// </summary>
        static async Task RunComparisonDemo(string dataPath, string workDir, int durationSec, double threshold)
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("COMPARISON: Baseline vs Attention vs Full System");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            var results = new (string name, long sentences, int vocab, double timeMs)[]
            {
                await RunScenario("Baseline", dataPath, workDir + "_baseline", durationSec, false, false, threshold),
                await RunScenario("Attention Only", dataPath, workDir + "_attention", durationSec, true, false, threshold),
                await RunScenario("Full System", dataPath, workDir + "_full", durationSec, true, true, threshold)
            };

            // Print comparison table
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    PERFORMANCE COMPARISON                      ║");
            Console.WriteLine("╠════════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║  Scenario         │ Sentences │ Vocab │  Time (ms) │ Sent/sec ║");
            Console.WriteLine("╟───────────────────┼───────────┼───────┼────────────┼──────────╢");
            
            foreach (var (name, sentences, vocab, timeMs) in results)
            {
                var sentPerSec = sentences / (timeMs / 1000.0);
                Console.WriteLine($"║  {name,-16} │ {sentences,9} │ {vocab,5} │ {timeMs,10:F1} │ {sentPerSec,8:F1} ║");
            }
            
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            // Analysis
            var baselineSentPerSec = results[0].sentences / (results[0].timeMs / 1000.0);
            var attentionSentPerSec = results[1].sentences / (results[1].timeMs / 1000.0);
            var fullSentPerSec = results[2].sentences / (results[2].timeMs / 1000.0);

            Console.WriteLine("📊 Analysis:");
            Console.WriteLine($"   Attention speedup: {(attentionSentPerSec / baselineSentPerSec - 1) * 100:F1}%");
            Console.WriteLine($"   Full system speedup: {(fullSentPerSec / baselineSentPerSec - 1) * 100:F1}%");
            Console.WriteLine($"   Vocabulary comparison: {results[0].vocab} → {results[1].vocab} → {results[2].vocab}");
        }

        /// <summary>
        /// Run a single scenario and return results
        /// </summary>
        static async Task<(string name, long sentences, int vocab, double timeMs)> RunScenario(
            string name,
            string dataPath,
            string workDir,
            int durationSec,
            bool enableAttention,
            bool enableEpisodicMemory,
            double threshold)
        {
            Console.WriteLine($"\n▶️  Running: {name}");
            Console.WriteLine($"   Attention: {enableAttention}, Episodic: {enableEpisodicMemory}");

            var service = new ContinuousLearningService(
                dataPath: dataPath,
                workingDirectory: workDir,
                autoSaveInterval: 100,
                batchSize: 50,
                useIntegration: true,
                enableAttention: enableAttention,
                enableEpisodicMemory: enableEpisodicMemory,
                attentionThreshold: threshold
            );

            var startTime = DateTime.Now;
            var serviceTask = service.StartAsync();
            await Task.Delay(durationSec * 1000);
            await service.StopAsync();
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;

            // Read results from status file
            var statusFile = Path.Combine(workDir, "status.json");
            long sentences = 0;
            int vocab = 0;

            if (File.Exists(statusFile))
            {
                var statusJson = File.ReadAllText(statusFile);
                // Simple parsing - in production would use JsonDocument
                var sentMatch = System.Text.RegularExpressions.Regex.Match(statusJson, @"""SentencesProcessed""\s*:\s*(\d+)");
                var vocabMatch = System.Text.RegularExpressions.Regex.Match(statusJson, @"""VocabularySize""\s*:\s*(\d+)");
                
                if (sentMatch.Success) sentences = long.Parse(sentMatch.Groups[1].Value);
                if (vocabMatch.Success) vocab = int.Parse(vocabMatch.Groups[1].Value);
            }

            Console.WriteLine($"   ✅ Complete: {sentences} sentences, {vocab} vocab, {elapsed:F1}ms");
            return (name, sentences, vocab, elapsed);
        }

        // Helper methods

        static string GetArg(string[] args, string flag, string defaultValue)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == flag)
                {
                    return args[i + 1];
                }
            }
            return defaultValue;
        }

        static bool HasFlag(string[] args, string flag)
        {
            return Array.IndexOf(args, flag) >= 0;
        }

        static void EnsureTestData(string dataPath)
        {
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }

            var testFile = Path.Combine(dataPath, "week7_test.txt");
            if (!File.Exists(testFile))
            {
                var sentences = new[]
                {
                    "The quick brown fox jumps over the lazy dog",
                    "Machine learning algorithms process vast amounts of data",
                    "Neural networks learn patterns through backpropagation",
                    "Attention mechanisms focus on salient information",
                    "Episodic memory stores temporal event sequences",
                    "The fox jumps over obstacles",
                    "Machine learning improves with more data",
                    "Neural networks require training data",
                    "Quantum computing leverages superposition",
                    "Episodic memory enables context retrieval",
                    "Attention weights guide learning resources",
                    "The neural networks use machine learning",
                    "Quantum algorithms process data efficiently",
                    "Memory consolidation occurs during rest periods",
                    "Attention filters low-priority information",
                    "Learning systems benefit from selective focus",
                    "Contextual information aids comprehension",
                    "Pattern recognition improves with experience",
                    "Cognitive systems integrate multiple modalities",
                    "Biological inspiration guides artificial intelligence"
                };

                File.WriteAllLines(testFile, sentences);
                Console.WriteLine($"✅ Created test data: {testFile}\n");
            }
        }

        public static void PrintUsage()
        {
            Console.WriteLine("Usage: dotnet run Week7ContinuousDemo.cs [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --data <path>        Data directory (default: ./test_data)");
            Console.WriteLine("  --work-dir <path>    Working directory (default: ./continuous_learning_week7)");
            Console.WriteLine("  --duration <sec>     Run duration in seconds (default: 60)");
            Console.WriteLine("  --attention          Enable attention mechanisms");
            Console.WriteLine("  --episodic           Enable episodic memory (requires --attention)");
            Console.WriteLine("  --threshold <val>    Attention threshold 0.0-1.0 (default: 0.5)");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  # Run comparison demo (recommended)");
            Console.WriteLine("  dotnet run Week7ContinuousDemo.cs --duration 30");
            Console.WriteLine();
            Console.WriteLine("  # Custom threshold");
            Console.WriteLine("  dotnet run Week7ContinuousDemo.cs --threshold 0.7 --duration 60");
            Console.WriteLine();
        }
    }
}
