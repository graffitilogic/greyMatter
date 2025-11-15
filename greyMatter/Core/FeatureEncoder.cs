using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GreyMatter.Core
{
    /// <summary>
    /// Phase 1: Simple deterministic feature encoder
    /// Converts raw text → fixed-dimensional feature vectors
    /// 
    /// Future upgrade path: Replace with frozen CLIP/DINO-style encoder
    /// </summary>
    public class FeatureEncoder
    {
        private readonly int _dimensions;
        
        public FeatureEncoder(int dimensions = 128)
        {
            _dimensions = dimensions;
        }

        /// <summary>
        /// Encode a word into a feature vector
        /// Deterministic: same word → same vector (for reproducibility)
        /// </summary>
        public double[] Encode(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return new double[_dimensions];

            var normalized = word.ToLowerInvariant().Trim();
            var features = new double[_dimensions];
            
            // Section 1: Orthographic features (0-31)
            EncodeOrthographic(normalized, features, offset: 0, count: 32);
            
            // Section 2: Character n-grams (32-63)
            EncodeCharNGrams(normalized, features, offset: 32, count: 32);
            
            // Section 3: Phonetic features (64-95)
            EncodePhonetic(normalized, features, offset: 64, count: 32);
            
            // Section 4: Statistical features (96-127)
            EncodeStatistical(normalized, features, offset: 96, count: 32);
            
            // Normalize to unit length (for cosine similarity)
            Normalize(features);
            
            return features;
        }

        /// <summary>
        /// Encode multiple words and average (simple compositional)
        /// </summary>
        public double[] EncodePhrase(string phrase)
        {
            var words = phrase.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
                return new double[_dimensions];

            var combined = new double[_dimensions];
            foreach (var word in words)
            {
                var wordFeatures = Encode(word);
                for (int i = 0; i < _dimensions; i++)
                {
                    combined[i] += wordFeatures[i];
                }
            }
            
            // Average
            for (int i = 0; i < _dimensions; i++)
            {
                combined[i] /= words.Length;
            }
            
            Normalize(combined);
            return combined;
        }

        private void EncodeOrthographic(string word, double[] features, int offset, int count)
        {
            // Length features (exponentially scaled)
            features[offset + 0] = Math.Tanh(word.Length / 10.0);
            
            // Character type ratios
            var vowels = word.Count(c => "aeiou".Contains(c));
            var consonants = word.Count(c => char.IsLetter(c) && !"aeiou".Contains(c));
            var digits = word.Count(char.IsDigit);
            var special = word.Count(c => !char.IsLetterOrDigit(c));
            
            features[offset + 1] = vowels / (double)Math.Max(1, word.Length);
            features[offset + 2] = consonants / (double)Math.Max(1, word.Length);
            features[offset + 3] = digits / (double)Math.Max(1, word.Length);
            features[offset + 4] = special / (double)Math.Max(1, word.Length);
            
            // Capitalization pattern
            features[offset + 5] = char.IsUpper(word[0]) ? 1.0 : 0.0;
            features[offset + 6] = word.All(char.IsUpper) ? 1.0 : 0.0;
            features[offset + 7] = word.Any(char.IsUpper) && !word.All(char.IsUpper) ? 1.0 : 0.0;
            
            // Repeating characters
            var maxRepeat = 1;
            var currentRepeat = 1;
            for (int i = 1; i < word.Length; i++)
            {
                if (word[i] == word[i - 1])
                {
                    currentRepeat++;
                    maxRepeat = Math.Max(maxRepeat, currentRepeat);
                }
                else
                {
                    currentRepeat = 1;
                }
            }
            features[offset + 8] = Math.Tanh(maxRepeat / 3.0);
            
            // Remaining slots: deterministic hash spread
            for (int i = 9; i < count; i++)
            {
                var hash = StableHash(word + "_orth_" + i);
                features[offset + i] = (hash % 1000) / 1000.0 - 0.5; // Range: [-0.5, 0.5]
            }
        }

        private void EncodeCharNGrams(string word, double[] features, int offset, int count)
        {
            // Character bigrams and trigrams
            var bigrams = new HashSet<string>();
            var trigrams = new HashSet<string>();
            
            for (int i = 0; i < word.Length - 1; i++)
            {
                bigrams.Add(word.Substring(i, 2));
                if (i < word.Length - 2)
                    trigrams.Add(word.Substring(i, 3));
            }
            
            // Hash n-grams into feature space
            foreach (var bigram in bigrams.Take(count / 2))
            {
                var hash = StableHash(bigram);
                var idx = offset + (hash % (count / 2));
                features[idx] += 0.5; // Accumulate (multiple n-grams may hash to same slot)
            }
            
            foreach (var trigram in trigrams.Take(count / 2))
            {
                var hash = StableHash(trigram);
                var idx = offset + count / 2 + (hash % (count / 2));
                features[idx] += 0.5;
            }
        }

        private void EncodePhonetic(string word, double[] features, int offset, int count)
        {
            // Simple phonetic features (extensible for metaphone/soundex later)
            
            // Syllable estimate (rough heuristic)
            var syllables = EstimateSyllables(word);
            features[offset + 0] = Math.Tanh(syllables / 4.0);
            
            // Starting/ending sounds
            if (word.Length > 0)
            {
                var first = word[0].ToString().ToLower();
                var last = word[^1].ToString().ToLower();
                
                // Consonant clusters
                var startsWithCluster = word.Length > 1 && 
                    !"aeiou".Contains(word[0]) && !"aeiou".Contains(word[1]);
                var endsWithCluster = word.Length > 1 && 
                    !"aeiou".Contains(word[^1]) && !"aeiou".Contains(word[^2]);
                
                features[offset + 1] = startsWithCluster ? 1.0 : 0.0;
                features[offset + 2] = endsWithCluster ? 1.0 : 0.0;
                
                // Hash first/last sounds
                var hashFirst = StableHash("first_" + first);
                var hashLast = StableHash("last_" + last);
                features[offset + 3] = (hashFirst % 1000) / 1000.0 - 0.5;
                features[offset + 4] = (hashLast % 1000) / 1000.0 - 0.5;
            }
            
            // Remaining: phonetic hash spread
            for (int i = 5; i < count; i++)
            {
                var hash = StableHash(word + "_phon_" + i);
                features[offset + i] = (hash % 1000) / 1000.0 - 0.5;
            }
        }

        private void EncodeStatistical(string word, double[] features, int offset, int count)
        {
            // Frequency estimate (from observations or corpus stats)
            var freq = GetFrequencyEstimate(word);
            features[offset + 0] = Math.Tanh(Math.Log(freq + 1) / 10.0);
            
            // Zipf distribution estimate (rank-frequency)
            var rank = EstimateRank(word);
            features[offset + 1] = Math.Tanh(Math.Log(rank + 1) / 10.0);
            
            // Word shape patterns
            var shape = GetWordShape(word);
            var shapeHash = StableHash(shape);
            features[offset + 2] = (shapeHash % 1000) / 1000.0 - 0.5;
            
            // Remaining: statistical hash spread
            for (int i = 3; i < count; i++)
            {
                var hash = StableHash(word + "_stat_" + i);
                features[offset + i] = (hash % 1000) / 1000.0 - 0.5;
            }
        }

        private int EstimateSyllables(string word)
        {
            // Rough syllable count (vowel groups)
            var count = 0;
            var inVowelGroup = false;
            
            foreach (var c in word.ToLower())
            {
                var isVowel = "aeiouy".Contains(c);
                if (isVowel && !inVowelGroup)
                {
                    count++;
                    inVowelGroup = true;
                }
                else if (!isVowel)
                {
                    inVowelGroup = false;
                }
            }
            
            // Adjust for silent 'e'
            if (word.EndsWith("e") && count > 1)
                count--;
            
            return Math.Max(1, count);
        }

        private double GetFrequencyEstimate(string word)
        {
            // Deterministic frequency estimate based on word characteristics
            // (not tracking actual frequencies - that would make encoding non-deterministic)
            
            // Simple heuristic: shorter common words are more frequent
            var lengthScore = Math.Max(0, 10 - word.Length) / 10.0;
            
            // Common letter patterns boost frequency estimate
            var commonPatterns = new[] { "the", "ing", "er", "ed", "ly", "s" };
            var patternScore = commonPatterns.Count(p => word.Contains(p)) * 0.1;
            
            // Combine heuristics
            return Math.Max(1.0, lengthScore * 10 + patternScore * 5);
        }

        private int EstimateRank(string word)
        {
            // Deterministic rank estimate (not using actual frequency tracking)
            // Estimate based on word characteristics
            
            var freq = GetFrequencyEstimate(word);
            
            // Simple mapping: higher frequency → lower rank
            // Common words (freq > 5) → rank 1-1000
            // Uncommon words (freq 1-5) → rank 1000-10000
            // Rare words (freq < 1) → rank > 10000
            
            if (freq > 5.0)
                return (int)(1000 * (10.0 / freq));
            else if (freq > 1.0)
                return (int)(1000 + 9000 * (5.0 - freq) / 4.0);
            else
                return 10000 + (int)(1000 * (1.0 / Math.Max(0.1, freq)));
        }

        private string GetWordShape(string word)
        {
            // Abstract word shape: "Cat" → "Xxx", "HELLO" → "XXXXX", "test123" → "xxxx999"
            var sb = new StringBuilder();
            foreach (var c in word)
            {
                if (char.IsUpper(c)) sb.Append('X');
                else if (char.IsLower(c)) sb.Append('x');
                else if (char.IsDigit(c)) sb.Append('9');
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private void Normalize(double[] vector)
        {
            // L2 normalization for cosine similarity
            var magnitude = Math.Sqrt(vector.Sum(x => x * x));
            if (magnitude > 0)
            {
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] /= magnitude;
                }
            }
        }

        private int StableHash(string input)
        {
            // Deterministic hash (same as Cerebro's)
            unchecked
            {
                int hash = 17;
                foreach (char c in input)
                {
                    hash = hash * 31 + c;
                }
                return Math.Abs(hash);
            }
        }
    }
}
