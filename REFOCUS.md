# REFOCUS: Back to the Core Thesis

*January 2026 reboot plan. This document supersedes all archived roadmaps in `greyMatter/docs/archive/`.*

## The Thesis (unchanged since day one)

A commodity PC can host a *virtual cortex* far larger than its RAM by borrowing the
No Man's Sky trick: **procedurally generate neural structure on demand, within the
activation-scope of the current "observer" (the active input), and persist only what
learning actually changed.**

The falsifiable core question:

> **Can a cortical region that is evicted and later procedurally regenerated
> (VQ code + budgeted synapses) match a fully-persisted region on recall fidelity,
> at a fraction of the storage and resident memory?**

Nothing else in this repo matters until that question has a measured answer.
The project has never run this experiment. That is the drift, and this is the fix.

## How We Got Off Track (honest history)

1. **Aug–Sep 2025 — anthropomorphic scope creep.** Ethics drives, goal systems,
   developmental stages, "consciousness" processing. Interesting, premature, deleted.
   (All recoverable from git history.)
2. **Nov 2025–Jan 2026 — infrastructure capture.** The work became checkpoint
   serialization, NAS I/O, LRU eviction. Competent plumbing — but the actual thesis
   ("Phase 2: procedural loading") was marked *DEFERRED* in the old PROJECT_STATUS.
   The scaffolding became the building.
3. **Learning-loop scare (corrected):** Jan 19 debug logs showed zero neurons
   passing the Hebbian gate (raw negative potentials tested against a 0.1
   threshold). The Jan 23 commit fixed the gate to measure activation *above
   resting potential*; the 133M-synapse run came after the fix. Verified by code
   review during the 2026 reboot — but never re-verified empirically, hence P1.
4. **Doc inflation:** claims like "trillion-parameter model in a gigabyte" and
   "production ready" have no eval behind them. New rule below.

## Ground Rules (anti-sycophancy guardrails)

- **No claim without a command.** Any "✅ works" statement in any doc must cite a
  reproducible command and its measured output.
- **One status doc.** This file. No new SUMMARY/COMPLETE/MILESTONE docs; failed
  experiments get a dated note in `greyMatter/docs/archive/` and nothing else.
- **Success is architectural, not linguistic.** greyMatter will not out-language a
  transformer. Metrics are: regeneration fidelity, storage per learned association,
  resident memory vs. virtual capacity, recall vs. activation-distance.
- **Small corpus until the thesis is proven.** `tatoeba_small` is the standard
  benchmark input. The 571GB Wikipedia pipeline, LLM teacher, and progressive
  curriculum stay parked until P4.

## The Plan

### P0 — Cleanup (this commit)
Dead modules deleted (ethics/goals/developmental systems, 3 redundant ephemeral
brains, orphaned trainers/evaluators — 26 files, all already excluded from the
build). Training logs, stale phase docs, and generated artifacts purged or archived.
csproj exclude-list collapsed to wildcards. `.gitignore` hardened.

### P1 — Verify the learning loop ✅ VERIFIED (2026-07-29)
Command: `dotnet run -- --test-hebbian` → PASS (synapses form).
Command: `--production-training --dataset tatoeba_small --duration 300` →
1,496 sentences, synapses created — **65,498,012 of them (3.54 GB)**.
The loop is alive; the new problem is the opposite failure: synaptic
incontinence. See P1.5.

### P1.5 — Synaptic budget (added 2026-07-29 after the 5-min run) ✅ implemented, pending re-run
Root causes found:
- `RecordCoactivationPattern` wired **all pairs bidirectionally**: a 340-neuron
  group = ~115K synapses in one call, per word occurrence.
- Activations fed to Hebbian Δw were raw deltas (~10–27), not 0..1 →
  Δw = 0.01·15·15 ≈ 2.25 → **every synapse born saturated at max weight**,
  making decay/pruning meaningless.
- `ApplyDecay`/`PruneWeakSynapses` existed but were **never called**.

Fixes (SparseSynapticGraph + Cerebro):
- Top-K co-activation group (K=16, winner-take-all analog) → ≤240 pairs/event.
- Per-neuron out-degree budget (64); strengthening always allowed, creation
  beyond budget blocked (counted + reported).
- Creation requires activation product ≥ 0.15; activations normalized
  tanh(delta/20); birth weight = prune threshold + Δw → persistence must be
  earned through reinforcement.
- Decay (×0.995) + prune wired into every checkpoint; log line reports
  pruned + blocked_by_budget.
- Log spam gated: `DebugLog` levels (0/1/2, env `GREYMATTER_VERBOSITY`);
  per-cluster/per-region traces now level 2, evictions/partition saves level 1.

**Result (2026-07-29 re-run):** 3.16M synapses / 255MB from 1,741 sentences —
20× count / 14× storage reduction vs baseline; checkpoint 120s (was 402s);
throughput doubled. Budget verified. Two residual findings → P1.6.

### P1.6 — Stable assemblies (added 2026-07-29 after the re-run)
Re-run exposed the real growth driver: **neurons, not synapses**
(783,935 neurons from 1,741 sentences; ~38% of neurons new in every block;
throughput sagged 12 → 5.8 sent/sec as population grew).

Root cause (verified in `FindOrCreateClusterForPattern`): best-match cluster
selection over drifting centroids + a learning VQ codebook means the same
word's winning cluster changes visit-to-visit; the concept re-grows its full
~66-neuron allocation in each. Only ~293 clusters existed — words revisited
them, but not the ones holding their neurons.

Fixes:
- **Assembly reuse:** among similarity-qualified candidate clusters, prefer
  the one where the concept already has neurons (re-activation over
  colonization). Recall stays pattern-based; no global word index added.
- **Decay 0.995 → 0.98:** first checkpoint pruned exactly 0 — birth weight
  (~0.102–0.108) × 0.995 stays above the 0.1 prune line for 4-5 cycles.
  At 0.98 newborns die within 1-2 un-reinforced checkpoints.

**Result (2026-07-29 second re-run):** partial win, two lessons:
- Assembly reuse works on repeated vocabulary: % new fell 49 → 20.5% while
  tatoeba cycled — then curriculum advanced to news (308K unique headlines)
  and legitimately-new words pushed it back to 33-45%. Benchmarks must pin
  the dataset → added `--no-curriculum`.
- "pruned 0" again: real birth weights ~0.105-0.108 need ~3 decay passes,
  and decay only ran at checkpoints — which fire once per 10 min, i.e.
  never during a 5-min run. Decay moved to the maintenance loop
  (0.97 every 2 min); newborns now die in ~6 unreinforced minutes.
- 20% new during pure repetition is still too high (should → 0 after the
  corpus cycles twice). Added allocation instrumentation (reuse%,
  assembly_pref hits, grew_events, avg_grow) to distinguish: home cluster
  missing from top-5 candidates (VQ drift) vs capacity-target ratcheting.

**Exit criterion (revised):** `--no-curriculum` 5-min run shows reuse% → ~100
and grew_events% → ~0 as tatoeba saturates; maintenance log shows pruned > 0.

**P1.6b correction (2026-07-29 evening):** the maintenance-loop decay raced
the training thread over the (non-thread-safe) synapse dictionary — training
errors at runtime. Decay moved inline to the training path
(every 5,000 learn events ≈ 1 min), single-threaded with all other graph
writes. Lesson recorded: all synaptic-graph mutation stays on the training
path; the checkpoint path only reads, guarded by the existing checkpoint lock.

**Resolved:**
- "Vocabulary learned" was `TotalNeuronsCreated` used as a proxy
  (ProductionTrainingService checkpoint metadata). Final-stats label fixed;
  a true distinct-concept count is a TODO.

### P1.6d — VQ codebook drift found (2026-07-29 late)

**Instrumented run (`--no-curriculum`, 5 min):** decay confirmed working —
`1,920,163 → 1,412,800 (pruned 507,363)`. 3,282 sentences (1.85× the previous
run), 1.42M synapses persisted, checkpoint 65s. Cumulative vs the pre-budget
baseline: **46× fewer synapses, ~14× less storage, 6× faster checkpoints.**

**Allocation line answered the open question exactly:**
`events=791 reuse=63.0% (assembly_pref=498) grew_events=309 (39.1%) avg_grow=69.1`
- `assembly_pref` (498) == `reuse` (63.0% × 791) → reuse happens *only* when the
  concept's home cluster appears among candidates; when it doesn't, nothing is
  found and a fresh assembly is grown.
- non-reuse (37%) ≈ grew_events (39.1%) → **~95% of neuron growth is a
  known concept failing to find itself**, not capacity ratcheting.

**Root cause (verified in code, not inferred):**
`FeatureEncoder.Encode(word)` is fully deterministic — same word, same vector,
always. But `Cerebro.GetRegionId()` calls `VectorQuantizer.QuantizeAndUpdate()`,
which runs an EMA codebook update **on every single lookup**. A stable vector
therefore maps to a drifting code: the word's home region moves, its home
cluster drops out of the 5-candidate set, and the concept is treated as new.

The same drift is also a latent **P2 fidelity bug**: neurons persist a VQ code
and `ProceduralNeuronRegenerator` rebuilds Threshold/Bias from
`GetCodebookVector(code)`. If the codebook moves after a neuron was assigned its
code, that neuron regenerates into something different from what was saved.
Freezing serves both problems.

**Fixes:**
- `VectorQuantizer.IsLearning` + `FreezeCodebook()`; EMA updates only while
  learning; `ImportCodebook` marks a loaded codebook frozen (resuming a run must
  not restart the drift).
- Cerebro freezes after `VqWarmupUpdates = 20,000` and logs perplexity /
  utilization at the freeze point.
- **Staged growth:** first allocation capped at `FirstAllocationNeurons = 16`
  (was: full target, ~69). Capacity is earned through repetition — most words
  in a corpus are seen once (Zipf), and they were each costing ~69 neurons.

**Exit criterion:** 5-min run on fresh brainData shows `reuse%` climbing past
~90 after the freeze, `avg_grow` ≈ 16 for new concepts, and total neurons well
under the 730K of this run at equal-or-better sentence throughput. Requires a
brainData reset (region→cluster indexes built under the drifting codebook are
stale) **and `--corpus-limit`** (see correction below).

### P1.6e — Benchmark methodology correction (2026-07-29, full log reviewed)

**My "95% of growth is a known concept failing to find itself" claim was
overstated, and the exit criterion was invalid.** `--no-curriculum` does not
pin a small cycling corpus: with `currentPhase == null`, `LoadTrainingData()`
calls `LoadSentences(_datasetKey, shuffle: true)` with **no maxSentences**, so
it loaded **50,000 sentences** and the 5-min run consumed only 3,282 — every
sentence seen exactly once, no repetition. A large share of the 37% non-reuse
is therefore *legitimately new vocabulary*, not a reuse failure. The
arithmetic (non-reuse ≈ grew_events) held; the interpretation didn't.

The VQ drift finding (P1.6d) is unaffected — it was verified from code
(`GetRegionId` → `QuantizeAndUpdate` mutating the codebook on every lookup),
not inferred from these numbers. Its P2 fidelity implication also stands.

**Fix:** added `--corpus-limit N`, which pins the corpus so it cycles. Reuse
saturation is only measurable when the same sentences repeat.

**What the full log does show (all genuine wins):**
- **Throughput flat** at ~11 sent/sec / ~84 cps for the entire run (previous
  run sagged 12 → 6.0). Bounded synapse count removed the drag.
- **Synapse count now oscillates** rather than growing linearly: creation
  pushes to ~1.9M, each decay pass prunes 25-28% back to ~1.4M. Floor still
  creeps (1.18M → 1.24M → 1.36M → 1.41M), partly legitimate new vocabulary.
- `reuse%` climbed 33.5% → ~63% and plateaued — consistent with a
  non-repeating corpus's type/token ratio.
- First decay pass pruned 0 (synapses too young), subsequent passes 144K,
  486K, 442K, 507K. Working as designed.

**New signal to watch:** Hebbian `passed%` declined 99.9% → ~83% and mean
delta 20.05 → 15.9 over the run, with some events showing `avg=-62,
above_threshold=78/241`. Neurons are accumulating in clusters whose patterns
they don't respond to — consistent with over-allocation, and a plausible
early-warning metric for assembly quality. Not urgent; revisit in P2/P4.

### P1.6f — Resume run (2026-07-30 08:35): the real blocker surfaces

Run resumed from an existing checkpoint rather than a fresh brainData, which
accidentally produced the most informative run so far.

**Working as designed:**
- **Staged growth confirmed:** `avg_grow` 68 → ~28 (mix of 16-neuron first
  allocations and up-to-64 top-ups).
- **Codebook froze on load** (`ImportCodebook` → `IsLearning=false`), so a
  resumed run no longer re-drifts. The warmup-freeze path is still untested.

**Two bugs of mine, both fixed here:**
1. `--no-curriculum` only applied to the *initial* load.
   `ReloadTrainingDataAsync` still consulted the curriculum, so at batch
   exhaustion the run switched to `news` (log: "Loading dataset by count:
   news"). The reload path now respects the flag.
2. Assembly reuse probed every candidate cluster with `FindNeuronsByConcept`,
   which calls `EnsureLoadedAsync` → up to 5 NAS loads per learn event. Fine on
   a fresh in-memory brain, brutal on resume (`find` 0.8ms → 28.8ms). Now only
   probes clusters already resident (`NeuronCluster.IsLoaded`).

**THE BLOCKER — persistence starvation (~99.8% of neurons never persist):**
Checkpoint consolidation budget is
`Math.Max(5, Math.Min(50, MaxParallelSaves * 5))` = **5 neurons per cluster**
per checkpoint. Only consolidated (LTM) neurons enter membership packs, so a
3,000-neuron cluster persists 5. Evidence across runs:
- `📦 New cluster entry: 5 neurons` / `Membership changed: 5→10` (every cluster)
- earlier run: `Cluster 2213174d: 3122 total neurons, 5 LTM neurons`
- this run's restore: `Neurons: 0`, then
  `⚠️ Procedural bank not found for partition ...`

Consequences: learning is essentially discarded at every eviction/restart; a
resumed brain regrows from nothing (this run: 68K neurons created to relearn
what it already "knew"); and **P2 cannot be run at all** — there is no
persisted assembly to regenerate and compare against.

This is not "limited persistence" (the thesis). It is *no* persistence.

**Root cause found — a wiring gap, not a design choice:**
`ProductionTrainingService` created `new Cerebro(nasStoragePath)` and **never
called `AttachConfiguration`**, so `_configForLogging` stayed null and
`UseProceduralSave` defaulted to false. The Phase 6B procedural save path —
compact `ProceduralNeuronData` (VQ code + budgeted synapses, ~50-100 bytes)
for *every* neuron — has therefore never run in production training. Only the
5-per-cluster LTM consolidation path ever wrote neurons. The machinery the
whole thesis rests on was built, tested in isolation, and left unplugged.

Fixed: production training now attaches a `CerebroConfiguration` with
`UseProceduralSave = true`. Side effects of attaching config (previously all
inert in production runs): `MaxParallelSaves`, `CompressClusters`, and
`Verbosity` now apply. `GREYMATTER_VERBOSITY` accepted as an alias for
`VERBOSITY`.

### P1.6h — SATURATION ACHIEVED, and the caveat that matters (2026-07-30 09:06)

First clean run with everything wired: fresh brainData, pinned cycling corpus,
procedural save on. **The system converged.**

| metric | before (07-29) | this run | change |
|---|---|---|---|
| sentences / 5 min | 3,282 | **11,532** | 3.5x |
| neurons | 729,956 | **75,273** | 9.7x fewer |
| synapses | 1,421,928 | **249,747** | 5.7x fewer |
| throughput | 11 sent/sec | **38.4 sent/sec** (rising) | 3.5x |
| neurons persisted | ~5/cluster | **74,682 (99.2%)** | — |

Trajectory over the run — this is the thesis behaving as designed:
- `reuse%`: 58.7 → 78.4 → 97.4 → 99.6 → **100.0** (and holds)
- `grew_events`: 55.2% → 19.3% → 4.8% → **0.0%**
- neurons: climb to ~75,000 then **plateau flat** for the last 3 minutes
- synapses: peak ~294K then settle to ~250K under decay
- throughput *increases* as it saturates (132 → 283 cps)

The brain learned a 500-sentence corpus, saturated, and stopped allocating.
Every subsequent pass recognized everything with zero new neurons. Bounded
memory, bounded storage, rising speed. All P1.6d exit criteria met.

**CAVEAT — the win is partly an artifact. VQ utilization is 2.5%.**
`🧊 VQ codebook frozen ... perplexity 7.03, utilization 2.5%` — roughly **13 of
512 codes** are in use, and ~60 clusters hold the entire vocabulary. With that
few regions, *any* concept finds its home cluster in the top-5 candidates, so
100% reuse is partly real assembly reuse and partly "everything is in the same
few buckets". Supporting evidence: Hebbian `passed%` settled at 22.5% with mean
delta 4.1 — most neurons in a cluster don't respond to the pattern being
trained, exactly what coarse buckets predict.

**Root cause (verified):** `VectorQuantizer` initialized all 512 codes to
±0.01 random noise — effectively 512 near-identical vectors at the origin —
while `FeatureEncoder` emits **unit-norm** vectors. Every code was ~equidistant
from all data; the first winner got EMA-pulled onto the unit sphere and then
beat the origin-bound codes forever. Textbook VQ codebook collapse from
non-data-driven initialization.

**Fix (P1.6h):** codes are now claimed by real observations, and only when an
observation is farther than `SeedDistanceThreshold` (squared-L2 0.35 ≈ cosine
0.825) from every code already claimed. Nearest-code search and neighbour
lookup consider claimed codes only. `SeededCount` reported at freeze time.

**Also corrected:** `TECHNICAL_DETAILS.md` claimed "90% compression" throughout.
Measured reality from this run's procedural save: **2.19x** (18,589,460 →
8,478,848 bytes for 74,682 neurons, ~113 bytes/neuron). Docs updated. The
ceiling is set by persisted synaptic weights, not the VQ code — raising it is a
synapse-budget question (P3), not a VQ question.

### P1.6i — Codebook fix validated; substring bug found (2026-07-30 10:14)

Post-fix run. **The collapse fix worked, and it confirmed the earlier caveat
was correct: the previous run's 100% reuse was partly bucket-collapse.**

| metric | collapsed codebook | seeded codebook |
|---|---|---|
| clusters | ~60 | **226-250** (4x discrimination) |
| sentences / 5 min | 11,532 | **15,378** |
| throughput | 38.4 sent/sec | **51.0 sent/sec** (377 cps) |
| reuse% | 100% (artifact) | **96%** (real) |
| grew_events | 0% | 6-7% |
| neurons | 75,273 (plateau) | 278,355 (still climbing) |

Honest reading: with real discrimination the system no longer reaches perfect
saturation — reuse settles ~96% and neurons keep growing slowly, because more
clusters means more places for a concept to land. The remaining 4-6% is the
genuine assembly-reuse miss rate (home cluster absent from the top-5
candidates). That is a real, bounded problem to solve, unlike the previous
run's flattering artifact.

**The fix did NOT move Hebbian `passed%`: still 22.2%, mean delta 4.1** —
identical to the collapsed run. My "coarse buckets cause low pass rate"
hypothesis was **wrong**, and the invariance across a 4x change in cluster
count is what exposed the real cause.

**Root cause — substring matching in `FindNeuronsByConcept`:**
```csharp
n.AssociatedConcepts.Contains(concept) || n.ConceptTag.Contains(concept)
```
`ConceptTag.Contains` is a **substring** test. Concept "the" matched neurons
tagged "there", "them", "other", "together". So every learn event trained a set
padded with neurons belonging to unrelated words, which of course don't respond
to this concept's features. The log's arithmetic is exact: 16 of 78, 32 of 146,
208 of 947 — always the concept's own 16-neuron allocation passing, everything
else noise. Hence 22% forever, regardless of clustering.

Second consequence: **reuse% was inflated**. A first-time word "found" neurons
belonging to any word containing it, so it reported reuse instead of growing.
The true reuse rate is therefore *below* the 96% measured here.

Fixed: exact (case-insensitive) match on `ConceptTag`.

**Expected effects:** `passed%` should jump from 22% toward 90-100%; group
sizes drop from ~78 to the concept's actual allocation; the train step gets
~4x cheaper; `reuse%` may *fall* — that would be a more honest number, not a
regression.

**Also observed:** `syn` in the Perf line is `CreateConceptualConnections`
(legacy random cross-cluster wiring into the old `_synapses` dict), not the
Hebbian step — it loads up to 3 clusters per learn event (35-105ms on resume).
Candidate for deletion; it predates the sparse synaptic graph and duplicates
its job badly. The Hebbian step itself is timed but never reported.

**Still open:**
- `Clusters: 0` and `Storage size: 103 B` in progress/final stats are bogus
  (read storage before first save).

#### P1 original notes
Code review (Jul 2026 reboot) found the Jan 19 "dead gate" was fixed on Jan 23;
current code should create synapses. Per ground rules, that claim needs a command:
- ✅ Instrumentation added: per-interval Hebbian histogram
  (`calls / neurons / passed% / delta min-avg-max / skip reasons / synapses_created`)
  printed with every training progress update.
- ✅ Self-check added: `dotnet run -- --test-hebbian` (temp brain, 3 sentences,
  asserts graph synapse count grows; exit code 0/1).
- **Exit criterion:** `--test-hebbian` passes, and a short `tatoeba_small` run on
  the fresh brainData shows `synapses_created > 0` with a sane histogram
  (deltas ≈ +5..+30, passed% high).

**P1 findings feeding later phases:**
- Regeneration recomputes Threshold/Bias from VQ code, discarding learned Bias →
  fidelity loss to be measured in P2.
- `Learn()` writes STM deltas; consolidation to real weights happens only at
  checkpoint, budget-capped (5–50 neurons/cluster), and `NeuronSnapshot` doesn't
  persist STM → LRU eviction silently drops unconsolidated learning (P3 must fix
  or consolidate-before-evict).
- Hebbian gate is non-selective: trained neurons sit ~10–27 above resting, so all
  batch neurons pass the 0.1 gate — "co-activation" currently means "same batch".
  Revisit selectivity in P4.
- `BrainStats.TotalSynapses` reports a legacy `_synapses` list, not the synaptic
  graph; use `GetSynapticGraphSynapseCount()` for the real number.

### P2 — The fidelity experiment (the experiment this project exists to run)
Build `--fidelity-test`:
1. Train on N sentences → snapshot **A** (full persistence, no eviction).
2. Force-evict everything → regenerate procedurally from VQ codes + persisted
   synapses → snapshot **B**.
3. Replay a fixed cue set against both; measure top-k activation overlap A vs B.
- **Exit criterion:** a number. Fidelity ≥95% = thesis supported at small scale.
  Materially lower = we learn exactly which state procedural regen loses, and decide
  with data whether the thesis survives.

### P3 — Reinstate limited persistence (the deferred "Phase 2", now the point)
- Procedural loading becomes the *default* path, not the fallback.
- Add a **persistence budget**: top-k synapses per neuron, dirty-region-only writes.
  (Current "90% compression" is mostly weak-synapse pruning; make the budget explicit
  and measurable.)
- **Exit criterion:** fidelity from P2 holds at ≤10% of full-persistence storage.

### P4 — Scoped activation distance (the "observer" concept)
- Make cascade depth / activation-distance `d` a first-class runtime parameter.
- Measure recall quality and compute cost as a function of `d`.
- **Exit criterion:** a recall-vs-d curve demonstrating useful recall inside a
  bounded activation scope — the "only render near the player" claim, quantified.

### P4.5 — Assembly recipes & composition (Bill's direction, 2026-07-29)
The compositional payoff of the procedural thesis: each stable concept has an
**activation recipe** — its assembly signature (anchor neuron set, VQ code
distribution, strong-synapse pattern). A compound concept ("red apple") is not
stored; it's a **procedurally generated column composed from its constituents'
recipes** at activation time. Direct kinship with assembly calculus
(Papadimitriou et al.: project / associate / merge) and vector-symbolic binding.

Prerequisites (why this sits after P2/P3): base assemblies must be stable
(P1.6) and their regeneration fidelity proven (P2) before composition is
meaningful — you can't compose recipes that don't reliably exist.

First experiment when we get here: train "red" and "apple" separately, compose
their recipes procedurally, test whether the composed column's activation
pattern matches a jointly-trained "red apple" baseline. Known hard problem to
respect: binding (a red apple ≠ an apple-colored red).

### P5 — Scale demonstration
- Grow virtual neuron count 10–100× on the same machine; hold resident memory flat
  (LRU work from Jan finally earns its keep here).
- Report the virtual-capacity vs. resident-memory curve. This is the headline
  artifact of the whole project.
- Only after this: reconsider big corpora, LLM teacher, richer tasks.

## What Was Deleted vs. Archived

- **Deleted (git history keeps them):** `EthicalDriveSystem`, `LongTermGoalSystem`,
  `DevelopmentalLearningSystem`, `InstinctualProcessor`, `ContinuousProcessor`,
  `EnvironmentalLearner`, `EnhancedContinuousLearner`, `Simple/Biological/Language-
  EphemeralBrain`, `ComprehensiveLanguageTrainer`, `LanguageFoundationsTrainer`,
  `TatoebaLanguageTrainer`, `MultiSourceTrainer`, `RealisticTrainingRegimen`,
  `LearningResourceManager`, `TrainingService`, `KnowledgeQueryCLI`, `EvalHarness`,
  `UnifiedTrainingEvaluator`, `BrainScanVisualizer`, root scratch tests/diagnostics,
  all `*.log`, `word_associations.json`, empty dirs.
- **Archived to `greyMatter/docs/archive/`:** phase summaries, fix write-ups,
  superseded roadmaps and status docs.
- **Kept (the load-bearing core):** `Cerebro`, `HybridNeuron`, `NeuronCluster`,
  `VectorQuantizer`, `SparseSynapticGraph`, `NeuronHypernetwork`,
  `ProceduralNeuronData`, `ProceduralCorticalColumnGenerator`, `FeatureEncoder`,
  `LSHPartitioner`, `EnhancedBrainStorage`, `GlobalNeuronStore`,
  `ProductionTrainingService`, Tatoeba/Wikipedia readers, query/inspection CLIs.

## Immediate Next Session

1. Commit this cleanup (see README note on Dropbox + git).
2. `dotnet build` — confirm clean build after prune.
3. Start P1: add the activation histogram, reproduce the negative-activation bug
   on a 5-minute run, fix, prove synapse creation.
