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
    public class BiologicalStorageManager
    {
        private readonly string _brainDataPath;
        private readonly string _hippocampusPath;
        private readonly string _corticalColumnsPath;
        private readonly string _workingMemoryPath;
        private readonly string _neuronPoolPath;
        
        // Hippocampus indices for sparse routing
        private Dictionary<string, string> _vocabularyIndex;
        private Dictionary<string, ConceptIndexEntry> _conceptIndex;
        private Dictionary<int, NeuronLocationEntry> _neuronLocationIndex;
        private Dictionary<string, DateTime> _clusterAccessTimes;
        
        // Global neuron pool management
        private Dictionary<int, SharedNeuron> _neuronPool;
        private int _nextNeuronId;
        
        public BiologicalStorageManager(string brainDataPath)
        {
            _brainDataPath = brainDataPath;
            _hippocampusPath = Path.Combine(_brainDataPath, "hippocampus");
            _corticalColumnsPath = Path.Combine(_brainDataPath, "cortical_columns");
            _workingMemoryPath = Path.Combine(_brainDataPath, "working_memory");
            _neuronPoolPath = Path.Combine(_brainDataPath, "neuron_pool");
            
            _vocabularyIndex = new Dictionary<string, string>();
            _conceptIndex = new Dictionary<string, ConceptIndexEntry>();
            _neuronLocationIndex = new Dictionary<int, NeuronLocationEntry>();
            _clusterAccessTimes = new Dictionary<string, DateTime>();
            _neuronPool = new Dictionary<int, SharedNeuron>();
            
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
            
            // Cortical columns - semantic clustering
            var corticalDomains = new[]
            {
                "language_structures/verbs",
                "language_structures/nouns", 
                "language_structures/sentence_patterns",
                "semantic_domains/animals",
                "semantic_domains/technology",
                "semantic_domains/emotions",
                "semantic_domains/spatial_relations",
                "semantic_domains/temporal_relations",
                "episodic_memories"
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
        /// </summary>
        private string DetermineSemanticDomain(string word, WordInfo wordInfo)
        {
            // Analyze word for semantic markers
            var lowerWord = word.ToLowerInvariant();
            
            // Animal domain
            if (IsAnimalWord(lowerWord)) return "semantic_domains/animals";
            
            // Technology domain  
            if (IsTechnologyWord(lowerWord)) return "semantic_domains/technology";
            
            // Emotion domain
            if (IsEmotionWord(lowerWord)) return "semantic_domains/emotions";
            
            // Spatial relations
            if (IsSpatialWord(lowerWord)) return "semantic_domains/spatial_relations";
            
            // Temporal relations
            if (IsTemporalWord(lowerWord)) return "semantic_domains/temporal_relations";
            
            // Default to language structures
            return "language_structures";
        }
        
        private string DetermineWordType(string word, WordInfo wordInfo)
        {
            // Analyze grammatical patterns from usage
            var estimatedType = wordInfo.EstimatedType?.ToLowerInvariant();
            
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
                ConceptType.SentencePattern => "language_structures/sentence_patterns",
                ConceptType.WordAssociation => "language_structures/associations", 
                ConceptType.SemanticRelation => "semantic_domains/relations",
                ConceptType.EpisodicMemory => "episodic_memories",
                _ => "language_structures/general"
            };
        }
        
        // Semantic classification helpers
        private bool IsAnimalWord(string word) => 
            new[] { "cat", "dog", "bird", "fish", "animal", "pet", "wild" }.Any(word.Contains);
            
        private bool IsTechnologyWord(string word) =>
            new[] { "computer", "phone", "internet", "software", "digital", "tech" }.Any(word.Contains);
            
        private bool IsEmotionWord(string word) =>
            new[] { "happy", "sad", "angry", "love", "hate", "feel", "emotion" }.Any(word.Contains);
            
        private bool IsSpatialWord(string word) =>
            new[] { "on", "in", "under", "over", "beside", "near", "far", "here", "there" }.Any(word.Contains);
            
        private bool IsTemporalWord(string word) =>
            new[] { "when", "then", "now", "before", "after", "during", "while", "time" }.Any(word.Contains);
        
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
        General
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
