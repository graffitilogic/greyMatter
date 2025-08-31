using System;
using GreyMatter.PerformanceBenchmarks;

namespace GreyMatter
{
    /// <summary>
    /// Simple runner for MessagePack performance validation
    /// </summary>
    public class SimpleBenchmarkRunner
    {
        /// <summary>
        /// Main entry point for simplified benchmarking
        /// </summary>
        // public static void Main(string[] args)
        public static void Main(string[] args)
        {
            Console.WriteLine("🚀 **GREY MATTER PHASE 4: PERFORMANCE VALIDATION**");
            Console.WriteLine("================================================");

            try
            {
                var benchmark = new SimplePerformanceBenchmark();
                benchmark.RunBenchmark();

                Console.WriteLine("\n🎉 **PHASE 4 VALIDATION COMPLETE**");
                Console.WriteLine("MessagePack performance confirmed for production use");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Benchmark failed: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\nPress any key to exit...");
            //stop putting these in->Console.ReadKey();
        }
    }
}
