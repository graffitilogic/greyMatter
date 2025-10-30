using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter.Core
{
    /// <summary>
    /// Types of language input the system can process
    /// </summary>
    public enum LanguageInputType
    {
        /// <summary>Written sentence (Tatoeba, books, articles)</summary>
        Sentence,
        
        /// <summary>Spoken utterance (audio transcription, conversation)</summary>
        Speech,
        
        /// <summary>Reading comprehension passage (multi-sentence)</summary>
        Passage,
        
        /// <summary>Conversational exchange (multi-turn dialogue)</summary>
        Conversation,
        
        /// <summary>Question requiring answer</summary>
        Query
    }

    /// <summary>
    /// Unified interface for language input across all modalities
    /// Enables column-based processor to handle sentences, passages, conversations, etc.
    /// </summary>
    public class LanguageInput
    {
        /// <summary>Type of input</summary>
        public LanguageInputType Type { get; set; }

        /// <summary>Primary content (sentence text, passage, etc.)</summary>
        public string Content { get; set; } = "";

        /// <summary>Optional secondary content (translation, answer, etc.)</summary>
        public string? SecondaryContent { get; set; }

        /// <summary>Source language (for multilingual processing)</summary>
        public string SourceLanguage { get; set; } = "en";

        /// <summary>Target language (for translation tasks)</summary>
        public string? TargetLanguage { get; set; }

        /// <summary>Domain/context (technical, conversational, literary, etc.)</summary>
        public string Domain { get; set; } = "general";

        /// <summary>Optional metadata (speaker, timestamp, topic tags, etc.)</summary>
        public Dictionary<string, string> Metadata { get; set; } = new();

        /// <summary>Individual words/tokens (populated by tokenizer)</summary>
        public List<string> Tokens { get; set; } = new();

        /// <summary>
        /// Create from simple sentence (most common case - Tatoeba)
        /// </summary>
        public static LanguageInput FromSentence(string sentence, string language = "en", string domain = "general")
        {
            return new LanguageInput
            {
                Type = LanguageInputType.Sentence,
                Content = sentence,
                SourceLanguage = language,
                Domain = domain,
                Tokens = SimpleTokenize(sentence)
            };
        }

        /// <summary>
        /// Create from reading passage (multi-sentence)
        /// </summary>
        public static LanguageInput FromPassage(string passage, string language = "en", string domain = "general")
        {
            return new LanguageInput
            {
                Type = LanguageInputType.Passage,
                Content = passage,
                SourceLanguage = language,
                Domain = domain,
                Tokens = SimpleTokenize(passage)
            };
        }

        /// <summary>
        /// Create from conversational turn
        /// </summary>
        public static LanguageInput FromConversation(string utterance, string speaker, string language = "en")
        {
            return new LanguageInput
            {
                Type = LanguageInputType.Conversation,
                Content = utterance,
                SourceLanguage = language,
                Domain = "conversational",
                Metadata = new Dictionary<string, string> { ["speaker"] = speaker },
                Tokens = SimpleTokenize(utterance)
            };
        }

        /// <summary>
        /// Simple word tokenization (can be enhanced later)
        /// </summary>
        private static List<string> SimpleTokenize(string text)
        {
            return text
                .ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']' }, 
                       StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }
    }

    /// <summary>
    /// Column-based processor for language input
    /// Routes inputs through phonetic → semantic → syntactic → episodic pipeline
    /// Uses working memory, message bus, and attention system
    /// </summary>
    public class ColumnBasedProcessor
    {
        private readonly ProceduralCorticalColumnGenerator _columnGenerator;
        private readonly WorkingMemory _workingMemory;
        private readonly MessageBus _messageBus;
        private readonly AttentionSystem _attentionSystem;

        // Active columns for current processing
        private readonly Dictionary<string, ProceduralCorticalColumn> _activeColumns;

        public ColumnBasedProcessor(
            ProceduralCorticalColumnGenerator columnGenerator,
            WorkingMemory workingMemory,
            MessageBus messageBus,
            AttentionSystem attentionSystem)
        {
            _columnGenerator = columnGenerator;
            _workingMemory = workingMemory;
            _messageBus = messageBus;
            _attentionSystem = attentionSystem;
            _activeColumns = new Dictionary<string, ProceduralCorticalColumn>();
        }

        /// <summary>
        /// Process language input through column pipeline
        /// Returns learned patterns and statistics
        /// </summary>
        public async Task<ColumnProcessingResult> ProcessInputAsync(LanguageInput input)
        {
            var result = new ColumnProcessingResult
            {
                InputType = input.Type,
                ProcessingSteps = new List<ProcessingStep>()
            };

            // Set attention profile based on input type
            SetAttentionProfile(input.Type);

            // Process through column pipeline
            foreach (var token in input.Tokens)
            {
                await ProcessTokenThroughPipelineAsync(token, input, result);
            }

            // Aggregate results
            result.TotalPatternsLearned = _workingMemory.Count;
            result.MessagesSent = _messageBus.GetStats().TotalMessagesSent;
            result.AttentionFocus = _attentionSystem.GetStats();

            return result;
        }

        /// <summary>
        /// Process a single token through the full column pipeline
        /// </summary>
        private async Task ProcessTokenThroughPipelineAsync(
            string token,
            LanguageInput input,
            ColumnProcessingResult result)
        {
            var coordinates = CreateSemanticCoordinates(token, input);
            var requirements = CreateTaskRequirements(input);

            // Step 1: Phonetic Column (sound/form patterns)
            var phoneticColumn = await GetOrCreateColumnAsync("phonetic", coordinates, requirements);
            var phoneticPattern = await ProcessPhoneticAsync(token, phoneticColumn);
            
            if (phoneticPattern != null)
            {
                _workingMemory.Store($"phonetic_{token}", phoneticPattern, 1.0, $"phonetic pattern for '{token}'");
                result.ProcessingSteps.Add(new ProcessingStep
                {
                    ColumnType = "phonetic",
                    Token = token,
                    PatternGenerated = true
                });

                // Send to semantic column
                phoneticColumn.BroadcastToType("semantic", MessageType.Forward, phoneticPattern, 0.8);
            }

            // Step 2: Semantic Column (meaning patterns)
            var semanticColumn = await GetOrCreateColumnAsync("semantic", coordinates, requirements);
            var semanticPattern = await ProcessSemanticAsync(token, semanticColumn, phoneticPattern);
            
            if (semanticPattern != null)
            {
                _workingMemory.Store($"semantic_{token}", semanticPattern, 1.0, $"semantic pattern for '{token}'");
                result.ProcessingSteps.Add(new ProcessingStep
                {
                    ColumnType = "semantic",
                    Token = token,
                    PatternGenerated = true
                });

                // Send to syntactic and episodic
                semanticColumn.BroadcastToType("syntactic", MessageType.Forward, semanticPattern, 0.7);
                semanticColumn.BroadcastToType("episodic", MessageType.Forward, semanticPattern, 0.5);
            }

            // Step 3: Syntactic Column (structure patterns)
            var syntacticColumn = await GetOrCreateColumnAsync("syntactic", coordinates, requirements);
            var syntacticPattern = await ProcessSyntacticAsync(token, syntacticColumn, semanticPattern);
            
            if (syntacticPattern != null)
            {
                _workingMemory.Store($"syntactic_{token}", syntacticPattern, 1.0, $"syntactic pattern for '{token}'");
                result.ProcessingSteps.Add(new ProcessingStep
                {
                    ColumnType = "syntactic",
                    Token = token,
                    PatternGenerated = true
                });

                // Send to contextual and episodic
                syntacticColumn.BroadcastToType("contextual", MessageType.Forward, syntacticPattern, 0.6);
                syntacticColumn.BroadcastToType("episodic", MessageType.Forward, syntacticPattern, 0.5);
            }

            // Step 4: Episodic Column (memory consolidation)
            var episodicColumn = await GetOrCreateColumnAsync("episodic", coordinates, requirements);
            await ProcessEpisodicAsync(token, episodicColumn, semanticPattern, syntacticPattern);

            // Update attention activations
            _attentionSystem.UpdateActivation(phoneticColumn.Id, 0.8);
            _attentionSystem.UpdateActivation(semanticColumn.Id, 1.0);
            _attentionSystem.UpdateActivation(syntacticColumn.Id, 0.7);
            _attentionSystem.UpdateActivation(episodicColumn.Id, 0.6);
        }

        /// <summary>
        /// Get or create column with specified type and coordinates
        /// </summary>
        private async Task<ProceduralCorticalColumn> GetOrCreateColumnAsync(
            string columnType,
            SemanticCoordinates coordinates,
            TaskRequirements requirements)
        {
            var column = await _columnGenerator.GenerateColumnAsync(columnType, coordinates, requirements);
            
            // Wire up working memory and message bus
            column.WorkingMemory = _workingMemory;
            column.MessageBus = _messageBus;

            // CRITICAL FIX: Register column with message bus so it can receive messages
            _messageBus?.RegisterColumn(column.Id);

            // Track active column
            _activeColumns[column.Id] = column;

            return column;
        }

        /// <summary>
        /// Process phonetic patterns (sound/form)
        /// </summary>
        private async Task<SparsePattern?> ProcessPhoneticAsync(string token, ProceduralCorticalColumn column)
        {
            // Check if we have a learned pattern for this token
            var existingPattern = column.NeuralPatterns.TryGetValue("phonetic_pattern", out var pattern) 
                ? pattern 
                : null;

            if (existingPattern != null)
            {
                return existingPattern;
            }

            // Generate new phonetic pattern based on token characters
            var charBits = token.ToCharArray()
                .Select(c => (int)c % column.Size)
                .Distinct()
                .ToArray();

            return new SparsePattern(charBits, 1);
        }

        /// <summary>
        /// Process semantic patterns (meaning)
        /// </summary>
        private async Task<SparsePattern?> ProcessSemanticAsync(
            string token,
            ProceduralCorticalColumn column,
            SparsePattern? phoneticPattern)
        {
            // Check working memory for related semantic patterns
            var similarPatterns = phoneticPattern != null 
                ? _workingMemory.QuerySimilar(phoneticPattern, topK: 3, minSimilarity: 0.3)
                : new List<(string, double)>();

            // Generate semantic pattern influenced by phonetic input and memory
            var baseSemantic = column.NeuralPatterns.TryGetValue("semantic", out var pattern) 
                ? pattern 
                : null;

            if (baseSemantic != null)
            {
                return baseSemantic;
            }

            // Create new semantic pattern
            var semanticBits = token.GetHashCode() % column.Size;
            var bitArray = Enumerable.Range(0, (int)(column.Size * column.Sparsity))
                .Select(i => (semanticBits + i * 13) % column.Size)
                .ToArray();

            return new SparsePattern(bitArray, similarPatterns.Count + 1);
        }

        /// <summary>
        /// Process syntactic patterns (structure)
        /// </summary>
        private async Task<SparsePattern?> ProcessSyntacticAsync(
            string token,
            ProceduralCorticalColumn column,
            SparsePattern? semanticPattern)
        {
            // Syntactic patterns focus on word position and relationships
            // Influenced by semantic input and working memory context
            
            var contextKeys = _workingMemory.Keys
                .Where(k => k.StartsWith("syntactic_"))
                .ToList();

            var baseSyntactic = column.NeuralPatterns.TryGetValue("syntactic", out var pattern)
                ? pattern
                : null;

            if (baseSyntactic != null)
            {
                return baseSyntactic;
            }

            // Create syntactic pattern based on token position in working memory
            var position = _workingMemory.CurrentContext.ActivationCount;
            var syntacticBits = Enumerable.Range(0, (int)(column.Size * column.Sparsity))
                .Select(i => ((position * 17) + (i * 23)) % column.Size)
                .ToArray();

            return new SparsePattern(syntacticBits, position);
        }

        /// <summary>
        /// Process episodic patterns (memory consolidation)
        /// </summary>
        private async Task ProcessEpisodicAsync(
            string token,
            ProceduralCorticalColumn column,
            SparsePattern? semanticPattern,
            SparsePattern? syntacticPattern)
        {
            // Episodic column consolidates patterns into long-term memory
            // This is where learning actually happens

            if (semanticPattern != null && syntacticPattern != null)
            {
                // Combine semantic and syntactic into episodic memory
                var combinedBits = semanticPattern.ActiveBits
                    .Concat(syntacticPattern.ActiveBits)
                    .Distinct()
                    .ToArray();

                var episodicPattern = new SparsePattern(combinedBits, semanticPattern.ActiveBits.Length + syntacticPattern.ActiveBits.Length);
                _workingMemory.Store($"episodic_{token}", episodicPattern, 1.0, $"episodic memory for '{token}'");

                // Store in column's neural patterns (long-term learning)
                column.NeuralPatterns[$"learned_{token}"] = episodicPattern;
            }
        }

        /// <summary>
        /// Set attention profile based on input type
        /// </summary>
        private void SetAttentionProfile(LanguageInputType inputType)
        {
            var profile = inputType switch
            {
                LanguageInputType.Sentence => "reading",
                LanguageInputType.Speech => "listening",
                LanguageInputType.Passage => "comprehension",
                LanguageInputType.Conversation => "production",
                LanguageInputType.Query => "recall",
                _ => "learning"
            };

            _attentionSystem.SetProfile(profile);
        }

        /// <summary>
        /// Create semantic coordinates from token and input context
        /// </summary>
        private SemanticCoordinates CreateSemanticCoordinates(string token, LanguageInput input)
        {
            return new SemanticCoordinates
            {
                Domain = input.Domain,
                Topic = token,
                Context = input.Type.ToString(),
                IsAbstract = false, // Could be enhanced with word analysis
                PolysemyCount = 1  // Could be enhanced with dictionary lookup
            };
        }

        /// <summary>
        /// Create task requirements from input context
        /// </summary>
        private TaskRequirements CreateTaskRequirements(LanguageInput input)
        {
            return new TaskRequirements
            {
                Complexity = input.Type == LanguageInputType.Passage ? "high" : "medium",
                Precision = "medium",
                ExpectedLoad = input.Tokens.Count
            };
        }

        /// <summary>
        /// Apply decay to working memory and attention
        /// Should be called periodically
        /// </summary>
        public void ApplyDecay()
        {
            _workingMemory.DecayActivations();
            _attentionSystem.DecayActivations();
        }

        /// <summary>
        /// Get processing statistics
        /// </summary>
        public ProcessorStats GetStats()
        {
            return new ProcessorStats
            {
                ActiveColumns = _activeColumns.Count,
                WorkingMemoryStats = _workingMemory.GetStats(),
                MessageBusStats = _messageBus.GetStats(),
                AttentionStats = _attentionSystem.GetStats()
            };
        }
    }

    /// <summary>
    /// Result of processing language input through column pipeline
    /// </summary>
    public class ColumnProcessingResult
    {
        public LanguageInputType InputType { get; set; }
        public List<ProcessingStep> ProcessingSteps { get; set; } = new();
        public int TotalPatternsLearned { get; set; }
        public int MessagesSent { get; set; }
        public AttentionStats? AttentionFocus { get; set; }
    }

    /// <summary>
    /// Single step in processing pipeline
    /// </summary>
    public class ProcessingStep
    {
        public string ColumnType { get; set; } = "";
        public string Token { get; set; } = "";
        public bool PatternGenerated { get; set; }
    }

    /// <summary>
    /// Combined statistics from processor
    /// </summary>
    public class ProcessorStats
    {
        public int ActiveColumns { get; set; }
        public WorkingMemoryStats? WorkingMemoryStats { get; set; }
        public MessageBusStats? MessageBusStats { get; set; }
        public AttentionStats? AttentionStats { get; set; }
    }
}
