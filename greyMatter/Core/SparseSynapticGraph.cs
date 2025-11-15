using System;
using System.Collections.Generic;
using System.Linq;

namespace GreyMatter.Core
{
    /// <summary>
    /// ADPC-Net Phase 3: Sparse Synaptic Graph
    /// 
    /// Implements sparse connectivity between neurons based on Hebbian learning.
    /// Only stores synapses that have been strengthened through co-activation.
    /// 
    /// Key principles:
    /// - Sparse: Only active connections stored (not N² dense)
    /// - Hebbian: "Neurons that fire together, wire together"
    /// - Dynamic: Synapses strengthen with use, weaken without
    /// - Prunable: Weak connections removed to save memory
    /// </summary>
    public class SparseSynapticGraph
    {
        // Sparse synapse storage: (sourceId, targetId) → weight
        private readonly Dictionary<(Guid source, Guid target), float> _synapses;
        
        // Hebbian learning parameters
        private readonly float _learningRate;
        private readonly float _minWeight;
        private readonly float _maxWeight;
        private readonly float _pruneThreshold;
        
        // Statistics
        public int SynapseCount => _synapses.Count;
        public int TotalNeurons { get; private set; }
        
        /// <summary>
        /// Initialize sparse synaptic graph
        /// </summary>
        /// <param name="learningRate">Hebbian learning rate (default: 0.01)</param>
        /// <param name="minWeight">Minimum synapse weight (default: 0.0)</param>
        /// <param name="maxWeight">Maximum synapse weight (default: 1.0)</param>
        /// <param name="pruneThreshold">Prune synapses below this weight (default: 0.1)</param>
        public SparseSynapticGraph(
            float learningRate = 0.01f,
            float minWeight = 0.0f,
            float maxWeight = 1.0f,
            float pruneThreshold = 0.1f)
        {
            _synapses = new Dictionary<(Guid, Guid), float>();
            _learningRate = learningRate;
            _minWeight = minWeight;
            _maxWeight = maxWeight;
            _pruneThreshold = pruneThreshold;
            TotalNeurons = 0;
        }
        
        /// <summary>
        /// Record co-activation between two neurons (Hebbian learning)
        /// Δw = η * a_i * a_j
        /// </summary>
        /// <param name="sourceId">Source neuron ID</param>
        /// <param name="targetId">Target neuron ID</param>
        /// <param name="sourceActivation">Source neuron activation (0.0-1.0)</param>
        /// <param name="targetActivation">Target neuron activation (0.0-1.0)</param>
        public void RecordCoactivation(Guid sourceId, Guid targetId, float sourceActivation, float targetActivation)
        {
            // Skip self-connections
            if (sourceId == targetId)
                return;
            
            // Hebbian update: Δw = η * a_i * a_j
            var key = (sourceId, targetId);
            var deltaWeight = _learningRate * sourceActivation * targetActivation;
            
            if (_synapses.TryGetValue(key, out var currentWeight))
            {
                // Strengthen existing synapse
                var newWeight = Math.Clamp(currentWeight + deltaWeight, _minWeight, _maxWeight);
                _synapses[key] = newWeight;
            }
            else
            {
                // Create new synapse only if activation is strong enough
                // Use pruning threshold to determine if connection is worth creating
                var creationThreshold = Math.Max(_pruneThreshold * 0.5f, _learningRate * 0.5f);
                if (deltaWeight > creationThreshold)
                {
                    _synapses[key] = Math.Clamp(deltaWeight, _minWeight, _maxWeight);
                }
            }
        }
        
        /// <summary>
        /// Record co-activation between groups of neurons (batch Hebbian)
        /// </summary>
        /// <param name="activeNeurons">List of (neuronId, activation) pairs</param>
        public void RecordCoactivationPattern(List<(Guid neuronId, float activation)> activeNeurons)
        {
            // For each pair of active neurons, strengthen connection
            for (int i = 0; i < activeNeurons.Count; i++)
            {
                for (int j = i + 1; j < activeNeurons.Count; j++)
                {
                    var (sourceId, sourceAct) = activeNeurons[i];
                    var (targetId, targetAct) = activeNeurons[j];
                    
                    // Bidirectional strengthening
                    RecordCoactivation(sourceId, targetId, sourceAct, targetAct);
                    RecordCoactivation(targetId, sourceId, targetAct, sourceAct);
                }
            }
        }
        
        /// <summary>
        /// Get synapse weight between two neurons
        /// </summary>
        /// <returns>Weight (0.0 if no synapse exists)</returns>
        public float GetSynapseWeight(Guid sourceId, Guid targetId)
        {
            var key = (sourceId, targetId);
            return _synapses.TryGetValue(key, out var weight) ? weight : 0.0f;
        }
        
        /// <summary>
        /// Get total number of synapses in the graph
        /// </summary>
        public int GetSynapseCount()
        {
            return _synapses.Count;
        }
        
        /// <summary>
        /// Get all outgoing synapses from a neuron
        /// </summary>
        /// <returns>List of (targetId, weight) pairs</returns>
        public List<(Guid targetId, float weight)> GetOutgoingSynapses(Guid sourceId)
        {
            return _synapses
                .Where(kvp => kvp.Key.source == sourceId)
                .Select(kvp => (kvp.Key.target, kvp.Value))
                .ToList();
        }
        
        /// <summary>
        /// Get all incoming synapses to a neuron
        /// </summary>
        /// <returns>List of (sourceId, weight) pairs</returns>
        public List<(Guid sourceId, float weight)> GetIncomingSynapses(Guid targetId)
        {
            return _synapses
                .Where(kvp => kvp.Key.target == targetId)
                .Select(kvp => (kvp.Key.source, kvp.Value))
                .ToList();
        }
        
        /// <summary>
        /// Prune weak synapses below threshold
        /// Implements "lottery ticket" style pruning - keep only strong connections
        /// </summary>
        /// <returns>Number of synapses pruned</returns>
        public int PruneWeakSynapses()
        {
            var beforeCount = _synapses.Count;
            
            // Remove synapses below threshold
            var toRemove = _synapses
                .Where(kvp => kvp.Value < _pruneThreshold)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in toRemove)
            {
                _synapses.Remove(key);
            }
            
            return beforeCount - _synapses.Count;
        }
        
        /// <summary>
        /// Apply synaptic decay (forgetting)
        /// Weakens all synapses by a decay factor
        /// </summary>
        /// <param name="decayFactor">Decay multiplier (0.0-1.0, default 0.99)</param>
        /// <returns>Number of synapses that decayed below threshold</returns>
        public int ApplyDecay(float decayFactor = 0.99f)
        {
            var beforeCount = _synapses.Count;
            var toRemove = new List<(Guid, Guid)>();
            
            // Decay all synapses
            foreach (var key in _synapses.Keys.ToList())
            {
                var newWeight = _synapses[key] * decayFactor;
                
                if (newWeight < _pruneThreshold)
                {
                    toRemove.Add(key);
                }
                else
                {
                    _synapses[key] = newWeight;
                }
            }
            
            // Remove decayed synapses
            foreach (var key in toRemove)
            {
                _synapses.Remove(key);
            }
            
            return beforeCount - _synapses.Count;
        }
        
        /// <summary>
        /// Get graph statistics
        /// </summary>
        public SynapticGraphStats GetStats()
        {
            if (_synapses.Count == 0)
            {
                return new SynapticGraphStats
                {
                    TotalSynapses = 0,
                    AverageWeight = 0,
                    MaxWeight = 0,
                    MinWeight = 0,
                    SparsityRatio = 0
                };
            }
            
            var weights = _synapses.Values.ToList();
            var uniqueNeurons = new HashSet<Guid>();
            
            foreach (var (source, target) in _synapses.Keys)
            {
                uniqueNeurons.Add(source);
                uniqueNeurons.Add(target);
            }
            
            TotalNeurons = uniqueNeurons.Count;
            var maxPossibleSynapses = (long)TotalNeurons * (TotalNeurons - 1);
            
            return new SynapticGraphStats
            {
                TotalSynapses = _synapses.Count,
                UniqueNeurons = TotalNeurons,
                AverageWeight = weights.Average(),
                MaxWeight = weights.Max(),
                MinWeight = weights.Min(),
                SparsityRatio = maxPossibleSynapses > 0 
                    ? (double)_synapses.Count / maxPossibleSynapses 
                    : 0
            };
        }
        
        /// <summary>
        /// Export synapses for persistence
        /// Returns sparse representation: only non-zero weights
        /// </summary>
        public List<SynapseSnapshot> ExportSynapses()
        {
            return _synapses
                .Select(kvp => new SynapseSnapshot
                {
                    Id = Guid.NewGuid(),
                    PresynapticNeuronId = kvp.Key.source,
                    PostsynapticNeuronId = kvp.Key.target,
                    Weight = kvp.Value,
                    Strength = kvp.Value,
                    Type = SynapseType.Excitatory,
                    LastActive = DateTime.UtcNow,
                    TransmissionCount = 0,
                    AverageSignalStrength = kvp.Value,
                    PlasticityRate = _learningRate,
                    IsPlastic = true
                })
                .ToList();
        }
        
        /// <summary>
        /// Import synapses from persistence
        /// </summary>
        public void ImportSynapses(List<SynapseSnapshot> snapshots)
        {
            _synapses.Clear();
            
            foreach (var snapshot in snapshots)
            {
                var key = (snapshot.PresynapticNeuronId, snapshot.PostsynapticNeuronId);
                _synapses[key] = (float)snapshot.Weight;
            }
        }
        
        /// <summary>
        /// Clear all synapses
        /// </summary>
        public void Clear()
        {
            _synapses.Clear();
            TotalNeurons = 0;
        }
    }
    
    /// <summary>
    /// Statistics about the synaptic graph
    /// </summary>
    public class SynapticGraphStats
    {
        public int TotalSynapses { get; set; }
        public int UniqueNeurons { get; set; }
        public float AverageWeight { get; set; }
        public float MaxWeight { get; set; }
        public float MinWeight { get; set; }
        public double SparsityRatio { get; set; }  // Actual / Maximum possible
        
        public override string ToString()
        {
            return $"Synapses: {TotalSynapses:N0}, Neurons: {UniqueNeurons:N0}, " +
                   $"Avg Weight: {AverageWeight:F3}, Sparsity: {SparsityRatio:E2}";
        }
    }
}
