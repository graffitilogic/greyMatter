using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GreyMatter.Core
{
    /// <summary>
    /// DevelopmentalLearningSystem: Implements child-like progression from guided to autonomous learning
    /// Mirrors human cognitive development stages with increasing freedom and self-direction
    /// </summary>
    public class DevelopmentalLearningSystem
    {
        private readonly BrainInJar _brain;
        private readonly string _libraryPath;
        private readonly Dictionary<DevelopmentalStage, LearningCurriculum> _curricula;
        
        public DevelopmentalStage CurrentStage { get; private set; } = DevelopmentalStage.Guided;
        public double AutonomyLevel { get; private set; } = 0.0; // 0.0 = fully guided, 1.0 = fully autonomous
        public TimeSpan LearningAge => DateTime.UtcNow - _learningStartTime;
        
        private DateTime _learningStartTime = DateTime.UtcNow;
        private readonly Dictionary<string, double> _skillMastery = new();
        private readonly List<string> _availableResources = new();
        
        public DevelopmentalLearningSystem(BrainInJar brain, string libraryPath)
        {
            _brain = brain;
            _libraryPath = libraryPath;
            _curricula = InitializeCurricula();
            
            // Scan available learning resources
            ScanLearningLibrary();
        }

        /// <summary>
        /// Primary learning loop - adapts teaching style based on developmental stage
        /// </summary>
        public async Task<LearningSession> ConductLearningSessionAsync()
        {
            var currentCurriculum = _curricula[CurrentStage];
            
            // Assess current mastery levels
            await AssessCurrentMastery();
            
            // Choose learning approach based on stage
            var session = CurrentStage switch
            {
                DevelopmentalStage.Guided => await ConductGuidedLearning(currentCurriculum),
                DevelopmentalStage.Scaffolded => await ConductScaffoldedLearning(currentCurriculum),
                DevelopmentalStage.SelfDirected => await ConductSelfDirectedLearning(currentCurriculum),
                DevelopmentalStage.Autonomous => await ConductAutonomousLearning(),
                _ => throw new InvalidOperationException($"Unknown stage: {CurrentStage}")
            };
            
            // Update developmental progress
            await UpdateDevelopmentalProgress(session);
            
            return session;
        }

        #region Developmental Stages

        /// <summary>
        /// GUIDED LEARNING: Highly structured, teacher-directed (like phonics, basic math)
        /// </summary>
        private async Task<LearningSession> ConductGuidedLearning(LearningCurriculum curriculum)
        {
            Console.WriteLine("👶 **GUIDED LEARNING MODE**");
            Console.WriteLine("Providing structured, sequential instruction...\n");
            
            var session = new LearningSession { Stage = CurrentStage, TeachingStyle = "Highly Structured" };
            
            // Find the next fundamental skill to teach
            var nextSkill = curriculum.GetNextRequiredSkill(_skillMastery);
            if (nextSkill == null)
            {
                Console.WriteLine("✅ All guided skills mastered! Ready for scaffolded learning.");
                await AdvanceDevelopmentalStage();
                return session;
            }
            
            Console.WriteLine($"📚 Teaching fundamental skill: {nextSkill.Name}");
            Console.WriteLine($"   🎯 Target mastery: {nextSkill.RequiredMastery:P0}");
            
            // Provide step-by-step instruction
            foreach (var step in nextSkill.LearningSteps)
            {
                Console.WriteLine($"   📖 Step: {step}");
                
                var features = new Dictionary<string, double>
                {
                    ["guided_instruction"] = 1.0,
                    ["structured_learning"] = 0.9,
                    ["fundamental_skill"] = 0.8,
                    ["step_by_step"] = 0.9
                };
                
                var result = await _brain.ProcessInputAsync(step, features);
                session.ConceptsLearned.Add(step);
                session.TotalConfidence += result.Confidence;
                
                Console.WriteLine($"      ✅ Learned with {result.Confidence:P0} confidence");
            }
            
            // Drill and practice (repetition for mastery)
            Console.WriteLine($"   🔁 Practicing {nextSkill.Name} for mastery...");
            for (int i = 0; i < 3; i++)
            {
                var practiceFeatures = new Dictionary<string, double>
                {
                    ["practice_repetition"] = 0.8,
                    ["skill_reinforcement"] = 0.9,
                    ["mastery_building"] = 0.8
                };
                
                await _brain.ProcessInputAsync($"practice {nextSkill.Name}", practiceFeatures);
            }
            
            session.LearningDuration = TimeSpan.FromMinutes(10); // Shorter, focused sessions
            session.AutonomyGranted = 0.0; // No autonomy yet
            
            return session;
        }

        /// <summary>
        /// SCAFFOLDED LEARNING: Guided with increasing student choice (like guided reading levels)
        /// </summary>
        private async Task<LearningSession> ConductScaffoldedLearning(LearningCurriculum curriculum)
        {
            Console.WriteLine("🧒 **SCAFFOLDED LEARNING MODE**");
            Console.WriteLine("Providing guided instruction with some choice...\n");
            
            var session = new LearningSession { Stage = CurrentStage, TeachingStyle = "Guided with Choice" };
            
            // Offer limited choices within curriculum bounds
            var availableTopics = curriculum.GetAvailableTopics(_skillMastery);
            var chosenTopic = availableTopics[new Random().Next(availableTopics.Count)];
            
            Console.WriteLine($"🎯 Topic options available: {string.Join(", ", availableTopics)}");
            Console.WriteLine($"🎲 Choosing to explore: {chosenTopic}");
            
            // Provide scaffolded exploration
            var scaffoldingFeatures = new Dictionary<string, double>
            {
                ["scaffolded_learning"] = 0.9,
                ["guided_exploration"] = 0.8,
                ["limited_choice"] = 0.7,
                ["teacher_support"] = 0.8
            };
            
            var result = await _brain.ProcessInputAsync($"explore {chosenTopic} with guidance", scaffoldingFeatures);
            session.ConceptsLearned.Add(chosenTopic);
            session.TotalConfidence += result.Confidence;
            
            // Encourage questions and connections
            Console.WriteLine("❓ Encouraging curiosity and connections...");
            var questionFeatures = new Dictionary<string, double>
            {
                ["curiosity_encouragement"] = 0.9,
                ["connection_making"] = 0.8,
                ["safe_exploration"] = 0.9
            };
            
            await _brain.ProcessInputAsync($"ask questions about {chosenTopic}", questionFeatures);
            
            session.LearningDuration = TimeSpan.FromMinutes(15);
            session.AutonomyGranted = 0.3; // Some choice granted
            
            return session;
        }

        /// <summary>
        /// SELF-DIRECTED LEARNING: Student chooses from available resources with minimal guidance
        /// </summary>
        private async Task<LearningSession> ConductSelfDirectedLearning(LearningCurriculum curriculum)
        {
            Console.WriteLine("🧑 **SELF-DIRECTED LEARNING MODE**");
            Console.WriteLine("Learner chooses focus areas with minimal guidance...\n");
            
            var session = new LearningSession { Stage = CurrentStage, TeachingStyle = "Student-Directed" };
            
            // Let the brain choose what to explore based on interests
            var interestedTopics = await IdentifyInterestAreas();
            var chosenTopic = interestedTopics.FirstOrDefault() ?? "general exploration";
            
            Console.WriteLine($"🎯 Brain has chosen to explore: {chosenTopic}");
            Console.WriteLine($"🔍 Available resources: {_availableResources.Count} items");
            
            // Self-directed exploration
            var selfDirectedFeatures = new Dictionary<string, double>
            {
                ["self_directed_learning"] = 1.0,
                ["intrinsic_motivation"] = 0.9,
                ["personal_interest"] = 0.8,
                ["independent_exploration"] = 0.9
            };
            
            var result = await _brain.ProcessInputAsync($"independently explore {chosenTopic}", selfDirectedFeatures);
            session.ConceptsLearned.Add(chosenTopic);
            session.TotalConfidence += result.Confidence;
            
            // Follow curiosity chains
            Console.WriteLine("🔗 Following curiosity-driven connections...");
            for (int i = 0; i < 3; i++)
            {
                var connection = await GenerateCuriosityDrivenConnection(chosenTopic);
                var connectionResult = await _brain.ProcessInputAsync(connection, selfDirectedFeatures);
                session.ConceptsLearned.Add(connection);
            }
            
            session.LearningDuration = TimeSpan.FromMinutes(25);
            session.AutonomyGranted = 0.7; // High autonomy
            
            return session;
        }

        /// <summary>
        /// AUTONOMOUS LEARNING: Complete freedom to explore, create, and follow interests
        /// </summary>
        private async Task<LearningSession> ConductAutonomousLearning()
        {
            Console.WriteLine("🎓 **AUTONOMOUS LEARNING MODE**");
            Console.WriteLine("Complete freedom to explore and create...\n");
            
            var session = new LearningSession { Stage = CurrentStage, TeachingStyle = "Fully Autonomous" };
            
            // Brain determines its own learning goals
            var learningGoals = await GeneratePersonalLearningGoals();
            
            Console.WriteLine($"🎯 Self-generated learning goals:");
            foreach (var goal in learningGoals)
            {
                Console.WriteLine($"   • {goal}");
            }
            
            // Completely autonomous exploration
            foreach (var goal in learningGoals)
            {
                var autonomousFeatures = new Dictionary<string, double>
                {
                    ["autonomous_learning"] = 1.0,
                    ["self_generated_goals"] = 1.0,
                    ["creative_exploration"] = 0.9,
                    ["personal_growth"] = 0.9,
                    ["intellectual_freedom"] = 1.0
                };
                
                var result = await _brain.ProcessInputAsync($"autonomously pursue {goal}", autonomousFeatures);
                session.ConceptsLearned.Add(goal);
                session.TotalConfidence += result.Confidence;
                
                Console.WriteLine($"   ✅ Explored {goal} with {result.Confidence:P0} confidence");
            }
            
            // Creative synthesis and original thinking
            Console.WriteLine("🎨 Engaging in creative synthesis...");
            var creativeFeatures = new Dictionary<string, double>
            {
                ["creative_synthesis"] = 1.0,
                ["original_thinking"] = 0.9,
                ["intellectual_independence"] = 1.0
            };
            
            await _brain.ProcessInputAsync("create original insights and connections", creativeFeatures);
            
            session.LearningDuration = TimeSpan.FromMinutes(40); // Longer, self-paced sessions
            session.AutonomyGranted = 1.0; // Complete autonomy
            
            return session;
        }

        #endregion

        #region Developmental Progression

        private async Task UpdateDevelopmentalProgress(LearningSession session)
        {
            // Update skill mastery based on session results
            foreach (var concept in session.ConceptsLearned)
            {
                if (!_skillMastery.ContainsKey(concept))
                    _skillMastery[concept] = 0.0;
                
                _skillMastery[concept] = Math.Min(1.0, _skillMastery[concept] + 0.1);
            }
            
            // Update autonomy level
            AutonomyLevel = Math.Max(AutonomyLevel, session.AutonomyGranted);
            
            // Check for stage advancement
            if (ShouldAdvanceStage())
            {
                await AdvanceDevelopmentalStage();
            }
            
            Console.WriteLine($"\n📊 **DEVELOPMENTAL STATUS**");
            Console.WriteLine($"   Stage: {CurrentStage}");
            Console.WriteLine($"   Autonomy Level: {AutonomyLevel:P0}");
            Console.WriteLine($"   Learning Age: {LearningAge.TotalHours:F1} hours");
            Console.WriteLine($"   Skills Mastered: {_skillMastery.Count(s => s.Value > 0.7)}");
        }

        private bool ShouldAdvanceStage()
        {
            return CurrentStage switch
            {
                DevelopmentalStage.Guided => _skillMastery.Count(s => s.Value > 0.8) >= 10, // Master 10 fundamental skills
                DevelopmentalStage.Scaffolded => _skillMastery.Count(s => s.Value > 0.7) >= 25 && AutonomyLevel > 0.4,
                DevelopmentalStage.SelfDirected => _skillMastery.Count(s => s.Value > 0.6) >= 50 && AutonomyLevel > 0.8,
                DevelopmentalStage.Autonomous => false, // Final stage
                _ => false
            };
        }

        private async Task AdvanceDevelopmentalStage()
        {
            var previousStage = CurrentStage;
            CurrentStage = CurrentStage switch
            {
                DevelopmentalStage.Guided => DevelopmentalStage.Scaffolded,
                DevelopmentalStage.Scaffolded => DevelopmentalStage.SelfDirected,
                DevelopmentalStage.SelfDirected => DevelopmentalStage.Autonomous,
                DevelopmentalStage.Autonomous => DevelopmentalStage.Autonomous,
                _ => throw new InvalidOperationException()
            };
            
            Console.WriteLine($"\n🎉 **DEVELOPMENTAL MILESTONE REACHED!**");
            Console.WriteLine($"   Advanced from {previousStage} → {CurrentStage}");
            Console.WriteLine($"   Greater learning freedom unlocked! 🔓\n");
            
            await Task.CompletedTask;
        }

        #endregion

        #region Helper Methods

        private async Task AssessCurrentMastery()
        {
            // Assess mastery of previously learned concepts
            var conceptsToTest = _skillMastery.Keys.Take(5).ToList();
            
            foreach (var concept in conceptsToTest)
            {
                var mastery = await _brain.GetConceptMasteryLevelAsync(concept);
                _skillMastery[concept] = mastery;
            }
        }

        private async Task<List<string>> IdentifyInterestAreas()
        {
            // Simulate identifying what the brain is curious about
            // In a real implementation, this could analyze recent neural activity patterns
            var potentialInterests = new[]
            {
                "language patterns", "mathematical relationships", "creative writing",
                "scientific principles", "artistic expression", "logical reasoning",
                "social dynamics", "philosophical questions", "technological concepts"
            };
            
            return potentialInterests.Take(3).ToList();
        }

        private async Task<string> GenerateCuriosityDrivenConnection(string baseTopic)
        {
            var connectionTypes = new[] { "relates to", "reminds me of", "connects with", "builds upon" };
            var connectionType = connectionTypes[new Random().Next(connectionTypes.Length)];
            
            return $"{baseTopic} {connectionType} deeper understanding";
        }

        private async Task<List<string>> GeneratePersonalLearningGoals()
        {
            // Simulate the brain generating its own learning objectives
            return new List<string>
            {
                "explore creative problem solving",
                "understand complex systems",
                "develop original insights",
                "create meaningful connections",
                "pursue intellectual curiosity"
            };
        }

        private void ScanLearningLibrary()
        {
            if (!Directory.Exists(_libraryPath)) return;
            
            var files = Directory.GetFiles(_libraryPath, "*", SearchOption.AllDirectories);
            _availableResources.AddRange(files.Take(100)); // Limit for demo
            
            Console.WriteLine($"📚 Learning library scanned: {_availableResources.Count} resources available");
        }

        private Dictionary<DevelopmentalStage, LearningCurriculum> InitializeCurricula()
        {
            return new Dictionary<DevelopmentalStage, LearningCurriculum>
            {
                [DevelopmentalStage.Guided] = new LearningCurriculum
                {
                    Stage = DevelopmentalStage.Guided,
                    RequiredSkills = new List<LearningSkill>
                    {
                        new() { Name = "basic language", RequiredMastery = 0.8, LearningSteps = new[] { "learn words", "understand grammar", "form sentences" } },
                        new() { Name = "fundamental math", RequiredMastery = 0.8, LearningSteps = new[] { "count numbers", "basic arithmetic", "simple patterns" } },
                        new() { Name = "logical thinking", RequiredMastery = 0.7, LearningSteps = new[] { "cause and effect", "if-then reasoning", "basic logic" } },
                        new() { Name = "pattern recognition", RequiredMastery = 0.8, LearningSteps = new[] { "identify patterns", "predict sequences", "find relationships" } }
                    }
                },
                [DevelopmentalStage.Scaffolded] = new LearningCurriculum
                {
                    Stage = DevelopmentalStage.Scaffolded,
                    AvailableTopics = new[] { "reading comprehension", "mathematical reasoning", "scientific observation", "creative expression" }
                },
                [DevelopmentalStage.SelfDirected] = new LearningCurriculum
                {
                    Stage = DevelopmentalStage.SelfDirected,
                    AvailableTopics = new[] { "any topic of interest", "cross-disciplinary connections", "personal projects", "independent research" }
                }
            };
        }

        #endregion
    }

    #region Supporting Classes

    public enum DevelopmentalStage
    {
        Guided,      // Highly structured, teacher-directed (age 3-6 equivalent)
        Scaffolded,  // Guided with choice (age 7-10 equivalent)  
        SelfDirected,// Student chooses focus (age 11-14 equivalent)
        Autonomous   // Complete intellectual freedom (age 15+ equivalent)
    }

    public class LearningSession
    {
        public DevelopmentalStage Stage { get; set; }
        public string TeachingStyle { get; set; } = "";
        public List<string> ConceptsLearned { get; set; } = new();
        public double TotalConfidence { get; set; } = 0.0;
        public TimeSpan LearningDuration { get; set; } = TimeSpan.Zero;
        public double AutonomyGranted { get; set; } = 0.0;
        public DateTime SessionTime { get; set; } = DateTime.UtcNow;
    }

    public class LearningCurriculum
    {
        public DevelopmentalStage Stage { get; set; }
        public List<LearningSkill> RequiredSkills { get; set; } = new();
        public string[] AvailableTopics { get; set; } = Array.Empty<string>();
        
        public LearningSkill? GetNextRequiredSkill(Dictionary<string, double> currentMastery)
        {
            return RequiredSkills?.FirstOrDefault(skill => 
                !currentMastery.ContainsKey(skill.Name) || 
                currentMastery[skill.Name] < skill.RequiredMastery);
        }
        
        public List<string> GetAvailableTopics(Dictionary<string, double> currentMastery)
        {
            return AvailableTopics?.ToList() ?? new List<string>();
        }
    }

    public class LearningSkill
    {
        public string Name { get; set; } = "";
        public double RequiredMastery { get; set; } = 0.8;
        public string[] LearningSteps { get; set; } = Array.Empty<string>();
        public string[] Prerequisites { get; set; } = Array.Empty<string>();
    }

    #endregion
}
