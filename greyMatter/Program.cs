using System;
using System.Threading.Tasks;
using GreyMatter.Core;
using GreyMatter.Validation;

namespace GreyMatter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--adpc-test")
            {
                Console.WriteLine("🧪 Running ADPC-Net Phase 1 validation tests...\n");
                var tests = new AdpcPhase1ValidationTests();
                tests.RunAllTests();
                return;
            }

            if (args.Length > 0 && args[0] == "--adpc-phase2-test")
            {
                Console.WriteLine("🧬 Running ADPC-Net Phase 2 validation tests...\n");
                var tests = new AdpcPhase2ValidationTests();
                await tests.RunAllTests();
                return;
            }

            if (args.Length > 0 && args[0] == "--adpc-phase3-test")
            {
                Console.WriteLine("🕸️  Running ADPC-Net Phase 3 validation tests...\n");
                AdpcPhase3ValidationTests.RunAllTests();
                return;
            }

            if (args.Length > 0 && args[0] == "--adpc-phase4-test")
            {
                Console.WriteLine("📚 Running ADPC-Net Phase 4 validation tests...\n");
                AdpcPhase4ValidationTests.RunAllTests();
                return;
            }

            if (args.Length > 0 && args[0] == "--adpc-phase5-test")
            {
                Console.WriteLine("🚀 Running ADPC-Net Phase 5 validation tests...\n");
                await AdpcPhase5ValidationTests.RunAllTests();
                return;
            }

            if (args.Length > 0 && args[0] == "--cerebro-query")
            {
                await CerebroQueryCLI.Run(args);
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
                    checkpointIntervalMinutes: 60,
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
                Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
                Console.WriteLine("║  GreyMatter - Neural Architecture System              ║");
                Console.WriteLine("║  BUILD SUCCESSFUL ✅                                  ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine("  dotnet run -- --production-training    Start 24/7 training");
                Console.WriteLine("  dotnet run -- --cerebro-query <cmd>    Query trained brain");
                Console.WriteLine("  dotnet run -- --adpc-test              Run ADPC-Net Phase 1 tests");
                Console.WriteLine("  dotnet run -- --adpc-phase2-test       Run ADPC-Net Phase 2 tests");
                Console.WriteLine("  dotnet run -- --adpc-phase3-test       Run ADPC-Net Phase 3 tests");
                Console.WriteLine("  dotnet run -- --adpc-phase4-test       Run ADPC-Net Phase 4 tests");
                Console.WriteLine("  dotnet run -- --adpc-phase5-test       Run ADPC-Net Phase 5 tests");
                Console.WriteLine();
                Console.WriteLine("Query Commands:");
                Console.WriteLine("  stats                    Show brain statistics");
                Console.WriteLine("  think <concept>          Query what brain knows about concept");
                Console.WriteLine("  clusters [limit]         List learned concepts");
                Console.WriteLine();
                Console.WriteLine("Testing:");
                Console.WriteLine("  --adpc-test              Validate pattern-based learning (Phase 1)");
                Console.WriteLine("  --adpc-phase2-test       Validate dynamic neuron generation (Phase 2)");
                Console.WriteLine("  --adpc-phase3-test       Validate sparse synaptic graph (Phase 3)");
                Console.WriteLine("  --adpc-phase4-test       Validate VQ-VAE codebook (Phase 4)");
                Console.WriteLine("  --adpc-phase5-test       Validate production training integration (Phase 5)");
                Console.WriteLine();
                Console.WriteLine("Training Options:");
                Console.WriteLine("  --dataset <name>      Dataset to use (default: tatoeba_small)");
                Console.WriteLine("  --duration <seconds>  Training duration (default: 86400)");
                Console.WriteLine("  --llm-teacher         Enable LLM teacher assistance");
                Console.WriteLine();
                Console.WriteLine("Examples:");
                Console.WriteLine("  dotnet run -- --adpc-test");
                Console.WriteLine("  dotnet run -- --adpc-phase5-test");
                Console.WriteLine("  dotnet run -- --adpc-phase2-test");
                Console.WriteLine("  dotnet run -- --adpc-phase3-test");
                Console.WriteLine("  dotnet run -- --adpc-phase4-test");
                Console.WriteLine("  dotnet run -- --cerebro-query stats");
                Console.WriteLine("  dotnet run -- --cerebro-query think cat");
                Console.WriteLine("  dotnet run -- --cerebro-query think pizza");
                Console.WriteLine();
                Console.WriteLine("Note: All other demo commands temporarily disabled.");
                Console.WriteLine("      Full Program.cs backed up to Program.cs.backup");
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
    }
}
