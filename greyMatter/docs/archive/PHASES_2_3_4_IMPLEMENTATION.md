# Phase 2-4 Implementation Guide

**Prerequisites:** Phase 1 complete (synapses and neurons have matching IDs)

---

## Phase 2: Connect Procedural Loading to Runtime

**Goal:** Make neurons lazy-load from procedural banks instead of full hydration

**Time Estimate:** 2-3 hours implementation + testing

### Step 1: Modify NeuronCluster.LoadFromDiskAsync

**File:** `Core/NeuronCluster.cs` around line 445

**Current behavior:**
```csharp
private async Task LoadFromDiskAsync()
{
    if (_loadFunction != null)
    {
        var snapshots = await _loadFunction(_persistencePath);
        _neurons = snapshots.ToDictionary(s => s.Id, HybridNeuron.FromSnapshot);
    }
}
```

**New behavior:**
```csharp
private async Task LoadFromDiskAsync()
{
    // Try procedural bank first (compressed, fast)
    var proceduralBankPath = GetProceduralBankPath();
    if (File.Exists(proceduralBankPath))
    {
        await LoadFromProceduralBank(proceduralBankPath);
    }
    else if (_loadFunction != null)
    {
        // Fallback to full hydration
        var snapshots = await _loadFunction(_persistencePath);
        _neurons = snapshots.ToDictionary(s => s.Id, HybridNeuron.FromSnapshot);
    }
}

private async Task LoadFromProceduralBank(string bankPath)
{
    // Use existing ProceduralNeuronRegenerator
    var regenerator = new ProceduralNeuronRegenerator();
    
    // Load compact representations
    var proceduralData = await LoadProceduralData(bankPath);
    
    // Regenerate neurons on-demand
    foreach (var data in proceduralData)
    {
        var neuron = regenerator.RegenerateNeuron(data);
        _neurons[neuron.Id] = neuron;
    }
}
```

### Step 2: Connect to EnhancedBrainStorage

**File:** `Storage/EnhancedBrainStorage.cs` around line 708

**Find:** `LoadProceduralNeuronBankAsync()` method (currently has zero callers)

**Add caller in:** `LoadClusterAsync()` method

**Change:**
```csharp
public async Task<NeuronCluster?> LoadClusterAsync(string clusterId)
{
    // ... existing code ...
    
    // NEW: Try procedural path first
    var proceduralPath = GetProceduralBankPath(partition);
    if (File.Exists(proceduralPath))
    {
        loadFunction = async (path) => {
            return await LoadProceduralNeuronBankAsync(partition, neuronIds);
        };
    }
    else
    {
        // Existing: Fall back to standard bank
        loadFunction = async (path) => {
            return await _globalNeuronStore.LoadNeuronsAsync(partition, neuronIds);
        };
    }
    
    // ... rest of existing code ...
}
```

### Step 3: Validation

```bash
# 1. Start fresh (avoid old data)
mv /Volumes/jarvis/brainData /Volumes/jarvis/brainData_old_$(date +%Y%m%d)

# 2. Train for 5 minutes
dotnet run -- --production-training --duration 300

# 3. Check that procedural banks exist
find /Volumes/jarvis/brainData -name "procedural_bank.msgpack*"

# 4. Test query
dotnet run -- --cerebro-query think "neural"

# 5. Verify memory usage is lower than before
# (Should see neurons regenerating instead of all loading at once)
```

**Success Criteria:**
- ✅ Queries still work
- ✅ Console shows "Regenerating X neurons from procedural bank"
- ✅ Memory usage during query is lower (check with Activity Monitor)

---

## Phase 3: Lazy Synaptic Graph with Partitions

**Goal:** Don't load all 699K synapses upfront - stream them on-demand during cascade

**Time Estimate:** 4-6 hours implementation + testing

### Step 1: Partition Synapses by Source Neuron

**File:** `Storage/EnhancedBrainStorage.cs`

**Add new method:**
```csharp
public async Task SaveSynapsesPartitionedAsync(Dictionary<Guid, Synapse> synapses)
{
    const int NUM_PARTITIONS = 256;
    
    // Group synapses by source neuron hash
    var partitions = new Dictionary<int, List<SynapseSnapshot>>();
    
    foreach (var synapse in synapses.Values)
    {
        var partition = Math.Abs(synapse.PresynapticNeuronId.GetHashCode()) % NUM_PARTITIONS;
        
        if (!partitions.ContainsKey(partition))
            partitions[partition] = new List<SynapseSnapshot>();
            
        partitions[partition].Add(synapse.ToSnapshot());
    }
    
    // Save each partition to separate file
    foreach (var (partitionId, synapseList) in partitions)
    {
        var partitionPath = Path.Combine(_brainStoragePath, "hierarchical", $"synapses_part_{partitionId:D3}.msgpack.gz");
        await SaveCompressedMessagePack(partitionPath, synapseList);
    }
}
```

### Step 2: Modify Cerebro to Stream Synapses

**File:** `Core/Cerebro.cs` around line 357 (LoadFromStorageAsync)

**Current:**
```csharp
// Load ALL synapses at once (BAD - 699K synapses!)
var synapseSnapshots = await _storage.LoadSynapsesAsync();
foreach (var snapshot in synapseSnapshots)
{
    _synapses[snapshot.Id] = Synapse.FromSnapshot(snapshot);
}
```

**New:**
```csharp
// DON'T load synapses upfront - just note they exist
Console.WriteLine("🔗 Synapses configured for lazy loading (streaming during queries)");
_synapsesAvailable = true;  // Flag to indicate synapses exist
```

### Step 3: Stream During Cascade

**File:** `Core/Cerebro.cs` in `PropagateActivationThroughSynapticGraph`

**Add:**
```csharp
private IEnumerable<(Guid targetId, double weight)> GetOutgoingSynapsesLazy(Guid sourceId)
{
    // Calculate which partition this neuron's synapses are in
    const int NUM_PARTITIONS = 256;
    var partition = Math.Abs(sourceId.GetHashCode()) % NUM_PARTITIONS;
    
    // Stream from partition file (memory-mapped for efficiency)
    var partitionPath = Path.Combine(_storageBasePath, "hierarchical", $"synapses_part_{partition:D3}.msgpack.gz");
    
    if (!File.Exists(partitionPath))
        yield break;
    
    // Read partition, filter for this source neuron
    var allSynapses = LoadCompressedMessagePack<List<SynapseSnapshot>>(partitionPath);
    
    foreach (var synapse in allSynapses)
    {
        if (synapse.PresynapticNeuronId == sourceId)
        {
            yield return (synapse.PostsynapticNeuronId, synapse.Weight);
        }
    }
}
```

### Step 4: Validation

```bash
# 1. Start fresh
mv /Volumes/jarvis/brainData /Volumes/jarvis/brainData_before_phase3_$(date +%Y%m%d)

# 2. Train
dotnet run -- --production-training --duration 600

# 3. Check partitioned files exist
ls -lh /Volumes/jarvis/brainData/hierarchical/synapses_part_*.msgpack.gz | wc -l
# Should show 256 files

# 4. Test query
dotnet run -- --cerebro-query think "neural"

# 5. Memory check - should be MUCH lower than before
# Before: Loads all 699K synapses (280MB+ in RAM)
# After: Loads only needed partitions (few MB)
```

**Success Criteria:**
- ✅ Queries still work
- ✅ Cascade propagation depth >0
- ✅ Memory usage drastically reduced (check with Activity Monitor during query)
- ✅ 256 partition files exist

---

## Phase 4: LRU Cluster Eviction

**Goal:** Prevent unbounded memory growth - evict least-recently-used clusters

**Time Estimate:** 2-3 hours implementation + testing

### Step 1: Add LRU Tracking to Cerebro

**File:** `Core/Cerebro.cs`

**Add fields:**
```csharp
private readonly int _maxLoadedClusters = 100;  // Configurable
private readonly LinkedList<string> _clusterAccessOrder = new();
private readonly Dictionary<string, LinkedListNode<string>> _clusterAccessNodes = new();
```

### Step 2: Track Access in LoadTrainedNeuronsForConcept

**Find:** Method around line 1646

**Add after cluster load:**
```csharp
private async Task<List<NeuronCluster>> LoadTrainedNeuronsForConcept(...)
{
    // ... existing loading code ...
    
    // Mark this cluster as recently accessed
    TouchCluster(cluster.ClusterId);
    
    // Evict if we've exceeded capacity
    if (_loadedClusters.Count > _maxLoadedClusters)
    {
        await EvictLRUCluster();
    }
}

private void TouchCluster(string clusterId)
{
    // Move to front of LRU list
    if (_clusterAccessNodes.TryGetValue(clusterId, out var node))
    {
        _clusterAccessOrder.Remove(node);
    }
    
    var newNode = _clusterAccessOrder.AddFirst(clusterId);
    _clusterAccessNodes[clusterId] = newNode;
}

private async Task EvictLRUCluster()
{
    // Remove least recently used (back of list)
    var lruClusterId = _clusterAccessOrder.Last?.Value;
    if (lruClusterId == null) return;
    
    if (_loadedClusters.TryGetValue(lruClusterId, out var cluster))
    {
        // Save if dirty before evicting
        if (cluster.IsDirty)
        {
            await _storage.SaveClusterAsync(cluster);
        }
        
        _loadedClusters.Remove(lruClusterId);
        _clusterAccessOrder.RemoveLast();
        _clusterAccessNodes.Remove(lruClusterId);
        
        Console.WriteLine($"♻️ Evicted LRU cluster: {lruClusterId}");
    }
}
```

### Step 3: Validation

```bash
# 1. Configure small limit for testing
# Edit BrainConfiguration.cs: MaxLoadedClusters = 10

# 2. Run long training (should trigger evictions)
dotnet run -- --production-training --duration 600

# 3. Watch for eviction messages
grep "♻️ Evicted LRU cluster" training.log | wc -l
# Should show evictions happened

# 4. Check memory stays bounded
# Run Activity Monitor, watch memory usage stays flat (not growing)
```

**Success Criteria:**
- ✅ Training completes successfully
- ✅ Eviction messages appear in logs
- ✅ Memory usage stays bounded (doesn't grow indefinitely)
- ✅ Queries still work after evictions

---

## Testing Strategy

### Quick Smoke Test (5 minutes)
```bash
# After each phase
dotnet run -- --production-training --duration 60
dotnet run -- --cerebro-query think "neural"
dotnet run -- --cerebro-query think "you"
dotnet run -- --cerebro-query think "asdfghjkl"  # Novel word
```

### Full Validation (30 minutes)
```bash
# Train longer
dotnet run -- --production-training --duration 1800  # 30 min

# Test multiple queries
for word in "neural" "language" "brain" "learning" "novel" "xyz"; do
    echo "=== Testing: $word ==="
    dotnet run -- --cerebro-query think "$word"
done

# Memory monitoring
ps aux | grep dotnet
# Watch memory column - should stay bounded
```

---

## Rollback Plan

If any phase breaks:

```bash
# 1. Keep the backup
ls -d /Volumes/jarvis/brainData_*

# 2. Restore previous version
rm -rf /Volumes/jarvis/brainData
mv /Volumes/jarvis/brainData_before_phase_X /Volumes/jarvis/brainData

# 3. Revert code changes
git diff HEAD~1 Core/Cerebro.cs
git checkout HEAD~1 Core/Cerebro.cs
```

---

## Expected Final State

**After Phase 2:**
- Neurons regenerate on-demand from procedural banks
- Memory usage lower during queries
- Still loads all synapses (Phase 3 fixes this)

**After Phase 3:**
- Synapses stream on-demand during cascade
- Massive memory reduction (280MB → few MB)
- Still might accumulate clusters (Phase 4 fixes this)

**After Phase 4:**
- Bounded memory usage (caps at ~100 clusters)
- LRU eviction prevents growth
- System can run indefinitely without memory leak

**Final Architecture:**
```
Query "neural" → 
  Load seed neurons (procedural regeneration) → 
  Stream synapses (partition-by-partition) → 
  Load target neurons (procedural regeneration) →
  Evict LRU clusters (bounded memory)
```

Memory: **O(active_set)** not O(total_data) ✅
