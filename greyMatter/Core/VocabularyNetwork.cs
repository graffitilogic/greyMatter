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
        
        // Biological storage extensions for shared neuron architecture
        public int? ConceptNeuronId { get; set; }
        public int? AttentionNeuronId { get; set; }
        public List<int> AssociatedNeuronIds { get; set; } = new();
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
                // Base forms
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
                "put", "puts", "put", "find", "finds", "found",
                "sit", "sits", "sat", "stand", "stands", "stood",
                "work", "works", "worked", "live", "lives", "lived",
                "think", "thinks", "thought", "know", "knows", "knew",
                "say", "says", "said", "tell", "tells", "told",
                "ask", "asks", "asked", "help", "helps", "helped",
                "feel", "feels", "felt", "hear", "hears", "heard",
                "read", "reads", "read", "write", "writes", "wrote",
                "speak", "speaks", "spoke", "talk", "talks", "talked",
                "use", "uses", "used", "move", "moves", "moved",
                "start", "starts", "started", "stop", "stops", "stopped",
                "open", "opens", "opened", "close", "closes", "closed",
                "call", "calls", "called", "wait", "waits", "waited",
                "try", "tries", "tried", "turn", "turns", "turned",
                "follow", "follows", "followed", "watch", "watches", "watched",
                "show", "shows", "showed", "carry", "carries", "carried",
                "bring", "brings", "brought", "buy", "buys", "bought",
                "build", "builds", "built", "break", "breaks", "broke",
                "catch", "catches", "caught", "choose", "chooses", "chose",
                "come", "comes", "came", "cut", "cuts", "cut",
                "draw", "draws", "drew", "drive", "drives", "drove",
                "fall", "falls", "fell", "fight", "fights", "fought",
                "fly", "flies", "flew", "forget", "forgets", "forgot",
                "grow", "grows", "grew", "hang", "hangs", "hung",
                "hold", "holds", "held", "keep", "keeps", "kept",
                "learn", "learns", "learned", "leave", "leaves", "left",
                "lose", "loses", "lost", "meet", "meets", "met",
                "pay", "pays", "paid", "ride", "rides", "rode",
                "rise", "rises", "rose", "sell", "sells", "sold",
                "send", "sends", "sent", "set", "sets", "set",
                "shake", "shakes", "shook", "shine", "shines", "shone",
                "shoot", "shoots", "shot", "sing", "sings", "sang",
                "sink", "sinks", "sank", "sit", "sits", "sat",
                "sleep", "sleeps", "slept", "speak", "speaks", "spoke",
                "spend", "spends", "spent", "stand", "stands", "stood",
                "steal", "steals", "stole", "stick", "sticks", "stuck",
                "strike", "strikes", "struck", "swim", "swims", "swam",
                "swing", "swings", "swung", "take", "takes", "took",
                "teach", "teaches", "taught", "tear", "tears", "tore",
                "tell", "tells", "told", "think", "thinks", "thought",
                "throw", "throws", "threw", "understand", "understands", "understood",
                "wake", "wakes", "woke", "wear", "wears", "wore",
                "win", "wins", "won", "write", "writes", "wrote", "chase", "chases", "chased",
                "study", "studies", "studied", "eat", "eats", "ate", "drink", "drinks", "drank"
            };

            _commonNouns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "cat", "dog", "bird", "fish", "animal", "pet",
                "apple", "ball", "book", "car", "tree", "house",
                "boy", "girl", "man", "woman", "child", "person",
                "water", "food", "milk", "bread", "fruit",
                "sun", "moon", "star", "sky", "cloud", "rain",
                "school", "home", "park", "store", "garden",
                "bed", "chair", "table", "door", "window",
                "mat", "park", "computer", "science", "time", "day",
                "night", "morning", "evening", "week", "month", "year",
                "friend", "family", "job", "work", "money", "game",
                "music", "song", "movie", "book", "story", "life",
                "world", "country", "city", "street", "room", "kitchen",
                "bathroom", "garden", "office", "company", "business",
                "student", "teacher", "doctor", "nurse", "engineer",
                "programmer", "artist", "writer", "scientist", "researcher",
                "university", "college", "classroom", "library", "laboratory",
                "restaurant", "cafe", "hotel", "airport", "station", "train",
                "bus", "taxi", "bicycle", "motorcycle", "truck", "ship", "plane",
                "mountain", "river", "lake", "ocean", "forest", "desert", "beach",
                "building", "bridge", "road", "highway", "village", "town"
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
            
            // Find all potential verbs in the sentence
            var verbIndices = new List<int>();
            for (int i = 0; i < words.Count; i++)
            {
                if (_commonVerbs.Contains(words[i]))
                {
                    verbIndices.Add(i);
                }
            }

            if (verbIndices.Count == 0) return; // No verb found

            // Use the first verb as the main verb (can be improved later)
            var verbIndex = verbIndices[0];
            structure.Verb = words[verbIndex];

            // Enhanced subject detection with better pronoun handling
            structure.Subject = FindSubject(words, verbIndex);
            
            // Enhanced object detection with better prepositional phrase handling
            structure.Object = FindObject(words, verbIndex);

            // Extract attributes (adjectives before nouns)
            structure.Attributes = ExtractAttributes(words);
        }

        private string FindSubject(List<string> words, int verbIndex)
        {
            // First, check if the sentence starts with a pronoun or proper noun
            if (verbIndex > 0)
            {
                var firstWord = words[0];
                if (IsPronoun(firstWord) || char.IsUpper(firstWord[0]) || _commonNouns.Contains(firstWord))
                {
                    return firstWord;
                }
            }

            // Look backwards from verb for subject
            for (int i = verbIndex - 1; i >= 0; i--)
            {
                var word = words[i];
                if (!_stopWords.Contains(word) && !IsPreposition(word))
                {
                    // Check if it's a common noun or could be a proper noun
                    if (_commonNouns.Contains(word) || 
                        char.IsUpper(word[0]) || 
                        IsLikelyNoun(word))
                    {
                        return word;
                    }
                    // Special handling for pronouns
                    else if (IsPronoun(word))
                    {
                        return word;
                    }
                    // If it's not a known noun but not a stop word/preposition, it might still be a subject
                    else if (!_commonVerbs.Contains(word))
                    {
                        return word;
                    }
                }
            }

            return string.Empty;
        }

        private string FindObject(List<string> words, int verbIndex)
        {
            var objectCandidates = new List<string>();
            var inPrepositionalPhrase = false;
            var directObjectFound = false;
            var directObject = string.Empty;
            
            for (int i = verbIndex + 1; i < words.Count; i++)
            {
                var word = words[i];
                
                if (IsPreposition(word))
                {
                    inPrepositionalPhrase = true;
                    continue;
                }
                
                if (!_stopWords.Contains(word))
                {
                    if (!inPrepositionalPhrase && !directObjectFound)
                    {
                        // Direct object - prefer nouns, but also handle gerunds and complex objects
                        if (_commonNouns.Contains(word) || IsLikelyNoun(word))
                        {
                            directObject = word;
                            directObjectFound = true;
                        }
                        // Handle gerunds (words ending in "ing" that could be objects)
                        else if (word.EndsWith("ing") && word.Length > 3 && !_commonVerbs.Contains(word))
                        {
                            directObject = word;
                            directObjectFound = true;
                        }
                        // Handle compound nouns (e.g., "computer science")
                        else if (string.IsNullOrEmpty(directObject) && !_commonVerbs.Contains(word) && word.Length > 2)
                        {
                            // Check if next word is also a potential noun
                            if (i + 1 < words.Count && !_stopWords.Contains(words[i + 1]) && 
                                !_commonVerbs.Contains(words[i + 1]) && !IsPreposition(words[i + 1]))
                            {
                                directObject = $"{word} {words[i + 1]}";
                                directObjectFound = true;
                                i++; // Skip next word as it's part of the compound
                            }
                            else
                            {
                                directObject = word;
                                directObjectFound = true;
                            }
                        }
                    }
                    else if (inPrepositionalPhrase)
                    {
                        // Object of preposition - collect candidates but don't use as direct object
                        if (_commonNouns.Contains(word) || IsLikelyNoun(word))
                        {
                            objectCandidates.Add(word);
                        }
                    }
                }
                else if (inPrepositionalPhrase && word == "the")
                {
                    // Skip "the" in prepositional phrases but continue looking
                    continue;
                }
            }

            // Return direct object if found, otherwise empty string
            return directObject;
        }

        private List<string> ExtractAttributes(List<string> words)
        {
            var attributes = new List<string>();
            var commonAdjectives = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "big", "small", "large", "little", "good", "bad", "hot", "cold",
                "fast", "slow", "easy", "hard", "new", "old", "young", "happy",
                "sad", "red", "blue", "green", "yellow", "black", "white", "brown",
                "nice", "beautiful", "ugly", "smart", "stupid", "rich", "poor",
                "clean", "dirty", "wet", "dry", "full", "empty", "open", "closed",
                "high", "low", "long", "short", "wide", "narrow", "thick", "thin",
                "heavy", "light", "strong", "weak", "soft", "hard", "smooth", "rough",
                "sweet", "sour", "bitter", "salty", "spicy", "delicious", "tasty"
            };
            
            for (int i = 0; i < words.Count - 1; i++)
            {
                var currentWord = words[i];
                var nextWord = words[i + 1];
                
                // Skip if current word is a stop word, preposition, or verb
                if (_stopWords.Contains(currentWord) || IsPreposition(currentWord) || _commonVerbs.Contains(currentWord))
                    continue;
                
                // Check if current word is a known adjective
                if (commonAdjectives.Contains(currentWord))
                {
                    attributes.Add(currentWord);
                    continue;
                }
                
                // Check if next word is a noun (or could be a noun)
                if (_commonNouns.Contains(nextWord) || 
                    (!_stopWords.Contains(nextWord) && !_commonVerbs.Contains(nextWord) && !IsPreposition(nextWord)))
                {
                    // Additional adjective detection heuristics
                    if (currentWord.Length > 2 && 
                        (currentWord.EndsWith("ing") || currentWord.EndsWith("ed") || 
                         currentWord.EndsWith("ful") || currentWord.EndsWith("ous") ||
                         currentWord.EndsWith("able") || currentWord.EndsWith("ible") ||
                         currentWord.EndsWith("al") || currentWord.EndsWith("ic") ||
                         currentWord.EndsWith("y") || currentWord.EndsWith("ly") ||
                         currentWord.EndsWith("ish") || currentWord.EndsWith("ive") ||
                         currentWord.EndsWith("ent") || currentWord.EndsWith("ant")))
                    {
                        attributes.Add(currentWord);
                    }
                    // Short words that could be adjectives
                    else if (currentWord.Length >= 3 && currentWord.Length <= 6 && 
                             !currentWord.EndsWith("s") && !IsLikelyNoun(currentWord))
                    {
                        attributes.Add(currentWord);
                    }
                }
            }
            
            return attributes;
        }

        private bool IsPreposition(string word)
        {
            var prepositions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "in", "on", "at", "to", "for", "of", "with", "by", "from", "up", "down", "over", "under",
                "above", "below", "behind", "beside", "between", "into", "onto", "upon", "within",
                "without", "through", "during", "before", "after", "about", "against", "along",
                "among", "around", "across", "besides", "beyond", "concerning", "considering",
                "despite", "except", "inside", "outside", "regarding", "respecting", "throughout",
                "toward", "towards", "underneath", "unlike", "until", "unto"
            };
            return prepositions.Contains(word);
        }

        private bool IsLikelyNoun(string word)
        {
            if (string.IsNullOrEmpty(word) || word.Length < 2) return false;
            
            // Common noun patterns: ends with common noun suffixes
            var nounSuffixes = new[] { "tion", "ment", "ness", "ity", "er", "or", "ist", "ship", "age", "ing" };
            if (nounSuffixes.Any(suffix => word.EndsWith(suffix))) return true;
            
            // Likely proper nouns (capitalized)
            if (char.IsUpper(word[0])) return true;
            
            // Words that are longer and not verbs are likely nouns
            if (word.Length > 4 && !_commonVerbs.Contains(word)) return true;
            
            return false;
        }

        private bool IsPronoun(string word)
        {
            var pronouns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "i", "you", "he", "she", "it", "we", "they", "me", "him", "her", "us", "them",
                "my", "your", "his", "its", "our", "their", "this", "that", "these", "those",
                "who", "what", "where", "when", "why", "how", "which", "whose"
            };
            return pronouns.Contains(word);
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
