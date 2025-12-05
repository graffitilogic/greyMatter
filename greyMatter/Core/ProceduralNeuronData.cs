using System;
using System.Collections.Generic;
using MessagePack;

namespace GreyMatter.Core
{
    /// <summary>
    /// Phase 6B: Compact neuron representation for procedural regeneration.
    /// Implements "No Man's Sky principle" - store only VQ code + connection weights,
    /// regenerate full neuron structure on-demand.
    /// 
    /// Storage savings: ~90% compression vs full NeuronSnapshot
    /// - Full snapshot: ~500-1000 bytes per neuron (weights, metadata, etc.)
    /// - Compact: ~50-100 bytes (VQ code + sparse connections)
    /// </summary>
    [MessagePackObject]
    public class ProceduralNeuronData
    {
        /// <summary>
        /// Neuron identity (required for synaptic connections)
        /// </summary>
        [Key(0)]
        public Guid Id { get; set; }
        
        /// <summary>
        /// VQ code index that defines this neuron's pattern response.
        /// Range: 0-511 (for 512-code VQ codebook)
        /// This is the CORE of procedural regeneration - everything else derives from this.
        /// </summary>
        [Key(1)]
        public int VqCode { get; set; }
        
        /// <summary>
        /// Sparse synaptic connections: (targetNeuronId, weight)
        /// Only store strong connections (>0.1 weight) for Hebbian network.
        /// Average: 5-20 connections per neuron (~90% sparsity)
        /// </summary>
        [Key(2)]
        public Dictionary<Guid, float> SynapticWeights { get; set; } = new();
        
        /// <summary>
        /// Importance score for prioritization during regeneration.
        /// High-importance neurons regenerated first, low-importance can be lazy-loaded.
        /// </summary>
        [Key(3)]
        public float ImportanceScore { get; set; }
        
        /// <summary>
        /// Activation count for usage tracking.
        /// Used to determine if neuron should be kept in procedural form vs fully loaded.
        /// </summary>
        [Key(4)]
        public int ActivationCount { get; set; }
        
        /// <summary>
        /// Optional: Cluster membership for locality-aware regeneration.
        /// Neurons in same cluster often loaded together.
        /// </summary>
        [Key(5)]
        public Guid ClusterId { get; set; }
        
        /// <summary>
        /// Optional: Concept tag for debugging/validation.
        /// Not used in regeneration, but helpful for verification.
        /// </summary>
        [Key(6)]
        public string ConceptTag { get; set; } = "";
        
        /// <summary>
        /// Convert full NeuronSnapshot to compact procedural representation.
        /// Extracts VQ code from pattern, keeps only strong synaptic weights.
        /// </summary>
        public static ProceduralNeuronData FromSnapshot(NeuronSnapshot snapshot, int vqCode, Guid clusterId)
        {
            // Extract only strong connections (Hebbian network sparsity)
            var strongConnections = new Dictionary<Guid, float>();
            foreach (var weight in snapshot.InputWeights)
            {
                if (Math.Abs(weight.Value) > 0.1) // Threshold for significant connection
                {
                    strongConnections[weight.Key] = (float)weight.Value;
                }
            }
            
            return new ProceduralNeuronData
            {
                Id = snapshot.Id,
                VqCode = vqCode,
                SynapticWeights = strongConnections,
                ImportanceScore = (float)snapshot.ImportanceScore,
                ActivationCount = snapshot.ActivationCount,
                ClusterId = clusterId,
                ConceptTag = snapshot.ConceptTag
            };
        }
        
        /// <summary>
        /// Estimate storage size in bytes for compression ratio calculation.
        /// </summary>
        public int EstimatedBytes()
        {
            // Guid (16) + int (4) + float (4) + int (4) + Guid (16) + string (~20)
            int baseSize = 64;
            // Each connection: Guid (16) + float (4) = 20 bytes
            int connectionsSize = SynapticWeights.Count * 20;
            return baseSize + connectionsSize;
        }
    }
    
    /// <summary>
    /// Manages procedural neuron regeneration from compact representation.
    /// Uses VQ code + hypernetwork-style rules to rebuild full neuron.
    /// </summary>
    public class ProceduralNeuronRegenerator
    {
        private readonly VectorQuantizer _vectorQuantizer;
        private readonly FeatureEncoder _featureEncoder;
        
        // Hypernetwork-style parameters for neuron generation
        private const double BASE_THRESHOLD = -69.0;
        private const double BASE_BIAS = 0.0;
        private const double BASE_LEARNING_RATE = 0.1;
        
        public ProceduralNeuronRegenerator(VectorQuantizer vq, FeatureEncoder encoder)
        {
            _vectorQuantizer = vq;
            _featureEncoder = encoder;
        }
        
        /// <summary>
        /// Regenerate full HybridNeuron from compact procedural data.
        /// This is the core of Phase 6B - procedural reconstruction.
        /// </summary>
        public HybridNeuron RegenerateNeuron(ProceduralNeuronData compactData)
        {
            // Step 1: Get VQ code vector (learned pattern this neuron responds to)
            var vqVector = _vectorQuantizer.GetCodebookVector(compactData.VqCode);
            
            // Convert float[] to double[] for neuron properties
            var vqVectorDouble = Array.ConvertAll(vqVector, x => (double)x);
            
            // Step 2: Create neuron with generated properties from VQ code
            var neuron = new HybridNeuron(compactData.ConceptTag)
            {
                // Regenerate properties from VQ code characteristics
                Threshold = GenerateThreshold(vqVectorDouble),
                Bias = GenerateBias(vqVectorDouble),
                LearningRate = BASE_LEARNING_RATE,
                
                // LastUsed set to now (freshly regenerated)
                LastUsed = DateTime.UtcNow
            };
            
            // Step 3: Restore identity using FromSnapshot (which has access to private setters)
            // Create minimal snapshot with essential fields
            var tempSnapshot = new NeuronSnapshot
            {
                Id = compactData.Id,
                ConceptTag = compactData.ConceptTag,
                ImportanceScore = compactData.ImportanceScore,
                ActivationCount = compactData.ActivationCount,
                Bias = neuron.Bias,
                Threshold = neuron.Threshold,
                LearningRate = BASE_LEARNING_RATE,
                InputWeights = new Dictionary<Guid, double>(),
                AssociatedConcepts = new List<string> { compactData.ConceptTag },
                LastUsed = DateTime.UtcNow
            };
            
            // Regenerate from snapshot to get proper identity
            neuron = HybridNeuron.FromSnapshot(tempSnapshot);
            
            // Step 4: Restore synaptic weights (Hebbian connections preserved)
            foreach (var connection in compactData.SynapticWeights)
            {
                neuron.InputWeights[connection.Key] = connection.Value;
            }
            
            // Step 5: Set concept tag for cluster membership
            if (!string.IsNullOrEmpty(compactData.ConceptTag))
            {
                neuron.AssociateConcept(compactData.ConceptTag);
            }
            
            return neuron;
        }
        
        /// <summary>
        /// Generate activation threshold from VQ code vector.
        /// Uses vector magnitude to determine selectivity.
        /// High magnitude = more selective = higher threshold
        /// </summary>
        private double GenerateThreshold(double[] vqVector)
        {
            double magnitude = 0.0;
            foreach (var val in vqVector)
            {
                magnitude += val * val;
            }
            magnitude = Math.Sqrt(magnitude);
            
            // Map magnitude to threshold range: -69.5 to -68.5
            // More selective neurons have higher (less negative) thresholds
            double normalizedMag = Math.Tanh(magnitude / 10.0); // 0-1 range
            return BASE_THRESHOLD + (normalizedMag * 1.0);
        }
        
        /// <summary>
        /// Generate bias from VQ code vector.
        /// Uses vector mean to determine baseline activation tendency.
        /// </summary>
        private double GenerateBias(double[] vqVector)
        {
            double sum = 0.0;
            foreach (var val in vqVector)
            {
                sum += val;
            }
            double mean = sum / vqVector.Length;
            
            // Map mean to bias range: -0.5 to +0.5
            return Math.Tanh(mean) * 0.5;
        }
        
        /// <summary>
        /// Batch regenerate neurons for cluster loading.
        /// More efficient than regenerating one at a time.
        /// </summary>
        public Dictionary<Guid, HybridNeuron> RegenerateNeurons(List<ProceduralNeuronData> compactNeurons)
        {
            var regenerated = new Dictionary<Guid, HybridNeuron>();
            
            foreach (var compactData in compactNeurons)
            {
                var neuron = RegenerateNeuron(compactData);
                regenerated[neuron.Id] = neuron;
            }
            
            return regenerated;
        }
    }
}
