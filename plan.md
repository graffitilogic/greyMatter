# greyMatter — Implementation Plan

**Audience:** Implementing Agent
**Authority:** `Prompt.md` is the specification. This plan is the route to it. Where they conflict, `Prompt.md` wins.
**Prime directive:** Ship the proof-of-concept described in Prompt.md's *Deliverables* section, pass its *Guardrails*, and stop. Everything else is a tangent.

---

## 0. Rules of engagement (read before writing any code)

The current codebase is the residue of many refactor cycles. The following rules exist to prevent a repeat. They are not suggestions.

1. **Do not read or consult git history.** The current working tree plus this plan is the entire input.
2. **The legacy code is a read-only reference quarry.** Never edit files in `greyMatter/` (the existing project). Port logic *out* of it; never bolt new work *onto* it.
   *(Historical as of Addendum B.3, 2026-08-18: the legacy tree has been deleted. Recoverable from git history if ever needed. The rule is kept as the record of how the rebuild was conducted.)*
3. **Phase gates are hard.** Do not begin phase N+1 until phase N's gate passes. If a gate fails twice after honest attempts, STOP, write the finding in `RESULTS.md`, and surface it to Bill. Do not redesign mid-phase to chase the gate.
4. **No refactoring of a phase that has passed its gate.** If later work reveals a defect, fix the defect minimally; do not restructure.
5. **One plan document (this one), one results document (`RESULTS.md`, append-only).** Do not create additional strategy/architecture/vision markdown files. The old `docs/` folder was emptied for a reason.
6. **New experiments require registration.** Any evaluation beyond the ones specified in §6 gets one paragraph in `RESULTS.md` *first* (hypothesis, metric, decision rule), then code as a named subcommand of the eval CLI. No ad-hoc `--test-whatever` flags, no new shell scripts.
7. **No new abstractions without two concrete call sites.** No interfaces "for later", no manager/service/orchestrator classes. The legacy tree contains `IIntegratedBrain`, `SemanticStorageManagerStub`, `DeletedTypeStubs`, and six zero-byte gutted files — that is what this rule prevents.
8. **Determinism everywhere.** Every run reproducible from a seed. (Legacy comment in `Program.cs` line ~474: "Cluster IDs are Guid.NewGuid(), so cluster iteration order differs every run" — an entire class of noise that invalidated single-run results. Integer IDs and seeded RNG eliminate it by construction.)
9. **Every number reported in `RESULTS.md` carries the exact command line that produced it.**
10. **Honest nulls are deliverables.** The legacy eval harness's greatest strength was refusing verdicts it couldn't support (insufficient repeats, low bigram support, encoder-ceiling confounds). Keep that ethic; port those rules (§6.1).

---

## 1. Where the project stands (current-state assessment)

### 1.1 What the current tree is

A .NET 8 solution, single project `greyMatter/greyMatter.csproj` (MessagePack, ONNX runtime, System.Numerics.Tensors). Roughly 100 source files including:

- `Core/Cerebro.cs` — 3,374 lines. The brain class: cluster management, learning, probing, cascade, maintenance, save orchestration, stats, cognition modes. Accreted far past maintainability.
- `Program.cs` — 2,212 lines. A dozen one-off experiment entry points (`--fidelity-test`, `--encoder-ceiling`, `--cascade-test`, `--cascade-stats`, `--test-procedural-*`, `--production-*`) grown by accretion.
- `Storage/EnhancedBrainStorage.cs` — 1,991 lines of partitioned storage with a string-keyed concept→cluster inverted index.
- A stub graveyard: `LearnerStubs.cs`, `IntegrationStubs.cs`, `DeletedTypeStubs.cs`, `SemanticStorageManagerStub.cs`, plus six zero-byte files (`BrainInJar.cs`, `EnhancedEphemeralBrain.cs`, all of `Evaluations/`, etc.).
- ~15 shell scripts of overlapping purpose in `scripts/` and the project root.

### 1.2 What is genuinely proven and worth porting

| Component | File | Why it earned its place |
|---|---|---|
| Procedural neuron representation ("recipe = VQ code + deviations") | `Core/ProceduralNeuronData.cs`, `Core/ProceduralReceptiveField.cs`, `Core/FeatureMapper.cs` | This IS Prompt.md's "activation recipes / conceptual engrams." Deviation-from-prototype storage (only what learning moved gets bytes) is the right idea and already works at ~50–100 B/neuron vs ~500–1000 B full snapshots. |
| Vector quantizer | `Core/VectorQuantizer.cs` | The codebook that makes recipes compact. |
| LSH partitioning | `Core/LSHPartitioner.cs` | The "lookup scheme to determine which recipes may aid specific concepts." |
| Sparse synaptic graph mechanics | `Core/SparseSynapticGraph.cs` | Coactivation recording, decay, pruning, chunked export — the logic is sound; the data layout is not (see §1.4). |
| Surface-form feature encoder | `Core/FeatureEncoder.cs` | Useful as the *baseline stage* of encoding — and as the null model every result must beat (see §1.3). |
| NAS data plumbing | `Core/TrainingDataProvider.cs`, `Learning/TatoebaReader.cs`, `Learning/TatoebaDataSource.cs`, `Learning/SimpleTextParser.cs`, `Learning/CBTDataSource.cs` | Streams from `/Volumes/jarvis/trainData` without copying to SSD. Datasets confirmed present: `Tatoeba/`, `SimpleWiki/`, `CBT/`, `enhanced_sources/`, `structured_wikipedia/`. |
| Evaluation statistics + ground rules | `Program.cs` (`Spearman`, `RankOf`, `ScoreArm`, `RunEncoderCeiling`, verdict logic) | Hard-won methodology: repeats, shuffled nulls scored on identical pairs, PMI as primary order metric, support diagnostics, refusal to emit verdicts from n=1. Port the *rules*, rewrite the harness. |
| MessagePack partition storage format | `Storage/EnhancedBrainStorage.cs` (format only) | Compact binary partitions with gzip work fine. The 1,991 lines around them do not need to exist. |

### 1.3 The critical open finding: the encoder ceiling

The most recent work in the tree (`RunEncoderCeiling`, Aug 2026) established that `FeatureEncoder` encodes **surface form only** — spelling, n-grams, phonetics. Consequences, documented in the code's own comments:

- Trained-vs-control separation (AUC 0.94–1.00 across 40 fidelity runs) may be **entirely attributable to the encoder**, before any learning.
- The highest-magnitude encoder dims are generic (length, vowel ratio); discrimination lives in the low-magnitude tail — fatal for magnitude-weighted receptive fields.
- Order-learning experiments (P5.x cascade series) returned mostly null or low-support verdicts once confounds were controlled.

**Implication for this plan:** the rebuild must add a *distributional* (context/co-occurrence) component to encoding, and every recall result must be reported as **architecture lift = system metric − encoder-only-ceiling metric**. A system that doesn't beat its own encoder's ceiling has learned nothing. This is baked into the gates in §5 and the protocol in §6.

### 1.4 What blocks the stated end goal (CUDA later)

Bill's sequencing: prove the algorithms in .NET, port to CUDA afterward. The legacy substrate fights that port at every level: `Guid` neuron/cluster IDs (16 bytes, unordered, nondeterministic iteration), `Dictionary<Guid,double>` weights, per-neuron heap objects (`HybridNeuron`), LINQ in hot paths, GC-heavy save paths. The new substrate must be data-oriented from day one (§4.1, §7) — not because we're writing kernels now, but so the eventual port is a translation, not a rewrite.

### 1.5 Guardrail audit of the current state (from Prompt.md)

- *"Failure if it stores wordlists and concepts directly to disc"* — **currently violated in spirit**: `EnhancedBrainStorage` persists a string concept index; `NeuronSnapshot.AssociatedConcepts` and `ProceduralNeuronData.ConceptTag` write concept strings into partitions; `VocabularyNetwork` is a word store. The rebuild stores only hashed/sparse codes (§4.3).
- *"Failure if it only operates at hundreds wide and dozens deep"* — unproven either way today; the scale sweep (§5, P6) answers it.
- *"Success: train on a random dataset and test recall vs neural-network scales"* — the pieces exist but are welded into `Cerebro`; the rebuild makes this a first-class pipeline (§5, P5–P6).

### 1.6 Decision: clean rebuild beside the legacy tree

*(Historical as of Addendum B.3, 2026-08-18: the rebuild is complete and the legacy tree deleted. §1 stays as the record of what was inherited and why the rebuild happened — it is not a description of the current repo.)*

Given the refactor fatigue and the tangle above, this plan directs a **fresh, minimal project in the same solution**, porting only the table in §1.2. The legacy project stays on disk, read-only, as reference. When P6 passes, ask Bill before deleting anything. Do not "clean up" the legacy tree along the way — that is a tangent.

---

## 2. The system to build (restating Prompt.md as architecture)

One sentence: **a virtual neuron space far larger than RAM, in which a cue materializes only the neurons and synapses in its activation scope (procedurally regenerated from compact recipes), runs a local-learning cycle, persists only deviations from what regeneration would reproduce, and evicts — with recall quality measured honestly across scale settings.**

The mapping from Prompt.md:

- *"Short-lived neurons and synapses only needed for the scope of activation"* → the JIT runtime (§4.4): regenerate → activate → learn → consolidate deviations → evict.
- *"Minimalist storage of activation recipes / conceptual engrams"* → engram store (§4.3): VQ code + seed + sparse deviations per neuron; recipes per assembly.
- *"A lookup scheme to determine which recipes may aide specific concepts"* → LSH index over sparse codes (§4.3).
- *"The neurons and their synapses ARE the data and the processor"* → no separate knowledge base; recall = re-activation of regenerated structure. Nothing readable (no word lists) on disk.
- *"Trading recall accuracy for scale"* → the deviation threshold and working-set cap are explicit accuracy/scale dials; the scale sweep quantifies the trade.
- *"Configurable parameters to scale baseline size, activation depth and size"* → §4.5.
- *"Learning pipeline … testing pipeline"* → §4.6 and §6.

---

## 3. Project layout

```
GreyMatter.sln                     (existing — add the new project)
greyMatter/                        LEGACY — read-only reference. Never edit.
src/
  GreyMatter.Poc/                  the new console project (net8.0)
    Poc.csproj                     (MessagePack only; add nothing without need)
    Cli.cs                         single entry: gm <command> [options]
    Substrate/
      NeuronPool.cs                SoA arrays for the materialized working set
      SynapseStore.cs              CSR-style adjacency, capped degree
      Rng.cs                       splittable, seeded, deterministic
    Encoding/
      SurfaceEncoder.cs            ported FeatureEncoder (baseline stage)
      ContextEncoder.cs            distributional stage (new)
      SparseCode.cs                k-of-n code type + similarity
    Engrams/
      VqCodebook.cs                ported VectorQuantizer
      NeuronRecipe.cs              id, vqCode, seed, deviations[]
      EngramStore.cs               MessagePack partitions, load/save/append
      LshIndex.cs                  ported LSHPartitioner
    Runtime/
      ActivationScope.cs           materialize/evict lifecycle
      Cascade.cs                   propagation + k-WTA inhibition
      Plasticity.cs                Hebbian/STDP + deviation consolidation
    Pipeline/
      Corpus.cs                    ported TrainingDataProvider + readers
      Trainer.cs                   streaming learn loop, checkpoint/resume
    Eval/
      Harness.cs                   shared stats (Spearman, RankOf, AUC, d')
      EncoderCeiling.cs            ported, runs against BOTH encoder stages
      RecallEval.cs                trained-vs-control discrimination
      OrderEval.cs                 cascade-vs-corpus-statistics (P5.x rules)
      ScaleSweep.cs                the Prompt.md success experiment
tests/
  GreyMatter.Poc.Tests/            xunit; substrate + engram roundtrip tests
plan.md                            this file
RESULTS.md                         append-only findings log (create at P0)
```

Guideline sizes (soft, but a file at 2× these is a smell to raise, not a rule to silently break): Substrate ≤ 800 lines total, Encoding ≤ 700, Engrams ≤ 900, Runtime ≤ 900, Pipeline ≤ 600, Eval ≤ 1,400. The legacy project proves what happens without pressure in this direction.

---

## 4. Component specifications

### 4.1 Substrate (`Substrate/`)

- **Neuron identity is a `uint` index into a virtual space of `BaselineNeuronCount` neurons** (up to tens of millions). No `Guid` anywhere in the new code. Virtual = addressable; only the working set is materialized.
- `NeuronPool` is structure-of-arrays over the *materialized* set: `float[] potential`, `float[] threshold`, `float[] fatigue`, `float[] familiarity`, `uint[] virtualId`, plus a `virtualId → slot` hash. Fixed capacity = `WorkingSetMax`; materializing beyond capacity forces eviction (LRU by last-active tick).
- `SynapseStore`: CSR-style — per materialized neuron, a bounded segment of `(uint target, float weight)` pairs, `SynapseCapPerNeuron` max (default 32). Coactivation recording, decay (`ApplyDecay`), pruning (`PruneWeakSynapses`) port their logic from `SparseSynapticGraph` onto this layout.
- No LINQ, no allocation, no virtual dispatch in the per-cycle path. Plain `for` loops over arrays (this is the CUDA translation surface, §7).
- `Rng`: one root seed per run; child streams derived per (purpose, id) so results never depend on iteration order.

### 4.2 Encoding (`Encoding/`)

Two stages, both emitting a **k-of-n sparse code** (defaults n=2048, k=32; both configurable):

1. **SurfaceEncoder** — port `FeatureEncoder` (128-dim orthographic/phonetic vector), then top-k sparsification. This is the *null model*: fully deterministic, requires no training.
2. **ContextEncoder** — the new piece the encoder-ceiling finding demands. Online distributional refinement: maintain per-word context accumulators (random-projected co-occurrence counts within a ±2 window, updated during training), and blend into the final code: a word's code = top-k over `(1−β)·surface + β·context` (β configurable, default 0.5, β=0 must exactly reproduce the null model). Rare/unseen words degrade gracefully to surface-only.
   - This is *not* stored as a vocabulary table on disk (guardrail): accumulators live in the engram store keyed by code-hash like everything else, and are bounded (`ContextSlots` with decay/eviction).
3. `SparseCode`: overlap similarity, hash (stable 64-bit over active bit set), and the **rarity weighting** the ceiling experiment showed matters: dims weighted by inverse document frequency of appearing in top-k sets, not by magnitude.

### 4.3 Engram store (`Engrams/`)

- `NeuronRecipe` (the engram, ported concept from `ProceduralNeuronData` + `ProceduralReceptiveField`): `{ uint id; ushort vqCode; uint seed; (ushort dim, float delta)[] deviations; float familiarity; ushort activationCount }`. Regeneration = decode codebook prototype → derive receptive field deterministically from `(seed, vqCode)` via `FeatureMapper` port → apply deviations. Only weights that learning moved beyond `DeviationThreshold` are stored (this mechanism exists and works in the legacy tree; keep its semantics, change its layout).
- `VqCodebook`: ported `VectorQuantizer`; codebook size configurable (default 512); trained online during P4, frozen per checkpoint.
- Assembly recipes: a concept's engram is `{ codeHash → uint[] memberNeuronIds (sorted, delta-encoded) }` — which virtual neurons an activation pattern recruits.
- `EngramStore`: MessagePack partitions on `BrainDataPath` (default `/Volumes/jarvis/brainData_poc`), partitioned by LSH bucket of the sparse code. Append + compact; atomic writes (temp + rename, as legacy storage does).
- `LshIndex`: ported `LSHPartitioner`; maps a cue's sparse code → candidate partitions → candidate assembly recipes. This is the entire lookup scheme.
- **Guardrail enforcement:** no strings in any persisted record. Keys are code hashes and `uint` ids. A `--debug-labels` sidecar (hash → word, JSON, off by default) may exist for development but lives outside `BrainDataPath` and is excluded from all storage measurements. A CI-style check greps serialized partitions for ASCII runs ≥4 chars and fails if found (§5 P3 gate).

### 4.4 JIT runtime (`Runtime/`)

The activation cycle — the core loop of the whole project:

1. **Cue** → sparse code (encoder).
2. **Lookup**: LSH → candidate assemblies → recipe fetch (bounded: `ActivationWidth` assemblies max).
3. **Materialize**: regenerate member neurons into `NeuronPool` (skip already-resident; evict LRU if at `WorkingSetMax`), hydrate their synapse segments.
4. **Propagate**: up to `ActivationDepth` steps; each step, integrate inputs, fire, then **k-WTA inhibition** (`ActivationWidth` winners per step — the interneuron-inhibition LEGO). Neurons reachable via synapses but not yet materialized are regenerated on demand up to the working-set cap; beyond it, the cascade truncates (this is the accuracy-for-scale trade, measured, not hidden).
5. **Learn** (training mode): Hebbian coactivation on the synapse store; a short temporal trace links this cue's winners to the previous cue's winners (sequence/STDP-lite; `EndSequence` resets the trace). Neuron-level: nudge receptive-field weights toward inputs (port `ReinforceTowardInput`'s STM→LTM consolidation idea in array form: deltas accumulate in STM arrays, consolidate to recipe deviations on eviction/checkpoint).
6. **Consolidate + evict**: on eviction or checkpoint, diff materialized state against what regeneration would produce; write only above-threshold deviations back to recipes. Unchanged neurons cost zero write.
7. **Recall mode** = steps 1–4 + readout (activation mass per assembly), no writes.

### 4.5 Configuration (`Cli.cs`, one flat record, JSON-loadable, all CLI-overridable)

`BaselineNeuronCount` (default 1_000_000), `WorkingSetMax` (default 100_000), `ActivationDepth` (default 4), `ActivationWidth` (default 256), `PatternSize` n=2048, `Sparsity` k=32, `ContextBlend` β=0.5, `VqCodebookSize` 512, `DeviationThreshold` (port legacy default), `SynapseCapPerNeuron` 32, `Seed`, `BrainDataPath`, `TrainingDataRoot` (default `/Volumes/jarvis/trainData`), `Dataset` (tatoeba_small | tatoeba | simplewiki | cbt). The scale sweep varies exactly these; nothing is hard-coded.

### 4.6 CLI (single binary, no shell scripts)

```
gm learn  --dataset tatoeba_small --sentences 500 [--config …] [--resume]
gm probe  --cue <word> [--topk 16]
gm eval   encoder-ceiling | recall | order | scale   [eval-specific options]
gm stats                       # store size, recipes, bytes/neuron, working set
gm audit  --strings            # guardrail: scan partitions for readable text
```

---

## 5. Phases and gates

Every phase ends with: tests green, gate command run, result + command line appended to `RESULTS.md`. Estimated sizes are for orientation, not deadlines.

### P0 — Scaffold and baseline instruments
Create `src/GreyMatter.Poc`, `tests/`, `RESULTS.md`; wire into the solution; port `Harness` stats (Spearman/RankOf/AUC/d′ from legacy `Program.cs`) and `SurfaceEncoder` + `EncoderCeiling` eval; port `Corpus` (TrainingDataProvider + Tatoeba/SimpleWiki/CBT readers) with a `--local-sample` fallback so absence of the NAS doesn't block development.
**Gate:** `gm eval encoder-ceiling --train 500` runs on Tatoeba and reproduces the legacy finding (ceiling AUC, NN-overlap distribution, top-k collision curve) with numbers recorded in `RESULTS.md` as *the baseline every later result must beat*.

### P1 — Substrate
`NeuronPool`, `SynapseStore`, `Rng`, unit tests (materialize/evict/LRU, CSR record/decay/prune, determinism: same seed ⇒ bit-identical state after a scripted workload).
**Gate:** microbenchmark in `RESULTS.md`: 1M-neuron virtual space; materialize a 2,000-neuron scope, run a 4-step propagation, evict — sustained ≥ 50 cycles/sec single-threaded on the dev machine, zero GC gen2 collections during a 10k-cycle soak.

### P2 — Encoding
`ContextEncoder`, `SparseCode` with rarity weighting; property tests (β=0 ≡ surface null; determinism; graceful unseen-word path).
**Gate:** `gm eval encoder-ceiling` extended to both stages shows the context stage (after a 5k-sentence accumulation pass) separates at least one morphological-relative pair the surface stage confuses (e.g. from the legacy "hardest pairs" list: sleep/sleeps class), and top-k collision at k=32 stays 0% over a 3k-word vocabulary. Record both stages' ceilings.

### P3 — Engram store
`VqCodebook`, `NeuronRecipe`, `EngramStore`, `LshIndex`; roundtrip tests (recipe → regenerate → diff ≤ threshold; partition atomicity; LSH recall of planted neighbors).
**Gate:** at 100k stored recipes: mean bytes/neuron ≤ 100 B measured by `gm stats`; regeneration fidelity — 100% of weights within `DeviationThreshold` of their pre-save values; `gm audit --strings` clean (no readable text in partitions).

### P4 — JIT runtime (the heart)
`ActivationScope`, `Cascade`, `Plasticity`; `gm learn` and `gm probe` become real; STM→consolidation on evict; sequence trace + `EndSequence`.
**Gate:** train 500 Tatoeba sentences (seeded), then `gm eval recall --repeats 5`: trained-vs-control (mash + pseudoword controls, ported sets) **architecture lift** — system AUC minus the P2-recorded encoder ceiling AUC — ≥ +0.05 with non-overlapping repeat ranges; working set never exceeds `WorkingSetMax`; post-run store growth consists of deviations and assemblies only (verified by `gm stats` before/after).
*This gate failing honestly is a legitimate project finding — record it precisely; do not soften the metric.*

### P5 — Pipelines hardened
`Trainer` streaming with checkpoint/resume; `gm eval order` ported under the P5.x ground rules (§6.1); unattended-run ergonomics (progress lines, final summary).
**Gate:** one unattended `gm learn --dataset tatoeba --sentences 50000` completes with a mid-run kill/resume test producing a state equivalent (same stats within tolerance) to an uninterrupted run; `gm eval order --repeats 5` executes end-to-end and emits a rule-compliant verdict (any verdict — including NULL — is a pass; the gate is that the instrument works).

### P6 — The Prompt.md experiment: scale sweep
`gm eval scale`: the success criterion from Prompt.md. Sweep `BaselineNeuronCount` ∈ {10⁴, 10⁵, 10⁶, 10⁷} × `ActivationDepth` ∈ {2, 4, 8} × `ActivationWidth` ∈ {64, 256, 1024} (pruned grid is fine; ≥ 12 cells), fixed corpus and seed set, `--repeats 5` at each cell. Output: one table in `RESULTS.md` — recall lift, order metric, ms/sentence, bytes on disk, peak RSS per cell.
**Gate (== Prompt.md success):** the sweep runs on commodity hardware (the dev Mac; NAS for storage); recall is measurable and reported at every scale including 10⁶⁺ virtual neurons (guardrail: demonstrably beyond "hundreds wide, dozens deep"); the accuracy-vs-scale trade appears as a curve, whatever its shape. Deliver the table and a ≤1-page interpretation appended to `RESULTS.md`.

### P7 — Optional, only after P6 passes and Bill agrees
(a) Re-enable the LLM teacher (`Core/LLMTeacher.cs` reference) as a data-source enrichment; (b) SimpleWiki/CBT curriculum runs; (c) `CUDA-PORT.md` — a mapping of each hot loop (materialize, propagate, k-WTA, Hebbian update, consolidate-diff) to kernel sketches over the existing SoA layout. None of this starts unprompted.

---

## 6. Evaluation protocol (non-negotiable, inherited from hard experience)

### 6.1 Ground rules (ported from the legacy P-series; violations invalidated months of results)

1. **No verdict from n=1 on any correlation- or AUC-valued metric.** `--repeats 5` minimum; report mean and [min..max]; require non-overlapping ranges to claim separation.
2. **Nulls are scored on the same pairs as the real arm** (the P5.6 lesson: filtering to reached successors silently changed the experiment).
3. **Order claims use PMI-style base-rate-corrected association as primary** (raw bigram and unigram counts are collinear); real correlation must itself be positive — a positive gap over a more-negative null is not learning.
4. **Report support diagnostics** (fraction of bigrams seen >1×) and refuse order verdicts under 20% support.
5. **Controls must be at least as hard as vocabulary neighbors** — report the control-difficulty comparison every time (legacy section E).
6. **Every recall/discrimination result is reported as lift over the encoder ceiling** measured on the same encoder configuration (the §1.3 lesson: 40 runs of AUC 0.94–1.00 turned out to be a statement about the encoder).
7. **Experiments run on scratch brains** — an experiment must not mutate what it measures (isolated `BrainDataPath` per arm, deleted after).
8. Seeds fixed and recorded; arms differ by exactly one factor.

### 6.2 Standard metrics

Recall: AUC and d′, trained cue set vs mash + pseudoword control sets (port the legacy word lists). Order: pooled within-cue rank correlation of cascade mass vs corpus PMI, real vs shuffled arm. Cost: ms/sentence (learn), ms/probe, bytes on disk (`gm stats`), peak RSS, working-set high-water mark.

---

## 7. CUDA-portability rules (design pressure now, port later)

The port is out of scope until P7c, but every substrate/runtime decision obeys: SoA over AoS; `uint` indices, never references or GUIDs; fixed-capacity pools sized at startup; hot loops are flat `for` over contiguous arrays with no allocation, no LINQ, no virtual calls; k-WTA and propagation expressed as data-parallel passes (per-neuron map, then a reduction/partial-sort) rather than pointer-chasing graph walks; RNG is counter-based per (seed, id) so parallel execution is order-independent; float32 throughout (no doubles in hot state). If a design choice in P1–P4 would be awkward as a kernel over arrays, choose the alternative that wouldn't.

---

## 8. Definition of done

The POC is done when P6's gate has passed and `RESULTS.md` contains: the encoder ceilings (both stages), the P4 architecture-lift result, the order-eval verdict, and the scale-sweep table with interpretation — each with its command line. That satisfies Prompt.md's success criteria: training on a random dataset with recall tested across neural-network scales, at a scale demonstrably beyond hundreds-wide/dozens-deep, with nothing human-readable in the brain store. At that point: stop, present results to Bill, and ask before touching anything in P7 or deleting the legacy tree.

## 9. Known tangent attractors (name them to avoid them)

Attention systems, cortical-column messaging buses, pattern detectors, hierarchical learning managers, working-memory subsystems, voice/interactive modes, production-training services with monitoring shell scripts, LLM-teacher integration before recall works, storage-layer rewrites for speed before storage is measured slow, and any refactor whose justification begins with "while I was in there." All of these exist in the legacy tree; none of them are on the path to the P6 gate. If genuinely needed, they can be argued for in `RESULTS.md` — after P6.

---

# Addendum A (2026-08-17) — P7: Close the association gap

**Status of the base plan:** P0–P6 are complete and recorded in `RESULTS.md`. This addendum
**supersedes §5's P7** (the old optional-extensions list — LLM teacher, wiki curriculum, CUDA doc —
moves to P8 and remains deferred). All rules of engagement in §0 and all ground rules in §6.1
remain in force, plus the new rules in §A.2.

## A.0 Why this addendum exists: the system built is not yet the system Prompt.md asked for

Bill's assessment on re-reading Prompt.md against the P6 results: the alignment isn't there yet.
He is right, and `RESULTS.md` P5.8/P6.4 already contain the evidence. Stated without cushioning:

**Prompt.md asks for conceptual engrams — a system where activating a concept lights up a
synaptic graph that carries its relationships, the way a biological brain's does. What P0–P6
built is a system that knows, with great reliability and at impressive scale, *how often it has
seen each word* — and nothing about how words relate.** ρ(freq) → 1.00 while R_PMI ≈ 0.004
(P5.5, P6.1). The cascade exists but carries no signal; recall is effectively single-hop into
the cue's own assembly.

This one defect explains every other spirit-level miss at once:

- *"The neurons and their synapses ARE the data"* — currently the synapses are not the data;
  assembly membership plus accumulated drive (frequency) is. The 46.6 billion Hebbian updates of
  the 50k run produced a graph whose ranking of successors is indistinguishable from noise.
- *"Trading recall accuracy for scale"* — the trade does not exist (P6.3: flat over a 385×
  working-set reduction) precisely **because** recall never depends on multi-hop paths. The trade
  Prompt.md anticipated is a property of associative recall; a frequency detector has nothing for
  memory pressure to break.
- *"A concept can activate a comparable synaptic graph to a biological brain"* — a cue activates
  its own 256 neurons and a halo of synapses that rank nothing.

What P0–P6 *did* deliver — and P7 must not regress — is the substrate thesis: JIT
materialize/evict works, a 260-slot pool serves a 10⁷ virtual space, storage is recipes with
nothing readable on disk, and the instruments refuse dishonest verdicts. The foundation is sound.
The cognition on top of it has not been built. P7 is that build.

**Scope note on "concepts."** Words remain the operational stand-in for concepts throughout P7,
as in the base plan. Multi-word and cross-modal concepts stay out of scope until word-level
association exists — there is no meaning to "concept engrams" in a system that cannot yet
associate `water` with `drink`.

## A.1 Diagnosis: where the association went, on current evidence

The results already point at four suspects, in causal order:

1. **The synaptic budget is spent on wiring that encodes nothing.** All-pairs Hebbian among
   `ActivationWidth`=256 k-WTA winners proposes ~65k edges per step into 32 slots per neuron;
   97% are declined (P1.3). Critically, the winners within one cue are mostly *members of the
   same assembly* — so the slots fill with within-assembly edges, which encode only "I fired,"
   i.e. frequency, which familiarity/activation counts already track for free. Cross-assembly
   and cross-cue edges — the only ones that could carry association — arrive later and find the
   slots full.
2. **Displacement is structurally inert, so the graph cannot correct itself.** A candidate must
   beat the weakest incumbent; incumbents reinforce toward 1.0 while candidates are born at 0.11
   (P1.3). First-arrival wins permanently. Combined with (1), the graph freezes early into a
   frequency shape.
3. **The readout is dominated by hop zero.** Trained-cue mass ≈ 688 against an initial assembly
   drive of 256 (P4.1) — most of what cascade adds is first-hop, and nothing downstream of it is
   consulted in a way that could rank successors.
4. **Width is 5× overpaid** (P6.2: width 64 ≡ width 256 on every recall metric at 5× the
   throughput) — meaning there is free compute budget to spend on fixing 1–3.

These are hypotheses with evidence, not conclusions. P7.0 exists to convert them into
measurements before anything is changed — the base plan's discipline, kept.

## A.2 Additional rules for P7 (extending §0 and §6.1)

- **A-R1. Frequency-matched controls everywhere.** Every discrimination claim in P7 uses
  in-vocabulary, frequency-matched contrasts (the P4.2 lesson). The mash/pseudoword sets are dead.
- **A-R2. The shuffled-order null is the judge of "association", and it is a good one:**
  shuffling preserves every unigram frequency, so *any* mechanism — learning rule or readout
  arithmetic — that only encodes frequency scores identically in both arms and produces zero gap.
  A readout tweak that "finds" association a frequency-only graph cannot contain will be exposed
  by the null. Trust the instrument.
- **A-R3. Default changes are registered decisions.** P7 is allowed to change defaults the base
  plan fixed (`ActivationWidth`, `SynapseCapPerNeuron`, plasticity constants) — that is its job —
  but each change lands as a `RESULTS.md` entry stating the measurement that motivated it, and
  recall (`gm eval recall`) must be re-run to show no regression. Silent tuning remains forbidden.
- **A-R4. No new representational machinery until the synaptic channel is exhausted.** No encoder
  rework, no assembly-overlap schemes, no SDM detours while the P7.3 question is open. Those are
  the P8 fallback (§A.5), reachable only through a failed P7.3 and a design review with Bill.

## A.3 Phases and gates

### P7.0 — Attribution instrumentation (measure before touching)
Partition the synapse population and the recall readout by provenance, changing no behavior:
tag each synapse as within-assembly / cross-assembly (same-cue) / cross-cue (sequence trace), and
report per population: count, weight distribution, proposal/decline/displacement rates. Extend
`gm probe`/`gm eval recall` to attribute cascade mass by hop (0 / 1 / 2+) and by the synapse
population that delivered it. Extend `gm stats` accordingly.
**Gate:** one `RESULTS.md` table from a standard 4k-sentence run attributing (a) the synaptic
budget and (b) recall mass across these populations, plus the measured decline rate *per
population*. The A.1 hypotheses are each either confirmed or killed by a number.

### P7.1 — Rebalance the synaptic budget toward edges that can carry information
Informed by P7.0, stop within-assembly wiring from consuming the graph. Candidate levers, in
order of least invasiveness: per-population slot budgets (reserve most of `SynapseCapPerNeuron`
for cross-assembly/cross-cue edges); drop all-pairs within-assembly wiring entirely (frequency is
already tracked by familiarity — argue the redundancy in `RESULTS.md` if taken); reduce
`ActivationWidth` to 64 per P6.2 and spend the freed 5× on a larger cap. Pick the minimal set
that moves the P7.0 numbers.
**Gate:** cross-assembly + cross-cue synapses go from budget-starved to first-class — their
decline rate falls below 50% (P7.0 will have measured the baseline; expected ≈97%) and their
share of total slots exceeds 50% — while `gm eval recall --repeats 5` still passes its P4 bar
(lift ≥ +0.05, separated, zero-truncation config).

### P7.2 — Make slot competition live (displacement repair)
Fix the dead window: an incumbent must be contestable by an edge with genuinely more evidence.
Candidate mechanisms (choose by measurement, not preference): evidence-proportional challenge
(probabilistic displacement weighted by candidate vs incumbent accumulated coactivation);
incumbent weight decay that keeps saturated edges contestable without destroying them;
birth-weight derived from proposal pressure rather than a constant. Re-run the P1 substrate bench
(adversarial) and a real 4k-sentence run.
**Gate:** on the adversarial bench, displacement rises from 0.003% to ≥ 0.5% of proposals made
against a full segment; on the real run, a corpus-statistics shift test passes — train 2k
sentences, then 2k more with a deliberately altered successor distribution (a filtered corpus
variant), and the graph's top edges for affected cues measurably follow the shift. No recall
regression (A-R3).

### P7.3 — The association gate (the point of the addendum)
With budget and competition fixed, association either appears or it does not. Two instruments,
both existing or minor extensions, both under §6.1 discipline:

1. **Order:** `gm eval order --repeats 5 --train 4000 --min-successors 12` (the P5.5
   support-clearing configuration).
2. **Association:** new `gm eval assoc`, registered per rule 6 — for each cue, rank
   frequency-matched in-vocabulary words that *did* co-occur with it (within-sentence, window ±2)
   against those that never did, by cascade mass; AUC over cue set, ≥5 repeats, same-pairs
   shuffled null. If readout changes are needed to let multi-hop mass count (hop-0 subtraction,
   base-rate normalization at readout), they are made here and A-R2 polices them.

**Gate:** order — real `R_PMI` ≥ +0.10 with `PMI_GAP` ≥ +0.15 and non-overlapping repeat ranges
(the base plan's original LEARNED ORDER bar, §5-P5/legacy verdict rule); association —
`ASSOC_AUC` ≥ 0.70 vs shuffled null ≤ 0.55, separated. **Passing either at full rigor is a P7.3
pass**; passing both is the target.

### P7.4 — Re-measure the Prompt.md trade, which should now exist
With recall dependent on multi-hop paths, working-set pressure finally has something to break.
Re-run the P6.3 working-set sweep (`WorkingSetMax` from 100k down past assembly size) scoring
**association/order metrics, not just lift**; re-run the P6.1 scale grid at the new defaults;
re-open the P3/P5.2 bytes-per-neuron question against `SynapseCapPerNeuron` at the chosen
operating point and record the chosen point on the storage/recall curve.
**Gate:** the accuracy-for-scale curve appears — truncations > 0 in the constrained cells and a
monotone-trending degradation of the association metric as the working set shrinks — recorded as
the final table alongside a refreshed scale sweep. This, not P6.1, becomes the deliverable table
for Prompt.md's success clause, because it is measured on recall that finally has content.

## A.4 Stop rule

Each gate: two honest attempts, then stop and write the finding (§0 rule 3 unchanged). If P7.3
fails after P7.0–P7.2 have landed and their gates hold, that is a major negative result stated
plainly in `RESULTS.md`: *Hebbian wiring over hash-disjoint assemblies, at this design point,
does not encode association even when the budget and competition permit it.* Do not proceed to
P7.4 (there would be nothing to measure). Do not start P8 unprompted. Bring Bill the finding and
the A.5 options.

## A.5 The fallback design space (P8 candidates — locked until a P7.3 failure + design review)

Named now so they are not invented under pressure later, and explicitly out of bounds until then
(A-R4): **(a)** similarity-bearing assemblies — controlled member overlap proportional to code
similarity (SDR-style), reintroducing deliberately what P4.3-defect-4 removed accidentally, so
related words share substrate; **(b)** the context encoder feeding assembly recruitment (β
re-examined outside its P2.3 valley, with the OOV artifact controls A-R1 already provides);
**(c)** explicit anti-Hebbian/depression on non-coincidence (the legacy `DepressSynapse` idea) to
divide base rates out in the learning rule rather than the readout; **(d)** SDM-style content
addressing over engrams. Each would be its own gated phase with a registered eval. None are
licensed by this addendum.

---

# Addendum B (2026-08-18) — P9: Measure the right thing, then close; and the legacy cleanout

**Status of Addendum A:** P7.0 PASS, P7.1 PASS, P7.2 displacement PASS / shift criterion
UNMEASURED (instrument measured the wrong observable — P7.2.8). P7.3 was **never attempted as
specified**: Bill reviewed the P7.2.8 evidence and elected to take the A.5 fallbacks directly — a
recorded deviation from A-R4, decided by the plan's author. P8(a) failed its own pre-committed
rule and corrected P7.2.8's diagnosis on the way. P8(c) ran its two honest attempts and hit the
A.4 stop rule. All §0, §6.1 and A.2 rules remain in force.

## B.0 Where the evidence actually stands

The diagnosis chain is complete, and every link is measured: **not the budget** (P7.1:
cross-share 0% → 74.6%, multi-hop mass 0% → 33%), **not competition** (P7.2: displacement at
capacity 0.000% → 8.472%), **not the substrate** (P8a: co-occurring pairs 60% connected vs 15%
null, 418× the edge mass). The constraint is the learning rule — and P8c's two attempts sharpened
that into something more specific than A.5(c) anticipated:

- The λ that makes base-rate division *mechanically correct* (weight–frequency correlation ≈ 0 at
  λ=0.001) produces **no association** (`R_PMI` −0.075).
- The λ values that produce the project's only positive, null-separated association signal
  (λ=0.02: `R_PMI` +0.18 vs shuffled +0.04) do it by **sparsification** — pruning the graph to
  high-covariance edges at the cost of half the real path coverage — not by the registered
  normalisation mechanism.
- The event-wise anti-Hebbian rule A.5(c) actually describes was **never tested**: P8c's
  registration explicitly recorded that its analytic-proxy form "falsifies analytic base-rate
  correction over a saturating familiarity proxy, not the event-wise rule A.5(c) describes." That
  falsification is what happened. The event-wise rule needs an in-edge index the substrate lacks.

And one course-correction that outranks all of the above:

**The project has been grading itself on its hardest metric and never ran its most relevant one.**
`gm eval order` asks the graph to rank a cue's *successors against each other* by base-rate-
corrected sequence statistics — syntagmatic order, the hardest association question available.
Prompt.md's ask ("conceptual engrams"; the north-star example is `water`~`drink`-class relatedness)
is the *easier and more fundamental* question: does the cue's activated graph distinguish related
words from unrelated ones at all? That is exactly the `gm eval assoc` instrument P7.3 specified as
an equal-alternative gate ("passing either at full rigor is a P7.3 pass") — **and it was never
built or run**. Meanwhile P8a measured, at default settings, co-occurring pairs 60% connected with
418× the edge mass of frequency-matched non-co-occurring pairs. That is precisely the raw material
an association AUC reads out. It is plausible the system already passes the Prompt.md-relevant
gate at defaults, and the only reason we don't know is that the instrument doesn't exist.

P9 therefore starts by measuring the right thing, and only then decides how much more mechanism
work Prompt.md actually requires.

## B.1 Rules, amended

- **B-R1. A.5 ledger.** A.5(a) is consumed (tested, failed its pre-committed rule — P8a). A.5(c)
  is consumed *in its analytic-proxy form only* (P8c); the event-wise form is untested and is P9.2.
  A.5(b) and A.5(d) remain locked behind a design review. No third λ grid, ever — P8c.4's stop
  rule stands; P9.1 is a different hypothesis (mechanism discrimination), not a retune.
- **B-R2. The P8c bars do not move.** Any adoption decision in P9 is judged against the unchanged
  P8c.0 criteria (weight–freq falls; connectivity gap ≥ +0.30; recall lift ≥ +0.05 separated;
  `R_PMI` ≥ +0.10 with `PMI_GAP` ≥ +0.15, non-overlapping) — or, for the assoc route, the
  unchanged P7.3 bar (`ASSOC_AUC` ≥ 0.70 vs shuffled ≤ 0.55, separated).
- **B-R3. The shift eval stays parked.** Its redesign (cascade-mediated observable, aggregated
  pairs — P7.2.8 consequence 1) is registered only if and when a P9 gate needs it. Rebuilding a
  defective instrument nobody currently depends on is a tangent.
- **B-R4. Instrument-first discipline, kept:** P9.0 changes no behaviour; every mechanism phase
  after it changes exactly one thing and re-verifies recall (A-R3).

## B.2 Phases and gates

### P9.0 — Build and run `gm eval assoc` (the missing P7.3 instrument)
As specified in P7.3, unchanged: for each cue, rank frequency-matched in-vocabulary words that
*did* co-occur with it (within-sentence, window ±2) against those that never did, by cascade mass;
AUC over the cue set, ≥5 repeats, same-pairs shuffled null (A-R2 polices any readout arithmetic).
Run at current defaults (λ=0, `ContestErosion` 1e-5, quota 64, cap 8) and, as a recorded
diagnostic arm only, at λ=0.02. Registration paragraph in `RESULTS.md` first (rule 6).
**Gate:** the instrument runs end-to-end and emits a rule-compliant verdict — any verdict,
including a refusal, passes the *instrument* gate (the P5 precedent). **Decision fork, fixed
now:** `ASSOC_AUC` ≥ 0.70 vs shuffled ≤ 0.55 separated at defaults → **P7.3 is declared passed on
its association arm**; skip P9.1/P9.2 and go directly to P9.3. Below the bar → its per-cue
diagnostics become P9.1's baseline. Between (signal but short) → P9.1 proceeds with the assoc
metric added to its judgement alongside order.

### P9.1 — Discriminate sparsification from normalisation (the P8c.5 open question)
One registered experiment, three arms differing in exactly one mechanism, all at the P8c-strongest
operating point so results are comparable to the recorded +0.18:
(i) **depression as measured** (λ=0.02 — prunes via the creation guard and rescales);
(ii) **rescale-only** — identical Δw but weights floor-clamped above the prune/guard thresholds so
no edge is ever deleted by depression;
(iii) **prune-only** — λ=0, plus a post-hoc covariance prune that removes the same *fraction* of
edges arm (i) loses (coverage-matched by construction), touching no surviving weight.
Judge each arm on `R_PMI`/`PMI_GAP`, `ASSOC_AUC`, connectivity gap and coverage, recall lift —
≥5 repeats, shuffled nulls, seeds fixed.
**Gate:** the arms separate — the +0.18 is attributed to one mechanism with non-overlapping
ranges. **Adoption** only if some arm meets B-R2's unchanged bars in full; otherwise record which
mechanism carries the signal and what coverage it costs, and take that to Bill. This phase's two
honest attempts are its own (new hypothesis, per P8c.5's closing paragraph); it is not λ-grid #3.

### P9.2 — The event-wise rule, on its own substrate change (conditional)
Only if P9.0 and P9.1 both end short of their bars. Add the in-edge index (CSR by target
alongside CSR by source — a registered substrate change; re-run the P1 bench and re-verify its
gate before any learning experiment), then implement A.5(c) as written: depress s→t when t fires
without s. Expected magnitude ∝ p(t)(1−p(s)) — the correct sign P8c.0 derived and could not
implement. **Gate:** the unchanged P8c.0 criteria, judged once, two attempts max, stop rule as
ever. A failure here, after P9.1, exhausts the synaptic-channel program: the finding goes to Bill
with A.5(b)/(d) as the remaining reviewed options.

### P9.3 — Close out Prompt.md (former P7.4, unchanged in substance)
Triggered by **any** association pass (P9.0 fork, P9.1 adoption, or P9.2). Re-run the working-set
sweep scoring association/order metrics; refresh the scale grid at adopted defaults; settle
bytes/neuron against `SynapseCapPerNeuron` at the chosen operating point.
**Gate:** the accuracy-for-scale curve appears on recall that has content — truncations > 0 in
constrained cells with monotone-trending degradation of the association metric — recorded as the
final deliverable table for Prompt.md's success clause.

## B.3 The legacy cleanout (Bill's directive, authorized now)

Bill's call: the point of diminishing returns on borrowing from the old architecture has been
reached or passed. The evidence agrees — every §1.2 port candidate has been ported or deliberately
replaced, P7–P8 mined the legacy tree for nothing but one comment, and the only asset with
plausible future value (`Core/LLMTeacher.cs`, deferred to P10) is recoverable from git history if
ever needed. §0 rule 1 forbids consulting history for *design decisions*; using git as the recycle
bin for a deleted file is not that.

Execute as **one commit containing no code changes**, at any point before or during P9:

1. **Audit first** (recorded in the `RESULTS.md` entry): confirm `src/` and `tests/` contain no
   reference to the legacy project (project refs, usings, paths); confirm the legacy tree's only
   unported asset of note is `LLMTeacher.cs`; list anything else found or state there was nothing.
2. **Delete:** the entire legacy project `greyMatter/` (source, scripts, tests, .vscode, test_data);
   the empty root `docs/`; stray `.DS_Store` files (and add to `.gitignore` if absent).
3. **Solution:** remove the `greyMatter` project entry and its configuration/nesting lines from
   `GreyMatter.sln`, leaving Poc and Tests.
4. **Restore `Prompt.md` to the repo root.** It is this plan's stated authority (§ header) and is
   currently absent from the tree. The plan cannot outrank a file that isn't there.
5. **Docs:** update README's "Where to look" table (drop the legacy row); add a one-line note at
   §0 rule 2 and §1.6 marking them historical as of this addendum — do not rewrite them (the
   assessment stays as the record of what was inherited and why the rebuild happened).
6. **Verify, then record:** `dotnet build GreyMatter.sln -c Release` clean; full test suite green;
   `gm eval recall --repeats 3 --train 500 --working-set-max 500000` reproduces the recorded
   numbers; `gm audit --strings` clean. Paste all four commands and outcomes into the `RESULTS.md`
   cleanout entry. If any check fails, revert the commit rather than patching forward.

Out of scope for the repo cleanout, flagged for Bill to handle when convenient: legacy brain data
under `/Volumes/jarvis/brainData` (the POC writes to `brainData_poc` and scratch paths), and the
legacy `bin/`/`obj/` build residue disappears with the tree.

## B.4 Definition of done, restated

Done is P9.3's gate: Prompt.md's success clause measured on recall that carries association, at
scale, on commodity hardware, with nothing readable on disk — plus a repo whose only source tree
is the POC. The fallback ledger (B-R1) and stop rules bound everything else. When P9.3 passes, or
when P9.2 fails its two attempts: stop, present to Bill.

---

# Addendum C (2026-08-21) — P10: Convergence. The campaign gets a bound and an ending.

**Occasion.** Bill's assessment of the running session: "starting to feel a little like groundhog's
day — burning tokens for tokens' sake." This addendum agrees, shows why with the record's own
numbers, and converts the open-ended program into a two-step endgame with a written verdict.

## C.0 The Groundhog Day diagnosis, measured

P9 was honest work — the discipline never slipped. It also produced almost no new facts about the
*system*. What it produced:

- `gm eval assoc` finally exists — and reads **chance at every configuration ever tried**:
  `ASSOC_AUC` has never exceeded 0.58 under any λ, any of three readouts, either pair definition
  (P9.0, P9.1, P9.2R). That is the one genuinely new system fact in the whole addendum-B campaign.
- The order signal has been **+0.16–0.18 since P8c.1**. Three phases have since re-measured it from
  new angles (P9.1's three arms, P9.2R's readouts) without moving it. P9.1 did settle *what* it is
  (normalisation, not sparsification — a real attribution) — but its bars were missed by the **same
  two criteria at the same margins as P8c.1**: connectivity gap 0.275 vs 0.30, `PMI_GAP` ~0.143 vs
  0.15. The same near-miss has now been recorded three times.
- The last three substantive entries — P9.2R, P9.3D, P9.3E — are **instruments examining
  instruments**: a positive control failing, a null found unbalanced (P8a's 418× deflating to
  11.96×), and a meta-check to catch the next unbalanced null. Each was necessary. None taught us
  anything about the brain being built. Null-construction defects have now consumed more of the
  P7–P9 token spend than any mechanism result.

The loop has a structural cause, not a behavioral one: **stop rules bound each phase, and nothing
bounds the campaign.** Every honest near-miss licenses one more registered diagnostic, forever.
Rule 6 (registration) was designed to prevent undisciplined experiments; it cannot prevent an
unbounded sequence of disciplined ones. This addendum adds the missing bound.

## C.1 Endgame rules

- **C-R1 (closed list).** The remaining program is exactly P10.1 and P10.2 below. No new
  registrations, no new instruments, no new mechanism arms, no re-measurement of anything on the
  settled ledger — without Bill's explicit go-ahead, given in conversation, not inferred from the
  plan. Rule 6's registration path is suspended for anything outside this list.
- **C-R2 (settled ledger — closed to re-opening).** Substrate thesis at scale (P6.3). Budget fixed
  (P7.1). Competition fixed (P7.2). Substrate-not-the-constraint, in its corrected 12× form
  (P8a + P9.3D). Normalisation-carries-order, sparsification-carries-nothing (P9.1). k-WTA is not
  where association is lost (P9.2R). The order gate is **spent**: two honest attempts (P8c.1,
  P8c.5) plus a mechanism attribution (P9.1) all land at WEAK ORDER SIGNAL, +0.16–0.18, bar unmet.
  It does not get a fourth attempt.
- **C-R3 (diagnostics are cheap by decree).** A diagnostic runs once, at n=1, on one persisted
  brain, and claims nothing. Repeats are spent only where a pre-registered bar is being judged.
  No more 5-repeat multi-arm sweeps in service of a question a printout answers.
- **C-R4 (instrument hardening is finished).** P9.3E closed it: the sample-composition check exists,
  is wired into recall/order/assoc, and is tested against the historical defects. No further
  meta-instrumentation unless two instruments contradict each other again.
- **C-R5 (the deliverable is the verdict).** P10 ends with the P10.2 capstone entry *regardless of
  outcome*. A bar not passed is a finished result, not a reason for P11. There is no Addendum D on
  this campaign: whatever follows the verdict starts as a new directive from Bill.

## C.2 P10.1 — The last diagnostic: read the tie structure of the association AUC

One persisted 2k-sentence brain, one run, no repeats (C-R3). P9.3D already registered the suspect
and its prediction: at 60% / 38.1% connectivity, roughly a quarter of all AUC comparisons are
0-vs-0 ties scoring 0.5 by definition, so the AUC may be dominated by empty pairs regardless of
what the non-empty ones say. Print, from the existing `gm eval assoc --readout edge` path:

1. the measured tie fraction in `Harness.Auc`, both arms;
2. the AUC restricted to pairs with non-zero mass in at least one arm — **diagnostic only**: the
   gate number stays the full-sample AUC, because an edgeless related pair is a real recall failure
   (the P5.6 lesson), and coverage may not be assumed away;
3. per-pair mass distributions for related and unrelated pairs (the numbers P9.2R aggregated);
4. the residual ±2 co-occurrence rate of the global-redistribution null corpus — the one P9.2R
   candidate this printout can close for free.

**Decision rule, fixed now — both branches end in P10.2:**

- **Restricted AUC ≈ chance too** → even where edges exist, per-pair edge mass does not
  discriminate related from unrelated. The 12× ratio is a diffuse population effect invisible at
  pair level. The synaptic channel is exhausted *by measurement*, and the verdict says so.
- **Restricted AUC clearly separated** → the discrimination exists and is masked by coverage: ~40%
  of related pairs have no edge at all, a direct property of hash-disjoint assemblies. Coverage is
  representation, representation is the locked A.5(b)/(d) territory, and building it is a new
  campaign, not a P10 step. The verdict names A.5(b) as the single evidenced lever, with this
  measurement as its quantified motivation.

If implementing the printout surfaces any defect needing more than a trivial fix: do not fix it —
record it and proceed to P10.2 with the tie hypothesis marked *untested* (the verdict absorbs an
unmeasured diagnostic; the campaign does not reopen for it).

**There is no P10.1b.** No conditional mechanism phase hangs off this diagnostic. That is the
difference between this addendum and both of its predecessors.

## C.3 P10.2 — The verdict (capstone entry in `RESULTS.md`, plus a README truth pass)

One entry, written for a reader who will not read the 2,700 lines above it. Required content:

1. **Prompt.md scorecard.** Letter of the success criteria: met at P6 and still true (scale table,
   guardrails, nothing readable on disk, commodity hardware). Spirit: partial — the system
   demonstrably learns *frequency* (ρ→1.00), carries a real but weak *order* signal under
   base-rate-corrected weights (+0.18 vs null +0.04, bar unmet), and has **no readable pairwise
   association** (never >0.58, any configuration). The measured causal chain, one line per link,
   each with its section reference — through to the P10.1 branch that fired.
2. **The abandoned-not-failed ledger.** Event-wise anti-Hebbian (P9.2 — never run; set aside
   because P9.1 showed the learning rule is no longer the binding constraint). A.5(b)
   context-similarity recruitment. A.5(d) SDM addressing. Shift-eval redesign (B-R3). For each:
   one line on why it stopped and what evidence would justify reviving it.
3. **What survives any continuation.** The substrate (JIT materialize/evict at 10⁷:260, procedural
   recipes, determinism through an OS suspend, CUDA-ready SoA layout), the instrument suite with
   its hard-won rules, and the corrected record (418×→12×; sparsification→normalisation;
   lottery→specific-but-sparse).
4. **The options, priced, for Bill** — the fork this campaign returns to its owner rather than
   deciding: **(a)** declare the POC complete as the substrate deliverable and take association
   into a representation-redesign v2 (the A.5(b) shape: similarity has to enter the representation,
   because the one channel that ignores similarity by design was measured unable to carry it);
   **(b)** write `CUDA-PORT.md` for the substrate now — the port thesis was proven at P6 and does
   not depend on the association outcome; **(c)** park the project with the verdict as its record.

README updated to the final state in the same commit; §0 rule 5 holds — no new documents. P9.3's
trade closeout is recorded as **unreached** (its trigger — an association pass — never fired), not
failed.

**Gate:** Bill reads it. The campaign is over when this entry lands.

## C.4 Cost envelope, stated so it can be held to

P10 in total: one build, one 2k-sentence training run, one diagnostic eval pass, and writing.
Everything else this addendum forbids is forbidden *because* of what P9 measured: the marginal
token is currently buying instrument archaeology, not knowledge. Ending a campaign well is the
last thing it can spend tokens on that returns more than it costs.
