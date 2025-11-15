# ADPC-Net Implementation Roadmap
## Activation-Driven Procedural Clustering

**Start Date**: November 14, 2025  
**Goal**: Replace hash-table-based learning with biologically-realistic pattern activation

---

## 🎯 Phase 1: Feature-Based Retrieval (NO WORD LISTS)

**Objective**: Prove pattern-based retrieval works without cluster_index.json lookup

### Implementation Checklist

- [x] **1.1 Create FeatureEncoder** (`Core/FeatureEncoder.cs`) ✅
  - Simple learned encoder: word → 128-dim vector
  - Features: word length, character n-grams, frequency estimate, phonetic features
  - Deterministic: same word → same vector (for now)
  - Future upgrade path: frozen CLIP/DINO-style encoder

- [x] **1.2 Create LSHPartitioner** (`Core/LSHPartitioner.cs`) ✅
  - Locality-sensitive hashing for feature space
  - Hash 128-dim vectors → region IDs
  - Similar vectors → same/nearby regions
  - Support for k-nearest region queries

- [x] **1.3 Modify Cerebro.LearnConceptAsync** ✅
  - Replace: `FindOrCreateClusterForConcept(concept)` (word lookup)
  - With: `FindClustersMatchingPattern(features)` (pattern-based)
  - Use cosine similarity for cluster activation
  - No cluster_index.json dependency
  - Fixed: LoadClusterMetadataAsync → try/catch pattern

- [x] **1.4 Modify Cerebro.ProcessInputAsync** ✅
  - Pattern-based retrieval for queries
  - Activate clusters by feature similarity
  - Test: Query without concept names
  - Accumulate similarity scores across multiple concept matches

- [x] **1.5 Create ActivationStats** (`Core/ActivationStats.cs`) ✅
  - Track region activation frequencies
  - Novelty detection (new vs familiar patterns)
  - Persistence layer for stats

- [x] **1.6 Update Storage** ✅
  - Save: Region→Cluster mappings (not concept→cluster)
  - Save: Activation statistics per region
  - Keep cluster_index.json as DEBUG SIDECAR only
  - Load mappings and stats on initialization

### Success Criteria (Phase 1)

- ✅ Query "cat" → activates animal/noun clusters (no lookup)
- ✅ Similar words activate similar clusters (dog/cat overlap)
- ✅ Cosine similarity determines activation strength
- ✅ No cluster_index.json used during retrieval
- ✅ Retrieval works on novel inputs (compositional)

---

## 🎯 Phase 2: Hypernetwork Neuron Generation

**Objective**: Replace fixed 64-neuron buckets with dynamic procedural generation

### Implementation Checklist

- [ ] **2.1 Create NeuronHypernetwork** (`Core/NeuronHypernetwork.cs`)
  - Generate neuron params from feature hash + novelty
  - Deterministic: same features → same neurons
  - Budget formula: `N = α * log(freq) + β * novelty`
  - Variable counts: 5, 23, 147, etc. (not 64, 128, 503)

- [ ] **2.2 Create NeuronBudgetCalculator**
  - Calculate neuron needs from activation patterns
  - High novelty → more neurons
  - High frequency → fewer new neurons
  - Adaptive capacity adjustment

- [ ] **2.3 Modify NeuronCluster.GrowForConcept**
  - Replace fixed increment logic
  - Use hypernetwork generation
  - Store only weight deltas from procedural baseline

- [ ] **2.4 Update Persistence**
  - Save: Hypernetwork parameters
  - Save: Novelty scores per region
  - Don't save: Individual neuron structures

### Success Criteria (Phase 2)

- ✅ Neuron counts vary: 5-500 (not bucketed at 64, 128, 503)
- ✅ Same pattern → same neurons (deterministic)
- ✅ High novelty patterns → more neurons allocated
- ✅ Frequent patterns → stable neuron count
- ✅ Memory scales with conceptual diversity, not input volume

---

## 🎯 Phase 3: Sparse Synaptic Graph

**Objective**: Replace dense synapse storage with sparse graph

### Implementation Checklist

- [ ] **3.1 Create SparseSynapticGraph** (`Core/SparseSynapticGraph.cs`)
  - Hashtable: `(src_id, tgt_id) → weight`
  - Hebbian update: `Δw ∝ a_i * a_j`
  - Dynamic pruning: Remove edges below threshold

- [ ] **3.2 Implement Hebbian Learning**
  - Local rule: Co-activation strengthens synapses
  - Spike-timing consideration (optional)
  - Credit assignment through active paths

- [ ] **3.3 Implement Synaptic Pruning**
  - Remove weak synapses (< threshold)
  - Lottery ticket style: Keep strong subgraph
  - Graceful degradation under memory pressure

- [ ] **3.4 Update Synapse Persistence**
  - Save: Only active edges (sparse)
  - Save: Edge weights as deltas
  - Compress with gzip

### Success Criteria (Phase 3)

- ✅ Synapse count << neuron_count² (sparse)
- ✅ Co-activated patterns strengthen connections
- ✅ Unused synapses pruned automatically
- ✅ Memory usage stays constant despite training

---

## 🎯 Phase 4: VQ-VAE Codebook (Future)

**Objective**: Replace LSH with learned vector quantization

### Implementation Checklist

- [ ] **4.1 Create VectorQuantizedFeatureSpace** (`Core/VQFeatureSpace.cs`)
  - Learned codebook (k-means or online clustering)
  - Quantize features → discrete region IDs
  - Better locality than fixed LSH

- [ ] **4.2 Codebook Training**
  - Online updates during learning
  - Adaptive codebook refinement
  - Hierarchical clustering option

- [ ] **4.3 Migration from LSH**
  - Backward compatibility
  - Gradual transition
  - Performance comparison

### Success Criteria (Phase 4)

- ✅ Better feature space packing than LSH
- ✅ Learned regions align with semantic similarity
- ✅ Faster retrieval than LSH
- ✅ Supports hierarchical navigation

---

## 🎯 Phase 5: Training Dynamics (Polish)

**Objective**: Implement full ADPC-Net training cycle

### Training Phases

**Cold Start** (Session 1)
- All clusters empty → generate all neurons procedurally
- Establish baseline activation patterns
- Build initial sparse synapse graph

**Warm-Up** (Sessions 2-10)
- Frequently co-activated regions → persist weight deltas
- Codebook refinement (if using VQ-VAE)
- Synaptic graph densifies for common patterns

**Maturation** (Sessions 10+)
- Rare features → still procedural
- Common features → optimized with deltas
- Stable neuron budgets for established concepts

**Forgetting** (Continuous)
- Low-activation clusters → prune deltas
- Fall back to procedural baseline
- Graceful degradation under memory pressure

---

## 📊 Validation Experiments

### Experiment 1: Toy Language Task
- **Dataset**: 1000 sentences about animals
- **Baseline**: Current hash-table system
- **Metrics**: 
  - Concept emergence (no labels)
  - Compression ratio
  - Generalization to novel sentences

### Experiment 2: Scaling Test
- **Dataset**: Full Tatoeba (1M+ sentences)
- **Baseline**: GPT-2 memory footprint
- **Metrics**:
  - Memory usage (target: <100 MB for 1M concepts)
  - Retrieval accuracy
  - Training throughput

### Experiment 3: Pattern Retrieval
- **Test**: Query without exact matches
- **Example**: Train on "cat sat", query "dog ran"
- **Success**: Activates similar patterns (verb, animal, past-tense)

---

## 🚨 Critical Success Factors

### Must-Haves (Phase 1)
1. ✅ Pattern-based retrieval (no word lookups)
2. ✅ Activation by similarity (cosine/euclidean)
3. ✅ Works on novel inputs

### Must-Haves (Phase 2)
1. ✅ Variable neuron counts (no buckets)
2. ✅ Deterministic generation (same input → same neurons)
3. ✅ Memory efficiency (store deltas, not structures)

### Nice-to-Haves (Future)
- Hierarchical feature space navigation
- Differentiable hypernetwork (for backprop)
- Multi-modal encoders (text + image)

---

## 🔧 Current Status

**Active Phase**: Phase 1 - Feature-Based Retrieval  
**Current Task**: Creating FeatureEncoder.cs

**Completed**:
- [x] Architecture design agreed
- [x] Implementation roadmap created
- [x] Success criteria defined
- [x] Phase 1.1: FeatureEncoder.cs (128-dim deterministic encoding)
- [x] Phase 1.2: LSHPartitioner.cs (16 bands × 4 rows locality-sensitive hashing)
- [x] Phase 1.5: ActivationStats.cs (novelty detection & frequency tracking)

**In Progress**:
- [ ] Phase 1.3: Modify Cerebro.LearnConceptAsync (pattern-based clustering)

**Blocked**: None

---

## 📝 Notes & Decisions

**Decision Log**:
- **2025-11-14**: Keep cluster_index.json as debug sidecar (not integrated)
- **2025-11-14**: Start with LSH, upgrade to VQ-VAE later
- **2025-11-14**: Simple feature encoder first, frozen encoder later
- **2025-11-14**: Hebbian learning for synapse updates

**Open Questions**:
- Feature dimensionality: 128? 256? (start 128)
- LSH band count: How many? (tune empirically)
- Neuron budget formula coefficients: α=10, β=50? (tune)

---

**"A trillion-parameter model in a gigabyte of RAM."**
