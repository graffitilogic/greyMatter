using System;
using System.Collections.Generic;
using System.Linq;
using GreyMatter.Core;

namespace GreyMatter.Core
{
    /// <summary>
    /// Message types for inter-column communication
    /// Models different types of neural signals between cortical columns
    /// </summary>
    public enum MessageType
    {
        /// <summary>Excitatory signal - activate similar patterns in receiver</summary>
        Excitatory,
        
        /// <summary>Inhibitory signal - suppress patterns in receiver</summary>
        Inhibitory,
        
        /// <summary>Query signal - request information from receiver</summary>
        Query,
        
        /// <summary>Response signal - answer to a query</summary>
        Response,
        
        /// <summary>Forward signal - pass information to next processing stage</summary>
        Forward,
        
        /// <summary>Feedback signal - send correction/reinforcement backwards</summary>
        Feedback
    }

    /// <summary>
    /// Message passed between cortical columns
    /// Carries pattern information, strength, and routing metadata
    /// </summary>
    public class ColumnMessage
    {
        /// <summary>ID of sending column</summary>
        public string SenderId { get; set; } = "";

        /// <summary>ID of receiving column (empty for broadcast)</summary>
        public string ReceiverId { get; set; } = "";

        /// <summary>Type of message (excitatory, inhibitory, query, etc.)</summary>
        public MessageType Type { get; set; }

        /// <summary>Pattern payload being transmitted</summary>
        public SparsePattern Payload { get; set; }

        /// <summary>Signal strength (0.0 to 1.0)</summary>
        public double Strength { get; set; }

        /// <summary>When message was created</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Optional metadata (context, task phase, etc.)</summary>
        public string Metadata { get; set; } = "";

        /// <summary>Response ID if this is a response to a query</summary>
        public string? ResponseToId { get; set; }

        /// <summary>Unique message ID</summary>
        public string Id { get; set; }

        public ColumnMessage()
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 16);
            Timestamp = DateTime.UtcNow;
            Payload = new SparsePattern(Array.Empty<int>(), 0);
        }
    }

    /// <summary>
    /// Message bus for inter-column communication
    /// Routes messages between columns based on connection rules and attention
    /// </summary>
    public class MessageBus
    {
        private readonly Dictionary<string, Queue<ColumnMessage>> _columnInboxes;
        private readonly List<ColumnMessage> _messageHistory;
        private readonly int _maxInboxSize;
        private readonly int _maxHistorySize;
        private readonly ColumnConnectionRules _connectionRules;

        public MessageBus(int maxInboxSize = 100, int maxHistorySize = 1000)
        {
            _columnInboxes = new Dictionary<string, Queue<ColumnMessage>>();
            _messageHistory = new List<ColumnMessage>();
            _maxInboxSize = maxInboxSize;
            _maxHistorySize = maxHistorySize;
            _connectionRules = new ColumnConnectionRules();
        }

        /// <summary>
        /// Send a message to a specific column
        /// </summary>
        public void SendMessage(ColumnMessage message)
        {
            // Validate connection is allowed
            if (!string.IsNullOrEmpty(message.ReceiverId))
            {
                var senderType = ExtractColumnType(message.SenderId);
                var receiverType = ExtractColumnType(message.ReceiverId);

                if (!_connectionRules.IsConnectionAllowed(senderType, receiverType))
                {
                    Console.WriteLine($"⚠️  Connection denied: {senderType} → {receiverType}");
                    return;
                }
            }

            // Get or create inbox for receiver
            if (!_columnInboxes.ContainsKey(message.ReceiverId))
            {
                _columnInboxes[message.ReceiverId] = new Queue<ColumnMessage>();
            }

            var inbox = _columnInboxes[message.ReceiverId];

            // Enforce inbox size limit
            if (inbox.Count >= _maxInboxSize)
            {
                inbox.Dequeue(); // Remove oldest message
            }

            inbox.Enqueue(message);

            // Add to history
            _messageHistory.Add(message);
            if (_messageHistory.Count > _maxHistorySize)
            {
                _messageHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// Register a column with the message bus (creates inbox)
        /// </summary>
        public void RegisterColumn(string columnId)
        {
            if (!_columnInboxes.ContainsKey(columnId))
            {
                _columnInboxes[columnId] = new Queue<ColumnMessage>();
                Console.WriteLine($"📬 Registered column inbox: {columnId}");
            }
        }

        /// <summary>
        /// Unregister a column (cleanup)
        /// </summary>
        public void UnregisterColumn(string columnId)
        {
            if (_columnInboxes.Remove(columnId))
            {
                Console.WriteLine($"📭 Unregistered column inbox: {columnId}");
            }
        }

        /// <summary>
        /// Broadcast message to all columns of a specific type
        /// </summary>
        public void Broadcast(string columnType, ColumnMessage message)
        {
            var targetColumns = _columnInboxes.Keys
                .Where(id => ExtractColumnType(id) == columnType)
                .ToList();

            // Enhanced logging for debugging
            Console.WriteLine($"📡 Broadcasting {message.Type} from {message.SenderId} to type '{columnType}'");
            Console.WriteLine($"   Registered columns: {_columnInboxes.Count}");
            Console.WriteLine($"   Target columns found: {targetColumns.Count}");
            
            if (targetColumns.Count == 0)
            {
                Console.WriteLine($"   ⚠️  No columns of type '{columnType}' registered!");
                Console.WriteLine($"   Available column types: {string.Join(", ", _columnInboxes.Keys.Select(ExtractColumnType).Distinct())}");
            }

            foreach (var columnId in targetColumns)
            {
                var broadcastMessage = new ColumnMessage
                {
                    SenderId = message.SenderId,
                    ReceiverId = columnId,
                    Type = message.Type,
                    Payload = message.Payload,
                    Strength = message.Strength,
                    Metadata = message.Metadata,
                    Timestamp = message.Timestamp
                };
                SendMessage(broadcastMessage);
                Console.WriteLine($"   ✉️  Sent to {columnId}");
            }
        }

        /// <summary>
        /// Get messages for a specific column (up to maxCount)
        /// </summary>
        public List<ColumnMessage> GetMessages(string columnId, int maxCount = 10)
        {
            if (!_columnInboxes.TryGetValue(columnId, out var inbox))
            {
                return new List<ColumnMessage>();
            }

            var messages = new List<ColumnMessage>();
            var count = Math.Min(maxCount, inbox.Count);

            for (int i = 0; i < count; i++)
            {
                if (inbox.Count > 0)
                {
                    messages.Add(inbox.Dequeue());
                }
            }

            return messages;
        }

        /// <summary>
        /// Peek at messages without removing them
        /// </summary>
        public List<ColumnMessage> PeekMessages(string columnId, int maxCount = 10)
        {
            if (!_columnInboxes.TryGetValue(columnId, out var inbox))
            {
                return new List<ColumnMessage>();
            }

            return inbox.Take(maxCount).ToList();
        }

        /// <summary>
        /// Get inbox size for a column
        /// </summary>
        public int GetInboxSize(string columnId)
        {
            return _columnInboxes.TryGetValue(columnId, out var inbox) ? inbox.Count : 0;
        }

        /// <summary>
        /// Purge messages older than specified age
        /// </summary>
        public void PurgeOldMessages(TimeSpan maxAge)
        {
            var cutoff = DateTime.UtcNow - maxAge;

            // Purge from inboxes
            foreach (var inbox in _columnInboxes.Values)
            {
                var toRemove = new List<ColumnMessage>();
                foreach (var msg in inbox)
                {
                    if (msg.Timestamp < cutoff)
                    {
                        toRemove.Add(msg);
                    }
                }

                foreach (var msg in toRemove)
                {
                    // Can't easily remove from Queue, so we'll drain and rebuild
                    var temp = inbox.ToList();
                    inbox.Clear();
                    foreach (var m in temp.Where(m => !toRemove.Contains(m)))
                    {
                        inbox.Enqueue(m);
                    }
                }
            }

            // Purge from history
            _messageHistory.RemoveAll(m => m.Timestamp < cutoff);
        }

        /// <summary>
        /// Get message statistics
        /// </summary>
        public MessageBusStats GetStats()
        {
            var now = DateTime.UtcNow;
            var recentMessages = _messageHistory
                .Where(m => (now - m.Timestamp).TotalSeconds < 60)
                .ToList();

            var messagesByType = _messageHistory
                .GroupBy(m => m.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            return new MessageBusStats
            {
                TotalInboxes = _columnInboxes.Count,
                TotalMessagesInInboxes = _columnInboxes.Values.Sum(q => q.Count),
                TotalMessagesSent = _messageHistory.Count,
                RecentMessagesPerSecond = recentMessages.Count / 60.0,
                MessagesByType = messagesByType,
                OldestMessageAge = _messageHistory.Count > 0 
                    ? (now - _messageHistory.Min(m => m.Timestamp)).TotalSeconds 
                    : 0
            };
        }

        /// <summary>
        /// Clear all messages
        /// </summary>
        public void Clear()
        {
            _columnInboxes.Clear();
            _messageHistory.Clear();
        }

        /// <summary>
        /// Extract column type from column ID (e.g., "semantic_ABC123" -> "semantic")
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
    /// Connection rules defining which column types can communicate
    /// Models biological cortical hierarchy and information flow
    /// </summary>
    public class ColumnConnectionRules
    {
        private readonly Dictionary<string, List<string>> _forwardConnections;
        private readonly Dictionary<string, List<string>> _feedbackConnections;

        public ColumnConnectionRules()
        {
            // Forward connections: standard processing pipeline
            _forwardConnections = new Dictionary<string, List<string>>
            {
                // Phonetic processes sound → sends to semantic
                ["phonetic"] = new List<string> { "semantic" },
                
                // Semantic processes meaning → sends to syntactic and episodic
                ["semantic"] = new List<string> { "syntactic", "episodic" },
                
                // Syntactic processes structure → sends to contextual and episodic
                ["syntactic"] = new List<string> { "contextual", "episodic" },
                
                // Contextual processes situation → sends to episodic
                ["contextual"] = new List<string> { "episodic" },
                
                // Episodic stores memories → no forward connections
                ["episodic"] = new List<string>()
            };

            // Feedback connections: error correction and reinforcement
            _feedbackConnections = new Dictionary<string, List<string>>
            {
                // Semantic can send feedback to phonetic
                ["semantic"] = new List<string> { "phonetic" },
                
                // Syntactic can send feedback to semantic and phonetic
                ["syntactic"] = new List<string> { "semantic", "phonetic" },
                
                // Contextual can send feedback to syntactic and semantic
                ["contextual"] = new List<string> { "syntactic", "semantic" },
                
                // Episodic can send feedback to all earlier stages
                ["episodic"] = new List<string> { "contextual", "syntactic", "semantic", "phonetic" }
            };
        }

        /// <summary>
        /// Check if connection from sender to receiver is allowed
        /// </summary>
        public bool IsConnectionAllowed(string senderType, string receiverType)
        {
            // Allow self-connections (lateral inhibition)
            if (senderType == receiverType)
                return true;

            // Check forward connections
            if (_forwardConnections.TryGetValue(senderType, out var forward))
            {
                if (forward.Contains(receiverType))
                    return true;
            }

            // Check feedback connections
            if (_feedbackConnections.TryGetValue(senderType, out var feedback))
            {
                if (feedback.Contains(receiverType))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Get downstream columns that sender can communicate with
        /// </summary>
        public List<string> GetDownstreamColumns(string columnType)
        {
            return _forwardConnections.TryGetValue(columnType, out var downstream)
                ? new List<string>(downstream)
                : new List<string>();
        }

        /// <summary>
        /// Get upstream columns that can send feedback to receiver
        /// </summary>
        public List<string> GetFeedbackSources(string columnType)
        {
            var sources = new List<string>();
            foreach (var kvp in _feedbackConnections)
            {
                if (kvp.Value.Contains(columnType))
                {
                    sources.Add(kvp.Key);
                }
            }
            return sources;
        }

        /// <summary>
        /// Get all valid connections as (sender, receiver) pairs
        /// </summary>
        public List<(string sender, string receiver)> GetAllConnections()
        {
            var connections = new List<(string, string)>();

            // Add forward connections
            foreach (var kvp in _forwardConnections)
            {
                foreach (var receiver in kvp.Value)
                {
                    connections.Add((kvp.Key, receiver));
                }
            }

            // Add feedback connections
            foreach (var kvp in _feedbackConnections)
            {
                foreach (var receiver in kvp.Value)
                {
                    connections.Add((kvp.Key, receiver));
                }
            }

            return connections;
        }
    }

    /// <summary>
    /// Statistics about message bus activity
    /// </summary>
    public class MessageBusStats
    {
        public int TotalInboxes { get; set; }
        public int TotalMessagesInInboxes { get; set; }
        public int TotalMessagesSent { get; set; }
        public double RecentMessagesPerSecond { get; set; }
        public Dictionary<MessageType, int> MessagesByType { get; set; } = new();
        public double OldestMessageAge { get; set; }
    }
}
