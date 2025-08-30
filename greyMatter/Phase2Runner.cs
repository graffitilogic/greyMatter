using System;
using System.Threading.Tasks;
using greyMatter;

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
            // Check for neuron growth diagnostic first
            if (args.Length > 0 && (args[0] == "--diagnostic" || args[0] == "--analyze-growth"))
            {
                var diagnostic = new NeuronGrowthDiagnostic();
                await diagnostic.RunDiagnostic();
                return;
            }

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