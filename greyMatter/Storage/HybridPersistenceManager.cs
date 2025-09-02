using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter.Storage
{
    /// <summary>
    /// Hybrid persistence strategy for procedural neural networks
    /// Decides what to persist vs procedurally generate based on usage patterns and importance
    /// Inspired by No Man's Sky's selective persistence approach
    /// </summary>
    public class HybridPersistenceManager
    {
        private readonly string _brainDataPath;
        private readonly ProceduralCorticalColumnGenerator _generator;
        private readonly Dictionary<string, PersistenceDecision> _persistenceDecisions;
        private readonly Dictionary<string, DateTime> _lastAccessTimes;
        private readonly Dictionary<string, int> _accessCounts;

        // Persistence thresholds
        private const int HIGH_USAGE_THRESHOLD = 100;      // Persist if accessed > 100 times
        private const int RECENT_ACCESS_THRESHOLD_DAYS = 7; // Persist if accessed within 7 days
        private const double STRUCTURAL_IMPORTANCE_THRESHOLD = 0.8; // Persist if structural importance > 0.8
        private const int MAX_PERSISTED_COLUMNS = 10000;   // Don't persist more than 10K columns

        public HybridPersistenceManager(string brainDataPath, ProceduralCorticalColumnGenerator generator)
        {
            _brainDataPath = brainDataPath;
            _generator = generator;
            _persistenceDecisions = new Dictionary<string, PersistenceDecision>();
            _lastAccessTimes = new Dictionary<string, DateTime>();
            _accessCounts = new Dictionary<string, int>();

            LoadPersistenceDecisions();
        }

        /// <summary>
        /// Get or generate a cortical column using hybrid persistence strategy
        /// </summary>
        public async Task<ProceduralCorticalColumn> GetColumnAsync(
            string columnType,
            SemanticCoordinates coordinates,
            TaskRequirements requirements)
        {
            var columnId = GenerateColumnId(columnType, coordinates);

            // Check if we should load from persistence
            if (ShouldLoadFromPersistence(columnId))
            {
                var persistedColumn = await LoadPersistedColumnAsync(columnId);
                if (persistedColumn != null)
                {
                    UpdateAccessMetrics(columnId);
                    return persistedColumn;
                }
            }

            // Generate procedurally
            var generatedColumn = await _generator.GenerateColumnAsync(columnType, coordinates, requirements);
            UpdateAccessMetrics(generatedColumn.Id);

            // Decide whether to persist this column
            var decision = await EvaluatePersistenceDecisionAsync(generatedColumn);
            _persistenceDecisions[generatedColumn.Id] = decision;

            if (decision.ShouldPersist)
            {
                await PersistColumnAsync(generatedColumn);
            }

            return generatedColumn;
        }

        /// <summary>
        /// Determine if a column should be loaded from persistence
        /// </summary>
        private bool ShouldLoadFromPersistence(string columnId)
        {
            if (!_persistenceDecisions.TryGetValue(columnId, out var decision))
                return false;

            return decision.ShouldPersist && decision.LastPersisted > DateTime.UtcNow.AddDays(-1);
        }

        /// <summary>
        /// Evaluate whether a column should be persisted
        /// </summary>
        private async Task<PersistenceDecision> EvaluatePersistenceDecisionAsync(ProceduralCorticalColumn column)
        {
            var decision = new PersistenceDecision
            {
                ColumnId = column.Id,
                ShouldPersist = false,
                Reason = "default",
                StructuralImportance = CalculateStructuralImportance(column),
                LastPersisted = DateTime.UtcNow
            };

            // High usage threshold
            var accessCount = _accessCounts.GetValueOrDefault(column.Id, 0);
            if (accessCount > HIGH_USAGE_THRESHOLD)
            {
                decision.ShouldPersist = true;
                decision.Reason = "high_usage";
                return decision;
            }

            // Recent access threshold
            var lastAccess = _lastAccessTimes.GetValueOrDefault(column.Id, DateTime.MinValue);
            if ((DateTime.UtcNow - lastAccess).TotalDays < RECENT_ACCESS_THRESHOLD_DAYS)
            {
                decision.ShouldPersist = true;
                decision.Reason = "recent_access";
                return decision;
            }

            // Structural importance threshold
            if (decision.StructuralImportance > STRUCTURAL_IMPORTANCE_THRESHOLD)
            {
                decision.ShouldPersist = true;
                decision.Reason = "structural_importance";
                return decision;
            }

            // Complex columns (large size or high complexity)
            if (column.Size > 4000 || column.Complexity > 1.5)
            {
                decision.ShouldPersist = true;
                decision.Reason = "complex_structure";
                return decision;
            }

            // Don't exceed maximum persisted columns
            var currentPersistedCount = _persistenceDecisions.Count(d => d.Value.ShouldPersist);
            if (currentPersistedCount >= MAX_PERSISTED_COLUMNS)
            {
                decision.ShouldPersist = false;
                decision.Reason = "max_capacity_reached";
                return decision;
            }

            return decision;
        }

        /// <summary>
        /// Calculate structural importance of a column
        /// </summary>
        private double CalculateStructuralImportance(ProceduralCorticalColumn column)
        {
            var importance = 0.0;

            // Size importance (larger = more important)
            importance += Math.Min(column.Size / 4096.0, 0.3);

            // Complexity importance
            importance += Math.Min(column.Complexity / 2.0, 0.3);

            // Sparsity importance (more sparse = more important for efficiency)
            importance += Math.Min((column.Sparsity - 0.01) / 0.04, 0.2);

            // Pattern diversity importance
            var patternCount = column.NeuralPatterns?.Count ?? 0;
            importance += Math.Min(patternCount / 10.0, 0.2);

            return Math.Min(importance, 1.0);
        }

        /// <summary>
        /// Persist a column to storage
        /// </summary>
        private async Task PersistColumnAsync(ProceduralCorticalColumn column)
        {
            try
            {
                var persistencePath = GetPersistencePath(column.Id);
                Directory.CreateDirectory(Path.GetDirectoryName(persistencePath));

                var json = JsonSerializer.Serialize(column, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(persistencePath, json);
                Console.WriteLine($"💾 Persisted cortical column: {column.Id} ({column.Type})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to persist column {column.Id}: {ex.Message}");
            }
        }

        /// <summary>
        /// Load a persisted column
        /// </summary>
        private async Task<ProceduralCorticalColumn> LoadPersistedColumnAsync(string columnId)
        {
            try
            {
                var persistencePath = GetPersistencePath(columnId);

                if (!File.Exists(persistencePath))
                    return null;

                var json = await File.ReadAllTextAsync(persistencePath);
                var column = JsonSerializer.Deserialize<ProceduralCorticalColumn>(json);

                if (column != null)
                {
                    Console.WriteLine($"📚 Loaded persisted cortical column: {columnId}");
                }

                return column;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to load persisted column {columnId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Update access metrics for a column
        /// </summary>
        private void UpdateAccessMetrics(string columnId)
        {
            _lastAccessTimes[columnId] = DateTime.UtcNow;
            _accessCounts[columnId] = _accessCounts.GetValueOrDefault(columnId, 0) + 1;
        }

        /// <summary>
        /// Generate column ID (consistent with generator)
        /// </summary>
        private string GenerateColumnId(string columnType, SemanticCoordinates coordinates)
        {
            var coordinateString = $"{coordinates.Domain}:{coordinates.Topic}:{coordinates.Context}";
            var combined = $"{columnType}:{coordinateString}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));
            return $"{columnType}_{BitConverter.ToString(hash).Replace("-", "").Substring(0, 16)}";
        }

        /// <summary>
        /// Get persistence file path for a column
        /// </summary>
        private string GetPersistencePath(string columnId)
        {
            var persistenceDir = Path.Combine(_brainDataPath, "procedural_columns");
            return Path.Combine(persistenceDir, $"{columnId}.json");
        }

        /// <summary>
        /// Load existing persistence decisions
        /// </summary>
        private void LoadPersistenceDecisions()
        {
            try
            {
                var decisionsPath = Path.Combine(_brainDataPath, "persistence_decisions.json");
                if (File.Exists(decisionsPath))
                {
                    var json = File.ReadAllText(decisionsPath);
                    var decisions = JsonSerializer.Deserialize<Dictionary<string, PersistenceDecision>>(json);
                    if (decisions != null)
                    {
                        foreach (var kvp in decisions)
                        {
                            _persistenceDecisions[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to load persistence decisions: {ex.Message}");
            }
        }

        /// <summary>
        /// Save persistence decisions
        /// </summary>
        public async Task SavePersistenceDecisionsAsync()
        {
            try
            {
                var decisionsPath = Path.Combine(_brainDataPath, "persistence_decisions.json");
                Directory.CreateDirectory(Path.GetDirectoryName(decisionsPath));

                var json = JsonSerializer.Serialize(_persistenceDecisions, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(decisionsPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to save persistence decisions: {ex.Message}");
            }
        }

        /// <summary>
        /// Get persistence statistics
        /// </summary>
        public PersistenceStats GetStats()
        {
            var persistedCount = _persistenceDecisions.Count(d => d.Value.ShouldPersist);
            var generatedCount = _persistenceDecisions.Count - persistedCount;

            return new PersistenceStats
            {
                TotalColumns = _persistenceDecisions.Count,
                PersistedColumns = persistedCount,
                GeneratedColumns = generatedCount,
                PersistenceRatio = persistedCount / (double)_persistenceDecisions.Count,
                AverageStructuralImportance = _persistenceDecisions.Values.Average(d => d.StructuralImportance)
            };
        }

        /// <summary>
        /// Clean up old/unused persisted columns to free space
        /// </summary>
        public async Task CleanupOldColumnsAsync(int maxAgeDays = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-maxAgeDays);
            var columnsToRemove = new List<string>();

            foreach (var kvp in _persistenceDecisions)
            {
                var decision = kvp.Value;
                if (decision.ShouldPersist &&
                    _lastAccessTimes.GetValueOrDefault(kvp.Key, DateTime.MinValue) < cutoffDate &&
                    _accessCounts.GetValueOrDefault(kvp.Key, 0) < HIGH_USAGE_THRESHOLD / 2)
                {
                    // Mark for removal from persistence
                    decision.ShouldPersist = false;
                    decision.Reason = "cleanup_old";

                    // Delete the persisted file
                    var persistencePath = GetPersistencePath(kvp.Key);
                    if (File.Exists(persistencePath))
                    {
                        File.Delete(persistencePath);
                    }

                    columnsToRemove.Add(kvp.Key);
                }
            }

            Console.WriteLine($"🧹 Cleaned up {columnsToRemove.Count} old persisted columns");
            await SavePersistenceDecisionsAsync();
        }
    }

    /// <summary>
    /// Decision about whether to persist a column
    /// </summary>
    public class PersistenceDecision
    {
        public string ColumnId { get; set; }
        public bool ShouldPersist { get; set; }
        public string Reason { get; set; }
        public double StructuralImportance { get; set; }
        public DateTime LastPersisted { get; set; }
    }

    /// <summary>
    /// Statistics for persistence system
    /// </summary>
    public class PersistenceStats
    {
        public int TotalColumns { get; set; }
        public int PersistedColumns { get; set; }
        public int GeneratedColumns { get; set; }
        public double PersistenceRatio { get; set; }
        public double AverageStructuralImportance { get; set; }
    }
}
