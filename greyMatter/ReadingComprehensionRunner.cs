using System;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter
{
    /// <summary>
    /// Runner for the Reading Comprehension Demo
    /// </summary>
    public class ReadingComprehensionRunner
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🧠 **GREYMatter Reading Comprehension Runner**");
            Console.WriteLine("============================================");

            try
            {
                // Initialize configuration
                var config = new CerebroConfiguration();
                Console.WriteLine($"📁 Brain Data Path: {config.BrainDataPath}");
                Console.WriteLine($"📚 Training Data Root: {config.TrainingDataRoot}");

                // Initialize Cerebro brain
                var cerebro = new Cerebro();
                await cerebro.InitializeAsync();
                Console.WriteLine("✅ Cerebro brain initialized");

                // Create and run reading comprehension demo
                var demo = new ReadingComprehensionDemo(cerebro, config);
                await demo.RunDemoAsync();

                Console.WriteLine("🎉 Reading comprehension demo completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error running reading comprehension demo: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
