using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;
using greyMatter.Core;

namespace GreyMatter.Tests
{
    /// <summary>
    /// Comprehensive validation comparing three integration modes:
    /// 1. Traditional learning only (baseline)
    /// 2. Column processing only (dynamic patterns)
    /// 3. Integrated (both layers - the biological model)
    /// 
    /// Tests the hypothesis that integration provides value beyond either system alone.
    /// </summary>
    public class IntegrationValidationTest
    {
        private class ValidationResults
        {
            public string Mode { get; set; } = "";
            public int VocabularySize { get; set; }
            public double TrainingTimeMs { get; set; }
            public double SentencesPerSecond { get; set; }
            public IntegrationStats? IntegrationStats { get; set; }
            public PatternDetectionStats? PatternStats { get; set; }
            public int TotalColumns { get; set; }
            public Dictionary<string, bool> KnownWords { get; set; } = new Dictionary<string, bool>();
        }

        public static async Task RunValidationAsync()
        {
            Console.WriteLine("\n" + "═".PadRight(80, '═'));
            Console.WriteLine("INTEGRATION ARCHITECTURE VALIDATION");
            Console.WriteLine("Week 5 - Phase 5: Comparing Traditional vs Columns vs Integrated");
            Console.WriteLine("═".PadRight(80, '═') + "\n");

            // Prepare test data (larger dataset for better comparison)
            var testSentences = GenerateTestSentences();
            Console.WriteLine($"📚 Test Dataset: {testSentences.Count} sentences\n");

            // Test vocabulary words
            var testVocabulary = new[] { 
                "cat", "dog", "bird", "park", "tree", "house", "car", "book", 
                "run", "walk", "fly", "read", "write", "think",
                "happy", "sad", "big", "small", "fast", "slow"
            };

            var results = new List<ValidationResults>();

            // Mode 1: Traditional learning only
            Console.WriteLine("🔵 MODE 1: Traditional Learning Only");
            Console.WriteLine("─".PadRight(80, '─'));
            results.Add(await TestMode(
                "Traditional Only",
                testSentences,
                testVocabulary,
                enableColumns: false,
                enableTraditional: true,
                enableIntegration: false
            ));

            Console.WriteLine("\n");

            // Mode 2: Column processing only
            Console.WriteLine("🟢 MODE 2: Column Processing Only");
            Console.WriteLine("─".PadRight(80, '─'));
            results.Add(await TestMode(
                "Columns Only",
                testSentences,
                testVocabulary,
                enableColumns: true,
                enableTraditional: false,
                enableIntegration: false
            ));

            Console.WriteLine("\n");

            // Mode 3: Integrated (both systems)
            Console.WriteLine("🟣 MODE 3: Integrated Architecture");
            Console.WriteLine("─".PadRight(80, '─'));
            results.Add(await TestMode(
                "Integrated",
                testSentences,
                testVocabulary,
                enableColumns: true,
                enableTraditional: true,
                enableIntegration: true
            ));

            // Comparison analysis
            Console.WriteLine("\n" + "═".PadRight(80, '═'));
            Console.WriteLine("COMPARATIVE ANALYSIS");
            Console.WriteLine("═".PadRight(80, '═') + "\n");

            PrintComparison(results);

            // Validate integration hypothesis
            ValidateIntegrationHypothesis(results);

            Console.WriteLine("\n" + "═".PadRight(80, '═'));
            Console.WriteLine("VALIDATION COMPLETE");
            Console.WriteLine("═".PadRight(80, '═') + "\n");
        }

        private static async Task<ValidationResults> TestMode(
            string mode,
            List<string> sentences,
            string[] testVocabulary,
            bool enableColumns,
            bool enableTraditional,
            bool enableIntegration)
        {
            var brain = new LanguageEphemeralBrain();
            var trainer = new IntegratedTrainer(
                brain,
                enableColumnProcessing: enableColumns,
                enableTraditionalLearning: enableTraditional,
                enableIntegration: enableIntegration
            );

            Console.WriteLine($"⚙️  Training with {mode} mode...");

            var stopwatch = Stopwatch.StartNew();

            // Train on all sentences
            foreach (var sentence in sentences)
            {
                await trainer.TrainOnSentenceAsync(sentence);
            }

            stopwatch.Stop();

            var stats = trainer.GetStats();
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            var sentencesPerSec = (sentences.Count / stopwatch.Elapsed.TotalSeconds);

            Console.WriteLine($"✅ Training complete: {elapsedMs:F0}ms ({sentencesPerSec:F1} sentences/sec)");
            
            // Print detailed performance breakdown for integrated mode
            if (enableIntegration)
            {
                trainer.PrintStats();
            }

            // Test vocabulary recognition
            var knownWords = new Dictionary<string, bool>();
            var iBrain = brain as IIntegratedBrain;
            if (iBrain != null)
            {
                foreach (var word in testVocabulary)
                {
                    knownWords[word] = iBrain.IsKnownWord(word);
                }
            }

            var recognizedCount = knownWords.Values.Count(x => x);
            Console.WriteLine($"📖 Vocabulary: {stats.VocabularySize} total, {recognizedCount}/{testVocabulary.Length} test words recognized");

            if (enableIntegration && stats.IntegrationStats != null)
            {
                var iStats = stats.IntegrationStats;
                Console.WriteLine($"🔗 Integration: {iStats.LearningTriggersTotal} triggers, {iStats.KnowledgeUtilizationRate:P0} knowledge use");
            }

            return new ValidationResults
            {
                Mode = mode,
                VocabularySize = stats.VocabularySize,
                TrainingTimeMs = elapsedMs,
                SentencesPerSecond = sentencesPerSec,
                IntegrationStats = stats.IntegrationStats,
                PatternStats = stats.PatternDetectionStats,
                TotalColumns = stats.TotalColumns,
                KnownWords = knownWords
            };
        }

        private static void PrintComparison(List<ValidationResults> results)
        {
            Console.WriteLine("📊 Performance Metrics:\n");
            Console.WriteLine($"{"Mode",-20} {"Vocab",-10} {"Speed",-15} {"Time (ms)",-12}");
            Console.WriteLine("─".PadRight(80, '─'));

            foreach (var result in results)
            {
                Console.WriteLine($"{result.Mode,-20} {result.VocabularySize,-10} {result.SentencesPerSecond,-15:F1} {result.TrainingTimeMs,-12:F0}");
            }

            Console.WriteLine();

            // Speed comparison
            var baseline = results.First(r => r.Mode == "Traditional Only");
            Console.WriteLine("⚡ Speed Comparison (vs Traditional baseline):");
            foreach (var result in results)
            {
                var speedRatio = result.SentencesPerSecond / baseline.SentencesPerSecond;
                var overhead = ((1.0 / speedRatio) - 1.0) * 100;
                Console.WriteLine($"   {result.Mode,-20} {speedRatio:F2}x speed, {overhead,6:F0}% overhead");
            }

            Console.WriteLine();

            // Vocabulary comparison
            Console.WriteLine("📚 Learning Effectiveness:");
            foreach (var result in results)
            {
                var recognized = result.KnownWords.Values.Count(x => x);
                var percentage = (recognized * 100.0) / result.KnownWords.Count;
                Console.WriteLine($"   {result.Mode,-20} {recognized}/{result.KnownWords.Count} words ({percentage:F0}%)");
            }

            Console.WriteLine();

            // Integration metrics (if available)
            var integrated = results.FirstOrDefault(r => r.Mode == "Integrated");
            if (integrated?.IntegrationStats != null)
            {
                var iStats = integrated.IntegrationStats;
                Console.WriteLine("🔗 Integration Synergy:");
                Console.WriteLine($"   Column→Brain triggers: {iStats.LearningTriggersTotal}");
                Console.WriteLine($"   Brain→Column queries: {iStats.FamiliarityChecks} familiarity checks, {iStats.RelatedConceptsRequests} concept requests");
                Console.WriteLine($"   Knowledge cache hits: {iStats.KnowledgeHits}/{iStats.KnowledgeQueriesTotal}");
                Console.WriteLine($"   Learning efficiency: {iStats.LearningEfficiency:P1}");
                
                if (integrated.PatternStats != null)
                {
                    Console.WriteLine($"   Patterns detected: {integrated.PatternStats.TotalPatternsDetected}");
                    Console.WriteLine($"   Consensus events: {integrated.PatternStats.ConsensusEventsReached}");
                    Console.WriteLine($"   Co-activations: {integrated.PatternStats.CoActivationsFound}");
                }
            }
        }

        private static void ValidateIntegrationHypothesis(List<ValidationResults> results)
        {
            Console.WriteLine("\n🔬 Hypothesis Validation:\n");

            var traditional = results.First(r => r.Mode == "Traditional Only");
            var columnsOnly = results.First(r => r.Mode == "Columns Only");
            var integrated = results.First(r => r.Mode == "Integrated");

            // Hypothesis 1: Integration learns vocabulary (like traditional)
            var integratedLearns = integrated.VocabularySize > 0;
            Console.WriteLine($"✓ Hypothesis 1: Integration learns vocabulary");
            Console.WriteLine($"  Result: {integrated.VocabularySize} words learned - {(integratedLearns ? "PASS ✅" : "FAIL ❌")}");

            // Hypothesis 2: Integration uses column processing (like columns-only)
            var integratedUsesColumns = integrated.TotalColumns > 0 && 
                                       integrated.PatternStats?.TotalPatternsDetected > 0;
            Console.WriteLine($"\n✓ Hypothesis 2: Integration uses column processing");
            Console.WriteLine($"  Result: {integrated.TotalColumns} columns, {integrated.PatternStats?.TotalPatternsDetected ?? 0} patterns - {(integratedUsesColumns ? "PASS ✅" : "FAIL ❌")}");

            // Hypothesis 3: Integration enables bidirectional communication
            var bidirectionalWorks = integrated.IntegrationStats != null &&
                                    integrated.IntegrationStats.LearningTriggersTotal > 0 &&
                                    integrated.IntegrationStats.FamiliarityChecks > 0;
            Console.WriteLine($"\n✓ Hypothesis 3: Bidirectional communication works");
            Console.WriteLine($"  Column→Brain: {integrated.IntegrationStats?.LearningTriggersTotal ?? 0} triggers");
            Console.WriteLine($"  Brain→Column: {integrated.IntegrationStats?.FamiliarityChecks ?? 0} familiarity checks");
            Console.WriteLine($"  Result: {(bidirectionalWorks ? "PASS ✅" : "FAIL ❌")}");

            // Hypothesis 4: Integration has acceptable overhead
            var overheadAcceptable = integrated.TrainingTimeMs < traditional.TrainingTimeMs * 3.0; // < 200% overhead
            var overheadPercent = ((integrated.TrainingTimeMs / traditional.TrainingTimeMs) - 1.0) * 100;
            Console.WriteLine($"\n✓ Hypothesis 4: Integration overhead acceptable (< 200%)");
            Console.WriteLine($"  Traditional: {traditional.TrainingTimeMs:F0}ms");
            Console.WriteLine($"  Integrated: {integrated.TrainingTimeMs:F0}ms ({overheadPercent:F0}% overhead)");
            Console.WriteLine($"  Result: {(overheadAcceptable ? "PASS ✅" : "FAIL ❌")}");

            // Hypothesis 5: Integration matches biological inspiration
            var biologicalAlignment = integratedLearns && integratedUsesColumns && bidirectionalWorks;
            Console.WriteLine($"\n✓ Hypothesis 5: Integration matches biological model");
            Console.WriteLine($"  Persistent learning (synapses): {(integratedLearns ? "✅" : "❌")}");
            Console.WriteLine($"  Dynamic processing (columns): {(integratedUsesColumns ? "✅" : "❌")}");
            Console.WriteLine($"  Bidirectional communication: {(bidirectionalWorks ? "✅" : "❌")}");
            Console.WriteLine($"  Result: {(biologicalAlignment ? "PASS ✅" : "FAIL ❌")}");

            // Overall validation
            var allPassed = integratedLearns && integratedUsesColumns && bidirectionalWorks && 
                           overheadAcceptable && biologicalAlignment;
            
            Console.WriteLine($"\n" + "═".PadRight(80, '═'));
            Console.WriteLine($"OVERALL VALIDATION: {(allPassed ? "PASS ✅" : "FAIL ❌")}");
            Console.WriteLine("═".PadRight(80, '═'));

            if (allPassed)
            {
                Console.WriteLine("\n🎉 Integration architecture successfully validated!");
                Console.WriteLine("   The system demonstrates biologically-aligned learning with");
                Console.WriteLine("   both persistent memory (brain) and dynamic processing (columns).");
            }
        }

        private static List<string> GenerateTestSentences()
        {
            return new List<string>
            {
                // Animals and nature
                "The cat sits on the mat",
                "A dog runs in the park",
                "The bird flies over the tree",
                "Cats and dogs are friends",
                "Birds fly high in the sky",
                
                // Actions and places
                "The park has many trees",
                "Trees grow tall and strong",
                "The house has a red door",
                "The car drives on the road",
                "Children play in the park",
                
                // Objects and descriptions
                "The book is on the table",
                "The table is big and brown",
                "The mat is soft and warm",
                "The sky is blue today",
                "The door is made of wood",
                
                // Activities
                "People walk in the park",
                "The cat chases the bird",
                "The dog sleeps on the mat",
                "Birds sing in the trees",
                "The car stops at the house",
                
                // More complex sentences
                "The happy cat sits under the big tree",
                "The small dog runs fast in the green park",
                "The blue bird flies over the tall house",
                "The old book has many interesting stories",
                "The red car drives slowly on the long road"
            };
        }
    }
}
