using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using greyMatter.Core;
using greyMatter.Learning;
using GreyMatter.Storage;
using GreyMatter.Core;

namespace greyMatter
{
    /// <summary>
    /// Diagnostic tool to analyze neuron growth patterns and identify efficiency issues
    /// </summary>
    public class NeuronGrowthDiagnostic
    {
        private readonly TatoebaLanguageTrainer _trainer;
        private readonly LanguageEphemeralBrain _brain;
        private readonly SemanticStorageManager _storageManager;

        public NeuronGrowthDiagnostic()
        {
            // Use centralized configuration with fallbacks
            var config = new CerebroConfiguration();
            config.ValidateAndSetup();
            
            var tatoebaDataPath = Path.Combine(config.TrainingDataRoot, "tatoeba");
            _trainer = new TatoebaLanguageTrainer(tatoebaDataPath);
            _brain = _trainer.Brain;
            _storageManager = new SemanticStorageManager(config.BrainDataPath, config.TrainingDataRoot);
        }

        /// <summary>
        /// Run comprehensive neuron growth analysis
        /// </summary>
        public async Task RunDiagnostic()
        {
            Console.WriteLine("🔬 **NEURON GROWTH DIAGNOSTIC**");
            Console.WriteLine("==================================================");
            Console.WriteLine("Analyzing neuron creation patterns and storage efficiency\n");

            await AnalyzeStorageBreakdown();
            await AnalyzeNeuronDistribution();
            await AnalyzeClusterEfficiency();
            await AnalyzeReusePatterns();
            await AnalyzeMemoryFootprint();
            
            Console.WriteLine("\n💡 **DIAGNOSTIC SUMMARY & RECOMMENDATIONS**");
            await ProvideDiagnosticSummary();
        }

        /// <summary>
        /// Analyze what's taking up storage space
        /// </summary>
        private async Task AnalyzeStorageBreakdown()
        {
            Console.WriteLine("📊 **STORAGE BREAKDOWN ANALYSIS**");
            
            try
            {
                var storageStats = await _storageManager.GetStorageStatisticsAsync();
                var brainStats = _brain.GetLearningStats();
                
                Console.WriteLine($"   Total Storage: {storageStats.TotalStorageSize / 1024.0 / 1024.0:F1} MB");
                Console.WriteLine($"   Total Neurons: {storageStats.TotalNeuronsInPool:N0}");
                Console.WriteLine($"   Vocabulary Entries: {brainStats.VocabularySize:N0}");
                Console.WriteLine($"   Neural Concepts: {brainStats.TotalConcepts:N0}");
                Console.WriteLine($"   Sentences Processed: {brainStats.LearnedSentences:N0}");
                
                // Calculate efficiency metrics
                var bytesPerNeuron = storageStats.TotalNeuronsInPool > 0 ? 
                    storageStats.TotalStorageSize / (double)storageStats.TotalNeuronsInPool : 0;
                var neuronsPerSentence = brainStats.LearnedSentences > 0 ? 
                    storageStats.TotalNeuronsInPool / (double)brainStats.LearnedSentences : 0;
                var bytesPerSentence = brainStats.LearnedSentences > 0 ? 
                    storageStats.TotalStorageSize / (double)brainStats.LearnedSentences : 0;
                
                Console.WriteLine($"\n📈 **EFFICIENCY METRICS**");
                Console.WriteLine($"   Bytes per neuron: {bytesPerNeuron:F0} bytes");
                Console.WriteLine($"   Neurons per sentence: {neuronsPerSentence:F0}");
                Console.WriteLine($"   Storage per sentence: {bytesPerSentence / 1024.0:F1} KB");
                
                // Analyze storage distribution
                await AnalyzeStorageFiles();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Storage analysis failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Analyze storage file distribution
        /// </summary>
        private async Task AnalyzeStorageFiles()
        {
            await Task.Delay(1); // Make async
            
            try
            {
                var config = new CerebroConfiguration();
                config.ValidateAndSetup();
                var brainDataPath = config.BrainDataPath;
                
                if (!Directory.Exists(brainDataPath))
                {
                    Console.WriteLine("   ⚠️  Brain data directory not found");
                    return;
                }

                Console.WriteLine($"\n📁 **STORAGE FILE ANALYSIS**");
                
                // Analyze different storage components
                var directories = new[]
                {
                    ("Vocabulary", Path.Combine(brainDataPath, "clusters")),
                    ("Neural Concepts", Path.Combine(brainDataPath, "hierarchical")),
                    ("Neuron Pools", brainDataPath)
                };

                long totalSize = 0;
                foreach (var (name, path) in directories)
                {
                    if (Directory.Exists(path))
                    {
                        var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                        var dirSize = files.Sum(f => new FileInfo(f).Length);
                        totalSize += dirSize;
                        
                        Console.WriteLine($"   {name}: {dirSize / 1024.0 / 1024.0:F1} MB ({files.Length:N0} files)");
                        
                        // Show largest files in this category
                        var largestFiles = files
                            .Select(f => new FileInfo(f))
                            .OrderByDescending(f => f.Length)
                            .Take(3);
                            
                        foreach (var file in largestFiles)
                        {
                            Console.WriteLine($"      - {Path.GetFileName(file.Name)}: {file.Length / 1024.0 / 1024.0:F1} MB");
                        }
                    }
                }
                
                Console.WriteLine($"   Total Analyzed: {totalSize / 1024.0 / 1024.0:F1} MB");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ File analysis failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Analyze neuron distribution patterns
        /// </summary>
        private async Task AnalyzeNeuronDistribution()
        {
            await Task.Delay(1); // Make async
            
            Console.WriteLine("\n🧠 **NEURON DISTRIBUTION ANALYSIS**");
            
            try
            {
                var stats = _brain.GetLearningStats();
                var topWords = _brain.GetTopWords(20);
                
                Console.WriteLine($"   Total Vocabulary: {stats.VocabularySize:N0} unique words");
                Console.WriteLine($"   Total Concepts: {stats.TotalConcepts:N0}");
                Console.WriteLine($"   Concepts per word: {(double)stats.TotalConcepts / Math.Max(stats.VocabularySize, 1):F1}");
                
                Console.WriteLine($"\n📈 **WORD FREQUENCY DISTRIBUTION**");
                Console.WriteLine($"   Most frequent words:");
                for (int i = 0; i < Math.Min(10, topWords.Count); i++)
                {
                    var (word, frequency) = topWords[i];
                    Console.WriteLine($"      {i + 1,2}. '{word}' → {frequency:N0} occurrences");
                }
                
                // Analyze frequency distribution
                var frequencies = topWords.Select(w => w.frequency).ToList();
                if (frequencies.Any())
                {
                    Console.WriteLine($"\n📊 **FREQUENCY STATISTICS**");
                    Console.WriteLine($"   Highest frequency: {frequencies.Max():N0}");
                    Console.WriteLine($"   Average frequency: {frequencies.Average():F1}");
                    Console.WriteLine($"   Median frequency: {frequencies[frequencies.Count / 2]:N0}");
                    
                    // Check for potential duplicates or excessive repetition
                    var veryHighFreq = frequencies.Where(f => f > 1000).Count();
                    var highFreq = frequencies.Where(f => f > 100 && f <= 1000).Count();
                    var normalFreq = frequencies.Where(f => f <= 100).Count();
                    
                    Console.WriteLine($"   Very high freq (>1000): {veryHighFreq:N0} words");
                    Console.WriteLine($"   High freq (100-1000): {highFreq:N0} words");
                    Console.WriteLine($"   Normal freq (≤100): {normalFreq:N0} words");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Distribution analysis failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Analyze clustering efficiency
        /// </summary>
        private async Task AnalyzeClusterEfficiency()
        {
            await Task.Delay(1); // Make async
            
            Console.WriteLine("\n🔗 **CLUSTER EFFICIENCY ANALYSIS**");
            
            try
            {
                // Analyze cluster file distribution
                var config = new CerebroConfiguration();
                config.ValidateAndSetup();
                var clusterPath = Path.Combine(config.BrainDataPath, "clusters");
                if (Directory.Exists(clusterPath))
                {
                    var clusterFiles = Directory.GetFiles(clusterPath, "cluster_*.json");
                    
                    Console.WriteLine($"   Total cluster files: {clusterFiles.Length:N0}");
                    
                    if (clusterFiles.Length > 0)
                    {
                        var fileSizes = clusterFiles.Select(f => new FileInfo(f).Length).ToList();
                        
                        Console.WriteLine($"   Average cluster size: {fileSizes.Average() / 1024.0:F1} KB");
                        Console.WriteLine($"   Largest cluster: {fileSizes.Max() / 1024.0:F1} KB");
                        Console.WriteLine($"   Smallest cluster: {fileSizes.Min() / 1024.0:F1} KB");
                        
                        // Check for potential clustering issues
                        var largeFiles = fileSizes.Where(s => s > 1024 * 1024).Count(); // >1MB
                        var smallFiles = fileSizes.Where(s => s < 1024).Count(); // <1KB
                        
                        Console.WriteLine($"   Large clusters (>1MB): {largeFiles:N0}");
                        Console.WriteLine($"   Small clusters (<1KB): {smallFiles:N0}");
                        
                        if (smallFiles > clusterFiles.Length * 0.5)
                        {
                            Console.WriteLine($"   ⚠️  Warning: {smallFiles:N0} very small clusters suggest poor clustering");
                        }
                        
                        if (largeFiles > 0)
                        {
                            Console.WriteLine($"   ⚠️  Warning: {largeFiles:N0} very large clusters may need subdivision");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("   ⚠️  No cluster directory found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Cluster analysis failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Analyze neuron reuse patterns
        /// </summary>
        private async Task AnalyzeReusePatterns()
        {
            await Task.Delay(1); // Make async
            
            Console.WriteLine("\n♻️ **NEURON REUSE ANALYSIS**");
            
            try
            {
                var stats = _brain.GetLearningStats();
                
                // Estimate reuse efficiency
                var sentencesLearned = stats.LearnedSentences;
                var totalConcepts = stats.TotalConcepts;
                var vocabularySize = stats.VocabularySize;
                
                Console.WriteLine($"   Sentences learned: {sentencesLearned:N0}");
                Console.WriteLine($"   Unique concepts: {totalConcepts:N0}");
                Console.WriteLine($"   Unique vocabulary: {vocabularySize:N0}");
                
                if (sentencesLearned > 0)
                {
                    var conceptsPerSentence = (double)totalConcepts / sentencesLearned;
                    var wordsPerSentence = sentencesLearned > 0 ? 
                        stats.LearnedSentences / (double)vocabularySize : 0;
                    
                    Console.WriteLine($"   Concepts per sentence: {conceptsPerSentence:F1}");
                    Console.WriteLine($"   Est. word reuse rate: {wordsPerSentence:F1}x per word");
                    
                    // Theoretical vs actual analysis
                    var avgSentenceLength = 8; // Typical sentence length
                    var expectedConcepts = sentencesLearned * avgSentenceLength;
                    var actualConcepts = totalConcepts;
                    var reuseRatio = expectedConcepts / (double)Math.Max(actualConcepts, 1);
                    
                    Console.WriteLine($"\n📊 **REUSE EFFICIENCY**");
                    Console.WriteLine($"   Expected concepts (no reuse): {expectedConcepts:N0}");
                    Console.WriteLine($"   Actual concepts: {actualConcepts:N0}");
                    Console.WriteLine($"   Reuse efficiency: {reuseRatio:F1}x");
                    
                    if (reuseRatio < 2.0)
                    {
                        Console.WriteLine($"   ⚠️  Low reuse efficiency - creating too many unique concepts");
                    }
                    else if (reuseRatio > 10.0)
                    {
                        Console.WriteLine($"   ✅ Good reuse efficiency");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Reuse analysis failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Analyze memory footprint patterns
        /// </summary>
        private async Task AnalyzeMemoryFootprint()
        {
            Console.WriteLine("\n💾 **MEMORY FOOTPRINT ANALYSIS**");
            
            try
            {
                var storageStats = await _storageManager.GetStorageStatisticsAsync();
                
                // Analyze neuron pool files
                var config = new CerebroConfiguration();
                config.ValidateAndSetup();
                var brainDataPath = config.BrainDataPath;
                var poolFiles = Directory.GetFiles(brainDataPath, "neuron_pool_*.json");
                
                Console.WriteLine($"   Neuron pool files: {poolFiles.Length:N0}");
                
                if (poolFiles.Length > 0)
                {
                    var poolSizes = poolFiles.Select(f => new FileInfo(f).Length).ToList();
                    var totalPoolSize = poolSizes.Sum();
                    
                    Console.WriteLine($"   Total pool storage: {totalPoolSize / 1024.0 / 1024.0:F1} MB");
                    Console.WriteLine($"   Average pool file: {poolSizes.Average() / 1024.0 / 1024.0:F1} MB");
                    Console.WriteLine($"   Largest pool file: {poolSizes.Max() / 1024.0 / 1024.0:F1} MB");
                    
                    // Estimate neurons per file
                    if (storageStats.TotalNeuronsInPool > 0)
                    {
                        var neuronsPerFile = storageStats.TotalNeuronsInPool / (double)poolFiles.Length;
                        var bytesPerNeuronInPool = totalPoolSize / (double)storageStats.TotalNeuronsInPool;
                        
                        Console.WriteLine($"   Neurons per pool file: {neuronsPerFile:F0}");
                        Console.WriteLine($"   Bytes per neuron in pool: {bytesPerNeuronInPool:F0}");
                        
                        if (bytesPerNeuronInPool > 1000)
                        {
                            Console.WriteLine($"   ⚠️  High storage per neuron - check for inefficient serialization");
                        }
                    }
                }
                
                // Check for backup files or duplicates
                var allFiles = Directory.GetFiles(brainDataPath, "*", SearchOption.AllDirectories);
                var backupFiles = allFiles.Where(f => f.Contains("backup") || f.Contains(".bak")).ToList();
                
                if (backupFiles.Any())
                {
                    var backupSize = backupFiles.Sum(f => new FileInfo(f).Length);
                    Console.WriteLine($"   Backup files: {backupFiles.Count:N0} ({backupSize / 1024.0 / 1024.0:F1} MB)");
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Memory analysis failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Provide diagnostic summary and recommendations
        /// </summary>
        private async Task ProvideDiagnosticSummary()
        {
            await Task.Delay(1); // Make async
            
            try
            {
                var storageStats = await _storageManager.GetStorageStatisticsAsync();
                var brainStats = _brain.GetLearningStats();
                
                var neuronsPerSentence = brainStats.LearnedSentences > 0 ? 
                    storageStats.TotalNeuronsInPool / (double)brainStats.LearnedSentences : 0;
                var bytesPerSentence = brainStats.LearnedSentences > 0 ? 
                    storageStats.TotalStorageSize / (double)brainStats.LearnedSentences : 0;
                
                Console.WriteLine($"🔍 **KEY FINDINGS**");
                Console.WriteLine($"   Growth Rate: {neuronsPerSentence:F0} neurons/sentence");
                Console.WriteLine($"   Storage Rate: {bytesPerSentence / 1024.0:F0} KB/sentence");
                
                Console.WriteLine($"\n🎯 **ASSESSMENT**");
                
                if (neuronsPerSentence > 500)
                {
                    Console.WriteLine($"   ❌ EXCESSIVE: {neuronsPerSentence:F0} neurons/sentence is too high");
                    Console.WriteLine($"      Expected: 50-200 neurons/sentence for efficient learning");
                }
                else if (neuronsPerSentence > 200)
                {
                    Console.WriteLine($"   ⚠️  HIGH: {neuronsPerSentence:F0} neurons/sentence could be optimized");
                }
                else
                {
                    Console.WriteLine($"   ✅ GOOD: {neuronsPerSentence:F0} neurons/sentence is reasonable");
                }
                
                if (bytesPerSentence > 300 * 1024) // >300KB per sentence
                {
                    Console.WriteLine($"   ❌ BLOATED: {bytesPerSentence / 1024.0:F0} KB/sentence storage is excessive");
                }
                else if (bytesPerSentence > 100 * 1024) // >100KB per sentence
                {
                    Console.WriteLine($"   ⚠️  HEAVY: {bytesPerSentence / 1024.0:F0} KB/sentence storage could be optimized");
                }
                else
                {
                    Console.WriteLine($"   ✅ EFFICIENT: {bytesPerSentence / 1024.0:F0} KB/sentence storage is reasonable");
                }
                
                Console.WriteLine($"\n🚀 **OPTIMIZATION RECOMMENDATIONS**");
                
                if (neuronsPerSentence > 300)
                {
                    Console.WriteLine($"   1. 🔧 Implement neuron deduplication");
                    Console.WriteLine($"   2. 🔧 Improve clustering to merge similar concepts");
                    Console.WriteLine($"   3. 🔧 Add neuron reuse detection");
                }
                
                if (bytesPerSentence > 200 * 1024)
                {
                    Console.WriteLine($"   4. 🔧 Optimize JSON serialization (use binary format?)");
                    Console.WriteLine($"   5. 🔧 Remove unnecessary metadata from neurons");
                    Console.WriteLine($"   6. 🔧 Implement compression for storage");
                }
                
                Console.WriteLine($"   7. 🔧 Consider batch processing to reduce overhead");
                Console.WriteLine($"   8. 🔧 Implement periodic garbage collection for unused neurons");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Summary generation failed: {ex.Message}");
            }
        }

        // Commented out to avoid multiple entry points
        // /// <summary>
        // /// Entry point for diagnostic
        // /// </summary>
        // public static async Task Main(string[] args)
        // {
        //     try
        //     {
        //         var diagnostic = new NeuronGrowthDiagnostic();
        //         await diagnostic.RunDiagnostic();
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"❌ Diagnostic failed: {ex.Message}");
        //         Console.WriteLine($"Stack trace: {ex.StackTrace}");
        //     }
        //     
        //     Console.WriteLine("\nPress any key to exit...");
        //     Console.ReadKey();
        // }
    }
}
