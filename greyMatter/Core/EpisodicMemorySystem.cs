using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Storage;

namespace GreyMatter.Core
{
    /// <summary>
    /// Episodic Memory System - Records and retrieves personal experiences and event sequences
    /// Implements temporal memory for narrative understanding and reading comprehension
    /// </summary>
    public class EpisodicMemorySystem
    {
        private readonly SemanticStorageManager _storageManager;
        private readonly Dictionary<string, EpisodicEvent> _activeEpisodes = new();
        private readonly List<EpisodicEvent> _recentEvents = new();
        private readonly Dictionary<string, NarrativeChain> _narratives = new();
        private readonly object _memoryLock = new();

        // Memory parameters
        public int MaxRecentEvents { get; set; } = 1000;
        public TimeSpan EventDecayTime { get; set; } = TimeSpan.FromHours(24);
        public double ContextSimilarityThreshold { get; set; } = 0.6;

        public EpisodicMemorySystem(SemanticStorageManager storageManager)
        {
            _storageManager = storageManager;
        }

        /// <summary>
        /// Record a new episodic event with temporal and contextual information
        /// </summary>
        public async Task RecordEventAsync(string eventId, string description, Dictionary<string, object> context,
                                         DateTime timestamp, List<string> participants = null, string location = null)
        {
            var episodicEvent = new EpisodicEvent
            {
                Id = eventId,
                Description = description,
                Context = context ?? new Dictionary<string, object>(),
                Timestamp = timestamp,
                Participants = participants ?? new List<string>(),
                Location = location,
                EmotionalValence = AnalyzeEmotionalValence(description),
                Importance = CalculateEventImportance(description, context)
            };

            lock (_memoryLock)
            {
                _activeEpisodes[eventId] = episodicEvent;
                _recentEvents.Add(episodicEvent);

                // Maintain memory limits
                if (_recentEvents.Count > MaxRecentEvents)
                {
                    _recentEvents.RemoveAt(0);
                }
            }

            // Update narrative chains
            await UpdateNarrativeChainsAsync(episodicEvent);

            // Persist to storage
            await PersistEventAsync(episodicEvent);
        }

        /// <summary>
        /// Retrieve events related to a specific context or time period
        /// </summary>
        public List<EpisodicEvent> RetrieveEvents(string query, DateTime? startTime = null, DateTime? endTime = null)
        {
            lock (_memoryLock)
            {
                var relevantEvents = _recentEvents.Where(e =>
                    (startTime == null || e.Timestamp >= startTime) &&
                    (endTime == null || e.Timestamp <= endTime) &&
                    IsEventRelevant(e, query)
                ).ToList();

                return relevantEvents.OrderByDescending(e => e.Timestamp).ToList();
            }
        }

        /// <summary>
        /// Build narrative chains from related events
        /// </summary>
        public NarrativeChain BuildNarrative(string narrativeId, List<EpisodicEvent> events)
        {
            var narrative = new NarrativeChain
            {
                Id = narrativeId,
                Events = events.OrderBy(e => e.Timestamp).ToList(),
                StartTime = events.Min(e => e.Timestamp),
                EndTime = events.Max(e => e.Timestamp),
                Theme = ExtractNarrativeTheme(events),
                KeyParticipants = ExtractKeyParticipants(events),
                Summary = GenerateNarrativeSummary(events)
            };

            lock (_memoryLock)
            {
                _narratives[narrativeId] = narrative;
            }

            return narrative;
        }

        /// <summary>
        /// Retrieve narratives related to a topic or context
        /// </summary>
        public List<NarrativeChain> RetrieveNarratives(string topic)
        {
            lock (_memoryLock)
            {
                return _narratives.Values
                    .Where(n => IsNarrativeRelevant(n, topic))
                    .OrderByDescending(n => n.EndTime)
                    .ToList();
            }
        }

        /// <summary>
        /// Generate questions about stored events and narratives
        /// </summary>
        public List<string> GenerateQuestions(string context = null)
        {
            var questions = new List<string>();
            var relevantEvents = string.IsNullOrEmpty(context) ?
                _recentEvents.Take(10).ToList() :
                RetrieveEvents(context);

            foreach (var evt in relevantEvents)
            {
                questions.AddRange(GenerateEventQuestions(evt));
            }

            return questions.Distinct().Take(5).ToList();
        }

        private async Task UpdateNarrativeChainsAsync(EpisodicEvent newEvent)
        {
            // Find related events and update/create narrative chains
            var relatedEvents = _recentEvents
                .Where(e => e.Id != newEvent.Id && AreEventsRelated(e, newEvent))
                .ToList();

            if (relatedEvents.Count >= 2)
            {
                var narrativeId = GenerateNarrativeId(relatedEvents.Concat(new[] { newEvent }).ToList());
                var allEvents = relatedEvents.Concat(new[] { newEvent }).ToList();
                BuildNarrative(narrativeId, allEvents);
            }
        }

        private async Task PersistEventAsync(EpisodicEvent evt)
        {
            // Persist episodic event to storage
            var eventData = new Dictionary<string, object>
            {
                ["id"] = evt.Id,
                ["description"] = evt.Description,
                ["timestamp"] = evt.Timestamp,
                ["context"] = evt.Context,
                ["participants"] = evt.Participants,
                ["location"] = evt.Location,
                ["emotional_valence"] = evt.EmotionalValence,
                ["importance"] = evt.Importance
            };

            await _storageManager.SaveEpisodicEventAsync(evt.Id, eventData);
        }

        private bool IsEventRelevant(EpisodicEvent evt, string query)
        {
            return evt.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                   evt.Context.Values.Any(v => v?.ToString()?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
                   evt.Participants.Any(p => p.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        private bool AreEventsRelated(EpisodicEvent e1, EpisodicEvent e2)
        {
            // Check for shared participants, locations, or contextual similarity
            var sharedParticipants = e1.Participants.Intersect(e2.Participants).Any();
            var sharedLocation = e1.Location == e2.Location && !string.IsNullOrEmpty(e1.Location);
            var timeProximity = Math.Abs((e1.Timestamp - e2.Timestamp).TotalHours) < 24;

            return sharedParticipants || (sharedLocation && timeProximity);
        }

        private double AnalyzeEmotionalValence(string description)
        {
            // Simple emotional analysis - can be enhanced with more sophisticated NLP
            var positiveWords = new[] { "happy", "good", "great", "excellent", "wonderful", "amazing" };
            var negativeWords = new[] { "sad", "bad", "terrible", "awful", "horrible", "angry" };

            var positiveCount = positiveWords.Count(word => description.Contains(word, StringComparison.OrdinalIgnoreCase));
            var negativeCount = negativeWords.Count(word => description.Contains(word, StringComparison.OrdinalIgnoreCase));

            return (positiveCount - negativeCount) / Math.Max(positiveCount + negativeCount, 1.0);
        }

        private double CalculateEventImportance(string description, Dictionary<string, object> context)
        {
            // Calculate importance based on description length, emotional valence, and context richness
            var baseImportance = Math.Min(description.Length / 100.0, 1.0);
            var contextBonus = Math.Min(context.Count / 5.0, 0.5);
            var emotionalBonus = Math.Abs(AnalyzeEmotionalValence(description)) * 0.3;

            return Math.Min(baseImportance + contextBonus + emotionalBonus, 1.0);
        }

        private string ExtractNarrativeTheme(List<EpisodicEvent> events)
        {
            // Extract common themes from event descriptions
            var words = events.SelectMany(e => e.Description.Split(' '))
                             .GroupBy(w => w.ToLower())
                             .OrderByDescending(g => g.Count())
                             .Take(3)
                             .Select(g => g.Key);

            return string.Join(", ", words);
        }

        private List<string> ExtractKeyParticipants(List<EpisodicEvent> events)
        {
            return events.SelectMany(e => e.Participants)
                        .GroupBy(p => p)
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .Select(g => g.Key)
                        .ToList();
        }

        private string GenerateNarrativeSummary(List<EpisodicEvent> events)
        {
            var startTime = events.Min(e => e.Timestamp);
            var endTime = events.Max(e => e.Timestamp);
            var duration = endTime - startTime;
            var participantCount = events.SelectMany(e => e.Participants).Distinct().Count();

            return $"Narrative spanning {duration.TotalHours:F1} hours with {participantCount} participants and {events.Count} events.";
        }

        private string GenerateNarrativeId(List<EpisodicEvent> events)
        {
            var keyParticipants = ExtractKeyParticipants(events);
            var theme = ExtractNarrativeTheme(events);
            return $"{string.Join("_", keyParticipants.Take(2))}_{theme.Replace(" ", "_")}_{events.First().Timestamp:yyyyMMdd}";
        }

        private bool IsNarrativeRelevant(NarrativeChain narrative, string topic)
        {
            return narrative.Theme.Contains(topic, StringComparison.OrdinalIgnoreCase) ||
                   narrative.KeyParticipants.Any(p => p.Contains(topic, StringComparison.OrdinalIgnoreCase)) ||
                   narrative.Events.Any(e => IsEventRelevant(e, topic));
        }

        private List<string> GenerateEventQuestions(EpisodicEvent evt)
        {
            var questions = new List<string>();

            questions.Add($"What happened in '{evt.Description}'?");
            questions.Add($"When did '{evt.Description}' occur?");

            if (evt.Participants.Any())
            {
                questions.Add($"Who was involved in '{evt.Description}'?");
            }

            if (!string.IsNullOrEmpty(evt.Location))
            {
                questions.Add($"Where did '{evt.Description}' happen?");
            }

            if (evt.Context.Any())
            {
                questions.Add($"What was the context of '{evt.Description}'?");
            }

            return questions;
        }
    }

    /// <summary>
    /// Represents a single episodic event in memory
    /// </summary>
    public class EpisodicEvent
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Context { get; set; }
        public DateTime Timestamp { get; set; }
        public List<string> Participants { get; set; }
        public string Location { get; set; }
        public double EmotionalValence { get; set; }
        public double Importance { get; set; }
    }

    /// <summary>
    /// Represents a chain of related events forming a narrative
    /// </summary>
    public class NarrativeChain
    {
        public string Id { get; set; }
        public List<EpisodicEvent> Events { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Theme { get; set; }
        public List<string> KeyParticipants { get; set; }
        public string Summary { get; set; }
    }
}
