using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GreyMatter.Learning
{
    /// <summary>
    /// Data source implementation for Children's Book Test (CBT) dataset
    /// CBT provides narrative text from children's literature
    /// Format: Plain text with book titles, chapters, and story content
    /// Excellent for learning conversational language patterns and narrative structure
    /// </summary>
    public class CBTDataSource : IDataSource
    {
        private readonly string _filePath;
        private static readonly Regex SentenceEndPattern = new Regex(@"[.!?]+\s+", RegexOptions.Compiled);

        public string SourceName => "CBT";
        public string Description => "Children's Book Test - Narrative literature dataset";
        public DataSourceType SourceType => DataSourceType.NarrativeText;

        public CBTDataSource(string filePath)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
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

                // Verify it's text by reading first chunk
                using var sr = new StreamReader(fs);
                var sample = await sr.ReadAsync(new char[1000], 0, 1000);
                if (sample == 0)
                {
                    errors.Add("Unable to read file content");
                    return DataSourceValidation.Failure("File read error", errors);
                }

                // Check for common CBT markers
                fs.Position = 0;
                var firstLines = new List<string>();
                sr.DiscardBufferedData();
                for (int i = 0; i < 10 && !sr.EndOfStream; i++)
                {
                    var line = await sr.ReadLineAsync();
                    if (line != null) firstLines.Add(line);
                }

                bool hasCBTMarker = firstLines.Any(l => 
                    l.Contains("_BOOK_TITLE_") || 
                    l.Contains("CHAPTER") || 
                    l.Contains(".txt.out"));

                if (!hasCBTMarker)
                {
                    warnings.Add("File does not contain typical CBT format markers");
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
            
            // Estimate sentence count based on file size
            // Average ~100 chars per sentence in narrative text
            long estimatedCount = fileInfo.Exists ? fileInfo.Length / 100 : 0;

            return new DataSourceMetadata
            {
                SourceName = SourceName,
                FilePath = _filePath,
                FileSizeBytes = fileInfo.Length,
                EstimatedSentenceCount = estimatedCount,
                LastModified = fileInfo.LastWriteTime,
                AdditionalInfo = new Dictionary<string, object>
                {
                    ["Format"] = "Plain text narrative",
                    ["ContentType"] = "Children's literature",
                    ["Language"] = "English"
                }
            };
        }

        public async IAsyncEnumerable<SentenceData> LoadSentencesAsync(int? maxSentences = null)
        {
            int count = 0;
            string? currentBook = null;
            string? currentChapter = null;

            using var fs = File.OpenRead(_filePath);
            using var sr = new StreamReader(fs);

            string? line;
            var paragraphBuffer = new List<string>();

            while ((line = await sr.ReadLineAsync()) != null)
            {
                // Track book/chapter context
                if (line.Contains("_BOOK_TITLE_"))
                {
                    currentBook = ExtractBookTitle(line);
                    continue;
                }
                if (line.StartsWith("CHAPTER"))
                {
                    currentChapter = line.Trim();
                    continue;
                }

                // Skip empty lines and metadata markers
                if (string.IsNullOrWhiteSpace(line) || 
                    line.Contains("-LCB-") || 
                    line.Contains("-RCB-"))
                {
                    // Process accumulated paragraph if any
                    if (paragraphBuffer.Any())
                    {
                        await foreach (var sentence in ProcessParagraphAsync(paragraphBuffer, currentBook, currentChapter))
                        {
                            yield return sentence;
                            count++;
                            
                            if (maxSentences.HasValue && count >= maxSentences.Value)
                            {
                                yield break;
                            }
                        }
                        paragraphBuffer.Clear();
                    }
                    continue;
                }

                // Accumulate text lines
                paragraphBuffer.Add(line.Trim());
            }

            // Process any remaining paragraph
            if (paragraphBuffer.Any())
            {
                await foreach (var sentence in ProcessParagraphAsync(paragraphBuffer, currentBook, currentChapter))
                {
                    yield return sentence;
                    count++;
                    
                    if (maxSentences.HasValue && count >= maxSentences.Value)
                    {
                        break;
                    }
                }
            }
        }

        private async IAsyncEnumerable<SentenceData> ProcessParagraphAsync(
            List<string> paragraphLines, 
            string? bookTitle, 
            string? chapter)
        {
            await Task.CompletedTask; // Make async

            // Join paragraph lines
            var paragraph = string.Join(" ", paragraphLines);

            // Split into sentences
            var sentences = SplitIntoSentences(paragraph);

            foreach (var sentence in sentences)
            {
                // Skip very short fragments
                if (sentence.Length < 10) continue;

                yield return new SentenceData
                {
                    Text = sentence,
                    SourceName = SourceName,
                    Context = "narrative_fiction",
                    Metadata = new Dictionary<string, object>
                    {
                        ["book_title"] = bookTitle ?? "unknown",
                        ["chapter"] = chapter ?? "unknown",
                        ["content_type"] = "children_literature"
                    }
                };
            }
        }

        private static List<string> SplitIntoSentences(string text)
        {
            var sentences = new List<string>();
            
            // Split on sentence endings (. ! ?)
            var parts = SentenceEndPattern.Split(text);
            
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.Length > 0)
                {
                    sentences.Add(trimmed);
                }
            }

            return sentences;
        }

        private static string ExtractBookTitle(string line)
        {
            // Format: "_BOOK_TITLE_ : Author___Title.txt.out"
            var parts = line.Split(':');
            if (parts.Length > 1)
            {
                var titlePart = parts[1].Trim();
                // Remove .txt.out extension
                titlePart = titlePart.Replace(".txt.out", "");
                return titlePart;
            }
            return "unknown";
        }
    }
}
