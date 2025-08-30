using System;
using System.Threading.Tasks;
using GreyMatter.Storage;
using GreyMatter.PerformanceBenchmarks;

namespace GreyMatter
{
    /// <summary>
    /// Runner for comprehensive performance benchmarking
    /// </summary>
    public class PerformanceBenchmarkRunner
    {
        /// <summary>
        /// Main entry point for performance benchmarking
        /// </summary>
        // public static async Task Main(string[] args)
        // {
        //     Console.WriteLine("🧪 **GREY MATTER PERFORMANCE BENCHMARK RUNNER**");
        //     Console.WriteLine("=============================================");

        //     try
        //     {
        //         // Initialize storage manager
        //         var config = new CerebroConfiguration();
        //         await config.ValidateAndSetupAsync();

        //         var storageManager = new SemanticStorageManager(config);

        //         // Initialize benchmark suite
        //         var benchmarkSuite = new PerformanceBenchmarkSuite(storageManager);

        //         // Run comprehensive benchmarks
        //         await benchmarkSuite.RunComprehensiveBenchmark();

        //         Console.WriteLine("\n🎉 **PERFORMANCE BENCHMARKING COMPLETED**");
        //         Console.WriteLine("Check the benchmark_results directory for detailed reports");
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"❌ Benchmark failed: {ex.Message}");
        //         Console.WriteLine(ex.StackTrace);
        //     }

        //     Console.WriteLine("\nPress any key to exit...");
        //     Console.ReadKey();
        // }
    }
}
