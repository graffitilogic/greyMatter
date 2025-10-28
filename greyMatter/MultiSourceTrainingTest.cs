using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreyMatter.Learning;

namespace GreyMatter
{
    /// <summary>
    /// Test multi-source training integration
    /// Validates that brain can learn from multiple data sources simultaneously
    /// and track source attribution
    /// </summary>
    public static class MultiSourceTrainingTest
    {
        public static async Task Run()
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("🧪 MULTI-SOURCE TRAINING TEST");
            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

            // Define data source paths
            const string tatoebaPath = "/Volumes/jarvis/trainData/Tatoeba/sentences_eng_small.csv";
            const string cbtPath = "/Volumes/jarvis/trainData/CBT/CBTest/data/cbt_train.txt";
            const string enhancedPath = "/Volumes/jarvis/trainData/enhanced_learning_data/enhanced_word_database.json";

            // Initialize data sources
            Console.WriteLine("📚 Initializing data sources...\n");
            var dataSources = new List<IDataSource>
            {
                new TatoebaDataSource(tatoebaPath),
                new CBTDataSource(cbtPath),
                new EnhancedDataSource(enhancedPath)
            };

            // Create trainer
            var trainer = new MultiSourceTrainer();

            // Train from all sources (limited to 100 sentences per source for quick test)
            Console.WriteLine("🧠 Starting multi-source training...\n");
            await trainer.TrainFromMultipleSourcesAsync(
                dataSources, 
                maxSentencesPerSource: 100
            );

            // Display detailed statistics
            Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
            Console.WriteLine("📈 DETAILED STATISTICS");
            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

            var overall = trainer.GetOverallStatistics();
            
            Console.WriteLine($"Overall Performance:");
            Console.WriteLine($"  Sources trained: {overall.TotalSources}");
            Console.WriteLine($"  Total sentences: {overall.TotalSentences:N0}");
            Console.WriteLine($"  Total vocabulary: {overall.TotalVocabulary:N0} unique words");
            Console.WriteLine($"  Processing time: {overall.TotalProcessingTime:F2}s");
            Console.WriteLine($"  Average rate: {overall.TotalSentences / Math.Max(overall.TotalProcessingTime, 0.001):F1} sentences/sec");

            Console.WriteLine($"\n\nPer-Source Breakdown:");
            foreach (var sourceStats in overall.SourceBreakdown)
            {
                Console.WriteLine($"\n  {sourceStats.SourceName} ({sourceStats.SourceType}):");
                Console.WriteLine($"    Sentences: {sourceStats.SentencesLearned:N0}");
                Console.WriteLine($"    Vocabulary: {sourceStats.VocabularyContributed:N0} new words");
                Console.WriteLine($"    Time: {sourceStats.ProcessingTimeSeconds:F2}s");
                Console.WriteLine($"    Rate: {sourceStats.SentencesPerSecond:F1} sentences/sec");
                
                if (sourceStats.SampleSentences.Count > 0)
                {
                    Console.WriteLine($"    Sample sentences:");
                    for (int i = 0; i < Math.Min(3, sourceStats.SampleSentences.Count); i++)
                    {
                        var sample = sourceStats.SampleSentences[i];
                        var preview = sample.Length > 60 ? sample.Substring(0, 60) + "..." : sample;
                        Console.WriteLine($"      • {preview}");
                    }
                }
            }

            // Verify biological learning worked
            var brainStats = trainer.Brain.GetLearningStats();
            Console.WriteLine($"\n\n🧬 Biological Learning Verification:");
            Console.WriteLine($"  Vocabulary size: {brainStats.VocabularySize:N0} words");
            Console.WriteLine($"  Total concepts: {brainStats.TotalConcepts:N0}");
            Console.WriteLine($"  Word associations: {brainStats.WordAssociationCount:N0}");
            Console.WriteLine($"  Learned sentences: {brainStats.LearnedSentences:N0}");
            
            if (brainStats.TotalConcepts > 0)
            {
                Console.WriteLine("  ✅ Biological learning: WORKING (concepts created)");
            }
            else
            {
                Console.WriteLine("  ⚠️  Warning: No concepts created");
            }

            // Save brain state
            Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
            await trainer.SaveBrainStateAsync();
            Console.WriteLine("═══════════════════════════════════════════════════════════════");

            Console.WriteLine("\n✅ Multi-source training test complete!");
        }
    }
}
