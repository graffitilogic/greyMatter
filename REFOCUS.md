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
3. **Known-broken state at last commit:** debug logs (Jan 19) show Hebbian
   activations averaging ~−59 with zero neurons above threshold → *no new synapses
   form*. The learning loop is silently dead. The "133M synapses" milestone predates
   this regression.
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

### P1 — Resurrect the learning loop (prerequisite bug fix)
The Hebbian threshold gate never passes: activations are deeply negative
(avg ≈ −59, `above_threshold=0` on every batch).
- Instrument: activation histogram per batch (min/mean/max, % above threshold).
- Locate why membrane potentials go negative (bias accumulation, decay sign,
  or unnormalized weighted sums in `HybridNeuron`/cluster processing).
- **Exit criterion:** a unit test proving two co-activated neurons form a synapse,
  and a 10-minute `tatoeba_small` run that *creates* synapses (count grows, logged).

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
