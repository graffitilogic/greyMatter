using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace GreyMatter.Learning
{
    /// <summary>
    /// Data source implementation for Enhanced Learning Data
    /// Pre-processed JSON database with word information and sentence contexts
    /// Provides rich metadata about words and their usage patterns
    /// </summary>
    public class EnhancedDataSource : IDataSource
    {
        private readonly string _wordDatabasePath;

        public string SourceName => "Enhanced";
        public string Description => "Enhanced pre-processed word database with contexts";
        public DataSourceType SourceType => DataSourceType.PreProcessed;

        public EnhancedDataSource(string wordDatabasePath)
        {
            _wordDatabasePath = wordDatabasePath ?? throw new ArgumentNullException(nameof(wordDatabasePath));
        }

        public async Task<DataSourceValidation> ValidateAsync()
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // Check file exists
            if (!File.Exists(_wordDatabasePath))
            {
                errors.Add($"File not found: {_wordDatabasePath}");
                return DataSourceValidation.Failure("File does not exist", errors);
            }

            // Check file is readable and valid JSON
            try
            {
                using var fs = File.OpenRead(_wordDatabasePath);
                if (fs.Length == 0)
                {
                    errors.Add("File is empty");
                    return DataSourceValidation.Failure("File is empty", errors);
                }

                // Try to parse a small chunk to verify JSON format
                using var sr = new StreamReader(fs);
                var sample = await sr.ReadToEndAsync();
                
                // Quick validation - should start with { for JSON object
                if (!sample.TrimStart().StartsWith("{"))
                {
                    errors.Add("File does not appear to be valid JSON");
                    return DataSourceValidation.Failure("Invalid JSON format", errors);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Error reading file: {ex.Message}");
                return DataSourceValidation.Failure("File read error", errors);
            }

            return DataSourceValidation.Success("Validation successful");
        }

        public async Task<DataSourceMetadata> GetMetadataAsync()
        {
            var fileInfo = new FileInfo(_wordDatabasePath);
            
            // For enhanced data, word count is a better metric than sentence count
            // We'll estimate based on file size
            long estimatedWords = fileInfo.Exists ? fileInfo.Length / 10000 : 0; // Rough estimate

            return new DataSourceMetadata
            {
                SourceName = SourceName,
                FilePath = _wordDatabasePath,
                FileSizeBytes = fileInfo.Length,
                EstimatedSentenceCount = estimatedWords * 10, // Each word typically has ~10 context sentences
                LastModified = fileInfo.LastWriteTime,
                AdditionalInfo = new Dictionary<string, object>
                {
                    ["Format"] = "JSON word database",
                    ["ContentType"] = "Pre-processed word contexts",
                    ["EstimatedWords"] = estimatedWords
                }
            };
        }

        public async IAsyncEnumerable<SentenceData> LoadSentencesAsync(int? maxSentences = null)
        {
            int count = 0;
            HashSet<string> seenSentences = new HashSet<string>();

            await foreach (var sentence in LoadSentencesInternalAsync())
            {
                // Skip duplicates (enhanced data may have same sentence for multiple words)
                if (seenSentences.Contains(sentence.Text))
                {
                    continue;
                }
                
                seenSentences.Add(sentence.Text);
                yield return sentence;
                count++;
                
                if (maxSentences.HasValue && count >= maxSentences.Value)
                {
                    break;
                }
            }
        }

        private async IAsyncEnumerable<SentenceData> LoadSentencesInternalAsync()
        {
            using var fs = File.OpenRead(_wordDatabasePath);
            
            // Parse JSON as a dictionary of word entries
            var wordDatabase = await JsonSerializer.DeserializeAsync<Dictionary<string, EnhancedWordEntry>>(fs);
            
            if (wordDatabase == null)
            {
                yield break;
            }

            // Extract sentences from sentence contexts
            foreach (var wordEntry in wordDatabase.Values)
            {
                if (wordEntry.SentenceContexts == null) continue;

                foreach (var context in wordEntry.SentenceContexts)
                {
                    // Context format appears to be: "title,sentence" or just sentence
                    var cleanedSentence = ExtractSentence(context);
                    
                    if (string.IsNullOrWhiteSpace(cleanedSentence) || cleanedSentence.Length < 10)
                    {
                        continue;
                    }

                    yield return new SentenceData
                    {
                        Text = cleanedSentence,
                        SourceName = SourceName,
                        Context = "news_headlines",
                        Metadata = new Dictionary<string, object>
                        {
                            ["word"] = wordEntry.Word ?? "unknown",
                            ["frequency"] = wordEntry.Frequency,
                            ["source_type"] = "pre_processed"
                        }
                    };
                }
            }
        }

        private static string ExtractSentence(string context)
        {
            // Enhanced data format has sentences within the context string
            // Try to extract the actual sentence text
            
            // Split by comma - first part might be title/source
            var parts = context.Split(',', 2);
            var sentence = parts.Length > 1 ? parts[1] : parts[0];
            
            // Clean up any JSON escaping
            sentence = sentence.Replace("\\\"", "\"");
            sentence = sentence.Replace("\u0022", "\"");
            sentence = sentence.Trim();
            
            // Remove quotes if wrapped
            if (sentence.StartsWith("\"") && sentence.EndsWith("\""))
            {
                sentence = sentence.Substring(1, sentence.Length - 2);
            }
            
            return sentence;
        }

        // Helper class to deserialize enhanced word database
        private class EnhancedWordEntry
        {
            public string? Word { get; set; }
            public int Frequency { get; set; }
            public List<string>? SentenceContexts { get; set; }
        }
    }
}
