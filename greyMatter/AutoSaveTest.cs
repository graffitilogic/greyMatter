using System;
using System.Threading.Tasks;
using GreyMatter.Core;
using GreyMatter.Storage;

namespace GreyMatter.demos
{
    public class AutoSaveTest
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🧠 **GREYMATTER AUTO-SAVE TEST**");
            Console.WriteLine("=====================================");
            
            // Check initial storage size
            Console.WriteLine($"📁 Initial storage size check...");
            var initialProcess = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "du",
                    Arguments = "-sh /Volumes/jarvis/brainData",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            initialProcess.Start();
            string initialOutput = await initialProcess.StandardOutput.ReadToEndAsync();
            await initialProcess.WaitForExitAsync();
            Console.WriteLine($"📊 Initial size: {initialOutput.Trim()}");
            
            // Initialize brain with NAS storage
            var brain = new Cerebro("/Volumes/jarvis/brainData");
            await brain.InitializeAsync();
            
            Console.WriteLine($"📁 Brain storage path: {brain.GetStoragePath()}");
            
            // Initialize enhanced continuous learner
            var learner = new EnhancedContinuousLearner(brain, "/Volumes/jarvis/trainData");
            
            Console.WriteLine("🎯 Starting learning with 10 words (should trigger auto-save every 10)...");
            
            // Learn only 10 words to test auto-save without triggering consolidation
            int wordsLearned = await learner.StartContinuousLearningAsync(10);
            
            Console.WriteLine($"✅ Learning completed: {wordsLearned} words learned");
            
            // Check final storage size
            Console.WriteLine($"📁 Final storage size check...");
            var finalProcess = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "du",
                    Arguments = "-sh /Volumes/jarvis/brainData",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            finalProcess.Start();
            string finalOutput = await finalProcess.StandardOutput.ReadToEndAsync();
            await finalProcess.WaitForExitAsync();
            Console.WriteLine($"📊 Final size: {finalOutput.Trim()}");
            
            Console.WriteLine("🎉 Test completed!");
        }
    }
}
