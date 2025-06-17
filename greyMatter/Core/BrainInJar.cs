using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;
using GreyMatter.Storage;

namespace GreyMatter.Core
{
    /// <summary>
    /// BrainInJar: Main orchestrator for the SBIJ system
    /// Manages neuron clusters, learning, and dynamic scaling
    /// </summary>
    public class BrainInJar
    {
        private readonly BrainStorage _storage;
        private readonly Dictionary<Guid, NeuronCluster> _loadedClusters = new();
        private readonly Dictionary<Guid, Synapse> _synapses = new();
        private readonly FeatureMapper _featureMapper = new();
        private readonly Random _random = new();
        
        // Brain configuration
        public int MaxLoadedClusters { get; set; } = 10;
        public int MaxNeuronsPerCluster { get; set; } = 100;
        public double ClusterCreationThreshold { get; set; } = 0.3;
        public TimeSpan ClusterUnloadTime { get; set; } = TimeSpan.FromMinutes(30);
        
        // Learning parameters
        public double GlobalLearningRate { get; set; } = 0.01;
        public double ConceptSimilarityThreshold { get; set; } = 0.7;
        
        // Statistics
        public int TotalClustersCreated { get; private set; } = 0;
        public int TotalNeuronsCreated { get; private set; } = 0;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        public BrainInJar(string storagePath = "brain_data")
        {
            _storage = new BrainStorage(storagePath);
        }

        /// <summary>
        /// Initialize the brain - load existing clusters and synapses
        /// </summary>
        public async Task InitializeAsync()
        {
            Console.WriteLine("🧠 Initializing Brain in Jar...");
            
            // Load feature mappings first
            var featureMappings = await _storage.LoadFeatureMappingsAsync();
            _featureMapper.RestoreFromSnapshot(featureMappings);
            Console.WriteLine($"Loaded {featureMappings.FeatureMappings.Count} feature mappings");
            
            // Load synapses
            var synapseSnapshots = await _storage.LoadSynapsesAsync();
            foreach (var snapshot in synapseSnapshots)
            {
                _synapses[snapshot.Id] = Synapse.FromSnapshot(snapshot);
            }
            
            Console.WriteLine($"Loaded {_synapses.Count} synapses");
            
            // Load cluster index but don't load clusters yet (lazy loading)
            var clusterIndex = await _storage.LoadClusterIndexAsync();
            Console.WriteLine($"Found {clusterIndex.Count} clusters in storage");
            
            var stats = await _storage.GetStorageStatsAsync();
            Console.WriteLine($"Storage: {stats.ClusterCount} clusters, {stats.TotalSizeFormatted}");
        }

        /// <summary>
        /// Learn a new concept by creating or growing relevant clusters
        /// </summary>
        public async Task<LearningResult> LearnConceptAsync(string concept, Dictionary<string, double> features)
        {
            Console.WriteLine($"🎓 Learning concept: {concept}");
            
            var result = new LearningResult { Concept = concept };
            
            // Find or create relevant cluster
            var cluster = await FindOrCreateClusterForConcept(concept);
            result.ClusterId = cluster.ClusterId;
            
            // Get neurons for this concept
            var conceptNeurons = await cluster.FindNeuronsByConcept(concept);
            if (conceptNeurons.Count < 3) // Need more neurons for this concept
            {
                var newNeurons = await cluster.GrowForConcept(concept, 5);
                conceptNeurons.AddRange(newNeurons);
                result.NeuronsCreated = newNeurons.Count;
                TotalNeuronsCreated += newNeurons.Count;
            }
            
            // Train the neurons with the features
            foreach (var neuron in conceptNeurons)
            {
                await TrainNeuronWithFeatures(neuron, features);
            }
            
            // Create connections between related concepts
            await CreateConceptualConnections(concept, features);
            
            result.Success = true;
            result.NeuronsInvolved = conceptNeurons.Count;
            
            Console.WriteLine($"✅ Learned '{concept}' using {result.NeuronsInvolved} neurons in cluster {result.ClusterId:N}");
            
            return result;
        }

        /// <summary>
        /// Process input and generate response using relevant clusters
        /// </summary>
        public async Task<ProcessingResult> ProcessInputAsync(string input, Dictionary<string, double> features)
        {
            Console.WriteLine($"🤔 Processing input: {input}");
            
            var result = new ProcessingResult { Input = input };
            var activatedClusters = new List<Guid>();
            var neuronOutputs = new Dictionary<Guid, double>();
            
            // Extract concepts from input
            var inputConcepts = ExtractConcepts(input);
            
            // Find relevant clusters
            var relevantClusters = await FindRelevantClusters(inputConcepts);
            
            foreach (var cluster in relevantClusters.Take(5)) // Limit to top 5 clusters
            {
                var clusterOutputs = await cluster.ProcessInputAsync(ConvertFeaturesToNeuronInputs(features));
                
                foreach (var output in clusterOutputs)
                {
                    neuronOutputs[output.Key] = output.Value;
                }
                
                activatedClusters.Add(cluster.ClusterId);
            }
            
            // Generate response based on activated neurons
            result.Response = GenerateResponse(neuronOutputs, inputConcepts);
            result.ActivatedClusters = activatedClusters;
            result.ActivatedNeurons = neuronOutputs.Count;
            result.Confidence = CalculateConfidence(neuronOutputs);
            
            Console.WriteLine($"💭 Generated response with confidence {result.Confidence:F2}");
            
            return result;
        }

        /// <summary>
        /// Save brain state to disk
        /// </summary>
        public async Task SaveAsync()
        {
            Console.WriteLine("💾 Saving brain state...");
            
            // Save feature mappings
            var featureMappingSnapshot = _featureMapper.CreateSnapshot();
            await _storage.SaveFeatureMappingsAsync(featureMappingSnapshot);
            
            // Save loaded clusters
            foreach (var cluster in _loadedClusters.Values)
            {
                await cluster.PersistAndUnloadAsync();
            }
            
            // Save cluster index
            var clusterSnapshots = _loadedClusters.Values.Select(c => c.CreateSnapshot()).ToList();
            await _storage.SaveClusterIndexAsync(clusterSnapshots);
            
            // Save synapses
            var synapseSnapshots = _synapses.Values.Select(s => s.CreateSnapshot()).ToList();
            await _storage.SaveSynapsesAsync(synapseSnapshots);
            
            Console.WriteLine("✅ Brain state saved");
        }

        /// <summary>
        /// Cleanup - unload old clusters and prune weak connections
        /// </summary>
        public async Task MaintenanceAsync()
        {
            Console.WriteLine("🧹 Running brain maintenance...");
            
            int unloadedClusters = 0;
            int prunedSynapses = 0;
            
            // Unload old clusters
            var clustersToUnload = _loadedClusters.Values
                .Where(c => !c.ShouldStayLoaded())
                .ToList();
            
            foreach (var cluster in clustersToUnload)
            {
                await cluster.PersistAndUnloadAsync(forceUnload: true);
                _loadedClusters.Remove(cluster.ClusterId);
                unloadedClusters++;
            }
            
            // Prune weak synapses
            var weakSynapses = _synapses.Values
                .Where(s => s.ShouldBePruned())
                .Select(s => s.Id)
                .ToList();
            
            foreach (var synapseId in weakSynapses)
            {
                _synapses.Remove(synapseId);
                prunedSynapses++;
            }
            
            // Age remaining synapses
            foreach (var synapse in _synapses.Values)
            {
                synapse.Age(TimeSpan.FromHours(1));
            }
            
            Console.WriteLine($"🧹 Maintenance complete: unloaded {unloadedClusters} clusters, pruned {prunedSynapses} synapses");
        }

        /// <summary>
        /// Get brain statistics
        /// </summary>
        public async Task<BrainStats> GetStatsAsync()
        {
            var storageStats = await _storage.GetStorageStatsAsync();
            
            return new BrainStats
            {
                LoadedClusters = _loadedClusters.Count,
                TotalClusters = storageStats.ClusterCount,
                TotalSynapses = _synapses.Count,
                TotalNeuronsCreated = TotalNeuronsCreated,
                StorageSizeFormatted = storageStats.TotalSizeFormatted,
                UptimeFormatted = FormatTimeSpan(DateTime.UtcNow - CreatedAt)
            };
        }

        // Private helper methods

        private async Task<NeuronCluster> FindOrCreateClusterForConcept(string concept)
        {
            // Look for existing cluster with high relevance
            var relevantClusters = await FindRelevantClusters(new[] { concept });
            var bestCluster = relevantClusters.FirstOrDefault();
            
            if (bestCluster != null && bestCluster.CalculateRelevance(new[] { concept }) > ConceptSimilarityThreshold)
            {
                return bestCluster;
            }
            
            // Create new cluster
            var newCluster = new NeuronCluster(concept, _storage.LoadClusterAsync, _storage.SaveClusterAsync);
            _loadedClusters[newCluster.ClusterId] = newCluster;
            TotalClustersCreated++;
            
            return newCluster;
        }

        private async Task<List<NeuronCluster>> FindRelevantClusters(IEnumerable<string> concepts)
        {
            var allClusters = new List<NeuronCluster>(_loadedClusters.Values);
            
            // Load additional clusters if needed
            if (allClusters.Count < 3)
            {
                var clusterIndex = await _storage.LoadClusterIndexAsync();
                var conceptSet = concepts.Select(c => c.ToLowerInvariant()).ToHashSet();
                
                var relevantClusterSnapshots = clusterIndex
                    .Where(c => c.AssociatedConcepts.Any(ac => conceptSet.Contains(ac.ToLowerInvariant())))
                    .OrderByDescending(c => c.AverageImportance)
                    .Take(3)
                    .ToList();
                
                foreach (var snapshot in relevantClusterSnapshots)
                {
                    if (!_loadedClusters.ContainsKey(snapshot.ClusterId))
                    {
                        var cluster = new NeuronCluster(snapshot.ConceptDomain, _storage.LoadClusterAsync, _storage.SaveClusterAsync);
                        _loadedClusters[cluster.ClusterId] = cluster;
                        allClusters.Add(cluster);
                    }
                }
            }
            
            return allClusters
                .OrderByDescending(c => c.CalculateRelevance(concepts))
                .ToList();
        }

        private async Task TrainNeuronWithFeatures(HybridNeuron neuron, Dictionary<string, double> features)
        {
            // Convert features to consistent neuron inputs
            var inputs = _featureMapper.ConvertFeaturesToNeuronInputs(features);
            
            // Initialize connections if neuron has no weights
            if (!neuron.InputWeights.Any())
            {
                foreach (var feature in features)
                {
                    var featureNeuronId = _featureMapper.GetNeuronIdForFeature(feature.Key);
                    // Initialize with much larger weights for guaranteed activation
                    neuron.InputWeights[featureNeuronId] = (_random.NextDouble() + 0.5) * 3.0; // Range: 1.5 to 4.5
                }
            }
            
            // Process inputs and get output
            var output = neuron.ProcessInputs(inputs);
            
            // Train regardless of output (supervised learning)
            foreach (var feature in features)
            {
                var featureNeuronId = _featureMapper.GetNeuronIdForFeature(feature.Key);
                // Use a target activation of 0.8 for concept learning
                neuron.Learn(featureNeuronId, feature.Value, 0.8, output);
            }
            
            await Task.CompletedTask;
        }

        private async Task CreateConceptualConnections(string concept, Dictionary<string, double> features)
        {
            // Find related concepts and create synaptic connections
            var relatedClusters = await FindRelevantClusters(features.Keys);
            
            foreach (var cluster in relatedClusters.Take(2))
            {
                var clusterNeurons = await cluster.GetNeuronsAsync();
                var conceptCluster = await FindOrCreateClusterForConcept(concept);
                var conceptNeurons = await conceptCluster.GetNeuronsAsync();
                
                // Create a few random connections
                for (int i = 0; i < Math.Min(3, Math.Min(clusterNeurons.Count, conceptNeurons.Count)); i++)
                {
                    var sourceNeuron = clusterNeurons.Values.ElementAt(_random.Next(clusterNeurons.Count));
                    var targetNeuron = conceptNeurons.Values.ElementAt(_random.Next(conceptNeurons.Count));
                    
                    var synapse = new Synapse(sourceNeuron.Id, targetNeuron.Id, _random.NextDouble() * 0.2 - 0.1);
                    _synapses[synapse.Id] = synapse;
                }
            }
        }

        private Dictionary<Guid, double> ConvertFeaturesToNeuronInputs(Dictionary<string, double> features)
        {
            return _featureMapper.ConvertFeaturesToNeuronInputs(features);
        }

        private string[] ExtractConcepts(string input)
        {
            // Simple concept extraction - in practice would use NLP
            return input.ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length > 2)
                .Distinct()
                .ToArray();
        }

        private string GenerateResponse(Dictionary<Guid, double> neuronOutputs, string[] concepts)
        {
            if (!neuronOutputs.Any())
                return "I need to learn more about this.";
            
            var avgActivation = neuronOutputs.Values.Average();
            var maxActivation = neuronOutputs.Values.Max();
            var activationCount = neuronOutputs.Count;
            
            // Build response based on activation patterns
            var responseBuilder = new List<string>();
            
            if (maxActivation > 0.7)
            {
                responseBuilder.Add($"I recognize this strongly!");
                responseBuilder.Add($"It relates to: {string.Join(", ", concepts.Take(3))}");
            }
            else if (maxActivation > 0.4)
            {
                responseBuilder.Add($"This seems familiar to me.");
                responseBuilder.Add($"I associate it with: {string.Join(", ", concepts.Take(2))}");
            }
            else if (avgActivation > 0.2)
            {
                responseBuilder.Add($"I have some knowledge about this.");
                responseBuilder.Add($"Possibly related to: {concepts.FirstOrDefault() ?? "unknown"}");
            }
            else
            {
                responseBuilder.Add("This is quite new to me.");
                responseBuilder.Add("I'm learning from this input...");
            }
            
            // Add activation details
            if (activationCount > 0)
            {
                responseBuilder.Add($"({activationCount} neurons activated)");
            }
            
            return string.Join(" ", responseBuilder);
        }

        private double CalculateConfidence(Dictionary<Guid, double> neuronOutputs)
        {
            if (!neuronOutputs.Any()) return 0.0;
            
            var avgActivation = neuronOutputs.Values.Average();
            var maxActivation = neuronOutputs.Values.Max();
            
            return (avgActivation + maxActivation) / 2.0;
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
                return $"{timeSpan.Days}d {timeSpan.Hours}h";
            else if (timeSpan.TotalHours >= 1)
                return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
            else
                return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        }
    }

    // Result classes
    public class LearningResult
    {
        public string Concept { get; set; } = "";
        public Guid ClusterId { get; set; }
        public bool Success { get; set; }
        public int NeuronsCreated { get; set; }
        public int NeuronsInvolved { get; set; }
    }

    public class ProcessingResult
    {
        public string Input { get; set; } = "";
        public string Response { get; set; } = "";
        public List<Guid> ActivatedClusters { get; set; } = new();
        public int ActivatedNeurons { get; set; }
        public double Confidence { get; set; }
    }

    public class BrainStats
    {
        public int LoadedClusters { get; set; }
        public int TotalClusters { get; set; }
        public int TotalSynapses { get; set; }
        public int TotalNeuronsCreated { get; set; }
        public string StorageSizeFormatted { get; set; } = "";
        public string UptimeFormatted { get; set; } = "";
    }
}
