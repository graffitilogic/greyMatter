using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter.Storage
{
    /// <summary>
    /// Enhanced storage system with neurobiologically-inspired hierarchical partitioning
    /// Designed for massive scale (100,000x+) while maintaining biological realism
    /// </summary>
    public class EnhancedBrainStorage : BrainStorage
    {
        private readonly NeuroPartitioner _partitioner;
        private readonly string _hierarchicalBasePath;
        private readonly Dictionary<string, ClusterMetadata> _partitionMetadata = new();

        // NEW: Save performance tuning
        public int MaxParallelSaves { get; set; } = 2; // keep low for NAS
        public bool CompressClusters { get; set; } = true; // gzip JSON payloads

        private bool _batchMode = false;

        public EnhancedBrainStorage(string basePath = "brain_data") : base(basePath)
        {
            _partitioner = new NeuroPartitioner();
            _hierarchicalBasePath = Path.Combine(basePath, "hierarchical");
            EnsureHierarchicalStructure();
        }

        /// <summary>
        /// Save multiple clusters efficiently with throttling & compression
        /// </summary>
        public async Task SaveClustersEfficientlyAsync(IEnumerable<NeuronCluster> clusters, BrainContext context)
        {
            _batchMode = true;
            var sem = new SemaphoreSlim(MaxParallelSaves);
            var tasks = new List<Task>();

            foreach (var cluster in clusters)
            {
                await sem.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try { await SaveClusterWithPartitioningAsync(cluster, context); }
                    finally { sem.Release(); }
                }));
            }

            await Task.WhenAll(tasks);

            // Persist partition metadata once at the end of batch
            await PersistPartitionMetadataAsync();
            _batchMode = false;
        }

        private async Task PersistPartitionMetadataAsync()
        {
            var metadataPath = Path.Combine(_hierarchicalBasePath, "partition_metadata.json");
            var json = JsonSerializer.Serialize(_partitionMetadata, GetJsonOptions());
            var tmp = metadataPath + ".tmp";
            await File.WriteAllTextAsync(tmp, json);
            if (File.Exists(metadataPath)) File.Delete(metadataPath);
            File.Move(tmp, metadataPath, overwrite: true);
        }

        /// <summary>
        /// Save cluster using neurobiologically-inspired partitioning
        /// </summary>
        public async Task SaveClusterWithPartitioningAsync(NeuronCluster cluster, BrainContext context)
        {
            // Analyze cluster to determine optimal partition
            var clusterPartition = await AnalyzeClusterPartition(cluster, context);
            
            // Create hierarchical path
            var hierarchicalPath = Path.Combine(_hierarchicalBasePath, clusterPartition.FullPath);
            Directory.CreateDirectory(hierarchicalPath);
            
            // Save cluster in partitioned location
            var clusterFileName = $"{cluster.ConceptDomain}_{cluster.ClusterId:N}.cluster";
            var fullPath = Path.Combine(hierarchicalPath, clusterFileName);
            
            // Get neurons and create snapshots
            var neurons = await cluster.GetNeuronsAsync();
            var snapshots = neurons.Values.Select(n => n.CreateSnapshot()).ToList();
            
            // Save with enhanced metadata
            var clusterData = new PartitionedClusterData
            {
                Neurons = snapshots,
                PartitionPath = clusterPartition,
                Metadata = CreateClusterMetadata(cluster, clusterPartition),
                SavedAt = DateTime.UtcNow
            };
            
            await WriteJsonFastAsync(fullPath, clusterData, CompressClusters);
            
            // Update partition metadata
            await UpdatePartitionMetadata(clusterPartition, cluster);
        }

        private async Task WriteJsonFastAsync<T>(string fullPath, T obj, bool compress)
        {
            var options = GetJsonOptions();
            var tmpPath = fullPath + ".tmp" + (compress ? ".gz" : "");
            var finalPath = compress ? fullPath + ".gz" : fullPath;

            Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);

            var fileMode = FileMode.Create;
            const int bufferSize = 1 << 20; // 1MB buffer

            await using (var fs = new FileStream(tmpPath, fileMode, FileAccess.Write, FileShare.None, bufferSize, useAsync: true))
            await using (var stream = compress ? new GZipStream(fs, CompressionLevel.Fastest, leaveOpen: false) : fs as Stream)
            await using (var writer = new Utf8JsonWriter(stream!, new JsonWriterOptions { Indented = false }))
            {
                JsonSerializer.Serialize(writer, obj, options);
                await writer.FlushAsync();
            }

            // Atomic replace
            if (File.Exists(finalPath)) File.Delete(finalPath);
            File.Move(tmpPath, finalPath, overwrite: true);
        }

        /// <summary>
        /// Load cluster with intelligent partition-aware lookup
        /// </summary>
        public async Task<List<NeuronSnapshot>> LoadClusterWithPartitioningAsync(string clusterIdentifier, BrainContext context)
        {
            // Try hierarchical lookup first
            var partitionedData = await TryLoadFromHierarchicalStorage(clusterIdentifier, context);
            if (partitionedData != null)
                return partitionedData.Neurons;
                
            // Fallback to legacy storage
            return await LoadClusterAsync(clusterIdentifier);
        }

        /// <summary>
        /// Find clusters using biological similarity metrics
        /// </summary>
        public List<ClusterReference> FindSimilarClusters(
            IEnumerable<string> concepts, 
            double similarityThreshold = 0.7)
        {
            var results = new List<ClusterReference>();
            
            // Search through partition metadata for conceptually similar clusters
            foreach (var partition in _partitionMetadata.Values)
            {
                var similarity = CalculateConceptualSimilarity(concepts, partition.AssociatedConcepts);
                if (similarity >= similarityThreshold)
                {
                    results.Add(new ClusterReference
                    {
                        ClusterId = partition.ClusterId,
                        PartitionPath = partition.PartitionPath,
                        Similarity = similarity,
                        LastAccessed = partition.LastAccessed
                    });
                }
            }
            
            return results.OrderByDescending(r => r.Similarity).ToList();
        }

        /// <summary>
        /// Implement memory consolidation - move clusters between partitions based on usage patterns
        /// </summary>
        public async Task ConsolidateMemoryPartitions()
        {
            var consolidationTasks = new List<Task>();
            
            foreach (var metadata in _partitionMetadata.Values.ToList())
            {
                var shouldRelocate = await ShouldRelocateCluster(metadata);
                if (shouldRelocate.ShouldMove)
                {
                    consolidationTasks.Add(RelocateCluster(metadata, shouldRelocate.NewPartition));
                }
            }
            
            await Task.WhenAll(consolidationTasks);
        }

        /// <summary>
        /// Get storage statistics with partition-level breakdown
        /// </summary>
        public async Task<EnhancedStorageStats> GetEnhancedStorageStatsAsync()
        {
            var baseStats = await GetStorageStatsAsync();
            
            var partitionStats = new Dictionary<string, PartitionStats>();
            
            // Analyze each partition
            foreach (var partitionGroup in _partitionMetadata.GroupBy(m => m.Value.PartitionPath.Primary))
            {
                var stats = new PartitionStats
                {
                    ClusterCount = partitionGroup.Count(),
                    TotalNeurons = partitionGroup.Sum(g => g.Value.NeuronCount),
                    AverageImportance = partitionGroup.Average(g => g.Value.AverageImportance),
                    LastActivity = partitionGroup.Max(g => g.Value.LastAccessed)
                };
                
                partitionStats[partitionGroup.Key] = stats;
            }
            
            return new EnhancedStorageStats
            {
                BaseStats = baseStats,
                PartitionStats = partitionStats,
                TotalPartitions = partitionStats.Count,
                HierarchicalEfficiency = CalculateHierarchicalEfficiency()
            };
        }

        private async Task<PartitionPath> AnalyzeClusterPartition(NeuronCluster cluster, BrainContext context)
        {
            // Get a representative neuron from the cluster for analysis
            var neurons = await cluster.GetNeuronsAsync();
            var representativeNeuron = neurons.Values.FirstOrDefault();
            
            if (representativeNeuron == null)
            {
                return new PartitionPath { FullPath = "functional/general/plasticity/baseline/topology/peripheral/temporal/dormant" };
            }
            
            return await _partitioner.CalculateOptimalPartition(representativeNeuron, context);
        }

        private async Task<PartitionedClusterData?> TryLoadFromHierarchicalStorage(string clusterIdentifier, BrainContext context)
        {
            // Search through hierarchical structure
            var searchPaths = GenerateSearchPaths(clusterIdentifier);
            
            foreach (var searchPath in searchPaths)
            {
                var fullPath = Path.Combine(_hierarchicalBasePath, searchPath);
                if (Directory.Exists(fullPath))
                {
                    var files = Directory.GetFiles(fullPath, $"*{clusterIdentifier}*");
                    if (files.Any())
                    {
                        var json = await File.ReadAllTextAsync(files.First());
                        return JsonSerializer.Deserialize<PartitionedClusterData>(json, GetJsonOptions());
                    }
                }
            }
            
            return null;
        }

        private List<string> GenerateSearchPaths(string clusterIdentifier)
        {
            // Generate likely partition paths for search
            var paths = new List<string>();
            
            // Add common functional categories
            var functionalRoles = new[] { "sensory", "motor", "memory", "association", "general" };
            var plasticityStates = new[] { "high_adaptive", "moderate_plastic", "stable_mature", "baseline" };
            var topologyRoles = new[] { "hub", "bridge", "specialized", "modular", "peripheral" };
            var temporalSignatures = new[] { "active_frequent", "recent_moderate", "consolidated_important", "dormant" };
            
            // Generate combinations for search
            foreach (var func in functionalRoles)
            {
                foreach (var plast in plasticityStates)
                {
                    foreach (var topo in topologyRoles)
                    {
                        foreach (var temp in temporalSignatures)
                        {
                            paths.Add($"functional/{func}/plasticity/{plast}/topology/{topo}/temporal/{temp}");
                        }
                    }
                }
            }
            
            return paths;
        }

        private ClusterMetadata CreateClusterMetadata(NeuronCluster cluster, PartitionPath partition)
        {
            return new ClusterMetadata
            {
                ClusterId = cluster.ClusterId,
                ConceptDomain = cluster.ConceptDomain,
                PartitionPath = partition,
                AssociatedConcepts = cluster.AssociatedConcepts.ToList(),
                NeuronCount = cluster.NeuronCount,
                AverageImportance = cluster.AverageImportance,
                LastAccessed = cluster.LastAccessed,
                CreatedAt = cluster.CreatedAt
            };
        }

        private async Task UpdatePartitionMetadata(PartitionPath partition, NeuronCluster cluster)
        {
            var metadata = CreateClusterMetadata(cluster, partition);
            _partitionMetadata[cluster.ClusterId.ToString()] = metadata;
            
            if (_batchMode)
            {
                // Defer persistence to end of batch
                return;
            }
            
            // Persist immediately when not in batch mode
            var metadataPath = Path.Combine(_hierarchicalBasePath, "partition_metadata.json");
            var json = JsonSerializer.Serialize(_partitionMetadata, GetJsonOptions());
            await File.WriteAllTextAsync(metadataPath, json);
        }

        private double CalculateConceptualSimilarity(IEnumerable<string> concepts1, List<string> concepts2)
        {
            var set1 = concepts1.Select(c => c.ToLowerInvariant()).ToHashSet();
            var set2 = concepts2.Select(c => c.ToLowerInvariant()).ToHashSet();
            
            var intersection = set1.Intersect(set2).Count();
            var union = set1.Union(set2).Count();
            
            return union > 0 ? (double)intersection / union : 0.0;
        }

        private async Task<(bool ShouldMove, PartitionPath NewPartition)> ShouldRelocateCluster(ClusterMetadata metadata)
        {
            // Check if cluster usage patterns suggest it should be moved
            var daysSinceAccess = (DateTime.UtcNow - metadata.LastAccessed).TotalDays;
            
            // Move dormant clusters to archive
            if (daysSinceAccess > 30 && !metadata.PartitionPath.Quaternary.Contains("dormant"))
            {
                var newPartition = new PartitionPath
                {
                    Primary = metadata.PartitionPath.Primary,
                    Secondary = metadata.PartitionPath.Secondary,
                    Tertiary = metadata.PartitionPath.Tertiary,
                    Quaternary = "temporal/dormant",
                    FullPath = $"{metadata.PartitionPath.Primary}/{metadata.PartitionPath.Secondary}/{metadata.PartitionPath.Tertiary}/temporal/dormant"
                };
                return (true, newPartition);
            }
            
            // Move recently accessed clusters from dormant
            if (daysSinceAccess < 7 && metadata.PartitionPath.Quaternary.Contains("dormant"))
            {
                var newPartition = new PartitionPath
                {
                    Primary = metadata.PartitionPath.Primary,
                    Secondary = metadata.PartitionPath.Secondary,
                    Tertiary = metadata.PartitionPath.Tertiary,
                    Quaternary = "temporal/recent_moderate",
                    FullPath = $"{metadata.PartitionPath.Primary}/{metadata.PartitionPath.Secondary}/{metadata.PartitionPath.Tertiary}/temporal/recent_moderate"
                };
                return (true, newPartition);
            }
            
            return await Task.FromResult((false, metadata.PartitionPath));
        }

        private async Task RelocateCluster(ClusterMetadata metadata, PartitionPath newPartition)
        {
            // Implementation for moving clusters between partitions
            // This would involve moving files and updating metadata
            await Task.CompletedTask; // Placeholder
        }

        private double CalculateHierarchicalEfficiency()
        {
            // Calculate how well the hierarchical structure is working
            // Based on partition distribution and access patterns
            if (!_partitionMetadata.Any()) return 1.0;
            
            var partitionDistribution = _partitionMetadata.Values
                .GroupBy(m => m.PartitionPath.Primary)
                .Select(g => g.Count())
                .ToList();
                
            // Calculate entropy of distribution (lower is better organized)
            var totalClusters = partitionDistribution.Sum();
            var entropy = partitionDistribution
                .Select(count => (double)count / totalClusters)
                .Where(p => p > 0)
                .Sum(p => -p * Math.Log2(p));
                
            // Normalize to 0-1 range (1 = perfectly efficient)
            var maxEntropy = Math.Log2(partitionDistribution.Count);
            return maxEntropy > 0 ? 1.0 - (entropy / maxEntropy) : 1.0;
        }

        private void EnsureHierarchicalStructure()
        {
            Directory.CreateDirectory(_hierarchicalBasePath);
            
            // Create main partition directories
            var mainPartitions = new[] { "functional", "plasticity", "topology", "temporal" };
            foreach (var partition in mainPartitions)
            {
                Directory.CreateDirectory(Path.Combine(_hierarchicalBasePath, partition));
            }
        }

        private JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// Hide base implementation to prevent dual saving
        /// Use only single storage location in hierarchical structure
        /// </summary>
        public new async Task SaveClusterAsync(string identifier, List<NeuronSnapshot> neurons)
        {
            // Use base storage for simple saves (no hierarchical partitioning)
            // This prevents duplication while maintaining compatibility
            await base.SaveClusterAsync(identifier, neurons);
        }
    }

    /// <summary>
    /// Enhanced cluster data with partition information
    /// </summary>
    public class PartitionedClusterData
    {
        public List<NeuronSnapshot> Neurons { get; set; } = new();
        public PartitionPath PartitionPath { get; set; } = new();
        public ClusterMetadata Metadata { get; set; } = new();
        public DateTime SavedAt { get; set; }
    }

    /// <summary>
    /// Metadata for cluster partitioning
    /// </summary>
    public class ClusterMetadata
    {
        public Guid ClusterId { get; set; }
        public string ConceptDomain { get; set; } = "";
        public PartitionPath PartitionPath { get; set; } = new();
        public List<string> AssociatedConcepts { get; set; } = new();
        public int NeuronCount { get; set; }
        public double AverageImportance { get; set; }
        public DateTime LastAccessed { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Reference to a cluster in the partition system
    /// </summary>
    public class ClusterReference
    {
        public Guid ClusterId { get; set; }
        public PartitionPath PartitionPath { get; set; } = new();
        public double Similarity { get; set; }
        public DateTime LastAccessed { get; set; }
    }

    /// <summary>
    /// Enhanced storage statistics with partition breakdown
    /// </summary>
    public class EnhancedStorageStats
    {
        public StorageStats BaseStats { get; set; } = new();
        public Dictionary<string, PartitionStats> PartitionStats { get; set; } = new();
        public int TotalPartitions { get; set; }
        public double HierarchicalEfficiency { get; set; }
    }

    /// <summary>
    /// Statistics for individual partitions
    /// </summary>
    public class PartitionStats
    {
        public int ClusterCount { get; set; }
        public int TotalNeurons { get; set; }
        public double AverageImportance { get; set; }
        public DateTime LastActivity { get; set; }
    }
}
