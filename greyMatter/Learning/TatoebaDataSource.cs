using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GreyMatter.Learning
{
    /// <summary>
    /// Data source implementation for Tatoeba sentence database
    /// Tatoeba provides high-quality sentence pairs in multiple languages
    /// Format: TSV with columns: id, language, sentence_text
    /// </summary>
    public class TatoebaDataSource : IDataSource
    {
        private readonly string _filePath;
        private readonly TatoebaReader _reader;

        public string SourceName => "Tatoeba";
        public string Description => "Tatoeba Project - Multilingual sentence database";
        public DataSourceType SourceType => DataSourceType.SentencePairs;

        public TatoebaDataSource(string filePath)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _reader = new TatoebaReader();
        }

        public async Task<DataSourceValidation> ValidateAsync()
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // Check file exists
            if (!File.Exists(_filePath))
            {
                errors.Add($"File not found: {_filePath}");
                return DataSourceValidation.Failure("File does not exist", errors);
            }

            // Check file is readable
            try
            {
                using var fs = File.OpenRead(_filePath);
                if (fs.Length == 0)
                {
                    errors.Add("File is empty");
                    return DataSourceValidation.Failure("File is empty", errors);
                }

                // Verify format by reading first few lines
                using var sr = new StreamReader(fs);
                int linesChecked = 0;
                int validLines = 0;
                while (linesChecked < 10 && !sr.EndOfStream)
                {
                    var line = await sr.ReadLineAsync();
                    if (line != null)
                    {
                        var parts = line.Split('\t');
                        if (parts.Length >= 3)
                        {
                            validLines++;
                        }
                        linesChecked++;
                    }
                }

                if (validLines == 0)
                {
                    errors.Add("File does not appear to be in Tatoeba TSV format (expected: id\\tlang\\ttext)");
                    return DataSourceValidation.Failure("Invalid file format", errors);
                }

                if (validLines < linesChecked / 2)
                {
                    warnings.Add($"Only {validLines}/{linesChecked} lines appear valid - file may have formatting issues");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Error reading file: {ex.Message}");
                return DataSourceValidation.Failure("File read error", errors);
            }

            var message = warnings.Any() 
                ? $"Validation successful with {warnings.Count} warnings" 
                : "Validation successful";

            return new DataSourceValidation
            {
                IsValid = true,
                Message = message,
                Warnings = warnings
            };
        }

        public async Task<DataSourceMetadata> GetMetadataAsync()
        {
            var fileInfo = new FileInfo(_filePath);
            
            // Quick count estimation by sampling
            long estimatedCount = 0;
            if (fileInfo.Exists)
            {
                try
                {
                    using var fs = File.OpenRead(_filePath);
                    using var sr = new StreamReader(fs);
                    
                    // Count lines in first 1MB and extrapolate
                    int sampleSize = (int)Math.Min(1024 * 1024, fs.Length);
                    byte[] buffer = new byte[sampleSize];
                    await fs.ReadAsync(buffer, 0, sampleSize);
                    int linesInSample = buffer.Count(b => b == '\n');
                    
                    if (linesInSample > 0)
                    {
                        double ratio = (double)fs.Length / sampleSize;
                        estimatedCount = (long)(linesInSample * ratio);
                    }
                }
                catch
                {
                    estimatedCount = 0; // Estimation failed, set to 0
                }
            }

            return new DataSourceMetadata
            {
                SourceName = SourceName,
                FilePath = _filePath,
                FileSizeBytes = fileInfo.Length,
                EstimatedSentenceCount = estimatedCount,
                LastModified = fileInfo.LastWriteTime,
                AdditionalInfo = new Dictionary<string, object>
                {
                    ["Format"] = "TSV (Tab-Separated Values)",
                    ["Language"] = "English",
                    ["Columns"] = "id, language, sentence_text"
                }
            };
        }

        public async IAsyncEnumerable<SentenceData> LoadSentencesAsync(int? maxSentences = null)
        {
            int count = 0;
            
            await foreach (var sentence in LoadSentencesInternalAsync())
            {
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
            // Use existing TatoebaReader for proven parsing logic
            await Task.CompletedTask; // Make async
            
            foreach (var sentenceText in _reader.ReadEnglishSentences(_filePath))
            {
                yield return new SentenceData
                {
                    Text = sentenceText,
                    SourceName = SourceName,
                    Context = "dictionary_sentences",
                    Metadata = new Dictionary<string, object>
                    {
                        ["source_type"] = "tatoeba_database"
                    }
                };
            }
        }
    }
}
