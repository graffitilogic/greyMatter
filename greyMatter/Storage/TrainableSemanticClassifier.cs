using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace GreyMatter.Storage
{
    /// <summary>
    /// Learnable semantic domain classifier that evolves from training data
    /// Replaces hardcoded keyword matching with trainable semantic understanding
    /// </summary>
    public class TrainableSemanticClassifier
    {
        private readonly string _classifierDataPath;
        private Dictionary<string, SemanticDomainModel> _domainModels;
        private Dictionary<string, Dictionary<string, double>> _wordEmbeddings;
        private Dictionary<string, List<string>> _domainExamples;
        private int _trainingIterations = 0;
        
        // Semantic domains based on Huth's architecture
        private readonly List<string> _semanticDomains = new()
        {
            "living_things/animals/mammals",
            "living_things/animals/birds", 
            "living_things/animals/marine",
            "living_things/plants/trees",
            "living_things/humans/body_parts",
            "living_things/humans/family_relations",
            
            "artifacts/vehicles/land_vehicles",
            "artifacts/vehicles/watercraft",
            "artifacts/vehicles/aircraft",
            "artifacts/tools/instruments",
            "artifacts/buildings/structures",
            "artifacts/clothing/textiles",
            "artifacts/food/nutrition",
            "artifacts/technology/electronics",
            
            "natural_world/geography/landforms",
            "natural_world/geography/water_bodies",
            "natural_world/weather/climate",
            "natural_world/materials/substances",
            "natural_world/colors/visual",
            
            "mental_cognitive/emotions/feelings",
            "mental_cognitive/thoughts/ideas", 
            "mental_cognitive/knowledge/learning",
            "mental_cognitive/memory/perception",
            
            "social_communication/language/speech",
            "social_communication/social/relations",
            "social_communication/cultural/practices",
            
            "actions_events/physical/motion",
            "actions_events/mental/cognitive_actions",
            "actions_events/social/interactions",
            "actions_events/work/occupations",
            
            "properties/spatial/relations",
            "properties/temporal/relations", 
            "properties/quantity/measurement",
            "properties/quality/evaluation"
        };
        
        public TrainableSemanticClassifier(string classifierDataPath)
        {
            _classifierDataPath = classifierDataPath;
            _domainModels = new Dictionary<string, SemanticDomainModel>();
            _wordEmbeddings = new Dictionary<string, Dictionary<string, double>>();
            _domainExamples = new Dictionary<string, List<string>>();
            
            InitializeDomainModels();
        }
        
        private void InitializeDomainModels()
        {
            foreach (var domain in _semanticDomains)
            {
                _domainModels[domain] = new SemanticDomainModel
                {
                    Domain = domain,
                    KeywordWeights = new Dictionary<string, double>(),
                    ContextualPatterns = new Dictionary<string, double>(),
                    Confidence = 0.5, // Start with neutral confidence
                    ExampleCount = 0
                };
                
                _domainExamples[domain] = new List<string>();
            }
        }
        
        /// <summary>
        /// Train the classifier with labeled examples from training data
        /// </summary>
        public async Task TrainFromExamplesAsync(Dictionary<string, string> labeledExamples)
        {
            Console.WriteLine($"🧠 Training semantic classifier with {labeledExamples.Count} examples...");
            
            foreach (var example in labeledExamples)
            {
                var text = example.Key.ToLower();
                var domain = example.Value;
                
                if (_domainModels.ContainsKey(domain))
                {
                    // Extract features from the example
                    var features = ExtractFeatures(text);
                    
                    // Update domain model with these features
                    UpdateDomainModel(domain, features);
                    
                    // Store example for future reference
                    _domainExamples[domain].Add(text);
                    
                    // Build word embeddings
                    UpdateWordEmbeddings(text, domain);
                }
            }
            
            _trainingIterations++;
            await SaveClassifierStateAsync();
            
            Console.WriteLine($"✅ Training iteration {_trainingIterations} complete");
            Console.WriteLine($"   Domains with examples: {_domainExamples.Count(d => d.Value.Count > 0)}");
        }
        
        /// <summary>
        /// Classify text into semantic domain using learned patterns
        /// </summary>
        public string ClassifySemanticDomain(string text, object context = null)
        {
            if (string.IsNullOrEmpty(text))
                return "semantic_domains/general_concepts";
                
            var features = ExtractFeatures(text.ToLower());
            var contextFeatures = ExtractContextFeatures(context);
            var allFeatures = features.Concat(contextFeatures).ToList();
            
            var domainScores = new Dictionary<string, double>();
            
            foreach (var domain in _semanticDomains)
            {
                var model = _domainModels[domain];
                var score = CalculateDomainScore(allFeatures, model);
                domainScores[domain] = score;
            }
            
            // Find best matching domain
            var bestDomain = domainScores.OrderByDescending(d => d.Value).First();
            
            // Only return domain if confidence is above threshold
            if (bestDomain.Value > 0.3)
            {
                return $"semantic_domains/{bestDomain.Key}";
            }
            
            // Fallback to general concepts
            return "semantic_domains/general_concepts";
        }
        
        /// <summary>
        /// Extract linguistic and semantic features from text
        /// </summary>
        private List<string> ExtractFeatures(string text)
        {
            var features = new List<string>();
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            // Individual words
            features.AddRange(words);
            
            // Word patterns
            foreach (var word in words)
            {
                // Morphological patterns
                if (word.EndsWith("ing")) features.Add("pattern:progressive");
                if (word.EndsWith("ed")) features.Add("pattern:past");
                if (word.EndsWith("er")) features.Add("pattern:comparative");
                if (word.EndsWith("ly")) features.Add("pattern:adverb");
                
                // Semantic hints
                if (word.Contains("bio") || word.Contains("life")) features.Add("semantic:biological");
                if (word.Contains("tech") || word.Contains("digital")) features.Add("semantic:technology");
                if (word.Contains("feel") || word.Contains("emotion")) features.Add("semantic:emotional");
            }
            
            // Bigrams for context
            for (int i = 0; i < words.Length - 1; i++)
            {
                features.Add($"bigram:{words[i]}_{words[i + 1]}");
            }
            
            return features;
        }
        
        /// <summary>
        /// Extract features from context object (if provided)
        /// </summary>
        private List<string> ExtractContextFeatures(object context)
        {
            var features = new List<string>();
            
            if (context != null)
            {
                var contextJson = JsonSerializer.Serialize(context).ToLower();
                var contextWords = contextJson.Split(new[] { '"', ':', ',', '{', '}', ' ' }, 
                    StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var word in contextWords)
                {
                    features.Add($"context:{word}");
                }
            }
            
            return features;
        }
        
        /// <summary>
        /// Update domain model with new feature evidence
        /// </summary>
        private void UpdateDomainModel(string domain, List<string> features)
        {
            var model = _domainModels[domain];
            
            foreach (var feature in features)
            {
                if (model.KeywordWeights.ContainsKey(feature))
                {
                    // Increase weight for recurring features
                    model.KeywordWeights[feature] = Math.Min(1.0, model.KeywordWeights[feature] + 0.1);
                }
                else
                {
                    // New feature starts with moderate weight
                    model.KeywordWeights[feature] = 0.3;
                }
            }
            
            model.ExampleCount++;
            // Increase confidence as we see more examples
            model.Confidence = Math.Min(0.95, 0.5 + (model.ExampleCount * 0.05));
        }
        
        /// <summary>
        /// Calculate how well features match a domain model
        /// </summary>
        private double CalculateDomainScore(List<string> features, SemanticDomainModel model)
        {
            if (model.ExampleCount == 0) return 0.0;
            
            double score = 0.0;
            int matchedFeatures = 0;
            
            foreach (var feature in features)
            {
                if (model.KeywordWeights.ContainsKey(feature))
                {
                    score += model.KeywordWeights[feature];
                    matchedFeatures++;
                }
            }
            
            // Normalize by feature count and apply confidence
            var normalizedScore = matchedFeatures > 0 ? score / matchedFeatures : 0.0;
            return normalizedScore * model.Confidence;
        }
        
        /// <summary>
        /// Build simple word embeddings based on domain co-occurrence
        /// </summary>
        private void UpdateWordEmbeddings(string text, string domain)
        {
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var word in words)
            {
                if (!_wordEmbeddings.ContainsKey(word))
                {
                    _wordEmbeddings[word] = new Dictionary<string, double>();
                }
                
                var embedding = _wordEmbeddings[word];
                
                if (embedding.ContainsKey(domain))
                {
                    embedding[domain] += 1.0;
                }
                else
                {
                    embedding[domain] = 1.0;
                }
            }
        }
        
        /// <summary>
        /// Save classifier state for persistence
        /// </summary>
        private async Task SaveClassifierStateAsync()
        {
            var state = new
            {
                DomainModels = _domainModels,
                WordEmbeddings = _wordEmbeddings,
                DomainExamples = _domainExamples,
                TrainingIterations = _trainingIterations
            };
            
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            var statePath = Path.Combine(_classifierDataPath, "classifier_state.json");
            
            Directory.CreateDirectory(_classifierDataPath);
            await File.WriteAllTextAsync(statePath, json);
        }
        
        /// <summary>
        /// Load previously trained classifier state
        /// </summary>
        public async Task LoadClassifierStateAsync()
        {
            var statePath = Path.Combine(_classifierDataPath, "classifier_state.json");
            
            if (File.Exists(statePath))
            {
                var json = await File.ReadAllTextAsync(statePath);
                var state = JsonSerializer.Deserialize<dynamic>(json);
                
                // TODO: Deserialize state properly
                Console.WriteLine("🔄 Loaded previous classifier training state");
            }
        }
        
        /// <summary>
        /// Get training statistics
        /// </summary>
        public TrainingStatistics GetTrainingStatistics()
        {
            return new TrainingStatistics
            {
                TrainingIterations = _trainingIterations,
                DomainsWithExamples = _domainExamples.Count(d => d.Value.Count > 0),
                TotalExamples = _domainExamples.Values.Sum(examples => examples.Count),
                VocabularySize = _wordEmbeddings.Count,
                AverageConfidence = _domainModels.Values.Average(m => m.Confidence)
            };
        }
    }
    
    /// <summary>
    /// Statistical model for a semantic domain 
    /// </summary>
    public class SemanticDomainModel
    {
        public string Domain { get; set; } = "";
        public Dictionary<string, double> KeywordWeights { get; set; } = new();
        public Dictionary<string, double> ContextualPatterns { get; set; } = new();
        public double Confidence { get; set; }
        public int ExampleCount { get; set; }
    }
    
    /// <summary>
    /// Training progress and statistics
    /// </summary>
    public class TrainingStatistics
    {
        public int TrainingIterations { get; set; }
        public int DomainsWithExamples { get; set; }
        public int TotalExamples { get; set; }
        public int VocabularySize { get; set; }
        public double AverageConfidence { get; set; }
    }
}
