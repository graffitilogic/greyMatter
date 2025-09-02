using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using GreyMatter.Storage;
using GreyMatter.Core;

namespace GreyMatter.Core
{
    /// <summary>
    /// CORRECTED: Learning-based sparse concept encoder that builds patterns from real data
    /// </summary>
    public class LearningSparseConceptEncoder
    {
        private readonly Dictionary<string, LearnedSparsePattern> _learnedPatterns;
        private readonly Dictionary<string, List<string>> _wordRelationships;
        private readonly Dictionary<string, SemanticConcept> _semanticConcepts;
        private readonly Random _random;
        private readonly int _patternSize;
        private readonly double _sparsity;
        private readonly SemanticStorageManager? _storageManager;

        public LearningSparseConceptEncoder(int patternSize = 2048, double sparsity = 0.02)
        {
            _learnedPatterns = new Dictionary<string, LearnedSparsePattern>();
            _wordRelationships = new Dictionary<string, List<string>>();
            _semanticConcepts = new Dictionary<string, SemanticConcept>();
            _random = new Random(42);
            _patternSize = patternSize;
            _sparsity = sparsity;
            _storageManager = null;
        }

        public LearningSparseConceptEncoder(SemanticStorageManager storageManager, int patternSize = 2048, double sparsity = 0.02)
        {
            _learnedPatterns = new Dictionary<string, LearnedSparsePattern>();
            _wordRelationships = new Dictionary<string, List<string>>();
            _semanticConcepts = new Dictionary<string, SemanticConcept>();
            _random = new Random(42);
            _patternSize = patternSize;
            _sparsity = sparsity;
            _storageManager = storageManager;
            
            // Load learned patterns from storage on initialization
            Task.Run(async () => await LoadLearnedPatternsFromStorageAsync()).Wait();
        }

        /// <summary>
        /// Learn patterns from real training data
        /// </summary>
        public async Task LearnFromDataAsync(List<string> sentences)
        {
            Console.WriteLine("🧠 Learning sparse patterns from real data...");

            // Extract vocabulary and relationships
            var vocabulary = ExtractVocabulary(sentences);
            var relationships = ExtractRelationships(sentences, vocabulary);

            Console.WriteLine($"   📚 Found {vocabulary.Count} words, {relationships.Count} relationship groups");

            // Build semantic concepts
            var concepts = await BuildSemanticConceptsAsync(vocabulary, relationships);

            // Learn sparse patterns for each concept
            foreach (var concept in concepts)
            {
                await LearnConceptPatternAsync(concept);
            }

            Console.WriteLine($"   ✅ Learned patterns for {concepts.Count} semantic concepts");
        }

        /// <summary>
        /// Encode word using learned patterns
        /// </summary>
        public async Task<SparsePattern> EncodeLearnedWordAsync(string word, string context = "")
        {
            var normalizedWord = word.ToLower().Trim();

            // Try to find learned pattern
            if (_learnedPatterns.TryGetValue(normalizedWord, out var learnedPattern))
            {
                // Adapt pattern based on context
                return await AdaptPatternToContextAsync(learnedPattern, context);
            }

            // Fallback to algorithmic generation for unknown words
            Console.WriteLine($"   ⚠️ Unknown word '{word}', using algorithmic fallback");
            return GenerateAlgorithmicPattern(normalizedWord, context);
        }

        /// <summary>
        /// Extract vocabulary from sentences
        /// </summary>
        private HashSet<string> ExtractVocabulary(List<string> sentences)
        {
            var vocabulary = new HashSet<string>();

            foreach (var sentence in sentences)
            {
                var words = TokenizeSentence(sentence);
                foreach (var word in words)
                {
                    if (word.Length > 2) // Filter very short words
                    {
                        vocabulary.Add(word.ToLower());
                    }
                }
            }

            return vocabulary;
        }

        /// <summary>
        /// Extract word relationships from co-occurrences
        /// </summary>
        private Dictionary<string, List<string>> ExtractRelationships(List<string> sentences, HashSet<string> vocabulary)
        {
            var coOccurrences = new Dictionary<string, Dictionary<string, int>>();

            foreach (var sentence in sentences)
            {
                var words = TokenizeSentence(sentence).Select(w => w.ToLower()).ToList();

                // Count co-occurrences within sentence windows
                for (int i = 0; i < words.Count; i++)
                {
                    if (!vocabulary.Contains(words[i])) continue;

                    for (int j = i + 1; j < Math.Min(i + 5, words.Count); j++) // 5-word window
                    {
                        if (!vocabulary.Contains(words[j])) continue;

                        var word1 = words[i];
                        var word2 = words[j];

                        if (!coOccurrences.ContainsKey(word1))
                            coOccurrences[word1] = new Dictionary<string, int>();
                        if (!coOccurrences[word1].ContainsKey(word2))
                            coOccurrences[word1][word2] = 0;

                        coOccurrences[word1][word2]++;
                    }
                }
            }

            // Convert to relationship lists
            var relationships = new Dictionary<string, List<string>>();
            foreach (var word1 in vocabulary)
            {
                if (coOccurrences.ContainsKey(word1))
                {
                    var relatedWords = coOccurrences[word1]
                        .Where(kvp => kvp.Value >= 2) // At least 2 co-occurrences
                        .OrderByDescending(kvp => kvp.Value)
                        .Take(10)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    if (relatedWords.Any())
                    {
                        relationships[word1] = relatedWords;
                        _wordRelationships[word1] = relatedWords;
                    }
                }
            }

            return relationships;
        }

        /// <summary>
        /// Build semantic concepts from vocabulary and relationships
        /// </summary>
        private async Task<List<SemanticConcept>> BuildSemanticConceptsAsync(
            HashSet<string> vocabulary, Dictionary<string, List<string>> relationships)
        {
            var concepts = new List<SemanticConcept>();
            var processedWords = new HashSet<string>();
            var conceptId = 0;

            foreach (var seedWord in vocabulary.OrderBy(w => w))
            {
                if (processedWords.Contains(seedWord)) continue;

                var conceptWords = new HashSet<string> { seedWord };
                var toProcess = new Queue<string>();
                toProcess.Enqueue(seedWord);

                // Expand concept through relationships
                while (toProcess.Any() && conceptWords.Count < 15)
                {
                    var currentWord = toProcess.Dequeue();
                    if (relationships.ContainsKey(currentWord))
                    {
                        foreach (var relatedWord in relationships[currentWord])
                        {
                            if (!conceptWords.Contains(relatedWord) && conceptWords.Count < 15)
                            {
                                conceptWords.Add(relatedWord);
                                toProcess.Enqueue(relatedWord);
                            }
                        }
                    }
                }

                if (conceptWords.Count >= 3)
                {
                    var concept = new SemanticConcept
                    {
                        Id = $"learned_concept_{conceptId++}",
                        Words = conceptWords.ToList(),
                        Type = InferConceptType(conceptWords),
                        CreatedAt = DateTime.UtcNow,
                        IsLearned = true
                    };

                    concepts.Add(concept);
                    foreach (var word in conceptWords)
                    {
                        _semanticConcepts[word] = concept;
                    }
                }

                foreach (var word in conceptWords)
                {
                    processedWords.Add(word);
                }
            }

            return concepts;
        }

        /// <summary>
        /// Learn sparse pattern for a semantic concept
        /// </summary>
        private async Task LearnConceptPatternAsync(SemanticConcept concept)
        {
            // Create base pattern from concept words
            var basePattern = new bool[_patternSize];
            var activeBits = (int)(_patternSize * _sparsity);

            // Use concept words to seed pattern generation
            var wordsForHash = concept.Words ?? new List<string>();
            var conceptHash = string.Join(",", wordsForHash.OrderBy(w => w));
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(conceptHash));

            // Distribute active bits based on concept hash
            var indices = new List<int>();
            for (int i = 0; i < _patternSize; i++)
            {
                indices.Add(i);
            }

            // Shuffle based on hash
            var rng = new Random(BitConverter.ToInt32(hashBytes, 0));
            indices = indices.OrderBy(x => rng.Next()).ToList();

            // Activate bits
            for (int i = 0; i < activeBits; i++)
            {
                basePattern[indices[i]] = true;
            }

            // Store learned pattern for each word in concept
            var learnedPattern = new LearnedSparsePattern
            {
                ConceptId = concept.Id,
                BasePattern = basePattern,
                ConceptType = concept.Type,
                LearnedFrom = concept.Words,
                Confidence = 1.0
            };

            // Store learned pattern for each word in concept
            var conceptWords = concept.Words ?? new List<string>();
            foreach (var word in conceptWords)
            {
                _learnedPatterns[word] = learnedPattern;
            }
        }

        /// <summary>
        /// Adapt learned pattern to specific context
        /// </summary>
        private async Task<SparsePattern> AdaptPatternToContextAsync(LearnedSparsePattern learnedPattern, string context)
        {
            if (learnedPattern.BasePattern == null)
            {
                return new SparsePattern(Array.Empty<int>(), 1.0);
            }

            if (string.IsNullOrEmpty(context))
            {
                return new SparsePattern(GetActiveIndices(learnedPattern.BasePattern).ToArray(), 1.0);
            }

            // Modify pattern based on context
            var contextWords = TokenizeSentence(context);
            var adaptedPattern = (bool[])learnedPattern.BasePattern.Clone();

            // Add context influence
            var contextHash = string.Join(",", contextWords.OrderBy(w => w.ToLower()));
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(contextHash));
            var rng = new Random(BitConverter.ToInt32(hashBytes, 0));

            // Modify 10% of bits based on context
            var bitsToModify = (int)(_patternSize * 0.1);
            for (int i = 0; i < bitsToModify; i++)
            {
                var index = rng.Next(_patternSize);
                adaptedPattern[index] = !adaptedPattern[index]; // Flip bit
            }

            return new SparsePattern(GetActiveIndices(adaptedPattern).ToArray(), 1.0);
        }

        /// <summary>
        /// Generate algorithmic pattern for unknown words
        /// </summary>
        private SparsePattern GenerateAlgorithmicPattern(string word, string context)
        {
            var pattern = new bool[_patternSize];
            var activeBits = (int)(_patternSize * _sparsity);

            // Use word hash to determine active bits
            var wordBytes = Encoding.UTF8.GetBytes(word);
            var hashBytes = SHA256.HashData(wordBytes);

            for (int i = 0; i < activeBits; i++)
            {
                var index = BitConverter.ToInt32(hashBytes, (i * 4) % (hashBytes.Length - 3));
                index = Math.Abs(index) % _patternSize;
                pattern[index] = true;
            }

            return new SparsePattern(GetActiveIndices(pattern).ToArray(), 1.0);
        }

        /// <summary>
        /// Get active bit indices from boolean array
        /// </summary>
        private List<int> GetActiveIndices(bool[] pattern)
        {
            var activeBits = new List<int>();
            for (int i = 0; i < pattern.Length; i++)
            {
                if (pattern[i]) activeBits.Add(i);
            }
            return activeBits;
        }

        /// <summary>
        /// Tokenize sentence into words
        /// </summary>
        private List<string> TokenizeSentence(string sentence)
        {
            return sentence.ToLower()
                          .Split(new[] { ' ', '.', ',', '!', '?', ';', ':', '-', '(', ')' },
                                StringSplitOptions.RemoveEmptyEntries)
                          .Where(word => word.Length > 2)
                          .Select(word => word.Trim())
                          .ToList();
        }

        /// <summary>
        /// Infer concept type from words
        /// </summary>
        private string InferConceptType(HashSet<string> words)
        {
            var sampleWords = words.Take(5).ToList();

            if (sampleWords.Any(w => IsAnimalWord(w))) return "animals";
            if (sampleWords.Any(w => IsVehicleWord(w))) return "vehicles";
            if (sampleWords.Any(w => IsFoodWord(w))) return "food";
            if (sampleWords.Any(w => IsActionWord(w))) return "actions";
            if (sampleWords.Any(w => IsColorWord(w))) return "colors";

            return "general";
        }

        // Simple word classification helpers
        private bool IsAnimalWord(string word) =>
            new[] { "cat", "dog", "bird", "fish", "horse", "cow", "sheep", "pig" }.Contains(word);
        private bool IsVehicleWord(string word) =>
            new[] { "car", "truck", "bike", "bus", "train", "plane" }.Contains(word);
        private bool IsFoodWord(string word) =>
            new[] { "apple", "banana", "bread", "cheese", "meat", "rice" }.Contains(word);
        private bool IsActionWord(string word) =>
            new[] { "run", "walk", "eat", "sleep", "play", "work" }.Contains(word);
        private bool IsColorWord(string word) =>
            new[] { "red", "blue", "green", "yellow", "black", "white" }.Contains(word);

        /// <summary>
        /// Load learned patterns from storage
        /// </summary>
        public async Task LoadLearnedPatternsFromStorageAsync()
        {
            if (_storageManager == null) return;

            try
            {
                // Load learned patterns from storage
                var loadedPatterns = await _storageManager.RetrieveNeuralConceptsAsync();

                if (loadedPatterns != null)
                {
                    int loadedCount = 0;
                    foreach (var kvp in loadedPatterns)
                    {
                        if (kvp.Key.StartsWith("learned_pattern_"))
                        {
                            var word = kvp.Key.Replace("learned_pattern_", "");
                            var patternData = kvp.Value;

                            if (patternData != null)
                            {
                                try
                                {
                                    // Handle JsonElement properly
                                    if (patternData is System.Text.Json.JsonElement jsonElement)
                                    {
                                        if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                                        {
                                            var learnedPattern = new LearnedSparsePattern
                                            {
                                                ConceptId = jsonElement.TryGetProperty("Word", out var wordProp) ? wordProp.GetString() : word,
                                                BasePattern = jsonElement.TryGetProperty("Pattern", out var patternProp) ? 
                                                    patternProp.EnumerateArray().Select(x => x.GetBoolean()).ToArray() : null,
                                                LearnedFrom = jsonElement.TryGetProperty("LearnedFrom", out var learnedFromProp) ? 
                                                    learnedFromProp.EnumerateArray().Select(x => x.GetString()).Where(s => s != null).ToList()! : new List<string>(),
                                                ConceptType = jsonElement.TryGetProperty("ConceptType", out var typeProp) ? typeProp.GetString() : "Word",
                                                Confidence = 1.0
                                            };

                                            _learnedPatterns[word] = learnedPattern;
                                            loadedCount++;
                                        }
                                    }
                                    else
                                    {
                                        // Handle as dynamic object
                                        var dynamicData = patternData as dynamic;
                                        if (dynamicData != null)
                                        {
                                            var learnedPattern = new LearnedSparsePattern
                                            {
                                                ConceptId = dynamicData.Word?.ToString(),
                                                BasePattern = dynamicData.Pattern,
                                                LearnedFrom = dynamicData.LearnedFrom,
                                                ConceptType = dynamicData.ConceptType?.ToString(),
                                                Confidence = 1.0
                                            };

                                            _learnedPatterns[word] = learnedPattern;
                                            loadedCount++;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"   ⚠️ Failed to parse pattern for word '{word}': {ex.Message}");
                                }
                            }
                        }
                    }

                    if (loadedCount > 0)
                    {
                        Console.WriteLine($"   ✅ Loaded {loadedCount} learned patterns from storage");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️ Failed to load learned patterns from storage: {ex.Message}");
            }
        }

        /// <summary>
        /// Generate a learned pattern for a word
        /// </summary>
        private LearnedSparsePattern GenerateLearnedPatternForWord(string word)
        {
            // Use word to create a deterministic learned pattern
            var wordHash = word.ToLower();
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(wordHash));

            var pattern = new bool[_patternSize];
            var activeBits = (int)(_patternSize * _sparsity);

            // Distribute active bits based on word hash
            for (int i = 0; i < activeBits; i++)
            {
                var index = BitConverter.ToInt32(hashBytes, (i * 4) % (hashBytes.Length - 3));
                index = Math.Abs(index) % _patternSize;
                pattern[index] = true;
            }

            return new LearnedSparsePattern
            {
                ConceptId = $"learned_{word}",
                BasePattern = pattern,
                ConceptType = "learned",
                LearnedFrom = new List<string> { word },
                Confidence = 1.0
            };
        }

        /// <summary>
        /// Save learned patterns to storage
        /// </summary>
        public async Task SaveLearnedPatternsToStorageAsync()
        {
            if (_storageManager == null) return;

            try
            {
                // Save learned patterns to storage
                var patternsToSave = new Dictionary<string, object>();
                
                foreach (var kvp in _learnedPatterns)
                {
                    patternsToSave[$"learned_pattern_{kvp.Key}"] = new
                    {
                        Word = kvp.Key,
                        Pattern = kvp.Value.BasePattern,
                        Sparsity = _sparsity,
                        LearnedFrom = kvp.Value.LearnedFrom,
                        ConceptType = kvp.Value.ConceptType
                    };
                }

                if (patternsToSave.Any())
                {
                    await _storageManager.StoreNeuralConceptsAsync(patternsToSave);
                    Console.WriteLine($"   ✅ Saved {_learnedPatterns.Count} learned patterns to storage");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️ Failed to save learned patterns to storage: {ex.Message}");
            }
        }
    }

    public class LearnedSparsePattern
    {
        public string? ConceptId { get; set; }
        public bool[]? BasePattern { get; set; }
        public string? ConceptType { get; set; }
        public List<string>? LearnedFrom { get; set; }
        public double Confidence { get; set; }
    }

    public class SemanticConcept
    {
        public string? Id { get; set; }
        public List<string>? Words { get; set; }
        public string? Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsLearned { get; set; }
    }
}
