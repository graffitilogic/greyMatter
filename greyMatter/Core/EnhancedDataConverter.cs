using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using GreyMatter.Core;
using GreyMatter.Storage;

namespace GreyMatter
{
    /// <summary>
    /// Enhanced Data Converter for diverse training sources
    /// Processes multiple data formats and integrates with existing learning pipeline
    /// </summary>
    public class EnhancedDataConverter
    {
        private readonly string _dataRoot;
        private readonly string _outputPath;
        private Dictionary<string, WordData> _wordDatabase;
        private Dictionary<string, Dictionary<string, int>> _wordCooccurrences;

        public EnhancedDataConverter(string dataRoot, string outputPath)
        {
            _dataRoot = dataRoot;
            _outputPath = outputPath;
            _wordDatabase = new Dictionary<string, WordData>();
            _wordCooccurrences = new Dictionary<string, Dictionary<string, int>>();
        }

        public async Task ConvertAllSourcesAsync(int maxSentences = 50000)
        {
            Console.WriteLine("🚀 **ENHANCED DATA CONVERSION & INTEGRATION**");
            Console.WriteLine("==========================================");
            Console.WriteLine($"Processing diverse data sources, max {maxSentences} sentences total");

            // Load existing data if available
            await LoadExistingDataAsync();

            // Process each data source
            await ProcessOpenSubtitlesAsync(maxSentences / 8);
            await ProcessNewsHeadlinesAsync(maxSentences / 8);
            await ProcessScientificAbstractsAsync(maxSentences / 8);
            await ProcessChildrensStoriesAsync(maxSentences / 8);
            await ProcessTechnicalDocsAsync(maxSentences / 8);
            await ProcessSocialMediaAsync(maxSentences / 8);
            await ProcessIdiomsAndExpressionsAsync(maxSentences / 8);
            await ProcessMultilingualCorpusAsync(maxSentences / 8);

            // Build integrated learning data
            BuildIntegratedCooccurrenceMatrix();
            await GenerateIntegratedLearningPatternsAsync();

            // Save enhanced learning data
            await SaveEnhancedLearningDataAsync();

            Console.WriteLine("\n✅ **ENHANCED DATA INTEGRATION COMPLETE**");
            Console.WriteLine($"Words learned: {_wordDatabase.Count}");
            Console.WriteLine($"Co-occurrence pairs: {_wordCooccurrences.Count}");
        }

        private async Task LoadExistingDataAsync()
        {
            var existingWordDbPath = Path.Combine(_outputPath, "enhanced_word_database.json");
            var existingCooccurrencesPath = Path.Combine(_outputPath, "enhanced_cooccurrence_matrix.json");

            if (File.Exists(existingWordDbPath))
            {
                Console.WriteLine("📚 Loading existing enhanced word database...");
                var json = await File.ReadAllTextAsync(existingWordDbPath);
                var existingData = JsonSerializer.Deserialize<Dictionary<string, WordData>>(json);
                if (existingData != null)
                {
                    foreach (var kvp in existingData)
                    {
                        _wordDatabase[kvp.Key] = kvp.Value;
                    }
                }
            }

            if (File.Exists(existingCooccurrencesPath))
            {
                Console.WriteLine("🔗 Loading existing co-occurrence matrix...");
                var json = await File.ReadAllTextAsync(existingCooccurrencesPath);
                var existingCooccurrences = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(json);
                if (existingCooccurrences != null)
                {
                    _wordCooccurrences = existingCooccurrences;
                }
            }
        }

        private async Task ProcessOpenSubtitlesAsync(int maxSentences)
        {
            Console.WriteLine("🎬 Processing OpenSubtitles conversational data...");
            var filePath = Path.Combine(_dataRoot, "enhanced_sources", "OpenSubtitles", "opensubtitles_sample.txt");

            if (File.Exists(filePath))
            {
                var lines = await File.ReadAllLinesAsync(filePath);
                var processed = 0;

                foreach (var line in lines.Take(maxSentences))
                {
                    if (!string.IsNullOrWhiteSpace(line) && line.Length > 10)
                    {
                        await ProcessSentenceAsync(line.Trim(), "conversational");
                        processed++;
                        if (processed >= maxSentences) break;
                    }
                }

                Console.WriteLine($"   → Processed {processed} conversational sentences");
            }
        }

        private async Task ProcessNewsHeadlinesAsync(int maxSentences)
        {
            Console.WriteLine("📰 Processing news headlines...");
            var filePath = Path.Combine(_dataRoot, "enhanced_sources", "NewsData", "headlines_sample.txt");

            if (File.Exists(filePath))
            {
                var content = await File.ReadAllTextAsync(filePath);
                var lines = content.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).Take(maxSentences);

                foreach (var line in lines)
                {
                    await ProcessSentenceAsync(line.Trim(), "formal");
                }

                Console.WriteLine($"   → Processed {lines.Count()} news headlines");
            }
        }

        private async Task ProcessScientificAbstractsAsync(int maxSentences)
        {
            Console.WriteLine("🔬 Processing scientific abstracts...");
            var filePath = Path.Combine(_dataRoot, "enhanced_sources", "ScienceData", "scientific_abstracts.txt");

            if (File.Exists(filePath))
            {
                var content = await File.ReadAllTextAsync(filePath);
                var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => s.Trim().Length > 20)
                    .Take(maxSentences);

                foreach (var sentence in sentences)
                {
                    await ProcessSentenceAsync(sentence.Trim(), "academic");
                }

                Console.WriteLine($"   → Processed {sentences.Count()} scientific sentences");
            }
        }

        private async Task ProcessChildrensStoriesAsync(int maxSentences)
        {
            Console.WriteLine("📖 Processing children's stories...");
            var filePath = Path.Combine(_dataRoot, "enhanced_sources", "ChildrensLiterature", "childrens_stories.txt");

            if (File.Exists(filePath))
            {
                var content = await File.ReadAllTextAsync(filePath);
                var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => s.Trim().Length > 10)
                    .Take(maxSentences);

                foreach (var sentence in sentences)
                {
                    await ProcessSentenceAsync(sentence.Trim(), "narrative");
                }

                Console.WriteLine($"   → Processed {sentences.Count()} story sentences");
            }
        }

        private async Task ProcessTechnicalDocsAsync(int maxSentences)
        {
            Console.WriteLine("💻 Processing technical documentation...");
            var filePath = Path.Combine(_dataRoot, "enhanced_sources", "TechnicalDocs", "technical_docs.txt");

            if (File.Exists(filePath))
            {
                var content = await File.ReadAllTextAsync(filePath);
                var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => s.Trim().Length > 15)
                    .Take(maxSentences);

                foreach (var sentence in sentences)
                {
                    await ProcessSentenceAsync(sentence.Trim(), "technical");
                }

                Console.WriteLine($"   → Processed {sentences.Count()} technical sentences");
            }
        }

        private async Task ProcessSocialMediaAsync(int maxSentences)
        {
            Console.WriteLine("💬 Processing social media patterns...");
            var filePath = Path.Combine(_dataRoot, "enhanced_sources", "SocialMedia", "social_media.txt");

            if (File.Exists(filePath))
            {
                var lines = await File.ReadAllLinesAsync(filePath);
                var processed = 0;

                foreach (var line in lines.Take(maxSentences))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        await ProcessSentenceAsync(line.Trim(), "informal");
                        processed++;
                    }
                }

                Console.WriteLine($"   → Processed {processed} social media sentences");
            }
        }

        private async Task ProcessIdiomsAndExpressionsAsync(int maxSentences)
        {
            Console.WriteLine("🎭 Processing idioms and expressions...");
            var filePath = Path.Combine(_dataRoot, "enhanced_sources", "enhanced_sources", "idioms_expressions.json");

            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, List<dynamic>>>(json);

                if (data != null)
                {
                    var processed = 0;
                    foreach (var category in data.Values)
                    {
                        foreach (var item in category.Take(maxSentences / data.Count))
                        {
                            if (item is JsonElement element)
                            {
                                var idiom = element.GetProperty("idiom").GetString();
                                var example = element.GetProperty("example").GetString();

                                if (!string.IsNullOrEmpty(idiom))
                                    await ProcessSentenceAsync(idiom, "figurative");
                                if (!string.IsNullOrEmpty(example))
                                    await ProcessSentenceAsync(example, "figurative");

                                processed += 2;
                                if (processed >= maxSentences) break;
                            }
                        }
                        if (processed >= maxSentences) break;
                    }

                    Console.WriteLine($"   → Processed {processed} idiomatic expressions");
                }
            }
        }

        private async Task ProcessMultilingualCorpusAsync(int maxSentences)
        {
            Console.WriteLine("🌍 Processing multilingual corpus...");
            var filePath = Path.Combine(_dataRoot, "enhanced_sources", "enhanced_sources", "parallel_corpus.json");

            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, List<dynamic>>>(json);

                if (data != null)
                {
                    var processed = 0;
                    foreach (var languagePair in data.Values)
                    {
                        foreach (var item in languagePair.Take(maxSentences / data.Count))
                        {
                            if (item is JsonElement element)
                            {
                                var english = element.GetProperty("en").GetString();
                                if (!string.IsNullOrEmpty(english))
                                {
                                    await ProcessSentenceAsync(english, "multilingual");
                                    processed++;
                                }
                            }
                        }
                        if (processed >= maxSentences) break;
                    }

                    Console.WriteLine($"   → Processed {processed} multilingual sentences");
                }
            }
        }

        private async Task ProcessSentenceAsync(string sentence, string context)
        {
            if (string.IsNullOrWhiteSpace(sentence)) return;

            // Tokenize and process words
            var words = TokenizeSentence(sentence);

            // Update word database
            foreach (var word in words)
            {
                if (!_wordDatabase.ContainsKey(word))
                {
                    _wordDatabase[word] = new WordData
                    {
                        Word = word,
                        Frequency = 1,
                        Contexts = new List<string> { context },
                        CooccurringWords = new Dictionary<string, int>()
                    };
                }
                else
                {
                    _wordDatabase[word].Frequency++;
                    if (!_wordDatabase[word].Contexts.Contains(context))
                    {
                        _wordDatabase[word].Contexts.Add(context);
                    }
                }
            }

            // Update co-occurrence matrix
            for (int i = 0; i < words.Count; i++)
            {
                var word1 = words[i];

                if (!_wordCooccurrences.ContainsKey(word1))
                {
                    _wordCooccurrences[word1] = new Dictionary<string, int>();
                }

                // Look at surrounding words (window size 3)
                for (int j = Math.Max(0, i - 3); j < Math.Min(words.Count, i + 4); j++)
                {
                    if (i != j)
                    {
                        var word2 = words[j];
                        if (!_wordCooccurrences[word1].ContainsKey(word2))
                        {
                            _wordCooccurrences[word1][word2] = 1;
                        }
                        else
                        {
                            _wordCooccurrences[word1][word2]++;
                        }
                    }
                }
            }
        }

        private List<string> TokenizeSentence(string sentence)
        {
            // Enhanced tokenization - filter out numbers and meaningless tokens
            var words = sentence.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim(new[] { '.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']', '{', '}' }))
                .Where(w => !string.IsNullOrWhiteSpace(w) && 
                           w.Length >= 3 && // At least 3 characters
                           !int.TryParse(w, out _) && // Not a number
                           !long.TryParse(w, out _) && // Not a large number
                           !w.All(char.IsDigit) && // Not all digits
                           w.Any(char.IsLetter)) // Must contain at least one letter
                .Select(w => w.ToLower())
                .ToList();

            return words;
        }

        private void BuildIntegratedCooccurrenceMatrix()
        {
            Console.WriteLine("🔗 Building integrated co-occurrence matrix...");

            // Clean up rare co-occurrences to reduce noise
            var keysToRemove = new List<string>();
            foreach (var kvp in _wordCooccurrences)
            {
                var totalCooccurrences = kvp.Value.Values.Sum();
                if (totalCooccurrences < 3) // Remove words with very few co-occurrences
                {
                    keysToRemove.Add(kvp.Key);
                }
                else
                {
                    // Keep only top co-occurring words (reduce matrix size)
                    var topCooccurrences = kvp.Value.OrderByDescending(x => x.Value)
                        .Take(20)
                        .ToDictionary(x => x.Key, x => x.Value);
                    _wordCooccurrences[kvp.Key] = topCooccurrences;
                }
            }

            foreach (var key in keysToRemove)
            {
                _wordCooccurrences.Remove(key);
            }

            Console.WriteLine($"   → Matrix size: {_wordCooccurrences.Count} words");
        }

        private async Task GenerateIntegratedLearningPatternsAsync()
        {
            Console.WriteLine("🎯 Generating integrated learning patterns...");

            // Ensure output directory exists
            Directory.CreateDirectory(_outputPath);

            var patterns = new List<LearningPattern>();

            // Generate patterns from co-occurrence data
            foreach (var kvp in _wordCooccurrences.Take(1000)) // Limit for performance
            {
                var word = kvp.Key;
                var cooccurrences = kvp.Value.OrderByDescending(x => x.Value).Take(5);

                var pattern = new LearningPattern
                {
                    Word = word,
                    RelatedWords = cooccurrences.Select(x => x.Key).ToList(),
                    PatternType = "cooccurrence",
                    Confidence = cooccurrences.Average(x => x.Value) / 10.0,
                    Contexts = _wordDatabase.ContainsKey(word) ? _wordDatabase[word].Contexts : new List<string>()
                };

                patterns.Add(pattern);
            }

            var patternsPath = Path.Combine(_outputPath, "enhanced_learned_patterns.json");
            var json = JsonSerializer.Serialize(patterns, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(patternsPath, json);

            Console.WriteLine($"   → Generated {patterns.Count} learning patterns");
        }

        private async Task SaveEnhancedLearningDataAsync()
        {
            Console.WriteLine("💾 Saving enhanced learning data...");

            // Ensure output directory exists
            Directory.CreateDirectory(_outputPath);

            // Save word database
            var wordDbPath = Path.Combine(_outputPath, "enhanced_word_database.json");
            var wordDbJson = JsonSerializer.Serialize(_wordDatabase, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(wordDbPath, wordDbJson);

            // Save co-occurrence matrix
            var cooccurrencePath = Path.Combine(_outputPath, "enhanced_cooccurrence_matrix.json");
            var cooccurrenceJson = JsonSerializer.Serialize(_wordCooccurrences, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(cooccurrencePath, cooccurrenceJson);

            // Save summary statistics
            var stats = new
            {
                TotalWords = _wordDatabase.Count,
                TotalCooccurrencePairs = _wordCooccurrences.Sum(x => x.Value.Count),
                AverageFrequency = _wordDatabase.Values.Average(x => x.Frequency),
                ContextTypes = _wordDatabase.Values.SelectMany(x => x.Contexts).Distinct().ToList(),
                Timestamp = DateTime.UtcNow
            };

            var statsPath = Path.Combine(_outputPath, "enhanced_data_stats.json");
            var statsJson = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(statsPath, statsJson);

            Console.WriteLine("   → Enhanced data saved successfully");
        }

        // Data structures
        public class WordData
        {
            public string Word { get; set; }
            public int Frequency { get; set; }
            public List<string> Contexts { get; set; } = new();
            public Dictionary<string, int> CooccurringWords { get; set; } = new();
        }

        public class LearningPattern
        {
            public string Word { get; set; }
            public List<string> RelatedWords { get; set; } = new();
            public string PatternType { get; set; }
            public double Confidence { get; set; }
            public List<string> Contexts { get; set; } = new();
        }
    }
}
