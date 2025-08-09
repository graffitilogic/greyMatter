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
- Lessons ingested: 800 (≈17m total) → ~0.7–0.8 lessons/sec (regression; historical target 5–7 lps)
- Concepts processed: 2,399 (unique ~723)
- Clusters (active loaded): 727 (storage metadata reports 4,338 entries – historical bloat)
- Neurons: Block logs show 1,574,669 (1k concepts) → 2,096,721 (2k concepts) → very high average neurons per concept (likely capacity oversizing)
- Synapses: 57,573 persisted
- Save Phase (improved but still heavy): ~38.35s total (neuron bank write dominated: 18.48s; cluster membership: 18.26s) with dirty clusters = 723/727
- Integrity sampler (sample=5): OK; mismatch=0 (improved)
- Cloze baseline: 24.7% (200 items)

Key Observations:
1. Training (ingestion) dominates elapsed time now; persistence optimizations shifted bottleneck.
2. Dirty cluster count remains nearly total despite membership pack diff logic — suggests clusters mark dirty due to neuron growth every pass (capacity target reached late or target too high causing perpetual additions).
3. Metadata inflation (4,338 vs ~727 active) inflates stats and may add overhead to similarity search.
4. Capacity Model: Deterministic + EMA + hysteresis; initial targets appear large, driving huge neuron counts and slowing per-concept training loops.
5. Synapse creation per concept may be amplifying overhead (50k+ synapses) without immediate accuracy benefit.
6. ETA calculation during lessons uses simple projection (not rolling average) → misleading.

---
## 3. Root Cause Hypotheses for Slow Training
- Excessive neuron growth per new concept (high base target) → O(N_neurons) weight init + training loops.
- Repeated similarity lookups + loading overhead for each concept (concept index helps, but growth still heavy).
- Large synapse set updating / enumerations (57k) adds overhead in memory & potential processing.
- Capacity adjustments too slow (EMA alpha 0.05 + hysteresis 15%) causing continued growth churn before stabilization.
- Lack of staged / lazy growth (allocating near final target immediately instead of incremental micro-batches) → early heavy cost.
- Logging overhead minimal now; not primary.
- Potential hidden N^2 behavior in cluster relevance calculations for many candidates.

---
## 4. Immediate Tactical Remediations (Proposed)
(Will be prioritized & instrumented before coding again.)
1. Instrumentation Pass:
   - Per-concept timing (cluster lookup, growth, training, synapse creation).
   - Count neurons added vs reused; log distribution of target capacities.
   - Track membership pack save skip/write ratio.
2. Capacity Model Revision:
   - Lower initial base target (e.g., 60–120) + staged growth increments (e.g., grow in chunks of 50 until demand justifies).
   - Demand-driven escalation: only increase after repeated activations (frequency counter or rolling activation score).
   - Add upper bound throttle per run (cap max neurons added per concept per session).
3. Lazy Weight Initialization:
   - Initialize weights only on first activation needed, not full feature set; or cap initial fan-in.
4. Synapse Load Reduction:
   - Temporarily disable or batch synapse creation (e.g., 1 per N concepts) until performance baseline restored.
5. Relevance & Similarity Optimization:
   - Cache per-concept last chosen cluster to shortcut search.
   - Limit similarity checks to small candidate list via concept→cluster index directly.
6. Dirty Flag Accuracy:
   - Mark cluster dirty only if membership set changes (neuron added with new concept) rather than any neuron growth; decouple capacity expansion from membership if concept already present.
7. Metadata Prune Tool:
   - Offline pass: remove partition metadata entries for clusters not in membership packs.
8. Rolling ETA:
   - Use exponential smoothing on lessons/sec (EMA) + remaining lessons for realistic ETA.

---
## 5. Medium-Term Roadmap
Category: Performance
- [ ] Add profiler hooks & summary table (avg ms per concept stage)
- [ ] Implement staged neuron growth strategy
- [ ] Introduce neuron reuse pools (reassign low-salience neurons)
- [ ] Parallelize learning (bounded degree) with careful contention control

Category: Persistence & Data Hygiene
- [ ] Metadata vacuum & compaction
- [ ] Membership pack write/skip instrumentation & reporting
- [ ] Adaptive partition rebalancing based on cluster density

Category: Capacity & Homeostasis
- [ ] Frequency-based demand metric (activation count decay)
- [ ] Dynamic shrink (gradually reduce capacity if underused)
- [ ] Concept aging / consolidation (merge low-activity clusters)

Category: Evaluation
- [ ] Larger cloze test with variance reporting
- [ ] Per-concept mastery tracking integration in evaluation output
- [ ] Benchmark suite (synthetic vs real dataset) + regression thresholds

Category: Integrity & Monitoring
- [ ] Deep integrity scan (membership ↔ neuron bank cross-check)
- [ ] Drift report: capacities vs actual neurons vs utilization
- [ ] Alert on abnormal growth (e.g., concept > X neurons)

Category: Developer Ergonomics
- [ ] Central config file (JSON/YAML) for thresholds & rates
- [ ] Verbosity tiers documented; add --perf flag
- [ ] Script to run full pipeline + produce HTML summary

Category: Future Expansion
- [ ] Multi-modal feature channels (vision/audio placeholders)
- [ ] Reinforcement signals + reward-modulated plasticity
- [ ] Goal-conditioned retrieval + reflective rehearsal cycles
- [ ] Emotional modulation gating learning rates

---
## 6. Known Issues (Tracking)
| Issue | Impact | Plan |
|-------|--------|------|
| Slow ingestion (~0.7 lps) | High wall-clock time | Instrument + staged growth |
| Dirty clusters ≈ total | Wasted membership checks | Refine dirty logic + reuse check |
| Metadata inflation (4,338 entries) | Overstated stats, possible search overhead | Prune & rebuild index |
| Oversized neuron counts | CPU intensive training | Lower initial target + incremental growth |
| Misleading ETA calc | UX noise | Rolling average ETA |
| High synapse count early | Extra overhead | Defer synapse creation |

---
## 7. Guiding Principles
- Measure before optimizing (add instrumentation first).
- Prefer lazy & incremental allocation.
- Distinguish structural changes (membership) from param updates (weights) in persistence.
- Keep deterministic seeds for reproducibility while allowing controlled stochastic exploration.

---
## 8. Getting Started (Quick)
1. Run setup scripts (datasets/resources) if needed.
2. Execute program (default pipeline: compile curriculum → learn → evaluate → save).
3. Check logs for per-block concept stats and save summary.

---
## 9. Next Session Plan
1. Cool-down period (avoid change thrash).
2. Add detailed timing instrumentation (no functional changes).
3. Observe one full run; capture histogram of neuron additions per concept.
4. Prototype staged capacity growth switch (config flag) and re-run.
5. Only then: prune metadata + refine dirty flag semantics.

---
## 10. Status Summary
Persistence layer largely optimized (batching, caching, diffing). Primary bottleneck now is front-side training cost driven by aggressive neuron growth & capacity model. Next work will focus on measurement, staged allocation, and data hygiene before adding new cognitive features.

---

(README last updated after performance regression analysis; roadmap reflects immediate corrective focus.)
