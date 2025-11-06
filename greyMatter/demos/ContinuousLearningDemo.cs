using System;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter.Demos
{
    /// <summary>
    /// Demo for the continuous learning service.
    /// Shows how the service runs in the background, learning continuously
    /// with auto-save, pause/resume, and graceful shutdown.
    /// </summary>
    public class ContinuousLearningDemo
    {
        public static async Task RunAsync(string dataPath, int durationSeconds = 60)
        {
            Console.WriteLine("\n" + "═".PadRight(80, '═'));
            Console.WriteLine("CONTINUOUS LEARNING SERVICE DEMO");
            Console.WriteLine("═".PadRight(80, '═') + "\n");
            
            Console.WriteLine($"This demo will run the continuous learning service for {durationSeconds} seconds.");
            Console.WriteLine("The service will:");
            Console.WriteLine("  • Process sentences from data sources continuously");
            Console.WriteLine("  • Auto-save checkpoints every 1000 sentences");
            Console.WriteLine("  • Update status file every 10 seconds");
            Console.WriteLine("  • Support pause/resume/stop via control file");
            Console.WriteLine();
            
            // Create service
            var service = new ContinuousLearningService(
                dataPath: dataPath,
                workingDirectory: "./continuous_learning_demo",
                autoSaveInterval: 1000,
                batchSize: 50,
                useIntegration: true
            );
            
            // Start service in background task
            var serviceTask = Task.Run(async () =>
            {
                try
                {
                    await service.StartAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Service error: {ex.Message}");
                }
            });
            
            // Monitor service for duration
            var startTime = DateTime.Now;
            var lastReport = DateTime.Now;
            
            while ((DateTime.Now - startTime).TotalSeconds < durationSeconds)
            {
                await Task.Delay(5000); // Check every 5 seconds
                
                if ((DateTime.Now - lastReport).TotalSeconds >= 10)
                {
                    var stats = service.GetStatistics();
                    
                    Console.WriteLine();
                    Console.WriteLine("📊 Service Statistics:");
                    Console.WriteLine($"   Running: {stats.IsRunning}, Paused: {stats.IsPaused}");
                    Console.WriteLine($"   Uptime: {stats.Uptime:hh\\:mm\\:ss}");
                    Console.WriteLine($"   Sentences: {stats.SentencesProcessed:N0}");
                    Console.WriteLine($"   Vocabulary: {stats.VocabularySize:N0}");
                    Console.WriteLine($"   Rate: {stats.ProcessingRate:F1} sentences/sec");
                    Console.WriteLine($"   State: {stats.CurrentState}");
                    
                    lastReport = DateTime.Now;
                }
            }
            
            // Stop service
            Console.WriteLine("\n⏰ Demo duration complete, stopping service...");
            await service.StopAsync();
            
            // Wait for service task to complete
            await serviceTask;
            
            // Final statistics
            var finalStats = service.GetStatistics();
            
            Console.WriteLine("\n" + "═".PadRight(80, '═'));
            Console.WriteLine("DEMO COMPLETE - FINAL STATISTICS");
            Console.WriteLine("═".PadRight(80, '═'));
            Console.WriteLine($"\nTotal Sentences Processed: {finalStats.SentencesProcessed:N0}");
            Console.WriteLine($"Total Vocabulary Learned: {finalStats.VocabularySize:N0}");
            Console.WriteLine($"Average Processing Rate: {finalStats.ProcessingRate:F1} sentences/sec");
            Console.WriteLine($"Total Uptime: {finalStats.Uptime:hh\\:mm\\:ss}");
            Console.WriteLine($"\n✅ Checkpoints saved to: ./continuous_learning_demo/checkpoints");
            Console.WriteLine($"✅ Status file: ./continuous_learning_demo/status.json");
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrate control file functionality (pause/resume/stop)
        /// </summary>
        public static async Task DemoControlFeaturesAsync(string dataPath)
        {
            Console.WriteLine("\n" + "═".PadRight(80, '═'));
            Console.WriteLine("CONTINUOUS LEARNING - CONTROL FEATURES DEMO");
            Console.WriteLine("═".PadRight(80, '═') + "\n");
            
            var service = new ContinuousLearningService(
                dataPath: dataPath,
                workingDirectory: "./continuous_learning_control_demo",
                autoSaveInterval: 500,
                batchSize: 50,
                useIntegration: true
            );
            
            // Start service
            var serviceTask = Task.Run(async () =>
            {
                try
                {
                    await service.StartAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Service error: {ex.Message}");
                }
            });
            
            // Let it run for a bit
            Console.WriteLine("▶️  Service running for 10 seconds...");
            await Task.Delay(10000);
            
            var stats1 = service.GetStatistics();
            Console.WriteLine($"📊 Stats: {stats1.SentencesProcessed:N0} sentences, {stats1.VocabularySize:N0} vocabulary");
            
            // Create control file to pause
            Console.WriteLine("\n⏸️  Creating control file: PAUSE");
            await System.IO.File.WriteAllTextAsync(
                "./continuous_learning_control_demo/control.json",
                "{\"Command\":\"pause\",\"Timestamp\":\"" + DateTime.Now.ToString("o") + "\"}"
            );
            
            await Task.Delay(3000);
            Console.WriteLine("   Service should be paused now...");
            await Task.Delay(5000);
            
            var stats2 = service.GetStatistics();
            Console.WriteLine($"📊 Stats: {stats2.SentencesProcessed:N0} sentences (should be same as before)");
            
            // Resume
            Console.WriteLine("\n▶️  Creating control file: RESUME");
            await System.IO.File.WriteAllTextAsync(
                "./continuous_learning_control_demo/control.json",
                "{\"Command\":\"resume\",\"Timestamp\":\"" + DateTime.Now.ToString("o") + "\"}"
            );
            
            await Task.Delay(3000);
            Console.WriteLine("   Service resumed, running for 5 more seconds...");
            await Task.Delay(5000);
            
            var stats3 = service.GetStatistics();
            Console.WriteLine($"📊 Stats: {stats3.SentencesProcessed:N0} sentences (should be higher now)");
            
            // Stop
            Console.WriteLine("\n🛑 Creating control file: STOP");
            await System.IO.File.WriteAllTextAsync(
                "./continuous_learning_control_demo/control.json",
                "{\"Command\":\"stop\",\"Timestamp\":\"" + DateTime.Now.ToString("o") + "\"}"
            );
            
            await serviceTask;
            
            Console.WriteLine("\n✅ Control features demo complete");
            Console.WriteLine($"   Final: {stats3.SentencesProcessed:N0} sentences, {stats3.VocabularySize:N0} vocabulary");
            Console.WriteLine();
        }
    }
}
