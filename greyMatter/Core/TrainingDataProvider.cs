using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GreyMatter.Core
{
    /// <summary>
    /// Provides training data from NAS without copying to SSD
    /// Supports multiple data sources and progressive difficulty
    /// </summary>
    public class TrainingDataProvider
    {
        private readonly string _nasPath;
        private readonly Random _random = new Random();
        
        public TrainingDataProvider(string nasPath = "/Volumes/jarvis/trainData")
        {
            _nasPath = nasPath;
        }

        /// <summary>
        /// Get available training datasets on NAS
        /// </summary>
        public Dictionary<string, DatasetInfo> GetAvailableDatasets()
        {
            var datasets = new Dictionary<string, DatasetInfo>();

            // === FOUNDATION: Simple Sentences ===
            AddIfExists(datasets, "tatoeba_small", 
                Path.Combine(_nasPath, "Tatoeba/sentences_eng_small.csv"),
                DatasetFormat.TatoebaTSV, 50000, "beginner-intermediate",
                "Real-world English sentences, varied complexity");

            AddIfExists(datasets, "tatoeba_full",
                Path.Combine(_nasPath, "Tatoeba/sentences.csv"),
                DatasetFormat.TatoebaTSV, -1, "beginner-advanced",
                "Complete Tatoeba corpus (685MB) - maximum vocabulary diversity");

            // === DIVERSE CONTENT: Real-world variety ===
            AddIfExists(datasets, "scientific",
                Path.Combine(_nasPath, "enhanced_sources/ScienceData/scientific_abstracts.txt"),
                DatasetFormat.ScientificText, -1, "advanced",
                "Scientific abstracts - technical vocabulary and complex concepts");

            AddIfExists(datasets, "childrens_stories",
                Path.Combine(_nasPath, "enhanced_sources/ChildrensLiterature/childrens_stories.txt"),
                DatasetFormat.NarrativeText, -1, "beginner",
                "Children's literature - simple narratives with moral lessons");

            AddIfExists(datasets, "technical_docs",
                Path.Combine(_nasPath, "enhanced_sources/TechnicalDocs/technical_docs.txt"),
                DatasetFormat.TechnicalDocs, -1, "advanced",
                "Technical documentation - precise language and instructions");

            AddIfExists(datasets, "dialogue",
                Path.Combine(_nasPath, "Tatoeba/sentences.csv"),
                DatasetFormat.TatoebaTSV, -1, "intermediate",
                "Conversational English from Tatoeba - questions, responses, informal language");

            AddIfExists(datasets, "news",
                Path.Combine(_nasPath, "enhanced_sources/NewsData/headlines_sample.txt"),
                DatasetFormat.PlainText, -1, "intermediate",
                "News headlines and articles - current events and journalism");

            AddIfExists(datasets, "social_media",
                Path.Combine(_nasPath, "enhanced_sources/SocialMedia/social_media.txt"),
                DatasetFormat.PlainText, -1, "intermediate",
                "Social media posts - informal language and slang");

            // === LARGE CORPORA: Deep learning ===
            AddIfExists(datasets, "simplewiki",
                Path.Combine(_nasPath, "SimpleWiki/simplewiki-latest-pages-articles-multistream.xml"),
                DatasetFormat.WikiXML, -1, "intermediate",
                "Wikipedia simplified English (1.5GB XML)");

            // === MASSIVE DATASETS: 571GB+ Wikipedia ===
            AddIfExists(datasets, "wikipedia_full",
                Path.Combine(_nasPath, "txtDump/cache/epub"),
                DatasetFormat.DirectoryText, -1, "intermediate-advanced",
                "Full Wikipedia dump (571GB+) - comprehensive world knowledge");

            AddIfExists(datasets, "wikipedia_chunked",
                Path.Combine(_nasPath, "txtDump/cache"),
                DatasetFormat.DirectoryText, -1, "intermediate-advanced",
                "Wikipedia pre-processed chunks - optimized for streaming");

            // === BOOKS: Deep narrative understanding ===
            AddIfExists(datasets, "books_corpus",
                Path.Combine(_nasPath, "books"),
                DatasetFormat.DirectoryText, -1, "intermediate-advanced",
                "Book corpus - long-form narrative and prose");

            AddIfExists(datasets, "epub_collection",
                Path.Combine(_nasPath, "txtDump/cache/epub"),
                DatasetFormat.EPUB, -1, "intermediate-advanced",
                "EPUB collection (500GB+) - diverse literature and non-fiction");

            // === LLM-GENERATED: Infinite curriculum ===
            datasets["llm_generated"] = new DatasetInfo
            {
                Name = "llm_generated",
                Path = "dynamic", // No static path - generated on-the-fly
                Format = DatasetFormat.LLMGenerated,
                SentenceCount = -1,
                DifficultyLevel = "adaptive",
                Description = "LLM-generated educational content - infinite, curriculum-driven training data"
            };

            return datasets;
        }

        private void AddIfExists(Dictionary<string, DatasetInfo> datasets, string key, string path, 
            DatasetFormat format, int sentenceCount, string difficulty, string description)
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                datasets[key] = new DatasetInfo
                {
                    Name = key,
                    Path = path,
                    Format = format,
                    SentenceCount = sentenceCount,
                    DifficultyLevel = difficulty,
                    Description = description
                };
            }
        }

        /// <summary>
        /// Load sentences from a dataset with optional filtering
        /// </summary>
        public IEnumerable<string> LoadSentences(
            string datasetKey,
            int? maxSentences = null,
            int? minWordCount = null,
            int? maxWordCount = null,
            bool shuffle = false)
        {
            var datasets = GetAvailableDatasets();
            if (!datasets.ContainsKey(datasetKey))
            {
                throw new ArgumentException($"Dataset '{datasetKey}' not found. Available: {string.Join(", ", datasets.Keys)}");
            }

            var dataset = datasets[datasetKey];
            var sentences = new List<string>();

            switch (dataset.Format)
            {
                case DatasetFormat.TatoebaTSV:
                    sentences = LoadTatoebaSentences(dataset.Path, maxSentences, minWordCount, maxWordCount);
                    break;
                    
                case DatasetFormat.PlainText:
                case DatasetFormat.ScientificText:
                case DatasetFormat.NarrativeText:
                case DatasetFormat.TechnicalDocs:
                case DatasetFormat.DialogueText:
                    sentences = LoadPlainTextContent(dataset.Path, dataset.Format, maxSentences, minWordCount, maxWordCount);
                    break;
                    
                case DatasetFormat.DirectoryText:
                    sentences = LoadDirectoryText(dataset.Path, maxSentences, minWordCount, maxWordCount);
                    break;
                    
                case DatasetFormat.LLMGenerated:
                    Console.WriteLine("⚠️  LLM-generated content requires LLMTeacher instance - use LoadLLMGeneratedSentences() instead");
                    break;
                    
                case DatasetFormat.WikiXML:
                    Console.WriteLine("⚠️  WikiXML parsing not yet implemented - use SimpleWiki text extracts instead");
                    break;
                    
                case DatasetFormat.EPUB:
                    Console.WriteLine("⚠️  EPUB parsing not yet implemented - use text extracts instead");
                    break;
                    
                default:
                    throw new NotSupportedException($"Format {dataset.Format} not supported");
            }

            if (shuffle)
            {
                sentences = sentences.OrderBy(x => _random.Next()).ToList();
            }

            return sentences;
        }

        /// <summary>
        /// Load plain text content - handles paragraphs, prose, dialogue, etc.
        /// Returns logical "chunks" (sentences or paragraphs depending on format)
        /// </summary>
        private List<string> LoadPlainTextContent(
            string path,
            DatasetFormat format,
            int? maxChunks = null,
            int? minWordCount = null,
            int? maxWordCount = null)
        {
            var chunks = new List<string>();
            
            try
            {
                var allText = File.ReadAllText(path);
                
                // Different strategies based on format
                switch (format)
                {
                    case DatasetFormat.ScientificText:
                    case DatasetFormat.NarrativeText:
                        // Keep paragraphs intact - context matters
                        chunks = SplitIntoParagraphs(allText);
                        Console.WriteLine($"📚 Loaded {chunks.Count:N0} paragraphs from {Path.GetFileName(path)}");
                        break;
                        
                    case DatasetFormat.DialogueText:
                        // Split by line breaks - each line is usually one utterance
                        chunks = allText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                            .Select(l => l.Trim())
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .ToList();
                        Console.WriteLine($"📚 Loaded {chunks.Count:N0} dialogue lines from {Path.GetFileName(path)}");
                        break;
                        
                    case DatasetFormat.TechnicalDocs:
                    case DatasetFormat.PlainText:
                    default:
                        // Split into sentences
                        chunks = SplitIntoSentences(allText);
                        Console.WriteLine($"📚 Loaded {chunks.Count:N0} sentences from {Path.GetFileName(path)}");
                        break;
                }

                // Apply filters
                if (minWordCount.HasValue || maxWordCount.HasValue)
                {
                    chunks = chunks.Where(chunk =>
                    {
                        var wordCount = chunk.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                        if (minWordCount.HasValue && wordCount < minWordCount.Value) return false;
                        if (maxWordCount.HasValue && wordCount > maxWordCount.Value) return false;
                        return true;
                    }).ToList();
                }

                // Limit count
                if (maxChunks.HasValue && chunks.Count > maxChunks.Value)
                {
                    chunks = chunks.Take(maxChunks.Value).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Error loading {path}: {ex.Message}");
            }

            return chunks;
        }

        /// <summary>
        /// Split text into paragraphs (preserves context)
        /// </summary>
        private List<string> SplitIntoParagraphs(string text)
        {
            return text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();
        }

        /// <summary>
        /// Split text into sentences (basic sentence splitting)
        /// </summary>
        private List<string> SplitIntoSentences(string text)
        {
            // Basic sentence splitting - could be enhanced with NLP
            var sentences = new List<string>();
            var currentSentence = "";
            
            foreach (var c in text)
            {
                currentSentence += c;
                
                // End of sentence markers
                if (c == '.' || c == '!' || c == '?')
                {
                    var trimmed = currentSentence.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed) && trimmed.Split(' ').Length > 2)
                    {
                        sentences.Add(trimmed);
                    }
                    currentSentence = "";
                }
            }
            
            // Add any remaining text
            if (!string.IsNullOrWhiteSpace(currentSentence.Trim()))
            {
                sentences.Add(currentSentence.Trim());
            }
            
            return sentences;
        }

        /// <summary>
        /// Load Tatoeba format: ID\tlang\tsentence
        /// </summary>
        private List<string> LoadTatoebaSentences(
            string path, 
            int? maxSentences = null,
            int? minWordCount = null,
            int? maxWordCount = null)
        {
            var sentences = new List<string>();
            var linesRead = 0;

            try
            {
                foreach (var line in File.ReadLines(path))
                {
                    linesRead++;
                    
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Format: ID\tlang\tsentence
                    var parts = line.Split('\t');
                    if (parts.Length < 3)
                        continue;

                    var sentence = parts[2].Trim();
                    if (string.IsNullOrWhiteSpace(sentence))
                        continue;

                    // Apply word count filters
                    var wordCount = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                    
                    if (minWordCount.HasValue && wordCount < minWordCount.Value)
                        continue;
                        
                    if (maxWordCount.HasValue && wordCount > maxWordCount.Value)
                        continue;

                    sentences.Add(sentence);

                    if (maxSentences.HasValue && sentences.Count >= maxSentences.Value)
                        break;
                }

                Console.WriteLine($"📚 Loaded {sentences.Count:N0} sentences from {Path.GetFileName(path)} (scanned {linesRead:N0} lines)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Error loading Tatoeba data: {ex.Message}");
            }

            return sentences;
        }

        /// <summary>
        /// Get progressive curriculum: start simple, increase complexity AND diversity
        /// NOW USING MASSIVE DATASETS: 571GB Wikipedia, Books, LLM-generated content
        /// Progressively expands from simple to encyclopedic knowledge
        /// </summary>
        public ProgressiveCurriculum GetProgressiveCurriculum(string baseDataset = "tatoeba_small")
        {
            return new ProgressiveCurriculum
            {
                Phase1_Foundation = new TrainingPhase
                {
                    Name = "Foundation (0-1K sentences)",
                    DatasetKey = baseDataset,  // Start with simple short sentences
                    MaxSentences = 1000,
                    MinWordCount = 3,
                    MaxWordCount = 12,
                    Description = "Short simple sentences - basic vocabulary and grammar"
                },
                Phase2_Expansion = new TrainingPhase
                {
                    Name = "Expansion (1K-5K sentences)",
                    DatasetKey = "news",  // News headlines - real-world events (39MB)
                    MaxSentences = 5000,
                    MinWordCount = 5,
                    MaxWordCount = 25,
                    Description = "News headlines - current events and journalism vocabulary"
                },
                Phase3_Dialogue = new TrainingPhase
                {
                    Name = "Dialogue (5K-10K sentences)",
                    DatasetKey = "dialogue",  // Conversational language from subtitles (608KB)
                    MaxSentences = 5000,
                    MinWordCount = 3,
                    MaxWordCount = 30,
                    Description = "Conversational English - questions, responses, informal language"
                },
                Phase4_BooksIntro = new TrainingPhase
                {
                    Name = "Narrative (10K-20K sentences)",
                    DatasetKey = "books_corpus",  // 🚀 BOOKS: Narrative structures, storytelling
                    MaxSentences = 10000,
                    MinWordCount = 5,
                    MaxWordCount = 40,
                    Description = "Book corpus - narrative structures, complex storytelling"
                },
                Phase5_WikipediaChunked = new TrainingPhase
                {
                    Name = "Encyclopedic (20K-50K sentences)",
                    DatasetKey = "wikipedia_chunked",  // 🚀 WIKIPEDIA CHUNKS: Pre-processed
                    MaxSentences = 30000,
                    MinWordCount = 10,
                    MaxWordCount = 50,
                    Description = "Wikipedia chunks - encyclopedic knowledge, technical vocabulary"
                },
                Phase6_FullCorpus = new TrainingPhase
                {
                    Name = "Full Corpus (50K+ sentences)",
                    DatasetKey = "wikipedia_full",  // 🚀 571GB WIKIPEDIA: Maximum diversity
                    MaxSentences = null,
                    MinWordCount = null,
                    MaxWordCount = null,
                    Description = "Full Wikipedia (571GB+) - comprehensive world knowledge"
                }
            };
        }
        
        /// <summary>
        /// Load all text files from a directory recursively (for massive Wikipedia/book corpora)
        /// </summary>
        private List<string> LoadDirectoryText(string dirPath, int? maxSentences, int? minWords, int? maxWords)
        {
            var sentences = new List<string>();
            
            if (!Directory.Exists(dirPath))
            {
                Console.WriteLine($"⚠️  Directory not found: {dirPath}");
                return sentences;
            }
            
            Console.WriteLine($"📁 Scanning directory: {dirPath}");
            var textFiles = Directory.GetFiles(dirPath, "*.txt", SearchOption.AllDirectories);
            Console.WriteLine($"   Found {textFiles.Length} text files");
            
            var filesProcessed = 0;
            foreach (var file in textFiles.OrderBy(x => _random.Next()))
            {
                if (maxSentences.HasValue && sentences.Count >= maxSentences.Value)
                    break;
                    
                try
                {
                    var lines = File.ReadAllLines(file);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        
                        var wordCount = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                        if (minWords.HasValue && wordCount < minWords.Value) continue;
                        if (maxWords.HasValue && wordCount > maxWords.Value) continue;
                        
                        sentences.Add(line.Trim());
                        
                        if (maxSentences.HasValue && sentences.Count >= maxSentences.Value)
                            break;
                    }
                    
                    filesProcessed++;
                    if (filesProcessed % 100 == 0)
                        Console.WriteLine($"   Processed {filesProcessed} files, collected {sentences.Count:N0} sentences");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️  Error reading {Path.GetFileName(file)}: {ex.Message}");
                }
            }
            
            Console.WriteLine($"✅ Loaded {sentences.Count:N0} sentences from {filesProcessed} files in {dirPath}");
            return sentences;
        }
        
        /// <summary>
        /// Generate sentences on-the-fly using LLM teacher
        /// </summary>
        public async Task<string> LoadLLMGeneratedSentencesAsync(
            LLMTeacher teacher, 
            int count, 
            string topic = "general knowledge",
            string difficulty = "intermediate")
        {
            var sentences = new List<string>();
            Console.WriteLine($"🤖 Generating {count} sentences via LLM on topic: {topic} (difficulty: {difficulty})");
            
            var request = new ContentRequest
            {
                Topic = topic,
                TargetAudience = "general learners",
                DifficultyLevel = difficulty,
                ContentLength = count * 20, // Rough estimate: 20 words per sentence
                LearningObjectives = new List<string> { $"Understand {topic}", "Build vocabulary", "Practice comprehension" }
            };
            
            var content = await teacher.GenerateEducationalContentAsync(request);
            
            // Split content into sentences
            if (!string.IsNullOrWhiteSpace(content.Content))
            {
                var rawSentences = content.Content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var sent in rawSentences.Take(count))
                {
                    var cleaned = sent.Trim();
                    if (!string.IsNullOrWhiteSpace(cleaned) && cleaned.Split(' ').Length > 3)
                    {
                        sentences.Add(cleaned + ".");
                    }
                }
            }
            
            Console.WriteLine($"✅ Generated {sentences.Count} LLM sentences");
            return string.Join("\n", sentences);
        }
    }

    public class DatasetInfo
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public DatasetFormat Format { get; set; }
        public int SentenceCount { get; set; }
        public string DifficultyLevel { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public enum DatasetFormat
    {
        TatoebaTSV,      // Tab-separated: ID\tlang\tsentence
        WikiXML,         // Wikipedia XML dump
        PlainText,       // Raw text files (books, articles)
        ScientificText,  // Scientific abstracts and papers
        DialogueText,    // Subtitles, conversations
        TechnicalDocs,   // Documentation, manuals
        NarrativeText,   // Stories, novels
        EPUB,            // EPUB book format
        DirectoryText,   // Recursively load all .txt files from directory
        LLMGenerated     // On-the-fly generation via LLM teacher
    }

    public class ProgressiveCurriculum
    {
        public TrainingPhase Phase1_Foundation { get; set; } = new TrainingPhase();
        public TrainingPhase Phase2_Expansion { get; set; } = new TrainingPhase();
        public TrainingPhase Phase3_Dialogue { get; set; } = new TrainingPhase();
        public TrainingPhase Phase4_BooksIntro { get; set; } = new TrainingPhase();  // 🚀 BOOKS
        public TrainingPhase Phase5_WikipediaChunked { get; set; } = new TrainingPhase();  // 🚀 WIKIPEDIA
        public TrainingPhase Phase6_FullCorpus { get; set; } = new TrainingPhase();

        public TrainingPhase GetPhaseForSentenceCount(long sentenceCount)
        {
            if (sentenceCount < 1000) return Phase1_Foundation;
            if (sentenceCount < 5000) return Phase2_Expansion;
            if (sentenceCount < 10000) return Phase3_Dialogue;
            if (sentenceCount < 20000) return Phase4_BooksIntro;  // 10K-20K: Books
            if (sentenceCount < 50000) return Phase5_WikipediaChunked;  // 20K-50K: Wikipedia chunks
            return Phase6_FullCorpus;  // 50K+: Full 571GB Wikipedia
        }
    }

    public class TrainingPhase
    {
        public string Name { get; set; } = "";
        public string DatasetKey { get; set; } = "";
        public int? MaxSentences { get; set; }
        public int? MinWordCount { get; set; }
        public int? MaxWordCount { get; set; }
        public string Description { get; set; } = "";
    }
}
