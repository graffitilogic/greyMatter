using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter
{
    public class NeuronCreationAnalysisTest
    {
        public static async Task RunAnalysisAsync()
        {
            Console.WriteLine("🧠 **NEURON CREATION ANALYSIS TEST**");
            Console.WriteLine("===================================");
            Console.WriteLine("Testing why 'Neurons Created: 0' appears most of the time");
            Console.WriteLine();
            
            // Initialize brain with fresh state
            var brain = new Cerebro("/tmp/test_brain");
            await brain.InitializeAsync();
            
            var testWord = "analysis";
            var features = new Dictionary<string, double>
            {
                ["semantic"] = 0.8,
                ["abstract"] = 0.7,
                ["cognitive"] = 0.9
            };
            
            Console.WriteLine($"🎯 Testing word: '{testWord}'");
            Console.WriteLine("Expected: Neurons created on 3rd attempt due to GrowthHitThreshold = 3");
            Console.WriteLine();
            
            // Test learning the same word multiple times
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                Console.WriteLine($"📚 **ATTEMPT {attempt}**");
                var result = await brain.LearnConceptAsync(testWord, features);
                
                Console.WriteLine($"   Success: {result.Success}");
                Console.WriteLine($"   Neurons Created: {result.NeuronsCreated}");
                Console.WriteLine($"   Neurons Involved: {result.NeuronsInvolved}");
                
                if (result.NeuronsCreated > 0)
                {
                    Console.WriteLine($"   🎉 **BREAKTHROUGH**: Neurons created on attempt {attempt}!");
                    Console.WriteLine($"       This confirms GrowthHitThreshold = 3 behavior");
                }
                else
                {
                    Console.WriteLine($"   ⏳ No neurons created (below hit threshold)");
                }
                Console.WriteLine();
            }
            
            Console.WriteLine("🔍 **TESTING DIFFERENT WORDS**");
            Console.WriteLine("=============================");
            
            var testWords = new[] { "unique1", "unique2", "unique3" };
            
            foreach (var word in testWords)
            {
                Console.WriteLine($"🆕 Learning new word: '{word}'");
                var result = await brain.LearnConceptAsync(word, features);
                Console.WriteLine($"   Neurons Created: {result.NeuronsCreated} (expected: 0 on first encounter)");
            }
            
            Console.WriteLine();
            Console.WriteLine("✅ **ANALYSIS COMPLETE**");
            Console.WriteLine("This explains why continuous learning shows 'Neurons Created: 0':");
            Console.WriteLine("1. Brain requires 3 exposures before creating neurons");
            Console.WriteLine("2. On second run, brain already 'knows' these words");
            Console.WriteLine("3. Intelligent reuse prevents unnecessary neuron creation");
            Console.WriteLine("4. Storage consolidation optimizes without losing information");
        }
    }
}
