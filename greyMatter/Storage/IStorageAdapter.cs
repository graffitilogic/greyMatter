using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GreyMatter.Storage
{
    /// <summary>
    /// Unified storage interface for brain state persistence.
    /// Enables decoupling of storage implementation from business logic.
    /// 
    /// Implementations:
    /// - FastStorageAdapter: High-performance MessagePack-based storage (1,350x speedup)
    /// - LegacyStorageAdapter: Compatibility wrapper for SemanticStorageManager (deprecated)
    /// 
    /// Design Goals:
    /// - Fast save/load operations (target: 50k-100k concepts in seconds)
    /// - Versioned schema support for safe format evolution
    /// - Snapshot capability for quick save/restore during experimentation
    /// - Storage tier abstraction (hot SSD / cold NAS)
    /// </summary>
    public interface IStorageAdapter
    {
        #region Core Brain State Persistence
        
        /// <summary>
        /// Save the complete brain state including neurons, synapses, and activation patterns.
        /// </summary>
        /// <param name="state">Dictionary containing brain state components</param>
        /// <returns>Task completing when save finishes</returns>
        Task SaveBrainStateAsync(Dictionary<string, object> state);
        
        /// <summary>
        /// Load the complete brain state from persistent storage.
        /// </summary>
        /// <returns>Dictionary containing all brain state components</returns>
        Task<Dictionary<string, object>> LoadBrainStateAsync();
        
        #endregion
        
        #region Vocabulary Management
        
        /// <summary>
        /// Save the learned vocabulary set.
        /// </summary>
        /// <param name="words">Set of all learned words</param>
        /// <returns>Task completing when vocabulary is saved</returns>
        Task SaveVocabularyAsync(HashSet<string> words);
        
        /// <summary>
        /// Load the learned vocabulary set.
        /// </summary>
        /// <returns>Set of all previously learned words</returns>
        Task<HashSet<string>> LoadVocabularyAsync();
        
        #endregion
        
        #region Neural Concepts
        
        /// <summary>
        /// Save neural concept representations (sparse distributed representations).
        /// </summary>
        /// <param name="concepts">Dictionary mapping concept IDs to neural data</param>
        /// <returns>Task completing when concepts are saved</returns>
        Task SaveNeuralConceptsAsync(Dictionary<string, object> concepts);
        
        /// <summary>
        /// Load neural concept representations.
        /// </summary>
        /// <returns>Dictionary of concept IDs to neural data</returns>
        Task<Dictionary<string, object>> LoadNeuralConceptsAsync();
        
        #endregion
        
        #region Snapshot Management
        
        /// <summary>
        /// Create a named snapshot of the current brain state for quick restore.
        /// Useful for experimentation, A/B testing, and recovery points.
        /// </summary>
        /// <param name="label">Optional label for the snapshot (defaults to timestamp)</param>
        /// <returns>Snapshot ID that can be used for restoration</returns>
        Task<string> CreateSnapshotAsync(string label = "");
        
        /// <summary>
        /// Restore brain state from a previously created snapshot.
        /// </summary>
        /// <param name="snapshotId">ID of the snapshot to restore</param>
        /// <returns>Task completing when restoration finishes</returns>
        Task RestoreSnapshotAsync(string snapshotId);
        
        /// <summary>
        /// List all available snapshots with metadata.
        /// </summary>
        /// <returns>List of snapshot information ordered by creation time (newest first)</returns>
        Task<List<SnapshotInfo>> ListSnapshotsAsync();
        
        #endregion
        
        #region Version and Integrity
        
        /// <summary>
        /// Get the storage format version being used.
        /// </summary>
        string SchemaVersion { get; }
        
        /// <summary>
        /// Verify integrity of stored data (checksums, format validation).
        /// </summary>
        /// <returns>True if all integrity checks pass, false otherwise</returns>
        Task<bool> VerifyIntegrityAsync();
        
        #endregion
    }
    
    /// <summary>
    /// Metadata about a brain state snapshot.
    /// </summary>
    public class SnapshotInfo
    {
        /// <summary>
        /// Unique identifier for this snapshot.
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Human-readable label (optional).
        /// </summary>
        public string Label { get; set; } = string.Empty;
        
        /// <summary>
        /// When the snapshot was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Size of vocabulary at snapshot time.
        /// </summary>
        public int VocabularySize { get; set; }
        
        /// <summary>
        /// Number of neural concepts stored.
        /// </summary>
        public int ConceptCount { get; set; }
        
        /// <summary>
        /// Storage format version used.
        /// </summary>
        public string SchemaVersion { get; set; } = "1.0";
        
        /// <summary>
        /// Total size in bytes.
        /// </summary>
        public long SizeBytes { get; set; }
        
        /// <summary>
        /// SHA256 checksum for integrity verification.
        /// </summary>
        public string Checksum { get; set; } = string.Empty;
    }
}
