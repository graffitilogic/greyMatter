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

### P2 run #2 — harness correctly refuses; the blocker is that learning never lands (2026-07-30)

The abort worked: `❌ CONTROLS ACTIVATE`, no verdict printed. The probe is now
genuinely pattern-based (activations spread 0.52–1.00 instead of a flat ~0.62,
so it *is* responding to the input), but `qwertyuiop` scored **0.839** —
above `water` (0.788) and `so` (0.786). Ranking is close to noise.

**Why: 95% of neurons have never had their learned weights applied.**
Checkpoint consolidation ran with
`budgetPerCluster = max(5, min(50, MaxParallelSaves*5))` = **10 neurons per
cluster**. With ~336 clusters and one checkpoint in a 5-minute run that promotes
~3,360 of ~70,000 neurons. The rest still hold their **random initialisation**
(`(rand+0.5)*3.0/density`, i.e. 7.5–22.5). `ProcessInputs` over random weights is
a random projection — it varies with the cue, which is why activations now
spread, but it carries no learned selectivity, which is why gibberish can
outrank a trained word.

So the probe fix in run #1 was necessary and correct; it simply exposed the next
layer. `Learn()` has been writing STM deltas the whole time and a budget tuned
for checkpoint speed was discarding ~95% of them.

*Fixed:* consolidation is now unbudgeted — every neuron with pending STM has its
deltas applied. Consolidation itself is cheap (dictionary adds); the cost is in
the resulting saves, and a neuron whose weights actually changed has earned its
write. The RF diagnostic now also reports `pendingStm` so starved learning is
visible directly rather than inferred.

**Note for P3:** unbudgeted consolidation will increase checkpoint size and time.
That is the correct trade for now — measure it, then decide the persistence
budget deliberately rather than inheriting a number chosen for save speed.

### P2 run #3 — consolidation fixed, and it was not the blocker (2026-07-30)

Consolidation now lands: **65,613 neurons promoted** across 316 clusters in 2.27s
(was ~3,360). Compression **2.53x** — the best figure this project has produced,
and a real one. Checkpoint 56s.

**But `qwertyuiop` scored 0.993** — higher than every trained cue except the
saturated 1.000s, and higher than `water` (0.728). My prediction that starved
learning caused the poor discrimination was **wrong**. Learning now lands fully
and the controls got *worse*.

**This is no longer a bug — it is a design gap, and it is structural.** Two facts
in the code make selectivity impossible in principle:

1. **The learning target is a constant.** `Cerebro.cs:2022`:
   ```csharp
   neuron.Learn(featureNeuronId, feature.Value, 0.8, output);
   ```
   Every neuron, for every concept, is trained toward output **0.8**. There is no
   negative example and no competition — no neuron is ever told *not* to respond
   to anything. The rule's fixed point is "respond 0.8 to whatever you see."
   A constant supervised target cannot produce discrimination; it is a
   fire-for-everything rule.

2. **Activation is an unnormalised dot product.** `HybridNeuron.ProcessInputs`:
   ```csharp
   weightedSum = Bias + Σ(input × weight)     // no normalisation
   ```
   With weights scaled 7.5–22.5 (P1.7 density compensation), a cue that drives
   only 3 of a neuron's 8 inputs still sums to ~18 → tanh(18/20) ≈ 0.72.
   **Magnitude dominates match quality.** Partial overlap between any two words'
   top-32 encoding dims is therefore enough to saturate the neuron, which is
   exactly why gibberish scores like language.

Both are load-bearing: fixing either alone leaves the other able to wash out
selectivity. This is the first blocker in the whole arc that is a genuine
architectural absence rather than a constant tuned for the wrong purpose.

Everything downstream — P2 fidelity, P4 scoped activation, P4.5 composition —
requires a network that can tell one pattern from another. That capability does
not currently exist, and no amount of instrumentation will reveal it, because
there is nothing there to reveal.

### P2.1 — Competitive learning + synaptic scaling (biology as north star)

Direction chosen: biological mechanisms over engineered fixes. Cortex has no
supervised target and no "0.8"; it has **lateral inhibition** and **synaptic
scaling**. Those map onto exactly the two gaps above.

**Lateral inhibition → competitive training.** All neurons in an assembly compute
their match to the pattern; only the top 25% (min 4) learn it. Losers are
untouched and stay tuned to what they already prefer. A neuron that wins for
"the" is never moved toward "water" — selectivity is a direct consequence rather
than something we have to supervise into existence.

**Synaptic scaling (Turrigiano) → conserved total strength.**
`ReinforceTowardInput` takes a Kohonen step toward the winning pattern
(`w += rate·(x − w)`) and then rescales so the neuron's total input strength over
its receptive field is unchanged. A neuron therefore cannot become dominant by
growing weights — only by becoming better *matched*. This is why normalisation
belongs at the synapse rather than bolted onto the readout.

**Recognition → cosine match.** `MatchQuality` replaces the raw weighted sum.
The dot product measured magnitude, which is how a cue driving 3 of a neuron's 8
inputs still summed to ~18 and saturated. Cosine measures alignment. Recall
threshold is now 0.5 on a genuine [0,1] similarity instead of 0.25 on an
unbounded sum.

Note the two learning paths are now cleanly separated by role: **neuron weights**
learn *what pattern this cell prefers* (competitive, unsupervised), while the
**synaptic graph** learns *what co-occurs with what* (Hebbian). Previously the
supervised delta rule was trying and failing to do both.

#### On STDP (asked 2026-07-30) — in scope at the sequence timescale, not the spike timescale

- **Lateral inhibition: yes, implemented above.** k-winners-take-all is the
  standard computational reduction and it fits this framework directly.
- **STDP proper: no, not without a time axis.** STDP is defined on Δt between
  pre- and post-synaptic spikes. `ProcessInputs` is a single instantaneous
  evaluation — all inputs arrive together, every neuron evaluates once. Faithful
  STDP needs event-driven simulation with spike queues. That is a different
  engine, not a tweak.
- **But there IS a real temporal axis already: word order within a sentence.**
  Words are presented sequentially, so "pre before post" is genuinely available
  at the concept timescale. `RecordCoactivationPattern` currently wires
  **bidirectionally and symmetrically** — completely time-blind. An asymmetric
  update (potentiate pre→post, depress post→pre) would give the synaptic graph
  real causal structure and make cascades directional, which is what recall
  through a graph ought to mean. **This is the most promising next step after
  selectivity is verified**, and it is a modest change to one method.

Sequencing: prove selectivity works (controls silent) → then make the synaptic
graph causal → then re-run fidelity. Verifying one mechanism at a time is what
finally worked in P1.

#### P2.1 first run — regression, caused by my own change

`max=-70.000, avg=-70.000, above_threshold=0` on every call: **zero synapses
created for the entire run**, graph saved empty, and the console flooded badly
enough to overflow the buffer.

Cause: the old training path called `neuron.ProcessInputs(inputs)`, which set
`CurrentPotential` **as a side effect**. `RecordHebbianCoactivation` read that
field to decide who was co-active. The competitive pass replaced `ProcessInputs`
with `MatchQuality` (a pure function) and `ReinforceTowardInput`, so
`CurrentPotential` sat at its initial value — resting, exactly −70 — and nothing
ever passed the gate. A hidden dependency on mutable state left behind by a
function called for a different purpose.

Fixes:
- `RecordHebbianCoactivation` now takes the competition results and uses
  **MatchQuality** directly, so training, wiring and recall all share one [0,1]
  activation measure and none of them depend on residual neuron state.
- `ReinforceTowardInput` records the win as a firing event (ActivationCount,
  LastActivation, Fatigue, ImportanceScore) — winning the competition *is*
  firing, and those fields feed consolidation ordering and procedural save.
- All per-call Hebbian logging moved to `DebugLog.Debug`. It was gated on
  `isFirstCall = synapseCount < 100`, so with an empty graph it fired on **every
  call forever**. The 10-second histogram remains the level-0 signal.

Also observed: writing and re-reading 256 empty synapse partitions costs ~50s
each way — the fidelity run wasn't hung, it was iterating empty partitions over
the NAS. Worth short-circuiting when the graph is empty.

#### P2.1 second run — selectivity is real but weak (2026-07-30)

**First control ever fully rejected: `qwertyuiop` → 0 active, nothing.** Under
every previous mechanism it scored at or above real words (0.839, then 0.993).
Competitive learning + cosine matching genuinely produces selectivity.

| | before P2.1 | after |
|---|---|---|
| `qwertyuiop` | 0.993 (top of all cues) | **0.000 (silent)** |
| `zxcvbnmasd` | 0.770 | 0.605 |
| trained cues | 0.73–1.00 (saturated) | 0.55–0.70 |
| throughput | 25 sent/sec | **44.6 sent/sec** |
| train step | 3.1 ms | **0.9 ms** |

Trained cues no longer saturate and several now fall *below* top-16
(`water`=11, `in`=12, `sleep`=12) — the 0.5 cosine threshold is a real filter
rather than a rubber stamp. Competitive training is also ~3× cheaper than the
old per-neuron delta rule, since only 25% of an assembly updates.

**Not yet good enough:** `zxcvbnmasd` at 0.605 sits *inside* the trained range
(0.55–0.70), above `water` (0.550) and `sleep` (0.569). One control rejected,
one indistinguishable. No threshold separates language from noise, so fidelity
remains unreportable — correctly.

Two measurement fixes, since both diagnostics had gone stale:
- **RF diagnostic was reporting `delta[p10=0.00 med=0.00 max=0.00] firing=0/78`**
  — it still read `CurrentPotential`, which nothing sets any more. Now uses
  `MatchQuality`. It had been silently useless since P2.1.
- **Control check now judges activation STRENGTH, not count.** A count test
  can't tell "control fires weakly" from "control fires like a word". It now
  reports a **discrimination margin** (trained mean − strongest control) and
  requires the strongest control to sit below the *weakest* trained cue, i.e.
  that a separating threshold exists at all.

#### P2.2 — the previous run's silent control was VARIANCE, and MatchQuality had a bug

Re-run with identical settings: `qwertyuiop` went from **0.000 (silent)** to
**0.544 / 6 active**. The "first control ever rejected" was run-to-run noise, not
a property. Recording that plainly because it was nearly written up as progress.

Measured margin with the new metric: **0.024**
(trained mean 0.625, weakest trained 0.559, strongest control 0.601). Controls
sit *inside* the trained range. Every cue — word or keyboard mash — lands in a
narrow 0.55–0.68 band.

**Cause: my cosine implementation normalised by the wrong denominator.**
`wNorm` was accumulated *inside* the loop over `inputs`, so it summed only the
weights that overlapped the current cue. A neuron with 8 input lines of which a
cue drove just 1 was normalised by that single weight — and scored as though it
had matched perfectly. Cosine was therefore measuring "of the lines we share, how
aligned are we", which is near-1 for almost any pair of non-negative vectors, and
is why the whole corpus compressed into a 0.13-wide band.

*Fixed:* ‖w‖ now spans the neuron's **entire receptive field**, so the quantity
becomes *how much of what I listen for is actually present*. A cue driving
different input lines now scores near zero rather than near one. Requires
distinguishing feature weights from synapses inside the shared `InputWeights`
dictionary — `FeatureMapper.GetFeatureNeuronIds()` added for that.

Thresholds recalibrated: a neuron listens to ~20% of a cue's lines, so a perfect
match is bounded well below 1.0 — the ceiling is set by sparsity, identically for
every neuron. Recall 0.5 → **0.2**, Hebbian 0.3 → **0.12**. The old values were
calibrated against the broken denominator.

**Method note:** one run is not a result. `qwertyuiop` flipping from 0.000 to
0.544 across identical configurations means single-run controls cannot establish
discrimination. Future claims need either multiple seeds or a wider cue set.

#### P2.2 result — margin 0.024 → 0.178 (7×), and the test itself was too brittle

The denominator fix was the binding constraint. Controls dropped to ~0.405 while
trained cues held ~0.587.

It still failed — by **0.006**. `qwertyuiop` at 0.409 vs the weakest trained cue
at 0.403. That is a flaw in the criterion, not the system: "strongest control
below *weakest* trained cue" is a min/max test on 14 vs 2 samples, so one weak
straggler vetoes an otherwise clean separation. Reporting a 7× improvement as a
flat failure is bad measurement.

Harness revised — and deliberately made *harder*, not easier:
- **Two tiers of control.** Tier 1 keyboard mash (`qwertyuiop`, `xkcdvbnm`) only
  proves the encoder notices surface weirdness. **Tier 2 pseudo-words**
  (`blorp`, `thrumble`, `flendish`, `grastic`) are English-looking and
  pronounceable but never seen — rejecting *those* is what shows discrimination
  comes from learned identity rather than orthographic oddity. This is a
  stricter bar than before.
- **8 controls instead of 2**, so the control distribution is actually estimable.
- **AUC and d′ reported** alongside the margin. AUC asks "over every
  trained/control pair, how often does the trained cue win?" — robust to one
  straggler, where min/max is not.
- **Three verdict tiers**: perfectly separable (valid) / strong but imperfect
  (AUC ≥ 0.90 and margin > 0.10 → fidelity printed but explicitly
  **PROVISIONAL**) / overlapping (invalid, aborts).
- The weakest trained cue is now named, so a straggler can be judged on whether
  it is a genuinely rare word.

The provisional tier is a deliberate judgement call and worth flagging as such:
it lets a strong-but-imperfect result be *seen* rather than hidden, at the cost
of a weaker guarantee. The hard abort remains for genuine overlap.

### P2.3 — the fidelity number has been vacuous the whole time (2026-07-30)

With 8 controls the statistics finally became readable: **AUC 0.902, d′ 2.23**,
trained mean 0.587 vs control mean 0.424. Discrimination is real and the effect
is large. Tier-2 pseudo-words behaved exactly as predicted — `grastic` 0.526,
`thrumble` 0.453 outscored the keyboard mash (0.359–0.445), so part of the
discrimination is orthographic plausibility rather than learned identity. Weakest
trained cues are `so` (0.403) and `water` (0.415): both short, and short words
have few distinctive n-grams for `FeatureEncoder` to work with.

**But the fidelity measurement itself was vacuous, and had been from the start.**
`--fidelity-test` began with `InitializeAsync`, i.e. by loading a brain from
disk. The first probe therefore materialised clusters **through**
`ProceduralNeuronRegenerator` — so baseline **A was already a regeneration**, B
was a second regeneration of the same files, and the two were identical by
construction. 100% for every cue including `qwertyuiop` was not a result; it was
the experiment measuring whether reading a file twice is deterministic.

Every 100% reported in P2 runs #1–#4 is void for this reason, independently of
the control problem.

*Fixed:* the harness now **trains in-process first** (`--train N`, default 500),
so assemblies are live in memory and never persisted when A is taken. Only then
save + evict + re-probe for B. A is the original; B is the regeneration.

**Also fixed — a metric that punished a better experiment.** The gate used
`trainedMean − controlMax`, mixing a mean against a max, so it *shrank* purely
from adding controls: 0.178 with 2 controls → 0.062 with 8, while d′ showed
discrimination was strong. Criteria are now AUC ≥ 0.90 **and** d′ ≥ 1.5 for the
provisional tier; strict min/max separation still required for a clean pass.
Mean gap and strict margin are still printed, but no longer gate anything.

### P2.4 — THE ANSWER: the fidelity test cannot test the thesis (2026-07-30)

The in-process training fix worked — eviction genuinely persisted this time
(`changed=258, packsWritten=6`, 83,172 neurons, 800 clusters evicted), so B is a
real regeneration from disk. Fidelity: **100.0%** across all 22 cues.

**And it still doesn't test the thesis.** The reason is structural:

- Fidelity is measured on the top-k active set, ranked by **`MatchQuality`**.
- `MatchQuality` reads **`InputWeights` only** — grep confirms zero references to
  `Threshold` or `Bias`.
- `Threshold` and `Bias` are **the only two properties**
  `ProceduralNeuronRegenerator` regenerates from the VQ code
  (`ProceduralNeuronData.cs:146-147`).
- Synaptic weights are persisted **verbatim** and restored verbatim.

So 100% means "explicitly persisted weights survive a round trip". Of course they
do. The recall measure is structurally blind to the procedurally-generated part,
so no value it returns can bear on procedural regeneration. The controls scoring
100% too was the tell, and it was visible from run #1.

**Quantified — how procedural is a "procedurally generated" neuron?**
Per persisted neuron: **4 bytes** of VQ code (from which Threshold and Bias are
regenerated) against ~60 bytes of identity/metadata plus 20 bytes per explicitly
stored synaptic weight. At ~8 weights that is roughly **2% procedural, 98%
explicitly persisted**. The 1.6–2.5× compression comes from *dropping weak
synapses* — pruning — not from regenerating structure.

This confirms, with measurement, the concern recorded at the very start of this
reboot: *"The 90% compression claim is soft… the compression mostly comes from
dropping weak synapses — that's pruning, not procedural generation. The core
hypothesis remains untested."* It is now untested **and demonstrated
untestable by this harness**.

Harness updated to say so in its own output rather than printing a green tick:
the report now prints the procedural/explicit byte split and states plainly that
a high number measures persistence round-tripping.

**What testing the thesis would actually require** — the readout must depend on
regenerated structure:
1. **Make recall depend on the VQ code.** If a neuron's receptive field were
   *generated* from `codebook[VqCode]` + neuron identity (the P1.7 sparse-subset
   trick already proves shapes can be derived rather than stored), then weights
   would not need persisting at all and regeneration fidelity would become a real
   question with a real failure mode.
2. **Then measure fidelity against storage.** Fidelity at 4 bytes/neuron versus
   fidelity at 220 bytes/neuron is the actual thesis curve — "how much can we
   throw away and still recall?" That is P3's persistence budget, and it only
   becomes meaningful once (1) exists.

**Status: P2 answered, negatively but cleanly.** The plumbing works — assemblies
persist, evict, and reload losslessly, discrimination is real (AUC 0.902,
d′ 2.22). What does not yet exist is any dependence of recall on procedurally
generated structure, so there is nothing yet for a fidelity experiment to lose.

### P2.5 — the experiment was mutating the thing it measured (caught 2026-07-30)

`--fidelity-test` calls `SaveAsync()` before eviction, then
`EvictAllClustersAsync()` persists every cluster on the way out. Run against a
real brain it therefore folded its own 500 in-process training sentences
**permanently into that brain**. Observed in one run: synapses **319,706 →
646,684**, plus `Membership changed: 210→290`, `New cluster entry: 178
neurons`, and so on across 258 clusters.

Consequences:
- Each run started from a fatter, different baseline than the last.
- Comparisons across runs were never like-for-like — this is a strong candidate
  for the run-to-run variance that had `qwertyuiop` swinging 0.000 → 0.544 and
  activation values shifting between otherwise identical configurations.
- `/Volumes/jarvis/brainData` has been contaminated by every fidelity run so far.

*Fixed:* the experiment now defaults to an **isolated scratch brain** — a temp
directory, trained from nothing, deleted on exit (including on the early-return
path, via `finally`). `--brain-path` still targets a real brain but prints a loud
warning before and after that the run will write into it.

Lesson, and it belongs with the others: a measurement harness must be inert with
respect to its subject. This one wrote to the same store it read from, and every
number produced before this fix carries that caveat.

### P2.6 — first clean-room run (2026-07-30 17:48). Isolation confirmed by Bill.

Scratch brain, trained from nothing, brainData verified unchanged. These are the
first numbers not contaminated by the harness writing into its own subject, and
they are materially better than the contaminated ones — the contamination was
*hiding* the improvement.

| | contaminated brain | clean room |
|---|---|---|
| VQ utilisation | 2.5% (perplexity 7) | **67.0%, 343 codes claimed, perplexity 212** |
| control mean | 0.428 | **0.240** |
| trained mean | 0.587 | 0.557 |
| mean gap | 0.159 | **0.316** |
| AUC | 0.902 | **0.971** |
| fidelity | 100.0% (all cues) | **99.7%** |

**Four of eight controls now activate nothing at all** — `xkcdvbnm`, `thrumble`,
`grastic` at zero, `flendish` a single neuron at 0.205. Three of those are
tier-2 pseudo-words, which was the hard test. The P1.6h codebook-seeding fix
also only shows its true effect on a fresh brain: 67% utilisation versus the
2.5% collapse.

**`water` activating nothing is correct, not a failure.** The scratch brain saw
only 500 sentences; "water" plausibly never appeared. An unseen English word
behaving exactly like unseen gibberish is the *right* result and is evidence the
gate is about exposure rather than orthography.

**First non-100% fidelity ever recorded: `to` at 93.8%** (15 of 16 neurons kept).
Small, but it means the measurement is finally sensitive to something.

**Caveat on how the silence is achieved.** Controls that score zero do so because
their VQ code maps to *no trained cluster* — `LoadTrainedNeuronsForConcept`
returns empty before any matching happens. That is a coarse region gate, not
fine pattern discrimination. `zxcvbnmasd` (0.451), `qqzzxxjj` (0.513) and
`blorp` (0.448) land on codes that *do* have clusters and then match moderately.
So the AUC of 0.971 is part real selectivity, part lookup miss.

**Procedural content measured: 1.9%** — 4B of VQ code against 208B explicit
(7.4 weights × 20B + 60B identity/metadata). Matches the analytic estimate.

**Conclusion, agreed with Bill: the plumbing is sound; the thesis is untested.**
P2 is closed as a negative result — not "the thesis failed" but "this experiment
cannot address it", with the reason precisely located and quantified.

### P3 (proposed) — make procedural generation load-bearing

The single change that converts P2 from vacuous to meaningful: **generate the
receptive field's weights, don't store them.**

Today a neuron's receptive-field *shape* is already generated from its identity
(`NeuronSamplesFeature`, P1.7) while its *weights* are persisted verbatim.
Instead:
1. Generate baseline weights from `codebook[VqCode]` projected onto the neuron's
   generated sparse subset — deterministic, reproducible, zero bytes stored.
2. Persist only the **deviation** from that baseline, and only where learning
   moved a weight materially.
3. Regeneration = generate baseline, apply stored deviations.

This makes recall depend on the VQ code, so fidelity acquires a real failure
mode. It also turns P3's persistence budget into the actual thesis curve:
**fidelity as a function of bytes retained per neuron** — how much can be thrown
away and still recall? At 1.9% procedural content there is nothing to trade; at
a generated baseline plus sparse deviations there is a curve to plot.

**Next mechanism (biology): intrinsic homeostatic plasticity.** Cortical neurons
regulate their own excitability toward a target firing rate — a cell that
responds to everything becomes harder to excite. That is precisely the remaining
gap: assemblies are tuned, but nothing punishes a neuron for being broadly
responsive. Per-neuron gain/threshold adjusted from a running average of its own
match would sharpen the margin without any supervision, and it is the natural
partner to the lateral inhibition and synaptic scaling already in place.

### P3 — Make procedural generation load-bearing ✅ IMPLEMENTED (2026-07-30)

The change that converts P2 from vacuous to meaningful. Previously a neuron's
receptive-field *shape* was generated (P1.7) but its *weights* were persisted
verbatim — measured at **1.9% procedural content**, and recall never touched the
VQ code, so regeneration had no failure mode and returned 100% regardless.

**Neurons are now born as their VQ prototype.**
`ProceduralReceptiveField.GenerateBaselineWeight(neuronId, featureKey, codebook)`
derives a weight from `codebook[VqCode]` at the dimension a `cf_{dim}_p/n` line
encodes, weighting the matching polarity strongly and the opposite weakly, with
identity-derived jitter so neurons sharing a code aren't clones (the P1.6n
failure mode). Deterministic, so it never needs storing.

Pipeline:
1. **Birth** — `EnsureFeatureWiring` initialises weights to the prototype instead
   of random values.
2. **Learning** moves the neuron away from its prototype (competitive Hebbian +
   synaptic scaling, unchanged).
3. **Save** — `FromSnapshot` stores a feature weight *only* if it has drifted
   further than `ProceduralDeviationThreshold` from the baseline.
4. **Load** — `RegenerateNeuron` rebuilds the entire receptive field from
   `(VqCode, identity)`, then layers stored deviations on top.

Wiring: `GlobalNeuronStore` receives the mapper, the sparse-subset rule and the
quantizer via `EnhancedBrainStorage.ConfigureProceduralReceptiveFields`, injected
from `Cerebro.AttachConfiguration`. Unset, it falls back to verbatim weights, so
nothing breaks if a caller skips configuration.

**Why this makes the experiment real:** recall now depends on regenerated
structure. If the generated baseline is wrong, fidelity drops — the failure mode
that could not previously exist. Storage per neuron becomes a function of *how
much a neuron learned*, not of how many inputs it has.

**The thesis curve is now plottable.** `--deviation-threshold` is the persistence
budget: raise it and fewer deviations persist (smaller, lossier); lower it and
more do. Sweeping it against fidelity answers the actual question — **how much
can be thrown away and still recall?**

- **Exit criterion:** a fidelity-vs-bytes curve. Procedural content well above
  1.9%, and a visible knee where fidelity falls as the budget tightens. A flat
  100% across all thresholds would mean the baseline is still not load-bearing
  and I have missed something again.

#### P3 first result (2026-07-30) — the failure mode exists

**Fidelity fell from ~100% to 83.2%.** For the first time regeneration *costs*
something, which is the whole point: recall now depends on structure that is
generated rather than stored. Per-cue losses are uneven and informative —
`so` 43.8%, `it` 68.8%, `are` 75.0%, against `you` at 100%.

**Compression 1.66x → 4.68x** (12,650,224 → 2,704,080 bytes for 35,930 neurons):
~75 bytes per neuron where it was ~212, of which ~60B is irreducible
identity/metadata. So the *learned* portion collapsed from ~150B to ~15B.

**But the procedural-content diagnostic was wrong and I nearly reported it.**
It counted weights held **in memory**, which is independent of the persistence
threshold — hence a flat "0.4%, 42.4 weights" across an entire budget sweep while
the compression ratio was moving. It was measuring the wrong side of the save.
Fixed: `EnhancedBrainStorage` now records what actually reached disk
(`LastSaveWeightsStored` vs `LastSaveWeightsInMemory`), and the report states the
fraction of the receptive field **regenerated** rather than a byte percentage
dominated by fixed identity overhead.

**Threshold sweep (0.25 / 1.0 / 4.0 / 16.0):** fidelity 84.0 / 84.7 / 81.2 /
81.6 — a shallow downward trend, no sharp knee, and within run-to-run noise
(AUC swung 0.913–0.990 across the same four runs). Two readings are possible and
the fixed diagnostic should separate them: either the budget genuinely has a
broad flat region, or the sweep barely changed what was stored. The stored-weight
count per neuron will now say which.

**Also visible:** discrimination margin went slightly negative (−0.010, −0.011)
in two sweep runs while AUC stayed 0.91–0.99 — more evidence that the strict
min/max margin is the noisy statistic and AUC/d′ are the ones to trust.

#### P3.2 — the metric vanished from the sweep, and the sweep range was wrong

Second sweep: fidelity 83.0 / 83.3 / 80.1 / 81.2, and **no procedural line at
all**. Cause: renaming the report to "Receptive field:" / "Bytes per neuron:"
broke the existing grep, so the metric silently disappeared from results. A
diagnostic that changes shape breaks every script reading it — now emitted as a
single line with a stable `PROCEDURAL:` tag, alongside `BUDGET:` so each run
records the threshold it used.

**The sweep range was also wrong, and that explains the flatness.** Weights are
born at O(10–45) (`BaselineGain`), competitive learning steps by ~0.05 × (input −
weight), and synaptic scaling then conserves total strength — so a neuron drifts
only a little from its prototype. With ~0.75 weights stored per neuron at 4.68×
compression, nearly every deviation already sits **below 0.25**. Sweeping
0.25 → 16 therefore sampled the same point four times; the 2-point decline was
the tail, not the curve.

`scripts/sweep_fidelity.sh` added (versioned, not a local file) sweeping
**0.02 → 8.0**, weighted toward the low end where the deviations actually live,
and emitting CSV so the curve can be plotted directly.

**Substantive reading so far, pending the finer sweep:** ~98% of the receptive
field is regenerated and that alone yields **~83% fidelity**. The last ~17%
lives in deviations that are mostly *not* being stored at current thresholds.
That is already a thesis-relevant statement — prototype alone recovers most of
an assembly — but the shape of the trade only becomes visible once the sweep
straddles the deviation distribution.

#### P3.3 — THE CURVE, and it is flat (2026-07-30)

```
threshold  fidelity  regenerated  stored/neuron  bytes/neuron  AUC
0.02         83.0       78.5%         1.60           96       0.885
0.05         83.3       81.4%         1.38           92       0.952
0.10         85.1       83.3%         1.24           89       0.942
0.25         80.6       86.9%         0.97           83       0.981
0.50         85.4       90.1%         0.74           79       0.952
1.00         83.3       92.4%         0.56           75       0.981
2.00         81.2       94.6%         0.40           72       0.962
8.00         84.2      100.0%         0.00           64       0.923
```

**At the bottom row: zero weights stored, 100% regenerated, 64 bytes per neuron
— and fidelity is 84.2%, the best in the table.** Storing nothing performs as
well as storing 1.6 weights per neuron. Fidelity is flat at 83% ± 2 across a
400× budget range and does not correlate with the budget at all.

Two conclusions, and the second is more interesting than the first:

1. **The receptive field is fully procedural, and it works.** Weights need not be
   persisted at all. What remains is 64 B/neuron of pure identity/metadata —
   Guid, cluster Guid, importance, activation count, concept tag — none of which
   is learned state. Against the pre-P3 ~212 B that is a **3.3× reduction with no
   measurable loss**, and the residual is bookkeeping, not knowledge.

2. **The stored deviations contribute nothing to recall**, so the missing 17% is
   not weight error. Buying more weights does not buy it back. Learning is
   evidently not shaping the receptive field in any way the readout can see —
   which, combined with P2.6's observation that silent controls are silent
   because their VQ code maps to *no cluster*, suggests recall is currently close
   to **nearest-prototype lookup** rather than learned memory.

**Where does the 17% go, then?** Two candidates, and they are distinguishable:
a neuron in A's top-k that is missing from B's either **vanished** from the
cluster (a membership/persistence issue, unrelated to regeneration) or is
**present but out-ranked** (a genuine activation difference). Added
`ProbeConceptCandidatesAsync` and a `LOSS: absent=… demoted=…` line to attribute
every lost neuron to one or the other, with both columns now in the sweep CSV.

Per the rule that has actually worked this session: measure it, do not reason
about it.

#### P3.4 — the 17% located: A and B were building different-sized fields

```
threshold  fidelity  regenerated  lost_absent  lost_demoted
0.02         83.6       78.4%          0            47
0.10         83.8       83.6%          0            46
1.00         83.3       92.4%          0            48
8.00         81.9      100.0%          0            52
```

**`lost_absent = 0` in every row.** Not one neuron vanishes across the whole
sweep. Regeneration reproduces the neuron *set* perfectly; every loss is a
survivor that got out-ranked. So the residual is pure activation shift — and
since stored weights make no difference either, it is not weight error.

**Cause, confirmed in the code:** `EnsureFeatureWiring` wired only the features
present in the *current training input*, while `RegenerateNeuron` rebuilds every
line the neuron's identity samples across the *whole* feature set. Regenerated
neurons therefore had **larger receptive fields than they ever had in memory**.
`MatchQuality` normalises by ‖w‖ over the full field, so a bigger field means a
different denominator, every activation shifts slightly, and the top-16 reorders.
Nothing lost, everything nudged — exactly the measured signature.

*Fixed:* a receptive field is defined by **identity, not history**, so the
in-memory field must be complete too. `EnsureFeatureWiring` now wires every line
the neuron samples across all known features, guarded by vocabulary size
(`LastWiredFeatureCount`) so the full walk only runs when new features appear.
`FeatureMapper.GetAllFeaturesSnapshot()` added because `GetAllFeatures()` returns
a live key collection that is unsafe to iterate on a path that can register a
feature.

**Prediction, recorded before running:** fidelity should rise sharply — A and B
now construct the same field by the same rule, so the remaining difference is
only genuine learned drift. If it stays at ~83%, the field sizes still disagree
and the next step is to log field size at A and B directly rather than infer it.

**Risk worth flagging:** every neuron now carries ~20% of the *entire* feature
vocabulary rather than 20% of what it met. Fields grow with vocabulary, so
absolute cosine values will drop and `RecallMatchThreshold` / discrimination
statistics may need recalibrating. Watch AUC and the active counts, not just
fidelity.

---

## P3 RESULT — the thesis curve (2026-07-30)

```
threshold  fidelity  regenerated  stored/neuron  bytes/neuron   AUC    absent  demoted
  0.02      100.0%      91.1%         3.73           139       0.904     0        0
  0.05       99.7%      93.2%         2.86           121       0.885     0        1
  0.10      100.0%      95.0%         2.11           106       0.894     0        0
  0.25       99.3%      97.0%         1.28            90       0.913     0        2
  0.50       99.0%      98.4%         0.68            78       0.904     0        3
  1.00       98.3%      99.2%         0.32            70       0.837     0        5
  2.00       96.9%      99.7%         0.12            66       0.933     0        9
  8.00       95.5%     100.0%         0.00            64       0.837     0       13
```

The P3.4 field-size fix worked as predicted: fidelity went from a flat ~83% to
**95.5–100%**, and for the first time the budget actually buys something. The
curve is monotonic across a 400× range in threshold and 2.2× in bytes.

**The headline:** with **zero learned weights persisted** — the receptive field
generated in full from `(VqCode, identity)` — recall is **95.5% faithful at 64
bytes per neuron**. Spending 14 more bytes (78 B, 0.68 stored weights) buys
**99.0%**.

Against the pre-P3 baseline of ~212 B/neuron: **2.7× smaller at 99% fidelity,
3.3× at 95.5%.** Unlike the old "90% compression" claim, both figures are
measured, and the compression is genuinely procedural rather than pruning —
`regenerated` reaches 100%, meaning nothing about the receptive field is stored
at all.

**The knee is at ~0.5–1.0.** Fidelity holds ≥99% down to 78 B/neuron, then
starts paying: 98.3% at 70 B, 96.9% at 66 B, 95.5% at 64 B. That is the answer to
*how much can be thrown away and still recall* — for this architecture, at this
scale: **essentially all of the learned weight state, for ~4.5 points of recall.**

**`lost_absent = 0` throughout.** Regeneration never loses a neuron; the entire
residual is a handful of survivors changing rank (0–13 out of 288 slots).

### Caveats, stated plainly

1. **Discrimination degraded.** AUC fell to 0.837–0.933 from 0.91–0.99, d′ to
   1.35–1.68 from 1.5–2.2. This is the risk flagged before the run: neurons now
   carry ~20% of the *entire* vocabulary rather than 20% of what they met, which
   dilutes the cosine. High fidelity over less-distinguishable assemblies is
   worth less, so this needs recovering before the result is banked — most likely
   by recalibrating `RecallMatchThreshold` for the larger fields, or by scaling
   field density inversely with vocabulary.
2. **The 64 B floor is not fundamental.** It is Guid (16) + ClusterId (16) +
   importance (4) + activation count (4) + concept tag (~20) + VqCode (4). The
   tag is debug-only, ClusterId is implicit in the partition path, and importance
   and count are recomputable. A real floor is nearer 20 B, which would put the
   ratio above 10×.
3. **Recall still leans on nearest-prototype lookup** (P2.6, P3.3). The
   generated field carries the assembly; learned drift contributes little. That
   the thesis works *at all* here is partly because the VQ codebook is doing the
   representational work.

### Status

P2's question is answered affirmatively **for the receptive field**: procedural
generation preserves the assembly, and the storage/fidelity trade is now a
measured curve rather than an assertion. What remains untested is whether
*learned* structure survives the same treatment — currently there is little
learned structure for recall to depend on, which is the P4 problem.

---

### P4.1 — recover discrimination: the field should come from the prototype

Diagnosing the AUC drop changed what the fix should be. Before P3.4 a neuron's
field was implicitly **history**-determined — it only held lines it had actually
met — and that history *was* the discriminator: a neuron trained on "the" had
weights only on "the"'s lines, so "the" drove all of them and `blorp` drove few.
P3.4 made fields identity-determined, which is regenerable but carries no
information about what the neuron represents, so every neuron overlaps any cue by
the same ~8 lines and cosine cannot separate them. Fidelity was bought with
discrimination.

History cannot be regenerated. A **prototype** can — and it costs 4 bytes.

`ProceduralReceptiveField.SamplesFeature(neuronId, featureKey, prototype)` now
draws the field from the neuron's VQ code: it listens to lines whose codebook
dimension is significant for that code (magnitude above ~1.35× the prototype's
own mean, so flat codes aren't starved), preferring the polarity the prototype
expresses, narrowed by an identity hash so neurons sharing a code still differ.
Regenerable from the code alone, and correlated with meaning — **a neuron hears
what it is for**.

Also removed `TrainNeuronWithFeatures`, dead since the competitive pass replaced
it in P2.1. It held a *second, divergent copy of the wiring rule* — precisely the
shape of bug that produced the P3.4 field mismatch. There is now one rule in one
place, used by both training and regeneration.

**Predictions, before running:** AUC should recover toward 0.95+ as fields
regain meaning; fidelity should stay high since both paths still share one rule;
field size should *shrink* (a neuron listens only to its prototype's significant
dims), so bytes/neuron may fall further. Least certain: absolute cosine values
shift again, so `RecallMatchThreshold` (0.2) may need retuning — watch the active
counts, and if controls go silent because *everything* goes silent, that is the
threshold, not selectivity.

#### P4.1 result — discrimination recovered, at a small fidelity cost

```
threshold  fidelity  regenerated  stored/neuron  bytes/neuron   AUC
  0.02       96.2%      86.5%         4.80           160       0.981
  0.10       97.2%      92.5%         2.67           117       0.990
  0.25       96.5%      95.6%         1.58            96       0.990
  1.00       96.9%      98.9%         0.38            72       0.981
  2.00       95.5%      99.5%         0.19            68       0.990
  8.00       92.7%     100.0%         0.00            64       0.952
```

| | P3.4 (identity fields) | P4.1 (prototype fields) |
|---|---|---|
| AUC | 0.837–0.933 | **0.923–0.990** (mostly ≥0.98) |
| fidelity @ 0 stored | 95.5% | 92.7% |
| field size | 41.9 weights | **35.6** (−15%) |

Predictions: AUC recovery ✅, field shrink ✅, fidelity holding ✗ (−3 points).
**A good trade** — discrimination is what makes a fidelity number mean anything,
and ~3 points of recall bought ~7 points of AUC.

The curve is also flatter at the top now: fidelity sits at ~96% from 160 B all
the way down to 68 B, then drops to 92.7% at 64 B. Best operating point:
**68 B/neuron, 99.5% regenerated, 95.5% fidelity, AUC 0.990.**

`lost_absent = 0` throughout, still. Caveat: one run per threshold, and AUC
varies ~±0.03 within a sweep, so individual points are soft; the trend is not.

### P4.2 — sequence-level STDP (implemented)

Bill's question from earlier, now built. Spike-timing STDP proper needs a
millisecond clock this engine does not have — `ProcessInputs` is one
instantaneous evaluation, so there is no Δt between pre- and post-synaptic
events. But there *is* an unused temporal axis: **word order within a sentence**.

`RecordCoactivationPattern` wires bidirectionally and symmetrically, so the graph
has been entirely time-blind — "cat"→"sat" and "sat"→"cat" indistinguishable.
`RecordCausalPattern` now applies the asymmetry: the previous word's assembly
**potentiates** onto the current word's (pre before post), and the reverse
direction is **depressed** (`DepressSynapse`, LTD analogue, which only weakens
existing synapses and never creates one — so the out-degree budget is untouched).

Depression is deliberately weaker than potentiation (`CausalDepressionRate` 0.4):
symmetric strength would cancel both directions for word pairs occurring in both
orders and erase the very structure the rule exists to build.

`Cerebro.EndSequence()` clears the previous-assembly buffer at sentence
boundaries — without it the graph would learn that the last word of one sentence
causally precedes the first word of the next. Called from both
`ProductionTrainingService` and the fidelity harness's in-process trainer.

**Why this matters for the thesis:** P3 established that recall rides almost
entirely on the VQ prototype, with learned drift contributing little — a real
result, but a bounded one. Directional synapses give recall something *learned*
to depend on. **Prediction: no change to the fidelity/discrimination numbers**
(neither reads the synaptic graph), but synapse counts and decay behaviour should
shift, and the graph acquires order information that cascade-based recall in P5
can actually use.

#### P4.2 result — prediction confirmed, and a resume regression surfaced

**Fidelity and AUC did not move**, as predicted: 92.7–99.3% (was 92.7–97.2%),
AUC 0.962–1.000 (was 0.923–0.990) — all within the ±0.03 run noise, and
`regenerated`/`stored`/`bytes` are identical to three digits. Clean confirmation
that the synaptic graph is decoupled from these measures. Compression improved
slightly to **5.14x**.

**Synapse count roughly doubled**, 348,806 → 783,175. Expected: the causal rule
adds a second, *directional* population on top of the symmetric one, so the graph
now stores order as well as co-occurrence. Decay never ran in this short session
(5,000-event cadence, ~4,800 events), so that figure is pre-equilibrium.

**Regression found — and it is mine, from P1.6f.** Assembly reuse started at
**11.8%** and 23,905 neurons were created in 200 sentences. Cause: the
resident-only guard on assembly reuse. On a *warm* brain it correctly avoids
pulling 5 clusters off the NAS per learn event; on a *resumed* brain nothing is
resident, so every concept failed to find its existing assembly and colonised a
new one — the exact failure P1.6 was written to prevent, reintroduced for the
resume path only.

*Attempted fix (P4.3):* check non-resident candidates against
`ClusterMetadata.ConceptLabel`, which is already in memory. **This did not work —
see P4.4.**

**Not a regression:** the 0.4 sent/sec throughput. That is the NAS-resume path
(`lookup 77 ms`, `syn 145 ms` — both cluster loads over the network), the same
cost measured back in P1.6f. Scratch-brain training in the fidelity harness still
runs at ~240 cps, so P4.2 itself is not slowing anything.

### P4.4 — assembly-reuse gate, third attempt (the one that identifies the field)

The P4.3 run showed reuse *still* starting at 6.2% and climbing slowly
(6.2 → 9.7 → 23.7 → 40.0 → 38.1 → 34.8 → 41.4%), with 7,041 neurons created in
63 sentences. The curve shape is the tell: reuse rises only as clusters happen to
become resident *within the session*, which is exactly the P1.6f behaviour the
fix was supposed to remove.

**Cause: P4.3 gated on the wrong field.** `ClusterMetadata.ConceptLabel` is the
cluster's *founder* — assigned once in `FindOrCreateClusterForPattern` from
whichever word first created it, and never revised. A cluster hosts many words'
assemblies, so the founder check admits exactly one concept and rejects every
other legitimate member. Functionally almost identical to the residency rule it
replaced, which is why the numbers barely moved.

**The correct field already existed:** `ClusterMetadata.AssociatedConcepts` — the
union of every member neuron's concepts (`NeuronCluster.AddNeuronAsync` unions it
in; `CreateClusterMetadata` persists it), fully in memory after `LoadAsync`. The
gate now admits on membership in that set, falling back to `ConceptLabel`.

**Instrumentation added, because reuse% could not distinguish the failure modes.**
Three fixes were now aimed at this gate and two missed, because a low reuse% is
consistent with both "the gate rejected the right cluster" and "no similar cluster
existed." The `Allocation` line now carries:

```
gate[resident=N member=N skip=N nometa=N]
```

- `resident` — candidate already materialised, probed for free
- `member` — non-resident, metadata says the concept lives here → worth the load
- `skip` — non-resident, metadata says it doesn't → no NAS hit
- `nometa` — non-resident with no metadata (unsaved/new cluster)

**Falsifiable read on the next resumed run.** If P4.4 is right, `member` is
substantially non-zero from the first window and reuse starts high. If `member`
stays ~0 while `skip` is large, `AssociatedConcepts` isn't reaching disk and the
problem is in persistence, not the gate. If `nometa` dominates, the region map
is pointing at clusters that were never saved — a different bug again. Each
outcome names its own next step, which the reuse curve alone never did.

*Generalises the standing rule* (measure the mechanism, don't infer it from
summary statistics): when a fix targets a decision, instrument the **decision**,
not its downstream aggregate.

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
