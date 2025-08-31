using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;
using GreyMatter.Storage;

namespace GreyMatter.Core
{
    /// <summary>
    /// Reading Comprehension System - Processes long-form text and enables question answering
    /// Integrates episodic memory for context tracking and narrative understanding
    /// </summary>
    public class ReadingComprehensionSystem
    {
        private readonly Cerebro _brain;
        private readonly EpisodicMemorySystem _episodicMemory;
        private readonly SemanticStorageManager _storageManager;
        private readonly greyMatter.Core.LanguageEphemeralBrain _languageBrain;
        private readonly Dictionary<string, TextDocument> _processedDocuments = new();
        private readonly object _documentLock = new();

        // Reading comprehension parameters
        public int MaxContextWindow { get; set; } = 1000; // Characters for context window
        public double SemanticSimilarityThreshold { get; set; } = 0.7;
        public int MaxAnswerCandidates { get; set; } = 5;

        public ReadingComprehensionSystem(Cerebro brain, EpisodicMemorySystem episodicMemory,
                                        SemanticStorageManager storageManager, greyMatter.Core.LanguageEphemeralBrain languageBrain)
        {
            _brain = brain;
            _episodicMemory = episodicMemory;
            _storageManager = storageManager;
            _languageBrain = languageBrain;
        }

        /// <summary>
        /// Process a text document for reading comprehension
        /// </summary>
        public async Task<TextDocument> ProcessDocumentAsync(string documentId, string title, string content,
                                                           string source = null, Dictionary<string, object> metadata = null)
        {
            var document = new TextDocument
            {
                Id = documentId,
                Title = title,
                Content = content,
                Source = source ?? "unknown",
                Metadata = metadata ?? new Dictionary<string, object>(),
                ProcessedAt = DateTime.UtcNow,
                WordCount = CountWords(content),
                SentenceCount = CountSentences(content)
            };

            // Extract key information
            document.KeyEntities = await ExtractKeyEntitiesAsync(content);
            document.Topics = await ExtractTopicsAsync(content);
            document.Summary = await GenerateSummaryAsync(content);

            // Process content in chunks for episodic memory
            await ProcessContentForEpisodicMemoryAsync(documentId, content);

            // Store document
            lock (_documentLock)
            {
                _processedDocuments[documentId] = document;
            }

            // Persist document to storage
            await PersistDocumentAsync(document);

            return document;
        }

        /// <summary>
        /// Answer questions about processed documents
        /// </summary>
        public async Task<List<QuestionAnswer>> AnswerQuestionAsync(string question, string documentId = null)
        {
            var answers = new List<QuestionAnswer>();

            // Determine relevant documents
            var relevantDocuments = GetRelevantDocuments(question, documentId);

            foreach (var doc in relevantDocuments)
            {
                var docAnswers = await AnswerQuestionForDocumentAsync(question, doc);
                answers.AddRange(docAnswers);
            }

            // Sort by confidence and return top answers
            return answers.OrderByDescending(a => a.Confidence)
                         .Take(MaxAnswerCandidates)
                         .ToList();
        }

        /// <summary>
        /// Generate questions about a document
        /// </summary>
        public async Task<List<string>> GenerateQuestionsAsync(string documentId)
        {
            TextDocument document;
            lock (_documentLock)
            {
                if (!_processedDocuments.TryGetValue(documentId, out document))
                    return new List<string>();
            }

            var questions = new List<string>();

            // Generate questions from key entities
            foreach (var entity in document.KeyEntities.Take(3))
            {
                questions.Add($"What is the significance of {entity} in this text?");
                questions.Add($"How does {entity} relate to the main topic?");
            }

            // Generate questions from topics
            foreach (var topic in document.Topics.Take(2))
            {
                questions.Add($"What does the text say about {topic}?");
                questions.Add($"How is {topic} discussed in this document?");
            }

            // Generate questions from episodic memory
            var episodicQuestions = _episodicMemory.GenerateQuestions(documentId);
            questions.AddRange(episodicQuestions);

            return questions.Distinct().Take(10).ToList();
        }

        /// <summary>
        /// Find connections between documents
        /// </summary>
        public List<DocumentConnection> FindDocumentConnections()
        {
            var connections = new List<DocumentConnection>();

            lock (_documentLock)
            {
                var documents = _processedDocuments.Values.ToList();

                for (int i = 0; i < documents.Count; i++)
                {
                    for (int j = i + 1; j < documents.Count; j++)
                    {
                        var connection = AnalyzeDocumentSimilarity(documents[i], documents[j]);
                        if (connection != null)
                        {
                            connections.Add(connection);
                        }
                    }
                }
            }

            return connections.OrderByDescending(c => c.SimilarityScore).ToList();
        }

        /// <summary>
        /// Get reading comprehension statistics
        /// </summary>
        public ReadingStats GetReadingStats()
        {
            lock (_documentLock)
            {
                return new ReadingStats
                {
                    TotalDocuments = _processedDocuments.Count,
                    TotalWords = _processedDocuments.Values.Sum(d => d.WordCount),
                    TotalSentences = _processedDocuments.Values.Sum(d => d.SentenceCount),
                    AverageDocumentLength = _processedDocuments.Count > 0 ?
                        _processedDocuments.Values.Average(d => d.WordCount) : 0,
                    UniqueTopics = _processedDocuments.Values
                        .SelectMany(d => d.Topics)
                        .Distinct()
                        .Count(),
                    LastProcessedDocument = _processedDocuments.Values
                        .OrderByDescending(d => d.ProcessedAt)
                        .FirstOrDefault()?.Title
                };
            }
        }

        private async Task ProcessContentForEpisodicMemoryAsync(string documentId, string content)
        {
            // Split content into meaningful segments
            var segments = SegmentContent(content);

            foreach (var segment in segments)
            {
                var eventId = $"{documentId}_segment_{segments.IndexOf(segment)}";
                var context = new Dictionary<string, object>
                {
                    ["document_id"] = documentId,
                    ["segment_index"] = segments.IndexOf(segment),
                    ["content_length"] = segment.Length,
                    ["word_count"] = CountWords(segment)
                };

                await _episodicMemory.RecordEventAsync(
                    eventId,
                    $"Reading segment from document: {segment.Substring(0, Math.Min(100, segment.Length))}",
                    context,
                    DateTime.UtcNow,
                    new List<string> { "reader", "document_processor" },
                    "text_reading"
                );
            }
        }

        private async Task<List<string>> ExtractKeyEntitiesAsync(string content)
        {
            // Simple entity extraction - can be enhanced with NLP
            var entities = new List<string>();
            var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var sentence in sentences)
            {
                // Look for capitalized words that might be entities
                var words = sentence.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    if (word.Length > 3 && char.IsUpper(word[0]) &&
                        !IsCommonWord(word.ToLower()) && !entities.Contains(word))
                    {
                        entities.Add(word);
                    }
                }
            }

            return entities.Distinct().Take(10).ToList();
        }

        private async Task<List<string>> ExtractTopicsAsync(string content)
        {
            // Simple topic extraction based on word frequency
            var words = content.ToLower()
                              .Split(new[] { ' ', '\t', '\n', '.', ',', '!', '?' },
                                     StringSplitOptions.RemoveEmptyEntries)
                              .Where(w => w.Length > 3 && !IsCommonWord(w))
                              .GroupBy(w => w)
                              .OrderByDescending(g => g.Count())
                              .Take(10)
                              .Select(g => g.Key)
                              .ToList();

            return words;
        }

        private async Task<string> GenerateSummaryAsync(string content)
        {
            // Simple extractive summarization
            var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            var importantSentences = sentences
                .OrderByDescending(s => CalculateSentenceImportance(s))
                .Take(3)
                .ToArray();

            return string.Join(". ", importantSentences) + ".";
        }

        private List<TextDocument> GetRelevantDocuments(string question, string documentId = null)
        {
            lock (_documentLock)
            {
                if (!string.IsNullOrEmpty(documentId) && _processedDocuments.TryGetValue(documentId, out var specificDoc))
                {
                    return new List<TextDocument> { specificDoc };
                }

                // Find documents relevant to the question
                return _processedDocuments.Values
                    .Where(d => IsDocumentRelevant(d, question))
                    .OrderByDescending(d => CalculateDocumentRelevance(d, question))
                    .Take(3)
                    .ToList();
            }
        }

        private async Task<List<QuestionAnswer>> AnswerQuestionForDocumentAsync(string question, TextDocument document)
        {
            var answers = new List<QuestionAnswer>();

            // Split document into sentences for analysis
            var sentences = document.Content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var sentence in sentences)
            {
                var confidence = CalculateAnswerConfidence(question, sentence);
                if (confidence > 0.3)
                {
                    answers.Add(new QuestionAnswer
                    {
                        Question = question,
                        Answer = sentence.Trim(),
                        Confidence = confidence,
                        SourceDocument = document.Title,
                        ContextWindow = GetContextWindow(document.Content, sentence)
                    });
                }
            }

            return answers;
        }

        private async Task PersistDocumentAsync(TextDocument document)
        {
            var documentData = new Dictionary<string, object>
            {
                ["id"] = document.Id,
                ["title"] = document.Title,
                ["content"] = document.Content,
                ["source"] = document.Source,
                ["metadata"] = document.Metadata,
                ["processed_at"] = document.ProcessedAt,
                ["word_count"] = document.WordCount,
                ["sentence_count"] = document.SentenceCount,
                ["key_entities"] = document.KeyEntities,
                ["topics"] = document.Topics,
                ["summary"] = document.Summary
            };

            await _storageManager.SaveConceptAsync(document.Id, documentData, ConceptType.General);
        }

        private List<string> SegmentContent(string content)
        {
            // Split content into logical segments (paragraphs or fixed-size chunks)
            var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (paragraphs.Length > 1)
                return paragraphs.ToList();

            // If no paragraphs, split by sentence count
            var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            var segments = new List<string>();
            var currentSegment = "";

            foreach (var sentence in sentences)
            {
                currentSegment += sentence + ". ";
                if (CountWords(currentSegment) >= 100) // ~100 words per segment
                {
                    segments.Add(currentSegment.Trim());
                    currentSegment = "";
                }
            }

            if (!string.IsNullOrEmpty(currentSegment))
                segments.Add(currentSegment.Trim());

            return segments;
        }

        private bool IsCommonWord(string word)
        {
            var commonWords = new HashSet<string>
            {
                "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by",
                "this", "that", "these", "those", "is", "are", "was", "were", "be", "been", "being",
                "have", "has", "had", "do", "does", "did", "will", "would", "could", "should", "may",
                "can", "might", "must", "shall", "it", "its", "they", "them", "their", "we", "us", "our"
            };

            return commonWords.Contains(word.ToLower());
        }

        private double CalculateSentenceImportance(string sentence)
        {
            // Simple importance calculation based on length and entity mentions
            var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var lengthScore = Math.Min(words.Length / 20.0, 1.0); // Prefer medium-length sentences
            var entityScore = words.Count(w => char.IsUpper(w[0])) / (double)words.Length;

            return (lengthScore + entityScore) / 2.0;
        }

        private bool IsDocumentRelevant(TextDocument document, string question)
        {
            var questionWords = question.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var contentWords = document.Content.ToLower();

            var matchingWords = questionWords.Count(word =>
                contentWords.Contains(word) ||
                document.Topics.Any(topic => topic.Contains(word)) ||
                document.KeyEntities.Any(entity => entity.ToLower().Contains(word)));

            return matchingWords >= questionWords.Length * 0.3; // At least 30% word match
        }

        private double CalculateDocumentRelevance(TextDocument document, string question)
        {
            var questionWords = question.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var contentLower = document.Content.ToLower();

            var exactMatches = questionWords.Count(word => contentLower.Contains(word));
            var topicMatches = questionWords.Count(word =>
                document.Topics.Any(topic => topic.Contains(word)));
            var entityMatches = questionWords.Count(word =>
                document.KeyEntities.Any(entity => entity.ToLower().Contains(word)));

            return (exactMatches + topicMatches * 0.8 + entityMatches * 0.6) / questionWords.Length;
        }

        private double CalculateAnswerConfidence(string question, string sentence)
        {
            var questionWords = question.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var sentenceWords = sentence.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var matchingWords = questionWords.Count(word =>
                sentenceWords.Any(sword => sword.Contains(word) || word.Contains(sword)));

            return matchingWords / (double)questionWords.Length;
        }

        private string GetContextWindow(string content, string targetSentence)
        {
            var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            var targetIndex = Array.FindIndex(sentences, s => s.Trim() == targetSentence.Trim());

            if (targetIndex == -1) return targetSentence;

            var startIndex = Math.Max(0, targetIndex - 1);
            var endIndex = Math.Min(sentences.Length - 1, targetIndex + 1);

            var contextSentences = sentences.Skip(startIndex).Take(endIndex - startIndex + 1);
            return string.Join(". ", contextSentences) + ".";
        }

        private DocumentConnection AnalyzeDocumentSimilarity(TextDocument doc1, TextDocument doc2)
        {
            // Calculate similarity based on shared topics and entities
            var sharedTopics = doc1.Topics.Intersect(doc2.Topics).Count();
            var sharedEntities = doc1.KeyEntities.Intersect(doc2.KeyEntities).Count();
            var topicSimilarity = sharedTopics / Math.Max(doc1.Topics.Count + doc2.Topics.Count - sharedTopics, 1.0);
            var entitySimilarity = sharedEntities / Math.Max(doc1.KeyEntities.Count + doc2.KeyEntities.Count - sharedEntities, 1.0);

            var similarityScore = (topicSimilarity + entitySimilarity) / 2.0;

            if (similarityScore > 0.2)
            {
                return new DocumentConnection
                {
                    Document1Id = doc1.Id,
                    Document2Id = doc2.Id,
                    Document1Title = doc1.Title,
                    Document2Title = doc2.Title,
                    SimilarityScore = similarityScore,
                    SharedTopics = doc1.Topics.Intersect(doc2.Topics).ToList(),
                    SharedEntities = doc1.KeyEntities.Intersect(doc2.KeyEntities).ToList()
                };
            }

            return null;
        }

        private int CountWords(string text)
        {
            return text.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private int CountSentences(string text)
        {
            return text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }

    /// <summary>
    /// Represents a processed text document
    /// </summary>
    public class TextDocument
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Source { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public DateTime ProcessedAt { get; set; }
        public int WordCount { get; set; }
        public int SentenceCount { get; set; }
        public List<string> KeyEntities { get; set; }
        public List<string> Topics { get; set; }
        public string Summary { get; set; }
    }

    /// <summary>
    /// Represents a question-answer pair
    /// </summary>
    public class QuestionAnswer
    {
        public string Question { get; set; }
        public string Answer { get; set; }
        public double Confidence { get; set; }
        public string SourceDocument { get; set; }
        public string ContextWindow { get; set; }
    }

    /// <summary>
    /// Represents a connection between two documents
    /// </summary>
    public class DocumentConnection
    {
        public string Document1Id { get; set; }
        public string Document1Title { get; set; }
        public string Document2Id { get; set; }
        public string Document2Title { get; set; }
        public double SimilarityScore { get; set; }
        public List<string> SharedTopics { get; set; }
        public List<string> SharedEntities { get; set; }
    }

    /// <summary>
    /// Reading comprehension statistics
    /// </summary>
    public class ReadingStats
    {
        public int TotalDocuments { get; set; }
        public int TotalWords { get; set; }
        public int TotalSentences { get; set; }
        public double AverageDocumentLength { get; set; }
        public int UniqueTopics { get; set; }
        public string LastProcessedDocument { get; set; }
    }
}
