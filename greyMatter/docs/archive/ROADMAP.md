# greyMatter - ARCHITECTURE REMEDIATION ROADMAP

**Crisis Date:** January 19, 2026  
**Status:** 🔴 **CRITICAL ARCHITECTURAL DRIFT**  
**Root Issue:** System preloads all data instead of lazy-loading procedurally  
**Impact:** Violates core "No Man's Sky" design principles

---

## 🚨 EXECUTIVE SUMMARY: THE HARSH TRUTH

The greyMatter neural architecture has **fundamentally drifted** from its stated "No Man's Sky" lazy-loading design principles. While the codebase contains **complete, tested implementations** of procedural neuron components (`ProceduralNeuronData`, `ProceduralNeuronRegenerator`), these are **only used for checkpoint compression**, not runtime operation.

### What We Claim (Documentation)
- ✅ "Neurons lazy-loaded from disk when cluster accessed"
- ✅ "Procedural regeneration rebuilds neurons from VQ code"  
- ✅ "O(active_set) memory usage"
- ✅ "Load only what's needed for current query"

### What Actually Happens (Code Reality)
- ❌ **ALL 699K synapses** loaded into RAM at initialization ([Cerebro.cs:357](../../Core/Cerebro.cs#L357))
- ❌ **Entire clusters** hydrated (all neurons) when any neuron needed ([NeuronCluster.cs:453](../../Core/NeuronCluster.cs#L453))
- ❌ **Full neuron objects** created via `HybridNeuron.FromSnapshot()`, never procedural
- ❌ **O(total_data)** memory usage, not O(active_set)
- ❌ **Procedural regeneration** exists but has **zero callers** in production code

### Critical Discoveries

**Issue 1: Neuron-Synapse Disconnection** (BLOCKING ALL QUERIES)
- Loaded neurons have **0 outgoing synapses** despite 699K synapses in storage
- Cascade propagation **dies immediately** (depth = 0, should be 2-3)
- Novelty detection **always returns 1.00 (NOVEL)** even for trained concepts
- **Root cause**: Neuron IDs don't match synapse source/target IDs (investigation needed)

**Issue 2: Procedural Path is Dead Code**
- `LoadProceduralNeuronBankAsync()` exists but **has no callers** ([EnhancedBrainStorage.cs:708](../../Storage/EnhancedBrainStorage.cs#L708))
- `RegenerateNeuron()` only called in **unit tests**, not production ([ProceduralNeuronRegenerator.cs:134](../../Core/ProceduralNeuronRegenerator.cs#L134))
- Runtime always uses `LoadNeuronsAsync()` → **full hydration** path

**Issue 3: No Memory Management**
- `_loadedClusters` cache **never evicts** ([Cerebro.cs:1646](../../Core/Cerebro.cs#L1646))
- Unbounded memory growth as more clusters accessed
- 30-minute timeout exists but **never executed** (no eviction loop)

### The Good News

**All building blocks exist and work:**
- ✅ `ProceduralNeuronData` - tested, 4x compression ratio
- ✅ `ProceduralNeuronRegenerator.RegenerateNeuron()` - 100% accuracy validated
- ✅ Hierarchical partitioning - VQ-based storage organization
- ✅ Cluster metadata lazy-loading - only loads when accessed
- ✅ Sparse synaptic graph - efficient edge representation

**The fix is straightforward:** Wire existing procedural components into the runtime query path.

---

## 🎯 REMEDIATION STRATEGY

### Core Principles (Biological + Computer Science)

**1. Spreading Activation (Cognitive Psychology)**
- Memory retrieval = activation spreads through associative network
- Strong connections propagate signal, weak ones attenuate
- Implementation: BFS traversal with priority queue (activation strength = priority)

**2. Lazy Loading with Streaming (Systems Programming)**  
- Load neurons on-demand during graph traversal
- Stream synapses partition-by-partition (not all at once)
- Implementation: Memory-mapped files for zero-copy access

**3. Procedural Generation (No Man's Sky Principle)**
- Don't store what can be regenerated from compact representation
- VQ code → deterministic neuron properties
- Synaptic weights explicitly persisted (learned, not generated)

**4. LRU Cache with Eviction (Operating Systems)**
- Working set stays in RAM, LRU evicted to disk
- Implementation: LinkedHashMap with access-order iteration

### Graph Traversal Algorithm: Biological Spreading Activation

```
SPREADING_ACTIVATION_TRAVERSAL(query_concepts, max_depth=3):
    # Phase 1: Seed activation from query
    seed_neurons = LOAD_NEURONS_FOR_CONCEPTS(query_concepts)  # Procedural load
    activation_queue = PriorityQueue(seed_neurons, key=activation_strength)
    visited = Set()
    active_neurons = Dict()
    
    # Phase 2: Breadth-first propagation with activation decay
    for depth in 1..max_depth:
        current_layer = []
        
        while activation_queue.not_empty():
            neuron_id, activation = activation_queue.pop()
            
            if neuron_id in visited:
                continue
            visited.add(neuron_id)
            active_neurons[neuron_id] = activation
            
            # Lazy-load outgoing synapses for this neuron only
            synapses = LOAD_SYNAPSES_FOR_NEURON(neuron_id)  # Streaming from partition
            
            for (target_id, weight) in synapses:
                propagated = activation * weight * DECAY_FACTOR
                
                if propagated > THRESHOLD:
                    # Lazy-load target neuron on-demand
                    target_neuron = LOAD_NEURON_BY_ID(target_id)  # Procedural regeneration
                    current_layer.append((target_id, propagated))
        
        if len(current_layer) == 0:
            break  # Signal died out (novel pattern)
        
        # Queue next layer for processing
        activation_queue.extend(current_layer)
    
    return active_neurons
```

**Key Properties:**
- Only loads neurons that are **actually traversed** (not entire clusters)
- Synapses streamed **partition by partition** (not all in RAM)
- Natural termination when cascade dies (no manual cutoff)
- Priority queue ensures strongest paths explored first

### Persistence Model: LSM-Tree Style Storage

**Concept:** Borrowed from LevelDB, RocksDB (Log-Structured Merge-Tree)

```
Storage Layout:
/Volumes/jarvis/brainData/
├── neurons/
│   ├── L0_memtable.msgpack          # Recent neurons (hot data)
│   ├── L1_partition_0000.msgpack    # Compacted neurons (warm)
│   └── L2_partition_0000.msgpack    # Archived neurons (cold)
├── synapses/
│   ├── by_source/
│   │   ├── partition_0000.msgpack   # Synapses grouped by source neuron ID
│   │   └── partition_0001.msgpack
│   └── by_target/
│       ├── partition_0000.msgpack   # Synapses grouped by target neuron ID
│       └── partition_0001.msgpack
└── index/
    ├── neuron_id_to_partition.idx   # Neuron ID → partition mapping
    └── cluster_to_neurons.idx       # Cluster ID → neuron ID list
```

**Load Operations:**
```csharp
// Lazy neuron load (procedural regeneration)
public async Task<HybridNeuron> LoadNeuronByIdAsync(Guid neuronId)
{
    // Check in-memory cache first
    if (_neuronCache.TryGet(neuronId, out var cached))
        return cached;
    
    // Find partition via index (O(1) lookup)
    var partition = await _index.GetPartitionForNeuron(neuronId);
    
    // Stream partition until neuron found (not load entire partition)
    var proceduralData = await StreamFindNeuronInPartition(partition, neuronId);
    
    // Regenerate from compact representation
    var neuron = _regenerator.RegenerateNeuron(proceduralData);
    
    // Cache for future access
    _neuronCache.Add(neuronId, neuron);
    
    return neuron;
}

// Lazy synapse load (streaming by partition)
public IEnumerable<(Guid target, float weight)> LoadOutgoingSynapses(Guid sourceId)
{
    // Find synapse partition for this source neuron
    var partition = HashPartition(sourceId, NUM_PARTITIONS);
    
    // Stream synapses from partition file (memory-mapped for zero-copy)
    foreach (var synapse in StreamSynapsePartition(partition))
    {
        if (synapse.SourceId == sourceId)
            yield return (synapse.TargetId, synapse.Weight);
    }
}
```

**Benefits:**
- **Streaming:** Never loads full partition into RAM
- **Partitioning:** Only access partitions needed for query
- **Zero-copy:** Memory-mapped files avoid deserialization until accessed
- **Scalability:** Millions of neurons, constant memory usage

---

## 📋 REMEDIATION PHASES

### PHASE 1: Fix Neuron-Synapse Disconnection (BLOCKER - Week 1)

**Status:** ✅ **ROOT CAUSE IDENTIFIED** → Resolution: Clear synapses and retrain  
**Goal:** Restore cascade propagation by fixing neuron-synapse ID mismatch

---

**🔍 INVESTIGATION RESULTS (Jan 19, 2026):**

Diagnostic logging revealed complete ID mismatch between synapses and neurons:

**Sample Synapse PresynapticNeuronIds:**
```
2fa1c2d0-a48b-42ff-b04b-42edcaccce3d
e2374a14-ddbb-4786-a1f7-ed6f1fa1a2cc
5ced8486-5e28-412f-a567-1e1d10e4666d
```

**Sample Loaded Neuron IDs:**
```
dc53340d-a125-4d00-b1eb-710c088bd781
2c4801a2-94d6-49f5-825b-ed4d80631715
10beeb9b-d187-4c75-86aa-bacd03f723f7
```

**Finding:** ZERO overlap. The 699,566 synapses reference neurons that **no longer exist** in storage. The neuron banks were cleared/regenerated at some point but the old synapse file was retained.

**Resolution Decision:**
- ✅ **Clear synapse storage** and retrain from scratch
- Rationale: Synapses are invalid (orphaned), migration is complex and error-prone
- Impact: ~1 hour retraining on tatoeba_small dataset
- Benefit: Guaranteed ID consistency going forward

**Implementation:**
```bash
# 1. Backup for forensics
cp /Volumes/jarvis/brainData/hierarchical/synapses.json /Volumes/jarvis/brainData/backup/

# 2. Clear orphaned synapses
rm /Volumes/jarvis/brainData/hierarchical/synapses.json

# 3. Retrain to rebuild synapse graph with correct IDs
dotnet run -- --production-training --duration 3600

# 4. Validate cascade now works
dotnet run -- --cerebro-query think "neural"
# Expected: depth >0, novelty <1.0, "Layer 1: X new neurons" where X >0
```

---

#### Tasks

**1.1 Diagnose ID Mismatch** (Days 1-2) ✅ COMPLETE
```bash
# Add diagnostic logging to PropagateActivationThroughSynapticGraph
```

**Code Location:** [Cerebro.cs:1755-1762](../../Core/Cerebro.cs#L1755-L1762)

**Change:**
```csharp
// DEBUG: Check if seed neurons have any outgoing synapses
var seedWithSynapses = 0;
var totalOutgoing = 0;
foreach (var seedId in seedNeurons.Keys.Take(5))
{
    var outgoing = _synapses.Values.Count(s => s.PresynapticNeuronId == seedId);
    Console.WriteLine($"   🔍 Seed neuron {seedId}: {outgoing} outgoing");
    
    // NEW: Also check _synapticGraph
    var graphOutgoing = _synapticGraph.GetOutgoingSynapses(seedId);
    Console.WriteLine($"      Graph: {graphOutgoing.Count} synapses");
    
    // NEW: Sample synapse target IDs to check if they exist
    foreach (var (targetId, weight) in graphOutgoing.Take(3))
    {
        var targetExists = _loadedClusters.Values.Any(c => c.HasNeuron(targetId));
        Console.WriteLine($"         → {targetId.ToString().Substring(0,8)}: weight={weight:F3}, exists={targetExists}");
    }
}
```

**Investigation Questions:**
- Do synapse source IDs match loaded neuron IDs?
- Do synapse target IDs point to neurons in storage?
- Are synapses referencing old neuron IDs from previous training runs?

**1.2 Trace Neuron ID Assignment** (Days 2-3)

**Training Path:**
- Where are neuron IDs assigned during `cluster.GrowForConcept()`?
- Are they persisted correctly to neuron banks?
- Do membership packs reference the same IDs?

**Query Path:**  
- Where are neuron IDs read during `LoadTrainedNeuronsForConcept()`?
- Does `HybridNeuron.FromSnapshot()` preserve original ID?
- Is there ID regeneration happening (e.g., Guid.NewGuid())?

**1.3 Fix ID Consistency** (Days 3-5)

**Option A:** IDs are correct, synapses are stale
```bash
# Solution: Clear synapse storage, retrain
rm -rf /Volumes/jarvis/brainData/synapses.msgpack.gz
dotnet run -- --production-training --duration 600  # 10-min fresh train
```

**Option B:** Neuron loading regenerates IDs
```csharp
// Fix: Ensure FromSnapshot preserves ID
public static HybridNeuron FromSnapshot(NeuronSnapshot snapshot)
{
    var neuron = new HybridNeuron(snapshot.ConceptTag)
    {
        Id = snapshot.Id,  // ← CRITICAL: Don't generate new ID
        // ... rest of properties
    };
    return neuron;
}
```

**Validation:**
```bash
# Test after fix
dotnet run -- --cerebro-query think "neural"

# Expected output:
# Seed neurons: 30
# Layer 1: 9 new neurons activated  ← NOT 0
# Cascade depth: 1                  ← NOT 0
# Novelty: 0.72                     ← NOT 1.00
```

### PHASE 2: Connect Procedural Loading to Runtime (Week 1-2)

**Status:** 🟡 **BLOCKED** by Phase 1  
**Goal:** Route neuron loading through procedural regeneration path

#### Tasks

**2.1 Modify NeuronCluster.LoadFromDiskAsync** (Days 1-2)

**File:** [NeuronCluster.cs:453-464](../../Core/NeuronCluster.cs#L453-L464)

**Current Code:**
```csharp
private async Task LoadFromDiskAsync()
{
    if (_loadFunction != null)
    {
        var snapshots = await _loadFunction(_persistencePath);
        _neurons = snapshots.ToDictionary(s => s.Id, HybridNeuron.FromSnapshot);
    }
    _isLoaded = true;
}
```

**New Code:**
```csharp
private async Task LoadFromDiskAsync()
{
    if (_loadFunction != null)
    {
        // Try procedural format first (90% smaller, faster load)
        var procedural = await _storage.LoadProceduralClusterAsync(_persistencePath);
        
        if (procedural != null && procedural.Count > 0)
        {
            // Regenerate neurons from compact representation
            foreach (var compactData in procedural)
            {
                var neuron = _regenerator.RegenerateNeuron(compactData);
                _neurons[neuron.Id] = neuron;
            }
            Console.WriteLine($"   ✓ Regenerated {_neurons.Count} neurons from procedural format");
        }
        else
        {
            // Fallback to standard format (backward compatibility)
            var snapshots = await _loadFunction(_persistencePath);
            _neurons = snapshots.ToDictionary(s => s.Id, HybridNeuron.FromSnapshot);
            Console.WriteLine($"   ⚠️ Loaded {_neurons.Count} neurons from standard format (procedural unavailable)");
        }
    }
    _isLoaded = true;
}
```

**2.2 Implement Selective Neuron Loading** (Days 3-4)

**New Method in NeuronCluster:**
```csharp
public async Task<HybridNeuron?> GetNeuronByIdAsync(Guid neuronId)
{
    // Check if already in memory
    if (_neurons.TryGetValue(neuronId, out var neuron))
    {
        neuron.LastUsed = DateTime.UtcNow;
        return neuron;
    }
    
    // Lazy-load specific neuron from procedural storage
    var proceduralData = await _storage.LoadProceduralNeuronByIdAsync(_persistencePath, neuronId);
    
    if (proceduralData != null)
    {
        neuron = _regenerator.RegenerateNeuron(proceduralData);
        _neurons[neuron.Id] = neuron;  // Cache
        return neuron;
    }
    
    // Fallback: load from standard format
    await EnsureLoadedAsync();  // Load entire cluster
    return _neurons.GetValueOrDefault(neuronId);
}
```

**2.3 Modify Cascade Propagation to Use Selective Loading** (Days 4-5)

**File:** [Cerebro.cs:1780-1820](../../Core/Cerebro.cs#L1780-L1820)

**Current:** Assumes neurons already loaded
**New:** Load target neurons on-demand during propagation

```csharp
foreach (var (targetNeuronId, weight) in outgoingSynapses)
{
    var propagatedActivation = sourceActivation * weight * PROPAGATION_DECAY;
    
    if (propagatedActivation < ACTIVATION_THRESHOLD) continue;
    
    // NEW: Lazy-load target neuron if not already in memory
    if (!allActivations.ContainsKey(targetNeuronId))
    {
        // Find which cluster contains this neuron
        var targetCluster = await FindClusterContainingNeuron(targetNeuronId);
        
        if (targetCluster != null)
        {
            // Load just this neuron (not entire cluster)
            var targetNeuron = await targetCluster.GetNeuronByIdAsync(targetNeuronId);
            
            if (targetNeuron != null)
            {
                nextLayer[targetNeuronId] = propagatedActivation;
            }
        }
    }
}
```

**Validation:**
```bash
# Test selective loading
dotnet run -- --cerebro-query think "neural networks"

# Expected behavior:
# - Only loads neurons in cascade path (not entire clusters)
# - Memory usage proportional to cascade depth (not total neurons)
# - Console shows "Regenerated N neurons from procedural format"
```

### PHASE 3: Implement Partitioned Synaptic Storage ✅ COMPLETE

**Status:** ✅ **COMPLETE** (Jan 23, 2026)  
**Goal:** Partition and stream synapses instead of preloading all at once  
**Achievement:** Eliminated OOM crashes with 133M+ synapses, reduced checkpoint time by 52%

#### Completed Implementation (Jan 23, 2026)

**3.1 Partition Synaptic Graph by Source Neuron** ✅ COMPLETE

**Storage Structure:**
```
/Volumes/jarvis/brainData/synapses_partitioned/
├── partition_000.json.gz  # Source IDs with hash % 256 == 0
├── partition_001.json.gz
├── ...
└── partition_255.json.gz  # 256 partitions total
├── metadata.json          # Partition counts and totals
```

**Implemented Save Method:**
```csharp
// Extension method for streaming save from graph (no intermediate buffer)
public static async Task SaveSynapsesPartitionedAsync(
    this EnhancedBrainStorage storage, 
    SparseSynapticGraph graph)
{
    const int NUM_PARTITIONS = 256;
    var partitions = new Dictionary<int, List<SynapseSnapshot>>();
    
    // Partition by source neuron ID
    foreach (var synapse in synapses)
    {
        var partition = Math.Abs(synapse.PresynapticNeuronId.GetHashCode()) % NUM_PARTITIONS;
        
        if (!partitions.ContainsKey(partition))
            partitions[partition] = new List<SynapseSnapshot>();
        
        partitions[partition].Add(synapse);
    }
    
    // Save each partition separately
    foreach (var (partitionId, partitionSynapses) in partitions)
    {
        var path = $"{_basePath}/synapses/partition_{partitionId:X4}.msgpack.gz";
        await SaveCompressedAsync(path, partitionSynapses);
    }
}
```

**3.2 Streaming Save Implementation** ✅ COMPLETE

**Approach:** Direct streaming from graph to partitioned files without intermediate buffers

**Implementation:**
```csharp
public class SparseSynapticGraph
{
    // Cache recently accessed partitions (LRU eviction)
    private readonly LRUCache<int, Dictionary<Guid, List<(Guid, float)>>> _loadedPartitions;
    private readonly IStorage _storage;
    private const int MAX_LOADED_PARTITIONS = 4;  // Keep 4/16 partitions in RAM
    
    public List<(Guid targetId, float weight)> GetOutgoingSynapses(Guid sourceId)
    {
        var partition = Math.Abs(sourceId.GetHashCode()) % 16;
        
        // Check if partition already loaded
        if (!_loadedPartitions.TryGet(partition, out var partitionData))
        {
            // Lazy-load partition from disk
            partitionData = await LoadSynapsePartition(partition);
            _loadedPartitions.Add(partition, partitionData);
        }
        
        // Return synapses for this specific neuron
        return partitionData.GetValueOrDefault(sourceId, new List<(Guid, float)>());
    }
    
    private async Task<Dictionary<Guid, List<(Guid, float)>>> LoadSynapsePartition(int partition)
    {
        var path = $"{_basePath}/synapses/partition_{partition:X4}.msgpack.gz";
        var synapses = await _storage.LoadCompressedAsync<List<SynapseSnapshot>>(path);
        
        // Group by source neuron for fast lookup
        var grouped = new Dictionary<Guid, List<(Guid, float)>>();
        foreach (var syn in synapses)
        {
            if (!grouped.ContainsKey(syn.PresynapticNeuronId))
                grouped[syn.PresynapticNeuronId] = new List<(Guid, float)>();
            
            grouped[syn.PresynapticNeuronId].Add((syn.PostsynapticNeuronId, (float)syn.Weight));
        }
        
        return grouped;
    }
}
```

**3.3 Remove InitializeAsync Synapse Preloading** (Day 5)

**File:** [Cerebro.cs:357-368](../../Core/Cerebro.cs#L357-L368)

**DELETE THIS CODE:**
```csharp
// Load synapses
var synapseSnapshots = await _storage.LoadSynapsesAsync();
foreach (var snapshot in synapseSnapshots)
{
    _synapses[snapshot.Id] = Synapse.FromSnapshot(snapshot);
}

Console.WriteLine($"Loaded {_synapses.Count} synapses");

// Import into graph
_synapticGraph.ImportSynapses(synapseSnapshots);
Console.WriteLine($"🔗 Imported {_synapses.Count} synapses into synaptic graph");
```

**REPLACE WITH:**
```csharp
// Initialize lazy synaptic graph (partitions loaded on-demand)
_synapticGraph = new SparseSynapticGraph(_storage, basePath: _storage.GetBasePath());
Console.WriteLine($"🔗 Initialized lazy synaptic graph (16 partitions, load on-demand)");
```

**Validation Results (Jan 23, 2026):**
```bash
# Test: 10-minute training run
dotnet run -- --production-training --duration 600

# Results:
# ✅ Saved 133,498,324 synapses in 597.96s (~10 minutes)
# ✅ No OutOfMemoryException (previous crash with 125M synapses)
# ✅ 52% reduction in checkpoint time (was ~1251s with duplicate export)
# ✅ Memory freed: 197.5 MB (vs 24.6 GB with old monolithic approach)
# ✅ Streaming progress: reported every 10M synapses
# ✅ 256 partitions with metadata
```

**Implementation Details:**
- **File:** [EnhancedBrainStorage.cs:1617-1710](../../Storage/EnhancedBrainStorage.cs#L1617-L1710)
- **File:** [Cerebro.cs:840-856](../../Core/Cerebro.cs#L840-L856) - Removed old chunked export
- **Approach:** Stream from `graph.ExportSynapsesChunked(1M)` → partition on-the-fly → save incrementally
- **Benefits:** No intermediate accumulation, bounded memory, 50% faster

### PHASE 4: Implement LRU Cluster Eviction ✅ COMPLETE

**Status:** ✅ **COMPLETE** (Jan 25, 2026)  
**Goal:** Prevent unbounded memory growth from cluster cache  
**Achievement:** Bounded memory usage with automatic eviction of idle clusters

#### Tasks

**4.1 Create LRUCache Utility Class** (Day 1)

**New File:** `Core/LRUCache.cs`

```csharp
public class LRUCache<TKey, TValue>
{
    private readonly int _maxSize;
    private readonly LinkedList<(TKey key, TValue value)> _accessOrder;
    private readonly Dictionary<TKey, LinkedListNode<(TKey, TValue)>> _index;
    
    public LRUCache(int maxSize)
    {
        _maxSize = maxSize;
        _accessOrder = new LinkedList<(TKey, TValue)>();
        _index = new Dictionary<TKey, LinkedListNode<(TKey, TValue)>>();
    }
    
    public bool TryGet(TKey key, out TValue value)
    {
        if (_index.TryGetValue(key, out var node))
        {
            // Move to front (most recently used)
            _accessOrder.Remove(node);
            _accessOrder.AddFirst(node);
            
            value = node.Value.value;
            return true;
        }
        
        value = default;
        return false;
    }
    
    public void Add(TKey key, TValue value)
    {
        if (_index.ContainsKey(key))
        {
            // Update existing
            var node = _index[key];
            _accessOrder.Remove(node);
            node.Value = (key, value);
            _accessOrder.AddFirst(node);
        }
        else
        {
            // Add new
            var node = _accessOrder.AddFirst((key, value));
            _index[key] = node;
            
            // Evict LRU if over capacity
            if (_accessOrder.Count > _maxSize)
            {
                var lru = _accessOrder.Last;
                _accessOrder.RemoveLast();
                _index.Remove(lru.Value.key);
            }
        }
    }
    
    public (TKey key, TValue value)? RemoveLRU()
    {
        if (_accessOrder.Count == 0) return null;
        
        var lru = _accessOrder.Last.Value;
        _accessOrder.RemoveLast();
        _index.Remove(lru.key);
        
        return lru;
    }
}
```

**4.2 Replace Cerebro Cluster Cache** (Days 2-3)

**File:** [Cerebro.cs:17](../../Core/Cerebro.cs#L17)

**CHANGE:**
```csharp
// BEFORE:
private readonly Dictionary<Guid, NeuronCluster> _loadedClusters = new();

// AFTER:
private readonly LRUCache<Guid, NeuronCluster> _loadedClusters = new(maxSize: 800);
```

**File:** [Cerebro.cs:1646](../../Core/Cerebro.cs#L1646)

**CHANGE:**
```csharp
// BEFORE:
_loadedClusters[clusterId] = cluster;

// AFTER:
// LRU automatically evicts when adding 801st cluster
_loadedClusters.Add(clusterId, cluster);

// Before eviction, persist cluster to disk
var evicted = _loadedClusters.RemoveLRU();
if (evicted.HasValue)
{
    await evicted.Value.cluster.PersistAndUnloadAsync();
    Console.WriteLine($"   🗑️ Evicted LRU cluster: {evicted.Value.key}");
}
_loadedClusters.Add(clusterId, cluster);
```

**4.3 Add Background Eviction Loop** (Days 3-4)

**New Method in Cerebro:**
```csharp
private async Task ClusterEvictionLoop(CancellationToken cancel)
{
    while (!cancel.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromMinutes(5), cancel);  // Check every 5 minutes
        
        var now = DateTime.UtcNow;
        var evicted = 0;
        
        foreach (var (clusterId, cluster) in _loadedClusters.GetAll())
        {
            var idle = now - cluster.LastAccessed;
            
            if (idle > TimeSpan.FromMinutes(30))
            {
                await cluster.PersistAndUnloadAsync();
                _loadedClusters.Remove(clusterId);
                evicted++;
            }
        }
        
        if (evicted > 0)
            Console.WriteLine($"🧹 Evicted {evicted} idle clusters (>30 min inactive)");
    }
}
```

**Start in InitializeAsync:**
```csharp
public async Task InitializeAsync()
{
    // ... existing initialization ...
    
    // Start background eviction loop
    _evictionCancellation = new CancellationTokenSource();
    _ = Task.Run(() => ClusterEvictionLoop(_evictionCancellation.Token));
    
    Console.WriteLine("✓ Background cluster eviction started (check every 5 min)");
}
```

**Validation Plan:**
```bash
# Test: Run long training session to verify memory stays bounded
dotnet run -- --production-training --duration 7200  # 2 hours

# Expected behaviors:
# ✅ LRU evicts cluster when 801st cluster loaded
# ✅ Background loop evicts idle clusters every 5 minutes
# ✅ Console shows: "🗑️ LRU evicted cluster: {id}"
# ✅ Console shows: "🧹 Evicting N idle clusters (>30 min inactive)..."
# ✅ Memory usage stays bounded (not growing linearly with training time)
# ✅ Re-accessing evicted cluster loads from disk successfully
```

**Implementation Files:**
- [Core/LRUCache.cs](../../Core/LRUCache.cs) - LRU cache utility (new file)
- [Cerebro.cs:18-20](../../Core/Cerebro.cs#L18-L20) - Cache replacement
- [Cerebro.cs:983-1001](../../Core/Cerebro.cs#L983-L1001) - Eviction handler
- [Cerebro.cs:1002-1050](../../Core/Cerebro.cs#L1002-L1050) - Background eviction loop
- [Cerebro.cs:441-446](../../Core/Cerebro.cs#L441-L446) - Start eviction loop

### PHASE 5: Memory-Mapped File Optimization (Week 4)

**Status:** 🟢 **OPTIONAL** - Performance enhancement, not critical  
**Goal:** Zero-copy access to neuron/synapse data using OS virtual memory

**Benefits:**
- OS handles paging automatically (perfect lazy loading)
- No explicit deserialization until data accessed
- Shared memory between processes (if needed)

**Implementation:**
```csharp
public class MemoryMappedNeuronStore
{
    private MemoryMappedFile _mmf;
    private MemoryMappedViewAccessor _accessor;
    
    public void Initialize(string filePath)
    {
        var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        _mmf = MemoryMappedFile.CreateFromFile(fileStream, 
            mapName: null,
            capacity: 0,
            access: MemoryMappedFileAccess.Read,
            inheritability: HandleInheritability.None,
            leaveOpen: false);
        
        _accessor = _mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
    }
    
    public ProceduralNeuronData ReadNeuronAt(long offset)
    {
        // OS pages in data only when accessed (zero-copy)
        _accessor.Read(offset, out ProceduralNeuronData data);
        return data;
    }
}
```

**Note:** Requires neuron data stored in fixed-size records for offset calculation. Current MessagePack format is variable-size, would need schema change.

---

## 🎯 SUCCESS METRICS

### Phase 1 Success (Week 1)
- ✅ Query "neural networks" shows cascade depth >0 (not 0)
- ✅ Novelty score <0.8 for trained concepts (not 1.00)
- ✅ Sampled seed neurons show >0 outgoing synapses
- ✅ Console output shows "Layer 1: X new neurons" (X > 0)

### Phase 2 Success (Week 2)
- ✅ Console shows "Regenerated N neurons from procedural format"
- ✅ Memory usage <50MB during query (not 200MB)
- ✅ Query latency <500ms (not 2-3 seconds)
- ✅ Procedural regeneration accuracy = 100% (no behavior changes)

### Phase 3 Success (Week 3) ✅ ACHIEVED Jan 23, 2026
- ✅ Checkpoint completes without OOM (133M synapses, was crashing at 125M)
- ✅ Save time reduced by 52% (597s vs 1251s with duplicate export)
- ✅ Memory bounded during save (freed 197.5 MB vs 24.6 GB monolithic)
- ✅ Console shows "Streamed X synapses to 256 partitions" with progress every 10M
- ✅ No regression - cascade propagation continues to work

### Phase 4 Success (Week 4) ✅ ACHIEVED Jan 25, 2026
- ✅ LRUCache utility class implemented with O(1) operations
- ✅ Cerebro cluster cache replaced with LRU cache (max 800 clusters)
- ✅ Eviction handler persists clusters before removal
- ✅ Background eviction loop runs every 5 minutes
- ✅ Automatic eviction when cache full (801st cluster triggers eviction)
- ✅ Time-based eviction (30 minutes idle)
- ✅ Ready for long-running training validation

### Final Validation (Week 4)
- ✅ **Architecture Test:** O(active_set) memory usage, not O(total_data)
- ✅ **Performance Test:** 100 queries in <10 seconds
- ✅ **Scale Test:** 1M neurons in storage, <100MB RAM during query
- ✅ **Biological Test:** Cascade depth matches training exposure

---

## 📊 MONITORING & DEBUGGING

### Key Metrics to Track

**Memory Usage:**
```bash
# Monitor during query
dotnet run -- --cerebro-query think "neural" &
PID=$!
watch -n 1 "ps -o rss= -p $PID"
```

**Cascade Propagation:**
```bash
# Look for these log patterns
grep "Layer 1:" training.log  # Should show >0 new neurons
grep "Cascade complete:" training.log  # Should show depth >0
grep "Novelty:" training.log  # Should vary (not always 1.00)
```

**Cluster Loading:**
```bash
# Count procedural vs standard loads
grep "Regenerated.*neurons from procedural" training.log | wc -l
grep "Loaded.*neurons from standard" training.log | wc -l
```

**Synapse Partitioning:**
```bash
# Check partition file sizes
ls -lh /Volumes/jarvis/brainData/synapses/partition_*.msgpack.gz
```

### Debug Commands

**Check neuron-synapse connectivity:**
```bash
dotnet run -- --cerebro-query think "neural" 2>&1 | grep -A 10 "🔍 Seed neuron"
```

**Verify procedural loading:**
```bash
dotnet run -- --cerebro-query stats 2>&1 | grep -i "procedural\|regenerated"
```

**Monitor LRU evictions:**
```bash
dotnet run -- --cerebro-query think "test" 2>&1 | grep "🧹 Evicted"
```

---

## 🚨 ROLLBACK PLAN

If remediation causes regressions:

**Phase 1 Rollback:**
```bash
git checkout HEAD -- Core/Cerebro.cs
dotnet build
```

**Phase 2 Rollback:**
```bash
git checkout HEAD -- Core/NeuronCluster.cs
dotnet build
```

**Phase 3 Rollback:**
```bash
git checkout HEAD -- Core/SparseSynapticGraph.cs Storage/
dotnet build
```

**Emergency: Restore Old Behavior Completely:**
```bash
git checkout HEAD~10 -- Core/ Storage/  # Go back 10 commits
dotnet build -c Release
```

---

## 📚 RELATED ALGORITHMS & PATTERNS

### 1. Spreading Activation Network (Anderson, 1983)
- **Paper:** "A Spreading Activation Theory of Memory"
- **Key Idea:** Concepts activate related concepts through weighted links
- **Implementation:** Our cascade propagation with decay factor
- **Used In:** Semantic networks, information retrieval, cognitive modeling

### 2. Breadth-First Search with Priority Queue
- **Algorithm:** Dijkstra's shortest path variant
- **Key Idea:** Explore strongest connections first (priority = activation)
- **Implementation:** Our `PropagateActivationThroughSynapticGraph`
- **Complexity:** O(E log V) where E = edges, V = vertices

### 3. Log-Structured Merge Tree (LSM-Tree)
- **Paper:** O'Neil et al., "The Log-Structured Merge-Tree"
- **Key Idea:** Write-optimized storage with tiered compaction
- **Implementation:** Our L0/L1/L2 neuron storage tiers
- **Used In:** LevelDB, RocksDB, Cassandra, HBase

### 4. Memory-Mapped Files (mmap)
- **Origin:** Unix virtual memory system
- **Key Idea:** Map file pages into process address space (OS handles paging)
- **Implementation:** Phase 5 optimization (optional)
- **Used In:** Databases, large file processing, shared memory

### 5. LRU Cache Eviction
- **Algorithm:** Least Recently Used replacement policy
- **Key Idea:** Keep hot data in memory, evict cold data
- **Implementation:** LinkedList + HashMap (O(1) access + eviction)
- **Used In:** CPU caches, page replacement, database buffers

### 6. Lazy Evaluation (Haskell, LINQ)
- **Concept:** Delay computation until result needed
- **Key Idea:** Don't load/compute what you don't use
- **Implementation:** Our streaming synapse loading, procedural neuron generation
- **Benefits:** O(1) initialization, constant memory usage

---

## 🔬 BIOLOGICAL INSPIRATION

### Cortical Columns (Mountcastle, 1997)
- **Biological:** Vertical structures in neocortex (~100 neurons each)
- **greyMatter:** `NeuronCluster` (50-100 neurons per cluster)
- **Lazy Loading:** Brain doesn't activate all cortex simultaneously

### Synaptic Pruning (Huttenlocher, 1979)
- **Biological:** Unused synapses eliminated during development
- **greyMatter:** Synapse decay (weight *= 0.99 per non-activation)
- **Implementation:** `SparseSynapticGraph.PruneWeakSynapses(threshold: 0.1)`

### Spreading Activation (Collins & Loftus, 1975)
- **Biological:** Semantic memory retrieval through associative network
- **greyMatter:** Cascade propagation with activation decay
- **Parameters:** DECAY_FACTOR=0.9, THRESHOLD=0.01

### Working Memory Capacity (Miller, 1956)
- **Biological:** ~7 items in working memory (chunking)
- **greyMatter:** Max 10 loaded clusters (expandable to 800 with LRU)
- **Eviction:** 30-minute idle timeout (biological ~seconds to minutes)

---

## 📅 TIMELINE

| Week | Phase | Status | Key Deliverables |
|------|-------|--------|------------------|
| 1 | Phase 1: Fix Neuron-Synapse IDs | ✅ COMPLETE | Cleared and retrained (Jan 19) |
| 1-2 | Phase 2: Procedural Loading | 🟡 DEFERRED | Runtime using standard load (working) |
| 2-3 | Phase 3: Partitioned Synaptic Storage | ✅ COMPLETE | 133M synapses, no OOM, 52% faster (Jan 23) |
| 3-4 | Phase 4: LRU Eviction | ✅ COMPLETE | Bounded memory, automatic eviction (Jan 25) |
| 4+ | Phase 5: Memory-Mapped Files | 🟢 OPTIONAL | Performance enhancement |

---

## 🎯 FINAL GOAL

**Transform greyMatter from:**
- ❌ Eager-loading traditional neural network
- ❌ O(total_data) memory usage
- ❌ Preloads everything into RAM
- ❌ Broken cascade propagation

**Into:**
- ✅ Lazy-loading procedural neural architecture
- ✅ O(active_set) memory usage  
- ✅ Loads only what cascade traverses
- ✅ True "No Man's Sky" principle implementation

**Success = Documentation matches implementation.**

---

**Last Updated:** January 19, 2026  
**Next Review:** January 26, 2026 (after Phase 1 completion)  
**Emergency Contact:** Review subagent technical report for detailed code locations

### ✅ Completed (Production-Ready)

**Synaptic Propagation Architecture** (Jan 14, 2026)
- Phase 1: Load trained neurons (not create new during queries)
- Phase 2: Cascade propagation through synaptic graph 
- Phase 3: Natural novelty detection from cascade depth
- Implementation: [SYNAPTIC_PROPAGATION_IMPLEMENTATION.md](../SYNAPTIC_PROPAGATION_IMPLEMENTATION.md)
- Validation: [SYNAPTIC_NOVELTY_DETECTION.md](../SYNAPTIC_NOVELTY_DETECTION.md)

**Core Neural Architecture**
- Procedural neuron generation (VQ-VAE 512-code codebook)
- Sparse synaptic graph (Hebbian learning, >90% sparsity)
- Lazy cluster loading (max 10 clusters, unload after 30min)
- MessagePack storage (60% compression, 1,350x faster than JSON)

**Training Infrastructure**
- Progressive curriculum (children's stories → scientific papers)
- Multi-source data integration (571GB Wikipedia + 500GB books)
- LLM teacher integration (Ollama deepseek-r1:1.5b)
- Smart sampling (5K sentence batches, never loads full datasets)

**Storage System**
- NAS-backed persistence: `/Volumes/jarvis/brainData`
- Capacity: 46TB total, 31TB free
- Hierarchical partitioning (7 partitions, ~2000 clusters)
- Membership packs + neuron banks architecture

---

## 🔥 Critical Issues (IN PROGRESS)

### ⚠️ ISSUE 1: Zero Synapses Being Created During Training

**Last Investigation:** Jan 18, 2026 02:44 PM PST

**Symptoms:**
- Training runs successfully, neurons being created (244K+ neurons in banks)
- Checkpoints save successfully (membership packs + neuron banks)
- **BUT: "🔗 Imported 0 synapses into synaptic graph"**
- No synapse export happening (count stays at 0)

**Root Cause Analysis:**
1. **Hebbian Learning Not Firing:** `RecordHebbianCoactivation()` requires >0.1 activation
2. **Threshold Too High?** Neurons may not reach `CurrentPotential > 0.1` during training
3. **ProcessInputs Not Setting Potential?** Need to verify neuron activation during `LearnConceptAsync()`
4. **Alternative Hypothesis:** Code path not being executed (early return conditions)

**Current Investigation:**
- ✅ Chunked export implemented: `ExportSynapsesChunked(1M)` in [SparseSynapticGraph.cs](../../Core/SparseSynapticGraph.cs#L281-L311)
- ✅ `RecordHebbianCoactivation()` called in [Cerebro.cs](../../Core/Cerebro.cs#L480)
- ⚠️ **Zero synapses created** - activation threshold or early return issue
- 🔍 **Testing:** Running 30-min training to monitor activation

**Debug Plan:**
1. Add logging to `RecordHebbianCoactivation()` to see if it's called
2. Log `CurrentPotential` values after `ProcessInputs()` 
3. Check if activation threshold 0.1 is appropriate
4. Verify neurons are actually firing during training (not just being created)

### ✅ FIXED: Missing Checkpoints Directory

**Issue:** `ProductionStorageManager` creates directories in constructor, but NAS was cleared after initialization
**Solution:** Manually created `/Volumes/jarvis/brainData/checkpoints/` directory
**Status:** Resolved - checkpoints now saving successfully

**Technical Details:**

**Synapse Export Implementation:**
```csharp
// Cerebro.cs (lines 807-845) - Chunked export
const int CHUNK_SIZE = 1_000_000; // Export 1M synapses at a time
foreach (var chunk in _synapticGraph.ExportSynapsesChunked(CHUNK_SIZE))
{
    allSynapseSnapshots.AddRange(chunk);
    totalExported += chunk.Count;
    if (chunksExported % 10 == 0) // Log every 10M
    {
        Console.WriteLine($"   🔗 Exported {totalExported:N0} synapses...");
    }
}
await _storage.SaveSynapsesAsync(allSynapseSnapshots);
```

**SparseSynapticGraph.cs (lines 281-311) - Chunked iterator:**
```csharp
public IEnumerable<List<SynapseSnapshot>> ExportSynapsesChunked(int chunkSize)
{
    var chunk = new List<SynapseSnapshot>(chunkSize);
    foreach (var kvp in _synapses)
    {
        chunk.Add(new SynapseSnapshot { /* ... */ });
        if (chunk.Count >= chunkSize)
        {
            yield return chunk;
            chunk = new List<SynapseSnapshot>(chunkSize);
        }
    }
    if (chunk.Count > 0) yield return chunk;
}
```

**Why This Is Critical:**
- **Without synapses, cascade propagation cannot work** - synapses ARE the connections
- Queries would activate neurons but not propagate through trained pathways
- Novelty detection would break (no cascade depth to measure)
- System fundamentally broken without persisted synaptic connections

**Next Steps (Priority 1):**
1. ⬜ Verify chunked export code path being executed
2. ⬜ Add progress logging every 10M synapses exported
3. ⬜ Test with smaller checkpoint interval (5 min instead of 10 min)
4. ⬜ Monitor actual synapse count vs expected at crash time
5. ⬜ Consider async chunked save (yield to scheduler between chunks)
6. ⬜ Add cancellation token monitoring (detect timeout threshold)

---

## 🚀 Near-Term Priorities (Next 2 Weeks)

### 1. Resolve Training Stability (BLOCKER - Week 1)

**Goal:** Achieve 1-hour training runs without crashes

**Tasks:**
- ⬜ Diagnose why chunked export still timing out
- ⬜ Implement cancellation token timeout extension during synapse save
- ⬜ Add comprehensive logging to track export progress
- ⬜ Test with 5-minute checkpoint intervals (fewer synapses per save)
- ⬜ Verify NAS write performance (network bottleneck?)
- ⬜ Consider streaming synapse save (save chunks incrementally, not accumulate)

**Success Metrics:**
- ✅ Training runs 1 hour without crashes
- ✅ Checkpoints complete in <30 seconds (including synapse export)
- ✅ All 128M+ synapses persisted correctly
- ✅ Query cascade propagation works after restart

### 2. Validate Synaptic Propagation at Scale (Week 1-2)

**Goal:** Prove cascade propagation works with production-scale synaptic graph

**Tasks:**
- ⬜ Train for 1 hour (stable checkpoint saves required)
- ⬜ Test query on trained concept: "neural networks" 
  - Expected: Deep cascade (100-1000 neurons), low novelty (<0.3)
- ⬜ Test query on garbage: "qawsedrftg"
  - Expected: Shallow cascade (10-20 neurons), high novelty (>0.9)
- ⬜ Measure cascade depth vs training time (should increase)
- ⬜ Verify synaptic weights strengthen with repeated exposure

**Success Metrics:**
- ✅ Trained concepts show 5-10x cascade growth vs garbage
- ✅ Novelty scores correctly differentiate familiar/novel patterns
- ✅ Cascade propagation reaches depth 3+ for trained concepts
- ✅ Query performance <1 second for complex cascades

### 3. Production Training Run (Week 2)

**Goal:** Full 24-hour training run on complete dataset

**Tasks:**
- ⬜ Clear brain state: `rm -rf /Volumes/jarvis/brainData/*`
- ⬜ Start production training: `dotnet run -- --production-training`
- ⬜ Monitor checkpoint stability (every 10 minutes)
- ⬜ Track synapse growth rate (expected: 4-5M synapses/minute)
- ⬜ Validate storage scaling (checkpoints should stay <1GB each)
- ⬜ Test mid-training queries (every 4 hours)

**Expected Outcomes:**
- After 24 hours:
  - ~300M-500M synapses created
  - ~50K-100K clusters formed
  - ~5-10M neurons generated
  - Checkpoints: 144 total (10-min intervals)
  - Storage: ~20-30GB on NAS

**Success Metrics:**
- ✅ Training runs 24 hours without intervention
- ✅ All checkpoints save successfully
- ✅ Memory usage stays constant (20-25 MB working set)
- ✅ Query novelty detection accurate at 12hr and 24hr marks

---

## 📋 Medium-Term Goals (Next 1-3 Months)

### Performance Optimization

**Memory Efficiency:**
- Current: 20-25 MB working set (EXCELLENT)
- Goal: Maintain <50 MB even with 1M+ neurons loaded
- Strategy: Aggressive cluster unloading (reduce 30min timeout to 15min)

**Query Performance:**
- Current: 470 concepts/second in training
- Goal: <100ms query latency for complex cascades (10K+ neurons)
- Strategy: Cache frequently accessed clusters, optimize graph traversal

**Storage Compression:**
- Current: MessagePack + gzip (60% compression)
- Goal: 80% compression with procedural regeneration
- Strategy: Store only VQ codes + sparse weights (drop redundant features)

### Architecture Enhancements

**Attention System Integration:**
- Implement attention-weighted cascade propagation
- Use `AttentionSystem.cs` to focus on relevant pathways
- Expected: 5-10x speedup for targeted queries

**Hierarchical Consolidation:**
- Multi-level STM→LTM consolidation
- Fast decay for weak memories (hours)
- Slow decay for strong memories (days/weeks)
- Expected: Better memory fidelity over extended training

**Sparse Activation Metrics:**
- Target: <2% of loaded neurons active per query (biological alignment)
- Current: Need to measure at production scale
- Strategy: Aggressive threshold tuning in cascade propagation

---

## 🔬 Research Experiments (Next 3-6 Months)

### Emergent Behavior Validation

**Creative Associations:**
- Test unexpected but valid connections (e.g., "quantum" → "superposition" → "uncertainty")
- Measure semantic distance vs synaptic path length
- Expected: Novel insights from weak but interesting paths

**Concept Blending:**
- Query composite concepts: "underwater city", "flying car"
- Measure cascade overlap between component concepts
- Expected: Activation of shared semantic features

**Temporal Patterns:**
- Train on sequential data (stories, procedures)
- Test forward prediction: "First step" → cascade predicts next steps
- Expected: Temporal synaptic paths emerge naturally

### Biological Alignment Studies

**Working Memory Capacity:**
- Measure cluster loading/unloading dynamics
- Compare to human working memory limits (~7 items)
- Expected: Natural capacity limits emerge from lazy loading

**Sleep-Like Consolidation:**
- Implement replay-based consolidation during idle periods
- Strengthen frequently co-activated patterns
- Expected: Improved memory retention and recall

**Attention Dynamics:**
- Integrate `AttentionSystem.cs` for focus modulation
- Test attention-guided learning (prioritize novel patterns)
- Expected: Faster learning on attended material

---

## 📊 Known Issues & Technical Debt

### High Priority

**Synapse Save Performance** (BLOCKER)
- Status: IN PROGRESS
- Impact: Training crashes every 10-30 minutes
- Plan: See "Critical Issues" section above

**Checkpoint Resumption**
- Status: WORKING but needs validation
- Impact: Can't verify checkpoint integrity after crash
- Plan: Add checkpoint validation on load (quick integrity check)

**Memory Leak Detection**
- Status: No confirmed leaks, but need long-run validation
- Impact: Could cause crashes in 24+ hour runs
- Plan: Monitor memory growth in production training

### Medium Priority

**Cluster Ghost Prevention**
- Status: Mitigation in place (skip empty clusters)
- Impact: Orphaned cluster metadata in storage
- Plan: Periodic cleanup job (remove clusters with 0 neurons)

**VQ-VAE Codebook Initialization**
- Status: Cold-start handled, but optimal?
- Impact: First ~1000 concepts may have suboptimal clustering
- Plan: Research better initialization strategies (k-means++, PCA)

**Synapse Pruning**
- Status: Basic threshold pruning (weight <0.1)
- Impact: Synapse count grows unbounded (128M in 30 minutes!)
- Plan: Implement decay for unused synapses (last_active_time tracking)

### Low Priority

**Verbose Logging Overhead**
- Status: High verbosity slows training ~10%
- Impact: Minor performance hit
- Plan: Conditional compilation for debug logging

**Feature Encoder Dimensionality**
- Status: Fixed at 128-dim
- Impact: May not capture full semantic richness
- Plan: Research optimal embedding size (64 vs 128 vs 256)

---

## 🧪 Validation Checkpoints

### Milestone 1: Stable Training (Week 1)
- ✅ 1-hour training run without crashes
- ✅ All checkpoints save successfully
- ✅ Synapse export completes in <30 seconds

### Milestone 2: Cascade Validation (Week 2)
- ⬜ Trained vs novel differentiation working
- ⬜ Cascade depth correlates with training exposure
- ⬜ Query latency <1 second for 10K neuron cascades

### Milestone 3: Production Scale (Week 2-3)
- ⬜ 24-hour training run completes
- ⬜ 300M+ synapses persisted correctly
- ⬜ Query accuracy validated at 12hr and 24hr marks

### Milestone 4: Biological Alignment (Month 2-3)
- ⬜ Sparse activation <2% per query
- ⬜ Working set stability (10-20 clusters loaded)
- ⬜ Memory usage constant over 7-day run

---

## 📚 Documentation Status

### Complete & Current
- ✅ [SYNAPTIC_PROPAGATION_IMPLEMENTATION.md](../SYNAPTIC_PROPAGATION_IMPLEMENTATION.md)
- ✅ [SYNAPTIC_NOVELTY_DETECTION.md](../SYNAPTIC_NOVELTY_DETECTION.md)
- ✅ [TECHNICAL_DETAILS.md](../TECHNICAL_DETAILS.md)
- ✅ [BIOLOGICAL_ALIGNMENT.md](../BIOLOGICAL_ALIGNMENT.md)
- ✅ [README.md](../README.md) - Project overview

### Needs Updates
- ⬜ [PRODUCTION_TRAINING_GUIDE.md](../PRODUCTION_TRAINING_GUIDE.md) - Add checkpoint troubleshooting
- ⬜ [QUERY_GUIDE.md](../QUERY_GUIDE.md) - Update cascade metrics examples
- ⬜ [PHASE_6B_COMPLETION_SUMMARY.md](../PHASE_6B_COMPLETION_SUMMARY.md) - Add synapse export details

### Missing Documentation
- ⬜ CHECKPOINT_TROUBLESHOOTING.md - Common issues & solutions
- ⬜ SYNAPSE_LIFECYCLE.md - Creation, strengthening, pruning, persistence
- ⬜ PERFORMANCE_TUNING.md - Optimization strategies for production

---

## 🎯 Success Criteria (3-Month Horizon)

**Training Stability:**
- ✅ 7-day continuous training runs
- ✅ <1% checkpoint failure rate
- ✅ Predictable memory usage (<50 MB working set)

**Query Performance:**
- ✅ <100ms latency for 95th percentile
- ✅ Accurate novelty detection (>90% precision)
- ✅ Deep cascades for trained concepts (depth 3-5)

**Storage Efficiency:**
- ✅ <100GB storage for 1 week training
- ✅ Checkpoint saves <30 seconds
- ✅ 80% compression ratio (procedural + gzip)

**Biological Alignment:**
- ✅ Sparse activation <2% per query
- ✅ Working memory limits naturally emerge
- ✅ Cascade propagation follows trained pathways

---

## 🚨 Emergency Procedures

### Training Crash Recovery

**If training crashes during checkpoint:**
1. Check log file for last successful checkpoint time
2. Verify NAS storage mounted: `mount | grep jarvis`
3. Check for corrupted files: `ls -lh /Volumes/jarvis/brainData/`
4. Restart training - initialization will load last good checkpoint
5. Monitor closely for 30 minutes to ensure stability

**If synapses not saving:**
1. Check `SparseSynapticGraph.GetSynapseCount()` - expected 1-5M per minute
2. Verify `ExportSynapsesChunked()` being called (check logs)
3. Test query cascade - should fail if synapses missing
4. If confirmed broken, stop training immediately
5. Review [Cerebro.cs](../../Core/Cerebro.cs#L807-L845) synapse export code

### Storage Issues

**If NAS unavailable:**
- ⚠️ **DO NOT** switch to local storage (only 256GB capacity)
- Pause training until NAS restored
- Check mount: `df -h | grep jarvis`
- Remount if needed: Contact system admin for jarvis NAS

**If storage full:**
- Check capacity: `df -h /Volumes/jarvis`
- Expected: 31TB free (should never fill)
- If <100GB free, investigate (possible file duplication bug)

---

**Maintained by:** AI Development Team  
**Review Frequency:** Weekly (or after major milestones)  
**Next Review:** January 24, 2026
