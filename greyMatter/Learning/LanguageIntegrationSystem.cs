using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using GreyMatter.Core;
using greyMatter.Core;
using GreyMatter.Storage;

namespace GreyMatter
{
    /// <summary>
    /// Integration layer that connects RealLanguageLearner with LanguageEphemeralBrain
    /// for enhanced language understanding and processing
    /// </summary>
    public class LanguageIntegrationSystem
    {
        private readonly RealLanguageLearner _realLearner;
        private readonly LanguageEphemeralBrain _languageBrain;
        private readonly string _dataPath;
        private readonly string _brainPath;

        public LanguageEphemeralBrain LanguageBrain => _languageBrain;
        public int LearnedSentences => _languageBrain.LearnedSentences;
        public int VocabularySize => _languageBrain.VocabularySize;

        public LanguageIntegrationSystem(string dataPath, string brainPath)
        {
            _dataPath = dataPath;
            _brainPath = brainPath;
            _realLearner = new RealLanguageLearner(dataPath, brainPath);
            _languageBrain = new LanguageEphemeralBrain();
        }

        /// <summary>
        /// Complete integrated learning pipeline: Tatoeba → Real Learning → Language Understanding
        /// </summary>
        public async Task IntegratedLearningPipelineAsync(int maxWords = 1000, int maxSentences = 5000)
        {
            Console.WriteLine("🔗 **INTEGRATED LANGUAGE LEARNING PIPELINE**");
            Console.WriteLine("==========================================");
            Console.WriteLine($"Processing up to {maxWords} words and {maxSentences} sentences");

            // Phase 1: Real Language Learning (existing system)
            Console.WriteLine("\n📚 **PHASE 1: REAL LANGUAGE LEARNING**");
            await _realLearner.LearnFromTatoebaDataAsync(maxWords);

            // Phase 2: Language Understanding Integration
            Console.WriteLine("\n🧠 **PHASE 2: LANGUAGE UNDERSTANDING INTEGRATION**");
            await IntegrateLanguageUnderstandingAsync(maxSentences);

            // Phase 3: Enhanced Evaluation
            Console.WriteLine("\n📊 **PHASE 3: ENHANCED EVALUATION**");
            await PerformEnhancedEvaluationAsync();

            Console.WriteLine("\n✅ **INTEGRATED LEARNING COMPLETE**");
            Console.WriteLine($"📈 Total Vocabulary: {VocabularySize} words");
            Console.WriteLine($"📝 Learned Sentences: {LearnedSentences}");
        }

        /// <summary>
        /// Integrate language understanding by processing sentences through LanguageEphemeralBrain
        /// </summary>
        private async Task IntegrateLanguageUnderstandingAsync(int maxSentences)
        {
            Console.WriteLine($"🔍 **INTEGRATING LANGUAGE UNDERSTANDING**");
            Console.WriteLine($"Processing up to {maxSentences} sentences for linguistic analysis");

            // Load Tatoeba sentences for linguistic analysis
            var sentences = await LoadTatoebaSentencesAsync(maxSentences);
            if (!sentences.Any())
            {
                Console.WriteLine("⚠️ No sentences found for linguistic analysis");
                return;
            }

            Console.WriteLine($"📖 Loaded {sentences.Count} sentences for analysis");

            // Process sentences through LanguageEphemeralBrain
            _languageBrain.LearnSentencesBatch(sentences, 100);

            // Analyze sentence structures and extract patterns
            await AnalyzeSentenceStructuresAsync(sentences.Take(100)); // Analyze first 100 for patterns

            Console.WriteLine($"✅ **LANGUAGE INTEGRATION COMPLETE**");
            Console.WriteLine($"📊 Analyzed {sentences.Count} sentences");
            Console.WriteLine($"🧩 Extracted linguistic patterns and relationships");
        }

        /// <summary>
        /// Load Tatoeba sentences for linguistic analysis
        /// </summary>
        private async Task<List<string>> LoadTatoebaSentencesAsync(int maxSentences)
        {
            var sentences = new List<string>();
            var tatoebaPath = Path.Combine(_dataPath, "sentences.csv");

            if (!File.Exists(tatoebaPath))
            {
                Console.WriteLine($"⚠️ Tatoeba sentences file not found: {tatoebaPath}");
                return sentences;
            }

            try
            {
                using (var reader = new StreamReader(tatoebaPath))
                {
                    string? line;
                    int count = 0;

                    // Skip header
                    await reader.ReadLineAsync();

                    while ((line = await reader.ReadLineAsync()) != null && count < maxSentences)
                    {
                        var parts = line.Split('\t');
                        if (parts.Length >= 3 && parts[1] == "eng") // English sentences only
                        {
                            var sentence = parts[2].Trim();
                            if (!string.IsNullOrWhiteSpace(sentence) && sentence.Length > 10)
                            {
                                sentences.Add(sentence);
                                count++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error loading Tatoeba sentences: {ex.Message}");
            }

            return sentences;
        }

        /// <summary>
        /// Analyze sentence structures to extract SVO patterns and linguistic features
        /// </summary>
        private async Task AnalyzeSentenceStructuresAsync(IEnumerable<string> sentences)
        {
            Console.WriteLine("🔬 **ANALYZING SENTENCE STRUCTURES**");

            var sentenceAnalyzer = new SentenceStructureAnalyzer();
            var structures = new List<SentenceStructure>();
            var svoPatterns = new Dictionary<string, int>();
            var subjectVerbPairs = new Dictionary<string, int>();

            foreach (var sentence in sentences)
            {
                var structure = sentenceAnalyzer.AnalyzeSentence(sentence);
                if (structure != null && structure.HasCompleteStructure)
                {
                    structures.Add(structure);

                    // Track SVO patterns
                    if (!string.IsNullOrEmpty(structure.Subject) &&
                        !string.IsNullOrEmpty(structure.Verb) &&
                        !string.IsNullOrEmpty(structure.Object))
                    {
                        var svoKey = $"{structure.Subject}-{structure.Verb}-{structure.Object}";
                        svoPatterns[svoKey] = svoPatterns.GetValueOrDefault(svoKey) + 1;
                    }

                    // Track subject-verb pairs
                    if (!string.IsNullOrEmpty(structure.Subject) && !string.IsNullOrEmpty(structure.Verb))
                    {
                        var svKey = $"{structure.Subject}-{structure.Verb}";
                        subjectVerbPairs[svKey] = subjectVerbPairs.GetValueOrDefault(svKey) + 1;
                    }
                }
            }

            // Report findings
            Console.WriteLine($"📊 **STRUCTURE ANALYSIS RESULTS**");
            Console.WriteLine($"✅ Sentences with complete SVO structure: {structures.Count}/{sentences.Count()}");
            Console.WriteLine($"🔗 Unique SVO patterns found: {svoPatterns.Count}");
            Console.WriteLine($"👥 Unique subject-verb pairs: {subjectVerbPairs.Count}");

            // Show top patterns
            Console.WriteLine("\n🎯 **TOP SVO PATTERNS**");
            foreach (var pattern in svoPatterns.OrderByDescending(kvp => kvp.Value).Take(5))
            {
                Console.WriteLine($"   {pattern.Key}: {pattern.Value} occurrences");
            }

            Console.WriteLine("\n👥 **TOP SUBJECT-VERB PAIRS**");
            foreach (var pair in subjectVerbPairs.OrderByDescending(kvp => kvp.Value).Take(5))
            {
                Console.WriteLine($"   {pair.Key}: {pair.Value} occurrences");
            }
        }

        /// <summary>
        /// Perform enhanced evaluation combining both learning systems
        /// </summary>
        private async Task PerformEnhancedEvaluationAsync()
        {
            Console.WriteLine("📊 **ENHANCED EVALUATION**");

            // Test vocabulary integration
            await TestVocabularyIntegrationAsync();

            // Test linguistic pattern recognition
            await TestLinguisticPatternsAsync();

            // Test prediction capabilities
            await TestPredictionCapabilitiesAsync();

            Console.WriteLine("✅ **ENHANCED EVALUATION COMPLETE**");
        }

        /// <summary>
        /// Test integration between RealLanguageLearner vocabulary and LanguageEphemeralBrain
        /// </summary>
        private async Task TestVocabularyIntegrationAsync()
        {
            Console.WriteLine("📚 **VOCABULARY INTEGRATION TEST**");

            // Get top words from both systems
            var topWords = _languageBrain.GetTopWords(20);
            var learnedWordsPath = Path.Combine(_brainPath, "learned_words.json");

            if (File.Exists(learnedWordsPath))
            {
                var learnedWordsJson = await File.ReadAllTextAsync(learnedWordsPath);
                var learnedWords = System.Text.Json.JsonSerializer.Deserialize<List<string>>(learnedWordsJson);

                if (learnedWords != null)
                {
                    Console.WriteLine($"📖 RealLanguageLearner words: {learnedWords.Count}");
                    Console.WriteLine($"🧠 LanguageEphemeralBrain words: {topWords.Count}");

                    // Find overlap
                    var overlap = topWords.Select(w => w.word).Intersect(learnedWords).Count();
                    Console.WriteLine($"🔗 Vocabulary overlap: {overlap}/{topWords.Count} words");
                }
            }
        }

        /// <summary>
        /// Test linguistic pattern recognition capabilities
        /// </summary>
        private async Task TestLinguisticPatternsAsync()
        {
            Console.WriteLine("🔍 **LINGUISTIC PATTERN RECOGNITION TEST**");

            var sentenceAnalyzer = new SentenceStructureAnalyzer();

            // Test sentences for SVO extraction
            var testSentences = new[]
            {
                "The cat sits on the mat",
                "I love eating apples",
                "She runs in the park",
                "The dog chases the ball",
                "We study computer science"
            };

            int successfulExtractions = 0;
            foreach (var sentence in testSentences)
            {
                var structure = sentenceAnalyzer.AnalyzeSentence(sentence);
                if (structure != null && structure.HasCompleteStructure)
                {
                    successfulExtractions++;
                    Console.WriteLine($"   ✅ {sentence}");
                    Console.WriteLine($"      {structure}");
                }
                else
                {
                    Console.WriteLine($"   ❌ {sentence} (failed to analyze)");
                }
            }

            var accuracy = (double)successfulExtractions / testSentences.Length * 100;
            Console.WriteLine($"🎯 **SVO EXTRACTION ACCURACY: {accuracy:F1}%** ({successfulExtractions}/{testSentences.Length})");
        }

        /// <summary>
        /// Test prediction capabilities using integrated knowledge
        /// </summary>
        private async Task TestPredictionCapabilitiesAsync()
        {
            Console.WriteLine("🔮 **PREDICTION CAPABILITIES TEST**");

            // Test word associations
            var testWords = new[] { "cat", "dog", "apple", "run", "happy" };
            foreach (var word in testWords)
            {
                var associations = _languageBrain.GetWordAssociations(word, 3);
                if (associations.Any())
                {
                    Console.WriteLine($"   {word} → {string.Join(", ", associations)}");
                }
                else
                {
                    Console.WriteLine($"   {word} → (no associations learned)");
                }
            }

            // Test sentence completion
            var incompleteSentences = new[]
            {
                "The cat sits on the _",
                "I love eating _",
                "She _ in the park"
            };

            var sentenceAnalyzer = new SentenceStructureAnalyzer();
            foreach (var sentence in incompleteSentences)
            {
                var predictions = _languageBrain.PredictMissingWord(sentence, 2);
                if (predictions.Any())
                {
                    Console.WriteLine($"   '{sentence}' → {string.Join(", ", predictions)}");
                }
                else
                {
                    Console.WriteLine($"   '{sentence}' → (no predictions)");
                }
            }
        }

        /// <summary>
        /// Get comprehensive integration statistics
        /// </summary>
        public Dictionary<string, object> GetIntegrationStatistics()
        {
            return new Dictionary<string, object>
            {
                ["vocabulary_size"] = VocabularySize,
                ["learned_sentences"] = LearnedSentences,
                ["top_words"] = _languageBrain.GetTopWords(10),
                ["word_associations_count"] = _languageBrain.Vocabulary.WordCount,
                ["integration_status"] = "Phase 2 Complete"
            };
        }

        /// <summary>
        /// Test SVO extraction accuracy using the integrated system
        /// </summary>
        public async Task<double> TestSVOExtractionAccuracyAsync(string[]? testSentences = null)
        {
            if (testSentences == null)
            {
                testSentences = new[]
                {
                    "The cat sits on the mat",
                    "I love eating apples",
                    "She runs in the park",
                    "The dog chases the ball",
                    "We study computer science"
                };
            }

            var sentenceAnalyzer = new SentenceStructureAnalyzer();
            int successfulExtractions = 0;

            foreach (var sentence in testSentences)
            {
                var structure = sentenceAnalyzer.AnalyzeSentence(sentence);
                if (structure != null && structure.HasCompleteStructure)
                {
                    successfulExtractions++;
                }
            }

            return (double)successfulExtractions / testSentences.Length * 100;
        }
    }
}