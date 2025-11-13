using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GreyMatter.Storage;

namespace GreyMatter.Core
{
    /// <summary>
    /// Implementation of IKnowledgeQuery that reads from production storage.
    /// Makes trained knowledge accessible and useful.
    /// </summary>
    public class ProductionKnowledgeQuery : IKnowledgeQuery
    {
        private readonly ProductionStorageManager _storage;
        private readonly string _brainDataPath;
        
        public ProductionKnowledgeQuery(ProductionStorageManager storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _brainDataPath = _storage.ActiveBrainPath;
        }
        
        /// <summary>
        /// Find words that co-occur with the given word in learned sentences.
        /// </summary>
        public async Task<List<WordAssociation>> GetWordAssociationsAsync(string word, int limit = 10)
        {
            word = word.ToLowerInvariant();
            var associations = new Dictionary<string, int>();
            
            // Read episodic memory to find co-occurrences
            var episodicPath = Path.Combine(_brainDataPath, "episodic_memory");
            if (!Directory.Exists(episodicPath))
            {
                return new List<WordAssociation>();
            }
            
            // Scan recent episodic memory files
            var files = Directory.GetFiles(episodicPath, "*.json")
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .Take(100); // Scan last 100 memory files
            
            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var memory = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    
                    if (memory != null && memory.TryGetValue("description", out var desc))
                    {
                        var text = desc.ToString()?.ToLowerInvariant() ?? "";
                        if (text.Contains(word))
                        {
                            // Extract words from this sentence
                            var words = text.Split(new[] { ' ', ',', '.', '!', '?', ';', ':' }, 
                                StringSplitOptions.RemoveEmptyEntries);
                            
                            foreach (var w in words)
                            {
                                if (w != word && w.Length > 2) // Skip the query word and short words
                                {
                                    associations[w] = associations.GetValueOrDefault(w, 0) + 1;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Skip malformed files
                }
            }
            
            // Convert to WordAssociation objects with confidence scores
            var totalCoOccurrences = associations.Values.Sum();
            var results = associations
                .OrderByDescending(kv => kv.Value)
                .Take(limit)
                .Select(kv => new WordAssociation
                {
                    Word = kv.Key,
                    Confidence = totalCoOccurrences > 0 ? (double)kv.Value / totalCoOccurrences : 0,
                    RelationType = "co-occurs",
                    CoOccurrenceCount = kv.Value
                })
                .ToList();
            
            return results;
        }
        
        /// <summary>
        /// Search for syntactic patterns in learned content.
        /// </summary>
        public async Task<List<PatternMatch>> FindPatternsAsync(string pattern, int limit = 10)
        {
            var matches = new List<PatternMatch>();
            var episodicPath = Path.Combine(_brainDataPath, "episodic_memory");
            
            if (!Directory.Exists(episodicPath))
            {
                return matches;
            }
            
            var files = Directory.GetFiles(episodicPath, "*.json")
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .Take(200);
            
            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var memory = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    
                    if (memory != null && memory.TryGetValue("description", out var desc))
                    {
                        var text = desc.ToString() ?? "";
                        // Simple pattern matching - can be enhanced with NLP
                        if (text.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        {
                            matches.Add(new PatternMatch
                            {
                                Pattern = pattern,
                                Example = text.Length > 100 ? text.Substring(0, 100) + "..." : text,
                                Confidence = 1.0,
                                Frequency = 1
                            });
                            
                            if (matches.Count >= limit)
                                break;
                        }
                    }
                }
                catch
                {
                    // Skip malformed files
                }
            }
            
            return matches;
        }
        
        /// <summary>
        /// Search episodic memory with natural language query.
        /// </summary>
        public async Task<List<EpisodicMemory>> SearchEpisodicMemoryAsync(string query, int limit = 10)
        {
            var results = new List<EpisodicMemory>();
            var episodicPath = Path.Combine(_brainDataPath, "episodic_memory");
            
            if (!Directory.Exists(episodicPath))
            {
                return results;
            }
            
            var queryWords = query.ToLowerInvariant()
                .Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToHashSet();
            
            var files = Directory.GetFiles(episodicPath, "*.json")
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .Take(500);
            
            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var memory = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    
                    if (memory != null && memory.TryGetValue("description", out var desc))
                    {
                        var text = desc.ToString()?.ToLowerInvariant() ?? "";
                        var textWords = text.Split(new[] { ' ', ',', '.', '!', '?', ';', ':' }, 
                            StringSplitOptions.RemoveEmptyEntries)
                            .ToHashSet();
                        
                        // Calculate relevance based on word overlap
                        var matchCount = queryWords.Intersect(textWords).Count();
                        if (matchCount > 0)
                        {
                            var relevance = (double)matchCount / queryWords.Count;
                            
                            results.Add(new EpisodicMemory
                            {
                                Timestamp = new FileInfo(file).LastWriteTime,
                                Description = desc.ToString() ?? "",
                                Context = memory.TryGetValue("context", out var ctx) ? ctx.ToString() ?? "" : "",
                                Relevance = relevance,
                                Metadata = memory
                            });
                        }
                    }
                }
                catch
                {
                    // Skip malformed files
                }
            }
            
            return results
                .OrderByDescending(r => r.Relevance)
                .Take(limit)
                .ToList();
        }
        
        /// <summary>
        /// Get comprehensive vocabulary statistics from latest checkpoint.
        /// </summary>
        public async Task<VocabularyStats> GetVocabularyStatsAsync()
        {
            var checkpointsPath = Path.Combine(_brainDataPath, "checkpoints");
            
            if (!Directory.Exists(checkpointsPath))
            {
                return new VocabularyStats();
            }
            
            // Find latest checkpoint
            var latestCheckpoint = Directory.GetDirectories(checkpointsPath)
                .OrderByDescending(d => new DirectoryInfo(d).LastWriteTime)
                .FirstOrDefault();
            
            if (latestCheckpoint == null)
            {
                return new VocabularyStats();
            }
            
            // Read checkpoint metadata
            var metadataPath = Path.Combine(latestCheckpoint, "metadata.json");
            if (!File.Exists(metadataPath))
            {
                return new VocabularyStats();
            }
            
            var json = await File.ReadAllTextAsync(metadataPath);
            var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            
            if (metadata == null)
            {
                return new VocabularyStats();
            }
            
            var stats = new VocabularyStats();
            
            if (metadata.TryGetValue("VocabularySize", out var vocabSize))
            {
                stats.TotalWords = vocabSize.GetInt32();
                stats.UniqueWords = vocabSize.GetInt32();
            }
            
            if (metadata.TryGetValue("SentencesProcessed", out var sentCount))
            {
                stats.TotalSentences = sentCount.GetInt32();
            }
            
            if (stats.TotalSentences > 0 && stats.TotalWords > 0)
            {
                stats.AverageWordFrequency = (double)stats.TotalWords / stats.TotalSentences;
            }
            
            // TODO: Parse vocabulary.json for frequency data once it's properly saved
            
            return stats;
        }
        
        /// <summary>
        /// Get list of learned words (currently limited by vocabulary storage format).
        /// </summary>
        public async Task<List<VocabularyWord>> GetLearnedWordsAsync(int minFrequency = 1, int limit = 1000)
        {
            var words = new List<VocabularyWord>();
            
            // TODO: This requires vocabulary.json to be properly saved with word list
            // Currently checkpoints only save vocabulary count, not the actual words
            // This is a limitation we should fix in ProductionTrainingService
            
            await Task.CompletedTask;
            return words;
        }
        
        /// <summary>
        /// Test comprehension by asking questions about learned content.
        /// </summary>
        public async Task<double> TestComprehensionAsync(List<ComprehensionQuestion> questions)
        {
            if (questions.Count == 0)
                return 0.0;
            
            int correct = 0;
            
            foreach (var q in questions)
            {
                // Search episodic memory for relevant context
                var memories = await SearchEpisodicMemoryAsync(q.Question, limit: 5);
                
                // Simple check: if any memory contains the expected answer
                var hasAnswer = memories.Any(m => 
                    m.Description.Contains(q.ExpectedAnswer, StringComparison.OrdinalIgnoreCase));
                
                if (hasAnswer)
                    correct++;
            }
            
            return (double)correct / questions.Count;
        }
    }
}
