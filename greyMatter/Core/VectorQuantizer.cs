using System;
using System.Collections.Generic;
using System.Linq;

namespace GreyMatter.Core
{
    /// <summary>
    /// VQ-VAE style vector quantization for discrete concept codes
    /// 
    /// Instead of fixed LSH hashing, learns a codebook of prototype vectors
    /// that capture the natural clustering structure of the feature space.
    /// 
    /// Key features:
    /// - Learned codebook (not random hash functions)
    /// - EMA updates for stability
    /// - Commitment loss to prevent codebook collapse
    /// - Perplexity tracking for codebook utilization
    /// </summary>
    public class VectorQuantizer
    {
        private readonly int _codebookSize;      // Number of discrete codes (e.g., 512)
        private readonly int _embeddingDim;      // Dimension of feature vectors (e.g., 128)
        private readonly float _commitment;      // β: Commitment loss weight (default: 0.25)
        private readonly float _emaDecay;        // γ: EMA decay for codebook updates (default: 0.99)
        
        // Codebook: [codebookSize x embeddingDim]
        private readonly float[][] _codebook;
        
        // EMA statistics for online codebook learning
        private readonly float[] _emaClusterSize;    // Running count per code
        private readonly float[][] _emaCodebookSum;  // Running sum of embeddings per code
        
        // Usage statistics
        private readonly int[] _usageCounts;     // How often each code is used
        private int _totalEncodings;             // Total number of encodings
        
        public VectorQuantizer(
            int codebookSize = 512,
            int embeddingDim = 128,
            float commitment = 0.25f,
            float emaDecay = 0.99f)
        {
            _codebookSize = codebookSize;
            _embeddingDim = embeddingDim;
            _commitment = commitment;
            _emaDecay = emaDecay;
            
            // Initialize codebook with random vectors (will be learned)
            _codebook = new float[codebookSize][];
            for (int i = 0; i < codebookSize; i++)
            {
                _codebook[i] = new float[embeddingDim];
                for (int j = 0; j < embeddingDim; j++)
                {
                    // Small random initialization
                    _codebook[i][j] = (float)(Random.Shared.NextDouble() * 0.02 - 0.01);
                }
            }
            
            // Initialize EMA statistics
            _emaClusterSize = new float[codebookSize];
            _emaCodebookSum = new float[codebookSize][];
            for (int i = 0; i < codebookSize; i++)
            {
                _emaClusterSize[i] = 0.0f;
                _emaCodebookSum[i] = new float[embeddingDim];
            }
            
            _usageCounts = new int[codebookSize];
            _totalEncodings = 0;
        }
        
        /// <summary>
        /// Quantize a feature vector to the nearest codebook entry
        /// Returns the discrete code ID
        /// </summary>
        public int Quantize(float[] embedding)
        {
            if (embedding.Length != _embeddingDim)
                throw new ArgumentException($"Expected {_embeddingDim}-dim vector, got {embedding.Length}");
            
            // Find nearest codebook entry (argmin ||z_e - e_k||²)
            int bestCode = 0;
            float bestDistance = float.MaxValue;
            
            for (int k = 0; k < _codebookSize; k++)
            {
                float distance = ComputeL2Distance(embedding, _codebook[k]);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCode = k;
                }
            }
            
            // Track usage
            _usageCounts[bestCode]++;
            _totalEncodings++;
            
            return bestCode;
        }
        
        /// <summary>
        /// Quantize and update codebook with EMA (Exponential Moving Average)
        /// This is the training-time operation
        /// </summary>
        public (int code, float commitmentLoss) QuantizeAndUpdate(float[] embedding)
        {
            int code = Quantize(embedding);
            
            // Compute commitment loss: β * ||z_e - sg[e_k]||²
            // (sg = stop gradient, so we only penalize the encoder)
            float commitmentLoss = _commitment * ComputeL2Distance(embedding, _codebook[code]);
            
            // EMA update for codebook
            UpdateCodebookEMA(code, embedding);
            
            return (code, commitmentLoss);
        }
        
        /// <summary>
        /// Get the codebook vector for a given code
        /// </summary>
        public float[] GetCodebookVector(int code)
        {
            if (code < 0 || code >= _codebookSize)
                throw new ArgumentException($"Code {code} out of range [0, {_codebookSize})");
            
            return (float[])_codebook[code].Clone();
        }
        
        /// <summary>
        /// Decode: map discrete code back to continuous embedding
        /// </summary>
        public float[] Decode(int code)
        {
            return GetCodebookVector(code);
        }
        
        /// <summary>
        /// Update codebook using exponential moving average
        /// More stable than direct gradient descent
        /// </summary>
        private void UpdateCodebookEMA(int code, float[] embedding)
        {
            // Update cluster size: N_k = γ * N_k + (1 - γ) * 1
            _emaClusterSize[code] = _emaDecay * _emaClusterSize[code] + (1 - _emaDecay);
            
            // Update cluster sum: m_k = γ * m_k + (1 - γ) * z_e
            for (int i = 0; i < _embeddingDim; i++)
            {
                _emaCodebookSum[code][i] = _emaDecay * _emaCodebookSum[code][i] 
                                          + (1 - _emaDecay) * embedding[i];
            }
            
            // Update codebook: e_k = m_k / N_k
            // (with Laplace smoothing to prevent division by zero)
            float n = _emaClusterSize[code] + 1e-5f;
            for (int i = 0; i < _embeddingDim; i++)
            {
                _codebook[code][i] = _emaCodebookSum[code][i] / n;
            }
        }
        
        /// <summary>
        /// Compute L2 distance between two vectors
        /// </summary>
        private float ComputeL2Distance(float[] a, float[] b)
        {
            float sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                float diff = a[i] - b[i];
                sum += diff * diff;
            }
            return sum;
        }
        
        /// <summary>
        /// Get codebook utilization statistics
        /// </summary>
        public VQStats GetStats()
        {
            // Calculate perplexity: exp(entropy)
            // Measures effective codebook size
            float entropy = 0;
            if (_totalEncodings > 0)
            {
                for (int k = 0; k < _codebookSize; k++)
                {
                    if (_usageCounts[k] > 0)
                    {
                        float p = (float)_usageCounts[k] / _totalEncodings;
                        entropy -= p * MathF.Log(p);
                    }
                }
            }
            float perplexity = MathF.Exp(entropy);
            
            // Count active codes (used at least once)
            int activeCodes = _usageCounts.Count(c => c > 0);
            
            // Find most/least used codes
            int mostUsedCode = 0;
            int leastUsedCode = 0;
            int maxUsage = 0;
            int minUsage = int.MaxValue;
            
            for (int k = 0; k < _codebookSize; k++)
            {
                if (_usageCounts[k] > maxUsage)
                {
                    maxUsage = _usageCounts[k];
                    mostUsedCode = k;
                }
                if (_usageCounts[k] < minUsage)
                {
                    minUsage = _usageCounts[k];
                    leastUsedCode = k;
                }
            }
            
            return new VQStats
            {
                CodebookSize = _codebookSize,
                EmbeddingDim = _embeddingDim,
                ActiveCodes = activeCodes,
                TotalEncodings = _totalEncodings,
                Perplexity = perplexity,
                CodebookUtilization = (float)activeCodes / _codebookSize,
                MostUsedCode = mostUsedCode,
                MostUsedCount = maxUsage,
                LeastUsedCode = leastUsedCode,
                LeastUsedCount = minUsage
            };
        }
        
        /// <summary>
        /// Reset usage statistics (not codebook itself)
        /// </summary>
        public void ResetStats()
        {
            Array.Clear(_usageCounts, 0, _usageCounts.Length);
            _totalEncodings = 0;
        }
        
        /// <summary>
        /// Export codebook for persistence
        /// </summary>
        public CodebookSnapshot ExportCodebook()
        {
            return new CodebookSnapshot
            {
                CodebookSize = _codebookSize,
                EmbeddingDim = _embeddingDim,
                Commitment = _commitment,
                EmaDecay = _emaDecay,
                Codebook = _codebook.Select(v => (float[])v.Clone()).ToArray(),
                EmaClusterSize = (float[])_emaClusterSize.Clone(),
                EmaCodebookSum = _emaCodebookSum.Select(v => (float[])v.Clone()).ToArray(),
                UsageCounts = (int[])_usageCounts.Clone(),
                TotalEncodings = _totalEncodings
            };
        }
        
        /// <summary>
        /// Import codebook from persistence
        /// </summary>
        public void ImportCodebook(CodebookSnapshot snapshot)
        {
            if (snapshot.CodebookSize != _codebookSize || snapshot.EmbeddingDim != _embeddingDim)
                throw new ArgumentException("Codebook dimensions don't match");
            
            for (int i = 0; i < _codebookSize; i++)
            {
                Array.Copy(snapshot.Codebook[i], _codebook[i], _embeddingDim);
                Array.Copy(snapshot.EmaCodebookSum[i], _emaCodebookSum[i], _embeddingDim);
            }
            
            Array.Copy(snapshot.EmaClusterSize, _emaClusterSize, _codebookSize);
            Array.Copy(snapshot.UsageCounts, _usageCounts, _codebookSize);
            _totalEncodings = snapshot.TotalEncodings;
        }
        
        /// <summary>
        /// Get k-nearest codes for a given embedding
        /// Useful for multi-region activation
        /// </summary>
        public List<(int code, float distance)> GetNearestCodes(float[] embedding, int k = 5)
        {
            var distances = new List<(int code, float distance)>();
            
            for (int i = 0; i < _codebookSize; i++)
            {
                float distance = ComputeL2Distance(embedding, _codebook[i]);
                distances.Add((i, distance));
            }
            
            return distances.OrderBy(x => x.distance).Take(k).ToList();
        }
    }
    
    /// <summary>
    /// Statistics about codebook usage and learning
    /// </summary>
    public class VQStats
    {
        public int CodebookSize { get; set; }
        public int EmbeddingDim { get; set; }
        public int ActiveCodes { get; set; }
        public int TotalEncodings { get; set; }
        public float Perplexity { get; set; }
        public float CodebookUtilization { get; set; }
        public int MostUsedCode { get; set; }
        public int MostUsedCount { get; set; }
        public int LeastUsedCode { get; set; }
        public int LeastUsedCount { get; set; }
    }
    
    /// <summary>
    /// Snapshot for persisting codebook state
    /// </summary>
    public class CodebookSnapshot
    {
        public int CodebookSize { get; set; }
        public int EmbeddingDim { get; set; }
        public float Commitment { get; set; }
        public float EmaDecay { get; set; }
        public float[][] Codebook { get; set; } = Array.Empty<float[]>();
        public float[] EmaClusterSize { get; set; } = Array.Empty<float>();
        public float[][] EmaCodebookSum { get; set; } = Array.Empty<float[]>();
        public int[] UsageCounts { get; set; } = Array.Empty<int>();
        public int TotalEncodings { get; set; }
    }
}
