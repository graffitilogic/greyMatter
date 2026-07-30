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

### P1.6j — Neuron training was concept-blind (2026-07-30 10:35)

**Prediction failed:** I expected the substring fix to move Hebbian `passed%`
from 22% toward 90%. It did not move *at all* — still 22.2%, mean delta 4.1,
identical to two runs prior. (The substring fix was still correct; it just
wasn't the cause. Reuse and throughput held: 96%, 48.7 sent/sec, 14,655
sentences.)

**What the failure revealed.** `passed` is *always a multiple of 16*: 16 of 78,
32 of 146, 208 of 947. Sixteen is `FirstAllocationNeurons`. Neurons therefore
respond in whole allocation-cohorts — an entire cohort fires or none of it does.

Cause, verified in `ProductionTrainingService`:
```csharp
var features = ExtractFeatures(sentence);          // 10 features: length, word
                                                   // count, 3 bools, first 5 chars
foreach (var word in words)
    await _cerebro.LearnConceptAsync(word, features);   // SAME dict every word
```
The training features are a **sentence fingerprint**, and every word in a
sentence receives an identical copy. So "cat" and "the" were trained on the same
input pattern; a neuron's receptive field encoded *which sentence it saw*, never
*which concept it belongs to*. Concept identity influenced only which cluster
and neurons were selected — never what they learned.

Consequences, all of which we had been measuring without understanding:
- A concept accrues one 16-neuron cohort per distinct sentence-context; only the
  cohort matching the current sentence fires → the immovable ~22%.
- Neuron count grows with *contexts*, not vocabulary — why it never plateaus.
- "Recognition" was closer to sentence-matching than concept recall.
- **P2 would have been meaningless**: regeneration fidelity measured against
  sentence fingerprints, not concept representations.

**Fix:** `Cerebro.BuildTrainingFeatures` composes the receptive field from the
concept's own encoding (top-32 magnitude dims of `FeatureEncoder.Encode(word)`,
deterministic and stable across sentences) plus the sentence context retained at
0.25 weight as modulation. Cluster selection already used the concept encoding;
now training does too.

**Predictions (recorded before running, given the last one missed):** `passed%`
should rise well above 22% and stop being a multiple of 16; neuron growth should
slow because repeat encounters reinforce an existing assembly instead of
recruiting a context cohort. Less certain: `reuse%` and cluster count. If
`passed%` stays pinned at 22.2% a third time, the cohort behaviour is coming
from somewhere other than the feature path and I should instrument
`TrainNeuronWithFeatures` directly rather than theorise again.

### P1.6k — Cohort lockstep broken; signed-input regression (2026-07-30 11:02)

**Prediction was half right.** I said `passed%` would rise above 22% and stop
being a multiple of 16. The *second* half happened, the first did not:

- **Lockstep broke** — `above_threshold` now takes values 0, 1, 4, 5, 7, 8, 9,
  10, 13, 15, 16. Every prior run produced only multiples of 16. Neurons no
  longer respond in whole allocation-cohorts, which was the actual claim under
  test, and it confirms the concept-blind training diagnosis.
- **But `passed%` fell to 18.5%** (from 22.2%), and mean delta fell 4.1 → 2.04.

**Cause of the regression — my own change.** `TrainNeuronWithFeatures`
initializes weights **positive** (1.5–4.5), an assumption inherited from the old
sentence features, which were all non-negative (char/128, booleans, lengths).
`FeatureEncoder` emits a **unit-norm vector with signed components**, so
Σ(signed value × positive weight) cancels. The log shows it directly:
`delta[min=-6.84 ...]` — the first negative minimum in any run; previously 0.00.
Also new: `none_passed=257–318` per window (~13% of events now produce no
co-activation at all).

**Fix (P1.6k):** rectify concept features into ON/OFF channels — each top-K dim
emits `cf_{d}_p` or `cf_{d}_n` carrying |v|. All inputs non-negative, sign
information preserved, sparsity unchanged (one channel per dim). This is also
the biologically standard arrangement (rectified ON/OFF pathways).

**Genuine gains this run, worth keeping:** neurons 280,583 → **169,089** (−40%)
and synapses 464,184 → **220,258** (−53%) — concept-based receptive fields mean
repeat encounters reinforce rather than recruit. `reuse%` 96.7–97.4%.

**Costs to watch:** throughput 48.7 → 29.7 sent/sec and the train step
0.5 → 2.4 ms (features went from 10 keys to ~42); compression 2.38x → 1.67x
(more InputWeights per neuron). If ON/OFF restores activation, consider
lowering `ConceptFeatureDims` from 32 to trade discriminability for speed.

### P1.6l — ON/OFF fixed the regression, lockstep returned (2026-07-30 11:16)

Rectification did exactly what it was supposed to, and nothing more:
`delta_min` 0.00 (was −6.84), `none_passed` 0 (was 257–318), mean delta
2.04 → 3.8. My P1.6k regression is gone.

**But `passed%` is back to 22.1–22.4% and `above_threshold` is 16 again** —
the multiple-of-16 lockstep returned. Taken together with P1.6j:

| variant | inputs | delta | passed% | lockstep |
|---|---|---|---|---|
| sentence fingerprint | non-neg | 4.1 | 22.2% | yes (16) |
| concept, signed | signed | 2.04 | 18.5% | **broken** (0–16 spread) |
| concept, rectified | non-neg | 3.8 | 22.2% | yes (16) |

The spread in the signed variant was **cancellation noise**, not discrimination —
random weight signs made sums land either side of threshold. Remove the noise and
the underlying behaviour reappears unchanged. So concept-vs-sentence features were
never what pinned the rate: something makes exactly ~16 neurons per concept
eligible to fire regardless of what is fed in.

Ruled out by code inspection this round: cross-concept pollution via
`AssociatedConcepts` — `NeuronCluster.AddNeuronAsync` associates neurons with
`ConceptDomain`, which is `pattern_vq_N`, never a word.

**No fourth theory.** Added `LogReceptiveFieldOverlap` (sampled 1-in-4000 learn
events, concepts with >20 neurons), reporting per concept: how many of its
neurons have **zero** input-weight overlap with the current input lines, how many
partial, how many full, plus median/max delta and firing count. That separates
the two candidate explanations directly:
- `none=62 full=16` → wiring gap: most neurons never received weights for these
  inputs (`TrainNeuronWithFeatures` only initialises when `InputWeights` is
  empty, so a neuron keeps whatever key set it was born with).
- `full=78, firing=16` → excitability: the weights exist and the sums are simply
  too low, pointing at threshold/normalisation rather than wiring.

**Retained from the concept-feature work** (worth keeping regardless): neurons
189,033 vs 280,583 pre-P1.6j, reuse 95–97%.
**Still costing:** 29.2 sent/sec (vs 48.7), compression 1.68x (vs 2.38x) —
both from the larger feature set; revisit once the 16 is explained.

### P1.6m — FOUND IT: the 22% was a wiring bug (2026-07-30 11:29)

The receptive-field diagnostic answered on the first run, identically in every
single sample:

```
RF[same]:       neurons=78 inputs=42 coverage[none=62 partial=0 full=16] firing=16
RF[very]:       neurons=78 inputs=42 coverage[none=62 partial=0 full=16] firing=16
RF[within]:     neurons=71 inputs=42 coverage[none=55 partial=0 full=16] firing=16
RF[married]:    neurons=69 inputs=42 coverage[none=53 partial=0 full=16] firing=16
RF[everything]: neurons=76 inputs=42 coverage[none=60 partial=0 full=16] firing=16
RF[never]:      neurons=79 inputs=42 coverage[none=63 partial=0 full=16] firing=16
RF[sleep]:      neurons=78 inputs=42 coverage[none=62 partial=0 full=16] firing=16
RF[in]:         neurons=75 inputs=42 coverage[none=59 partial=0 full=16] firing=16
```

**`partial=0` everywhere, `full=16` exactly, regardless of concept or neuron
count (69–79).** Coverage is binary and capped at one allocation cohort. Median
delta is 0.00 — the median neuron of every concept is incapable of firing.

**Root cause.** `HybridNeuron.InputWeights` is a single dictionary holding two
different kinds of key:
1. **feature-input IDs** (from `FeatureMapper`) — the receptive field;
2. **other neurons' IDs** — synapses, written by `HybridNeuron.ConnectTo` and
   restored by `ProceduralNeuronRegenerator`.

`TrainNeuronWithFeatures` gated initialisation on
`if (!neuron.InputWeights.Any())`. `NeuronCluster.GrowForConcept` connects every
new neuron to 3 random peers (`ConnectTo`, line 530), so the dictionary is
almost always non-empty by the time training runs — and the neuron then
**never receives feature weights at all, ever**. Only neurons that happened to
be trained before acquiring a synapse got a receptive field: one cohort, 16.

This is why the pass rate was immovable at ~22% across every intervention —
clustering granularity, concept-vs-sentence features, signed vs rectified inputs.
None of them could matter: 80% of each concept's neurons had no input wiring to
evaluate. It also explains the ballooning neuron counts (dead neurons never fire,
so the system keeps recruiting) and, retroactively, the original
`FirstAllocationNeurons`-multiple lockstep.

**Fix:** initialise any *missing* feature weight per neuron rather than gating on
the whole dictionary being empty. Three lines.

**Lesson recorded:** four consecutive wrong theories (coarse clusters, substring
matching, concept-blind features, signed inputs) were all reasoned from aggregate
metrics. One targeted measurement of the actual data structure settled it
immediately. Instrument the mechanism, don't infer it from summary statistics.

### P1.6n — Wiring fix confirmed; new problem is the opposite one (2026-07-30 12:31)

```
RF[but]:    neurons=67  coverage[none=0 partial=0 full=67 100%]  delta[med=17.76]  firing=67
RF[the]:    neurons=78  coverage[none=0 partial=0 full=78 100%]  delta[med=17.35]  firing=78
RF[we]:     neurons=77  coverage[none=0 partial=0 full=77 100%]  delta[med=19.71]  firing=77
RF[to]:     neurons=80  coverage[none=0 partial=0 full=80 100%]  delta[med=18.26]  firing=80
```
`none=0` everywhere, median delta 0.00 → **15–19**, and **`passed%` 22% → 100.0%**
(`delta[min=12.43 avg=17.31 max=24.86]`). The ceiling that survived five
interventions is gone. Wiring bug confirmed and fixed.

**But 100% is not success — it is the opposite failure.** Every neuron in an
assembly now fires on every presentation, with near-identical activation
(`RF[but]`: median 17.76, max 19.54 across 67 neurons). They share one input set
and differ only by random weight jitter, so a 78-neuron assembly is functionally
**one neuron replicated 78 times**. There is no selectivity *within* an assembly
and therefore no distributed code — which would make a P2 fidelity result
meaningless in the other direction: regenerating 78 identical clones is trivial
and proves nothing.

Second cost, directly caused: every neuron now carries all 42 feature weights.
Procedural save **Full 29.1MB → 79.9MB**, compression **1.68x → 1.34x**. We are
now storing 78 copies of the same receptive field per concept.

Unchanged: throughput 29.1 sent/sec, reuse 96–98%, neurons still growing
(172,635), synapses bounded 370–450K under decay (60–80K pruned per pass).

**The real question this exposes (design call, P1.7):** what should distinguish
two neurons in the same assembly? Candidate: give each neuron a deterministic
*sparse subset* of the concept's input dims, derived from (VQ code, neuron
index). Different neurons then respond to different feature combinations —
graded activation, genuine sparsity, and each receptive field becomes
**procedurally regenerable rather than stored**, which is the thesis in its
purest form and would cut storage rather than inflate it.

### P1.7 — Sparse procedural receptive fields (implemented 2026-07-30)

Chosen over k-WTA because it makes the receptive field itself procedural rather
than stored — the thesis applied one level deeper.

Each neuron listens to a deterministic **sparse subset** (`ReceptiveFieldDensity
= 0.2`, ≈8 of 42 inputs) selected by `NeuronSamplesFeature(neuronId, featureKey)`
— FNV-1a over the GUID and key with a murmur3 avalanche. Properties:
- **Deterministic**: same neuron, same subset, forever.
- **Regenerable from identity alone**: the *shape* of a receptive field never
  needs persisting; only learned weight values do. Direct P3 consequence —
  `ProceduralNeuronData` can drop the key set entirely and store deviations.
- **Collectively complete**: ~78 neurons × 20% still covers every input ~15×,
  so the assembly sees everything while no two neurons see the same thing.
- Initial weights scaled by `1/density`, so *expected* activation stays ~17 above
  resting and thresholds / the tanh(delta/20) gate / decay stay calibrated. The
  only thing that changed is which inputs a neuron can see.

Diagnostic extended with a p10 delta and `firing=n/N` to show the spread.

**Expected signature:** `coverage[none=0 partial=N full=0 avg≈20%]`, delta with a
real p10→max spread instead of everything within 2 of the median, `firing < N`,
and `passed%` landing somewhere between the two failure modes (22% / 100%).
Storage should fall back below 1.34x, since each neuron stores ~8 weights
instead of 42.

**If instead** coverage is ~20% but `firing` is still N/N and deltas stay tight,
sparsity isn't producing selectivity and the next lever is competition (k-WTA)
rather than wiring.

### P1.7 result — sparsity worked; the pass-rate metric is now the broken thing (2026-07-30 12:52)

Scored against predictions made in advance: **2 of 4 hit, and both misses are
the same miss.**

✅ `coverage[none=0 partial=N full=0 avg=18–20%]` — exactly as designed.
✅ **Graded activation achieved.** `delta[p10≈8–11 med≈13–17 max≈27–36]` — a 3–4×
   spread across an assembly, where before the median sat within 2 of the max.
   Neurons in an assembly are now genuinely different from one another.
✅ **Storage improved beyond any previous run**: procedural save Full
   79.9MB → **29.2MB**, compression 1.34x → **1.80x** (previous best 1.68x).
   Each neuron stores ~8 weights instead of 42.
❌ `firing < N` — still N/N in 6 of 7 samples.
❌ `passed%` between the failure modes — still 99.9%.

**Why the misses, and why they matter less than they look.** The Hebbian gate is
`tanh(delta/20) > 0.1`, i.e. delta > ~2.0. The **p10** of the new distribution is
8–11. Even the least-active decile clears the gate by 4–5×. A fixed absolute
threshold is not a selectivity mechanism against this distribution — it admits
everything regardless of how well differentiated the population is.

**But selection is already happening downstream.** `RecordCoactivationPattern`
trims to `MaxCoactivationGroup = 16` by activation before wiring. So synapse
formation has always been winner-take-all — the difference is that it now picks
**the strongest 16 of a differentiated population** instead of 16 arbitrary
clones. That is precisely what P1.7 set out to achieve, and `passed%` cannot see
it because it measures the pre-trim gate.

**Conclusion: `passed%` has outlived its usefulness.** It was the right
instrument for finding the wiring bug and is now saturated and uninformative.
The question worth measuring is *selectivity*: does the active set differ
between concepts, and is it stable for the same concept across contexts? That is
a mini version of the P2 fidelity measurement, so it should be built there
rather than as more activation tuning.

**Cost noted:** throughput 29 → 25.4 sent/sec, train step 2.6 → 3.1 ms
(~3,300 hash evaluations per learn event: 78 neurons × 42 features). Cacheable
in memory without giving up regenerability — recompute on load. Deferred.

**Still open:** neurons continue to grow (165,411, `grew_events` ~6%, reuse ~96%)
with no plateau on a 500-sentence cycling corpus.

---

## P1 COMPLETE — moving to P2

The structural prerequisites for the fidelity experiment are now in place:
- assemblies are stable and reused (~96–98%)
- receptive fields are sparse, deterministic, and **regenerable from neuron
  identity** rather than stored
- activation within an assembly is graded, so co-activation selects meaningfully
- persistence works end-to-end (procedural save, 1.80x, ~66K neurons/checkpoint)
- synapse count is bounded by decay; storage and throughput are stable

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

### P2 — The fidelity experiment ✅ HARNESS BUILT (2026-07-30)

`dotnet run -- --fidelity-test [--brain-path P] [--topk 16]`

1. Probe a fixed cue set, record top-k active neurons per cue → **A**
2. `SaveAsync()` then `EvictAllClustersAsync()` — persist and unload everything
3. Re-probe; the next activation must rebuild from disk → **B**
4. `fidelity = |A ∩ B| / |A|` per cue, averaged

Supporting pieces added to `Cerebro`:
- `ProbeConceptAsync(concept, topK)` — activation with **no side effects**: no
  growth, no training, no Hebbian recording, no capacity adjustment. Without
  this the act of measuring would change what is measured.
- `EvictAllClustersAsync()` — persist + unload every resident cluster, so the
  re-probe is forced down the procedural regeneration path.

**Selectivity is reported alongside fidelity, not after it.** Cross-concept
overlap of the active sets is computed first, because a fidelity number is
meaningless without it: if every cue activates the same neurons, fidelity is
trivially 100% and measures nothing. The harness says so explicitly when
overlap ≥25%.

The cue set includes two controls (`qwertyuiop`, `zxcvbnmasd`) that never appear
in the corpus. They should activate nothing; if they light up, whatever is being
measured is not concept-specific recall.

- **Exit criterion:** a number, with its selectivity caveat attached.
  ≥95% fidelity *and* <25% cross-concept overlap = thesis supported at this
  scale. Lower fidelity = we learn precisely what regeneration loses.

### P2 run #1 — 100% fidelity, and it is **INVALID** (2026-07-30)

Reported `REGENERATION FIDELITY: 100.0%`, `CROSS-CONCEPT OVERLAP: 0.0%`,
"thesis supported". It is not. The controls caught it, which is the only reason
this wasn't written up as a success.

**`qwertyuiop` and `zxcvbnmasd` — strings never in the corpus — activated 10
neurons each at 0.54 / 0.63, indistinguishable from "the" (10 @ 0.61) and
"water" (16 @ 0.62).** Three bugs behind it:

1. **`CalculateNeuronActivation` ignored its `featureVector` parameter entirely.**
   It returned `0.3 + importance*0.5` — a property of the neuron, not of the cue.
   Every neuron in a cluster returned the same value for any input. The comment
   said "Phase 2 will enhance this with actual feature similarity"; it never was.
   Recall through this path has **never** been pattern-based, which retroactively
   invalidates the novelty-detection claims in `docs/SYNAPTIC_NOVELTY_DETECTION.md`
   ("neural networks cascades, qawsedrftg activates nothing").
   *Fixed:* the probe now builds the concept's input lines and runs the real
   neuron model (`ProcessInputs`), reporting activation on the same
   tanh(delta/20) scale as the Hebbian gate. Context features are omitted so a
   probe carries concept identity only.

2. **`ProceduralNeuronRegenerator.RegenerateNeuron` dropped the VQ code.** The
   temp snapshot it rebuilds from omitted `VqCode`, so a neuron that had been
   procedurally saved once came back with none and was skipped on the next save
   — `170/170 neurons "has no VQ code"`, `Procedural save: 0 neurons`.
   Procedural persistence was a **one-way trip**. *Fixed:* carry `VqCode`.

3. **Consequently the eviction persisted nothing** (`examined=17, changed=0,
   packsWritten=0`), so the re-probe re-read the same files A had read. The
   experiment measured whether reading a file twice is deterministic. It is.

Also: 0.0% cross-concept overlap across all 91 pairs was not evidence of good
selectivity — with an importance-only activation, each cue simply resolved to a
different cluster via `_regionToClusterMapping` and never touched another's
neurons. Perfect scores on both axes should have read as alarming, not good.

**Harness hardened:** controls are now checked explicitly and the run
**aborts before printing a verdict** if they activate. A fidelity number is no
longer reportable when the probe can't tell language from noise.

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
