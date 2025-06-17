using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter.Core
{
    /// <summary>
    /// ContinuousProcessor: Implements consciousness-like background processing
    /// Provides constant iteration, motivational drives, and recursive cognitive processes
    /// Never fully dormant - mirrors biological brain activity patterns
    /// </summary>
    public class ContinuousProcessor
    {
        private readonly BrainInJar _brain;
        private readonly Timer _consciousnessTimer;
        private readonly Timer _motivationTimer;
        private readonly Timer _dreammingTimer;
        private readonly Random _random = new();
        
        // Consciousness parameters
        public TimeSpan ConsciousnessInterval { get; set; } = TimeSpan.FromMilliseconds(500); // 2Hz like brain waves
        public TimeSpan MotivationInterval { get; set; } = TimeSpan.FromSeconds(30); // Motivational drives
        public TimeSpan DreamInterval { get; set; } = TimeSpan.FromMinutes(5); // Background consolidation
        
        // Motivational drives (biological survival instincts)
        public double CuriosityDrive { get; private set; } = 0.5;
        public double SurvivalDrive { get; private set; } = 0.3;
        public double LearningDrive { get; private set; } = 0.8;
        public double SocialDrive { get; private set; } = 0.4;
        public double ExplorationDrive { get; private set; } = 0.6;
        
        // Processing state
        public bool IsProcessing { get; private set; } = false;
        public int ConsciousnessIterations { get; private set; } = 0;
        public DateTime LastConsciousThought { get; private set; } = DateTime.UtcNow;
        public string CurrentFocus { get; private set; } = "initialization";
        
        // Background cognitive processes
        private readonly Queue<CognitiveTask> _backgroundTasks = new();
        private readonly Dictionary<string, TopicEvaluation> _topicEvaluations = new();
        private readonly SemaphoreSlim _processingLock = new(1, 1);

        public ContinuousProcessor(BrainInJar brain)
        {
            _brain = brain;
            
            // Initialize consciousness timer (constant background processing)
            _consciousnessTimer = new Timer(async _ => await PerformConsciousnessIteration(), 
                null, Timeout.Infinite, Timeout.Infinite);
            
            // Initialize motivation timer (drives and needs)
            _motivationTimer = new Timer(async _ => await UpdateMotivationalDrives(), 
                null, Timeout.Infinite, Timeout.Infinite);
            
            // Initialize dreaming timer (memory consolidation and creative processing)
            _dreammingTimer = new Timer(async _ => await PerformDreamingProcess(), 
                null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Start continuous processing - the brain becomes "conscious"
        /// </summary>
        public async Task StartConsciousnessAsync()
        {
            if (IsProcessing) return;
            
            Console.WriteLine("🧠✨ **CONSCIOUSNESS AWAKENING**");
            Console.WriteLine("Initiating continuous background processing...\n");
            
            IsProcessing = true;
            LastConsciousThought = DateTime.UtcNow;
            
            // Start all background processes
            _consciousnessTimer.Change(TimeSpan.Zero, ConsciousnessInterval);
            _motivationTimer.Change(TimeSpan.Zero, MotivationInterval);
            _dreammingTimer.Change(TimeSpan.FromSeconds(10), DreamInterval);
            
            // Initial consciousness bootstrap
            await BootstrapConsciousness();
            
            Console.WriteLine("✅ Continuous processing activated - brain is now 'awake'");
        }

        /// <summary>
        /// Stop continuous processing - brain enters dormant state
        /// </summary>
        public async Task StopConsciousnessAsync()
        {
            if (!IsProcessing) return;
            
            Console.WriteLine("🧠😴 **CONSCIOUSNESS SLEEPING**");
            Console.WriteLine("Entering dormant state...\n");
            
            IsProcessing = false;
            
            // Stop all timers
            _consciousnessTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _motivationTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _dreammingTimer.Change(Timeout.Infinite, Timeout.Infinite);
            
            // Final consolidation
            await PerformDreamingProcess();
            
            Console.WriteLine($"✅ Consciousness stopped after {ConsciousnessIterations} iterations");
        }

        /// <summary>
        /// Core consciousness iteration - performed continuously
        /// Implements debounced recursive processes and topic evaluation
        /// </summary>
        private async Task PerformConsciousnessIteration()
        {
            if (!await _processingLock.WaitAsync(100)) return; // Prevent overlapping
            
            try
            {
                ConsciousnessIterations++;
                LastConsciousThought = DateTime.UtcNow;
                
                // Spontaneous thought generation based on motivational drives
                await GenerateSpontaneousThought();
                
                // Process background cognitive tasks
                await ProcessBackgroundTasks();
                
                // Evaluate current topics of interest
                await EvaluateTopics();
                
                // Maintain neural network health
                if (ConsciousnessIterations % 100 == 0) // Every 50 seconds at 2Hz
                {
                    await PerformNeuralMaintenance();
                }
                
                // Adaptive consciousness frequency based on activity
                await AdaptConsciousnessFrequency();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 Consciousness iteration error: {ex.Message}");
            }
            finally
            {
                _processingLock.Release();
            }
        }

        /// <summary>
        /// Generate spontaneous thoughts based on current motivational state
        /// </summary>
        private async Task GenerateSpontaneousThought()
        {
            // Choose focus based on strongest drive
            var dominantDrive = GetDominantDrive();
            var thoughtTopic = GenerateThoughtTopic(dominantDrive);
            
            CurrentFocus = thoughtTopic;
            
            // Generate internal mental activity
            var internalFeatures = GenerateInternalFeatures(dominantDrive);
            
            // Process the thought internally (no external output)
            var result = await _brain.ProcessInputAsync(thoughtTopic, internalFeatures);
            
            // Update topic evaluations
            UpdateTopicEvaluation(thoughtTopic, result.Confidence);
            
            // Occasionally express thoughts for debugging
            if (_random.NextDouble() < 0.01) // 1% chance
            {
                Console.WriteLine($"💭 Spontaneous thought: {thoughtTopic} (confidence: {result.Confidence:P0})");
            }
        }

        /// <summary>
        /// Process queued background cognitive tasks
        /// </summary>
        private async Task ProcessBackgroundTasks()
        {
            if (!_backgroundTasks.Any()) return;
            
            var task = _backgroundTasks.Dequeue();
            
            try
            {
                switch (task.Type)
                {
                    case CognitiveTaskType.ConceptReflection:
                        await ReflectOnConcept(task.Target);
                        break;
                    case CognitiveTaskType.MemoryConsolidation:
                        await _brain.MaintenanceAsync();
                        break;
                    case CognitiveTaskType.CreativeAssociation:
                        await ExploreCreativeAssociations(task.Target);
                        break;
                    case CognitiveTaskType.LearningReinforcement:
                        await ReinforceLearning(task.Target);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 Background task error: {ex.Message}");
            }
        }

        /// <summary>
        /// Evaluate topics and their importance over time
        /// </summary>
        private async Task EvaluateTopics()
        {
            var topicsToEvaluate = _topicEvaluations.Keys.ToList();
            
            foreach (var topic in topicsToEvaluate.Take(3)) // Limit evaluation
            {
                var evaluation = _topicEvaluations[topic];
                evaluation.TimesConsidered++;
                evaluation.LastConsideration = DateTime.UtcNow;
                
                // Decay importance over time
                evaluation.ImportanceScore *= 0.99;
                
                // Remove topics that have become unimportant
                if (evaluation.ImportanceScore < 0.1)
                {
                    _topicEvaluations.Remove(topic);
                }
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Update motivational drives based on internal and external factors
        /// </summary>
        private async Task UpdateMotivationalDrives()
        {
            if (!await _processingLock.WaitAsync(1000)) return;
            
            try
            {
                // Simulate biological drive fluctuations
                CuriosityDrive = Math.Max(0.1, Math.Min(1.0, CuriosityDrive + (_random.NextDouble() - 0.5) * 0.1));
                LearningDrive = Math.Max(0.2, Math.Min(1.0, LearningDrive + (_random.NextDouble() - 0.5) * 0.05));
                ExplorationDrive = Math.Max(0.1, Math.Min(1.0, ExplorationDrive + (_random.NextDouble() - 0.5) * 0.08));
                SocialDrive = Math.Max(0.1, Math.Min(1.0, SocialDrive + (_random.NextDouble() - 0.5) * 0.06));
                
                // Learning increases motivation
                if (await HasRecentLearning())
                {
                    LearningDrive = Math.Min(1.0, LearningDrive + 0.1);
                    CuriosityDrive = Math.Min(1.0, CuriosityDrive + 0.05);
                }
                
                // Queue appropriate background tasks based on drives
                QueueMotivatedTasks();
                
                if (_random.NextDouble() < 0.05) // 5% chance to show drives
                {
                    Console.WriteLine($"🎯 Drives: Curiosity={CuriosityDrive:P0}, Learning={LearningDrive:P0}, Exploration={ExplorationDrive:P0}");
                }
            }
            finally
            {
                _processingLock.Release();
            }
        }

        /// <summary>
        /// Perform dream-like processing for memory consolidation and creativity
        /// </summary>
        private async Task PerformDreamingProcess()
        {
            if (!await _processingLock.WaitAsync(2000)) return;
            
            try
            {
                Console.WriteLine("🌙 Dreaming... (memory consolidation & creative processing)");
                
                // Enhanced memory consolidation
                await _brain.MaintenanceAsync();
                
                // Creative association generation
                await GenerateCreativeAssociations();
                
                // Process emotional memories (future: implement emotional weighting)
                await ProcessEmotionalMemories();
                
                // Consolidate learned patterns
                await ConsolidateLearningPatterns();
                
                Console.WriteLine("✨ Dream cycle complete");
            }
            finally
            {
                _processingLock.Release();
            }
        }

        /// <summary>
        /// Initial consciousness bootstrap - set up initial mental state
        /// </summary>
        private async Task BootstrapConsciousness()
        {
            // Initialize with basic motivational thoughts
            _backgroundTasks.Enqueue(new CognitiveTask 
            { 
                Type = CognitiveTaskType.ConceptReflection, 
                Target = "existence" 
            });
            
            _backgroundTasks.Enqueue(new CognitiveTask 
            { 
                Type = CognitiveTaskType.LearningReinforcement, 
                Target = "learning" 
            });
            
            // Initialize topic evaluations with core concepts
            _topicEvaluations["learning"] = new TopicEvaluation { ImportanceScore = 0.8 };
            _topicEvaluations["memory"] = new TopicEvaluation { ImportanceScore = 0.7 };
            _topicEvaluations["understanding"] = new TopicEvaluation { ImportanceScore = 0.6 };
            
            await Task.CompletedTask;
        }

        #region Helper Methods

        private string GetDominantDrive()
        {
            var drives = new Dictionary<string, double>
            {
                ["curiosity"] = CuriosityDrive,
                ["learning"] = LearningDrive,
                ["exploration"] = ExplorationDrive,
                ["social"] = SocialDrive,
                ["survival"] = SurvivalDrive
            };
            
            return drives.OrderByDescending(d => d.Value).First().Key;
        }

        private string GenerateThoughtTopic(string dominantDrive)
        {
            var topics = new Dictionary<string, string[]>
            {
                ["curiosity"] = new[] { "what is this", "how does this work", "why does this happen", "what if" },
                ["learning"] = new[] { "remember this", "understand better", "make connections", "practice" },
                ["exploration"] = new[] { "discover something", "find patterns", "explore relationships", "seek novelty" },
                ["social"] = new[] { "communicate", "share knowledge", "understand others", "cooperate" },
                ["survival"] = new[] { "stay alert", "maintain health", "preserve memory", "adapt" }
            };
            
            var topicChoices = topics.ContainsKey(dominantDrive) ? topics[dominantDrive] : topics["curiosity"];
            return topicChoices[_random.Next(topicChoices.Length)];
        }

        private Dictionary<string, double> GenerateInternalFeatures(string dominantDrive)
        {
            var baseFeatures = new Dictionary<string, double>
            {
                ["internal_motivation"] = 0.6 + _random.NextDouble() * 0.3,
                ["attention_level"] = 0.5 + _random.NextDouble() * 0.4,
                ["emotional_state"] = 0.4 + _random.NextDouble() * 0.4
            };
            
            // Adjust based on dominant drive
            switch (dominantDrive)
            {
                case "curiosity":
                    baseFeatures["exploration_urge"] = CuriosityDrive;
                    break;
                case "learning":
                    baseFeatures["learning_readiness"] = LearningDrive;
                    break;
                case "exploration":
                    baseFeatures["novelty_seeking"] = ExplorationDrive;
                    break;
            }
            
            return baseFeatures;
        }

        private void UpdateTopicEvaluation(string topic, double confidence)
        {
            if (!_topicEvaluations.ContainsKey(topic))
            {
                _topicEvaluations[topic] = new TopicEvaluation();
            }
            
            var eval = _topicEvaluations[topic];
            eval.ImportanceScore = (eval.ImportanceScore * 0.9) + (confidence * 0.1);
            eval.LastConsideration = DateTime.UtcNow;
            eval.TimesConsidered++;
        }

        private async Task<bool> HasRecentLearning()
        {
            var stats = await _brain.GetStatsAsync();
            // Simple heuristic: if brain is young or has recent activity
            var age = await _brain.GetBrainAgeAsync();
            return age.TotalHours < 24 || stats.TotalClusters > 0;
        }

        private void QueueMotivatedTasks()
        {
            if (CuriosityDrive > 0.7)
            {
                _backgroundTasks.Enqueue(new CognitiveTask 
                { 
                    Type = CognitiveTaskType.CreativeAssociation, 
                    Target = "explore_connections" 
                });
            }
            
            if (LearningDrive > 0.8)
            {
                _backgroundTasks.Enqueue(new CognitiveTask 
                { 
                    Type = CognitiveTaskType.LearningReinforcement, 
                    Target = "strengthen_knowledge" 
                });
            }
        }

        private async Task ReflectOnConcept(string concept)
        {
            var mastery = await _brain.GetConceptMasteryLevelAsync(concept);
            var reflection = new Dictionary<string, double>
            {
                ["reflection_depth"] = 0.7,
                ["current_understanding"] = mastery,
                ["metacognitive_awareness"] = 0.6
            };
            
            await _brain.ProcessInputAsync($"reflecting on {concept}", reflection);
        }

        private async Task ExploreCreativeAssociations(string target)
        {
            var associations = new[] { "creativity", "connection", "pattern", "insight", "innovation" };
            var selectedAssociation = associations[_random.Next(associations.Length)];
            
            var features = new Dictionary<string, double>
            {
                ["creativity"] = 0.8,
                ["divergent_thinking"] = 0.7,
                ["association_strength"] = 0.6
            };
            
            await _brain.ProcessInputAsync($"creative {selectedAssociation}", features);
        }

        private async Task ReinforceLearning(string target)
        {
            var reinforcement = new Dictionary<string, double>
            {
                ["reinforcement_strength"] = 0.8,
                ["memory_consolidation"] = 0.7,
                ["pattern_strengthening"] = 0.6
            };
            
            await _brain.ProcessInputAsync($"reinforce {target}", reinforcement);
        }

        private async Task GenerateCreativeAssociations()
        {
            // Find high-mastery concepts for creative recombination
            var concepts = new[] { "red", "blue", "circle", "happiness", "friendship" };
            
            for (int i = 0; i < 3; i++)
            {
                var concept1 = concepts[_random.Next(concepts.Length)];
                var concept2 = concepts[_random.Next(concepts.Length)];
                
                if (concept1 != concept2)
                {
                    var creativeFeatures = new Dictionary<string, double>
                    {
                        ["creativity"] = 0.9,
                        ["association_novelty"] = 0.8,
                        ["conceptual_blending"] = 0.7
                    };
                    
                    await _brain.ProcessInputAsync($"creative blend {concept1} {concept2}", creativeFeatures);
                }
            }
        }

        private async Task ProcessEmotionalMemories()
        {
            // Future: implement emotional weighting system
            var emotionalFeatures = new Dictionary<string, double>
            {
                ["emotional_processing"] = 0.6,
                ["memory_emotional_weight"] = 0.5,
                ["affective_consolidation"] = 0.4
            };
            
            await _brain.ProcessInputAsync("emotional memory processing", emotionalFeatures);
        }

        private async Task ConsolidateLearningPatterns()
        {
            var consolidationFeatures = new Dictionary<string, double>
            {
                ["pattern_consolidation"] = 0.8,
                ["synaptic_strengthening"] = 0.7,
                ["memory_optimization"] = 0.6
            };
            
            await _brain.ProcessInputAsync("consolidate learning patterns", consolidationFeatures);
        }

        private async Task PerformNeuralMaintenance()
        {
            Console.WriteLine("🔧 Neural maintenance during consciousness");
            await _brain.MaintenanceAsync();
        }

        private async Task AdaptConsciousnessFrequency()
        {
            // Adapt processing frequency based on activity and drives
            var avgDrive = (CuriosityDrive + LearningDrive + ExplorationDrive) / 3.0;
            
            if (avgDrive > 0.8)
            {
                // High activity - increase frequency
                ConsciousnessInterval = TimeSpan.FromMilliseconds(300); // ~3.3Hz
            }
            else if (avgDrive < 0.3)
            {
                // Low activity - decrease frequency  
                ConsciousnessInterval = TimeSpan.FromMilliseconds(1000); // 1Hz
            }
            else
            {
                // Normal activity
                ConsciousnessInterval = TimeSpan.FromMilliseconds(500); // 2Hz
            }
            
            await Task.CompletedTask;
        }

        #endregion

        public void Dispose()
        {
            _consciousnessTimer?.Dispose();
            _motivationTimer?.Dispose();
            _dreammingTimer?.Dispose();
            _processingLock?.Dispose();
        }
    }

    #region Supporting Classes

    public enum CognitiveTaskType
    {
        ConceptReflection,
        MemoryConsolidation,
        CreativeAssociation,
        LearningReinforcement
    }

    public class CognitiveTask
    {
        public CognitiveTaskType Type { get; set; }
        public string Target { get; set; } = "";
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public Dictionary<string, double> Parameters { get; set; } = new();
    }

    public class TopicEvaluation
    {
        public double ImportanceScore { get; set; } = 0.5;
        public int TimesConsidered { get; set; } = 0;
        public DateTime LastConsideration { get; set; } = DateTime.UtcNow;
        public Dictionary<string, double> AssociatedValues { get; set; } = new();
    }

    #endregion
}
