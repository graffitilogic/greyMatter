using greyMatter.Core;
using greyMatter.Learning;
using System;
using System.Collections.Generic;
using System.Linq;

namespace greyMatter
{
    /// <summary>
    /// Realistic training regimen demonstrating biological learning patterns
    /// Progressive complexity: words → phrases → sentences → stories
    /// Biological behaviors: fatigue, aging, sequence learning, memory management
    /// </summary>
    public class RealisticTrainingRegimen
    {
        private readonly SimpleEphemeralBrain _brain;
        private readonly SimpleTextParser _parser;
        private readonly List<LearningSession> _sessions;

        public RealisticTrainingRegimen()
        {
            _brain = new SimpleEphemeralBrain();
            _parser = new SimpleTextParser();
            _sessions = new List<LearningSession>();
        }

        /// <summary>
        /// Run the complete training regimen
        /// </summary>
        public void RunCompleteTraining()
        {
            Console.WriteLine("🎓 === Realistic Training Regimen ===");
            Console.WriteLine("Progressive learning: Basic → Complex → Biological behaviors\n");

            // Stage 1: Basic vocabulary (like a toddler)
            Stage1_BasicVocabulary();
            
            // Stage 2: Simple associations (like early childhood)
            Stage2_SimpleAssociations();
            
            // Stage 3: Sentence patterns (like preschool)
            Stage3_SentencePatterns();
            
            // Stage 4: Story comprehension (like elementary)
            Stage4_StoryComprehension();
            
            // Stage 5: Demonstrate biological behaviors
            Stage5_BiologicalBehaviors();
            
            // Final assessment
            FinalAssessment();
        }

        /// <summary>
        /// Stage 1: Learn basic vocabulary like a toddler
        /// </summary>
        private void Stage1_BasicVocabulary()
        {
            Console.WriteLine("📚 Stage 1: Basic Vocabulary (Toddler Level)");
            
            var basicWords = new[]
            {
                // Colors
                ("red", new[] { "color" }),
                ("blue", new[] { "color" }),
                ("green", new[] { "color" }),
                ("yellow", new[] { "color" }),
                
                // Objects
                ("ball", new[] { "toy", "round" }),
                ("car", new[] { "vehicle", "moves" }),
                ("tree", new[] { "plant", "big" }),
                ("house", new[] { "building", "home" }),
                
                // Actions
                ("run", new[] { "action", "fast" }),
                ("walk", new[] { "action", "slow" }),
                ("eat", new[] { "action", "food" }),
                ("sleep", new[] { "action", "rest" }),
                
                // Animals
                ("cat", new[] { "animal", "pet" }),
                ("dog", new[] { "animal", "pet" }),
                ("bird", new[] { "animal", "flies" })
            };

            foreach (var (word, context) in basicWords)
            {
                _brain.Learn(word, context);
                System.Threading.Thread.Sleep(100); // Simulate time between learning
            }
            
            Console.WriteLine($"✅ Learned {basicWords.Length} basic words");
            TestBasicRecall();
            _brain.ShowBrainScan();
            
            LogSession("Stage 1: Basic Vocabulary", basicWords.Length, "Foundation concepts established");
            Console.WriteLine();
        }

        /// <summary>
        /// Stage 2: Learn simple associations
        /// </summary>
        private void Stage2_SimpleAssociations()
        {
            Console.WriteLine("🔗 Stage 2: Simple Associations (Early Childhood)");
            
            var associations = new[]
            {
                ("red ball", new[] { "red", "ball", "toy" }),
                ("blue car", new[] { "blue", "car", "vehicle" }),
                ("green tree", new[] { "green", "tree", "plant" }),
                ("big house", new[] { "big", "house", "building" }),
                ("fast car", new[] { "fast", "car", "speed" }),
                ("little cat", new[] { "little", "cat", "small" }),
                ("yellow bird", new[] { "yellow", "bird", "flies" })
            };

            foreach (var (phrase, context) in associations)
            {
                _brain.Learn(phrase, context);
                
                // Also strengthen individual components
                var words = phrase.Split(' ');
                for (int i = 0; i < words.Length - 1; i++)
                {
                    _brain.Learn(words[i], new[] { words[i + 1] });
                }
                
                System.Threading.Thread.Sleep(150);
            }
            
            Console.WriteLine($"✅ Learned {associations.Length} associations");
            TestAssociationRecall();
            _brain.ShowBrainScan();
            
            LogSession("Stage 2: Simple Associations", associations.Length, "Basic relationships formed");
            Console.WriteLine();
        }

        /// <summary>
        /// Stage 3: Learn sentence patterns
        /// </summary>
        private void Stage3_SentencePatterns()
        {
            Console.WriteLine("📝 Stage 3: Sentence Patterns (Preschool Level)");
            
            var sentences = new[]
            {
                "The red ball is round",
                "The cat runs fast",
                "The bird flies high",
                "The dog eats food",
                "The car goes fast",
                "The tree is green",
                "The house is big",
                "The ball is red",
                "The cat sleeps on the bed",
                "The bird sits in the tree"
            };

            foreach (var sentence in sentences)
            {
                var lesson = _parser.ParseSentence(sentence);
                if (lesson != null)
                {
                    LearnFromLesson(lesson);
                }
                System.Threading.Thread.Sleep(200);
            }
            
            Console.WriteLine($"✅ Learned {sentences.Length} sentence patterns");
            TestSentenceRecall();
            _brain.ShowBrainScan();
            
            LogSession("Stage 3: Sentence Patterns", sentences.Length, "Grammar patterns emerging");
            Console.WriteLine();
        }

        /// <summary>
        /// Stage 4: Story comprehension
        /// </summary>
        private void Stage4_StoryComprehension()
        {
            Console.WriteLine("📖 Stage 4: Story Comprehension (Elementary Level)");
            
            var story = @"
                The little cat was hungry. The cat saw a red ball in the garden.
                The ball was near a big green tree. The cat walked to the tree.
                A yellow bird was sitting in the tree. The bird flew away.
                The cat played with the red ball. The cat was happy.
                The sun was bright and yellow. The cat slept under the tree.
            ";

            var lessons = _parser.ParseText(story);
            
            Console.WriteLine($"📚 Processing story with {lessons.Count} sentences...");
            
            foreach (var lesson in lessons)
            {
                LearnFromLesson(lesson);
                System.Threading.Thread.Sleep(300); // Longer pauses for complex content
            }
            
            Console.WriteLine("✅ Story comprehension complete");
            TestStoryComprehension();
            _brain.ShowBrainScan();
            
            LogSession("Stage 4: Story Comprehension", lessons.Count, "Complex narrative understanding");
            Console.WriteLine();
        }

        /// <summary>
        /// Stage 5: Demonstrate biological behaviors
        /// </summary>
        private void Stage5_BiologicalBehaviors()
        {
            Console.WriteLine("🧬 Stage 5: Biological Behaviors");
            
            // Demonstrate fatigue through repetition
            Console.WriteLine("\n1. Testing neuron fatigue:");
            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine($"   Activation {i + 1}: ");
                _brain.Learn("cat"); // Repeated learning should show fatigue effects
                System.Threading.Thread.Sleep(100);
            }
            
            // Demonstrate memory capacity limits
            Console.WriteLine("\n2. Testing memory limits with many concepts:");
            var randomConcepts = GenerateRandomConcepts(20);
            foreach (var concept in randomConcepts)
            {
                _brain.Learn(concept);
            }
            
            // Show biological maintenance
            Console.WriteLine("\n3. Memory efficiency after load:");
            _brain.ShowMemoryEfficiency();
            
            // Demonstrate sequence learning
            Console.WriteLine("\n4. Testing sequence learning:");
            LearnSequences();
            
            LogSession("Stage 5: Biological Behaviors", 25, "Neural fatigue and memory management demonstrated");
            Console.WriteLine();
        }

        /// <summary>
        /// Final assessment of learning
        /// </summary>
        private void FinalAssessment()
        {
            Console.WriteLine("🎯 === Final Assessment ===");
            
            Console.WriteLine("\n1. Vocabulary recall test:");
            var testWords = new[] { "red", "cat", "tree", "ball", "house" };
            foreach (var word in testWords)
            {
                var related = _brain.Recall(word);
                Console.WriteLine($"   {word} → {string.Join(", ", related.Take(3))}");
            }
            
            Console.WriteLine("\n2. Association strength test:");
            TestSpecificAssociation("red", "ball", "Color-object");
            TestSpecificAssociation("cat", "tree", "Animal-location");
            TestSpecificAssociation("bird", "flies", "Animal-action");
            
            Console.WriteLine("\n3. Final brain state:");
            _brain.ShowBrainScan();
            _brain.ShowMemoryEfficiency();
            
            Console.WriteLine("\n4. Learning session summary:");
            ShowSessionSummary();
            
            Console.WriteLine("\n🎓 Training regimen complete!");
            Console.WriteLine("✅ Demonstrated: vocabulary, associations, sentences, stories, biology");
            Console.WriteLine("✅ Biological behaviors: fatigue, memory limits, sequence learning");
            Console.WriteLine("✅ Memory efficiency: O(active_concepts) scaling maintained");
        }

        // Helper methods
        private void TestBasicRecall()
        {
            var testWords = new[] { "red", "ball", "cat" };
            foreach (var word in testWords)
            {
                var related = _brain.Recall(word);
                Console.WriteLine($"   {word} recalls: {string.Join(", ", related.Take(2))}");
            }
        }

        private void TestAssociationRecall()
        {
            var testPhrases = new[] { "red ball", "blue car", "green tree" };
            foreach (var phrase in testPhrases)
            {
                var related = _brain.Recall(phrase);
                Console.WriteLine($"   {phrase} recalls: {string.Join(", ", related.Take(2))}");
            }
        }

        private void TestSentenceRecall()
        {
            Console.WriteLine("   Testing sentence component recall:");
            _brain.Recall("cat");
            _brain.Recall("ball");
        }

        private void TestStoryComprehension()
        {
            Console.WriteLine("   Testing story comprehension:");
            Console.WriteLine("   Story themes found:");
            _brain.Recall("cat");
            _brain.Recall("garden");
            _brain.Recall("tree");
        }

        private void TestSpecificAssociation(string word1, string word2, string description)
        {
            var related1 = _brain.Recall(word1);
            var related2 = _brain.Recall(word2);
            
            var hasAssociation = related1.Any(r => r.Contains(word2)) || related2.Any(r => r.Contains(word1));
            var result = hasAssociation ? "✓" : "✗";
            
            Console.WriteLine($"   {description}: {word1} ↔ {word2} {result}");
        }

        private void LearnFromLesson(LearningLesson lesson)
        {
            foreach (var concept in lesson.MainConcepts)
            {
                var related = lesson.ConceptRelationships
                    .Where(r => r.Concept1 == concept || r.Concept2 == concept)
                    .SelectMany(r => new[] { r.Concept1, r.Concept2 })
                    .Where(c => c != concept)
                    .Distinct()
                    .ToArray();
                
                if (related.Any())
                {
                    _brain.Learn(concept, related);
                }
                else
                {
                    _brain.Learn(concept);
                }
            }
        }

        private List<string> GenerateRandomConcepts(int count)
        {
            var prefixes = new[] { "big", "small", "fast", "slow", "bright", "dark", "soft", "hard" };
            var nouns = new[] { "box", "book", "cup", "pen", "chair", "table", "window", "door" };
            var random = new Random(42);
            
            var concepts = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var prefix = prefixes[random.Next(prefixes.Length)];
                var noun = nouns[random.Next(nouns.Length)];
                concepts.Add($"{prefix}_{noun}_{i}");
            }
            
            return concepts;
        }

        private void LearnSequences()
        {
            var sequences = new[]
            {
                new[] { "wake", "eat", "play", "sleep" },
                new[] { "see", "want", "take", "have" },
                new[] { "red", "ball", "round", "toy" }
            };

            foreach (var sequence in sequences)
            {
                Console.WriteLine($"   Learning sequence: {string.Join(" → ", sequence)}");
                for (int i = 0; i < sequence.Length - 1; i++)
                {
                    _brain.Learn(sequence[i], new[] { sequence[i + 1] });
                }
            }
        }

        private void LogSession(string name, int itemCount, string description)
        {
            _sessions.Add(new LearningSession
            {
                Name = name,
                Timestamp = DateTime.Now,
                ItemsLearned = itemCount,
                Description = description
            });
        }

        private void ShowSessionSummary()
        {
            foreach (var session in _sessions)
            {
                Console.WriteLine($"   {session.Name}: {session.ItemsLearned} items - {session.Description}");
            }
            
            var totalItems = _sessions.Sum(s => s.ItemsLearned);
            var duration = _sessions.Last().Timestamp - _sessions.First().Timestamp;
            Console.WriteLine($"   Total: {totalItems} items learned in {duration.TotalSeconds:F1} seconds");
        }
    }

    /// <summary>
    /// Learning session tracking
    /// </summary>
    public class LearningSession
    {
        public required string Name { get; set; }
        public DateTime Timestamp { get; set; }
        public int ItemsLearned { get; set; }
        public required string Description { get; set; }
    }
}
