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
using MessagePack;

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
            return Path.Combine(GetPartitionDir(partition), "neurons.bank.msgpack.gz");
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
            // Support migration from JSON to MessagePack
            // Try MessagePack first (new format), fall back to JSON (old format)
            var msgpackPath = bankPath; // Already .msgpack.gz from GetNeuronBankPath
            var jsonPath = bankPath.Replace(".msgpack.gz", ".json.gz");
            
            string? pathToRead = null;
            bool useMsgPack = false;
            
            if (File.Exists(msgpackPath))
            {
                pathToRead = msgpackPath;
                useMsgPack = true;
            }
            else if (File.Exists(jsonPath))
            {
                pathToRead = jsonPath;
                useMsgPack = false;
                Console.WriteLine($"   📦 Migrating {Path.GetFileName(jsonPath)} from JSON to MessagePack");
            }
            else
            {
                // No file exists yet
                return new Dictionary<string, NeuronSnapshot>();
            }
            
            try
            {
                await using var fs = File.OpenRead(pathToRead);
                await using var gz = new GZipStream(fs, CompressionMode.Decompress);
                
                Dictionary<string, NeuronSnapshot>? dict = null;
                
                if (useMsgPack)
                {
                    try
                    {
                        dict = await MessagePackSerializer.DeserializeAsync<Dictionary<string, NeuronSnapshot>>(gz).ConfigureAwait(false);
                    }
                    catch (MessagePack.MessagePackSerializationException msgEx)
                    {
                        Console.WriteLine($"❌ MessagePack deserialization failed for {Path.GetFileName(pathToRead)}:");
                        Console.WriteLine($"   Message: {msgEx.Message}");
                        Console.WriteLine($"   Inner: {msgEx.InnerException?.Message}");
                        Console.WriteLine($"   File size: {new FileInfo(pathToRead).Length} bytes");
                        Console.WriteLine($"   🔧 File appears corrupted, returning empty dictionary (will be rewritten)");
                        return new Dictionary<string, NeuronSnapshot>();
                    }
                }
                else
                {
                    // Read old JSON format
                    try
                    {
                        dict = await JsonSerializer.DeserializeAsync<Dictionary<string, NeuronSnapshot>>(gz, _jsonOptions).ConfigureAwait(false);
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"❌ JSON deserialization failed for {Path.GetFileName(pathToRead)}:");
                        Console.WriteLine($"   Message: {jsonEx.Message}");
                        Console.WriteLine($"   Path: {jsonEx.Path}");
                        Console.WriteLine($"   Line: {jsonEx.LineNumber}, Pos: {jsonEx.BytePositionInLine}");
                        Console.WriteLine($"   🔧 File appears corrupted, returning empty dictionary (will be rewritten)");
                        return new Dictionary<string, NeuronSnapshot>();
                    }
                }
                
                var result = new Dictionary<string, NeuronSnapshot>();
                if (dict != null)
                {
                    // Re-key to normalized IDs and last-writer-wins for duplicates
                    foreach (var kvp in dict)
                    {
                        if (kvp.Key == null || kvp.Value == null) continue;
                        var norm = NormalizeId(kvp.Key);
                        result[norm] = kvp.Value;
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Unexpected error reading {Path.GetFileName(pathToRead ?? bankPath)}:");
                Console.WriteLine($"   Message: {ex.Message}");
                Console.WriteLine($"   Type: {ex.GetType().Name}");
                Console.WriteLine($"   🔧 Returning empty dictionary (file will be rewritten)");
                return new Dictionary<string, NeuronSnapshot>();
            }
        }

        private async Task WriteBankAsync(string bankPath, Dictionary<string, NeuronSnapshot> dict)
        {
            var tmp = bankPath + ".tmp";
            try
            {
                // DEFENSIVE: Validate dictionary before serialization
                if (dict == null)
                {
                    Console.WriteLine($"⚠️  Null dictionary passed to WriteBankAsync for {Path.GetFileName(bankPath)}, skipping save");
                    return;
                }
                
                // DEFENSIVE: Remove any null keys or values, sanitize snapshots
                var sanitizedDict = new Dictionary<string, NeuronSnapshot>();
                var nullKeyCount = 0;
                var nullValueCount = 0;
                var invalidSnapshotCount = 0;
                
                foreach (var kvp in dict)
                {
                    if (string.IsNullOrEmpty(kvp.Key))
                    {
                        nullKeyCount++;
                        continue;
                    }
                    
                    if (kvp.Value == null)
                    {
                        nullValueCount++;
                        continue;
                    }
                    
                    // Sanitize the snapshot to ensure all properties are valid for MessagePack
                    var snapshot = kvp.Value;
                    
                    // Check for invalid ID (all zeros = default/uninitialized)
                    if (snapshot.Id == Guid.Empty)
                    {
                        invalidSnapshotCount++;
                        continue;
                    }
                    
                    // Ensure collections are not null (MessagePack requires non-null collections)
                    if (snapshot.AssociatedConcepts == null)
                        snapshot.AssociatedConcepts = new List<string>();
                    if (snapshot.InputWeights == null)
                        snapshot.InputWeights = new Dictionary<Guid, double>();
                    if (snapshot.ConceptTag == null)
                        snapshot.ConceptTag = "";
                    
                    // Sanitize doubles in InputWeights
                    var badWeights = new List<Guid>();
                    foreach (var weight in snapshot.InputWeights)
                    {
                        if (double.IsNaN(weight.Value) || double.IsInfinity(weight.Value))
                        {
                            badWeights.Add(weight.Key);
                        }
                    }
                    foreach (var badKey in badWeights)
                    {
                        snapshot.InputWeights.Remove(badKey);
                    }
                    
                    // Sanitize other doubles
                    if (double.IsNaN(snapshot.Bias) || double.IsInfinity(snapshot.Bias))
                        snapshot.Bias = 0.0;
                    if (double.IsNaN(snapshot.Threshold) || double.IsInfinity(snapshot.Threshold))
                        snapshot.Threshold = 0.5;
                    if (double.IsNaN(snapshot.LearningRate) || double.IsInfinity(snapshot.LearningRate))
                        snapshot.LearningRate = 0.01;
                    if (double.IsNaN(snapshot.ImportanceScore) || double.IsInfinity(snapshot.ImportanceScore))
                        snapshot.ImportanceScore = 0.0;
                    
                    sanitizedDict[kvp.Key] = snapshot;
                }
                
                if (nullKeyCount > 0 || nullValueCount > 0 || invalidSnapshotCount > 0)
                {
                    Console.WriteLine($"⚠️  Sanitized {Path.GetFileName(bankPath)}: removed {nullKeyCount} null keys, {nullValueCount} null values, {invalidSnapshotCount} invalid snapshots");
                }
                
                if (sanitizedDict.Count == 0)
                {
                    Console.WriteLine($"⚠️  No valid neurons to save in {Path.GetFileName(bankPath)} after sanitization, skipping save");
                    return;
                }
                
                await using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                await using (var gz = new GZipStream(fs, CompressionLevel.Fastest, leaveOpen: false))
                {
                    // Use MessagePack instead of JSON to avoid System.Text.Json double serialization bugs
                    await MessagePackSerializer.SerializeAsync(gz, sanitizedDict).ConfigureAwait(false);
                }

                if (File.Exists(bankPath)) File.Delete(bankPath);
                File.Move(tmp, bankPath, overwrite: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Serialization error in {Path.GetFileName(bankPath)}:");
                Console.WriteLine($"   Message: {ex.Message}");
                Console.WriteLine($"   Type: {ex.GetType().Name}");
                Console.WriteLine($"   Inner: {ex.InnerException?.Message}");
                Console.WriteLine($"   Stack trace (first 500 chars): {ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace?.Length ?? 0))}");
                Console.WriteLine($"   Dictionary contains {dict?.Count ?? 0} neurons");
                
                // Clean up temp file
                if (File.Exists(tmp))
                {
                    try { File.Delete(tmp); } catch { }
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

        // ==================== PHASE 6B: PROCEDURAL NEURON STORAGE ====================

        private string GetProceduralBankPath(PartitionPath partition)
        {
            return Path.Combine(GetPartitionDir(partition), "neurons.bank.procedural.msgpack.gz");
        }

        /// <summary>
        /// Save neurons in compact procedural format (VQ code + sparse weights)
        /// Phase 6B: Alternative to SaveOrUpdateNeuronsAsync for checkpoint compression
        /// </summary>
        public async Task SaveProceduralNeuronsAsync(
            PartitionPath partition, 
            IEnumerable<HybridNeuron> neurons,
            Dictionary<Guid, Guid> clusterIdMap)
        {
            var dir = GetPartitionDir(partition);
            Directory.CreateDirectory(dir);
            var bankPath = GetProceduralBankPath(partition);

            var bankLock = GetBankLock(bankPath);
            await bankLock.WaitAsync().ConfigureAwait(false);
            try
            {
                // Load existing procedural data if present
                var proceduralList = new List<ProceduralNeuronData>();
                if (File.Exists(bankPath))
                {
                    proceduralList = await ReadProceduralBankAsync(bankPath).ConfigureAwait(false);
                }

                // Create lookup for existing data
                var existingDict = proceduralList.ToDictionary(p => p.Id, p => p);
                bool changed = false;

                // Update/add neurons
                foreach (var neuron in neurons)
                {
                    // Skip neurons without VQ codes
                    if (!neuron.VqCode.HasValue)
                    {
                        Console.WriteLine($"⚠️  Neuron {neuron.Id} has no VQ code - skipping procedural save");
                        continue;
                    }

                    var snapshot = neuron.CreateSnapshot();
                    var clusterId = clusterIdMap.TryGetValue(neuron.Id, out var cid) ? cid : Guid.Empty;
                    var procedural = ProceduralNeuronData.FromSnapshot(snapshot, neuron.VqCode.Value, clusterId);

                    if (existingDict.TryGetValue(neuron.Id, out var existing))
                    {
                        // Simple equality check - update if different
                        if (existing.VqCode != procedural.VqCode || 
                            existing.ActivationCount != procedural.ActivationCount ||
                            !existing.SynapticWeights.SequenceEqual(procedural.SynapticWeights))
                        {
                            existingDict[neuron.Id] = procedural;
                            changed = true;
                        }
                    }
                    else
                    {
                        existingDict[neuron.Id] = procedural;
                        changed = true;
                    }
                }

                // Write back only if changed
                if (changed)
                {
                    await WriteProceduralBankAsync(bankPath, existingDict.Values.ToList()).ConfigureAwait(false);
                }
            }
            finally
            {
                bankLock.Release();
            }
        }

        /// <summary>
        /// Load neurons from compact procedural format and regenerate full HybridNeuron instances
        /// Phase 6B: Alternative to LoadNeuronsAsync for checkpoint decompression
        /// </summary>
        // Cache of partitions known to NOT have procedural banks (to suppress repeated warnings)
        private readonly HashSet<string> _missingProceduralBanks = new HashSet<string>();
        private readonly object _missingBanksLock = new object();

        public async Task<Dictionary<Guid, HybridNeuron>> LoadProceduralNeuronsAsync(
            PartitionPath partition,
            IEnumerable<Guid> ids,
            VectorQuantizer vectorQuantizer,
            FeatureEncoder featureEncoder)
        {
            var result = new Dictionary<Guid, HybridNeuron>();
            var bankPath = GetProceduralBankPath(partition);
            
            if (!File.Exists(bankPath))
            {
                // Only warn once per partition (suppress spam)
                bool shouldWarn = false;
                lock (_missingBanksLock)
                {
                    if (!_missingProceduralBanks.Contains(partition.FullPath))
                    {
                        _missingProceduralBanks.Add(partition.FullPath);
                        shouldWarn = true;
                    }
                }
                
                if (shouldWarn)
                {
                    Console.WriteLine($"⚠️  Procedural bank not found for partition {partition.FullPath} (suppressing further warnings)");
                }
                
                // Fallback to standard format
                return await LoadNeuronsAsync(partition, ids).ConfigureAwait(false);
            }

            try
            {
                var proceduralList = await ReadProceduralBankAsync(bankPath).ConfigureAwait(false);
                var regenerator = new ProceduralNeuronRegenerator(vectorQuantizer, featureEncoder);

                // Create lookup
                var proceduralDict = proceduralList.ToDictionary(p => p.Id, p => p);

                foreach (var id in ids)
                {
                    if (proceduralDict.TryGetValue(id, out var procedural))
                    {
                        var neuron = regenerator.RegenerateNeuron(procedural);
                        result[id] = neuron;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Error loading procedural neurons from {partition.FullPath}: {ex.Message}");
                // Fallback to standard format
                return await LoadNeuronsAsync(partition, ids).ConfigureAwait(false);
            }
        }

        private async Task<List<ProceduralNeuronData>> ReadProceduralBankAsync(string bankPath)
        {
            try
            {
                await using var fs = File.OpenRead(bankPath);
                await using var gz = new GZipStream(fs, CompressionMode.Decompress);
                var data = await MessagePackSerializer.DeserializeAsync<List<ProceduralNeuronData>>(gz).ConfigureAwait(false);
                return data ?? new List<ProceduralNeuronData>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reading procedural bank {Path.GetFileName(bankPath)}: {ex.Message}");
                return new List<ProceduralNeuronData>();
            }
        }

        private async Task WriteProceduralBankAsync(string bankPath, List<ProceduralNeuronData> data)
        {
            var tmp = bankPath + ".tmp";
            try
            {
                await using var fs = File.Create(tmp);
                await using var gz = new GZipStream(fs, CompressionLevel.Optimal);
                await MessagePackSerializer.SerializeAsync(gz, data).ConfigureAwait(false);
                await gz.FlushAsync().ConfigureAwait(false);
                await fs.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error writing procedural bank {Path.GetFileName(bankPath)}: {ex.Message}");
                if (File.Exists(tmp)) File.Delete(tmp);
                throw;
            }

            // Atomic replace
            if (File.Exists(bankPath)) File.Delete(bankPath);
            File.Move(tmp, bankPath);
        }

        private int EstimateSnapshotSize(NeuronSnapshot snapshot)
        {
            int baseSize = 100; // GUID, timestamps, primitives
            int conceptsSize = snapshot.AssociatedConcepts?.Sum(c => c?.Length ?? 0) * 2 ?? 0;
            int weightsSize = snapshot.InputWeights?.Count * (16 + 8) ?? 0;
            return baseSize + conceptsSize + weightsSize;
        }
    }
}
