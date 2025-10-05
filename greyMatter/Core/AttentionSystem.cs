using System;
using System.Collections.Generic;
using System.Linq;
using GreyMatter.Core;

namespace GreyMatter.Core
{
    /// <summary>
    /// Attention system for cortical columns
    /// Implements selective focus mechanism - determines which columns should be active
    /// for current task, similar to attentional spotlight in biological cognition
    /// </summary>
    public class AttentionSystem
    {
        private readonly Dictionary<string, double> _columnActivations;
        private readonly Dictionary<string, AttentionProfile> _profiles;
        private AttentionProfile? _currentProfile;
        private readonly double _baselineActivation;
        private readonly double _decayRate;

        /// <summary>
        /// Current attention profile (defines task-specific column weights)
        /// </summary>
        public AttentionProfile? CurrentProfile
        {
            get => _currentProfile;
            set => _currentProfile = value;
        }

        /// <summary>
        /// All registered attention profiles
        /// </summary>
        public IReadOnlyDictionary<string, AttentionProfile> Profiles => _profiles;

        public AttentionSystem(double baselineActivation = 0.1, double decayRate = 0.05)
        {
            _columnActivations = new Dictionary<string, double>();
            _profiles = new Dictionary<string, AttentionProfile>();
            _baselineActivation = baselineActivation;
            _decayRate = decayRate;

            InitializeDefaultProfiles();
        }

        /// <summary>
        /// Initialize default attention profiles for common tasks
        /// </summary>
        private void InitializeDefaultProfiles()
        {
            // Comprehension task: focus on semantic and contextual
            _profiles["comprehension"] = new AttentionProfile
            {
                Name = "comprehension",
                Description = "Understanding meaning and context",
                ColumnTypeWeights = new Dictionary<string, double>
                {
                    ["phonetic"] = 0.1,
                    ["semantic"] = 0.5,
                    ["syntactic"] = 0.2,
                    ["contextual"] = 0.3,
                    ["episodic"] = 0.1
                }
            };

            // Production task: focus on syntactic and contextual
            _profiles["production"] = new AttentionProfile
            {
                Name = "production",
                Description = "Generating language output",
                ColumnTypeWeights = new Dictionary<string, double>
                {
                    ["phonetic"] = 0.2,
                    ["semantic"] = 0.3,
                    ["syntactic"] = 0.4,
                    ["contextual"] = 0.3,
                    ["episodic"] = 0.1
                }
            };

            // Listening task: focus on phonetic and semantic
            _profiles["listening"] = new AttentionProfile
            {
                Name = "listening",
                Description = "Processing auditory input",
                ColumnTypeWeights = new Dictionary<string, double>
                {
                    ["phonetic"] = 0.5,
                    ["semantic"] = 0.4,
                    ["syntactic"] = 0.1,
                    ["contextual"] = 0.1,
                    ["episodic"] = 0.1
                }
            };

            // Reading task: focus on semantic and syntactic
            _profiles["reading"] = new AttentionProfile
            {
                Name = "reading",
                Description = "Processing written text",
                ColumnTypeWeights = new Dictionary<string, double>
                {
                    ["phonetic"] = 0.05,
                    ["semantic"] = 0.5,
                    ["syntactic"] = 0.3,
                    ["contextual"] = 0.2,
                    ["episodic"] = 0.1
                }
            };

            // Memory retrieval task: focus on episodic and semantic
            _profiles["recall"] = new AttentionProfile
            {
                Name = "recall",
                Description = "Retrieving stored memories",
                ColumnTypeWeights = new Dictionary<string, double>
                {
                    ["phonetic"] = 0.05,
                    ["semantic"] = 0.3,
                    ["syntactic"] = 0.1,
                    ["contextual"] = 0.2,
                    ["episodic"] = 0.6
                }
            };

            // Learning task: balanced across all types
            _profiles["learning"] = new AttentionProfile
            {
                Name = "learning",
                Description = "Acquiring new knowledge",
                ColumnTypeWeights = new Dictionary<string, double>
                {
                    ["phonetic"] = 0.2,
                    ["semantic"] = 0.25,
                    ["syntactic"] = 0.2,
                    ["contextual"] = 0.2,
                    ["episodic"] = 0.25
                }
            };

            // Set default profile
            _currentProfile = _profiles["comprehension"];
        }

        /// <summary>
        /// Set current attention profile by name
        /// </summary>
        public void SetProfile(string profileName)
        {
            if (_profiles.TryGetValue(profileName, out var profile))
            {
                _currentProfile = profile;
                Console.WriteLine($"🎯 Attention profile set to: {profileName}");
            }
            else
            {
                Console.WriteLine($"⚠️  Profile '{profileName}' not found, keeping current profile");
            }
        }

        /// <summary>
        /// Register a custom attention profile
        /// </summary>
        public void RegisterProfile(AttentionProfile profile)
        {
            _profiles[profile.Name] = profile;
        }

        /// <summary>
        /// Compute attention scores for all known columns based on current profile
        /// </summary>
        public Dictionary<string, double> ComputeAttentionScores(
            List<string> columnIds,
            WorkingMemory? workingMemory = null,
            TaskRequirements? task = null)
        {
            var scores = new Dictionary<string, double>();

            if (_currentProfile == null)
            {
                // No profile, use baseline for all
                foreach (var id in columnIds)
                {
                    scores[id] = _baselineActivation;
                }
                return scores;
            }

            foreach (var columnId in columnIds)
            {
                var columnType = ExtractColumnType(columnId);
                
                // Base score from profile
                var baseScore = _currentProfile.ColumnTypeWeights.TryGetValue(columnType, out var weight)
                    ? weight
                    : _baselineActivation;

                // Boost if column was recently active (temporal continuity)
                var recencyBoost = 0.0;
                if (_columnActivations.TryGetValue(columnId, out var activation))
                {
                    recencyBoost = activation * 0.3; // Recent activity boosts attention
                }

                // Boost if column's patterns are in working memory (relevance)
                var relevanceBoost = 0.0;
                if (workingMemory != null)
                {
                    var memoryKeys = workingMemory.Keys.ToList();
                    var columnKeys = memoryKeys.Where(k => k.StartsWith(columnType)).ToList();
                    relevanceBoost = Math.Min(0.2, columnKeys.Count * 0.05);
                }

                // Task-specific boost
                var taskBoost = 0.0;
                if (task != null)
                {
                    taskBoost = ComputeTaskBoost(columnType, task);
                }

                // Combined score
                var totalScore = baseScore + recencyBoost + relevanceBoost + taskBoost;
                scores[columnId] = Math.Clamp(totalScore, 0.0, 1.0);
            }

            return scores;
        }

        /// <summary>
        /// Get top-K columns by attention score (focused columns)
        /// </summary>
        public List<string> FocusColumns(
            List<string> columnIds,
            int topK = 5,
            WorkingMemory? workingMemory = null,
            TaskRequirements? task = null)
        {
            var scores = ComputeAttentionScores(columnIds, workingMemory, task);

            return scores
                .OrderByDescending(kvp => kvp.Value)
                .Take(topK)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>
        /// Modulate (boost or suppress) activation of a specific column
        /// </summary>
        public void ModulateColumn(string columnId, double factor)
        {
            if (!_columnActivations.ContainsKey(columnId))
            {
                _columnActivations[columnId] = _baselineActivation;
            }

            _columnActivations[columnId] = Math.Clamp(
                _columnActivations[columnId] * factor,
                0.0,
                1.0
            );
        }

        /// <summary>
        /// Update column activation (typically called after column processes input)
        /// </summary>
        public void UpdateActivation(string columnId, double activation)
        {
            _columnActivations[columnId] = Math.Clamp(activation, 0.0, 1.0);
        }

        /// <summary>
        /// Apply decay to all column activations (attention fades over time)
        /// </summary>
        public void DecayActivations()
        {
            var toRemove = new List<string>();

            foreach (var kvp in _columnActivations.ToList())
            {
                var decayed = kvp.Value * (1.0 - _decayRate);
                
                if (decayed < 0.01) // Below threshold
                {
                    toRemove.Add(kvp.Key);
                }
                else
                {
                    _columnActivations[kvp.Key] = decayed;
                }
            }

            foreach (var key in toRemove)
            {
                _columnActivations.Remove(key);
            }
        }

        /// <summary>
        /// Check if column is currently attended (activation above threshold)
        /// </summary>
        public bool IsAttended(string columnId, double threshold = 0.2)
        {
            return _columnActivations.TryGetValue(columnId, out var activation)
                && activation >= threshold;
        }

        /// <summary>
        /// Get current activation level for a column
        /// </summary>
        public double GetActivation(string columnId)
        {
            return _columnActivations.TryGetValue(columnId, out var activation)
                ? activation
                : _baselineActivation;
        }

        /// <summary>
        /// Compute task-specific boost for column type
        /// </summary>
        private double ComputeTaskBoost(string columnType, TaskRequirements task)
        {
            // High complexity tasks boost semantic and syntactic
            if (task.Complexity == "high")
            {
                if (columnType == "semantic" || columnType == "syntactic")
                    return 0.15;
            }

            // High precision tasks boost phonetic and syntactic
            if (task.Precision == "high")
            {
                if (columnType == "phonetic" || columnType == "syntactic")
                    return 0.1;
            }

            return 0.0;
        }

        /// <summary>
        /// Get attention statistics
        /// </summary>
        public AttentionStats GetStats()
        {
            return new AttentionStats
            {
                ActiveProfile = _currentProfile?.Name ?? "none",
                TotalTrackedColumns = _columnActivations.Count,
                AverageActivation = _columnActivations.Count > 0 
                    ? _columnActivations.Values.Average() 
                    : 0,
                HighestActivation = _columnActivations.Count > 0
                    ? _columnActivations.Values.Max()
                    : 0,
                AttendedColumns = _columnActivations.Count(kvp => kvp.Value >= 0.2)
            };
        }

        /// <summary>
        /// Clear all activations
        /// </summary>
        public void Clear()
        {
            _columnActivations.Clear();
        }

        /// <summary>
        /// Extract column type from column ID
        /// </summary>
        private string ExtractColumnType(string columnId)
        {
            if (string.IsNullOrEmpty(columnId))
                return "";

            var underscoreIndex = columnId.IndexOf('_');
            return underscoreIndex > 0 ? columnId.Substring(0, underscoreIndex) : columnId;
        }
    }

    /// <summary>
    /// Attention profile defining task-specific column weights
    /// Different tasks require different patterns of attention
    /// </summary>
    public class AttentionProfile
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        
        /// <summary>
        /// Weight for each column type (0.0 to 1.0)
        /// Higher weight = more attention during this task
        /// </summary>
        public Dictionary<string, double> ColumnTypeWeights { get; set; } = new();
    }

    /// <summary>
    /// Statistics about attention system state
    /// </summary>
    public class AttentionStats
    {
        public string ActiveProfile { get; set; } = "";
        public int TotalTrackedColumns { get; set; }
        public double AverageActivation { get; set; }
        public double HighestActivation { get; set; }
        public int AttendedColumns { get; set; }
    }
}
