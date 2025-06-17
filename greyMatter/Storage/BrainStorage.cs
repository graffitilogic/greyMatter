using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GreyMatter.Core;

namespace GreyMatter.Storage
{
    /// <summary>
    /// Handles persistence of neuron clusters and brain state
    /// Optimized for lazy loading and efficient storage
    /// </summary>
    public class BrainStorage
    {
        private readonly string _basePath;
        private readonly JsonSerializerOptions _jsonOptions;
        
        public BrainStorage(string basePath = "brain_data")
        {
            _basePath = basePath;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            EnsureDirectoryStructure();
        }

        /// <summary>
        /// Save neuron cluster to disk
        /// </summary>
        public async Task SaveClusterAsync(string clusterPath, List<NeuronSnapshot> neurons)
        {
            var fullPath = Path.Combine(_basePath, clusterPath);
            var directory = Path.GetDirectoryName(fullPath);
            
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            
            var json = JsonSerializer.Serialize(neurons, _jsonOptions);
            await File.WriteAllTextAsync(fullPath, json);
        }

        /// <summary>
        /// Load neuron cluster from disk
        /// </summary>
        public async Task<List<NeuronSnapshot>> LoadClusterAsync(string clusterPath)
        {
            var fullPath = Path.Combine(_basePath, clusterPath);
            
            if (!File.Exists(fullPath))
                return new List<NeuronSnapshot>();
            
            var json = await File.ReadAllTextAsync(fullPath);
            return JsonSerializer.Deserialize<List<NeuronSnapshot>>(json, _jsonOptions) ?? new List<NeuronSnapshot>();
        }

        /// <summary>
        /// Save cluster index for fast lookup
        /// </summary>
        public async Task SaveClusterIndexAsync(List<ClusterSnapshot> clusters)
        {
            var indexPath = Path.Combine(_basePath, "cluster_index.json");
            var json = JsonSerializer.Serialize(clusters, _jsonOptions);
            await File.WriteAllTextAsync(indexPath, json);
        }

        /// <summary>
        /// Load cluster index
        /// </summary>
        public async Task<List<ClusterSnapshot>> LoadClusterIndexAsync()
        {
            var indexPath = Path.Combine(_basePath, "cluster_index.json");
            
            if (!File.Exists(indexPath))
                return new List<ClusterSnapshot>();
            
            var json = await File.ReadAllTextAsync(indexPath);
            return JsonSerializer.Deserialize<List<ClusterSnapshot>>(json, _jsonOptions) ?? new List<ClusterSnapshot>();
        }

        /// <summary>
        /// Save synapses
        /// </summary>
        public async Task SaveSynapsesAsync(List<SynapseSnapshot> synapses)
        {
            var synapsePath = Path.Combine(_basePath, "synapses.json");
            var json = JsonSerializer.Serialize(synapses, _jsonOptions);
            await File.WriteAllTextAsync(synapsePath, json);
        }

        /// <summary>
        /// Load synapses
        /// </summary>
        public async Task<List<SynapseSnapshot>> LoadSynapsesAsync()
        {
            var synapsePath = Path.Combine(_basePath, "synapses.json");
            
            if (!File.Exists(synapsePath))
                return new List<SynapseSnapshot>();
            
            var json = await File.ReadAllTextAsync(synapsePath);
            return JsonSerializer.Deserialize<List<SynapseSnapshot>>(json, _jsonOptions) ?? new List<SynapseSnapshot>();
        }

        /// <summary>
        /// Save feature mappings
        /// </summary>
        public async Task SaveFeatureMappingsAsync(FeatureMappingSnapshot mappings)
        {
            var mappingPath = Path.Combine(_basePath, "feature_mappings.json");
            var json = JsonSerializer.Serialize(mappings, _jsonOptions);
            await File.WriteAllTextAsync(mappingPath, json);
        }

        /// <summary>
        /// Load feature mappings
        /// </summary>
        public async Task<FeatureMappingSnapshot> LoadFeatureMappingsAsync()
        {
            var mappingPath = Path.Combine(_basePath, "feature_mappings.json");
            
            if (!File.Exists(mappingPath))
                return new FeatureMappingSnapshot();
            
            var json = await File.ReadAllTextAsync(mappingPath);
            return JsonSerializer.Deserialize<FeatureMappingSnapshot>(json, _jsonOptions) ?? new FeatureMappingSnapshot();
        }

        /// <summary>
        /// Delete old cluster files
        /// </summary>
        public async Task CleanupOldClusters(TimeSpan maxAge)
        {
            var clusterDir = Path.Combine(_basePath, "clusters");
            if (!Directory.Exists(clusterDir)) return;
            
            var cutoffDate = DateTime.UtcNow - maxAge;
            var filesToDelete = new List<string>();
            
            foreach (var file in Directory.GetFiles(clusterDir, "*.cluster"))
            {
                var lastWrite = File.GetLastWriteTimeUtc(file);
                if (lastWrite < cutoffDate)
                {
                    filesToDelete.Add(file);
                }
            }
            
            foreach (var file in filesToDelete)
            {
                File.Delete(file);
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Get storage statistics
        /// </summary>
        public async Task<StorageStats> GetStorageStatsAsync()
        {
            var stats = new StorageStats();
            
            if (Directory.Exists(_basePath))
            {
                var clusterDir = Path.Combine(_basePath, "clusters");
                if (Directory.Exists(clusterDir))
                {
                    stats.ClusterCount = Directory.GetFiles(clusterDir, "*.cluster").Length;
                }
                
                // Calculate total size
                var dirInfo = new DirectoryInfo(_basePath);
                stats.TotalSizeBytes = CalculateDirectorySize(dirInfo);
            }
            
            return await Task.FromResult(stats);
        }

        private void EnsureDirectoryStructure()
        {
            Directory.CreateDirectory(_basePath);
            Directory.CreateDirectory(Path.Combine(_basePath, "clusters"));
            Directory.CreateDirectory(Path.Combine(_basePath, "backups"));
        }

        private long CalculateDirectorySize(DirectoryInfo directory)
        {
            long size = 0;
            
            try
            {
                foreach (var file in directory.GetFiles())
                {
                    size += file.Length;
                }
                
                foreach (var subDir in directory.GetDirectories())
                {
                    size += CalculateDirectorySize(subDir);
                }
            }
            catch
            {
                // Ignore access errors
            }
            
            return size;
        }
    }

    /// <summary>
    /// Storage statistics
    /// </summary>
    public class StorageStats
    {
        public int ClusterCount { get; set; }
        public long TotalSizeBytes { get; set; }
        public string TotalSizeFormatted => FormatBytes(TotalSizeBytes);

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
