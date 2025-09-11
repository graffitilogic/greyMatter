using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;
using GreyMatter.Learning;

namespace GreyMatter.demos
{
    /// <summary>
    /// LLM Teacher Demo: Demonstrates dynamic learning with Ollama API guidance
    /// Shows how the local LLM acts as a teacher to guide curriculum and learning priorities
    /// Combines fast storage with intelligent curriculum management
    /// </summary>
    public class LLMTeacherDemo
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Console.WriteLine("🧠 **GREYMATTER LLM TEACHER DEMO**");
                Console.WriteLine("===================================");
                Console.WriteLine("Demonstrating dynamic learning with Ollama API teacher guidance");
                Console.WriteLine();

                var demo = new LLMTeacherDemo();
                await demo.RunDemo();

                Console.WriteLine();
                Console.WriteLine("✅ **LLM TEACHER DEMO COMPLETE**");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Demo failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return 1;
            }
        }

        public async Task RunDemo()
        {
            // Phase 1: Test LLM Teacher Connection
            await TestLLMConnection();

            // Phase 2: Create Personalized Learning Plan
            var learningPlan = await CreatePersonalizedPlan();

            // Phase 3: Simulate Learning Session with Teacher Guidance
            await SimulateLearningSession(learningPlan);

            // Phase 4: Interactive Q&A with Teacher
            await DemonstrateInteractiveTeaching();
        }

        private async Task TestLLMConnection()
        {
            Console.WriteLine("🔗 **PHASE 1: TESTING LLM TEACHER CONNECTION**");
            Console.WriteLine("-----------------------------------------------");

            try
            {
                var teacher = new LLMTeacher();
                
                // Test basic concept mapping
                var conceptMapping = await teacher.ProvideConceptualMapping("learning", 
                    new List<string> { "education", "knowledge", "brain", "memory" });

                Console.WriteLine($"✅ LLM Teacher connected successfully!");
                Console.WriteLine($"📚 Test concept mapping for 'learning':");
                Console.WriteLine($"   Category: {conceptMapping.semantic_category}");
                Console.WriteLine($"   Difficulty: {conceptMapping.difficulty_level}/10");
                Console.WriteLine($"   Abstract: {conceptMapping.is_abstract}");
                Console.WriteLine($"   Prerequisites: {string.Join(", ", conceptMapping.prerequisites)}");
                Console.WriteLine($"   Strategy: {conceptMapping.learning_strategy}");
                Console.WriteLine();

                teacher.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ LLM Teacher connection failed: {ex.Message}");
                Console.WriteLine("⚠️  Make sure Ollama is running at http://192.168.69.138:11434");
                throw;
            }
        }

        private async Task<LearningPlan> CreatePersonalizedPlan()
        {
            Console.WriteLine("📋 **PHASE 2: CREATING PERSONALIZED LEARNING PLAN**");
            Console.WriteLine("--------------------------------------------------");

            var curriculumManager = new DynamicCurriculumManager();

            var studentProfile = new LearningProfile
            {
                Name = "GreyMatter AI",
                CurrentVocabularySize = 2500,
                TargetVocabularySize = 10000,
                Interests = new List<string> { "language", "science", "technology", "literature" },
                RecentWords = new List<string> { "neural", "procedural", "cognitive", "semantic", "ephemeral" },
                PreferredLearningStyle = "progressive"
            };

            Console.WriteLine($"👤 Student Profile:");
            Console.WriteLine($"   Current vocabulary: {studentProfile.CurrentVocabularySize} words");
            Console.WriteLine($"   Target vocabulary: {studentProfile.TargetVocabularySize} words");
            Console.WriteLine($"   Interests: {string.Join(", ", studentProfile.Interests)}");
            Console.WriteLine();

            var learningPlan = await curriculumManager.CreatePersonalizedLearningPlan(studentProfile);

            Console.WriteLine($"🎯 **TEACHER'S CURRICULUM RECOMMENDATION:**");
            Console.WriteLine($"   Recommended start: {learningPlan.TeacherGuidance.recommended_start}");
            Console.WriteLine($"   Success metrics: {string.Join(", ", learningPlan.TeacherGuidance.success_metrics)}");
            Console.WriteLine($"   Total phases: {learningPlan.TeacherGuidance.phases.Count}");
            
            if (learningPlan.TeacherGuidance.phases.Count > 0)
            {
                Console.WriteLine($"   Phase details:");
                foreach (var phase in learningPlan.TeacherGuidance.phases)
                {
                    Console.WriteLine($"     - {phase.Name}: {phase.Description}");
                    Console.WriteLine($"       Data sources: {string.Join(", ", phase.DataSources)}");
                    Console.WriteLine($"       Target words: {string.Join(", ", phase.TargetWords)}");
                }
            }
            Console.WriteLine();

            curriculumManager.Dispose();
            return learningPlan;
        }

        private async Task SimulateLearningSession(LearningPlan plan)
        {
            Console.WriteLine("🚀 **PHASE 3: SIMULATED LEARNING SESSION**");
            Console.WriteLine("-----------------------------------------");

            var curriculumManager = new DynamicCurriculumManager();
            
            // Start first phase
            var session = await curriculumManager.StartLearningSession(plan, 0);

            Console.WriteLine($"📖 Active learning session started");
            Console.WriteLine($"   Phase: {session.CurrentPhase.Name}");
            Console.WriteLine($"   Description: {session.CurrentPhase.Description}");
            Console.WriteLine();

            // Simulate learning some words
            var simulatedLearning = await SimulateWordLearning(session);

            // Get teacher's evaluation
            var evaluation = await curriculumManager.EvaluateProgress(session, 
                simulatedLearning.NewWords, simulatedLearning.Metrics);

            Console.WriteLine($"📊 **TEACHER'S PROGRESS EVALUATION:**");
            Console.WriteLine($"   Next focus: {evaluation.TeacherFeedback.next_focus}");
            Console.WriteLine($"   Suggested topics: {string.Join(", ", evaluation.TeacherFeedback.suggested_topics)}");
            Console.WriteLine($"   Learning priority: {evaluation.TeacherFeedback.learning_priority}");
            Console.WriteLine($"   Ready for next phase: {evaluation.ShouldAdvancePhase}");
            Console.WriteLine();

            curriculumManager.Dispose();
        }

        private async Task<(List<string> NewWords, Dictionary<string, double> Metrics)> SimulateWordLearning(CurriculumLearningSession session)
        {
            Console.WriteLine("📚 Simulating word learning...");
            
            // Simulate learning words based on the current phase
            var newWords = session.CurrentPhase.Name.ToLower() switch
            {
                var name when name.Contains("foundation") => 
                    new List<string> { "foundation", "basic", "elementary", "simple", "core", "primary" },
                var name when name.Contains("vocabulary") => 
                    new List<string> { "vocabulary", "lexicon", "words", "terms", "language", "expression" },
                var name when name.Contains("comprehension") => 
                    new List<string> { "understand", "comprehend", "grasp", "interpret", "meaning", "context" },
                _ => new List<string> { "learning", "knowledge", "information", "data", "facts", "concepts" }
            };

            var metrics = new Dictionary<string, double>
            {
                ["words_per_minute"] = 42.5,
                ["retention_rate"] = 0.85,
                ["accuracy"] = 0.92,
                ["processing_speed"] = 1350.0  // Using our proven fast storage speed
            };

            Console.WriteLine($"   Learned {newWords.Count} new words");
            Console.WriteLine($"   Processing speed: {metrics["processing_speed"]:F0}x faster than baseline");
            Console.WriteLine();

            return (newWords, metrics);
        }

        private async Task DemonstrateInteractiveTeaching()
        {
            Console.WriteLine("💬 **PHASE 4: INTERACTIVE TEACHING DEMONSTRATION**");
            Console.WriteLine("------------------------------------------------");

            var curriculumManager = new DynamicCurriculumManager();

            var brainState = new BrainState
            {
                ActiveVocabulary = 3200,
                RecentFocus = "procedural generation and neural networks",
                KnowledgeDomains = new List<string> { "language", "AI", "cognitive science", "computer science" }
            };

            var testQuestions = new List<string>
            {
                "What is procedural generation?",
                "How does the brain learn new words?",
                "What makes this AI system different from traditional models?",
                "Can you explain neural plasticity?",
                "What should I focus on learning next?"
            };

            Console.WriteLine("🤖 Testing interactive Q&A with LLM teacher:");
            Console.WriteLine();

            foreach (var question in testQuestions)
            {
                Console.WriteLine($"❓ User: {question}");
                var response = await curriculumManager.GetTeacherResponse(question, brainState);
                Console.WriteLine();
            }

            curriculumManager.Dispose();
        }
    }
}
