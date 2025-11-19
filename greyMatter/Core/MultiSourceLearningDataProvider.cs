using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using GreyMatter.Core;
using GreyMatter.DataIntegration;

namespace GreyMatter.Core
{
    /// <summary>
    /// Learning quality report containing session statistics and word occurrence data
    /// </summary>
    public class LearningQualityReport
    {
        public TimeSpan SessionDuration { get; set; }
        public int TotalWordsProcessed { get; set; }
        public int UniqueWordsLearned { get; set; }
        public Dictionary<string, int> TopWordsByOccurrence { get; set; } = new Dictionary<string, int>();
        public double AverageOccurrencePerWord { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// MultiSourceLearningDataProvider: Bridges the gap between multi-source data capabilities
    /// and actual learning implementation. Provides continuous streams of learning content
    /// from vast NAS datasets, LLM teacher, and various data sources.
    /// 
    /// This replaces the hardcoded TatoebaDataConverter limitations with truly scalable learning.
    /// </summary>
    public class MultiSourceLearningDataProvider : IDisposable
    {
        private readonly LLMTeacher _llmTeacher;
        private readonly string _nasPath;
        private readonly Random _random = new();
        
        // Thread-safe word occurrence tracking for quality monitoring
        private readonly ConcurrentDictionary<string, int> _learnedWordOccurrences = new();
        private readonly object _sessionStatsLock = new object();
        private int _totalWordsProcessed = 0;
        private DateTime _sessionStartTime = DateTime.Now;
        
        // Learning chunk management
        private readonly Queue<LearningChunk> _pendingChunks = new();
        private readonly Dictionary<string, DataSourceStats> _sourceStats = new();
        private int _currentChunkIndex = 0;
        private const int CHUNK_SIZE = 1000; // Words per chunk
        private const int MAX_PENDING_CHUNKS = 10;
        
        // Multi-source content streams
        private readonly Dictionary<DataSourceType, bool> _enabledSources = new();
        private DateTime _lastLLMGeneration = DateTime.MinValue;
        private readonly TimeSpan _llmGenerationInterval = TimeSpan.FromMinutes(30);

        public MultiSourceLearningDataProvider(string nasPath = "/Volumes/jarvis/trainData")
        {
            _nasPath = nasPath;
            // Note: EnhancedDataIntegrator requires RealLanguageLearner parameter
            // For now, we'll create a simplified provider that generates content directly
            _llmTeacher = new LLMTeacher();
            
            // Initialize enabled data sources
            InitializeDataSources();
            
            Console.WriteLine("🔗 **MULTI-SOURCE LEARNING DATA PROVIDER INITIALIZED**");
            Console.WriteLine($"📁 NAS Path: {nasPath}");
            Console.WriteLine($"🎯 Chunk Size: {CHUNK_SIZE} words");
            Console.WriteLine($"📊 Enabled Sources: {_enabledSources.Count(kvp => kvp.Value)}");
        }

        /// <summary>
        /// Get the next learning chunk with continuous content from all available sources
        /// This method ensures we never run out of learning material
        /// </summary>
        public async Task<LearningChunk> GetNextLearningChunkAsync()
        {
            // Ensure we have pending chunks available
            await EnsurePendingChunks();
            
            if (_pendingChunks.Count == 0)
            {
                // Emergency fallback - generate LLM content immediately
                Console.WriteLine("⚠️  No pending chunks available, generating emergency LLM content...");
                await GenerateLLMContent(force: true);
            }
            
            var chunk = _pendingChunks.Dequeue();
            _currentChunkIndex++;
            
            // Update statistics
            UpdateSourceStats(chunk.SourceType);
            
            Console.WriteLine($"📦 Delivered chunk #{_currentChunkIndex} from {chunk.SourceType}");
            Console.WriteLine($"   📚 Words: {chunk.Words.Count}");
            Console.WriteLine($"   🔗 Contexts: {chunk.SentenceContexts.Count}");
            Console.WriteLine($"   📈 Difficulty: {chunk.AverageDifficulty:F2}");
            
            return chunk;
        }

        /// <summary>
        /// Get learning statistics across all sources
        /// </summary>
        public LearningDataStats GetOverallStats()
        {
            var totalWords = _sourceStats.Values.Sum(s => s.WordsProcessed);
            var totalChunks = _sourceStats.Values.Sum(s => s.ChunksGenerated);
            
            return new LearningDataStats
            {
                TotalWordsProcessed = totalWords,
                TotalChunksGenerated = totalChunks,
                ActiveSources = _enabledSources.Count(kvp => kvp.Value),
                CurrentChunkIndex = _currentChunkIndex,
                PendingChunks = _pendingChunks.Count,
                SourceBreakdown = _sourceStats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.WordsProcessed),
                AverageWordsPerChunk = totalChunks > 0 ? (double)totalWords / totalChunks : 0
            };
        }

        /// <summary>
        /// Generate a learning curriculum dynamically from LLM teacher
        /// </summary>
        public async Task<List<LearningChunk>> GenerateDynamicCurriculumAsync(string topic, int chunkCount = 5)
        {
            Console.WriteLine($"🎓 Generating dynamic curriculum for topic: {topic}");
            
            var learningContext = new LearningContext
            {
                VocabularySize = 5000,
                RecentWords = new List<string>(),
                Sources = _enabledSources.Where(kvp => kvp.Value).Select(kvp => kvp.Key.ToString()).ToList(),
                PerformanceMetrics = $"Learning focus: {topic}"
            };
            
            var curriculumTopics = await _llmTeacher.GenerateLearningCurriculumAsync(learningContext, chunkCount);
            
            var chunks = new List<LearningChunk>();
            
            foreach (var topicName in curriculumTopics.Take(chunkCount))
            {
                var contentRequest = new ContentRequest
                {
                    Topic = topicName,
                    TargetAudience = "intermediate learner",
                    DifficultyLevel = "intermediate",
                    LearningObjectives = new List<string> { $"Learn about {topicName}", "Understand key concepts", "Apply knowledge" },
                    ContentLength = CHUNK_SIZE
                };
                
                var educationalContent = await _llmTeacher.GenerateEducationalContentAsync(contentRequest);
                
                var chunk = ConvertEducationalContentToChunk(educationalContent, DataSourceType.LLMGenerated);
                chunks.Add(chunk);
            }
            
            Console.WriteLine($" Generated {chunks.Count} curriculum chunks for '{topic}'");
            return chunks;
        }

        #region Private Methods

        private void InitializeDataSources()
        {
            // Enable all available data sources by default
            foreach (DataSourceType sourceType in Enum.GetValues<DataSourceType>())
            {
                _enabledSources[sourceType] = true;
            }
            
            // Initialize statistics
            foreach (var sourceType in _enabledSources.Keys)
            {
                _sourceStats[sourceType.ToString()] = new DataSourceStats
                {
                    SourceType = sourceType.ToString(),
                    WordsProcessed = 0,
                    ChunksGenerated = 0,
                    LastAccessed = DateTime.MinValue
                };
            }
        }

        private async Task EnsurePendingChunks()
        {
            while (_pendingChunks.Count < MAX_PENDING_CHUNKS)
            {
                await GenerateNextChunk();
            }
        }

        private async Task GenerateNextChunk()
        {
            // Rotate through data sources for variety
            var enabledSources = _enabledSources.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
            if (enabledSources.Count == 0)
            {
                throw new InvalidOperationException("No data sources enabled");
            }
            
            var sourceIndex = _currentChunkIndex % enabledSources.Count;
            var selectedSource = enabledSources[sourceIndex];
            
            LearningChunk chunk = selectedSource switch
            {
                DataSourceType.SimpleWiki => await GenerateSimpleWikiChunk(),
                DataSourceType.NewsHeadlines => await GenerateNewsChunk(),
                DataSourceType.ScientificAbstracts => await GenerateScientificChunk(),
                DataSourceType.TechnicalDocs => await GenerateTechnicalChunk(),
                DataSourceType.OpenSubtitles => await GenerateSubtitlesChunk(),
                DataSourceType.SocialMedia => await GenerateSocialMediaChunk(),
                DataSourceType.LLMGenerated => await GenerateLLMChunk(),
                _ => await GenerateFallbackChunk()
            };
            
            _pendingChunks.Enqueue(chunk);
        }

        private async Task<LearningChunk> GenerateSimpleWikiChunk()
        {
            // Try to read actual SimpleWiki content from NAS
            var simpleWikiPath = Path.Combine(_nasPath, "SimpleWiki", "simplewiki-latest-pages-articles-multistream.xml");
            if (File.Exists(simpleWikiPath))
            {
                return await ReadWikiContentFromFileAsync(simpleWikiPath, DataSourceType.SimpleWiki);
            }
            
            // Fallback to CBT data if SimpleWiki not available
            var cbtPath = Path.Combine(_nasPath, "CBT", "CBTest", "data", "cbt_train.txt");
            if (File.Exists(cbtPath))
            {
                return await ReadTextContentFromFileAsync(cbtPath, DataSourceType.SimpleWiki, 0.4);
            }
            
            // No static arrays - return fallback only
            return await GenerateFallbackChunk();
        }

        private async Task<LearningChunk> GenerateNewsChunk()
        {
            // Try to read from news-specific data sources
            var newsPath = Path.Combine(_nasPath, "news", "headlines.txt");
            if (File.Exists(newsPath))
            {
                return await ReadTextContentFromFileAsync(newsPath, DataSourceType.NewsHeadlines, 0.5);
            }
            
            // Try enhanced learning data as fallback
            var enhancedPath = Path.Combine(_nasPath, "enhanced_learning_data", "enhanced_word_database.json");
            if (File.Exists(enhancedPath))
            {
                return await ReadJsonContentFromFileAsync(enhancedPath, DataSourceType.NewsHeadlines, 0.5);
            }
            
            // Try CBT data as news alternative
            var cbtPath = Path.Combine(_nasPath, "CBT", "CBTest", "data", "cbt_train.txt");
            if (File.Exists(cbtPath))
            {
                return await ReadTextContentFromFileAsync(cbtPath, DataSourceType.NewsHeadlines, 0.5);
            }
            
            // No data files available - throw exception
            throw new InvalidOperationException($"No news headlines data found. Expected files: {newsPath}, {enhancedPath}, or {cbtPath}");
        }

        private async Task<LearningChunk> GenerateScientificChunk()
        {
            // Try scientific-specific data sources
            var sciencePath = Path.Combine(_nasPath, "scientific", "abstracts.txt");
            if (File.Exists(sciencePath))
            {
                return await ReadTextContentFromFileAsync(sciencePath, DataSourceType.ScientificAbstracts, 0.7);
            }
            
            // Try reading from enhanced data
            var enhancedPath = Path.Combine(_nasPath, "enhanced_learning_data", "enhanced_word_database.json");
            if (File.Exists(enhancedPath))
            {
                return await ReadJsonContentFromFileAsync(enhancedPath, DataSourceType.ScientificAbstracts, 0.7);
            }
            
            // Try CBT data for scientific vocabulary
            var cbtPath = Path.Combine(_nasPath, "CBT", "CBTest", "data", "cbt_train.txt");
            if (File.Exists(cbtPath))
            {
                return await ReadTextContentFromFileAsync(cbtPath, DataSourceType.ScientificAbstracts, 0.7);
            }
            
            // No data files available - throw exception
            throw new InvalidOperationException($"No scientific abstracts data found. Expected files: {sciencePath}, {enhancedPath}, or {cbtPath}");
        }

        private async Task<LearningChunk> GenerateTechnicalChunk()
        {
            // Try technical-specific data sources
            var techPath = Path.Combine(_nasPath, "technical", "documentation.txt");
            if (File.Exists(techPath))
            {
                return await ReadTextContentFromFileAsync(techPath, DataSourceType.TechnicalDocs, 0.8);
            }
            
            // Try enhanced data first
            var enhancedPath = Path.Combine(_nasPath, "enhanced_learning_data", "enhanced_word_database.json");
            if (File.Exists(enhancedPath))
            {
                return await ReadJsonContentFromFileAsync(enhancedPath, DataSourceType.TechnicalDocs, 0.8);
            }
            
            // Try CBT data for technical vocabulary
            var cbtPath = Path.Combine(_nasPath, "CBT", "CBTest", "data", "cbt_train.txt");
            if (File.Exists(cbtPath))
            {
                return await ReadTextContentFromFileAsync(cbtPath, DataSourceType.TechnicalDocs, 0.8);
            }
            
            // No data files available - throw exception
            throw new InvalidOperationException($"No technical documentation data found. Expected files: {techPath}, {enhancedPath}, or {cbtPath}");
        }

        private async Task<LearningChunk> GenerateSubtitlesChunk()
        {
            // Try to read from learning data sentences
            var sentencesPath = Path.Combine(_nasPath, "learning_data", "sentences.csv");
            if (File.Exists(sentencesPath))
            {
                return await ReadTextContentFromFileAsync(sentencesPath, DataSourceType.OpenSubtitles, 0.3);
            }
            
            // Try CBT data for conversational language
            var cbtPath = Path.Combine(_nasPath, "CBT", "CBTest", "data", "cbt_train.txt");
            if (File.Exists(cbtPath))
            {
                return await ReadTextContentFromFileAsync(cbtPath, DataSourceType.OpenSubtitles, 0.3);
            }
            
            // No static arrays - return fallback
            return await GenerateFallbackChunk();
        }

        private async Task<LearningChunk> GenerateSocialMediaChunk()
        {
            // Try to read from learning data sentences (contains social language)
            var sentencesPath = Path.Combine(_nasPath, "learning_data", "sentences.csv");
            if (File.Exists(sentencesPath))
            {
                return await ReadTextContentFromFileAsync(sentencesPath, DataSourceType.SocialMedia, 0.4);
            }
            
            // Try enhanced learning data as alternative
            var enhancedPath = Path.Combine(_nasPath, "enhanced_learning_data", "enhanced_word_database.json");
            if (File.Exists(enhancedPath))
            {
                return await ReadJsonContentFromFileAsync(enhancedPath, DataSourceType.SocialMedia, 0.4);
            }
            
            // Use CBT data as fallback instead of static arrays
            var cbtPath = Path.Combine(_nasPath, "CBT", "CBTest", "data", "cbt_train.txt");
            if (File.Exists(cbtPath))
            {
                return await ReadTextContentFromFileAsync(cbtPath, DataSourceType.SocialMedia, 0.4);
            }
            
            // Last resort - use basic fallback
            return await GenerateFallbackChunk();
        }

        private async Task<LearningChunk> GenerateLLMChunk()
        {
            await GenerateLLMContent();
            return _pendingChunks.LastOrDefault() ?? await GenerateFallbackChunk();
        }

        private async Task GenerateLLMContent(bool force = false)
        {
            if (!force && DateTime.UtcNow - _lastLLMGeneration < _llmGenerationInterval)
            {
                return; // Too soon for next LLM generation
            }
            
            var topics = new[]
            {
                "science and technology",
                "literature and language",
                "history and culture",
                "mathematics and logic",
                "philosophy and ethics",
                "art and creativity"
            };
            
            var randomTopic = topics[_random.Next(topics.Length)];
            
            var contentRequest = new ContentRequest
            {
                Topic = randomTopic,
                TargetAudience = "advanced learner",
                DifficultyLevel = "intermediate",
                LearningObjectives = new List<string> { "vocabulary expansion", "concept understanding", "critical thinking" },
                ContentLength = CHUNK_SIZE
            };
            
            var educationalContent = await _llmTeacher.GenerateEducationalContentAsync(contentRequest);
            var chunk = ConvertEducationalContentToChunk(educationalContent, DataSourceType.LLMGenerated);
            
            _pendingChunks.Enqueue(chunk);
            _lastLLMGeneration = DateTime.UtcNow;
            
            Console.WriteLine($"🤖 Generated LLM chunk on '{randomTopic}' ({chunk.Words.Count} words)");
        }

        private async Task<LearningChunk> GenerateFallbackChunk()
        {
            // Try to read from ANY available data file before using emergency static words
            var possiblePaths = new[]
            {
                Path.Combine(_nasPath, "learning_data", "sentences.csv"),
                Path.Combine(_nasPath, "enhanced_learning_data", "enhanced_word_database.json"),
                Path.Combine(_nasPath, "CBT", "CBTest", "data", "cbt_train.txt"),
                Path.Combine(_nasPath, "CBT", "CBTest", "data", "cbt_valid.txt")
            };
            
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    if (path.EndsWith(".json"))
                    {
                        return await ReadJsonContentFromFileAsync(path, DataSourceType.Fallback, 0.5);
                    }
                    else
                    {
                        return await ReadTextContentFromFileAsync(path, DataSourceType.Fallback, 0.5);
                    }
                }
            }
            
            // No files found - throw exception instead of generating static content
            throw new InvalidOperationException("No learning data files found at any of the expected paths. Please ensure training data is available.");
        }

        private async Task<LearningChunk> ReadJsonContentFromFileAsync(string filePath, DataSourceType sourceType, double baseDifficulty)
        {
            try
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                
                // Try to parse as word database format
                if (jsonContent.Contains("words") || jsonContent.Contains("vocabulary"))
                {
                    // Extract words from JSON - this is a simplified approach
                    // In practice, you'd want proper JSON parsing based on the actual structure
                    var words = new HashSet<string>();
                    var lines = jsonContent.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                          .Where(line => line.Contains("\"") && !line.Contains("metadata"))
                                          .Take(100);
                    
                    foreach (var line in lines)
                    {
                        var matches = System.Text.RegularExpressions.Regex.Matches(line, @"""([a-zA-Z]+)""");
                        foreach (System.Text.RegularExpressions.Match match in matches)
                        {
                            var word = match.Groups[1].Value.ToLowerInvariant();
                            
                            // Skip JSON property names and metadata
                            if (IsJsonPropertyName(word))
                                continue;
                            
                            if (word.Length > 2 && word.Length < 20 && IsValidWord(word))
                            {
                                words.Add(word);
                                if (words.Count >= 1000) break;
                            }
                        }
                        if (words.Count >= 1000) break;
                    }
                    
                    var selectedWords = words.OrderBy(x => _random.Next()).Take(15).ToArray();
                    return CreateChunkFromWords(selectedWords, sourceType, baseDifficulty);
                }
                
                // Fallback if JSON parsing fails - return null to indicate failure
                throw new InvalidOperationException($"Failed to parse JSON data from {filePath} - no valid learning content found");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading JSON from {filePath}: {ex.Message}");
                throw new InvalidOperationException($"Failed to read learning data from {filePath}: {ex.Message}");
            }
        }

        private async Task<LearningChunk> ReadTextContentFromFileAsync(string filePath, DataSourceType sourceType, double baseDifficulty)
        {
            try
            {
                // Read file content in chunks to avoid memory issues
                var lines = await Task.Run(() => File.ReadLines(filePath).Take(100).ToArray()); // Read first 100 lines
                var words = new HashSet<string>();
                
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    // Extract words from line, removing common punctuation
                    var lineWords = line.Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':', '"', '(', ')', '[', ']' }, 
                                               StringSplitOptions.RemoveEmptyEntries)
                                      .Where(w => w.Length > 2 && w.Length < 20 && IsValidWord(w))
                                      .Select(w => w.ToLowerInvariant())
                                      .Distinct();
                    
                    foreach (var word in lineWords)
                    {
                        words.Add(word);
                        if (words.Count >= CHUNK_SIZE) break; // Use CHUNK_SIZE constant
                    }
                    if (words.Count >= CHUNK_SIZE) break;
                }
                
                // Select random subset for this chunk - use 15 to match current chunk size
                var selectedWords = words.OrderBy(x => _random.Next()).Take(15).ToArray();
                
                return CreateChunkFromWords(selectedWords, sourceType, baseDifficulty);
            }
            catch (Exception ex)
            {
                // Log error and throw exception instead of generating static content
                Console.WriteLine($"Error reading from {filePath}: {ex.Message}");
                throw new InvalidOperationException($"Failed to read learning data from {filePath}: {ex.Message}");
            }
        }

        private async Task<LearningChunk> ReadWikiContentFromFileAsync(string filePath, DataSourceType sourceType)
        {
            try
            {
                // For XML files, we need special handling
                // For now, treat as text file but we could add XML parsing later
                return await ReadTextContentFromFileAsync(filePath, sourceType, 0.4);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading Wiki content from {filePath}: {ex.Message}");
                throw new InvalidOperationException($"Failed to read Wiki content from {filePath}: {ex.Message}");
            }
        }

        private bool IsJsonPropertyName(string word)
        {
            // Filter out common JSON property names that should not be learned as vocabulary
            var propertyNames = new HashSet<string>
            {
                "word", "frequency", "difficulty", "contexts", "sentencecontexts", "metadata", 
                "sourcetype", "averagedifficulty", "generatedat", "chunkid", "words", "data",
                "id", "name", "value", "type", "source", "content", "text", "title", "url",
                "created", "updated", "version", "count", "length", "size", "format",
                "language", "encoding", "timestamp", "index", "key", "schema", "config"
            };
            return propertyNames.Contains(word);
        }

        private bool IsValidWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return false;
            
            // Length constraints - too short or too long are likely garbage
            if (word.Length < 2 || word.Length > 25) return false;
            
            // Must start and end with a letter (no leading/trailing punctuation)
            if (!char.IsLetter(word[0]) || !char.IsLetter(word[^1])) return false;
            
            // Check for valid characters - only letters, hyphens, apostrophes, and periods (for abbreviations)
            if (!word.All(c => char.IsLetter(c) || c == '-' || c == '\'' || c == '.')) return false;
            
            // Skip words that are mostly non-alphabetic
            double letterRatio = word.Count(char.IsLetter) / (double)word.Length;
            if (letterRatio < 0.7) return false;
            
            // Skip all-caps words (likely acronyms, sound effects, or technical terms)
            if (word.All(c => char.IsUpper(c) || !char.IsLetter(c))) return false;
            
            // Skip words with excessive repetition (like "hahaha", "nooooo")
            if (HasExcessiveRepetition(word)) return false;
            
            // Skip common subtitle garbage patterns
            if (IsSubtitleGarbage(word)) return false;
            
            // Skip words with unusual character patterns (foreign encodings, corrupted text)
            if (HasUnusualCharacterPatterns(word)) return false;
            
            // Skip words that are likely not English (too many consecutive consonants/vowels)
            if (!HasReasonablePhonetics(word)) return false;
            
            return true;
        }
        
        private bool HasExcessiveRepetition(string word)
        {
            // Check for patterns like "hahaha", "nooooo", "yessss"
            for (int i = 0; i < word.Length - 2; i++)
            {
                char currentChar = word[i];
                int consecutiveCount = 1;
                
                for (int j = i + 1; j < word.Length && word[j] == currentChar; j++)
                {
                    consecutiveCount++;
                }
                
                // If any character repeats more than 3 times consecutively, likely garbage
                if (consecutiveCount > 3) return true;
            }
            return false;
        }
        
        private bool IsSubtitleGarbage(string word)
        {
            // Common subtitle artifacts and sound effects
            string[] garbagePatterns = {
                // Sound effects
                "hmm", "uhh", "ahh", "ohh", "eww", "ugh", "heh", "pfft", "tsk", "shh",
                "whoa", "huh", "duh", "meh", "bah", "psh", "tch", "oof", "yep", "nah",
                
                // Technical terms that appear in subtitles
                "sync", "subs", "subtitle", "captioning", "encode", "decode", "fps",
                "mkv", "avi", "mp4", "srt", "ass", "ssa", "vtt",
                
                // Common interjections that don't add learning value
                "um", "uh", "er", "ah", "oh", "eh", "mm", "hm",
                
                // Internet/texting abbreviations
                "lol", "wtf", "omg", "btw", "fyi", "imo", "tbh", "smh", "rip",
                
                // Single letters that shouldn't be learned as words
                "a", "i", "o", "u", "e", "x", "y", "z"
            };
            
            return garbagePatterns.Contains(word.ToLowerInvariant());
        }
        
        private bool HasUnusualCharacterPatterns(string word)
        {
            // Check for patterns that suggest corrupted text or foreign encoding issues
            
            // Too many uppercase letters scattered throughout (not at beginning)
            int uppercaseCount = word.Skip(1).Count(char.IsUpper);
            if (uppercaseCount > word.Length / 3) return true;
            
            // Mixed case in unusual patterns (like "WoRd" or "woRD")
            bool hasUnusualCasing = false;
            for (int i = 1; i < word.Length - 1; i++)
            {
                if (char.IsUpper(word[i]) && char.IsLower(word[i-1]) && char.IsLower(word[i+1]))
                {
                    hasUnusualCasing = true;
                    break;
                }
            }
            if (hasUnusualCasing) return true;
            
            return false;
        }
        
        private bool HasReasonablePhonetics(string word)
        {
            // Basic phonetic validation for English-like words
            string vowels = "aeiouAEIOU";
            string consonants = "bcdfghjklmnpqrstvwxyzBCDFGHJKLMNPQRSTVWXYZ";
            
            int maxConsecutiveConsonants = 0;
            int maxConsecutiveVowels = 0;
            int currentConsonants = 0;
            int currentVowels = 0;
            
            foreach (char c in word)
            {
                if (!char.IsLetter(c)) continue;
                
                if (vowels.Contains(c))
                {
                    currentVowels++;
                    currentConsonants = 0;
                    maxConsecutiveVowels = Math.Max(maxConsecutiveVowels, currentVowels);
                }
                else if (consonants.Contains(c))
                {
                    currentConsonants++;
                    currentVowels = 0;
                    maxConsecutiveConsonants = Math.Max(maxConsecutiveConsonants, currentConsonants);
                }
            }
            
            // English words rarely have more than 4 consecutive consonants or 3 consecutive vowels
            if (maxConsecutiveConsonants > 4 || maxConsecutiveVowels > 3) return false;
            
            // Word should have at least one vowel (with some exceptions for common words)
            bool hasVowel = word.Any(c => vowels.Contains(c));
            if (!hasVowel)
            {
                // Allow some common consonant-only words/abbreviations
                string[] allowedConsonantWords = { "by", "my", "dry", "fly", "try", "cry", "spy", "sky", "why" };
                if (!allowedConsonantWords.Contains(word.ToLowerInvariant()))
                {
                    return false;
                }
            }
            
            return true;
        }

        private LearningChunk CreateChunkFromWords(string[] words, DataSourceType sourceType, double baseDifficulty)
        {
            // Track word occurrences for quality monitoring
            TrackLearnedWords(words);
            
            var chunkWords = words.ToDictionary(
                word => word,
                word => new WordLearningData
                {
                    Word = word,
                    Frequency = _random.Next(1, 10),
                    Difficulty = baseDifficulty + (_random.NextDouble() * 0.3 - 0.15), // ±0.15 variation
                    Contexts = new List<string> { $"This is an example sentence using the word {word} in context." }
                }
            );

            return new LearningChunk
            {
                Words = chunkWords,
                SentenceContexts = words.Select(w => $"Learning about {w} enhances understanding of {sourceType} content.").ToList(),
                SourceType = sourceType,
                AverageDifficulty = baseDifficulty,
                GeneratedAt = DateTime.UtcNow,
                ChunkId = $"{sourceType}_{DateTime.UtcNow.Ticks % 10000:X4}",
                Metadata = new Dictionary<string, string>
                {
                    ["source"] = sourceType.ToString(),
                    ["word_count"] = words.Length.ToString(),
                    ["generation_method"] = "simplified_simulation"
                }
            };
        }

        private LearningChunk ConvertEducationalContentToChunk(EducationalContentResponse content, DataSourceType sourceType)
        {
            var words = content.KeyVocabulary.Union(content.CoreConcepts)
                .ToDictionary(
                    word => word,
                    word => new WordLearningData
                    {
                        Word = word,
                        Frequency = 1,
                        Difficulty = EstimateDifficultyFromLevel(content.DifficultyLevel),
                        Contexts = new List<string> { content.Content }
                    }
                );
            
            return new LearningChunk
            {
                Words = words,
                SentenceContexts = new List<string> { content.Content },
                SourceType = sourceType,
                AverageDifficulty = EstimateDifficultyFromLevel(content.DifficultyLevel),
                GeneratedAt = DateTime.UtcNow,
                ChunkId = $"llm_{DateTime.UtcNow.Ticks % 10000:X4}",
                Metadata = new Dictionary<string, string>
                {
                    ["topic"] = content.Topic,
                    ["difficulty"] = content.DifficultyLevel,
                    ["objectives"] = string.Join(", ", content.LearningObjectives),
                    ["duration"] = content.EstimatedDuration
                }
            };
        }

        private double EstimateDifficulty(string word, dynamic wordData)
        {
            // Basic difficulty estimation based on word length and frequency
            var lengthFactor = Math.Min(word.Length / 10.0, 1.0);
            var frequencyFactor = 1.0 - Math.Min(wordData.Frequency / 100.0, 0.9);
            return (lengthFactor + frequencyFactor) / 2.0;
        }

        private double EstimateDifficultyFromLevel(string level)
        {
            return level?.ToLower() switch
            {
                "beginner" or "basic" => 0.2,
                "elementary" => 0.3,
                "intermediate" => 0.5,
                "advanced" => 0.7,
                "expert" => 0.9,
                _ => 0.5
            };
        }

        private void UpdateSourceStats(DataSourceType sourceType)
        {
            var key = sourceType.ToString();
            if (_sourceStats.ContainsKey(key))
            {
                _sourceStats[key].ChunksGenerated++;
                _sourceStats[key].WordsProcessed += CHUNK_SIZE;
                _sourceStats[key].LastAccessed = DateTime.UtcNow;
            }
        }

        #endregion

        #region Word Occurrence Tracking for Quality Monitoring

        /// <summary>
        /// Thread-safe method to track learned word occurrences across all learning threads
        /// </summary>
        private void TrackLearnedWords(string[] words)
        {
            foreach (var word in words)
            {
                // Thread-safe increment: if word exists, occurrence++, else add with count 1
                _learnedWordOccurrences.AddOrUpdate(word, 1, (key, oldValue) => oldValue + 1);
            }
            
            // Update session statistics
            lock (_sessionStatsLock)
            {
                _totalWordsProcessed += words.Length;
            }
        }

        /// <summary>
        /// Get learning quality report showing top words by occurrence count
        /// </summary>
        public LearningQualityReport GetLearningQualityReport()
        {
            var sessionDuration = DateTime.Now - _sessionStartTime;
            var topWords = _learnedWordOccurrences
                .OrderByDescending(kvp => kvp.Value)
                .Take(8)
                .ToList();

            return new LearningQualityReport
            {
                SessionDuration = sessionDuration,
                TotalWordsProcessed = _totalWordsProcessed,
                UniqueWordsLearned = _learnedWordOccurrences.Count,
                TopWordsByOccurrence = topWords.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                AverageOccurrencePerWord = _learnedWordOccurrences.Count > 0 
                    ? (double)_totalWordsProcessed / _learnedWordOccurrences.Count 
                    : 0,
                GeneratedAt = DateTime.Now
            };
        }

        /// <summary>
        /// Display learning quality report to console
        /// </summary>
        public void DisplayLearningQualityReport()
        {
            var report = GetLearningQualityReport();
            
            Console.WriteLine();
            Console.WriteLine("📊 **LEARNING QUALITY REPORT**");
            Console.WriteLine("=============================");
            Console.WriteLine($"⏱️  Session Duration: {report.SessionDuration:hh\\:mm\\:ss}");
            Console.WriteLine($"📚 Total Words Processed: {report.TotalWordsProcessed:N0}");
            Console.WriteLine($"🔤 Unique Words Learned: {report.UniqueWordsLearned:N0}");
            Console.WriteLine($"📈 Average Occurrences/Word: {report.AverageOccurrencePerWord:F2}");
            Console.WriteLine();
            Console.WriteLine("🏆 **TOP 8 LEARNED WORDS BY OCCURRENCE:**");
            Console.WriteLine("========================================");
            
            int rank = 1;
            foreach (KeyValuePair<string, int> kvp in report.TopWordsByOccurrence)
            {
                var indicator = GetQualityIndicator(kvp.Key, kvp.Value, report.AverageOccurrencePerWord);
                Console.WriteLine($"   {rank}. {kvp.Key} ({kvp.Value} times) {indicator}");
                rank++;
            }
            
            Console.WriteLine();
            Console.WriteLine("💡 Quality indicators:  Expected, ⚠️  High frequency, 🚨 Potential anomaly");
            Console.WriteLine();
        }

        /// <summary>
        /// Get quality indicator for a word based on its occurrence pattern
        /// </summary>
        private string GetQualityIndicator(string word, int count, double averageOccurrence)
        {
            // Articles, prepositions, conjunctions are expected to have high frequency
            string[] expectedHighFrequency = { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "of", "for", "with", "by" };
            
            if (expectedHighFrequency.Contains(word.ToLowerInvariant()))
            {
                return " (common word)";
            }
            
            // If count is significantly higher than average, flag as potential anomaly
            if (count > averageOccurrence * 3)
            {
                return "🚨 (potential anomaly)";
            }
            
            // If count is moderately high, mark as high frequency
            if (count > averageOccurrence * 1.5)
            {
                return "⚠️  (high frequency)";
            }
            
            return " (normal)";
        }

        /// <summary>
        /// Reset word occurrence tracking for a new learning session
        /// </summary>
        public void ResetLearningQualityTracking()
        {
            _learnedWordOccurrences.Clear();
            lock (_sessionStatsLock)
            {
                _totalWordsProcessed = 0;
                _sessionStartTime = DateTime.Now;
            }
        }

        #endregion

        public void Dispose()
        {
            _llmTeacher?.Dispose();
        }
    }

    #region Supporting Classes

    public class LearningChunk
    {
        public Dictionary<string, WordLearningData> Words { get; set; } = new();
        public List<string> SentenceContexts { get; set; } = new();
        public DataSourceType SourceType { get; set; }
        public double AverageDifficulty { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string ChunkId { get; set; } = "";
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class WordLearningData
    {
        public string Word { get; set; } = "";
        public int Frequency { get; set; }
        public double Difficulty { get; set; }
        public List<string> Contexts { get; set; } = new();
    }

    public class LearningDataStats
    {
        public int TotalWordsProcessed { get; set; }
        public int TotalChunksGenerated { get; set; }
        public int ActiveSources { get; set; }
        public int CurrentChunkIndex { get; set; }
        public int PendingChunks { get; set; }
        public Dictionary<string, int> SourceBreakdown { get; set; } = new();
        public double AverageWordsPerChunk { get; set; }
    }

    public class DataSourceStats
    {
        public string SourceType { get; set; } = "";
        public int WordsProcessed { get; set; }
        public int ChunksGenerated { get; set; }
        public DateTime LastAccessed { get; set; }
    }

    public class LearningContent
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public List<string> KeyConcepts { get; set; } = new();
        public List<string> Vocabulary { get; set; } = new();
        public string DifficultyLevel { get; set; } = "";
        public DateTime Created { get; set; } = DateTime.UtcNow;
    }

    public class DataIntegrationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int ProcessedItems { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public enum DataSourceType
    {
        SimpleWiki,
        NewsHeadlines,
        ScientificAbstracts,
        TechnicalDocs,
        OpenSubtitles,
        SocialMedia,
        LLMGenerated,
        Fallback
    }

    #endregion
}
