using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using greyMatter.Core;
using GreyMatter.Learning;

namespace greyMatter.Learning
{
    /// <summary>
    /// Enhanced Tatoeba trainer that uses LanguageEphemeralBrain for sentence structure learning
    /// Phase 1 of the language learning roadmap: Foundation sentence pattern learning
    /// </summary>
    public class TatoebaLanguageTrainer
    {
        private readonly TatoebaReader _reader;
        private readonly LanguageEphemeralBrain _brain;
        private readonly string _dataPath;

        public LanguageEphemeralBrain Brain => _brain;

        public TatoebaLanguageTrainer(string tatoebaDataPath)
        {
            _reader = new TatoebaReader();
            _brain = new LanguageEphemeralBrain();
            _dataPath = tatoebaDataPath;
        }

        /// <summary>
        /// Train on English sentences from Tatoeba dataset
        /// Implements Phase 1: Sentence Pattern Learning
        /// </summary>
        public void TrainOnEnglishSentences(int maxSentences = 10000, int batchSize = 100)
        {
            Console.WriteLine("=== Tatoeba Language Training (Phase 1) ===");
            Console.WriteLine("Learning sentence patterns from real English sentences\n");

            var sentencesPath = Path.Combine(_dataPath, "sentences_eng_small.csv");
            if (!File.Exists(sentencesPath))
            {
                sentencesPath = Path.Combine(_dataPath, "sentences.csv");
                if (!File.Exists(sentencesPath))
                {
                    Console.WriteLine($"❌ Tatoeba data not found at {_dataPath}");
                    Console.WriteLine("Expected: sentences_eng_small.csv or sentences.csv");
                    return;
                }
            }

            Console.WriteLine($"📖 Reading sentences from: {Path.GetFileName(sentencesPath)}");
            
            var sentences = _reader.ReadEnglishSentences(sentencesPath)
                .Where(s => IsGoodLearningSentence(s))
                .Take(maxSentences)
                .ToList();

            Console.WriteLine($"✅ Found {sentences.Count:N0} suitable learning sentences");
            
            if (sentences.Count == 0)
            {
                Console.WriteLine("❌ No suitable sentences found for learning");
                return;
            }

            // Train the language brain
            var startTime = DateTime.Now;
            _brain.LearnSentencesBatch(sentences, batchSize);
            var elapsed = DateTime.Now - startTime;

            // Display learning results
            DisplayLearningResults(elapsed);
            
            // Test the learned capabilities
            TestLearnedCapabilities();
        }

        /// <summary>
        /// Train with focused vocabulary building (first 5000 most common words)
        /// </summary>
        public void TrainVocabularyFoundation(int targetVocabularySize = 5000)
        {
            Console.WriteLine($"=== Building Vocabulary Foundation (Target: {targetVocabularySize:N0} words) ===\n");

            var sentencesPath = Path.Combine(_dataPath, "sentences_eng_small.csv");
            if (!File.Exists(sentencesPath))
            {
                sentencesPath = Path.Combine(_dataPath, "sentences.csv");
            }

            if (!File.Exists(sentencesPath))
            {
                Console.WriteLine($"❌ Tatoeba data not found at {_dataPath}");
                return;
            }

            var sentences = _reader.ReadEnglishSentences(sentencesPath)
                .Where(s => IsSimpleVocabularySentence(s))
                .ToList();

            Console.WriteLine($"Processing sentences to build vocabulary...");
            
            var processedSentences = 0;
            var startTime = DateTime.Now;

            foreach (var sentence in sentences)
            {
                _brain.LearnSentence(sentence);
                processedSentences++;

                // Check if we've reached target vocabulary size
                if (_brain.VocabularySize >= targetVocabularySize)
                {
                    break;
                }

                // Progress update every 1000 sentences
                if (processedSentences % 1000 == 0)
                {
                    var elapsed = DateTime.Now - startTime;
                    Console.WriteLine($"Processed {processedSentences:N0} sentences, " +
                                    $"vocabulary: {_brain.VocabularySize:N0} words, " +
                                    $"rate: {processedSentences / elapsed.TotalSeconds:F1}/sec");
                }
            }

            var finalElapsed = DateTime.Now - startTime;
            Console.WriteLine($"\n✅ Vocabulary foundation complete!");
            Console.WriteLine($"📊 Final vocabulary size: {_brain.VocabularySize:N0} words");
            Console.WriteLine($"📊 Processed {processedSentences:N0} sentences in {finalElapsed:mm\\:ss}");
            
            DisplayTopWords();
        }

        /// <summary>
        /// Test various learned capabilities and display results
        /// </summary>
        public void TestLearnedCapabilities()
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("🧪 Testing Learned Capabilities");
            Console.WriteLine(new string('=', 60));

            // Test 1: Word prediction
            TestWordPrediction();
            
            // Test 2: Word associations
            TestWordAssociations();
            
            // Test 3: Vocabulary knowledge
            TestVocabularyKnowledge();
        }

        private void TestWordPrediction()
        {
            Console.WriteLine("\n🔮 Word Prediction Test:");
            var testSentences = new[]
            {
                "The cat _ on the mat",
                "I like to _ apples", 
                "The big dog _ fast",
                "She _ a book",
                "The sun _ bright"
            };

            foreach (var testSentence in testSentences)
            {
                var predictions = _brain.PredictMissingWord(testSentence, 3);
                Console.WriteLine($"  '{testSentence}' → {string.Join(", ", predictions)}");
            }
        }

        private void TestWordAssociations()
        {
            Console.WriteLine("\n🔗 Word Association Test:");
            var testWords = new[] { "cat", "run", "apple", "big", "happy" };

            foreach (var word in testWords)
            {
                var associations = _brain.GetWordAssociations(word, 5);
                if (associations.Any())
                {
                    Console.WriteLine($"  '{word}' → {string.Join(", ", associations)}");
                }
            }
        }

        private void TestVocabularyKnowledge()
        {
            Console.WriteLine("\n📚 Vocabulary Knowledge Test:");
            var stats = _brain.GetLearningStats();
            
            Console.WriteLine($"  Total vocabulary: {stats.VocabularySize:N0} words");
            Console.WriteLine($"  Sentences learned: {stats.LearnedSentences:N0}");
            Console.WriteLine($"  Neural concepts: {stats.TotalConcepts:N0}");
            Console.WriteLine($"  Word associations: {stats.WordAssociationCount:N0}");
            Console.WriteLine($"  Avg word frequency: {stats.AverageWordFrequency:F1}");
        }

        private void DisplayLearningResults(TimeSpan elapsed)
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("📊 Learning Results");
            Console.WriteLine(new string('=', 60));

            var stats = _brain.GetLearningStats();
            
            Console.WriteLine($"⏱️  Training time: {elapsed:mm\\:ss}");
            Console.WriteLine($"📝 Sentences learned: {stats.LearnedSentences:N0}");
            Console.WriteLine($"📚 Vocabulary size: {stats.VocabularySize:N0} words");
            Console.WriteLine($"🧠 Neural concepts: {stats.TotalConcepts:N0}");
            Console.WriteLine($"🔗 Word associations: {stats.WordAssociationCount:N0}");
            Console.WriteLine($"⚡ Learning rate: {stats.LearnedSentences / elapsed.TotalSeconds:F1} sentences/sec");
        }

        private void DisplayTopWords()
        {
            Console.WriteLine("\n📈 Most Frequent Words:");
            var topWords = _brain.GetTopWords(20);
            
            for (int i = 0; i < Math.Min(20, topWords.Count); i++)
            {
                var (word, frequency) = topWords[i];
                Console.WriteLine($"  {i + 1,2}. {word,-12} ({frequency:N0} times)");
            }
        }

        /// <summary>
        /// Filter for good learning sentences (not too short, not too complex)
        /// </summary>
        private bool IsGoodLearningSentence(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence)) return false;
            
            var wordCount = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            
            // Good learning sentences: 3-15 words, basic punctuation only
            return wordCount >= 3 && 
                   wordCount <= 15 && 
                   !sentence.Contains('"') && 
                   !sentence.Contains('(') && 
                   !sentence.Contains('[') &&
                   !sentence.Contains('{') &&
                   sentence.Length < 100; // Not too long
        }

        /// <summary>
        /// Filter for simple vocabulary building sentences
        /// </summary>
        private bool IsSimpleVocabularySentence(string sentence)
        {
            if (!IsGoodLearningSentence(sentence)) return false;
            
            var wordCount = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            
            // Even simpler sentences for vocabulary building
            return wordCount >= 3 && 
                   wordCount <= 10 && 
                   !sentence.Contains(':') && 
                   !sentence.Contains(';') &&
                   sentence.Length < 80;
        }

        /// <summary>
        /// Save the trained brain to NAS storage
        /// </summary>
        public async Task SaveTrainedBrain(string savePath = "")
        {
            if (string.IsNullOrEmpty(savePath))
            {
                savePath = "/Volumes/jarvis/brainData";
            }

            Console.WriteLine($"\n💾 Saving trained language brain to: {savePath}");
            
            // Use the ScalePersistence system like in ScaleDemo
            var persistence = new ScalePersistence(savePath);
            await persistence.SaveBrain(_brain);
            
            Console.WriteLine("✅ Brain saved successfully!");
            
            // Also save vocabulary statistics
            var stats = _brain.GetLearningStats();
            var statsPath = Path.Combine(savePath, "language_stats.txt");
            
            var statsContent = $"Language Learning Statistics\n" +
                             $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n" +
                             $"Vocabulary Size: {stats.VocabularySize:N0} words\n" +
                             $"Sentences Learned: {stats.LearnedSentences:N0}\n" +
                             $"Neural Concepts: {stats.TotalConcepts:N0}\n" +
                             $"Word Associations: {stats.WordAssociationCount:N0}\n" +
                             $"Average Word Frequency: {stats.AverageWordFrequency:F2}\n\n" +
                             $"Top 20 Most Frequent Words:\n";
            
            var topWords = _brain.GetTopWords(20);
            for (int i = 0; i < topWords.Count; i++)
            {
                var (word, frequency) = topWords[i];
                statsContent += $"{i + 1,2}. {word,-15} {frequency:N0}\n";
            }

            await File.WriteAllTextAsync(statsPath, statsContent);
            Console.WriteLine($"📊 Language statistics saved to: language_stats.txt");
        }
    }
}
