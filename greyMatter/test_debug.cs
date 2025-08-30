using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter
{
    /// <summary>
    /// Simple test class to debug specific inputs
    /// </summary>
    public class TestDebug
    {
        public static async Task RunTestAsync()
        {
            Console.WriteLine("🔍 Running debug test...");
            
            // Initialize with proper NAS configuration
            var config = new CerebroConfiguration();
            config.ValidateAndSetup();
            var brain = new Cerebro(config.BrainDataPath);
            await brain.InitializeAsync();

            // Test with red features - should match what was learned
            var result = await brain.ProcessInputAsync("What is red?", 
                new Dictionary<string, double> 
                { 
                    ["wavelength"] = 0.7, 
                    ["warmth"] = 0.8, 
                    ["intensity"] = 0.9, 
                    ["brightness"] = 0.6 
                });

            Console.WriteLine($"Result: {result.Response}");
            Console.WriteLine($"Confidence: {result.Confidence:F2}");
        }
    }
}
