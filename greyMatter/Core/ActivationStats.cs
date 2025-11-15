using System;
using System.Collections.Generic;
using System.Linq;

namespace GreyMatter.Core
{
    /// <summary>
    /// Track activation statistics for regions and patterns
    /// Used for novelty detection and neuron budget calculation
    /// 
    /// Key metrics:
    /// - Activation frequency per region
    /// - Co-activation patterns
    /// - Novelty scores (new vs familiar)
    /// </summary>
    public class ActivationStats
    {
        // Region activation counts
        private readonly Dictionary<string, int> _regionActivations = new();
        
        // Pattern activation counts (feature vector hash → count)
        private readonly Dictionary<int, int> _patternActivations = new();
        
        // Co-activation patterns (region pair → count)
        private readonly Dictionary<(string, string), int> _coactivations = new();
        
        // Total activations (for frequency calculations)
        private int _totalActivations = 0;
        
        public ActivationStats()
        {
        }

        /// <summary>
        /// Record an activation event for a region
        /// </summary>
        public void RecordActivation(string regionId, double[] featureVector)
        {
            _totalActivations++;
            
            // Track region frequency
            if (!_regionActivations.ContainsKey(regionId))
                _regionActivations[regionId] = 0;
            _regionActivations[regionId]++;
            
            // Track pattern frequency
            var patternHash = HashFeatureVector(featureVector);
            if (!_patternActivations.ContainsKey(patternHash))
                _patternActivations[patternHash] = 0;
            _patternActivations[patternHash]++;
        }

        /// <summary>
        /// Record co-activation of multiple regions
        /// </summary>
        public void RecordCoactivation(List<string> activeRegions)
        {
            if (activeRegions.Count < 2)
                return;

            // Record all pairwise co-activations
            for (int i = 0; i < activeRegions.Count; i++)
            {
                for (int j = i + 1; j < activeRegions.Count; j++)
                {
                    var pair = (activeRegions[i], activeRegions[j]);
                    if (!_coactivations.ContainsKey(pair))
                        _coactivations[pair] = 0;
                    _coactivations[pair]++;
                }
            }
        }

        /// <summary>
        /// Calculate novelty score for a pattern
        /// Returns 0.0 (very familiar) to 1.0 (completely novel)
        /// </summary>
        public double CalculateNovelty(string regionId, double[] featureVector)
        {
            var regionFreq = _regionActivations.GetValueOrDefault(regionId, 0);
            var patternHash = HashFeatureVector(featureVector);
            var patternFreq = _patternActivations.GetValueOrDefault(patternHash, 0);
            
            // Novelty components:
            // 1. Region novelty (haven't seen this region much)
            // 2. Pattern novelty (haven't seen this exact pattern)
            
            var regionNovelty = 1.0 / (1.0 + regionFreq);
            var patternNovelty = 1.0 / (1.0 + patternFreq);
            
            // Combined novelty (weighted average)
            var novelty = 0.3 * regionNovelty + 0.7 * patternNovelty;
            
            return Math.Clamp(novelty, 0.0, 1.0);
        }

        /// <summary>
        /// Get activation frequency for a region
        /// Returns probability ∈ [0, 1]
        /// </summary>
        public double GetRegionFrequency(string regionId)
        {
            if (_totalActivations == 0)
                return 0;
                
            var count = _regionActivations.GetValueOrDefault(regionId, 0);
            return (double)count / _totalActivations;
        }

        /// <summary>
        /// Get co-activation strength between two regions
        /// Returns normalized count ∈ [0, ∞)
        /// </summary>
        public double GetCoactivationStrength(string region1, string region2)
        {
            var pair = (region1, region2);
            var reversePair = (region2, region1);
            
            var count = _coactivations.GetValueOrDefault(pair, 0) + 
                       _coactivations.GetValueOrDefault(reversePair, 0);
            
            return (double)count / Math.Max(1, _totalActivations);
        }

        /// <summary>
        /// Get top N most frequent regions
        /// </summary>
        public List<(string regionId, int count)> GetTopRegions(int n = 10)
        {
            return _regionActivations
                .OrderByDescending(kvp => kvp.Value)
                .Take(n)
                .Select(kvp => (kvp.Key, kvp.Value))
                .ToList();
        }

        /// <summary>
        /// Get top N most frequent co-activation pairs
        /// </summary>
        public List<((string, string) pair, int count)> GetTopCoactivations(int n = 10)
        {
            return _coactivations
                .OrderByDescending(kvp => kvp.Value)
                .Take(n)
                .Select(kvp => (kvp.Key, kvp.Value))
                .ToList();
        }

        /// <summary>
        /// Get statistics summary
        /// </summary>
        public ActivationStatsSummary GetSummary()
        {
            return new ActivationStatsSummary
            {
                TotalActivations = _totalActivations,
                UniqueRegions = _regionActivations.Count,
                UniquePatterns = _patternActivations.Count,
                UniqueCoactivations = _coactivations.Count,
                AverageRegionFrequency = _regionActivations.Count > 0
                    ? _regionActivations.Values.Average()
                    : 0,
                MostFrequentRegion = _regionActivations.Count > 0
                    ? _regionActivations.OrderByDescending(kvp => kvp.Value).First().Key
                    : "none"
            };
        }

        /// <summary>
        /// Reset all statistics (for fresh start)
        /// </summary>
        public void Reset()
        {
            _regionActivations.Clear();
            _patternActivations.Clear();
            _coactivations.Clear();
            _totalActivations = 0;
        }

        /// <summary>
        /// Merge statistics from another instance (for distributed learning)
        /// </summary>
        public void Merge(ActivationStats other)
        {
            _totalActivations += other._totalActivations;
            
            foreach (var kvp in other._regionActivations)
            {
                _regionActivations[kvp.Key] = 
                    _regionActivations.GetValueOrDefault(kvp.Key, 0) + kvp.Value;
            }
            
            foreach (var kvp in other._patternActivations)
            {
                _patternActivations[kvp.Key] = 
                    _patternActivations.GetValueOrDefault(kvp.Key, 0) + kvp.Value;
            }
            
            foreach (var kvp in other._coactivations)
            {
                _coactivations[kvp.Key] = 
                    _coactivations.GetValueOrDefault(kvp.Key, 0) + kvp.Value;
            }
        }

        private int HashFeatureVector(double[] vector)
        {
            // Hash feature vector to detect exact pattern matches
            // Use quantized values to allow small floating-point variations
            unchecked
            {
                int hash = 17;
                foreach (var value in vector)
                {
                    var quantized = (int)(value * 1000); // 3 decimal places
                    hash = hash * 31 + quantized;
                }
                return hash;
            }
        }
    }

    /// <summary>
    /// Summary of activation statistics
    /// </summary>
    public class ActivationStatsSummary
    {
        public int TotalActivations { get; set; }
        public int UniqueRegions { get; set; }
        public int UniquePatterns { get; set; }
        public int UniqueCoactivations { get; set; }
        public double AverageRegionFrequency { get; set; }
        public string MostFrequentRegion { get; set; } = "";

        public override string ToString()
        {
            return $"Activation Stats:\n" +
                   $"  Total activations: {TotalActivations:N0}\n" +
                   $"  Unique regions: {UniqueRegions:N0}\n" +
                   $"  Unique patterns: {UniquePatterns:N0}\n" +
                   $"  Co-activations: {UniqueCoactivations:N0}\n" +
                   $"  Avg region freq: {AverageRegionFrequency:F2}\n" +
                   $"  Most frequent: {MostFrequentRegion}";
        }
    }
}
