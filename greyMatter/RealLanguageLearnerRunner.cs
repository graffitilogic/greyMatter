using System;
using System.Threading.Tasks;

namespace GreyMatter
{
    /// <summary>
    /// Simple runner for RealLanguageLearner to test biological encoding
    /// </summary>
    public class RealLanguageLearnerRunner
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🧬 **BIOLOGICAL LEARNING TEST - REAL LANGUAGE LEARNER**");
            Console.WriteLine("======================================================");

            try
            {
                // Initialize with biological encoding
                var learner = new RealLanguageLearner("/Volumes/jarvis/trainData/learning_data", "/Volumes/jarvis/brainData");

                // Run biological learning with small batch
                await learner.LearnFromTatoebaDataAsync(50); // Learn just 50 words for testing

                Console.WriteLine("\n✅ **BIOLOGICAL LEARNING TEST COMPLETE**");
                Console.WriteLine("Check learned_words.json and neural clusters for biological encoding");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in biological learning test: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}