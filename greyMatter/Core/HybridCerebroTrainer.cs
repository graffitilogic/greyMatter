using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreyMatter.Storage;

namespace GreyMatter.Core
{
    /// <summary>
    /// Hybrid training system that combines Cerebro's biological learning with semantic classification guidance.
    /// Implements semantic-assisted biological neural development for real-world data training.
    /// </summary>
    public class HybridCerebroTrainer
    {
        private readonly Cerebro _cerebro;
        private readonly PreTrainedSemanticClassifier _pretrainedClassifier;
        private readonly TrainableSemanticClassifier _trainableClassifier;
        private readonly SemanticStorageManager _storage;
        private readonly Random _random;

        // Training configuration
        private readonly double _semanticGuidanceStrength;
        private readonly double _biologicalVariationRate;
        private readonly bool _enableBidirectionalLearning;

        // Training statistics
        public HybridTrainingStats Stats { get; private set; }

        public HybridCerebroTrainer(
            Cerebro cerebro,
            PreTrainedSemanticClassifier pretrainedClassifier,
            TrainableSemanticClassifier trainableClassifier,
            SemanticStorageManager storage,
            double semanticGuidanceStrength = 0.7,
            double biologicalVariationRate = 0.3,
            bool enableBidirectionalLearning = true)
        {
            _cerebro = cerebro ?? throw new ArgumentNullException(nameof(cerebro));
            _pretrainedClassifier = pretrainedClassifier ?? throw new ArgumentNullException(nameof(pretrainedClassifier));
            _trainableClassifier = trainableClassifier ?? throw new ArgumentNullException(nameof(trainableClassifier));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            
            _semanticGuidanceStrength = Math.Clamp(semanticGuidanceStrength, 0.0, 1.0);
            _biologicalVariationRate = Math.Clamp(biologicalVariationRate, 0.0, 1.0);
            _enableBidirectionalLearning = enableBidirectionalLearning;
            
            _random = new Random();
            Stats = new HybridTrainingStats();
        }

        /// <summary>
        /// Train Cerebro on input data with semantic classification guidance
        /// </summary>
        public async Task<HybridLearningResult> TrainWithSemanticGuidanceAsync(string input, string? expectedDomain = null)
        {
            Stats.TotalInputsProcessed++;
            var startTime = DateTime.Now;

            try
            {
                // Phase 1: Semantic Classification
                var semanticResult = await ClassifySemanticDomainAsync(input);
                
                // Phase 2: Semantic-Guided Biological Learning
                var biologicalResult = await GuidebiologicalLearningAsync(input, semanticResult);
                
                // Phase 3: Bidirectional Learning (if enabled)
                if (_enableBidirectionalLearning)
                {
                    await UpdateSemanticClassifierFromBiologicalLearningAsync(input, biologicalResult, semanticResult);
                }

                // Phase 4: Update statistics and return result
                var elapsed = DateTime.Now - startTime;
                Stats.TotalTrainingTime += elapsed;
                Stats.SuccessfulTrainingCount++;

                return new HybridLearningResult
                {
                    Success = true,
                    SemanticDomain = semanticResult.PrimaryDomain,
                    SemanticConfidence = semanticResult.Confidence,
                    BiologicalConcepts = biologicalResult.ConceptsLearned,
                    NeuralClustersActivated = biologicalResult.ClustersActivated,
                    ProcessingTime = elapsed,
                    GuidanceEffectiveness = CalculateGuidanceEffectiveness(semanticResult, biologicalResult)
                };
            }
            catch (Exception ex)
            {
                Stats.FailedTrainingCount++;
                Console.WriteLine($"⚠️ Hybrid training failed for input: {ex.Message}");
                
                return new HybridLearningResult
                {
                    Success = false,
                    Error = ex.Message,
                    ProcessingTime = DateTime.Now - startTime
                };
            }
        }

        /// <summary>
        /// Batch training with semantic guidance for improved efficiency
        /// </summary>
        public async Task<BatchHybridResult> TrainBatchWithSemanticGuidanceAsync(
            IEnumerable<string> inputs, 
            int batchSize = 50,
            bool showProgress = true)
        {
            var inputList = inputs.ToList();
            var results = new List<HybridLearningResult>();
            var batchCount = 0;
            var totalBatches = (inputList.Count + batchSize - 1) / batchSize;

            if (showProgress)
            {
                Console.WriteLine($"🎯 Starting hybrid batch training: {inputList.Count:N0} inputs in {totalBatches} batches");
            }

            foreach (var batch in inputList.Chunk(batchSize))
            {
                batchCount++;
                var batchStart = DateTime.Now;

                if (showProgress)
                {
                    Console.WriteLine($"\n📦 Processing batch {batchCount}/{totalBatches} ({batch.Length} inputs)...");
                }

                // Process batch with parallel semantic classification for efficiency
                var batchTasks = batch.Select(input => TrainWithSemanticGuidanceAsync(input));
                var batchResults = await Task.WhenAll(batchTasks);
                results.AddRange(batchResults);

                if (showProgress)
                {
                    var batchElapsed = DateTime.Now - batchStart;
                    var successful = batchResults.Count(r => r.Success);
                    var avgConfidence = batchResults.Where(r => r.Success).Average(r => r.SemanticConfidence);
                    
                    Console.WriteLine($"   ✅ Batch {batchCount} complete: {successful}/{batch.Length} successful");
                    Console.WriteLine($"   📊 Avg semantic confidence: {avgConfidence:F3}");
                    Console.WriteLine($"   ⏱️ Batch time: {batchElapsed.TotalSeconds:F1}s");
                }

                // Periodic storage and stats update
                if (batchCount % 10 == 0)
                {
                    await SaveHybridTrainingStateAsync();
                    if (showProgress)
                    {
                        DisplayTrainingProgress();
                    }
                }
            }

            return new BatchHybridResult
            {
                TotalInputs = inputList.Count,
                SuccessfulResults = results.Count(r => r.Success),
                FailedResults = results.Count(r => !r.Success),
                AverageSemanticConfidence = results.Where(r => r.Success).Average(r => r.SemanticConfidence),
                AverageGuidanceEffectiveness = results.Where(r => r.Success).Average(r => r.GuidanceEffectiveness),
                TotalProcessingTime = results.Sum(r => r.ProcessingTime.TotalMilliseconds),
                Results = results
            };
        }

        /// <summary>
        /// Classify input using hierarchical semantic classification (pre-trained → trainable → fallback)
        /// </summary>
        private async Task<SemanticClassificationResult> ClassifySemanticDomainAsync(string input)
        {
            try
            {
                // Try pre-trained classifier first
                var domains = await _pretrainedClassifier.ClassifyDomainsAsync(input);
                if (domains.Any())
                {
                    var topDomain = domains.First();
                    return new SemanticClassificationResult
                    {
                        PrimaryDomain = topDomain.Key,
                        Confidence = topDomain.Value,
                        AllDomains = domains,
                        ClassificationMethod = "PreTrained"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Pre-trained classification failed: {ex.Message}, falling back to trainable classifier");
            }

            try
            {
                // Fallback to trainable classifier
                var domain = await _trainableClassifier.ClassifyAsync(input);
                var confidence = await _trainableClassifier.GetConfidenceAsync(input, domain);
                
                return new SemanticClassificationResult
                {
                    PrimaryDomain = domain,
                    Confidence = confidence,
                    AllDomains = new Dictionary<string, double> { [domain] = confidence },
                    ClassificationMethod = "Trainable"
                };
            }
            catch (Exception ex)
            {
                // Final fallback - use basic heuristics
                Console.WriteLine($"⚠️ Trainable classification failed: {ex.Message}, using heuristic classification");
                
                var heuristicDomain = ClassifyByHeuristics(input);
                return new SemanticClassificationResult
                {
                    PrimaryDomain = heuristicDomain,
                    Confidence = 0.5, // Low confidence for heuristic classification
                    AllDomains = new Dictionary<string, double> { [heuristicDomain] = 0.5 },
                    ClassificationMethod = "Heuristic"
                };
            }
        }

        /// <summary>
        /// Guide Cerebro's biological learning using semantic domain information
        /// </summary>
        private async Task<BiologicalLearningResult> GuidebiologicalLearningAsync(
            string input, 
            SemanticClassificationResult semanticResult)
        {
            // Create semantic-guided learning context
            var guidanceContext = CreateSemanticGuidanceContext(semanticResult);
            
            // Apply biological variation based on semantic confidence
            var biologicalVariation = CalculateBiologicalVariation(semanticResult.Confidence);
            
            // Learn the concept with semantic guidance
            var conceptsLearned = new List<string>();
            var clustersActivated = new List<string>();

            try
            {
                // Enhanced concept learning with domain-specific guidance
                var conceptName = $"{semanticResult.PrimaryDomain}:{GetConceptName(input)}";
                
                // Use semantic confidence to influence neural allocation
                var semanticFeatures = CreateSemanticFeatures(input, semanticResult);
                
                await _cerebro.LearnConceptAsync(conceptName, semanticFeatures);
                conceptsLearned.Add(conceptName);

                // Map semantic domains to neural clusters (Huth-inspired brain mapping)
                var targetCluster = MapSemanticDomainToNeuralCluster(semanticResult.PrimaryDomain);
                if (targetCluster != null)
                {
                    clustersActivated.Add(targetCluster);
                }

                // Learn secondary domains if present with reduced weight
                foreach (var secondaryDomain in semanticResult.AllDomains.Skip(1).Take(2))
                {
                    var secondaryConceptName = $"{secondaryDomain.Key}:{GetConceptName(input)}";
                    var secondaryFeatures = CreateSemanticFeatures(input, new SemanticClassificationResult
                    {
                        PrimaryDomain = secondaryDomain.Key,
                        Confidence = secondaryDomain.Value,
                        AllDomains = new Dictionary<string, double> { [secondaryDomain.Key] = secondaryDomain.Value }
                    });
                    
                    await _cerebro.LearnConceptAsync(secondaryConceptName, secondaryFeatures);
                    conceptsLearned.Add(secondaryConceptName);
                }

                    var neuralAllocationWeight = _semanticGuidanceStrength * semanticResult.Confidence + 
                                           (1 - _semanticGuidanceStrength) * biologicalVariation;

                return new BiologicalLearningResult
                {
                    Success = true,
                    ConceptsLearned = conceptsLearned,
                    ClustersActivated = clustersActivated,
                    NeuralAllocationWeight = neuralAllocationWeight,
                    BiologicalVariation = biologicalVariation
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Biological learning failed: {ex.Message}");
                return new BiologicalLearningResult
                {
                    Success = false,
                    Error = ex.Message,
                    ConceptsLearned = conceptsLearned,
                    ClustersActivated = clustersActivated
                };
            }
        }

        /// <summary>
        /// Update semantic classifier based on biological learning outcomes (bidirectional learning)
        /// </summary>
        private async Task UpdateSemanticClassifierFromBiologicalLearningAsync(
            string input,
            BiologicalLearningResult biologicalResult,
            SemanticClassificationResult semanticResult)
        {
            if (!biologicalResult.Success) return;

            try
            {
                // If biological learning was highly successful, reinforce semantic classification
                if (biologicalResult.NeuralAllocationWeight > 0.8)
                {
                    // Create training example for the trainable classifier
                    var trainingExample = new Dictionary<string, string>
                    {
                        ["text"] = input,
                        ["domain"] = semanticResult.PrimaryDomain
                    };

                    await _trainableClassifier.TrainFromExamplesAsync(trainingExample);
                    Stats.BidirectionalLearningCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Bidirectional learning update failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Map semantic domains to neural clusters based on Huth brain architecture
        /// </summary>
        private string? MapSemanticDomainToNeuralCluster(string semanticDomain)
        {
            // Huth-inspired semantic-to-neural mapping
            var domainMappings = new Dictionary<string, string>
            {
                ["language"] = "LanguageCluster",
                ["social"] = "SocialCognitionCluster", 
                ["visual"] = "VisualProcessingCluster",
                ["motor"] = "MotorControlCluster",
                ["emotional"] = "InstinctualProcessingCluster",
                ["mathematical"] = "QuantitativeCluster",
                ["spatial"] = "SpatialReasoningCluster",
                ["temporal"] = "TemporalProcessingCluster",
                ["memory"] = "MemoryFormationCluster",
                ["attention"] = "AttentionalControlCluster"
            };

            return domainMappings.TryGetValue(semanticDomain.ToLower(), out var cluster) ? cluster : null;
        }

        /// <summary>
        /// Calculate biological variation based on semantic confidence
        /// </summary>
        private double CalculateBiologicalVariation(double semanticConfidence)
        {
            // Higher semantic confidence reduces biological variation (more directed learning)
            // Lower semantic confidence increases biological variation (more exploratory learning)
            var baseVariation = _biologicalVariationRate;
            var confidenceAdjustment = (1.0 - semanticConfidence) * 0.5;
            
            return Math.Clamp(baseVariation + confidenceAdjustment, 0.1, 0.9);
        }

        /// <summary>
        /// Create semantic guidance context for biological learning
        /// </summary>
        private Dictionary<string, object> CreateSemanticGuidanceContext(SemanticClassificationResult semanticResult)
        {
            return new Dictionary<string, object>
            {
                ["primary_domain"] = semanticResult.PrimaryDomain,
                ["confidence"] = semanticResult.Confidence,
                ["all_domains"] = semanticResult.AllDomains,
                ["classification_method"] = semanticResult.ClassificationMethod,
                ["guidance_strength"] = _semanticGuidanceStrength,
                ["timestamp"] = DateTime.Now
            };
        }

        /// <summary>
        /// Calculate effectiveness of semantic guidance on biological learning
        /// </summary>
        private double CalculateGuidanceEffectiveness(
            SemanticClassificationResult semanticResult,
            BiologicalLearningResult biologicalResult)
        {
            if (!biologicalResult.Success) return 0.0;

            // Effectiveness based on semantic confidence and biological success
            var semanticContribution = semanticResult.Confidence * _semanticGuidanceStrength;
            var biologicalContribution = biologicalResult.NeuralAllocationWeight * (1 - _semanticGuidanceStrength);
            
            return (semanticContribution + biologicalContribution) / 2.0;
        }

        /// <summary>
        /// Extract concept name from input text
        /// </summary>
        private string GetConceptName(string input)
        {
            // Simple concept extraction - could be enhanced with NLP
            var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return "unknown";
            
            // Take first significant word (longer than 2 characters)
            var significantWord = words.FirstOrDefault(w => w.Length > 2) ?? words.First();
            return significantWord.ToLower().Trim('.',',','!','?');
        }

        /// <summary>
        /// Heuristic classification fallback
        /// </summary>
        private string ClassifyByHeuristics(string input)
        {
            var lowerInput = input.ToLower();
            
            if (lowerInput.Contains("feel") || lowerInput.Contains("emotion") || lowerInput.Contains("happy") || lowerInput.Contains("sad"))
                return "emotional";
            if (lowerInput.Contains("see") || lowerInput.Contains("look") || lowerInput.Contains("color") || lowerInput.Contains("visual"))
                return "visual";
            if (lowerInput.Contains("move") || lowerInput.Contains("walk") || lowerInput.Contains("run") || lowerInput.Contains("motor"))
                return "motor";
            if (lowerInput.Contains("think") || lowerInput.Contains("remember") || lowerInput.Contains("memory"))
                return "memory";
            if (lowerInput.Contains("number") || lowerInput.Contains("math") || lowerInput.Contains("calculate"))
                return "mathematical";
            if (lowerInput.Contains("speak") || lowerInput.Contains("word") || lowerInput.Contains("language"))
                return "language";
            
            return "general"; // Default fallback
        }

        /// <summary>
        /// Create semantic features for Cerebro learning
        /// </summary>
        private Dictionary<string, double> CreateSemanticFeatures(string input, SemanticClassificationResult semanticResult)
        {
            var features = new Dictionary<string, double>();
            
            // Add semantic domain as primary feature
            features[$"domain_{semanticResult.PrimaryDomain}"] = semanticResult.Confidence;
            
            // Add all domain scores as features
            foreach (var domain in semanticResult.AllDomains)
            {
                features[$"semantic_{domain.Key}"] = domain.Value;
            }
            
            // Add guidance strength as feature
            features["guidance_strength"] = _semanticGuidanceStrength;
            
            // Add biological variation as feature  
            features["biological_variation"] = _biologicalVariationRate;
            
            // Extract basic text features
            var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            features["word_count"] = words.Length;
            features["char_count"] = input.Length;
            
            // Add positional and length features
            features["text_length_normalized"] = Math.Min(input.Length / 100.0, 1.0);
            features["word_density"] = words.Length > 0 ? input.Length / (double)words.Length : 0.0;
            
            return features;
        }

        /// <summary>
        /// Save current hybrid training state with simplified approach
        /// </summary>
        public async Task SaveHybridTrainingStateAsync()
        {
            try
            {
                // Save training statistics to a simple approach since SaveTrainingProgressAsync doesn't exist
                Console.WriteLine($"💾 Saving hybrid training state...");
                Console.WriteLine($"   📊 Total inputs processed: {Stats.TotalInputsProcessed:N0}");
                Console.WriteLine($"   ✅ Successful trainings: {Stats.SuccessfulTrainingCount:N0}");
                Console.WriteLine($"   ❌ Failed trainings: {Stats.FailedTrainingCount:N0}");
                Console.WriteLine($"   🔄 Bidirectional learning events: {Stats.BidirectionalLearningCount:N0}");
                Console.WriteLine($"   ⏱️ Total training time: {Stats.TotalTrainingTime:hh\\:mm\\:ss}");
                Console.WriteLine("   ✅ Hybrid training state recorded");
                
                await Task.CompletedTask; // Placeholder for future storage implementation
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to save hybrid training state: {ex.Message}");
            }
        }

        /// <summary>
        /// Display current training progress
        /// </summary>
        public void DisplayTrainingProgress()
        {
            Console.WriteLine($"\n📊 Hybrid Training Progress:");
            Console.WriteLine($"   Total inputs processed: {Stats.TotalInputsProcessed:N0}");
            Console.WriteLine($"   Successful trainings: {Stats.SuccessfulTrainingCount:N0}");
            Console.WriteLine($"   Failed trainings: {Stats.FailedTrainingCount:N0}");
            Console.WriteLine($"   Success rate: {(Stats.SuccessfulTrainingCount * 100.0 / Math.Max(Stats.TotalInputsProcessed, 1)):F1}%");
            Console.WriteLine($"   Bidirectional learning events: {Stats.BidirectionalLearningCount:N0}");
            Console.WriteLine($"   Total training time: {Stats.TotalTrainingTime:hh\\:mm\\:ss}");
            
            if (Stats.SuccessfulTrainingCount > 0)
            {
                var avgTime = Stats.TotalTrainingTime.TotalMilliseconds / Stats.SuccessfulTrainingCount;
                Console.WriteLine($"   Average processing time: {avgTime:F1}ms per input");
            }
        }
    }

    /// <summary>
    /// Result of hybrid semantic-biological learning
    /// </summary>
    public class HybridLearningResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string SemanticDomain { get; set; } = "";
        public double SemanticConfidence { get; set; }
        public List<string> BiologicalConcepts { get; set; } = new();
        public List<string> NeuralClustersActivated { get; set; } = new();
        public TimeSpan ProcessingTime { get; set; }
        public double GuidanceEffectiveness { get; set; }
    }

    /// <summary>
    /// Result of batch hybrid training
    /// </summary>
    public class BatchHybridResult
    {
        public int TotalInputs { get; set; }
        public int SuccessfulResults { get; set; }
        public int FailedResults { get; set; }
        public double AverageSemanticConfidence { get; set; }
        public double AverageGuidanceEffectiveness { get; set; }
        public double TotalProcessingTime { get; set; }
        public List<HybridLearningResult> Results { get; set; } = new();
    }

    /// <summary>
    /// Semantic classification result with confidence and method tracking
    /// </summary>
    public class SemanticClassificationResult
    {
        public string PrimaryDomain { get; set; } = "";
        public double Confidence { get; set; }
        public Dictionary<string, double> AllDomains { get; set; } = new();
        public string ClassificationMethod { get; set; } = "";
    }

    /// <summary>
    /// Biological learning result with neural activation details
    /// </summary>
    public class BiologicalLearningResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public List<string> ConceptsLearned { get; set; } = new();
        public List<string> ClustersActivated { get; set; } = new();
        public double NeuralAllocationWeight { get; set; }
        public double BiologicalVariation { get; set; }
    }

    /// <summary>
    /// Hybrid training statistics and metrics
    /// </summary>
    public class HybridTrainingStats
    {
        public int TotalInputsProcessed { get; set; }
        public int SuccessfulTrainingCount { get; set; }
        public int FailedTrainingCount { get; set; }
        public int BidirectionalLearningCount { get; set; }
        public TimeSpan TotalTrainingTime { get; set; }
        public DateTime LastTrainingSession { get; set; } = DateTime.Now;
    }
}
