using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace GreyMatter.Core
{
    /// <summary>
    /// Biologically-inspired sparse concept encoding system
    /// Uses distributed representations across multiple cortical columns
    /// Optimized for massive scale while maintaining rich semantic relationships
    /// </summary>
    public class SparseConceptEncoder
    {
        private readonly Dictionary<string, SparsePattern> _conceptPatterns;
        private readonly Dictionary<string, ConceptColumn> _corticalColumns;
        private readonly Random _random;
        private readonly int _patternSize;
        private readonly double _sparsity; // Percentage of active neurons (typically 2-5%)

        public SparseConceptEncoder(int patternSize = 2048, double sparsity = 0.02)
        {
            _conceptPatterns = new Dictionary<string, SparsePattern>();
            _corticalColumns = new Dictionary<string, ConceptColumn>();
            _random = new Random(42); // Reproducible for consistency
            _patternSize = patternSize;
            _sparsity = sparsity;
            
            InitializeCorticalColumns();
        }

        /// <summary>
        /// Initialize specialized cortical columns for different concept types
        /// </summary>
        private void InitializeCorticalColumns()
        {
            _corticalColumns["phonetic"] = new ConceptColumn("phonetic", _patternSize, _sparsity);
            _corticalColumns["semantic"] = new ConceptColumn("semantic", _patternSize, _sparsity);
            _corticalColumns["syntactic"] = new ConceptColumn("syntactic", _patternSize, _sparsity);
            _corticalColumns["contextual"] = new ConceptColumn("contextual", _patternSize, _sparsity);
        }

        /// <summary>
        /// Encode a word using sparse distributed representation
        /// Multiple encodings for the same word create overlapping but distinct patterns
        /// </summary>
        public SparsePattern EncodeWord(string word, string context = "")
        {
            var normalizedWord = word.ToLower().Trim();
            var patternKey = $"{normalizedWord}:{context}";

            // Check if we have an existing pattern for this word+context
            if (_conceptPatterns.TryGetValue(patternKey, out var existingPattern))
            {
                // Return existing pattern but with slight variation (biological noise)
                return AddBiologicalVariation(existingPattern);
            }

            // Create new sparse pattern using multiple cortical columns
            var combinedPattern = CreateMultiColumnPattern(normalizedWord, context);
            _conceptPatterns[patternKey] = combinedPattern;

            return combinedPattern;
        }

        /// <summary>
        /// Create pattern using multiple specialized cortical columns
        /// </summary>
        private SparsePattern CreateMultiColumnPattern(string word, string context)
        {
            var phoneticPattern = _corticalColumns["phonetic"].EncodeFeatures(ExtractPhoneticFeatures(word));
            var semanticPattern = _corticalColumns["semantic"].EncodeFeatures(ExtractSemanticFeatures(word));
            var syntacticPattern = _corticalColumns["syntactic"].EncodeFeatures(ExtractSyntacticFeatures(word));
            var contextualPattern = _corticalColumns["contextual"].EncodeFeatures(ExtractContextualFeatures(context));

            // Combine patterns with column-specific weights
            return CombineColumnPatterns(phoneticPattern, semanticPattern, syntacticPattern, contextualPattern);
        }

        /// <summary>
        /// Extract phonetic features from word (sound patterns, syllables)
        /// </summary>
        private List<string> ExtractPhoneticFeatures(string word)
        {
            var features = new List<string>();
            
            // Word-specific features for maximum distinctiveness
            features.Add($"word:{word}"); // Every word gets unique phonetic signature
            
            // Basic phonetic features
            features.Add($"length:{word.Length}");
            features.Add($"starts:{word[0]}");
            if (word.Length > 1) features.Add($"ends:{word[^1]}");
            
            // Character-level features
            for (int i = 0; i < Math.Min(word.Length, 3); i++)
            {
                features.Add($"char_{i}:{word[i]}");
            }
            
            // Vowel patterns
            var vowels = word.Where(c => "aeiou".Contains(c)).ToArray();
            if (vowels.Any()) features.Add($"vowel_pattern:{string.Join("", vowels)}");
            
            // Consonant clusters
            var consonants = word.Where(c => !"aeiou".Contains(c)).ToArray();
            if (consonants.Any()) features.Add($"consonant_pattern:{string.Join("", consonants)}");

            return features;
        }

        /// <summary>
        /// Extract semantic features (meaning-based patterns)
        /// </summary>
        private List<string> ExtractSemanticFeatures(string word)
        {
            var features = new List<string>();
            
            // Simple semantic categorization based on common patterns
            if (IsNoun(word)) features.Add("pos:noun");
            if (IsVerb(word)) features.Add("pos:verb");
            if (IsAdjective(word)) features.Add("pos:adjective");
            
            // Semantic domains (simplified)
            if (IsAnimal(word)) features.Add("domain:animal");
            if (IsColor(word)) features.Add("domain:color");
            if (IsAction(word)) features.Add("domain:action");
            if (IsObject(word)) features.Add("domain:object");
            if (IsFinance(word)) features.Add("domain:finance");
            if (IsNature(word)) features.Add("domain:nature");
            
            // Add word-specific semantic features for distinctiveness
            features.Add($"semantic_root:{word}");
            
            return features;
        }

        /// <summary>
        /// Extract syntactic features (grammatical patterns)
        /// </summary>
        private List<string> ExtractSyntacticFeatures(string word)
        {
            var features = new List<string>();
            
            // Common suffixes and prefixes
            var commonSuffixes = new[] { "ing", "ed", "er", "est", "ly", "tion", "ness" };
            var commonPrefixes = new[] { "un", "re", "pre", "dis", "over", "under" };
            
            foreach (var suffix in commonSuffixes)
            {
                if (word.EndsWith(suffix))
                    features.Add($"suffix:{suffix}");
            }
            
            foreach (var prefix in commonPrefixes)
            {
                if (word.StartsWith(prefix))
                    features.Add($"prefix:{prefix}");
            }
            
            return features;
        }

        /// <summary>
        /// Extract contextual features from surrounding context
        /// </summary>
        private List<string> ExtractContextualFeatures(string context)
        {
            var features = new List<string>();
            
            // Context-specific signature
            if (!string.IsNullOrEmpty(context))
            {
                features.Add($"context_signature:{context.GetHashCode()}");
                
                var words = context.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words.Take(3)) // Context window
                {
                    features.Add($"context:{word.ToLower()}");
                }
                
                // Context type detection
                if (context.Contains("animal") || context.Contains("pet")) features.Add("context_type:animal");
                if (context.Contains("vehicle") || context.Contains("transport")) features.Add("context_type:vehicle");
                if (context.Contains("financial") || context.Contains("money")) features.Add("context_type:finance");
                if (context.Contains("river") || context.Contains("water")) features.Add("context_type:nature");
                if (context.Contains("fruit") || context.Contains("food")) features.Add("context_type:food");
                if (context.Contains("tech") || context.Contains("computer")) features.Add("context_type:technology");
            }
            else
            {
                features.Add("context:none");
            }
            
            return features;
        }

        /// <summary>
        /// Combine patterns from different cortical columns
        /// </summary>
        private SparsePattern CombineColumnPatterns(params SparsePattern[] patterns)
        {
            var bitWeights = new Dictionary<int, double>();
            var totalWeight = 0.0;

            // Accumulate weighted bits from each column
            foreach (var pattern in patterns)
            {
                var columnWeight = 1.0 / patterns.Length; // Equal weight to each column
                foreach (var bit in pattern.ActiveBits)
                {
                    if (bitWeights.ContainsKey(bit))
                        bitWeights[bit] += columnWeight;
                    else
                        bitWeights[bit] = columnWeight;
                }
                totalWeight += pattern.ActivationStrength;
            }

            // Select top-weighted bits to maintain sparsity
            var targetActiveBits = (int)(_patternSize * _sparsity);
            var finalBits = bitWeights
                .OrderByDescending(kvp => kvp.Value)  // Sort by weight
                .ThenBy(kvp => kvp.Key)               // Stable sort for reproducibility
                .Take(targetActiveBits)
                .Select(kvp => kvp.Key)
                .ToArray();

            return new SparsePattern(finalBits, totalWeight / patterns.Length);
        }

        /// <summary>
        /// Add biological variation to existing pattern (cortical noise)
        /// </summary>
        private SparsePattern AddBiologicalVariation(SparsePattern basePattern)
        {
            var variationRate = 0.1; // 10% of bits can vary
            var newBits = new List<int>(basePattern.ActiveBits);
            
            // Remove some bits (neural fatigue)
            var bitsToRemove = (int)(newBits.Count * variationRate * 0.5);
            for (int i = 0; i < bitsToRemove; i++)
            {
                if (newBits.Count > 1)
                    newBits.RemoveAt(_random.Next(newBits.Count));
            }
            
            // Add some new bits (neural plasticity)
            var bitsToAdd = (int)(newBits.Count * variationRate * 0.5);
            for (int i = 0; i < bitsToAdd; i++)
            {
                var newBit = _random.Next(_patternSize);
                if (!newBits.Contains(newBit))
                    newBits.Add(newBit);
            }
            
            return new SparsePattern(newBits.ToArray(), basePattern.ActivationStrength * 0.95);
        }

        /// <summary>
        /// Calculate similarity between two sparse patterns
        /// </summary>
        public double CalculateSimilarity(SparsePattern pattern1, SparsePattern pattern2)
        {
            var intersection = pattern1.ActiveBits.Intersect(pattern2.ActiveBits).Count();
            var union = pattern1.ActiveBits.Union(pattern2.ActiveBits).Count();
            
            return union > 0 ? (double)intersection / union : 0.0;
        }

        // Simple heuristics for semantic categorization
        private bool IsNoun(string word) => !word.EndsWith("ing") && !word.EndsWith("ly");
        private bool IsVerb(string word) => word.EndsWith("ing") || word.EndsWith("ed") || 
            new[] { "run", "walk", "eat", "sleep", "think", "learn" }.Contains(word);
        private bool IsAdjective(string word) => word.EndsWith("ly") || 
            new[] { "big", "small", "red", "blue", "happy", "sad" }.Contains(word);
        private bool IsAnimal(string word) => 
            new[] { "cat", "dog", "bird", "fish", "lion", "tiger" }.Contains(word);
        private bool IsColor(string word) => 
            new[] { "red", "blue", "green", "yellow", "black", "white" }.Contains(word);
        private bool IsAction(string word) => 
            new[] { "run", "walk", "jump", "sit", "stand", "eat" }.Contains(word);
        private bool IsObject(string word) => 
            new[] { "car", "table", "chair", "book", "phone", "computer" }.Contains(word);
        private bool IsFinance(string word) => 
            new[] { "bank", "money", "loan", "credit", "deposit", "account" }.Contains(word);
        private bool IsNature(string word) => 
            new[] { "river", "tree", "mountain", "ocean", "forest", "bank" }.Contains(word);

        public int ConceptCount => _conceptPatterns.Count;
        public double AverageSparsity => _conceptPatterns.Values.Average(p => (double)p.ActiveBits.Length / _patternSize);
    }

    /// <summary>
    /// Sparse pattern representation using active bit indices
    /// </summary>
    public class SparsePattern
    {
        public int[] ActiveBits { get; }
        public double ActivationStrength { get; }

        public SparsePattern(int[] activeBits, double activationStrength)
        {
            ActiveBits = activeBits ?? throw new ArgumentNullException(nameof(activeBits));
            ActivationStrength = activationStrength;
        }
    }

    /// <summary>
    /// Specialized cortical column for encoding specific feature types
    /// </summary>
    public class ConceptColumn
    {
        private readonly string _columnType;
        private readonly int _patternSize;
        private readonly double _sparsity;
        private readonly Dictionary<string, int[]> _featureToPattern;
        private readonly Random _random;

        public ConceptColumn(string columnType, int patternSize, double sparsity)
        {
            _columnType = columnType;
            _patternSize = patternSize;
            _sparsity = sparsity;
            _featureToPattern = new Dictionary<string, int[]>();
            // Create more distinct random seeds for different column types
            var seed = columnType.GetHashCode() ^ patternSize.GetHashCode() ^ Environment.TickCount;
            _random = new Random(seed);
        }

        /// <summary>
        /// Encode features into sparse pattern for this column
        /// </summary>
        public SparsePattern EncodeFeatures(List<string> features)
        {
            var allBits = new HashSet<int>();
            
            foreach (var feature in features)
            {
                if (!_featureToPattern.TryGetValue(feature, out var pattern))
                {
                    // Create new sparse pattern for this feature
                    var activeBitCount = (int)(_patternSize * _sparsity);
                    pattern = Enumerable.Range(0, _patternSize)
                        .OrderBy(x => _random.Next())
                        .Take(activeBitCount)
                        .ToArray();
                    
                    _featureToPattern[feature] = pattern;
                }
                
                foreach (var bit in pattern)
                {
                    allBits.Add(bit);
                }
            }
            
            return new SparsePattern(allBits.ToArray(), features.Count);
        }
    }
}
