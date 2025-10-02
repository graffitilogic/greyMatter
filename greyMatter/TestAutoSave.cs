using System;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter
{
    /// <summary>
    /// Test the auto-save functionality of EnhancedContinuousLearner
    /// </summary>
    public class TestAutoSave
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Console.WriteLine("🧠 **AUTO-SAVE TEST**");
                Console.WriteLine("====================");
                Console.WriteLine("Testing EnhancedContinuousLearner auto-save functionality");
                Console.WriteLine();

                // Parse max words from command line (default 300)
                int maxWords = 300;
                if (args.Length > 0 && int.TryParse(args[0], out int parsedWords))
                {
                    maxWords = parsedWords;
                }

                Console.WriteLine($"📊 Will learn {maxWords} words to test auto-save every 100 words");
                Console.WriteLine();

                // Initialize brain with actual NAS storage
                var brainPath = "/Volumes/jarvis/brainData";
                var brain = new Cerebro(brainPath);
                
                Console.WriteLine($"📁 Brain initialized at: {brain.GetStoragePath()}");
                Console.WriteLine();

                // Initialize enhanced continuous learner with NAS training data
                var trainDataPath = "/Volumes/jarvis/trainData";
                var learner = new EnhancedContinuousLearner(brain, trainDataPath);

                Console.WriteLine("🚀 Starting enhanced continuous learning with auto-save...");
                Console.WriteLine();

                // Start learning (this should trigger auto-save every 100 words)
                int wordsLearned = await learner.StartContinuousLearningAsync(maxWords);
                Console.WriteLine($"📚 Learned {wordsLearned} words total");

                Console.WriteLine();
                Console.WriteLine("✅ **AUTO-SAVE TEST COMPLETE**");
                Console.WriteLine($"📁 Check storage at: {brain.GetStoragePath()}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return 1;
            }
        }
    }
}
