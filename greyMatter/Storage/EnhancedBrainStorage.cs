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

        // New: Global neuron store per partition (Phase B)
        private readonly GlobalNeuronStore _globalNeuronStore;

        public EnhancedBrainStorage(string basePath = "brain_data") : base(basePath)
        {
            _partitioner = new NeuroPartitioner();
            _hierarchicalBasePath = Path.Combine(basePath, "hierarchical");
            EnsureHierarchicalStructure();
            _globalNeuronStore = new GlobalNeuronStore(_hierarchicalBasePath);
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
                    try { await SaveClusterMembershipOnlyAsync(cluster, context); }
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
        /// Save cluster using neurobiologically-inspired partitioning (full save: membership + bank upsert)
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
            
            // Get neurons and create membership list
            var neurons = await cluster.GetNeuronsAsync();
            var neuronIds = neurons.Keys.ToList();
            
            // Save membership + metadata only (neurons stored in per-partition bank)
            var clusterData = new PartitionedClusterData
            {
                NeuronIds = neuronIds,
                Neurons = new List<NeuronSnapshot>(),
                PartitionPath = clusterPartition,
                Metadata = CreateClusterMetadata(cluster, clusterPartition),
                SavedAt = DateTime.UtcNow
            };
            
            await WriteJsonFastAsync(fullPath, clusterData, CompressClusters);

            // Upsert neurons into partition bank (shared store)
            await _globalNeuronStore.SaveOrUpdateNeuronsAsync(clusterPartition, neurons.Values);
            
            // Update partition metadata
            await UpdatePartitionMetadata(clusterPartition, cluster);
        }

        /// <summary>
        /// Save only the cluster membership and metadata (no neuron bank updates).
        /// </summary>
        public async Task SaveClusterMembershipOnlyAsync(NeuronCluster cluster, BrainContext context)
        {
            var clusterPartition = await AnalyzeClusterPartition(cluster, context);
            var hierarchicalPath = Path.Combine(_hierarchicalBasePath, clusterPartition.FullPath);
            Directory.CreateDirectory(hierarchicalPath);

            var clusterFileName = $"{cluster.ConceptDomain}_{cluster.ClusterId:N}.cluster";
            var fullPath = Path.Combine(hierarchicalPath, clusterFileName);

            var neurons = await cluster.GetNeuronsAsync();
            var neuronIds = neurons.Keys.ToList();

            var clusterData = new PartitionedClusterData
            {
                NeuronIds = neuronIds,
                Neurons = new List<NeuronSnapshot>(),
                PartitionPath = clusterPartition,
                Metadata = CreateClusterMetadata(cluster, clusterPartition),
                SavedAt = DateTime.UtcNow
            };

            await WriteJsonFastAsync(fullPath, clusterData, CompressClusters);
            await UpdatePartitionMetadata(clusterPartition, cluster);

            // Clear dirty flag since membership/metadata saved
            cluster.MarkMembershipPersisted();
        }

        /// <summary>
        /// Save only neuron bank updates for a cluster (changed neurons only, no membership write).
        /// </summary>
        public async Task SaveClusterBankOnlyAsync(NeuronCluster cluster, IEnumerable<HybridNeuron> changedNeurons, BrainContext context)
        {
            var clusterPartition = await AnalyzeClusterPartition(cluster, context);
            // Upsert ONLY the changed neurons to reduce write amplification
            await _globalNeuronStore.SaveOrUpdateNeuronsAsync(clusterPartition, changedNeurons);
        }

        /// <summary>
        /// Batch-update neuron banks by grouping changes per partition to minimize rewrites.
        /// </summary>
        public async Task SaveNeuronBanksInBatchesAsync(IEnumerable<(NeuronCluster Cluster, IEnumerable<HybridNeuron> Changed)> changes, BrainContext context)
        {
            // Group by partition path string
            var byPartition = new Dictionary<string, (PartitionPath Path, List<HybridNeuron> Neurons)>();
            foreach (var (cluster, changed) in changes)
            {
                var partition = await AnalyzeClusterPartition(cluster, context);
                var key = partition.FullPath;
                if (!byPartition.TryGetValue(key, out var entry))
                {
                    entry = (partition, new List<HybridNeuron>());
                    byPartition[key] = entry;
                }
                // Deduplicate by neuron Id within the partition batch
                var existing = entry.Neurons.ToDictionary(n => n.Id, n => n);
                foreach (var n in changed)
                {
                    existing[n.Id] = n;
                }
                byPartition[key] = (entry.Path, existing.Values.ToList());
            }

            var sem = new SemaphoreSlim(MaxParallelSaves);
            var tasks = new List<Task>();
            foreach (var kvp in byPartition.Values)
            {
                await sem.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try { await _globalNeuronStore.SaveOrUpdateNeuronsAsync(kvp.Path, kvp.Neurons); }
                    finally { sem.Release(); }
                }));
            }
            await Task.WhenAll(tasks);
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
                var fullPathDir = Path.Combine(_hierarchicalBasePath, searchPath);
                if (!Directory.Exists(fullPathDir)) continue;

                // Match both plain and gz files
                var files = Directory.GetFiles(fullPathDir, $"*{clusterIdentifier}*.cluster*");
                if (!files.Any()) continue;

                var file = files.First();
                PartitionedClusterData? data = null;
                if (file.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                {
                    await using var fs = File.OpenRead(file);
                    await using var gz = new GZipStream(fs, CompressionMode.Decompress);
                    data = await JsonSerializer.DeserializeAsync<PartitionedClusterData>(gz, GetJsonOptions());
                }
                else
                {
                    var json = await File.ReadAllTextAsync(file);
                    data = JsonSerializer.Deserialize<PartitionedClusterData>(json, GetJsonOptions());
                }

                if (data == null) continue;

                // If membership-only, hydrate neurons from bank
                if ((data.Neurons == null || data.Neurons.Count == 0) && data.NeuronIds != null && data.NeuronIds.Count > 0)
                {
                    var loaded = await _globalNeuronStore.LoadNeuronsAsync(data.PartitionPath, data.NeuronIds);
                    data.Neurons = loaded.Values.Select(n => n.CreateSnapshot()).ToList();
                }

                return data;
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

        /// <summary>
        /// Inspect a cluster's membership vs hydrated neuron count from the bank.
        /// Returns (membershipCount, hydratedCount). If file missing, returns (0,0).
        /// </summary>
        public async Task<(int membershipCount, int hydratedCount)> InspectClusterMembershipAsync(NeuronCluster cluster, BrainContext context)
        {
            // Try find partition from metadata first
            PartitionPath partition;
            if (_partitionMetadata.TryGetValue(cluster.ClusterId.ToString(), out var meta))
            {
                partition = meta.PartitionPath;
            }
            else
            {
                partition = await AnalyzeClusterPartition(cluster, context);
            }

            var dir = Path.Combine(_hierarchicalBasePath, partition.FullPath);
            var baseName = $"{cluster.ConceptDomain}_{cluster.ClusterId:N}.cluster";
            var pathJson = Path.Combine(dir, baseName);
            var pathGz = pathJson + ".gz";

            PartitionedClusterData? data = null;
            if (File.Exists(pathGz))
            {
                await using var fs = File.OpenRead(pathGz);
                await using var gz = new GZipStream(fs, CompressionMode.Decompress);
                data = await JsonSerializer.DeserializeAsync<PartitionedClusterData>(gz, GetJsonOptions());
            }
            else if (File.Exists(pathJson))
            {
                var json = await File.ReadAllTextAsync(pathJson);
                data = JsonSerializer.Deserialize<PartitionedClusterData>(json, GetJsonOptions());
            }

            if (data == null)
                return (0, 0);

            var ids = data.NeuronIds ?? new List<Guid>();
            var hydrated = await _globalNeuronStore.LoadNeuronsAsync(partition, ids);
            return (ids.Count, hydrated.Count);
        }

        private string GetConceptCapacityPath() => Path.Combine(_hierarchicalBasePath, "concept_capacity.json");

        /// <summary>
        /// Load persisted concept capacity targets. Returns empty if not present.
        /// </summary>
        public async Task<Dictionary<string, int>> LoadConceptCapacitiesAsync()
        {
            var path = GetConceptCapacityPath();
            if (!File.Exists(path)) return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var json = await File.ReadAllTextAsync(path);
                var dict = JsonSerializer.Deserialize<Dictionary<string, int>>(json, GetJsonOptions())
                           ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                return new Dictionary<string, int>(dict, StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Persist concept capacity targets atomically.
        /// </summary>
        public async Task SaveConceptCapacitiesAsync(Dictionary<string, int> capacities)
        {
            var path = GetConceptCapacityPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(capacities, GetJsonOptions());
            var tmp = path + ".tmp";
            await File.WriteAllTextAsync(tmp, json);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path, overwrite: true);
        }
    }

    /// <summary>
    /// Enhanced cluster data with partition information
    /// </summary>
    public class PartitionedClusterData
    {
        public List<NeuronSnapshot> Neurons { get; set; } = new();
        public List<Guid> NeuronIds { get; set; } = new();
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
