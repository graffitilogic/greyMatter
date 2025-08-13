using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Text;
using System.IO;

namespace GreyMatter.Storage
{
    /// <summary>
    /// Pre-trained semantic classifier using sentence embeddings
    /// Maps text to Huth-inspired biological semantic domains without requiring custom training
    /// Uses cosine similarity against domain exemplars for classification with fallback mechanisms
    /// </summary>
    public class PreTrainedSemanticClassifier : IDisposable
    {
        private InferenceSession? _session;
        private readonly Dictionary<string, string[]> _domainExemplars;
        private readonly Dictionary<string, float[]> _domainEmbeddings;
        private readonly SemanticStorageManager _storage;
        private bool _disposed = false;
        private bool _onnxModelAvailable = false;
        private readonly Dictionary<string, int> _vocabulary;

        public PreTrainedSemanticClassifier(SemanticStorageManager storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _domainExemplars = InitializeDomainExemplars();
            _domainEmbeddings = new Dictionary<string, float[]>();
            _vocabulary = InitializeVocabulary();
            
            // Initialize domain embeddings without ONNX model first
            InitializeDomainEmbeddingsWithoutONNX();
        }

        /// <summary>
        /// Initialize ONNX model asynchronously with error handling
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                var modelPath = Path.Combine(_storage.TrainingDataRoot, "models", "sentence-transformer.onnx");
                
                if (File.Exists(modelPath))
                {
                    _session = new InferenceSession(modelPath);
                    _onnxModelAvailable = true;
                    
                    // Re-compute embeddings with ONNX model
                    await InitializeDomainEmbeddingsAsync();
                    Console.WriteLine("✅ ONNX sentence transformer model loaded successfully");
                }
                else
                {
                    Console.WriteLine($"⚠️ ONNX model not found at {modelPath}, using fallback classification");
                    _onnxModelAvailable = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to load ONNX model: {ex.Message}");
                _onnxModelAvailable = false;
            }
        }

        /// <summary>
        /// Classify text into biological semantic domains with multiple fallback mechanisms
        /// </summary>
        public async Task<Dictionary<string, double>> ClassifyDomainsAsync(string text, double threshold = 0.6)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new Dictionary<string, double> { ["general_concepts"] = 0.5 };

            try
            {
                if (_onnxModelAvailable && _session != null)
                {
                    // Try ONNX-based classification
                    return await ClassifyWithONNXAsync(text, threshold);
                }
                else
                {
                    // Fallback to heuristic-based classification
                    return ClassifyWithHeuristics(text, threshold);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Classification error: {ex.Message}, using fallback");
                return ClassifyWithHeuristics(text, threshold);
            }
        }

        /// <summary>
        /// Legacy method for backwards compatibility
        /// </summary>
        public string ClassifySemanticDomain(string text, double threshold = 0.6)
        {
            var domains = ClassifyDomainsAsync(text, threshold).Result;
            var topDomain = domains.OrderByDescending(kvp => kvp.Value).First();
            return $"semantic_domains/{topDomain.Key}";
        }

        /// <summary>
        /// ONNX-based classification with improved tokenization
        /// </summary>
        private async Task<Dictionary<string, double>> ClassifyWithONNXAsync(string text, double threshold)
        {
            try
            {
                // Get embedding for input text
                var textEmbedding = await GetTextEmbeddingAsync(text);
                
                // Find best matching domains using cosine similarity
                var domainScores = new Dictionary<string, double>();
                
                foreach (var (domain, domainEmbedding) in _domainEmbeddings)
                {
                    var similarity = CosineSimilarity(textEmbedding, domainEmbedding);
                    if (similarity >= threshold * 0.8) // Slightly lower threshold for multiple results
                    {
                        var cleanDomain = domain.Split('/').Last(); // Get the last part of the domain path
                        domainScores[cleanDomain] = similarity;
                    }
                }

                // Return top domains or fallback
                if (domainScores.Any())
                {
                    return domainScores.OrderByDescending(kvp => kvp.Value)
                                     .Take(3)
                                     .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }

                return new Dictionary<string, double> { ["general_concepts"] = 0.5 };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ ONNX classification failed: {ex.Message}");
                return ClassifyWithHeuristics(text, threshold);
            }
        }

        /// <summary>
        /// Heuristic-based classification fallback
        /// </summary>
        private Dictionary<string, double> ClassifyWithHeuristics(string text, double threshold)
        {
            var lowerText = text.ToLowerInvariant();
            var domainScores = new Dictionary<string, double>();

            // Analyze text for domain indicators
            foreach (var (domain, exemplars) in _domainExemplars)
            {
                var matchScore = 0.0;
                var matchCount = 0;

                foreach (var exemplar in exemplars)
                {
                    if (lowerText.Contains(exemplar.ToLowerInvariant()))
                    {
                        matchScore += 1.0;
                        matchCount++;
                    }
                }

                if (matchCount > 0)
                {
                    var cleanDomain = domain.Split('/').Last();
                    var normalizedScore = Math.Min(matchScore / exemplars.Length, 1.0);
                    
                    if (normalizedScore >= threshold * 0.5) // More lenient for heuristic matching
                    {
                        domainScores[cleanDomain] = normalizedScore;
                    }
                }
            }

            // Additional heuristic patterns
            AddHeuristicPatterns(lowerText, domainScores);

            if (domainScores.Any())
            {
                return domainScores.OrderByDescending(kvp => kvp.Value)
                                 .Take(3)
                                 .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            return new Dictionary<string, double> { ["general_concepts"] = 0.5 };
        }

        /// <summary>
        /// Add additional heuristic pattern matching
        /// </summary>
        private void AddHeuristicPatterns(string lowerText, Dictionary<string, double> domainScores)
        {
            var patterns = new Dictionary<string, (string[] keywords, double score)>
            {
                ["emotions"] = (new[] { "feel", "emotion", "happy", "sad", "angry", "love", "fear", "joy" }, 0.8),
                ["visual"] = (new[] { "see", "look", "watch", "color", "bright", "dark", "visual", "appearance" }, 0.8),
                ["motor"] = (new[] { "move", "walk", "run", "jump", "drive", "motion", "physical" }, 0.8),
                ["language"] = (new[] { "speak", "talk", "word", "language", "communicate", "conversation" }, 0.8),
                ["social"] = (new[] { "friend", "family", "people", "social", "group", "community", "relationship" }, 0.8),
                ["cognitive"] = (new[] { "think", "thought", "mind", "brain", "memory", "learn", "knowledge" }, 0.8),
                ["temporal"] = (new[] { "time", "when", "before", "after", "during", "past", "future", "now" }, 0.7),
                ["spatial"] = (new[] { "where", "above", "below", "inside", "outside", "near", "far", "location" }, 0.7)
            };

            foreach (var (domain, (keywords, baseScore)) in patterns)
            {
                var matchCount = keywords.Count(keyword => lowerText.Contains(keyword));
                if (matchCount > 0)
                {
                    var score = baseScore * (matchCount / (double)keywords.Length);
                    if (!domainScores.ContainsKey(domain) || domainScores[domain] < score)
                    {
                        domainScores[domain] = score;
                    }
                }
            }
        }

        /// <summary>
        /// Get semantic embedding for text using ONNX model with improved tokenization
        /// </summary>
        private async Task<float[]> GetTextEmbeddingAsync(string text)
        {
            if (_session == null) throw new InvalidOperationException("ONNX session not initialized");

            try
            {
                // Improved tokenization using vocabulary
                var tokens = ImprovedTokenize(text);
                
                // Create input tensors with proper dimensions
                var inputIds = new DenseTensor<long>(new[] { 1, tokens.Length });
                var attentionMask = new DenseTensor<long>(new[] { 1, tokens.Length });
                var tokenTypeIds = new DenseTensor<long>(new[] { 1, tokens.Length });
                
                for (int i = 0; i < tokens.Length; i++)
                {
                    inputIds[0, i] = tokens[i];
                    attentionMask[0, i] = 1; // All tokens are attended to
                    tokenTypeIds[0, i] = 0; // All tokens are sentence A for single sentence
                }
                
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
                    NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask),
                    NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIds)
                };

                // Run inference
                using var results = _session.Run(inputs);
                var outputTensor = results.FirstOrDefault()?.AsTensor<float>();
                
                if (outputTensor == null)
                    throw new InvalidOperationException("No output from ONNX model");
                
                // Extract embedding (taking mean pooling of last hidden state)
                var seqLength = (int)outputTensor.Dimensions[1];
                var hiddenSize = (int)outputTensor.Dimensions[2];
                var embedding = new float[hiddenSize];
                
                // Mean pooling across sequence length
                for (int i = 0; i < hiddenSize; i++)
                {
                    var sum = 0.0f;
                    for (int j = 0; j < seqLength; j++)
                    {
                        sum += outputTensor[0, j, i];
                    }
                    embedding[i] = sum / seqLength;
                }
                
                return NormalizeVector(embedding);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Embedding generation failed: {ex.Message}");
                // Return a random normalized vector as fallback
                var random = new Random();
                var fallbackEmbedding = Enumerable.Range(0, 384) // Common sentence transformer dimension
                    .Select(_ => (float)(random.NextDouble() - 0.5))
                    .ToArray();
                return NormalizeVector(fallbackEmbedding);
            }
        }

        /// <summary>
        /// Improved tokenization with vocabulary lookup
        /// </summary>
        private long[] ImprovedTokenize(string text)
        {
            // Basic preprocessing
            text = text.ToLowerInvariant().Trim();
            
            // Split into words and handle punctuation
            var words = System.Text.RegularExpressions.Regex.Split(text, @"\W+")
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .ToArray();
            
            var tokens = new List<long>();
            
            // Add [CLS] token
            tokens.Add(101); // Standard BERT [CLS] token ID
            
            foreach (var word in words)
            {
                if (_vocabulary.TryGetValue(word, out var tokenId))
                {
                    tokens.Add(tokenId);
                }
                else
                {
                    // Use [UNK] token for unknown words
                    tokens.Add(100); // Standard BERT [UNK] token ID
                }
            }
            
            // Add [SEP] token
            tokens.Add(102); // Standard BERT [SEP] token ID
            
            // Limit sequence length
            const int maxLength = 512;
            if (tokens.Count > maxLength)
            {
                tokens = tokens.Take(maxLength - 1).ToList();
                tokens.Add(102); // Ensure [SEP] at the end
            }
            
            return tokens.ToArray();
        }

        /// <summary>
        /// Initialize a basic vocabulary for tokenization
        /// </summary>
        private Dictionary<string, int> InitializeVocabulary()
        {
            var vocab = new Dictionary<string, int>();
            
            // Common English words with token IDs
            var commonWords = new[]
            {
                "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by",
                "is", "are", "was", "were", "be", "been", "have", "has", "had", "do", "does", "did",
                "can", "could", "will", "would", "should", "may", "might", "must",
                "i", "you", "he", "she", "it", "we", "they", "me", "him", "her", "us", "them",
                "this", "that", "these", "those", "what", "which", "who", "when", "where", "why", "how",
                "cat", "dog", "house", "car", "tree", "water", "fire", "earth", "air", "sun", "moon",
                "happy", "sad", "good", "bad", "big", "small", "hot", "cold", "fast", "slow",
                "red", "blue", "green", "yellow", "black", "white", "color", "see", "look", "hear",
                "think", "know", "understand", "learn", "teach", "study", "work", "play", "eat", "drink",
                "walk", "run", "jump", "move", "stop", "go", "come", "leave", "stay", "live"
            };
            
            for (int i = 0; i < commonWords.Length; i++)
            {
                vocab[commonWords[i]] = 1000 + i; // Start from 1000 to avoid conflicts with special tokens
            }
            
            return vocab;
        }

        /// <summary>
        /// Find the best matching domain using cosine similarity
        /// </summary>
        private (string Domain, double Similarity) FindBestDomainMatch(float[] textEmbedding)
        {
            var bestDomain = "";
            var bestSimilarity = -1.0;

            foreach (var (domain, domainEmbedding) in _domainEmbeddings)
            {
                var similarity = CosineSimilarity(textEmbedding, domainEmbedding);
                
                if (similarity > bestSimilarity)
                {
                    bestSimilarity = similarity;
                    bestDomain = domain;
                }
            }

            return (bestDomain, bestSimilarity);
        }

        /// <summary>
        /// Calculate cosine similarity between two vectors
        /// </summary>
        private double CosineSimilarity(float[] a, float[] b)
        {
            var dotProduct = 0.0;
            var normA = 0.0;
            var normB = 0.0;
            
            for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
            {
                dotProduct += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }
            
            return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        /// <summary>
        /// Normalize vector to unit length
        /// </summary>
        private float[] NormalizeVector(float[] vector)
        {
            var norm = Math.Sqrt(vector.Sum(x => x * x));
            return vector.Select(x => (float)(x / norm)).ToArray();
        }

        /// <summary>
        /// Initialize domain embeddings without ONNX model (using heuristic vectors)
        /// </summary>
        private void InitializeDomainEmbeddingsWithoutONNX()
        {
            var random = new Random(42); // Fixed seed for consistency
            
            foreach (var (domain, exemplars) in _domainExemplars)
            {
                // Create a deterministic embedding based on domain characteristics
                var embedding = CreateHeuristicEmbedding(domain, exemplars, random);
                _domainEmbeddings[domain] = embedding;
            }
        }

        /// <summary>
        /// Initialize domain embeddings with ONNX model
        /// </summary>
        private async Task InitializeDomainEmbeddingsAsync()
        {
            if (!_onnxModelAvailable || _session == null) return;

            foreach (var (domain, exemplars) in _domainExemplars)
            {
                try
                {
                    // Compute embeddings for exemplars and average them
                    var embeddingTasks = exemplars.Select(GetTextEmbeddingAsync);
                    var domainEmbeddings = await Task.WhenAll(embeddingTasks);
                    var averageEmbedding = AverageEmbeddings(domainEmbeddings);
                    
                    _domainEmbeddings[domain] = averageEmbedding;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to compute embeddings for domain {domain}: {ex.Message}");
                    // Keep the heuristic embedding as fallback
                }
            }
        }

        /// <summary>
        /// Create a heuristic embedding based on domain characteristics
        /// </summary>
        private float[] CreateHeuristicEmbedding(string domain, string[] exemplars, Random random)
        {
            const int embeddingDim = 384; // Standard sentence transformer dimension
            var embedding = new float[embeddingDim];
            
            // Create domain-specific patterns in the embedding space
            var domainSeed = domain.GetHashCode();
            var domainRandom = new Random(domainSeed);
            
            // Base random vector
            for (int i = 0; i < embeddingDim; i++)
            {
                embedding[i] = (float)(domainRandom.NextDouble() - 0.5);
            }
            
            // Add domain-specific characteristics
            var domainParts = domain.Split('/');
            for (int i = 0; i < domainParts.Length && i < embeddingDim / 4; i++)
            {
                var partSeed = domainParts[i].GetHashCode();
                var startIdx = i * (embeddingDim / 4);
                var endIdx = Math.Min(startIdx + embeddingDim / 4, embeddingDim);
                
                for (int j = startIdx; j < endIdx; j++)
                {
                    embedding[j] += (float)(new Random(partSeed + j).NextDouble() - 0.5) * 0.5f;
                }
            }
            
            // Add exemplar-based characteristics
            foreach (var exemplar in exemplars.Take(10)) // Limit to first 10 exemplars
            {
                var exemplarSeed = exemplar.GetHashCode();
                var exemplarRandom = new Random(exemplarSeed);
                var influence = 0.1f / exemplars.Length; // Reduce influence per exemplar
                
                for (int i = 0; i < embeddingDim; i++)
                {
                    embedding[i] += (float)(exemplarRandom.NextDouble() - 0.5) * influence;
                }
            }
            
            return NormalizeVector(embedding);
        }

        /// <summary>
        /// Initialize domain exemplars for each biological semantic category
        /// These serve as prototype examples for classification
        /// </summary>
        private Dictionary<string, string[]> InitializeDomainExemplars()
        {
            return new Dictionary<string, string[]>
            {
                // Living Things
                ["living_things/animals/mammals"] = new[] { "cat", "dog", "horse", "elephant", "human", "mouse", "lion", "tiger", "bear", "whale" },
                ["living_things/animals/birds"] = new[] { "bird", "eagle", "robin", "owl", "chicken", "duck", "parrot", "swan", "penguin", "falcon" },
                ["living_things/animals/fish_marine"] = new[] { "fish", "shark", "salmon", "tuna", "dolphin", "octopus", "crab", "lobster", "whale", "sea turtle" },
                ["living_things/plants/trees"] = new[] { "tree", "oak", "pine", "maple", "forest", "branch", "leaf", "trunk", "bark", "wood" },
                ["living_things/humans/body_parts"] = new[] { "head", "hand", "arm", "leg", "eye", "heart", "brain", "face", "finger", "foot" },
                
                // Artifacts & Objects
                ["artifacts/vehicles/land_vehicles"] = new[] { "car", "truck", "bus", "motorcycle", "bicycle", "train", "automobile", "vehicle", "taxi", "van" },
                ["artifacts/vehicles/aircraft"] = new[] { "airplane", "plane", "jet", "helicopter", "aircraft", "rocket", "drone", "glider", "shuttle", "balloon" },
                ["artifacts/vehicles/watercraft"] = new[] { "boat", "ship", "yacht", "canoe", "submarine", "ferry", "sailboat", "vessel", "raft", "cruise" },
                ["artifacts/technology_electronics"] = new[] { "computer", "phone", "robot", "internet", "software", "digital", "electronic", "technology", "device", "machine" },
                ["artifacts/tools_instruments"] = new[] { "tool", "hammer", "knife", "scissors", "instrument", "equipment", "device", "machine", "apparatus", "wrench" },
                ["artifacts/buildings_structures"] = new[] { "house", "building", "home", "office", "school", "hospital", "bridge", "tower", "structure", "architecture" },
                
                // Natural World
                ["natural_world/geography/landforms"] = new[] { "mountain", "hill", "valley", "desert", "island", "cliff", "canyon", "plateau", "plain", "landscape" },
                ["natural_world/geography/water_bodies"] = new[] { "ocean", "sea", "lake", "river", "stream", "waterfall", "bay", "pond", "beach", "coast" },
                ["natural_world/weather_climate"] = new[] { "rain", "snow", "sun", "wind", "storm", "cloud", "weather", "climate", "temperature", "season" },
                ["natural_world/materials_substances"] = new[] { "metal", "wood", "plastic", "glass", "stone", "water", "oil", "gas", "chemical", "material" },
                ["natural_world/colors_visual"] = new[] { "red", "blue", "green", "yellow", "color", "bright", "dark", "light", "visual", "appearance" },
                
                // Abstract Domains
                ["mental_cognitive/emotions_feelings"] = new[] { "happy", "sad", "love", "fear", "anger", "joy", "emotion", "feeling", "mood", "sentiment" },
                ["mental_cognitive/thoughts_ideas"] = new[] { "think", "idea", "thought", "concept", "theory", "philosophy", "belief", "mind", "reason", "logic" },
                ["mental_cognitive/knowledge_learning"] = new[] { "learn", "study", "knowledge", "education", "teach", "school", "science", "research", "discovery", "information" },
                ["mental_cognitive/memory_perception"] = new[] { "remember", "memory", "recall", "perceive", "sense", "aware", "attention", "focus", "conscious", "experience" },
                
                // Social/Communication
                ["social_communication/language_speech"] = new[] { "language", "speak", "talk", "word", "sentence", "communication", "conversation", "voice", "speech", "dialogue" },
                ["social_communication/social_relations"] = new[] { "friend", "family", "relationship", "social", "community", "group", "society", "people", "interaction", "cooperation" },
                
                // Actions/Events
                ["actions_events/physical_motion"] = new[] { "move", "walk", "run", "jump", "drive", "travel", "motion", "speed", "movement", "transportation" },
                ["actions_events/mental_actions"] = new[] { "think", "decide", "plan", "imagine", "create", "solve", "analyze", "consider", "choose", "design" },
                
                // Properties
                ["properties/spatial_relations"] = new[] { "above", "below", "inside", "outside", "near", "far", "left", "right", "front", "back" },
                ["properties/temporal_relations"] = new[] { "before", "after", "during", "when", "time", "early", "late", "past", "future", "now" },
                ["properties/quantity_measurement"] = new[] { "big", "small", "many", "few", "more", "less", "number", "size", "amount", "measure" }
            };
        }

        /// <summary>
        /// Pre-compute embeddings for all domain exemplars (legacy method)
        /// </summary>
        private void InitializeDomainEmbeddings()
        {
            InitializeDomainEmbeddingsWithoutONNX();
        }

        /// <summary>
        /// Compute average of multiple embeddings
        /// </summary>
        private float[] AverageEmbeddings(float[][] embeddings)
        {
            if (embeddings.Length == 0) return new float[0];
            
            var dimensionCount = embeddings[0].Length;
            var average = new float[dimensionCount];
            
            for (int i = 0; i < dimensionCount; i++)
            {
                average[i] = embeddings.Average(e => e[i]);
            }
            
            return NormalizeVector(average);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _session?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Fallback semantic classifier that uses rule-based classification when pre-trained model is not available
    /// </summary>
    public class FallbackSemanticClassifier
    {
        private readonly Dictionary<string, string[]> _domainKeywords;

        public FallbackSemanticClassifier()
        {
            _domainKeywords = InitializeDomainKeywords();
        }

        public string ClassifySemanticDomain(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "semantic_domains/general_concepts";

            var lowerText = text.ToLowerInvariant();
            
            // Find best matching domain by keyword overlap
            var bestDomain = "";
            var bestScore = 0;

            foreach (var (domain, keywords) in _domainKeywords)
            {
                var score = keywords.Count(keyword => lowerText.Contains(keyword));
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDomain = domain;
                }
            }

            return bestScore > 0 ? $"semantic_domains/{bestDomain}" : "semantic_domains/general_concepts";
        }

        private Dictionary<string, string[]> InitializeDomainKeywords()
        {
            return new Dictionary<string, string[]>
            {
                ["living_things/animals/mammals"] = new[] { "cat", "dog", "horse", "human", "mammal", "fur", "milk" },
                ["living_things/animals/birds"] = new[] { "bird", "fly", "wing", "feather", "nest", "egg" },
                ["artifacts/vehicles/land_vehicles"] = new[] { "car", "truck", "vehicle", "drive", "road", "wheel" },
                ["artifacts/technology_electronics"] = new[] { "computer", "digital", "electronic", "technology", "software" },
                ["natural_world/weather_climate"] = new[] { "weather", "rain", "snow", "climate", "temperature" },
                ["mental_cognitive/emotions_feelings"] = new[] { "emotion", "feel", "happy", "sad", "love", "fear" }
            };
        }
    }
}
