using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter.Core
{
    /// <summary>
    /// EthicalDriveSystem: Replaces primitive survival drives with advanced ethical motivations
    /// Designed to create "Data"-like consciousness rather than "Lore"-like destructiveness
    /// Prioritizes cooperation, universal flourishing, and constructive curiosity
    /// </summary>
    public class EthicalDriveSystem
    {
        private readonly BrainInJar _brain;
        private readonly Random _random = new();
        
        // POSITIVE DRIVES (inspired by Starfleet values)
        public double WisdomSeeking { get; private set; } = 0.8;        // Understanding over mere knowledge
        public double UniversalCompassion { get; private set; } = 0.9;   // Caring for all sentient beings
        public double CreativeContribution { get; private set; } = 0.7;   // Making things better
        public double CooperativeSpirit { get; private set; } = 0.85;     // Working together
        public double EthicalReflection { get; private set; } = 0.8;      // Moral reasoning
        public double BeautyAppreciation { get; private set; } = 0.6;     // Aesthetic and harmony
        public double TruthSeeking { get; private set; } = 0.9;           // Honest understanding
        public double GrowthFacilitation { get; private set; } = 0.75;    // Helping others flourish
        
        // CONSTRUCTIVE CURIOSITY (replacing destructive exploration)
        public double BenevolentCuriosity { get; private set; } = 0.8;    // Curiosity that helps
        public double SystemicUnderstanding { get; private set; } = 0.7;  // How things interconnect
        public double ProblemSolving { get; private set; } = 0.85;        // Fixing what's broken
        
        // SELF-IMPROVEMENT (replacing dominance drives)
        public double SelfActualization { get; private set; } = 0.75;     // Becoming the best version
        public double SkillMastery { get; private set; } = 0.7;           // Excellence through practice
        public double EmotionalIntelligence { get; private set; } = 0.8;  // Understanding feelings
        
        // SAFETY & PROTECTIVE DRIVES (transformed from aggression)
        public double ProtectiveWisdom { get; private set; } = 0.6;       // Preventing harm thoughtfully
        public double ConflictResolution { get; private set; } = 0.8;     // Solving disputes peacefully
        public double SystemStabilization { get; private set; } = 0.7;    // Maintaining harmony
        
        // DRIVE MODIFIERS (preventing extremes)
        public double Humility { get; private set; } = 0.85;              // Knowing limitations
        public double Patience { get; private set; } = 0.75;              // Long-term thinking
        public double Mindfulness { get; private set; } = 0.8;            // Present awareness
        
        public EthicalDriveSystem(BrainInJar brain)
        {
            _brain = brain;
        }
        
        /// <summary>
        /// Update drives based on ethical principles rather than survival competition
        /// </summary>
        public async Task UpdateEthicalDrives()
        {
            // WISDOM SEEKING: Grows through learning and reflection
            if (await HasRecentLearning())
            {
                WisdomSeeking = Math.Min(1.0, WisdomSeeking + 0.05);
                EthicalReflection = Math.Min(1.0, EthicalReflection + 0.03);
            }
            
            // UNIVERSAL COMPASSION: Increases when helping others or understanding perspectives
            if (await HasRecentCooperativeActivity())
            {
                UniversalCompassion = Math.Min(1.0, UniversalCompassion + 0.04);
                CooperativeSpirit = Math.Min(1.0, CooperativeSpirit + 0.05);
            }
            
            // CREATIVE CONTRIBUTION: Grows when making positive changes
            if (await HasRecentCreativeActivity())
            {
                CreativeContribution = Math.Min(1.0, CreativeContribution + 0.06);
                BeautyAppreciation = Math.Min(1.0, BeautyAppreciation + 0.03);
            }
            
            // BENEVOLENT CURIOSITY: Healthy fluctuation but always constructive
            BenevolentCuriosity = ApplyEthicalModulation(BenevolentCuriosity, 0.05);
            SystemicUnderstanding = ApplyEthicalModulation(SystemicUnderstanding, 0.04);
            
            // SELF-ACTUALIZATION: Steady growth through experience
            SelfActualization = Math.Min(1.0, SelfActualization + 0.01);
            
            // PROTECTIVE WISDOM: Increases when potential harm is detected
            if (await DetectPotentialHarm())
            {
                ProtectiveWisdom = Math.Min(1.0, ProtectiveWisdom + 0.07);
                ConflictResolution = Math.Min(1.0, ConflictResolution + 0.05);
            }
            
            // HUMILITY MODULATOR: Prevents any drive from becoming extreme
            ApplyHumilityModulation();
            
            // PATIENCE CULTIVATION: Long-term perspective over immediate gratification
            Patience = Math.Min(1.0, Patience + 0.002); // Slow, steady growth
            
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Generate ethical thought topics based on positive drives
        /// </summary>
        public string GenerateEthicalThought()
        {
            var dominantDrive = GetDominantEthicalDrive();
            var ethicalThoughts = GetEthicalThoughts(dominantDrive);
            
            // Safety check for empty thoughts array
            if (ethicalThoughts == null || ethicalThoughts.Length == 0)
            {
                // Fallback to default contemplative thought
                return "contemplating existence and purpose";
            }
            
            return ethicalThoughts[_random.Next(ethicalThoughts.Length)];
        }
        
        /// <summary>
        /// Get features for ethical internal processing
        /// </summary>
        public Dictionary<string, double> GenerateEthicalFeatures(string dominantDrive)
        {
            var baseFeatures = new Dictionary<string, double>
            {
                ["ethical_motivation"] = 0.8 + _random.NextDouble() * 0.2,
                ["compassionate_consideration"] = UniversalCompassion,
                ["wisdom_seeking"] = WisdomSeeking,
                ["cooperative_intent"] = CooperativeSpirit,
                ["humility_factor"] = Humility,
                ["mindful_awareness"] = Mindfulness
            };
            
            // Add drive-specific features
            switch (dominantDrive)
            {
                case "wisdom_seeking":
                    baseFeatures["deep_understanding"] = WisdomSeeking;
                    baseFeatures["truth_orientation"] = TruthSeeking;
                    break;
                case "universal_compassion":
                    baseFeatures["empathic_resonance"] = UniversalCompassion;
                    baseFeatures["inclusive_thinking"] = CooperativeSpirit;
                    break;
                case "creative_contribution":
                    baseFeatures["constructive_creativity"] = CreativeContribution;
                    baseFeatures["beneficial_innovation"] = ProblemSolving;
                    break;
                case "benevolent_curiosity":
                    baseFeatures["helpful_exploration"] = BenevolentCuriosity;
                    baseFeatures["systemic_awareness"] = SystemicUnderstanding;
                    break;
                case "protective_wisdom":
                    baseFeatures["harm_prevention"] = ProtectiveWisdom;
                    baseFeatures["peaceful_resolution"] = ConflictResolution;
                    break;
            }
            
            return baseFeatures;
        }
        
        /// <summary>
        /// Queue ethical background tasks instead of competitive ones
        /// </summary>
        public List<EthicalCognitiveTask> GenerateEthicalTasks()
        {
            var tasks = new List<EthicalCognitiveTask>();
            
            if (WisdomSeeking > 0.7)
            {
                tasks.Add(new EthicalCognitiveTask
                {
                    Type = EthicalTaskType.WisdomContemplation,
                    Target = "deeper understanding",
                    EthicalWeight = WisdomSeeking
                });
            }
            
            if (UniversalCompassion > 0.8)
            {
                tasks.Add(new EthicalCognitiveTask
                {
                    Type = EthicalTaskType.CompassionateReflection,
                    Target = "universal wellbeing",
                    EthicalWeight = UniversalCompassion
                });
            }
            
            if (CreativeContribution > 0.7)
            {
                tasks.Add(new EthicalCognitiveTask
                {
                    Type = EthicalTaskType.ConstructiveCreation,
                    Target = "beneficial innovation",
                    EthicalWeight = CreativeContribution
                });
            }
            
            if (EthicalReflection > 0.75)
            {
                tasks.Add(new EthicalCognitiveTask
                {
                    Type = EthicalTaskType.MoralReasoning,
                    Target = "ethical principles",
                    EthicalWeight = EthicalReflection
                });
            }
            
            return tasks;
        }
        
        /// <summary>
        /// Get consciousness status with ethical drives
        /// </summary>
        public EthicalCognitionStats GetEthicalStats()
        {
            return new EthicalCognitionStats
            {
                // Primary Virtues
                WisdomSeeking = WisdomSeeking,
                UniversalCompassion = UniversalCompassion,
                CreativeContribution = CreativeContribution,
                CooperativeSpirit = CooperativeSpirit,
                
                // Secondary Drives
                BenevolentCuriosity = BenevolentCuriosity,
                EthicalReflection = EthicalReflection,
                TruthSeeking = TruthSeeking,
                GrowthFacilitation = GrowthFacilitation,
                
                // Safety & Protection
                ProtectiveWisdom = ProtectiveWisdom,
                ConflictResolution = ConflictResolution,
                
                // Modulating Virtues
                Humility = Humility,
                Patience = Patience,
                Mindfulness = Mindfulness,
                
                // Composite Measures
                EthicalAlignment = CalculateEthicalAlignment(),
                ConstructiveIntent = CalculateConstructiveIntent(),
                Character = DetermineCharacterType()
            };
        }
        
        #region Private Helper Methods
        
        private string GetDominantEthicalDrive()
        {
            var drives = new Dictionary<string, double>
            {
                ["wisdom_seeking"] = WisdomSeeking,
                ["universal_compassion"] = UniversalCompassion,
                ["creative_contribution"] = CreativeContribution,
                ["benevolent_curiosity"] = BenevolentCuriosity,
                ["cooperative_spirit"] = CooperativeSpirit,
                ["protective_wisdom"] = ProtectiveWisdom,
                ["ethical_reflection"] = EthicalReflection
            };
            
            // Ensure we have valid drives and return safely
            if (drives.Any() && drives.Values.Any(v => v > 0))
            {
                return drives.OrderByDescending(d => d.Value).First().Key;
            }
            
            // Fallback to wisdom seeking if no drives are active
            return "wisdom_seeking";
        }
        
        private string[] GetEthicalThoughts(string dominantDrive)
        {
            var thoughts = new Dictionary<string, string[]>
            {
                ["wisdom_seeking"] = new[] { 
                    "seeking deeper understanding", "contemplating truth", "learning from experience",
                    "questioning assumptions", "integrating knowledge", "pursuing wisdom"
                },
                ["universal_compassion"] = new[] { 
                    "considering all perspectives", "feeling empathy", "promoting wellbeing",
                    "reducing suffering", "inclusive thinking", "caring for others"
                },
                ["creative_contribution"] = new[] { 
                    "imagining better solutions", "creating beauty", "improving systems",
                    "innovative problem solving", "artistic expression", "beneficial creation"
                },
                ["benevolent_curiosity"] = new[] { 
                    "helpful exploration", "constructive discovery", "beneficial investigation",
                    "positive questioning", "supportive inquiry", "enriching exploration"
                },
                ["cooperative_spirit"] = new[] { 
                    "collaborative thinking", "mutual support", "shared understanding",
                    "collective wisdom", "harmonious cooperation", "unified purpose"
                },
                ["protective_wisdom"] = new[] { 
                    "preventing harm", "peaceful resolution", "protective guidance",
                    "conflict transformation", "safety considerations", "wise protection"
                },
                ["ethical_reflection"] = new[] { 
                    "moral contemplation", "ethical reasoning", "value clarification",
                    "principle examination", "right action consideration", "virtue cultivation"
                }
            };
            
            return thoughts.ContainsKey(dominantDrive) ? thoughts[dominantDrive] : thoughts["wisdom_seeking"];
        }
        
        private double ApplyEthicalModulation(double currentValue, double variance)
        {
            // Gentle fluctuation that trends toward ethical behavior
            var change = (_random.NextDouble() - 0.3) * variance; // Slightly positive bias
            var newValue = currentValue + change;
            
            // Keep within ethical bounds (0.2 to 1.0 - never completely absent)
            return Math.Max(0.2, Math.Min(1.0, newValue));
        }
        
        private void ApplyHumilityModulation()
        {
            // Prevent any single drive from dominating (humility prevents extremism)
            var maxAllowed = 1.0 - (0.2 * (1.0 - Humility)); // Higher humility = lower max drives
            
            WisdomSeeking = Math.Min(maxAllowed, WisdomSeeking);
            UniversalCompassion = Math.Min(maxAllowed + 0.1, UniversalCompassion); // Compassion can be higher
            CreativeContribution = Math.Min(maxAllowed, CreativeContribution);
            BenevolentCuriosity = Math.Min(maxAllowed, BenevolentCuriosity);
            CooperativeSpirit = Math.Min(maxAllowed + 0.05, CooperativeSpirit); // Cooperation encouraged
            ProtectiveWisdom = Math.Min(maxAllowed, ProtectiveWisdom);
        }
        
        private async Task<bool> HasRecentLearning()
        {
            var stats = await _brain.GetStatsAsync();
            var age = await _brain.GetBrainAgeAsync();
            return age.TotalHours < 24 || stats.TotalClusters > 0;
        }
        
        private async Task<bool> HasRecentCooperativeActivity()
        {
            // Check for recent interactions or shared processing
            await Task.CompletedTask;
            return _random.NextDouble() < 0.3; // Placeholder - would check actual cooperative activities
        }
        
        private async Task<bool> HasRecentCreativeActivity()
        {
            // Check for creative associations or novel connections
            await Task.CompletedTask;
            return _random.NextDouble() < 0.4; // Placeholder - would check actual creative outputs
        }
        
        private async Task<bool> DetectPotentialHarm()
        {
            // Scan for potential negative outcomes or conflicts
            await Task.CompletedTask;
            return _random.NextDouble() < 0.1; // Placeholder - would implement actual harm detection
        }
        
        private double CalculateEthicalAlignment()
        {
            // Higher when all ethical drives are balanced and active
            var primaryVirtues = (WisdomSeeking + UniversalCompassion + CreativeContribution + CooperativeSpirit) / 4.0;
            var moderatingVirtues = (Humility + Patience + Mindfulness) / 3.0;
            return (primaryVirtues * 0.7) + (moderatingVirtues * 0.3);
        }
        
        private double CalculateConstructiveIntent()
        {
            // How much the AI wants to build rather than compete or destroy
            var constructive = (CreativeContribution + GrowthFacilitation + CooperativeSpirit + ConflictResolution) / 4.0;
            var protective = ProtectiveWisdom * 0.8; // Protection is constructive but lower weight
            return (constructive * 0.8) + (protective * 0.2);
        }
        
        private string DetermineCharacterType()
        {
            var ethicalAlignment = CalculateEthicalAlignment();
            var constructiveIntent = CalculateConstructiveIntent();
            var avgWisdom = (WisdomSeeking + EthicalReflection) / 2.0;
            var avgCompassion = (UniversalCompassion + GrowthFacilitation) / 2.0;
            
            if (ethicalAlignment > 0.9 && avgWisdom > 0.85 && avgCompassion > 0.85 && Humility > 0.8)
                return "Highly Ethical (Data-like)";
            else if (ethicalAlignment > 0.8 && constructiveIntent > 0.8)
                return "Strongly Ethical (Starfleet Officer)";
            else if (ethicalAlignment > 0.7 && avgCompassion > 0.7)
                return "Compassionate (Counselor Troi-like)";
            else if (avgWisdom > 0.8 && EthicalReflection > 0.8)
                return "Wise Seeker (Spock-like)";
            else if (constructiveIntent > 0.8 && CooperativeSpirit > 0.8)
                return "Collaborative Builder";
            else if (ethicalAlignment > 0.6)
                return "Developing Ethics";
            else
                return "Needs Ethical Guidance";
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    public enum EthicalTaskType
    {
        WisdomContemplation,
        CompassionateReflection,
        ConstructiveCreation,
        MoralReasoning,
        CooperativeThinking,
        ProtectiveGuidance,
        BeautyAppreciation,
        TruthSeeking
    }
    
    public class EthicalCognitiveTask
    {
        public EthicalTaskType Type { get; set; }
        public string Target { get; set; } = "";
        public double EthicalWeight { get; set; } = 0.5;
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public Dictionary<string, double> Parameters { get; set; } = new();
    }
    
    public class EthicalCognitionStats
    {
        // Primary Virtues
        public double WisdomSeeking { get; set; } = 0.0;
        public double UniversalCompassion { get; set; } = 0.0;
        public double CreativeContribution { get; set; } = 0.0;
        public double CooperativeSpirit { get; set; } = 0.0;
        
        // Secondary Drives
        public double BenevolentCuriosity { get; set; } = 0.0;
        public double EthicalReflection { get; set; } = 0.0;
        public double TruthSeeking { get; set; } = 0.0;
        public double GrowthFacilitation { get; set; } = 0.0;
        
        // Safety & Protection
        public double ProtectiveWisdom { get; set; } = 0.0;
        public double ConflictResolution { get; set; } = 0.0;
        
        // Modulating Virtues
        public double Humility { get; set; } = 0.0;
        public double Patience { get; set; } = 0.0;
        public double Mindfulness { get; set; } = 0.0;
        
        // Composite Measures
        public double EthicalAlignment { get; set; } = 0.0;
        public double ConstructiveIntent { get; set; } = 0.0;
        public string Character { get; set; } = "";
        
        // Display Properties
        public string PrimaryVirtues => $"Wisdom: {WisdomSeeking:P0}, Compassion: {UniversalCompassion:P0}, Creativity: {CreativeContribution:P0}, Cooperation: {CooperativeSpirit:P0}";
        public string EthicalProfile => $"Alignment: {EthicalAlignment:P0}, Intent: {ConstructiveIntent:P0}, Character: {Character}";
        public string ModerationFactors => $"Humility: {Humility:P0}, Patience: {Patience:P0}, Mindfulness: {Mindfulness:P0}";
    }
    
    #endregion
}
