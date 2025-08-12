using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Text;

namespace GreyMatter.Storage
{
    /// <summary>
    /// Pre-trained semantic classifier using sentence embeddings
    /// Maps text to Huth-inspired biological semantic domains without requiring custom training
    /// Uses cosine similarity against domain exemplars for classification
    /// </summary>
    public class PreTrainedSemanticClassifier : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly Dictionary<string, string[]> _domainExemplars;
        private readonly Dictionary<string, float[]> _domainEmbeddings;
        private bool _disposed = false;

        public PreTrainedSemanticClassifier(string modelPath)
        {
            // Initialize ONNX session for sentence transformer model
            // Note: You would download a pre-trained sentence transformer model like all-MiniLM-L6-v2
            _session = new InferenceSession(modelPath);
            
            _domainExemplars = InitializeDomainExemplars();
            _domainEmbeddings = new Dictionary<string, float[]>();
            
            // Pre-compute embeddings for domain exemplars
            InitializeDomainEmbeddings();
        }

        /// <summary>
        /// Classify text into biological semantic domain using pre-trained embeddings
        /// </summary>
        public string ClassifySemanticDomain(string text, double threshold = 0.6)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "semantic_domains/general_concepts";

            try
            {
                // Get embedding for input text
                var textEmbedding = GetTextEmbedding(text);
                
                // Find best matching domain using cosine similarity
                var bestMatch = FindBestDomainMatch(textEmbedding);
                
                // Return domain if similarity is above threshold
                if (bestMatch.Similarity >= threshold)
                {
                    return $"semantic_domains/{bestMatch.Domain}";
                }
                
                return "semantic_domains/general_concepts";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Classification error: {ex.Message}");
                return "semantic_domains/general_concepts";
            }
        }

        /// <summary>
        /// Get semantic embedding for text using pre-trained model
        /// </summary>
        private float[] GetTextEmbedding(string text)
        {
            // Tokenize and encode text (simplified - real implementation would use proper tokenizer)
            var tokens = SimpleTokenize(text);
            
            // Create input tensors
            var inputIds = new DenseTensor<long>(new[] { 1, tokens.Length });
            for (int i = 0; i < tokens.Length; i++)
            {
                inputIds[0, i] = tokens[i];
            }
            
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputIds)
            };

            // Run inference
            using var results = _session.Run(inputs);
            var embeddings = results.First().AsTensor<float>();
            
            // Extract and normalize embedding
            var embedding = new float[embeddings.Dimensions[2]];
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] = embeddings[0, 0, i];
            }
            
            return NormalizeVector(embedding);
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
        /// Simple tokenization (in real implementation, use proper tokenizer for the model)
        /// </summary>
        private long[] SimpleTokenize(string text)
        {
            // This is a placeholder - real implementation would use the model's tokenizer
            var words = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Select(w => (long)w.GetHashCode() % 50000).ToArray();
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
        /// Pre-compute embeddings for all domain exemplars
        /// </summary>
        private void InitializeDomainEmbeddings()
        {
            foreach (var (domain, exemplars) in _domainExemplars)
            {
                // Compute average embedding for domain exemplars
                var domainEmbeddings = exemplars.Select(GetTextEmbedding).ToArray();
                var averageEmbedding = AverageEmbeddings(domainEmbeddings);
                
                _domainEmbeddings[domain] = averageEmbedding;
            }
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
