using System;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--test-sparse-activation")
            {
                await RunSparseActivationTest();
                return;
            }
            
            if (args.Length > 0 && args[0] == "--cerebro-query")
            {
                await CerebroQueryCLI.Run(args);
                return;
            }
            
            if (args.Length > 0 && args[0] == "--inspect-brain")
            {
                await BrainInspector.Run(args);
                return;
            }
            
            if (args.Length > 0 && (args[0] == "--production-training" || args[0] == "--production"))
            {
                var datasetKey = GetArgValue(args, "--dataset", "tatoeba_small");
                var durationSec = int.Parse(GetArgValue(args, "--duration", "86400"));
                var useLLMTeacher = args.Contains("--llm-teacher");
                
                var service = new ProductionTrainingService(
                    datasetKey: datasetKey,
                    llmTeacher: useLLMTeacher ? new LLMTeacher() : null,
                    useLLMTeacher: useLLMTeacher,
                    useProgressiveCurriculum: true,
                    checkpointIntervalMinutes: 10, // Frequent checkpoints for data safety
                    validationIntervalHours: 6,
                    nasArchiveIntervalHours: 24,
                    enableAttention: true,
                    enableEpisodicMemory: true
                );
                
                await service.StartAsync();
                await Task.Delay(durationSec * 1000);
                await service.StopAsync();
                
                var stats = service.GetStats();
                Console.WriteLine("\n" + "═".PadRight(80, '═'));
                Console.WriteLine("PRODUCTION TRAINING - FINAL STATISTICS");
                Console.WriteLine("═".PadRight(80, '═'));
                Console.WriteLine($"Total runtime: {stats.Uptime.TotalHours:F1} hours");
                Console.WriteLine($"Sentences processed: {stats.TotalSentencesProcessed:N0}");
                Console.WriteLine($"Vocabulary learned: {stats.VocabularySize:N0} words");
                Console.WriteLine($"Checkpoints saved: {stats.CheckpointsSaved}");
                Console.WriteLine($"Validations: {stats.ValidationsPassed}/{stats.ValidationsPassed + stats.ValidationsFailed}");
                Console.WriteLine("═".PadRight(80, '═'));
            }
            else
            {
                Console.WriteLine("║                                                           ║");
                Console.WriteLine("║  Available Commands:                                      ║");
                Console.WriteLine("║  ─────────────────────────────────────────────────────── ║");
                Console.WriteLine("║                                                           ║");
                Console.WriteLine("║  Production Training:                                     ║");
                Console.WriteLine("║    dotnet run -- --production-training                    ║");
                Console.WriteLine("║    dotnet run -- --production-training --duration 3600    ║");
                Console.WriteLine("║                                                           ║");
                Console.WriteLine("║  Query & Inspection:                                      ║");
                Console.WriteLine("║    dotnet run -- --cerebro-query stats                    ║");
                Console.WriteLine("║    dotnet run -- --cerebro-query think <word>             ║");
                Console.WriteLine("║    dotnet run -- --inspect-brain                          ║");
                Console.WriteLine("║                                                           ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            }
        }

        static string GetArgValue(string[] args, string key, string defaultValue)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == key) return args[i + 1];
            }
            return defaultValue;
        }
        
        static async Task RunSparseActivationTest()
        {
            Console.WriteLine("🧪 Phase 6A: Sparse Activation Test");
            Console.WriteLine("====================================\n");
            
            var cerebro = new Cerebro("/Volumes/jarvis/brainData");
            Console.WriteLine("✅ Cerebro initialized\n");
            
            Console.WriteLine("📚 Training on 20 sentences...");
            var sentences = new[]
            {
                "the cat sat on the mat",
                "dogs are loyal animals",
                "birds can fly in the sky",
                "fish swim in the water",
                "the sun is bright and warm",
                "rain falls from the clouds",
                "trees grow tall and strong",
                "flowers bloom in spring",
                "winter brings cold and snow",
                "summer is hot and sunny",
                "apples are red or green",
                "bananas are yellow fruit",
                "carrots are orange vegetables",
                "bread is made from wheat",
                "milk comes from cows",
                "cheese is made from milk",
                "pizza is a popular food",
                "coffee keeps people awake",
                "tea is a soothing drink",
                "water is essential for life"
            };
            
            for (int i = 0; i < sentences.Length; i++)
            {
                var features = new System.Collections.Generic.Dictionary<string, double>();
                await cerebro.LearnConceptAsync(sentences[i], features);
            }
            
            Console.WriteLine($"✅ Training complete: {sentences.Length} sentences\n");
            Console.WriteLine("🔍 Running queries to measure sparse activation...\n");
            
            var queries = new[] { "cat", "dog", "sun", "water", "tree", "food", "pizza", "milk" };
            
            foreach (var query in queries)
            {
                var features = new System.Collections.Generic.Dictionary<string, double>();
                await cerebro.ProcessInputAsync(query, features);
            }
            
            Console.WriteLine("\n💾 Saving checkpoint to show biological alignment metrics...\n");
            await cerebro.SaveAsync();
            
            Console.WriteLine("\n✅ Test complete!");
        }
    }
}
