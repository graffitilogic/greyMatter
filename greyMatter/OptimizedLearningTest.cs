using System;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;
using greyMatter.Learning;

namespace greyMatter
{
    /// <summary>
    /// Test the optimized neuron learning approach to verify efficiency improvements
    /// </summary>
    public class OptimizedLearningTest
    {
        public static async Task TestOptimizedLearning()
        {
            Console.WriteLine("🔬 **OPTIMIZED LEARNING TEST**");
            Console.WriteLine("==================================================");
            Console.WriteLine("Testing neuron efficiency improvements\n");

            try
            {
                // Initialize brain with optimized approach
                var cerebro = new Cerebro("/Volumes/jarvis/brainData");
                await cerebro.InitializeAsync();

                // Test sentences with repeated words to test reuse
                var testSentences = new[]
                {
                    "The cat sits on the mat",
                    "The dog runs in the park",
                    "The bird flies over the tree",
                    "Tom likes the red cat",
                    "Mary reads the good book",
                    "The cat plays with Tom",
                    "The dog barks at the cat",
                    "Tom and Mary walk in the park"
                };

                Console.WriteLine($"🎯 Testing {testSentences.Length} sentences for neuron reuse...\n");

                var totalNeuronsUsed = 0;
                var totalNeuronsCreated = 0;
                var totalNeuronsReused = 0;

                foreach (var sentence in testSentences)
                {
                    Console.WriteLine($"📝 Learning: \"{sentence}\"");
                    
                    var result = await cerebro.LearnSentenceOptimizedAsync(sentence, "general");
                    
                    totalNeuronsUsed += result.TotalNeuronsUsed;
                    totalNeuronsCreated += result.NeuronsCreated;
                    totalNeuronsReused += result.NeuronsReused;

                    Console.WriteLine($"   Neurons: {result.TotalNeuronsUsed} used ({result.NeuronsCreated} new, {result.NeuronsReused} reused)");
                    Console.WriteLine($"   Reuse efficiency: {result.ReuseEfficiency:P1}");
                    Console.WriteLine($"   Words processed: {result.WordsExtracted}");
                    
                    // Show word-level details for first few sentences
                    if (Array.IndexOf(testSentences, sentence) < 3)
                    {
                        Console.WriteLine($"   Word details:");
                        foreach (var wordResult in result.WordResults.Take(5))
                        {
                            Console.WriteLine($"      '{wordResult.Word}': {wordResult.NeuronsUsed} neurons ({(wordResult.WasNewWord ? "NEW" : "REUSED")})");
                        }
                    }
                    Console.WriteLine();
                }

                // Overall statistics
                Console.WriteLine("📊 **OVERALL STATISTICS**");
                Console.WriteLine($"   Total sentences: {testSentences.Length}");
                Console.WriteLine($"   Total neurons used: {totalNeuronsUsed:N0}");
                Console.WriteLine($"   Neurons created: {totalNeuronsCreated:N0}");
                Console.WriteLine($"   Neurons reused: {totalNeuronsReused:N0}");
                Console.WriteLine($"   Overall reuse efficiency: {(totalNeuronsUsed > 0 ? (double)totalNeuronsReused / totalNeuronsUsed : 0):P1}");
                Console.WriteLine($"   Avg neurons per sentence: {(double)totalNeuronsUsed / testSentences.Length:F1}");
                
                // Compare with expected unoptimized performance
                var expectedUnoptimized = testSentences.Length * 713; // Previous performance
                var improvement = expectedUnoptimized - totalNeuronsUsed;
                var improvementPercent = (double)improvement / expectedUnoptimized;
                
                Console.WriteLine($"\n🚀 **OPTIMIZATION RESULTS**");
                Console.WriteLine($"   Expected (unoptimized): {expectedUnoptimized:N0} neurons");
                Console.WriteLine($"   Actual (optimized): {totalNeuronsUsed:N0} neurons");
                Console.WriteLine($"   Improvement: {improvement:N0} neurons saved ({improvementPercent:P1} reduction)");

                // Get efficiency statistics
                var efficiencyStats = CerebroOptimizations.GetGlobalEfficiencyStats();
                foreach (var domainStats in efficiencyStats)
                {
                    var stats = domainStats.Value;
                    Console.WriteLine($"\n📈 **DOMAIN '{domainStats.Key.ToUpper()}' EFFICIENCY**");
                    Console.WriteLine($"   Total unique words: {stats.TotalWords:N0}");
                    Console.WriteLine($"   Total neurons: {stats.TotalNeurons:N0}");
                    Console.WriteLine($"   Avg neurons per word: {stats.AverageNeuronsPerWord:F1}");
                    Console.WriteLine($"   Reuse efficiency: {stats.ReuseEfficiency:F1}x");
                    
                    Console.WriteLine($"   Top frequent words:");
                    foreach (var word in stats.TopFrequentWords.Take(5))
                    {
                        Console.WriteLine($"      '{word.Key}': {word.Value} occurrences");
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
