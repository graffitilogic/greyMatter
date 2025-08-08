using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GreyMatter.Core
{
    /// <summary>
    /// NeuronCluster: Lazy-loaded groups of neurons that can be persisted to disk
    /// Enables scaling by loading only relevant clusters into memory
    /// </summary>
    public class NeuronCluster
    {
        public Guid ClusterId { get; private set; } = Guid.NewGuid();
        public string ConceptDomain { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime LastAccessed { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        
        // Lazy loading state
        private bool _isLoaded = false;
        private Dictionary<Guid, HybridNeuron> _neurons = new();
        private readonly object _loadLock = new object();
        
        // Cluster metadata (always in memory)
        public HashSet<string> AssociatedConcepts { get; private set; } = new();
        public int NeuronCount => _isLoaded ? _neurons.Count : _persistedNeuronCount;
        public double AverageImportance { get; private set; }
        public int TotalActivations { get; private set; }
        
        // Persistence metadata
        private int _persistedNeuronCount = 0;
        private bool _isDirty = false;
        public bool IsDirty => _isDirty;
        public bool HasUnsavedChanges => _isDirty;
        private string _persistencePath = "";
        
        // Clustering parameters
        public double CohesionStrength { get; private set; } = 0.0; // How tightly connected neurons are
        public double SpecializationLevel { get; private set; } = 0.0; // How specialized this cluster is
        
        // Dependencies for lazy loading
        private readonly Func<string, Task<List<NeuronSnapshot>>>? _loadFunction;
        private readonly Func<string, List<NeuronSnapshot>, Task>? _saveFunction;

        public NeuronCluster(string conceptDomain, 
                           Func<string, Task<List<NeuronSnapshot>>>? loadFunc = null,
                           Func<string, List<NeuronSnapshot>, Task>? saveFunc = null)
        {
            ConceptDomain = conceptDomain.ToLowerInvariant();
            _persistencePath = $"clusters/{ConceptDomain}_{ClusterId:N}.cluster";
            _loadFunction = loadFunc;
            _saveFunction = saveFunc;
        }

        /// <summary>
        /// Get neurons, loading from disk if necessary
        /// </summary>
        public async Task<Dictionary<Guid, HybridNeuron>> GetNeuronsAsync()
        {
            if (!_isLoaded)
            {
                await LoadFromDiskAsync();
            }
            
            LastAccessed = DateTime.UtcNow;
            return _neurons;
        }

        /// <summary>
        /// Add neuron to cluster
        /// </summary>
        public async Task AddNeuronAsync(HybridNeuron neuron)
        {
            await EnsureLoadedAsync();
            
            _neurons[neuron.Id] = neuron;
            _isDirty = true;
            LastModified = DateTime.UtcNow;
            
            // Update cluster metadata
            AssociatedConcepts.UnionWith(neuron.AssociatedConcepts);
            UpdateClusterMetrics();
            
            // Associate neuron with this cluster's domain
            neuron.AssociateConcept(ConceptDomain);
        }

        /// <summary>
        /// Get specific neuron by ID
        /// </summary>
        public async Task<HybridNeuron?> GetNeuronAsync(Guid neuronId)
        {
            await EnsureLoadedAsync();
            return _neurons.GetValueOrDefault(neuronId);
        }

        /// <summary>
        /// Remove neuron from cluster
        /// </summary>
        public async Task<bool> RemoveNeuronAsync(Guid neuronId)
        {
            await EnsureLoadedAsync();
            
            if (_neurons.Remove(neuronId))
            {
                _isDirty = true;
                LastModified = DateTime.UtcNow;
                UpdateClusterMetrics();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Find neurons by concept
        /// </summary>
        public async Task<List<HybridNeuron>> FindNeuronsByConcept(string concept)
        {
            await EnsureLoadedAsync();
            
            concept = concept.ToLowerInvariant();
            return _neurons.Values
                .Where(n => n.AssociatedConcepts.Contains(concept) || 
                           n.ConceptTag.Contains(concept))
                .ToList();
        }

        /// <summary>
        /// Process input through the cluster
        /// </summary>
        public async Task<Dictionary<Guid, double>> ProcessInputAsync(Dictionary<Guid, double> inputs)
        {
            await EnsureLoadedAsync();
            LastAccessed = DateTime.UtcNow;
            
            var outputs = new Dictionary<Guid, double>();
            
            // Process each neuron in the cluster
            foreach (var neuron in _neurons.Values)
            {
                var output = neuron.ProcessInputs(inputs);
                if (Math.Abs(output) > 0.001) // Only store significant outputs
                {
                    outputs[neuron.Id] = output;
                }
            }
            
            // Update metrics
            TotalActivations += outputs.Count;
            // Note: do NOT mark cluster dirty on activation alone; STM remains in neurons
            
            return outputs;
        }

        /// <summary>
        /// Promote short-term (STM) changes in neurons to long-term memory (LTM) with a budget.
        /// Marks cluster dirty only if any neuron's LTM actually changed.
        /// Returns number of neurons consolidated.
        /// </summary>
        public async Task<int> ConsolidateStmAsync(int maxNeurons = 10, double epsilon = 1e-3)
        {
            await EnsureLoadedAsync();

            // Choose top candidates by salience
            var candidates = _neurons.Values
                .Where(n => n.HasPendingStm)
                .OrderByDescending(n => n.StmSalience)
                .Take(Math.Max(0, maxNeurons))
                .ToList();

            int promoted = 0;
            foreach (var n in candidates)
            {
                if (n.ConsolidateToLtm(epsilon))
                    promoted++;
            }

            // Note: With global neuron store, LTM changes do not require cluster file rewrite.
            // Do not mark cluster dirty here; neuron banks will be persisted separately.

            return promoted;
        }

        /// <summary>
        /// Consolidate STM and collect neurons that had LTM changes. Does not mark cluster dirty.
        /// </summary>
        public async Task<List<HybridNeuron>> ConsolidateStmCollectAsync(int maxNeurons = 10, double epsilon = 1e-3)
        {
            await EnsureLoadedAsync();

            var candidates = _neurons.Values
                .Where(n => n.HasPendingStm)
                .OrderByDescending(n => n.StmSalience)
                .Take(Math.Max(0, maxNeurons))
                .ToList();

            var changed = new List<HybridNeuron>();
            foreach (var n in candidates)
            {
                if (n.ConsolidateToLtm(epsilon))
                    changed.Add(n);
            }

            return changed;
        }

        /// <summary>
        /// Save cluster to disk and unload from memory if appropriate
        /// </summary>
        public async Task PersistAndUnloadAsync(bool forceUnload = false)
        {
            if (!_isLoaded) return;
            
            if (_isDirty || forceUnload)
            {
                await SaveToDiskAsync();
            }
            
            // Unload if not recently used or forced
            var timeSinceAccess = DateTime.UtcNow - LastAccessed;
            if (forceUnload || timeSinceAccess.TotalMinutes > 30)
            {
                UnloadFromMemory();
            }
        }

        /// <summary>
        /// Check if cluster is relevant for given concepts
        /// </summary>
        public double CalculateRelevance(IEnumerable<string> concepts)
        {
            var conceptSet = concepts.Select(c => c.ToLowerInvariant()).ToHashSet();
            var matchingConcepts = AssociatedConcepts.Intersect(conceptSet).Count();
            var totalConcepts = Math.Max(AssociatedConcepts.Count, conceptSet.Count);
            
            return totalConcepts > 0 ? (double)matchingConcepts / totalConcepts : 0.0;
        }

        /// <summary>
        /// Determine if cluster should be kept in memory
        /// </summary>
        public bool ShouldStayLoaded()
        {
            return AverageImportance > 0.5 || 
                   (DateTime.UtcNow - LastAccessed).TotalMinutes < 10 ||
                   _isDirty;
        }

        private async Task EnsureLoadedAsync()
        {
            if (!_isLoaded)
            {
                await LoadFromDiskAsync();
            }
        }

        private async Task LoadFromDiskAsync()
        {
            lock (_loadLock)
            {
                if (_isLoaded) return;
            }

            try
            {
                if (_loadFunction != null)
                {
                    var snapshots = await _loadFunction(_persistencePath);
                    _neurons = snapshots.ToDictionary(s => s.Id, HybridNeuron.FromSnapshot);
                }
                
                _isLoaded = true;
                _isDirty = false;
                LastAccessed = DateTime.UtcNow;
                UpdateClusterMetrics();
            }
            catch (Exception)
            {
                // If loading fails, start with empty cluster
                _neurons = new Dictionary<Guid, HybridNeuron>();
                _isLoaded = true;
            }
        }

        private async Task SaveToDiskAsync()
        {
            if (_saveFunction != null && _isLoaded)
            {
                var snapshots = _neurons.Values.Select(n => n.CreateSnapshot()).ToList();
                await _saveFunction(_persistencePath, snapshots);
                _persistedNeuronCount = snapshots.Count;
                _isDirty = false;
            }
        }

        private void UnloadFromMemory()
        {
            _persistedNeuronCount = _neurons.Count;
            _neurons.Clear();
            _isLoaded = false;
        }

        private Task ConnectToExistingNeurons(HybridNeuron newNeuron, List<HybridNeuron> existingNeurons)
        {
            // Connect to a few random existing neurons to maintain cluster cohesion
            var connectionsToMake = Math.Min(3, existingNeurons.Count);
            var random = new Random();
            
            for (int i = 0; i < connectionsToMake; i++)
            {
                var targetNeuron = existingNeurons[random.Next(existingNeurons.Count)];
                newNeuron.ConnectTo(targetNeuron.Id, random.NextDouble() * 0.2 - 0.1); // Small random weight
            }
            
            return Task.CompletedTask;
        }

        private void UpdateClusterMetrics()
        {
            if (!_isLoaded || !_neurons.Any()) return;
            
            AverageImportance = _neurons.Values.Average(n => n.ImportanceScore);
            
            // Calculate cohesion (how connected neurons are within cluster)
            var totalConnections = _neurons.Values.Sum(n => n.InputWeights.Count + n.OutputConnections.Count);
            var maxPossibleConnections = _neurons.Count * (_neurons.Count - 1);
            CohesionStrength = maxPossibleConnections > 0 ? (double)totalConnections / maxPossibleConnections : 0.0;
            
            // Calculate specialization (how focused on specific concepts)
            var conceptVariety = AssociatedConcepts.Count;
            SpecializationLevel = conceptVariety > 0 ? 1.0 / Math.Log(conceptVariety + 1) : 1.0;
        }

        /// <summary>
        /// Create snapshot of cluster metadata for persistence index
        /// </summary>
        public ClusterSnapshot CreateSnapshot()
        {
            return new ClusterSnapshot
            {
                ClusterId = ClusterId,
                ConceptDomain = ConceptDomain,
                AssociatedConcepts = AssociatedConcepts.ToList(),
                NeuronCount = NeuronCount,
                AverageImportance = AverageImportance,
                CohesionStrength = CohesionStrength,
                SpecializationLevel = SpecializationLevel,
                LastAccessed = LastAccessed,
                LastModified = LastModified,
                PersistencePath = _persistencePath
            };
        }

        /// <summary>
        /// Grow cluster by adding new neurons for a concept
        /// </summary>
        public async Task<List<HybridNeuron>> GrowForConcept(string concept, int targetSize = 10)
        {
            await EnsureLoadedAsync();
            
            var existingNeurons = await FindNeuronsByConcept(concept);
            var neuronsToCreate = Math.Max(0, targetSize - existingNeurons.Count);
            
            var newNeurons = new List<HybridNeuron>();
            
            for (int i = 0; i < neuronsToCreate; i++)
            {
                var newNeuron = new HybridNeuron(concept);
                newNeuron.AssociateConcept(concept);
                
                // Create connections to existing neurons in cluster
                await ConnectToExistingNeurons(newNeuron, existingNeurons);
                
                await AddNeuronAsync(newNeuron);
                newNeurons.Add(newNeuron);
            }
            
            UpdateClusterMetrics();
            return newNeurons;
        }

        /// <summary>
        /// Shrink cluster by removing least important neurons
        /// </summary>
        public async Task<int> ShrinkCluster(double importanceThreshold = 0.1)
        {
            await EnsureLoadedAsync();
            
            var neuronsToRemove = _neurons.Values
                .Where(n => !n.ShouldPersist() || n.ImportanceScore < importanceThreshold)
                .Select(n => n.Id)
                .ToList();
            
            int removedCount = 0;
            foreach (var neuronId in neuronsToRemove)
            {
                if (await RemoveNeuronAsync(neuronId))
                    removedCount++;
            }
            
            return removedCount;
        }

        /// <summary>
        /// Sum of pending STM salience for neurons in this cluster.
        /// </summary>
        public double GetPendingStmSalience()
        {
            if (!_isLoaded || _neurons.Count == 0) return 0.0;
            return _neurons.Values.Where(n => n.HasPendingStm).Sum(n => n.StmSalience);
        }
    }

    /// <summary>
    /// Lightweight cluster metadata for indexing
    /// </summary>
    public class ClusterSnapshot
    {
        public Guid ClusterId { get; set; }
        public string ConceptDomain { get; set; } = "";
        public List<string> AssociatedConcepts { get; set; } = new();
        public int NeuronCount { get; set; }
        public double AverageImportance { get; set; }
        public double CohesionStrength { get; set; }
        public double SpecializationLevel { get; set; }
        public DateTime LastAccessed { get; set; }
        public DateTime LastModified { get; set; }
        public string PersistencePath { get; set; } = "";
    }
}