using System;
using System.Collections.Generic;
using System.Linq;

namespace GreyMatter.Core
{
    /// <summary>
    /// Phase 2: Hypernetwork for dynamic neuron generation
    /// 
    /// Instead of fixed neuron counts (e.g., always 503 neurons),
    /// generates neurons based on pattern characteristics:
    /// - Novelty: New patterns get more neurons
    /// - Frequency: Common patterns stabilize
    /// - Complexity: Rich features need more capacity
    /// 
    /// Formula: N = α * log(1 + freq) + β * novelty + γ * complexity
    /// </summary>
    public class NeuronHypernetwork
    {
        // Hyperparameters (tunable)
        private readonly double _alphaFrequency;
        private readonly double _betaNovelty;
        private readonly double _gammaComplexity;
        private readonly int _minNeurons;
        private readonly int _maxNeurons;
        
        // Deterministic seed for reproducibility
        private readonly int _seed;
        
        public NeuronHypernetwork(
            double alphaFrequency = 20.0,
            double betaNovelty = 100.0,
            double gammaComplexity = 50.0,
            int minNeurons = 5,
            int maxNeurons = 500,
            int seed = 42)
        {
            _alphaFrequency = alphaFrequency;
            _betaNovelty = betaNovelty;
            _gammaComplexity = gammaComplexity;
            _minNeurons = minNeurons;
            _maxNeurons = maxNeurons;
            _seed = seed;
        }
        
        /// <summary>
        /// Calculate how many neurons this pattern should have
        /// 
        /// Key insight: Neuron count should reflect:
        /// 1. Novelty: New patterns need capacity to learn
        /// 2. Frequency: Common patterns reach stable size
        /// 3. Complexity: Rich patterns need more neurons
        /// </summary>
        public int CalculateNeuronCount(
            double novelty,           // 0.0 = familiar, 1.0 = novel
            double frequency,         // How often pattern activated
            double complexity)        // Feature vector richness
        {
            // Logarithmic frequency term (diminishing returns)
            var freqTerm = _alphaFrequency * Math.Log(1 + frequency);
            
            // Linear novelty term (new patterns get boost)
            var noveltyTerm = _betaNovelty * novelty;
            
            // Complexity term (rich features need capacity)
            var complexityTerm = _gammaComplexity * complexity;
            
            // Combine terms
            var rawCount = freqTerm + noveltyTerm + complexityTerm;
            
            // Clamp to reasonable range
            var neuronCount = (int)Math.Round(rawCount);
            neuronCount = Math.Max(_minNeurons, Math.Min(_maxNeurons, neuronCount));
            
            return neuronCount;
        }
        
        /// <summary>
        /// Calculate feature complexity from vector
        /// Measures how "rich" the pattern is
        /// </summary>
        public double CalculateComplexity(double[] featureVector)
        {
            if (featureVector == null || featureVector.Length == 0)
                return 0.0;
            
            // Measure 1: Non-zero features (sparsity)
            var nonZeroCount = featureVector.Count(f => Math.Abs(f) > 1e-6);
            var sparsityScore = nonZeroCount / (double)featureVector.Length;
            
            // Measure 2: Variance (how spread out the values are)
            var mean = featureVector.Average();
            var variance = featureVector.Average(f => Math.Pow(f - mean, 2));
            var varianceScore = Math.Min(1.0, variance * 10); // Scale to [0,1]
            
            // Measure 3: Entropy-like measure
            var absSum = featureVector.Sum(f => Math.Abs(f));
            var entropy = 0.0;
            if (absSum > 0)
            {
                foreach (var f in featureVector)
                {
                    var p = Math.Abs(f) / absSum;
                    if (p > 1e-10)
                        entropy -= p * Math.Log(p);
                }
                entropy /= Math.Log(featureVector.Length); // Normalize
            }
            
            // Combine measures (weighted average)
            var complexity = 0.3 * sparsityScore + 0.3 * varianceScore + 0.4 * entropy;
            
            return Math.Max(0.0, Math.Min(1.0, complexity));
        }
        
        /// <summary>
        /// Generate deterministic neuron properties based on pattern
        /// 
        /// Uses feature vector as seed for procedural generation
        /// Same pattern → same neuron properties (deterministic)
        /// </summary>
        public List<NeuronProperties> GenerateNeurons(
            double[] featureVector,
            int neuronCount,
            string debugLabel = "")
        {
            var neurons = new List<NeuronProperties>(neuronCount);
            
            // Create deterministic random generator from feature hash
            var patternSeed = GetPatternSeed(featureVector);
            var rng = new Random(patternSeed);
            
            for (int i = 0; i < neuronCount; i++)
            {
                var neuron = new NeuronProperties
                {
                    Index = i,
                    DebugLabel = string.IsNullOrEmpty(debugLabel) 
                        ? $"neuron_{i}" 
                        : $"{debugLabel}_n{i}",
                    
                    // Procedurally generated properties (deterministic from pattern)
                    ActivationThreshold = 0.3 + rng.NextDouble() * 0.4,  // 0.3-0.7
                    DecayRate = 0.9 + rng.NextDouble() * 0.09,            // 0.9-0.99
                    RefractoryPeriod = rng.Next(1, 5),                    // 1-4 timesteps
                    
                    // Specialized role based on position in cluster
                    Role = DetermineNeuronRole(i, neuronCount, rng)
                };
                
                neurons.Add(neuron);
            }
            
            return neurons;
        }
        
        /// <summary>
        /// Get deterministic seed from feature vector
        /// Same features → same seed (reproducibility)
        /// </summary>
        private int GetPatternSeed(double[] featureVector)
        {
            unchecked
            {
                int hash = _seed;
                foreach (var value in featureVector)
                {
                    // Convert to bits for stable hashing
                    var bits = BitConverter.DoubleToInt64Bits(value);
                    hash = hash * 31 + (int)(bits ^ (bits >> 32));
                }
                return Math.Abs(hash);
            }
        }
        
        /// <summary>
        /// Determine neuron's specialized role within cluster
        /// Early neurons: Input receivers
        /// Middle neurons: Pattern integrators
        /// Late neurons: Output generators
        /// </summary>
        private NeuronRole DetermineNeuronRole(int index, int totalCount, Random rng)
        {
            var position = index / (double)totalCount;
            
            if (position < 0.2)
            {
                // First 20%: Input layer
                return NeuronRole.InputReceiver;
            }
            else if (position < 0.8)
            {
                // Middle 60%: Integration/processing
                return rng.NextDouble() < 0.3 
                    ? NeuronRole.PatternDetector 
                    : NeuronRole.Integrator;
            }
            else
            {
                // Last 20%: Output layer
                return NeuronRole.OutputGenerator;
            }
        }
        
        /// <summary>
        /// Get hypernetwork parameters for storage
        /// </summary>
        public HypernetworkParameters GetParameters()
        {
            return new HypernetworkParameters
            {
                AlphaFrequency = _alphaFrequency,
                BetaNovelty = _betaNovelty,
                GammaComplexity = _gammaComplexity,
                MinNeurons = _minNeurons,
                MaxNeurons = _maxNeurons,
                Seed = _seed
            };
        }
    }
    
    /// <summary>
    /// Properties for a single procedurally-generated neuron
    /// </summary>
    public class NeuronProperties
    {
        public int Index { get; set; }
        public string DebugLabel { get; set; } = "";
        public double ActivationThreshold { get; set; }
        public double DecayRate { get; set; }
        public int RefractoryPeriod { get; set; }
        public NeuronRole Role { get; set; }
    }
    
    /// <summary>
    /// Specialized roles within a cluster
    /// </summary>
    public enum NeuronRole
    {
        InputReceiver,      // Receives external input
        PatternDetector,    // Detects specific sub-patterns
        Integrator,         // Combines multiple signals
        OutputGenerator     // Generates cluster output
    }
    
    /// <summary>
    /// Hypernetwork configuration for persistence
    /// </summary>
    public class HypernetworkParameters
    {
        public double AlphaFrequency { get; set; }
        public double BetaNovelty { get; set; }
        public double GammaComplexity { get; set; }
        public int MinNeurons { get; set; }
        public int MaxNeurons { get; set; }
        public int Seed { get; set; }
    }
}
