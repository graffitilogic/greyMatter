# greyMatter — Implementation Plan

**Audience:** Implementing Agent
**Authority:** `Prompt.md` is the specification. This plan is the route to it. Where they conflict, `Prompt.md` wins.
**Prime directive:** Ship the proof-of-concept described in Prompt.md's *Deliverables* section, pass its *Guardrails*, and stop. Everything else is a tangent.

---

## 0. Rules of engagement (read before writing any code)

The current codebase is the residue of many refactor cycles. The following rules exist to prevent a repeat. They are not suggestions.

1. **Do not read or consult git history.** The current working tree plus this plan is the entire input.
2. **The legacy code is a read-only reference quarry.** Never edit files in `greyMatter/` (the existing project). Port logic *out* of it; never bolt new work *onto* it.
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
