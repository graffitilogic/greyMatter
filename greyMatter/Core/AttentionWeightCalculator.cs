using System;
using System.Collections.Generic;
using System.Linq;

namespace GreyMatter.Core
{
    /// <summary>
    /// Simple pattern structure for attention calculation
    /// </summary>
    public class AttentionPattern
    {
        public string PatternType { get; set; } = "";
        public double Confidence { get; set; }
    }

    /// <summary>
    /// Calculates attention weights for cortical columns based on:
    /// - Pattern novelty (new patterns get more attention)
    /// - Confidence levels (high confidence patterns need less verification)
    /// - Recent activity (recently active columns get boosted)
    /// - Learning progress (columns showing improvement get priority)
    /// 
    /// Biological analogy: Selective attention - brain focuses on salient stimuli
    /// </summary>
    public class AttentionWeightCalculator
    {
        private readonly Dictionary<string, ColumnAttentionState> _columnStates;
        private readonly AttentionConfiguration _config;
        private readonly Dictionary<string, double> _recentWeights; // Cache for performance
        
        public AttentionWeightCalculator(AttentionConfiguration? config = null)
        {
            _columnStates = new Dictionary<string, ColumnAttentionState>();
            _config = config ?? AttentionConfiguration.Default;
            _recentWeights = new Dictionary<string, double>();
        }

        /// <summary>
        /// Calculate attention weight for a column based on current state
        /// Returns 0.0 to 1.0, where 1.0 means highest priority
        /// </summary>
        public double CalculateWeight(string columnId, AttentionPattern pattern)
        {
            // Get or create state for this column
            if (!_columnStates.ContainsKey(columnId))
            {
                _columnStates[columnId] = new ColumnAttentionState(columnId);
            }
            
            var state = _columnStates[columnId];
            
            // Calculate component weights
            var noveltyWeight = CalculateNoveltyWeight(state, pattern);
            var confidenceWeight = CalculateConfidenceWeight(pattern);
            var recencyWeight = CalculateRecencyWeight(state);
            var progressWeight = CalculateProgressWeight(state);
            
            // Weighted combination
            var totalWeight = 
                (noveltyWeight * _config.NoveltyImportance) +
                (confidenceWeight * _config.ConfidenceImportance) +
                (recencyWeight * _config.RecencyImportance) +
                (progressWeight * _config.ProgressImportance);
            
            // Normalize to 0-1 range
            var normalizedWeight = Math.Clamp(totalWeight, 0.0, 1.0);
            
            // Update state
            state.UpdateWithPattern(pattern, normalizedWeight);
            _recentWeights[columnId] = normalizedWeight;
            
            return normalizedWeight;
        }

        /// <summary>
        /// Get attention weight for a column without updating state (cached)
        /// </summary>
        public double GetCurrentWeight(string columnId)
        {
            return _recentWeights.GetValueOrDefault(columnId, _config.DefaultAttentionWeight);
        }

        /// <summary>
        /// Get top N columns by attention weight
        /// </summary>
        public List<(string columnId, double weight)> GetTopAttentionColumns(int count)
        {
            return _recentWeights
                .OrderByDescending(kvp => kvp.Value)
                .Take(count)
                .Select(kvp => (kvp.Key, kvp.Value))
                .ToList();
        }

        /// <summary>
        /// Calculate novelty weight: new patterns deserve more attention
        /// </summary>
        private double CalculateNoveltyWeight(ColumnAttentionState state, AttentionPattern pattern)
        {
            // Check if this is a new pattern type
            var isNewPattern = !state.SeenPatternTypes.Contains(pattern.PatternType);
            
            if (isNewPattern)
            {
                return 1.0; // Maximum attention for completely new patterns
            }
            
            // Calculate staleness: how long since we saw a similar pattern
            var timeSinceLastSimilar = DateTime.Now - state.LastPatternTime;
            var stalenessFactor = Math.Min(timeSinceLastSimilar.TotalMinutes / 60.0, 1.0); // 0-1 over 1 hour
            
            return 0.3 + (0.7 * stalenessFactor); // Base 0.3, up to 1.0 if stale
        }

        /// <summary>
        /// Calculate confidence weight: low confidence needs verification
        /// </summary>
        private double CalculateConfidenceWeight(AttentionPattern pattern)
        {
            // Inverse relationship: low confidence = high attention needed
            return 1.0 - pattern.Confidence;
        }

        /// <summary>
        /// Calculate recency weight: recently active columns maintain momentum
        /// </summary>
        private double CalculateRecencyWeight(ColumnAttentionState state)
        {
            var timeSinceActive = DateTime.Now - state.LastUpdateTime;
            var recencyFactor = Math.Exp(-timeSinceActive.TotalSeconds / 30.0); // Decay over 30 seconds
            
            return recencyFactor;
        }

        /// <summary>
        /// Calculate progress weight: columns showing learning progress get priority
        /// </summary>
        private double CalculateProgressWeight(ColumnAttentionState state)
        {
            if (state.PatternHistory.Count < 2)
            {
                return 0.5; // Neutral for insufficient history
            }
            
            // Check if confidence is improving over recent patterns
            var recentPatterns = state.PatternHistory.TakeLast(5).ToList();
            if (recentPatterns.Count < 2) return 0.5;
            
            var firstConfidence = recentPatterns.First().confidence;
            var lastConfidence = recentPatterns.Last().confidence;
            var improvement = lastConfidence - firstConfidence;
            
            // Positive improvement = higher weight
            return 0.5 + (improvement * 0.5); // 0-1 range
        }

        /// <summary>
        /// Get attention statistics for monitoring
        /// </summary>
        public AttentionStatistics GetStatistics()
        {
            return new AttentionStatistics
            {
                TotalColumns = _columnStates.Count,
                ActiveColumns = _columnStates.Count(kvp => kvp.Value.IsActive),
                AverageWeight = _recentWeights.Values.Any() ? _recentWeights.Values.Average() : 0.0,
                TopColumn = _recentWeights.Any() 
                    ? _recentWeights.OrderByDescending(kvp => kvp.Value).First().Key 
                    : "none",
                TotalPatternsProcessed = _columnStates.Sum(kvp => kvp.Value.PatternCount)
            };
        }

        /// <summary>
        /// Reset attention state (useful for testing or fresh starts)
        /// </summary>
        public void Reset()
        {
            _columnStates.Clear();
            _recentWeights.Clear();
        }
    }

    /// <summary>
    /// Tracks attention state for a single column
    /// </summary>
    public class ColumnAttentionState
    {
        public string ColumnId { get; }
        public HashSet<string> SeenPatternTypes { get; }
        public List<(DateTime time, double confidence)> PatternHistory { get; }
        public DateTime LastPatternTime { get; private set; }
        public DateTime LastUpdateTime { get; private set; }
        public int PatternCount { get; private set; }
        public bool IsActive => (DateTime.Now - LastUpdateTime).TotalSeconds < 60;

        public ColumnAttentionState(string columnId)
        {
            ColumnId = columnId;
            SeenPatternTypes = new HashSet<string>();
            PatternHistory = new List<(DateTime, double)>();
            LastPatternTime = DateTime.MinValue;
            LastUpdateTime = DateTime.MinValue;
        }

        public void UpdateWithPattern(AttentionPattern pattern, double attentionWeight)
        {
            SeenPatternTypes.Add(pattern.PatternType);
            PatternHistory.Add((DateTime.Now, pattern.Confidence));
            LastPatternTime = DateTime.Now;
            LastUpdateTime = DateTime.Now;
            PatternCount++;
            
            // Keep history manageable
            if (PatternHistory.Count > 100)
            {
                PatternHistory.RemoveRange(0, 50);
            }
        }
    }

    /// <summary>
    /// Configuration for attention weight calculation
    /// </summary>
    public class AttentionConfiguration
    {
        public double NoveltyImportance { get; set; } = 0.4;    // 40% weight to novelty
        public double ConfidenceImportance { get; set; } = 0.3; // 30% weight to confidence
        public double RecencyImportance { get; set; } = 0.2;    // 20% weight to recency
        public double ProgressImportance { get; set; } = 0.1;   // 10% weight to progress
        public double DefaultAttentionWeight { get; set; } = 0.5;

        public static AttentionConfiguration Default => new AttentionConfiguration();
        
        /// <summary>
        /// Configuration optimized for novelty detection
        /// </summary>
        public static AttentionConfiguration NoveltyFocused => new AttentionConfiguration
        {
            NoveltyImportance = 0.6,
            ConfidenceImportance = 0.2,
            RecencyImportance = 0.1,
            ProgressImportance = 0.1
        };
        
        /// <summary>
        /// Configuration optimized for learning progress
        /// </summary>
        public static AttentionConfiguration ProgressFocused => new AttentionConfiguration
        {
            NoveltyImportance = 0.2,
            ConfidenceImportance = 0.2,
            RecencyImportance = 0.2,
            ProgressImportance = 0.4
        };
    }

    /// <summary>
    /// Statistics about attention system performance
    /// </summary>
    public class AttentionStatistics
    {
        public int TotalColumns { get; set; }
        public int ActiveColumns { get; set; }
        public double AverageWeight { get; set; }
        public string TopColumn { get; set; } = "";
        public long TotalPatternsProcessed { get; set; }
    }
}
