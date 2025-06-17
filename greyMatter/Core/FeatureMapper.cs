using System;
using System.Collections.Generic;
using System.Linq;

namespace GreyMatter.Core
{
    /// <summary>
    /// Feature mapping system to maintain consistent mappings between 
    /// feature names and neuron IDs across the brain
    /// </summary>
    public class FeatureMapper
    {
        private readonly Dictionary<string, Guid> _featureToNeuronId = new();
        private readonly Dictionary<Guid, string> _neuronIdToFeature = new();
        private readonly Random _random = new();

        /// <summary>
        /// Get or create a consistent neuron ID for a feature
        /// </summary>
        public Guid GetNeuronIdForFeature(string featureName)
        {
            featureName = featureName.ToLowerInvariant();
            
            if (!_featureToNeuronId.ContainsKey(featureName))
            {
                var neuronId = Guid.NewGuid();
                _featureToNeuronId[featureName] = neuronId;
                _neuronIdToFeature[neuronId] = featureName;
            }
            
            return _featureToNeuronId[featureName];
        }

        /// <summary>
        /// Get feature name for a neuron ID
        /// </summary>
        public string? GetFeatureForNeuronId(Guid neuronId)
        {
            return _neuronIdToFeature.GetValueOrDefault(neuronId);
        }

        /// <summary>
        /// Convert feature dictionary to neuron input format
        /// </summary>
        public Dictionary<Guid, double> ConvertFeaturesToNeuronInputs(Dictionary<string, double> features)
        {
            var neuronInputs = new Dictionary<Guid, double>();
            
            foreach (var feature in features)
            {
                var neuronId = GetNeuronIdForFeature(feature.Key);
                neuronInputs[neuronId] = feature.Value;
            }
            
            return neuronInputs;
        }

        /// <summary>
        /// Get all registered features
        /// </summary>
        public IEnumerable<string> GetAllFeatures()
        {
            return _featureToNeuronId.Keys;
        }

        /// <summary>
        /// Create feature mapping snapshot for persistence
        /// </summary>
        public FeatureMappingSnapshot CreateSnapshot()
        {
            return new FeatureMappingSnapshot
            {
                FeatureMappings = _featureToNeuronId.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }

        /// <summary>
        /// Restore from snapshot
        /// </summary>
        public void RestoreFromSnapshot(FeatureMappingSnapshot snapshot)
        {
            _featureToNeuronId.Clear();
            _neuronIdToFeature.Clear();
            
            foreach (var mapping in snapshot.FeatureMappings)
            {
                _featureToNeuronId[mapping.Key] = mapping.Value;
                _neuronIdToFeature[mapping.Value] = mapping.Key;
            }
        }
    }

    /// <summary>
    /// Snapshot of feature mappings for persistence
    /// </summary>
    public class FeatureMappingSnapshot
    {
        public Dictionary<string, Guid> FeatureMappings { get; set; } = new();
    }
}
