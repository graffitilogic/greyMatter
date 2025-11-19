using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using GreyMatter.Core;
using GreyMatter.Storage;

namespace GreyMatter
{
    /// <summary>
    /// Simple diagnostic to expose the core issue
    /// </summary>
    public class SimpleDiagnostic
    {
        public static async Task RunQuickDiagnosticAsync()
        {
            Console.WriteLine("🔍 **QUICK DIAGNOSTIC - ROOT CAUSE ANALYSIS**");
            Console.WriteLine("==========================================");

            // 1. Check Tatoeba data format
            Console.WriteLine("\n📄 **DATA FORMAT CHECK**");
            var tatoebaPath = "/Volumes/jarvis/trainData/Tatoeba/sentences.csv";
            if (File.Exists(tatoebaPath))
            {
                var lines = await File.ReadAllLinesAsync(tatoebaPath);
                Console.WriteLine($"Tatoeba file exists: {lines.Length} lines");
                Console.WriteLine($"Sample line: {lines[0]}");
                Console.WriteLine($"Format: {(lines[0].Contains('\t') ? "TSV" : "Unknown")}");

                if (lines[0].Contains('\t'))
                {
                    var parts = lines[0].Split('\t');
                    Console.WriteLine($"Columns: ID={parts[0]}, Lang={parts[1]}, Text={parts[2]}");
                }
            }
            else
            {
                Console.WriteLine("❌ Tatoeba file not found!");
            }

            // 2. Check what the system is actually learning
            Console.WriteLine("\n🧠 **LEARNING CHECK**");
            var brainPath = "/Volumes/jarvis/brainData";
            var storageManager = new SemanticStorageManager(brainPath);
            var encoder = new LearningSparseConceptEncoder(storageManager);

            var testWords = new[] { "cat", "dog", "the", "and", "is" };
            foreach (var word in testWords)
            {
                try
                {
                    var pattern = await encoder.EncodeLearnedWordAsync(word);
                    Console.WriteLine($"{word}: {pattern.ActiveBits.Length} active neurons");

                    // Check if word appears in recent Tatoeba data
                    var recentLines = await File.ReadAllLinesAsync(tatoebaPath);
                    var wordCount = recentLines.Count(line =>
                        line.Contains(word, StringComparison.OrdinalIgnoreCase));

                    Console.WriteLine($"  ↳ Appears in {wordCount} Tatoeba sentences");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{word}: ERROR - {ex.Message}");
                }
            }

            // 3. Check brain data content
            Console.WriteLine("\n💾 **BRAIN DATA ANALYSIS**");
            var brainDataPath = "/Volumes/jarvis/brainData/hierarchical/partition_metadata.json";
            if (File.Exists(brainDataPath))
            {
                var content = await File.ReadAllTextAsync(brainDataPath);
                var emotionalCount = content.Split(new[] { "emotional_memory" }, StringSplitOptions.None).Length - 1;
                var languageCount = content.Split(new[] { "language_data" }, StringSplitOptions.None).Length - 1;

                // Also check for actual learned patterns and vocabulary
                var learnedPatternCount = content.Split(new[] { "learned_" }, StringSplitOptions.None).Length - 1;
                var vocabularyCount = content.Split(new[] { "vocabulary" }, StringSplitOptions.None).Length - 1;

                Console.WriteLine($"Emotional memory patterns: {emotionalCount}");
                Console.WriteLine($"Language data patterns: {languageCount}");
                Console.WriteLine($"Learned word patterns: {learnedPatternCount}");
                Console.WriteLine($"Vocabulary entries: {vocabularyCount}");

                if (learnedPatternCount > 0 || vocabularyCount > 10)
                {
                    Console.WriteLine(" **LANGUAGE LEARNING DETECTED** - Found learned patterns and vocabulary");
                }
                else if (emotionalCount > languageCount)
                {
                    Console.WriteLine("⚠️  SYSTEM IS PRIMARILY DOING EMOTIONAL PROCESSING, NOT LANGUAGE LEARNING!");
                }
                else
                {
                    Console.WriteLine("🤔 **UNCLEAR LEARNING STATE** - Limited language learning activity detected");
                }
            }

            Console.WriteLine("\n🎯 **DIAGNOSIS**");
            Console.WriteLine("1. Tatoeba data is in CSV format, not JSON");
            Console.WriteLine("2. System should now learn from actual sentence data instead of algorithmic fallbacks");
            Console.WriteLine("3. Check for learned patterns and vocabulary in brain data");
            Console.WriteLine("4. If algorithmic fallback still occurs, the learning pipeline may need regeneration");
        }
    }
}
