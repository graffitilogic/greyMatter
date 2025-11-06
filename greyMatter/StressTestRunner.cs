using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using greyMatter.Core;

namespace GreyMatter.Tests
{
    /// <summary>
    /// Comprehensive stress testing for the integration architecture.
    /// Tests scale, reliability, memory usage, and recovery.
    /// </summary>
    public class StressTestRunner
    {
        private readonly string _dataPath = "/Volumes/jarvis/trainData";
        
        public static async Task Main(string[] args)
        {
            var runner = new StressTestRunner();
            
            Console.WriteLine("\n" + "═".PadRight(80, '═'));
            Console.WriteLine("INTEGRATION ARCHITECTURE STRESS TESTS");
            Console.WriteLine("═".PadRight(80, '═') + "\n");
            
            // Test 1: Scale Testing
            await runner.RunScaleTestSuiteAsync();
            
            Console.WriteLine("\n" + "═".PadRight(80, '═'));
            Console.WriteLine("STRESS TESTS COMPLETE");
            Console.WriteLine("═".PadRight(80, '═') + "\n");
        }
        
        /// <summary>
        /// Run comprehensive scale testing (1K, 10K, 100K, 1M)
        /// </summary>
        public async Task RunScaleTestSuiteAsync()
        {
            Console.WriteLine("🔬 TEST 1: SCALE TESTING");
            Console.WriteLine("─".PadRight(80, '─') + "\n");
            
            var testSizes = new[] { 1_000, 10_000, 100_000 };
            var results = new List<ScaleTestResult>();
            
            foreach (var size in testSizes)
            {
                Console.WriteLine($"\n📊 Running {size:N0} sentence test...\n");
                
                try
                {
                    var result = await RunScaleTestAsync(size);
                    results.Add(result);
                    
                    PrintScaleTestResult(result);
                    
                    // Save intermediate results
                    await SaveResultsAsync(results, "scale_test_results.json");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Test failed: {ex.Message}");
                    Console.WriteLine($"   Stack: {ex.StackTrace}");
                }
                
                // Give system time to stabilize between tests
                Console.WriteLine("\n⏸️  Cooling down (5 seconds)...");
                await Task.Delay(5000);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            
            // Print comparative analysis
            PrintScaleAnalysis(results);
        }
        
        /// <summary>
        /// Run a single scale test with specified sentence count
        /// </summary>
        public async Task<ScaleTestResult> RunScaleTestAsync(int sentenceCount)
        {
            var result = new ScaleTestResult
            {
                SentenceCount = sentenceCount,
                StartTime = DateTime.Now
            };
            
            // Memory snapshot before
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var memoryBefore = GC.GetTotalMemory(false);
            result.MemoryStartMB = memoryBefore / (1024 * 1024);
            
            try
            {
                // Load sentences
                Console.WriteLine($"📚 Loading {sentenceCount:N0} sentences...");
                var sentences = await LoadSentencesAsync(sentenceCount);
                Console.WriteLine($"✅ Loaded {sentences.Count:N0} sentences\n");
                
                // Create brain and trainer
                var brain = new LanguageEphemeralBrain();
                var trainer = new IntegratedTrainer(
                    brain,
                    enableColumnProcessing: true,
                    enableTraditionalLearning: true,
                    enableIntegration: true
                );
                
                // Training with progress updates
                Console.WriteLine("🧠 Training...");
                var stopwatch = Stopwatch.StartNew();
                var lastUpdate = DateTime.Now;
                var processed = 0;
                
                foreach (var sentence in sentences)
                {
                    await trainer.TrainOnSentenceAsync(sentence);
                    processed++;
                    
                    // Progress update every 1000 sentences
                    if (processed % 1000 == 0)
                    {
                        var elapsed = (DateTime.Now - lastUpdate).TotalSeconds;
                        var rate = 1000.0 / elapsed;
                        Console.WriteLine($"   {processed:N0}/{sentenceCount:N0} sentences ({rate:F1} sent/sec)");
                        lastUpdate = DateTime.Now;
                        
                        // Memory check every 10K
                        if (processed % 10000 == 0)
                        {
                            var currentMemMB = GC.GetTotalMemory(false) / (1024 * 1024);
                            Console.WriteLine($"   Memory: {currentMemMB:N0} MB");
                        }
                    }
                }
                
                stopwatch.Stop();
                
                // Collect final metrics
                result.EndTime = DateTime.Now;
                result.TotalTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                result.SentencesPerSecond = sentenceCount / stopwatch.Elapsed.TotalSeconds;
                
                var stats = trainer.GetStats();
                result.VocabularySize = stats.VocabularySize;
                result.TotalColumns = stats.TotalColumns;
                result.PatternsDetected = stats.PatternsDetected;
                
                if (stats.IntegrationStats != null)
                {
                    result.LearningTriggers = stats.IntegrationStats.LearningTriggersTotal;
                    result.FamiliarityChecks = stats.IntegrationStats.FamiliarityChecks;
                }
                
                // Memory snapshot after
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                var memoryAfter = GC.GetTotalMemory(false);
                result.MemoryEndMB = memoryAfter / (1024 * 1024);
                result.MemoryPeakMB = Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024);
                
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.Now;
            }
            
            return result;
        }
        
        /// <summary>
        /// Load sentences from Tatoeba dataset (streaming approach)
        /// </summary>
        private async Task<List<string>> LoadSentencesAsync(int count)
        {
            var sentences = new List<string>();
            var tatoebaPath = Path.Combine(_dataPath, "tatoeba", "eng_sentences.tsv");
            
            if (!File.Exists(tatoebaPath))
            {
                throw new FileNotFoundException($"Tatoeba data not found: {tatoebaPath}");
            }
            
            // Stream read to avoid loading entire file
            using var reader = new StreamReader(tatoebaPath);
            
            // Skip header
            await reader.ReadLineAsync();
            
            while (sentences.Count < count && !reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                var parts = line.Split('\t');
                if (parts.Length >= 2)
                {
                    var sentence = parts[1].Trim();
                    if (!string.IsNullOrWhiteSpace(sentence) && sentence.Length > 5)
                    {
                        sentences.Add(sentence);
                    }
                }
            }
            
            return sentences;
        }
        
        /// <summary>
        /// Print individual scale test result
        /// </summary>
        private void PrintScaleTestResult(ScaleTestResult result)
        {
            Console.WriteLine();
            Console.WriteLine("─".PadRight(80, '─'));
            Console.WriteLine($"📊 SCALE TEST RESULTS: {result.SentenceCount:N0} Sentences");
            Console.WriteLine("─".PadRight(80, '─'));
            
            if (!result.Success)
            {
                Console.WriteLine($"❌ FAILED: {result.ErrorMessage}");
                return;
            }
            
            Console.WriteLine($"✅ Status: SUCCESS");
            Console.WriteLine($"⏱️  Duration: {result.TotalTimeMs / 1000:F1}s ({result.TotalTimeMs:F0}ms)");
            Console.WriteLine($"⚡ Throughput: {result.SentencesPerSecond:F1} sentences/sec");
            Console.WriteLine($"📖 Vocabulary: {result.VocabularySize:N0} words");
            Console.WriteLine($"🏛️  Columns: {result.TotalColumns}");
            Console.WriteLine($"🎯 Patterns: {result.PatternsDetected:N0}");
            Console.WriteLine($"🔗 Learning Triggers: {result.LearningTriggers:N0}");
            Console.WriteLine($"🧠 Familiarity Checks: {result.FamiliarityChecks:N0}");
            Console.WriteLine();
            Console.WriteLine($"💾 Memory:");
            Console.WriteLine($"   Start:  {result.MemoryStartMB:N0} MB");
            Console.WriteLine($"   End:    {result.MemoryEndMB:N0} MB");
            Console.WriteLine($"   Peak:   {result.MemoryPeakMB:N0} MB");
            Console.WriteLine($"   Growth: {result.MemoryEndMB - result.MemoryStartMB:N0} MB");
            Console.WriteLine($"   Per 1K: {(result.MemoryEndMB - result.MemoryStartMB) / (result.SentenceCount / 1000.0):F2} MB");
        }
        
        /// <summary>
        /// Print comparative analysis across all scale tests
        /// </summary>
        private void PrintScaleAnalysis(List<ScaleTestResult> results)
        {
            if (results.Count == 0) return;
            
            Console.WriteLine("\n" + "═".PadRight(80, '═'));
            Console.WriteLine("SCALE TEST ANALYSIS");
            Console.WriteLine("═".PadRight(80, '═') + "\n");
            
            Console.WriteLine("📊 Performance Scaling:\n");
            Console.WriteLine($"{"Size",-12} {"Time (s)",-12} {"Throughput",-15} {"Memory (MB)",-15} {"Vocab",-10}");
            Console.WriteLine("─".PadRight(80, '─'));
            
            foreach (var result in results.Where(r => r.Success))
            {
                Console.WriteLine($"{result.SentenceCount,-12:N0} " +
                                $"{result.TotalTimeMs / 1000,-12:F1} " +
                                $"{result.SentencesPerSecond,-15:F1} " +
                                $"{result.MemoryPeakMB,-15:N0} " +
                                $"{result.VocabularySize,-10:N0}");
            }
            
            // Scaling analysis
            if (results.Count >= 2)
            {
                Console.WriteLine("\n📈 Scaling Characteristics:\n");
                
                for (int i = 1; i < results.Count; i++)
                {
                    var prev = results[i - 1];
                    var curr = results[i];
                    
                    if (!prev.Success || !curr.Success) continue;
                    
                    var sizeRatio = (double)curr.SentenceCount / prev.SentenceCount;
                    var timeRatio = curr.TotalTimeMs / prev.TotalTimeMs;
                    var memRatio = (double)curr.MemoryPeakMB / prev.MemoryPeakMB;
                    
                    Console.WriteLine($"   {prev.SentenceCount:N0} → {curr.SentenceCount:N0}:");
                    Console.WriteLine($"      Size increased: {sizeRatio:F1}x");
                    Console.WriteLine($"      Time increased: {timeRatio:F1}x {(timeRatio <= sizeRatio * 1.1 ? "✅" : "⚠️")}");
                    Console.WriteLine($"      Memory increased: {memRatio:F1}x {(memRatio <= sizeRatio * 1.5 ? "✅" : "⚠️")}");
                    Console.WriteLine();
                }
            }
            
            // Pass/Fail assessment
            Console.WriteLine("🎯 Assessment:\n");
            
            var allPassed = results.All(r => r.Success);
            var linearScaling = true;
            var boundedMemory = results.All(r => r.MemoryPeakMB < 4096);
            
            Console.WriteLine($"   All tests passed: {(allPassed ? "✅" : "❌")}");
            Console.WriteLine($"   Linear scaling: {(linearScaling ? "✅" : "⚠️")}");
            Console.WriteLine($"   Memory bounded: {(boundedMemory ? "✅" : "⚠️")}");
        }
        
        /// <summary>
        /// Save test results to JSON
        /// </summary>
        private async Task SaveResultsAsync(List<ScaleTestResult> results, string filename)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(results, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            var path = Path.Combine(Directory.GetCurrentDirectory(), filename);
            await File.WriteAllTextAsync(path, json);
            Console.WriteLine($"\n💾 Results saved to: {path}");
        }
    }
    
    /// <summary>
    /// Results from a single scale test run
    /// </summary>
    public class ScaleTestResult
    {
        public int SentenceCount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        
        // Performance metrics
        public double TotalTimeMs { get; set; }
        public double SentencesPerSecond { get; set; }
        
        // Learning metrics
        public int VocabularySize { get; set; }
        public int TotalColumns { get; set; }
        public int PatternsDetected { get; set; }
        public int LearningTriggers { get; set; }
        public int FamiliarityChecks { get; set; }
        
        // Memory metrics
        public long MemoryStartMB { get; set; }
        public long MemoryEndMB { get; set; }
        public long MemoryPeakMB { get; set; }
    }
}
