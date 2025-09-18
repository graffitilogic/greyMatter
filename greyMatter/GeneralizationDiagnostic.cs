using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using GreyMatter.Core;
using GreyMatter.Storage;
using GreyMatter.Learning;

namespace GreyMatter
{
    /// <summary>
    /// Diagnostic tool to analyze why generalization test is failing
    /// </summary>
    public class GeneralizationDiagnostic
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🔍 **GENERALIZATION DIAGNOSTIC**");
            Console.WriteLine("===============================");

            try
            {
                // Initialize the encoder
                var storage = new SemanticStorageManager("/Volumes/jarvis/brainData", "/Volumes/jarvis/trainData");
                var encoder = new LearningSparseConceptEncoder(storage);

                // Load learned patterns
                await encoder.LoadLearnedPatternsFromFileAsync("/Volumes/jarvis/trainData/Tatoeba/learning_data/learned_patterns.json");
                Console.WriteLine("✅ Loaded learned patterns");

                // Load actually learned words
                var learnedWords = await LoadActualLearnedWordsAsync();
                Console.WriteLine($"✅ Loaded {learnedWords.Count} learned words: {string.Join(", ", learnedWords)}");

                if (learnedWords.Count < 4)
                {
                    Console.WriteLine("❌ Not enough learned words for generalization analysis");
                    return;
                }

                // Analyze all pairwise similarities
                Console.WriteLine("\n📊 **PATTERN SIMILARITY ANALYSIS**");
                Console.WriteLine("==================================");

                var similarities = new List<(string word1, string word2, double similarity)>();

                for (int i = 0; i < learnedWords.Count; i++)
                {
                    for (int j = i + 1; j < learnedWords.Count; j++)
                    {
                        var word1 = learnedWords[i];
                        var word2 = learnedWords[j];

                        var pattern1 = await encoder.EncodeLearnedWordAsync(word1);
                        var pattern2 = await encoder.EncodeLearnedWordAsync(word2);

                        var similarity = CalculatePatternSimilarity(pattern1, pattern2);
                        similarities.Add((word1, word2, similarity));

                        Console.WriteLine($"   {word1} <-> {word2}: {similarity:F4} (bits: {pattern1.ActiveBits?.Length ?? 0}, {pattern2.ActiveBits?.Length ?? 0})");
                    }
                }

                // Analyze the distribution
                Console.WriteLine("\n📈 **SIMILARITY DISTRIBUTION**");
                Console.WriteLine("=============================");
                
                var sortedSimilarities = similarities.OrderBy(s => s.similarity).ToList();
                Console.WriteLine($"   Minimum: {sortedSimilarities.First().similarity:F4} ({sortedSimilarities.First().word1} <-> {sortedSimilarities.First().word2})");
                Console.WriteLine($"   Maximum: {sortedSimilarities.Last().similarity:F4} ({sortedSimilarities.Last().word1} <-> {sortedSimilarities.Last().word2})");
                Console.WriteLine($"   Average: {similarities.Average(s => s.similarity):F4}");
                Console.WriteLine($"   Median: {sortedSimilarities[sortedSimilarities.Count / 2].similarity:F4}");

                // Check generalization test pairs specifically
                Console.WriteLine("\n🎯 **GENERALIZATION TEST PAIRS**");
                Console.WriteLine("===============================");
                
                var testPairs = new List<(string, string)>();
                for (int i = 0; i < Math.Min(learnedWords.Count - 2, 5); i += 2)
                {
                    if (i + 2 < learnedWords.Count)
                    {
                        testPairs.Add((learnedWords[i], learnedWords[i + 2])); // Skip one word to test generalization
                    }
                }

                int passingPairs = 0;
                foreach (var (word1, word2) in testPairs)
                {
                    var matchingSim = similarities.FirstOrDefault(s => 
                        (s.word1 == word1 && s.word2 == word2) || 
                        (s.word1 == word2 && s.word2 == word1));
                    
                    var similarity = matchingSim.similarity;
                    var passes = similarity > 0.05 && similarity < 0.9;
                    
                    Console.WriteLine($"   {word1} <-> {word2}: {similarity:F4} {(passes ? "✅ PASS" : "❌ FAIL")} (target: 0.05 < sim < 0.9)");
                    
                    if (passes) passingPairs++;
                }

                Console.WriteLine($"\n🎯 **TEST RESULTS**: {passingPairs}/{testPairs.Count} pairs passed generalization test");

                // Recommendations
                Console.WriteLine("\n💡 **DIAGNOSTIC RECOMMENDATIONS**");
                Console.WriteLine("=================================");
                
                if (similarities.All(s => s.similarity < 0.05))
                {
                    Console.WriteLine("   🔍 ISSUE: All patterns too dissimilar (< 0.05)");
                    Console.WriteLine("   💡 SOLUTION: Patterns may be too sparse or contexts too different");
                    Console.WriteLine("   📝 ACTION: Increase pattern overlap or reduce context sensitivity");
                }
                else if (similarities.All(s => s.similarity > 0.9))
                {
                    Console.WriteLine("   🔍 ISSUE: All patterns too similar (> 0.9)");
                    Console.WriteLine("   💡 SOLUTION: Patterns may be too generic or contexts too similar");
                    Console.WriteLine("   📝 ACTION: Increase pattern differentiation or context sensitivity");
                }
                else if (similarities.Any(s => s.similarity > 0.05 && s.similarity < 0.9))
                {
                    Console.WriteLine("   🔍 ISSUE: Some patterns in range, but test pairs are not");
                    Console.WriteLine("   💡 SOLUTION: Generalization test methodology may need adjustment");
                    Console.WriteLine("   📝 ACTION: Consider different test pair selection strategy");
                }
                else
                {
                    Console.WriteLine("   🔍 ISSUE: Pattern distribution doesn't fit generalization expectations");
                    Console.WriteLine("   💡 SOLUTION: Review pattern generation and similarity calculation");
                    Console.WriteLine("   📝 ACTION: Analyze pattern encoding algorithm");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during diagnostic: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
        }

        private static double CalculatePatternSimilarity(SparsePattern pattern1, SparsePattern pattern2)
        {
            if (pattern1.ActiveBits?.Length == 0 || pattern2.ActiveBits?.Length == 0)
                return 0.0;

            var intersection = pattern1.ActiveBits.Intersect(pattern2.ActiveBits ?? new int[0]).Count();
            var union = pattern1.ActiveBits.Union(pattern2.ActiveBits ?? new int[0]).Count();

            return union > 0 ? (double)intersection / union : 0.0;
        }

        private static async Task<List<string>> LoadActualLearnedWordsAsync()
        {
            try
            {
                var learnedWordsPath = "/Volumes/jarvis/brainData/learned_words.json";
                if (File.Exists(learnedWordsPath))
                {
                    var json = await File.ReadAllTextAsync(learnedWordsPath);
                    var words = JsonSerializer.Deserialize<List<string>>(json);
                    return words ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to load learned words: {ex.Message}");
            }
            
            return new List<string>();
        }
    }
}
