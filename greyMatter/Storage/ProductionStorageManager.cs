using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GreyMatter.Storage
{
    /// <summary>
    /// Temporary stub for ProductionStorageManager to restore build.
    /// TODO: Replace with Cerebro + EnhancedBrainStorage architecture.
    /// </summary>
    public class ProductionStorageManager
    {
        private readonly string _basePath;
        private readonly string _livePath;
        private readonly string _checkpointsPath;
        private readonly string _metricsPath;
        private readonly string _episodicPath;

        // JSON options that safely handle NaN, Infinity, -Infinity
        private static readonly JsonSerializerOptions _safeJsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
        };

        public ProductionStorageManager(string basePath)
        {
            _basePath = basePath;
            _livePath = Path.Combine(basePath, "live");
            _checkpointsPath = Path.Combine(basePath, "checkpoints");
            _metricsPath = Path.Combine(basePath, "metrics");
            _episodicPath = Path.Combine(basePath, "episodic_memory");

            Directory.CreateDirectory(_livePath);
            Directory.CreateDirectory(_checkpointsPath);
            Directory.CreateDirectory(_metricsPath);
            Directory.CreateDirectory(_episodicPath);
        }

        public string GetLivePath(string subPath) => Path.Combine(_livePath, subPath);
        public string GetEpisodicMemoryPath() => _episodicPath;

        public async Task<GreyMatter.Core.BrainCheckpoint?> LoadLatestCheckpointAsync()
        {
            var files = Directory.GetFiles(_checkpointsPath, "checkpoint_*.json");
            if (files.Length == 0) return null;

            var latest = files.OrderByDescending(f => f).First();
            var json = await File.ReadAllTextAsync(latest);
            return JsonSerializer.Deserialize<GreyMatter.Core.BrainCheckpoint>(json);
        }

        public async Task<T?> LoadLiveStateAsync<T>(string filename)
        {
            var path = Path.Combine(_livePath, filename);
            if (!File.Exists(path)) return default;

            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<T>(json);
        }

        public async Task SaveLiveStateAsync<T>(string filename, T data)
        {
            var path = Path.Combine(_livePath, filename);
            var json = JsonSerializer.Serialize(data, _safeJsonOptions);
            await File.WriteAllTextAsync(path, json);
        }

        public async Task SaveCheckpointAsync(GreyMatter.Core.BrainCheckpoint checkpoint)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var path = Path.Combine(_checkpointsPath, $"checkpoint_{timestamp}.json");
            var json = JsonSerializer.Serialize(checkpoint, _safeJsonOptions);
            await File.WriteAllTextAsync(path, json);
        }

        public async Task LogMetricAsync(string metric, object value)
        {
            var path = Path.Combine(_metricsPath, $"{metric}.log");
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            var line = $"{timestamp}\t{value}\n";
            await File.AppendAllTextAsync(path, line);
        }

        public Task<bool> ArchiveToNASAsync()
        {
            // Stub: Would copy data to NAS
            return Task.FromResult(true);
        }
    }
}
