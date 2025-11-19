using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using greyMatter.Core;
using GreyMatter.Storage;

namespace GreyMatter.Learning
{
    /// <summary>
    /// Multi-source trainer that integrates learning from multiple IDataSource implementations
    /// Tracks source attribution, maintains biological learning with neural connections,
    /// and provides detailed statistics per source
    /// </summary>
    public class MultiSourceTrainer
    {
        private readonly LanguageEphemeralBrain _brain;
        private readonly IStorageAdapter _storage;
        private readonly Dictionary<string, SourceStatistics> _sourceStats;

        public LanguageEphemeralBrain Brain => _brain;
        public IReadOnlyDictionary<string, SourceStatistics> SourceStatistics => _sourceStats;

        public MultiSourceTrainer(IStorageAdapter? storage = null)
        {
            _brain = new LanguageEphemeralBrain();
            _storage = storage ?? new FastStorageAdapter(
                hotPath: "/Users/billdodd/Desktop/Cerebro/working",
                coldPath: "/Users/billdodd/Documents/Cerebro"
            );
            _sourceStats = new Dictionary<string, SourceStatistics>();
        }

        /// <summary>
        /// Train from multiple data sources
        /// </summary>
        /// <param name="dataSources">List of data sources to train from</param>
        /// <param name="maxSentencesPerSource">Optional limit per source</param>
        public async Task TrainFromMultipleSourcesAsync(
            IEnumerable<IDataSource> dataSources,
            int? maxSentencesPerSource = null)
        {
            var sources = dataSources.ToList();
            
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("🧠 MULTI-SOURCE TRAINING SESSION");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine($"\n📚 Training from {sources.Count} data sources:");
            
            foreach (var source in sources)
            {
                Console.WriteLine($"   • {source.SourceName} ({source.SourceType})");
            }
            Console.WriteLine();

            var overallStopwatch = Stopwatch.StartNew();
            int totalSentencesProcessed = 0;

            foreach (var dataSource in sources)
            {
                Console.WriteLine($"\n─────────────────────────────────────────────────────────────");
                Console.WriteLine($"📖 Loading from: {dataSource.SourceName}");
                Console.WriteLine($"─────────────────────────────────────────────────────────────");

                // Validate source first
                var validation = await dataSource.ValidateAsync();
                if (!validation.IsValid)
                {
                    Console.WriteLine($"❌ Validation failed: {validation.Message}");
                    foreach (var error in validation.Errors)
                    {
                        Console.WriteLine($"   • {error}");
                    }
                    continue;
                }

                // Get metadata
                var metadata = await dataSource.GetMetadataAsync();
                Console.WriteLine($"📊 File: {metadata.FileSizeFormatted}");
                Console.WriteLine($"📈 Estimated: {metadata.EstimatedSentenceCount:N0} sentences\n");

                // Initialize source statistics
                if (!_sourceStats.ContainsKey(dataSource.SourceName))
                {
                    _sourceStats[dataSource.SourceName] = new SourceStatistics
                    {
                        SourceName = dataSource.SourceName,
                        SourceType = dataSource.SourceType
                    };
                }
                var stats = _sourceStats[dataSource.SourceName];

                // Train from this source
                var sourceStopwatch = Stopwatch.StartNew();
                int sentencesFromThisSource = 0;
                int vocabBefore = _brain.VocabularySize;

                await foreach (var sentenceData in dataSource.LoadSentencesAsync(maxSentencesPerSource))
                {
                    // Learn the sentence (using existing biological learning)
                    _brain.LearnSentence(sentenceData.Text);
                    
                    // Track source attribution
                    stats.SentencesLearned++;
                    sentencesFromThisSource++;
                    totalSentencesProcessed++;

                    // Store sentence with source metadata
                    stats.SampleSentences.Add(sentenceData.Text);
                    if (stats.SampleSentences.Count > 10)
                    {
                        stats.SampleSentences.RemoveAt(0); // Keep only last 10 samples
                    }

                    // Progress update every 100 sentences
                    if (sentencesFromThisSource % 100 == 0)
                    {
                        var elapsed = sourceStopwatch.Elapsed.TotalSeconds;
                        var rate = sentencesFromThisSource / elapsed;
                        Console.WriteLine($"   Processed: {sentencesFromThisSource:N0} sentences " +
                                        $"({rate:F1}/sec) - " +
                                        $"Vocab: {_brain.VocabularySize:N0} words");
                    }
                }

                sourceStopwatch.Stop();
                
                // Calculate statistics for this source
                stats.VocabularyContributed = _brain.VocabularySize - vocabBefore;
                stats.ProcessingTimeSeconds = sourceStopwatch.Elapsed.TotalSeconds;
                stats.SentencesPerSecond = sentencesFromThisSource / Math.Max(stats.ProcessingTimeSeconds, 0.001);

                // Report source completion
                Console.WriteLine($"\n Completed {dataSource.SourceName}:");
                Console.WriteLine($"   Sentences learned: {stats.SentencesLearned:N0}");
                Console.WriteLine($"   Vocabulary contributed: {stats.VocabularyContributed:N0} words");
                Console.WriteLine($"   Processing time: {sourceStopwatch.Elapsed:mm\\:ss\\.ff}");
                Console.WriteLine($"   Rate: {stats.SentencesPerSecond:F1} sentences/sec");
            }

            overallStopwatch.Stop();

            // Final summary
            Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
            Console.WriteLine("📊 TRAINING SUMMARY");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine($"\n⏱️  Total time: {overallStopwatch.Elapsed:mm\\:ss\\.ff}");
            Console.WriteLine($"📚 Total sentences: {totalSentencesProcessed:N0}");
            Console.WriteLine($"📖 Total vocabulary: {_brain.VocabularySize:N0} words");
            Console.WriteLine($"🧬 Learned sentences tracked: {_brain.LearnedSentences:N0}");
            
            var brainStats = _brain.GetLearningStats();
            Console.WriteLine($"� Total concepts: {brainStats.TotalConcepts:N0}");
            Console.WriteLine($"� Word associations: {brainStats.WordAssociationCount:N0}");
            
            Console.WriteLine($"\n📊 Breakdown by source:");
            foreach (var kvp in _sourceStats.OrderByDescending(s => s.Value.SentencesLearned))
            {
                var stat = kvp.Value;
                var percentage = (stat.SentencesLearned * 100.0) / totalSentencesProcessed;
                Console.WriteLine($"   {stat.SourceName,-12} {stat.SentencesLearned,8:N0} sentences ({percentage:F1}%) " +
                                $"- {stat.VocabularyContributed,6:N0} words - {stat.SentencesPerSecond,6:F1}/sec");
            }
        }

        /// <summary>
        /// Save the trained brain state to storage
        /// </summary>
        public async Task SaveBrainStateAsync()
        {
            Console.WriteLine("\n💾 Saving brain state to storage...");
            var stopwatch = Stopwatch.StartNew();

            // Export vocabulary
            var vocabulary = _brain.ExportVocabulary();
            var vocabSet = new HashSet<string>(vocabulary.Keys);
            await _storage.SaveVocabularyAsync(vocabSet);
            Console.WriteLine($"    Saved vocabulary: {vocabulary.Count:N0} words");

            // Export neurons
            var neurons = _brain.ExportNeurons();
            var brainState = new Dictionary<string, object>
            {
                ["neurons"] = neurons,
                ["metadata"] = new Dictionary<string, object>
                {
                    ["saved_at"] = DateTime.UtcNow,
                    ["vocabulary_size"] = vocabulary.Count,
                    ["neuron_count"] = neurons.Count,
                    ["source_statistics"] = _sourceStats
                }
            };
            await _storage.SaveBrainStateAsync(brainState);
            Console.WriteLine($"    Saved brain state: {neurons.Count:N0} neurons");

            // Export language data
            var languageData = _brain.ExportLanguageData();
            await _storage.SaveNeuralConceptsAsync(languageData);
            Console.WriteLine($"    Saved language data");

            stopwatch.Stop();
            Console.WriteLine($"\n⏱️  Save completed in {stopwatch.Elapsed.TotalSeconds:F2}s");
        }

        /// <summary>
        /// Get statistics for a specific source
        /// </summary>
        public SourceStatistics? GetSourceStatistics(string sourceName)
        {
            return _sourceStats.TryGetValue(sourceName, out var stats) ? stats : null;
        }

        /// <summary>
        /// Get overall training statistics
        /// </summary>
        public MultiSourceTrainingStats GetOverallStatistics()
        {
            var totalSentences = _sourceStats.Values.Sum(s => s.SentencesLearned);
            var totalTime = _sourceStats.Values.Sum(s => s.ProcessingTimeSeconds);

            return new MultiSourceTrainingStats
            {
                TotalSources = _sourceStats.Count,
                TotalSentences = totalSentences,
                TotalVocabulary = _brain.VocabularySize,
                TotalProcessingTime = totalTime,
                SourceBreakdown = _sourceStats.Values.ToList()
            };
        }
    }

    /// <summary>
    /// Statistics tracked for each data source
    /// </summary>
    public class SourceStatistics
    {
        public string SourceName { get; set; } = string.Empty;
        public DataSourceType SourceType { get; set; }
        public int SentencesLearned { get; set; }
        public int VocabularyContributed { get; set; }
        public double ProcessingTimeSeconds { get; set; }
        public double SentencesPerSecond { get; set; }
        public List<string> SampleSentences { get; set; } = new List<string>();
    }

    /// <summary>
    /// Overall training statistics across all sources
    /// </summary>
    public class MultiSourceTrainingStats
    {
        public int TotalSources { get; set; }
        public int TotalSentences { get; set; }
        public int TotalVocabulary { get; set; }
        public double TotalProcessingTime { get; set; }
        public List<SourceStatistics> SourceBreakdown { get; set; } = new List<SourceStatistics>();
    }
}
