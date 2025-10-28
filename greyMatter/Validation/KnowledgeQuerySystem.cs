using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using greyMatter.Learning;
using GreyMatter.Storage;
using greyMatter.Core;

namespace greyMatter.Validation
{
    /// <summary>
    /// Week 1 Day 4-5: Knowledge Query System
    /// Provides basic inspection capabilities for accumulated brain knowledge
    /// Works with existing WordInfo structure (Word, Frequency, FirstSeen, EstimatedType)
    /// </summary>
    public class KnowledgeQuerySystem
    {
        private FastStorageAdapter? _storage;
        private TatoebaLanguageTrainer? _trainer;
        private LanguageEphemeralBrain? _brain;
        private Dictionary<string, object>? _rawBrainState;  // Direct access to neural structures
        
        private const string TatoebaDataPath = "/Volumes/jarvis/trainData/Tatoeba/sentences_eng_small.csv";
        // Updated paths to match where FullPipelineTest saves data
        private const string WorkingPath = "/Users/billdodd/Desktop/Cerebro/working";  // Hot path (SSD)
        private const string BrainStoragePath = "/Users/billdodd/Documents/Cerebro";   // Cold path (Documents)

        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Week 1 Knowledge Query System ===");
            Console.WriteLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            var querySystem = new KnowledgeQuerySystem();
            
            try
            {
                // Initialize system
                await querySystem.Initialize();
                
                // Parse command
                if (args.Length > 0)
                {
                    await querySystem.HandleCommand(args);
                }
                else
                {
                    // Interactive mode
                    await querySystem.RunInteractiveMode();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private async Task Initialize()
        {
            Console.WriteLine("=== Initializing Knowledge Query System ===");
            Console.WriteLine("🧠 Querying BIOLOGICAL LEARNING structures (neural clusters, not word lists)");
            Console.WriteLine();
            
            // Initialize storage (hotPath first, coldPath second)
            _storage = new FastStorageAdapter(WorkingPath, BrainStoragePath);
            
            // Initialize trainer with storage
            _trainer = new TatoebaLanguageTrainer(TatoebaDataPath, _storage);
            
            // Load existing brain state if available
            try
            {
                Console.WriteLine("📂 Loading neural brain state...");
                
                // Load brain state - contains neurons, languageData, trainingSession
                var brainState = await _storage.LoadBrainStateAsync();
                
                if (brainState != null && brainState.Count > 0)
                {
                    _brain = _trainer.Brain;
                    _rawBrainState = brainState;  // Store for direct neural queries
                    
                    // Report what we actually found - NEURAL STRUCTURES
                    Console.WriteLine($"✅ Brain state loaded ({brainState.Count} components)");
                    
                    // Check what components we have
                    foreach (var key in brainState.Keys)
                    {
                        Console.WriteLine($"   🔑 Component: {key}");
                    }
                    
                    // Also load vocabulary for word queries
                    try
                    {
                        var vocab = await _storage.LoadVocabularyAsync();
                        Console.WriteLine($"   � Vocabulary: {vocab.Count:N0} words learned");
                    }
                    catch
                    {
                        Console.WriteLine($"   📖 Vocabulary: Not available");
                    }
                    
                    Console.WriteLine();
                    Console.WriteLine("✅ Neural brain state loaded successfully");
                    Console.WriteLine("ℹ️  This system queries BIOLOGICAL LEARNING (clusters, neurons, activation patterns)");
                }
                else
                {
                    Console.WriteLine("⚠️ No existing brain state found");
                    Console.WriteLine("ℹ️ Train first using --baseline-test or --multi-source-test");
                }
                
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Could not load brain state: {ex.Message}");
                Console.WriteLine($"   Details: {ex.StackTrace?.Split('\n')[0]}");
                Console.WriteLine("ℹ️ Starting with empty brain. Train first using --baseline-test or --multi-source-test");
                Console.WriteLine();
            }
        }

        private async Task HandleCommand(string[] args)
        {
            var command = args[0].ToLower();
            
            switch (command)
            {
                case "--query":
                case "-q":
                    if (args.Length > 1)
                    {
                        var word = args[1];
                        await QueryWord(word);
                    }
                    else
                    {
                        Console.WriteLine("Usage: --query <word>");
                    }
                    break;
                    
                case "--stats":
                case "-s":
                    await ShowStatistics();
                    break;
                    
                case "--related":
                case "-r":
                    if (args.Length > 1)
                    {
                        var word = args[1];
                        await ShowRelatedConcepts(word);
                    }
                    else
                    {
                        Console.WriteLine("Usage: --related <word>");
                    }
                    break;
                    
                case "--sample":
                    await ShowVocabularySample();
                    break;
                    
                // Week 3: Multi-source query commands
                case "--source":
                case "--from-source":
                    if (args.Length > 1)
                    {
                        var sourceName = args[1];
                        await ListConceptsBySource(sourceName);
                    }
                    else
                    {
                        Console.WriteLine("Usage: --source <sourceName>");
                    }
                    break;
                    
                case "--source-stats":
                case "--sources":
                    await ShowSourceStatistics();
                    break;
                    
                case "--search":
                case "-f":
                    if (args.Length > 1)
                    {
                        var pattern = args[1];
                        await SearchConcepts(pattern);
                    }
                    else
                    {
                        Console.WriteLine("Usage: --search <pattern>");
                    }
                    break;
                    
                case "--export":
                case "--export-json":
                    if (args.Length > 1)
                    {
                        var outputPath = args[1];
                        await ExportToJson(outputPath);
                    }
                    else
                    {
                        var defaultPath = Path.Combine(BrainStoragePath, $"brain_export_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                        await ExportToJson(defaultPath);
                    }
                    break;
                    
                case "--help":
                case "-h":
                    ShowHelp();
                    break;
                    
                default:
                    Console.WriteLine($"Unknown command: {command}");
                    ShowHelp();
                    break;
            }
        }

        private async Task RunInteractiveMode()
        {
            Console.WriteLine("=== Interactive Query Mode ===");
            Console.WriteLine("Commands:");
            Console.WriteLine("  query <word>    - Query knowledge about a specific word");
            Console.WriteLine("  related <word>  - Show related concepts");
            Console.WriteLine("  stats           - Show brain statistics");
            Console.WriteLine("  sample          - Show vocabulary sample");
            Console.WriteLine("  help            - Show this help");
            Console.WriteLine("  quit            - Exit");
            Console.WriteLine();

            while (true)
            {
                Console.Write("> ");
                var input = Console.ReadLine()?.Trim();
                
                if (string.IsNullOrEmpty(input))
                    continue;
                    
                var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    continue;
                    
                var command = parts[0].ToLower();
                
                if (command == "quit" || command == "exit")
                {
                    Console.WriteLine("Goodbye!");
                    break;
                }
                
                try
                {
                    switch (command)
                    {
                        case "query":
                        case "q":
                            if (parts.Length > 1)
                                await QueryWord(parts[1]);
                            else
                                Console.WriteLine("Usage: query <word>");
                            break;
                            
                        case "related":
                        case "r":
                            if (parts.Length > 1)
                                await ShowRelatedConcepts(parts[1]);
                            else
                                Console.WriteLine("Usage: related <word>");
                            break;
                            
                        case "stats":
                        case "s":
                            await ShowStatistics();
                            break;
                            
                        case "sample":
                            await ShowVocabularySample();
                            break;
                            
                        case "help":
                        case "h":
                            ShowHelp();
                            break;
                            
                        default:
                            Console.WriteLine($"Unknown command: {command}. Type 'help' for available commands.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                }
                
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Query detailed information about a specific word
        /// </summary>
        private async Task QueryWord(string word)
        {
            if (_brain == null)
            {
                Console.WriteLine("❌ Brain not loaded. Train first using --baseline-test");
                return;
            }
            
            Console.WriteLine($"=== Query: '{word}' ===");
            
            var vocab = _brain.ExportVocabulary();
            
            if (vocab.ContainsKey(word.ToLower()))
            {
                var wordInfo = vocab[word.ToLower()];
                
                Console.WriteLine($"✅ Word found in vocabulary");
                Console.WriteLine($"📝 Type: {wordInfo.EstimatedType}");
                Console.WriteLine($"📊 Frequency: {wordInfo.Frequency} occurrences");
                Console.WriteLine($"📅 First seen: {wordInfo.FirstSeen:yyyy-MM-dd HH:mm:ss}");
                
                // Show neuron associations if available
                if (wordInfo.ConceptNeuronId.HasValue)
                {
                    Console.WriteLine($"🧠 Concept neuron: {wordInfo.ConceptNeuronId.Value}");
                }
                if (wordInfo.AttentionNeuronId.HasValue)
                {
                    Console.WriteLine($"👁️  Attention neuron: {wordInfo.AttentionNeuronId.Value}");
                }
                if (wordInfo.AssociatedNeuronIds.Any())
                {
                    Console.WriteLine($"🔗 Associated neurons: {string.Join(", ", wordInfo.AssociatedNeuronIds.Take(10))}");
                    if (wordInfo.AssociatedNeuronIds.Count > 10)
                    {
                        Console.WriteLine($"   ... and {wordInfo.AssociatedNeuronIds.Count - 10} more");
                    }
                }
            }
            else
            {
                Console.WriteLine($"❌ Word '{word}' not found in vocabulary");
                Console.WriteLine($"📚 Vocabulary size: {vocab.Count} words");
                
                // Suggest similar words
                var similar = FindSimilarWords(word, vocab.Keys.ToList());
                if (similar.Any())
                {
                    Console.WriteLine($"💡 Did you mean: {string.Join(", ", similar.Take(5))}");
                }
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Show related concepts for a word based on shared neuron associations
        /// </summary>
        private async Task ShowRelatedConcepts(string word)
        {
            if (_brain == null)
            {
                Console.WriteLine("❌ Brain not loaded. Train first using --baseline-test");
                return;
            }
            
            Console.WriteLine($"=== Related Concepts for '{word}' ===");
            
            var vocab = _brain.ExportVocabulary();
            
            if (!vocab.ContainsKey(word.ToLower()))
            {
                Console.WriteLine($"❌ Word '{word}' not found in vocabulary");
                return;
            }
            
            var wordInfo = vocab[word.ToLower()];
            
            // Find words with shared neuron associations
            var related = new Dictionary<string, int>();
            
            foreach (var otherWord in vocab.Keys)
            {
                if (otherWord == word.ToLower())
                    continue;
                    
                var otherInfo = vocab[otherWord];
                
                // Calculate similarity based on shared neurons
                int sharedNeurons = 0;
                
                // Check concept neuron overlap
                if (wordInfo.ConceptNeuronId.HasValue && 
                    otherInfo.ConceptNeuronId.HasValue && 
                    wordInfo.ConceptNeuronId == otherInfo.ConceptNeuronId)
                {
                    sharedNeurons += 3; // Strong connection via concept neuron
                }
                
                // Check attention neuron overlap
                if (wordInfo.AttentionNeuronId.HasValue && 
                    otherInfo.AttentionNeuronId.HasValue && 
                    wordInfo.AttentionNeuronId == otherInfo.AttentionNeuronId)
                {
                    sharedNeurons += 2; // Moderate connection via attention
                }
                
                // Check associated neuron overlap
                var sharedAssociations = wordInfo.AssociatedNeuronIds
                    .Intersect(otherInfo.AssociatedNeuronIds)
                    .Count();
                sharedNeurons += sharedAssociations;
                
                if (sharedNeurons > 0)
                {
                    related[otherWord] = sharedNeurons;
                }
            }
            
            if (related.Any())
            {
                Console.WriteLine($"🔗 Found {related.Count} related concepts");
                Console.WriteLine($"📊 Top 15 by shared neural connections:");
                
                var topRelated = related.OrderByDescending(kvp => kvp.Value).Take(15);
                int rank = 1;
                foreach (var rel in topRelated)
                {
                    var relInfo = vocab[rel.Key];
                    Console.WriteLine($"   {rank,2}. {rel.Key,-20} (shared neurons: {rel.Value}, type: {relInfo.EstimatedType})");
                    rank++;
                }
            }
            else
            {
                Console.WriteLine("❌ No related concepts found");
                Console.WriteLine("ℹ️ This may indicate the word has no neural associations yet, or the brain needs more training");
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Show comprehensive BIOLOGICAL LEARNING statistics
        /// </summary>
        private async Task ShowStatistics()
        {
            if (_rawBrainState == null)
            {
                Console.WriteLine("❌ Brain not loaded. Train first using --baseline-test");
                return;
            }
            
            Console.WriteLine("=== BIOLOGICAL LEARNING Statistics ===");
            Console.WriteLine();
            
            // Parse neurons from JSON
            int neuronCount = 0;
            var clusterDistribution = new Dictionary<string, int>();
            var activationCounts = new Dictionary<int, int>();
            
            try
            {
                if (_rawBrainState.ContainsKey("neurons"))
                {
                    var neuronsJson = System.Text.Json.JsonSerializer.Serialize(_rawBrainState["neurons"]);
                    var neuronsDoc = System.Text.Json.JsonDocument.Parse(neuronsJson);
                    
                    // Neurons are stored as a dictionary/object: { "0": {...}, "1": {...}, ... }
                    neuronCount = neuronsDoc.RootElement.EnumerateObject().Count();
                    
                    Console.WriteLine($"🧠 NEURAL STRUCTURES:");
                    Console.WriteLine($"   Total neurons: {neuronCount:N0}");
                    
                    // Analyze first 1000 neurons for cluster patterns
                    int sampleSize = Math.Min(1000, neuronCount);
                    var clusterIds = new HashSet<string>();
                    var typeCounts = new Dictionary<string, int>();
                    int neuronsWithWeights = 0;
                    int totalActivations = 0;
                    
                    int index = 0;
                    foreach (var neuronEntry in neuronsDoc.RootElement.EnumerateObject())
                    {
                        if (index++ >= sampleSize) break;
                        
                        var neuron = neuronEntry.Value;
                        
                        if (neuron.TryGetProperty("clusterId", out var clusterIdProp))
                        {
                            var clusterId = clusterIdProp.GetString() ?? "";
                            clusterIds.Add(clusterId);
                            
                            // Extract cluster type (word_, verb_, etc)
                            var parts = clusterId.Split('_');
                            if (parts.Length > 0)
                            {
                                var type = parts[0];
                                typeCounts[type] = typeCounts.GetValueOrDefault(type) + 1;
                            }
                        }
                        
                        if (neuron.TryGetProperty("weights", out var weights))
                        {
                            if (weights.ValueKind == System.Text.Json.JsonValueKind.Object && weights.EnumerateObject().Any())
                            {
                                neuronsWithWeights++;
                            }
                        }
                        
                        if (neuron.TryGetProperty("activationCount", out var actCount))
                        {
                            totalActivations += actCount.GetInt32();
                        }
                    }
                    
                    Console.WriteLine($"   Unique clusters (sample): {clusterIds.Count:N0}");
                    Console.WriteLine($"   Neurons with connections: {neuronsWithWeights:N0} ({neuronsWithWeights * 100.0 / sampleSize:F1}%)");
                    Console.WriteLine($"   Avg activations/neuron: {totalActivations / (double)sampleSize:F2}");
                    
                    Console.WriteLine();
                    Console.WriteLine($"� CLUSTER TYPE DISTRIBUTION (sample of {sampleSize}):");
                    foreach (var type in typeCounts.OrderByDescending(kvp => kvp.Value).Take(10))
                    {
                        var percentage = type.Value * 100.0 / sampleSize;
                        Console.WriteLine($"   {type.Key,-20} {type.Value,6:N0} neurons ({percentage:F1}%)");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Could not parse neuron data: {ex.Message}");
            }
            
            // Check language learning data
            Console.WriteLine();
            try
            {
                if (_rawBrainState.ContainsKey("languageData"))
                {
                    var langDataJson = System.Text.Json.JsonSerializer.Serialize(_rawBrainState["languageData"]);
                    var langData = System.Text.Json.JsonDocument.Parse(langDataJson);
                    
                    Console.WriteLine($"📚 LANGUAGE LEARNING DATA:");
                    
                    if (langData.RootElement.TryGetProperty("learning_stats", out var stats))
                    {
                        if (stats.TryGetProperty("LearnedSentences", out var sentences))
                        {
                            Console.WriteLine($"   Sentences learned: {sentences.GetInt32():N0}");
                        }
                        if (stats.TryGetProperty("WordAssociations", out var assocs))
                        {
                            var assocCount = assocs.EnumerateObject().Count();
                            Console.WriteLine($"   Word associations: {assocCount:N0}");
                        }
                    }
                    
                    if (langData.RootElement.TryGetProperty("word_associations", out var wordAssocs))
                    {
                        var assocCount = wordAssocs.EnumerateObject().Count();
                        Console.WriteLine($"   Word association entries: {assocCount:N0}");
                    }
                    
                    if (langData.RootElement.TryGetProperty("sentence_patterns", out var patterns))
                    {
                        var patternCount = patterns.EnumerateObject().Count();
                        Console.WriteLine($"   Sentence patterns: {patternCount:N0}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Could not parse language data: {ex.Message}");
            }
            
            // Training session info
            Console.WriteLine();
            try
            {
                if (_rawBrainState.ContainsKey("trainingSession"))
                {
                    var sessionJson = System.Text.Json.JsonSerializer.Serialize(_rawBrainState["trainingSession"]);
                    var session = System.Text.Json.JsonDocument.Parse(sessionJson);
                    
                    Console.WriteLine($"� TRAINING SESSION:");
                    if (session.RootElement.TryGetProperty("startTime", out var start))
                    {
                        Console.WriteLine($"   Start: {start.GetDateTime():yyyy-MM-dd HH:mm:ss}");
                    }
                    if (session.RootElement.TryGetProperty("endTime", out var end))
                    {
                        Console.WriteLine($"   End: {end.GetDateTime():yyyy-MM-dd HH:mm:ss}");
                    }
                    if (session.RootElement.TryGetProperty("totalSentences", out var total))
                    {
                        Console.WriteLine($"   Total sentences processed: {total.GetInt32():N0}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Could not parse training session: {ex.Message}");
            }
            
            // Vocabulary comparison
            Console.WriteLine();
            if (_brain != null)
            {
                var vocab = _brain.ExportVocabulary();
                Console.WriteLine($"📖 VOCABULARY (for comparison):");
                Console.WriteLine($"   Total words: {vocab.Count:N0}");
                Console.WriteLine();
                Console.WriteLine($"🔬 ANALYSIS:");
                Console.WriteLine($"   Neurons per word: {neuronCount / (double)Math.Max(1, vocab.Count):F1}");
                Console.WriteLine($"   This shows biological learning complexity beyond vocabulary");
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Show a sample of vocabulary
        /// </summary>
        private async Task ShowVocabularySample()
        {
            if (_brain == null)
            {
                Console.WriteLine("❌ Brain not loaded. Train first using --baseline-test");
                return;
            }
            
            Console.WriteLine("=== Vocabulary Sample ===");
            
            var vocab = _brain.ExportVocabulary();
            
            if (!vocab.Any())
            {
                Console.WriteLine("❌ Vocabulary is empty");
                return;
            }
            
            Console.WriteLine($"📚 Showing 50 random words from {vocab.Count:N0} total:");
            Console.WriteLine();
            
            var random = new Random();
            var sample = vocab.OrderBy(_ => random.Next()).Take(50);
            
            int count = 1;
            foreach (var word in sample)
            {
                var type = word.Value.EstimatedType;
                var freq = word.Value.Frequency;
                Console.WriteLine($"{count,3}. {word.Key,-25} [{type,-10}] (frequency: {freq})");
                count++;
            }
            
            await Task.CompletedTask;
        }

        private List<string> FindSimilarWords(string word, List<string> vocabulary)
        {
            // Simple Levenshtein distance-based similarity
            var similar = new List<(string word, int distance)>();
            
            foreach (var vocabWord in vocabulary)
            {
                var distance = LevenshteinDistance(word.ToLower(), vocabWord);
                if (distance <= 2) // Allow up to 2 character differences
                {
                    similar.Add((vocabWord, distance));
                }
            }
            
            return similar.OrderBy(x => x.distance).Select(x => x.word).ToList();
        }

        private int LevenshteinDistance(string s1, string s2)
        {
            var d = new int[s1.Length + 1, s2.Length + 1];
            
            for (int i = 0; i <= s1.Length; i++)
                d[i, 0] = i;
                
            for (int j = 0; j <= s2.Length; j++)
                d[0, j] = j;
                
            for (int j = 1; j <= s2.Length; j++)
            {
                for (int i = 1; i <= s1.Length; i++)
                {
                    int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            
            return d[s1.Length, s2.Length];
        }

        /// <summary>
        /// List concepts filtered by source (Week 3)
        /// </summary>
        private async Task ListConceptsBySource(string sourceName)
        {
            if (_brain == null)
            {
                Console.WriteLine("❌ Brain not loaded. Train first using multi-source trainer");
                return;
            }
            
            Console.WriteLine($"=== Concepts from '{sourceName}' ===\n");
            
            var vocab = _brain.ExportVocabulary();
            var sourceWords = vocab.Where(kvp => 
                kvp.Value.SourceFrequencies != null && 
                kvp.Value.SourceFrequencies.ContainsKey(sourceName))
                .OrderByDescending(kvp => kvp.Value.SourceFrequencies[sourceName])
                .ToList();
            
            if (!sourceWords.Any())
            {
                Console.WriteLine($"❌ No concepts found from source: {sourceName}");
                Console.WriteLine($"📚 Available sources:");
                
                var allSources = vocab.Values
                    .Where(w => w.SourceFrequencies != null)
                    .SelectMany(w => w.SourceFrequencies.Keys)
                    .Distinct()
                    .ToList();
                    
                foreach (var source in allSources)
                {
                    Console.WriteLine($"   • {source}");
                }
                return;
            }
            
            Console.WriteLine($"✅ Found {sourceWords.Count} concepts from {sourceName}");
            Console.WriteLine($"\n📊 Top 20 concepts (by frequency from this source):\n");
            
            int rank = 1;
            foreach (var kvp in sourceWords.Take(20))
            {
                var word = kvp.Key;
                var info = kvp.Value;
                var sourceFreq = info.SourceFrequencies[sourceName];
                Console.WriteLine($"   {rank,2}. {word,-20} (appeared {sourceFreq,3}x, type: {info.EstimatedType})");
                rank++;
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Get comprehensive source statistics (Week 3)
        /// </summary>
        private async Task ShowSourceStatistics()
        {
            if (_brain == null)
            {
                Console.WriteLine("❌ Brain not loaded. Train first using multi-source trainer");
                return;
            }
            
            Console.WriteLine("=== Multi-Source Statistics ===\n");
            
            var vocab = _brain.ExportVocabulary();
            var wordsWithSources = vocab.Values.Where(w => w.SourceFrequencies != null && w.SourceFrequencies.Any()).ToList();
            
            if (!wordsWithSources.Any())
            {
                Console.WriteLine("❌ No multi-source data found");
                Console.WriteLine("ℹ️ Train using MultiSourceTrainer to enable source tracking");
                return;
            }
            
            // Aggregate by source
            var sourceStats = new Dictionary<string, (int words, int totalOccurrences)>();
            
            foreach (var word in wordsWithSources)
            {
                foreach (var sourceKvp in word.SourceFrequencies!)
                {
                    if (!sourceStats.ContainsKey(sourceKvp.Key))
                    {
                        sourceStats[sourceKvp.Key] = (0, 0);
                    }
                    
                    var current = sourceStats[sourceKvp.Key];
                    sourceStats[sourceKvp.Key] = (current.words + 1, current.totalOccurrences + sourceKvp.Value);
                }
            }
            
            Console.WriteLine($"📚 Total vocabulary: {vocab.Count:N0} words");
            Console.WriteLine($"🔖 Words with source tracking: {wordsWithSources.Count:N0} ({(wordsWithSources.Count * 100.0 / vocab.Count):F1}%)");
            Console.WriteLine($"\n📊 Breakdown by source:\n");
            
            foreach (var source in sourceStats.OrderByDescending(kvp => kvp.Value.words))
            {
                var stats = source.Value;
                var percentage = (stats.words * 100.0) / vocab.Count;
                Console.WriteLine($"   {source.Key,-15} {stats.words,6:N0} words ({percentage,5:F1}%) - {stats.totalOccurrences,8:N0} occurrences");
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Search concepts by pattern (substring match) (Week 3)
        /// </summary>
        private async Task SearchConcepts(string pattern)
        {
            if (_brain == null)
            {
                Console.WriteLine("❌ Brain not loaded. Train first");
                return;
            }
            
            Console.WriteLine($"=== Searching for '{pattern}' ===\n");
            
            var vocab = _brain.ExportVocabulary();
            var matches = vocab.Where(kvp => kvp.Key.Contains(pattern.ToLower()))
                .OrderByDescending(kvp => kvp.Value.Frequency)
                .ToList();
            
            if (!matches.Any())
            {
                Console.WriteLine($"❌ No concepts found matching: {pattern}");
                return;
            }
            
            Console.WriteLine($"✅ Found {matches.Count} matching concepts\n");
            Console.WriteLine($"📊 Results (showing top 20):\n");
            
            int rank = 1;
            foreach (var kvp in matches.Take(20))
            {
                var word = kvp.Key;
                var info = kvp.Value;
                
                // Show source info if available
                string sourceInfo = "";
                if (info.SourceFrequencies != null && info.SourceFrequencies.Any())
                {
                    var sources = string.Join(", ", info.SourceFrequencies.Keys.Take(3));
                    if (info.SourceFrequencies.Count > 3)
                    {
                        sources += "...";
                    }
                    sourceInfo = $" [from: {sources}]";
                }
                
                Console.WriteLine($"   {rank,2}. {word,-25} (freq: {info.Frequency,4}, type: {info.EstimatedType}){sourceInfo}");
                rank++;
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Export brain knowledge to JSON (Week 3)
        /// </summary>
        private async Task ExportToJson(string outputPath)
        {
            if (_brain == null)
            {
                Console.WriteLine("❌ Brain not loaded. Train first");
                return;
            }
            
            Console.WriteLine($"=== Exporting Brain to JSON ===\n");
            Console.WriteLine($"📁 Output: {outputPath}");
            
            try
            {
                var vocab = _brain.ExportVocabulary();
                var neurons = _brain.ExportNeurons();
                var languageData = _brain.ExportLanguageData();
                
                var export = new Dictionary<string, object>
                {
                    ["exportedAt"] = DateTime.UtcNow,
                    ["vocabularySize"] = vocab.Count,
                    ["neuronCount"] = neurons.Count,
                    ["vocabulary"] = vocab.Select(kvp => new
                    {
                        word = kvp.Key,
                        frequency = kvp.Value.Frequency,
                        firstSeen = kvp.Value.FirstSeen,
                        type = kvp.Value.EstimatedType.ToString(),
                        conceptNeuronId = kvp.Value.ConceptNeuronId,
                        attentionNeuronId = kvp.Value.AttentionNeuronId,
                        associatedNeurons = kvp.Value.AssociatedNeuronIds,
                        sources = kvp.Value.SourceFrequencies
                    }).ToList(),
                    ["languageData"] = languageData
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(export, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                await File.WriteAllTextAsync(outputPath, json);
                
                var fileInfo = new FileInfo(outputPath);
                Console.WriteLine($"✅ Export successful");
                Console.WriteLine($"📊 File size: {fileInfo.Length / 1024.0 / 1024.0:F2} MB");
                Console.WriteLine($"📚 Exported: {vocab.Count:N0} words, {neurons.Count:N0} neurons");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Export failed: {ex.Message}");
            }
        }

        private void ShowHelp()
        {
            Console.WriteLine("=== Knowledge Query System - Help ===");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run -- --knowledge-query [command] [args]");
            Console.WriteLine();
            Console.WriteLine("Basic Commands:");
            Console.WriteLine("  --query <word>           Query detailed information about a word");
            Console.WriteLine("  --related <word>         Show semantically related concepts");
            Console.WriteLine("  --stats                  Show comprehensive brain statistics");
            Console.WriteLine("  --sample                 Show random vocabulary sample");
            Console.WriteLine();
            Console.WriteLine("Multi-Source Commands (Week 3):");
            Console.WriteLine("  --source <name>          List concepts from specific data source");
            Console.WriteLine("  --source-stats           Show breakdown by data source");
            Console.WriteLine("  --search <pattern>       Search concepts by substring pattern");
            Console.WriteLine("  --export [path]          Export brain to JSON (optional path)");
            Console.WriteLine();
            Console.WriteLine("  --help                   Show this help");
            Console.WriteLine();
            Console.WriteLine("Interactive mode:");
            Console.WriteLine("  dotnet run -- --knowledge-query");
            Console.WriteLine("  (no arguments starts interactive mode with query prompt)");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run -- --knowledge-query --query cat");
            Console.WriteLine("  dotnet run -- --knowledge-query --related computer");
            Console.WriteLine("  dotnet run -- --knowledge-query --stats");
            Console.WriteLine("  dotnet run -- --knowledge-query --source Tatoeba");
            Console.WriteLine("  dotnet run -- --knowledge-query --search animal");
            Console.WriteLine("  dotnet run -- --knowledge-query --export brain.json");
        }
    }
}
