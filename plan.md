# greyMatter — active recovery plan and agent guide

**Directive:** Bill, 2026-09-09. Build the proof of concept originally requested: learn from local data, recall related material through a learned network, and execute that network with substantially less resident memory than its complete learned state requires.

**Status:** Exact deferred-decay correction PASSED (180 tests; 5,120 frozen checks). R4 stopped after first 1x paired cell missed the registered 10x latency target (27.39x). Full capacity remains unmeasured; no larger run or R5 started.

## Authorized continuation — activation-travel review (2026-09-09)

Bill authorized this bounded review after the R1 stop, and permits brain-data resets.
Use fresh scratch state; deletion is unnecessary for this review. R1 remains failed;
this directive does not silently authorize new learning sweeps or R2.

Review T1: train the registered seed-100 cue-member arm once, freeze it, and inspect
all 32 four-hop chain queries. Record per-step activation before selection, selection
survival, and threshold eligibility at each chain position. Also count actual learned
edges between successive assemblies. Observers must not alter query scores or learned
state. Distinguish edge existence from usable activation; no new pass bar or success
claim is allowed from this diagnostic. Bound: one development run, targeted observer
correctness tests, one written design recommendation and handoff. No parameter tuning.

**Current work:** Approved correction and first registered 1x cell completed. Preserve the measured latency miss and low supported recall; corrective attempt consumed. Follow the bounded failure policy; do not silently tune or launch the remaining grid.

## A1 — protected assembly relay (authorized by Bill)

One explicit experimental model: the first eight deterministic members of a code's
assembly are both its entry and exit cohort. Only observed adjacent training cues create
relay-to-relay synapses, using the existing bounded Hebbian update and degree cap.
Relay storage is isolated from within-cue reinforcement. No answer labels, candidate
sets, corpus lookup, or completion oracle enter runtime learning or propagation.

Each source distributes its current drive in proportion to its positive outgoing
weights; the next tick uses fresh incoming potentials, not retained old potentials.
Keep the existing .5 source firing threshold and 256 winner cap. Sum delivered drive
for candidate scoring, including zeros and ties. This coherent relay contract changes
both topology and execution from R1; do NOT attribute outcomes to one scalar change.
Default legacy runtime remains unchanged. Eight members is fixed, not a sweep.

Use existing seed-100 development task, then fresh seeds 201–205 (R1 final seeds were
already examined). Same 32 candidates, 128 direct/128 composed queries, same episodes,
frequency matching and nulls. Report learned, untrained, shuffled, and transition-count
baseline. Connectivity: every observed adjacent pair has positive relay edges, and all
96 intermediate junctions share at least one receiving/outgoing relay member per seed.
Transmission: hand-built correctness fixtures plus observed four-hop emission traces;
a fixture cannot pass learning. Discrimination: mean direct/composed top-1 >=80%, each
seed >=70%, mean lift >=20 points over both untrained and shuffled, unchanged quality
requirements. Simulation ticks equal query path length for this relay contract.

Do not claim 256 active neurons per concept: only eight participate here. Report actual
learned nodes/edges and relay collisions. This may behave similarly to the transition
baseline; report that plainly. It is a routing substrate candidate, not a demonstrated
new theory of learning. It remains fully resident; no paging or CUDA claim.

One development smoke, one five-seed evaluation, no parameter tuning. Persist numeric
recall snapshots plus separate evaluation codes, and verify exact round-trip replay.
Deliver working code, tests, results and limitations. If it passes, close A1 with a
concrete R2 storage boundary recommendation; do not silently treat the historical R1
as passed or restart the whole old campaign. If it fails, report that failure without
another automatic mechanism arm.

## Read this first — every agent, every handoff

1. Read `Prompt.md`, this active guide through the historical boundary, and the latest recovery entry in `RESULTS.md`. Inspect the current code and working-tree changes before acting. Do not consult git history for design.
2. `Prompt.md` remains the authority for purpose. This new directive supersedes conflicting execution rules and settled-ledger claims in the historical plan below. The old P0–P10 campaign stays closed. This is a new campaign, not Addendum D or P11.
3. Work only on the current phase. Advance automatically after its gate passes; do not repeatedly ask Bill to authorize already specified work. A failed gate follows the bounded failure policy below.
4. Treat comments, README assertions, old verdicts, and another agent's summaries as claims to check. Never translate “tests pass” into “scientific hypothesis proven.”
5. Keep this as the single plan and `RESULTS.md` as the append-only evidence log. Update README orientation where specified. Do not create competing roadmaps. Small machine-readable run manifests and raw result artifacts are evidence, not new plans.
6. Preserve user changes. Bill authorizes resetting inconsequential brainData; prefer uniquely named scratch stores and never reset unrelated data. No automatic commits, publication, dependency changes, or new external services are needed for this plan.

**North-star demonstration:** a cue retrieves learned related material by following connections through repeated loading and eviction, while total application memory is bounded independently of the amount of learned state on disk. Increasing actual learned capacity must be measured separately from increasing the available ID range.

**Not promised:** biological equivalence, general intelligence, language generation, or replacing frontier training clusters. A useful resource/quality tradeoff is sufficient; a well-isolated negative result is also a finished outcome.

## What the review established, and what remains open

Verified by source inspection, not a fresh benchmark:

- `Runtime/Cascade.cs` follows a synapse only if its target is already in `NeuronPool`; it skips nonresident targets. Its header claims otherwise.
- `Runtime/ActivationScope.cs` retains recipes in an unbounded dictionary; `Pipeline/Checkpoint.cs` reloads all partitions into it. Bounded active slots do not bound total learned-state RAM.
- `Eval/RecallEval.cs` measures trained-versus-withheld activation mass. This is recognition/familiarity, not retrieval of associated knowledge.
- `Runtime/Assembly.cs` hashes codes into virtual IDs. Address range is not a count of independently learned, useful neurons.
- `Cli.cs` currently implements `probe` by training a fresh brain before probing, not by querying the saved brain. It prints neuron IDs, not related material.

The historical association failures are evidence about the tested system. They do not prove that representation is the sole cause, that learning is exonerated, or that the complete substrate thesis passed. Keep compression, deterministic IDs, sparse storage, and lifecycle components as candidates for reuse, not certified solutions.

## Contracts that must not drift

### Learning and evaluation

- Recognition asks “have I seen this?” Association asks “what belongs with this cue?” Report them separately; recognition cannot pass an association gate.
- A manually wired graph is a runtime correctness fixture, never evidence of learning. A transition-count baseline is explicitly a baseline; do not rename it a biologically inspired learning success.
- Evaluate query-local rankings over fixed candidate sets, then aggregate over queries. Never compare positives from one cue with negatives from another as the primary retrieval metric.
- Include zero scores, unreachable targets, ties, and failed queries. Specify deterministic tie handling. Do not filter nonzero cases to rescue a gate.
- Keep test labels, candidate lists, and answer decoding outside the brain. Evaluation must not expose the correct target to traversal or add an answer edge.
- Freeze the encoder, learned snapshot, readout, candidate definition, and configuration for a resident-versus-paged comparison. Only the storage/execution policy changes.
- Persistent model state is immutable during recall, including familiarity and fatigue that otherwise affect later queries. Query-local scratch may change. Query order must not silently train the brain.

### Memory, storage, and execution

- Budget recipes, indexes, encoder state, synapses, active/frontier buffers, writeback buffers, and temporary allocations. A hidden full-store dictionary invalidates a bounded-memory claim.
- Report peak process RSS and managed heap separately from owned-buffer accounting. Report operating-system file caching and memory mapping limitations explicitly. A fresh process is not proof of cold physical storage.
- Use a stable virtual ID for pending work; resident slot numbers may change during eviction/compaction. Never retain an unpinned slot across operations that can evict it.
- For exact mode, process a logical propagation step deterministically across pages. Aggregate contributions before selection; paging order must not change the numerical algorithm. Spill intermediate state if necessary or refuse an insufficient budget explicitly.
- Count missing/corrupt learned records as errors. Only genuinely unseen IDs may regenerate baseline state. Capacity loss, deferred work, and deliberate approximate pruning must be observable.
- Persist learned numerical state and necessary numerical addressing metadata. Do not persist wordlists, source passages, answer tables, or vocabulary disguised as bytes. Numeric weights inevitably encode learned information; the no-strings audit is not proof that memorization cannot occur.
- Human-readable results and an external evaluation/display vocabulary are allowed outside model storage. Construct display mappings from user-supplied source data; never let that mapping compute the association score. Include its memory in end-to-end utility measurements.

## R0 — establish an honest baseline and freeze the experiment contract

**Question:** what does the current executable do, and exactly what will count as recovery?

**Work:**
- Inspect repository instructions and working-tree changes. Record environment, runtime, hardware memory, available scratch space, and source-file checksums. Do not reinstall the toolchain without need.
- Build and run the existing test suite once. Classify failures as pre-existing, environmental, or introduced; a missing SMB corpus is not a model result. Use local synthetic fixtures until real data is needed.
- Append a recovery correction to RESULTS and add a short README status banner linking this guide. Preserve historical numbers; explicitly withdraw the conclusions that the complete paging thesis and uniquely located representation cause were proven.
- Register the R1–R5 protocol before seeing recovery outcomes: generators/splits, seeds, candidate counts, metric formulas, tie handling, budgets, and exact commands as implemented. Planned commands must be labelled unimplemented until they exist.
- Default synthetic evaluation: five fixed seeds 101–105, 32 frequency-balanced candidates/query, at least 100 test queries/seed. Train on random symbol sequences with deterministic generation and disjoint train/test episodes. Include one-step associations and 2–4-hop paths whose endpoint pair never occurs directly in training. Use distractors with matched exposure. Give every test endpoint training exposure; this tests relational composition, not unseen-symbol encoding.
- Before final evaluation, use a separate development seed (100). Fix real-data split and support criteria in R0 if data is available; otherwise register them at R5 before inspecting scores.

**Gate:** build/test status recorded; corrected claims visible; implementer can state the task, baseline, metric, and stopping rule without consulting old phase prose. An environment block prevents dependent work, not independent source review.

**Steering:** do not rerun old lambda sweeps, shift diagnostics, or the full P6 grid. Do not spend R0 repairing every historical instrument.

## R1 — demonstrate useful learning with the whole small network resident

**Question:** can this learning system retrieve an associated target before memory virtualization complicates it?

**Work:**
- Add one named recovery evaluation entry point in the existing CLI, with fixture/learning/paging/scale modes only as needed. Reuse trustworthy math, not historical verdict text.
- First implement small manually wired chain, branch, cycle, and distractor fixtures. Verify direction, hop limits, ties, readout, and reset behavior. These establish only execution correctness.
- Train the synthetic sequence task through the actual learning pipeline. Score candidate assemblies from propagated activation; do not score by consulting training pairs. Report direct and composed retrieval separately.
- Compare with an untrained model, a globally shuffled training-sequence control, and a simple transition-count graph with the same traversal budget. Chance co-occurrences in a shuffle are expected, not automatically a defective null. Print actual cue/candidate coverage.
- Begin with the existing representation and learning rule. If it fails, allow exactly one documented, local correction motivated by traced learning events: e.g. ensuring sequential coactivation can create the intended cross-cue edge. Do not replace the learner with the baseline or open a representation search.

**Gate:** on both direct and composed tasks, mean top-1 accuracy >= 80%, every seed >= 70%, and mean improvement >= 20 percentage points over both untrained and shuffled controls. These are new engineering acceptance targets, not biologically derived thresholds. Report MRR and transition-baseline performance too; beating that baseline is not required for the paging experiment.

**Failure:** after the initial implementation and one bounded corrective attempt, stop the campaign at “learning prerequisite unmet.” Preserve the runtime fixtures. Propose a separate learning-design decision, not another grid.

**Steering:** a higher familiarity AUC cannot pass R1. Larger virtual ID ranges cannot pass R1. A hand-wired chain cannot pass R1.

## R2 — make learned state genuinely pageable

**Question:** can loading, updating, and evicting state operate without retaining the entire store in RAM?

**Work:**
- Introduce the smallest storage boundary shared by two concrete backends: a fully resident reference and a disk-backed implementation. Use the same numerical record representation initially; compression is not an experimental variable yet.
- Support lookup by virtual ID without loading every recipe or every index entry. Prefer deterministic ID-to-page partitioning with bounded pages; do not rely on mutable VQ/LSH placement alone to locate a synaptic target.
- Bound the record cache and dirty writeback queue by bytes, not just record count. Separate persistent state from resident execution slots. Include encoder and metadata storage in the design.
- Keep any resident-only legacy path explicit; the new paged mode must not call the all-partitions Resume path.
- Define versioned snapshots and crash-safe publication: write completed page generations, then atomically publish the manifest. Never treat partial writes as baseline recipes. Old stores remain untouched.
- Prove round-trip fidelity, bounded cache behavior, dirty eviction/reload, missing-versus-unseen handling, and interrupted publication with targeted tests. Checkpoint/resume learning must match uninterrupted learning under the registered exact settings.

**Gate:** a store larger than its cache is read and updated correctly under repeated eviction; no full-store materialization exists on the paged path; all buffers/indexes have documented bounds; restart reconstructs learned state exactly. This is storage correctness, not yet large-network recall.

**Steering:** reuse numeric record formats where practical. Do not rewrite all storage code, add a database framework by default, or assume SoA implies CUDA readiness.

## R3 — move the activation frontier across pages without changing the answer

**Question:** does a cue follow learned connections into currently unloaded state?

**Work:**
- Replace the nonresident-target skip with stable-ID scheduling and actual loading. Define step boundaries explicitly; the existing loop can append active nodes while iterating, so do not assume its old “depth” means synchronous hops.
- Make the same step semantics run in both backends. Capture source contributions before eviction; pin only the necessary batch. Aggregate target contributions and perform global step selection consistently, using bounded spill storage if required.
- Keep algorithmic activation width separate from resident cache size. In exact mode, smaller memory means more scheduling/I/O, not fewer eligible targets. Approximation is a later named mode.
- Run fixtures with memory too small to hold an entire path simultaneously, then run the R1 learned snapshot in both modes. Include cold application-cache start, reload after eviction, changed query order, cycles, fan-in from multiple pages, and ties at the selection boundary.
- Report requested/loaded/evicted IDs, bytes read/written, cache hits, frontier size, and explicit truncations. Capture one understandable learned multi-load query trace as evidence.

**Gate:** all fixture outputs agree; all registered learned-query rankings agree between resident and paged exact modes; scores agree within abs 1e-6 + rel 1e-5 with tie policy fixed in R0. Persistent-state checksums remain unchanged during recall. Nonresident targets are demonstrably loaded and used. No hidden drops or unexplained order effects.

**Failure interpretation:** divergence is an execution/storage defect, not evidence that associations disappear at scale.

## R4 — measure useful capacity under a fixed memory budget

**Question:** what memory/latency tradeoff does paging buy for actual learned content?

**Work:**
- Freeze the R1-passing learner and R3 semantics. Use three increasing learned workloads (1x, 4x, 16x episodes/independent relationships), not merely increasing BaselineNeuronCount. Report learned nodes, edges, bytes, and task coverage.
- Choose an initial memory cap M from R0 hardware information before measurement (target <= 25% physical RAM, with safe headroom). Measure an empty-process/runtime baseline B separately. Account for all application-owned memory; require peak RSS <= B + 1.25M on paged runs. Do not redefine M or B after seeing failures.
- Ensure the largest complete resident representation requires at least 4M by actual accounting, including indexes, not extrapolation from virtual IDs. Compare resident and paged execution of that same snapshot where it safely fits physical RAM; otherwise use smaller paired cells for equivalence and mark large-cell equivalence unverified.
- Run five seeds, 100 fixed measured queries/seed, at most the three workload sizes and two cache budgets (M and M/2). Report p50/p95 latency, throughput, peak RSS, managed heap, store bytes, disk traffic, cache state, and exact recall metrics. Separate startup/indexing from query time.
- Show added independently learned relations remain retrievable as content grows. Compare a small-capacity model on the full fixed query set with the expanded model; include unavailable answers as failures. Do not present reduced hash collisions as proof of richer reasoning.
- Train the largest workload through the bounded storage path too. If only inference passes, label the result inference-only. Synthetic construction of a giant store is a stress fixture, not scalable learning.

**Gate:** largest paged workload meets the memory bound and R1 quality gate; exact paging retains R3 equivalence on paired cells; learned capacity actually grows. Practical utility target: p95 <= 2 seconds/query and <= 10x resident p95 on paired cells, excluding separately reported startup. These are prototype targets, not universal performance claims.

**Verdict distinctions:** memory/quality pass but latency fail = functional paging, impractical at this workload; bounded inference but unbounded training = inference-only; more available IDs alone = no capacity result. Never infer a network larger than physical RAM unless that condition was actually tested.

## R5 — deliver the local-data learning and recall utility

**Question:** can Bill train once, restart, and retrieve related material from his own data?

**Work:**
- Make learn accept an explicit local source and write a named model snapshot. Make probe load that snapshot without training or scanning the training corpus to compute answers. Provide top related candidate IDs/scores plus a concise traversal/resource report.
- Offer readable labels through a separately supplied display/candidate source or caller-provided candidates. Model storage remains numeric. Label this closed-candidate retrieval if candidates must be supplied; do not claim open-ended recall or generation.
- Register a deterministic held-out real-data task before scoring: at least 100 supported cues, fixed candidate selection/frequency matching, document/sentence split, duplicates policy, source checksum, and no train/test context leakage. Freeze the encoder after training.
- Use query-level MRR and Recall@10; compare with untrained and frequency-only controls and a transparent co-occurrence retrieval baseline. Keep broad natural-language reasoning out of scope.
- Acceptance: mean MRR lift >= 0.05 over both untrained and frequency-only controls, with a positive 95% paired bootstrap lower bound over queries. Report seed variation separately; queries sharing data are not independent training replicas. Report the co-occurrence baseline even if it wins.
- Run learn, shutdown, fresh-process probe, exact resident/paged comparison, and model-string audit. Include query quality and total memory with the display adapter present. Audit numerical schema and planted wordlist rejection; do not assert that a clean byte scan establishes semantic non-memorization.

**Gate:** persisted-model querying works, real association gate passes, and the R4 memory claim survives the real-data path. If synthetic succeeds but real data fails, finish with exactly that limited result. No representation redesign inside R5.

## R6 — close the campaign and decide whether GPU work is justified

Deliver one RESULTS capstone and README truth pass with four separate verdicts: learning, exact paging, resource tradeoff, and real-data utility. Publish reproducible commands/configurations and known limitations. No “letter met” substitution for a failed original requirement.

Profile the successful end-to-end path once. Report CPU compute, storage wait, serialization, scheduling, and allocation costs. CUDA becomes a proposed next directive only if useful recall works and substantial parallel compute remains after accounting for I/O. Do not port in this campaign. If a simple baseline gives comparable quality more cheaply, say so: any claimed advantage must be identified rather than implied.

The campaign ends at R6 or at its first exhausted prerequisite gate. No R7, new lambda grid, or automatic extra diagnostic phase. Emergent behavior remains untested unless an explicitly defined held-out compositional task supports that narrower claim.

## Effort bounds and failure handling

- Per phase: one initial implementation and one corrective attempt for a measured defect or failed gate. A corrective attempt must name the cause it tests and expected observable change. No multi-parameter search.
- Unit-test fixes before a scientific run are ordinary implementation work, but do not use that label to hide an architecture rewrite. If a phase needs more than two substantive redesigns, stop and explain the design decision required.
- One development smoke configuration before any registered full run. Estimate runtime/disk usage from it. No unattended experiment expected to exceed one hour without reporting the estimate and receiving an explicit larger run budget. Do useful independent work while a permitted run proceeds.
- An instrument bug invalidates affected results; it is not a failed model attempt. Permit one bounded repair and rerun with the unchanged protocol. A second material instrument defect ends the phase as inconclusive instead of launching instrument archaeology.
- Environmental interruptions do not consume scientific attempts. Check process status and artifacts before restarting. Record exit code and completion evidence; notifications alone are not results.
- If a gate fails, say what failed, what remains established, and what single decision would unblock progress. Do not move a threshold or invent approval requirements from historical prose.

## Handoff protocol — especially for a smaller or fresh agent

At every phase boundary, before a long run, and before a context/usage handoff, append a compact recovery checkpoint to RESULTS. Keep the following active pointer current in this guide:

- **Current phase:** R4 stopped at measured paired latency target; exact deferred correction passed.
- **Passed gates:** R0, A1, R2 storage, R3 exact paged traversal; original R1 remains failed. Evidence: artifacts/recovery/a1, r2 and r3.
- **Next action:** Review R4 result and decide on a new directive. No automatic extra correction, larger grid or R5.
- **Open decision:** none for deferred-decay implementation (approved); runtime budget remains a gate for projected long jobs.
- **Running jobs/artifacts:** None. artifacts/recovery/r4-deferred/summary.json; scratch model /private/tmp/gm-r4-deferred-201-1x/model. All workers completed; do not restart them.

Each checkpoint must contain: exact current phase and gate status; changed files; completed test commands and outcomes; full effective configuration and dataset checksum; raw artifact paths; process/session IDs and completion state; attempts used; unresolved defects; and one concrete next command/action. Never copy a prior agent's PASS without an artifact reference.

If you cannot explain why the next change advances the north-star demonstration, stop editing and reread the current phase. If an unexpected result occurs, first inspect the execution trace and metric definition; do not invent a biological explanation. If uncertain about a design choice affecting learning semantics, disk compatibility, or numerical equivalence, prepare the smallest concrete alternatives for Bill instead of silently choosing a new architecture.

**Forbidden shortcuts:** inflating virtual neuron count; relabelling familiarity as recall; scoring only nonzero pairs; loading the whole model behind a small pool; silently dropping unloaded targets; reconstructing answers from the source corpus; changing the resident reference to hide paging errors; claiming fixtures learned; claiming CUDA speedups without measurement; promoting an inconclusive result to success because time or tokens ran short.

---

# Historical plan — closed P0–P10 campaign

The material below is retained for provenance and component context. Its execution instructions and claims of settled proof do not override the active recovery guide above. Do not resume these phases.

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
