using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GreyMatter.Core
{
    /// <summary>
    /// Interface for querying accumulated knowledge from the trained brain.
    /// Makes learned knowledge actually useful by providing structured access.
    /// </summary>
    public interface IKnowledgeQuery
    {
        /// <summary>
        /// Find words associated with a given word based on learned patterns.
        /// </summary>
        /// <param name="word">The word to find associations for</param>
        /// <param name="limit">Maximum number of associations to return</param>
        /// <returns>List of associated words with confidence scores</returns>
        Task<List<WordAssociation>> GetWordAssociationsAsync(string word, int limit = 10);
        
        /// <summary>
        /// Search for patterns similar to the given pattern across learned content.
        /// </summary>
        /// <param name="pattern">Pattern to search for (e.g., "subject verb object")</param>
        /// <param name="limit">Maximum number of results</param>
        /// <returns>List of matching patterns with examples</returns>
        Task<List<PatternMatch>> FindPatternsAsync(string pattern, int limit = 10);
        
        /// <summary>
        /// Search episodic memory for events matching the query.
        /// </summary>
        /// <param name="query">Natural language query (e.g., "sentences about animals")</param>
        /// <param name="limit">Maximum number of results</param>
        /// <returns>List of relevant episodic memories</returns>
        Task<List<EpisodicMemory>> SearchEpisodicMemoryAsync(string query, int limit = 10);
        
        /// <summary>
        /// Get vocabulary statistics (total words, frequency distribution, etc.)
        /// </summary>
        Task<VocabularyStats> GetVocabularyStatsAsync();
        
        /// <summary>
        /// Test knowledge quality by asking comprehension questions.
        /// </summary>
        /// <param name="questions">List of questions to test</param>
        /// <returns>Accuracy score (0.0 to 1.0)</returns>
        Task<double> TestComprehensionAsync(List<ComprehensionQuestion> questions);
        
        /// <summary>
        /// Get all words learned, optionally filtered.
        /// </summary>
        /// <param name="minFrequency">Minimum occurrence count</param>
        /// <param name="limit">Maximum number to return</param>
        Task<List<VocabularyWord>> GetLearnedWordsAsync(int minFrequency = 1, int limit = 1000);
    }
    
    /// <summary>
    /// Word association with confidence score
    /// </summary>
    public class WordAssociation
    {
        public string Word { get; set; } = "";
        public double Confidence { get; set; }
        public string RelationType { get; set; } = ""; // synonym, antonym, co-occurs, etc.
        public int CoOccurrenceCount { get; set; }
    }
    
    /// <summary>
    /// Pattern match result
    /// </summary>
    public class PatternMatch
    {
        public string Pattern { get; set; } = "";
        public string Example { get; set; } = "";
        public double Confidence { get; set; }
        public int Frequency { get; set; }
    }
    
    /// <summary>
    /// Episodic memory result
    /// </summary>
    public class EpisodicMemory
    {
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = "";
        public string Context { get; set; } = "";
        public double Relevance { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
    
    /// <summary>
    /// Vocabulary statistics
    /// </summary>
    public class VocabularyStats
    {
        public int TotalWords { get; set; }
        public int UniqueWords { get; set; }
        public int TotalSentences { get; set; }
        public double AverageWordFrequency { get; set; }
        public List<(string Word, int Frequency)> MostFrequent { get; set; } = new();
        public List<(string Word, int Frequency)> LeastFrequent { get; set; } = new();
    }
    
    /// <summary>
    /// Vocabulary word with metadata
    /// </summary>
    public class VocabularyWord
    {
        public string Word { get; set; } = "";
        public int Frequency { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public List<string> ContextExamples { get; set; } = new();
    }
    
    /// <summary>
    /// Comprehension test question
    /// </summary>
    public class ComprehensionQuestion
    {
        public string Question { get; set; } = "";
        public string ExpectedAnswer { get; set; } = "";
        public string Context { get; set; } = "";
    }
}
