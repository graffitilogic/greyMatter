using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace greyMatter.Core
{
    /// <summary>
    /// Manages vocabulary learning and word relationships
    /// </summary>
    public class VocabularyNetwork
    {
        private readonly HashSet<string> _knownWords;
        private readonly Dictionary<string, WordInfo> _wordDetails;

        public int WordCount => _knownWords.Count;

        public VocabularyNetwork()
        {
            _knownWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _wordDetails = new Dictionary<string, WordInfo>(StringComparer.OrdinalIgnoreCase);
        }

        public void AddWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return;
            
            word = CleanWord(word);
            if (string.IsNullOrEmpty(word)) return;

            _knownWords.Add(word);
            
            if (!_wordDetails.ContainsKey(word))
            {
                _wordDetails[word] = new WordInfo
                {
                    Word = word,
                    FirstSeen = DateTime.Now,
                    Frequency = 0
                };
            }
            
            _wordDetails[word].Frequency++;
        }

        public bool IsKnownWord(string word) => 
            !string.IsNullOrWhiteSpace(word) && _knownWords.Contains(CleanWord(word));

        public WordInfo? GetWordInfo(string word)
        {
            word = CleanWord(word);
            return _wordDetails.GetValueOrDefault(word);
        }

        public List<string> GetWordsStartingWith(string prefix, int maxResults = 10)
        {
            prefix = prefix.ToLower();
            return _knownWords
                .Where(w => w.ToLower().StartsWith(prefix))
                .Take(maxResults)
                .ToList();
        }

        private string CleanWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return string.Empty;
            
            // Remove punctuation and normalize
            return Regex.Replace(word.Trim().ToLower(), @"[^\w]", "");
        }
    }

    /// <summary>
    /// Information about a learned word
    /// </summary>
    public class WordInfo
    {
        public string Word { get; set; } = string.Empty;
        public int Frequency { get; set; }
        public DateTime FirstSeen { get; set; }
        public WordType EstimatedType { get; set; } = WordType.Unknown;
    }

    public enum WordType
    {
        Unknown,
        Noun,
        Verb,
        Adjective,
        Adverb,
        Preposition,
        Article,
        Pronoun
    }

    /// <summary>
    /// Analyzes sentence structure to extract subjects, verbs, objects, and relationships
    /// </summary>
    public class SentenceStructureAnalyzer
    {
        private readonly HashSet<string> _commonVerbs;
        private readonly HashSet<string> _commonNouns;
        private readonly HashSet<string> _stopWords;
        private readonly Regex _wordPattern;

        public SentenceStructureAnalyzer()
        {
            _commonVerbs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "is", "are", "was", "were", "be", "being", "been",
                "have", "has", "had", "do", "does", "did",
                "go", "goes", "went", "come", "comes", "came",
                "see", "sees", "saw", "look", "looks", "looked",
                "run", "runs", "ran", "walk", "walks", "walked",
                "eat", "eats", "ate", "drink", "drinks", "drank",
                "sleep", "sleeps", "slept", "play", "plays", "played",
                "like", "likes", "liked", "love", "loves", "loved",
                "want", "wants", "wanted", "need", "needs", "needed",
                "make", "makes", "made", "take", "takes", "took",
                "give", "gives", "gave", "get", "gets", "got",
                "put", "puts", "put", "find", "finds", "found"
            };

            _commonNouns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "cat", "dog", "bird", "fish", "animal", "pet",
                "apple", "ball", "book", "car", "tree", "house",
                "boy", "girl", "man", "woman", "child", "person",
                "water", "food", "milk", "bread", "fruit",
                "sun", "moon", "star", "sky", "cloud", "rain",
                "school", "home", "park", "store", "garden",
                "bed", "chair", "table", "door", "window"
            };

            _stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
                "of", "with", "by", "this", "that", "these", "those",
                "i", "you", "he", "she", "it", "we", "they", "me", "him", "her", "us", "them"
            };

            _wordPattern = new Regex(@"\b\w+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public SentenceStructure? AnalyzeSentence(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence)) return null;

            var words = ExtractWords(sentence);
            if (words.Count < 2) return null;

            var structure = new SentenceStructure
            {
                OriginalSentence = sentence.Trim(),
                Words = words,
                CleanWords = words.Where(w => !_stopWords.Contains(w)).ToList()
            };

            // Simple pattern recognition for subject-verb-object
            IdentifySubjectVerbObject(structure);
            
            return structure;
        }

        private List<string> ExtractWords(string sentence)
        {
            return _wordPattern.Matches(sentence)
                .Cast<Match>()
                .Select(m => m.Value.ToLower())
                .Where(word => word.Length > 1)
                .ToList();
        }

        private void IdentifySubjectVerbObject(SentenceStructure structure)
        {
            var words = structure.Words;
            
            // Find the main verb
            var verbIndex = -1;
            for (int i = 0; i < words.Count; i++)
            {
                if (_commonVerbs.Contains(words[i]))
                {
                    structure.Verb = words[i];
                    verbIndex = i;
                    break;
                }
            }

            if (verbIndex == -1) return; // No verb found

            // Look for subject before the verb (skip articles and stop words)
            for (int i = verbIndex - 1; i >= 0; i--)
            {
                if (!_stopWords.Contains(words[i]))
                {
                    structure.Subject = words[i];
                    break;
                }
            }

            // Look for object after the verb (skip prepositions and stop words)
            for (int i = verbIndex + 1; i < words.Count; i++)
            {
                if (!_stopWords.Contains(words[i]) && !IsPreposition(words[i]))
                {
                    structure.Object = words[i];
                    break;
                }
            }

            // Extract attributes (adjectives before nouns)
            structure.Attributes = ExtractAttributes(words);
        }

        private List<string> ExtractAttributes(List<string> words)
        {
            var attributes = new List<string>();
            
            for (int i = 0; i < words.Count - 1; i++)
            {
                var currentWord = words[i];
                var nextWord = words[i + 1];
                
                // Simple heuristic: if current word is not a common noun/verb/stop word
                // and next word is a noun, current word might be an adjective
                if (!_commonVerbs.Contains(currentWord) && 
                    !_commonNouns.Contains(currentWord) && 
                    !_stopWords.Contains(currentWord) &&
                    (_commonNouns.Contains(nextWord) || !_stopWords.Contains(nextWord)))
                {
                    attributes.Add(currentWord);
                }
            }
            
            return attributes;
        }

        private bool IsPreposition(string word)
        {
            var prepositions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "in", "on", "at", "to", "for", "of", "with", "by", "from", "up", "down", "over", "under"
            };
            return prepositions.Contains(word);
        }
    }

    /// <summary>
    /// Represents the analyzed structure of a sentence
    /// </summary>
    public class SentenceStructure
    {
        public string OriginalSentence { get; set; } = string.Empty;
        public List<string> Words { get; set; } = new();
        public List<string> CleanWords { get; set; } = new();
        public string Subject { get; set; } = string.Empty;
        public string Verb { get; set; } = string.Empty;
        public string Object { get; set; } = string.Empty;
        public List<string> Attributes { get; set; } = new();
        
        public bool HasCompleteStructure => 
            !string.IsNullOrEmpty(Subject) && !string.IsNullOrEmpty(Verb);
            
        public override string ToString()
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Subject)) parts.Add($"Subject: {Subject}");
            if (!string.IsNullOrEmpty(Verb)) parts.Add($"Verb: {Verb}");
            if (!string.IsNullOrEmpty(Object)) parts.Add($"Object: {Object}");
            if (Attributes.Any()) parts.Add($"Attributes: {string.Join(", ", Attributes)}");
            
            return string.Join(" | ", parts);
        }
    }
}
