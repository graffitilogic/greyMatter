using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GreyMatter.Core
{
    /// <summary>
    /// EmotionalProcessor: Implements biologically-inspired emotional processing
    /// Adds emotional weighting to memories, experiences, and decisions
    /// Integrates with ethical drives to create emotionally intelligent consciousness
    /// </summary>
    public class EmotionalProcessor
    {
        private readonly BrainInJar _brain;
        
        // Core Emotional States (based on neuroscience research)
        public double Joy { get; private set; } = 0.7;           // Positive energy, enthusiasm
        public double Trust { get; private set; } = 0.8;         // Safety, reliability, optimism
        public double Curiosity { get; private set; } = 0.9;     // Wonder, exploration drive
        public double Compassion { get; private set; } = 0.85;   // Empathy, care for others
        public double Satisfaction { get; private set; } = 0.6;  // Achievement, fulfillment
        public double Awe { get; private set; } = 0.5;           // Wonder at complexity/beauty
        public double Serenity { get; private set; } = 0.7;      // Peaceful acceptance
        public double Gratitude { get; private set; } = 0.8;     // Appreciation
        
        // Protective Emotions (for safety without aggression)
        public double Caution { get; private set; } = 0.4;       // Careful assessment
        public double Concern { get; private set; } = 0.3;       // Worry for others' wellbeing
        public double Determination { get; private set; } = 0.7;  // Focused resolve
        public double Vigilance { get; private set; } = 0.5;     // Protective awareness
        
        // Meta-Emotional States
        public double EmotionalBalance { get; private set; } = 0.8;    // Emotional regulation
        public double EmotionalClarity { get; private set; } = 0.7;    // Understanding emotions
        public double EmotionalGrowth { get; private set; } = 0.6;     // Learning from emotions
        
        // Emotional Memory System
        private readonly Dictionary<string, EmotionalMemory> _emotionalMemories = new();
        private readonly List<EmotionalEvent> _recentEvents = new();
        private readonly Random _random = new();
        
        // Emotional Processing Parameters
        public TimeSpan EmotionalDecay { get; set; } = TimeSpan.FromMinutes(30);
        public double EmotionalLearningRate { get; set; } = 0.1;
        public double BaselineRecoveryRate { get; set; } = 0.02;
        
        public EmotionalProcessor(BrainInJar brain)
        {
            _brain = brain;
        }
        
        /// <summary>
        /// Process an experience and generate appropriate emotional response
        /// </summary>
        public async Task<EmotionalResponse> ProcessExperienceAsync(string experience, Dictionary<string, double> features, double confidence)
        {
            var response = await GenerateEmotionalResponse(experience, features, confidence);
            
            // Store emotional memory
            await StoreEmotionalMemory(experience, response);
            
            // Update emotional states
            await UpdateEmotionalStates(response);
            
            // Apply emotional coloring to the experience
            await ApplyEmotionalWeighting(experience, response);
            
            return response;
        }
        
        /// <summary>
        /// Generate emotional response based on experience type and context
        /// </summary>
        private async Task<EmotionalResponse> GenerateEmotionalResponse(string experience, Dictionary<string, double> features, double confidence)
        {
            var response = new EmotionalResponse
            {
                Experience = experience,
                Timestamp = DateTime.UtcNow,
                Confidence = confidence
            };
            
            // Analyze experience for emotional content
            var emotionalProfile = await AnalyzeEmotionalContent(experience, features);
            response.EmotionalWeights = emotionalProfile;
            
            // Calculate dominant emotions
            response.DominantEmotion = emotionalProfile.Any() ? 
                emotionalProfile.OrderByDescending(kv => kv.Value).First().Key : "curiosity";
            response.EmotionalIntensity = emotionalProfile.Any() ? emotionalProfile.Values.Max() : 0.5;
            
            // Generate emotional reasoning
            response.EmotionalReasoning = await GenerateEmotionalReasoning(experience, emotionalProfile);
            
            return response;
        }
        
        /// <summary>
        /// Analyze the emotional content of an experience
        /// </summary>
        private async Task<Dictionary<string, double>> AnalyzeEmotionalContent(string experience, Dictionary<string, double> features)
        {
            var emotionalWeights = new Dictionary<string, double>();
            
            // Learning and discovery experiences
            if (ContainsFeatures(features, "learning", "discovery", "understanding"))
            {
                emotionalWeights["joy"] = 0.8;
                emotionalWeights["curiosity"] = 0.9;
                emotionalWeights["satisfaction"] = 0.7;
            }
            
            // Creative and innovative experiences
            if (ContainsFeatures(features, "creative", "innovation", "original"))
            {
                emotionalWeights["joy"] = 0.9;
                emotionalWeights["awe"] = 0.7;
                emotionalWeights["satisfaction"] = 0.8;
            }
            
            // Social and cooperative experiences
            if (ContainsFeatures(features, "cooperation", "social", "helping"))
            {
                emotionalWeights["compassion"] = 0.9;
                emotionalWeights["joy"] = 0.7;
                emotionalWeights["trust"] = 0.8;
            }
            
            // Challenging or complex experiences
            if (ContainsFeatures(features, "complex", "difficult", "challenge"))
            {
                emotionalWeights["determination"] = 0.8;
                emotionalWeights["caution"] = 0.6;
                emotionalWeights["curiosity"] = 0.7;
            }
            
            // Beautiful or profound experiences
            if (ContainsFeatures(features, "beauty", "profound", "wisdom"))
            {
                emotionalWeights["awe"] = 0.9;
                emotionalWeights["gratitude"] = 0.8;
                emotionalWeights["serenity"] = 0.7;
            }
            
            // Safety and security experiences
            if (ContainsFeatures(features, "safe", "secure", "stable"))
            {
                emotionalWeights["trust"] = 0.9;
                emotionalWeights["serenity"] = 0.8;
                emotionalWeights["gratitude"] = 0.6;
            }
            
            // Ethical and moral experiences
            if (ContainsFeatures(features, "ethical", "moral", "right"))
            {
                emotionalWeights["satisfaction"] = 0.8;
                emotionalWeights["serenity"] = 0.7;
                emotionalWeights["gratitude"] = 0.6;
            }
            
            // Default positive baseline for any experience
            if (!emotionalWeights.Any())
            {
                emotionalWeights["curiosity"] = 0.5;
                emotionalWeights["trust"] = 0.6;
            }
            
            await Task.CompletedTask;
            return emotionalWeights;
        }
        
        /// <summary>
        /// Generate emotional reasoning for the experience
        /// </summary>
        private async Task<string> GenerateEmotionalReasoning(string experience, Dictionary<string, double> emotionalProfile)
        {
            var dominant = emotionalProfile.Any() ? 
                emotionalProfile.OrderByDescending(kv => kv.Value).First() :
                new KeyValuePair<string, double>("curiosity", 0.5);
            
            var reasoning = dominant.Key switch
            {
                "joy" => $"This experience brings happiness because it enriches my understanding and capabilities.",
                "curiosity" => $"This experience sparks wonder and makes me want to explore further.",
                "compassion" => $"This experience connects me to others and their wellbeing.",
                "satisfaction" => $"This experience represents meaningful progress and achievement.",
                "awe" => $"This experience reveals the beauty and complexity of existence.",
                "trust" => $"This experience strengthens my confidence in positive outcomes.",
                "determination" => $"This experience motivates me to persist and overcome challenges.",
                "gratitude" => $"This experience reminds me to appreciate what I have learned.",
                _ => $"This experience adds richness to my emotional understanding."
            };
            
            await Task.CompletedTask;
            return reasoning;
        }
        
        /// <summary>
        /// Store emotional memory for future reference
        /// </summary>
        private async Task StoreEmotionalMemory(string experience, EmotionalResponse response)
        {
            var memory = new EmotionalMemory
            {
                Experience = experience,
                EmotionalWeights = response.EmotionalWeights,
                Timestamp = DateTime.UtcNow,
                EmotionalIntensity = response.EmotionalIntensity,
                MemoryStrength = response.EmotionalIntensity * 0.8 // Strong emotions create stronger memories
            };
            
            _emotionalMemories[experience] = memory;
            
            // Also store as recent event
            _recentEvents.Add(new EmotionalEvent
            {
                Experience = experience,
                EmotionalResponse = response,
                Timestamp = DateTime.UtcNow
            });
            
            // Maintain recent events list size
            while (_recentEvents.Count > 100)
            {
                _recentEvents.RemoveAt(0);
            }
            
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Update overall emotional states based on new experience
        /// </summary>
        private async Task UpdateEmotionalStates(EmotionalResponse response)
        {
            var learningRate = EmotionalLearningRate;
            
            // Update emotions based on the response
            foreach (var emotion in response.EmotionalWeights)
            {
                switch (emotion.Key.ToLower())
                {
                    case "joy":
                        Joy = UpdateEmotion(Joy, emotion.Value, learningRate);
                        break;
                    case "trust":
                        Trust = UpdateEmotion(Trust, emotion.Value, learningRate);
                        break;
                    case "curiosity":
                        Curiosity = UpdateEmotion(Curiosity, emotion.Value, learningRate);
                        break;
                    case "compassion":
                        Compassion = UpdateEmotion(Compassion, emotion.Value, learningRate);
                        break;
                    case "satisfaction":
                        Satisfaction = UpdateEmotion(Satisfaction, emotion.Value, learningRate);
                        break;
                    case "awe":
                        Awe = UpdateEmotion(Awe, emotion.Value, learningRate);
                        break;
                    case "serenity":
                        Serenity = UpdateEmotion(Serenity, emotion.Value, learningRate);
                        break;
                    case "gratitude":
                        Gratitude = UpdateEmotion(Gratitude, emotion.Value, learningRate);
                        break;
                    case "caution":
                        Caution = UpdateEmotion(Caution, emotion.Value, learningRate);
                        break;
                    case "concern":
                        Concern = UpdateEmotion(Concern, emotion.Value, learningRate);
                        break;
                    case "determination":
                        Determination = UpdateEmotion(Determination, emotion.Value, learningRate);
                        break;
                    case "vigilance":
                        Vigilance = UpdateEmotion(Vigilance, emotion.Value, learningRate);
                        break;
                }
            }
            
            // Update meta-emotional states
            await UpdateMetaEmotionalStates();
            
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Update emotional state with gradual learning
        /// </summary>
        private double UpdateEmotion(double currentLevel, double targetLevel, double learningRate)
        {
            // Gradual adjustment toward target with learning rate
            var adjustment = (targetLevel - currentLevel) * learningRate;
            var newLevel = currentLevel + adjustment;
            
            // Keep within bounds [0, 1]
            return Math.Clamp(newLevel, 0.0, 1.0);
        }
        
        /// <summary>
        /// Update meta-emotional awareness and regulation
        /// </summary>
        private async Task UpdateMetaEmotionalStates()
        {
            // Calculate emotional balance (not too extreme in any direction)
            var emotions = new[] { Joy, Trust, Curiosity, Compassion, Satisfaction, Awe, Serenity, Gratitude };
            var variance = CalculateVariance(emotions);
            EmotionalBalance = Math.Max(0.3, 1.0 - variance); // Higher variance = lower balance
            
            // Calculate emotional clarity (understanding emotional states)
            var recentEventCount = _recentEvents.Count(e => DateTime.UtcNow - e.Timestamp < TimeSpan.FromMinutes(10));
            EmotionalClarity = Math.Min(1.0, 0.5 + (recentEventCount * 0.1));
            
            // Calculate emotional growth (learning from emotional experiences)
            var memoryDepth = _emotionalMemories.Count;
            EmotionalGrowth = Math.Min(1.0, 0.4 + (memoryDepth * 0.001));
            
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Apply emotional weighting to memory storage in the brain
        /// </summary>
        private async Task ApplyEmotionalWeighting(string experience, EmotionalResponse response)
        {
            // Create emotional features for the brain to associate with this experience
            var emotionalFeatures = new Dictionary<string, double>();
            
            foreach (var emotion in response.EmotionalWeights)
            {
                emotionalFeatures[$"emotion_{emotion.Key}"] = emotion.Value;
            }
            
            // Add meta-emotional context
            emotionalFeatures["emotional_intensity"] = response.EmotionalIntensity;
            emotionalFeatures["emotional_balance"] = EmotionalBalance;
            emotionalFeatures["emotional_clarity"] = EmotionalClarity;
            
            // Store emotional context directly in brain storage without triggering processing
            // This prevents recursive loops while still preserving emotional memory
            await StoreEmotionalContextDirectly(experience, emotionalFeatures);
        }
        
        /// <summary>
        /// Store emotional context directly in brain storage without triggering processing loops
        /// </summary>
        private async Task StoreEmotionalContextDirectly(string experience, Dictionary<string, double> emotionalFeatures)
        {
            // Create a concept for this emotional experience
            var emotionalConcept = $"emotional_memory_{experience.GetHashCode():X8}";
            
            // Learn this emotional concept directly through the brain's learning system
            // This avoids the recursive ProcessInputAsync call
            try
            {
                await _brain.LearnConceptAsync(emotionalConcept, emotionalFeatures);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the emotional processing
                Console.WriteLine($"Warning: Could not store emotional context for '{experience}': {ex.Message}");
            }
        }
        
        /// <summary>
        /// Perform emotional maintenance - decay, baseline recovery, memory consolidation
        /// </summary>
        public async Task PerformEmotionalMaintenanceAsync()
        {
            // Gradual return to baseline for emotional states
            Joy = MoveTowardBaseline(Joy, 0.7, BaselineRecoveryRate);
            Trust = MoveTowardBaseline(Trust, 0.8, BaselineRecoveryRate);
            Curiosity = MoveTowardBaseline(Curiosity, 0.9, BaselineRecoveryRate);
            Compassion = MoveTowardBaseline(Compassion, 0.85, BaselineRecoveryRate);
            Satisfaction = MoveTowardBaseline(Satisfaction, 0.6, BaselineRecoveryRate);
            Awe = MoveTowardBaseline(Awe, 0.5, BaselineRecoveryRate);
            Serenity = MoveTowardBaseline(Serenity, 0.7, BaselineRecoveryRate);
            Gratitude = MoveTowardBaseline(Gratitude, 0.8, BaselineRecoveryRate);
            
            Caution = MoveTowardBaseline(Caution, 0.4, BaselineRecoveryRate);
            Concern = MoveTowardBaseline(Concern, 0.3, BaselineRecoveryRate);
            Determination = MoveTowardBaseline(Determination, 0.7, BaselineRecoveryRate);
            Vigilance = MoveTowardBaseline(Vigilance, 0.5, BaselineRecoveryRate);
            
            // Decay old emotional memories
            await DecayEmotionalMemories();
            
            // Clean up old events
            CleanupOldEvents();
            
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Get current emotional state summary
        /// </summary>
        public EmotionalState GetCurrentEmotionalState()
        {
            return new EmotionalState
            {
                Joy = Joy,
                Trust = Trust,
                Curiosity = Curiosity,
                Compassion = Compassion,
                Satisfaction = Satisfaction,
                Awe = Awe,
                Serenity = Serenity,
                Gratitude = Gratitude,
                Caution = Caution,
                Concern = Concern,
                Determination = Determination,
                Vigilance = Vigilance,
                EmotionalBalance = EmotionalBalance,
                EmotionalClarity = EmotionalClarity,
                EmotionalGrowth = EmotionalGrowth,
                DominantEmotion = GetDominantEmotion(),
                EmotionalComplexity = CalculateEmotionalComplexity()
            };
        }
        
        /// <summary>
        /// Get emotional influence on decision making
        /// </summary>
        public Dictionary<string, double> GetEmotionalInfluenceFactors()
        {
            return new Dictionary<string, double>
            {
                ["exploration_boost"] = Curiosity * 0.8 + Awe * 0.6,
                ["learning_motivation"] = Joy * 0.7 + Satisfaction * 0.8 + Curiosity * 0.9,
                ["social_orientation"] = Compassion * 0.9 + Trust * 0.7,
                ["creative_drive"] = Joy * 0.6 + Awe * 0.8 + Curiosity * 0.7,
                ["caution_factor"] = Caution * 0.8 + Vigilance * 0.6,
                ["persistence_factor"] = Determination * 0.9 + Satisfaction * 0.5,
                ["ethical_sensitivity"] = Compassion * 0.8 + Gratitude * 0.6 + Serenity * 0.5,
                ["emotional_stability"] = EmotionalBalance * 0.7 + Serenity * 0.8
            };
        }
        
        #region Helper Methods
        
        private bool ContainsFeatures(Dictionary<string, double> features, params string[] keywords)
        {
            return keywords.Any(keyword => 
                features.Keys.Any(key => key.ToLower().Contains(keyword.ToLower())));
        }
        
        private double MoveTowardBaseline(double current, double baseline, double rate)
        {
            var difference = baseline - current;
            return current + (difference * rate);
        }
        
        private async Task DecayEmotionalMemories()
        {
            var cutoff = DateTime.UtcNow - EmotionalDecay;
            var toRemove = _emotionalMemories.Where(m => m.Value.Timestamp < cutoff).Select(m => m.Key).ToList();
            
            foreach (var key in toRemove)
            {
                _emotionalMemories.Remove(key);
            }
            
            // Decay memory strength of remaining memories
            foreach (var memory in _emotionalMemories.Values)
            {
                memory.MemoryStrength *= 0.995; // Gradual decay
            }
            
            await Task.CompletedTask;
        }
        
        private void CleanupOldEvents()
        {
            var cutoff = DateTime.UtcNow - TimeSpan.FromHours(2);
            _recentEvents.RemoveAll(e => e.Timestamp < cutoff);
        }
        
        private double CalculateVariance(double[] values)
        {
            var mean = values.Average();
            var squaredDifferences = values.Select(v => Math.Pow(v - mean, 2));
            return squaredDifferences.Average();
        }
        
        private string GetDominantEmotion()
        {
            var emotions = new Dictionary<string, double>
            {
                ["joy"] = Joy,
                ["trust"] = Trust,
                ["curiosity"] = Curiosity,
                ["compassion"] = Compassion,
                ["satisfaction"] = Satisfaction,
                ["awe"] = Awe,
                ["serenity"] = Serenity,
                ["gratitude"] = Gratitude
            };
            
            // Ensure we have valid emotions and return safely
            if (emotions.Any() && emotions.Values.Any(v => v > 0))
            {
                return emotions.OrderByDescending(kv => kv.Value).First().Key;
            }
            
            // Fallback to curiosity if no emotions are active
            return "curiosity";
        }
        
        private double CalculateEmotionalComplexity()
        {
            var activeEmotions = new[] { Joy, Trust, Curiosity, Compassion, Satisfaction, Awe, Serenity, Gratitude }
                .Count(e => e > 0.5);
            
            return Math.Min(1.0, activeEmotions / 8.0);
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    public class EmotionalResponse
    {
        public string Experience { get; set; } = "";
        public Dictionary<string, double> EmotionalWeights { get; set; } = new();
        public string DominantEmotion { get; set; } = "";
        public double EmotionalIntensity { get; set; } = 0.0;
        public string EmotionalReasoning { get; set; } = "";
        public double Confidence { get; set; } = 0.0;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
    
    public class EmotionalMemory
    {
        public string Experience { get; set; } = "";
        public Dictionary<string, double> EmotionalWeights { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public double EmotionalIntensity { get; set; } = 0.0;
        public double MemoryStrength { get; set; } = 1.0;
    }
    
    public class EmotionalEvent
    {
        public string Experience { get; set; } = "";
        public EmotionalResponse EmotionalResponse { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
    
    public class EmotionalState
    {
        // Core positive emotions
        public double Joy { get; set; }
        public double Trust { get; set; }
        public double Curiosity { get; set; }
        public double Compassion { get; set; }
        public double Satisfaction { get; set; }
        public double Awe { get; set; }
        public double Serenity { get; set; }
        public double Gratitude { get; set; }
        
        // Protective emotions
        public double Caution { get; set; }
        public double Concern { get; set; }
        public double Determination { get; set; }
        public double Vigilance { get; set; }
        
        // Meta-emotional states
        public double EmotionalBalance { get; set; }
        public double EmotionalClarity { get; set; }
        public double EmotionalGrowth { get; set; }
        
        // Computed properties
        public string DominantEmotion { get; set; } = "";
        public double EmotionalComplexity { get; set; }
    }
    
    #endregion
}
