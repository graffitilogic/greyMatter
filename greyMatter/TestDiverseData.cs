using System;
using System.Linq;
using GreyMatter.Core;

namespace GreyMatter
{
    /// <summary>
    /// Test loading diverse training data sources
    /// </summary>
    public class TestDiverseData
    {
        public static void Run()
        {
            Console.WriteLine("🔍 TESTING DIVERSE TRAINING DATA SOURCES\n");
            Console.WriteLine("═".PadRight(80, '═'));
            
            var provider = new TrainingDataProvider();
            var datasets = provider.GetAvailableDatasets();
            
            Console.WriteLine($"\n📚 Found {datasets.Count} datasets:\n");
            
            foreach (var kvp in datasets.OrderBy(x => x.Value.DifficultyLevel))
            {
                var dataset = kvp.Value;
                Console.WriteLine($"  {kvp.Key,-20} | {dataset.DifficultyLevel,-15} | {dataset.Format,-18}");
                Console.WriteLine($"  {"  └─",-20} {dataset.Description}");
                Console.WriteLine();
            }
            
            Console.WriteLine("\n" + "═".PadRight(80, '═'));
            Console.WriteLine("📖 TESTING PROGRESSIVE CURRICULUM\n");
            
            var curriculum = provider.GetProgressiveCurriculum();
            
            TestPhase("Phase 1: Foundation", curriculum.Phase1_Foundation, provider);
            TestPhase("Phase 2: Expansion", curriculum.Phase2_Expansion, provider);
            TestPhase("Phase 3: Advanced", curriculum.Phase3_Advanced, provider);
            TestPhase("Phase 4: Mastery", curriculum.Phase4_Mastery, provider);
            
            Console.WriteLine("\n" + "═".PadRight(80, '═'));
            Console.WriteLine("✅ DIVERSE DATA TEST COMPLETE\n");
        }
        
        private static void TestPhase(string phaseName, TrainingPhase phase, TrainingDataProvider provider)
        {
            Console.WriteLine($"\n{phaseName}:");
            Console.WriteLine($"  Dataset: {phase.DatasetKey}");
            Console.WriteLine($"  Description: {phase.Description}");
            
            try
            {
                var samples = provider.LoadSentences(
                    phase.DatasetKey,
                    maxSentences: 5,  // Just 5 samples
                    minWordCount: phase.MinWordCount,
                    maxWordCount: phase.MaxWordCount,
                    shuffle: false
                ).ToList();
                
                if (samples.Any())
                {
                    Console.WriteLine($"  ✅ Loaded {samples.Count} samples:");
                    foreach (var sample in samples.Take(3))
                    {
                        var preview = sample.Length > 100 ? sample.Substring(0, 100) + "..." : sample;
                        var wordCount = sample.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                        Console.WriteLine($"     • ({wordCount} words) {preview}");
                    }
                }
                else
                {
                    Console.WriteLine("  ⚠️  No samples loaded");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Error: {ex.Message}");
            }
        }
    }
}
