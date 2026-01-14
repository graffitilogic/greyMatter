# Synaptic Propagation Implementation Plan

**Goal**: Enable queries to traverse trained synaptic paths instead of creating new neurons, allowing emergent behavior through cascade activation.

**Date Started**: January 13, 2026

## Problem Statement

Current architecture creates NEW neurons during queries instead of loading TRAINED neurons, preventing:
- Recognition of trained patterns
- Novelty detection (garbage strings activate equally to trained concepts)
- Emergent behavior (no cascade through learned connections)
- Use of 989K trained synapses

## Architecture Decision: Option 3 - Biological Trunk/Branch Path Traversal

### Key Principles
1. **Graph traversal, not hash lookup**: Memory is synaptic connectivity, not pattern dictionary
2. **Lazy loading**: Load clusters on-demand as signal propagates
3. **Sparse activation**: Threshold filtering at each layer
4. **Emergent behavior**: Complex patterns from simple propagation rules

### Alignment with Project Goals
- ✅ Massive scale: 100,000+ neurons via cascade
- ✅ Procedural generation: Load clusters on-demand
- ✅ Short-lived activation: Release working set after query
- ✅ Emergent behavior: Patterns arise from synaptic traversal

## Implementation Phases

### Phase 1: Load Trained Neurons for Queries ✅ IN PROGRESS
**Status**: Implementing now

**Changes**:
1. Modify `ProcessInputAsync()` to load EXISTING neurons instead of creating new ones
2. Use region mapping to find trained clusters
3. Load neurons from disk (lazy loading)
4. Activate based on feature similarity with trained neurons

**Expected Outcome**: 
- Queries activate same neurons that were trained
- Synaptic connections become accessible
- Foundation for propagation

### Phase 2: Synaptic Cascade Propagation
**Status**: Planned

**Changes**:
1. Add `PropagateActivationThroughSynapticGraph()` method
2. Multi-layer propagation (depth 2-3)
3. Dendritic integration (accumulate signals at target neurons)
4. Threshold filtering (sparse activation)
5. Emergency brake (prevent runaway to >50K neurons)

**Expected Outcome**:
- Activation cascades through learned connections
- 10,000-100,000 neurons participate in complex queries
- Emergent associations

### Phase 3: Natural Novelty Detection
**Status**: Planned

**Changes**:
1. Measure propagation depth
2. Count cascade-activated neurons
3. Trained concepts: deep propagation, many neurons
4. Novel concepts: shallow propagation, few neurons

**Expected Outcome**:
- Automatic novelty scoring without heuristics
- "neural networks": deep cascade (familiar)
- "qawsedrftg": shallow die-out (novel)

## Technical Implementation Details

### Phase 1 Core Changes

**File**: `Core/Cerebro.cs`

**Method**: `ProcessInputAsync()`

**Before** (broken):
```csharp
// Find clusters matching pattern (creates NEW neurons)
var matchingClusters = await FindClustersMatchingPattern(featureVector, maxClusters: 3);
```

**After** (fixed):
```csharp
// Load EXISTING trained neurons for this pattern
var trainedNeurons = await LoadTrainedNeuronsForConcept(concept);
```

**New Method**: `LoadTrainedNeuronsForConcept()`
- Use `_regionToClusterMapping` to find trained clusters
- Load clusters from storage (lazy)
- Get existing neurons (don't create new)
- Calculate activation based on feature similarity
- Return activated neuron dictionary

### Phase 2 Propagation Algorithm

**Pseudocode**:
```
Input: Seed neurons with activation strengths
Output: All activated neurons after cascade

For depth = 1 to MaxDepth:
    For each active neuron:
        Get outgoing synapses from _synapticGraph
        For each synapse:
            propagatedSignal = neuronActivation * synapticWeight
            Accumulate signal at target neuron
    
    Apply threshold (filter weak signals)
    Apply sigmoid squashing (prevent runaway)
    
    If no neurons remain active:
        Break (signal died out - novel pattern)
    
    If neuron count > 50,000:
        Break (emergency brake)
    
    Continue to next depth

Return all activated neurons
```

## Data Migration Required

### Discard Existing Brain State

**Reason**: Current brain has:
- Neurons created during queries (not training)
- Synapses that may not connect properly
- Potential inconsistencies from development iterations

**Action**:
```bash
# Backup current state
mv /Volumes/jarvis/brainData /Volumes/jarvis/brainData.backup_2026-01-13

# Will need fresh training with new architecture
```

### Fresh Training Process
1. Run production training with synaptic propagation enabled
2. Verify region mapping populated correctly
3. Verify synapses connect properly
4. Test query propagation on trained data

## Testing Plan

### Phase 1 Tests
- [ ] Query "neural" activates SAME neurons as training (not new ones)
- [ ] Query "network" activates SAME neurons as training
- [ ] Activated neurons have synaptic connections (non-zero)
- [ ] Garbage string activates different (or no) neurons

### Phase 2 Tests
- [ ] Simple cascade: "neural" → propagates to "network" neurons
- [ ] Deep cascade: reaches depth 3+ for trained concepts
- [ ] Shallow cascade: depth 1 for novel concepts
- [ ] Emergency brake triggers at 50K neurons
- [ ] Sparse activation: <20% of available neurons active

### Phase 3 Tests
- [ ] Novelty score: trained concepts < 0.3 (familiar)
- [ ] Novelty score: garbage strings > 0.7 (novel)
- [ ] Novelty score: partial matches ~0.5 (moderate)

## Success Metrics

### Immediate (Phase 1)
- Queries activate <1000 EXISTING neurons (not new)
- Activated neurons have >0 synaptic connections
- Different queries activate different neurons

### Medium Term (Phase 2)
- Cascade activates 5,000-50,000 neurons for complex queries
- Propagation reaches depth 2-3 for trained concepts
- Propagation dies at depth 1 for garbage

### Long Term (Phase 3)
- Novelty detection accuracy >90%
- Emergent associations (unexpected but valid connections)
- Creative responses (following weak but interesting paths)

## Progress Log

### 2026-01-13 - Initial Analysis
- Identified root cause: queries create new neurons instead of loading trained ones
- Analyzed why all attempted novelty detection methods failed
- Designed 3-phase implementation plan
- Document created

### 2026-01-13 - Phase 1 Implementation Start
- Implementing `LoadTrainedNeuronsForConcept()` method
- Modifying `ProcessInputAsync()` to use trained neurons
- Will test with existing brain state first, then retrain if needed

## Notes and Considerations

### Why Previous Novelty Detection Failed
1. **Pattern hashing**: Destroys similarity, can't compare related patterns
2. **Cluster size heuristics**: Garbage activated LARGER clusters (generic VQ codes)
3. **Activation strength**: Garbage had STRONGER activations (62.8% vs 14%)
4. **Hebbian co-activation**: ALL neurons had 0% interconnections (created during query, not training)

### Why Synaptic Propagation Solves This
- Trained patterns have paths through the graph
- Novel patterns can't traverse non-existent connections
- Natural emergence from graph structure
- No heuristics needed - let physics (signal propagation) do the work

### Performance Considerations
- Lazy loading prevents memory explosion
- Threshold filtering maintains sparsity
- Emergency brake prevents runaway
- Working set release after query prevents accumulation

### Future Enhancements (Post Phase 3)
- Attention mechanism for context weighting
- Inhibitory neurons for competition
- Temporal dynamics (spike timing)
- Meta-learning (learning rate adaptation)
