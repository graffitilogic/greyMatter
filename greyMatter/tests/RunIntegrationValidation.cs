using System;
using System.Threading.Tasks;
using GreyMatter.Tests;

namespace GreyMatter.Tests
{
    /// <summary>
    /// Simple runner for Week 5 Integration Validation Test
    /// Executes comprehensive comparison of three integration modes.
    /// </summary>
    public class RunIntegrationValidation
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Starting Week 5 Integration Validation...\n");
            
            try
            {
                await IntegrationValidationTest.RunValidationAsync();
                
                Console.WriteLine("\n Integration validation completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error during validation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
