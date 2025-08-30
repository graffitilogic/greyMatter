using System;
using System.Threading.Tasks;

namespace GreyMatter
{
    /// <summary>
    /// Simple test runner for Phase 2
    /// </summary>
    public class Phase2Runner
    {
        public static async Task Main(string[] args)
        // public static async Task RunPhase2Test(string[] args)
        {
            Console.WriteLine("🚀 Starting Phase 2 Quick Test...");

            try
            {
                await Phase2QuickTest.RunQuickTestAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during Phase 2 test: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\n🎉 Phase 2 Test Complete!");
        }
    }
}