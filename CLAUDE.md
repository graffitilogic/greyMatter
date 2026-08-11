# CLAUDE.md — Operating Plan for greyMatter

**Read this first, every session. It is binding.** This project was nearly lost twice
to scope creep and to test harnesses that measured nothing (see REFOCUS.md "How We
Got Off Track"). The rules below are not style preferences — each one was earned by
a specific documented failure. Deviating from them recreates that failure.

## What this project is

One falsifiable question: *can a cortical region that is evicted and procedurally
regenerated (VQ code + budgeted synapses) match a fully-persisted region on recall
fidelity, at a fraction of the storage?* Nothing else matters until the current
work-queue item is done. No new features, no new subsystems, no "while I'm here"
improvements.

## Session protocol (do this in order)

1. Read the **last 3 dated sections** of `REFOCUS.md` (bottom of file) and the
   **work queue** below. That is the entire live state; everything above it in
   REFOCUS.md is history.
2. Take the top unblocked queue item. If you believe a different item should come
   first, **ask Bill — do not just reorder.**
3. Before any run: append a dated section to REFOCUS.md stating **predictions**
   (what metrics will move, which direction, roughly how much) and the exact
   command you will run.
4. Run. Score every prediction ✅/❌ explicitly. Wrong predictions get recorded
   plainly — they have repeatedly been the most informative results in this project.
5. Append results to REFOCUS.md (dated section, measured numbers, command).
   Update the work-queue checkboxes here.
6. Stop and ask Bill before: adding a new mechanism, deleting code, changing any
   metric or report format, touching `/Volumes/jarvis/brainData`, or writing up
   any result that looks *too good* (see Rule 4).

## Ground rules (each cites the failure that created it)

1. **One status doc: REFOCUS.md.** Append dated sections there. Never create
   SUMMARY / COMPLETE / MILESTONE / STATUS / PROGRESS docs, and never create new
   standalone test scripts or harness files.
   *Sanctioned exception, 2026-08-10, Bill's call:* `RESULTS.md` — a **terminal**
   write-up of the W3′ and P5 negative results. It is not live state and is not
   updated per session; REFOCUS.md remains the single status doc. Adding a second
   such file needs Bill. Extend the existing harnesses in
   `Program.cs` (`--fidelity-test`, `--cascade-test`, `--cascade-stats`,
   `--test-hebbian`) and versioned scripts in `greyMatter/scripts/`. The stale
   `*.sh` files at the greyMatter/ root are legacy — do not add to them, do not
   run them, do not "fix" them.
2. **No claim without a command.** Any "✅ works" statement must cite a
   reproducible command and its measured output. "Should work" and "code review
   confirms" are not claims — P1's learning loop "worked" by code review for six
   months while a wiring gap meant procedural save had never once run.
3. **Instrument the mechanism, not the aggregate.** Four consecutive wrong
   theories (P1.6h–P1.6l) were all reasoned from summary metrics; one targeted
   dump of the actual data structure (P1.6m) settled it immediately. When a fix
   targets a decision, add a counter on the **decision** (like
   `gate[resident/member/skip/nometa]`), not on its downstream aggregate.
4. **Perfect scores are alarming, not good.** Every 100% this project has ever
   produced was a broken measurement: 100% fidelity meant the probe ignored its
   input (P2 #1), then that A and B read the same files (P2.3), then that recall
   never touched the regenerated part (P2.4). 0.0% cross-concept overlap was a
   lookup artifact. If a result supports the thesis cleanly on the first run,
   your first hypothesis must be that the experiment cannot fail.
5. **Check the null is reachable.** Pre-registering a verdict is not enough —
   P5's "backward bias" verdict was structurally impossible because nothing could
   ever create a backward edge. Before running, state what achievable system
   state would produce the null result. If none exists, the experiment is theatre.
6. **No verdict from n=1 on any correlation-valued metric.** P5.3's +0.25 "learned
   order" signal was one draw from a distribution spanning a full unit (P5.4).
   Use `--repeats` (≥5), report mean and range, and claim a difference only when
   ranges don't overlap. Trust AUC/d′ over min/max margins — the strict margin is
   the noisy statistic (P2.3, P3).
7. **A harness must be inert with respect to its subject.** Experiments default
   to an isolated scratch brain, deleted on exit. `--brain-path` at a real brain
   requires Bill's explicit go-ahead: every pre-P2.5 fidelity run permanently
   contaminated `/Volumes/jarvis/brainData` with its own training data.
8. **Controls gate validity.** If controls (keyboard mash *and* pseudo-words like
   `blorp`, `thrumble`) activate inside the trained range, the harness aborts and
   **no fidelity number is reportable**. Never weaken a control to make a run
   pass; the pseudo-word tier exists because mash-only controls proved too easy.
9. **Machine-readable output is an interface.** Renaming a report line silently
   broke the sweep pipeline for two runs (P3.2). Metrics scripts read stable tags
   (`PROCEDURAL:`, `BUDGET:`, `LOSS:`, `gate[...]`, `synapses[...]`). Add tags;
   never rename or reshape existing ones.
10. **All synaptic-graph mutation stays on the training path.** A maintenance-loop
    decay raced the training thread over the non-thread-safe dictionary (P1.6b).
    Decay runs inline every 5,000 learn events. Checkpoint path only reads.
11. **Benchmarks pin the corpus.** `tatoeba_small` + `--no-curriculum` +
    `--corpus-limit N`. Note `--no-curriculum` alone does NOT limit corpus size —
    it once silently loaded 50K sentences and invalidated an exit criterion
    (P1.6e). Reuse/saturation is only measurable on a cycling corpus. The 571GB
    Wikipedia pipeline, LLM teacher, and curriculum stay parked.
12. **Repo/data hygiene.** Repo lives in Dropbox — make it available offline
    before git surgery. Never `rm -rf` brainData while training; rename aside.
    Cluster IDs are `Guid.NewGuid()`, so iteration order varies per run — some
    run-to-run variance is structural; don't chase it as a regression.

## Corrections to conclusions you might wrongly infer

A model skimming this codebase or its docs will infer several things that are
**false**. The honest state, as of 2026-07-30:

- **The banked receptive-field result is RETRACTED (2026-08-10, W3′).** It read:
  *"fields regenerate fully from `(VqCode, identity)`, 95.5% fidelity at 64 B/neuron,
  AUC ≈ 0.99, real and banked."* Five sweeps (40 runs) show **only 6 are
  reportable under rule 8**, scattered randomly across budgets, and **AUC 1.000 is
  definitionally identical to passing the gate** — so `AUC 0.990` means the gate
  failed. The headline was a failing run. What survives: regeneration reproduces
  the assembly faithfully (95.5–97.2% when reportable), but the regenerated field
  is a prototype and carries no lexical identity, so it cannot separate a trained
  word from a novel one sharing its VQ code. d′ ≈ 1.85 regardless of budget.
  See REFOCUS "W3′ RESULT".
- **The synaptic graph does NOT learn word order.** P5.4 verdict: NO SIGNAL.
  P5.3's positive was noise. Do not describe the causal/STDP machinery (P4.2) as
  working — it exists, its effect is unproven, and the P5.5 saturation finding
  (`MaxOutDegree` blocked 18.4M writes; more data → *fewer* reachable successors)
  means it cannot currently be proven. That's what W1 is for.
- **Recall is mostly nearest-prototype lookup through the VQ code**, not learned
  memory. Learned weight drift contributes almost nothing to recall (P3.3: storing
  zero weights performed as well as storing all of them). Silent controls are
  often silent because their VQ code maps to no cluster — a region gate, not fine
  discrimination (P2.6).
- **Any old "90% compression" or "trillion-parameter" claim is retracted.**
  Measured compression: 2.7–5.1×, procedural, honest. `TECHNICAL_DETAILS.md` was
  corrected once already; treat any grand claim in older docs/comments as false.
- **`passed%` is a dead metric** (saturated at ~100% since P1.7). Selectivity
  questions go through the fidelity/discrimination harness, not the Hebbian gate.
- **100% reuse can be an artifact** — of a collapsed VQ codebook (P1.6h: 2.5%
  utilization put everything in ~60 buckets) or of a corpus the brain already
  knows (P4.5 caveat). Check `SeededCount`/utilization at freeze and whether the
  vocabulary is genuinely novel before crediting reuse.
- **`HybridNeuron.InputWeights` mixes two kinds of key** — feature lines and
  synapses to other neurons. This single fact caused the worst bug arc in the
  project (P1.6m) and the broken cosine denominator (P2.2). Use
  `FeatureMapper.GetFeatureNeuronIds()` to separate them; never test
  `InputWeights.Any()` as "has this neuron been trained".
- **`MatchQuality` is the activation measure.** `ProcessInputs`/`CurrentPotential`
  is legacy; code that reads `CurrentPotential` reads a value nothing sets
  anymore (P2.1 regression, then the RF diagnostic went silently useless the same
  way). If you add a diagnostic, wire it to `MatchQuality`.
- **Concept identity fields:** `PrimaryConcept` is the allocation concept, set
  once; `ConceptTag` is a single token; `AssociatedConcepts` is a
  case-insensitive set. Old comma-joined tags exist on disk and are split on
  load — do not "migrate" or "clean" them.
- **Legacy traps:** `BrainStats.TotalSynapses` reads a dead list — use
  `GetSynapticGraphSynapseCount()`. The `syn` timing in the Perf line is legacy
  `CreateConceptualConnections`, not the Hebbian step. `Clusters: 0` /
  `Storage size: 103 B` in progress stats are known-bogus (read before first
  save). The warmup-freeze path of the VQ codebook has never been tested (only
  freeze-on-load has).
- **Throughput on a resumed NAS brain is ~1 sent/sec.** That is network I/O, not
  a regression (P4.2). Scratch-brain training runs ~40–50 sent/sec.

## Work queue (top item first; check off with date + REFOCUS section ref)

- [x] **W1 — P6: synaptic competition replaces budget blocking.**
  *Done 2026-08-10 (`ed2d732`, `2f7fd50`), REFOCUS "W1 / P6 VERDICT".*
  **Central prediction FALSIFIED.** Competition works exactly as designed
  (`displaced=5,825,016 declined=11,204,550`, blocked-by-budget −39%) and does
  not help: `reached` fell 24.2% → **0.6%** from 500 to 20,000 sentences, with
  the absolute count dropping 199 → 77. Every slot was overwritten ~8.6×.
  Blocking froze the graph early; competition makes it never settle. **Do not
  retry this with a tuned displacement rule** — the pre-registered falsification
  condition named the budget/co-activation rule as the real target, not the
  eviction policy. Still owed from W1: a `--fidelity-test` regression run
  (prediction 5, never executed).

- [ ] **W3 — Bank the P4.1 fidelity curve with multi-seed runs.** *Moved ahead of
  W1b 2026-08-10 (Bill's call): it is the project's only positive result and is
  still resting on one run per threshold.* **Blocked until the control gate is
  believed** — the 2026-08-10 regression run printed `REGENERATION FIDELITY:
  93.4%` while `qqzzxxjj` (0.627) beat trained `so` (0.555), i.e. rule 8 should
  have aborted it. Harness now enforces rule 8 (`0ac6890`+). Expect the sweep to
  produce **fewer reportable runs, possibly none** — that is the honest state, not
  a harness bug, and if nothing is reportable then the banked 95.5% / AUC 0.990
  headline is itself in question and W3 becomes the top priority in a stronger
  sense than planned. Run `scripts/sweep_fidelity.sh` 3× at thresholds
  {0.10, 1.00, 8.00}; report mean and range.
  **Confirmed 2026-08-10 (REFOCUS "Rule 8 gate confirmed firing"):** a run at
  threshold 1 aborts with **AUC 0.990** — the banked figure itself. AUC measures
  average ranking and is compatible with a control outscoring a trained word, so
  the headline has been quoting the metric that cannot fail. At threshold 1 only
  0.38 of 35.6 weights per neuron are individuated, so assemblies are nearly pure
  prototype and any input quantizing to the same VQ code matches them.
  **W3 is therefore W3′: find the storage budget at which rule 8 passes.** Sweep
  and record per threshold whether the run is reportable; deliverable is a
  fidelity-vs-storage curve restricted to passing thresholds, cheapest passing
  budget as the headline. Pre-register: passes only at ≈0.02–0.10, so real cost
  per neuron is well above 64 B and compression materially worse than 5.14×. If
  nothing passes, prototype-generated fields cannot discriminate lexical identity
  — and that is the project's result about procedural regeneration.
  **Sweep run 2026-08-10 — prediction FALSIFIED and result not yet usable.**
  7 of 8 thresholds abort; the only pass is threshold **2.00** (95.5%, 68 B,
  **AUC 1.000**), which stores *less* (0.19/neuron) than the failing threshold
  1.00 (0.38) — so the storage↔discrimination tradeoff does not exist. **d′ is
  flat at 1.80–2.05 across a 400× storage range**, so discrimination does not
  depend on the budget at all (P3.3 restated). Rule 4 applies to the AUC 1.000:
  treat the single pass as one lucky draw at a d′ that sits on the gate edge
  everywhere. n=1 per threshold also violates rule 6. **Next: run the full sweep
  5× and count reportable runs per threshold.** Pre-registered: scattered passes
  ~1–2 per sweep, no threshold reliable. If so, the honest result is that
  prototype fields cannot separate lexical identity from a novel input sharing
  their VQ code — a real negative result about the thesis, to be written up as
  one rather than retried.

- [ ] **W1b — make the co-activation gate selective.** The arithmetic W1
  surfaced: ~37M creation attempts (155,402 learn events × up to 240 pairs)
  against ~3.8M slots (59K neurons × `MaxOutDegree` 64) — **10× oversubscribed**.
  No eviction policy can fix that. The flood's source is
  `passed=9,309,604 (99.5%)`: `HebbianCoactivationThreshold = 0.12` against a
  mean match of 0.53 admits everything, so all real selection is done by
  `MaxCoactivationGroup`'s top-16 cut, downstream of a filter that filters
  nothing. Raise `CreationProductThreshold` and/or make the Hebbian threshold
  adaptive per-neuron (this is W6's homeostatic mechanism arriving early — fold
  W6 in rather than doing both). Pre-register before running: attempts fall ~10×,
  `reached` rises with corpus size instead of collapsing, `displaced` falls
  toward `declined`. Exit: same 500 vs 20,000 comparison as W1.

- [ ] **W2 — Re-run the order experiment properly (P5.6).** Only after W1.
  `--cascade-stats` with: pooled estimator (P5.5), both arms scored over **every**
  corpus successor with unreached = 0 (arm-comparability fix — the real arm was
  scored on 97 pairs vs the shuffled arm's 16), `--repeats 5`, at 5,000 and
  20,000 sentences, cross-word OFF. Report `reached N/M` and `bigram support`
  per arm. Verdict rules as in P5.4: real `R_PMI ≥ 0.10` required before any gap
  test; non-overlapping repeat ranges required for LEARNED ORDER. **The null
  (graph stores adjacency but not association strength) is reachable — state
  that in the write-up.** A NO SIGNAL result here, post-W1, is a real negative
  result about the architecture and should be written up as one, not retried
  with tweaked thresholds.

- [ ] **W3 — Bank the P4.1 fidelity curve with multi-seed runs.** The headline
  result (68 B/neuron, 99.5% regenerated, 95.5% fidelity, AUC 0.990) rests on
  one run per threshold with AUC noise ±0.03. Run `scripts/sweep_fidelity.sh`
  3× at thresholds {0.10, 1.00, 8.00}, report mean and range. If it holds, this
  is the project's headline artifact to date; record it as such. If it doesn't,
  that supersedes everything and becomes the top queue item.

- [ ] **W4 — Cleanup batch (only after W1–W3, only with before/after runs).**
  (a) Delete `CreateConceptualConnections` (legacy random cross-cluster wiring;
  predates the sparse graph, costs 35–105 ms/event on resume). (b) Short-circuit
  reading/writing 256 empty synapse partitions (~50 s each way on NAS).
  (c) Fix the bogus `Clusters: 0` / `Storage size` stats. (d) True
  distinct-concept count to replace the `TotalNeuronsCreated` proxy. Each change
  ships with a fidelity + cascade-stats run showing metrics did not move. Do not
  fold in extra "cleanups" — this list is closed; anything else needs Bill.

- [ ] **W5 — P4: scoped activation distance.** Cascade depth `d` as a runtime
  parameter; measure recall quality and compute cost vs `d`. Exit: a
  recall-vs-d curve showing useful recall inside a bounded scope. This
  quantifies the "only render near the player" claim and needs W1/W2's graph
  to be meaningful.

- [x] **WRITE-UP — `RESULTS.md`.** *Done 2026-08-10 (Bill's call: write-up before
  any new mechanism).* Terminal statement of the W3′ and P5 negative results,
  what survives, and the recurring fixed-threshold pattern.

- [ ] **W6 — per-neuron familiarity trace (REFRAMED 2026-08-10, Bill).**
  **Do not implement this as homeostatic threshold adjustment.** That framing has
  a reachable null that kills it: the two populations needing separation —
  weakly-trained words (0.55–0.67) and untrained words landing in a trained
  region (0.55–0.64) — occupy the same match range, so raising thresholds
  silences both, trades false positives for false negatives, and leaves d′ flat.
  It would look like "sharpening the margin" while changing nothing.
  **The discriminative signal is activation history**, which the prototype field
  cannot represent by construction: a neuron that fired 400 times for its word is
  in a different state from one matching a novel input for the first time.
  `ActivationCount` is an `int` — 4 bytes, already in `ProceduralCompactData`,
  already persisted, already round-tripped through regeneration, and **never read
  by recall** (`MatchQuality` is pure cosine over `InputWeights`).
  **Question:** can a few bytes of per-neuron history restore the lexical identity
  a prototype-generated field provably cannot carry?
  **Exit:** the `--fidelity-test` control gate passes reliably (≥4/5 sweeps at
  some budget), with d′ moving above its current flat 1.76–2.01. Pre-register
  before running, and state which achievable state produces the null — a
  familiarity term that shifts trained and novel cues equally is that null.
  If yes, the thesis survives in modified form and the storage claim survives with
  it, since the trace is already inside the 64 B floor. If no, the negative result
  in `RESULTS.md` is complete and final.

- [ ] **W7 — Storage floor: 64 B → ~20 B/neuron.** Drop the debug concept tag,
  make ClusterId implicit in the partition path, recompute importance/activation
  count. Only after W3 banks the curve, so the before/after is against a trusted
  number.

- [ ] **W8 — P4.5: assembly recipes & composition** (Bill's direction). Train
  "red" and "apple" separately, compose recipes procedurally, compare against a
  jointly-trained "red apple" baseline. Gated on W2: composition through the
  graph is uninterpretable until the graph demonstrably carries learned
  structure. Respect the binding problem; do not claim composition from mere
  co-activation overlap.

**Queue discipline:** one item at a time, in order. If a run surfaces a genuine
blocker (as P5.5 did), append it to REFOCUS.md, propose the queue change to Bill,
and wait. The failure mode this file exists to prevent is six half-finished
experiments and a pile of abandoned harnesses — depth over breadth, always.

## Standard commands

```bash
cd greyMatter && dotnet build

# Sanity: synapse formation
dotnet run -- --test-hebbian

# Pinned benchmark training (scratch metrics work goes through harnesses instead)
dotnet run -- --production-training --dataset tatoeba_small --duration 300 \
  --no-curriculum --corpus-limit 500

# Fidelity (isolated scratch brain by default — keep it that way)
dotnet run -- --fidelity-test --train 500 [--deviation-threshold T] [--topk 16]

# Order/association (after W1)
dotnet run -- --cascade-stats --train 5000 --repeats 5 --cross-word off

# Sweep (versioned)
./greyMatter/scripts/sweep_fidelity.sh

GREYMATTER_VERBOSITY=0|1|2   # log level
```
