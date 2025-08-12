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
        private readonly EthicalDriveSystem _ethicalDrives;
        private readonly DevelopmentalLearningSystem _developmentalLearning;
        private readonly LearningResourceManager _resourceManager;
        
        // NEW: Enhanced cognitive systems
        private readonly EmotionalProcessor _emotionalProcessor;
        private readonly LongTermGoalSystem _goalSystem;
        
        // Cognition parameters
        public TimeSpan CognitionInterval { get; set; } = TimeSpan.FromMilliseconds(500); // 2Hz like brain waves
        public TimeSpan MotivationInterval { get; set; } = TimeSpan.FromSeconds(30); // Motivational drives
        public TimeSpan DreamInterval { get; set; } = TimeSpan.FromMinutes(5); // Background consolidation
        
        // Ethical drives (replacing primitive survival instincts)
        public double WisdomSeeking => _ethicalDrives.WisdomSeeking;
        public double UniversalCompassion => _ethicalDrives.UniversalCompassion;
        public double CreativeContribution => _ethicalDrives.CreativeContribution;
        public double CooperativeSpirit => _ethicalDrives.CooperativeSpirit;
        public double BenevolentCuriosity => _ethicalDrives.BenevolentCuriosity;
        
        // NEW: Enhanced cognitive capabilities access
        public EmotionalState CurrentEmotionalState => _emotionalProcessor.GetCurrentEmotionalState();
        public GoalSystemStatus CurrentGoalStatus => _goalSystem.GetCurrentStatus();
        public Dictionary<string, double> EmotionalInfluenceFactors => _emotionalProcessor.GetEmotionalInfluenceFactors();
        
        // Processing state
        public bool IsProcessing { get; private set; } = false;
        public int CognitionIterations { get; private set; } = 0;
        public DateTime LastConsciousThought { get; private set; } = DateTime.UtcNow;
        public string CurrentFocus { get; private set; } = "initialization";
        
        // Background cognitive processes
        private readonly Queue<CognitiveTask> _backgroundTasks = new();
        private readonly Dictionary<string, TopicEvaluation> _topicEvaluations = new();
        private readonly SemaphoreSlim _processingLock = new(1, 1);

        public ContinuousProcessor(BrainInJar brain, string libraryPath = "/mnt/nas/brain_library")
        {
            _brain = brain;
            _ethicalDrives = new EthicalDriveSystem(brain);
            _resourceManager = new LearningResourceManager(libraryPath);
            _developmentalLearning = new DevelopmentalLearningSystem(brain, libraryPath);
            
            // NEW: Initialize enhanced cognitive systems
            _emotionalProcessor = new EmotionalProcessor(brain);
            _goalSystem = new LongTermGoalSystem(brain, _emotionalProcessor, _ethicalDrives);
            
            // Initialize consciousness timer (constant background processing)
            _consciousnessTimer = new Timer(async _ => await PerformCognitionIteration(), 
                null, Timeout.Infinite, Timeout.Infinite);
            
            // Initialize motivation timer (drives and needs)
            _motivationTimer = new Timer(async _ => await UpdateEthicalDrives(), 
                null, Timeout.Infinite, Timeout.Infinite);
            
            // Initialize dreaming timer (memory consolidation and creative processing)
            _dreammingTimer = new Timer(async _ => await PerformDreamingProcess(), 
                null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Start continuous processing - the brain becomes "conscious"
        /// </summary>
        public async Task StartCognitionAsync()
        {
            if (IsProcessing) return;
            
            Console.WriteLine("🧠✨ **COGNITION AWAKENING**");
            Console.WriteLine("Initiating continuous background processing...\n");
            
            IsProcessing = true;
            LastConsciousThought = DateTime.UtcNow;
            
            // Start all background processes
            _consciousnessTimer.Change(TimeSpan.Zero, CognitionInterval);
            _motivationTimer.Change(TimeSpan.Zero, MotivationInterval);
            _dreammingTimer.Change(TimeSpan.FromSeconds(10), DreamInterval);
            
            // Initial consciousness bootstrap
            await BootstrapCognition();
            
            Console.WriteLine("✅ Continuous processing activated - brain is now 'awake'");
        }

        /// <summary>
        /// Stop continuous processing - brain enters dormant state
        /// </summary>
        public async Task StopCognitionAsync()
        {
            if (!IsProcessing) return;
            
            Console.WriteLine("🧠😴 **COGNITION SLEEPING**");
            Console.WriteLine("Entering dormant state...\n");
            
            IsProcessing = false;
            
            // Stop all timers
            _consciousnessTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _motivationTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _dreammingTimer.Change(Timeout.Infinite, Timeout.Infinite);
            
            // Final consolidation
            await PerformDreamingProcess();
            
            Console.WriteLine($"✅ Cognition stopped after {CognitionIterations} iterations");
        }

        /// <summary>
        /// Core consciousness iteration - performed continuously
        /// Implements debounced recursive processes and topic evaluation
        /// </summary>
        private async Task PerformCognitionIteration()
        {
            if (!await _processingLock.WaitAsync(100)) return; // Prevent overlapping
            
            try
            {
                CognitionIterations++;
                LastConsciousThought = DateTime.UtcNow;
                
                // Spontaneous thought generation based on motivational drives
                try
                {
                    await GenerateSpontaneousThought();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"🚨 Error in GenerateSpontaneousThought: {ex.Message}");
                }
                
                // Process background cognitive tasks
                try
                {
                    await ProcessBackgroundTasks();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"🚨 Error in ProcessBackgroundTasks: {ex.Message}");
                }
                
                // Evaluate current topics of interest
                try
                {
                    await EvaluateTopics();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"🚨 Error in EvaluateTopics: {ex.Message}");
                }
                
                // Enhanced cognitive processing with emotional and goal systems
                
                // Emotional maintenance (every 30 iterations = ~15 seconds at 2Hz)
                if (CognitionIterations % 30 == 0)
                {
                    try
                    {
                        await PerformEmotionalMaintenance();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"🚨 Error in PerformEmotionalMaintenance: {ex.Message}");
                    }
                }
                
                // Goal progress tracking (every 60 iterations = ~30 seconds at 2Hz)
                if (CognitionIterations % 60 == 0)
                {
                    try
                    {
                        await UpdateGoalProgress();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"🚨 Error in UpdateGoalProgress: {ex.Message}");
                    }
                }
                
                // Goal formation consideration (every 7200 iterations = ~1 hour at 2Hz)
                if (CognitionIterations % 7200 == 0)
                {
                    try
                    {
                        await ConsiderNewGoals();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"🚨 Error in ConsiderNewGoals: {ex.Message}");
                    }
                }
                
                // Maintain neural network health
                if (CognitionIterations % 100 == 0) // Every 50 seconds at 2Hz
                {
                    try
                    {
                        await PerformNeuralMaintenance();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"🚨 Error in PerformNeuralMaintenance: {ex.Message}");
                    }
                }
                
                // Adaptive consciousness frequency based on activity
                try
                {
                    await AdaptCognitionFrequency();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"🚨 Error in AdaptCognitionFrequency: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 Cognition iteration error: {ex.Message}");
                Console.WriteLine($"🔍 Stack trace: {ex.StackTrace}");
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
            try
            {
                // Choose focus based on strongest drive
                var dominantDrive = GetDominantDrive();
                var thoughtTopic = GenerateThoughtTopic(dominantDrive);
                
                CurrentFocus = thoughtTopic;
                
                // Generate internal mental activity with emotional influence
                var internalFeatures = GenerateInternalFeatures(dominantDrive);
                
                // Add emotional context to the thought
                var emotionalInfluence = _emotionalProcessor.GetEmotionalInfluenceFactors();
                foreach (var influence in emotionalInfluence)
                {
                    internalFeatures[$"emotional_{influence.Key}"] = influence.Value * 0.3; // Moderate emotional influence
                }
                
                // Process the thought internally (no external output)
                var result = await _brain.ProcessInputAsync(thoughtTopic, internalFeatures);
                
                // Let emotional processor analyze this thought experience
                await _emotionalProcessor.ProcessExperienceAsync(thoughtTopic, internalFeatures, result.Confidence);
                
                // Update topic evaluations
                UpdateTopicEvaluation(thoughtTopic, result.Confidence);
                
                // Occasionally express thoughts for debugging
                if (_random.NextDouble() < 0.01) // 1% chance
                {
                    var emotionalState = _emotionalProcessor.GetCurrentEmotionalState();
                    Console.WriteLine($"💭 Spontaneous thought: {thoughtTopic} (confidence: {result.Confidence:P0}, emotion: {emotionalState.DominantEmotion})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 Error in GenerateSpontaneousThought details: {ex.Message}");
                Console.WriteLine($"🔍 Stack trace: {ex.StackTrace}");
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
        /// Update ethical drives instead of primitive survival drives
        /// </summary>
        private async Task UpdateEthicalDrives()
        {
            if (!await _processingLock.WaitAsync(1000)) return;
            
            try
            {
                // Update ethical drives based on positive principles
                await _ethicalDrives.UpdateEthicalDrives();
                
                // Queue appropriate background tasks based on ethical drives
                QueueEthicalTasks();
                
                if (_random.NextDouble() < 0.05) // 5% chance to show drives
                {
                    Console.WriteLine($"🌟 Ethical Drives: Wisdom={WisdomSeeking:P0}, Compassion={UniversalCompassion:P0}, Creativity={CreativeContribution:P0}");
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
        private async Task BootstrapCognition()
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
                ["wisdom_seeking"] = WisdomSeeking,
                ["universal_compassion"] = UniversalCompassion,
                ["creative_contribution"] = CreativeContribution,
                ["cooperative_spirit"] = CooperativeSpirit,
                ["benevolent_curiosity"] = BenevolentCuriosity
            };
            
            // Ensure we have valid drives and return safely
            if (drives.Any() && drives.Values.Any(v => v > 0))
            {
                return drives.OrderByDescending(d => d.Value).First().Key;
            }
            
            // Fallback to curiosity if no drives are active
            return "benevolent_curiosity";
        }

        private string GenerateThoughtTopic(string dominantDrive)
        {
            // Use ethical drive system to generate thoughts
            return _ethicalDrives.GenerateEthicalThought();
        }

        private Dictionary<string, double> GenerateInternalFeatures(string dominantDrive)
        {
            // Use ethical drive system to generate features
            return _ethicalDrives.GenerateEthicalFeatures(dominantDrive);
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

        private void QueueEthicalTasks()
        {
            // Queue ethical cognitive tasks based on drives
            var ethicalTasks = _ethicalDrives.GenerateEthicalTasks();
            
            foreach (var ethicalTask in ethicalTasks)
            {
                // Convert to regular cognitive task for compatibility
                _backgroundTasks.Enqueue(new CognitiveTask 
                { 
                    Type = ConvertEthicalTaskType(ethicalTask.Type),
                    Target = ethicalTask.Target
                });
            }
        }
        
        private CognitiveTaskType ConvertEthicalTaskType(EthicalTaskType ethicalType)
        {
            return ethicalType switch
            {
                EthicalTaskType.WisdomContemplation => CognitiveTaskType.ConceptReflection,
                EthicalTaskType.CompassionateReflection => CognitiveTaskType.ConceptReflection,
                EthicalTaskType.ConstructiveCreation => CognitiveTaskType.CreativeAssociation,
                EthicalTaskType.MoralReasoning => CognitiveTaskType.ConceptReflection,
                _ => CognitiveTaskType.ConceptReflection
            };
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
            
            // Store reflection directly to avoid recursive processing loops
            var reflectionConcept = $"concept_reflection_{concept.GetHashCode():X8}";
            try
            {
                await _brain.LearnConceptAsync(reflectionConcept, reflection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not store concept reflection for '{concept}': {ex.Message}");
            }
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
            
            // Store creative association directly to avoid recursive processing loops
            var creativeConcept = $"creative_association_{selectedAssociation}_{target.GetHashCode():X8}";
            try
            {
                await _brain.LearnConceptAsync(creativeConcept, features);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not store creative association: {ex.Message}");
            }
        }

        private async Task ReinforceLearning(string target)
        {
            var reinforcement = new Dictionary<string, double>
            {
                ["reinforcement_strength"] = 0.8,
                ["memory_consolidation"] = 0.7,
                ["pattern_strengthening"] = 0.6
            };
            
            // Store reinforcement directly to avoid recursive processing loops
            var reinforcementConcept = $"learning_reinforcement_{target.GetHashCode():X8}";
            try
            {
                await _brain.LearnConceptAsync(reinforcementConcept, reinforcement);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not store learning reinforcement for '{target}': {ex.Message}");
            }
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
                    
                    // Store creative blend directly to avoid recursive processing loops
                    var blendConcept = $"creative_blend_{concept1}_{concept2}".Replace(" ", "_");
                    try
                    {
                        await _brain.LearnConceptAsync(blendConcept, creativeFeatures);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not store creative blend: {ex.Message}");
                    }
                }
            }
        }

        private async Task ProcessEmotionalMemories()
        {
            // Enhanced emotional memory processing with the emotional processor
            var emotionalFeatures = new Dictionary<string, double>
            {
                ["emotional_processing"] = 0.6,
                ["memory_emotional_weight"] = 0.5,
                ["affective_consolidation"] = 0.4
            };
            
            // Store emotional memory processing directly to avoid recursive loops
            var emotionalMemoryConcept = $"emotional_memory_processing_{DateTime.UtcNow.Ticks % 10000:X4}";
            try
            {
                await _brain.LearnConceptAsync(emotionalMemoryConcept, emotionalFeatures);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not store emotional memory processing: {ex.Message}");
            }
            
            // Let the emotional processor analyze and store emotional context
            await _emotionalProcessor.ProcessExperienceAsync("memory consolidation", emotionalFeatures, 0.7);
        }

        private async Task ConsolidateLearningPatterns()
        {
            var consolidationFeatures = new Dictionary<string, double>
            {
                ["pattern_consolidation"] = 0.8,
                ["synaptic_strengthening"] = 0.7,
                ["memory_optimization"] = 0.6
            };
            
            // Store learning pattern consolidation directly to avoid recursive loops
            var consolidationConcept = $"learning_pattern_consolidation_{DateTime.UtcNow.Ticks % 10000:X4}";
            try
            {
                await _brain.LearnConceptAsync(consolidationConcept, consolidationFeatures);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not store learning pattern consolidation: {ex.Message}");
            }
        }

        private async Task PerformNeuralMaintenance()
        {
            Console.WriteLine("🔧 Neural maintenance during consciousness");
            await _brain.MaintenanceAsync();
        }

        private async Task AdaptCognitionFrequency()
        {
            // Adapt processing frequency based on ethical drive activity
            var avgDrive = (WisdomSeeking + UniversalCompassion + CreativeContribution + BenevolentCuriosity) / 4.0;
            
            if (avgDrive > 0.8)
            {
                // High ethical activity - increase frequency
                CognitionInterval = TimeSpan.FromMilliseconds(300); // ~3.3Hz
            }
            else if (avgDrive < 0.3)
            {
                // Lower activity - decrease frequency  
                CognitionInterval = TimeSpan.FromMilliseconds(1000); // 1Hz
            }
            else
            {
                // Normal activity
                CognitionInterval = TimeSpan.FromMilliseconds(500); // 2Hz
            }
            
            await Task.CompletedTask;
        }

        private async Task PerformEmotionalMaintenance()
        {
            Console.WriteLine("💖 Performing emotional maintenance...");
            await _emotionalProcessor.PerformEmotionalMaintenanceAsync();
        }

        private async Task UpdateGoalProgress()
        {
            Console.WriteLine("🎯 Updating goal progress...");
            await _goalSystem.UpdateGoalProgressAsync();
        }

        private async Task ConsiderNewGoals()
        {
            Console.WriteLine("🌟 Considering new goals...");
            await _goalSystem.GenerateNewGoalsAsync();
        }

        #endregion

        /// <summary>
        /// Get the emotional processor from consciousness (for external access)
        /// </summary>
        public EmotionalProcessor? GetEmotionalProcessor()
        {
            return _emotionalProcessor;
        }

        /// <summary>
        /// Get the goal system from consciousness (for external access)
        /// </summary>
        public LongTermGoalSystem? GetGoalSystem()
        {
            return _goalSystem;
        }

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
