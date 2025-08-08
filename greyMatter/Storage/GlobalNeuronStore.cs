using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GreyMatter.Core;
using System.Collections.Concurrent;
using System.Threading;

namespace GreyMatter.Storage
{
    /// <summary>
    /// GlobalNeuronStore: persists neurons per partition, shared across clusters.
    /// Current implementation stores one JSON dictionary (Guid->NeuronSnapshot) per partition, gzip-compressed.
    /// </summary>
    public class GlobalNeuronStore
    {
        private readonly string _hierarchicalBasePath;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Serialize writes per bank file to prevent concurrent writers thrashing the same partition
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _bankLocks = new();
        private static SemaphoreSlim GetBankLock(string bankPath) => _bankLocks.GetOrAdd(bankPath, _ => new SemaphoreSlim(1, 1));

        public GlobalNeuronStore(string hierarchicalBasePath)
        {
            _hierarchicalBasePath = hierarchicalBasePath;
        }

        private string GetPartitionDir(PartitionPath partition)
        {
            return Path.Combine(_hierarchicalBasePath, partition.FullPath);
        }

        private string GetNeuronBankPath(PartitionPath partition)
        {
            return Path.Combine(GetPartitionDir(partition), "neurons.bank.json.gz");
        }

        /// <summary>
        /// Save or update the given neurons into the partition bank. Only provided neurons are updated.
        /// Optimized to avoid rewriting the bank if nothing effectively changed.
        /// </summary>
        public async Task SaveOrUpdateNeuronsAsync(PartitionPath partition, IEnumerable<HybridNeuron> neurons)
        {
            var dir = GetPartitionDir(partition);
            Directory.CreateDirectory(dir);
            var bankPath = GetNeuronBankPath(partition);

            var bankLock = GetBankLock(bankPath);
            await bankLock.WaitAsync().ConfigureAwait(false);
            try
            {
                // Load existing if present
                var dict = new Dictionary<string, NeuronSnapshot>();
                if (File.Exists(bankPath))
                {
                    dict = await ReadBankAsync(bankPath).ConfigureAwait(false);
                }

                bool changed = false;

                // Update entries
                foreach (var n in neurons)
                {
                    var id = n.Id.ToString();
                    var newSnap = n.CreateSnapshot();

                    if (dict.TryGetValue(id, out var oldSnap))
                    {
                        if (!SnapshotsEqual(oldSnap, newSnap))
                        {
                            dict[id] = newSnap;
                            changed = true;
                        }
                    }
                    else
                    {
                        dict[id] = newSnap;
                        changed = true;
                    }
                }

                // Write back atomically only if something actually changed
                if (changed)
                {
                    await WriteBankAsync(bankPath, dict).ConfigureAwait(false);
                }
            }
            finally
            {
                bankLock.Release();
            }
        }

        /// <summary>
        /// Load specific neurons by ID from a partition. Returns map of ID to neuron.
        /// </summary>
        public async Task<Dictionary<Guid, HybridNeuron>> LoadNeuronsAsync(PartitionPath partition, IEnumerable<Guid> ids)
        {
            var result = new Dictionary<Guid, HybridNeuron>();
            var bankPath = GetNeuronBankPath(partition);
            if (!File.Exists(bankPath)) return result;

            var dict = await ReadBankAsync(bankPath).ConfigureAwait(false);
            foreach (var id in ids)
            {
                if (dict.TryGetValue(id.ToString(), out var snap))
                {
                    result[id] = HybridNeuron.FromSnapshot(snap);
                }
            }
            return result;
        }

        private async Task<Dictionary<string, NeuronSnapshot>> ReadBankAsync(string bankPath)
        {
            await using var fs = File.OpenRead(bankPath);
            await using var gz = new GZipStream(fs, CompressionMode.Decompress);
            var dict = await JsonSerializer.DeserializeAsync<Dictionary<string, NeuronSnapshot>>(gz, _jsonOptions).ConfigureAwait(false);
            return dict ?? new Dictionary<string, NeuronSnapshot>();
        }

        private async Task WriteBankAsync(string bankPath, Dictionary<string, NeuronSnapshot> dict)
        {
            var tmp = bankPath + ".tmp";
            await using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            await using (var gz = new GZipStream(fs, CompressionLevel.Fastest, leaveOpen: false))
            await using (var writer = new Utf8JsonWriter(gz, new JsonWriterOptions { Indented = false }))
            {
                JsonSerializer.Serialize(writer, dict, _jsonOptions);
                await writer.FlushAsync().ConfigureAwait(false);
            }

            if (File.Exists(bankPath)) File.Delete(bankPath);
            File.Move(tmp, bankPath, overwrite: true);
        }

        // Basic structural equality using deterministic JSON serialization
        private bool SnapshotsEqual(NeuronSnapshot a, NeuronSnapshot b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            // Serialize to bytes for comparison. Assumes stable property ordering.
            var bytesA = JsonSerializer.SerializeToUtf8Bytes(a, _jsonOptions);
            var bytesB = JsonSerializer.SerializeToUtf8Bytes(b, _jsonOptions);
            if (bytesA.Length != bytesB.Length) return false;
            // Fast path for same reference equality handled above; now compare spans
            return ((ReadOnlySpan<byte>)bytesA).SequenceEqual(bytesB);
        }
    }
}
