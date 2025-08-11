# GreyMatter Recovery Roadmap
## Getting Back to Original Vision

### Current State Assessment
The project has drifted from its core vision of ephemeral, FMRI-like neural clusters with shared neurons to a complex persistence-optimized system. We need to simplify and refocus on the biological inspiration.

---

## Phase 1: Simplification & Proof of Concept (2-3 weeks)

### 1.1 Strip Down to Essentials
**Goal**: Return to core concept with minimal viable implementation

**Actions**:
- [x] Create `SimpleEphemeralBrain.cs` - new lightweight implementation
- [x] Implement basic cluster activation/deactivation (no complex persistence)
- [x] Add shared neuron pools between clusters
- [x] Simple in-memory persistence only
- [ ] Remove: hierarchical storage, membership packs, partition metadata, capacity management

**Success Criteria**:
- Can learn "this is red" and activate a red cluster
- Can learn "this is an apple" and share neurons with red cluster
- Clusters activate/deactivate based on relevance
- Memory usage scales with active concepts only

### 1.2 Implement Cluster Overlap Mechanics
**Goal**: Prove the core Venn diagram concept works

**Actions**:
- [x] `SharedNeuronPool.cs` - manages neuron sharing between clusters
- [x] `ClusterOverlapManager.cs` - determines which neurons to share
- [x] Similarity-based neuron assignment (red + fruit → apple uses neurons from both)
- [x] Visual logging of cluster overlaps

**Success Criteria**:
- Learning "red apple" reuses neurons from both "red" and "apple" clusters
- Can trace neuron sharing in logs
- Related concepts show measurable overlap

### 1.3 Basic Evaluation
**Goal**: Measure if the concept learning actually works

**Actions**:
- [x] Simple association test: given "apple" can recall "red"
- [x] Cluster activation visualization
- [x] Memory efficiency metrics (active vs total neurons)

---

## Phase 2: Scale the Core Concept (3-4 weeks)

### 2.1 Ephemeral Scaling
**Goal**: Prove massive scale through procedural generation

**Actions**:
- [ ] Implement neuron factories (create neurons on-demand)
- [ ] Lazy cluster loading (only load clusters when needed)
- [ ] Simple LRU eviction (unload inactive clusters)
- [ ] Benchmark: 10K+ concepts with minimal memory footprint

### 2.2 Biological Behaviors
**Goal**: Add the biological characteristics that were originally planned

**Actions**:
- [ ] Neuron fatigue (temporary deactivation after heavy use)
- [ ] Dynamic thresholds (adapt based on usage patterns)
- [ ] Sparse connection pruning (remove weak associations)
- [ ] Activation decay (clusters naturally fade without reinforcement)

### 2.3 Learning Patterns
**Goal**: Move beyond single concepts to pattern recognition

**Actions**:
- [ ] Sequence learning (A→B→C patterns)
- [ ] Context-dependent activation (same word, different meanings)
- [ ] Hierarchical concepts (animal → mammal → dog)

---

## Phase 3: Realistic Training (4-5 weeks)

### 3.1 Simple Curriculum
**Goal**: Learn from actual text, not hardcoded examples

**Actions**:
- [x] Children's book parser (simple sentences)
- [x] Basic pattern extraction (noun-verb-object)
- [x] Incremental concept building
- [x] Track concept interconnections

### 3.2 Evaluation Harness
**Goal**: Measure actual learning quality

**Actions**:
- [x] Association strength tests
- [x] Concept retrieval accuracy
- [x] Pattern completion tasks
- [ ] Compare to baseline (word2vec similarity)

---

## Phase 4: Demonstrate the Vision (2-3 weeks)

### 4.1 FMRI-like Visualization
**Goal**: Show the "brain scan" aspect of your original vision

**Actions**:
- [ ] Real-time cluster activation display
- [ ] Neuron sharing heat maps
- [ ] Memory usage over time graphs
- [ ] "What's thinking about X" queries

### 4.2 Scale Demonstration
**Goal**: Prove the efficiency gains

**Actions**:
- [ ] Load 100K+ concepts
- [ ] Show memory usage vs traditional neural networks
- [ ] Demonstrate learning speed vs accuracy tradeoffs
- [ ] Performance comparison with current complex system

---

## Implementation Strategy

### Week 1-2: Back to Basics
1. Create `Core/SimpleEphemeralBrain.cs` alongside existing code
2. Implement basic cluster + shared neuron mechanics
3. Prove concept with red/apple/fruit examples
4. Document what works vs existing system

### Week 3-4: Scale Proof
1. Add procedural neuron generation
2. Implement cluster lifecycle management
3. Benchmark memory efficiency
4. Compare learning speed to current system

### Week 5-8: Real Learning
1. Simple text parsing and concept extraction
2. Build concept graphs from children's stories
3. Evaluation suite for concept quality
4. Measure association strength and recall

### Week 9-12: Visualization & Demo
1. Build "brain scan" visualization
2. Create compelling demos of the concept
3. Performance comparison documentation
4. Decision point: continue with simple system or evolve existing

---

## Success Metrics

### Technical
- [ ] Memory usage: O(active_concepts) not O(total_concepts)
- [ ] Learning speed: >10 lessons/sec (vs current 1.7)
- [ ] Concept overlap: measurable neuron sharing between related concepts
- [ ] Activation efficiency: clusters activate only when relevant

### Conceptual
- [ ] Can learn associations: "red" + "fruit" → "apple"
- [ ] Can generalize: learning "red car" after "red apple" reuses red neurons
- [ ] Can recall: given partial input, activate related clusters
- [ ] Can visualize: show which "brain regions" are active for any query

### Comparison to Current
- [ ] Simpler codebase (< 50% current complexity)
- [ ] Faster learning (> 5x current speed)
- [ ] Better memory efficiency (demonstrate with 10K+ concepts)
- [ ] More intuitive behavior (matches biological expectations)

---

## Decision Points

### After Phase 1 (Month 1)
- **If successful**: Continue with Phase 2
- **If struggling**: Reassess core concept, possibly hybrid approach
- **If clearly better**: Plan migration from current system

### After Phase 2 (Month 2)
- **If scaling works**: Proceed to real training data
- **If scaling fails**: Focus on smaller, high-quality demonstrations
- **If performance poor**: Optimize core algorithms before Phase 3

### After Phase 3 (Month 3)
- **If learning quality good**: Build compelling demo
- **If learning quality poor**: Debug concept formation
- **If competitive with baselines**: Prepare for broader evaluation

---

## Risk Mitigation

### Technical Risks
- **Shared neurons too complex**: Start with simple overlap rules
- **Memory efficiency worse than expected**: Profile early, optimize incrementally
- **Learning quality poor**: Compare to current system, identify gaps

### Project Risks
- **Concept doesn't scale**: Have fallback to hybrid approach
- **Takes too long**: Focus on proof of concept first, scale later
- **Team fatigue**: Keep phases short, celebrate small wins

---

## Conclusion

The current system has become an over-engineered neural network. Your original vision of ephemeral, FMRI-like clusters with shared neurons is still valid and worth pursuing. This roadmap provides a path back to that vision while preserving lessons learned from the current implementation.

The key insight: **start simple, prove the concept, then scale** rather than optimizing prematurely for persistence and performance.
