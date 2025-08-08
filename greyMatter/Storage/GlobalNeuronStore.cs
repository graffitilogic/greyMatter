using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GreyMatter.Core;

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
        /// </summary>
        public async Task SaveOrUpdateNeuronsAsync(PartitionPath partition, IEnumerable<HybridNeuron> neurons)
        {
            var dir = GetPartitionDir(partition);
            Directory.CreateDirectory(dir);
            var bankPath = GetNeuronBankPath(partition);

            // Load existing if present
            var dict = new Dictionary<string, NeuronSnapshot>();
            if (File.Exists(bankPath))
            {
                dict = await ReadBankAsync(bankPath);
            }

            // Update entries
            foreach (var n in neurons)
            {
                dict[n.Id.ToString()] = n.CreateSnapshot();
            }

            // Write back atomically
            await WriteBankAsync(bankPath, dict);
        }

        /// <summary>
        /// Load specific neurons by ID from a partition. Returns map of ID to neuron.
        /// </summary>
        public async Task<Dictionary<Guid, HybridNeuron>> LoadNeuronsAsync(PartitionPath partition, IEnumerable<Guid> ids)
        {
            var result = new Dictionary<Guid, HybridNeuron>();
            var bankPath = GetNeuronBankPath(partition);
            if (!File.Exists(bankPath)) return result;

            var dict = await ReadBankAsync(bankPath);
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
            var dict = await JsonSerializer.DeserializeAsync<Dictionary<string, NeuronSnapshot>>(gz, _jsonOptions);
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
                await writer.FlushAsync();
            }

            if (File.Exists(bankPath)) File.Delete(bankPath);
            File.Move(tmp, bankPath, overwrite: true);
        }
    }
}
