using System;
using System.IO;
using System.Threading.Tasks;
using GreyMatter.Storage;

namespace GreyMatter.Demos
{
    /// <summary>
    /// Migration utility to consolidate scattered demo data into production storage
    /// Run this ONCE to clean up the mess
    /// </summary>
    public class MigrateToProductionStorage
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  PRODUCTION STORAGE MIGRATION                                     ║");
            Console.WriteLine("║  Consolidating scattered demo data into centralized storage      ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════╝\n");
            
            // Initialize production storage manager
            var storage = new ProductionStorageManager();
            
            // Migrate from old demo directories
            await storage.MigrateFromDemoDirectoriesAsync();
            
            Console.WriteLine("\n═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("MIGRATION COMPLETE");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════\n");
            
            Console.WriteLine("📁 Production Storage Structure:");
            Console.WriteLine("   /Users/billdodd/Desktop/Cerebro/brainData/");
            Console.WriteLine("   ├── live/               # Active brain state");
            Console.WriteLine("   ├── checkpoints/        # Hourly snapshots (last 24)");
            Console.WriteLine("   ├── episodic_memory/    # Event log");
            Console.WriteLine("   └── metrics/            # Performance tracking");
            Console.WriteLine();
            Console.WriteLine("   /Volumes/jarvis/brainData/");
            Console.WriteLine("   ├── archives/           # Daily backups");
            Console.WriteLine("   └── training_logs/      # Historical records");
            Console.WriteLine();
            
            // Show what was migrated
            var checkpoints = storage.ListCheckpoints();
            if (checkpoints.Count > 0)
            {
                Console.WriteLine($"✅ Found {checkpoints.Count} checkpoints:");
                foreach (var cp in checkpoints.Take(5))
                {
                    Console.WriteLine($"   • {cp.Timestamp:yyyy-MM-dd HH:mm} - {cp.SentencesProcessed:N0} sentences, {cp.VocabularySize:N0} words");
                }
                if (checkpoints.Count > 5)
                {
                    Console.WriteLine($"   ... and {checkpoints.Count - 5} more");
                }
            }
            
            Console.WriteLine("\n💡 Next Steps:");
            Console.WriteLine("   1. Review migrated data in production directories");
            Console.WriteLine("   2. Delete old demo directories if migration successful:");
            Console.WriteLine("      rm -rf ./continuous_learning_demo");
            Console.WriteLine("      rm -rf ./continuous_learning_week7");
            Console.WriteLine("      rm -rf ./attention_showcase_memory");
            Console.WriteLine("      rm -rf ./demo_episodic_memory");
            Console.WriteLine("      rm -rf ./episodic_memory");
            Console.WriteLine("   3. Start using ProductionTrainingService (Phase 2)");
            Console.WriteLine();
        }
    }
}
