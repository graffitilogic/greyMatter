using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using greyMatter.Core;

namespace GreyMatter.Core
{
    /// <summary>
    /// Analyzes column message patterns to detect significant learning opportunities.
    /// Monitors cross-column consensus, co-activation patterns, and novel concepts.
    /// Feeds the Column → Brain learning path with high-confidence patterns.
    /// </summary>
    public class ColumnPatternDetector
    {
        private readonly MessageBus _messageBus;
        private readonly IIntegratedBrain _brain;
        
        // Pattern tracking across all columns
        private readonly Dictionary<string, ConceptActivity> _conceptActivity;
        private readonly Dictionary<string, HashSet<string>> _coActivations;
        private readonly Dictionary<string, List<DateTime>> _activationTimeline;
        
        // Configuration
        private readonly int _consensusThreshold = 3; // Require 3+ columns for consensus
        private readonly double _confidenceThreshold = 0.7; // Minimum confidence to notify brain
        private readonly TimeSpan _coActivationWindow = TimeSpan.FromSeconds(2);
        
        // Statistics
        private int _patternsDetected = 0;
        private int _patternsNotified = 0;
        private int _consensusReached = 0;
        private int _coActivationsFound = 0;

        public ColumnPatternDetector(MessageBus messageBus, IIntegratedBrain brain)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _brain = brain ?? throw new ArgumentNullException(nameof(brain));
            
            _conceptActivity = new Dictionary<string, ConceptActivity>();
            _coActivations = new Dictionary<string, HashSet<string>>();
            _activationTimeline = new Dictionary<string, List<DateTime>>();
        }

        /// <summary>
        /// Analyze recent messages to detect patterns worth learning from.
        /// Should be called periodically (e.g., after processing a batch of input).
        /// </summary>
        public async Task<List<ColumnPattern>> AnalyzeRecentActivityAsync(TimeSpan windowSize)
        {
            var detectedPatterns = new List<ColumnPattern>();
            var now = DateTime.Now;
            var cutoff = now - windowSize;

            // Get recent messages from the bus
            var recentMessages = _messageBus.GetRecentMessages(100, cutoff);
            
            // Update concept activity from messages
            foreach (var message in recentMessages)
            {
                UpdateConceptActivity(message, now);
            }

            // Detect novel words (concepts with high activation but low brain familiarity)
            var novelPatterns = await DetectNovelWordsAsync(cutoff);
            detectedPatterns.AddRange(novelPatterns);

            // Detect co-activation patterns (concepts that fire together)
            var coActivationPatterns = DetectCoActivationPatterns(cutoff);
            detectedPatterns.AddRange(coActivationPatterns);

            // Detect reinforcement patterns (repeated consistent activation)
            var reinforcementPatterns = DetectReinforcementPatterns(cutoff);
            detectedPatterns.AddRange(reinforcementPatterns);

            // Detect semantic clusters (groups of related concepts activating together)
            var clusterPatterns = DetectSemanticClusters(cutoff);
            detectedPatterns.AddRange(clusterPatterns);

            _patternsDetected += detectedPatterns.Count;

            // Notify brain of significant patterns
            foreach (var pattern in detectedPatterns.Where(p => p.Confidence >= _confidenceThreshold))
            {
                await _brain.NotifyColumnPatternAsync(pattern);
                _patternsNotified++;
            }

            return detectedPatterns;
        }

        /// <summary>
        /// Update concept activity tracking from a message
        /// </summary>
        private void UpdateConceptActivity(ColumnMessage message, DateTime timestamp)
        {
            // Extract concept from message (could be in metadata or derived from payload)
            var concept = ExtractConceptFromMessage(message);
            if (string.IsNullOrWhiteSpace(concept))
                return;

            // Update activity tracking
            if (!_conceptActivity.ContainsKey(concept))
            {
                _conceptActivity[concept] = new ConceptActivity
                {
                    Concept = concept,
                    FirstSeen = timestamp,
                    ColumnsSeen = new HashSet<string>()
                };
            }

            var activity = _conceptActivity[concept];
            activity.ColumnsSeen.Add(message.SenderId);
            activity.MessageCount++;
            activity.LastSeen = timestamp;
            activity.TotalStrength += message.Strength;

            // Update activation timeline
            if (!_activationTimeline.ContainsKey(concept))
                _activationTimeline[concept] = new List<DateTime>();
            _activationTimeline[concept].Add(timestamp);

            // Track co-activations (concepts active in same time window)
            TrackCoActivation(concept, timestamp);
        }

        /// <summary>
        /// Track concepts that activate together
        /// </summary>
        private void TrackCoActivation(string concept, DateTime timestamp)
        {
            // Find other concepts active in the same time window
            var coActiveConcepts = _activationTimeline
                .Where(kvp => kvp.Key != concept)
                .Where(kvp => kvp.Value.Any(t => Math.Abs((t - timestamp).TotalMilliseconds) < _coActivationWindow.TotalMilliseconds))
                .Select(kvp => kvp.Key)
                .ToList();

            if (!_coActivations.ContainsKey(concept))
                _coActivations[concept] = new HashSet<string>();

            foreach (var coActive in coActiveConcepts)
            {
                _coActivations[concept].Add(coActive);
                
                // Bidirectional tracking
                if (!_coActivations.ContainsKey(coActive))
                    _coActivations[coActive] = new HashSet<string>();
                _coActivations[coActive].Add(concept);
            }
        }

        /// <summary>
        /// Detect novel words (concepts with high column consensus but low brain familiarity)
        /// </summary>
        private async Task<List<ColumnPattern>> DetectNovelWordsAsync(DateTime cutoff)
        {
            var patterns = new List<ColumnPattern>();

            foreach (var kvp in _conceptActivity.Where(kvp => kvp.Value.FirstSeen >= cutoff))
            {
                var concept = kvp.Key;
                var activity = kvp.Value;

                // Check if brain knows about it
                var familiarity = await _brain.GetWordFamiliarityAsync(concept);

                // Novel if: multiple columns agree but brain doesn't know it well
                if (activity.ColumnsSeen.Count >= _consensusThreshold && familiarity < 0.3)
                {
                    var confidence = CalculateConfidence(activity);
                    
                    patterns.Add(new ColumnPattern
                    {
                        PrimaryConcept = concept,
                        RelatedConcepts = GetRelatedConcepts(concept),
                        Confidence = confidence,
                        ColumnCount = activity.ColumnsSeen.Count,
                        MessageCount = activity.MessageCount,
                        Type = PatternType.NovelWord,
                        DetectedAt = DateTime.Now
                    });

                    _consensusReached++;
                }
            }

            return patterns;
        }

        /// <summary>
        /// Detect co-activation patterns (concepts that consistently fire together)
        /// </summary>
        private List<ColumnPattern> DetectCoActivationPatterns(DateTime cutoff)
        {
            var patterns = new List<ColumnPattern>();

            foreach (var kvp in _coActivations)
            {
                var concept = kvp.Key;
                var coActivated = kvp.Value;

                // Require minimum co-activations
                if (coActivated.Count >= 2)
                {
                    var activity = _conceptActivity.GetValueOrDefault(concept);
                    if (activity != null && activity.LastSeen >= cutoff)
                    {
                        var confidence = CalculateConfidence(activity);
                        
                        patterns.Add(new ColumnPattern
                        {
                            PrimaryConcept = concept,
                            RelatedConcepts = coActivated.Take(5).ToList(),
                            Confidence = confidence,
                            ColumnCount = activity.ColumnsSeen.Count,
                            MessageCount = activity.MessageCount,
                            Type = PatternType.NewAssociation,
                            DetectedAt = DateTime.Now
                        });

                        _coActivationsFound++;
                    }
                }
            }

            return patterns;
        }

        /// <summary>
        /// Detect reinforcement patterns (concepts with repeated consistent activation)
        /// </summary>
        private List<ColumnPattern> DetectReinforcementPatterns(DateTime cutoff)
        {
            var patterns = new List<ColumnPattern>();

            foreach (var kvp in _conceptActivity)
            {
                var concept = kvp.Key;
                var activity = kvp.Value;

                // Reinforcement: seen many times, multiple columns, consistent strength
                if (activity.MessageCount >= 10 && 
                    activity.ColumnsSeen.Count >= _consensusThreshold &&
                    activity.LastSeen >= cutoff)
                {
                    var avgStrength = activity.TotalStrength / activity.MessageCount;
                    var confidence = Math.Min(avgStrength, 1.0);

                    patterns.Add(new ColumnPattern
                    {
                        PrimaryConcept = concept,
                        RelatedConcepts = GetRelatedConcepts(concept),
                        Confidence = confidence,
                        ColumnCount = activity.ColumnsSeen.Count,
                        MessageCount = activity.MessageCount,
                        Type = PatternType.Reinforcement,
                        DetectedAt = DateTime.Now
                    });
                }
            }

            return patterns;
        }

        /// <summary>
        /// Detect semantic clusters (groups of related concepts activating together)
        /// </summary>
        private List<ColumnPattern> DetectSemanticClusters(DateTime cutoff)
        {
            var patterns = new List<ColumnPattern>();
            var processed = new HashSet<string>();

            foreach (var kvp in _coActivations)
            {
                var concept = kvp.Key;
                if (processed.Contains(concept))
                    continue;

                var coActivated = kvp.Value;
                
                // Look for dense clusters (concepts with many mutual co-activations)
                var cluster = FindDenseCluster(concept, coActivated);
                
                if (cluster.Count >= 3) // Minimum cluster size
                {
                    var activity = _conceptActivity.GetValueOrDefault(concept);
                    if (activity != null && activity.LastSeen >= cutoff)
                    {
                        patterns.Add(new ColumnPattern
                        {
                            PrimaryConcept = concept,
                            RelatedConcepts = cluster.Take(10).ToList(),
                            Confidence = CalculateClusterConfidence(cluster),
                            ColumnCount = activity.ColumnsSeen.Count,
                            MessageCount = activity.MessageCount,
                            Type = PatternType.SemanticCluster,
                            DetectedAt = DateTime.Now
                        });

                        processed.UnionWith(cluster);
                    }
                }
            }

            return patterns;
        }

        /// <summary>
        /// Find densely connected cluster of concepts
        /// </summary>
        private HashSet<string> FindDenseCluster(string seed, HashSet<string> candidates)
        {
            var cluster = new HashSet<string> { seed };
            
            foreach (var candidate in candidates)
            {
                // Add if candidate is also connected to many cluster members
                var candidateConnections = _coActivations.GetValueOrDefault(candidate, new HashSet<string>());
                var connectionsInCluster = candidateConnections.Intersect(cluster).Count();
                
                if (connectionsInCluster >= cluster.Count / 2) // At least half connections
                {
                    cluster.Add(candidate);
                }
            }

            return cluster;
        }

        /// <summary>
        /// Calculate confidence score for a concept's activity
        /// </summary>
        private double CalculateConfidence(ConceptActivity activity)
        {
            // Confidence based on: column consensus, message count, strength
            var consensusScore = Math.Min((double)activity.ColumnsSeen.Count / 10.0, 1.0);
            var frequencyScore = Math.Min((double)activity.MessageCount / 20.0, 1.0);
            var strengthScore = Math.Min(activity.TotalStrength / activity.MessageCount, 1.0);

            return (consensusScore * 0.4) + (frequencyScore * 0.3) + (strengthScore * 0.3);
        }

        /// <summary>
        /// Calculate confidence for a semantic cluster
        /// </summary>
        private double CalculateClusterConfidence(HashSet<string> cluster)
        {
            var totalActivity = cluster
                .Select(c => _conceptActivity.GetValueOrDefault(c))
                .Where(a => a != null)
                .Sum(a => a!.MessageCount);

            return Math.Min(totalActivity / 50.0, 1.0);
        }

        /// <summary>
        /// Get related concepts for a given concept
        /// </summary>
        private List<string> GetRelatedConcepts(string concept)
        {
            return _coActivations.GetValueOrDefault(concept, new HashSet<string>())
                .Take(5)
                .ToList();
        }

        /// <summary>
        /// Extract concept name from message (placeholder - needs proper implementation)
        /// </summary>
        private string ExtractConceptFromMessage(ColumnMessage message)
        {
            // In a real implementation, this would analyze the SparsePattern payload
            // For now, use message metadata or sender type
            return message.SenderId.Split('_').FirstOrDefault() ?? "";
        }

        /// <summary>
        /// Get detection statistics
        /// </summary>
        public PatternDetectionStats GetStats()
        {
            return new PatternDetectionStats
            {
                TotalPatternsDetected = _patternsDetected,
                PatternsNotifiedToBrain = _patternsNotified,
                ConsensusEventsReached = _consensusReached,
                CoActivationsFound = _coActivationsFound,
                UniqueConcepts = _conceptActivity.Count,
                ActiveClusters = _coActivations.Count(kvp => kvp.Value.Count >= 3)
            };
        }

        /// <summary>
        /// Reset tracking (for testing or new sessions)
        /// </summary>
        public void Reset()
        {
            _conceptActivity.Clear();
            _coActivations.Clear();
            _activationTimeline.Clear();
            _patternsDetected = 0;
            _patternsNotified = 0;
            _consensusReached = 0;
            _coActivationsFound = 0;
        }
    }

    /// <summary>
    /// Tracks activity for a single concept across columns
    /// </summary>
    internal class ConceptActivity
    {
        public string Concept { get; set; } = "";
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public HashSet<string> ColumnsSeen { get; set; } = new HashSet<string>();
        public int MessageCount { get; set; }
        public double TotalStrength { get; set; }
    }

    /// <summary>
    /// Statistics about pattern detection performance
    /// </summary>
    public class PatternDetectionStats
    {
        public int TotalPatternsDetected { get; set; }
        public int PatternsNotifiedToBrain { get; set; }
        public int ConsensusEventsReached { get; set; }
        public int CoActivationsFound { get; set; }
        public int UniqueConcepts { get; set; }
        public int ActiveClusters { get; set; }

        public override string ToString()
        {
            return $"Pattern Detection Stats:\n" +
                   $"  Patterns: {TotalPatternsDetected} detected, {PatternsNotifiedToBrain} notified\n" +
                   $"  Consensus: {ConsensusEventsReached} events\n" +
                   $"  Co-activations: {CoActivationsFound} found\n" +
                   $"  Concepts: {UniqueConcepts} unique, {ActiveClusters} clusters";
        }
    }
}
