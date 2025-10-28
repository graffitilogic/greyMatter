using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GreyMatter.Learning
{
    /// <summary>
    /// Unified interface for all data sources in the multi-source learning pipeline.
    /// Enables consistent loading, validation, and metadata tracking across diverse data formats.
    /// </summary>
    public interface IDataSource
    {
        /// <summary>
        /// Unique identifier for this data source (e.g., "Tatoeba", "CBT", "Enhanced")
        /// Used for source attribution in learned concepts
        /// </summary>
        string SourceName { get; }

        /// <summary>
        /// Human-readable description of the data source
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Type of content provided by this source
        /// </summary>
        DataSourceType SourceType { get; }

        /// <summary>
        /// Validate that the data source is accessible and readable
        /// </summary>
        /// <returns>ValidationResult with status and any error messages</returns>
        Task<DataSourceValidation> ValidateAsync();

        /// <summary>
        /// Get metadata about the data source (file size, record count, etc.)
        /// </summary>
        Task<DataSourceMetadata> GetMetadataAsync();

        /// <summary>
        /// Load sentences from the data source
        /// Returns an enumerable stream to support large datasets without loading entirely into memory
        /// </summary>
        /// <param name="maxSentences">Optional limit on number of sentences to load</param>
        IAsyncEnumerable<SentenceData> LoadSentencesAsync(int? maxSentences = null);
    }

    /// <summary>
    /// Type of data source
    /// </summary>
    public enum DataSourceType
    {
        /// <summary>Raw sentence pairs (e.g., Tatoeba)</summary>
        SentencePairs,
        
        /// <summary>Narrative text (e.g., books, stories)</summary>
        NarrativeText,
        
        /// <summary>Pre-processed structured data (e.g., word databases)</summary>
        PreProcessed,
        
        /// <summary>XML/HTML documents (e.g., Wikipedia)</summary>
        StructuredDocuments,
        
        /// <summary>Key-value concept relationships</summary>
        ConceptualData
    }

    /// <summary>
    /// Validation result from a data source
    /// </summary>
    public class DataSourceValidation
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();

        public static DataSourceValidation Success(string message = "Validation successful")
        {
            return new DataSourceValidation { IsValid = true, Message = message };
        }

        public static DataSourceValidation Failure(string message, List<string>? errors = null)
        {
            return new DataSourceValidation 
            { 
                IsValid = false, 
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
    }

    /// <summary>
    /// Metadata about a data source
    /// </summary>
    public class DataSourceMetadata
    {
        public string SourceName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public long EstimatedSentenceCount { get; set; }
        public DateTime? LastModified { get; set; }
        public Dictionary<string, object> AdditionalInfo { get; set; } = new Dictionary<string, object>();

        public string FileSizeFormatted => FormatFileSize(FileSizeBytes);

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// Represents a single sentence with metadata from a data source
    /// </summary>
    public class SentenceData
    {
        /// <summary>The actual sentence text</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>Source this sentence came from</summary>
        public string SourceName { get; set; } = string.Empty;

        /// <summary>Optional: Context or category (e.g., "fiction", "technical", "conversation")</summary>
        public string? Context { get; set; }

        /// <summary>Optional: Metadata specific to this sentence</summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>Timestamp when loaded</summary>
        public DateTime LoadedAt { get; set; } = DateTime.UtcNow;
    }
}
