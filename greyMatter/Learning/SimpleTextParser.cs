using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace greyMatter.Learning
{
    /// <summary>
    /// Simple text parser for Phase 3: extracting concepts from real text
    /// Focuses on simple sentences and basic pattern recognition
    /// </summary>
    public class SimpleTextParser
    {
        private readonly HashSet<string> _stopWords;
        private readonly Regex _sentencePattern;
        private readonly Regex _wordPattern;

        public SimpleTextParser()
        {
            _stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", 
                "of", "with", "by", "is", "are", "was", "were", "be", "been", "have", 
                "has", "had", "do", "does", "did", "will", "would", "could", "should",
                "this", "that", "these", "those", "i", "you", "he", "she", "it", "we", "they"
            };
            
            _sentencePattern = new Regex(@"[.!?]+\s*", RegexOptions.Compiled);
            _wordPattern = new Regex(@"\b\w+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Parse text into learning lessons for the ephemeral brain
        /// </summary>
        public List<LearningLesson> ParseText(string text)
        {
            var lessons = new List<LearningLesson>();
            var sentences = SplitIntoSentences(text);
            
            foreach (var sentence in sentences)
            {
                var lesson = ParseSentence(sentence);
                if (lesson != null)
                {
                    lessons.Add(lesson);
                }
            }
            
            return lessons;
        }

        /// <summary>
        /// Parse a single sentence into concepts and relationships
        /// </summary>
        public LearningLesson? ParseSentence(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence)) return null;
            
            var words = ExtractWords(sentence);
            if (words.Count < 2) return null; // Need at least 2 words for meaningful concepts
            
            var lesson = new LearningLesson
            {
                OriginalText = sentence.Trim(),
                MainConcepts = ExtractMainConcepts(words),
                ConceptRelationships = ExtractRelationships(words),
                Sequence = words
            };
            
            return lesson.MainConcepts.Any() ? lesson : null;
        }

        private List<string> SplitIntoSentences(string text)
        {
            return _sentencePattern.Split(text)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Where(s => s.Length > 10) // Filter out very short fragments
                .ToList();
        }

        private List<string> ExtractWords(string sentence)
        {
            return _wordPattern.Matches(sentence)
                .Cast<Match>()
                .Select(m => m.Value.ToLower())
                .Where(word => !_stopWords.Contains(word) && word.Length > 2)
                .Distinct()
                .ToList();
        }

        private List<string> ExtractMainConcepts(List<string> words)
        {
            // Simple heuristic: longer words and nouns are likely main concepts
            return words
                .Where(word => word.Length >= 4) // Focus on substantial words
                .Where(word => !IsLikelyVerb(word)) // Filter out obvious verbs
                .Take(5) // Limit to top 5 concepts per sentence
                .ToList();
        }

        private List<ConceptRelationship> ExtractRelationships(List<string> words)
        {
            var relationships = new List<ConceptRelationship>();
            
            // Simple adjacency relationships
            for (int i = 0; i < words.Count - 1; i++)
            {
                var word1 = words[i];
                var word2 = words[i + 1];
                
                if (!_stopWords.Contains(word1) && !_stopWords.Contains(word2))
                {
                    relationships.Add(new ConceptRelationship
                    {
                        Concept1 = word1,
                        Concept2 = word2,
                        RelationType = "adjacent",
                        Strength = 0.6
                    });
                }
            }
            
            // Color-object relationships
            foreach (var word in words)
            {
                if (IsColor(word))
                {
                    foreach (var otherWord in words)
                    {
                        if (otherWord != word && !IsColor(otherWord) && !IsLikelyVerb(otherWord))
                        {
                            relationships.Add(new ConceptRelationship
                            {
                                Concept1 = word,
                                Concept2 = otherWord,
                                RelationType = "color-object",
                                Strength = 0.8
                            });
                        }
                    }
                }
            }
            
            return relationships;
        }

        private bool IsLikelyVerb(string word)
        {
            // Simple heuristic for verbs
            return word.EndsWith("ed") || word.EndsWith("ing") || 
                   word.EndsWith("s") && word.Length > 3 ||
                   CommonVerbs.Contains(word);
        }

        private bool IsColor(string word)
        {
            return ColorWords.Contains(word);
        }

        private static readonly HashSet<string> CommonVerbs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "run", "walk", "jump", "eat", "drink", "see", "look", "hear", "feel", "think",
            "know", "say", "tell", "ask", "go", "come", "take", "give", "make", "get",
            "put", "keep", "let", "help", "work", "play", "live", "love", "like", "want",
            "need", "try", "use", "find", "show", "move", "turn", "start", "stop", "open",
            "close", "read", "write", "draw", "sing", "dance", "sleep", "wake", "sit", "stand"
        };

        private static readonly HashSet<string> ColorWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "red", "blue", "green", "yellow", "orange", "purple", "pink", "brown", 
            "black", "white", "gray", "grey"
        };
    }

    /// <summary>
    /// A learning lesson extracted from text
    /// </summary>
    public class LearningLesson
    {
        public required string OriginalText { get; set; }
        public List<string> MainConcepts { get; set; } = new List<string>();
        public List<ConceptRelationship> ConceptRelationships { get; set; } = new List<ConceptRelationship>();
        public List<string> Sequence { get; set; } = new List<string>();
    }

    /// <summary>
    /// Relationship between two concepts
    /// </summary>
    public class ConceptRelationship
    {
        public required string Concept1 { get; set; }
        public required string Concept2 { get; set; }
        public required string RelationType { get; set; }
        public double Strength { get; set; }
    }
}
