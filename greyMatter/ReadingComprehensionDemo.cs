using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;
using GreyMatter.Storage;
using greyMatter.Core;

namespace GreyMatter
{
    /// <summary>
    /// Reading Comprehension Demo - Demonstrates episodic memory and text understanding
    /// </summary>
    public class ReadingComprehensionDemo
    {
        private readonly Cerebro _brain;
        private readonly CerebroConfiguration _config;
        private readonly EpisodicMemorySystem _episodicMemory;
        private readonly ReadingComprehensionSystem _readingSystem;
        private readonly SemanticStorageManager _storageManager;
        private readonly greyMatter.Core.LanguageEphemeralBrain _languageBrain;

        public ReadingComprehensionDemo(Cerebro brain, CerebroConfiguration config)
        {
            _brain = brain;
            _config = config;
            _storageManager = new SemanticStorageManager(config.BrainDataPath, config.TrainingDataRoot);
            _episodicMemory = new EpisodicMemorySystem(_storageManager);
            _languageBrain = new LanguageEphemeralBrain();
            _readingSystem = new ReadingComprehensionSystem(brain, _episodicMemory, _storageManager, _languageBrain);
        }

        /// <summary>
        /// Run the reading comprehension demonstration
        /// </summary>
        public async Task RunDemoAsync()
        {
            Console.WriteLine("📖 **READING COMPREHENSION SYSTEM DEMO**");
            Console.WriteLine("=======================================");
            Console.WriteLine("This demo showcases episodic memory and reading comprehension capabilities.");
            Console.WriteLine();

            // Process sample documents
            await ProcessSampleDocumentsAsync();

            // Demonstrate question answering
            await DemonstrateQuestionAnsweringAsync();

            // Show episodic memory capabilities
            await DemonstrateEpisodicMemoryAsync();

            // Display reading statistics
            DisplayReadingStats();

            // Interactive mode
            await RunInteractiveModeAsync();
        }

        private async Task ProcessSampleDocumentsAsync()
        {
            Console.WriteLine("📚 **PROCESSING SAMPLE DOCUMENTS**");
            Console.WriteLine("----------------------------------");

            var sampleDocuments = new[]
            {
                new
                {
                    Id = "doc_001",
                    Title = "Introduction to Artificial Intelligence",
                    Content = @"Artificial Intelligence (AI) is a branch of computer science that aims to create machines capable of intelligent behavior. AI systems can learn from experience, adapt to new inputs, and perform tasks that typically require human intelligence. Machine learning is a subset of AI that focuses on algorithms that can learn patterns from data without being explicitly programmed. Deep learning, a further subset of machine learning, uses neural networks with multiple layers to process complex data patterns. AI has applications in various fields including healthcare, finance, transportation, and entertainment. The development of AI raises important ethical considerations about job displacement, privacy, and decision-making transparency."
                },
                new
                {
                    Id = "doc_002",
                    Title = "The History of Neural Networks",
                    Content = @"Neural networks have their roots in the 1940s when Warren McCulloch and Walter Pitts created a computational model based on biological neurons. The perceptron, invented by Frank Rosenblatt in 1957, was one of the first artificial neural networks. However, early neural networks faced limitations due to computational constraints and the lack of large datasets. The field experienced a renaissance in the 1980s with the development of backpropagation algorithms. Geoffrey Hinton, Yann LeCun, and Yoshua Bengio made significant contributions to deep learning in the 2000s and 2010s. Today, neural networks power many AI applications including image recognition, natural language processing, and autonomous vehicles. The success of modern neural networks relies on large amounts of data, powerful computing resources, and sophisticated algorithms."
                },
                new
                {
                    Id = "doc_003",
                    Title = "Cognitive Science and Human Learning",
                    Content = @"Cognitive science is an interdisciplinary field that studies the mind and intelligence. It combines insights from psychology, neuroscience, linguistics, philosophy, and computer science. Human learning involves various processes including perception, attention, memory, and problem-solving. Episodic memory allows us to remember specific events and experiences, while semantic memory stores general knowledge and facts. Working memory temporarily holds information for processing, and long-term memory provides permanent storage. Cognitive scientists study how people acquire knowledge, solve problems, and make decisions. Understanding human cognition helps in designing better AI systems and educational approaches. Research in cognitive science continues to advance our understanding of intelligence and learning mechanisms."
                }
            };

            foreach (var doc in sampleDocuments)
            {
                Console.WriteLine($"📖 Processing: {doc.Title}");
                var processedDoc = await _readingSystem.ProcessDocumentAsync(
                    doc.Id, doc.Title, doc.Content, "demo_corpus");

                Console.WriteLine($"   ✅ Processed {processedDoc.WordCount} words, {processedDoc.SentenceCount} sentences");
                Console.WriteLine($"   📋 Key topics: {string.Join(", ", processedDoc.Topics.Take(3))}");
                Console.WriteLine($"   🎯 Key entities: {string.Join(", ", processedDoc.KeyEntities.Take(3))}");
                Console.WriteLine();
            }
        }

        private async Task DemonstrateQuestionAnsweringAsync()
        {
            Console.WriteLine("❓ **QUESTION ANSWERING DEMONSTRATION**");
            Console.WriteLine("--------------------------------------");

            var questions = new[]
            {
                "What is artificial intelligence?",
                "Who invented the perceptron?",
                "What are the main types of human memory?",
                "What is deep learning?",
                "How do neural networks work?"
            };

            foreach (var question in questions)
            {
                Console.WriteLine($"Q: {question}");

                var answers = await _readingSystem.AnswerQuestionAsync(question);
                if (answers.Any())
                {
                    var bestAnswer = answers.First();
                    Console.WriteLine($"A: {bestAnswer.Answer}");
                    Console.WriteLine($"   📊 Confidence: {bestAnswer.Confidence:F2}, Source: {bestAnswer.SourceDocument}");
                    Console.WriteLine($"   📝 Context: {bestAnswer.ContextWindow}");
                }
                else
                {
                    Console.WriteLine("A: No confident answer found.");
                }
                Console.WriteLine();
            }
        }

        private async Task DemonstrateEpisodicMemoryAsync()
        {
            Console.WriteLine("🧠 **EPISODIC MEMORY DEMONSTRATION**");
            Console.WriteLine("-----------------------------------");

            // Record some reading events
            await _episodicMemory.RecordEventAsync(
                "reading_session_001",
                "Started reading about AI history",
                new Dictionary<string, object> { ["topic"] = "AI", ["progress"] = "beginning" },
                DateTime.UtcNow,
                new List<string> { "reader", "student" },
                "study_session"
            );

            await _episodicMemory.RecordEventAsync(
                "reading_session_002",
                "Learned about neural network history",
                new Dictionary<string, object> { ["topic"] = "neural_networks", ["progress"] = "middle" },
                DateTime.UtcNow.AddMinutes(15),
                new List<string> { "reader", "student" },
                "study_session"
            );

            // Retrieve events
            var recentEvents = _episodicMemory.RetrieveEvents("reading");
            Console.WriteLine($"📅 Found {recentEvents.Count} recent reading events:");

            foreach (var evt in recentEvents.Take(3))
            {
                Console.WriteLine($"   • {evt.Timestamp:HH:mm}: {evt.Description}");
                Console.WriteLine($"     Participants: {string.Join(", ", evt.Participants)}");
            }

            // Build narrative
            if (recentEvents.Count >= 2)
            {
                var narrative = _episodicMemory.BuildNarrative("reading_narrative_001", recentEvents);
                Console.WriteLine();
                Console.WriteLine("📖 **NARRATIVE SUMMARY**");
                Console.WriteLine($"   Duration: {(narrative.EndTime - narrative.StartTime).TotalMinutes:F1} minutes");
                Console.WriteLine($"   Theme: {narrative.Theme}");
                Console.WriteLine($"   Summary: {narrative.Summary}");
            }

            // Generate questions from memory
            var memoryQuestions = _episodicMemory.GenerateQuestions();
            Console.WriteLine();
            Console.WriteLine("🤔 **MEMORY-GENERATED QUESTIONS**");
            foreach (var question in memoryQuestions.Take(3))
            {
                Console.WriteLine($"   • {question}");
            }

            Console.WriteLine();
        }

        private void DisplayReadingStats()
        {
            Console.WriteLine("📊 **READING COMPREHENSION STATISTICS**");
            Console.WriteLine("--------------------------------------");

            var stats = _readingSystem.GetReadingStats();
            Console.WriteLine($"📚 Total Documents: {stats.TotalDocuments}");
            Console.WriteLine($"📝 Total Words: {stats.TotalWords}");
            Console.WriteLine($"📋 Total Sentences: {stats.TotalSentences}");
            Console.WriteLine($"📏 Average Document Length: {stats.AverageDocumentLength:F0} words");
            Console.WriteLine($"🏷️  Unique Topics: {stats.UniqueTopics}");
            Console.WriteLine($"🕒 Last Processed: {stats.LastProcessedDocument}");

            // Show document connections
            var connections = _readingSystem.FindDocumentConnections();
            if (connections.Any())
            {
                Console.WriteLine();
                Console.WriteLine("🔗 **DOCUMENT CONNECTIONS**");
                foreach (var connection in connections.Take(3))
                {
                    Console.WriteLine($"   • {connection.Document1Title} ↔ {connection.Document2Title}");
                    Console.WriteLine($"     Similarity: {connection.SimilarityScore:F2}");
                    if (connection.SharedTopics.Any())
                        Console.WriteLine($"     Shared topics: {string.Join(", ", connection.SharedTopics.Take(2))}");
                }
            }

            Console.WriteLine();
        }

        private async Task RunInteractiveModeAsync()
        {
            Console.WriteLine("🎮 **INTERACTIVE MODE**");
            Console.WriteLine("======================");
            Console.WriteLine("Commands:");
            Console.WriteLine("  'ask <question>' - Ask a question about processed documents");
            Console.WriteLine("  'memory' - Show recent episodic memories");
            Console.WriteLine("  'questions <doc_id>' - Generate questions about a document");
            Console.WriteLine("  'stats' - Show reading statistics");
            Console.WriteLine("  'quit' - Exit interactive mode");
            Console.WriteLine();

            while (true)
            {
                Console.Write("🤖> ");
                var input = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(input))
                    continue;

                if (input.ToLower() == "quit" || input.ToLower() == "exit")
                    break;

                if (input.ToLower().StartsWith("ask "))
                {
                    var question = input.Substring(4);
                    var answers = await _readingSystem.AnswerQuestionAsync(question);
                    if (answers.Any())
                    {
                        var bestAnswer = answers.First();
                        Console.WriteLine($"Answer: {bestAnswer.Answer}");
                        Console.WriteLine($"Confidence: {bestAnswer.Confidence:F2}");
                    }
                    else
                    {
                        Console.WriteLine("No confident answer found.");
                    }
                }
                else if (input.ToLower() == "memory")
                {
                    var events = _episodicMemory.RetrieveEvents("");
                    Console.WriteLine($"Recent events ({events.Count}):");
                    foreach (var evt in events.Take(5))
                    {
                        Console.WriteLine($"  {evt.Timestamp:HH:mm}: {evt.Description}");
                    }
                }
                else if (input.ToLower().StartsWith("questions "))
                {
                    var docId = input.Substring(10);
                    var questions = await _readingSystem.GenerateQuestionsAsync(docId);
                    Console.WriteLine($"Generated questions for {docId}:");
                    foreach (var question in questions)
                    {
                        Console.WriteLine($"  • {question}");
                    }
                }
                else if (input.ToLower() == "stats")
                {
                    DisplayReadingStats();
                }
                else
                {
                    Console.WriteLine("Unknown command. Try 'ask <question>', 'memory', 'questions <doc_id>', 'stats', or 'quit'");
                }

                Console.WriteLine();
            }
        }
    }
}
