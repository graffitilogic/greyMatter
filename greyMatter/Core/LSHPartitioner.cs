using System;
using System.Collections.Generic;
using System.Linq;

namespace GreyMatter.Core
{
    /// <summary>
    /// Phase 1: Locality-Sensitive Hashing (LSH) for feature space partitioning
    /// Maps high-dimensional feature vectors to discrete region IDs
    /// 
    /// Key property: Similar vectors → same or nearby regions
    /// Future upgrade: Replace with VQ-VAE learned codebook
    /// </summary>
    public class LSHPartitioner
    {
        private readonly int _dimensions;
        private readonly int _numBands;
        private readonly int _rowsPerBand;
        private readonly List<double[][]> _randomProjections;
        
        /// <summary>
        /// Initialize LSH with specified parameters
        /// </summary>
        /// <param name="dimensions">Feature vector dimensionality (e.g., 128)</param>
        /// <param name="numBands">Number of hash bands (more = finer granularity)</param>
        /// <param name="rowsPerBand">Rows per band (more = stricter similarity)</param>
        public LSHPartitioner(int dimensions = 128, int numBands = 16, int rowsPerBand = 4)
        {
            _dimensions = dimensions;
            _numBands = numBands;
            _rowsPerBand = rowsPerBand;
            _randomProjections = new List<double[][]>();
            
            // Generate random projection matrices (deterministic from seed)
            InitializeProjections(seed: 42);
        }

        /// <summary>
        /// Get primary region ID for a feature vector
        /// </summary>
        public string GetRegionId(double[] featureVector)
        {
            if (featureVector.Length != _dimensions)
                throw new ArgumentException($"Expected {_dimensions}-dim vector, got {featureVector.Length}");

            // Combine all band hashes into single region ID
            var bandHashes = new List<string>();
            for (int band = 0; band < _numBands; band++)
            {
                var bandHash = ComputeBandHash(featureVector, band);
                bandHashes.Add(bandHash.ToString("X4"));
            }
            
            return string.Join("_", bandHashes);
        }

        /// <summary>
        /// Get k nearest regions (for similarity search)
        /// Returns regions in order of likely similarity
        /// </summary>
        public List<string> GetNearbyRegions(double[] featureVector, int neighbors = 5)
        {
            if (featureVector.Length != _dimensions)
                throw new ArgumentException($"Expected {_dimensions}-dim vector, got {featureVector.Length}");

            var regions = new HashSet<string>();
            
            // Primary region
            var primaryRegion = GetRegionId(featureVector);
            regions.Add(primaryRegion);
            
            // Generate similar regions by perturbing band hashes
            // Strategy: Flip bits in band hashes to get nearby buckets
            for (int band = 0; band < Math.Min(_numBands, neighbors); band++)
            {
                var bandHashes = new List<string>();
                for (int b = 0; b < _numBands; b++)
                {
                    var hash = ComputeBandHash(featureVector, b);
                    
                    // Perturb this band's hash if it's the target band
                    if (b == band)
                    {
                        hash ^= 0x1; // Flip low bit (creates nearby bucket)
                    }
                    
                    bandHashes.Add(hash.ToString("X4"));
                }
                
                var similarRegion = string.Join("_", bandHashes);
                regions.Add(similarRegion);
                
                if (regions.Count >= neighbors)
                    break;
            }
            
            return regions.ToList();
        }

        /// <summary>
        /// Compute similarity between two feature vectors
        /// Returns cosine similarity ∈ [-1, 1]
        /// </summary>
        public double CosineSimilarity(double[] v1, double[] v2)
        {
            if (v1.Length != v2.Length)
                throw new ArgumentException("Vectors must have same dimensionality");

            double dot = 0;
            double mag1 = 0;
            double mag2 = 0;
            
            for (int i = 0; i < v1.Length; i++)
            {
                dot += v1[i] * v2[i];
                mag1 += v1[i] * v1[i];
                mag2 += v2[i] * v2[i];
            }
            
            var magnitude = Math.Sqrt(mag1) * Math.Sqrt(mag2);
            return magnitude > 0 ? dot / magnitude : 0;
        }

        /// <summary>
        /// Check if two vectors likely belong to same/nearby regions
        /// </summary>
        public bool AreSimilar(double[] v1, double[] v2, double threshold = 0.5)
        {
            var similarity = CosineSimilarity(v1, v2);
            return similarity >= threshold;
        }

        private void InitializeProjections(int seed)
        {
            // Create deterministic random projection matrices
            // Each band has rowsPerBand random hyperplanes
            var rng = new Random(seed);
            
            for (int band = 0; band < _numBands; band++)
            {
                var bandProjections = new double[_rowsPerBand][];
                for (int row = 0; row < _rowsPerBand; row++)
                {
                    bandProjections[row] = GenerateRandomVector(rng, _dimensions);
                }
                _randomProjections.Add(bandProjections);
            }
        }

        private double[] GenerateRandomVector(Random rng, int dimensions)
        {
            // Generate random unit vector (for projection)
            var vector = new double[dimensions];
            double magnitude = 0;
            
            for (int i = 0; i < dimensions; i++)
            {
                // Box-Muller transform for Gaussian distribution
                var u1 = rng.NextDouble();
                var u2 = rng.NextDouble();
                var normal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                vector[i] = normal;
                magnitude += normal * normal;
            }
            
            // Normalize to unit length
            magnitude = Math.Sqrt(magnitude);
            for (int i = 0; i < dimensions; i++)
            {
                vector[i] /= magnitude;
            }
            
            return vector;
        }

        private int ComputeBandHash(double[] featureVector, int bandIndex)
        {
            var projections = _randomProjections[bandIndex];
            var hash = 0;
            
            for (int row = 0; row < _rowsPerBand; row++)
            {
                // Project feature vector onto random hyperplane
                var projection = DotProduct(featureVector, projections[row]);
                
                // Hash bit: 1 if projection > 0, else 0
                var bit = projection > 0 ? 1 : 0;
                hash = (hash << 1) | bit;
            }
            
            return hash;
        }

        private double DotProduct(double[] v1, double[] v2)
        {
            double sum = 0;
            for (int i = 0; i < v1.Length; i++)
            {
                sum += v1[i] * v2[i];
            }
            return sum;
        }

        /// <summary>
        /// Get statistics about region distribution (for diagnostics)
        /// </summary>
        public string GetStats()
        {
            return $"LSH: {_numBands} bands × {_rowsPerBand} rows/band = {_numBands * _rowsPerBand} hash bits\n" +
                   $"     Theoretical buckets: 2^{_numBands * _rowsPerBand} = {Math.Pow(2, _numBands * _rowsPerBand):E2}\n" +
                   $"     Dimensions: {_dimensions}";
        }
    }
}
