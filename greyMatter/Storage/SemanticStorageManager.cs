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
        
        // Batch processing for performance optimization
        private Dictionary<string, ConceptCluster> _pendingConceptClusters;
        private Dictionary<string, object> _pendingConcepts;
        private bool _batchModeEnabled;
        private int _batchSizeThreshold;
        
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
            
            // Initialize batch processing
            _pendingConceptClusters = new Dictionary<string, ConceptCluster>();
            _pendingConcepts = new Dictionary<string, object>();
            _batchModeEnabled = false;
            _batchSizeThreshold = 50; // Flush after 50 concepts per cluster
            
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
        private async Task SaveNeuronPoolIndexAsync()
        {
            var poolData = new Dictionary<string, object>
            {
                ["next_neuron_id"] = _nextNeuronId,
                ["pool_size"] = _neuronPool.Count,
                ["last_updated"] = DateTime.UtcNow.ToString("O")
            };
            
            var poolIndexPath = Path.Combine(_neuronPoolPath, "pool_index.json");
            await File.WriteAllTextAsync(poolIndexPath, JsonSerializer.Serialize(poolData, new JsonSerializerOptions { WriteIndented = true }));
            
            // Save neuron location index
            var locationIndexPath = Path.Combine(_hippocampusPath, "neuron_locations.json");
            var locationData = _neuronLocationIndex.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
            await File.WriteAllTextAsync(locationIndexPath, JsonSerializer.Serialize(locationData, new JsonSerializerOptions { WriteIndented = true }));
        }
        
        /// <summary>
        /// Synchronous version of SaveNeuronPoolIndexAsync for use in constructors
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
        /// Optimized batch concept storage for high-performance scenarios
        /// </summary>
        public async Task SaveConceptsBatchAsync(Dictionary<string, (object Data, ConceptType Type)> concepts)
        {
            // Group concepts by cluster to minimize I/O operations
            var clusterGroups = new Dictionary<string, List<(string conceptId, object data, ConceptType type)>>();

            foreach (var (conceptId, (data, type)) in concepts)
            {
                var clusterPath = GetConceptCluster(conceptId, type);
                if (!clusterGroups.ContainsKey(clusterPath))
                {
                    clusterGroups[clusterPath] = new List<(string, object, ConceptType)>();
                }
                clusterGroups[clusterPath].Add((conceptId, data, type));
            }

            // Process each cluster group
            var saveTasks = new List<Task>();
            foreach (var (clusterPath, conceptList) in clusterGroups)
            {
                saveTasks.Add(ProcessConceptClusterBatchAsync(clusterPath, conceptList));
            }

            await Task.WhenAll(saveTasks);

            // Update concept index in batch
            await UpdateConceptIndexBatchAsync(concepts);
        }

        /// <summary>
        /// Enable batch mode for automatic buffering
        /// </summary>
        public void EnableBatchMode(int batchSizeThreshold = 50)
        {
            _batchModeEnabled = true;
            _batchSizeThreshold = batchSizeThreshold;
            _pendingConceptClusters.Clear();
            _pendingConcepts.Clear();
        }

        /// <summary>
        /// Disable batch mode and flush any pending concepts
        /// </summary>
        public async Task DisableBatchModeAsync()
        {
            if (_batchModeEnabled)
            {
                await FlushPendingConceptsAsync();
            }
            _batchModeEnabled = false;
        }

        /// <summary>
        /// Optimized SaveConceptAsync with batch buffering
        /// </summary>
        public async Task SaveConceptOptimizedAsync(string conceptId, object conceptData, ConceptType conceptType)
        {
            if (_batchModeEnabled)
            {
                // Buffer concept for batch processing
                var clusterPath = GetConceptCluster(conceptId, conceptType);
                
                if (!_pendingConceptClusters.ContainsKey(clusterPath))
                {
                    _pendingConceptClusters[clusterPath] = await LoadConceptClusterAsync(clusterPath) ?? new ConceptCluster();
                }

                _pendingConceptClusters[clusterPath].Concepts[conceptId] = conceptData;
                _pendingConceptClusters[clusterPath].LastModified = DateTime.UtcNow;
                _pendingConcepts[conceptId] = (conceptData, conceptType);

                // Flush if batch size threshold reached
                if (_pendingConcepts.Count >= _batchSizeThreshold)
                {
                    await FlushPendingConceptsAsync();
                }
            }
            else
            {
                // Fall back to original method
                await SaveConceptAsync(conceptId, conceptData, conceptType);
            }
        }

        /// <summary>
        /// Process a batch of concepts for a single cluster
        /// </summary>
        private async Task ProcessConceptClusterBatchAsync(string clusterPath, List<(string conceptId, object data, ConceptType type)> concepts)
        {
            // Load cluster once
            var cluster = await LoadConceptClusterAsync(clusterPath) ?? new ConceptCluster();

            // Add all concepts to cluster
            foreach (var (conceptId, data, _) in concepts)
            {
                cluster.Concepts[conceptId] = data;
            }
            cluster.LastModified = DateTime.UtcNow;

            // Save cluster once
            await SaveConceptClusterAsync(clusterPath, cluster);
        }

        /// <summary>
        /// Update concept index in batch
        /// </summary>
        private async Task UpdateConceptIndexBatchAsync(Dictionary<string, (object Data, ConceptType Type)> concepts)
        {
            foreach (var (conceptId, (_, type)) in concepts)
            {
                var clusterPath = GetConceptCluster(conceptId, type);
                _conceptIndex[conceptId] = new ConceptIndexEntry
                {
                    ClusterPath = clusterPath,
                    ConceptType = type,
                    LastAccessed = DateTime.UtcNow
                };
            }

            // Save index once
            await SaveConceptIndexAsync();
        }

        /// <summary>
        /// Flush all pending concepts to storage
        /// </summary>
        private async Task FlushPendingConceptsAsync()
        {
            if (_pendingConceptClusters.Count == 0)
                return;

            // Save all modified clusters
            var saveTasks = _pendingConceptClusters.Select(kvp => 
                SaveConceptClusterAsync(kvp.Key, kvp.Value)).ToArray();
            
            await Task.WhenAll(saveTasks);

            // Update concept index
            await UpdateConceptIndexBatchAsync(
                _pendingConcepts.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => ((object Data, ConceptType Type))kvp.Value));

            // Clear buffers
            _pendingConceptClusters.Clear();
            _pendingConcepts.Clear();
        }

        /// <summary>
        /// Get comprehensive storage statistics
        /// </summary>
        public async Task<StorageStatistics> GetStorageStatisticsAsync()
        {
            var stats = new StorageStatistics
            {
                TotalConcepts = _conceptIndex.Count,
                TotalVocabularyWords = _vocabularyIndex.Count,
                TotalNeurons = _neuronPool.Count + _neuronLocationIndex.Count,
                LoadedNeurons = _neuronPool.Count,
                StorageSizeBytes = await CalculateStorageSizeAsync(),
                LastUpdated = DateTime.UtcNow
            };

            // Count concepts by type
            stats.ConceptsByType = new Dictionary<ConceptType, int>();
            foreach (var entry in _conceptIndex.Values)
            {
                if (!stats.ConceptsByType.ContainsKey(entry.ConceptType))
                    stats.ConceptsByType[entry.ConceptType] = 0;
                stats.ConceptsByType[entry.ConceptType]++;
            }

            return stats;
        }

        /// <summary>
        /// Calculate total storage size
        /// </summary>
        private async Task<long> CalculateStorageSizeAsync()
        {
            long totalSize = 0;

            // Calculate size of brain data directory
            if (Directory.Exists(_brainDataPath))
            {
                totalSize += await Task.Run(() => GetDirectorySize(_brainDataPath));
            }

            return totalSize;
        }

        /// <summary>
        /// Get directory size recursively
        /// </summary>
        private long GetDirectorySize(string path)
        {
            long size = 0;

            try
            {
                var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    size += info.Length;
                }
            }
            catch (Exception)
            {
                // Ignore errors in size calculation
            }

            return size;
        }

        /// <summary>
        /// Get predicted semantic domain for a word
        /// </summary>
        public async Task<string> GetPredictedDomainAsync(string word)
        {
            if (_preTrainedClassifier != null)
            {
                var domains = await _preTrainedClassifier.ClassifyDomainsAsync(word);
                return domains.OrderByDescending(d => d.Value).FirstOrDefault().Key ?? "semantic_domains/general_concepts";
            }

            // Fallback to rule-based classification
            return _fallbackClassifier.ClassifySemanticDomain(word);
        }

        /// <summary>
        /// Check if there's existing brain state
        /// </summary>
        public bool HasExistingBrainState()
        {
            return Directory.Exists(_brainDataPath) &&
                   (File.Exists(Path.Combine(_hippocampusPath, "vocabulary_index.json")) ||
                    File.Exists(Path.Combine(_hippocampusPath, "concept_index.json")));
        }

        /// <summary>
        /// Load vocabulary data
        /// </summary>
        public async Task<Dictionary<string, WordInfo>> LoadVocabularyAsync()
        {
            var vocabulary = new Dictionary<string, WordInfo>();

            if (!Directory.Exists(_corticalColumnsPath))
                return vocabulary;

            // Load all vocabulary clusters
            var clusterFiles = Directory.GetFiles(_corticalColumnsPath, "*.json", SearchOption.AllDirectories);
            foreach (var clusterFile in clusterFiles)
            {
                if (clusterFile.Contains("vocabulary") || clusterFile.Contains("freq_"))
                {
                    var cluster = await LoadVocabularyClusterAsync(clusterFile);
                    if (cluster != null)
                    {
                        foreach (var (word, info) in cluster.Words)
                        {
                            vocabulary[word] = info;
                        }
                    }
                }
            }

            return vocabulary;
        }

        /// <summary>
        /// Store vocabulary data
        /// </summary>
        public async Task StoreVocabularyAsync(Dictionary<string, WordInfo> vocabulary)
        {
            foreach (var (word, info) in vocabulary)
            {
                await SaveVocabularyWordAsync(word, info);
            }
        }

        /// <summary>
        /// Load language data
        /// </summary>
        public async Task<Dictionary<string, object>> LoadLanguageDataAsync()
        {
            var languageData = new Dictionary<string, object>();

            // Load concept clusters that contain language patterns
            if (!Directory.Exists(_corticalColumnsPath))
                return languageData;

            var clusterFiles = Directory.GetFiles(_corticalColumnsPath, "*.json", SearchOption.AllDirectories);
            foreach (var clusterFile in clusterFiles)
            {
                var cluster = await LoadConceptClusterAsync(clusterFile);
                if (cluster != null)
                {
                    foreach (var (conceptId, data) in cluster.Concepts)
                    {
                        languageData[conceptId] = data;
                    }
                }
            }

            return languageData;
        }

        /// <summary>
        /// Store language data
        /// </summary>
        public async Task StoreLanguageDataAsync(Dictionary<string, object> languageData)
        {
            foreach (var (conceptId, data) in languageData)
            {
                await SaveConceptAsync(conceptId, data, ConceptType.SentencePattern);
            }
        }

        /// <summary>
        /// Load all neurons from storage
        /// </summary>
        public async Task<Dictionary<int, object>> LoadAllNeuronsAsync()
        {
            var allNeurons = new Dictionary<int, object>();

            // Load from neuron pool files
            if (Directory.Exists(_neuronPoolPath))
            {
                var poolFiles = Directory.GetFiles(_neuronPoolPath, "*.json");
                foreach (var poolFile in poolFiles)
                {
                    if (Path.GetFileName(poolFile) != "pool_index.json")
                    {
                        var neurons = await LoadNeuronPoolFileAsync(poolFile);
                        foreach (var neuron in neurons)
                        {
                            allNeurons[neuron.Id] = neuron.Data ?? new object();
                        }
                    }
                }
            }

            return allNeurons;
        }

        /// <summary>
        /// Store neural concepts
        /// </summary>
        public async Task StoreNeuralConceptsAsync(Dictionary<string, object> concepts)
        {
            foreach (var (conceptId, data) in concepts)
            {
                await SaveConceptAsync(conceptId, data, ConceptType.Neural);
            }
        }

        /// <summary>
        /// Add neuron to pool
        /// </summary>
        public async Task<int> AddNeuronToPoolAsync(int neuronId, object neuronData)
        {
            SharedNeuron neuron;
            
            // If neuronData is already a SharedNeuron, use it directly
            if (neuronData is SharedNeuron existingNeuron)
            {
                neuron = existingNeuron;
                neuron.Id = neuronId; // Ensure ID is correct
            }
            else
            {
                // Create new SharedNeuron from object data
                neuron = new SharedNeuron
                {
                    Id = neuronId,
                    Data = neuronData,
                    LastUsed = DateTime.UtcNow
                };
            }

            _neuronPool[neuron.Id] = neuron;

            // Update location index
            _neuronLocationIndex[neuron.Id] = new NeuronLocationEntry
            {
                PoolFile = GetNeuronPoolFile(neuron.Id),
                LastAccessed = DateTime.UtcNow,
                IsLoaded = true
            };

            await SaveNeuronPoolIndexAsync();
            return neuron.Id;
        }

        /// <summary>
        /// Flush neuron pool to storage
        /// </summary>
        public async Task FlushNeuronPoolAsync()
        {
            // Group neurons by pool file
            var poolGroups = new Dictionary<string, List<SharedNeuron>>();
            foreach (var neuron in _neuronPool.Values)
            {
                var poolFile = GetNeuronPoolFile(neuron.Id);
                if (!poolGroups.ContainsKey(poolFile))
                    poolGroups[poolFile] = new List<SharedNeuron>();
                poolGroups[poolFile].Add(neuron);
            }

            // Save each pool file
            foreach (var (poolFile, neurons) in poolGroups)
            {
                await SaveNeuronPoolFileAsync(poolFile, neurons);
            }

            await SaveNeuronPoolIndexAsync();
        }

        /// <summary>
        /// Load vocabulary word
        /// </summary>
        public async Task<WordInfo?> LoadVocabularyWordAsync(string word)
        {
            if (_vocabularyIndex.TryGetValue(word, out var clusterPath))
            {
                var cluster = await LoadVocabularyClusterAsync(clusterPath);
                if (cluster != null && cluster.Words.TryGetValue(word, out var wordInfo))
                {
                    return wordInfo;
                }
            }
            return null;
        }

        /// <summary>
        /// Get neuron pool file path for a neuron ID
        /// </summary>
        private string GetNeuronPoolFile(int neuronId)
        {
            var poolIndex = neuronId / 1000; // 1000 neurons per file
            return Path.Combine(_neuronPoolPath, $"pool_{poolIndex}.json");
        }

        /// <summary>
        /// Load neuron pool file
        /// </summary>
        private async Task<List<SharedNeuron>> LoadNeuronPoolFileAsync(string poolFile)
        {
            if (!File.Exists(poolFile))
                return new List<SharedNeuron>();

            var json = await File.ReadAllTextAsync(poolFile);
            return JsonSerializer.Deserialize<List<SharedNeuron>>(json) ?? new List<SharedNeuron>();
        }

        /// <summary>
        /// Save neuron pool file
        /// </summary>
        private async Task SaveNeuronPoolFileAsync(string poolFile, List<SharedNeuron> neurons)
        {
            var json = JsonSerializer.Serialize(neurons, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(poolFile, json);
        }

        /// <summary>
        /// Determine semantic domain for a word
        /// </summary>
        private string DetermineSemanticDomain(string word, WordInfo wordInfo)
        {
            // Use pre-trained classifier if available
            if (_preTrainedClassifier != null)
            {
                try
                {
                    var domains = _preTrainedClassifier.ClassifyDomainsAsync(word).Result;
                    var bestDomain = domains.OrderByDescending(d => d.Value).FirstOrDefault().Key;
                    if (!string.IsNullOrEmpty(bestDomain))
                        return bestDomain;
                }
                catch
                {
                    // Fall back to rule-based
                }
            }

            // Rule-based semantic domain determination
            return _fallbackClassifier.ClassifySemanticDomain(word);
        }

        /// <summary>
        /// Determine word type
        /// </summary>
        private string DetermineWordType(string word, WordInfo wordInfo)
        {
            // Use the EstimatedType from WordInfo
            return wordInfo.EstimatedType switch
            {
                WordType.Noun => "nouns",
                WordType.Verb => "verbs",
                WordType.Adjective => "adjectives",
                WordType.Adverb => "adverbs",
                WordType.Preposition => "prepositions",
                WordType.Article => "articles",
                WordType.Pronoun => "pronouns",
                _ => "other"
            };
        }

        /// <summary>
        /// Get frequency band for storage organization
        /// </summary>
        private string GetFrequencyBand(int frequency)
        {
            if (frequency >= 10000) return "very_high";
            if (frequency >= 1000) return "high";
            if (frequency >= 100) return "medium";
            if (frequency >= 10) return "low";
            return "very_low";
        }

        /// <summary>
        /// Generate semantic hash for concept clustering
        /// </summary>
        private string GetSemanticHash(string conceptId)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(conceptId);
            var hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        /// <summary>
        /// Get concept type folder name
        /// </summary>
        private string GetConceptTypeFolder(ConceptType conceptType)
        {
            return conceptType switch
            {
                ConceptType.SentencePattern => "sentence_patterns",
                ConceptType.WordAssociation => "word_associations",
                ConceptType.SemanticRelation => "semantic_relations",
                ConceptType.EpisodicMemory => "episodic_memories",
                ConceptType.General => "general_concepts",
                ConceptType.Neural => "neural_concepts",
                _ => "other"
            };
        }

        /// <summary>
        /// Load vocabulary cluster
        /// </summary>
        private async Task<VocabularyCluster?> LoadVocabularyClusterAsync(string clusterPath)
        {
            if (!File.Exists(clusterPath))
                return null;

            try
            {
                var json = await File.ReadAllTextAsync(clusterPath);
                if (string.IsNullOrWhiteSpace(json))
                    return new VocabularyCluster(); // Return empty cluster for empty files
                
                return JsonSerializer.Deserialize<VocabularyCluster>(json);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"⚠️ Warning: Failed to load vocabulary cluster {clusterPath}: {ex.Message}");
                Console.WriteLine("🔄 Creating new empty cluster");
                return new VocabularyCluster(); // Return empty cluster on JSON errors
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Warning: Unexpected error loading vocabulary cluster {clusterPath}: {ex.Message}");
                return new VocabularyCluster(); // Return empty cluster on any error
            }
        }

        /// <summary>
        /// Save vocabulary cluster
        /// </summary>
        private async Task SaveVocabularyClusterAsync(string clusterPath, VocabularyCluster cluster)
        {
            var directory = Path.GetDirectoryName(clusterPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(cluster, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(clusterPath, json);
        }

        /// <summary>
        /// Update vocabulary index
        /// </summary>
        private async Task UpdateVocabularyIndexAsync(string word, string clusterPath)
        {
            _vocabularyIndex[word] = clusterPath;
            await SaveVocabularyIndexAsync();
        }

        /// <summary>
        /// Save vocabulary index
        /// </summary>
        private async Task SaveVocabularyIndexAsync()
        {
            var indexPath = Path.Combine(_hippocampusPath, "vocabulary_index.json");
            var json = JsonSerializer.Serialize(_vocabularyIndex, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(indexPath, json);
        }

        /// <summary>
        /// Load concept cluster
        /// </summary>
        private async Task<ConceptCluster?> LoadConceptClusterAsync(string clusterPath)
        {
            if (!File.Exists(clusterPath))
                return null;

            try
            {
                var json = await File.ReadAllTextAsync(clusterPath);
                if (string.IsNullOrWhiteSpace(json))
                    return new ConceptCluster(); // Return empty cluster for empty files
                
                return JsonSerializer.Deserialize<ConceptCluster>(json);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"⚠️ Warning: Failed to load concept cluster {clusterPath}: {ex.Message}");
                Console.WriteLine("🔄 Creating new empty cluster");
                return new ConceptCluster(); // Return empty cluster on JSON errors
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Warning: Unexpected error loading concept cluster {clusterPath}: {ex.Message}");
                return new ConceptCluster(); // Return empty cluster on any error
            }
        }

        /// <summary>
        /// Save concept cluster
        /// </summary>
        private async Task SaveConceptClusterAsync(string clusterPath, ConceptCluster cluster)
        {
            var directory = Path.GetDirectoryName(clusterPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(cluster, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(clusterPath, json);
        }

        /// <summary>
        /// Update concept index
        /// </summary>
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

        /// <summary>
        /// Save concept index
        /// </summary>
        private async Task SaveConceptIndexAsync()
        {
            var indexPath = Path.Combine(_hippocampusPath, "concept_index.json");
            var json = JsonSerializer.Serialize(_conceptIndex, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(indexPath, json);
        }
    }

    // Missing type definitions
    public enum ConceptType
    {
        SentencePattern,
        WordAssociation,
        SemanticRelation,
        EpisodicMemory,
        General,
        Neural
    }

    public class ConceptIndexEntry
    {
        public string ClusterPath { get; set; } = "";
        public ConceptType ConceptType { get; set; }
        public DateTime LastAccessed { get; set; }
    }

    public class NeuronLocationEntry
    {
        public string PoolFile { get; set; } = "";
        public DateTime LastAccessed { get; set; }
        public bool IsLoaded { get; set; }
    }

    public class SharedNeuron
    {
        public int Id { get; set; }
        public object? Data { get; set; }
        public DateTime LastUsed { get; set; }
    }

    public class StorageStatistics
    {
        public int TotalConcepts { get; set; }
        public int TotalVocabularyWords { get; set; }
        public int TotalNeurons { get; set; }
        public int LoadedNeurons { get; set; }
        public long StorageSizeBytes { get; set; }
        public long TotalStorageSize => StorageSizeBytes; // Alias for backward compatibility
        public int TotalNeuronsInPool => LoadedNeurons; // Alias for backward compatibility
        public int ConceptIndexSize => TotalConcepts; // Alias for backward compatibility
        public int CorticalColumnCount { get; set; } = 0; // Number of cortical columns
        public int VocabularyIndexSize => TotalVocabularyWords; // Alias for backward compatibility
        public DateTime LastUpdated { get; set; }
        public Dictionary<ConceptType, int> ConceptsByType { get; set; } = new Dictionary<ConceptType, int>();
    }

    public class VocabularyCluster
    {
        public Dictionary<string, WordInfo> Words { get; set; } = new Dictionary<string, WordInfo>();
        public DateTime LastModified { get; set; }
    }

    public class ConceptCluster
    {
        public Dictionary<string, object> Concepts { get; set; } = new Dictionary<string, object>();
        public DateTime LastModified { get; set; }
    }
}