using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = null, // Don't transform Guid dictionary keys
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
            ReferenceHandler = ReferenceHandler.IgnoreCycles, // Prevent circular reference serialization
            MaxDepth = 64 // Limit nesting depth to catch runaway serialization
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

        // Canonicalize neuron IDs to lowercase Guid("N") to avoid mismatches across files
        private static string NormalizeId(Guid id) => id.ToString("N").ToLowerInvariant();
        private static string NormalizeId(string id)
        {
            if (Guid.TryParse(id, out var g)) return g.ToString("N").ToLowerInvariant();
            return id.Replace("-", string.Empty).Trim().ToLowerInvariant();
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
                    var id = NormalizeId(n.Id);
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
                var key = NormalizeId(id);
                if (dict.TryGetValue(key, out var snap))
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
            var result = new Dictionary<string, NeuronSnapshot>();
            if (dict != null)
            {
                // Re-key to normalized IDs and last-writer-wins for duplicates
                foreach (var kvp in dict)
                {
                    var norm = NormalizeId(kvp.Key);
                    result[norm] = kvp.Value;
                }
            }
            return result;
        }

        private async Task WriteBankAsync(string bankPath, Dictionary<string, NeuronSnapshot> dict)
        {
            var tmp = bankPath + ".tmp";
            try
            {
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
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"❌ JSON serialization error in {Path.GetFileName(bankPath)}:");
                Console.WriteLine($"   Message: {jsonEx.Message}");
                Console.WriteLine($"   Path: {jsonEx.Path}");
                Console.WriteLine($"   Line: {jsonEx.LineNumber}, Position: {jsonEx.BytePositionInLine}");
                
                // Parse the JSON path to extract neuron ID and weight ID
                if (!string.IsNullOrEmpty(jsonEx.Path))
                {
                    var pathParts = jsonEx.Path.Split('.');
                    if (pathParts.Length >= 3)
                    {
                        var neuronId = pathParts[1]; // $.neuronId.inputWeights.weightId
                        Console.WriteLine($"   🔍 Problematic neuron ID: {neuronId}");
                        
                        if (dict.TryGetValue(neuronId, out var neuron))
                        {
                            Console.WriteLine($"   🔍 Neuron ConceptTag: '{neuron.ConceptTag}'");
                            Console.WriteLine($"   🔍 Neuron has {neuron.InputWeights?.Count ?? 0} weights");
                            
                            // If the path mentions a specific weight, find it
                            if (pathParts.Length >= 4 && pathParts[2] == "inputWeights")
                            {
                                var weightIdStr = pathParts[3];
                                if (Guid.TryParse(weightIdStr, out var weightId) && neuron.InputWeights != null)
                                {
                                    if (neuron.InputWeights.TryGetValue(weightId, out var weightValue))
                                    {
                                        Console.WriteLine($"   🔍 Problematic weight ID: {weightId}");
                                        Console.WriteLine($"   🔍 Weight value: {weightValue} (0x{BitConverter.DoubleToInt64Bits(weightValue):X16})");
                                        Console.WriteLine($"   🔍 IsNaN: {double.IsNaN(weightValue)}, IsInfinity: {double.IsInfinity(weightValue)}");
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Try to identify the problematic neuron
                Console.WriteLine($"   Dictionary contains {dict.Count} neurons");
                foreach (var kvp in dict.Take(5))
                {
                    Console.WriteLine($"   Sample ID: {kvp.Key}, ConceptTag: '{kvp.Value.ConceptTag?.Substring(0, Math.Min(50, kvp.Value.ConceptTag?.Length ?? 0)) ?? "null"}'");
                }
                throw;
            }
        }

        // Basic structural equality using deterministic JSON serialization
        private bool SnapshotsEqual(NeuronSnapshot a, NeuronSnapshot b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            
            // Quick structural checks before expensive serialization
            if (a.Id != b.Id) return false;
            if (a.ActivationCount != b.ActivationCount) return false;
            if (a.InputWeights?.Count != b.InputWeights?.Count) return false;
            if (a.AssociatedConcepts?.Count != b.AssociatedConcepts?.Count) return false;
            
            try
            {
                // Serialize to bytes for comparison. Assumes stable property ordering.
                var bytesA = JsonSerializer.SerializeToUtf8Bytes(a, _jsonOptions);
                var bytesB = JsonSerializer.SerializeToUtf8Bytes(b, _jsonOptions);
                if (bytesA.Length != bytesB.Length) return false;
                // Fast path for same reference equality handled above; now compare spans
                return ((ReadOnlySpan<byte>)bytesA).SequenceEqual(bytesB);
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"⚠️  JSON error in SnapshotsEqual (neuron {a.Id}):");
                Console.WriteLine($"   Message: {jsonEx.Message}");
                Console.WriteLine($"   ConceptTag: '{a.ConceptTag}'");
                Console.WriteLine($"   First few concepts: {string.Join(", ", a.AssociatedConcepts?.Take(3) ?? Enumerable.Empty<string>())}");
                // If we can't serialize, treat as not equal to force a save attempt
                return false;
            }
        }
    }
}
