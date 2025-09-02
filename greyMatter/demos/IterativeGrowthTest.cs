using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using greyMatter.Core;
using greyMatter.Learning;

namespace greyMatter
{
    /// <summary>
    /// Test iterative growth to validate randomness and concept consolidation
    /// </summary>
    public static class IterativeGrowthTest
    {
        public static async Task RunIterativeGrowthAnalysis(int sampleSize, int iterations)
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              ITERATIVE GROWTH ANALYSIS                        ║");
            Console.WriteLine("║        Validating Randomness & Concept Consolidation          ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine($"🔬 Test Parameters:");
            Console.WriteLine($"   • Sample size per iteration: {sampleSize:N0}");
            Console.WriteLine($"   • Number of iterations: {iterations}");
            Console.WriteLine($"   • Total sentences across all runs: {sampleSize * iterations:N0}");
            Console.WriteLine();

            var brain = new LanguageEphemeralBrain();
            var dataPath = "/Volumes/jarvis/trainData/Tatoeba";
            var trainer = new TatoebaLanguageTrainer(dataPath);

            var iterationResults = new List<IterationResult>();
            
            for (int i = 1; i <= iterations; i++)
            {
                Console.WriteLine($"═══════════════════════════════════════════════════════════════");
                Console.WriteLine($"ITERATION {i}/{iterations} - Sample Size: {sampleSize:N0}");
                Console.WriteLine($"═══════════════════════════════════════════════════════════════");

                var startStats = trainer.Brain.GetLearningStats();
                var startTime = DateTime.Now;
                
                // Get random starting position for this iteration
                var random = new Random();
                var totalSentences = await CountTotalSentences(dataPath);
                var maxStartPosition = Math.Max(0, totalSentences - sampleSize);
                var startPosition = random.Next(0, maxStartPosition);
                
                Console.WriteLine($"🎲 Random start position: {startPosition:N0}");
                Console.WriteLine($"📏 Sample range: {startPosition:N0} to {startPosition + sampleSize:N0}");
                
                // Train on random sample
                await trainer.TrainWithRandomSample(startPosition, sampleSize, 10000);
                
                var endStats = trainer.Brain.GetLearningStats();
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                
                // Calculate growth metrics
                var conceptGrowth = endStats.TotalConcepts - startStats.TotalConcepts;
                var vocabGrowth = endStats.VocabularySize - startStats.VocabularySize;
                var associationGrowth = endStats.WordAssociationCount - startStats.WordAssociationCount;
                
                // Save brain state and measure disk usage
                await trainer.SaveTrainedBrain();
                var diskSize = await GetBrainDiskSize();
                
                var result = new IterationResult
                {
                    Iteration = i,
                    StartPosition = startPosition,
                    SampleSize = sampleSize,
                    Duration = duration,
                    ConceptGrowth = conceptGrowth,
                    VocabGrowth = vocabGrowth,
                    AssociationGrowth = associationGrowth,
                    TotalConcepts = endStats.TotalConcepts,
                    TotalVocab = endStats.VocabularySize,
                    TotalAssociations = endStats.WordAssociationCount,
                    DiskSizeBytes = diskSize
                };
                
                iterationResults.Add(result);
                
                // Display iteration results
                Console.WriteLine($"\n📊 Iteration {i} Results:");
                Console.WriteLine($"   🧠 Concept growth: +{conceptGrowth:N0} (total: {endStats.TotalConcepts:N0})");
                Console.WriteLine($"   📚 Vocab growth: +{vocabGrowth:N0} (total: {endStats.VocabularySize:N0})");
                Console.WriteLine($"   🔗 Association growth: +{associationGrowth:N0} (total: {endStats.WordAssociationCount:N0})");
                Console.WriteLine($"   💾 Disk size: {FormatBytes(diskSize)}");
                Console.WriteLine($"   ⏱️ Duration: {duration:mm\\:ss}");
                
                // Analyze growth patterns
                if (i > 1)
                {
                    var prevResult = iterationResults[i - 2];
                    var conceptGrowthRate = (double)conceptGrowth / prevResult.ConceptGrowth;
                    var vocabGrowthRate = (double)vocabGrowth / prevResult.VocabGrowth;
                    var diskGrowthRate = (double)diskSize / prevResult.DiskSizeBytes;
                    
                    Console.WriteLine($"\n📈 Growth Rate Analysis (vs iteration {i-1}):");
                    Console.WriteLine($"   🧠 Concept growth rate: {conceptGrowthRate:F2}x");
                    Console.WriteLine($"   📚 Vocab growth rate: {vocabGrowthRate:F2}x");
                    Console.WriteLine($"   💾 Disk growth rate: {diskGrowthRate:F2}x");
                    
                    // Check for plateau indicators
                    if (conceptGrowthRate < 0.1)
                    {
                        Console.WriteLine($"   ⚠️  PLATEAU DETECTED: Concept growth <10% of previous iteration");
                    }
                    if (vocabGrowthRate < 0.1)
                    {
                        Console.WriteLine($"   ⚠️  PLATEAU DETECTED: Vocabulary growth <10% of previous iteration");
                    }
                    if (diskGrowthRate < 1.05)
                    {
                        Console.WriteLine($"   ✅ CONSOLIDATION: Disk growth <5% indicates effective concept merging");
                    }
                }
                
                Console.WriteLine();
            }
            
            // Final analysis
            await DisplayFinalAnalysis(iterationResults, sampleSize);
        }
        
        private static async Task<int> CountTotalSentences(string dataPath)
        {
            var sentencesPath = Path.Combine(dataPath, "sentences_eng_small.csv");
            if (!File.Exists(sentencesPath))
            {
                sentencesPath = Path.Combine(dataPath, "sentences.csv");
            }
            
            if (!File.Exists(sentencesPath))
            {
                return 50000; // Default fallback
            }
            
            var count = 0;
            using var fs = new FileStream(sentencesPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);
            
            string? line;
            while ((line = await sr.ReadLineAsync()) != null)
            {
                var parts = line.Split('\t');
                if (parts.Length >= 2 && parts[1].Equals("eng", StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }
            
            return count;
        }
        
        private static async Task<long> GetBrainDiskSize()
        {
            var brainPath = "/Volumes/jarvis/brainData";
            if (!Directory.Exists(brainPath))
                return 0;
                
            long totalSize = 0;
            var files = Directory.GetFiles(brainPath, "*", SearchOption.AllDirectories);
            
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                totalSize += fileInfo.Length;
            }
            
            return totalSize;
        }
        
        private static async Task DisplayFinalAnalysis(List<IterationResult> results, int sampleSize)
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    FINAL ANALYSIS                             ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");
            
            // Randomness validation
            Console.WriteLine("🎲 RANDOMNESS VALIDATION:");
            var positions = results.Select(r => r.StartPosition).ToList();
            var uniquePositions = positions.Distinct().Count();
            var totalPositions = positions.Count;
            
            Console.WriteLine($"   • Unique starting positions: {uniquePositions}/{totalPositions}");
            Console.WriteLine($"   • Randomness score: {(double)uniquePositions/totalPositions*100:F1}%");
            
            if (uniquePositions == totalPositions)
            {
                Console.WriteLine($"   ✅ PERFECT RANDOMNESS: All iterations used different starting positions");
            }
            else
            {
                Console.WriteLine($"   ⚠️  COLLISION DETECTED: Some iterations may have overlapped");
            }
            
            Console.WriteLine($"\n📊 Position spread analysis:");
            var minPos = positions.Min();
            var maxPos = positions.Max();
            var avgPos = positions.Average();
            Console.WriteLine($"   • Min position: {minPos:N0}");
            Console.WriteLine($"   • Max position: {maxPos:N0}");
            Console.WriteLine($"   • Average position: {avgPos:N0}");
            Console.WriteLine($"   • Dataset coverage: {(maxPos - minPos + sampleSize):N0} sentences");
            
            // Growth pattern analysis
            Console.WriteLine($"\n📈 GROWTH PATTERN ANALYSIS:");
            var firstResult = results.First();
            var lastResult = results.Last();
            
            Console.WriteLine($"   • Total concept growth: {lastResult.TotalConcepts - firstResult.TotalConcepts:N0}");
            Console.WriteLine($"   • Total vocab growth: {lastResult.TotalVocab - firstResult.TotalVocab:N0}");
            Console.WriteLine($"   • Final disk size: {FormatBytes(lastResult.DiskSizeBytes)}");
            
            // Calculate diminishing returns
            if (results.Count >= 3)
            {
                var earlyGrowth = results[1].ConceptGrowth; // Second iteration growth
                var lateGrowth = results.Last().ConceptGrowth; // Last iteration growth
                var diminishingFactor = (double)lateGrowth / earlyGrowth;
                
                Console.WriteLine($"\n🔍 CONCEPT CONSOLIDATION ANALYSIS:");
                Console.WriteLine($"   • Early iteration growth: {earlyGrowth:N0} concepts");
                Console.WriteLine($"   • Late iteration growth: {lateGrowth:N0} concepts");
                Console.WriteLine($"   • Diminishing factor: {diminishingFactor:F2}x");
                
                if (diminishingFactor < 0.3)
                {
                    Console.WriteLine($"   ✅ STRONG CONSOLIDATION: Growth reduced to {diminishingFactor*100:F0}% - concept merging working well");
                }
                else if (diminishingFactor < 0.7)
                {
                    Console.WriteLine($"   📊 MODERATE CONSOLIDATION: Some concept overlap detected");
                }
                else
                {
                    Console.WriteLine($"   ⚠️  WEAK CONSOLIDATION: Limited concept merging - may indicate poor overlap or encoding issues");
                }
            }
            
            // Disk efficiency analysis
            Console.WriteLine($"\n💾 STORAGE EFFICIENCY:");
            var totalSentencesProcessed = results.Sum(r => r.SampleSize);
            var bytesPerSentence = (double)lastResult.DiskSizeBytes / totalSentencesProcessed;
            var bytesPerConcept = (double)lastResult.DiskSizeBytes / lastResult.TotalConcepts;
            
            Console.WriteLine($"   • Total sentences processed: {totalSentencesProcessed:N0}");
            Console.WriteLine($"   • Bytes per sentence: {bytesPerSentence:F1}");
            Console.WriteLine($"   • Bytes per concept: {bytesPerConcept:F1}");
            Console.WriteLine($"   • Concepts per sentence: {(double)lastResult.TotalConcepts/totalSentencesProcessed:F1}");
            
            // Generate iteration table
            Console.WriteLine($"\n📋 ITERATION SUMMARY TABLE:");
            Console.WriteLine($"{"Iter",-4} | {"Start Pos",-10} | {"Concepts",-10} | {"Vocab",-8} | {"Disk Size",-10} | {"Growth Rate",-12}");
            Console.WriteLine(new string('-', 70));
            
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                var growthRate = i == 0 ? "baseline" : $"{(double)result.ConceptGrowth / results[i-1].ConceptGrowth:F2}x";
                
                Console.WriteLine($"{result.Iteration,-4} | {result.StartPosition,-10:N0} | {result.TotalConcepts,-10:N0} | {result.TotalVocab,-8:N0} | {FormatBytes(result.DiskSizeBytes),-10} | {growthRate,-12}");
            }
        }
        
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double size = bytes;
            int order = 0;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
        
        private class IterationResult
        {
            public int Iteration { get; set; }
            public int StartPosition { get; set; }
            public int SampleSize { get; set; }
            public TimeSpan Duration { get; set; }
            public long ConceptGrowth { get; set; }
            public long VocabGrowth { get; set; }
            public long AssociationGrowth { get; set; }
            public long TotalConcepts { get; set; }
            public long TotalVocab { get; set; }
            public long TotalAssociations { get; set; }
            public long DiskSizeBytes { get; set; }
        }
    }
}
