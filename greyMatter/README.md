# greyMatter ("Brain in a Jar")

Biologically inspired experimental learning sandbox: adaptive neuron clusters, hierarchical partitioned persistence, STM→LTM consolidation, curriculum ingestion, and simple cloze evaluation.

---
## 1. Current Architecture

Components:
- BrainInJar orchestrator: curriculum ingestion, adaptive concept capacity, consolidation, save pipeline.
- NeuronCluster / HybridNeuron: concept-associated dynamic neuron populations with STM delta buffers and incremental LTM consolidation.
- Hierarchical Storage (EnhancedBrainStorage):
  - Partition metadata (partition_metadata.json) with normalized GUID keys (N format)
  - Membership packs per partition (membership.pack.json.gz)
  - Per-partition neuron banks (neurons.bank.json.gz) with snapshot diff + gzip
  - Concept capacity persistence (concept_capacity.json)
  - Inverted concept→cluster index + membership pack cache
- GlobalNeuronStore: batched neuron persistence, per-bank async locks, skip identical snapshots.
- FeatureMapper: deterministic feature→neuron id mapping.
- Synapses: sparse inter-neuron connections (currently large count, minimally pruned).
- DevelopmentalLearningSystem / EnvironmentalLearner: curriculum compilation from datasets (staged S1/S2/S3 streams).
- ContinuousProcessor (consciousness/emotional/goal systems) – optional/background.
- Evaluation Harness: simple cloze accuracy baseline.

Data Flow (simplified):
Curriculum Lesson → Extract Concepts → For each concept: find/load cluster (metadata + membership) → Ensure capacity target → Grow neurons (if below target) → Train neurons (feature-weight adjustments) → Optionally form synapses → Periodically consolidate STM → Save (banks first, then membership packs + metadata) → Evaluate.

---
## 2. Recent State (Latest Run Snapshot)
- Lessons ingested: 800 (~8–10 min total) → ~1.3–1.7 lessons/sec
- Concepts processed: 2,399 (unique ~723)
- Clusters (active loaded): 727 (storage metadata reports ~4,338 entries)
- Neurons (block logs): ~114k–150k per 1k concepts
- Synapses persisted: ~115k–122k
- Save phase: 31.7–40.2s total
  - Neuron bank upserts: ~11–12s
  - Membership/metadata (117 dirty of 727): ~19–26s
- Integrity sampler (sample=5): previously Mismatch=5; fixes applied (see below); pending validation on next run
- Cloze baseline: 24.7% (200 items)
- Startup: first run can pause while computing storage stats; subsequent runs use cached stats; a shorter delay can still occur before the first learning step during on-demand hydration

Key Observations (current):
1. Training latency now dominates elapsed time; persistence is improved but still significant at scale.
2. Dirty clusters repeat at 117 between identical runs; this is expected with deterministic growth until capacities fully stabilize.
3. Metadata inflation (4,338 vs ~727 active) persists and may bias stats and similarity search.
4. Capacity targets likely oversized for some concepts, increasing per-concept training work.
5. Occasional ~1 min delay before first concept in subsequent runs stems from initial hydration and environment reading.

### 2.1 Implemented Fixes (this session)
- Neuron identity restored on hydrate: HybridNeuron.FromSnapshot now sets Id from snapshot (prevents new GUIDs and repeat upserts).
- Global neuron bank ID normalization: lowercase Guid("N") keys; bank re-key on read; normalized lookups to eliminate membership↔bank inconsistency.
- Membership pack normalization: normalized cluster keys and de-duplicated Guid lists.
- Cached storage stats: storage_stats.json with background refresh to avoid long NAS scans on startup; immediate cached display with later correction.
- Incremental membership updates: merge only newly added neuron IDs when available; full compare as fallback.
- Batched neuron-bank saves per partition with per-bank locks; skip no-op writes via snapshot equality.
- Save-only CLI: `--save-only` initializes and persists without learning to verify churn and integrity quickly.
- Consciousness stats API: added EthicalState, EmotionalStatus, GoalStatus properties used by Program.cs.

---
## 3. Root Cause Hypotheses for Slow Training
- Excessive neuron growth per new concept (high base target) → O(N) initialization and training.
- Similarity lookups + hydration overhead on first touches per run.
- Large synapse set adds overhead with limited immediate accuracy gains.
- Capacity adjustments too slow (EMA/hysteresis) causing continued growth before stabilization.
- Lack of staged/lazy growth (allocating near-final capacity upfront) increases early cost.

---
## 4. Immediate Tactical Remediations (Proposed)
(Will be prioritized & instrumented before coding again.)
1. Instrumentation Pass:
   - Per-concept timings (cluster lookup, growth, training, synapses) with summary table.
   - Count neurons added vs reused; track capacity targets distribution.
   - Membership pack skip/write ratios.
2. Capacity Model Revision:
   - Lower initial targets (e.g., 60–120) and staged growth in chunks.
   - Demand-driven escalation only after repeated activations.
   - Per-run cap (already present) with tighter limits for A/B tests.
3. Lazy Weight Initialization:
   - Initialize limited fan-in; defer full weights until needed.
4. Synapse Load Reduction:
   - Temporarily reduce/defer synapse creation until baseline recovered.
5. Relevance & Similarity Optimization:
   - Prefer last chosen cluster cache; bound candidate set from concept index.
6. Dirty Flag Semantics:
   - Ensure membership dirty only when neuron membership changes (not when just weights change).
7. Metadata Hygiene:
   - Prune partition metadata entries that lack membership; rebuild index.
8. Rolling ETA:
   - Use EMA on lessons/sec for realistic ETA.

---
## 5. Medium-Term Roadmap
Category: Performance
- [ ] Profiler hooks & summary table (avg ms per concept stage)
- [ ] Staged neuron growth strategy
- [ ] Reuse pools for low-salience neurons
- [ ] Bounded parallelism for learning with contention control

Category: Persistence & Data Hygiene
- [ ] Metadata vacuum & compaction
- [ ] Membership pack write/skip instrumentation & reporting
- [ ] Adaptive partition rebalancing by density

Category: Capacity & Homeostasis
- [ ] Frequency-based demand metric (activation count decay)
- [ ] Dynamic shrink for underused concepts
- [ ] Concept aging / consolidation

Category: Evaluation
- [ ] Larger cloze test + variance reporting
- [ ] Per-concept mastery tracking
- [ ] Benchmark suite + regression thresholds

Category: Integrity & Monitoring
- [ ] Deep integrity scan (membership ↔ neuron bank)
- [ ] Drift report: capacities vs actual vs utilization
- [ ] Alert on abnormal growth patterns

Category: Developer Ergonomics
- [ ] Central config file for thresholds & rates
- [ ] Verbosity tiers doc; add --perf flag
- [ ] Script: run pipeline → HTML summary

Category: Future Expansion
- [ ] Multi-modal feature channels
- [ ] Reward-modulated plasticity
- [ ] Goal-conditioned retrieval + reflection cycles
- [ ] Emotional modulation of learning rates

---
## 6. Known Issues (Tracking)
| Issue | Impact | Plan |
|-------|--------|------|
| Startup delay (first run) | Pause before stats printed | Cached stats + background refresh (done); consider deferring scan or prefetch warm-up |
| ~1 min delay before first lesson | Hydration/env read | Pre-warm small working set; cap initial hydration |
| Dirty clusters stable at 117/727 | Expected with deterministic growth | Option to freeze growth; tune hit threshold/cap; staged growth |
| Metadata inflation (~4,338 entries) | Overstated stats; possible search overhead | Prune & rebuild index |
| Capacity oversizing | CPU heavy training | Lower initial target + staged growth |
| Integrity sampler showed 5 mismatches | Possible ID drift (now fixed) | Validate post-fix; save-only should show 0 |

---
## 7. Guiding Principles
- Measure before optimizing (add instrumentation first).
- Prefer lazy & incremental allocation.
- Separate structural changes (membership) from parameter updates (weights) in persistence.
- Keep deterministic seeds for reproducibility while allowing controlled stochastic exploration.

---
## 8. Getting Started (Quick)
1. Setup datasets/resources if needed.
2. Typical preschool pipeline (macOS + tuned saves):
   - `dotnet run -- --preschool-train -bd /Volumes/jarvis/brainData -td /Volumes/jarvis/trainData -log 1 -mps 2 -cc true`
3. Validate persistence stability (no learning):
   - `dotnet run -- --save-only -bd /Volumes/jarvis/brainData -td /Volumes/jarvis/trainData -log 2`

---
## 9. Next Session Plan
1. Add detailed timing instrumentation (no functional changes).
2. Observe a full run; capture histogram of neuron additions per concept.
3. Prototype staged capacity growth flag and re-run.
4. Add optional growth-freeze switch for churn validation.
5. Prune metadata + refine dirty flag semantics.

---
## 10. Status Summary
Persistence layer is optimized (batching, caching, diffs, normalized IDs). Primary bottleneck is front-side training driven by aggressive neuron growth & capacity targets. Next work focuses on measurement, staged allocation, and data hygiene before adding new cognitive features.

---

(README updated with current metrics and recent fixes.)
