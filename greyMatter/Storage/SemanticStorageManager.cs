using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using greyMatter.Core;


namespace GreyMatter.Storage
{
    /// <summary>
    /// Biologically-inspired storage manager with shared neurons across cortical columns
    /// Implements hippocampus-style sparse indexing + global neuron pool + semantic clustering
    /// 
    /// Architecture:
    /// - Global SharedNeuronPool: All neurons stored centrally with distributed access
    /// - Cortical Columns: Concepts clustered semantically for efficient loading
    /// - Neural Pointers: Concepts reference neurons by ID, enabling shared representations
    /// - Hippocampus Index: Sparse routing to find neurons and concepts quickly
    /// </summary>
    public class SemanticStorageManager
    {
        private readonly string _brainDataPath;
        private readonly string _trainingDataRoot;
        private readonly string _hippocampusPath;
        private readonly string _corticalColumnsPath;
        private readonly string _workingMemoryPath;
        private readonly string _neuronPoolPath;
        
        // Public property for training data root
        public string TrainingDataRoot => _trainingDataRoot;
        
        // Hippocampus indices for sparse routing
        private Dictionary<string, string> _vocabularyIndex;
        private Dictionary<string, ConceptIndexEntry> _conceptIndex;
        private Dictionary<int, NeuronLocationEntry> _neuronLocationIndex;
        private Dictionary<string, DateTime> _clusterAccessTimes;
        
        // Global neuron pool management
        private Dictionary<int, SharedNeuron> _neuronPool;
        private int _nextNeuronId;
        
        // Pre-trained semantic classifier for learning-based categorization
        private PreTrainedSemanticClassifier? _preTrainedClassifier;
        private FallbackSemanticClassifier _fallbackClassifier;
        
        public SemanticStorageManager(string brainDataPath, string trainingDataRoot = "/Volumes/jarvis/trainData")
        {
            _brainDataPath = brainDataPath;
            _trainingDataRoot = trainingDataRoot;
            _hippocampusPath = Path.Combine(_brainDataPath, "hippocampus");
            _corticalColumnsPath = Path.Combine(_brainDataPath, "cortical_columns");
            _workingMemoryPath = Path.Combine(_brainDataPath, "working_memory");
            _neuronPoolPath = Path.Combine(_brainDataPath, "neuron_pool");
            
            _vocabularyIndex = new Dictionary<string, string>();
            _conceptIndex = new Dictionary<string, ConceptIndexEntry>();
            _neuronLocationIndex = new Dictionary<int, NeuronLocationEntry>();
            _clusterAccessTimes = new Dictionary<string, DateTime>();
            _neuronPool = new Dictionary<int, SharedNeuron>();
            
            // Initialize semantic classifiers
            _fallbackClassifier = new FallbackSemanticClassifier();
            
            // Try to initialize pre-trained classifier (fallback to rule-based if model not available)
            try
            {
                _preTrainedClassifier = new PreTrainedSemanticClassifier(this);
                Console.WriteLine("✅ Pre-trained semantic classifier initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to load pre-trained classifier: {ex.Message}");
                Console.WriteLine("🔄 Using fallback rule-based classifier");
            }
            
            InitializeStorageStructure();
            InitializeNeuronPool();
        }
        
        /// <summary>
        /// Create the biological storage directory structure
        /// </summary>
        private void InitializeStorageStructure()
        {
            // Hippocampus - sparse indices
            Directory.CreateDirectory(_hippocampusPath);
            
            // Cortical columns - semantic clustering based on Huth's semantic brain map
            // Following the ~24 major semantic domains identified in cortical organization research
            var corticalDomains = new[]
            {
                // === CONCRETE SEMANTIC DOMAINS ===
                
                // Living Things
                "semantic_domains/living_things/animals/mammals",
                "semantic_domains/living_things/animals/birds", 
                "semantic_domains/living_things/animals/fish_marine",
                "semantic_domains/living_things/animals/insects",
                "semantic_domains/living_things/plants/trees",
                "semantic_domains/living_things/plants/flowers",
                "semantic_domains/living_things/humans/body_parts",
                "semantic_domains/living_things/humans/family_relations",
                
                // Artifacts & Objects
                "semantic_domains/artifacts/tools_instruments",
                "semantic_domains/artifacts/vehicles/land_vehicles", 
                "semantic_domains/artifacts/vehicles/watercraft",
                "semantic_domains/artifacts/vehicles/aircraft",
                "semantic_domains/artifacts/buildings_structures",
                "semantic_domains/artifacts/clothing_textiles",
                "semantic_domains/artifacts/food_nutrition",
                "semantic_domains/artifacts/technology_electronics",
                "semantic_domains/artifacts/weapons_military",
                
                // Natural World
                "semantic_domains/natural_world/geography/landforms",
                "semantic_domains/natural_world/geography/water_bodies", 
                "semantic_domains/natural_world/weather_climate",
                "semantic_domains/natural_world/materials_substances",
                "semantic_domains/natural_world/colors_visual",
                
                // === ABSTRACT SEMANTIC DOMAINS ===
                
                // Mental/Cognitive
                "semantic_domains/mental_cognitive/emotions_feelings",
                "semantic_domains/mental_cognitive/thoughts_ideas",
                "semantic_domains/mental_cognitive/knowledge_learning", 
                "semantic_domains/mental_cognitive/memory_perception",
                
                // Social/Communication
                "semantic_domains/social_communication/language_speech",
                "semantic_domains/social_communication/social_relations",
                "semantic_domains/social_communication/cultural_practices",
                "semantic_domains/social_communication/politics_government",
                
                // Actions/Events
                "semantic_domains/actions_events/physical_motion",
                "semantic_domains/actions_events/mental_actions",
                "semantic_domains/actions_events/social_interactions",
                "semantic_domains/actions_events/work_occupations",
                
                // Properties/Attributes  
                "semantic_domains/properties/spatial_relations",
                "semantic_domains/properties/temporal_relations",
                "semantic_domains/properties/quantity_measurement", 
                "semantic_domains/properties/quality_evaluation",
                
                // === LANGUAGE STRUCTURE DOMAINS ===
                
                // Grammatical Categories
                "language_structures/grammatical/verbs/action_verbs",
                "language_structures/grammatical/verbs/mental_verbs", 
                "language_structures/grammatical/verbs/motion_verbs",
                "language_structures/grammatical/nouns/concrete_nouns",
                "language_structures/grammatical/nouns/abstract_nouns",
                "language_structures/grammatical/adjectives/descriptive",
                "language_structures/grammatical/adjectives/evaluative",
                "language_structures/grammatical/function_words",
                
                // Syntactic Patterns
                "language_structures/syntactic/sentence_patterns",
                "language_structures/syntactic/phrase_structures",
                "language_structures/syntactic/grammatical_relations",
                
                // === EPISODIC & CONTEXTUAL ===
                "episodic_memories/personal_experiences",
                "episodic_memories/cultural_contexts",
                "episodic_memories/temporal_sequences"
            };
            
            foreach (var domain in corticalDomains)
            {
                Directory.CreateDirectory(Path.Combine(_corticalColumnsPath, domain));
            }
            
            // Working memory - active concepts
            Directory.CreateDirectory(_workingMemoryPath);
            
            // Neuron pool storage
            Directory.CreateDirectory(_neuronPoolPath);
        }
        
        /// <summary>
        /// Initialize global neuron pool from disk or create fresh
        /// </summary>
        private void InitializeNeuronPool()
        {
            var poolIndexPath = Path.Combine(_neuronPoolPath, "pool_index.json");
            
            if (File.Exists(poolIndexPath))
            {
                // Load existing neuron pool index
                var poolData = File.ReadAllText(poolIndexPath);
                var poolIndex = JsonSerializer.Deserialize<Dictionary<string, object>>(poolData);
                
                if (poolIndex.ContainsKey("next_neuron_id"))
                {
                    _nextNeuronId = ((JsonElement)poolIndex["next_neuron_id"]).GetInt32();
                }
                
                // Load neuron location index for routing
                LoadNeuronLocationIndex();
            }
            else
            {
                _nextNeuronId = 1;
                SaveNeuronPoolIndex();
            }
        }
        
        /// <summary>
        /// Load the neuron location index for routing neurons to their storage files
        /// </summary>
        private void LoadNeuronLocationIndex()
        {
            var locationIndexPath = Path.Combine(_hippocampusPath, "neuron_locations.json");
            if (File.Exists(locationIndexPath))
            {
                var indexData = File.ReadAllText(locationIndexPath);
                var locations = JsonSerializer.Deserialize<Dictionary<string, NeuronLocationEntry>>(indexData);
                
                _neuronLocationIndex.Clear();
                foreach (var kvp in locations)
                {
                    if (int.TryParse(kvp.Key, out int neuronId))
                    {
                        _neuronLocationIndex[neuronId] = kvp.Value;
                    }
                }
            }
        }
        
        /// <summary>
        /// Save neuron pool metadata
        /// </summary>
        private void SaveNeuronPoolIndex()
        {
            var poolData = new Dictionary<string, object>
            {
                ["next_neuron_id"] = _nextNeuronId,
                ["pool_size"] = _neuronPool.Count,
                ["last_updated"] = DateTime.UtcNow.ToString("O")
            };
            
            var poolIndexPath = Path.Combine(_neuronPoolPath, "pool_index.json");
            File.WriteAllText(poolIndexPath, JsonSerializer.Serialize(poolData, new JsonSerializerOptions { WriteIndented = true }));
            
            // Save neuron location index
            var locationIndexPath = Path.Combine(_hippocampusPath, "neuron_locations.json");
            var locationData = _neuronLocationIndex.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
            File.WriteAllText(locationIndexPath, JsonSerializer.Serialize(locationData, new JsonSerializerOptions { WriteIndented = true }));
        }
        
        /// <summary>
        /// Determine the storage cluster for a vocabulary word
        /// Uses semantic analysis and frequency to route to appropriate cortical column
        /// </summary>
        public string GetVocabularyCluster(string word, WordInfo wordInfo)
        {
            var semanticDomain = DetermineSemanticDomain(word, wordInfo);
            var wordType = DetermineWordType(word, wordInfo);
            var frequencyBand = GetFrequencyBand(wordInfo.Frequency);
            
            return Path.Combine(_corticalColumnsPath, 
                $"{semanticDomain}/{wordType}/freq_{frequencyBand}.json");
        }
        
        /// <summary>
        /// Determine storage cluster for a neural concept
        /// Routes based on concept type and semantic hash
        /// </summary>
        public string GetConceptCluster(string conceptId, ConceptType conceptType)
        {
            var semanticHash = GetSemanticHash(conceptId);
            var typeFolder = GetConceptTypeFolder(conceptType);
            
            // Use first 2 chars of hash for balanced distribution
            var hashPrefix = semanticHash.Substring(0, 2);
            
            return Path.Combine(_corticalColumnsPath,
                $"{typeFolder}/{hashPrefix}/{semanticHash}.json");
        }
        
        /// <summary>
        /// Save vocabulary word to appropriate semantic cluster
        /// </summary>
        public async Task SaveVocabularyWordAsync(string word, WordInfo wordInfo)
        {
            var clusterPath = GetVocabularyCluster(word, wordInfo);
            
            // Load existing cluster or create new
            var cluster = await LoadVocabularyClusterAsync(clusterPath) ?? new VocabularyCluster();
            
            // Add/update word in cluster
            cluster.Words[word] = wordInfo;
            cluster.LastModified = DateTime.UtcNow;
            
            // Save cluster
            await SaveVocabularyClusterAsync(clusterPath, cluster);
            
            // Update hippocampus index
            await UpdateVocabularyIndexAsync(word, clusterPath);
        }
        
        /// <summary>
        /// Save neural concept to appropriate cluster
        /// </summary>
        public async Task SaveConceptAsync(string conceptId, object conceptData, ConceptType conceptType)
        {
            var clusterPath = GetConceptCluster(conceptId, conceptType);
            
            // Load existing concept cluster
            var cluster = await LoadConceptClusterAsync(clusterPath) ?? new ConceptCluster();
            
            // Add concept to cluster
            cluster.Concepts[conceptId] = conceptData;
            cluster.LastModified = DateTime.UtcNow;
            
            // Save cluster
            await SaveConceptClusterAsync(clusterPath, cluster);
            
            // Update hippocampus concept index
            await UpdateConceptIndexAsync(conceptId, clusterPath, conceptType);
        }
        
        /// <summary>
        /// Load vocabulary word using hippocampus routing
        /// </summary>
        public async Task<WordInfo?> LoadVocabularyWordAsync(string word)
        {
            // Check hippocampus index first
            if (!_vocabularyIndex.ContainsKey(word))
            {
                await LoadVocabularyIndexAsync();
            }
            
            if (!_vocabularyIndex.TryGetValue(word, out var clusterPath))
            {
                return null; // Word not found
            }
            
            // Load cluster lazily
            var cluster = await LoadVocabularyClusterAsync(clusterPath);
            return cluster?.Words.GetValueOrDefault(word);
        }
        
        /// <summary>
        /// Load multiple vocabulary words efficiently (batch loading)
        /// </summary>
        public async Task<Dictionary<string, WordInfo>> LoadVocabularyBatchAsync(IEnumerable<string> words)
        {
            var result = new Dictionary<string, WordInfo>();
            var clusterGroups = new Dictionary<string, List<string>>();
            
            // Ensure vocabulary index is loaded
            if (_vocabularyIndex.Count == 0)
            {
                await LoadVocabularyIndexAsync();
            }
            
            // Group words by cluster for batch loading
            foreach (var word in words)
            {
                if (_vocabularyIndex.TryGetValue(word, out var clusterPath))
                {
                    if (!clusterGroups.ContainsKey(clusterPath))
                    {
                        clusterGroups[clusterPath] = new List<string>();
                    }
                    clusterGroups[clusterPath].Add(word);
                }
            }
            
            // Load each cluster once and extract requested words
            foreach (var (clusterPath, wordsInCluster) in clusterGroups)
            {
                var cluster = await LoadVocabularyClusterAsync(clusterPath);
                if (cluster != null)
                {
                    foreach (var word in wordsInCluster)
                    {
                        if (cluster.Words.TryGetValue(word, out var wordInfo))
                        {
                            result[word] = wordInfo;
                        }
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Semantic domain classification for biological clustering
        /// Uses pre-trained semantic embeddings when available, fallback to rule-based classification
        /// Based on Huth's semantic brain mapping research with hierarchical organization
        /// </summary>
        private string DetermineSemanticDomain(string word, WordInfo wordInfo)
        {
            // Try pre-trained classifier first
            if (_preTrainedClassifier != null)
            {
                try
                {
                    var domain = _preTrainedClassifier.ClassifySemanticDomain(word);
                    if (domain != "semantic_domains/general_concepts")
                    {
                        return domain.Replace("semantic_domains/", "");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Pre-trained classifier error: {ex.Message}");
                }
            }
            
            // Fallback to rule-based classification
            var fallbackDomain = _fallbackClassifier.ClassifySemanticDomain(word);
            if (fallbackDomain != "semantic_domains/general_concepts")
            {
                return fallbackDomain.Replace("semantic_domains/", "");
            }
            
            // Final fallback to original rule-based logic
            return DetermineSemanticDomainRuleBased(word, wordInfo);
        }

        /// <summary>
        /// Original rule-based semantic domain classification as fallback
        /// </summary>
        private string DetermineSemanticDomainRuleBased(string word, WordInfo wordInfo)
        {
            var lowerWord = word.ToLowerInvariant();
            
            // === LIVING THINGS DOMAIN ===
            
            // Animals - with subcategorization
            if (IsMammal(lowerWord)) return "semantic_domains/living_things/animals/mammals";
            if (IsBird(lowerWord)) return "semantic_domains/living_things/animals/birds";
            if (IsFishOrMarine(lowerWord)) return "semantic_domains/living_things/animals/fish_marine";
            if (IsInsect(lowerWord)) return "semantic_domains/living_things/animals/insects";
            
            // Plants
            if (IsTree(lowerWord)) return "semantic_domains/living_things/plants/trees";
            if (IsFlowerOrPlant(lowerWord)) return "semantic_domains/living_things/plants/flowers";
            
            // Human-related
            if (IsBodyPart(lowerWord)) return "semantic_domains/living_things/humans/body_parts";
            if (IsFamilyRelation(lowerWord)) return "semantic_domains/living_things/humans/family_relations";
            
            // === ARTIFACTS & OBJECTS DOMAIN ===
            
            // Vehicles - hierarchical
            if (IsLandVehicle(lowerWord)) return "semantic_domains/artifacts/vehicles/land_vehicles";
            if (IsWatercraft(lowerWord)) return "semantic_domains/artifacts/vehicles/watercraft";
            if (IsAircraft(lowerWord)) return "semantic_domains/artifacts/vehicles/aircraft";
            
            // Tools and objects
            if (IsToolOrInstrument(lowerWord)) return "semantic_domains/artifacts/tools_instruments";
            if (IsBuildingOrStructure(lowerWord)) return "semantic_domains/artifacts/buildings_structures";
            if (IsClothingOrTextile(lowerWord)) return "semantic_domains/artifacts/clothing_textiles";
            if (IsFoodOrNutrition(lowerWord)) return "semantic_domains/artifacts/food_nutrition";
            if (IsTechnologyOrElectronics(lowerWord)) return "semantic_domains/artifacts/technology_electronics";
            if (IsWeaponOrMilitary(lowerWord)) return "semantic_domains/artifacts/weapons_military";
            
            // === NATURAL WORLD DOMAIN ===
            
            // Geography
            if (IsLandform(lowerWord)) return "semantic_domains/natural_world/geography/landforms";
            if (IsWaterBody(lowerWord)) return "semantic_domains/natural_world/geography/water_bodies";
            if (IsWeatherOrClimate(lowerWord)) return "semantic_domains/natural_world/weather_climate";
            if (IsMaterialOrSubstance(lowerWord)) return "semantic_domains/natural_world/materials_substances";
            if (IsColorOrVisual(lowerWord)) return "semantic_domains/natural_world/colors_visual";
            
            // === ABSTRACT DOMAINS ===
            
            // Mental/Cognitive
            if (IsEmotionOrFeeling(lowerWord)) return "semantic_domains/mental_cognitive/emotions_feelings";
            if (IsThoughtOrIdea(lowerWord)) return "semantic_domains/mental_cognitive/thoughts_ideas";
            if (IsKnowledgeOrLearning(lowerWord)) return "semantic_domains/mental_cognitive/knowledge_learning";
            if (IsMemoryOrPerception(lowerWord)) return "semantic_domains/mental_cognitive/memory_perception";
            
            // Social/Communication
            if (IsLanguageOrSpeech(lowerWord)) return "semantic_domains/social_communication/language_speech";
            if (IsSocialRelation(lowerWord)) return "semantic_domains/social_communication/social_relations";
            if (IsCulturalPractice(lowerWord)) return "semantic_domains/social_communication/cultural_practices";
            if (IsPoliticsOrGovernment(lowerWord)) return "semantic_domains/social_communication/politics_government";
            
            // Actions/Events
            if (IsPhysicalMotion(lowerWord)) return "semantic_domains/actions_events/physical_motion";
            if (IsMentalAction(lowerWord)) return "semantic_domains/actions_events/mental_actions";
            if (IsSocialInteraction(lowerWord)) return "semantic_domains/actions_events/social_interactions";
            if (IsWorkOrOccupation(lowerWord)) return "semantic_domains/actions_events/work_occupations";
            
            // Properties/Attributes
            if (IsSpatialRelation(lowerWord)) return "semantic_domains/properties/spatial_relations";
            if (IsTemporalRelation(lowerWord)) return "semantic_domains/properties/temporal_relations";
            if (IsQuantityOrMeasurement(lowerWord)) return "semantic_domains/properties/quantity_measurement";
            if (IsQualityOrEvaluation(lowerWord)) return "semantic_domains/properties/quality_evaluation";
            
            // === LANGUAGE STRUCTURE FALLBACK ===
            
            // Grammatical categorization based on word type
            return wordInfo.EstimatedType switch
            {
                WordType.Verb => DetermineVerbSubcategory(lowerWord),
                WordType.Noun => DetermineNounSubcategory(lowerWord), 
                WordType.Adjective => DetermineAdjectiveSubcategory(lowerWord),
                _ => "language_structures/grammatical/function_words"
            };
        }
        
        private string DetermineWordType(string word, WordInfo wordInfo)
        {
            // Analyze grammatical patterns from usage
            var estimatedType = wordInfo.EstimatedType.ToString().ToLowerInvariant();
            
            return estimatedType switch
            {
                "verb" => "verbs",
                "noun" => "nouns", 
                "adjective" => "adjectives",
                "adverb" => "adverbs",
                "preposition" => "prepositions",
                _ => "mixed" // Unknown or multiple types
            };
        }
        
        private string GetFrequencyBand(int frequency)
        {
            return frequency switch
            {
                >= 1000 => "high",
                >= 100 => "medium", 
                >= 10 => "low",
                _ => "rare"
            };
        }
        
        private string GetSemanticHash(string concept)
        {
            // Create semantic hash for balanced distribution
            var hash = concept.GetHashCode();
            return Math.Abs(hash).ToString("x8");
        }
        
        private string GetConceptTypeFolder(ConceptType conceptType)
        {
            return conceptType switch
            {
                ConceptType.SentencePattern => "language_structures/syntactic/sentence_patterns",
                ConceptType.WordAssociation => "semantic_domains/social_communication/language_speech/associations", 
                ConceptType.SemanticRelation => "semantic_domains/properties/semantic_relations",
                ConceptType.EpisodicMemory => "episodic_memories/personal_experiences",
                _ => "language_structures/syntactic/general"
            };
        }
        
        // === LIVING THINGS CLASSIFICATION ===
        
        private bool IsMammal(string word) => 
            new[] { "cat", "dog", "horse", "cow", "sheep", "pig", "elephant", "lion", "tiger", "bear", 
                   "wolf", "deer", "rabbit", "mouse", "rat", "monkey", "human", "person", "man", "woman", "child" }.Any(word.Contains);
        
        private bool IsBird(string word) =>
            new[] { "bird", "eagle", "hawk", "owl", "robin", "sparrow", "crow", "chicken", "duck", "goose", 
                   "turkey", "pigeon", "parrot", "penguin", "ostrich", "swan", "flamingo" }.Any(word.Contains);
        
        private bool IsFishOrMarine(string word) =>
            new[] { "fish", "shark", "whale", "dolphin", "salmon", "tuna", "cod", "bass", "trout", 
                   "octopus", "squid", "crab", "lobster", "shrimp", "jellyfish", "starfish" }.Any(word.Contains);
        
        private bool IsInsect(string word) =>
            new[] { "insect", "bug", "bee", "ant", "fly", "mosquito", "spider", "butterfly", "moth", 
                   "beetle", "cricket", "grasshopper", "wasp", "termite" }.Any(word.Contains);
        
        private bool IsTree(string word) =>
            new[] { "tree", "oak", "pine", "maple", "birch", "cedar", "willow", "palm", "fir", 
                   "forest", "woods", "branch", "trunk", "bark", "leaf", "leaves" }.Any(word.Contains);
        
        private bool IsFlowerOrPlant(string word) =>
            new[] { "flower", "plant", "rose", "tulip", "daisy", "sunflower", "lily", "orchid", 
                   "grass", "bush", "shrub", "vine", "garden", "bloom", "petal" }.Any(word.Contains);
        
        private bool IsBodyPart(string word) =>
            new[] { "head", "face", "eye", "ear", "nose", "mouth", "hand", "arm", "leg", "foot", 
                   "finger", "toe", "heart", "brain", "lung", "stomach", "back", "chest", "shoulder" }.Any(word.Contains);
        
        private bool IsFamilyRelation(string word) =>
            new[] { "family", "mother", "father", "parent", "child", "son", "daughter", "brother", "sister", 
                   "grandmother", "grandfather", "aunt", "uncle", "cousin", "spouse", "husband", "wife" }.Any(word.Contains);
        
        // === ARTIFACTS & OBJECTS CLASSIFICATION ===
        
        private bool IsLandVehicle(string word) =>
            new[] { "car", "truck", "bus", "motorcycle", "bicycle", "train", "subway", "taxi", 
                   "van", "suv", "vehicle", "automobile", "scooter", "tractor" }.Any(word.Contains);
        
        private bool IsWatercraft(string word) =>
            new[] { "boat", "ship", "yacht", "canoe", "kayak", "sailboat", "submarine", "ferry", 
                   "cruise", "vessel", "raft", "barge", "speedboat" }.Any(word.Contains);
        
        private bool IsAircraft(string word) =>
            new[] { "plane", "airplane", "aircraft", "jet", "helicopter", "drone", "glider", 
                   "balloon", "rocket", "spaceship", "shuttle" }.Any(word.Contains);
        
        private bool IsToolOrInstrument(string word) =>
            new[] { "tool", "hammer", "screwdriver", "wrench", "knife", "scissors", "saw", "drill", 
                   "instrument", "equipment", "device", "machine", "apparatus" }.Any(word.Contains);
        
        private bool IsBuildingOrStructure(string word) =>
            new[] { "house", "building", "home", "office", "school", "hospital", "church", "store", 
                   "bridge", "tower", "wall", "roof", "door", "window", "room", "structure" }.Any(word.Contains);
        
        private bool IsClothingOrTextile(string word) =>
            new[] { "clothes", "shirt", "pants", "dress", "shoe", "hat", "coat", "jacket", "sock", 
                   "fabric", "cotton", "silk", "wool", "leather", "textile" }.Any(word.Contains);
        
        private bool IsFoodOrNutrition(string word) =>
            new[] { "food", "eat", "bread", "meat", "fruit", "vegetable", "milk", "cheese", "egg", 
                   "rice", "pasta", "soup", "meal", "dinner", "lunch", "breakfast", "nutrition" }.Any(word.Contains);
        
        private bool IsTechnologyOrElectronics(string word) =>
            new[] { "computer", "phone", "internet", "software", "digital", "tech", "electronic", 
                   "robot", "artificial", "data", "network", "system", "program", "app" }.Any(word.Contains);
        
        private bool IsWeaponOrMilitary(string word) =>
            new[] { "weapon", "gun", "rifle", "sword", "knife", "bomb", "military", "army", "war", 
                   "soldier", "battle", "fight", "defense", "attack" }.Any(word.Contains);
        
        // === NATURAL WORLD CLASSIFICATION ===
        
        private bool IsLandform(string word) =>
            new[] { "mountain", "hill", "valley", "canyon", "cliff", "plain", "desert", "island", 
                   "peninsula", "plateau", "cave", "rock", "stone", "soil", "earth" }.Any(word.Contains);
        
        private bool IsWaterBody(string word) =>
            new[] { "ocean", "sea", "lake", "river", "stream", "pond", "waterfall", "bay", "gulf", 
                   "beach", "shore", "coast", "water", "wave", "current" }.Any(word.Contains);
        
        private bool IsWeatherOrClimate(string word) =>
            new[] { "weather", "rain", "snow", "sun", "wind", "storm", "cloud", "hot", "cold", 
                   "warm", "cool", "climate", "temperature", "season", "winter", "summer" }.Any(word.Contains);
        
        private bool IsMaterialOrSubstance(string word) =>
            new[] { "metal", "wood", "plastic", "glass", "paper", "concrete", "rubber", "oil", 
                   "gas", "liquid", "solid", "chemical", "substance", "material" }.Any(word.Contains);
        
        private bool IsColorOrVisual(string word) =>
            new[] { "color", "red", "blue", "green", "yellow", "black", "white", "orange", "purple", 
                   "pink", "brown", "gray", "bright", "dark", "light", "visual", "see", "look" }.Any(word.Contains);
        
        // === ABSTRACT DOMAINS CLASSIFICATION ===
        
        private bool IsEmotionOrFeeling(string word) =>
            new[] { "happy", "sad", "angry", "love", "hate", "fear", "joy", "excited", "nervous", 
                   "calm", "worried", "proud", "ashamed", "emotion", "feeling", "mood" }.Any(word.Contains);
        
        private bool IsThoughtOrIdea(string word) =>
            new[] { "think", "thought", "idea", "concept", "theory", "philosophy", "belief", 
                   "opinion", "mind", "mental", "cognitive", "reason", "logic" }.Any(word.Contains);
        
        private bool IsKnowledgeOrLearning(string word) =>
            new[] { "know", "learn", "study", "teach", "education", "school", "knowledge", 
                   "information", "fact", "truth", "science", "research", "discovery" }.Any(word.Contains);
        
        private bool IsMemoryOrPerception(string word) =>
            new[] { "remember", "memory", "forget", "recall", "recognize", "perceive", "sense", 
                   "aware", "conscious", "attention", "focus", "notice" }.Any(word.Contains);
        
        private bool IsLanguageOrSpeech(string word) =>
            new[] { "language", "speak", "talk", "say", "tell", "word", "sentence", "voice", 
                   "communication", "conversation", "discuss", "explain", "describe" }.Any(word.Contains);
        
        private bool IsSocialRelation(string word) =>
            new[] { "friend", "social", "relationship", "community", "group", "team", "society", 
                   "culture", "people", "person", "human", "interaction", "cooperation" }.Any(word.Contains);
        
        private bool IsCulturalPractice(string word) =>
            new[] { "culture", "tradition", "custom", "ritual", "ceremony", "festival", "art", 
                   "music", "dance", "religion", "spiritual", "cultural", "ethnic" }.Any(word.Contains);
        
        private bool IsPoliticsOrGovernment(string word) =>
            new[] { "government", "politics", "law", "legal", "court", "judge", "police", "vote", 
                   "election", "democracy", "president", "minister", "political", "policy" }.Any(word.Contains);
        
        private bool IsPhysicalMotion(string word) =>
            new[] { "move", "walk", "run", "jump", "climb", "swim", "fly", "drive", "travel", 
                   "motion", "speed", "fast", "slow", "direction", "forward", "backward" }.Any(word.Contains);
        
        private bool IsMentalAction(string word) =>
            new[] { "think", "decide", "choose", "plan", "imagine", "dream", "wonder", "consider", 
                   "analyze", "solve", "create", "invent", "design", "mental", "cognitive" }.Any(word.Contains);
        
        private bool IsSocialInteraction(string word) =>
            new[] { "meet", "greet", "talk", "discuss", "argue", "agree", "help", "cooperate", 
                   "share", "give", "take", "exchange", "social", "interact" }.Any(word.Contains);
        
        private bool IsWorkOrOccupation(string word) =>
            new[] { "work", "job", "career", "profession", "business", "office", "company", 
                   "employee", "boss", "manager", "doctor", "teacher", "engineer", "lawyer" }.Any(word.Contains);
        
        private bool IsSpatialRelation(string word) =>
            new[] { "on", "in", "under", "over", "beside", "near", "far", "here", "there", 
                   "above", "below", "inside", "outside", "left", "right", "front", "back" }.Any(word.Contains);
        
        private bool IsTemporalRelation(string word) =>
            new[] { "when", "then", "now", "before", "after", "during", "while", "time", 
                   "early", "late", "soon", "yesterday", "today", "tomorrow", "past", "future" }.Any(word.Contains);
        
        private bool IsQuantityOrMeasurement(string word) =>
            new[] { "number", "count", "measure", "size", "big", "small", "many", "few", "all", 
                   "some", "more", "less", "most", "least", "quantity", "amount" }.Any(word.Contains);
        
        private bool IsQualityOrEvaluation(string word) =>
            new[] { "good", "bad", "better", "best", "worse", "worst", "quality", "excellent", 
                   "poor", "beautiful", "ugly", "perfect", "terrible", "amazing", "awful" }.Any(word.Contains);
        
        // === GRAMMATICAL SUBCATEGORIZATION ===
        
        private string DetermineVerbSubcategory(string word)
        {
            if (IsPhysicalMotion(word)) return "language_structures/grammatical/verbs/action_verbs";
            if (IsMentalAction(word)) return "language_structures/grammatical/verbs/mental_verbs";
            if (IsPhysicalMotion(word)) return "language_structures/grammatical/verbs/motion_verbs";
            return "language_structures/grammatical/verbs/action_verbs";
        }
        
        private string DetermineNounSubcategory(string word)
        {
            // Check if it's a concrete vs abstract noun
            if (IsMammal(word) || IsBird(word) || IsToolOrInstrument(word) || IsBuildingOrStructure(word))
                return "language_structures/grammatical/nouns/concrete_nouns";
            if (IsEmotionOrFeeling(word) || IsThoughtOrIdea(word) || IsKnowledgeOrLearning(word))
                return "language_structures/grammatical/nouns/abstract_nouns";
            return "language_structures/grammatical/nouns/concrete_nouns";
        }
        
        private string DetermineAdjectiveSubcategory(string word)
        {
            if (IsQualityOrEvaluation(word)) return "language_structures/grammatical/adjectives/evaluative";
            return "language_structures/grammatical/adjectives/descriptive";
        }
        
        // Index management methods
        private async Task UpdateVocabularyIndexAsync(string word, string clusterPath)
        {
            _vocabularyIndex[word] = clusterPath;
            await SaveVocabularyIndexAsync();
        }
        
        private async Task UpdateConceptIndexAsync(string conceptId, string clusterPath, ConceptType conceptType)
        {
            _conceptIndex[conceptId] = new ConceptIndexEntry
            {
                ClusterPath = clusterPath,
                ConceptType = conceptType,
                LastAccessed = DateTime.UtcNow
            };
            await SaveConceptIndexAsync();
        }
        
        // Cluster I/O operations
        private async Task<VocabularyCluster?> LoadVocabularyClusterAsync(string clusterPath)
        {
            if (!File.Exists(clusterPath)) return null;
            
            var json = await File.ReadAllTextAsync(clusterPath);
            return JsonSerializer.Deserialize<VocabularyCluster>(json);
        }
        
        private async Task SaveVocabularyClusterAsync(string clusterPath, VocabularyCluster cluster)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(clusterPath)!);
            var json = JsonSerializer.Serialize(cluster, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(clusterPath, json);
        }
        
        private async Task<ConceptCluster?> LoadConceptClusterAsync(string clusterPath)
        {
            if (!File.Exists(clusterPath)) return null;
            
            var json = await File.ReadAllTextAsync(clusterPath);
            return JsonSerializer.Deserialize<ConceptCluster>(json);
        }
        
        private async Task SaveConceptClusterAsync(string clusterPath, ConceptCluster cluster)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(clusterPath)!);
            var json = JsonSerializer.Serialize(cluster, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(clusterPath, json);
        }
        
        // Index persistence
        private async Task SaveVocabularyIndexAsync()
        {
            var indexPath = Path.Combine(_hippocampusPath, "vocabulary_index.json");
            var json = JsonSerializer.Serialize(_vocabularyIndex, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(indexPath, json);
        }
        
        private async Task LoadVocabularyIndexAsync()
        {
            var indexPath = Path.Combine(_hippocampusPath, "vocabulary_index.json");
            if (File.Exists(indexPath))
            {
                var json = await File.ReadAllTextAsync(indexPath);
                _vocabularyIndex = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
        }
        
        private async Task SaveConceptIndexAsync()
        {
            var indexPath = Path.Combine(_hippocampusPath, "concept_index.json");
            var json = JsonSerializer.Serialize(_conceptIndex, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(indexPath, json);
        }
        
        private async Task LoadConceptIndexAsync()
        {
            var indexPath = Path.Combine(_hippocampusPath, "concept_index.json");
            if (File.Exists(indexPath))
            {
                var json = await File.ReadAllTextAsync(indexPath);
                _conceptIndex = JsonSerializer.Deserialize<Dictionary<string, ConceptIndexEntry>>(json) ?? new();
            }
        }

        /// <summary>
        /// Generate a hash for concept clustering to distribute load
        /// </summary>
        private string GetConceptHash(string conceptId)
        {
            var hash = conceptId.GetHashCode();
            return Math.Abs(hash % 1000).ToString("D3"); // 000-999 for distribution
        }
        
        // Shared neuron management methods
        
        /// <summary>
        /// Create a new shared neuron in the global pool
        /// </summary>
        public int CreateSharedNeuron(NeuronType type)
        {
            var neuronId = _nextNeuronId++;
            var neuron = new SharedNeuron
            {
                Id = neuronId,
                Type = type,
                WeightMap = new Dictionary<int, double>(),
                ActiveConcepts = new HashSet<string>(),
                LastActivated = DateTime.UtcNow,
                ActivationCount = 0
            };
            
            _neuronPool[neuronId] = neuron;
            
            // Determine storage file based on neuron ID for load balancing
            var poolFile = GetNeuronPoolFile(neuronId);
            _neuronLocationIndex[neuronId] = new NeuronLocationEntry
            {
                PoolFile = poolFile,
                LastAccessed = DateTime.UtcNow,
                IsLoaded = true
            };
            
            return neuronId;
        }
        
        /// <summary>
        /// Get or load a shared neuron from the pool
        /// </summary>
        public async Task<SharedNeuron?> GetSharedNeuronAsync(int neuronId)
        {
            // Check if neuron is already in memory
            if (_neuronPool.ContainsKey(neuronId))
            {
                _neuronPool[neuronId].LastActivated = DateTime.UtcNow;
                _neuronPool[neuronId].ActivationCount++;
                return _neuronPool[neuronId];
            }
            
            // Check if we know where to find this neuron
            if (_neuronLocationIndex.ContainsKey(neuronId))
            {
                var location = _neuronLocationIndex[neuronId];
                var neuron = await LoadNeuronFromPoolFileAsync(neuronId, location.PoolFile);
                
                if (neuron != null)
                {
                    _neuronPool[neuronId] = neuron;
                    location.IsLoaded = true;
                    location.LastAccessed = DateTime.UtcNow;
                    
                    neuron.LastActivated = DateTime.UtcNow;
                    neuron.ActivationCount++;
                    return neuron;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Update synaptic weights for a shared neuron
        /// </summary>
        public void UpdateNeuronWeights(int neuronId, Dictionary<int, double> weightUpdates)
        {
            if (_neuronPool.ContainsKey(neuronId))
            {
                var neuron = _neuronPool[neuronId];
                foreach (var update in weightUpdates)
                {
                    neuron.WeightMap[update.Key] = update.Value;
                }
                neuron.LastActivated = DateTime.UtcNow;
                neuron.ActivationCount++;
            }
        }
        
        /// <summary>
        /// Associate a neuron with a concept for cross-referencing
        /// </summary>
        public void AssociateNeuronWithConcept(int neuronId, string conceptId)
        {
            if (_neuronPool.ContainsKey(neuronId))
            {
                _neuronPool[neuronId].ActiveConcepts.Add(conceptId);
            }
        }
        
        /// <summary>
        /// Get neuron pool file path based on neuron ID for load balancing
        /// </summary>
        private string GetNeuronPoolFile(int neuronId)
        {
            // Distribute neurons across multiple files for parallel access
            var bucketId = neuronId % 100; // 100 buckets for load balancing
            return Path.Combine(_neuronPoolPath, $"pool_bucket_{bucketId:D3}.json");
        }
        
        /// <summary>
        /// Load a specific neuron from its pool file
        /// </summary>
        private async Task<SharedNeuron?> LoadNeuronFromPoolFileAsync(int neuronId, string poolFile)
        {
            if (!File.Exists(poolFile)) return null;
            
            var json = await File.ReadAllTextAsync(poolFile);
            var poolData = JsonSerializer.Deserialize<Dictionary<string, SharedNeuron>>(json);
            
            if (poolData != null && poolData.ContainsKey(neuronId.ToString()))
            {
                return poolData[neuronId.ToString()];
            }
            
            return null;
        }
        
        /// <summary>
        /// Save dirty neurons back to their pool files
        /// </summary>
        public async Task FlushNeuronPoolAsync()
        {
            // Group neurons by their pool file for efficient batch writing
            var neuronsByFile = new Dictionary<string, Dictionary<string, SharedNeuron>>();
            
            foreach (var kvp in _neuronPool)
            {
                var neuronId = kvp.Key;
                var neuron = kvp.Value;
                
                if (_neuronLocationIndex.ContainsKey(neuronId))
                {
                    var poolFile = _neuronLocationIndex[neuronId].PoolFile;
                    
                    if (!neuronsByFile.ContainsKey(poolFile))
                    {
                        neuronsByFile[poolFile] = new Dictionary<string, SharedNeuron>();
                    }
                    
                    neuronsByFile[poolFile][neuronId.ToString()] = neuron;
                }
            }
            
            // Write each pool file
            var tasks = neuronsByFile.Select(async kvp =>
            {
                var poolFile = kvp.Key;
                var neurons = kvp.Value;
                
                // Merge with existing pool file if it exists
                if (File.Exists(poolFile))
                {
                    var existingJson = await File.ReadAllTextAsync(poolFile);
                    var existingData = JsonSerializer.Deserialize<Dictionary<string, SharedNeuron>>(existingJson) ?? new();
                    
                    foreach (var neuronKvp in neurons)
                    {
                        existingData[neuronKvp.Key] = neuronKvp.Value;
                    }
                    
                    neurons = existingData;
                }
                
                Directory.CreateDirectory(Path.GetDirectoryName(poolFile)!);
                var json = JsonSerializer.Serialize(neurons, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(poolFile, json);
            });
            
            await Task.WhenAll(tasks);
            
            // Save metadata
            SaveNeuronPoolIndex();
        }
        
        /// <summary>
        /// Cleanup memory by unloading least recently used neurons
        /// </summary>
        public void CleanupMemory(int maxNeuronsInMemory = 10000)
        {
            if (_neuronPool.Count <= maxNeuronsInMemory) return;
            
            // Sort neurons by last activation time
            var sortedNeurons = _neuronPool
                .OrderBy(kvp => kvp.Value.LastActivated)
                .Take(_neuronPool.Count - maxNeuronsInMemory / 2)
                .ToList();
            
            foreach (var kvp in sortedNeurons)
            {
                _neuronPool.Remove(kvp.Key);
                if (_neuronLocationIndex.ContainsKey(kvp.Key))
                {
                    _neuronLocationIndex[kvp.Key].IsLoaded = false;
                }
            }
        }
        
        // Integration methods for existing brain classes
        
        /// <summary>
        /// Store vocabulary using biological clustering with shared neurons
        /// </summary>
        public async Task StoreVocabularyAsync(Dictionary<string, WordInfo> vocabulary)
        {
            foreach (var kvp in vocabulary)
            {
                var word = kvp.Key;
                var wordInfo = kvp.Value;
                
                // Get or create cluster for this word
                var clusterPath = GetVocabularyCluster(word, wordInfo);
                var cluster = await LoadVocabularyClusterAsync(clusterPath) ?? new VocabularyCluster();
                
                cluster.Words[word] = wordInfo;
                cluster.LastModified = DateTime.UtcNow;
                cluster.AccessCount++;
                
                await SaveVocabularyClusterAsync(clusterPath, cluster);
                await UpdateVocabularyIndexAsync(word, clusterPath);
            }
        }
        
        /// <summary>
        /// Load vocabulary from biological storage
        /// </summary>
        public async Task<Dictionary<string, WordInfo>> LoadVocabularyAsync()
        {
            var vocabulary = new Dictionary<string, WordInfo>();
            
            // Load vocabulary index first
            await LoadVocabularyIndexAsync();
            
            // Load each cluster mentioned in the index
            var clusterGroups = _vocabularyIndex.Values.Distinct().ToList();
            
            var tasks = clusterGroups.Select(async clusterPath =>
            {
                var cluster = await LoadVocabularyClusterAsync(clusterPath);
                return new { ClusterPath = clusterPath, Cluster = cluster };
            });
            
            var results = await Task.WhenAll(tasks);
            
            foreach (var result in results.Where(r => r.Cluster != null))
            {
                foreach (var kvp in result.Cluster!.Words)
                {
                    vocabulary[kvp.Key] = kvp.Value;
                }
                
                // Update access time
                _clusterAccessTimes[result.ClusterPath] = DateTime.UtcNow;
            }
            
            return vocabulary;
        }
        
        /// <summary>
        /// Store language-specific data using biological organization
        /// </summary>
        public async Task StoreLanguageDataAsync(Dictionary<string, object> languageData)
        {
            foreach (var kvp in languageData)
            {
                var dataType = kvp.Key;
                var data = kvp.Value;
                
                // Determine semantic domain for language data using Huth's hierarchical organization
                var domain = dataType switch
                {
                    "sentence_patterns" => "language_structures/syntactic/sentence_patterns",
                    "word_associations" => "semantic_domains/social_communication/language_speech/associations",
                    "grammar_rules" => "language_structures/syntactic/grammatical_relations",
                    "phrase_structures" => "language_structures/syntactic/phrase_structures",
                    "semantic_relations" => "semantic_domains/properties/semantic_relations",
                    "episodic_memories" => "episodic_memories/personal_experiences",
                    _ => "language_structures/grammatical/function_words"
                };
                
                var clusterPath = Path.Combine(_corticalColumnsPath, domain, $"{dataType}.json");
                var cluster = await LoadConceptClusterAsync(clusterPath) ?? new ConceptCluster();
                
                cluster.Concepts[dataType] = data;
                cluster.LastModified = DateTime.UtcNow;
                cluster.AccessCount++;
                
                await SaveConceptClusterAsync(clusterPath, cluster);
                await UpdateConceptIndexAsync(dataType, clusterPath, ConceptType.General);
            }
        }
        
        /// <summary>
        /// Load language data from biological storage
        /// </summary>
        public async Task<Dictionary<string, object>> LoadLanguageDataAsync()
        {
            var languageData = new Dictionary<string, object>();
            
            // Load from language structure domains following Huth's semantic organization
            var languageDomains = new[]
            {
                "language_structures/syntactic/sentence_patterns",
                "language_structures/syntactic/phrase_structures", 
                "language_structures/syntactic/grammatical_relations",
                "language_structures/grammatical/verbs",
                "language_structures/grammatical/nouns",
                "language_structures/grammatical/adjectives",
                "language_structures/grammatical/function_words",
                "semantic_domains/social_communication/language_speech/associations",
                "semantic_domains/properties/semantic_relations",
                "episodic_memories/personal_experiences"
            };
            
            foreach (var domain in languageDomains)
            {
                var domainPath = Path.Combine(_corticalColumnsPath, domain);
                if (!Directory.Exists(domainPath)) continue;
                
                var clusterFiles = Directory.GetFiles(domainPath, "*.json");
                
                foreach (var clusterFile in clusterFiles)
                {
                    var cluster = await LoadConceptClusterAsync(clusterFile);
                    if (cluster?.Concepts != null)
                    {
                        foreach (var conceptKvp in cluster.Concepts)
                        {
                            languageData[conceptKvp.Key] = conceptKvp.Value;
                        }
                    }
                }
            }
            
            return languageData;
        }
        
        /// <summary>
        /// Check if brain state exists in biological storage
        /// </summary>
        public bool HasExistingBrainState()
        {
            // Check for hippocampus indices
            var vocabIndexPath = Path.Combine(_hippocampusPath, "vocabulary_index.json");
            var conceptIndexPath = Path.Combine(_hippocampusPath, "concept_index.json");
            var neuronLocationPath = Path.Combine(_hippocampusPath, "neuron_locations.json");
            
            return File.Exists(vocabIndexPath) || File.Exists(conceptIndexPath) || File.Exists(neuronLocationPath);
        }
        
        /// <summary>
        /// Get storage statistics for analysis
        /// </summary>
        public async Task<StorageStatistics> GetStorageStatisticsAsync()
        {
            var stats = new StorageStatistics
            {
                TotalNeuronsInPool = _neuronPool.Count,
                NeuronsInMemory = _neuronPool.Count,
                VocabularyIndexSize = _vocabularyIndex.Count,
                ConceptIndexSize = _conceptIndex.Count,
                LastUpdated = DateTime.UtcNow
            };
            
            // Count cortical columns
            if (Directory.Exists(_corticalColumnsPath))
            {
                stats.CorticalColumnCount = Directory.GetDirectories(_corticalColumnsPath, "*", SearchOption.AllDirectories).Length;
                
                // Calculate total file sizes
                var allFiles = Directory.GetFiles(_corticalColumnsPath, "*.json", SearchOption.AllDirectories);
                stats.TotalStorageSize = allFiles.Sum(f => new FileInfo(f).Length);
            }
            
            // Count neuron pool files
            if (Directory.Exists(_neuronPoolPath))
            {
                var poolFiles = Directory.GetFiles(_neuronPoolPath, "*.json");
                stats.NeuronPoolFiles = poolFiles.Length;
                stats.NeuronPoolSize = poolFiles.Sum(f => new FileInfo(f).Length);
            }
            
            return stats;
        }
        
        /// <summary>
        /// Store individual neural concepts using Huth's semantic domain categorization
        /// This method analyzes each concept and routes it to the appropriate semantic domain
        /// </summary>
        public async Task StoreNeuralConceptsAsync(Dictionary<string, object> neuralConcepts)
        {
            foreach (var conceptKvp in neuralConcepts)
            {
                var conceptId = conceptKvp.Key;
                var conceptData = conceptKvp.Value;
                
                // Determine semantic domain for this concept using Huth's hierarchical organization
                var semanticDomain = DetermineSemanticDomainForConcept(conceptId, conceptData);
                
                // Create cluster path in the appropriate semantic domain
                var clusterPath = Path.Combine(_corticalColumnsPath, semanticDomain, $"concepts_{GetConceptHash(conceptId)}.json");
                
                // Load existing concept cluster
                var cluster = await LoadConceptClusterAsync(clusterPath) ?? new ConceptCluster();
                
                // Add concept to cluster
                cluster.Concepts[conceptId] = conceptData;
                cluster.LastModified = DateTime.UtcNow;
                cluster.AccessCount++;
                
                // Save cluster
                await SaveConceptClusterAsync(clusterPath, cluster);
                
                // Update hippocampus index for cross-domain retrieval
                await UpdateConceptIndexAsync(conceptId, clusterPath, ConceptType.Neural);
            }
        }

        /// <summary>
        /// Determine the semantic domain for a neural concept using Huth's hierarchical organization
        /// Analyzes concept content to route to appropriate domain/subdomain
        /// </summary>
        private string DetermineSemanticDomainForConcept(string conceptId, object conceptData)
        {
            var conceptString = conceptId.ToLower();
            var dataString = JsonSerializer.Serialize(conceptData).ToLower();
            var combinedText = $"{conceptString} {dataString}";
            
            // Simplified semantic domain categorization
            // Language and communication concepts
            if (combinedText.Contains("word") || combinedText.Contains("language") || 
                combinedText.Contains("speak") || combinedText.Contains("communication"))
                return "semantic_domains/social_communication/language_speech";
            
            // Actions and events
            if (combinedText.Contains("action") || combinedText.Contains("event") || 
                combinedText.Contains("activity") || combinedText.Contains("work"))
                return "semantic_domains/actions_events/general_actions";
            
            // Cognitive and mental processes
            if (combinedText.Contains("think") || combinedText.Contains("memory") || 
                combinedText.Contains("learn") || combinedText.Contains("knowledge"))
                return "semantic_domains/mental_cognitive/general_cognitive";
            
            // Social interactions
            if (combinedText.Contains("social") || combinedText.Contains("friend") || 
                combinedText.Contains("family") || combinedText.Contains("relationship"))
                return "semantic_domains/social_communication/social_relations";
            
            // Default: general concepts
            return "semantic_domains/general_concepts";
        }
        
        /// <summary>
        /// Classify semantic domain using pre-trained model
        /// No training required - uses semantic embeddings for intelligent categorization
        /// </summary>
        public async Task<string> ClassifySemanticDomainAsync(string conceptId)
        {
            // Try pre-trained classifier first
            if (_preTrainedClassifier != null)
            {
                try
                {
                    var domain = _preTrainedClassifier.ClassifySemanticDomain(conceptId);
                    return await Task.FromResult(domain);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Pre-trained classifier error: {ex.Message}");
                }
            }
            
            // Fallback to rule-based classification
            var fallbackDomain = _fallbackClassifier.ClassifySemanticDomain(conceptId);
            return await Task.FromResult(fallbackDomain);
        }
        
        /// <summary>
        /// Get the predicted domain for a concept using semantic classification
        /// </summary>
        public Task<string> GetPredictedDomainAsync(string conceptId)
        {
            return ClassifySemanticDomainAsync(conceptId);
        }
    }

    /// <summary>
    /// Shared neuron that can participate in multiple concepts and cortical columns
    /// Enables biological realism through neural sharing across brain regions
    /// </summary>
    public class SharedNeuron
    {
        public int Id { get; set; }
        public NeuronType Type { get; set; }
        public Dictionary<int, double> WeightMap { get; set; } = new();
        public HashSet<string> ActiveConcepts { get; set; } = new();
        public DateTime LastActivated { get; set; }
        public int ActivationCount { get; set; }
    }
    
    /// <summary>
    /// Biological concept stored with neural pointers instead of full neuron data
    /// </summary>
    public class BiologicalConcept
    {
        public string ConceptId { get; set; } = "";
        public List<int> NeuronIds { get; set; } = new();
        public string StorageColumn { get; set; } = "";
        public double ActivationStrength { get; set; }
        public DateTime LastAccessed { get; set; }
    }
    
    /// <summary>
    /// Cortical column for semantic clustering and efficient loading
    /// </summary>
    public class CorticalColumn
    {
        public string Specialization { get; set; } = "";
        public List<string> ConceptIds { get; set; } = new();
        public DateTime LastAccessed { get; set; }
        public Dictionary<string, double> ConceptStrengths { get; set; } = new();
    }

    // Supporting data structures
    public class VocabularyCluster
    {
        public Dictionary<string, WordInfo> Words { get; set; } = new();
        public DateTime LastModified { get; set; }
        public int AccessCount { get; set; }
    }
    
    public class ConceptCluster
    {
        public Dictionary<string, object> Concepts { get; set; } = new();
        public DateTime LastModified { get; set; }
        public int AccessCount { get; set; }
    }
    
    public class ConceptIndexEntry
    {
        public string ClusterPath { get; set; } = "";
        public ConceptType ConceptType { get; set; }
        public DateTime LastAccessed { get; set; }
    }
    
    public enum ConceptType
    {
        SentencePattern,
        WordAssociation,
        SemanticRelation,
        EpisodicMemory,
        General,
        Neural
    }
    
    public class NeuronLocationEntry
    {
        public string PoolFile { get; set; } = "";
        public DateTime LastAccessed { get; set; }
        public bool IsLoaded { get; set; }
    }
    
    public enum NeuronType
    {
        Input,
        Hidden,
        Output,
        Memory,
        Attention,
        Concept
    }
    
    public class StorageStatistics
    {
        public int TotalNeuronsInPool { get; set; }
        public int NeuronsInMemory { get; set; }
        public int VocabularyIndexSize { get; set; }
        public int ConceptIndexSize { get; set; }
        public int CorticalColumnCount { get; set; }
        public int NeuronPoolFiles { get; set; }
        public long TotalStorageSize { get; set; }
        public long NeuronPoolSize { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}