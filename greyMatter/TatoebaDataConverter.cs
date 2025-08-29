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
    /// Converts Tatoeba TSV data to JSON format and builds learning data
    /// </summary>
    public class TatoebaDataConverter
    {
        private readonly string _tatoebaPath;
        private readonly string _outputPath;
        private readonly Dictionary<string, WordData> _wordDatabase;
        private readonly Dictionary<string, Dictionary<string, int>> _wordCooccurrences;

        public TatoebaDataConverter(string tatoebaPath, string outputPath)
        {
            _tatoebaPath = tatoebaPath;
            _outputPath = outputPath;
            _wordDatabase = new Dictionary<string, WordData>();
            _wordCooccurrences = new Dictionary<string, Dictionary<string, int>>();
        }

        public async Task ConvertAndBuildLearningDataAsync(int maxSentences = 10000)
        {
            Console.WriteLine("🔄 **TATOEBA DATA CONVERSION & LEARNING BUILD**");
            Console.WriteLine("==============================================");
            Console.WriteLine($"Processing up to {maxSentences} sentences from Tatoeba dataset");

            // Step 1: Parse Tatoeba TSV data
            await ParseTatoebaSentencesAsync(maxSentences);

            // Step 2: Build word co-occurrence matrix
            BuildCooccurrenceMatrix();

            // Step 3: Generate learning patterns
            await GenerateLearningPatternsAsync();

            // Step 4: Save learning data
            await SaveLearningDataAsync();

            Console.WriteLine("\n✅ **CONVERSION COMPLETE**");
            Console.WriteLine($"Words learned: {_wordDatabase.Count}");
            Console.WriteLine($"Co-occurrence pairs: {_wordCooccurrences.Count}");
        }

        private async Task ParseTatoebaSentencesAsync(int maxSentences)
        {
            Console.WriteLine("\n📖 **PARSING TATOEBA SENTENCES**");

            var sentences = new List<string>();
            var lineCount = 0;

            using (var reader = new StreamReader(_tatoebaPath))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null && lineCount < maxSentences)
                {
                    lineCount++;
                    if (lineCount % 1000 == 0)
                    {
                        Console.WriteLine($"Processed {lineCount} lines...");
                    }

                    // Parse TSV format: ID<TAB>Language<TAB>Sentence
                    var parts = line.Split('\t');
                    if (parts.Length >= 3 && parts[1] == "eng") // English only
                    {
                        var sentence = parts[2].Trim();
                        if (!string.IsNullOrEmpty(sentence))
                        {
                            sentences.Add(sentence);
                            ProcessSentence(sentence);
                        }
                    }
                }
            }

            Console.WriteLine($"Found {sentences.Count} English sentences");
        }

        private void ProcessSentence(string sentence)
        {
            // Clean and tokenize sentence
            var words = TokenizeSentence(sentence);

            // Update word database
            foreach (var word in words)
            {
                if (!_wordDatabase.ContainsKey(word))
                {
                    _wordDatabase[word] = new WordData
                    {
                        Word = word,
                        Frequency = 0,
                        SentenceContexts = new List<string>(),
                        CooccurringWords = new Dictionary<string, int>()
                    };
                }

                _wordDatabase[word].Frequency++;
                _wordDatabase[word].SentenceContexts.Add(sentence);
            }

            // Build co-occurrence matrix for words in same sentence
            for (int i = 0; i < words.Length; i++)
            {
                for (int j = i + 1; j < words.Length; j++)
                {
                    var word1 = words[i];
                    var word2 = words[j];

                    // Update co-occurrence for word1 -> word2
                    if (!_wordCooccurrences.ContainsKey(word1))
                    {
                        _wordCooccurrences[word1] = new Dictionary<string, int>();
                    }
                    if (!_wordCooccurrences[word1].ContainsKey(word2))
                    {
                        _wordCooccurrences[word1][word2] = 0;
                    }
                    _wordCooccurrences[word1][word2]++;

                    // Update co-occurrence for word2 -> word1
                    if (!_wordCooccurrences.ContainsKey(word2))
                    {
                        _wordCooccurrences[word2] = new Dictionary<string, int>();
                    }
                    if (!_wordCooccurrences[word2].ContainsKey(word1))
                    {
                        _wordCooccurrences[word2][word1] = 0;
                    }
                    _wordCooccurrences[word2][word1]++;

                    // Update word data
                    if (!_wordDatabase[word1].CooccurringWords.ContainsKey(word2))
                    {
                        _wordDatabase[word1].CooccurringWords[word2] = 0;
                    }
                    _wordDatabase[word1].CooccurringWords[word2]++;

                    if (!_wordDatabase[word2].CooccurringWords.ContainsKey(word1))
                    {
                        _wordDatabase[word2].CooccurringWords[word1] = 0;
                    }
                    _wordDatabase[word2].CooccurringWords[word1]++;
                }
            }
        }

        private string[] TokenizeSentence(string sentence)
        {
            // Simple tokenization - remove punctuation and split on whitespace
            var cleaned = System.Text.RegularExpressions.Regex.Replace(sentence.ToLower(), @"[^\w\s]", "");
            return cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private void BuildCooccurrenceMatrix()
        {
            Console.WriteLine("\n🔗 **BUILDING CO-OCCURRENCE MATRIX**");
            Console.WriteLine($"Words in database: {_wordDatabase.Count}");
            Console.WriteLine($"Co-occurrence pairs: {_wordCooccurrences.Count}");

            // Filter to keep only meaningful co-occurrences (appear together > 1 time)
            var filteredCooccurrences = new Dictionary<string, Dictionary<string, int>>();
            foreach (var kvp in _wordCooccurrences)
            {
                var filtered = kvp.Value.Where(x => x.Value > 1).ToDictionary(x => x.Key, x => x.Value);
                if (filtered.Count > 0)
                {
                    filteredCooccurrences[kvp.Key] = filtered;
                }
            }

            _wordCooccurrences.Clear();
            foreach (var kvp in filteredCooccurrences)
            {
                _wordCooccurrences[kvp.Key] = kvp.Value;
            }

            Console.WriteLine($"Filtered co-occurrence pairs: {_wordCooccurrences.Count}");
        }

        private async Task GenerateLearningPatternsAsync()
        {
            Console.WriteLine("\n🎭 **GENERATING LEARNING PATTERNS**");

            var learningEncoder = new LearningSparseConceptEncoder();

            foreach (var wordData in _wordDatabase.Values)
            {
                // Generate pattern based on actual word data
                var pattern = await GenerateLearnedPatternAsync(wordData, learningEncoder);
                wordData.LearnedPattern = pattern;

                if (_wordDatabase.Count % 100 == 0)
                {
                    Console.WriteLine($"Generated patterns for {_wordDatabase.Count} words...");
                }
            }

            Console.WriteLine($"Generated learning patterns for {_wordDatabase.Count} words");
        }

        private async Task<SparsePattern> GenerateLearnedPatternAsync(WordData wordData, LearningSparseConceptEncoder encoder)
        {
            // Create pattern based on word frequency and co-occurrences
            var patternSize = 2048; // SDR size
            var activeBits = new List<int>();

            // Base pattern from word hash
            var baseHash = wordData.Word.GetHashCode();
            var random = new Random(baseHash);

            // Add bits based on frequency (more frequent words have more stable patterns)
            var frequencyFactor = Math.Min(wordData.Frequency / 10.0, 1.0);
            var baseBits = (int)(patternSize * 0.02 * frequencyFactor); // 2% sparsity, modulated by frequency

            for (int i = 0; i < baseBits; i++)
            {
                activeBits.Add(random.Next(patternSize));
            }

            // Add bits based on co-occurring words
            foreach (var cooccurrence in wordData.CooccurringWords.OrderByDescending(x => x.Value).Take(5))
            {
                var coHash = cooccurrence.Key.GetHashCode();
                var coRandom = new Random(coHash);
                var coBits = Math.Min(cooccurrence.Value / 2, 5); // Up to 5 bits per co-occurring word

                for (int i = 0; i < coBits; i++)
                {
                    activeBits.Add(coRandom.Next(patternSize));
                }
            }

            // Remove duplicates and limit to 2% sparsity
            activeBits = activeBits.Distinct().Take((int)(patternSize * 0.02)).ToList();

            return new SparsePattern(activeBits.ToArray(), 0.02);
        }

        private async Task SaveLearningDataAsync()
        {
            Console.WriteLine("\n💾 **SAVING LEARNING DATA**");

            // Ensure output directory exists
            Directory.CreateDirectory(_outputPath);

            // Save word database
            var wordDbPath = Path.Combine(_outputPath, "word_database.json");
            var wordDbJson = JsonSerializer.Serialize(_wordDatabase, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(wordDbPath, wordDbJson);

            // Save co-occurrence matrix
            var cooccurrencePath = Path.Combine(_outputPath, "cooccurrence_matrix.json");
            var cooccurrenceJson = JsonSerializer.Serialize(_wordCooccurrences, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(cooccurrencePath, cooccurrenceJson);

            // Save learning patterns
            var patternsPath = Path.Combine(_outputPath, "learned_patterns.json");
            var patterns = _wordDatabase.ToDictionary(x => x.Key, x => x.Value.LearnedPattern);
            var patternsJson = JsonSerializer.Serialize(patterns, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(patternsPath, patternsJson);

            Console.WriteLine("✅ Learning data saved to:");
            Console.WriteLine($"   {wordDbPath}");
            Console.WriteLine($"   {cooccurrencePath}");
            Console.WriteLine($"   {patternsPath}");
        }

        public class WordData
        {
            public string Word { get; set; }
            public int Frequency { get; set; }
            public List<string> SentenceContexts { get; set; }
            public Dictionary<string, int> CooccurringWords { get; set; }
            public SparsePattern LearnedPattern { get; set; }
        }
    }
}
