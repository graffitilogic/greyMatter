using System;
using System.Collections.Generic;
using System.Linq;
using GreyMatter.Core;

namespace GreyMatter.Core
{
    /// <summary>
    /// Working Memory system for cortical columns
    /// Provides shared temporary state, pattern decay, and similarity-based retrieval
    /// Enables inter-column communication and temporal context tracking
    /// </summary>
    public class WorkingMemory
    {
        private readonly Dictionary<string, WorkingMemoryEntry> _activePatterns;
        private readonly int _maxCapacity;
        private readonly double _defaultDecayRate;
        private TemporalContext _currentContext;

        /// <summary>
        /// Current temporal context (recent activations, task phase)
        /// </summary>
        public TemporalContext CurrentContext
        {
            get => _currentContext;
            set => _currentContext = value;
        }

        /// <summary>
        /// Number of patterns currently stored
        /// </summary>
        public int Count => _activePatterns.Count;

        /// <summary>
        /// All active pattern keys
        /// </summary>
        public IEnumerable<string> Keys => _activePatterns.Keys;

        public WorkingMemory(int maxCapacity = 1000, double defaultDecayRate = 0.1)
        {
            _activePatterns = new Dictionary<string, WorkingMemoryEntry>();
            _maxCapacity = maxCapacity;
            _defaultDecayRate = defaultDecayRate;
            _currentContext = new TemporalContext();
        }

        /// <summary>
        /// Store a pattern in working memory with optional initial strength
        /// </summary>
        public void Store(string key, SparsePattern pattern, double strength = 1.0, string? metadata = null)
        {
            // If at capacity, remove weakest pattern
            if (_activePatterns.Count >= _maxCapacity && !_activePatterns.ContainsKey(key))
            {
                var weakest = _activePatterns.OrderBy(kvp => kvp.Value.Strength).First();
                _activePatterns.Remove(weakest.Key);
            }

            var entry = new WorkingMemoryEntry
            {
                Pattern = pattern,
                Strength = strength,
                StoredAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                AccessCount = 0,
                Metadata = metadata ?? ""
            };

            _activePatterns[key] = entry;

            // Track in temporal context
            _currentContext.RecordActivation(key);
        }

        /// <summary>
        /// Retrieve a pattern from working memory
        /// Returns null if not found
        /// </summary>
        public SparsePattern? Retrieve(string key)
        {
            if (_activePatterns.TryGetValue(key, out var entry))
            {
                entry.LastAccessedAt = DateTime.UtcNow;
                entry.AccessCount++;
                return entry.Pattern;
            }
            return null;
        }

        /// <summary>
        /// Get pattern strength (0.0 to 1.0)
        /// Returns 0 if pattern not found
        /// </summary>
        public double GetStrength(string key)
        {
            return _activePatterns.TryGetValue(key, out var entry) ? entry.Strength : 0.0;
        }

        /// <summary>
        /// Apply decay to all patterns based on time elapsed
        /// Removes patterns that decay below threshold
        /// </summary>
        public void DecayActivations(double? decayRate = null, double minThreshold = 0.01)
        {
            var rate = decayRate ?? _defaultDecayRate;
            var now = DateTime.UtcNow;
            var toRemove = new List<string>();

            foreach (var kvp in _activePatterns)
            {
                var entry = kvp.Value;
                var elapsed = (now - entry.LastAccessedAt).TotalSeconds;

                // Exponential decay: strength = strength * e^(-decay_rate * time)
                entry.Strength *= Math.Exp(-rate * elapsed);

                // Mark for removal if below threshold
                if (entry.Strength < minThreshold)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            // Remove decayed patterns
            foreach (var key in toRemove)
            {
                _activePatterns.Remove(key);
            }
        }

        /// <summary>
        /// Find patterns similar to query pattern using cosine similarity
        /// Returns top-K most similar patterns with their similarity scores
        /// </summary>
        public List<(string key, double similarity)> QuerySimilar(SparsePattern query, int topK = 5, double minSimilarity = 0.0)
        {
            var similarities = new List<(string key, double similarity)>();

            foreach (var kvp in _activePatterns)
            {
                var similarity = CalculateCosineSimilarity(query, kvp.Value.Pattern);
                
                if (similarity >= minSimilarity)
                {
                    similarities.Add((kvp.Key, similarity));
                }
            }

            // Return top-K by similarity, descending
            return similarities
                .OrderByDescending(x => x.similarity)
                .Take(topK)
                .ToList();
        }

        /// <summary>
        /// Calculate cosine similarity between two sparse patterns
        /// Returns value between 0.0 (no overlap) and 1.0 (identical)
        /// </summary>
        private double CalculateCosineSimilarity(SparsePattern a, SparsePattern b)
        {
            if (a.ActiveBits.Length == 0 || b.ActiveBits.Length == 0)
                return 0.0;

            // Convert to HashSet for fast intersection
            var setBits = new HashSet<int>(a.ActiveBits);
            var overlap = b.ActiveBits.Count(bit => setBits.Contains(bit));

            if (overlap == 0)
                return 0.0;

            // Cosine similarity = overlap / sqrt(|A| * |B|)
            var magnitudeProduct = Math.Sqrt(a.ActiveBits.Length * b.ActiveBits.Length);
            return overlap / magnitudeProduct;
        }

        /// <summary>
        /// Clear all patterns from working memory
        /// </summary>
        public void Clear()
        {
            _activePatterns.Clear();
            _currentContext.Clear();
        }

        /// <summary>
        /// Get statistics about current working memory state
        /// </summary>
        public WorkingMemoryStats GetStats()
        {
            var now = DateTime.UtcNow;
            var entries = _activePatterns.Values.ToList();

            return new WorkingMemoryStats
            {
                TotalPatterns = entries.Count,
                AverageStrength = entries.Count > 0 ? entries.Average(e => e.Strength) : 0,
                TotalAccessCount = entries.Sum(e => e.AccessCount),
                OldestPattern = entries.Count > 0 ? entries.Min(e => (now - e.StoredAt).TotalSeconds) : 0,
                NewestPattern = entries.Count > 0 ? entries.Max(e => (now - e.StoredAt).TotalSeconds) : 0,
                Capacity = _maxCapacity,
                UtilizationPercent = (entries.Count / (double)_maxCapacity) * 100
            };
        }

        /// <summary>
        /// Get detailed information about a specific pattern
        /// </summary>
        public WorkingMemoryEntry? GetEntry(string key)
        {
            return _activePatterns.TryGetValue(key, out var entry) ? entry : null;
        }
    }

    /// <summary>
    /// Entry stored in working memory with metadata
    /// </summary>
    public class WorkingMemoryEntry
    {
        public SparsePattern Pattern { get; set; }
        public double Strength { get; set; }
        public DateTime StoredAt { get; set; }
        public DateTime LastAccessedAt { get; set; }
        public int AccessCount { get; set; }
        public string Metadata { get; set; } = "";
    }

    /// <summary>
    /// Temporal context tracking recent activations and task phase
    /// </summary>
    public class TemporalContext
    {
        private readonly Queue<ActivationEvent> _recentActivations;
        private readonly int _maxHistorySize;

        public DateTime CurrentTimestamp { get; private set; }
        public string TaskPhase { get; set; }
        public int ActivationCount { get; private set; }

        /// <summary>
        /// Recent activation history (limited by maxHistorySize)
        /// </summary>
        public IEnumerable<ActivationEvent> RecentActivations => _recentActivations;

        public TemporalContext(int maxHistorySize = 100)
        {
            _recentActivations = new Queue<ActivationEvent>();
            _maxHistorySize = maxHistorySize;
            CurrentTimestamp = DateTime.UtcNow;
            TaskPhase = "idle";
            ActivationCount = 0;
        }

        /// <summary>
        /// Record an activation event
        /// </summary>
        public void RecordActivation(string key, string? context = null)
        {
            var activation = new ActivationEvent
            {
                Key = key,
                Timestamp = DateTime.UtcNow,
                Context = context ?? TaskPhase
            };

            _recentActivations.Enqueue(activation);
            ActivationCount++;

            // Maintain max size
            while (_recentActivations.Count > _maxHistorySize)
            {
                _recentActivations.Dequeue();
            }

            CurrentTimestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Get recent activations within time window
        /// </summary>
        public List<ActivationEvent> GetRecentActivations(TimeSpan timeWindow)
        {
            var cutoff = DateTime.UtcNow - timeWindow;
            return _recentActivations
                .Where(a => a.Timestamp >= cutoff)
                .ToList();
        }

        /// <summary>
        /// Detect if a pattern key has been activated recently
        /// </summary>
        public bool WasRecentlyActivated(string key, TimeSpan timeWindow)
        {
            var cutoff = DateTime.UtcNow - timeWindow;
            return _recentActivations.Any(a => a.Key == key && a.Timestamp >= cutoff);
        }

        /// <summary>
        /// Clear temporal context
        /// </summary>
        public void Clear()
        {
            _recentActivations.Clear();
            ActivationCount = 0;
            CurrentTimestamp = DateTime.UtcNow;
            TaskPhase = "idle";
        }
    }

    /// <summary>
    /// Single activation event in temporal history
    /// </summary>
    public class ActivationEvent
    {
        public string Key { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Context { get; set; } = "";
    }

    /// <summary>
    /// Statistics about working memory state
    /// </summary>
    public class WorkingMemoryStats
    {
        public int TotalPatterns { get; set; }
        public double AverageStrength { get; set; }
        public int TotalAccessCount { get; set; }
        public double OldestPattern { get; set; }
        public double NewestPattern { get; set; }
        public int Capacity { get; set; }
        public double UtilizationPercent { get; set; }
    }
}
