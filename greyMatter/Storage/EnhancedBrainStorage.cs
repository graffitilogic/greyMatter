using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GreyMatter.Core;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace GreyMatter.Storage
{
    /// <summary>
    /// Enhanced storage system with neurobiologically-inspired hierarchical partitioning
    /// Designed for massive scale (100,000x+) while maintaining biological realism
    /// </summary>
    public class EnhancedBrainStorage
    {
        private readonly NeuroPartitioner _partitioner;
        private readonly string _hierarchicalBasePath;
        private readonly Dictionary<string, ClusterMetadata> _partitionMetadata = new();
        private readonly object _metadataLock = new object();

        // NEW: Save performance tuning
        public int MaxParallelSaves { get; set; } = 2; // keep low for NAS
        public bool CompressClusters { get; set; } = true; // gzip JSON payloads

        private bool _batchMode = false;

        // New: Global neuron store per partition (Phase B)
        private readonly GlobalNeuronStore _globalNeuronStore;
        
        // Phase 6B: References for procedural neuron regeneration
        private VectorQuantizer? _vectorQuantizer;
        private FeatureEncoder? _featureEncoder;

        // Per-partition membership pack (clusterId -> neuronIds) to avoid many small writes
        private class MembershipPack
        {
            public Dictionary<string, List<Guid>> Membership { get; set; } = new();
            public DateTime SavedAt { get; set; } = DateTime.UtcNow;
        }

        // Fast lookup: concept -> clusters (built from metadata)
        private readonly Dictionary<string, List<ClusterReference>> _conceptIndex = new(StringComparer.OrdinalIgnoreCase);
        private bool _conceptIndexDirty = false;

        // Cache membership packs per partition to avoid repeated disk reads
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (DateTime loadedAt, MembershipPack pack)> _membershipPackCache2 = new();

        // Lightweight cached storage stats to avoid expensive NAS scans on startup
        private class StatsCache
        {
            public int ClusterCount { get; set; }
            public long BaseBytes { get; set; }
            public long HierarchicalBytes { get; set; }
            public DateTime LastUpdatedUtc { get; set; }
        }

        private StatsCache _statsCache = new StatsCache();
        private Task? _statsRefreshTask;
        private readonly object _statsLock = new();

        private string GetStatsCachePath() => Path.Combine(_hierarchicalBasePath, "storage_stats.json");

        private void TryLoadStatsCache()
        {
            try
            {
                var path = GetStatsCachePath();
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var sc = JsonSerializer.Deserialize<StatsCache>(json, GetJsonOptions());
                    if (sc != null) _statsCache = sc;
                }
            }
            catch { /* ignore cache errors */ }
        }

        private async Task SaveStatsCacheAsync()
        {
            try
            {
                var path = GetStatsCachePath();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                var json = JsonSerializer.Serialize(_statsCache, GetJsonOptions());
                await File.WriteAllTextAsync(path, json);
            }
            catch { /* ignore cache errors */ }
        }

        private void StartBackgroundStatsRefreshIfNeeded()
        {
            lock (_statsLock)
            {
                if (_statsRefreshTask != null && !_statsRefreshTask.IsCompleted) return;
                _statsRefreshTask = Task.Run(async () =>
                {
                    try
                    {
                        // TODO: Compute stats properly
                        // var baseStats = await base.GetStorageStatsAsync();
                        // Compute hierarchical size
                        long hierSize = 0;
                        try
                        {
                            if (Directory.Exists(_hierarchicalBasePath))
                            {
                                hierSize = new DirectoryInfo(_hierarchicalBasePath)
                                    .EnumerateFiles("*", SearchOption.AllDirectories)
                                    .Sum(f => f.Length);
                            }
                        }
                        catch { /* ignore */ }

                        _statsCache = new StatsCache
                        {
                            ClusterCount = _partitionMetadata.Count,
                            BaseBytes = 0, // TODO: Calculate properly
                            HierarchicalBytes = hierSize,
                            LastUpdatedUtc = DateTime.UtcNow
                        };
                        await SaveStatsCacheAsync();
                    }
                    catch { /* ignore */ }
                });
            }
        }

        public EnhancedBrainStorage(string basePath)
        {
            _partitioner = new NeuroPartitioner();
            _hierarchicalBasePath = Path.Combine(basePath, "hierarchical");
            EnsureHierarchicalStructure();
            _globalNeuronStore = new GlobalNeuronStore(_hierarchicalBasePath);
            // Load partition metadata if present
            TryLoadPartitionMetadata();
            // Load cached stats and kick a background refresh
            TryLoadStatsCache();
            StartBackgroundStatsRefreshIfNeeded();
        }

        /// <summary>
        /// Phase 6B: Attach VectorQuantizer and FeatureEncoder for procedural neuron regeneration
        /// </summary>
        public void AttachProceduralComponents(VectorQuantizer vectorQuantizer, FeatureEncoder featureEncoder)
        {
            _vectorQuantizer = vectorQuantizer;
            _featureEncoder = featureEncoder;
        }

        private void BuildConceptIndexFromMetadata()
        {
            _conceptIndex.Clear();
            foreach (var meta in _partitionMetadata.Values)
            {
                var cref = new ClusterReference
                {
                    ClusterId = meta.ClusterId,
                    PartitionPath = meta.PartitionPath,
                    Similarity = 1.0,
                    LastAccessed = meta.LastAccessed,
                    ConceptDomain = meta.ConceptDomain
                };
                foreach (var concept in meta.AssociatedConcepts)
                {
                    if (string.IsNullOrWhiteSpace(concept)) continue;
                    if (!_conceptIndex.TryGetValue(concept, out var list))
                    {
                        list = new List<ClusterReference>();
                        _conceptIndex[concept] = list;
                    }
                    // Avoid duplicates
                    if (!list.Any(x => x.ClusterId == cref.ClusterId))
                        list.Add(cref);
                }
            }
        }

        private void TryLoadPartitionMetadata()
        {
            try
            {
                var metadataPath = Path.Combine(_hierarchicalBasePath, "partition_metadata.json");
                if (File.Exists(metadataPath))
                {
                    var json = File.ReadAllText(metadataPath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, ClusterMetadata>>(json, GetJsonOptions());
                    if (dict != null)
                    {
                        lock (_metadataLock)
                        {
                            _partitionMetadata.Clear();
                            foreach (var kvp in dict)
                            {
                                var key = kvp.Key;
                                if (Guid.TryParse(key, out var gid))
                                {
                                    // Normalize to 'N' format
                                    _partitionMetadata[gid.ToString("N")] = kvp.Value;
                                }
                                else
                                {
                                    _partitionMetadata[key] = kvp.Value;
                                }
                            }
                            // Build inverted index once
                            BuildConceptIndexFromMetadata();
                            _conceptIndexDirty = false;
                        }
                    }
                }
            }
            catch { /* best-effort load */ }
        }

        private string GetMembershipPackPath(PartitionPath partition)
        {
            var dir = Path.Combine(_hierarchicalBasePath, partition.FullPath);
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "membership.pack.json.gz");
        }

        private async Task<MembershipPack> LoadMembershipPackAsync(PartitionPath partition)
        {
            var path = GetMembershipPackPath(partition);
            if (!File.Exists(path)) return new MembershipPack();
            try
            {
                await using var fs = File.OpenRead(path);
                await using var gz = new GZipStream(fs, CompressionMode.Decompress);
                var pack = await JsonSerializer.DeserializeAsync<MembershipPack>(gz, GetJsonOptions());
                pack ??= new MembershipPack();

                // Normalize keys to Guid "N" lowercase and de-duplicate Guid lists
                var normalized = new Dictionary<string, List<Guid>>();
                foreach (var kvp in pack.Membership)
                {
                    if (kvp.Key == null) continue;
                    var key = Guid.TryParse(kvp.Key, out var gid)
                        ? gid.ToString("N")
                        : kvp.Key.Trim().ToLowerInvariant();

                    var ids = kvp.Value ?? new List<Guid>();
                    if (ids.Count > 1)
                    {
                        var set = new HashSet<Guid>(ids.Where(g => g != Guid.Empty));
                        ids = set.ToList();
                    }
                    else if (ids.Count == 1 && ids[0] == Guid.Empty)
                    {
                        ids = new List<Guid>();
                    }

                    if (normalized.TryGetValue(key, out var existing))
                    {
                        // Merge with existing list, avoiding duplicates
                        var set = new HashSet<Guid>(existing);
                        foreach (var id in ids) set.Add(id);
                        normalized[key] = set.ToList();
                    }
                    else
                    {
                        normalized[key] = ids;
                    }
                }
                pack.Membership = normalized;
                return pack;
            }
            catch { return new MembershipPack(); }
        }

        private async Task SaveMembershipPackAsync(PartitionPath partition, MembershipPack pack)
        {
            var path = GetMembershipPackPath(partition);
            var tmp = path + ".tmp";
            await using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20, useAsync: true))
            await using (var gz = new GZipStream(fs, CompressionLevel.Fastest, leaveOpen: false))
            {
                await JsonSerializer.SerializeAsync(gz, pack, GetJsonOptions());
                await gz.FlushAsync();
            }
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path, overwrite: true);
        }

        // Public helper: get neuron IDs for a cluster via membership pack (no hydration)
        public async Task<List<Guid>> GetClusterNeuronIdsAsync(Guid clusterId, int maxToReturn = 0)
        {
            var key = clusterId.ToString("N");
            if (_partitionMetadata.TryGetValue(key, out var meta))
            {
                var pack = await GetMembershipPackCachedAsync(meta.PartitionPath);
                if (pack.Membership.TryGetValue(key, out var ids))
                {
                    if (ids == null || ids.Count == 0) return new List<Guid>();
                    if (maxToReturn > 0 && ids.Count > maxToReturn)
                    {
                        // return a simple random sample without allocation overhead
                        var rnd = new Random(key.GetHashCode());
                        var sample = new List<Guid>(maxToReturn);
                        for (int i = 0; i < maxToReturn; i++)
                        {
                            sample.Add(ids[rnd.Next(ids.Count)]);
                        }
                        return sample;
                    }
                    return new List<Guid>(ids);
                }
            }
            return new List<Guid>();
        }

        private async Task<MembershipPack> GetMembershipPackCachedAsync(PartitionPath partition)
        {
            var key = partition.FullPath;
            if (_membershipPackCache2.TryGetValue(key, out var entry))
            {
                return entry.pack;
            }
            var pack = await LoadMembershipPackAsync(partition);
            _membershipPackCache2[key] = (DateTime.UtcNow, pack);
            return pack;
        }

        // Save metrics for instrumentation
        public class SaveMetrics
        {
            public int ClustersExamined { get; set; }
            public int ClustersChangedMembership { get; set; }
            public int MembershipPacksWritten { get; set; }
            public int MembershipPacksSkipped { get; set; }
            public int NeuronBankPartitions { get; set; }
            public int NeuronsUpserted { get; set; }
        }
        private SaveMetrics _lastSaveMetrics = new SaveMetrics();
        public SaveMetrics GetAndResetLastSaveMetrics()
        {
            var snapshot = _lastSaveMetrics;
            _lastSaveMetrics = new SaveMetrics();
            return snapshot;
        }

        /// <summary>
        /// Save multiple clusters efficiently with throttling & compression
        /// </summary>
        public async Task SaveClustersEfficientlyAsync(IEnumerable<NeuronCluster> clusters, BrainContext context)
        {
            _batchMode = true;
            // Group clusters by stable partition (metadata if present)
            var groups = new Dictionary<string, (PartitionPath Path, List<NeuronCluster> Clusters)>();
            foreach (var cluster in clusters)
            {
                var partition = GetStablePartitionForCluster(cluster, context);
                var key = partition.FullPath;
                if (!groups.TryGetValue(key, out var entry))
                {
                    entry = (partition, new List<NeuronCluster>());
                    groups[key] = entry;
                }
                entry.Clusters.Add(cluster);
                groups[key] = entry;
            }

            // Instrumentation counters
            int clustersExamined = groups.Values.Sum(g => g.Clusters.Count);
            int clustersChanged = 0;
            int packsWritten = 0;

            // Write one membership pack per partition (only if changed)
            var sem = new SemaphoreSlim(MaxParallelSaves);
            var tasks = new List<Task>();
            foreach (var entry in groups.Values)
            {
                await sem.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var pack = await LoadMembershipPackAsync(entry.Path);
                        bool packChanged = false;

                        foreach (var c in entry.Clusters)
                        {
                            var cid = c.ClusterId.ToString("N");
                            // Try incremental update using newly added neuron ids
                            var newlyAdded = c.GetNewNeuronIdsSincePersist();
                            if (newlyAdded != null && newlyAdded.Count > 0)
                            {
                                if (!pack.Membership.TryGetValue(cid, out var existingIds) || existingIds == null)
                                {
                                    existingIds = new List<Guid>();
                                    pack.Membership[cid] = existingIds;
                                }
                                int before = existingIds.Count;
                                // Merge unique new ids
                                var set = new HashSet<Guid>(existingIds);
                                foreach (var id in newlyAdded)
                                {
                                    if (set.Add(id)) existingIds.Add(id);
                                }
                                if (existingIds.Count != before)
                                {
                                    packChanged = true;
                                    Interlocked.Increment(ref clustersChanged);
                                }
                            }
                            else
                            {
                                // Fallback: full set comparison
                                var neurons = await c.GetNeuronsAsync();
                                var newIds = neurons.Keys.ToList();
                                if (pack.Membership.TryGetValue(cid, out var existingIds2))
                                {
                                    if (!SetEquals(existingIds2, newIds))
                                    {
                                        pack.Membership[cid] = newIds;
                                        packChanged = true;
                                        Interlocked.Increment(ref clustersChanged);
                                    }
                                }
                                else
                                {
                                    pack.Membership[cid] = newIds;
                                    packChanged = true;
                                    Interlocked.Increment(ref clustersChanged);
                                }
                            }

                            // Update partition metadata for this cluster
                            await UpdatePartitionMetadata(entry.Path, c);

                            // Clear dirty flag regardless; current membership is persisted (incremental or full)
                            c.MarkMembershipPersisted();
                        }

                        if (packChanged)
                        {
                            pack.SavedAt = DateTime.UtcNow;
                            await SaveMembershipPackAsync(entry.Path, pack);
                            _membershipPackCache2[entry.Path.FullPath] = (DateTime.UtcNow, pack);
                            Interlocked.Increment(ref packsWritten);
                        }
                    }
                    finally { sem.Release(); }
                }));
            }
            await Task.WhenAll(tasks);

            // Persist partition metadata once at the end
            await this.PersistPartitionMetadataAsync();
            _batchMode = false;

            // Publish metrics (merge with any neuron bank metrics gathered earlier)
            var prior = _lastSaveMetrics;
            _lastSaveMetrics = new SaveMetrics
            {
                ClustersExamined = clustersExamined,
                ClustersChangedMembership = clustersChanged,
                MembershipPacksWritten = packsWritten,
                MembershipPacksSkipped = Math.Max(0, groups.Count - packsWritten),
                NeuronBankPartitions = prior.NeuronBankPartitions,
                NeuronsUpserted = prior.NeuronsUpserted
            };
        }

        private static bool SetEquals(List<Guid> a, List<Guid> b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;
            // Use HashSet for O(n)
            var setA = new HashSet<Guid>(a);
            return setA.SetEquals(b);
        }

        /// <summary>
        /// Save cluster using neurobiologically-inspired partitioning (full save: membership + bank upsert)
        /// </summary>
        public async Task SaveClusterWithPartitioningAsync(NeuronCluster cluster, BrainContext context)
        {
            // Use stable partition if exists
            var clusterPartition = GetStablePartitionForCluster(cluster, context);
            
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
            var clusterPartition = GetStablePartitionForCluster(cluster, context);
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
            var clusterPartition = GetStablePartitionForCluster(cluster, context);
            await _globalNeuronStore.SaveOrUpdateNeuronsAsync(clusterPartition, changedNeurons);
        }

        /// <summary>
        /// Batch-update neuron banks by grouping changes per partition to minimize rewrites.
        /// </summary>
        public async Task SaveNeuronBanksInBatchesAsync(IEnumerable<(NeuronCluster Cluster, IEnumerable<HybridNeuron> Changed)> changes, BrainContext context)
        {
            // Group by stable partition path string
            var byPartition = new Dictionary<string, (PartitionPath Path, List<HybridNeuron> Neurons)>();
            foreach (var (cluster, changed) in changes)
            {
                var partition = GetStablePartitionForCluster(cluster, context);
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

            var partitionsCount = byPartition.Count;
            var neuronsUpserted = byPartition.Values.Sum(v => v.Neurons.Count);

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

            // Update metrics (merge with existing)
            _lastSaveMetrics.NeuronBankPartitions = partitionsCount;
            _lastSaveMetrics.NeuronsUpserted = neuronsUpserted;
        }

        // ==================== PHASE 6B: PROCEDURAL NEURON STORAGE ====================

        /// <summary>
        /// Save neurons in compact procedural format (VQ code + sparse weights)
        /// Phase 6B: Alternative to SaveNeuronBanksInBatchesAsync for checkpoint compression
        /// </summary>
        public async Task SaveProceduralNeuronBanksAsync(
            IEnumerable<(NeuronCluster Cluster, IEnumerable<HybridNeuron> Neurons)> clusterBatches,
            BrainContext context)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            // Group neurons by partition and build cluster ID map
            var byPartition = new Dictionary<string, (PartitionPath Path, List<HybridNeuron> Neurons, Dictionary<Guid, Guid> ClusterIdMap)>();
            
            foreach (var (cluster, neurons) in clusterBatches)
            {
                var partition = GetStablePartitionForCluster(cluster, context);
                var key = partition.FullPath;
                
                if (!byPartition.TryGetValue(key, out var entry))
                {
                    entry = (partition, new List<HybridNeuron>(), new Dictionary<Guid, Guid>());
                    byPartition[key] = entry;
                }
                
                // Deduplicate neurons and track cluster membership
                var existing = entry.Neurons.ToDictionary(n => n.Id, n => n);
                foreach (var n in neurons)
                {
                    existing[n.Id] = n;
                    entry.ClusterIdMap[n.Id] = cluster.ClusterId;
                }
                
                byPartition[key] = (entry.Path, existing.Values.ToList(), entry.ClusterIdMap);
            }
            
            // Track compression statistics
            int totalFullBytes = 0;
            int totalCompactBytes = 0;
            int neuronCount = 0;
            int skippedCount = 0;
            
            // Calculate compression (before saving)
            foreach (var (partition, neurons, clusterIdMap) in byPartition.Values)
            {
                foreach (var neuron in neurons)
                {
                    if (!neuron.VqCode.HasValue)
                    {
                        skippedCount++;
                        continue;
                    }
                    
                    var snapshot = neuron.CreateSnapshot();
                    totalFullBytes += EstimateSnapshotSize(snapshot);
                    
                    var clusterId = clusterIdMap.TryGetValue(neuron.Id, out var cid) ? cid : Guid.Empty;
                    var procedural = ProceduralNeuronData.FromSnapshot(snapshot, neuron.VqCode.Value, clusterId);
                    totalCompactBytes += procedural.EstimatedBytes();
                    neuronCount++;
                }
            }
            
            // Save in parallel
            var sem = new SemaphoreSlim(MaxParallelSaves);
            var tasks = new List<Task>();
            
            foreach (var (partition, neurons, clusterIdMap) in byPartition.Values)
            {
                await sem.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try 
                    { 
                        await _globalNeuronStore.SaveProceduralNeuronsAsync(partition, neurons, clusterIdMap); 
                    }
                    finally { sem.Release(); }
                }));
            }
            
            await Task.WhenAll(tasks);
            
            // Log compression statistics
            var compressionRatio = totalFullBytes > 0 ? (double)totalFullBytes / totalCompactBytes : 1.0;
            var savedBytes = totalFullBytes - totalCompactBytes;
            
            Console.WriteLine($"   💾 Procedural save: {neuronCount} neurons in {byPartition.Count} partitions");
            Console.WriteLine($"      Full: {totalFullBytes:N0} bytes");
            Console.WriteLine($"      Compact: {totalCompactBytes:N0} bytes");
            Console.WriteLine($"      Ratio: {compressionRatio:F2}x (saved {savedBytes:N0} bytes)");
            if (skippedCount > 0)
                Console.WriteLine($"      ⚠️  Skipped {skippedCount} neurons without VQ codes");
            Console.WriteLine($"      Time: {sw.Elapsed.TotalSeconds:F2}s");
            
            // Update metrics
            _lastSaveMetrics.NeuronBankPartitions = byPartition.Count;
            _lastSaveMetrics.NeuronsUpserted = neuronCount;
        }

        /// <summary>
        /// Load neurons from procedural format for a specific partition
        /// Phase 6B: Requires VectorQuantizer and FeatureEncoder for regeneration
        /// </summary>
        public async Task<Dictionary<Guid, HybridNeuron>> LoadProceduralNeuronBankAsync(
            PartitionPath partition,
            IEnumerable<Guid> ids)
        {
            // Check if procedural components are available
            if (_vectorQuantizer == null || _featureEncoder == null)
            {
                // Fallback to standard format if components not attached
                return await _globalNeuronStore.LoadNeuronsAsync(partition, ids);
            }
            
            return await _globalNeuronStore.LoadProceduralNeuronsAsync(
                partition, 
                ids, 
                _vectorQuantizer, 
                _featureEncoder);
        }

        private int EstimateSnapshotSize(NeuronSnapshot snapshot)
        {
            int baseSize = 100; // GUID, timestamps, primitives
            int conceptsSize = snapshot.AssociatedConcepts?.Sum(c => c?.Length ?? 0) * 2 ?? 0;
            int weightsSize = snapshot.InputWeights?.Count * (16 + 8) ?? 0;
            return baseSize + conceptsSize + weightsSize;
        }

        // ==================== END PHASE 6B ====================

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
                
            // TODO: Implement fallback loading
            return new List<NeuronSnapshot>();
        }

        /// <summary>
        /// Find clusters using biological similarity metrics
        /// </summary>
        public List<ClusterReference> FindSimilarClusters(
            IEnumerable<string> concepts, 
            double similarityThreshold = 0.7)
        {
            var results = new List<ClusterReference>();

            // Fast path: exact concept matches via inverted index
            var set = new HashSet<Guid>();
            foreach (var concept in concepts)
            {
                if (_conceptIndex.TryGetValue(concept, out var list))
                {
                    foreach (var r in list)
                    {
                        if (set.Add(r.ClusterId)) results.Add(r);
                    }
                }
            }
            if (results.Count > 0)
            {
                // Already exact/near matches
                return results.OrderByDescending(r => r.Similarity).ToList();
            }

            // Fallback: scan metadata (rare)
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
                        LastAccessed = partition.LastAccessed,
                        ConceptDomain = partition.ConceptDomain
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
        /// Get cluster metadata by ID (includes centroid for pattern matching)
        /// </summary>
        public ClusterMetadata? GetClusterMetadata(Guid clusterId)
        {
            var key = clusterId.ToString("N");
            return _partitionMetadata.TryGetValue(key, out var metadata) ? metadata : null;
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
            // First attempt: parse clusterId and use partition metadata + membership pack
            if (TryParseClusterId(clusterIdentifier, out var cid))
            {
                var key = cid.ToString("N");
                if (_partitionMetadata.TryGetValue(key, out var meta))
                {
                    var pack = await LoadMembershipPackAsync(meta.PartitionPath);
                    if (pack.Membership.TryGetValue(key, out var ids))
                    {
                        // Phase 6B: Try procedural load first if components available
                        Dictionary<Guid, HybridNeuron> loaded;
                        if (_vectorQuantizer != null && _featureEncoder != null)
                        {
                            loaded = await _globalNeuronStore.LoadProceduralNeuronsAsync(
                                meta.PartitionPath, ids, _vectorQuantizer, _featureEncoder);
                        }
                        else
                        {
                            loaded = await _globalNeuronStore.LoadNeuronsAsync(meta.PartitionPath, ids);
                        }
                        
                        return new PartitionedClusterData
                        {
                            NeuronIds = ids,
                            Neurons = loaded.Values.Select(n => n.CreateSnapshot()).ToList(),
                            PartitionPath = meta.PartitionPath,
                            Metadata = meta,
                            SavedAt = DateTime.UtcNow
                        };
                    }
                }
            }
            
            // Fallback: scan files as before
            var searchPaths = GenerateSearchPaths(clusterIdentifier);
            foreach (var searchPath in searchPaths)
            {
                var fullPathDir = Path.Combine(_hierarchicalBasePath, searchPath);
                if (!Directory.Exists(fullPathDir)) continue;
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
                if ((data.Neurons == null || data.Neurons.Count == 0) && data.NeuronIds != null && data.NeuronIds.Count > 0)
                {
                    // Phase 6B: Try procedural load first if components available
                    Dictionary<Guid, HybridNeuron> loaded;
                    if (_vectorQuantizer != null && _featureEncoder != null)
                    {
                        loaded = await _globalNeuronStore.LoadProceduralNeuronsAsync(
                            data.PartitionPath, data.NeuronIds, _vectorQuantizer, _featureEncoder);
                    }
                    else
                    {
                        loaded = await _globalNeuronStore.LoadNeuronsAsync(data.PartitionPath, data.NeuronIds);
                    }
                    
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
                ConceptLabel = cluster.ConceptLabel,
                PartitionPath = partition,
                AssociatedConcepts = cluster.AssociatedConcepts.ToList(),
                NeuronCount = cluster.NeuronCount,
                AverageImportance = cluster.AverageImportance,
                LastAccessed = cluster.LastAccessed,
                CreatedAt = cluster.CreatedAt,
                // CRITICAL: Save centroid for pattern-based matching after reload
                Centroid = cluster.Centroid != null ? cluster.Centroid.ToArray() : null,
                CentroidNeuronCount = cluster.Centroid != null ? cluster.NeuronCount : 0
            };
        }

        private async Task UpdatePartitionMetadata(PartitionPath partition, NeuronCluster cluster)
        {
            var metadata = CreateClusterMetadata(cluster, partition);
            var key = cluster.ClusterId.ToString("N");
            lock (_metadataLock)
            {
                _partitionMetadata[key] = metadata;
                _conceptIndexDirty = true; // defer rebuild
            }
            
            if (_batchMode)
            {
                // Defer persistence to end of batch
                return;
            }
            
            // Persist immediately when not in batch mode
            var metadataPath = Path.Combine(_hierarchicalBasePath, "partition_metadata.json");
            var json = JsonSerializer.Serialize(_partitionMetadata, GetJsonOptions());
            await File.WriteAllTextAsync(metadataPath, json);
            // Rebuild index now for immediate consistency
            BuildConceptIndexFromMetadata();
            _conceptIndexDirty = false;
        }

        private async Task PersistPartitionMetadataAsync()
        {
            var metadataPath = Path.Combine(_hierarchicalBasePath, "partition_metadata.json");
            string json;
            int count;
            lock (_metadataLock)
            {
                json = JsonSerializer.Serialize(_partitionMetadata, GetJsonOptions());
                count = _partitionMetadata.Count;
                if (_conceptIndexDirty)
                {
                    BuildConceptIndexFromMetadata();
                    _conceptIndexDirty = false;
                }
            }
            var tmp = metadataPath + ".tmp";
            await File.WriteAllTextAsync(tmp, json);
            if (File.Exists(metadataPath)) File.Delete(metadataPath);
            File.Move(tmp, metadataPath, overwrite: true);
            // Also refresh cached cluster count fast; background refresh will correct sizes
            _statsCache.ClusterCount = count;
            _statsCache.LastUpdatedUtc = DateTime.UtcNow;
            await SaveStatsCacheAsync();
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
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = null, // Don't transform dictionary keys (breaks Guid keys)
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
                // Handles NaN, Infinity, -Infinity as JSON strings instead of failing
                ReferenceHandler = ReferenceHandler.IgnoreCycles, // Prevent circular reference serialization
                MaxDepth = 64 // Limit nesting depth to catch runaway serialization
            };
        }

        /// <summary>
        /// Hide base implementation to prevent dual saving
        /// Use only single storage location in hierarchical structure
        /// </summary>
        public async Task SaveClusterAsync(string identifier, List<NeuronSnapshot> neurons)
        {
            // TODO: Implement proper cluster saving
            await Task.CompletedTask;
        }

        /// <summary>
        /// Inspect a cluster's membership vs hydrated neuron count from the bank.
        /// Returns (membershipCount, hydratedCount). If file missing, returns (0,0).
        /// </summary>
        public async Task<(int membershipCount, int hydratedCount)> InspectClusterMembershipAsync(NeuronCluster cluster, BrainContext context)
        {
            // Prefer partition from metadata
            var key = cluster.ClusterId.ToString("N");
            if (!_partitionMetadata.TryGetValue(key, out var meta))
            {
                // Analyze if missing (should be rare)
                var analyzed = await AnalyzeClusterPartition(cluster, context);
                meta = new ClusterMetadata
                {
                    ClusterId = cluster.ClusterId,
                    ConceptDomain = cluster.ConceptDomain,
                    PartitionPath = analyzed,
                    AssociatedConcepts = cluster.AssociatedConcepts.ToList(),
                    NeuronCount = cluster.NeuronCount,
                    AverageImportance = cluster.AverageImportance,
                    LastAccessed = cluster.LastAccessed,
                    CreatedAt = cluster.CreatedAt
                };
            }

            // Read membership from membership.pack (authoritative)
            var pack = await LoadMembershipPackAsync(meta.PartitionPath);
            var ids = pack.Membership.TryGetValue(key, out var list) ? list : new List<Guid>();
            
            // Phase 6B: Try procedural load first if components available
            Dictionary<Guid, HybridNeuron> hydrated;
            if (_vectorQuantizer != null && _featureEncoder != null)
            {
                hydrated = await _globalNeuronStore.LoadProceduralNeuronsAsync(
                    meta.PartitionPath, ids, _vectorQuantizer, _featureEncoder);
            }
            else
            {
                hydrated = await _globalNeuronStore.LoadNeuronsAsync(meta.PartitionPath, ids);
            }
            
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

        private bool TryParseClusterId(string identifier, out Guid id)
        {
            // Accept both GUID and filenames that contain a GUID
            foreach (Match m in Regex.Matches(identifier, "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}"))
            {
                if (Guid.TryParse(m.Value, out id)) return true;
            }
            id = Guid.Empty;
            return false;
        }

        private PartitionPath GetStablePartitionForCluster(NeuronCluster cluster, BrainContext context)
        {
            // Prefer previously assigned partition to avoid churn; fall back to analysis for new clusters
            var key = cluster.ClusterId.ToString("N");
            if (_partitionMetadata.TryGetValue(key, out var meta))
            {
                return meta.PartitionPath;
            }
            return AnalyzeClusterPartition(cluster, context).GetAwaiter().GetResult();
        }

        public async Task<StorageStats> GetStorageStatsAsync()
        {
            // Fast path: use cached stats immediately to avoid blocking on NAS scans
            if (_statsCache != null && _statsCache.LastUpdatedUtc != default)
            {
                // Keep cluster count in sync with metadata (no scan)
                var clusterCount = Math.Max(_statsCache.ClusterCount, _partitionMetadata.Count);
                var totalBytes = Math.Max(0, _statsCache.BaseBytes) + Math.Max(0, _statsCache.HierarchicalBytes);
                // Kick a background refresh if needed (stale cache)
                StartBackgroundStatsRefreshIfNeeded();
                return await Task.FromResult(new StorageStats
                {
                    ClusterCount = clusterCount,
                    TotalSizeBytes = totalBytes
                });
            }

            // Slow path (first run without cache): compute then cache
            // TODO: Compute base stats properly
            long hierSize2 = 0;
            try
            {
                if (Directory.Exists(_hierarchicalBasePath))
                {
                    hierSize2 = new DirectoryInfo(_hierarchicalBasePath)
                        .EnumerateFiles("*", SearchOption.AllDirectories)
                        .Sum(f => f.Length);
                }
            }
            catch { /* ignore */ }

            _statsCache = new StatsCache
            {
                ClusterCount = _partitionMetadata.Count,
                BaseBytes = 0, // TODO: Calculate properly
                HierarchicalBytes = hierSize2,
                LastUpdatedUtc = DateTime.UtcNow
            };
            await SaveStatsCacheAsync();

            return new StorageStats
            {
                ClusterCount = _statsCache.ClusterCount,
                TotalSizeBytes = _statsCache.BaseBytes + _statsCache.HierarchicalBytes
            };
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
        public string ConceptLabel { get; set; } = ""; // Primary concept label for queries
        public PartitionPath PartitionPath { get; set; } = new();
        public List<string> AssociatedConcepts { get; set; } = new();
        public int NeuronCount { get; set; }
        public double AverageImportance { get; set; }
        public DateTime LastAccessed { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // CRITICAL: Persist centroid for pattern-based cluster matching
        public double[]? Centroid { get; set; } = null;
        public int CentroidNeuronCount { get; set; } = 0;
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
        public string ConceptDomain { get; set; } = "";
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

    // Extension methods for Cerebro compatibility
    public static class EnhancedBrainStorageExtensions
    {
        /// <summary>
        /// Get the hierarchical base path for this storage instance
        /// </summary>
        public static string GetBasePath(this EnhancedBrainStorage storage)
        {
            // Access via reflection since _hierarchicalBasePath is private
            var field = typeof(EnhancedBrainStorage).GetField("_hierarchicalBasePath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (string?)field?.GetValue(storage) ?? throw new InvalidOperationException("Unable to access _hierarchicalBasePath");
        }

        /// <summary>
        /// Load feature mappings from storage
        /// </summary>
        public static async Task<GreyMatter.Core.FeatureMappingSnapshot> LoadFeatureMappingsAsync(this EnhancedBrainStorage storage)
        {
            var basePath = storage.GetBasePath();
            var path = Path.Combine(basePath, "feature_mappings.json");
            
            if (!File.Exists(path))
            {
                return new GreyMatter.Core.FeatureMappingSnapshot 
                { 
                    FeatureMappings = new Dictionary<string, Guid>() 
                };
            }
            
            try
            {
                var json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<GreyMatter.Core.FeatureMappingSnapshot>(json) 
                       ?? new GreyMatter.Core.FeatureMappingSnapshot { FeatureMappings = new Dictionary<string, Guid>() };
            }
            catch
            {
                return new GreyMatter.Core.FeatureMappingSnapshot { FeatureMappings = new Dictionary<string, Guid>() };
            }
        }

        /// <summary>
        /// Load synapses from storage
        /// </summary>
        public static async Task<List<GreyMatter.Core.SynapseSnapshot>> LoadSynapsesAsync(this EnhancedBrainStorage storage)
        {
            var basePath = storage.GetBasePath();
            var path = Path.Combine(basePath, "synapses.json");
            
            if (!File.Exists(path))
            {
                return new List<GreyMatter.Core.SynapseSnapshot>();
            }
            
            try
            {
                var json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<List<GreyMatter.Core.SynapseSnapshot>>(json) 
                       ?? new List<GreyMatter.Core.SynapseSnapshot>();
            }
            catch
            {
                return new List<GreyMatter.Core.SynapseSnapshot>();
            }
        }

        /// <summary>
        /// Load cluster index from storage (returns partition metadata)
        /// </summary>
        public static async Task<Dictionary<string, object>> LoadClusterIndexAsync(this EnhancedBrainStorage storage)
        {
            var basePath = storage.GetBasePath();
            var path = Path.Combine(basePath, "partition_metadata.json");
            
            if (!File.Exists(path))
            {
                return new Dictionary<string, object>();
            }
            
            try
            {
                var json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<Dictionary<string, object>>(json) 
                       ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Save feature mappings to storage
        /// </summary>
        public static async Task SaveFeatureMappingsAsync(this EnhancedBrainStorage storage, GreyMatter.Core.FeatureMappingSnapshot snapshot)
        {
            var basePath = storage.GetBasePath();
            Directory.CreateDirectory(basePath);
            var path = Path.Combine(basePath, "feature_mappings.json");
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true, DictionaryKeyPolicy = null, NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals });
            await File.WriteAllTextAsync(path, json);
        }

        /// <summary>
        /// Save synapses to storage
        /// </summary>
        public static async Task SaveSynapsesAsync(this EnhancedBrainStorage storage, List<GreyMatter.Core.SynapseSnapshot> snapshots)
        {
            var basePath = storage.GetBasePath();
            Directory.CreateDirectory(basePath);
            var path = Path.Combine(basePath, "synapses.json");
            var json = JsonSerializer.Serialize(snapshots, new JsonSerializerOptions { WriteIndented = true, DictionaryKeyPolicy = null, NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals });
            await File.WriteAllTextAsync(path, json);
        }

        /// <summary>
        /// Save cluster index to storage
        /// </summary>
        public static async Task SaveClusterIndexAsync<T>(this EnhancedBrainStorage storage, List<T> clusterSnapshots)
        {
            var basePath = storage.GetBasePath();
            Directory.CreateDirectory(basePath);
            var path = Path.Combine(basePath, "cluster_index.json");
            var json = JsonSerializer.Serialize(clusterSnapshots, new JsonSerializerOptions { WriteIndented = true, DictionaryKeyPolicy = null, NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals });
            await File.WriteAllTextAsync(path, json);
        }

        /// <summary>
        /// Load a cluster by identifier - wrapper around LoadClusterWithPartitioningAsync
        /// </summary>
        public static async Task<List<NeuronSnapshot>> LoadClusterAsync(this EnhancedBrainStorage storage, string identifier)
        {
            var context = new BrainContext();
            return await storage.LoadClusterWithPartitioningAsync(identifier, context);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ADPC-Net Phase 1: Storage for Region→Cluster Mappings & Activation Stats
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Save region→cluster mappings for ADPC-Net pattern-based retrieval
        /// </summary>
        public static async Task SaveRegionMappingsAsync(this EnhancedBrainStorage storage, Dictionary<string, List<Guid>> regionMappings)
        {
            var basePath = storage.GetBasePath();
            Directory.CreateDirectory(basePath);
            var path = Path.Combine(basePath, "adpc_region_mappings.json");
            
            try
            {
                // Convert Guid to string for JSON compatibility
                var stringMappings = regionMappings.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Select(g => g.ToString()).ToList()
                );
                var json = JsonSerializer.Serialize(stringMappings, new JsonSerializerOptions { WriteIndented = true, DictionaryKeyPolicy = null, NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals });
                await File.WriteAllTextAsync(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error saving region mappings: {ex.Message}");
            }
        }

        /// <summary>
        /// Load region→cluster mappings for ADPC-Net pattern-based retrieval
        /// </summary>
        public static async Task<Dictionary<string, List<Guid>>> LoadRegionMappingsAsync(this EnhancedBrainStorage storage)
        {
            var basePath = storage.GetBasePath();
            var path = Path.Combine(basePath, "adpc_region_mappings.json");
            
            if (!File.Exists(path))
            {
                return new Dictionary<string, List<Guid>>();
            }
            
            try
            {
                var json = await File.ReadAllTextAsync(path);
                // Deserialize as string first, then convert to Guid
                var stringMappings = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
                if (stringMappings == null)
                    return new Dictionary<string, List<Guid>>();
                    
                return stringMappings.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Select(s => Guid.Parse(s)).ToList()
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error loading region mappings: {ex.Message}");
                return new Dictionary<string, List<Guid>>();
            }
        }

        /// <summary>
        /// Save activation statistics for ADPC-Net novelty detection
        /// </summary>
        public static async Task SaveActivationStatsAsync(this EnhancedBrainStorage storage, ActivationStats stats)
        {
            var basePath = storage.GetBasePath();
            Directory.CreateDirectory(basePath);
            var path = Path.Combine(basePath, "adpc_activation_stats.json");
            
            try
            {
                var summary = stats.GetSummary();
                var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true, DictionaryKeyPolicy = null, NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals });
                await File.WriteAllTextAsync(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error saving activation stats: {ex.Message}");
            }
        }

        /// <summary>
        /// Load activation statistics for ADPC-Net novelty detection
        /// </summary>
        public static async Task<ActivationStats> LoadActivationStatsAsync(this EnhancedBrainStorage storage)
        {
            var basePath = storage.GetBasePath();
            var path = Path.Combine(basePath, "adpc_activation_stats.json");
            
            // Always return a new instance - we can't fully reconstruct from summary
            // This is acceptable since activation stats rebuild naturally during training
            var stats = new ActivationStats();
            
            if (File.Exists(path))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(path);
                    var summary = JsonSerializer.Deserialize<ActivationStatsSummary>(json);
                    
                    if (summary != null && summary.TotalActivations > 0)
                    {
                        Console.WriteLine($"📊 Found previous activation stats ({summary.TotalActivations} activations) - will rebuild during training");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error loading activation stats: {ex.Message}");
                }
            }
            
            return stats;
        }

        /// <summary>
        /// Save VQ-VAE codebook for ADPC-Net Phase 5 learned vector quantization
        /// </summary>
        public static async Task SaveVQCodebookAsync(this EnhancedBrainStorage storage, CodebookSnapshot snapshot)
        {
            var basePath = storage.GetBasePath();
            Directory.CreateDirectory(basePath);
            var path = Path.Combine(basePath, "adpc_vqvae_codebook.json");
            
            try
            {
                var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions 
                { 
                    WriteIndented = true, 
                    DictionaryKeyPolicy = null,
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals 
                });
                await File.WriteAllTextAsync(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error saving VQ-VAE codebook: {ex.Message}");
            }
        }

        /// <summary>
        /// Load VQ-VAE codebook for ADPC-Net Phase 5 learned vector quantization
        /// </summary>
        public static async Task<CodebookSnapshot?> LoadVQCodebookAsync(this EnhancedBrainStorage storage)
        {
            var basePath = storage.GetBasePath();
            var path = Path.Combine(basePath, "adpc_vqvae_codebook.json");
            
            if (!File.Exists(path))
            {
                return null; // First run - no codebook yet
            }
            
            try
            {
                var json = await File.ReadAllTextAsync(path);
                var snapshot = JsonSerializer.Deserialize<CodebookSnapshot>(json);
                return snapshot;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error loading VQ-VAE codebook: {ex.Message}");
                return null;
            }
        }
    }
}
