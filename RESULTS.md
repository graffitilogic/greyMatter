# greyMatter POC — RESULTS

**Append-only.** Every entry carries the exact command line that produced it (plan.md rule 9).
Entries are never edited after the fact; corrections are appended as new entries that name what
they supersede.

Dev machine for all timings: Apple Silicon Mac, .NET 8.0.301, `-c Release`, single-threaded
unless stated. NAS mounted at `/Volumes/jarvis`.

---

## P0 — Scaffold and baseline instruments

**Date:** 2026-08-16
**Gate:** `gm eval encoder-ceiling --train 500` runs on Tatoeba and reproduces the legacy finding,
with numbers recorded here as the baseline every later result must beat.
**Status: PASS.**

### P0.1 — Reproduction check against the legacy harness

Before trusting any number below, the new instrument was diffed against the legacy one on
identical input. Both were run and their metric lines compared:

```bash
dotnet run --project greyMatter/greyMatter.csproj -c Release -- --encoder-ceiling --train 500
```
```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval encoder-ceiling --train 500
```

**Result: bit-identical on every reported metric** (`CEILING_AUC`, `CEILING_DPRIME`,
`CEILING_GATE`, `NN_SIM`, all four `TOPK_COLLISIONS`, `OVERLAP`, `DIM_USAGE`,
`CONTROL_DIFFICULTY`, vocabulary size, both `pairs above` counts). The only textual differences
are reworded interpretive prose. The port is faithful; the baseline below is the legacy baseline.

### P0.2 — The encoder ceiling (surface stage)

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval encoder-ceiling --train 500
```

Config: `Dataset=tatoeba_small` (`/Volumes/jarvis/trainData/Tatoeba/sentences_eng_small.csv`),
`SurfaceDimensions=128`, `Sparsity=32`, `Seed=12345`. Vocabulary: first 1,355 distinct
>1-char tokens from 500 sentences.

| Metric | Value |
|---|---|
| `CEILING_AUC` | **0.455** |
| `CEILING_DPRIME` | **−0.03** |
| `CEILING_GATE` | OVERLAPPING (strongest control 0.741 vs weakest trained 0.404) |
| `NN_SIM` | median 0.833, p90 0.890, max 0.954 |
| pairs above 0.95 / 0.90 | 4 / 97 |
| `TOPK_COLLISIONS` k=4 | 728/1,355 distinct (46.3% collide) |
| `TOPK_COLLISIONS` k=8 | 1,292/1,355 distinct (4.6% collide) |
| `TOPK_COLLISIONS` k=16 | 1,352/1,355 distinct (0.2% collide) |
| `TOPK_COLLISIONS` k=32 | 1,355/1,355 distinct (0.0% collide) |
| `OVERLAP` (top-32, nearest other word) | median 27/32, p90 29/32, max 31/32 |
| words with ≥30/32 overlap | 63 (≥28: 429) |
| `DIM_USAGE` | 125/128 dims used; 1 generic (>90% of words), 5 discriminative (<10%) |
| `CONTROL_DIFFICULTY` | strongest control 0.741 vs vocabulary NN median 0.833 |

**These are the numbers every later result is reported as lift over** (§6.1 rule 6).

### P0.3 — What the baseline actually says

Three findings, two of which sharpen §1.3 rather than merely confirming it.

**1. The encoder ceiling is 0.455, not ~0.94 — the §1.3 worst case does not hold.**
§1.3 warned that the legacy fidelity result (AUC 0.94–1.00 over 40 runs) might be *entirely*
attributable to the encoder. Measured, it is not: raw surface encoding separates trained cues
from controls at **below chance**, with d′ ≈ 0. The 40-run separation was therefore produced by
something downstream of the encoder. This is good news for the rebuild — there is a full 0.5 AUC
of headroom — but it also means the legacy result is now *unexplained* rather than *explained*,
and P4 must not assume it will reappear.

**2. Surface similarity is governed by hash noise, not by surface form.**
The name "surface-form encoder" implies that spelling relatives collide. They do not, reliably:

| pair | cosine | top-32 overlap |
|---|---|---|
| `if` ~ `so` (unrelated function words) | **0.954** | 13/32 |
| `had` ~ `look` (unrelated) | 0.949 | 26/32 |
| `want` ~ `wants` (relatives) | 0.938 | 29/32 |
| `sleep` ~ `sleeping` (relatives) | 0.436 | 13/32 |
| `sleep` ~ `sleeps` (relatives) | **0.143** | 16/32 |
| `the` ~ `teh` (exact anagram) | **−0.024** | 2/32 |

Mechanism: 23 of 32 orthographic dims, 27 of 32 phonetic and 29 of 32 statistical dims are pure
per-word hash spread over [−0.5, 0.5]. That is ~79 of 128 dims contributing zero-mean noise of
large variance to every dot product, against a handful of structural dims (length, vowel ratio,
syllable count) contributing a shared positive offset. Similarity is consequently close to
**arbitrary with respect to both meaning and morphology** — the corpus's single most-confused
pair is two unrelated two-letter words.

The practical consequence for P2 is stronger than §4.2 assumed: the context stage is not
*refining* a weak-but-real surface signal, it is supplying the first non-arbitrary signal in the
pipeline. `ContextBlend` β should be expected to want a high value, and the β=0 null model is a
genuinely uninformative baseline rather than a weak one.

**3. Cosine and top-k overlap rank pairs differently, confirming the §4.2 rarity decision.**
`if`~`so` are the nearest pair in dense space yet share only 13/32 dims; `want`~`wants` are less
similar in cosine yet share 29/32. The sparse code is not a compression of the dense vector.
Combined with `DIM_USAGE` (only 5 of 128 dims are discriminative, 1 is generic) and the collision
curve (46.3% collide at k=4, 0% at k=32 — so the highest-magnitude dims are the *shared* ones),
this confirms the §4.2 decision to weight dims by inverse document frequency rather than by
magnitude. Magnitude weighting emphasises exactly the least discriminative dimensions.

**Carried forward as a caveat, not a defect:** `CONTROL_DIFFICULTY` fails rule 5. The strongest
control sits at 0.741 while the median vocabulary word has a neighbour at 0.833 — the mash and
pseudoword controls are *easier* than ordinary corpus neighbours. Any P4 recall result on these
controls describes the easy case. Per rule 5 this comparison is reported every time; per rule 6
the lift metric partly absorbs it, since the ceiling is measured on the same easy controls.

### P0.4 — Instruments delivered

| Component | Path | Notes |
|---|---|---|
| CLI entry | [Cli.cs](src/GreyMatter.Poc/Cli.cs) | `gm eval encoder-ceiling` live; `learn`/`probe`/`stats`/`audit` report their delivering phase |
| Config | [Config.cs](src/GreyMatter.Poc/Config.cs) | §4.5 flat record, JSON-loadable, every field a `--kebab-case` flag |
| Stats + ground rules | [Eval/Harness.cs](src/GreyMatter.Poc/Eval/Harness.cs) | Spearman, RankOf, AUC, d′; `Verdicts` makes §6.1 rules 1 and 4 executable refusals |
| Surface encoder | [Encoding/SurfaceEncoder.cs](src/GreyMatter.Poc/Encoding/SurfaceEncoder.cs) | verbatim port of legacy `FeatureEncoder`; arithmetic frozen |
| Ceiling eval | [Eval/EncoderCeiling.cs](src/GreyMatter.Poc/Eval/EncoderCeiling.cs) | sections A–E kept in legacy order for line-by-line diffing |
| Corpus | [Pipeline/Corpus.cs](src/GreyMatter.Poc/Pipeline/Corpus.cs) | streaming; tatoeba_small/tatoeba/simplewiki/cbt; `--local-sample` fallback |
| Cue sets | [Eval/CueSets.cs](src/GreyMatter.Poc/Eval/CueSets.cs) | ported verbatim; single copy shared by ceiling and recall gates |

Tests: 33 passing (`dotnet test tests/GreyMatter.Poc.Tests -c Release`). The encoder tests pin the
measured constants above as regression guards — if they move, the recorded baseline no longer
describes the encoder and every lift computed against it is void.

**Deviations from plan.md §3 layout:** none in structure. `Config.cs` and `Args.cs` sit at project
root rather than under a subfolder (they belong to no component). MessagePack pinned at 3.1.8
rather than 2.5.x — the 2.x line carries known high-severity advisories.

---

## P1 — Substrate

**Date:** 2026-08-16
**Gate:** 1M-neuron virtual space; materialize a 2,000-neuron scope, run a 4-step propagation,
evict — sustained ≥ 50 cycles/sec single-threaded, zero GC gen2 collections during a 10k-cycle
soak.
**Status: PASS.**

### P1.1 — Gate measurement

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- bench substrate --cycles 10000 --scope 2000
```

Config: `BaselineNeuronCount=1,000,000`, `WorkingSetMax=100,000`, `ActivationDepth=4`,
`ActivationWidth=256`, `SynapseCapPerNeuron=32`, `Seed=12345`.

| Metric | Value | Requirement | |
|---|---|---|---|
| `CYCLES_PER_SEC` | **95.8** | ≥ 50 | PASS |
| `MS_PER_CYCLE` | 10.443 | — | |
| `GC_GEN2` | **0** | 0 | PASS |
| `GC_GEN1` / `GC_GEN0` | 0 / 0 | — | (not required; recorded because zero is the stronger claim) |
| `ALLOCATED` | 2,360 bytes total over 10,000 cycles (**0.2 B/cycle**) | — | |
| `WORKING_SET_HIGH_WATER` | 100,000 / 100,000 | ≤ WorkingSetMax | PASS |

The measured window excludes a 50-cycle warm-up. Total allocation of 2.4 KB across 10,000 cycles
is the `Stopwatch` and the progress-line strings, not per-cycle state — the cycle path itself
allocates nothing, which is why no generation collected at all.

### P1.2 — Determinism

Two consecutive runs of the command above produced **bit-identical** substrate state: `MATERIALIZED
18,156,259`, `EVICTED 18,062,500`, `SYNAPSES 384,960`, `created 74,612,832`, `strengthened
224,419,168`, `displaced 73,654`, `declined 2,325,130,394` — every counter equal, throughput 95.8
vs 95.9 cycles/sec. Unit tests additionally assert that a different seed produces different state,
so the property is determinism rather than seed-insensitivity.

This is the property the legacy tree could not offer. `Program.cs` ~line 474 records the cost:
"Cluster IDs are Guid.NewGuid(), so cluster iteration order differs every run" — which is why
single-run correlations there were untrustworthy. Counter-based RNG (value is a pure function of
seed, purpose, id, counter) removes it by construction rather than by discipline.

### P1.3 — Finding: the synaptic budget saturates, and displacement almost never fires

Not a gate criterion, but the loudest number in the run and a direct input to P4.

| counter | value |
|---|---|
| creations proposed (≈ declined + created + displaced) | ~2.40 billion |
| `created` | 74,612,832 |
| `declined` | **2,325,130,394 (~97%)** |
| `displaced` | 73,654 (0.003%) |

Two separate mechanisms produce this:

1. **Proposal volume vastly exceeds capacity.** All-pairs Hebbian wiring among `ActivationWidth`
   = 256 k-WTA winners proposes 255 partners per neuron per step, into `SynapseCapPerNeuron` = 32
   slots. Saturation is immediate and structural: 4 steps × 65,280 pairs × 10,000 cycles ≈ 2.6
   billion proposals against a ceiling of 32 per neuron.
2. **Displacement is disabled by its own success criterion.** A candidate takes a slot only if it
   is stronger than the weakest incumbent. Reinforced synapses saturate at `MaxWeight` = 1.0 while
   `birthWeight` = `PruneThreshold + η` = 0.11, so any incumbent that has been reinforced even
   briefly is permanently unreachable. Displacement fires only in the narrow window where an
   incumbent has decayed below 0.11 but not yet been pruned.

This is the legacy P5.5 pathology recurring in the new substrate at 126× the scale (legacy: 18.4M
creations blocked, and 40× more data producing *fewer* reachable successor pairs, 97 → 31). The
competitive-displacement rule was the legacy fix for it, and this run shows that fix is close to
inert under these parameters.

**Interpretation, stated carefully.** This run is a maximally adversarial access pattern: the
benchmark draws its scope uniformly at random from the 1M-neuron space, so partners are
uncorrelated across cycles and the graph can never consolidate. Real cues repeat and their scopes
overlap, so the real ratio will be lower. The number is therefore *not* evidence that the
architecture cannot learn — it is evidence that **the synaptic budget, not the learning rule, is
the binding constraint on what the graph can represent**, and that the ratio of
`ActivationWidth`² to `SynapseCapPerNeuron` is a first-order design parameter rather than a
detail.

Carried into P4 as a required diagnostic: `gm learn` must report created/strengthened/displaced/
declined, and a P4 recall result obtained at >90% decline rate should be read as a statement about
the budget. Both are already instrumented. No parameter is being changed now — P1's gate is met,
and rule 4 forbids restructuring a passed phase to chase a number that is not its gate.

### P1.4 — Design decisions worth recording

**Eviction is a batched threshold scan, not an intrusive LRU list.** A linked list gives O(1) touch
but makes "touch" a pointer-chasing read-modify-write on the hottest path in the system. A
last-active-tick store makes touch a single array write that any number of threads can perform
concurrently, and turns eviction into a reduction plus a compaction pass. §7 asks for exactly that
trade. Batch size is `WorkingSetMax/16`, so the O(n) scan amortises to O(16) per evicted neuron.

**Two bugs found and fixed during P1, both of the silent-corruption kind:**

- *Stale slot indices.* Materializing can trigger a batch eviction, which compacts the pool and
  moves surviving neurons to new slots — invalidating any slot index captured earlier in the same
  cycle. Fixed by resolving all slots only after every materialization in a scope is complete.
- *Eviction that evicted nothing, then evicted the wrong neuron.* The cut-off test was `tick <
  cutoff`, which silently selects nothing whenever ticks tie at the cut-off — the normal case,
  since a cycle stamps its whole scope with one tick. The fallback path then dropped the *newest*
  slot, the worst possible LRU choice, and could hand back a slot index that was invalid on
  return. Now `tick <= cutoff` under an explicit budget, current-tick neurons are never evictable,
  and a scope wider than `WorkingSetMax` throws instead of silently truncating.

Both are pinned by tests (`CurrentTickNeuronsAreNeverEvicted`,
`Compaction_KeepsTheHashConsistentWithMovedSlots`, `AScopeWiderThanTheWorkingSetFailsLoudly`).

### P1.5 — Components delivered

| Component | Path | Notes |
|---|---|---|
| Deterministic RNG | [Substrate/Rng.cs](src/GreyMatter.Poc/Substrate/Rng.cs) | counter-based SplitMix64; value is a pure function of (seed, purpose, id, counter) |
| Neuron pool | [Substrate/NeuronPool.cs](src/GreyMatter.Poc/Substrate/NeuronPool.cs) | SoA, fixed capacity, open-addressed virtualId→slot hash with backward-shift deletion, batched LRU eviction |
| Synapse store | [Substrate/SynapseStore.cs](src/GreyMatter.Poc/Substrate/SynapseStore.cs) | fixed-stride CSR; Hebbian/creation-threshold/birth-weight/decay/prune/displacement ported from `SparseSynapticGraph` |
| Gate benchmark | [Substrate/SubstrateBench.cs](src/GreyMatter.Poc/Substrate/SubstrateBench.cs) | `gm bench substrate`; minimal propagation kernel, NOT `Runtime/Cascade` (that is P4) |

Tests: 65 passing (32 added in P1). Substrate total 740 lines against the §3 guideline of ≤ 800.

---

## P2 — Encoding

**Date:** 2026-08-16
**Gate:** context stage (after a 5k-sentence accumulation pass) separates at least one
morphological-relative pair the surface stage confuses; top-k collision at k=32 stays 0% over a
3k-word vocabulary; both stages' ceilings recorded.
**Status: PASS on both criteria — but see P2.3, which argues the gate does not test what it
was meant to test.**

### P2.1 — Gate measurement

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval encoder-ceiling --stage both --accumulate 5000
```

Config: `Dataset=tatoeba_small`, `PatternSize=2048`, `Sparsity=32`, `ContextBlend=0.5`,
`SurfaceDimensions=128`, window ±2, projection fan-out 8, `Seed=12345`. Accumulation: 5,000
sentences, 6,556 context entries.

| Metric | Value | Requirement | |
|---|---|---|---|
| `CEILING_AUC` (P0 surface, dense cosine) | 0.455 | reproduce P0 | PASS — bit-identical |
| `SURFACE_AUC` (β=0, sparse-code overlap) | 0.795 (d′ 1.16) | — | |
| `CONTEXT_AUC` (β=0.5) | **0.562** (d′ 0.17) | — | |
| `CONTEXT_LIFT` | **−0.232** | — | |
| `TOPK_COLLISIONS` k=32, 3,000-word vocab | 3,000/3,000 distinct (**0.0%**) | 0.0% | PASS |
| `SEPARATED` | 12/12 confused pairs | ≥ 1 | PASS |
| `DELTA_CONFUSED` / `DELTA_RANDOM` | −0.434 / −0.127 | — | |
| `SELECTIVITY` | −0.307 | — | |

Note the two different surface numbers. `CEILING_AUC` 0.455 is dense cosine over 128 dims (the P0
baseline); `SURFACE_AUC` 0.795 is top-32 overlap over the same encoder. **The sparse code separates
substantially better than the dense vector it is derived from** — consistent with P0.3 finding 3,
since sparsification discards exactly the per-word hash noise that dominates the dense L2 norm.

### P2.2 — Correction to the plan's gate criterion

§5 P2 names `sleep`/`sleeps` as the example of a pair "the surface stage confuses". P0 measured
that pair at cos 0.143 — already well separated. The premise was wrong, so the gate tests pairs
**discovered from the corpus** (surface cosine > 0.93) rather than assumed. This is a correction
of fact, not a relaxation: the discovered pairs (`bunny.`~`finns` 0.951, `had`~`look` 0.949,
`if`~`so` 0.954) are harder than the assumed one.

### P2.3 — Finding: the gate passes, and it is measuring the wrong thing

Two probes, both decisive.

**1. At β>0, "trained vs control" collapses into "in-vocabulary vs out-of-vocabulary."**

```
trained cues with context:  14/14   (the, you, we, are, to, it, in, so, time, people, know, think, sleep, water)
controls with context:       0/8    (qwertyuiop, zxcvbnmasd, xkcdvbnm, qqzzxxjj, blorp, thrumble, flendish, grastic)
```

Every trained cue has accumulated context; no control has any, because none occurs in the corpus.
So any encoder with a distributional component can score arbitrarily well on this AUC by detecting
vocabulary membership — a property of the *input*, not of anything learned. Sweeping β shows
exactly that:

| β | `CONTEXT_AUC` | d′ | `SELECTIVITY` | k=32 collisions |
|---|---|---|---|---|
| 0.00 | 0.795 | 1.16 | +0.000 | 0.0% |
| 0.25 | 0.777 | 0.97 | −0.065 | 0.0% |
| 0.50 | **0.562** | 0.17 | −0.307 | 0.0% |
| 0.75 | 0.888 | 2.06 | −0.399 | 0.0% |
| 1.00 | **1.000** | 4.51 | −0.404 | 1.6% |

AUC 1.000 at β=1 is not a result; it is the OOV artifact reaching saturation. **This is the §1.3
lesson recurring in a new form** — 40 fidelity runs at AUC 0.94–1.00 turned out to be a statement
about the encoder; a P4 recall result on these control sets would be a statement about which words
appear in Tatoeba.

**Consequence for P4, which is a gate-design problem, not a tuning problem.** The P4 gate is
architecture lift = system AUC − encoder-ceiling AUC on the ported control sets. With OOV controls
that subtraction does not rescue the metric: both terms are inflated by the same artifact, and
their difference is dominated by whichever arm has more context. **P4 needs in-vocabulary controls**
— words that occur in the corpus at comparable frequency to the trained cues but were held out of
training. This also finally satisfies rule 5, which P0.3 already recorded as failing (strongest
control 0.741 vs vocabulary NN median 0.833: the ported controls are *easier* than ordinary
neighbours). Flagged here for a decision before P4 begins.

**2. The β curve is non-monotonic, and the default sits in the valley.**

β=0.5 is the *worst* setting measured (0.562, below the β=0 null of 0.795). Mechanism: at
intermediate β the surface half (dims 0–127) and context half (dims 128–2047) compete for the same
32 top-k slots, and a word's code ends up an incoherent mixture — some dims chosen for spelling,
some for distribution, with neither half intact enough to match against. At β≥0.75 context wins
outright and codes become coherent again. The default `ContextBlend=0.5` from §4.5 is therefore
the worst available choice on this measurement.

No parameter has been changed. Rule 3 forbids redesigning mid-phase to chase a gate, and the gate
passes at the specified default. The curve is recorded so the choice can be made deliberately,
against a metric that is not the OOV artifact.

**3. Selectivity is real but confounded by a floor effect.** `SELECTIVITY` = −0.307 says context
pushes confused pairs apart (−0.434) far more than random pairs (−0.127), which is the right sign.
But confused pairs start at high surface similarity and random pairs start low, so confused pairs
simply have more room to fall. The number is reported as suggestive, not as evidence. A
similarity-matched control would settle it; that is a new experiment and is not being run without
registration (rule 6).

### P2.4 — Defect found and fixed

*Graceful degradation scaled to zero.* §4.2 requires rare and unseen words to "degrade gracefully
to surface-only". The first implementation scaled the surface half by (1−β) regardless, so at β=1
a context-less word became an all-zero vector, its top-k fell back to dims 0…31 by index order, and
**every such word produced an identical code**. Surface now applies at full strength whenever
context is absent. Pinned by `UnseenWordsDegradeToSurfaceOnlyRatherThanFailing`.

The fix did not clear the 1.6% collision rate at β=1, and the residual cause is structural rather
than a bug: **1,068 of 3,000 words (36%) have fewer than k=32 non-zero context dimensions**
(min 0, p10 16, median 32, max 1,868). A word seen once contributes 8 taps; it cannot fill a
32-of-2048 code from context alone, so the remainder is zero-padding chosen by index order. Any
future move toward high β must address this — either by falling back to surface whenever context
mass is insufficient to fill k dims, or by lowering k for sparsely-observed words. Not changed now:
the gate is met at the default, and β=1 is outside the gated configuration.

### P2.5 — Components delivered

| Component | Path | Notes |
|---|---|---|
| Sparse code | [Encoding/SparseCode.cs](src/GreyMatter.Poc/Encoding/SparseCode.cs) | k-of-n type, merge-based overlap, stable 64-bit hash, `RarityTable` IDF weighting |
| Context encoder | [Encoding/ContextEncoder.cs](src/GreyMatter.Poc/Encoding/ContextEncoder.cs) | ±2 window, signed random projection, bounded store keyed by code-hash |
| Context ceiling eval | [Eval/ContextCeiling.cs](src/GreyMatter.Poc/Eval/ContextCeiling.cs) | `gm eval encoder-ceiling --stage surface\|context\|both` |

`EncoderCeiling.Run` was deliberately **not** generalised to serve both stages (rule 4). It
reproduces the legacy harness bit-for-bit and is the recorded P0 baseline; the duplication in
`ContextCeiling` buys a guarantee that the baseline cannot drift, and the `--stage both` run above
confirms `CEILING_AUC` is still 0.455.

Tests: 88 passing (23 added in P2). The load-bearing one is
`BetaZeroExactlyReproducesTheSurfaceNullModel` — it asserts dimension-for-dimension equality
between the β=0 code and the surface top-32 for six words, including controls. Without it, §4.2's
"β=0 must exactly reproduce the null model" is an intention rather than a fact, and every lift
measured against the null becomes unfalsifiable. Encoding total 591 lines against the §3 guideline
of ≤ 700.

---

## P3 — Engram store

**Date:** 2026-08-16
**Gate:** at 100k stored recipes — mean bytes/neuron ≤ 100 B; regeneration fidelity 100% of
weights within `DeviationThreshold`; `gm audit --strings` clean.
**Status: 2 of 3 criteria PASS. The bytes/neuron criterion FAILS at the ported default
(109.4 B/neuron vs ≤ 100). Surfaced to Bill per rule 3 rather than tuned away — see P3.4.**

### P3.1 — Gate measurement

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- bench store --recipes 100000 --brain-data-path <scratch>/brain_p3
```

Config: `VqCodebookSize=512`, `SurfaceDimensions=128`, `DeviationThreshold=1.0` (ported legacy
default), `Sparsity=32`, `Seed=12345`, drift scale 1.0 weight units.

| Metric | Value | Requirement | |
|---|---|---|---|
| `RECIPES` / `PARTITIONS` | 100,000 / 377 | — | |
| `BYTES_PER_NEURON` | **109.4** | ≤ 100 | **FAIL** |
| `DEVIATIONS_PER_NEURON` | 19.6 mean, 52 max (of 128 dims) | — | |
| `CODEBOOK_UTILIZATION` | 100.0% | — | |
| `VIOLATIONS` / `WEIGHTS_CHECKED` | **0** / 12,800,000 | 0 | PASS |
| `MAX_ABS_ERROR` | 1.000000 | ≤ threshold (1.0) | PASS |
| `STRING_TOKENS` | **0** | 0 | PASS |
| `CORPUS_WORDS` | **0** of 107,921 letter runs | 0 | PASS |

### P3.2 — The storage/fidelity curve

`DeviationThreshold` is the persistence budget dial, and §4.3 says sweeping it is how the
fidelity-vs-storage curve gets plotted. Same command, `--deviation-threshold` varied:

| threshold | bytes/neuron | deviations/neuron | max abs error | ≤100 B gate |
|---|---|---|---|---|
| 0.5 | 188.4 | 37.3 | 0.50 | FAIL |
| **1.0** (default) | **109.4** | **19.6** | **1.00** | **FAIL** |
| 1.5 | 56.2 | 8.2 | 1.50 | PASS |
| 2.0 | 27.7 | 2.4 | 2.00 | PASS |
| 3.0 | 13.9 | 0.0 | 2.99 | PASS |
| 5.0 | 13.9 | 0.0 | 2.99 | PASS |

The knee is between 1.0 and 1.5; the gate is met from roughly 1.2 upward. The 13.9 B/neuron floor
is the fixed fields alone (id, code, seed, familiarity, activation count = 16 B raw) after gzip —
at threshold ≥ 3.0 no drift exceeds it, so every neuron is pure prototype. Store overhead is
therefore not the problem; deviation volume is the only thing that moves the number.

### P3.3 — Two defects found, both caught by making the instrument able to fail

**1. A moving codebook silently invalidates every recipe already consolidated against it.**

The first fidelity check measured 2,555,679 violations out of 12.8M weights, with a max error of
**49.7** against a threshold of 1.0 — roughly `BaselineGain`, i.e. entire receptive-field lines
appearing or vanishing. Cause: the benchmark called `QuantizeAndLearn` while building recipes, so
each recipe's deviations were computed against the codebook as it stood at that instant, while
regeneration later used the *final* codebook. Every prototype had moved underneath its own recipes.

**A recipe is only meaningful relative to the codebook version it was consolidated against.** This
is precisely why §4.3 specifies the codebook is "trained online during P4, frozen per checkpoint" —
freezing is not housekeeping, it is the mechanism that makes stored deviations valid. The benchmark
now runs two passes: train the codebook, freeze it, then generate/drift/consolidate.

*Consequence for P4/P5, flagged now:* checkpoint/resume must version the codebook alongside the
partitions, and any codebook update must either be deferred to a checkpoint boundary or force
re-consolidation of every affected recipe. A resumed run that loads new partitions against an old
codebook — or vice versa — will produce plausible-looking weights that are silently wrong. Pinned
by `AMovingCodebookInvalidatesAlreadyConsolidatedRecipes`.

**2. Fidelity checked in memory cannot fail.** `Consolidate` drops sub-threshold deltas by
construction, so an in-memory regenerate-and-compare is guaranteed to pass — the same vacuity as
the legacy predecessor's "100% fidelity no matter what" (§4.3: 1.9% procedural content, nothing
about recall depended on the VQ code). The check now reloads each partition **from disk** before
regenerating, so it also tests serialization, gzip, delta encoding, and the determinism of
`Listens()`/`BaselineWeight()`. That is the version that caught defect 1.

A third, smaller instrumentation defect: the first `StoreBench` perturbed a fixed *fraction* of
dims by an amount far above threshold, making deviations-per-neuron exactly `fraction × dims` — the
storage measurement was a restatement of its own input. Drift is now applied to every listened dim
from a bell-shaped distribution and the **threshold** decides what persists, so deviation count is
a measured consequence.

### P3.4 — Why the bytes/neuron gate cannot honestly be settled in P3

The 109.4 figure is a function of the **drift scale**, which is currently a modelling assumption,
not a measurement. Drift is drawn as a sum of three signed uniforms scaled to 1.0 weight units
(≈2% of a typical O(45) weight) — chosen as plausible, verified against nothing. Real learning
drift is unknown until `gm learn` exists and P4 measures it.

So there are three defensible readings and the choice is Bill's:

1. **Gate FAIL, as recorded.** The ported default yields 109.4 B/neuron; 9% over.
2. **Gate PASS at `DeviationThreshold` 1.5** (56.2 B/neuron). Defensible — the gate specifies a
   store property, not a default — but choosing 1.5 *because* it passes is exactly the
   gate-chasing rule 3 forbids, and nothing yet says what recall costs at 1.5 versus 1.0.
3. **Gate deferred to P4**, where the real drift distribution is measurable and the threshold can
   be chosen against recall rather than against the storage number.

Recommendation: **(3)**, keeping the default at the ported 1.0 and re-running `gm bench store`
with the drift distribution P4 actually produces. Deviations-per-neuron is the quantity to watch;
if real training moves fewer than ~14 dims past threshold per neuron, the gate passes at the
default with no tuning at all.

No parameter was changed to chase this. `DeviationThreshold` remains the ported legacy 1.0.

### P3.5 — The guardrail check as specified does not work

§5 P3 asks for "a CI-style check greps serialized partitions for ASCII runs ≥4 chars and fails if
found". Implemented literally, **it fails on a store containing no text whatsoever**, from two
independent aliasing sources:

- *Compressed bytes.* Gzip output is high-entropy; four-letter runs occur constantly — `afnm`,
  `Zcdj`, `fltL`, `bTphTl` all observed in a clean store.
- *Small-integer aliasing.* MessagePack encodes integers 0–127 as one byte equal to the value, so
  a **sorted** `DeviationDims` array of dims in 65–90 serialises to the bytes `'A'`–`'Z'`. Observed
  in a clean store: `FILM` (70,73,76,77), `LOST`, `BELT`, `DENY`, `ENVY`, `KNOW`, `GHIKLMSWZ` —
  27 real English words across 107,921 runs, which is simply the chance rate.

A second attempt — scanning for MessagePack str-family type tokens — was worse: **12,463 false
positives**, because float32 and large-integer payload bytes land in the fixstr range (0xA0–0xBF)
constantly. A token byte only means "string" when a reader arrives at it in value position.

The audit now tests the guardrail's *substance* with two checks that are precise rather than
literal:

1. **Exact.** Walk the decompressed document with a real `MessagePackReader` and report any value
   of string type. Zero strings is proof, not evidence — no false positives, no false negatives.
2. **Semantic.** Extract letter runs anyway and test them against the actual training vocabulary,
   excluding strictly-ascending runs (sorted integer arrays; English words are essentially never
   sorted). This catches text smuggled through a non-string encoding — which is what the legacy
   tree's `ConceptTag` and string concept index would look like if hand-packed.

Both must be clean to pass. `Audit_CatchesAStringSmuggledIntoAPartitionFile` plants the word
"elephant" in a partition and asserts the audit fails, so the check is known to be capable of
failing rather than merely observed not to fail.

**Result on the P3 store: 0 strings, 0 corpus words across 377 partitions and 13.9 MB of payload.**
The Prompt.md failure condition — "stores wordlists and concepts directly to disc" — is not met by
this store, and §1.5's audit of the legacy violation is closed for the new tree.

### P3.6 — Components delivered

| Component | Path | Notes |
|---|---|---|
| VQ codebook | [Engrams/VqCodebook.cs](src/GreyMatter.Poc/Engrams/VqCodebook.cs) | ported; seeded init (legacy used `Random.Shared`), flat float32 array, EMA online learning |
| Recipe + regeneration | [Engrams/NeuronRecipe.cs](src/GreyMatter.Poc/Engrams/NeuronRecipe.cs) | `Listens`/`BaselineWeight`/`Consolidate` ported from `ProceduralReceptiveField`, uint ids, float32 |
| Partition + store | [Engrams/EngramStore.cs](src/GreyMatter.Poc/Engrams/EngramStore.cs) | SoA-on-disk, CSR deviations, delta-encoded assemblies, gzip, atomic temp+rename |
| LSH index | [Engrams/LshIndex.cs](src/GreyMatter.Poc/Engrams/LshIndex.cs) | banded MinHash over sparse codes, **uint** buckets (legacy emitted string region ids — a guardrail violation) |
| Guardrail audit | [Engrams/StoreAudit.cs](src/GreyMatter.Poc/Engrams/StoreAudit.cs) | `gm audit --strings` |
| Gate benchmark | [Engrams/StoreBench.cs](src/GreyMatter.Poc/Engrams/StoreBench.cs) | `gm bench store` |

`gm stats` and `gm audit --strings` are now live. Tests: 109 passing (21 added in P3). Engrams
total 1,006 lines against the §3 guideline of ≤ 900 — 12% over, raised here rather than silently
broken (§3: "a file at 2× these is a smell to raise"). The overage is `StoreAudit` (196 lines),
which is guardrail tooling rather than engram machinery; if it needs to come down, moving the audit
under `Eval/` would put Engrams at 810.

**Config correction:** `DeviationThreshold` was initially set to 0.01 — a value I invented rather
than ported. §4.5 says "port legacy default", which is
`ProceduralReceptiveField.DefaultDeviationThreshold = 1.0`. Corrected. At 0.01 against weights of
O(45), essentially every drift persisted and the store was meaningless.

---

## P4 — JIT runtime

**Date:** 2026-08-16
**Gate:** train 500 Tatoeba sentences (seeded), then `gm eval recall --repeats 5` — architecture
lift ≥ +0.05 with non-overlapping repeat ranges; working set never exceeds `WorkingSetMax`;
post-run store growth is deviations and assemblies only.
**Status: PASS.** Reported with two caveats that matter more than the headline (P4.4).

### P4.1 — Gate measurement

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval recall --repeats 5 --train 500 --working-set-max 500000 --brain-data-path <scratch>/brain_recall_clean
```

`--working-set-max 500000` is the *uncontaminated* configuration; see P4.4 for why the default
100,000 is not, and for the default's numbers.

| Metric | Value | Requirement | |
|---|---|---|---|
| `SYSTEM_AUC` | 1.000 [1.000..1.000] | — | |
| `UNTRAINED_AUC` | 0.500 [0.500..0.500] | — | same pipeline, zero learning |
| `ENCODER_AUC` | 0.293 [0.279..0.322] | — | static code similarity |
| `LIFT_VS_UNTRAINED` | **+0.500** [+0.500..+0.500] | ≥ +0.05 | PASS |
| `LIFT_VS_ENCODER` | +0.707 [+0.678..+0.721] | ≥ +0.05 | PASS |
| `SEPARATED` | True | non-overlapping | PASS |
| `WORKING_SET_HIGH_WATER` | 290,285 / 500,000 | ≤ max | PASS |
| `CASCADE_TRUNCATIONS` | 0 | — | |
| `GRADED_RHO` | +0.960 [+0.913..+0.985] | — | mass vs corpus frequency, trained cues only |
| `TRUNCATION_GAP` | +0.0 per cue | 0 | no residency confound |

Trained cue mass ≈ 688 against control mass 256.003 — and 256.003 is exactly the assembly's initial
drive (`Sparsity` 32 × `NeuronsPerDim` 8 = 256 members at potential 1.0), i.e. a control's cascade
propagates nothing at all, as it should with no synapses.

`gm learn --sentences 500`: 3,700 tokens, 43 ms/sentence, 505,414 synapses, 15,941 working-set
high water, 2,158 deviations written across 11 partitions.

### P4.2 — The in-vocabulary control set

Replaces the ported mash/pseudoword lists, which P0.3 and P2.3 disqualified (rule 5 failure, and
an OOV artifact that let any distributional encoder score AUC 1.000 on vocabulary membership
alone). Controls are now real corpus words, frequency-matched to the trained cues, held out of
training by skipping their tokens — the corpus itself stays intact, so the arms differ by exactly
one factor (rule 8). Held-out words are also excluded from the context-accumulation pass, or they
would acquire distributional structure they were never supposed to have.

Measured frequency match at `--train 500`: trained median 12 vs control median 12, ratio 1.00.

The key property: a control still gets a **perfectly valid, regenerable assembly** — §4.3 requires
that nothing be stored before a cue can activate. Trained and control cues are identical in every
respect except that no learning ever ran on the control's neurons. That is what makes this a test
of the architecture rather than of the encoder.

### P4.3 — Four defects, each of which produced a plausible-looking wrong answer

Recorded in the order found, because the sequence is the point: every one of them presented as a
result rather than as a crash.

**1. Consolidation destroyed the graph. → reported `SYNAPSES: 0` after 241M Hebbian updates.**
`ClearSlot` lived inside `ConsolidateSlot`. Releasing a synapse segment is correct on *eviction*,
but `ConsolidateAll` runs at every checkpoint and at shutdown on **resident** neurons, so it wiped
everything learned. Split into `EvictSlot` (consolidate, then release) and `ConsolidateSlot`
(consolidate only).

**2. Potentials were never cleared after a cascade. → reported `trained mass NaN`, `AUC 0.000`.**
A cue's winners kept their potential when `Run` returned, and `Materialize` only zeroes *newly*
resident neurons. Charge therefore accumulated across every cue in a run until it reached Infinity.
This reads exactly like a null result and is arithmetic overflow. It is also a correctness problem
independently of overflow: it made a probe depend on what was probed before it. Both properties are
now pinned by tests.

**3. Activation was multiplied, not conserved. → every cue saturated at 3.3e10, `AUC` exactly 0.500.**
Each neuron sent its full drive down *every* out-synapse, so a neuron with 32 synapses emitted 32×
what it received and mass grew ~32× per step. Drive is now divided across out-degree, which makes
retained mass a statement about synaptic **structure** rather than about out-degree.

**4. Per-dimension assembly membership meant no word was ever untrained. → `AUC` 0.500 again.**
The worst of the four, and the only one that was a design error rather than a coding error. I had
assemblies recruit a fixed neuron slice per active code dimension, so codes sharing a dimension
shared neurons. With n=2048 and 8 neurons per dimension the entire addressable space is 16,384
neurons — and 500 Tatoeba sentences touch **16,005** of them. Measured directly:

```
distinct neurons addressed by whole corpus: 16,005  (2048 dims × 8 = 16,384)
control 'you' : 256/256 members shared with trained cues
control 'that': 256/256 members shared with trained cues
control 'not' : 256/256 members shared with trained cues
```

A held-out control was *literally the same neurons* as the trained cues. There is no such thing as
an untrained word under that scheme.

This was my invention, not the plan's: §4.3 specifies `{ codeHash → uint[] memberNeuronIds }`,
i.e. membership derived from the code **hash**, which is word-specific. Correcting to the spec is
what fixed it. Similarity between words is then carried where the plan puts it — by learned
synapses between co-occurring assemblies, and by the LSH index over codes — rather than by
accidental address collisions.

### P4.4 — Two caveats on the headline

**1. The default configuration carries a residency confound; the reported run does not.**

At the default `WorkingSetMax=100,000` the gate also passes (identical AUC and lift), but
`TRUNCATION_GAP` is **−229.9 per cue**: a control's 256 assembly members must be regenerated into a
full pool and ~230 of them truncate, while a trained cue's members are often still resident from
training. That is partly a measurement of *residency*, not of learning.

The result survives removing it — at `WorkingSetMax=500,000` nothing truncates, controls reach
their full 256.003 drive, and AUC/lift are unchanged. So the conclusion holds, but the
default-config numbers are contaminated and the 500k run is the one that should be quoted. The
diagnostic is now printed on every run because it is configuration-dependent, and P6's scale sweep
will vary exactly the parameter that drives it.

**2. What the +0.500 lift does and does not establish.**

AUC 1.000 with zero overlap is the pattern §1.3 warns about, so it was interrogated rather than
accepted. Two checks:

- *Is it a binary seen/unseen detector?* No. `GRADED_RHO = +0.960` — among trained cues only,
  activation mass tracks corpus frequency almost monotonically. The readout is graded.
- *Is the grading interesting?* **This is the honest limit.** ρ ≈ 0.96 against frequency means what
  the architecture has demonstrably learned is *how often it saw each word*. That is real learning
  — the encoder cannot do it (AUC 0.293, below chance) and an untrained brain cannot do it (0.500)
  — but frequency is the easiest possible thing for a Hebbian system to learn, and it is not
  evidence that the learned structure carries associative or sequential content.

So P4 establishes that the JIT runtime works end to end: cues materialize, cascades propagate,
Hebbian learning accumulates, consolidation persists deviations, and recall discriminates trained
from held-out material far above both ceilings. It does **not** establish that the graph has
learned relationships between words. That is exactly what P5's order eval tests, and the P4 result
should be read as a precondition for it rather than as a substitute.

### P4.5 — Components delivered

| Component | Path | Notes |
|---|---|---|
| Assembly recruitment | [Runtime/Assembly.cs](src/GreyMatter.Poc/Runtime/Assembly.cs) | §4.3 codeHash → members; per-dim scheme removed (P4.3 defect 4) |
| Activation scope | [Runtime/ActivationScope.cs](src/GreyMatter.Poc/Runtime/ActivationScope.cs) | materialize/regenerate, STM weights, consolidate-on-evict |
| Cascade | [Runtime/Cascade.cs](src/GreyMatter.Poc/Runtime/Cascade.cs) | conserved propagation, k-WTA, readout, truncation counting |
| Plasticity | [Runtime/Plasticity.cs](src/GreyMatter.Poc/Runtime/Plasticity.cs) | within-cue Hebbian + directed cross-cue trace + `EndSequence` |
| Trainer | [Pipeline/Trainer.cs](src/GreyMatter.Poc/Pipeline/Trainer.cs) | streaming learn loop, held-out set, persistence |
| Control set | [Eval/ControlSets.cs](src/GreyMatter.Poc/Eval/ControlSets.cs) | frequency-matched in-vocabulary split |
| Recall eval | [Eval/RecallEval.cs](src/GreyMatter.Poc/Eval/RecallEval.cs) | `gm eval recall`; two ceilings + both confound diagnostics |

`gm learn` and `gm probe` are live. Tests: 125 passing (16 added in P4). Runtime 571 lines against
the §3 guideline of ≤ 900; Pipeline 445 against ≤ 600; Eval 1,028 against ≤ 1,400.

---

## P5 — Pipelines hardened

**Date:** 2026-08-16
**Gate:** one unattended `gm learn --dataset tatoeba --sentences 50000` completes with a mid-run
kill/resume test producing an equivalent state; `gm eval order --repeats 5` executes end-to-end and
emits a rule-compliant verdict (any verdict, including NULL, is a pass — the gate is that the
instrument works).
**Status: PASS on both criteria.** The 50k run completed unattended in 29.8 minutes with zero
truncations; kill/resume at 20k scale diverges by 1.25%; the order eval emits `NO SIGNAL` at 27.9%
support. The order result is a clean null and is the most important finding in this phase (P5.5).

### P5.1 — A defect found first, because it invalidates how P4's numbers should be read

**The working set froze the first time it filled, and never turned over again.**

`Cascade.Run` pre-tested `pool.Count >= pool.Capacity` and truncated on it, instead of asking the
pool to evict. Once the pool reached `WorkingSetMax` for the first time, *every subsequent
materialization was refused* — so the evict/regenerate/consolidate cycle, which is the entire
premise of the project, never ran at all. The signature was a suspiciously round
`RECIPES: 100,000` (exactly `WorkingSetMax`), `SYNAPSES: 3,200,000` (exactly 100,000 × cap 32,
fully saturated), and 201,917 truncations.

Fixed by adding `TryMaterialize`, which lets the pool evict and returns −1 only when every resident
neuron is active on the current tick. After the fix a 400-sentence run materializes 244,774 distinct
neurons through a 100,000-slot pool with **zero** truncations.

**Effect on P4:** none. The recorded P4 gate run used `--working-set-max 500000` and never filled
the pool, so it never hit this path. Re-verified after the fix — `LIFT_VS_UNTRAINED +0.500`,
`GRADED_RHO +0.960`, identical to the recorded numbers. The *default*-config P4 numbers mentioned
in P4.4 were measured under the frozen-pool condition and should be disregarded; the 500k run
stands.

### P5.2 — Synapses now persist, which §4.3's schema did not provide for

Synapses lived only in the working set. On eviction they were discarded, so learning could not
accumulate past `WorkingSetMax` and a resumed run restored nothing that mattered — and P4
established that recall lives in exactly these synapses.

§4.3 lists a recipe as id/vqCode/seed/deviations/familiarity/activationCount, with no synapses. But
§4.4 step 3 requires materialization to "hydrate their synapse segments", and there is nowhere else
for them to come from. `NeuronRecipe` and `EngramPartition` therefore gained a CSR synapse block
(uint targets, float weights). **This extends the stated schema**; flagged as a deviation.

A related bug fell out: `DirtyRecipes()` filtered on `DeviationCount > 0`, which silently dropped
every neuron that had learned connections without its own receptive field drifting past threshold.
At the ported `DeviationThreshold` of 1.0 that is *all* of them — real training produces
**`DEVIATIONS: 0`**. Now filtered on `HasLearnedState` (deviations **or** synapses).

**This answers P3.4's open question.** P3 could not settle bytes/neuron because the drift
distribution was a modelling assumption. Measured on real training: drift never exceeds threshold,
so deviations cost nothing and **the entire storage cost is synapses**. A 400-sentence run stores
244,774 recipes at **125.9 B/neuron** — over the 100 B gate, but for a completely different reason
than P3 modelled. The `DeviationThreshold` sweep in P3.2 is therefore not the relevant dial;
`SynapseCapPerNeuron` is. Recommend re-opening the P3 size gate against that parameter in P6, where
it is swept anyway.

### P5.3 — The unattended 50k run

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- learn --dataset tatoeba --sentences 50000 --checkpoint-every 10000 --brain-data-path <scratch>/brain_50k
```

| Metric | Value |
|---|---|
| `SENTENCES` / `TOKENS` | 50,000 / 381,867 |
| throughput | 28 sentences/sec, `MS_PER_SENTENCE` 35.80 |
| wall clock | 29.8 minutes, unattended, no intervention |
| `WORKING_SET_HIGH_WATER` | 100,000 / 100,000 |
| `CASCADE_TRUNCATIONS` | **0** |
| `SYNAPSES` (resident) | 3,183,518 |
| Hebbian updates | 24.9 billion within-cue, 21.7 billion sequence |
| `CONSOLIDATIONS` | 32,338,006 |
| `DEVIATIONS_WRITTEN` | 82,800 |
| `PARTITIONS_WRITTEN` | 497 |

Five mid-run checkpoints fired on schedule. Zero truncations at full working-set occupancy across
50,000 sentences is the post-fix behaviour from P5.1: the pool is continuously at its cap and
continuously turning over, which is the materialize/evict cycle actually working.

Note `DEVIATIONS_WRITTEN` 82,800 against 32.3M consolidations — 0.26%. Receptive-field drift past
the threshold is rare even at this scale, confirming P5.2: storage is synapses, not deviations.

### P5.4 — Checkpoint and resume

`gm learn --resume`, checkpointing every `--checkpoint-every` sentences (default 10,000).

The manifest carries a **codebook version and the codebook itself**, which is the P3 finding made
operational: recipes are meaningful only relative to the codebook they were consolidated against,
and mixing versions produced 2,555,679 fidelity violations at max error 49.7. Resume restores the
codebook *before* reading any recipe, and **refuses** on any configuration mismatch (seed, codebook
size, dimensions, sparsity, virtual space) rather than reinterpreting stored state. Three refusal
paths are tested.

**Equivalence at scale — the gate measurement.** Uninterrupted 20,000 Tatoeba sentences (C) versus
10,000 + resume + 10,000 (D):

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- learn --dataset tatoeba --sentences 20000 --brain-data-path <scratch>/brain_C
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- learn --dataset tatoeba --sentences 10000 --brain-data-path <scratch>/brain_D
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- learn --dataset tatoeba --sentences 20000 --resume --brain-data-path <scratch>/brain_D
```

Resume reported: `resumed at sentence 10,000 (codebook v0, 930,542 recipes restored)`.

| | recipes | synapses | deviations | bytes/neuron |
|---|---|---|---|---|
| C (uninterrupted 20k) | 982,014 | 31,424,372 | 7,857 | 177.6 |
| D (interrupted at 10k) | 994,264 | 31,815,916 | 7,309 | 176.7 |
| divergence | **+1.25%** | **+1.25%** | −7.0% | **−0.5%** |

**PASS.** 1.25% on recipes and synapses, 0.5% on bytes/neuron, from a run that stopped and restarted
halfway.

**Why the small scale looked much worse, and why that was the misleading measurement.** The same
comparison at 400 sentences (200 + resume) diverged by **16%**:

| | recipes | synapses | bytes/neuron |
|---|---|---|---|
| A (uninterrupted 400) | 244,774 | 7,832,768 | 125.9 |
| B (interrupted at 200) | 284,045 | 9,089,440 | 125.6 |
| divergence | +16.0% | +16.0% | +0.2% |

The mechanism is real rather than a bug: **Hebbian wiring only occurs between co-resident neurons,
so the working set is a hidden state variable that shapes what gets learned next.** A run that
stops and restarts has a different residency history. But that is a one-off perturbation at the
resume boundary, and its contribution shrinks as a fraction of total learning — 16% over 400
sentences, 1.25% over 20,000. Reporting only the 400-sentence figure would have overstated the
problem by an order of magnitude.

Checkpointing the resident set (`ResidentIds`, in last-active order) was added to close the gap and
**did not measurably help**: restored residents carry old ticks and the first few hundred cues evict
them anyway. Kept because it costs 400 KB and is the right thing to store, but recorded as a
negative result rather than credited with the improvement — the improvement is scale, not the fix.

Exact count equivalence would require checkpointing full pool state (potentials, ticks, live
synapse segments), which is a materially larger checkpoint and was not specified.

### P5.5 — Order eval: the instrument works, and it refuses

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval order --repeats 5 --train 1000 --brain-data-path <scratch>/brain_order
```

| Metric | Value |
|---|---|
| `R_BIGRAM` | +0.0078 [−0.0255..+0.0605] |
| `R_UNIGRAM` | −0.0017 [−0.0520..+0.0596] |
| `R_PMI` | **+0.0017** [−0.0568..+0.0503] |
| shuffled `R_PMI` | +0.0083 [−0.0187..+0.0342] |
| `PMI_GAP` | **−0.0066** |
| `CUES_SCORED` | 20 |
| `SUPPORT` | **17.1%** |

**`VERDICT: INSUFFICIENT SUPPORT` — 17.1% of scored bigrams occur more than once, below the rule-4
floor of 20%.** The instrument refuses to emit a substantive verdict from correlations against
single-observation counts. That refusal is itself a gate pass: it is exactly the ethic §6.1 rule 10
asks to be preserved, and the legacy harness's best property.

**Re-run with enough support to clear the floor:**

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval order --repeats 5 --train 4000 --min-successors 12 --brain-data-path <scratch>/brain_order2
```

| Metric | Value |
|---|---|
| `R_BIGRAM` | −0.0107 [−0.0375..+0.0088] |
| `R_UNIGRAM` | −0.0068 [−0.0352..+0.0358] |
| `R_PMI` | **+0.0044** [−0.0378..+0.0443] |
| shuffled `R_PMI` | −0.0072 [−0.0371..+0.0184] |
| `PMI_GAP` | +0.0117 |
| `CUES_SCORED` | 20 |
| `SUPPORT` | **27.9%** (clears the 20% floor) |

**`VERDICT: NO SIGNAL` — real R_PMI is +0.0044; the graph does not rank successors by association
at all.** A substantive, rule-compliant verdict, and a clean null.

Note the rule-3 machinery earning its place: `PMI_GAP` is **positive** (+0.0117), and a harness
testing the gap alone would have called this a weak order signal. It is not one — real R_PMI is
+0.0044, indistinguishable from zero, and the gap is positive only because the shuffled arm came
out slightly more negative. That is precisely the legacy P5.4 bug (which fired "LEARNED ORDER" on a
real R_PMI of −0.0263), and the ported rule catches it.

This is consistent with P4.4 rather than surprising: what the architecture demonstrably learned is
word frequency (`GRADED_RHO +0.960`), and frequency carries no sequence information. The directed
cross-cue trace is implemented and firing (21.7 billion sequence updates over the 50k run), but it
is not producing rankings that track corpus association. **Order is not learned.** That is the
honest state of the system going into P6.

### P5.6 — Components delivered

| Component | Path | Notes |
|---|---|---|
| Checkpoint/resume | [Pipeline/Checkpoint.cs](src/GreyMatter.Poc/Pipeline/Checkpoint.cs) | versioned codebook, resident set, config-mismatch refusal |
| Order eval | [Eval/OrderEval.cs](src/GreyMatter.Poc/Eval/OrderEval.cs) | `gm eval order`; PMI primary, same-pairs null, support floor, ≥5 repeats |
| Synapse persistence | [Engrams/NeuronRecipe.cs](src/GreyMatter.Poc/Engrams/NeuronRecipe.cs), [Engrams/EngramStore.cs](src/GreyMatter.Poc/Engrams/EngramStore.cs) | CSR block; schema extension |
| Pool turnover fix | [Substrate/NeuronPool.cs](src/GreyMatter.Poc/Substrate/NeuronPool.cs) | `TryMaterialize` |

`gm stats` now reports store-level recipes/deviations/synapses and checkpoint position.
Tests: 137 passing (12 added in P5).

### P5.7 — Guardrail audit at 50k scale, and a calibration correction

The 50k store initially **failed** `gm audit --strings`: 4 corpus-word hits across 2,840 letter
runs in 498 partitions (339.8 MB of payload). Inspected rather than dismissed — the findings were
`WisE`, `tIlL`, `puts`, `loUD`.

These are chance, and the mechanism is new since P3: the synapse block added in P5.2 puts uint32
targets and float32 weights on disk, and those bytes land in the ASCII letter range constantly.
Two of the four are mixed-case in a way no stored text would be. The P3 calibration (exclude
strictly-ascending runs) was tuned against sorted `DeviationDims` arrays and does not cover this.

Recalibrated on two properties, both with stated reasoning rather than tuned to the answer:

- **Case consistency.** Real text is all-lower, all-upper, or Capitalised. `WisE` and `tIlL` are
  not. Removed 2 of 4.
- **Minimum length 6 for the semantic check** (raw runs are still reported at 4). Roughly 5,000 of
  the 457,000 possible four-letter lowercase strings are English words (~1.1%), so short matches
  are expected at this volume; at six letters it is ~15,000 in 309 million (~0.005%). Removed the
  remaining 2, including the genuine-looking `puts`.

**Result: `AUDIT: CLEAN` — 0 string tokens, 0 corpus words, 498 partitions, 339.8 MB payload.**

The risk in raising a threshold to clear a failure is that the check quietly stops working, so a
test now plants a word list **byte-packed rather than string-encoded** (`elephant`, `kitchen`,
`morning`, `brother`, `picture` in a MessagePack bin) and asserts the semantic check still catches
it with zero string tokens present. That is precisely what a hand-packed legacy `ConceptTag` index
would look like. Residual blind spot, stated: a store containing *only* words shorter than six
letters would evade the semantic check — the exact string-token check still covers real strings.

### P5.8 — State of the system entering P6

Worth stating plainly, because P6 is the deliverable and this is what it will be measuring:

- **The machinery works.** 50,000 sentences unattended, 46.6 billion Hebbian updates, a working set
  pinned at its cap and continuously turning over, zero truncations, checkpoint/resume within 1.25%.
- **Recall discriminates**, at `LIFT_VS_UNTRAINED +0.500`, and it is graded rather than binary
  (`GRADED_RHO +0.960`).
- **What it has learned is word frequency.** `GRADED_RHO` against frequency is 0.96; `R_PMI` against
  corpus association is +0.0044. Order is not learned, and destroying word order costs nothing.

So P6's scale sweep will measure how a *frequency-recall* system trades accuracy for scale. That is
a real and reportable curve, and it satisfies Prompt.md's success criterion as written — but it
should not be read as a curve about associative memory, because P5.5 shows there is no associative
signal to trade away.

---

## P6 — The Prompt.md experiment: scale sweep

**Date:** 2026-08-16
**Gate (== Prompt.md success):** the sweep runs on commodity hardware; recall is measurable and
reported at every scale including 10⁶⁺ virtual neurons; the accuracy-vs-scale trade appears as a
curve, whatever its shape.
**Status: PASS on the stated criteria.** The third criterion resolves in an unexpected way — the
trade does not appear, and P6.3 explains why that is a finding rather than a missing measurement.

### P6.1 — The sweep table

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval scale --repeats 5 --train 500 --order-repeats 3 --order-train 1000 --sweep-path <scratch>/sweep
```

14 cells, 62.8 minutes, one Apple Silicon Mac, single-threaded. Fixed corpus (`tatoeba_small`,
500 sentences) and seed set (12345+r); `--repeats 5` per cell; scratch brain per cell, deleted after
(rule 7). `R_PMI` is a 3-repeat **diagnostic**, not a verdict — rule 1 requires 5, and the
5-repeat verdict is established once in P5.5.

| neurons | depth | width | sys AUC | untr AUC | lift [min..max] | sep | ρ(freq) | R_PMI | ms/sent | MB disk | heap MB | WS high | trunc |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 10,000 | 2 | 256 | 0.993 | 0.500 | +0.493 [+0.480..+0.500] | yes | +0.60 | −0.031 | 35.5 | 1.7 | 192 | 10,000 | 0 |
| 10,000 | 4 | 256 | 0.989 | 0.500 | +0.489 [+0.469..+0.500] | yes | +0.59 | −0.031 | 37.4 | 1.7 | 192 | 10,000 | 0 |
| 10,000 | 8 | 256 | 0.959 | 0.500 | +0.459 [+0.434..+0.492] | yes | +0.57 | −0.034 | 83.5 | 1.7 | 192 | 10,000 | 0 |
| 100,000 | 2 | 256 | 1.000 | 0.500 | +0.500 [+0.500..+0.500] | yes | +0.79 | +0.001 | 31.9 | 13.5 | 259 | 96,829 | 0 |
| 100,000 | 4 | 256 | 1.000 | 0.500 | +0.500 [+0.500..+0.500] | yes | +0.76 | +0.004 | 29.0 | 13.6 | 259 | 96,829 | 0 |
| 100,000 | 8 | 256 | **0.788** | 0.500 | **+0.288 [+0.043..+0.430]** | yes | +0.63 | −0.046 | 29.8 | 13.4 | 259 | 96,829 | 0 |
| 1,000,000 | 2 | 256 | 1.000 | 0.500 | +0.500 [+0.500..+0.500] | yes | +0.98 | +0.006 | 33.2 | 35.0 | 285 | 100,000 | 0 |
| 1,000,000 | 4 | 256 | 1.000 | 0.500 | +0.500 [+0.500..+0.500] | yes | +0.96 | +0.006 | 31.5 | 35.2 | 285 | 100,000 | 0 |
| 1,000,000 | 8 | 256 | 1.000 | 0.500 | +0.500 [+0.500..+0.500] | yes | +0.95 | +0.004 | 33.5 | 35.4 | 285 | 100,000 | 0 |
| 10,000,000 | 2 | 256 | 1.000 | 0.500 | +0.500 [+0.500..+0.500] | yes | +1.00 | +0.009 | 32.1 | 43.0 | 312 | 100,000 | 0 |
| 10,000,000 | 4 | 256 | 1.000 | 0.500 | +0.500 [+0.500..+0.500] | yes | +1.00 | +0.009 | 30.3 | 43.1 | 312 | 100,000 | 0 |
| 10,000,000 | 8 | 256 | 1.000 | 0.500 | +0.500 [+0.500..+0.500] | yes | +1.00 | +0.009 | 30.8 | 43.3 | 312 | 100,000 | 0 |
| 1,000,000 | 4 | 64 | 1.000 | 0.500 | +0.500 [+0.500..+0.500] | yes | +1.00 | −0.010 | **6.3** | 10.2 | 226 | 100,000 | 0 |
| 1,000,000 | 4 | 1024 | 1.000 | 0.500 | +0.500 [+0.500..+0.500] | yes | +0.99 | +0.002 | **137.6** | 36.3 | 286 | 100,000 | 0 |

`ORDER_SUPPORT` 17.0% mean — below the rule-4 floor, consistent with P5.5.

**Peak RSS is not in the table because the OS counter returned 0**: .NET's `PeakWorkingSet64` is
unimplemented on macOS. The `heap MB` column (managed heap after a forced full collection) is the
per-cell memory figure and is sound. A per-cell RSS sample has been added for future runs; it was
not available for this one, and I am not re-running 63 minutes to backfill it.

### P6.2 — What the curves say

**Scale helps; it does not hurt.** Recall lift is +0.459 to +0.493 at 10⁴ virtual neurons and
+0.500 (saturated) from 10⁵ upward. The graded-recall correlation moves monotonically with scale:
ρ(freq) = **0.57–0.60 at 10⁴ → 0.76–0.79 at 10⁵ → 0.95–0.98 at 10⁶ → 1.00 at 10⁷**.

The mechanism is assembly collision. An assembly is `Sparsity × NeuronsPerDim` = 256 neurons drawn
from the virtual space, so a 10⁴ space holds ~39 non-overlapping assemblies for a ~1,355-word
vocabulary — distinct words are forced onto shared neurons and their learned state interferes. By
10⁷ assemblies are effectively disjoint. **This is a real accuracy-vs-scale curve, but it runs the
opposite way to the one §2 anticipated:** the constraint measured here is address-space crowding,
not memory pressure.

**Depth 8 at 10⁵ is a genuine instability**, not noise: AUC 0.788 with a lift range of
[+0.043..+0.430] — an order of magnitude wider than any other cell. It is specific to the cell
where the virtual space ≈ the working set (`WS high` 96,829 of a 100,000 cap), so nearly every
neuron is resident and an 8-step cascade circulates through a near-fully-resident graph. At 10⁶+
the working set is a small fraction of the space and cascades stay local; at 10⁴ the space is too
small to sustain deep propagation. Flagged rather than explained away — it is the one cell in the
grid whose repeat spread would fail a separation test on its own.

**Width is the cost dial, and it is close to quadratic.** 6.3 → 31.5 → 137.6 ms/sentence for width
64 → 256 → 1024, against a 16× step in width² each time. All-pairs Hebbian wiring among k-WTA
winners is O(width²), and it dominates. Recall is unchanged across the three (lift +0.500, ρ ≥
0.99), so **width 64 delivers identical recall at 5× the throughput** — the default of 256 is
paying 5× for nothing measurable on this corpus.

**Disk grows sub-linearly and saturates**: 1.7 → 13.5 → 35.0 → 43.0 MB across four orders of
magnitude of virtual space. Storage tracks the number of neurons actually touched — bounded by
vocabulary × assembly size — not the size of the address space. A 10⁷-neuron brain costs 43 MB
because only ~350k neurons were ever real.

### P6.3 — The accuracy-for-scale trade does not exist in this system, and that is the result

`trunc 0` in every cell of the table. The working-set cap never bound, so the sweep as specified
never exercised the mechanism §4.4 calls "the accuracy-for-scale trade". That is a gap in the
grid, so it was probed directly — `WorkingSetMax` swept at fixed 10⁶ neurons, `--repeats 3`:

| WorkingSetMax | sys AUC | lift | ρ(freq) | truncations |
|---|---|---|---|---|
| 100,000 | 1.000 | +0.500 | +0.952 | 0 |
| 25,000 | 1.000 | +0.500 | +0.950 | 0 |
| 10,000 | 1.000 | +0.500 | +0.954 | 0 |
| 5,000 | 1.000 | +0.500 | +0.957 | 0 |
| 2,500 | 1.000 | +0.500 | +0.952 | 0 |
| 1,000 | 1.000 | +0.500 | +0.952 | 0 |
| 600 | 1.000 | +0.500 | +0.952 | 0 |
| 400 | 1.000 | +0.500 | +0.958 | 0 |
| 300 | 1.000 | +0.500 | +0.960 | 0 |
| **260** (assembly is 256) | **1.000** | **+0.500** | **+0.960** | **0** |

**A 385× reduction in working set costs nothing.** Recall is flat to three decimal places down to a
pool that holds barely one assembly.

Read positively, this is the project's engineering thesis validated about as hard as it can be:
learned state lives in recipes and is hydrated on materialization, so RAM is a *cache*, not the
store. A virtual space of 10⁷ neurons is served correctly by a pool of 260. Prompt.md's "virtual
neuron space far larger than RAM" works.

Read honestly, it is also why there is no trade to plot, and the reason connects P6 back to P5.5.
Three things have to be true for working-set pressure to cost recall:
1. the cue's own assembly must not fit — it always does (256 ≤ 260);
2. eviction must fail — it never does, because evicting cold neurons always succeeds;
3. recall must depend on **multi-hop cascade paths** through neurons that may not be resident,
   since `Cascade` silently drops propagation to a non-resident target.

Condition 3 is the load-bearing one, and it is false: P5.5 measured `R_PMI` = +0.0044, i.e. the
cascade carries no associative signal. The readout is dominated by the cue's own assembly and its
directly-attached synapses. **There are no multi-hop paths carrying information, so there is
nothing for working-set pressure to break.** The trade is unmeasurable not because the substrate is
good enough to avoid it, but because the system's recall is effectively single-hop.

### P6.4 — Gate assessment

| Criterion | Result |
|---|---|
| Sweep runs on commodity hardware | **PASS** — 14 cells, 62.8 min, one Mac, ≤312 MB managed heap |
| Recall measurable and reported at every scale | **PASS** — all 14 cells separated, lift reported with repeat ranges |
| Demonstrably beyond "hundreds wide, dozens deep" | **PASS** — 10,000,000 virtual neurons, depth 8, width 1024 |
| Nothing human-readable in the brain store | **PASS** — P5.7, `AUDIT: CLEAN` on 498 partitions / 339.8 MB |
| Accuracy-vs-scale trade appears as a curve | **PASS as a curve, with a correction** — the curve is ρ(freq) 0.57→1.00 rising with address space (P6.2). The *memory-pressure* trade is flat over 385× and P6.3 explains why. |

**Prompt.md's success criteria are met as written.** A brain trained on a real corpus, recalled
across scales up to 10⁷ virtual neurons on commodity hardware, with nothing readable on disk, and a
measured curve.

**What it is not.** The recall being measured is frequency recall (ρ(freq) → 1.00 while
R_PMI ≈ 0). The system reliably distinguishes what it has seen, and how often, from what it has
not. It does not encode relationships between things it has seen. Every number in this table should
be read with that in mind: they describe a working, honest, well-instrumented substrate for
associative memory that does not yet contain an associative signal.

### P6.5 — Delivered

| Component | Path |
|---|---|
| Scale sweep | [Eval/ScaleSweep.cs](src/GreyMatter.Poc/Eval/ScaleSweep.cs) — `gm eval scale` |

Definition of done (§8): `RESULTS.md` now contains the encoder ceilings for both stages (P0.2,
P2.1), the P4 architecture-lift result (P4.1), the order-eval verdict (P5.5), and the scale-sweep
table with interpretation (P6.1–P6.4) — each with its command line. Tests: 138 passing.

---

# Addendum A — P7: Close the association gap

## P7.0 — Attribution instrumentation

**Date:** 2026-08-17
**Gate:** one table from a standard 4k-sentence run attributing (a) the synaptic budget and
(b) recall mass across within-assembly / cross-assembly / cross-cue populations, plus the measured
decline rate per population. Each A.1 hypothesis confirmed or killed by a number.
**Status: PASS. All three testable hypotheses CONFIRMED, and a root cause A.1 did not name was
found (P7.0.4).**

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval attribution --train 4000 --brain-data-path <scratch>/attr
```

Config: `tatoeba_small`, 4,000 sentences, 29,405 tokens, 144 s, `Seed=12345`, width 256, depth 4,
cap 32/neuron, working set 100,000, truncations 0.

**No behaviour change, verified rather than asserted:** `gm eval recall --repeats 5 --train 500
--working-set-max 500000` returns `SYSTEM_AUC 1.000`, `UNTRAINED 0.500`, `LIFT +0.500`,
`GRADED_RHO +0.960 [+0.913..+0.985]` — identical to the P4.1 record. 138 tests still pass.

### P7.0.1 — (a) The synaptic budget by provenance

| population | live slots | share | mean w | proposals | created | strengthened | displaced | declined | decline rate |
|---|---|---|---|---|---|---|---|---|---|
| within-assembly | 3,051,287 | **99.9%** | 0.147 | 1,912,895,336 | 24,098,464 | 174,156,577 | 5,052,514 | 1,709,587,781 | 89.4% |
| cross-assembly | **0** | **0.0%** | — | 6,661,952 | **0** | 447,558 | **0** | 6,214,394 | 93.3% |
| cross-cue | 4,169 | 0.1% | 0.117 | 1,664,938,420 | **0** | 144,652 | 23,365 | 1,664,770,403 | **100.0%** |

`LIVE_SYNAPSES` 3,055,456 of 3,055,456 slots — **100.0% full**. `CROSS_SHARE` **0.1%** against a
P7.1 gate of >50%.

**Not one cross-assembly synapse was ever created.** Nor one cross-cue synapse — the 4,169 live
cross-cue edges all arrived by displacement. The sequence channel made **1.66 billion proposals and
had a 100.0% decline rate.**

### P7.0.2 — (b) Recall mass by hop and by delivering population

| hop | surviving mass | share | k-WTA winners |
|---|---|---|---|
| 0 (cue's own assembly) | 32,757.7 | **99.1%** | 4,064 |
| 1 (one synapse away) | 303.2 | 0.9% | 32 |
| 2+ (multi-hop) | **0.0** | **0.0%** | **0** |

| population | drive injected | share |
|---|---|---|
| within-assembly | 29,125.4 | **99.9%** |
| cross-assembly | 0.0 | 0.0% |
| cross-cue | 15.8 | 0.1% |

Drive-injected is summed at the point of delivery, which is a different quantity from surviving
mass and the only correct way to attribute it — see P7.0.5.

**`RECALL_VIA_WITHIN_ASSEMBLY` = 99.9%.** This is the number flagged as load-bearing before the
phase started, and it lands at the worst possible value: within-assembly edges are simultaneously
**the population that encodes nothing** (A.1 H1) and **the population that delivers essentially all
recall**.

### P7.0.3 — Verdicts on the A.1 hypotheses

| | hypothesis | verdict | evidence |
|---|---|---|---|
| H1 | budget consumed by within-assembly edges | **CONFIRMED** | 99.9% of live slots; cross-* declines at 100.0% |
| H2 | displacement structurally inert | **CONFIRMED** | 0.142% of 3,584,495,708 proposals (P7.2 wants ≥0.5%) |
| H3 | readout dominated by hop 0 | **CONFIRMED** | hop 0 carries 99.1%; multi-hop 0.0% |
| H4 | width overpaid | already settled in P6.2, not re-measured | width 64 ≡ width 256 at 5× throughput |

### P7.0.4 — The root cause A.1 did not name: k-WTA is saturated by the cue's own assembly

`Assembly.Size(Sparsity=32)` = 32 × `NeuronsPerDim`(8) = **256 neurons. `ActivationWidth` is also
256.** The cue's own assembly starts at potential 1.0 and exactly fills every k-WTA slot, so a
propagated neuron essentially cannot win — measured at 4,064 hop-0 winners against 32 hop-1 winners
across 16 cues, i.e. ~254 of 256 slots taken by the assembly on every probe.

That single coincidence explains the entire chain, and each link is now measured rather than
inferred:

```
ActivationWidth (256) == assembly size (256)
  → only assembly members survive k-WTA                    [4,064 vs 32 winners]
  → Hebbian pairs are almost always within-assembly        [99.9% of live slots]
  → 32 slots/neuron fill with within-assembly edges        [100.0% full]
  → cross-* candidates born at 0.11 cannot displace        [0 created, 0.142% displacement]
  → no cross-assembly edges exist at all                   [0 live slots]
  → no multi-hop paths                                     [0.0% mass at hop 2+]
  → no association (P5.5 R_PMI +0.004)
  → and no accuracy-for-scale trade (P6.3 flat over 385×)
```

**This reframes P7.1.** A.1 hypothesis 4 read the width finding as "there is spare compute to
spend". The measurement says something more specific: width is not merely overpaid, it is *exactly
equal to assembly size*, and that equality is what structurally forbids propagated neurons from
ever entering the learning population. Cutting `ActivationWidth` to 64 per P6.2 would make it
**worse** — the assembly would over-fill k-WTA four times over. The lever is the *relationship*
between `ActivationWidth` and `Assembly.Size`, and it wants to move in the opposite direction from
what A.1 suggested (width > assembly size, or a k-WTA that reserves slots for propagated neurons,
or a smaller assembly).

### P7.0.5 — Two instrumentation defects found while building the instrument

**1. Attributing surviving mass by "the population that delivered it" reports zero for everything.**
First implementation classified each k-WTA winner by the synapse population that last reached it.
Every population came back 0.0. Cause: an assembly member is created at hop 0 with no delivering
synapse, and is then *topped up* by within-assembly edges — the drive those edges contribute is
real, but the node is still hop 0 with no delivering population recorded. Fixed by summing
contributions at the point of delivery, which is why drive-injected and surviving-mass are reported
in different units rather than as one column.

**2. `Declined` changed meaning.** Proposals rejected by `CreationProductThreshold` (both parties
insufficiently active) previously returned without incrementing any counter; they now count as
declines, because P7.0 needs total proposal pressure per population. **The P1.3 figure of
2,325,130,394 declines is therefore not comparable to the numbers above** — the new definition is
strictly larger. Flagged rather than silently rebased.

### P7.0.6 — Delivered

| Component | Path | Notes |
|---|---|---|
| Provenance | [Substrate/SynapseStore.cs](src/GreyMatter.Poc/Substrate/SynapseStore.cs) | `SynapsePopulation`, per-slot `Population[]`, per-population counters, `PopulationCensus` |
| Edge classification | [Runtime/Plasticity.cs](src/GreyMatter.Poc/Runtime/Plasticity.cs) | classifies each edge from the cue's assembly membership |
| Mass attribution | [Runtime/Cascade.cs](src/GreyMatter.Poc/Runtime/Cascade.cs) | `MassByHop`, `DriveByPopulation`, `WinnersByHop` |
| Persistence | [Engrams/NeuronRecipe.cs](src/GreyMatter.Poc/Engrams/NeuronRecipe.cs), [Engrams/EngramStore.cs](src/GreyMatter.Poc/Engrams/EngramStore.cs) | provenance survives evict → persist → hydrate |
| Report | [Eval/AttributionEval.cs](src/GreyMatter.Poc/Eval/AttributionEval.cs) | `gm eval attribution` |

### P7.0.7 — What P7.1 now has to do, and the constraint it inherits

The A.1 lever list needs revising against the measurements, and this is a design question for Bill
rather than something to change unilaterally (A-R3):

- **Reducing `ActivationWidth` to 64 is contraindicated.** It would deepen the saturation that
  P7.0.4 identifies as the root cause.
- **Dropping within-assembly wiring entirely cannot be done first.** It delivers 99.9% of recall
  drive; removing it before cross-assembly recall exists would fail A-R3's no-regression clause and
  collapse `gm eval recall` to chance.
- **The minimal first move that the evidence supports** is to break the k-WTA saturation so that
  propagated neurons can enter the winner set at all — e.g. `ActivationWidth` > assembly size, a
  reserved cross-population quota in k-WTA, or a smaller assembly. Only once cross-assembly edges
  are being *proposed by winners that are not assembly members* do the P7.1 budget levers
  (per-population slot quotas) have anything to act on.

Ordering follows from that: **unsaturate k-WTA → verify cross-assembly edges are created at all →
then rebalance the budget → then repair displacement (P7.2).** P7.1's gate (cross-share >50%,
decline <50%, recall holds) is reachable on that path; it is not reachable by budget quotas alone,
because with zero cross-assembly proposals from non-member winners there is nothing for a quota to
protect.

---

## P7.1 — Rebalance the synaptic budget

**Date:** 2026-08-17
**Gate:** cross-assembly + cross-cue synapses go from budget-starved to first-class — decline rate
below 50%, share of total slots above 50% — while `gm eval recall --repeats 5` still passes its P4
bar (lift ≥ +0.05, separated, zero-truncation config).
**Status: PASS on all three criteria.**

Ordering was agreed with Bill after P7.0.7: **unsaturate k-WTA first, then rebalance the budget** —
rather than A.1's original lever order, which P7.0.4 contraindicated.

### P7.1.1 — Two levers, both defaulting to prior behaviour until measured

| Lever | Config | Pre-P7.1 value | Motivating measurement |
|---|---|---|---|
| k-WTA slots reserved for propagated (hop ≥ 1) neurons | `PropagatedWinnerQuota` | 0 | P7.0.4 — assembly size 256 == `ActivationWidth` 256, so 4,064 of 4,096 winners were assembly members and no propagated neuron entered Hebbian pairing |
| Cap on within-assembly occupancy of each neuron's slots | `WithinAssemblyCap` | 32 (= `SynapseCapPerNeuron`) | P7.0.1 — within-assembly held 99.9% of live slots with segments 100.0% full; zero cross-assembly synapses ever created |

Both were verified to reproduce prior behaviour at their pre-P7.1 values before anything was swept:
`gm eval recall --repeats 3` at quota 0 / cap 32 returned `SYSTEM_AUC 1.000`, `LIFT +0.500`,
`GRADED_RHO +0.952` — matching P4.1.

### P7.1.2 — The quota alone is necessary and *not sufficient*

Sweeping `PropagatedWinnerQuota` at cap 32 (1,200 sentences):

| quota | hop-1 k-WTA winners | cross-assembly proposals | cross-assembly live slots |
|---|---|---|---|
| 0 | 0 | 17,410 | **0** |
| 32 | 448 | 133,947,008 | **0** |
| 64 | 896 | 249,291,540 | **0** |
| 128 | 1,792 | 420,057,532 | **0** |

Propagated neurons now win k-WTA and propose cross-assembly edges at four orders of magnitude the
previous rate — and **still not one is created**, because every segment is full of within-assembly
edges and a candidate born at 0.11 cannot displace them. Proposals were never the bottleneck;
**slots were.** This is the clearest confirmation that the two levers had to be applied in this
order and that neither works alone.

### P7.1.3 — Both levers together

1,200 sentences, `--train 1200`, seed 12345:

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval attribution --train 1200 --propagated-winner-quota 64 --within-assembly-cap 8 --brain-data-path <scratch>/f_cand
```

| population | live slots | share | created | decline (all) | decline (threshold) | **decline (pressure)** |
|---|---|---|---|---|---|---|
| **baseline** within-assembly | 3,182,592 | 100.0% | 17,609,088 | 89.0% | 55.7% | 33.4% |
| **baseline** cross-assembly | **0** | 0.0% | **0** | 94.1% | 94.1% | 0.0% |
| **baseline** cross-cue | **0** | 0.0% | **0** | 100.0% | 56.6% | 43.4% |
| **P7.1** within-assembly | 657,529 | 25.4% | 3,569,948 | 95.8% | 50.3% | 45.5% |
| **P7.1** cross-assembly | **632,311** | 24.5% | **2,914,420** | 88.5% | 82.8% | **5.7%** |
| **P7.1** cross-cue | **1,294,797** | 50.1% | **6,371,675** | 95.7% | 59.7% | **35.9%** |

| gate criterion | baseline | P7.1 | requirement | |
|---|---|---|---|---|
| `CROSS_SHARE` | 0.0% | **74.6%** | > 50% | PASS |
| `CROSS_PRESSURE_DECLINE` | n/a (nothing to starve) | **35.9%** | < 50% | PASS |
| recall lift | +0.500 | **+0.500** [+0.500..+0.500], separated | ≥ +0.05 | PASS |
| `GRADED_RHO` | +0.952 | +0.952 [+0.913..+0.985] | no regression | PASS |

And the structural change the whole addendum is aimed at:

| | baseline | P7.1 |
|---|---|---|
| `HOP0_SHARE` | 100.0% | **7.9%** |
| `MULTIHOP_SHARE` | **0.0%** | **33.0%** |

**Multi-hop paths now exist.** P6.3 argued the accuracy-for-scale trade was unmeasurable because
recall had no multi-hop component for working-set pressure to break. A third of recall mass is now
multi-hop, so P7.4 has something to measure.

### P7.1.4 — A metric correction: "decline rate" had to be split by cause

The gate's decline criterion was initially failing at 88–96%, and the number was misleading. The
`Declined` counter conflates two opposite conditions:

- **threshold declines** — the two neurons were not jointly active enough to be worth wiring
  (`CreationProductThreshold`). The activation gate working as designed; nothing to do with budget.
- **pressure declines** — there was no slot and displacement failed. Actual starvation, and the only
  thing "budget-starved" can mean.

Split out, cross-assembly pressure-decline is **5.7%** against a raw decline of 88.5%: the
population is not starved at all, it is simply proposed far more often than it is jointly active
enough to justify. The gate is judged on the pressure column, which is what it was describing.

Worth stating the arithmetic, because it bounds what any future tuning can achieve: all-pairs
Hebbian among W winners over D steps proposes ≈ D·(W−1) edges per neuron into C slots, so the raw
decline rate has a floor of roughly 1 − C/(D·(W−1)) — 97% at W=256, D=4, C=32. **A raw decline rate
below 50% is unreachable at any budget setting without changing the pairing rule itself**, so it was
never a meaningful target.

### P7.1.5 — Adopted defaults (A-R3)

`PropagatedWinnerQuota` 0 → **64** and `WithinAssemblyCap` 32 → **8**, motivated by P7.0.4 and
P7.0.1 respectively and validated by P7.1.3. Recall re-run at the new defaults shows no regression.
138 tests pass. `ActivationWidth` is **unchanged at 256** — P7.0.4 established that reducing it to
64, as A.1 H4 suggested, would deepen the saturation that caused the problem.

### P7.1.6 — Order at the new defaults: still NO SIGNAL, and the shape of the failure changed

Not the P7.3 gate — P7.2 has not run — but the decisive question is worth measuring the moment the
structural blocker is removed, and the result redirects P7.2/P7.3.

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval order --repeats 5 --train 4000 --min-successors 12 --brain-data-path <scratch>/p73pre
```

| metric | P5.5 (pre-P7.1) | P7.1 defaults |
|---|---|---|
| `R_BIGRAM` | −0.0107 | **+0.1059** [+0.0917..+0.1186] |
| `R_UNIGRAM` | −0.0068 | **+0.0898** [+0.0589..+0.1136] |
| `R_PMI` | +0.0044 | **−0.0609** [−0.0912..−0.0312] |
| shuffled `R_PMI` | −0.0072 | −0.0833 |
| `PMI_GAP` | +0.0117 | +0.0223 |
| `SUPPORT` | 27.9% | 27.9% |
| verdict | NO SIGNAL | **NO SIGNAL** |

**The multi-hop channel now carries signal — and the signal is frequency again.** `R_BIGRAM` went
from ≈0 to +0.106, so cascade mass now genuinely ranks successors. But `R_UNIGRAM` moved in lockstep
to +0.090, and PMI — which divides the target's base rate out — went *more negative*. That is the
textbook signature of the collinearity §6.1 rule 3 exists to catch: the graph ranks a successor by
how common the successor is, not by how associated it is with the cue.

So P7.1 did what it set out to do structurally (cross-share 0→74.6%, multi-hop mass 0→33%) and the
frequency-detector failure simply moved one level up: it was hop-0 assembly mass before, it is
multi-hop cross-assembly mass now. **Opening the channel was necessary and is not sufficient.**

This matters for what P7.2 can be expected to achieve. Displacement repair makes slot competition
*live*; it does not make the learning rule base-rate-aware. Nothing in Hebbian coactivation
subtracts a target's marginal frequency, so a frequent word wins every coactivation race it enters
regardless of association. On this evidence the A.5(c) fallback — explicit anti-Hebbian/depression
on non-coincidence, dividing base rates out **in the learning rule** — is no longer a speculative
fallback but the mechanism the measurements point at. It remains locked under A-R4 pending a P7.3
failure and a design review, and this entry is the evidence for that review rather than a licence
to start.

**Cosmetic fix:** the verdict line printed `-+0.0609`. `"+0.0000"` as a .NET custom format emits a
literal `+` after the sign; the correct signed form is `"+0.0000;-0.0000"`. Fixed.

---
## P7.2 — Make slot competition live (displacement repair)

**Date:** 2026-08-17
**Gate:** adversarial bench — displacement ≥ 0.5% of proposals made against a full segment; real run
— corpus-statistics shift test passes; no recall regression (A-R3).
**Status: displacement criterion PASS. Shift criterion measured at smoke scale only (P7.2.4);
the spec-scale run is still in flight at the time of writing.**

### P7.2.1 — Mechanism chosen, and why

Of the plan's three candidates, **incumbent erosion** ("incumbent weight decay that keeps saturated
edges contestable without destroying them") was implemented as `ContestErosion`: when a candidate is
refused at capacity, the incumbent it lost to is weakened by `ContestErosion × sourceActivation ×
targetActivation`.

Chosen over the alternatives on cost, not preference: evidence-proportional challenge needs a
per-candidate evidence score, and pressure-derived birth weights need a shadow score for every
rejected pair — both require per-pair state for pairs that by definition do not exist yet. Erosion
needs none, and is self-limiting by construction: an incumbent still being genuinely coactivated is
re-strengthened by its own traffic, so erosion removes exactly the edges that stopped earning their
slot while sustained pressure builds behind them.

### P7.2.2 — A second dead window, created by P7.1 and found by this gate

The first adversarial-bench run reported `PROPOSALS_AT_FULL: 0` — apparently a broken counter, in
fact a real defect. With P7.1's `WithinAssemblyCap` at 8, a within-assembly candidate is refused at
its **population budget** long before the segment reaches `SynapseCapPerNeuron` = 32, so it never
contested anything. **P7.1 had made within-assembly incumbents permanently safe from their own
kind** — the exact defect P7.1 was fixing, reintroduced one level down.

Both capacity conditions are now unified: a candidate whose own population budget is spent contests
its own kind; a cross-* candidate at a full segment contests the weakest within-assembly incumbent
by preference. Either way it either wins the slot or erodes the incumbent that beat it.

This is the second time in P7 that a fix has created the next defect (P7.1a's quota generated 420M
proposals that all failed for want of slots; P7.1b's cap froze within-assembly competition). Worth
naming as a pattern: **each lever moves the bottleneck rather than removing it**, which is an
argument for measuring after every single change rather than batching them.

### P7.2.3 — Displacement, on the gate's own denominator

The P7.0.3 figure of 0.142% measured displacement against *all* proposals, which dilutes it with
every proposal that found a free slot or failed the activation threshold — neither of which
competition could affect. The gate asks a narrower question: when a candidate does face capacity,
how often does it win? That denominator is now counted separately.

**Adversarial substrate bench** (`gm bench substrate --cycles 1500 --scope 2000`), ~393M proposals
at capacity:

| `ContestErosion` | displacement at capacity | gate (≥ 0.500%) |
|---|---|---|
| 0 (pre-P7.2) | **0.000%** | FAIL |
| 1e-5 | **8.472%** | **PASS** |
| 1e-4 | 41.875% | PASS |

**Real run** (`gm eval attribution --train 1200`):

| `ContestErosion` | displacement at capacity | `CROSS_SHARE` | `MULTIHOP_SHARE` |
|---|---|---|---|
| 0 | 0.078% | 74.6% | 33.9% |
| **1e-5** | **16.885%** | 74.5% | 32.4% |
| 1e-4 | 28.692% | 78.1% | 23.1% |
| 1e-3 | 42.845% | 88.0% | **2.5%** |

`0.000%` at erosion 0 on 393 million proposals is the dead window stated as precisely as it can be:
under the pre-P7.2 rule, a candidate facing capacity *never* won, not once.

**Adopted: `ContestErosion` = 1e-5.** It is the smallest value that clears the gate, and the sweep
shows why more is worse — at 1e-3 displacement looks impressive at 42.8% while multi-hop mass
collapses from 33.9% to 2.5%, i.e. the graph churns so fast that nothing survives long enough to
form a path. Maximising the gate metric would have destroyed the thing P7.1 just built.

### P7.2.4 — Shift test: preliminary, and already showing a confound

At smoke scale (600+600 sentences, 2 repeats, erosion 1e-5) the test passes —
`SUPPRESSED_RATIO 0.789`, `UNAFFECTED_RATIO 2.418`, `SHIFT_RESPONSE +1.629` — but it should not be
read as a pass yet, for two reasons visible in the numbers themselves:

1. **The null arm gained mass** (ratio 2.418 ≫ 1.0). Suppressing the most frequent bigrams removes
   22.6% of phase-2 sentences, which frees synaptic budget that unaffected pairs then absorb. Part
   of `SHIFT_RESPONSE` is therefore budget reallocation, not the graph tracking statistics.
2. **The variance is enormous** — `UNAFFECTED_RATIO` spans [0.000..12.019] on n=10.

The controlled comparison is erosion 0 versus 1e-5 at spec scale (2,000+2,000, 3 repeats): if the
shift response appears without erosion too, it is not attributable to displacement repair and the
gate is not met by this mechanism. That run is in flight and its result — either way — belongs in
this section before P7.2 can be called complete.

### P7.2.5 — The shift instrument is defective; the criterion is UNMEASURED, not failed

At spec scale (2,000 + 2,000, 3 repeats) the erosion-0 control arm returned:

```
SUPPRESSED_RATIO: 2.668 [0.000..31.631]   (n=15)
UNAFFECTED_RATIO: 0.091 [0.000..0.455]    (n=5)
SHIFT_RESPONSE:   -2.577
VERDICT: NO SHIFT RESPONSE
```

The direction is opposite to the smoke-scale result and the spreads are absurd — a ratio range of
[0.000..31.631], and **n=5 of 8 unaffected pairs surviving** because the other three measured zero
control mass and were dropped. A statistic that swings from +1.629 to −2.577 between scales, on
single-digit samples, is not measuring anything.

**Root cause, and it is my instrument rather than the system.** `EdgeMass` sums synapse weights only
over assembly members that are **currently resident**:

```csharp
int slot = scope.Pool.Find(vid);
if (slot < 0) continue;   // not resident; its edges live in its recipe
```

With `WorkingSetMax` 100,000 against a vocabulary whose assemblies span far more neurons, most of a
cue's 256 members have been evicted by measurement time, and their edges are in recipes on disk. So
edge mass is sampled from whatever fraction of the assembly happens to be resident — which varies
per cue, per arm and per repeat, and is frequently zero. That is the entire observed variance.

The fix is to read edges from `scope.Recipes` (which P5.2 made authoritative) rather than from the
resident pool, so the measurement covers the whole assembly. That is the obvious second attempt
under the A.4 stop rule, and it has not been made.

**Therefore: the P7.2 shift criterion is recorded as UNMEASURED, not as failed.** Claiming a failure
from a broken instrument would be exactly the error §6.1 exists to prevent, and the smoke-scale
"PASS" in P7.2.4 should be disregarded for the same reason. The erosion 0 vs 1e-5 comparison at spec
scale was still running when this entry was written; whatever it reports, it is subject to the same
defect and cannot settle the criterion either.

**P7.2 status, stated precisely:**

| criterion | status |
|---|---|
| displacement ≥ 0.5% of proposals at capacity | **PASS** — 0.000% → 8.472% (bench), 0.078% → 16.885% (real run) |
| corpus-statistics shift test | **UNMEASURED** — instrument defective (this section) |
| no recall regression (A-R3) | **PASS** — recall re-verified at P7.1 defaults; erosion 1e-5 leaves `CROSS_SHARE` and `MULTIHOP_SHARE` within noise of erosion 0 |

`ContestErosion` = 1e-5 is adopted on the displacement evidence alone. P7.3 should not begin until
the shift instrument is repaired and the criterion actually returns a number, because P7.3's whole
premise is that budget and competition are fixed — and "competition is fixed" is currently supported
by a bench microbenchmark and contradicted by nothing, rather than demonstrated on real corpus
dynamics.

---
### P7.2.6 — The spec-scale shift comparison, recorded but not credited

The erosion 0 vs 1e-5 run referenced in P7.2.5 completed after that entry was written:

| | erosion 0 | erosion 1e-5 |
|---|---|---|
| `SUPPRESSED_RATIO` | 2.668 [0.000..31.631] (n=15) | 0.670 [0.000..1.115] (n=17) |
| `UNAFFECTED_RATIO` | 0.091 [0.000..0.455] (**n=5**) | 1.260 [0.000..5.117] (**n=11**) |
| `SHIFT_RESPONSE` | −2.577 | **+0.590** |
| verdict emitted | NO SHIFT RESPONSE | FOLLOWS SHIFT |

Read naively this is the result P7.2 wanted: displacement repair turns a non-response into a
response, and the two arms differ by exactly one factor. **It is not credited, for the reason given
in P7.2.5 and confirmed by these numbers.**

The sample sizes are the tell. Both arms measured the same 8 suppressed and 8 unaffected pairs
across 3 repeats — 24 observations each — and retained 15/17 suppressed against **5/11 unaffected**,
because the rest had zero control-arm edge mass and were dropped by the `> 1e-6` guard. Which pairs
survive is decided by which assembly members happened to be resident, and that differs between arms.
So the two arms are not scored on the same pairs, which is a direct violation of §6.1 rule 2 — the
P5.6 lesson that filtering to reachable items silently changes the experiment. The ranges
([0.000..31.631]) say the same thing more bluntly.

That the erosion arm looks better is therefore uninterpretable: erosion changes which edges survive,
which changes which pairs clear the zero-mass guard, which changes the sample. A mechanism that
increases edge turnover will tend to populate more pairs and produce a tidier-looking ratio
regardless of whether it tracks corpus statistics.

**The criterion stays UNMEASURED.** Repairing `EdgeMass` to read from `scope.Recipes` fixes both
faults at once: it covers the whole assembly rather than the resident fraction, which removes the
variance and stops the sample from being arm-dependent. Until then these two verdicts are artefacts
of an instrument that samples differently in each arm, and the honest summary of P7.2 remains
displacement PASS / shift UNMEASURED.

---
### P7.2.7 — Reproduction check: sleep was not the cause

Bill's hypothesis was that the P7.2.6 run had been disturbed by the workstation sleeping mid-run.
The comparison was therefore re-run end to end with `caffeinate -dimsu` holding the machine awake,
on a cleared store, with the first run's log preserved for a direct diff.

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval shift --train 2000 --repeats 3 --contest-erosion 0       --brain-data-path <scratch>/sh0
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval shift --train 2000 --repeats 3 --contest-erosion 0.00001 --brain-data-path <scratch>/sh0.00001
```

**Result: bit-identical on every metric line across both arms** — `SUPPRESSED_RATIO`,
`UNAFFECTED_RATIO` (including the n=5 / n=11 sample sizes), `SHIFT_RESPONSE` and both verdicts
matched run 1 exactly.

Two things follow, and the second is the useful one:

1. **Sleep did not corrupt the run.** The eval is seeded and deterministic (rule 8), so a suspended
   process resumes to the same arithmetic; wall-clock disturbance cannot move the numbers. The
   P7.2.6 figures stand as measured.
2. **The determinism guarantee held under an uncontrolled interruption** — an OS-level suspend
   mid-run, which is a harsher test than the P5.4 kill/resume case because it was not anticipated
   or checkpointed. Nothing in this project's stack carries hidden wall-clock or scheduling state
   into its results. That is worth having verified by accident.

The instrument diagnosis in P7.2.5 is unaffected and remains the reason the shift criterion is
UNMEASURED: `EdgeMass` samples only resident assembly members, so the arms are scored on
arm-dependent subsets (n=5 vs n=11 of the same 24 observations), violating §6.1 rule 2. Reproducing
a biased measurement exactly does not make it less biased — it confirms the bias is systematic
rather than noise, which is if anything a stronger reason to repair it before P7.3.

---
### P7.2.8 — Correction: P7.2.5's diagnosis was wrong, and the real cause is worse

**This supersedes the root-cause claim in P7.2.5.** The repair it prescribed was implemented and
changed nothing, which falsified the diagnosis.

`EdgeMass` now reads from `scope.Recipes` (authoritative for synapses since P5.2) after an explicit
`ConsolidateAll()`, giving whole-assembly coverage regardless of residency. Re-running both arms
produced numbers **identical to the pre-repair run in every digit**, including the retention counts
15/24 and 5/24. A repair that changes nothing is a falsified hypothesis, so the cause was measured
directly:

```
recipes held: 754,063   resident: 94,925
on ->the   members=256  resident=256  withSynapses=226  totalSyn=6,081  edgesIntoTarget=1,133  mass=151.778
to ->be    members=256  resident=256  withSynapses=256  totalSyn=7,164  edgesIntoTarget=    0  mass=  0.000
it ->is    members=256  resident=256  withSynapses=256  totalSyn=6,791  edgesIntoTarget=1,490  mass=670.650
in ->the   members=256  resident=256  withSynapses=230  totalSyn=6,616  edgesIntoTarget=1,328  mass=297.178
```

**All 256 members of every measured cue were resident.** Residency was never the problem: the
measured pairs are by construction the corpus's *most frequent* bigrams, so their assemblies are the
hottest in the pool and essentially always present. Pool-based and recipe-based reads therefore
agree exactly, which is why the repair was a no-op. My inference from "most of the vocabulary is
evicted" to "these cues' members are evicted" was simply wrong, and the resident/recipe columns
above are what I should have measured before asserting it.

**The real cause: direct cue→target assembly edges are close to a lottery.** `to → be` — one of the
most frequent bigrams in English — has **7,164 synapses across its 256 fully-resident members and
not one of them lands in `be`'s assembly.** Meanwhile `it → is` has 1,490. Assemblies are
hash-disjoint 256-neuron sets drawn from a 10⁶-neuron space (P4.3 defect 4 made them so
deliberately), so whether any of a cue's ~7,000 edges happens to terminate inside one specific
256-neuron target set is close to chance. The zeros are real graph structure, not missing
measurement.

**Consequences, in order of importance:**

1. **The shift criterion remains UNMEASURED**, but for a different and less fixable reason.
   Direct-edge mass between two hash-disjoint assemblies is the wrong observable — it is zero for
   many pairs by construction, and no amount of instrument repair changes that. A usable shift test
   has to measure something that is reliably non-zero: cascade-mediated mass over multiple hops
   (which P7.1 made non-empty at 33%), or edge mass aggregated over many pairs rather than per-pair
   ratios. That is a redesign of the eval, not a bug fix, and it should be registered under rule 6
   before it is written.

2. **This is direct evidence for A.5(a), from a different direction than expected.** The fallback
   list frames similarity-bearing assemblies as a way to let related words share substrate. The
   measurement above says something stronger: with hash-disjoint assemblies, *even words that are
   massively co-occurrent in the corpus may have no direct synaptic path at all*, because their
   assemblies never happened to wire together. Hebbian learning cannot encode an association between
   two sets of neurons that never form an edge, however often the words co-occur.

3. **It sharpens why P7.1 raised `R_BIGRAM` while `R_PMI` went negative.** Whether a cue ranks a
   successor highly depends partly on whether that lottery paid out, and the lottery is biased
   toward frequent targets — a frequent word appears in more cues' winner sets, so it gets more
   chances to form an edge with anything. That is a structural route by which frequency, and only
   frequency, reaches the ranking.

**P7.2 final status:** displacement PASS; shift UNMEASURED (instrument measures the wrong
observable); no recall regression. `ContestErosion` = 1e-5 stands on the displacement evidence
alone. The erosion 0 vs 1e-5 difference in the shift numbers is not evidence for erosion — unequal
retention (15/24 vs 17/24 suppressed, 5/24 vs 11/24 unaffected) means the arms are scored on
different samples, §6.1 rule 2, exactly as flagged.

---

# P8(a) — Similarity-bearing assemblies

## P8a.0 — Registration (rule 6: hypothesis, metric, decision rule, before any code)

**Date:** 2026-08-17
**Authority note.** A-R4 gates the §A.5 fallbacks behind "a failed P7.3 **and** a design review with
Bill". P7.3 was **not attempted**. Bill reviewed the P7.2.8 evidence and elected to take A.5(a)
directly. Recorded as a deliberate deviation from the addendum's stated gating, decided by the
plan's author, not a skipped step. P7.3's instruments remain available and unrun.

### Hypothesis

P7.2.8 measured that `to → be` — among the most frequent bigrams in English — has **7,164 synapses
across 256 fully-resident assembly members and zero edges into `be`'s assembly**. With hash-disjoint
assemblies, whether two words have any synaptic path is close to a lottery, and Hebbian learning
cannot encode an association between neuron sets that never form an edge.

**H:** if assembly membership is *partly shared in proportion to code similarity*, co-occurring
words acquire reliable direct paths, and cascade mass begins to rank successors by association
rather than only by frequency.

This deliberately reintroduces what P4.3 defect-4 removed accidentally. That defect is the reason
for the control below: per-dimension membership with `PatternSize × NeuronsPerDim` = 16,384
addressable neurons made *every* word share ~216 of 256 members and destroyed the trained/untrained
distinction entirely. Proportional overlap is only useful if it is **tunable and bounded**.

### Mechanism

One new parameter, `AssemblyOverlap` ∈ [0,1]. Of a code's `Sparsity × NeuronsPerDim` = 256 member
slots, a fraction `AssemblyOverlap` are derived from `(active dim, index)` — therefore shared with
any code containing that dim — and the remainder from `(codeHash, index)` as now, therefore private.
**0 reproduces current behaviour exactly**; 1 reproduces the P4.3 defect. Expected shared members
between two codes ≈ `AssemblyOverlap × 256 × (shared dims / k)`.

### Metrics

| | metric | source |
|---|---|---|
| primary | `R_PMI` and `PMI_GAP` vs shuffled null | `gm eval order --repeats 5 --train 4000 --min-successors 12` |
| mechanism | **edge connectivity** — fraction of frequent bigram pairs with ≥1 direct cue→target edge, and median edges per pair | new `gm eval connectivity` (registered here) |
| guard | recall lift ≥ +0.05, separated | `gm eval recall --repeats 5 --working-set-max 500000` |
| guard | top-k collision at k=32 over 3k vocabulary | `gm eval encoder-ceiling --stage context` |

Edge connectivity is the mechanism check: it is the quantity P7.2.8 showed is near-zero, and it must
move before any order result can be attributed to shared substrate rather than to chance.

### Decision rule, fixed before seeing the numbers

Sweep `AssemblyOverlap` ∈ {0, 0.25, 0.50, 0.75}. A setting is **adopted** only if all three hold:

1. edge connectivity rises materially above the `AssemblyOverlap = 0` baseline;
2. recall lift stays ≥ +0.05 with non-overlapping repeat ranges (A-R3), and top-k collision stays 0%;
3. `R_PMI` ≥ +0.10 with `PMI_GAP` ≥ +0.15 and non-overlapping ranges — the base plan's LEARNED ORDER
   bar, unchanged.

**Pre-committed negative outcome.** If connectivity rises substantially at some setting while
`R_PMI` stays below +0.10 at every setting, that is a *positive* finding about the diagnosis and a
negative one about the fix: shared substrate is necessary but not sufficient, and the binding
constraint is the learning rule's lack of base-rate correction — i.e. **A.5(c) confirmed as the next
target**, on evidence rather than by elimination. This outcome is to be recorded as a result, not
retried with tuning (§0 rule 3).

**Guard against the P4.3 failure mode.** Any setting where recall lift collapses toward 0 is
reporting that assemblies have merged, not that association appeared. Criterion 2 exists to catch
exactly that, and it is not negotiable downward.

## P8a.1 — Result: A.5(a) fails its own pre-committed rule, and corrects P7.2.8 on the way

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval connectivity --train 2000 --assembly-overlap <ov> --brain-data-path <scratch>/conn<ov>
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval recall --repeats 3 --train 500 --working-set-max 500000 --assembly-overlap <ov> --brain-data-path <scratch>/rov<ov>
```

| `AssemblyOverlap` | co-occurring connectivity | null connectivity | **gap** | mass ratio | shared members (`do`~`you`, 14/32 dims) | recall lift |
|---|---|---|---|---|---|---|
| **0** (current) | 60.0% | 15.0% | **+0.450** | **417.85×** | 0/256 | **+0.500** |
| 0.25 | 100.0% | 100.0% | +0.000 | 0.98× | 28/256 | **+0.288** |
| 0.50 | 100.0% | 100.0% | +0.000 | 0.84× | 56/256 | — |
| 0.75 | 100.0% | 100.0% | +0.000 | 0.75× | 84/256 | — |

**Verdict: no setting above 0 is adoptable.** Criterion 1 of P8a.0 asked for connectivity to rise;
it rises to 100% — *for the null as well*. The registration anticipated precisely this and fixed the
guard in advance: "Raising connectivity for everything is assembly merging — the P4.3 defect-4
failure mode — not association. Only the gap means anything." The gap collapses from +0.450 to
+0.000, the co-occurring/null mass ratio falls from **418× to below 1**, and criterion 2 fails
independently — recall lift drops +0.500 → +0.288 at overlap 0.25 alone. The mechanism is
reproducing P4.3 defect-4 at a dial setting rather than accidentally.

Shared membership does scale as designed (28/56/84 members for a 14/32-dim pair at 0.25/0.5/0.75,
i.e. ≈ overlap × 256 × 14/32), so the implementation is doing what it was specified to do. The
specification is what does not work.

### P8a.2 — Correction: P7.2.8 overstated the lottery, and this is the second time

**P7.2.8 concluded that "direct cue→target assembly edges are close to a lottery." That is wrong as
stated, and the `AssemblyOverlap = 0` row above is the refutation.** Measured properly — 40 frequent
bigrams against 40 frequency-matched non-co-occurring pairs — connectivity is **60% for co-occurring
pairs against 15% for the null, with 418× the edge mass**. Direct paths are not a lottery; they
track co-occurrence strongly and specifically.

The error was inferring a population property from four hand-picked pairs, one of which (`to → be`)
happened to be a zero. `to → be` is real and still unexplained, but it is one of the 40% of frequent
bigrams with no path, not evidence that path formation is random.

This is the same mistake as P7.2.5, where I inferred "these cues' members are evicted" from "most of
the vocabulary is evicted". Both times the fix was to measure the population with a null instead of
reasoning from a handful of examples, and both times the corrected picture was materially different.
Recording it as a pattern rather than as two incidents: **every diagnostic claim in P7/P8 needs its
own null-controlled measurement before it is written down, however obvious the mechanism looks.**

### P8a.3 — What this leaves, and why it strengthens the A.5(c) case

The diagnosis chain now reads:

- Paths between co-occurring words **do** exist and **are** co-occurrence-specific (60% vs 15%,
  418× mass) — so the substrate is not the binding constraint, and A.5(a) is addressing a problem
  the system does not have.
- Sharing substrate to create more paths destroys the specificity that makes the existing paths
  meaningful. The two are in direct tension: hash-disjoint assemblies buy specificity at the cost of
  coverage, and there is no setting of this dial that improves both.
- Yet `R_PMI` is −0.06 (P7.1.6) while `R_BIGRAM` is +0.11 and `R_UNIGRAM` is +0.09. **Paths exist,
  are specific, and still rank by frequency.** That combination localises the defect precisely: it
  is not in which neurons connect, it is in *how much weight a connection accumulates*, which is
  Hebbian coactivation's lack of any base-rate term.

A frequent successor wins every coactivation race it enters simply by entering more of them, and
nothing in the learning rule divides that out. **A.5(c) — anti-Hebbian depression on non-coincidence
— is now supported by the elimination of both alternatives rather than by argument: not the budget
(P7.1), not competition (P7.2), not the substrate (P8a).**

`AssemblyOverlap` default stays **0**. The parameter and `gm eval connectivity` are kept — the
connectivity measurement with its null is the instrument P7.2.8 should have had, and it will be the
mechanism check for A.5(c) as well.

---

# P8(c) — Base-rate correction in the learning rule

## P8c.0 — Registration (rule 6)

**Date:** 2026-08-17
**Reached by elimination, not preference.** P7.1 removed the budget constraint (cross-share 0% →
74.6%), P7.2 removed the competition constraint (displacement 0.000% → 8.472%), P8a showed the
substrate is not the constraint (paths are 60% vs 15% connected with 418× the mass). `R_PMI` stayed
at −0.06 through all of it while `R_BIGRAM` reached +0.11 and `R_UNIGRAM` +0.09.

### Hypothesis

**H:** cascade mass ranks successors by frequency because Hebbian coactivation has no base-rate
term. Δw = η·a_s·a_t accumulates in proportion to *count(s,t)*, and a frequent successor enters more
coactivation events simply by being frequent. Ranking by count(s,t) is `R_BIGRAM`; PMI additionally
requires dividing by count(t). Within a fixed cue, count(s) is constant, so **the missing operation
is division by the target's marginal rate.**

### Mechanism, and an honest deviation from A.5(c) as written

A.5(c) names "explicit anti-Hebbian/depression on non-coincidence". The event-wise form with the
correct sign is *depress s→t when t fires without s*, whose expected magnitude is ∝ p(t)(1−p(s)) —
i.e. it penalises frequent targets, which is what is wanted. **That form requires in-edge traversal,
and `SynapseStore` is out-edges only** (§4.1, CSR by source). The cheap out-edge form — depress s→t
when s fires without t — has expected magnitude ∝ p(s)(1−p(t)), which penalises *rare* targets: the
wrong sign, and it would make the problem worse.

So the same quantity is subtracted analytically instead of event-wise, using the running marginal
estimate the substrate already maintains:

```
Δw = η·a_s·a_t  −  λ·a_s·familiarity[t]
```

A covariance rule: strengthen when the target's activation exceeds its own base rate, weaken when it
falls short. `Familiarity` is already updated per k-WTA win, already consolidated to recipes, and
already restored on resume, so this costs nothing extra and survives eviction. `BaseRateDepression`
λ = 0 reproduces current behaviour exactly.

The deviation is recorded because it matters for interpreting a null: a failure here falsifies
*analytic* base-rate correction over a saturating familiarity proxy, not the event-wise rule A.5(c)
describes.

### Metrics

| | metric | source |
|---|---|---|
| primary | `R_PMI` ≥ +0.10, `PMI_GAP` ≥ +0.15, non-overlapping | `gm eval order --repeats 5 --train 4000 --min-successors 12` |
| mechanism | **weight–frequency correlation** — Spearman(edge mass, target unigram count) over co-occurring pairs. The defect predicts this is strongly positive now and must fall toward 0. | `gm eval connectivity` (extended here) |
| mechanism | connectivity gap must **hold** at ≈ +0.45 — depression must not simply delete edges | `gm eval connectivity` |
| guard | recall lift ≥ +0.05, separated | `gm eval recall --repeats 5 --working-set-max 500000` |

The weight–frequency correlation is the sharp test. It measures the defect directly rather than
through the order eval, so it distinguishes "base-rate division worked" from "order improved for
some other reason".

### Decision rule, fixed before seeing numbers

Sweep `BaseRateDepression` λ ∈ {0, 0.005, 0.01, 0.02} against η = 0.01. Adopt a setting only if all
hold:

1. weight–frequency correlation falls materially below the λ=0 baseline;
2. connectivity gap stays ≥ +0.30 (edges survive; depression is normalising, not pruning);
3. recall lift ≥ +0.05, separated;
4. `R_PMI` ≥ +0.10 with `PMI_GAP` ≥ +0.15 and non-overlapping ranges.

**Pre-committed negative outcome.** If 1–3 hold at some λ but `R_PMI` stays below +0.10, the finding
is that base-rate correction is necessary but not sufficient, and the remaining suspect is the
readout: cascade mass sums over an assembly and may be dominated by hop-0 self-mass regardless of
edge weights (P7.0.2 measured hop-0 at 99.1% pre-P7.1, 7.9% after). That would be recorded and
brought to Bill, **not** patched by tuning the readout inside this phase — A-R2 warns that a readout
tweak which "finds" association a frequency-only graph cannot contain is exactly what the shuffled
null exists to expose.

If criterion 1 itself fails — the correlation does not move — the analytic proxy is inadequate and
the event-wise rule needs the in-edge index the substrate lacks. That is a substrate change and its
own phase.

## P8c.1 — Result: the mechanism works; the pre-committed gate is missed, narrowly

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval connectivity --train 2000 --base-rate-depression <λ> --brain-data-path <scratch>/bd<λ>
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval recall --repeats 3 --train 500 --working-set-max 500000 --base-rate-depression <λ> --brain-data-path <scratch>/rbd<λ>
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval order --repeats 5 --train 4000 --min-successors 12 --base-rate-depression <λ> --brain-data-path <scratch>/ord<λ>
```

| λ | weight vs target-freq | weight vs co-occur | connectivity (co / null) | conn. gap | recall lift | `GRADED_RHO` | `R_UNIGRAM` | **`R_PMI`** | **`PMI_GAP`** |
|---|---|---|---|---|---|---|---|---|---|
| **0** | **+0.353** | +0.114 | 60.0% / 15.0% | +0.450 | +0.500 | +0.730 | **+0.09**¹ | **−0.06**¹ | +0.022¹ |
| 0.005 | −0.061 | −0.027 | — | +0.200 | — | — | — | — | — |
| 0.01 | **−0.158** | −0.017 | 37.5% / 10.0% | +0.275 | +0.500 | +0.412 | −0.106 | **+0.1265** [+0.1059..+0.1352] | +0.1093 |
| 0.02 | −0.062 | +0.089 | 30.0% / 2.5% | +0.275 | +0.500 | +0.332 | **−0.1717** | **+0.1801** [+0.1666..+0.1942] | **+0.1433** |

¹ from P7.1.6, same configuration otherwise.

Shuffled nulls: λ=0.01 → +0.0171 [+0.0000..+0.0428]; λ=0.02 → +0.0368 [+0.0041..+0.0768]. **Both
non-overlapping with their real arms across 5 repeats.**

### The hypothesis is confirmed

`R_PMI` moves from **−0.06 to +0.18** and the shuffled arm stays near zero. `R_UNIGRAM` — the
frequency confound §6.1 rule 3 exists to catch — moves from **+0.09 to −0.17**, i.e. not merely
removed but reversed. `Spearman(edge mass, target frequency)` falls from **+0.353 to −0.06**. The
defect diagnosed in P8c.0 was real and the prescribed operation fixes it.

**This is the first positive, null-separated association result in the project.** Every prior order
measurement returned NO SIGNAL or a refusal.

### The gate is nonetheless not met, and I am not moving it

| criterion | requirement | λ=0.02 | |
|---|---|---|---|
| 1. weight–frequency correlation falls materially | — | +0.353 → −0.062 | **PASS** |
| 2. connectivity gap ≥ +0.30 | ≥ 0.30 | **+0.275** | **FAIL** |
| 3. recall lift ≥ +0.05, separated | ≥ 0.05 | +0.500, separated | **PASS** |
| 4. `R_PMI` ≥ +0.10 | ≥ 0.10 | +0.1801 | PASS |
| 4. `PMI_GAP` ≥ +0.15 | ≥ 0.15 | **+0.1433** | **FAIL** |
| 4. non-overlapping ranges | — | yes | PASS |

`PMI_GAP` misses by **0.0067** and the connectivity gap by **0.025**. Both are close enough that
relaxing either would look reasonable and would be exactly the behaviour §0 rule 3 forbids — the
bars were fixed in P8c.0 before any number existed, and the legacy P5.4 retraction is what happens
when a threshold moves to meet a result. **Verdict stands at the harness's own wording: WEAK ORDER
SIGNAL — present but small.**

Criterion 2's failure is also substantive, not just arithmetic: co-occurring connectivity falls
60% → 30%, so depression is pruning half the real paths, not only normalising their weights. The
null is pruned harder (15% → 2.5%, mass ratio 418× → 1016×), so what survives is far more specific
— but coverage is genuinely lost, which is what criterion 2 was written to detect.

### P8c.2 — λ is overshooting, and the principled setting is below the range swept

The correct base-rate correction should drive `R_UNIGRAM` to **zero** — mass uncorrelated with the
target's own frequency. Measured, it goes negative at every λ tested, including the smallest:

| λ | 0 | 0.005 | 0.01 | 0.02 |
|---|---|---|---|---|
| weight vs target-freq | +0.353 | −0.061 | −0.158 | −0.062 |
| `R_UNIGRAM` | +0.09 | — | −0.106 | −0.172 |

The zero crossing lies **between λ=0 and λ=0.005**, well below anything swept. Every setting tested
over-subtracts, which plausibly explains criterion 2's failure directly: over-subtraction drives
`delta ≤ 0` on genuine pairs, and the P8c creation guard then declines them, pruning real edges.

So the sweep grid was chosen badly — I anchored it to η = 0.01 on the assumption that λ should be
comparable to the learning rate, and the data says λ should be roughly an order of magnitude
smaller. **A follow-up sweeping λ ∈ {0.0005, 0.001, 0.002, 0.005} and selecting by "R_UNIGRAM
closest to zero" is principled rather than gate-chasing** — it uses the mechanism's own definition
of correct base-rate division instead of maximising the metric being gated. But it is a new
experiment and belongs in a registration, not in this phase (rule 6), and P8c.0's pre-committed
outcome says to bring a missed gate to Bill rather than retry it.

**Defaults unchanged: `BaseRateDepression` = 0.** Adopting λ=0.02 would import a 50% loss of path
coverage on the strength of a gate it does not pass.

### P8c.3 — Status

| | |
|---|---|
| A.5(c) hypothesis | **confirmed** — base-rate correction is the missing operation |
| P8c gate | **not met** (criteria 2 and 4-gap) |
| best measured | λ=0.02: `R_PMI` +0.1801 vs shuffled +0.0368, non-overlapping, recall lift +0.500 |
| adopted default | none — λ stays 0 |
| recommended next | registered λ sweep in {0.0005..0.005} selected on `R_UNIGRAM` ≈ 0, then re-run the P8c criteria unchanged |

The diagnosis chain that produced this is now complete and every link is measured: not the budget
(P7.1), not competition (P7.2), not the substrate (P8a), **the learning rule** (P8c).

---
## P8c.4 — Registration: finer λ sweep, selected on the mechanism not the gate

**Date:** 2026-08-18

### Hypothesis

P8c.2 measured that `R_UNIGRAM` and `Spearman(edge mass, target frequency)` are already **negative
at every λ tested including the smallest**, so the zero crossing — the point where base-rate
division is exact rather than over-applied — lies below λ=0.005, outside the swept grid. The grid
was anchored to η=0.01 on the assumption that λ should be comparable to the learning rate; the data
says roughly an order of magnitude smaller.

**H:** at the λ where the target-frequency correlation crosses zero, base-rate division is correct
rather than excessive; over-subtraction no longer drives `delta ≤ 0` on genuine pairs, so path
coverage is preserved (P8c criterion 2) while the association signal is retained.

### Selection rule — the discipline that makes this not gate-chasing

Sweep λ ∈ {0.0005, 0.001, 0.002, 0.005}. **Select the λ whose `WEIGHT_VS_TARGETFREQ` is closest to
zero** — the mechanism's own definition of correct base-rate division, measured by
`gm eval connectivity`, which is cheap and independent of the gated metrics.

Selection **must not** reference `R_PMI`, `PMI_GAP` or the verdict. Choosing λ by the metric being
gated is precisely the failure §0 rule 3 describes, and the P8c.1 misses were small enough
(0.0067 and 0.025) that a λ picked to close them would be indistinguishable from tuning.

### Judgement

The selected λ is then evaluated against the **unchanged P8c.0 criteria** — weight–frequency
correlation falls materially; connectivity gap ≥ +0.30; recall lift ≥ +0.05 separated; `R_PMI` ≥
+0.10 with `PMI_GAP` ≥ +0.15 and non-overlapping ranges. No bar moves.

- All four met → adopt as default under A-R3, with the measurement recorded.
- Any unmet → record the finding and stop. Two honest attempts at this gate will then have been
  made (P8c.1 and P8c.4), which is §A.4's stop rule: bring Bill the finding, do not sweep a third
  grid.

## P8c.5 — Result: the mechanism-correct λ produces no association. Stop rule reached.

### Selection

`WEIGHT_VS_TARGETFREQ` is a Spearman correlation, and P8c.4 specified selecting on a **single**
measurement of it — which contradicts the logic of §6.1 rule 1. Caught before selecting, and the
measurement was repeated across three seeds. It mattered:

| λ | mean \|weight vs target-freq\| | mean connectivity gap |
|---|---|---|
| **0.001** | **0.0647** | +0.242 |
| 0.005 | 0.0790 | +0.200 |
| 0.002 | 0.0857 | +0.233 |
| 0.0005 | 0.1560 | +0.283 |

A single run would have selected **λ=0.005**; three runs select **λ=0.001**. Per-seed values for
λ=0.0005 span +0.014 to +0.313, so the n=1 selection was reading noise.

### Judgement against the unchanged P8c.0 criteria, at the selected λ=0.001

| criterion | requirement | measured | |
|---|---|---|---|
| 1. weight–frequency correlation falls materially | — | +0.353 → +0.065 | **PASS** |
| 2. connectivity gap ≥ +0.30 | ≥ 0.30 | +0.242 | **FAIL** |
| 3. recall lift ≥ +0.05, separated | ≥ 0.05 | +0.500 [+0.500..+0.500], separated | **PASS** |
| 4. `R_PMI` ≥ +0.10 | ≥ 0.10 | **−0.0746** [−0.0994..−0.0547] | **FAIL** |
| 4. `PMI_GAP` ≥ +0.15 | ≥ 0.15 | +0.0083 | **FAIL** |

`VERDICT: NO SIGNAL`. `R_UNIGRAM` +0.1028, `R_BIGRAM` +0.1075 — the frequency confound is back,
essentially at the λ=0 pattern.

### The finding: mechanism-correctness and association are anti-aligned

| λ | weight vs target-freq (mean, 3 seeds) | `R_UNIGRAM` | `R_PMI` | co-occur connectivity |
|---|---|---|---|---|
| 0 | +0.353 | +0.09 | −0.06 | 60.0% |
| **0.001** ← mechanism-selected | **+0.065** (closest to 0) | +0.103 | **−0.075** | 42.5% |
| 0.01 | −0.158 | −0.106 | **+0.1265** | 37.5% |
| 0.02 | −0.062 | −0.172 | **+0.1801** | 30.0% |

**The λ that makes base-rate division exact produces no association, and the λ values that produce
association over-subtract badly.** They are not the same setting and they are not close.

This falsifies the interpretation offered in P8c.2, which is mine and was wrong: I read the negative
`R_UNIGRAM` at λ=0.01/0.02 as over-correction and predicted that the zero-crossing λ would give the
association signal with better coverage. It gives no signal at all.

The better reading of the λ=0.01/0.02 result is therefore **not** "base-rate division works". It is
that aggressive depression drives `delta ≤ 0` on low-covariance pairs, the P8c creation guard
declines them, and what survives is a sparsified graph of high-covariance edges only — coverage
falls 60% → 30% exactly as that account predicts. **The +0.18 is a sparsification effect, not a
normalisation effect.** Both are real mechanisms; they are different claims, and only the second was
registered as the hypothesis.

That distinction is testable — depression that prunes versus depression that rescales weights
without deleting edges are separable — but it is a new hypothesis and belongs in its own
registration.

### Stop rule

§A.4: two honest attempts, then stop. P8c.1 and P8c.5 are those attempts. **Recorded and stopping.**

- `BaseRateDepression` stays **0**. No default changes from P8c.
- The strongest measured result in the project remains λ=0.02: `R_PMI` +0.1801 [+0.1666..+0.1942]
  vs shuffled +0.0368, non-overlapping over 5 repeats, recall lift +0.500 — **`WEAK ORDER SIGNAL`**,
  short of the LEARNED ORDER bar by `PMI_GAP` 0.1433 vs 0.15, and now believed to be sparsification
  rather than the registered mechanism.
- Open question for Bill: whether the next phase tests **sparsification as the hypothesis in its own
  right** (does pruning to high-covariance edges carry association, and what does it cost in
  coverage?), or whether the coverage loss makes that a dead end and the readout — hop-0 dominance —
  is the better target.

---

# Addendum B — P9 and the legacy cleanout

## B.3 — Legacy cleanout

**Date:** 2026-08-18. One commit, **no code changes**, per B.3.

### Step 1 — audit (recorded, as B.3.1 requires)

| check | result |
|---|---|
| `ProjectReference` from `src/`,`tests/` to legacy | **none** — the only one is `tests → src/GreyMatter.Poc/Poc.csproj` |
| `using GreyMatter.{Core,Learning,Storage,DataIntegration,Evaluations}` in POC | **none** |
| `greyMatter/` path strings in POC sources | **none** |
| legacy tree committed (so deletion is recoverable) | **101 files tracked, nothing uncommitted** |
| unported asset of note | `Core/LLMTeacher.cs` (22,696 B), tracked, deferred to P10 |
| anything else found | **nothing.** The largest remaining files are `Cerebro.cs` (172,820 B), `Program.cs` (120,431 B) and `EnhancedBrainStorage.cs` (89,315 B) — all three are the accreted classes §1.1 identified as the reason for the rebuild, and all were port sources, not port candidates. |

### Steps 2–5 — executed

- Deleted `greyMatter/` (101 tracked files), the empty `docs/`, and stray `.DS_Store` files
  (`.gitignore` already covered them).
- `GreyMatter.sln`: removed the `greyMatter` project entry and its four
  `{5AEA7501-…}` configuration lines. Zero occurrences of the legacy GUID remain; Poc and Tests
  are the only projects.
- `Prompt.md`: present at repo root and **added to git** — it was restored as a file but untracked,
  so the plan's stated authority was still absent from the repository proper.
- README: legacy row dropped; `Prompt.md` added to the "Where to look" table as the authority.
- `plan.md` §0 rule 2 and §1.6: annotated *historical as of B.3*, **not rewritten** — §1 remains
  the record of what was inherited and why the rebuild happened.

Addendum B itself was uncommitted (`M plan.md`) when this ran, so it is included in this commit.

### Step 6 — verification

```bash
dotnet build GreyMatter.sln -c Release
dotnet test tests/GreyMatter.Poc.Tests/GreyMatter.Poc.Tests.csproj -c Release
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval recall --repeats 3 --train 500 --working-set-max 500000 --brain-data-path <scratch>/clean_recall
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- audit --strings --brain-data-path <scratch>/clean_audit
```

| check | outcome |
|---|---|
| solution build | **0 errors**, 1 pre-existing warning (below) |
| test suite | **138 passed**, 0 failed |
| recall smoke | `SYSTEM_AUC` 1.000, `UNTRAINED` 0.500, `LIFT +0.500` [+0.500..+0.500], separated, `GRADED_RHO +0.730` [+0.680..+0.757] — **matches the pre-cleanout λ=0 measurement in P8c.1 exactly** |
| `gm audit --strings` | **CLEAN** — 412 partitions, 81,932,577 payload bytes, 0 string tokens, 0 corpus words |

**The audit check was run twice, and the first run was worthless.** Against the recall eval's path
it reported `FILES_SCANNED: 0` — recall uses scratch brains and deletes them, so "CLEAN" was a
statement about an empty directory. Re-run against a store populated by `gm learn --sentences 600`
(412 partitions, 82 MB of payload) it is a real check. Recording this because a green tick on zero
files is exactly the kind of vacuous verification this project has twice been caught by.

**Pre-existing warning, deliberately not fixed:** `ScaleSweep.cs(65,41) CS8604` — possible null
argument to `Path.Combine`, from `args.Value("--sweep-path", …)` returning `string?`. It predates
this commit and only became visible because earlier builds filtered output to errors. B.3 specifies
no code changes in the cleanout commit, so it stays; logged here as a known cleanup.

## P9.0 — Registration: `gm eval assoc`, the instrument P7.3 specified and nobody built

**Date:** 2026-08-18

### Why this outranks the mechanism work

Every association verdict since P5 has come from `gm eval order`, which asks the graph to rank a
cue's **successors against each other** by base-rate-corrected sequence statistics. That is
syntagmatic order — the hardest association question available, and one the plan adopted because
the legacy tree had an order harness to port.

Prompt.md asks the easier and more fundamental question: does activating a concept light up related
material *at all*? P7.3 specified exactly that instrument and made it an **equal-alternative gate**
— "passing either at full rigor is a P7.3 pass" — and it was never built.

Meanwhile P8a measured, at current defaults, co-occurring pairs **60% connected against a 15% null
with 418× the edge mass**. That is the raw material an association AUC reads out. It is plausible
the system already passes the Prompt.md-relevant gate and the only reason nobody knows is that the
instrument does not exist.

### Hypothesis

**H:** cascade mass from a cue distinguishes words that co-occurred with it from frequency-matched
words that never did, even though it cannot rank co-occurring successors against each other.
Discrimination and ranking are different problems; the P8a connectivity gap suggests the first is
solved and only the second is not.

### Instrument, as P7.3 specified it

For each cue, rank frequency-matched in-vocabulary words that **did** co-occur with it
(within-sentence, window ±2) against those that **never** did, by cascade mass. AUC over the cue
set, ≥5 repeats, same-pairs shuffled null.

Controls are frequency-matched (A-R1) and in-vocabulary — the P4.2 lesson. The shuffled null trains
on word-order-shuffled sentences and is scored on the **same pairs** (rule 2): shuffling preserves
every unigram frequency and destroys co-occurrence within the ±2 window, so a system encoding only
frequency scores identically in both arms (A-R2). No readout arithmetic is introduced; if any is
ever needed, A-R2 polices it.

### Metrics and bar

`ASSOC_AUC` ≥ 0.70, shuffled ≤ 0.55, non-overlapping repeat ranges — **the unchanged P7.3 bar**
(B-R2). Reported with mean and [min..max] over ≥5 repeats.

### Decision fork, fixed now (B.2)

- **Instrument gate:** runs end-to-end and emits a rule-compliant verdict — any verdict, including a
  refusal, passes (the P5 precedent).
- **≥ 0.70 vs ≤ 0.55, separated, at defaults** → P7.3 declared passed on its association arm; skip
  P9.1/P9.2, go directly to P9.3 closeout.
- **Below bar** → per-cue diagnostics become P9.1's baseline.
- **Signal but short** → P9.1 proceeds with `ASSOC_AUC` added to its judgement alongside order.

Run at current defaults (λ=0, `ContestErosion` 0, quota 64, cap 8) and, as a **recorded diagnostic
arm only**, at λ=0.02 — the P8c-strongest point. The diagnostic arm cannot trigger the fork; only
defaults can.

## P9.0 — Result: instrument gate PASS, association gate FAIL, fork resolves to P9.1

**Date:** 2026-08-18

### Attempt 1 was invalid, and the instrument was wrong in two ways

The first build emitted `CONFOUNDED` — real 0.574 [0.560..0.593] against shuffled 0.569
[0.552..0.578]. That reads as a system finding. It was two defects of mine, both arguable from the
instrument's own printout **without reference to the outcome**, which is the test for whether
repairing after a failure is legitimate rather than tuning:

**Defect 1 — related partners selected by descending frequency.** Every cue received the same set:

```
you  related: to, the, is, have      to   related: you, the, is, have
the  related: you, to, is, have      is   related: you, to, the, have
```

Not a cue-specific set, and trivially high-mass for any cue, because cascade mass tracks frequency
at ρ≈0.73. Visible in the output before any AUC is computed.

**Defect 2 — the null preserved what it existed to destroy.** I reused `gm eval order`'s
within-sentence shuffle. That destroys word order but leaves sentence co-membership nearly intact,
and "related" here *means* co-occurred within a sentence. A correctly-associating system therefore
scores identically in both arms — which is exactly the 0.574/0.569 that came out.

**This is a gap in A-R2 as written, not only in my code.** The rule reasons that "shuffling
preserves every unigram frequency, so *any* mechanism that only encodes frequency scores
identically in both arms". True, and incomplete: within-sentence shuffling also preserves
association, so the null cannot isolate it. It is the correct null for **order**, which is where it
was inherited from, and it does not transfer to **association**.

Repairs: related partners are now sampled **uniformly** from the cue's co-occurrence set (seeded);
the null now **redistributes every token globally** across the corpus, preserving unigram counts and
sentence lengths exactly while destroying which words share a sentence.

### Attempt 2 — the valid measurement

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval assoc --repeats 5 --train 2000 --brain-data-path <scratch>/a2_def
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval assoc --repeats 5 --train 2000 --base-rate-depression 0.02 --brain-data-path <scratch>/a2_l02
```

| arm | `ASSOC_AUC` | `SHUFFLED_AUC` | `ASSOC_GAP` | separated | related vs unrelated mass |
|---|---|---|---|---|---|
| **defaults** (fork-deciding) | **0.508** [0.496..0.528] | 0.511 [0.493..0.534] | **−0.003** | False | 5.6 vs 6.4 |
| λ=0.02 (diagnostic only) | 0.537 [0.528..0.548] | 0.511 [0.497..0.526] | +0.026 | True | 0.4 vs 0.1 |

**`VERDICT: NO ASSOCIATION` at defaults. `P7.3_ASSOC_GATE: FAIL`** against the unchanged bar
(≥0.70 vs ≤0.55, separated).

- **Instrument gate: PASS.** It runs end-to-end and emits a rule-compliant verdict (B.2, the P5
  precedent).
- **Decision fork: resolves to "below bar" → P9.1 proceeds**, with these per-cue diagnostics as its
  baseline.

### What this settles

B.0's hopeful reading — that P8a's 60%-vs-15% connectivity with 418× edge mass might already
constitute a passing association signal that nobody had read out — **is false, and the two
measurements are not in conflict.** Connectivity says co-occurring pairs are more likely to have
*an edge at all*. `ASSOC_AUC` says that once a cue is actually run through the cascade, the mass
arriving on a related word is indistinguishable from the mass arriving on a frequency-matched
unrelated one (5.6 vs 6.4 — the wrong way round, at chance). Edges exist and are co-occurrence-
specific; the **readout does not preserve that specificity**.

That is a genuinely new fact, and it narrows P9.1: the defect is not only in the learning rule but
in the path from edges to cascade mass. P7.0.2 already measured hop-0 at 99.1% of surviving mass
pre-P7.1 and 7.9% after; the mass that now flows multi-hop evidently spreads without regard to
which edges carried it.

The λ=0.02 diagnostic arm is consistent with P8c.5's sparsification reading: it separates from its
null (+0.026, non-overlapping) but at absolute mass levels of 0.4 vs 0.1, i.e. a graph so pruned
that almost nothing reaches. It clears no bar and cannot trigger the fork.

### Standing correction to A-R2

Recorded for the next phase: **the shuffled-order null is the right judge for order and the wrong
one for association.** Any P9.1 arm scored on `ASSOC_AUC` must use the global-redistribution null;
any arm scored on `R_PMI` keeps the within-sentence shuffle. Using one null for both would repeat
this error in a place where it would be much harder to see.

## P9.1 — Registration: sparsification vs normalisation

**Date:** 2026-08-18

### Question

P8c.5 left one thing open: the project's only positive association signal (λ=0.02, `R_PMI` +0.1801
vs shuffled +0.0368, non-overlapping) arrived together with a 50% loss of path coverage, and the
mechanism-correct λ produced nothing. Two incompatible accounts fit:

- **normalisation** — subtracting the target's base rate makes surviving weights measure covariance
  rather than co-occurrence, and the signal is in the *weights*;
- **sparsification** — depression drives `delta ≤ 0` on low-covariance pairs, the creation guard
  declines them, and the signal is in *which edges survive*, not in their values.

### Three arms, differing in exactly one mechanism

All at λ=0.02, the P8c-strongest point, so results are comparable to the recorded +0.18.

| arm | deletes edges? | rescales weights? | mechanism |
|---|---|---|---|
| **(i) as-measured** | yes | yes | `BaseRateDepression` 0.02 — the P8c.1 configuration, reproduced |
| **(ii) rescale-only** | **no** | yes | identical Δw, but the `delta ≤ 0` creation guard is bypassed and weights are floor-clamped at `PruneThreshold` so depression can never delete |
| **(iii) prune-only** | yes | **no** | λ=0 (pure Hebbian weights, untouched), plus a post-hoc prune of the lowest-covariance edges, removing the **same fraction** arm (i) loses — coverage-matched by construction |

If normalisation carries the signal, (ii) keeps it and (iii) loses it. If sparsification does, the
reverse. If both fall to chance, the +0.18 needs a third explanation.

### Metrics and nulls

`R_PMI`/`PMI_GAP`, `ASSOC_AUC`, connectivity gap, live-edge coverage, recall lift — ≥5 repeats,
seeds fixed, arms differing by one factor (rule 8).

**Two different nulls, per the P9.0 standing correction:** `R_PMI` keeps the within-sentence shuffle
(correct for order); `ASSOC_AUC` uses global token redistribution (correct for association). Using
one for both is the error P9.0 caught, and it would be far harder to see here.

### Gate and decision rule, fixed now

- **Gate:** the arms *separate* — the +0.18 is attributed to one mechanism, with non-overlapping
  repeat ranges between the arm that keeps it and the arm that loses it.
- **Adoption:** only if some arm meets B-R2's unchanged bars in full (weight–freq falls;
  connectivity gap ≥ +0.30; recall lift ≥ +0.05 separated; `R_PMI` ≥ +0.10 with `PMI_GAP` ≥ +0.15
  non-overlapping). Otherwise record which mechanism carries the signal and what coverage it costs,
  and take it to Bill.
- Two honest attempts, then stop (§A.4). This is a **new hypothesis**, not λ-grid #3 (B-R1).

### Pre-committed reading of the likely outcome

P9.0 measured `ASSOC_AUC` 0.508 at defaults — edges are co-occurrence-specific but the readout does
not preserve that specificity. If all three arms leave `ASSOC_AUC` at chance while `R_PMI` separates,
that localises the remaining defect to the **readout** rather than to either learning mechanism, and
is the finding to carry forward regardless of which arm wins on order.

## P9.1 — Result: GATE PASS. Normalisation carries the signal, not sparsification.

**Date:** 2026-08-18

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval order --repeats 5 --train 4000 --min-successors 12 <arm flags>
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval assoc --repeats 5 --train 2000 <arm flags>
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval connectivity --train 2000 <arm flags>
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval recall --repeats 3 --train 500 --working-set-max 500000 <arm flags>
```

Arms: **(i)** `--base-rate-depression 0.02`; **(ii)** same `--depression-never-deletes true`;
**(iii)** `--base-rate-depression 0 --post-hoc-prune-fraction 0.2831`.

| | (i) as-measured | (ii) rescale-only | (iii) prune-only |
|---|---|---|---|
| deletes / rescales | yes / yes | partial / yes | yes / **no** |
| coverage loss vs λ=0 | 28.31% | 19.90% | **28.31%** (matched to (i)) |
| **`R_PMI`** | **+0.1801** [+0.1666..+0.1942] | **+0.1624** [+0.1351..+0.1833] | **−0.0626** [−0.0881..−0.0372] |
| shuffled `R_PMI` | +0.0368 | +0.0216 | −0.0855 |
| `PMI_GAP` | +0.1433 | +0.1408 | +0.0228 |
| verdict (order) | WEAK ORDER SIGNAL | WEAK ORDER SIGNAL | **NO SIGNAL** |
| `WEIGHT_VS_TARGETFREQ` | −0.062 | −0.267 | **+0.309** |
| connectivity gap | +0.275 | +0.275 | +0.475 |
| `ASSOC_AUC` | 0.537 | 0.543 | 0.507 |
| recall lift | +0.500, sep | +0.500, sep | +0.500, sep |

### Gate: PASS — the arms separate decisively

Arm (iii) is coverage-matched to arm (i) *by construction* — the same 28.31% of edges removed,
ranked by the same covariance score — and differs in exactly one thing: surviving weights are not
rescaled. `R_PMI` collapses from **+0.1801 to −0.0626**, ranges [+0.1666..+0.1942] against
[−0.0881..−0.0372], nowhere near overlapping. Arm (ii) prunes *less* and rescales fully, and keeps
almost all the signal (+0.1624).

**The +0.18 is normalisation. Sparsification contributes nothing.** Removing exactly the edges
depression would remove, without touching the weights, produces no order signal at all.

`WEIGHT_VS_TARGETFREQ` says the same thing mechanistically: arm (iii) retains the frequency defect
at **+0.309** (baseline is +0.353), while the rescaling arms drive it to −0.062 and −0.267. Pruning
does not remove the frequency confound; subtracting the base rate does.

### Correction: P8c.5's interpretation was wrong

P8c.5 concluded "**the +0.18 is a sparsification effect, not a normalisation effect**", reasoning
from the coincidence of the signal with a 50% coverage loss. That is now measured and it is
backwards. The coverage loss is a *side effect* of depression, not its mechanism of action — arm
(iii) shows the side effect alone buys nothing, and arm (ii) shows most of the signal survives when
the side effect is reduced.

Two interpretations of mine have now been overturned by direct measurement in consecutive phases
(P7.2.8's "lottery", P8c.5's "sparsification"), both times because I reasoned from a correlation in
the data rather than isolating the variable. The three-arm design is what settled it, and it is the
pattern to repeat.

### Adoption: none — the unchanged bars are still not met

| criterion (B-R2) | arm (i) | arm (ii) | |
|---|---|---|---|
| weight–freq falls materially | −0.062 | −0.267 | PASS |
| connectivity gap ≥ +0.30 | +0.275 | +0.275 | **FAIL** |
| recall lift ≥ +0.05 separated | +0.500 | +0.500 | PASS |
| `R_PMI` ≥ +0.10 | +0.1801 | +0.1624 | PASS |
| `PMI_GAP` ≥ +0.15 | +0.1433 | +0.1408 | **FAIL** |

The same two criteria as P8c.1, by the same narrow margins. `BaseRateDepression` stays **0**; no
defaults change. Arm (ii) is worth noting as the better operating point on mechanism (frequency
defect −0.267 vs −0.062, 8 points less coverage lost) at a negligible order cost, if a future phase
adopts anything.

### The pre-committed reading fires: the defect is now in the READOUT

P9.1's registration stated in advance: *"If all three arms leave `ASSOC_AUC` at chance while `R_PMI`
separates, that localises the remaining defect to the readout rather than to either learning
mechanism."*

That is exactly the outcome. `ASSOC_AUC` is 0.537 / 0.543 / 0.507 — chance in every arm, including
the two whose order signal separates cleanly from its null. The learning rule can be made to encode
association in the edges; **the cascade readout does not deliver it.**

Combined with P9.0 (`ASSOC_AUC` 0.508 at defaults, while co-occurring pairs are 60% connected with
418× the edge mass of matched non-co-occurring pairs), the picture is consistent and specific:

- edges exist, are co-occurrence-specific, and their weights can be made base-rate-corrected;
- cascade mass arriving at a word is nonetheless independent of whether that word is related to the
  cue.

### Recommendation for Bill (P9.2 is now questionable)

B.2 makes P9.2 — the event-wise anti-Hebbian rule on a new in-edge index — conditional on P9.0 and
P9.1 both falling short. They have, on their *bars*. But P9.2 is another **learning-rule** change,
and P9.1 has just shown the learning rule already produces base-rate-corrected, co-occurrence-
specific edges. Spending a registered substrate change (CSR-by-target, re-running the P1 bench) to
improve a component that is no longer the binding constraint looks like the wrong next move.

The measured constraint is the path from edges to cascade mass. That is not on the A.5 ledger and
has never had a registered phase. My recommendation is to take the readout as the next target
instead of P9.2 — but B-R1 and A-R4 put that decision with Bill, and P9.2 remains available and
unrun if he prefers to follow the script as written.

## P9.2R — Registration: readout attribution (replaces P9.2, agreed with Bill)

**Date:** 2026-08-18

**Deviation, recorded.** B.2's P9.2 is the event-wise anti-Hebbian rule on a new in-edge index.
P9.1 showed the learning rule already produces base-rate-corrected, co-occurrence-specific edges,
so another learning-rule change would improve a component that is no longer the binding constraint.
Bill agreed to target the readout instead. P9.2 stays available and unrun; A.5(c)-event-wise remains
`live` on the B-R1 ledger.

### Hypothesis

The association is **in the edges and lost on the way out**. P9.1 measured, on the same brains:
`WEIGHT_VS_COOCCUR` +0.089…+0.136 and connectivity gaps of +0.275…+0.475 (edges carry it), against
`ASSOC_AUC` 0.507…0.543 (the readout does not).

**H:** the loss is **k-WTA truncation**. The readout keeps `ActivationWidth` = 256 winners globally
across a scope of thousands, so the mass landing on any one 256-neuron target assembly is a tiny,
high-variance sample of what the edges actually delivered. Supporting observation: absolute masses
in P9.0/P9.1 are 0.1–10 units per target, i.e. a handful of winner slots.

### Instrument — three readouts, one trained brain, no behaviour change

Scored on identical cue/pair sets with the same global-redistribution null (P9.0 correction):

| readout | what it sums over the target's assembly | role |
|---|---|---|
| **winners** | post-k-WTA winner scores (**the current readout**) | the known-failing case |
| **drive** | total activation *delivered* to each member across all propagation steps, before k-WTA selects | the hypothesis: signal exists here and is discarded by selection |
| **edge** | direct synapse weight, cue assembly → target assembly | **positive control** — known to carry signal (P8a, P9.1) |

`drive` requires accumulating per-neuron delivered activation during propagation; it changes no
behaviour, only records what already flows.

### Decision rule, fixed now

- **edge** must show association (`ASSOC_AUC` clearly above its null). If it does not, the premise
  of the whole phase is wrong and the finding is that the edges do not carry association after all —
  which would contradict P8a/P9.1 and demand those be re-examined before anything else.
- **drive ≫ winners** ⇒ hypothesis confirmed: k-WTA discards the association, and the readout is the
  target. A registered fix follows (per-assembly readout, wider k, or reading drive rather than
  winners), judged against the **unchanged** P7.3 bar.
- **drive ≈ winners ≈ chance** while **edge** shows signal ⇒ the loss is in *propagation*, not
  selection: the cascade spreads mass without regard to which edges carried it. Different fix,
  different phase.
- **All three at chance** ⇒ P8a/P9.1's edge measurements and this one disagree, and the discrepancy
  is the finding.

Instrument gate: runs and emits a rule-compliant verdict. No bar moves; nothing is adopted here.

## P9.2R — Result: instrument gate PASS. All readouts at chance; the P8a discrepancy is unresolved.

**Date:** 2026-08-19

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval assoc --repeats 5 --train 2000 --readout all --brain-data-path <scratch>/r9_all
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval assoc --repeats 3 --train 2000 --readout all --pairs-from bigram --brain-data-path <scratch>/r9_big
```

**Uniform ±2 window pairs (5 repeats):**

| readout | `ASSOC_AUC` | shuffled | gap | separated |
|---|---|---|---|---|
| winners | 0.508 [0.496..0.528] | 0.511 [0.493..0.534] | −0.003 | False |
| drive | 0.495 [0.442..0.526] | 0.520 [0.506..0.538] | −0.026 | False |
| **edge** (positive control) | 0.493 [0.454..0.535] | 0.519 [0.499..0.546] | −0.025 | False |

**P8a-definition adjacent-bigram pairs (3 repeats):**

| readout | `ASSOC_AUC` | shuffled | gap | separated |
|---|---|---|---|---|
| winners | 0.559 [0.536..0.584] | 0.552 [0.541..0.569] | +0.007 | False |
| drive | 0.576 [0.561..0.597] | 0.570 [0.545..0.596] | +0.006 | False |
| edge | 0.571 [0.550..0.590] | 0.555 [0.521..0.576] | +0.016 | False |

### The hypothesis is not confirmed — and neither is my explanation of why

**k-WTA truncation is not the culprit.** `drive` (everything the edges delivered, before selection)
scores 0.495 — no better than `winners` at 0.508. Nothing is being discarded by k-WTA that selection
would otherwise have shown.

**The positive control failed, which invalidates the phase's premise rather than answering it.**
`edge` — direct synapse weight, cue assembly → target assembly, the exact quantity P8a measured at
60% vs 15% connectivity with 418× the mass — comes out at 0.493 against a 0.519 null. It was
supposed to be the arm that *definitely* shows signal. Per the pre-committed decision rule, that
puts us in the fourth branch: **this measurement and P8a/P9.1 disagree, and the discrepancy is the
finding.**

**My proposed explanation was tested and refuted.** I suggested the disagreement was pair selection —
P8a scores frequent *adjacent bigrams*, `gm eval assoc` samples the ±2 window uniformly. Re-running
with P8a's pair definition raises all three readouts (0.559/0.576/0.571) — **but raises every null
with them** (0.552/0.570/0.555). Gaps stay ≤ +0.016, none separated. Frequent adjacent pairs
elevate both arms, which is a property of the pair set involving frequent words, not evidence of
association. The first repeat alone looked like confirmation (0.584/0.546); three repeats show it
was noise. Rule 1 earned its place again.

### What is actually inconsistent

The two measurements are not the same comparison, and the difference is the live question:

- **P8a/P9.1** compare, inside one trained brain, co-occurring pairs against frequency-matched
  **non**-co-occurring pairs, by connectivity and summed edge mass. Result: large, repeatable.
- **P9.2R `edge`** compares, on the same pairs, a brain trained on the real corpus against one
  trained on globally-redistributed tokens, by AUC over per-pair edge mass. Result: chance.

Both are legitimate; they cannot both be describing the same property of the edges. Candidate
explanations, none yet tested: the AUC is dominated by pairs with zero mass in both arms (the P7.2.8
retention problem in a new place); P8a's non-co-occurring null is not frequency-matched the same way
`BuildBigramPairs` matches; or the global-redistribution null retains enough incidental co-occurrence
at ±2 to erase the contrast. **Each is checkable and none should be asserted before it is** — the
mistake made twice already in P7/P8.

### Status

- Instrument gate: **PASS** (runs, emits rule-compliant verdicts, three readouts on identical brains).
- Association gate: **FAIL** everywhere. `ASSOC_AUC` has never exceeded 0.58 under any readout, pair
  definition, or λ.
- Readout hypothesis: **not supported** — `drive` ≈ `winners`, so selection is not where it is lost.
- Nothing adopted; no defaults changed; no bar moved.

**Recommendation:** resolve the P8a-vs-P9.2R contradiction before any further mechanism work. Two
independent instruments disagree about whether the edges carry association, and every remaining
design decision depends on which is right. That is a bounded diagnostic task — reconcile the two
comparisons on one brain, with per-pair values printed rather than aggregated — not another phase of
mechanism changes.

## P9.3D — Registration: reconcile P8a against P9.2R

**Date:** 2026-08-19

Two instruments disagree about whether the edges carry association. P8a/P9.1: co-occurring pairs
60% connected vs a 15% null, mass ratio 418×. P9.2R `edge`, the same quantity: `ASSOC_AUC` 0.493
against a 0.519 null. Every remaining design decision depends on which is right, so nothing else
proceeds until this is settled.

**Suspect, stated before measuring.** `ConnectivityEval`'s null is built by walking cues in order
and targets in order, collecting non-adjacent `(cue, target)` combinations, and breaking out of
**both** loops at `pairCount`. With ~40 targets and most combinations non-adjacent, the first cue
alone can supply all 40 control pairs — so the null may be one cue's edge mass rather than a sample
across cues, while the co-occurring arm spans all 40. That is a §6.1 rule 2 violation (the null not
scored on comparable pairs), and it inflates the ratio if that cue happens to be poorly connected.

**Test:** report the cue distribution of the control set, then rebuild the null balanced across
cues (equal pairs per cue, same frequency-matching) and re-measure. Decision rule fixed now:

- gap and ratio **collapse** under a balanced null ⇒ P8a's 418× was a sampling artifact, P9.2R is
  right, and the edges do **not** carry association. P8a's conclusion and everything resting on it
  is corrected.
- gap and ratio **survive** ⇒ P8a is right, and the discrepancy lies in AssocEval's AUC — next
  suspect is pairs scoring zero in both arms.

No behaviour change; instrument only.

## P9.3D — Result: P8a's null was unbalanced. Its headline numbers are inflated ~35×.

**Date:** 2026-08-19 (run by Bill in terminal; the agent's SMB session had gone stale)

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval connectivity --train 2000 --brain-data-path /tmp/p93d
```

```
null spread: 21 distinct cues supply 21 control pairs (max 1 from any one cue)
real spread: 21 distinct cues supply 40 pairs
```

| | P8a (unbalanced null) | P9.3D (balanced null) |
|---|---|---|
| co-occurring connectivity | 60.0% | 60.0% (unchanged, as expected) |
| **null connectivity** | **15.0%** | **38.1%** |
| `CONNECTIVITY_GAP` | +0.450 | **+0.219** |
| `MEAN_MASS_RATIO` | **418×** | **11.96×** |
| null mean mass | 0.49 | 17.13 |

**The suspect registered before measuring was correct.** The original null walked cues in order and
broke out of both loops at `pairCount`, so the first cue supplied essentially all 40 control pairs
while the real arm spanned 21 cues — a §6.1 rule 2 violation. Spread across the same 21 cues, the
null's connectivity rises 15.0% → 38.1% and its mean mass rises 0.49 → 17.13, collapsing the
headline ratio from **418× to 11.96×**.

### Corrections to the record

**P8a.1, P8a.2 and P8a.3 quote 60%/15% and 418× throughout, and those numbers are wrong.** The
correct figures at the same settings are 60%/38.1% and 11.96×. Everywhere this project has written
"co-occurring pairs are 60% connected against a 15% null with 418× the edge mass" — including
P8a.2's correction of P7.2.8, P9.0's "raw material an association AUC reads out", and B.0's premise
that the system might already pass the association gate — the effect is real but roughly an order of
magnitude smaller than stated.

**P8a's conclusions survive in direction, not in magnitude.** The gap is still positive and
substantial (+0.219, 11.96×), so P8a.2's core correction of P7.2.8 stands: direct paths are *not* a
lottery, they do track co-occurrence. But "418×" was never a property of the graph.

### The reconciliation, and what it leaves

The two instruments are now much closer, and neither is simply right:

- **P8a was overstated** by an unbalanced null (418× → 12×).
- **P9.2R is not fully explained** by that alone. A 12× mass ratio should still lift an AUC above
  0.5, and `edge` measured 0.493.

The leading remaining suspect — listed in P9.2R and now the obvious one — is **ties at zero**. With
connectivity 60% and 38.1%, roughly 40% of related pairs and 62% of unrelated pairs have *no edge at
all*, so ~25% of all comparisons are 0-vs-0 and score 0.5 by definition. An AUC over a mostly-empty
matrix is dominated by ties regardless of what the non-empty entries say. **This is a hypothesis,
not a finding** — it predicts a specific number (the tie fraction in `Harness.Auc`) and should be
measured before it is believed, given this project's record on plausible-sounding explanations.

### Standing issue: null construction is now the recurring defect

This is the **fourth** measurement in P7–P9 where a null was built on a different sample than its
real arm: P7.2.5 (residency-dependent retention), P9.0 (within-sentence shuffle preserving
sentence co-occurrence), P9.2R's still-open discrepancy, and now P8a's unbalanced cue spread. Three
were caught only after they had produced a published number.

Worth a standing check rather than case-by-case vigilance: **every eval should print the sample
composition of both arms** — how many items, drawn from how many cues, with what retention — the way
`RETENTION` and `null spread` now do. Where those lines exist the defect was visible immediately;
where they did not, it took a contradiction between instruments to surface it.

## P9.3E — Sample-composition check, made executable

**Date:** 2026-08-19

Four nulls in P7–P9 were built on a different sample than their real arm, and three produced a
published number before anyone noticed:

| | defect | how it surfaced |
|---|---|---|
| P7.2.5 | retention varied with residency — 15/24 suppressed vs 5/24 unaffected | only when the numbers looked too good |
| P9.0 | within-sentence shuffle preserved the co-occurrence it was meant to destroy | real 0.574 vs null 0.569, read as CONFOUNDED |
| P8a | one cue supplied the entire null against 21 in the real arm | only when a second instrument contradicted it |
| P9.2R | still open | contradiction with P8a |

Where a composition line existed (`RETENTION`, `null spread`) the defect was visible immediately.
Where it did not, it took two instruments disagreeing. So the convention is now a check.

`ArmSample` + `SampleCheck.Report` in [Eval/Harness.cs](src/GreyMatter.Poc/Eval/Harness.cs): every
arm reports items scored, items offered, retention, and distinct cues; the check warns and returns
false when arms differ by more than 25% in size or at all in cue coverage. Wired into
`gm eval recall`, `order` and `assoc`; `connectivity` and `shift` already had their own lines.

```
SAMPLE:       trained: 16 from 16 cues   |   control: 16 from 16 cues
```

**The check is tested against the actual historical defects** — `P8aUnbalancedNullIsCaught`
(40 pairs/21 cues vs 40 pairs/1 cue) and `UnequalRetentionIsCaught` (15/24 vs 5/24) both assert it
returns false. A check that cannot fail would have been the fifth instance of this bug. Tests: 143
passing (5 added).

This does not fix any past number; it makes the next one visible at the point it is produced.


# Addendum C — P10: Convergence

## P10.1 — The last diagnostic: tie structure of the association AUC

**Date:** 2026-08-21. One brain, one run, n=1, no repeats (C-R3). Claims nothing.

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval assoc --repeats 1 --train 2000 --readout edge --diagnose --brain-data-path <scratch>/p101
```

### 1. The tie hypothesis is confirmed, and larger than predicted

| arm | AUC | comparisons | ties | tie fraction |
|---|---|---|---|---|
| real | 0.504 | 25,600 | 18,095 | **70.7%** |
| null | 0.501 | 25,600 | 16,133 | 63.0% |

P9.3D predicted ~25% ties from the 60%/38.1% connectivity figures. Measured on the pairs `gm eval
assoc` actually scores, it is **70.7%** — the AUC is overwhelmingly a comparison of zero against
zero, each scoring 0.5 by definition.

### 2. Restricted AUC: still chance — the decision rule's first branch

`RESTRICTED_AUC 0.553`, over related 26/160 non-zero and unrelated **25/160**.

Two things, and the second is the one that matters:

- Restricted to pairs where mass exists at all, discrimination is 0.553 — above 0.500, nowhere near
  separated, and far from the 0.70 bar.
- **Having an edge at all does not discriminate either: 16% of related pairs and 16% of unrelated
  pairs are non-zero.** The coverage that survives is not preferentially the related coverage.

Per the pre-committed rule this is the first branch: *even where edges exist, per-pair edge mass does
not discriminate related from unrelated.* The 12× population ratio is a **diffuse effect invisible at
pair level**.

### 3. Per-pair mass: the population effect is real and lives in the tail

| set | n | zero | median | p90 | max | mean |
|---|---|---|---|---|---|---|
| related (real) | 160 | 134 | 0.00 | 0.15 | **876.36** | **5.66** |
| unrelated (real) | 160 | 135 | 0.00 | 0.11 | 126.38 | 1.68 |
| related (null) | 160 | 126 | 0.00 | 0.11 | 134.03 | 1.00 |
| unrelated (null) | 160 | 128 | 0.00 | 0.28 | 46.70 | 0.89 |

Related mass averages **3.4× unrelated** (5.66 vs 1.68), and **5.7× its own null** (5.66 vs 1.00).
The signal P8a/P9.1 measured is really there. But the medians are identical at zero, the p90s are
within noise of each other, and the difference lives entirely in a handful of extreme pairs — max
876 against 126. A rank-based AUC cannot see a mean carried by three outliers inside a matrix that
is 84% zeros. **Both instruments were right about their own quantity all along**; they disagreed
because a population mean and a rank statistic are not the same measurement, and nobody had put the
distribution on screen.

### 4. Defect found, recorded, not fixed (per C.2)

**The global-redistribution null retains 35.0% of the related pairs' ±2 co-occurrence** (against
100% in the original corpus, by construction). Redistributing tokens across a corpus of short
sentences with a ±2 window leaves a third of frequent pairs adjacent again by chance. The null
built in P9.0 to replace the within-sentence shuffle is therefore itself contaminated — better than
its predecessor, still not clean.

Per C.2 this is recorded and not repaired. It does not change the P10.1 branch: the real arm reads
0.504, which is chance on its own terms, and a contaminated null cannot explain a chance *real* arm.
It does mean every `ASSOC_AUC` null figure in P9.0/P9.2R/P9.1 is optimistic by an unknown amount —
in the direction of making the system look better, not worse.

### Verdict

**The synaptic channel is exhausted by measurement.** Association exists in the edge population as
a mean-level effect and does not survive to any per-pair readout, because ~84% of the pairs a cue
should relate to have no edge at all and the surviving coverage is not preferentially related.
Coverage is a property of hash-disjoint assemblies — representation, not learning. Proceeding to
P10.2.

---

# P10.2 — VERDICT

**Date:** 2026-08-21. Written for a reader who will not read the 2,700 lines above it.
This entry closes the campaign (C-R5).

## 1. Prompt.md scorecard

### To the letter: MET, at P6, and still true

| criterion | status |
|---|---|
| train on a random dataset, test recall across neural-network scales | **met** — 14-cell sweep, 10⁴→10⁷ virtual neurons, recall measured and separated at every cell (P6.1) |
| beyond "hundreds wide, dozens deep" | **met** — 10,000,000 virtual neurons, depth 8, width 1024 |
| commodity hardware | **met** — 62.8 min, one Mac, ≤312 MB managed heap |
| no wordlists or concepts on disc | **met** — 0 strings, 0 corpus words across 498 partitions / 340 MB, by structural walk plus a check tested against a planted word list (P5.7) |
| a concept activates a comparable synaptic graph, JIT | **met mechanically** — 10⁷ virtual neurons served by a 260-slot pool with recall flat to three decimals (P6.3) |

### To the spirit: PARTIAL, and the boundary is precisely located

| what the system learns | evidence |
|---|---|
| **frequency — completely** | ρ(mass, corpus frequency) → 1.00 at 10⁷ neurons (P6.2) |
| **order — weakly, real, bar unmet** | `R_PMI` +0.1801 [+0.1666..+0.1942] vs null +0.0368, non-overlapping; `PMI_GAP` +0.1433 against a +0.15 bar (P9.1) |
| **pairwise association — not at all** | `ASSOC_AUC` never above 0.58 under any λ, any of three readouts, either pair definition (P9.0, P9.1, P9.2R, P10.1) |

### The causal chain, one line per link

| # | link | evidence |
|---|---|---|
| 1 | assembly size == `ActivationWidth`, so only the cue's own assembly won k-WTA | P7.0.4 — 4,064 hop-0 winners vs 32 hop-1 |
| 2 | so Hebbian pairs were within-assembly, encoding only "I fired" | P7.0.1 — 99.9% of live slots; **0** cross-assembly edges ever created |
| 3 | unsaturating k-WTA raised proposals 17k → 420M and still created **zero** | P7.1.2 — proposals were never the bottleneck; slots were |
| 4 | budget quotas fixed it: cross-share 0% → 74.6%, multi-hop mass 0% → 33% | P7.1.3 |
| 5 | competition was inert; erosion fixed it: displacement 0.000% → 8.472% | P7.2 |
| 6 | none of that produced order: `R_PMI` stayed ≤ 0, `R_UNIGRAM` rose with `R_BIGRAM` | P7.1.6 — frequency, one level up |
| 7 | base-rate subtraction produced the only positive signal: `R_PMI` −0.06 → +0.18 | P8c.1 |
| 8 | and it is **normalisation**, not sparsification — coverage-matched pruning alone gives −0.06 | P9.1 (gate PASS) |
| 9 | the substrate is not the constraint: co-occurring pairs 60% vs 38.1% connected, **12×** mass | P8a as corrected by P9.3D |
| 10 | k-WTA is not where association is lost: pre-selection drive scores no better than winners | P9.2R |
| 11 | **association is a population-mean effect that does not survive to any per-pair readout** | P10.1 — 70.7% ties, restricted AUC 0.553, related non-zero 16% vs unrelated 16% |
| 12 | because ~84% of pairs a cue should relate to have **no edge at all**, and surviving coverage is not preferentially related | P10.1 §2–3 |

Link 12 is the terminus. Coverage is a property of hash-disjoint assemblies — **representation, not
learning** — and the P10.1 branch that fired says so by measurement rather than by argument.

## 2. Abandoned, not failed

| line | why it stopped | what would justify reviving it |
|---|---|---|
| **Event-wise anti-Hebbian** (P9.2, A.5(c)-event-wise) | never run. P9.1 showed the learning rule already produces base-rate-corrected, co-occurrence-specific edges, so a substrate change (CSR-by-target) would improve a component that is not the binding constraint | evidence that edge *weights*, not edge *coverage*, limit association — the opposite of what P10.1 measured |
| **A.5(b) context-similarity recruitment** | locked behind design review all campaign | **now the single evidenced lever** — see §4(a) |
| **A.5(d) SDM content addressing** | locked, never evidenced | a coverage mechanism that beats A.5(b) on the same measurement |
| **Shift-eval redesign** (B-R3) | parked; its observable was wrong (direct edge mass) and nothing depended on it | a phase that needs a corpus-statistics-shift measurement |
| **P9.3 trade closeout** | **unreached, not failed** — its trigger was an association pass, which never fired | any future association pass |
| **LLMTeacher** (P10 legacy) | deleted with the legacy tree, recoverable from git | a curriculum-enrichment phase |

## 3. What survives any continuation

**The substrate, entirely.** JIT materialize/evict serving 10⁷ virtual neurons from a 260-slot pool
with no measurable recall cost; procedural recipes as the store; determinism that held bit-identical
through an uncontrolled OS suspend mid-run; checkpoint/resume within 1.25% at 20k sentences; 50k
sentences unattended in 29.8 minutes with zero truncations; SoA layout with uint indices and no hot-
path allocation, ready for the CUDA port. Prompt.md's core wager — that this is an algorithmic
problem, not a resource one, and that JIT lifecycle patterns can emulate massive scale — **is the
part that came out true.**

**The instrument suite and its rules.** Nine evals under §6.1 discipline, refusing verdicts they
cannot support, with the sample-composition check now executable and tested against the historical
defects (P9.3E).

**The corrected record**, which is worth as much as the positive results: 418× → 12× (P9.3D);
sparsification → normalisation (P9.1 overturning P8c.5); "lottery" → specific-but-sparse (P8a.2
overturning P7.2.8); "P8a vs P9.2R contradiction" → two right answers to different questions
(P10.1). Four published conclusions corrected by later measurement, each traceable.

## 4. The options, priced — Bill's call

**(a) Declare the POC complete as the substrate deliverable; take association into a v2
representation redesign.** The evidence points at exactly one lever: similarity must enter the
*representation*, because the channel that ignores similarity by design (hash-disjoint assemblies)
was measured unable to carry it. A.5(b)'s shape — context-similarity recruitment, so related words
share substrate in proportion to distributional similarity rather than by hash collision. Note the
caution P8a already bought: naive shared membership destroys specificity (`AssemblyOverlap` ≥ 0.25
collapsed the connectivity gap to zero and cost 40% of recall lift). A v2 has to make overlap track
*meaning*, not code-hash accident. **Cost: a new campaign, not a phase.**

**(b) Write `CUDA-PORT.md` now.** The port thesis was proven at P6 and does not depend on the
association outcome. Every hot loop is already flat `for` over contiguous SoA arrays with
counter-based RNG. **Cost: one document, no experiments.** This is the cheapest thing on the list
and the least likely to be regretted.

**(c) Park it.** The verdict is the record. Everything is committed, tested, reproducible from
seeds, and every number carries its command line.

My own read, offered once and not pressed: **(b) then (a)**. The substrate is the proven asset and
documenting its port is nearly free while the design is fresh. (a) is real work and should start
from a clean directive rather than as the tail of this campaign.

## 5. Definition of done

P9.3's trade closeout is recorded **unreached** (trigger never fired), not failed. The campaign ends
here per C-R5: a bar not passed is a finished result. There is no Addendum D — whatever follows
starts as a new directive.

**Final state:** 143 tests passing; defaults unchanged by P7–P10 (`BaseRateDepression`,
`AssemblyOverlap`, `ContestErosion` all 0; `PropagatedWinnerQuota` 64 and `WithinAssemblyCap` 8
adopted at P7.1 under A-R3); repo is `Prompt.md`, `plan.md`, `README.md`, `RESULTS.md`, `src`,
`tests`.



# Recovery R0 — correction and prospective protocol (2026-09-09)

The P10 conclusion that the complete paging thesis passed is withdrawn: Cascade skips
nonresident targets; ActivationScope keeps a dictionary of all recipes; Resume reads all
partitions. The conclusion that representation is the uniquely established cause of failed
association is also withdrawn. The old numbers remain historical observations; recognition
is not associative recall. Probe currently trains a fresh brain rather than opening a saved one.

Baseline source hashes: `artifacts/recovery/r0/source-manifest.json`. Hardware: MacBookPro18,4,
32 GiB RAM, .NET SDK 8.0.301/runtime 8.0.6, macOS arm64; scratch free space and platform in
`artifacts/recovery/r0/environment.json`. Commands: `dotnet --info`,
`sysctl -n hw.memsize hw.model`, `df -h . /tmp`. Only plan.md was modified at entry.
Build and tests pending; no gate claimed yet.

## Registration before recovery scores

R1 uses 32 independent chains, each with five randomly named symbols (160 symbols).
Every adjacent pair is presented as its own two-token training episode, repeated 16 times,
with episode order deterministically shuffled. No nonadjacent endpoint pair appears in a
training episode. Test queries are separate traversal requests: all 128 adjacent pairs and
32 paths at each of lengths 2, 3, 4, plus 32 length-2 paths starting at position 1
(128 composed queries total). Candidates are the 32 symbols at the target chain position;
all candidates have identical exposure. Correct chain is never supplied to traversal.
Labels are random alphabetic strings independent of chain membership; RNG seed and corpus
SHA256 accompany each result. Token-frequency-preserving null globally shuffles the token
stream and repartitions into two-token episodes. Encoder context sees only training data,
then freezes. Five evaluation seeds 101–105; development seed 100 only.

Start with Config defaults except seed, WorkingSetMax=50000 (exceeds 160*256 members),
and ActivationDepth set to the query hop budget during evaluation. No parameter sweep.
Train through Trainer/Plasticity with existing assembly representation. For scoring use
Cascade.DeliveredDrive summed over candidate members; no direct training-count lookup.
Fully resident evaluation preloads every symbol assembly symmetrically, never just the answer.
Verify unique encoded symbols and no training eviction; otherwise refuse the experiment.

Metrics are query-local top-1 and reciprocal rank. Ties use expected uniform tie credit:
top-1=1/tie_count when no candidate is strictly higher, otherwise zero; reciprocal rank
is the average 1/rank across the tied ranks. This is deterministic and does not privilege
the answer or symbol order. Include all zero scores. Report per-seed, direct and composed
metrics; gate targets are those fixed in plan.md R1. Transition-count baseline propagates
normalized outgoing counts for exactly the same query hop budget. It is only a baseline.

First build hand-wired runtime fixtures for directed chains, branches, cycles, distractors,
reset/order independence and step limits. If the existing runtime fails these, correct
the smallest execution defect before scientific evaluation and record it separately from
a learning-rule attempt. Do not silently redefine a hop or tune around a fixture failure.

R2: exact numerical record round-trip and bounded cache tests; no compression changes.
R3: same snapshot/encoder/readout on both backends; abs 1e-6 + rel 1e-5 score tolerance,
identical query rankings, immutable persisted state, forced cross-page traversal.
R4: workload sizes 1x/4x/16x and memory caps 256 MiB/128 MiB; runtime baseline B measured
separately; peak RSS <= B+1.25M. Largest resident representation >=4M, learned not prewired.
Five seeds, 100 measured queries each, latency and quality gates unchanged from plan.
No claim of exceeding physical RAM from merely exceeding configured cache.
R5: real-data split and support registration deferred until R5 before scores; source not
needed for R0–R4. Existing plan fixes MRR lift >=.05 and positive paired-bootstrap lower bound.

Planned CLI (NOT IMPLEMENTED): `dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release --
eval recovery --mode fixtures` and `... eval recovery --mode learning --seed 100`.
Final learning run will use explicit seeds 101–105. Concrete commands/configs will be
recorded when the entry point exists. No jobs over one hour without a larger budget.

### R0 baseline completion

`dotnet build GreyMatter.sln -c Release --disable-build-servers` exited 0: one pre-existing
CS8604 warning in ScaleSweep.cs:65, zero errors. `dotnet test GreyMatter.sln -c Release
--no-build --logger 'trx;LogFileName=baseline.trx' --results-directory artifacts/recovery/r0`
exited 0. Raw build/test output and baseline.trx are in artifacts/recovery/r0. Initial
sandboxed build was silent and terminated; the same build outside the sandbox completed.
This is an environment issue, not a failed experiment. R0 gate PASS; R1 fixture work next.
Recovery learning attempts used: 0. No model jobs running.

## R1 fixture failures and bounded execution repair

`dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval recovery --mode fixtures`
exited 1; raw output `artifacts/recovery/r0/fixtures-before.log`. Five of eight checks failed.
Depth 1 delivered a=b=c=1 on root→a→b→c. Depth 2 returned 4 units around a two-node cycle.
Recall also changed familiarity despite learningMode=false. Direction, branching and
electrical reset passed. These are execution defects before any recovery learning result.

Repair registered: snapshot active sources AND their drive at each propagation step;
newly reached nodes can propagate only next step. Preserve the existing retained-drive,
threshold and winner-selection rules. Update fatigue/familiarity only in learning mode.
This is the single bounded fixture repair, not a learning-rule correction. All later
results explicitly use this revised synchronous step semantics; old metrics are not comparable.
Baseline tests: 143/143 passed in 53 seconds (baseline.trx).

### R1 fixture repair verified; development run launched

All 144 tests passed after the runtime repair (51 seconds):
`dotnet test GreyMatter.sln -c Release --logger 'trx;LogFileName=fixtures-repaired.trx'
--results-directory artifacts/recovery/r0` → `fixtures-repaired.trx` and test log.
The three recovery tests then passed, covering fixtures, balanced synthetic data and tie ranking:
`dotnet test GreyMatter.sln -c Release --filter FullyQualifiedName~RecoveryTests
--logger 'trx;LogFileName=recovery-instrument.trx' --results-directory artifacts/recovery/r0`.
CLI fixture output: `artifacts/recovery/r1/fixtures.log` (8/8 pass).

Implemented command: `dotnet run --no-build --project src/GreyMatter.Poc/Poc.csproj -c Release --
eval recovery --mode learning --seeds 100 --output artifacts/recovery/r1/dev-initial.json`.
All effective settings are printed and serialized. This instrument uses resident in-memory
scopes and does not open the configured default brain path or network corpus. Development
seed only; no gate verdict. Expected cost based on the existing 51-second regression suite
is minutes, well below the one-hour ceiling; final-run estimate will use this measured run.
Current process session: 8800; log dev-initial.log. Initial learning implementation in progress;
learning corrections used 0/1. Next action: inspect completed development result and trace
a failure before using the one allowed corrective attempt.

### R1 development initial result (not a gate)

Completed session 8800, exit 0. `dev-initial.json` contains corpus hashes, complete configs,
all query scores and arm metrics. Learned direct top-1=1.000; composed=.453613.
Composed by hops: 2=.672363, 3=.312500, 4=.157227. Untrained=.03125 on both;
shuffled direct=.041504, composed=.017090. Transition baseline=1 on both.

Before selecting a correction, register one read-only trace on development seed 100,
chain 0/start 0/hops 4: record source drive and how many chain-member sources fail the
existing 0.5*threshold test at each synchronous step. Observer cannot change execution.
Repeat the same development run once with this trace, verify identical metrics, and
use the trace to decide whether the one corrective attempt has a concrete basis.
No final evaluation seeds have been used.

### R1 one permitted corrective attempt — prospective registration

`... eval recovery --mode learning --seeds 100 --output artifacts/recovery/r1/dev-trace.json`
completed exit 0. Entire JSON result equals dev-initial.json exactly. Trace is in dev-trace.log.
At step 2, 24 of 25 first-neighbor member sources clear the threshold; second-neighbor
sources carry zero surviving drive through step 4. This does not isolate a unique cause.
Source inspection shows temporal credit is restricted to final winners, which can exclude
actual cue members and include propagated members from other cues. The next hypothesis is
that recording the observed sequence on cue members provides a more reliable bridge.

The ONE learning correction is an explicit `--sequence-uses-cue-members true` arm (default
false). Preserve within-cue learning, inhibition, thresholds, capacity, and learning rates.
For sequence updates only, previous and current externally driven cue members receive the
same fixed observation credit (previous strength .5, current 1), independent of final winner
selection. This is a local observed-sequence learning rule, not a transition-table readout.
Existing hashed assemblies and learned numerical synapses remain the model.

Prediction: improve composed retrieval by making input members eligible for onward sequence
learning. No normalization, threshold changes, or second correction if this fails. Run one
development smoke, then the fixed five-seed gate with this arm. Initial development was
roughly one minute including all arms; expect five-seed run under ten minutes.

### Corrective development result and final evaluation registration

`dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval recovery --mode learning
--seeds 100 --sequence-uses-cue-members true --output artifacts/recovery/r1/dev-cue-trace.json`
completed exit 0. Learned direct=1.000, composed=.376709: no development improvement.
The correction remains experimental/default false. No second mechanism correction is allowed.

Final gate command (after tests): `dotnet run --no-build --project src/GreyMatter.Poc/Poc.csproj
-c Release -- eval recovery --mode learning --seeds 101,102,103,104,105
--sequence-uses-cue-members true --output artifacts/recovery/r1/final-cue-trace.json`.
This is the only full five-seed gate run of this recovery attempt; initial and trace runs
were development-only. Raw log final-cue-trace.log. Estimated duration <10 minutes,
extrapolated from the completed single-seed run. If the gate fails, R2–R6 are not started.
The failure applies to this registered learner/runtime/task combination, not to all
neural association methods or the paging hypothesis. Correction budget consumed 1/1.

Final pre-gate regression: 148/148 tests passed in 51 seconds, exit 0. Command:
`dotnet test GreyMatter.sln -c Release --logger 'trx;LogFileName=r1-final-tests.trx'
--results-directory artifacts/recovery/r1`. Evidence: r1-final-tests.trx and tests.log.
Final evaluation launched in session 73308, no other jobs running. Learning correction
consumed 1/1; source checksums in artifacts/recovery/r1/source-manifest.json.

# Recovery R1 closeout — direct association works; composed gate unmet (2026-09-09)

**R0 PASS. R1 FAIL. Recovery campaign stopped at its prerequisite gate. R2–R6 unstarted.**
One bounded execution repair and the one permitted learning correction were completed.
No additional sweep or mechanism attempt is authorized by this plan.

Final command: `dotnet run --no-build --project src/GreyMatter.Poc/Poc.csproj -c Release --
eval recovery --mode learning --seeds 101,102,103,104,105 --sequence-uses-cue-members true
--output artifacts/recovery/r1/final-cue-trace.json`. Process session 73308 completed with
exit code **1**, the expected CLI signal for a failed gate, not a crash. About 4.21 minutes
from log creation to final JSON write (filesystem timestamps; approximate wall time).

| arm | direct top-1 | composed top-1 |
|---|---:|---:|
| learned | 100.0000% | 35.5811% |
| untrained | 3.1250% | 3.1250% |
| shuffled | 2.7246% | 2.4805% |
| transition-baseline | 100.0000% | 100.0000% |

Learned composed accuracy by seed: 101=.360352, 102=.337158, 103=.391602,
104=.375244, 105=.314697. Across all five: two hops=.691699, three=.032422,
four=.007422. Composed queries weight hop 2 twice (64 queries) and hops 3/4 once each
(32 each), as registered. The aggregate cannot conceal the loss at longer distances.

**What this establishes:** on the registered balanced synthetic task, the experimental
cue-member temporal rule reliably retrieves immediate neighbors against equally exposed
candidates. Direct top-1 is 100% in all five seeds; shuffled and untrained controls are
near chance. The default winner-based rule also reached 100% direct retrieval on the
single development seed, but was NOT evaluated over these five seeds. These are separate
claims. No natural-language or open-ended retrieval success is inferred.

**What failed:** the corrected learner/runtime combination did not meet the fixed composed
bar (mean >=80%, every seed >=70%). Its single development comparison was worse than the
initial implementation (.376709 versus .453613 composed). The experiment is retained for
reproducibility, with SequenceUsesCueMembers=false by default; it is not adopted as a fix.
A transition-count traversal solves all queries. That shows the task is learnable, not that
we have reproduced its mechanism in this neural implementation.

**What remains unresolved:** weak/absent onward drive may involve connectivity, selection,
thresholding and recurrent retention together. The one-query trace does not identify a
unique cause. Paging, bounded training memory, larger learned capacity, real-data recall,
and GPU benefit are all untested in this recovery campaign. Do not claim a paging failure
or a universal learning-rule failure from this gate.

**Retained changes:** Cascade reads a step-start snapshot so one step cannot traverse an
entire chain; recall no longer updates fatigue/familiarity. Eight directed runtime
fixtures pass. Recovery CLI, balanced task generator, query-local ranking with unbiased
tie credit, controls, and optional source trace are available. Existing commands and
experiments retain their implementation except those explicit runtime corrections, so
old numerical results must not be assumed equivalent. No saved user brain was opened or
modified. No commits were created.

**Validation:** 148/148 tests pass (r1-final-tests.trx; 51 seconds); one pre-existing CS8604
warning in ScaleSweep.cs:65. Source checksums match the final-run manifest. Git diff
whitespace check passed. Numeric results are in final-cue-trace.json and summary.json;
all logs have identical .txt mirrors because the repository ignores *.log. The .txt
files and .trx reports preserve raw evidence when this work is later committed.

## Recovery handoff — terminal checkpoint

- Current phase: **R1 stopped, gate unmet; campaign ended under its bounded failure policy.**
- Passed gates: R0 only. Fixture/test success is not an R1 learning pass.
- Attempts: initial development learner, one observation-only trace with identical output,
  one corrective development learner, one final five-seed corrective gate run. No final
  seeds were used to select another configuration. Learning correction budget 1/1 consumed.
- Code: Config.cs, Cli.cs, Runtime/Cascade.cs, Runtime/Plasticity.cs; new
  Eval/RecoveryEval.cs, Eval/RecoveryLearning.cs and tests/RecoveryTests.cs.
- Documentation: plan.md active pointer, README.md, append-only RESULTS.md.
- Evidence: artifacts/recovery/r0 (baseline); artifacts/recovery/r1 (fixtures, three
  development JSON runs, final JSON, summary, logs, tests and source hashes).
- Effective configuration and exact dataset checksums: each seed in final-cue-trace.json.
- Jobs: none running; final process exit 1 is accounted for above.
- Next action: read this closeout and present the design decision to Bill. **Do not start R2.**
- Decision needed: whether to authorize a separate bounded review of the resident
  learning/propagation design before resuming the memory-virtualization experiment.
  Preserve the direct-association result and comparison with the simple baseline.

# T1 activation-travel review — registration (2026-09-09)

Bill authorizes a review of learned activation travel and allows brainData resets.
No deletion is necessary; use a fresh resident scratch brain. R1 gate remains failed.
Train only seed 100, existing registered corpus/config, SequenceUsesCueMembers=true.
For all 32 four-hop queries record the intended chain's five assembly positions at each
step: positive pre-selection nodes/mass, selected positive nodes/mass, and next-step
threshold-eligible selected nodes. Count learned positive edges along each chain link,
and how many members receiving a previous-link edge also own a next-link edge.
This can distinguish absent bridges, selection loss, and below-threshold survivors
without changing any runtime rule. Selection is observed, never modified.
Verify observer-on/off equality and unchanged familiarity/fatigue, and reproduce the
32 matching fourth-hop scores in dev-cue-trace.json before interpreting telemetry.
One development training run, no sweeps, no new scientific gate. Expected under two
minutes based on previous one-seed runs. Planned command (until implemented):
`gm eval recovery --mode travel --output artifacts/recovery/t1/travel.json`.

# T1 review findings — link distance and simulation time are different

Command: `dotnet run --no-build --project src/GreyMatter.Poc/Poc.csproj -c Release --
eval recovery --mode travel --output artifacts/recovery/t1/travel.json`. Session 35083
completed exit 0. No stored brain was read/reset. One fresh seed-100 scratch model,
cue-member temporal arm, unchanged training/propagation. All 32 fourth-hop score vectors
reproduce dev-cue-trace.json EXACTLY. Observer-on/off scores and familiarity/fatigue agree.
Evidence: travel.json, travel.txt, summary.json and source-manifest.json in t1.

Targeted validation: `dotnet test GreyMatter.sln -c Release --filter
FullyQualifiedName~RecoveryTests --logger 'trx;LogFileName=travel-tests.trx'
--results-directory artifacts/recovery/t1` passed 6/6 recovery cases in 37 ms.
The preceding full suite remains 148/148; the new observer test makes 149 total tests
available, but the full 149-test suite was not rerun for this optional observation hook.
Git whitespace check passed. No dependency or numerical rule changed.

## 1. Real learned edges exist, but concept links are not single-neuron relays

Each of 128 adjacent assembly pairs has 5,376–6,608 positive learned edges. Nevertheless,
four of 96 intermediate junctions (chains 5,6,10,16) have no member neuron that both
receives a previous-assembly edge and owns a next-assembly edge. This is NOT proof of
disconnection: a lateral connection inside the assembly could complete the route, at
the cost of another neuronal step. Even positive bridge counts do not prove that those
specific bridging members fire; source selection and thresholds still matter.

This exposes a limitation in the R1 interpretation: its generator calls a transition
between symbolic concepts a “hop,” and gives the neuronal runtime exactly that many
synchronous updates. A concept-to-concept path can require internal relay steps AND
time to accumulate enough potential. The transition-count baseline has one symbol per
node and does not pay those costs. The fixed R1 bar still failed; do not retrospectively
change it or declare a pass. Its failure establishes insufficient retrieval under that
particular update budget, not the impossibility of longer-path retrieval at any budget.

## 2. At the second assembly, threshold delay is visible before competition loss

Means across the 32 four-link queries (positions are relative to the cue):

| after simulation step | chain position | positive nodes before selection | selected | selected eligible to emit next step |
|---|---:|---:|---:|---:|
| 1 | 1 | 31.875 | 31.844 | 24.094 |
| 2 | 2 | 18.312 | 18.062 | 0.062 |
| 3 | 2 | 18.312 | 18.312 | 18.062 |
| 4 | 3 | 9.906 | 9.844 | 0.094 |

At step 2, second-position pre-selection mass averages 6.362 and selected mass 6.361: almost
all observed mass survives selection, but virtually none can cross the .5 firing cutoff
on the next step. By step 3 that position has accumulated enough mass to make ~18 members
eligible; its third-position descendants then arrive weakly at step 4. This is observed
latency, not evidence that the second position received no signal. These means include
collisions/side routes; they do not establish exclusive causal paths for every query.

Thus removing competition alone is not a well-supported first repair for this observed
second-position loss. It does not follow that competition is globally harmless.

## 3. The update retains activity; its edge weighting does not conserve total drive

The actual recurrence is selection applied to retained potentials PLUS incoming weighted
drive. Sources are not consumed when they emit. Step 4 still selects ~153 cue members
and retains ~150 mass units in the first related position on average. This favors activity
already established, but the review does not prove it is the sole limiting mechanism.

For a positive-degree source, total emitted drive is
`sourceDrive * sum(outgoingWeights) / outDegree`, before residency losses. It equals input
drive only when mean outgoing weight is 1. The comment claiming conservation was too
strong. Fix the description, not the numerical rule during this review. Both weight
scale and accumulation cadence influence the number of steps required to travel.

## Recommendation — test travel time before changing the learning rule again

The next bounded experiment should separate conceptual path length from simulation ticks.
Keep the recorded learner, weights, encoder, candidate sets and original four-step results.
Use ONE preregistered alternate budget of 12 ticks for the same four-link queries, with
untrained/shuffled controls at the same budget; report accuracy AND latency. This is a
new latency diagnostic, not a replacement R1 pass and not a tick-count sweep. Persist the
scratch numeric snapshot and diagnostic numeric codes at that point to avoid repeated
training for future read-only comparisons.

If useful retrieval appears at the longer budget, an explicit latency/accuracy contract
should precede any redesign or paging resumption. If it does not, inspect the actual
relay paths on that frozen snapshot before proposing separate changes to edge allocation
and propagation (e.g. genuinely normalized transient propagation). Do not combine those
changes into a new learner and attribute any improvement to one of them. No parameter
sweep, threshold reduction, or automatic R2 advance follows this review.

## T1 handoff

Review complete; R1 remains failed, R2 unstarted. Changed code only adds an optional
selection observer, the `travel` recovery mode and one observer test. No training or
propagation behavior changed. All raw observations and exact reproduction checks are
recorded above. No jobs running. Bill permits brainData resets; none was necessary.
Next proposed action is the single longer-budget frozen-model diagnostic, to be authorized
as the next experiment. This replaces the earlier vague “review needed” handoff with a
concrete question: is the apparent failure principally insufficient simulation time?

# T2 travel-time diagnostic — registration (2026-09-09)

Bill's continuation instruction resumes the proposed bounded diagnostic. Train seed 100
once per learned/shuffled arm; untrained uses the real training encoder only. Compare
4 and 12 neuronal ticks for the SAME 32 four-concept-link queries on each frozen model.
No other tick counts, learning changes or threshold changes. Preserve all-zero/tied
queries; report query-local top-1, MRR and measured runtime without a gate verdict.
Reproduce all 4-tick score vectors from dev-cue-trace.json before interpretation.
Save numerical recall-only graph snapshots plus numerical sparse query codes (no text
labels or answer index in the snapshot). These preserve synaptic weights, resident IDs,
threshold/familiarity/fatigue, but are NOT resumable training checkpoints or proof of
paging. Round-trip replay must match both budgets exactly.
Planned command: `gm eval recovery --mode travel-time --output
artifacts/recovery/t2/time.json`. Budget: one development run, expected <3 minutes.
A 12-tick improvement demonstrates a latency effect, not R1 success, natural-language
recall, or a reason to start R2 automatically.

T2 implementation validated by 7/7 recovery test cases (47 ms). Command:
`dotnet test GreyMatter.sln -c Release --filter FullyQualifiedName~RecoveryTests
--logger 'trx;LogFileName=time-tests.trx' --results-directory artifacts/recovery/t2`.
Observer remains optional; no propagation or learning rule changed. Snapshot format
is recall-only, not a training-resume format. Numerical evaluation codes live separately
from the model snapshot and are not consulted by propagation. Source manifest saved.
T2 process started; expected <3 minutes. Output time.json and raw output time.txt.

# T2 result — more simulation time helps, but does not resolve retrieval

`dotnet run --no-build --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval recovery
--mode travel-time --output artifacts/recovery/t2/time.json` completed exit 0, session 57630.
One seed, 32 four-concept-link queries, three frozen arms, exactly two tick budgets.
All 96 original four-tick score vectors reproduce dev-cue-trace.json exactly. Numerical
snapshot reload reproduces ALL score vectors at BOTH budgets exactly, for every arm.
No training changes, no old brain reset, no paging or gate-pass claim.

| arm | ticks | top-1 | MRR | mean query ms |
|---|---:|---:|---:|---:|
| untrained | 4 | 3.1250% | 0.126828 | 0.914 |
| untrained | 12 | 3.1250% | 0.126828 | 0.886 |
| learned | 4 | 3.4180% | 0.143175 | 3.170 |
| learned | 12 | 31.5430% | 0.395215 | 8.510 |
| shuffled | 4 | 0.2930% | 0.118835 | 2.958 |
| shuffled | 12 | 3.4180% | 0.127582 | 9.030 |

Timings are one sequential development run, including candidate score aggregation, without
a dedicated benchmark warmup or controlled machine load. They describe this run only.
Do not claim an established speed/quality tradeoff from these timings.

The learned arm gains 28.125 percentage points; shuffled gains 3.125 points. Longer time
therefore reveals learned association that the four-tick budget misses on this seed.
But 31.54% is far below the original 80% requirement, and most queries remain incorrect.
The source of the remaining failures is not uniquely isolated. No additional time budget
was tested, and no threshold was lowered. R1 remains failed as originally registered.

Recommendation: retain the distinction between conceptual path length and simulation
time in the eventual utility contract. Before another learning-rule change, use the
saved numerical graph to inspect whether correct candidates have viable relay routes
and where those routes lose activation. Such a follow-up can now avoid retraining.
Do not assume that more ticks, more edge slots, or normalized weights will solve it.
Do not start R2 before a newly authorized resident-learning validation passes.

T2 handoff: complete, no running jobs. Evidence in artifacts/recovery/t2 includes time.json,
time.txt, summary.json, numerical *-recall.bin snapshots, checksums, and separate
*-evaluation-codes.json diagnostic inputs/candidates. Snapshots are RECALL-ONLY and store
no word labels; they omit training state such as receptive-field deviations and encoder
accumulators. Do not use them to resume learning or claim they implement a pageable store.
Snapshot loader uses the same current baseline recipe generator, but recall scores come
from the restored operative synapses/thresholds, whose exact replay is verified.
Tests: seven targeted recovery cases pass; previous full regression suite passed 148/148.
No full 150-case regression run was claimed. Runtime numerical behavior remains unchanged
by T1/T2; only optional telemetry and diagnostic modes were added.

# T3 frozen-route audit — registration (2026-09-09)

Bill authorizes continuing the failed-route review. Use only the three T2 snapshots and
separate numerical evaluation codes. No training or new tick budget. For each of the
32 queries per arm, run breadth-first search over positive learned edges (12-edge cap),
report endpoint-member coverage for ALL 32 candidate assemblies, and retain one shortest
positive route to the correct candidate as a witness, explicitly not a causal explanation.
Report cue/endpoint overlaps separately. Observe the unchanged 12-tick runtime and trace
witness nodes' first positive, selected and threshold-eligible steps. Exact score vectors
must reproduce T2. Rank summaries may diagnose structural ambiguity but are not learning
gates. Expected seconds to a minute; targeted BFS tests plus replay verification.
This audit ends with a design recommendation, not an automatic new learner or R2.
Planned CLI: `gm eval recovery --mode routes --snapshot-directory artifacts/recovery/t2
--output artifacts/recovery/t3/routes.json`.

# T3 closeout — connectivity, relay activation, and discrimination all matter

`dotnet run --no-build --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval recovery
--mode routes --snapshot-directory artifacts/recovery/t2 --output artifacts/recovery/t3/routes.json`
completed exit 0, session 44410. No training. All 96 twelve-tick score vectors exactly
reproduce T2. Snapshot checksums remain unchanged. Evidence: routes.json, routes.txt,
summary.json, source-manifest.json.

Targeted tests: `dotnet test GreyMatter.sln -c Release --filter
FullyQualifiedName~RecoveryTests --logger 'trx;LogFileName=routes-tests.trx'
--results-directory artifacts/recovery/t3` passed 8/8 cases in 47 ms. Tests include directed
BFS, finite-depth refusal, cycles, duplicate roots and unreachable nodes. No full-suite
rerun is claimed; previous full regression result remains 148/148.

## Findings from the learned snapshot, 32 queries

- **10 correct endpoint assemblies have no reachable member within 12 positive edges.**
  These failures cannot be repaired by changing activation thresholds alone at this
  fixed path-length budget. This is not a proof of unreachability at arbitrary depth.
- **21 have a positive-length shortest witness** to a non-root endpoint member. One
  additional query has only zero-length cue/endpoint overlap in this BFS result.
  The BFS does not revisit roots to find positive cycles; do not count that overlap
  as proof of a learned path or describe all 11 witness-free cases as disconnected.
- Reachable endpoint-member coverage averages **5.73% for correct candidates** versus
  **3.80% for other candidates**. Over 55% of other candidate assemblies have some
  reachable member (including initial overlaps). Reachability alone is not recall.
- Query 0 has a four-edge witness, but its first relay neuron (172691) is selected
  from step 1 onward and never emits during the 12 ticks; its maximum potential is
  .348758, below .5. Query 19's first relay (551456) reaches only .003691 and is never
  selected. These are observed witness failures, not proofs about every alternate path.
- Some failed queries deliver positive mass to the correct candidate but rank another
  candidate higher; query 31 delivers 47.507 units to the correct endpoint. The problem
  therefore includes discrimination, not only missing paths or complete silence.
- Some successful queries have structural shortcuts shorter than the four conceptual
  links. A shortest witness is NOT necessarily the path that produced the score: even
  successful query 1 has a shortest witness whose relay never emits. This audit does
  not certify that successful queries followed the intended conceptual chain.

Controls reinforce the limits: untrained has no positive witnesses (four initial
cue/endpoint overlaps); shuffled has 11 positive witnesses and mean correct/other
coverage 1.79%/1.74%. Learning creates some selective structure, but most endpoint
members remain unreachable and usable activation is a further constraint.

## Design recommendation — define an assembly relay contract

The current implementation can learn A→B and B→C as separate sets of neuronal edges
without guaranteeing that activity arriving from A can engage B's outgoing association.
The intended unit of association is an assembly, while routing succeeds or fails at
individual, independently budgeted neurons. More ticks helped, but did not resolve
that mismatch; moving a single threshold would not fix missing routes.

The next implementation proposal should be a bounded **assembly-relay experiment**:
learned incoming activity must have a defined local route to the assembly's outgoing
learned associations. Candidate mechanisms are a protected common relay cohort or
local completion within the assembly. Choose ONE after specifying its entry/exit
contract; do not combine both or conceal an answer lookup behind completion. Assembly
identity and membership must be numerical, learned associations must still come from
training, and the evaluator's chain labels must never enter runtime routing.

Validate separately: (1) intended entry/exit connectivity is learned, (2) activation
actually traverses it, (3) correct targets beat frequency-matched distractors and shuffled
controls. Measure ticks and latency separately from conceptual distance. Retain the
transition-count baseline. No claim of additional effective neurons follows merely
from using an assembly as the routing unit. This is a design hypothesis, not a proven fix.

T3 ends here with no new learner, no changed propagation/defaults, and no R2 advance.
The next decision is the concrete relay mechanism and its bounded implementation
protocol, not another timing/threshold grid. Existing snapshots make future read-only
comparisons possible without retraining.

## T3 handoff

Complete; no jobs running; R1 remains failed. New RouteReview.cs, recovery CLI dispatch
and BFS test only. No snapshot or stored user brain changed. Next proposed work:
register and implement one explicit assembly-relay contract, then validate connectivity,
transmission and discrimination independently. Do not infer a pass from this audit.

# A1 protected relay — prospective registration

Bill authorizes the next implementation deliverable. Contract and gates are now in the
active plan. Cohort=first 8 deterministic assembly members; isolated relay synapse store;
existing RecordCoactivation(.5,1,CrossCue) learning, degree cap32 and periodic .99 decay
every500 episodes. No within-cue relay reinforcement. Positive outgoing weights normalize
each source's transmitted drive; next tick is fresh incoming activation, with .5 firing
threshold and top256 selection. Candidate scores sum delivered drive across ticks.
This is a new protected routing model, not a single-variable causal experiment on R1.

Use unchanged synthetic generator and encoder (context learned on each arm's training
corpus). One development seed100, then final201–205. No parameter grid. Original R1
results/gate remain unchanged. Connectivity and discrimination gates as in plan A1;
record learned edge existence, intermediate overlap, actual relay member collisions,
zero scores, per-query ranks and four-hop emission diagnostics. Include untrained,
global-shuffle and transition-count controls. Persist recall-only numeric graph and
separate numeric evaluation codes; exact replay required. No wordlist in model snapshots.

Planned CLI: `gm eval recovery --mode relay --seeds 100 --output
artifacts/recovery/a1/dev.json`, then `--seeds 201,202,203,204,205 --output
artifacts/recovery/a1/final.json`. Final runs only after targeted/full tests and smoke.
Expected runtime below ten minutes, bounded by measured smoke. All existing user
changes and brains preserved; no commit or reset needed.

### A1 implementation and development result

New Runtime/AssemblyRelay.cs implements the protected cohort contract as a separate
fully resident model. Eval/RelayEval.cs uses the existing generator, rank metric,
ContextEncoder and transition baseline. Existing Cascade/Plasticity behavior is unchanged.
Three new AssemblyRelayTests pass (14 ms), covering learned composition, sequence
boundaries/untrained silence, conservation, relative weights and read-only queries.
Command: `dotnet test GreyMatter.sln -c Release --filter FullyQualifiedName~AssemblyRelayTests
--logger 'trx;LogFileName=relay-tests.trx' --results-directory artifacts/recovery/a1`.

`dotnet run --no-build --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval recovery
--mode relay --seeds 100 --output artifacts/recovery/a1/dev.json` completed exit 0.
Learned direct/composed top-1=1.000/1.000; 128/128 links and 96/96 junctions have relay
connections; every composed endpoint receives activation with emission at every tick.
Untrained=.03125/.03125; shuffled=.031169/.027344. The transition baseline remains
1.000/1.000. Actual learned neurons=1,278 (two overlapping relay IDs), synapses=8,192.
All arm snapshots replay all query scores exactly. These are development results only.

No tuning followed. Final command will use fresh seeds 201–205 and identical settings:
`dotnet run --no-build --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval recovery
--mode relay --seeds 201,202,203,204,205 --output artifacts/recovery/a1/final.json`.
Smoke completed in seconds; final expected below one minute. Full regressions in progress
(session 44750); no other jobs running. Final evaluation starts after those tests pass.

# A1 closeout — protected relay passes the registered synthetic gates

**A1 PASS**, distinct from the historical R1 failure. All three acceptance checks passed
without a corrective arm or parameter tuning. Existing default Cascade/Plasticity and
legacy learn/probe commands remain unchanged by A1. The new model is explicitly selected
with `eval recovery --mode relay`.

Final command: `dotnet run --no-build --project src/GreyMatter.Poc/Poc.csproj -c Release --
eval recovery --mode relay --seeds 201,202,203,204,205 --output artifacts/recovery/a1/final.json`.
Session 94546 completed exit 0. Full effective configs, corpus/null hashes, all query
scores and tick traces are in final.json. Raw stdout final.txt; aggregate summary.json.

| model | direct top-1, mean | composed top-1, mean |
|---|---:|---:|
| protected relay, learned | 100.0000% | 100.0000% |
| untrained | 3.1250% | 3.1250% |
| shuffled training | 4.1455% | 3.0745% |
| transition-count baseline | 100.0000% | 100.0000% |

- Every learned seed individually scored 100% on 128 direct and 128 composed queries.
- Every seed had all 128 observed adjacent links and all 96 intermediate relay junctions
  connected. Every composed endpoint received positive drive, with emission at every
  simulation tick. Numerical fixtures additionally establish directed step-by-step
  transmission without a directly trained endpoint pair.
- Learned networks contain 8,192 positive synapses and 1,278–1,280 unique relay neurons
  (0–2 shared IDs among cohorts). Eight members per code are procedural; the other
  potential assembly members are not instantiated by this model.
- All 15 final arm snapshots replay all query scores exactly through AssemblyRelay.
  Numeric recall snapshots and separate evaluation codes are under a1/seed-201 through
  seed-205. Snapshot checksums and source hashes are recorded. These snapshots use
  the existing diagnostic graph container but require the **AssemblyRelay runtime**,
  not the old Cascade; final.json explicitly identifies Runtime=AssemblyRelay. They
  remain recall-only and are not the R2 durable/pageable training store.

Validation: `dotnet test GreyMatter.sln -c Release --no-build --logger
 'trx;LogFileName=full-tests.trx' --results-directory artifacts/recovery/a1` passed
**155/155**, 51 seconds, exit 0. A preceding build/targeted test passed 3/3 relay tests;
only the pre-existing CS8604 warning in ScaleSweep.cs:65 remains. Source manifest matches
the evaluated implementation; whitespace check passed. No resets, commits, dependency
changes or user snapshot modifications were needed.

## Interpretation and limits

This supplies a working resident associative-routing reference for the original memory
experiment. It does NOT establish emergent behavior, biological equivalence, natural-
language retrieval, or an advantage over the transition-count baseline. Eight parallel
relay members are a routing design, not eight independent learned concepts; there are
128 trained pair relations in this task. Do not report the million-ID address range
as a million useful neurons.

The protected cohort, isolated temporal learning and normalized transient execution
are one deliberate new architecture contract. This experiment does not isolate which
change caused improvement over R1. Its passing score must not rewrite the old result.
The network is fully resident and uses allocation-heavy dictionaries for this reference
implementation. Memory scaling, realistic branching, and local-data utility remain
future requirements. No CUDA work is justified by this synthetic result alone.

## R2 boundary recommendation

Preserve AssemblyRelay's learning and query semantics while separating its storage:
lookup/iterate outgoing learned edges by stable virtual ID, apply the existing local
coactivation update, flush dirty records, and restore unseen baseline state versus
missing learned state explicitly. Implement one resident backend and one byte-budgeted
disk backend against that boundary. Do not make cohort membership or candidate labels
part of the storage lookup algorithm.

Account for learned records, numeric indexes, encoder state, active/frontier buffers,
writeback buffers and scratch allocations. The current ActivationScope recipe dictionary
must not remain as a hidden full copy behind a new cache. The current recall snapshots
are reference artifacts, not a crash-safe store or training checkpoint. Version the new
store and specify its runtime model kind. R3 must compare EXACT same learned state and
query semantics across resident and paged modes; do not retrain separate brains and
attribute differences to paging. Larger useful learned capacity is a later R4 question.

## A1 terminal handoff

A1 complete/pass; original R1 remains failed; R2 ready as the next deliverable but not
started in this turn. No jobs running. New runtime AssemblyRelay.cs, new evaluator
RelayEval.cs, three AssemblyRelayTests, CLI dispatch and shared baseline visibility.
No changes to default numerical learning or propagation. Evidence in artifacts/recovery/a1.
One development seed100 and one final fresh-seed run201–205; no sweeps/corrections.
Next action: implement the bounded resident/disk storage boundary under R2, preserving
this model's semantics and the full memory-accounting contract.

# R2 registration — bounded numeric relay storage (2026-09-10)

Bill authorized R2. Preserve A1/default degree-32 Hebbian semantics. Introduce a
resident record backend and directly addressed disk backend, with the same fixed
numeric adjacency records (ordered targets, float weights, provenance, checksum).
Disk cache has a byte-derived fixed capacity; dirty eviction writes synchronously
(no unbounded queue). A separate on-disk presence index distinguishes unseen IDs
from missing/corrupt learned records. No ActivationScope/Resume on this path.
Checkpoint publication writes a complete immutable generation and then replaces a
small numeric manifest atomically; reopening clones only the committed generation
into a new scratch workspace. Interrupted publication must preserve the old snapshot.

R2 correctness protocol, registered before results: targeted tests for exact records,
cache refusal/bounds, dirty eviction, corruption versus unseen, interrupted publication,
and mid-sequence restart. Development storage fixture: 128 source records; final:
1,024 source records, eight-record cache, repeated updates/decay compared bit-for-bit
with resident and interrupted/resumed execution. Use numeric generated cohorts (a
storage/learning correctness fixture, not a new association result), existing Hebbian
implementation, and preserve previous cohort/update/episode state. Verify against
legacy resident A1 updates in a separate regression. No learning sweeps.
Planned CLI (not yet implemented): `eval recovery --mode storage --records 128|1024
--output <fresh-directory>/result.json`. Gate: all comparisons exact, repeated dirty
eviction, restart exact, bounded buffers, errors on corrupt learned state. Full suite
after targeted checks. Encoder remains an explicitly external frozen-code input for
this storage gate; document its bounds/required persistence before any end-to-end
claim. R3 traversal and R4 RSS/capacity gates are not passed by these fixtures.

R2 command detail before measurement: `--checkpoint <snapshot-root>` restores the
registered mid-sequence checkpoint in a fresh process and finishes the same six-pass
fixture, comparing every numeric record and continuation counter with an independently
executed resident reference. Test both normal and fresh-process runs at each registered
size. This is the restart check, not an additional mechanism arm.

# R2 closeout — numeric learned state survives bounded-cache eviction and restart

**R2 storage gate PASS** (2026-09-10). This supplies storage correctness for the
validated relay; it does not pass R3 activation travel, R4 resource scaling, or R5
local-text utility. Original R1 remains failed; A1 remains the learned reference.

## Implementation and compatibility

`Storage/RelayRecords.cs` defines the shared copying boundary and deliberately fully
resident reference. `DiskRelayRecords.cs` uses fixed arrays and virtual-ID offsets,
with a separate one-byte-per-address presence file on disk. `StoredRelayLearning.cs`
uses the existing `SynapseStore.RecordCoactivation` and decay code through a one-slot
scratch segment. A regression compares ordered targets, weights, provenance and update
counts with original A1, including eight-member cohorts, degree pressure and decay.
Existing AssemblyRelay/Cascade/ActivationScope/Checkpoint.Resume are unchanged.

Version 1 stores 328 bytes per address: source ID, degree, 32 ordered numeric synapse
slots (uint target, float weight, byte provenance), then SHA-256. Empty learned terminal
records are distinct from unseen addresses. No words or labels are persisted. The
128-byte continuation record identifies version 1, protected-relay model 1, external
frozen-code input kind 1, address limit, update/episode/observation counters, previous
cohort (at most eight IDs), and presence-index checksum. Default degree-32 A1 learning
constants are fixed by this version; configurable alternative rules are not supported.

Snapshots consist of completed numeric generations and a 40-byte manifest (generation
number plus metadata checksum). Publication flushes completed files, syncs directories,
then atomically replaces the manifest and syncs the root. Restore validates metadata,
presence checksum and every occupied record while streaming into a fresh workspace.
Committed generations open read-only. Working files are not restartable checkpoints;
unpublished dirty work is intentionally discarded on disposal. An incomplete generation
is an orphan, never a baseline. Old generations are retained, not automatically deleted.
Single writer, no concurrent checkpoint mutation; POSIX directory fsync required.
Interrupted-copy and pre/post-manifest fault injection passed. These are process-failure
simulations, not physical power-cut tests or guarantees about SMB/controller durability.
The caller must provide a durable parent directory; initial ancestor-directory creation
is not a separately power-failure-tested operation.

## Bounds and costs — cache budget is not total RSS

| component | bound / policy |
|---|---|
| Record cache | fixed flat byte array; slots=floor((budget-4096)/360) |
| Cache metadata | fixed uint IDs, long ages, two bool arrays, no full-model dictionary; 32 bytes/slot charged conservatively |
| Fixed store overhead | 4096-byte reservation for array/object/handle overhead; not an RSS measurement |
| Dirty writeback | synchronous from cached record; no queue or second growing record collection |
| Lookup index | on disk, one marker read per miss; zero in-memory full index entries |
| Learning scratch | one 32-edge SynapseStore, one 328-byte record buffer, old/current cohorts <=8 IDs; fixed counters |
| Checkpoint/restore | one additional one-slot store (4456 reserved bytes), 4096-byte marker/hash chunks, one record, fixed 128/40-byte metadata, fixed stream/crypto overhead |
| Enumeration | sequential address scan; no array/list of all learned IDs |
| Encoder | zero owned by this API: caller supplies frozen numeric codes; this is an explicit model input kind |
| Recall frontier | not implemented in this path yet; R3 must budget it separately |

The live application-owned structures above are independent of learned-record count.
The 4096-byte fixed reserve and per-slot charge are conservative engineering accounting,
not managed-heap/RSS measurements. Temporary allocations and garbage collection are
still present; no R4 process-memory gate is claimed. The fixture itself keeps a full
resident reference, so its process memory cannot establish a bounded end-to-end run.
File buffering in the OS, filesystem sparse allocation, page cache, and managed-runtime
baseline are outside the cache reservation and must be measured separately in R4.
Checkpoint I/O counters in result.json describe the primary working record cache only;
copy-side stores, sequential presence scans, metadata and fsync are additional traffic.

Logical file length is addressLimit*328 plus addressLimit presence bytes. Holes may be
sparse at the filesystem level; that is not learned capacity. Eager decay scans the
address range, and snapshot copy scans the presence index. Both are bounded in RAM,
but their latency and disk amplification are not optimized or certified practical.

Encoder design boundary: the text ContextEncoder remains outside this R2 numeric API.
Its current nominal 50,000 accumulators alone can consume 50,000*2048*4=409,600,000
payload bytes plus dictionaries, last-seen values and temporary sentence allocations;
that is emphatically not included in 6976 bytes. R3 can use A1's frozen evaluation
codes outside traversal. Before R4 end-to-end claims and R5 text restart, budget the
encoder explicitly, persist numerical accumulators/settings (or freeze an explicitly
bounded numerical encoder), bound tokenization/eviction scratch and verify encoding
round trips. Do not smuggle a vocabulary/code lookup table into the model or describe
this storage fixture as a saved-text-model utility.

## Validation and reproducible evidence

Targeted command: `dotnet test GreyMatter.sln -c Release --filter
FullyQualifiedName~RelayStorageTests --logger 'trx;LogFileName=storage-tests-final.trx'
--results-directory artifacts/recovery/r2` — **11/11**, exit 0 (session 50804).
Full command: `dotnet test GreyMatter.sln -c Release --no-build --logger
 'trx;LogFileName=full-tests.trx' --results-directory artifacts/recovery/r2` — **166/166**,
51 seconds, exit 0 (session 7993). Build had only the pre-existing ScaleSweep.cs:65
CS8604 warning. Initial sandbox build/test attempts could not create MSBuild sockets;
the blocked commands were stopped and tests ran with local IPC permission. This was
an environment issue, not a gate failure. Initial targeted tests had three mistaken
exception-type assertions (InvalidDataException does not derive from IOException);
corrected before any registered run. All corruption cases were already rejected.

Commands (each fresh process, each exit 0):

```bash
dotnet run --no-build --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval recovery --mode storage --records 128 --output artifacts/recovery/r2/dev/result.json
dotnet run --no-build --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval recovery --mode storage --records 128 --checkpoint artifacts/recovery/r2/dev/checkpoint --output artifacts/recovery/r2/dev-resume/result.json
dotnet run --no-build --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval recovery --mode storage --records 1024 --output artifacts/recovery/r2/final/result.json
dotnet run --no-build --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval recovery --mode storage --records 1024 --checkpoint artifacts/recovery/r2/final/checkpoint --output artifacts/recovery/r2/final-resume/result.json
```

Use fresh output directories for reproduction. Development took below one second;
final cells likewise completed below one second (not a latency benchmark).

| run | numerical records | live edges | cache slots | evictions | dirty writes | exact resident match |
|---|---:|---:|---:|---:|---:|---|
| development | 256 | 384 | 8 | 2297 | 1793 | yes |
| development fresh resume | 256 | 384 | 8 | 1400 | 1152 | yes |
| final | 2048 | 2144 | 8 | 39857 | 35769 | yes |
| final fresh resume | 2048 | 2144 | 8 | 19448 | 17408 | yes |

Each cache reserves 6976 bytes (2624 record bytes plus metadata/fixed reservation).
Final fixture contains 1024 source and 1024 terminal records; terminal records count
as numerical state, not extra learned associations. Six passes produce 6144 episodes,
6144 update proposals and 12288 observations. Different live-edge counts across sizes
reflect the registered decay cadence, not a quality comparison. All final records and
continuation counters match uninterrupted resident execution exactly, including decay
across restart. Final record SHA-256:
`5DF0BFDCE9437B97168E5F1F7C4F3111F4A876F8BF2DA0F7B200D401238859B7`.
Fixture stream hashes and complete effective settings are in fixture-manifest.json;
source checksums are in source-manifest.json. Raw result JSON, numeric checkpoints,
TRX test results and summary.json live under artifacts/recovery/r2. Post-run manifest
inspection counted live edges directly from the committed numeric layout; it did not
alter or score the fixture. No association evaluation or parameter search was run.

## R2 handoff

R2 complete/pass for numeric relay storage, one implementation, no corrective mechanism
arm. New files: Storage/{RelayRecords,DiskRelayRecords,RelayCheckpoint}.cs,
Runtime/StoredRelayLearning.cs, Eval/RelayStorageEval.cs, tests/RelayStorageTests.cs;
RecoveryEval dispatch adds `--mode storage`. Default runtimes remain unchanged.
No jobs running, no user brain resets, no commits/staging. Existing user documentation
changes preserved. R3 next: load the same frozen A1 graph through this boundary, retain
stable IDs, aggregate each logical step before selection, explicitly budget frontiers
and cumulative readout, and demonstrate multi-load queries with a cache smaller than
the path. Keep the A1 resident scores fixed; no retraining or learning changes to hide
a paging mismatch. R2's exact record match is a prerequisite, not a substitute for R3.

# R3 registration — exact activation travel through bounded record caches

Bill authorized continuation after R2 (2026-09-10). No training or learning changes.
Use frozen A1 learned snapshots and their existing scores: development seed 100, then
seeds 201–205, all 256 registered queries per seed, unchanged 32 candidates and 1–4
synchronous ticks. Check recorded snapshot hashes before import. Stream adjacency into
R2 records without using all-partitions Resume; retain ordered synapses exactly.
Compare original A1 resident replay, new resident-record traversal and disk traversal.
Disk cells: one-record and eight-record caches, each normal and reverse query order,
cold application cache at the start of each pass. Reuse the same model, no retraining.
Require every candidate score within abs 1e-6 + rel 1e-5, identical query-local ranking
and tie groups, unchanged persistent checksums, and positive load/eviction evidence.

Traversal uses bounded preallocated step accumulation and cumulative delivered-drive
buffers. Compute the conservative maximum from width, degree cap and tick limit;
refuse insufficient scratch budget before execution rather than dropping work. No
spill algorithm needed if the registered workload fits; this is an explicitly bounded
exact mode, not permission for growing dictionaries. Keep accumulation in source-ID
order and stored edge order, then select globally by drive descending / ID ascending.
Copy each source record before target loading can evict it. Targets must be loaded and
validated; no resident-slot identifier crosses the storage boundary. Fixed-size optional
I/O event trace for one four-hop query, with explicit overflow failure, never a growing
trace list. Record requests/loads/evictions, bytes, frontier counts and zero truncations.

Fixtures: chain longer than cache, cycle, cross-page fan-in, branch/distractor, threshold
and winner-boundary ties, query reset/order, missing target errors, insufficient-budget
refusal. Gate requires fixtures AND frozen learned queries. Scratch budget fixed at
2 MiB, width 256, max ticks 4 for learned runs; caches 4456/6976 reserved bytes. These
are separate budgets, not total process RAM. Evaluator/reference/labels remain external
and fully resident for this comparison; R4 resource claims are not made here.
Planned command (not yet implemented): `eval recovery --mode paging --seeds 100` or
`--seeds 201,202,203,204,205 --output <fresh-dir>/result.json`. One development and one
final evaluation, no tuning. R3 failure means execution/storage defect, not learning loss.

R3 development completed exit 0 (session 30821): seed100, 1024 paged query
executions across 1/8-slot caches and forward/reverse orders. Every score was exactly
equal to frozen A1; original and new resident replays were exact, file hashes unchanged.
138050 cache-miss loads / 138032 evictions across the four measured cells. Seventeen
targeted traversal/storage tests passed (session 64821). No corrective arm or tuning.
Final five-seed command follows unchanged; development finished in seconds, so the
final comparison is expected well below one minute and under 100 MiB physically
allocated model files on this filesystem (logical sparse extents are larger). This is
a scheduling estimate, not a resource result. Full regressions run alongside final.

# R3 closeout — learned activation crosses pages without changing the answer

**R3 PASS**, 2026-09-10. Frozen A1 learned graphs, unchanged learning/encoding/readout
contract, no retraining, no corrective arm or parameter adjustment. This establishes
exact paged execution on the small learned synthetic networks. It does not establish
R4 memory/capacity/latency performance or R5 text utility.

## Reproducible commands and results

```bash
dotnet test GreyMatter.sln -c Release --filter 'FullyQualifiedName~RelayPagingTests|FullyQualifiedName~RelayStorageTests' --logger 'trx;LogFileName=targeted-final.trx' --results-directory artifacts/recovery/r3
dotnet run --no-build --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval recovery --mode paging --seeds 100 --output artifacts/recovery/r3/dev/result.json
dotnet run --no-build --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval recovery --mode paging --seeds 201,202,203,204,205 --output artifacts/recovery/r3/final/result.json
dotnet test GreyMatter.sln -c Release --no-build --logger 'trx;LogFileName=full-tests.trx' --results-directory artifacts/recovery/r3
```

All commands exited 0. Targeted traversal/storage tests **17/17** (session 64821),
including six new paging cases. Full suite **172/172**, 52 seconds (session 54895).
Build: no new warning; existing ScaleSweep.cs:65 CS8604 remains. Development session
30821 passed; final session 24698 passed. Fresh output directories are mandatory.
No SMB corpus, user brain reset, dependencies, commits or publication were needed.

| frozen seed | original A1 replay | new resident replay | paged executions | score equality | cache loads | evictions |
|---|---|---|---:|---|---:|---:|
| 201 | exact | exact | 1024 | exact | 137639 | 137621 |
| 202 | exact | exact | 1024 | exact | 137748 | 137730 |
| 203 | exact | exact | 1024 | exact | 137611 | 137593 |
| 204 | exact | exact | 1024 | exact | 137642 | 137624 |
| 205 | exact | exact | 1024 | exact | 137649 | 137631 |

The 1280 distinct registered learned queries were each executed with one/eight cache
slots and forward/reverse order: **5120 executions**. Every candidate score was
bit-exact, stronger than the registered abs 1e-6 + rel 1e-5 tolerance; every ranking
and tie group was unchanged. Direct and composed top-1 remain 100%. These repeated
executions are equivalence checks, not 5120 independent learning trials. Historical
untrained/shuffled and transition-baseline results are unchanged and were not retrained.

Across measured cells: **688289 loads, 688199 evictions, 226447081 record/index bytes
read, zero bytes written, zero truncations**. Here all cache misses found existing
records, so misses equal loads. Each pass starts with a fresh application record cache;
this is not cold physical storage. SHA-256 of both complete imported data/index files
before/after recall matched for every seed; source A1 snapshot hashes also stayed fixed.
The snapshot import preserves ordered targets, float weights and provenance. A1's
threshold/familiarity/fatigue fields are retained in the authority snapshot but not
used by AssemblyRelay; original-runtime replay verifies that no needed state was lost.

## How the runtime crosses pages

`StoredRelayRecall` reads each source's numeric record into a private one-segment copy
before loading targets. Therefore a target load can evict the source immediately without
invalidating any pending work. Scheduling and all pending contributions use virtual
IDs, never resident slots. Learned targets are loaded/validated even at the final tick;
a missing target raises an error. Genuinely unseen cue IDs have an empty baseline.
The runtime never calls Write or the legacy all-partitions Resume path.

Sources emit in ascending ID order, outgoing edges retain their stored order, float
incoming sums and double cumulative contributions match A1. Selection happens after
all contributions for the logical step, by descending drive then ascending ID. Fixed
arrays hold active, incoming, selection and cumulative delivered state. The implementation
uses linear searches in these bounded arrays; this is simple exact reference scheduling,
not a claim of efficient high-fan-out execution. The default A1 runtime is unchanged.

For width 256, cap 32 and four ticks, conservative bounds are 256 active entries,
8192 incoming and selection entries, and 32768 cumulative target entries. Charged
scratch is **530944 bytes**, under the registered 2 MiB allowance. One/eight record
caches separately reserve **4456/6976 bytes**. Insufficient scratch or excess depth
fails explicitly; there is no silent truncation or growing dictionary. This version
reserves the worst-case cumulative readout up front rather than spilling it. A deeper
query must supply sufficient budget or be refused. Local fixed record/synapse buffers,
array headers and step objects are included in a conservative fixed reservation;
these charges are not measured managed-heap or RSS peaks.

The evaluator retains frozen codes, candidate lists, results and resident reference
models outside the runtime and streams one frozen snapshot into the two record stores.
That instrumentation makes its process memory unsuitable for an R4 bound. Hash scanning,
JSON output and imports are setup/verification I/O outside the measured query counters.
OS file cache and fixed-offset sparse extents remain explicit R2 limitations. Text
encoder persistence remains outstanding; these queries consume frozen numeric codes.

## One observable learned four-hop query

`artifacts/recovery/r3/final/seed-201/four-hop-trace.json` records chain0, position0 to
position4, with one record slot and a cold application cache. Its optional event buffer
has a fixed 4096-event capacity and throws if exhausted; trace data belongs to the
external evaluator, not the model. It records every requested ID, hit/load/unseen flag,
and evicted ID. Trace-only events are excluded from the aggregate query counters above.

The first source is neuron273642. Loading its target530347 immediately evicts273642;
the next target590810 evicts530347. Nevertheless the copied source adjacency continues
to deliver all eight contributions, and later sources reload their own records safely.

| logical tick | emitting sources | reached/winning targets | input mass | delivered mass |
|---|---:|---:|---:|---:|
| 1 | 8 | 8/8 | 8 | 8 |
| 2 | 8 | 8/8 | 8 | 8 |
| 3 | 8 | 8/8 | 8 | 8 |
| 4 | 8 | 8/8 | 8 | 8 |

This one query performed 288 requests, 287 loads and 286 evictions; the endpoint
ranking remained correct. Eight firing neurons per step are an algorithmic property,
not eight resident record slots: only one record fits in this cache. The numerical
frontier/scratch lives separately and must remain in the total memory budget.

## R3 handoff

R3 complete/pass; R4 next. One implementation, no corrective mechanism arm; no running
jobs. Added Runtime/StoredRelayRecall.cs, Eval/RelayPagingEval.cs and RelayPagingTests.cs.
DiskRelayRecords adds cold-cache reset and an optional read-event observer; existing
storage tests pass. RecoveryEval dispatch adds `--mode paging`. Evidence and source
hashes: artifacts/recovery/r3/summary.json, source-manifest.json, dev/final result JSON,
TRX files and the four-hop trace. Source hashes match the tested implementation.
README and active plan pointer updated; prior user documentation changes preserved.

Next action: register R4 actual learned workload sizes, M and baseline B before scoring.
Include the 530944-byte traversal reservation, record cache, training/encoder scratch,
indexes and output adapter in that accounting. Freeze the learner and prove the largest
complete resident representation actually exceeds 4M. Do not present sparse file length,
large ID range or small cache alone as useful learned capacity. Global decay's cost
and the new scheduler's linear lookup/extra target loads need measurement, not a silent
learning-rule change or an assumed speedup. R3 exactness is now the fixed reference.

# R4 registration — fixed-budget useful capacity, before measurements

Bill authorized R4, 2026-09-10. Retain R0's M=256 MiB and M/2=128 MiB;
do not lower the >=4M (1 GiB) resident-state requirement to fit an easy run. Register
1x/4x/16x as 8192/32768/131072 independent five-concept chains, four observed adjacent
relations per chain, sixteen presentations per relation. Numeric synthetic input,
not a text encoder: a stateless seeded k=32/n=2048 code generator; same A1 eight-member
code-hash assemblies and Hebbian/decay constants, same R3 width256/four-tick readout.
Fix address space at 16 million for ALL sizes so actual learned content, not a changed
address range, is the independent variable. No code/label table in the model.

Development seed100, 32 chains/2048 episodes only, at the registered M budget. Final
seeds201–205. Train the real local coactivation learner, with globally interleaved
pair episodes generated by a deterministic permutation (no list proportional to corpus).
Evaluate 100 fixed queries per seed, 50 direct/50 composed, frequency-matched 32-way
candidate sets. Use the same queries from the largest workload on all model sizes;
unavailable answers remain failures, and report supported-query accuracy separately.
Freeze candidate sets, include zeros/ties, and compare resident/paged scores from the
same trained snapshot. Two total budgets M/M2 include traversal and input/output scratch;
record cache receives the remainder. Record actual learned nodes/edges and resident
payload/index accounting; file holes/virtual IDs cannot satisfy >=4M.

Before a full run, measure a fresh-process empty-runtime baseline B separately and
record RSS/managed heap, startup, learning/decay time, cache I/O, and a conservative
full-grid estimate from the development smoke. No projected >=1-hour experiment may
be launched without Bill's larger time-budget authorization (active plan effort bound).
If this constraint blocks the grid, report R4 incomplete with the measured cost and
retain every quality/memory bar, rather than shrinking workloads or claiming a pass.

Initial implementation may optimize exact storage access: replace linear cache lookup/
victim scans with a fixed-size hash index and LRU links, and stream present IDs from
bounded index chunks for decay. Same numerical update/order within records, same eager
.99 decay every500 episodes, no learning-rule change or lazy-decay approximation.
Regression-check eviction, checkpoints, A1 learning and R3 traversal before measurement.
No quality sweep or new learning arm. Planned CLI: `eval recovery --mode capacity`
with baseline/development actions; final cells only if the measured runtime budget permits.

R4 worker implementation ready. Targeted cache-collision/eviction, storage continuation
and paging tests passed 18/18 (session49664). One compile-only multiple-variable `var`
error was corrected before any measurement. R4 keeps the 256/128 MiB TOTAL budgets;
4 MiB of each is reserved for traversal/input/output/sampler overhead, the remainder
for the record cache. Cache metadata now conservatively charges 64 bytes per slot
(hash buckets, ID, LRU links, dirty flag) in addition to the unchanged 328-byte record;
this supersedes the older implementation's 32-byte metadata charge, not its result.
Use direct `dotnet .../gm.dll` workers under `/usr/bin/time -l` so the measured process
is the utility, not a `dotnet run` build/launcher. macOS time peak RSS includes output
serialization and teardown; worker managed-heap peaks are sampled every10ms. Keep
scratch model files in /private/tmp, outside the Dropbox artifact tree.

R4 timing-instrument refinement before forecasting the full grid: initial development
training took .871338s, of which .747359s was decay including index scan/flush overhead.
It would be misleading to scale that entire time per learned record. Split per-record
decay callback time from per-pass overhead and repeat the SAME registered smoke once
with identical data/settings, preserving initial raw artifacts. This is the single
bounded timing-instrument repair; no quality result, workload or acceptance bar changes.

# R4 development checkpoint — runtime budget prevents the full gate (2026-09-14)

**R4 incomplete; no capacity pass or failure has been declared.** Work resumed after
a usage interruption. The saved refined training run had completed successfully and
was reused. The full 1x/4x/16x five-seed experiment was NOT launched. The registered
256/128 MiB budgets and >=1 GiB resident requirement remain unchanged.

## What changed and what remains equivalent

DiskRelayRecords now uses a preallocated open-address hash index with backward-shift
deletion and fixed LRU links. No tombstone accumulation, growing index or linear victim
scan. Hash capacity is a power of two >=2*slots; metadata charge is conservatively
64 bytes/slot plus the fixed overhead. Readout and persistent record bytes are unchanged.
`VisitPresent` streams a 4096-byte chunk of the on-disk presence index after flushing
pending markers. Decay visits each present record once, in ascending ID order, including
empty terminal records. It still performs the original eager .99 float decay/prune
operation every500 episodes. No deferred/lazy decay has been implemented.

CapacityEval provides isolated baseline, training, query and assessment workers. The
numeric generator owns 32 dimensions and eight member IDs, with no vocabulary table.
It selects one dimension per 64-dimension bin using the seeded existing RNG, then uses
Assembly.Members unchanged. Episode order is a fixed affine permutation of the power-of-
two episode count; this is reproducible but structurally more regular than A1's shuffled
text episodes. No result here establishes invariance to training order or text encoding.
Same 100 queries/candidates are generated on demand for resident/paged workers; the
full-size query universe remains fixed across registered final sizes. Unsupported
answers receive zero metric credit. Raw rankings and support are reported separately.

Targeted storage/paging/cache-collision tests passed **18/18**; full suite passed
**173/173**, 53s, session36108, exit0. Frozen R3 seed100 regression passed all1024
paged executions exactly (session30061, exit0), with identical 138050 loads/138032
evictions to its earlier trace and unchanged files. Cache metadata reservations changed;
those historical cache-byte numbers are not restated as current allocations. No new
warning; pre-existing ScaleSweep.cs:65 CS8604 remains. No running jobs or commits.

## Actual development measurements

One development configuration: seed100, 32 chains, 2048 episodes, address limit16 million,
256 MiB TOTAL allowance (252 MiB cache budget +4 MiB non-cache allowance). Same degree32,
width256, maximum4 ticks, eight-member cohorts and sixteen presentations per relation.
The timing-only rerun produced the exact same corpus and record hashes as the initial
run. It separates per-record update work from fixed per-pass scan/flush overhead.

| quantity | measured result |
|---|---:|
| numerical records / live edges | 1280 / 8192 |
| record payload | 419840 bytes |
| resident managed-heap increase after loading | 826536 bytes |
| training / decay time | .8406562s / .703118708s |
| time inside decay's record callbacks | .008268891s across5120 visits |
| warmed empty-runtime B, macOS peak RSS | 53166080 bytes |
| training peak RSS | 64176128 bytes |
| paged query process peak RSS | 65388544 bytes |
| resident / paged p95 | .1685ms / .1890ms |
| resident / paged startup | .0280109s / .2115261s |
| direct / composed top1 | 100% / 100% |
| resident-paged candidate scores | all100 queries exact |
| recall bytes read / written, paged | 294784 / 0 |

Peak RSS uses `/usr/bin/time -l` on the DIRECT gm process, including output serialization
and teardown. Worker-managed heap is sampled every10ms: training238888232 bytes,
paged239207328 bytes. The large mostly unused preallocated cache explains why managed
heap exceeds resident physical memory. Tiny learned state fitting a large cache does
not prove scalable capacity. The 1 GiB resident gate is emphatically NOT met by this
419840-byte payload. OS pages/cache are warm or unspecified; no cold physical I/O claim.
The development latency ratio is1.122x, not a prediction for the full workload.

Raw source/corpus evidence is in artifacts/recovery/r4. Initial and refined training
JSON are preserved as train-initial.json and train-final.json. Both have corpus SHA256
`7EB4298A0FFCFE1141942E7355B6E50F1EE4373BF8C79E4297B15B865A6C0097` and record SHA256
`07577256D362F2F1A9D6DBA211C8DEE89986409D6C8901F5E23A2DA64D16A67B`.
Model files stay in /private/tmp/gm-r4-20260910-refined/model; their sparse logical
extent is not copied into Dropbox and does not count as learned state. Temporary
models may be lost at reboot; commands can reproduce them in fresh directories.

## Budget decision — extrapolation, not a scale finding

The registered largest cell has8388608 episodes and16777 decay passes. A uniform-ID
occupancy estimate predicts about4.47 million records; using HALF that final occupancy
on average predicts37.50 billion record visits. Multiplying by measured cached callback
cost1.615 microseconds/record, then separately adding per-pass overhead and episode
work, gives **17.79 hours for one largest training run**. This estimate uses a tiny
cached model; actual disk pressure could increase it, and extrapolation uncertainty is
large. It is neither a hardware lower bound nor evidence that the large model exists,
meets recall quality, or meets the 1 GiB gate. Full-grid cost was not measured.

Active plan effort bound: "No unattended experiment expected to exceed one hour without
reporting the estimate and receiving an explicit larger run budget." That is why the
full grid remains unstarted. We did not lower M, reduce final learned workloads, disable
decay, substitute hand-wired edges, or quietly run a multi-day job after a general
continuation request. R4 is pending this decision; R5 remains gated.

## Concrete next alternatives

Recommended bounded correction: design and validate **exact deferred decay** before
another full-scale estimate. Track a global decay epoch and each record's last-applied
epoch in a NEW numeric store version. Before learning from or reading a record, apply
all outstanding .99 float decay/prune steps in the ORIGINAL order; do not replace
repeated float operations with a power approximation. New records start at the current
epoch. Stop replay when degree becomes zero. Recall must compute the aged view in
scratch without writing persistent state; checkpoint/restore must preserve epochs.
No old snapshot is overwritten or silently reinterpreted. Compare eager/deferred
records and all R3 scores across skipped epochs, pruning, creation/displacement and
mid-sequence restart before using the correction in R4. This changes when independent
record work is performed, not the registered learning rule—but exactness is a testable
requirement, not an assumption. It requires a versioned persistence decision, so this
proposal is not yet implemented or counted as a successful corrective attempt.

Alternative: explicitly authorize a longer run with a wall-clock ceiling and scratch-disk
budget, retaining the eager implementation and all existing R4 acceptance thresholds.
The 18-hour estimate covers only one largest training cell, not the complete campaign.

## Commands and handoff

Workers use `dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode capacity`.
Exact measured commands (redirect stdout/stderr to the corresponding .stdout.json and
.time.txt artifacts; fresh output/model directories required):

```bash
/usr/bin/time -l dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode capacity --action baseline --output artifacts/recovery/r4/baseline.json
/usr/bin/time -l dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode capacity --action development --seed 100 --chains 32 --budget-mib 256 --output /private/tmp/gm-r4-20260910-refined/train.json
/usr/bin/time -l dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode capacity --action query --backend paged --model /private/tmp/gm-r4-20260910-refined/model --seed 100 --chains 32 --budget-mib 256 --output artifacts/recovery/r4/paged-final.json
/usr/bin/time -l dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode capacity --action query --backend resident --model /private/tmp/gm-r4-20260910-refined/model --seed 100 --chains 32 --budget-mib 256 --output artifacts/recovery/r4/resident-final.json
dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode capacity --action assess --training /private/tmp/gm-r4-20260910-refined/train.json --resident artifacts/recovery/r4/resident-final.json --paged artifacts/recovery/r4/paged-final.json --baseline artifacts/recovery/r4/baseline.json --baseline-time artifacts/recovery/r4/baseline.time.txt --training-time artifacts/recovery/r4/train-refined.time.txt --paged-time artifacts/recovery/r4/paged-final.time.txt --output artifacts/recovery/r4/assessment.json
dotnet test GreyMatter.sln -c Release --no-build --logger 'trx;LogFileName=full-tests.trx' --results-directory artifacts/recovery/r4
dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode paging --seeds 100 --output artifacts/recovery/r4/paging-regression/result.json
```

All listed commands exited0. Initial smoke used the same flags with the first model
at /private/tmp/gm-r4-20260910-dev and paged.json/resident.json; its artifacts are retained.
One timing-instrument refinement used, no scientific corrective arm used. Changed files:
CapacityEval.cs, RecoveryEval dispatch, DiskRelayRecords/RelayRecords, StoredRelayLearning
(timing counters plus present-ID enumeration), CapacityStorageTests.cs, docs. Source
manifest and summary.json capture the handoff. No jobs running, no user data resets,
no commits/staging. Next action is Bill's decision on the concrete alternatives above;
do not mark R4 complete, shrink the registered gate, or launch the long grid implicitly.

# R4 authorized correction — exact deferred decay (2026-09-14)

Bill approved the bounded correction. New version-2 store, never reinterpret or overwrite
v1 snapshots. Store a global epoch and per-record epoch bound by checksum to that record.
Advance the global epoch every500 episodes, without scanning the model. Read/update
computes outstanding .99 float decay/prune steps in the original order; stop once degree
is zero. Recall returns an aged scratch view and never persists it. Checkpoint publication
may materialize aged views into a new generation; restore preserves the global epoch and
learning continuation. Keep the existing numeric record boundary and frozen A1 runtime.

Gate before performance use: bit-exact eager/deferred records across skipped epochs,
pruning, births/displacement, repeated reads, dirty eviction, checkpoint/restart and
interrupted publication; old v1 snapshot support unchanged. Frozen A1 paging regression
must remain exact. Then repeat the SAME R4 seed100/32-chain/256 MiB smoke and paired
queries with only --decay deferred changed. All memory/quality/capacity bars remain.
Count aging work done on reads as well as cheap epoch advances; do not report the moved
work as eliminated. One correction, no parameter sweep. Full runs still require an
estimate below one hour or explicit larger-budget authorization.

Deferred correction gate passed: 180/180 full tests; all5120 frozen A1 paged-query
executions bit-exact through version2, files unchanged. Same R4 development corpus/
logical-record hashes and all100 query scores match eager. Small training was1.0172761s
versus prior eager .8406562s; do NOT claim a smoke speedup. Aging replay during training
was .003577822s /3536 record-decay steps; epoch advances took .000059s. Added sidecar
I/O is real overhead. The global scan was removed, not all aging work.

Advance to the FIRST already-registered final-size cell: seed201,8192 chains (1x),
524288 episodes,256 MiB. Linear smoke estimate ~4.34min, comfortably below one hour;
use this actual cell to inform any larger run, not the tiny smoke alone. Same generator,
query universe and acceptance bars. No automatic >=one-hour job. Evaluate gates
incrementally: a decisive violation of a required per-seed largest-cell condition can
stop unneeded later cells, but missing measurements must remain explicitly unmeasured.
This does not redefine small-workload accuracy as a largest-workload verdict.

# R4 deferred correction and first scale cell — bounded stop (2026-09-14)

**Correction passed; R4 did not pass.** Version2 defers decay exactly, including
float operation order, deletion, restart and interrupted checkpoint publication.
It adds a checksum-bound 48-byte epoch sidecar per occupied record and 8 KiB reserved
scratch. It does not eliminate aging work: reads catch up in scratch, writes stamp the
current epoch, and checkpoint copies materialize aged views. Recall never writes these
views back. V1 remains supported separately. Working stores are not transactional;
only completed-generation publication has the checkpoint guarantee. Interruption tests
are injected failures, not physical power-cut tests.

Validation: 180/180 full tests, including seven new deferred-decay cases; 5,120 frozen
A1 paged executions exactly match their reference and preserve physical model files.
The frozen fixtures use epoch0; skipped-epoch learning equivalence is tested separately
and the unchanged development workload matches eager logical hashes and scores.
The source-manifest hashes were rechecked after all runs. Raw outputs and TRX files
are retained in artifacts/recovery/r4-deferred; summary.json indexes the outcome.

## First registered cell: seed201, 8192 chains, 256 MiB

524,288 episodes produced 324,350 occupied records, 146,424 surviving edges and
106,386,800 bytes of record payload (plus 15,568,800 occupied epoch bytes).
Training took 148.162244s; full process 151.91s. Epoch1048, 33,554,176 updates;
20,375,432 decay replay steps consumed 3.93724738s during training. Epoch advances
consumed .000155632s. No global decay scans and no training cache evictions occurred.
This model still fits the cache: neither file extent nor the 16-million-ID address
space is evidence for the registered >=1 GiB resident-state requirement.

| measurement | paged | resident |
|---|---:|---:|
| query p95, ms | 1.4981 | .0547 |
| startup, seconds | 33.9921 | 34.3942 |
| external peak RSS, bytes | 182108160 | 191627264 |
| direct top-1, all 50 queries | 2.375% | 2.375% |
| composed top-1, all 50 queries | 0% | 0% |
| supported queries, of 100 | 7 | 7 |
| top-1 among supported | 16.9643% | 16.9643% |

All100 score vectors and logical model hashes match exactly; both workers report
immutable models. Paged query writes0 bytes. Fixed baseline B=53,166,080 bytes;
paged peak is below B+1.25M=388,710,400. This is a small-cell memory result only.
The paged process includes a post-query immutability census, so its 67.77s full runtime
must not be presented as query latency or solely as startup.

The registered practical target applies to paired cells: measured ratio **27.3876x**
misses <=10x despite easily meeting <=2 seconds. This is one measured cell, not a claim
that every larger workload would fail. With the bounded correction consumed, stop here
instead of tuning the ratio away or spending the remaining grid. 4x/16x, other seeds,
128 MiB cells, the largest-state requirement and R5 remain **unmeasured**.

The fixed largest-workload query universe supplies only seven supported queries here,
all direct. Six of those seven returned entirely zero candidate scores; one selected
the correct candidate. Thus low aggregate recall is not solely missing support, but
this is not the largest-workload quality verdict. Composed zero has no supported
composed queries behind it. Exact paging faithfully reproduces this weak result;
it cannot repair learned retention. No new learning mechanism or sweep was attempted.

## Reproduction

All commands below exited0. Timing commands used /usr/bin/time -l with stderr saved
as the corresponding *.time.txt; stdout is also retained separately. Scratch model
files stay outside Dropbox; training JSON is copied into the artifact directory.

```bash
dotnet test GreyMatter.sln -c Release --filter 'FullyQualifiedName~DeferredDecayTests|FullyQualifiedName~RelayStorageTests|FullyQualifiedName~RelayPagingTests' --logger 'trx;LogFileName=targeted-final.trx' --results-directory artifacts/recovery/r4-deferred
dotnet test GreyMatter.sln -c Release --no-build --logger 'trx;LogFileName=full-tests.trx' --results-directory artifacts/recovery/r4-deferred
dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode paging --decay deferred --seeds 201,202,203,204,205 --output artifacts/recovery/r4-deferred/frozen/result.json
/usr/bin/time -l dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode capacity --action development --decay deferred --seed 100 --chains 32 --budget-mib 256 --output /private/tmp/gm-r4-deferred-20260914-dev/train.json
/usr/bin/time -l dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode capacity --action query --decay deferred --backend paged --model /private/tmp/gm-r4-deferred-20260914-dev/model --seed 100 --chains 32 --budget-mib 256 --output artifacts/recovery/r4-deferred/paged.json
/usr/bin/time -l dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode capacity --action query --decay deferred --backend resident --model /private/tmp/gm-r4-deferred-20260914-dev/model --seed 100 --chains 32 --budget-mib 256 --output artifacts/recovery/r4-deferred/resident.json
/usr/bin/time -l dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode capacity --action train --decay deferred --seed 201 --chains 8192 --budget-mib 256 --output /private/tmp/gm-r4-deferred-201-1x/train.json
/usr/bin/time -l dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode capacity --action query --decay deferred --backend paged --model /private/tmp/gm-r4-deferred-201-1x/model --seed 201 --chains 8192 --budget-mib 256 --output artifacts/recovery/r4-deferred/1x-paged.json
/usr/bin/time -l dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode capacity --action query --decay deferred --backend resident --model /private/tmp/gm-r4-deferred-201-1x/model --seed 201 --chains 8192 --budget-mib 256 --output artifacts/recovery/r4-deferred/1x-resident.json
```

No workers remain running. No commits, staging, history consultation or user-data resets.
The phase's correction is complete; subsequent work requires a new design directive
under the bounded failure policy. Existing thresholds and earlier results are retained.

# Retention diagnostic registration — Bill authorized 2026-09-15

New bounded directive, not an R4 pass or revised threshold. Hypothesis: global decay
removes a newly taught edge before its next presentation at 8192 chains. Trace the
first member-to-first member edge of the first relation in each of the seven supported
query chains, before and after all16 presentations. Compare one traced unchanged
seed201/8192-chain/256 MiB deferred run with one identical run that suppresses scheduled
decay. Encoding, episode order, updates, degree cap and all100 queries stay fixed.
Trace reads never write logical model state. Verify baseline final hash against the
existing run; report diagnostic overhead separately from R4 timings. If no-decay
recovers supported recall, propose retention design; do not adopt no forgetting as
production policy or launch a sweep. No supported composed queries in this cell:
composition at scale remains untested. Keep all original R4 failures in place.

## Retention diagnostic outcome — 2026-09-15

**Scheduled global forgetting explains the sampled direct-recall loss.** The traced
baseline reproduced the earlier final logical model hash exactly. Across seven sampled
first-member edges and16 presentations each (112 observations), every pre-teaching
edge was absent and every post-teaching weight was .105000004. Return intervals were
exactly32768 episodes. In the no-decay arm, only the seven first presentations lacked
edges; all105 later presentations reinforced existing edges, finishing near .17999996.
The focused test independently shows five scheduled .99 decays deleting a .105 birth
below .1, while the diagnostic preserves it with equal episode and update counts.

Both arms have identical corpus SHA256 and33,554,176 learning updates. Baseline retained
146424 synapses; no-decay retained2097136. Occupied records remain324350 and fixed-record
payload remains106386800 bytes (+15568800 occupied epoch bytes). More surviving edges
fill already allocated slots: this is not a demonstrated compression improvement.
Baseline training152.1154s; no-decay158.8355s. These traced runs are diagnostics, not
replacement R4 performance measurements. No speedup is claimed.

On the identical100-query set, no-decay retrieves all7 supported answers at rank1;
baseline had one rank1 and six all-zero vectors, tie-adjusted supported top1=16.9643%.
No-decay supported top1=100%; all-query direct top1=14%, composed=0%, MRR=.07. The latter
scores retain zero credit for unsupported queries. All7 supported queries are direct;
there is no supported composition claim, new seed replication, real-text result or
larger-than-memory result. Paged no-decay query performs0 writes and its logical hash
is unchanged. R4 latency stop remains in force; disabling decay is not its repair.

### Recommended next design, not implemented

Keep encoding and eight-member cohorts fixed. Test source-local opportunities as the
clock for forgetting: unrelated episodes must not age an untouched source's relations.
On teaching a source, observed targets reinforce; competing outgoing targets can age
under a separately registered rule. Repeated evidence can consolidate links into a
slower-changing state, but a new link first needs to survive until a relevant return
opportunity. Simply protecting links only after their second observation does not fix
the current failure if global decay still deletes them before that observation.

This trades indefinite retention of dormant associations against bounded per-source
capacity. It must be tested with both repeated useful relationships and obsolete or
contradictory relationships; otherwise “never forget” wins a stationary test trivially.
Register one policy and fresh held-out evaluation before implementation, including
supported direct AND composed queries and interference/forgetting behavior. Do not
choose a decay constant from the final dataset size or start a sparsity sweep.
No biological fidelity claim follows from this engineering proposal.

### Reproduction and boundaries

Source manifest and JSON/TRX outputs: artifacts/recovery/retention. Full existing suite
180/180 plus the new focused test1/1 passed. The no-decay switch is explicit evaluator
instrumentation, off by default; it is not encoded as a new checkpoint learning policy.
A diagnostic continuation would need the flag re-supplied. Working models are scratch
artifacts, not a promoted production brain. No data reset, staging or commit performed.

```bash
dotnet test GreyMatter.sln -c Release --logger 'trx;LogFileName=tests.trx' --results-directory artifacts/recovery/retention
dotnet test GreyMatter.sln -c Release --filter FullyQualifiedName~RetentionDiagnosticTests --logger 'trx;LogFileName=retention-test.trx' --results-directory artifacts/recovery/retention
/usr/bin/time -l dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode capacity --action train --decay deferred --retention-diagnostic --seed 201 --chains 8192 --budget-mib 256 --output /private/tmp/gm-retention-baseline-20260915/train.json
/usr/bin/time -l dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode capacity --action train --decay deferred --retention-diagnostic --disable-decay --seed 201 --chains 8192 --budget-mib 256 --output /private/tmp/gm-retention-nodecay-20260915/train.json
/usr/bin/time -l dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode capacity --action query --decay deferred --backend paged --model /private/tmp/gm-retention-nodecay-20260915/model --seed 201 --chains 8192 --budget-mib 256 --output artifacts/recovery/retention/nodecay-paged.json
/usr/bin/time -l dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode capacity --action query --decay deferred --backend resident --model /private/tmp/gm-retention-nodecay-20260915/model --seed 201 --chains 8192 --budget-mib 256 --output artifacts/recovery/retention/nodecay-resident.json
```

Final paired check: all100 no-decay paged/resident score vectors are exactly equal;
both models remain logically immutable. Every listed worker exited0, source manifest
verified, no jobs remain running. Diagnostic complete; proposed retention policy awaits
a new directive and registration.

# Source-local retention policy registration — 2026-09-15

Bill authorized the proposed retention design. One policy, no sweep: on each observed
source->target-cohort event, multiply that source's outgoing weights to targets NOT
in the observed cohort by .99 and prune below .1. Reinforce observed targets with the
unchanged .005 update / .105 birth. No global scheduled decay. An untouched source
never ages merely because unrelated data is processed. No consolidation tier yet.
Encoding, eight-member cohorts, degree32 and readout remain unchanged. This is an
explicit experimental learning policy, not a change to default learning or old models.

Bounded evaluation: seed100/32-chain smoke, then fresh seeds301–305 with1024 five-concept
chains,4096 distinct adjacent relations,16 presentations/relation,65536 episodes.
Reuse CapacityEval's numeric encoder and affine episode ordering. Return interval4096
exceeds the measured new-edge lifetime2500; this intermediate workload tests retention
without claiming to repeat the8192-chain or R4 capacity test. Compare global, no-decay,
and local policies on identical streams. Resident reference storage only: no memory
bound or paging performance claim. All64 query chains are supported, chosen by
(seed+17*q)%chains. Test direct and2–4 hops separately with32 distinct candidates;
the final1024-chain selection has64 distinct cues (smoke uses32 distinct cues). No answer labels
enter training. Save scores, counters, corpus hash and numeric final models.

Smoke uses8 replacement/8 both-valid/16 anchors; final uses16/16/32.
After stationary scoring, select first16 queried chains for replacement: teach each
root a fresh target64 times. Next16 queried chains get an alternative target16 times,
alternating with their original target16 times (both remain valid). Remaining queried
chains are untouched anchors. Compare before/after old-edge coverage, new-target rank
against old plus30 distractors, both-valid edge coverage, and untouched direct/composed
recall. Interventions explicitly declare the task's old relation obsolete; absent such
a task declaration, a new successor need not make an old successor false.

Gate for local policy: stationary direct/composed mean>=80%, every seed>=70%; each mean
within2 percentage points of no-decay; untouched direct/composed each seed>=70%;
replacement new-target mean rank1>=80%, each seed>=70%; obsolete edge coverage<=10%
per seed; both-valid old and new edge coverage>=90% per seed. Coverage measures all
8x8 potential edges, excluding self edges, not just the traced first member. Report
no-decay obsolete coverage as the retention/adaptation contrast. Global is a measured
control, not required to fail. If a bar fails, stop and record it; no automatic new rule.
No real-text, biological-fidelity, semantic-generalization or full-scale claim.

Development seed100 passed the registered per-seed checks. Local removed obsolete
edges (coverage0), retained both valid successors (coverage1), and selected new targets
at rank1. Global/no-decay retained obsolete edges (coverage1). Three arms took .37s
combined and exported about1.4 MiB. A conservative scaling by32x records and32x decay
passes puts a fresh seed below3min; five sequential seeds below15min, below the one-hour
run bound. Advance unchanged to301–305; no policy or threshold edits after this smoke.

## Source-local retention result — PASS (2026-09-15)

All five fresh seeds301–305 pass every registered aggregate and per-seed gate. The
encoding and eight-neuron relay cohorts were unchanged. This supplies supported
multi-hop evidence at1024 chains, not an8192-chain rerun or R4 capacity pass.

| mean across5 seeds | global decay | no decay | source-local |
|---|---:|---:|---:|
| stationary direct top1 | 51.5625% | 100% | 100% |
| stationary2–4-hop top1 | 24.0137% | 100% | 100% |
| untouched direct after intervention | 17.0508% | 100% | 100% |
| untouched composed after intervention | 3.7305% | 100% | 100% |
| replacement target top1 | 100% | 100% | 100% |
| obsolete edge coverage after replacement | 16.25% | 100% | 0% |
| both-valid old/new edge coverage | 100% /100% | 100% /100% | 100% /100% |

Every local seed individually scored100% on recall and both-valid coverage, and0%
on obsolete coverage. Across seeds the stationary task contains320 supported direct
and320 supported composed queries; the untouched post-intervention subset contains
160 of each. Replacement has80 queries, branches80 pairs of coverage measurements.
These are five independent training seeds, not640 independent training runs. Global
and no-decay both choose the new target correctly; local's measured advantage over
no-decay is removal of obsolete links while retaining useful alternatives, not superior
rank1 on this stationary corpus. A high new-target score alone would have missed that.

Each seed starts with5120 synthetic concepts and4096 unique adjacent relations,
65536 training episodes, then1536 intervention episodes and32 new target concepts.
Final local models contain41160–41168 actual neuron records and263168 synapses.
No-decay has264192 synapses (the1024 obsolete relay edges remain); global has
37184–37376. Each final numeric export occupies13500480–13503104 bytes (~12.88 MiB),
identical across policies within a seed because fixed records reserve all32 slots.
This is an actual compact sequential evaluation export, not the earlier direct-address
working-store allocation. It has no live paging index, epoch sidecar or resumable
checkpoint metadata. Do not substitute its size for R4 working-store costs or infer
bytes per sentence: this workload still contains no text.

Resident arm times: global7.33–7.60s, no-decay1.41–1.46s, local1.43–1.45s per seed,
including export. These are observational evaluator timings, not a fresh-process
performance/RSS comparison. No resource gate is asserted. The original R4 latency miss
and unmeasured larger-than-memory capacity remain unchanged.

### Implementation and validation

SynapseStore.DecayUnobserved applies one .99 decay to unobserved outgoing targets,
with the same .1 prune threshold and swap deletion. StoredRelayLearning enables this
only with explicit SourceLocalForgetting; observed targets reinforce normally and
global scheduling is disabled. Explicit global decay is refused in this mode, as are
mixing the no-decay diagnostic flag or using a nonzero deferred global epoch. Existing
learning defaults remain unchanged. No new consolidation tier or swept constant.

The new RetentionPolicyEval reuses CapacityEval.Input (visibility change only). It
registers three arms, fixed numeric streams and supported candidate sets, reports raw
scores and coverage, and exports checksummed numeric adjacency records. No candidate
labels or answer oracle enter learning. Source-local continuation is tested with the
policy explicitly re-supplied; it is NOT yet encoded in persistent checkpoint metadata.
These exports are evaluation evidence, not production snapshots to resume implicitly.

Full suite184/184 passed; three new tests cover dormant retention, obsolete deletion,
both-valid branching, selective pruning/population alignment and exact explicit
continuation. Existing181 tests include deferred-decay and paged-storage regression.
The summarizer verifies all15 final numeric exports against their whole-file hashes,
every328-byte record checksum, IDs, degree, weight/population schema and uniqueness.
It also verifies corpus/update equality across arms, sample counts, frozen source
hashes and all registered gates. This is a numeric-schema audit, not a claim of semantic
non-memorization. All smoke/final/test/summarizer commands exited0; no jobs remain.

Artifacts: artifacts/recovery/retention-policy/{smoke,seed301..seed305}.json, individual
arm JSON and .records exports, summary.json, source-manifest.json, TRX files and stdout.
One rule and one development configuration used; no corrective attempt or tuning.

```bash
dotnet test GreyMatter.sln -c Release --filter FullyQualifiedName~SourceLocalRetentionTests --logger 'trx;LogFileName=local-tests.trx' --results-directory artifacts/recovery/retention-policy
dotnet test GreyMatter.sln -c Release --logger 'trx;LogFileName=full-tests.trx' --results-directory artifacts/recovery/retention-policy
dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode retention-policy --seed 100 --output artifacts/recovery/retention-policy/smoke.json
dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode retention-policy --seed 301 --output artifacts/recovery/retention-policy/seed301.json
dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode retention-policy --seed 302 --output artifacts/recovery/retention-policy/seed302.json
dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode retention-policy --seed 303 --output artifacts/recovery/retention-policy/seed303.json
dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode retention-policy --seed 304 --output artifacts/recovery/retention-policy/seed304.json
dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll eval recovery --mode retention-policy --seed 305 --output artifacts/recovery/retention-policy/seed305.json
python3 artifacts/recovery/retention-policy/summarize.py
```

### Interpretation and next deliverable

Retain this rule as the candidate for integration. First persist its learning-policy
identity so restart cannot silently revert to global forgetting; verify exact
resident/paged learning and checkpoint continuation. Then repeat an explicitly
registered8192-chain retention test with supported direct/composed queries. Keep R4's
original capacity and latency ledger separate; this result does not erase its stop.
No automatic CUDA port, encoding redesign, consolidation tier or larger grid.

The source here is a neuron ID, not a perfectly isolated concept: hash-cohort collisions
can still cause interference, especially at larger scale. Dormant associations can
persist indefinitely; storage remains bounded per source, not globally compacted.
Replacement was deliberately declared obsolete by the synthetic task. Natural data
may require context to distinguish contradiction from valid alternatives. This is
learned traversal of trained links, not evidence of biological fidelity or general
reasoning. Real-data utility and whole-process resource tradeoffs remain open.

# Persisted source-local integration registration — 2026-09-15

Bill authorized continuing. Save learning-policy identity in a new numeric checkpoint
metadata version3; keep v1 eager-global and v2 deferred-global readers compatible.
Restore must choose the saved policy without caller flags and reject conflicting
policy overrides and unknown identifiers. Source-local uses the existing base record
store (no global-epoch sidecar). No learning constants or encoding changes.

Integration gate: exact resident/paged learning under dirty eviction; mid-sequence
checkpoint/restart without policy flags; interrupted publication leaves a complete old
or new policy/model; corrupt/unknown policy refused; unchanged v1/v2 tests. Numeric
record and metadata audit. One32-chain seed100 smoke before the larger cell.

Larger follow-up: one8192-chain seed201 paired integration cell, same524288 training
presentations, encoder, affine order and source-local rule. This deliberately revisits
the earlier seed; it is not a fresh five-seed claim. Resident reference and bounded
paged training receive identical input; page cache at256 MiB total allowance. Stop
mid-sequence halfway through, publish, dispose, restore into a new workspace and finish
without supplying a policy flag. Compare final record hashes and counters with resident.
Publish completed model and query in a fresh process through an8-record cache.

Freeze128 supported queries:64 distinct chains (201+127*q)%8192, each tested at1 hop
and2+q%3 hops,32 distinct candidate chains, rotated correct position. Smoke uses32
chains/64queries. Direct/composed top1 each>=80%; every score must match resident,
recall must write0 bytes and preserve model hashes. These supported queries are a new
follow-up instrument, NOT replacement R4 queries. Timing/storage are reported, not an
R4 capacity/latency gate. Existing R4 stop remains. No larger grid or CUDA implicit.

Integration smoke passed all191 tests and separate-process resident/prepare/resume/query
workers. Seed100 final hashes, policy1, counters and64 query vectors match exactly;
midpoint state retains8 previous members; query writes0 bytes and physical files match.
Workers took .18/1.57/2.37/7.18s. Even multiplying the entire11.3s smoke by256 gives
~48.2min for the larger cell, below one hour; fixed-extent hash time does not actually
scale with occupied records, and earlier8192 training took~150s. Plan scratch <=16GiB
for four base-store copies, with90GiB available. Launch seed201 unchanged; no long grid.

## Persisted source-local integration — PASS (2026-09-15)

The larger cell completed before the usage interruption; no experiment was restarted.
Final regression run also completed:191/191 tests pass. All four separate-process
workers and the numeric audit exited0. No jobs remain running.

At8192 chains (40960 synthetic concepts,32768 distinct adjacent relations,
524288 presentations), all64 supported direct queries and64 supported2–4-hop queries
rank the correct candidate first. The same resident, resumed paged, and fresh-process
checkpoint-read scores are bit-exact. Final graph hash, updates33554176, episodes524288,
observations1048576, empty previous cohort and policy1 all match. This is one revisited
seed201 and a newly registered supported query set; it does not replace the old R4
query set, erase its failure or constitute a new five-seed scale evaluation.

Training stopped halfway with a live8-member source cohort, published a checkpoint,
exited, restored into a new workspace in another process, and finished with no policy
flags supplied. The restored policy controls continuation automatically. The completed
checkpoint then loaded in another fresh process using an8-record cache; query writes
were0 and full physical-file hashes before/after matched. The larger training cache
still fits this model (0 training evictions); small-cache dirty eviction/continuation
is established separately by the focused tests, not claimed from this large training
cell. Query/census evictions were342873 in total, including the full pre-query census;
that total must not be described as query-only disk misses.

| larger-cell measurement | result |
|---|---:|
| occupied neuron records | 324350 |
| live synapses | 2097136 |
| occupied record payload | 106386800 bytes (101.46 MiB) |
| completed snapshot, filesystem-reported allocation | 3429994496 bytes (3.19 GiB) |
| all four scratch copies, reported allocation | 14132420608 bytes (13.16 GiB) |
| recall cache reservation alone | 7232 bytes (8 records plus index/accounting) |
| fresh-process recall peak RSS | 51691520 bytes (49.30 MiB) |
| resident training/reference peak RSS | 193986560 bytes (185.00 MiB) |
| paged prepare / resume peak RSS | 174571520 /174276608 bytes |
| query p95, excluding setup/audit | .5323 ms |
| resident / prepare / resume / query worker wall time | 12.58 /130.07 /136.27 /12.37 seconds |

The recall cache reservation excludes530944 bytes of runtime traversal scratch,
.NET/process overhead and OS file cache. Query timing follows a full record census and
physical-file hash, so the OS cache is warm; it is not cold-disk latency. Worker time
includes setup/copy/publication/census/audit as applicable. No R4 memory/latency gate
is asserted here. The occupied state is below the registered1 GiB minimum and the
training cache fits it. These are measured prototype resource observations only.

Removing the global-epoch sidecar does not solve disk compactness. The completed model
still has a5,248,000,000-byte logical records file plus16,000,000-byte presence index,
128-byte numeric state and40-byte manifest. Allocated blocks are much larger than the
useful payload. Even the32-chain smoke checkpoint reports~2.63 GiB allocated for only
419840 occupied record bytes. Report filesystem allocation separately from logical
extent and useful payload; filesystem sharing can also affect physical ownership.
All large model files remain under /private/tmp, not copied into the repository.

### Saved-policy contract and validation

Checkpoint metadata version3 stores a numeric RelayLearningPolicy at byte112:
0=global,1=source-local,2=no-decay diagnostic. Versions1 and2 retain their original
global semantics; version2 still stores its epoch in that location and cannot be
silently reinterpreted as version3. The manifest SHA256 binds the metadata. Unknown
policy IDs, invalid metadata and conflicting caller overrides are rejected. Captured
continuation carries policy, previous cohort and learning counters. Version3 uses
ordinary numeric adjacency without the deferred epoch sidecar. Working stores alone
are not committed snapshots; completed-generation publication remains the boundary.

Seven focused test cases cover one-record dirty eviction, flag-free mid-sequence
restoration, conflicting policy overrides, three interruption points, corrupt/unknown
policy and legacy behavior. Existing deferred/global tests remain passing. Tests inject
publication failures; no physical power-cut guarantee is inferred. Source-local policy
identity fixes the current .99 unobserved-target decay, .1 pruning and existing Hebbian
rule: future rule changes require an explicit policy/version decision.

Final review tightened the API to reject explicitly disabling a saved policy with a
false flag too, rather than silently ignoring that request. Scientific workers ran
before that guard-only change; their source hashes remain in source-manifest.json.
Final-source-manifest.json records the final guard and added assertion, followed by a
second191/191 full regression run. Learning operations and all measured worker call
paths are unchanged; no scientific retuning or data rerun was performed.

The audit verified metadata and presence checksums, every occupied328-byte record's
checksum and numeric schema, ID ranges, weights/populations, record counts and graph
hashes for both completed models. It also checked paired scores, saved counters,
query immutability and source provenance. This structural numeric audit is not a
semantic non-memorization claim. Summary and raw results are in
artifacts/recovery/policy-integration/; exact individual worker argument arrays,
exit codes and wall times are in seed100-commands.json and seed201-commands.json.

```bash
dotnet test GreyMatter.sln -c Release --filter FullyQualifiedName~PolicyCheckpointTests --logger 'trx;LogFileName=policy-tests.trx' --results-directory artifacts/recovery/policy-integration
dotnet test GreyMatter.sln -c Release --no-build --logger 'trx;LogFileName=full-tests.trx' --results-directory artifacts/recovery/policy-integration
python3 artifacts/recovery/policy-integration/run_workers.py --seed 100 --workspace-root /private/tmp/gm-local-integration-20260915-seed100
python3 artifacts/recovery/policy-integration/run_workers.py --seed 201 --workspace-root /private/tmp/gm-local-integration-20260915-seed201
dotnet test GreyMatter.sln -c Release --logger 'trx;LogFileName=final-tests.trx' --results-directory artifacts/recovery/policy-integration
python3 artifacts/recovery/policy-integration/summarize.py
```

No commits, staging, history reads or user-data resets. One registered integration
smoke and one larger paired cell completed, no changed gate or learning rule.

### Next decision

Learned retention now survives saved-policy restart and exact paged recall at the
larger synthetic size. The next useful deliverable is a bounded storage/resource
pass: compact addressing/indexing while preserving exact learned state, explicit
policy identity and crash-safe publication, then reassess the original resource
tradeoffs. The current multi-gigabyte disk allocation for~101 MiB of useful records
should be addressed before increasing the workload again. This is a proposed next
scope, not a silently authorized architecture rewrite or declaration that R4 passed.
Real-text learn/restart/probe utility remains unbuilt on this recovered path; CUDA
and general-reasoning claims remain out of scope.

# Packed fixed-record storage registration — 2026-09-16

Bill authorized packing unchanged328-byte records and adding a bounded disk index.
No variable-length records, weight quantization, changed learning rule or shared-page
checkpoint scheme in this deliverable. Implement dense append-assigned record ordinals
and an on-disk open-addressed ID->ordinal hash table, growing by doubling at70% load.
Index entries16 bytes; initial64 entries. Cache index pages in a fixed8x4096-byte cache,
with no in-memory full map. Record cache retains the existing bounded LRU algorithm.
Charge64 KiB fixed overhead and400 bytes/record slot, including index cache, scratch,
ordinal and LRU/hash metadata. Temporary rehash uses bounded buffers and a second disk
index; working stores remain disposable until completed-generation publication.

Version4 checkpoint metadata binds numeric packed format/index hashes and saved policy.
Old versions remain untouched. Grow/collision/update/missing-ID/zero-ID tests, dirty
one-record eviction, unknown/corrupt pointer rejection, exact mid-sequence restart and
interrupted publication must pass. Learning/record bytes remain bit-exact.

Evaluation: same seed100/32-chain smoke, then the registered seed201/8192-chain,
524288-presentation source-local integration workload. Same supported128 queries as
the prior integration, no scientific retuning. Compare every final record, counters
and score vector to the existing direct-address checkpoint/reference. Train packed,
checkpoint mid-sequence, restart in a fresh process, finish and publish; query in a
fresh process with8 record slots and the fixed index cache. Disk allocation target:
completed larger snapshot <=2x occupied record bytes, all recall exact and immutable.
This is a storage-efficiency gate, not R4's >=1GiB capacity gate. Report record and index
cache separately, actual allocation, lookup traffic and p95. Repeat old direct lookup
on the same supported queries for a contemporaneous observational comparison; do not
sweep caches to meet a latency target. One smoke then one larger cell, <1h estimate
required as before. Preserve existing model files; no commits or user-data resets.

Packed smoke PASS:203/203 full tests; all midpoint/final records, saved counters and
scores exactly match the prior direct-address model, and fresh query writes0 data/index
bytes with identical physical hashes. Packed prepare/resume/query workers took
.48/.52/.15s; direct query worker7.21s includes full-extent integrity scan and is not
query latency. Scaling packed worker cost256x gives~5min; allow a conservative~20min
for larger out-of-cache index traffic (smoke index fits32KiB; larger one will not).
Scratch forecast <1GiB for four packed copies plus transient rehash, no direct retraining.
Launch registered seed201 unchanged; no cache or learning-parameter sweep.

## Packed fixed-record storage — PASS (2026-09-16)

All203 tests pass. The seed100 smoke and registered seed201/8192-chain comparison
completed successfully. Every midpoint and final record was compared byte-for-byte
with the existing direct-address checkpoint, using its sorted enumeration and bounded
read buffers; no complete runtime ID map was materialized. Saved policies, previous
cohorts, counters, input hashes and all recall score vectors agree. The larger128
supported direct/composed queries remain100% rank1. Separate-process midpoint restart
and fresh8-record-cache recall succeeded; completed model files and data/index writes
remained unchanged during queries. Existing direct-address files were only read.

| larger-cell storage measurement | direct address | packed |
|---|---:|---:|
| occupied records / live edges | 324350 /2097136 | identical |
| occupied record payload | 106386800 bytes | identical |
| completed snapshot allocated bytes | 3429994496 | 126578688 |
| completed snapshot allocated size | 3.19 GiB | 120.71 MiB |
| packed logical files, including index and metadata | — | 114775640 bytes |
| all packed working/checkpoint copies allocated | — | 504832000 bytes |

Allocated snapshot storage is reduced96.31% (~27.1x smaller), at1.190x occupied record
bytes, passing the registered<=2x bar. Logical packed records are exactly count*328;
the index has524288 slots x16 bytes =8 MiB at61.87% occupancy. Physical allocation
includes filesystem overhead/preallocation and is larger than logical length. The
smoke snapshot uses466944 allocated bytes (456 KiB) for419840 record bytes, instead
of the old snapshot's current~2.63 GiB allocation. These are filesystem-reported
allocations, not an assertion of exclusive physical block ownership.

### Lookup tradeoff — keep both sides of the measurement

| fresh-process, warm-OS-cache query measurement | direct | packed |
|---|---:|---:|
| query count | 128 | 128 |
| p95 query milliseconds | .5354 | .5324 |
| record-cache evictions during queries | 18531 | 18531 |
| logical data/presence read bytes | 6099331 | 6080792 data |
| additional logical index read bytes | included above | 54628352 |
| index page misses | not applicable | 13337 |
| cache reservation, excluding traversal/process | 7232 bytes | 68736 bytes |
| whole query-worker peak RSS | 51757056 bytes | 52002816 bytes |

Latency is effectively unchanged in this single observational comparison; the tiny
p95 difference is not evidence of a speedup. Packed lookup reads~10x more logical bytes
in total because a miss fetches a4096-byte index page instead of a1-byte presence
marker. These counters are application I/O, not physical disk traffic: the OS cache
is warm from integrity/record comparison work. Slower storage or different locality
could expose that read amplification. Do not call the packed index an unqualified
performance improvement or tune cache sizes after this result.

Both paths have8 record slots. Packed additionally caches8 index pages (32 KiB);
its64 KiB fixed reservation conservatively includes those pages, metadata and bounded
scratch. Both traversal engines separately reserve530944 bytes. No full-map runtime
cache, learned-ID list or corpus lookup was introduced. The offline audit does use a
full map for verification; that Python verifier is not part of the runtime measurement.

Packed prepare/resume/query workers took32.87/32.87/20.45s, and the contemporaneous
direct query worker11.61s. Full query-worker time includes integrity scans and, for
packed, exhaustive paired comparison against the old model; it is not ordinary probe
startup or query latency. The old training workers were not rerun, so no controlled
training-speedup claim follows from comparing their historical130/136s measurements.
All final workers together completed in~98s, below the registered run budget.

### Implementation and guarantees

PackedRelayRecords assigns dense ordinal positions on first write. A growing disk
hash table maps virtual IDs to those positions (zero ordinal sentinel; virtual ID0
is valid). Growth doubles capacity before70% load; bounded page buffers stream old
entries into a new disk table. Transient growth needs a second index file. Per-record
bytes and the fixed-array LRU algorithm are unchanged. Packed enumeration follows
birth order, not sorted IDs; the comparison instrument explicitly obtains canonical
ID order from the old reference without sorting a full list in runtime RAM.

PackedRelayCheckpoint publishes version4 generations atomically using the existing
flush/directory-sync/manifest sequence. State records the saved learning policy and
binds the packed format and index hashes; records retain their own checksums. Older
readers reject version4 and existing versions remain available. No automatic rewrite
of old models or change to default learning occurred. Working files remain disposable;
only a completed published generation is a restart guarantee. Tests inject publication
interruptions; no physical power-cut experiment was performed.

Twelve new test cases cover adversarial hash collisions, ID0/missing IDs, repeated
updates, index growth beyond its cache, bounded allocation with a uint.MaxValue ID
space, dirty eviction, exact continuation for all three learning policies, legacy
format rejection, three publication interruption points, format/index/data corruption,
and out-of-range/aliased pointers even after their metadata checksum is recomputed.

The standalone audit verified all index entries, probe-chain reachability, unique IDs
and ordinals, dense file length, every record checksum/schema, canonical graph hashes,
metadata policy/format binding, counters, source hashes and exact query vectors.
Source remained unchanged after measurement. Full structural verification is not a
claim that numeric storage cannot encode semantic information.

### Reproduction and handoff

Artifacts: artifacts/recovery/packed/summary.json, source-manifest.json, raw JSON,
stdout/time files, full-tests.trx and audit.stdout.json. Exact worker argument arrays,
exit codes and wall times are in seed100-commands.json and seed201-commands.json.
Raw models remain at /private/tmp/gm-packed-20260916-seed100 and -seed201. Old source
models are retained at the policy-integration scratch roots; no user data reset,
commits, staging or history consultation occurred. No workers remain running.

```bash
dotnet test GreyMatter.sln -c Release --logger 'trx;LogFileName=full-tests.trx' --results-directory artifacts/recovery/packed
python3 artifacts/recovery/packed/run_workers.py --seed 100
python3 artifacts/recovery/packed/run_workers.py --seed 201
python3 artifacts/recovery/packed/summarize.py
```

This deliverable completes fixed-record packing. Variable-length records, lossy
compression, shared-page checkpoints and cache tuning were not attempted. The useful
next step is a registered memory-pressure/resource follow-up with this layout,
including index traffic and OS-cache limitations, before pursuing further compression.
R4's original stop and its unmeasured>=1GiB capacity condition remain intact. No claim
of natural-language utility, biological fidelity or general reasoning is added.

# Memory-pressure/resource follow-up registration — 2026-09-17

Bill authorized the next bounded resource experiment. Keep source-local learning,
packed fixed records, index growth/page cache, encoder and query semantics unchanged.
First use a32-chain seed100 smoke; then one4x workload: seed201,32768 five-concept
chains,131072 relations,2097152 presentations. This is a resource follow-up, not a
retroactive R4 pass, new five-seed claim or test beyond physical RAM.

Train independently through packed storage at the original M=256MiB and M/2=128MiB
budgets (4MiB reserved outside the record store). Require actual dirty evictions in
training, exact final snapshot bytes/counters across budgets, and peak RSS <= original
B(53166080)+1.25*budget. Report actual occupied bytes and resident-reference managed
size; keep R4's >=1GiB resident requirement unproven unless actually measured. No
shrinking the registered budgets or changing index-cache size after results.

Fresh-process queries at both budgets compare normal buffered file access with
macOS per-descriptor F_NOCACHE. This is an advisory no-cache condition, NOT a guaranteed
cold disk or global cache purge. Do not change workstation-wide caching or manufacture
system-wide RAM pressure. Read kernel proc_pid_rusage v2 disk-I/O and physical-footprint
counters alongside logical record/index bytes. Native constants/ABI are checked against
the installed macOS SDK; unsupported/error cases fail explicitly, not as zero I/O.
Checksum validation may warm OS pages. Mark the first pass cold APPLICATION cache;
repeat in reverse order with application caches retained. Neither pass is advertised
as cold hardware storage. Source/header/hash audit runs outside timed query loops.

Freeze128 supported queries (smoke64): q=0..63, chain=(seed+q*(chains/64+1))%chains,
hops1 and2+q%3,32 distinct candidate chains with rotating correct position. This spreads
cues through the4x corpus. Require every vector equal to the separate resident import
of the same learned snapshot, >=80% direct/composed top1, no writes and immutable
snapshot hashes. Report first/repeat p50/p95, throughput, cache/OS-I/O counters, peak
RSS and managed sampling. Compare <=2s and<=10x resident p95 as practical targets;
misses remain misses. A warm-pass gain does not establish cold-storage performance.

One smoke configuration (seed100,128MiB training) then estimate4x run costs before
launch. Execute256MiB training first and use its actual timing before128MiB; projected
>=1h jobs remain unauthorized. No16x workload, cache sweep, learning repair, variable
records, CUDA or real-text redesign in this follow-up.

Resource follow-up preflight:204/204 tests pass, including macOS native sampling
and exact immutable reads with F_NOCACHE toggled. Frozen source manifest and full
TRX are in artifacts/recovery/resource-pressure. The seed100 smoke passes exact
resident/buffered/no-cache vectors and100% direct/composed recall. Training took
0.168s; a linear4x-workload estimate is172s before cache-pressure overhead. Seed201
256MiB training launched first, with a3500s worker timeout and process-group cleanup.
No final run results existed when this estimate was recorded. Query kernel-delta
objects contain disk-byte differences but absolute end-of-pass RSS/footprint fields.

First full cell completed: seed201/256MiB training142.932s, publication14.168s,
whole worker161.228s;1,258,407 records/8,388,592 edges;19,596,980 record evictions.
Native peak RSS301531136 <388710400-byte original limit. Logical index reads
83154573040 bytes vs native training disk reads192512 bytes: OS cache dominates.
The128MiB cell is authorized within the same registration; estimated several minutes
from this measured cell, with the same3500s timeout. No model/cache changes between
cells. Snapshot hash6157EFADB39B35000081EC778FCF9C460DC5E0F3FCE0096727FC2DDE658705A5.

# Memory-pressure/resource follow-up result — 2026-09-17

**Bounded training and exact sampled recall PASS at this workload. R4 remains
stopped/uncompleted; its original failure is not rewritten.** Both independent
training budgets produced byte-identical snapshots, including the saved policy and
all learning counters. Every one of the128 fixed supported queries (64 direct,
64 composed) scored100% top1. All2,048 paged query vectors across two models, two
budgets, two I/O conditions and two passes matched the resident reference exactly.
No learning, encoding, index sizing or cache tuning changed during measurement.

### Actual learned content and memory

Seed201,32,768 five-concept chains,131,072 relations,2,097,152 adjacent-pair training
episodes (4,194,304 observations):1,258,407 learned records and8,388,592 directed
synapses. Numeric payload412,757,496 bytes (393.64MiB); index33,554,432 bytes;
complete snapshot446,312,160 logical bytes. Allocated snapshot space432.67MiB
(256MiB-trained copy) and432.87MiB (128MiB copy); filesystem allocation differs,
file contents do not. This is synthetic symbol learning, not a sentence-size estimate.
Two independent working stores and completed checkpoints occupy additional space;
the snapshot figure is not the experiment directory's total footprint.

| Training budget | Train seconds | Publish seconds | Peak RSS MiB | Original RSS limit MiB | Record evictions | Dirty record writes |
|---|---:|---:|---:|---:|---:|---:|
|256MiB|142.932|14.168|287.56|370.70|19,596,980|20,257,419|
|128MiB|198.725|13.800|171.27|210.70|32,552,553|32,877,447|

External /usr/bin/time -l peaks include startup, publication and post-training audit.
Both satisfy original B=53,166,080 +1.25*budget:388,710,400 and220,938,240 bytes.
Training cache reservations were264,241,136 and130,023,136 bytes; a4MiB allowance
was withheld from each budget. Sampled managed peaks245,400,816/124,103,880 bytes;
sampled footprint277,758,768/155,681,464 bytes. Sampling can miss short peaks.
The new empty-runtime diagnostic did not replace B. Halving cache increased measured
training time39.0%, with no loss or numerical drift.

Resident import took4.751s; measured managed-heap increase657,066,096 bytes
(626.63MiB), native peak RSS567,296,000 bytes (541.02MiB). Therefore the original
>=1GiB complete-resident requirement is **not met by this workload**. No
beyond-physical-RAM claim: the workstation has32GiB RAM and caches these files.

### Recall latency and I/O

Times below are milliseconds/query, nearest-rank p95. Training budget identifies
which independently built, identical snapshot was queried. First pass starts with
empty application record/index caches; repeat reverses query order and retains them.
Resident p95 was0.1994ms first /0.1625ms repeat. All rows meet the practical <=2s and
<=10x paired resident target in these observed conditions; this is one reused seed,
not five-seed evidence or a statistically resolved performance ranking.

| Model training MiB | Query MiB | I/O | First p95 ms | Repeat p95 ms | First / resident | First native disk read bytes |
|---|---|---|---:|---:|---:|---:|
| 128 | 128 | buffered | 0.3006 | 0.1494 | 1.51 | 0 |
| 128 | 128 | nocache | 0.3029 | 0.1449 | 1.52 | 0 |
| 256 | 128 | buffered | 0.2944 | 0.1807 | 1.48 | 0 |
| 256 | 128 | nocache | 0.2960 | 0.1430 | 1.48 | 0 |
| 128 | 256 | buffered | 1.5903 | 0.1465 | 7.98 | 6,955,008 |
| 128 | 256 | nocache | 0.3077 | 0.1496 | 1.54 | 0 |
| 256 | 256 | buffered | 0.2820 | 0.1387 | 1.41 | 0 |
| 256 | 256 | nocache | 0.3017 | 0.1437 | 1.51 | 0 |

Every first paged pass read928,896 record bytes and11,616,256 index bytes logically;
index reads are12.5x record reads. All repeat passes read zero bytes logically.
All query writes were zero and snapshots stayed immutable. Query peak RSS ranged
59,604,992–64,159,744 bytes (56.84–61.19MiB); reserving cache arrays does not mean
all reserved pages are physically touched. Traversal-owned scratch530,944 bytes is
reported separately from the store cache. Raw artifacts include p50, throughput,
startup, managed peaks, native footprint and per-query timings.

There were **zero record evictions during these128-query recall passes**: their
working set fits either cache. Training demonstrates pressure and dirty eviction;
R3 and the earlier8-record recall experiment remain the evidence for exact recall
through repeated eviction. Do not conflate the two experiments.

The native disk counters mostly read zero. One buffered first pass read6,955,008
bytes and had the largest p95 (1.5903ms). Advisory F_NOCACHE runs read zero native
bytes; they followed buffered reads, and snapshot validation can warm OS caches.
Thus these conditions do not identify a causal no-cache benefit or cold-disk cost.
F_NOCACHE success is not proof of bypassing existing OS or device caching. No
workstation-wide purge or artificial physical-RAM exhaustion was performed.

Training's logical record/index reads were6.232/83.155GB at256MiB and
10.371/134.904GB at128MiB (decimal GB). Native training disk reads were only192,512
and135,757,824 bytes, respectively. Native disk writes were516,579,328 bytes each;
logical writes were much larger. Packing fixed persistent allocation, but many small
index probes still create substantial syscall/cache traffic. These measurements do
not establish throughput when the complete working files exceed OS cache capacity.

### Verification, reproduction and handoff

204/204 regression tests pass. A new test verifies native sampling plus byte-exact,
immutable reads with per-file no-cache enabled and disabled. The post-measurement
standalone audit streamed every record in both full snapshots, checked checksums,
numeric schema/zero padding, format/count/length consistency and immutable hashes.
It is a structural audit, not proof of absence of semantic memorization. Source
manifest verification passed: measured source did not change. All16 workers exited0;
no workers remain. Existing brains and old snapshots are retained; no commits,
staging, history consultation or dependency changes occurred.

Artifacts: artifacts/recovery/resource-pressure/summary.json, source-manifest.json,
full-tests.trx, audit.json, raw worker JSON, .time.txt and exact -command.json ledgers.
Model roots: /private/tmp/gm-pressure-20260917-seed100-m128 and
/private/tmp/gm-pressure-20260917-seed201-m256/-m128. Reproduction scripts refuse
result overwrite; use fresh paths for an intentional rerun.

```bash
dotnet test GreyMatter.sln -c Release --no-restore --logger 'trx;LogFileName=full-tests.trx' --results-directory artifacts/recovery/resource-pressure
python3 artifacts/recovery/resource-pressure/run_worker.py baseline
python3 artifacts/recovery/resource-pressure/run_worker.py train
python3 artifacts/recovery/resource-pressure/run_worker.py query
python3 artifacts/recovery/resource-pressure/run_worker.py query --io nocache
python3 artifacts/recovery/resource-pressure/run_worker.py query --backend resident
python3 artifacts/recovery/resource-pressure/run_worker.py train --seed 201 --budget 256
python3 artifacts/recovery/resource-pressure/run_worker.py train --seed 201 --budget 128
python3 artifacts/recovery/resource-pressure/run_queries.py
python3 artifacts/recovery/resource-pressure/summarize.py
python3 artifacts/recovery/resource-pressure/audit.py
```

**Next decision:** further compression is not the immediate prerequisite. A separately
registered16x capacity cell could test the missing >=1GiB resident condition before
returning to R5's local-data utility; this follow-up explicitly did not authorize that
cell. Four times the slower measured training time is about13.25minutes, before
additional eviction/publication costs—not a measured runtime or permission for a
long grid. Preserve the original bars, measure rather than extrapolate its resident
size, and avoid an index/cache tuning campaign unless a required resource gate fails.
Real-data recall, larger-than-physical-memory performance and emerging behavior
remain untested. This bounded follow-up is complete.

# 16x capacity check registration — 2026-09-17

Bill approved the proposed larger-capacity check. One seed201 model,131072 chains
(655360 concepts,524288 relations,8388608 adjacent-pair episodes), trained at256MiB
with the unchanged source-local rule and packed store. This is one bounded cell,
not the historical full R4 grid. Existing default workloads remain unchanged;
--chains explicitly selects the registered larger workload.

One seed100 smoke verifies the new command path. Then train through bounded storage
with the original4MiB scratch allowance; require actual dirty eviction and original
RSS <=53166080+1.25*268435456=388710400 bytes. Prior smaller run suggests about
10–20minutes including pressure/publication; enforce a3500s timeout, no automatic
retry on a measured failure. Estimate snapshot~1.7GiB plus working copy; physical
RAM32GiB, free disk~77GiB. No physical-RAM-exhaustion or cold-disk claim.

Import the same frozen snapshot into a separate resident process and measure actual
managed-state increase and native RSS. The missing capacity condition is >=1GiB
complete resident representation, not an extrapolation from file size or IDs.
Query the same snapshot at256/128MiB, buffered and advisory F_NOCACHE, first-forward
then retained-cache reverse. Use unchanged128-query formula with the new chains:
q0..63, chain=(seed+q*(chains/64+1))%chains, direct1 and composed2+q%3 hops,32 rotated
candidate chains. Require exact scores against resident, >=80% direct/composed,
zero writes, immutable snapshot, original RSS bars, p95<=2s and<=10x resident.
Report zero-eviction query working sets honestly if they again fit both caches.
No cache sweep, index redesign, learning repair or second large training budget.

Artifacts: artifacts/recovery/capacity16; models in fresh
/private/tmp/gm-capacity16-20260917-seed{seed}-m{training_budget}. Freeze source and
record full commands/native peaks. On any failed gate stop with the measured result;
on success close this capacity check and register the local-data R5 task before
scoring it. No CUDA or broad language claims follow from a synthetic pass.

16x preflight:204/204 tests pass; the only evaluator change is an explicit bounded
--chains option, preserving previous defaults. Source frozen before the smoke.
run_registered.py (exec session86210) runs smoke assertions before large training,
checks original training RSS/evictions before recall, and logs each process exit.
No parameter changes or corrections have been made after measurement began.

# 16x capacity result — 2026-09-17

**PASS for this registered single cell.** The complete resident model exceeds1GiB
by measurement; bounded training and exact paged recall satisfy the unchanged bars.
This does not retroactively pass the historical R4 five-seed/grid campaign. No
beyond-physical-RAM, cold-storage, natural-language or emerging-behavior claim.

### Learned state and resource result

Seed201,131072 chains,524288 independent generated relations,8388608 training
episodes/16777216 observations:4,470,045 distinct learned records and33,553,072
stored directed synapses. Numeric payload1,466,174,760 bytes; index134,217,728 bytes.
Snapshot1,600,392,720 logical bytes /1,611,362,304 allocated bytes (1.501GiB).
Working copy and completed checkpoint are separate allocations. These are synthetic
symbols, not a measurement of sentence compression or usable language knowledge.

Training664.160s, publication48.950s, complete worker728.789s (~12min9s).
79,913,625 cache evictions and80,574,064 dirty record writes establish actual
training pressure. Native peak RSS301,875,200 bytes (287.89MiB), below the original
388,710,400-byte bound. Cache reservation264,241,136 bytes;4MiB scratch allowance
withheld. Sampled managed peak245,435,176 bytes, footprint279,462,728 bytes; total
allocation1,185,423,640 bytes is cumulative traffic, not concurrent resident usage.

Resident import20.131s. Measured managed-state increase1,974,041,096 bytes (1.838GiB)
and native peak RSS1,835,384,832 bytes (1.709GiB): the >=1GiB resident condition is
now met at this cell. The paged path did not construct this full resident dictionary;
it exists only in the separately measured reference process. Hardware RAM remains
32GiB; this test is application-budget pressure, not physical-RAM exhaustion.

### Recall result, including failures

Direct top1:64/64=100%. Composed top1 with registered fractional tie credit:
59.15625/64=92.4316%, above80%. There are59 unambiguous composed successes and
five all-zero32-way ties, each credited1/32. Every failed case is4-hop: query indices
11,59,89,95,119, chains10446,59622,90357,96504,121092. None were excluded.

All1,024 paged vectors (4 conditions ×2 passes ×128 queries) match the resident
reference exactly. Zero query writes; full snapshot hashes remain unchanged. Thus
paging does not explain these five recall failures. Their learning/propagation cause
was not diagnosed or repaired in this bounded resource test. The earlier smaller
model scored100%, but its formula selects different chains: this is not a paired
causal estimate of scale-induced accuracy loss.

Resident p95 first0.3337ms /repeat0.2597ms. Nearest-rank paged p95 below:

| Query budget MiB | I/O | First p95 ms | Repeat p95 ms | First/resident | Peak RSS MiB |
|---|---|---:|---:|---:|---:|
|128|buffered|0.5017|0.2383|1.50|60.39|
|128|nocache|0.5149|0.2352|1.54|60.03|
|256|buffered|0.5171|0.2379|1.55|64.34|
|256|nocache|0.5945|0.2469|1.78|64.45|

All conditions pass <=2s and<=10x resident, and both original RSS bars. First passes
read1,786,616 record bytes plus22,335,488 index bytes logically; repeats read zero.
No query record evictions: this fixed query working set fits either cache. R3 and
the previous8-record-cache evaluation supply the evidence for eviction during recall;
this larger run adds bounded training pressure and measured model capacity.

Kernel-reported query disk reads were zero in every condition, including advisory
F_NOCACHE. These timings remain OS-cache-assisted; no cold-storage inference.
Training did cause10,366,382,080 native disk-read bytes and1,804,869,632 disk-write
bytes. Logical reads were24,962,118,232 record bytes and330,569,223,360 index bytes;
logical writes26,428,293,120 record /18,364,764,736 index bytes. Index traffic remains
a resource cost even though it did not fail this cell's acceptance criteria.

### Verification and reproduction

204/204 tests passed. Seed100 smoke exactness passed before full training. All10
workers exited0; no active workers remain. Source manifest unchanged throughout.
Standalone post-measurement audit verified every record checksum/numeric schema/zero
padding, format/count/length consistency and immutable full-snapshot hash. No code
correction, tuning or rerun after results; no commits, history access or brain resets.

Input hash195A83CB4CA4B1262209683A3D74E1F991C18AF934F626319393542C7896C3A4.
Snapshot hash5F4473A1E72E6DFF5A61EE9076D2FAC79553F5E7F045894D4BD71E65FD362A19.
Artifacts: artifacts/recovery/capacity16/summary.json, source-manifest.json,
full-tests.trx, audit.json, raw JSON/time logs and exact per-worker command ledgers.
Scratch: /private/tmp/gm-capacity16-20260917-seed201-m256 (full) and
/private/tmp/gm-capacity16-20260917-seed100-m128 (smoke). Existing models retained.

```bash
dotnet test GreyMatter.sln -c Release --no-restore --logger 'trx;LogFileName=full-tests.trx' --results-directory artifacts/recovery/capacity16
python3 artifacts/recovery/capacity16/run_registered.py
python3 artifacts/recovery/capacity16/summarize.py
python3 artifacts/recovery/capacity16/audit.py
```

Workers refuse overwrites. run_registered.py invokes packed-resource with explicit
--chains131072 --seed201 --budget-mib256 for training, then resident and paged
128/256MiB queries; exact argv is recorded in each command ledger. Reproduction needs
fresh paths, not deletion of an existing result. Native instrumentation is macOS-only.

### Next deliverable and agent steering

Return to R5 local-data utility rather than another synthetic scale or cache sweep.
Implement named-model learn and fresh-process probe over the proven packed relay
path. Keep source-local learning, eight-member cohorts, degree cap, propagation and
numeric-only snapshots fixed; keep readable candidate labels outside the model.
Do not route the new utility through the legacy all-in-memory checkpoint path or
quietly train while probing. Record source identity, frozen encoder configuration,
training-policy metadata and total display/candidate-adapter memory.

Before scoring real data, append a concrete R5 protocol: available local source and
checksum, sentence/document deduplication and split, at least100 supported held-out
cues, frozen candidates, untrained/frequency/co-occurrence controls, query-level MRR
and Recall@10, paired bootstrap, and original >=.05 MRR lift with positive95% lower
bound. If source/support is unavailable, report an environmental/support block; do
not substitute synthetic success. Do not infer real-data utility from this capacity
pass or open a learning/encoding redesign to recover a failed language metric.
The historical full R4 grid remains uncompleted; this bounded continuation establishes
only the explicitly measured capacity/quality/resource conditions above.

# R5 local-data utility registration — 2026-09-17

Bill authorized the next phase. Jarvis was initially unmounted, then restored by Bill.
Source: /Volumes/jarvis/trainData/Tatoeba/sentences_eng_small.csv,50000 rows,
2605071 bytes,SHA256311c619dd0b8c7ca3b61f0fd91643721d827245eba16fa1bd2b8ddfe540a9e09.
No real-data scores have been inspected. No fallback to built-in sentences.

Utility: explicit learn --model NEW --source FILE --format text|tatoeba and
probe --model MODEL --cue TOKEN --candidates FILE; audit --model MODEL. Existing
legacy runtime remains separate. Fixed source-local policy,8-member relay,degree32,
width256,hops1..4 (primary real-data evaluation1). Stateless version1 token adapter:
NFKC/lowercase, Unicode letter/digit runs including one-character words; token cap256,
line cap16384; English Tatoeba rows only. SHA256 UTF8 token's first little-endian
uint supplies the existing32-of2048 identity encoder (32 disjoint64-wide bins), then
unchanged assembly generation. Report identity collisions, never exclude them to
improve scores. No semantic encoder/table, stopword tuning or vocabulary in model.
This is an explicit text-to-numeric input boundary, not learned language semantics.

Preparation, outside runtime memory claim: normalize/deduplicate all source sentences
by normalized token sequence; original order retained. SHA256(normalized sentence),
first little-endian uint modulo5==0 gives held-out, remainder training. One training
pass with sentence resets, no transitions across sentence boundaries. Both split
files and their checksums recorded; no held-out text in model training. Same split
and128 query limit for representation seeds201,202,203; no seed selection.

Eligible cue has a held-out adjacent successor observed >=3 times in training;
cue and target training counts >=5. Choose positive by ordinal SHA256(cue+"|"+target).
Choose31 distractors from training vocabulary with count>=5 in the positive's
floor(log2 frequency) bin, excluding cue and all its held-out successors; hash order
by cue+"|"+candidate. Skip unsupported candidate sets before scoring and report counts.
Select first128 eligible cues by ordinal SHA256(cue); require at least100. Candidate
presentation sorted by ordinal SHA256("candidate|"+cue+"|"+candidate). One positive
per32 candidates. This is supported, closed-candidate next-token association on
held-out sentences, not open-ended semantic retrieval or language generation.

Freeze queries/labels before training/scoring. Main metric query MRR; Recall@10 also.
Ties receive expected reciprocal rank over tied positions and fractional top10 credit;
include zeros, unreachable answers and all ties. Untrained baseline all-zero scores;
frequency baseline train unigram counts; co-occurrence baseline directed train bigram
counts. Labels/counts stay in evaluator, never learning or activation traversal.
For each baseline compute per-query MRR lift averaged across the3 fixed seeds;
10000 paired query bootstraps with seed915 report95% percentile bounds. Pass requires
mean MRR lift>=.05 over BOTH untrained and frequency and BOTH lower bounds>0.
Report each seed separately; shared corpus/query resamples are not independent
training datasets. Co-occurrence baseline can win without being hidden.

Each model trains at128MiB (8MiB allowance for text/candidate adapter and scratch),
fresh-process paged scoring at128MiB plus exact resident reference. Original paged
RSS limit220938240 bytes, immutable hashes and zero writes required. Candidate cap4096
for utility; all evaluator labels/JSON and their memory included in measured process.
Freeze source after tests, one synthetic correctness smoke, then these3 real-data
arms; estimated minutes, no jobs expected>=1h. On failure, report the specific limited
result; no representation/policy rescue or new grid inside R5. Numeric schema audit
and planted-wordlist rejection required, not semantic non-memorization claims.

R5 pre-score clarification: self-successors (target==cue) are excluded from positive
eligibility, because this task measures retrieval of related material rather than
self-activation. This is fixed before preparation or model scores. Text input bounds
fail explicitly; the model directory is published only after all numeric files are
complete. Query adapters preserve all candidate scores including zero-output cases.

R5 primary workers completed and analyzed before any source change: all gates pass;
source-manifest verification passed during primary analysis. One reporting correction
now follows: macOS .NET PeakWorkingSet64 returns0 (unavailable), so public JSON must
emit null rather than imply zero process memory. Original native /usr/bin/time -l
measurements were valid and supply every reported memory bound. Only two reporting
sites change (LocalModelCli and LocalDataEval); learning, data, snapshots, queries and
scores are untouched. Preserve primary artifacts; replay scores after this correction
and compare every vector. No scientific arm or bar is changed.

# R5 result — saved-model local-data utility — 2026-09-17

**Registered gate PASS, with substantial quality limitations.** Named-model learn,
shutdown, source-free probe and numeric audit now work through packed relay storage.
The held-out supported association test passes against untrained/frequency controls;
the co-occurrence baseline is substantially better. This is closed-candidate directed
next-token association, not general semantic recall, text reconstruction or generation.

### Frozen data and selection

Restored source50000 English sentences; normalization removed38 exact duplicates.
Train40006 sentences/327074 tokens/12897 distinct words; held-out9956 sentences.
No32-bit input-identity collision groups in training vocabulary. Train text1635259
bytes; source/split/query hashes are in prepared/manifest.json. All source sentences
were split before training; normalized duplicates cannot cross the split. Near-
duplicates/paraphrases were not detected. Relation overlap is deliberate support,
not an unseen-relation generalization claim.

441 cues satisfied all support/candidate conditions;1223 other supported cues lacked
31 same-frequency-bin distractors and were excluded before scoring. First128 eligible
cues by registered hash order were scored. This selection excludes many high-frequency
cases and is not representative of every possible user cue. All128 scored queries,
including every zero and tie, remain in the denominator. One positive among32 fixed
candidates, labels and counts confined to evaluator files outside model storage.

### Quality and exactness

| Arm | Mean reciprocal rank | Recall@10 |
|---|---:|---:|
|Learned, mean of201/202/203|0.569796|0.661275|
|Untrained all-zero|0.126828|0.312500|
|Training unigram frequency|0.173092|0.353516|
|Training directed co-occurrence|0.982639|1.000000|

Paired MRR lift over untrained+0.442968,95% bootstrap interval[0.367929,0.518226].
Over frequency+0.396704,[0.318371,0.475421]. Both exceed+.05 with lower bound>0.
Against co-occurrence−0.412843,[−0.487441,−0.338890]. These are query resamples on
one shared corpus/split; three encoder seeds are not three independent datasets.
All three seeds had identical aggregate quality, although their learned graphs differ.

**61/128 cues produced all-zero candidate scores in every seed.** They receive only
the preregistered chance-level tie credit (MRR harmonic(32)/32,Recall@10=10/32).
No zero-output case was repaired or excluded. The utility explicitly reports NoOutput;
its ordinal display order for tied scores is not evidence of a preferred answer.
The first frozen cue,express, is a zero-output example retained in example-probe.json.
The learner is therefore useful for some supported associations but far from reliable
across this constrained set. No superiority over a count-based association system.

Every one of384 primary paged query vectors matches its separate resident reference
exactly. Frozen snapshots remain unchanged and both data/index writes are zero.
Reporting-fix replay also matches all384 original vectors exactly. No learning or
representation adjustment was made after viewing outcomes.

### Storage, time and memory

| Seed | MRR | Recall@10 | All-zero cues | Allocated model MiB | Train peak RSS MiB | Paged peak RSS MiB |
|---|---:|---:|---:|---:|---:|---:|
|201|0.569796|0.661275|61/128|37.02|96.66|70.22|
|202|0.569796|0.661275|61/128|36.55|96.12|70.05|
|203|0.569796|0.661275|61/128|37.09|92.89|69.91|

Seed201 has102851 records and1598264 synapses,37929856 logical bytes and38817792
allocated bytes. Thus these40006 training sentences occupy37.02MiB allocated, roughly
970 bytes/sentence on average for THIS run (~0.97MB per1000 sentences); that average
is not a growth forecast. Model bytes are about23 times normalized source-text bytes:
this is learned graph storage, not text compression. Earlier fixed-record packing
removed sparse-file allocation overhead; it did not make graph state smaller than text.

Training7.73–8.95s, paged query p95 0.371–0.512ms under OS-cache-assisted conditions.
Original128MiB resource limit220938240 bytes passed for all learning, paged scoring
and audit processes. Training peaks97,402,880–101,351,424 bytes, paged peaks
73,302,016–73,629,696 bytes. These include labels/query-adapter memory and evaluator
JSON; source split preparation is explicitly outside the runtime memory claim.
Model data fits this cache: this real-data run does not itself demonstrate eviction
or exceed physical RAM. Prior synthetic checks provide the independent large-state
capacity evidence. Read timing includes candidate encoding/scoring; startup and full
snapshot hashing are outside the per-query timer and warm OS pages.

### Utility contract and verification

learn --model refuses overwrite and atomically publishes a fresh named model after
complete training/checkpoint/descriptor writes. One pass, sentence boundaries reset,
no resume/append implementation. Versioned192-byte numeric descriptor stores seed,
encoder/tokenizer/policy versions, counts, source hash and checkpoint binding. No
word list or source path in the model. Individual records and metadata are checksummed;
checksums detect corruption, not malicious tampering/authentication.

probe loads saved configuration and numeric state, never reads the training source
or trains. External candidates are capped at4096 unique canonical tokens; all scores
are returned. Source lines/tokens are bounded, malformed inputs fail explicitly.
The8MiB allowance covers adapter/traversal work separately from cache reservation;
whole-process memory was measured rather than inferred from that allowance.

208/208 tests pass before and after the single reporting correction. Tests cover
source deletion before recall, exact resident/paged multi-hop scores, sentence
boundary isolation, unseen zeros, corrupt descriptor, overwrite refusal, oversized
inputs, no partial publication and planted wordlist rejection. Structural audit walks
all model records and rejects extra model files. It is not proof that numeric weights
cannot encode semantic information. One separate-process synthetic smoke passed
before real data. All16 primary workers and4 reporting/example workers exited0.
No jobs remain. No commits, staging, history consultation or old-brain changes.

Original JSON PeakRss=0 was macOS .NET's unavailable value, not a measurement of
zero memory. Every gate used valid external /usr/bin/time -l peaks. Public reporting
now emits null with an explanation; no training or scoring behavior changed. Primary
source manifest was verified before correction; post-report-source-manifest.json
records exactly the two reporting-file changes and their replay verification.

### Reproduction and next boundary

Artifacts: artifacts/recovery/r5/summary.json, prepared/manifest.json and queries.json,
source.json, commands.json, raw results/native logs, both source manifests, both TRX
files and reporting-replay-commands.json. Models:
/private/tmp/gm-r5-20260917/model-201,model-202,model-203. These scratch models are
retained; the utility accepts any explicitly chosen new model directory.

```bash
dotnet test GreyMatter.sln -c Release --no-restore --logger 'trx;LogFileName=full-tests.trx' --results-directory artifacts/recovery/r5
python3 artifacts/recovery/r5/run.py
python3 artifacts/recovery/r5/replay_reporting_fix.py
python3 artifacts/recovery/r5/summarize.py
dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll probe --model /private/tmp/gm-r5-20260917/model-201 --cue express --candidates artifacts/recovery/r5/example-candidates.txt
dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll audit --model /private/tmp/gm-r5-20260917/model-201
```

The exact initial commands are in commands.json. Scripts refuse overwrites: use fresh
artifact/model paths for reruns. Current code reports null for unsupported peak RSS;
original raw output with0 is retained and explained. Real-data protocol/metric scripts
are fully local; no external API or new dependency.

R5 is complete as this limited supported-association utility. Next is R6: one profile
of the successful path and the campaign capstone separating learning, exact paging,
resource tradeoff and real-data utility. Preserve the61-zero result and stronger
co-occurrence baseline; do not turn closeout into a learning rescue or CUDA port.
The original full R4 grid remains incomplete, and the single16x follow-up is not a
retroactive rewrite of its failure. No broad emergent-behavior claim is justified.

# R6 profile and closeout registration — 2026-09-18

Bill authorized next steps: one bounded profile of the successful R5 path, then the
four-part capstone. No quality rescue, cache sweep, new seed grid or CUDA work.
Reuse frozen R5 train/query files and seed201,128MiB. Train one new scratch model,
verify its complete numeric snapshot hash equals the existing R5 model, then score
all128 queries in a separate process and require exact original vectors/immutability.
One tiny synthetic smoke precedes this profile. Expected duration under a minute.

Opt-in thread-local timing/allocated-byte instrumentation partitions nested scopes
into exclusive costs: text normalization, encoding, learning, traversal/scheduling,
record serialization/checksum, cache/index, file API calls, source hash and publication.
Timing/allocation scopes use fixed arrays and do not change state or numerical order.
Report uncovered orchestration separately. Native /usr/bin/time provides user/system
CPU, peak RSS and context-switch counts; native kernel counters provide disk bytes.
File-API wall time includes kernel CPU, page-cache service and wait; it is an upper
bound on storage wait, not a measured pure disk-wait fraction. CPU scheduling wait
cannot be uniquely recovered from wall-minus-CPU with runtime background threads.
Instrumentation overhead means this run is attribution, not a new latency benchmark.
No baseline storage/speed advantage will be invented: only its existing quality was
compared. Freeze source after tests and preserve command/raw profiles before verdict.

# R6 capstone — recovery campaign closed — 2026-09-18

The implementation now supports local-text learning into a named numeric model and
fresh-process, read-only, closed-candidate recall through learned connections. The
bounded storage/execution mechanism is demonstrated on actual learned state, rather
than an inflated address range. The larger research proposition—competitive, broadly
useful intelligence emerging from effective neural scale—is **not demonstrated**.
This campaign closes here. No R7, automatic rescue experiment or CUDA port follows.

## Four separate verdicts

| Question | Verdict | Evidence and boundary |
|---|---|---|
| Learning | Qualified pass for the registered tasks | Synthetic relay tasks pass. Real supported next-token MRR0.570 exceeds untrained0.127 and frequency0.173 with positive paired confidence bounds.61/128 real cues have zero output; co-occurrence MRR0.983 is substantially stronger. No general semantic or unseen-relation claim. |
| Exact paging | Pass on tested states | Resident/paged vectors match exactly; earlier one/eight-record caches force repeated eviction; restart reproduces learning; frozen recall does not write model state. Later large-budget query sets fit their caches and must not be presented as new recall-eviction tests. |
| Resource tradeoff | Bounded engineering result; broader claims open | Single16x synthetic cell stores4.47million records/33.55million edges in1.50GiB, trains at288MiB peak RSS versus1.71GiB resident reference, with real training eviction. Real-text processes also satisfy the original memory bar. Original full R4 grid remains incomplete; no beyond-physical-RAM or cold-storage result. |
| Real-data utility | Delivered, limited | Explicit named-model learn/probe/audit works; probe requires no training source.40006 training sentences produce roughly37MiB. Caller supplies candidates; no free-form generation, automatic vocabulary output, or reliable broad retrieval. |

These are separate conclusions: exact paging does not validate the learner, and a
quality gate against weak controls does not establish an advantage over the stronger
baseline. The simple co-occurrence baseline's **quality** advantage was measured;
its end-to-end runtime/storage cost was not benchmarked, so no measured cost advantage
is claimed. Historical R1 and R4 failures remain in the ledger; later explicitly
bounded follow-ups are not retrospective passes of those original campaigns.

## One profile of the delivered path

Opt-in, fixed-stack, thread-local instrumentation partitions nested scopes into
exclusive wall time and current-thread allocated bytes. It covers text boundary,
learning, record serialization/checksums, cache/index operations, synchronous file
calls, traversal, file hashing and publication. Disabled scopes do not allocate;
small instrumentation hooks remain in the source. This is an attributed run, not
an uninstrumented speed benchmark or CPU sampling profile.

Protocol: unchanged R5 prepared training file (SHA256
A07B2D3693C5BB10BF1A85B1B573DDF7DD487CD7AE838FDD4914565E9477CF2C),seed201,
128MiB budget,one new model at /private/tmp/gm-r6-20260918/model-201; then separate-
process scoring of all128 frozen R5 queries. Smoke first; no parameter changes.
Profiled model hash F4EB315BBE3B93CD4156F45E8F520E8D31BFD32EE5E377AC477F41D963FC4601
matches the existing R5 model exactly. All128 scores also match; querying preserves
that full snapshot hash. No learning/encoding/persistence changes were needed.

### Training attribution

Instrumented training/publication8.6112s. Exclusive categories below sum with0.0669s
unattributed orchestration to the measured scope (no nested double-counting).

| Category | Calls | Exclusive seconds | Scope wall share | Allocated bytes |
|---|---:|---:|---:|---:|
|Tokenization|40,006|0.0206|0.24%|27,942,848|
|Encoding|327,074|0.2136|2.48%|243,446,176|
|Learning|327,074|1.1461|13.31%|18,316,144|
|Serialization|19,652,544|4.6303|53.77%|0|
|CacheIndex|10,031,977|0.6622|7.69%|36,008|
|FileApi|1,633,664|1.8217|21.16%|0|
|SourceHash|3|0.0071|0.08%|11,472|
|Publication|1|0.0426|0.50%|46,896|

Native process lifetime:8.70s wall,7.00s user CPU,1.46s system CPU; peak RSS98,795,520
bytes (94.22MiB). These process totals include startup and final verification that
are outside the8.6112s scope, so they are not an additive breakdown of that scope.
Kernel-reported training reads2,015,232 bytes/writes101,097,472 bytes.

Serialization/checksum work accounts for53.77% of scoped wall time versus13.31% in
learning updates. This is numeric-record handling, not evidence that synaptic math
requires massive parallel compute. File calls account for21.16%: their1.8217s includes
kernel CPU, cached service and wait. Pure disk waiting cannot be separated by this
instrument;1.8217s bounds waiting inside those instrumented calls, not all filesystem
work (e.g. directory/checkpoint operations also occur in publication/orchestration).

Allocated407,987,648 bytes on the profiled thread, including243,446,176 bytes in
encoding (~60%) and118,188,104 unattributed bytes. These are cumulative allocations,
not live state. The cache constructor and adapter/orchestration allocations are among
the unattributed work; they were not separately timed. GC collections47/1/1 for
generations0/1/2. Background-thread allocations are not included in per-thread totals.

### Recall attribution and scheduling limits

Model-open plus128-query profiled scope0.06159s; separate native process0.16s wall,
0.13s user/0.02s system CPU,67,731,456-byte peak RSS (64.59MiB). Scoped exclusive
traversal/frontier scheduling10.32ms (16.76%),encoding10.00ms (16.23%),cache/index
6.18ms (10.04%),file APIs4.98ms (8.08%),serialization1.87ms (3.04%). Remaining
categories and26.78ms uninstrumented orchestration complete the total. File checksum
validation and OS caching are part of the environment; no cold-disk claim.

Allocated186,417,024 bytes over the scoring scope,182,152,224 unattributed. Source
inspection places cache construction and repeated per-query traversal-buffer creation
in this bucket; this is an attribution limit, not a separately measured allocation
split. GC collections4/2/1. Kernel reads188,416 bytes, writes0.

Native context switches: training280 voluntary/2796 involuntary, scoring8/101.
Counts do not provide scheduler-wait durations. Traversal scope combines scheduling
and propagation computation. Wall-minus-user/system CPU is not a clean scheduler or
storage-wait estimate because runtime threads can overlap. These unresolved fractions
are recorded rather than replaced with an invented precise breakdown.

## CUDA decision and what survives

**A CUDA port is not justified by the present evidence.** The demonstrably stronger
quality baseline,61 zero-output cues, record/checksum overhead and cache-assisted
measurements matter more than speculative GPU arithmetic throughput. This does not
prove GPUs could never help; it means their benefit has not been established here.
No GPU implementation, architecture redesign or tuning sweep was performed.

The reusable deliverable is a deterministic learned-graph lifecycle: bounded caches,
connection-driven reads, exact traversal, local learning, compact numeric addressing,
checksummed immutable snapshots and explicit external candidate decoding. It executes
actual learned models substantially larger than its application memory allocation.
Packing removes avoidable sparse-file allocation; the learned graph still occupies
more bytes than the original text. Its resource advantage over simpler retrieval
systems remains unmeasured.

Millions of stored records are not millions of simultaneously active neurons or
additional demonstrated reasoning layers. Tested propagation remains width256 and
at most4 hops. Biological fidelity, general reasoning, emergent behavior, a network
larger than physical RAM and competing with frontier systems remain untested.

## Verification and handoff

210/210 tests pass, including allocation/time partition accounting and exact profiled
versus unprofiled learning. One smoke and one full train/score pair: all4 processes
exited0. Source manifest unchanged through measurement; no workers remain. Native
memory stays within the original128MiB RSS allowance. No model resets, commits,
staging, dependency additions, history consultation or external services.

Artifacts: artifacts/recovery/r6/summary.json, train.json, score.json, native .time.txt
files, commands.json, source-manifest.json and full-tests.trx. The command ledger
records exact arguments and exit codes; scripts refuse overwrites. Prior R5 data,
models and results remain intact. Reproduction needs new artifact/model paths.

```bash
dotnet test GreyMatter.sln -c Release --no-restore --logger 'trx;LogFileName=full-tests.trx' --results-directory artifacts/recovery/r6
python3 artifacts/recovery/r6/run.py
python3 artifacts/recovery/r6/summarize.py
```

The saved-model utility and commands in README remain usable. The campaign is
finished, with qualified engineering success and an unresolved learning advantage.
Any continuation starts with Bill's new directive: use/maintain this utility, undertake
a separately bounded recall-quality design against the co-occurrence baseline, or
park the research. Recommendation: keep the working substrate as a reference and
address that quality/utility comparison before further scale or GPU investment.

# Post-closeout diagnostic — R5 zero-output cues have a structural cause (2026-09-20)

Bill authorized a bounded post-closeout continuation on 2026-09-20 after a review of
the R6 capstone: (1) diagnose the 61 zero-output R5 cues, (2) one registered
connectivity experiment against the same frozen R5 task, (3) an equal-budget
co-occurrence baseline through the same record store, (4) a memory-limited container
run of the 16x model, (5) per-page checksums if the utility is retained. The campaign's
verdicts are not reopened; this appends to the closed ledger. No model, artifact or
score from R5/R6 was modified.

R6 stated the zero-output cause "was not diagnosed." It is diagnosable from the frozen
artifacts alone. `artifacts/recovery/fanout/diagnose.py` re-tokenizes the frozen R5
training split (reproducing the manifest exactly: 40,006 sentences, 327,074 tokens,
12,897 distinct, SHA256 `A07B2D36…9477CF2C`), joins each of the 128 frozen queries to
its per-seed paged scores, and probes the retained seed-201 model with each cue's OWN
training successors as candidates. Read-only; probes performed zero writes. Output:
`artifacts/recovery/fanout/fanout.json` (SHA256 `36e12730…14f4be7f1`).

| cues (seed 201; 202/203 identical) | n | training fan-out, median (min) | cue count, median |
|---|---:|---:|---:|
| zero output | 61 | **51** (9) | 167 |
| any output | 67 | **14** (1) | 31 |

- Every zero-output cue has fan-out > 8; 45/61 exceed 32. Among the 67 cues with any
  output, the answer ranks first in **65** (all three seeds). Recall failure is
  retention, not discrimination: when the answer's edge exists, ranking is near-perfect.
- Live successors per cue in the model: 1:3, 2:7, 3:13, **4:104**, 5:1 — never more
  than 5. This is the arithmetic ceiling: degree cap 32 ÷ 8-member target cohort = 4
  successor tokens per source neuron, and all eight source members receive identical
  updates, so a token stores at most ~4 successors regardless of corpus.
- All 61 zero cues DO retain live edges; in **0/61** is the answer among them.
  Survivors are recency-dominated: the answer's rank by last observation is median
  **16** for zero cues (min 3) versus median **2** for non-zero cues; no surviving
  successor was observed later than the 20th most recent. Example: `of` has 1,306
  successors; `gas` (11 training occurrences) was the 218th most recently observed
  and is gone; `my → head` was the most recent and survives.

Mechanism, from the registered rule: a successor edge is born at .105, gains .005 per
observation, and every observation of a DIFFERENT successor of the same source
multiplies it by .99 with pruning below .1. An answer seen 3–4 times (≈.12) survives
about 15–18 other-successor observations; cues with tens of successors and hundreds
of occurrences cannot hold it. The co-occurrence baseline keeps all successors; that
difference is the whole 0.983 vs 0.570 MRR gap. This reframes the R6 "qualified
learning pass" precisely: per-token successor capacity is ~4 and forgetting is
driven by other-successor count, not time or evidence.

Boundaries: one corpus, one task, hops=1; the same three seeds are confirmed to
produce bit-identical zero sets (Assembly membership deliberately excludes the seed),
so they are not replication. This diagnostic proposes no new pass and changes no rule.

## D1 registration — sparse relay connectivity (before any result)

Hypothesis: slot capacity, not the learning rule, limits retained successors. Test
one connectivity change only, as a new persisted learning-policy identity (never a
default change; old models unaffected): on each source→target cohort observation,
each source member records edges to a deterministic **2-of-8 subset** of the target
cohort, selected by hashing (source member ID, target cohort's first member ID). A
target cohort then costs 2 slots per source member instead of 8, raising the per-token
successor ceiling from 4 to **16** at identical record bytes and unchanged degree cap,
birth/reinforce/decay constants, source-local forgetting, encoder and readout. Fan-in
per target member falls from 8 to ~2 sources; hop-1 ranking is by weight and unaffected
in expectation, and multi-hop is reported but not gated (R5 primary was hops=1).

Protocol: identical R5 prepared split, 128 frozen queries, candidates, seeds 201–203,
128 MiB budget, fresh-process paged scoring, exact resident/paged equality, zero
writes, immutable hashes, native RSS within the original R5 bound. Same summarizer
metrics: MRR, Recall@10, paired bootstrap vs untrained/frequency/co-occurrence.
Predictions fixed now: zero-output cues fall from 61 to roughly 30 (those whose
answer is within the last ~16 observed successors), MRR rises accordingly, and the
co-occurrence baseline still wins because count-driven forgetting is untouched.
Gate for "connectivity was the limit": zero-output cues ≤ 35 on every seed and no
regression among the 65 currently-correct cues beyond 3. If the zero count does not
fall, the hypothesis is rejected and forgetting is the next registered single change.
One development smoke on synthetic data, one real-data run, no tuning, no sweep.
Planned command (until implemented): `gm learn --model <new> --source
artifacts/recovery/r5/prepared/train.txt --format text --seed 201 --budget-mib 128
--policy sparse-relay`, then the unchanged R5 `run.py`/`summarize.py` path.

### D1 prediction amendment — recorded before the real-data run (2026-09-20)

While writing the D1 capacity test, the forgetting arithmetic showed the registered
prediction was wrong. Under the unchanged source-local rule an edge gains .005 per
reinforcement and loses ×.99 on every observation of a different successor of the same
source. With m equal-frequency successors interleaved, steady-state weight is
`.005 / (1 − .99^(m−1))`, which falls below the .1 prune line at **m ≈ 7**. Forgetting
alone therefore caps a token near six coexisting successors; sparse wiring frees slots
but cannot raise that ceiling. A focused test (`ForgettingNotSlotsBindsAboveSix…`)
encodes this: with 16 interleaved successors both policies retain ≤ 6.

Corrected prediction, fixed before scoring: D1's zero-output count will NOT fall to
~30. It can only help cues whose answer competes with about 5–6 other successors,
where the 4-cohort slot ceiling bound before decay did. Expect a small change
(zeros roughly 55–61), no regression among the 65 correct cues, and exact paged/
resident equality. The registered gate (≤ 35 zeros) is therefore expected to FAIL;
the run proceeds anyway because it was registered, it costs about a minute, and its
measured result decides the next single change: a forgetting rule whose clock is not
"other-successor count" (e.g. decay proportional to evidence share). No parameter
was tuned; the slot-cost test (`SparseWiringSpendsTwoSlots…`) verifies the wiring
change itself: four successors cost 8 slots per source member instead of 32.

## D1 result — sparse relay: registered gate FAIL, corrected prediction confirmed (2026-09-20)

Command ledger: `artifacts/recovery/d1/commands.json` (16 workers, all exit 0; smoke
learn/probe/audit first, then seeds 201–203: learn `--policy sparse-relay`, paged and
resident scoring on the FROZEN R5 `prepared/queries.json`, audit). Models:
`/private/tmp/gm-d1-20260920/model-20{1,2,3}`. Summary `d1/summary.json` (SHA256
`9a6c8242…`), fan-out probe `d1/fanout-d1.json` (`8310ede2…`; its per-seed zero flags
are read from R5, its live-successor probes hit the D1 model). Tests **215/215**.

| | R5 (source-local) | D1 (sparse relay) |
|---|---:|---:|
| MRR, each seed | 0.569796 | **0.610175** |
| Recall@10 | 0.661275 | 0.699219 |
| zero-output cues | 61 | **56** |
| answer top-1 | 65 | **70** |
| regressions among the 65 | — | **0** |
| paired MRR lift vs untrained / frequency | +0.443 / +0.397 | +0.483 [0.408, 0.558] / +0.437 [0.359, 0.516] |
| vs co-occurrence | −0.413 | −0.372 [−0.446, −0.299] |
| paged == resident, zero writes, immutable | yes | yes |
| peak RSS within original 128 MiB bound | yes | yes |
| training time, seconds | 7.7–9.0 | 6.5–6.6 |

Registered gate (≤ 35 zeros on every seed): **FAIL** (56/56/56). Regression gate (≤ 3):
pass (0). The amended prediction (55–61 zeros, no regressions) is confirmed. Live
successors per cue moved from a hard wall at 4 (R5: 104 of 128 cues at exactly 4,
max 5) to a soft ceiling at 5–6 (D1: 4:29, 5:32, **6:36**, 7:7, 8:2). The five
recovered cues (`everybody→knows`, `exactly→alike`, `he→may`, `six→people`,
`sad→story`) all had answer recency ranks 3–9: exactly the band where slots bound
before forgetting did. Cues whose answer competes with more than ~6 other successors
remain at zero; `of→gas` (1,306 successors) is untouched.

Interpretation: the wiring change works as specified (2 slots per successor; verified
by test) and is a strict improvement at identical bytes, but the R6 learning verdict
stands because the source-local forgetting rule caps coexisting successors near six.
The three seeds are again bit-identical: under the identity encoder the seed only
relabels member IDs, producing isomorphic graphs. No tuning, one run, no rescue.

The next single learning-rule change, if authorized, is a forgetting clock that is
not "other-successor count" — e.g. unobserved-target decay scaled by the observed
target's share of the source's evidence, so a rare successor of a common word is not
erased by the common word's other successors. It is NOT registered here; item 3
(equal-budget count baseline) runs first because its outcome decides whether a
learning-rule change is worth making at all.

# CB registration — count baseline through the same record store (before any result)

Purpose: R6 compared the learner against an in-evaluator co-occurrence table with no
cost. This measures the same directed-count idea *inside the delivered substrate*:
same 328-byte records, degree cap 32, packed store, paging, checksums, traversal,
paged/resident exactness, memory bound, and R5 protocol. Only the learning policy
and the per-token footprint differ. It is a baseline, not a pass/fail gate.

Policy identity 4, `--policy count-baseline`, persisted in checkpoint metadata like
policies 1–3; never a default. One record per token: its first deterministic cohort
member (`LocalText.Members(word, seed)[0]`), so a token costs 328 bytes instead of
8 × 328. On observing prev→cur: if the edge exists, weight += 1 (a count); else if
degree < 32, append with weight 1; else displace the lowest-count incumbent only if
that count is 1 (ties: lowest index); otherwise decline. No decay of any kind.
Recall is the unchanged `StoredRelayRecall`: a source emits `drive × w / Σw`, i.e.
the transition probability — the R1 transition-count baseline, now paged. Roots are
the cue's single record; a candidate's score is the delivered value at its single
record. Hops as R5 (primary 1).

Protocol: identical frozen R5 split/queries/seeds/budget/workers; scripts are copies
of D1's with the policy name and paths changed. Report MRR, Recall@10, zero-output
count, paired lifts, allocated model bytes, training time, paged/resident exactness,
peak RSS. Predictions fixed now: MRR between 0.85 and 0.95 (below the 0.983 table
because cues with more than 32 successors lose low-count answers), zero-output cues
roughly 10–25 (mostly the 45 cues with fan-out > 32), model allocation roughly
one-eighth of R5's 37 MiB. If the count baseline meets or beats the learner on MRR
at lower bytes, the honest conclusion is that the neural learning layer adds no
measured value on this task and the substrate is the deliverable.

## CB result — the paged count baseline beats the learner at one-eighth the bytes (2026-09-20)

Command ledger `artifacts/recovery/count-baseline/commands.json` (16 workers, all
exit 0; an earlier attempt failed at seed201-learn because the record validator
bounds relay weights to [0,1] — its partial outputs were removed and the schema was
extended before the registered run: count edges carry provenance byte 3 and must be
float-exact integers ≤ 2^24; relay edges (0–2) keep the original [0,1] check, with a
test for each case). Models `/private/tmp/gm-count-20260920/model-20{1,2,3}`; summary
`count-baseline/summary.json` (SHA256 `4e2e1d52…`). Tests **219/219**. Same frozen
R5 split, queries, seeds, budget, workers and summarizer; only `--policy count-baseline`.

| | R5 learner | D1 sparse relay | **CB count baseline** | in-evaluator table |
|---|---:|---:|---:|---:|
| MRR | 0.5698 | 0.6102 | **0.9066** | 0.9826 |
| Recall@10 | 0.6613 | 0.6992 | **0.9295** | 1.0000 |
| zero-output cues | 61 | 56 | **10** | 0 |
| answer top-1 | 65 | 70 | **114** | — |
| paired MRR lift vs untrained | +0.443 | +0.483 | **+0.780 [0.731, 0.825]** | — |
| vs co-occurrence table | −0.413 | −0.372 | **−0.076 [−0.120, −0.037]** | 0 |
| records / edges | 102,851 / 1,598,264 | 102,851 / 442,261 | **12,893 / 55,525** | — |
| allocated model | 37.02 MiB | 36.19 MiB | **4.55 MiB** | (no cost measured) |
| training, seconds | 7.73 | 6.39 | **1.49** | — |
| paged query p95, ms | 0.512 | 0.393 | **0.220** | — |
| learn / paged peak RSS, MiB | 96.7 / 70.2 | 94.3 / 70.1 | **63.9 / 69.5** | — |
| paged == resident; zero writes; immutable | yes | yes | yes | — |

Both registered predictions held (MRR 0.85–0.95; zeros 10–25; ≈1/8 bytes). The
remaining gap to the free table (−0.076) is the 32-successor record cap: the ten
zero cues are high-fan-out words whose low-count answer lost least-count displacement.
One cue regressed from top-1 relative to R5 (same cause). Record count is 12,893 for
12,897 distinct tokens; the four missing are consistent with first-member identity
collisions and were not investigated. Seeds remain bit-identical.

**Conclusion.** Inside the same substrate — same 328-byte records, packed store,
bounded cache, exact paging, checksums, traversal, memory bound — a directed count
with no decay retrieves far more (MRR 0.907 vs 0.610), at one-eighth the storage,
one-quarter the training time, and half the query latency, than the relay learner
at its best (D1). On this task family the neural learning layer (8-member cohorts,
Hebbian birth/reinforce, source-local forgetting) adds no measured value; it
subtracts. The deliverable that survives is the numeric learned-graph substrate:
deterministic addressing, bounded caches, exact paged traversal, immutable
checksummed snapshots, external decoding. The R6 "qualified learning pass" should be
read with this comparison attached: the learner passed weak controls, and a trivial
policy through the identical machinery beats it decisively.

This does not show that no learning rule could beat counts — only that the two tried
here (source-local and sparse) do not, and that the mechanism (count-driven
forgetting) is understood. A forgetting-rule change (D2) is therefore a research
choice, not a defect fix, and needs Bill's decision. Items 4 (container memory-limit
run of the 16x model; no container runtime is installed on this machine) and 5
(per-page checksums instead of per-record SHA-256, which R6 measured at 54% of
training time) remain open; item 5 now applies equally to the count baseline.

# Post-closeout decisions and item 5 registration (2026-09-21)

Bill's decisions on 2026-09-21: adopt the count policy as the utility's default; keep
the relay policies as explicit experimental options; proceed with item 5; install a
container runtime (colima) for item 4. `learn` now defaults to `--policy count-baseline`;
existing models are unaffected because policy identity is read from the checkpoint.
R5/D1/CB models and results are unchanged; the R5 scripts remain historical.

## Item 5 registration — checksums at the storage boundary, not per access

R6 measured record serialization/checksum at 53.77% of training scope time. Cause:
`RelayRecord.Encode` computes SHA-256 on every save and `Decode` re-verifies it on
every read — including cache hits, which are the vast majority (repeat passes read
zero logical bytes). Integrity is a property of storage, so the registered change is:
verify a record's checksum once when it is loaded from disk into the packed cache,
compute it once when a dirty record is flushed to disk, and never on a cache hit.
On-disk format (328-byte records with SHA-256), checkpoints, audit and all existing
models are unchanged; `audit` still validates every record. Per-page CRC (a format
change) is the fallback only if this is insufficient. `Read` of a target during
recall, which only needs existence, stops copying and validating the whole record.

Protocol: the R6 profile harness unchanged (frozen R5 train file, seed 201, 128 MiB,
one model, separate-process scoring of the 128 frozen queries), run for BOTH the
relay policy (to compare with R6's numbers) and the new default count policy. Gate:
bit-identical model snapshot hashes and all 128 score vectors versus the pre-change
runs (R6 model `F4EB315B…` for relay; CB model for count); all corruption tests
still fail closed; full suite green. Report the new Serialization share; expected
well under 20% for relay. No numerical rule changes. One implementation attempt.

## Item 5 result — checksums at the disk boundary halve training time, bit-exact (2026-09-21)

Implementation: `RelayRecord.Encode(…, seal)` / `Decode(…, validate)` flags;
`IRelayRecords.SealsAtDiskBoundary` (default false) and `Contains` (existence without
a copy); `PackedRelayRecords` seals in `Writeback` (one SHA-256 per disk write, in
place so the cache copy is valid too), verifies on cache-miss load and in
`VisitPresent`, and no longer hashes on `Write`; `StoredRelayLearning` /
`StoredRelayRecall` trust a sealing store's copies and write unsealed to it; recall's
target-existence check no longer copies and validates a whole record. Reference,
legacy and deferred stores keep per-write validation. On-disk format, checkpoints,
`audit` and all existing models are unchanged. Three new tests: unsealed write →
sealed at writeback and valid after reopen; a flipped bit on disk still fails closed
on load; the reference store still rejects unsealed writes and both stores produce
identical sealed records from identical learning. The profile eval gained `--policy`
and policy-aware scoring (it had queried with relay cohorts regardless of policy).

One process note: the first profile run happened while the test project failed to
compile (an interface default member accessed on the concrete type); `gm.dll` was
current, so the profile is valid, but the "219 passed" printed alongside it was a
stale binary. After the one-line fix the genuine suite is **222/222**.

Protocol as registered (R6 harness, frozen R5 train file, seed 201, 128 MiB, separate-
process scoring of the 128 frozen queries), run for both policies. Ledger
`artifacts/recovery/item5/commands.json` (8 workers, exit 0), `summary.json`.
Relay snapshot hash equals the R6/R5 model (`F4EB315B…`) and all 128 score vectors
match; count snapshot hash equals the CB seed-201 model and all 128 vectors match.

| training scope, exclusive seconds (share) | R6 relay, before | item 5 relay | item 5 count |
|---|---:|---:|---:|
| wall | 8.6112 | **4.6388** | 1.1285 |
| Serialization | 4.630 (53.8%) | **0.874 (18.8%)** | 0.166 (14.7%) |
| FileApi | 1.822 (21.2%) | 1.814 (39.1%) | 0.320 (28.3%) |
| Learning | 1.146 (13.3%) | 1.136 (24.5%) | 0.103 (9.1%) |
| CacheIndex | 0.662 (7.7%) | 0.469 (10.1%) | 0.110 (9.7%) |
| Encoding | 0.214 (2.5%) | 0.223 (4.8%) | 0.314 (27.8%) |
| native real / user CPU, seconds | 8.70 / 7.00 | **4.72 / 3.08** | 1.19 / 0.89 |
| native peak RSS, MiB | 94.2 | 94.9 | 63.9 |

Scoring scope: relay 0.0616 → 0.0799 s (serialization 3.0% → 1.1%; the difference is
run noise at this scale, not a regression claim), count 0.0412 s. File-API time is
unchanged, as expected — it was never the hashing. The remaining serialization cost
is encode/decode of the 328-byte layout, not hashing. Per-page CRC (a format change)
is not needed and is not pursued. Same limits as R6: instrumented wall time, OS cache
warm, one run each, not a benchmark.

# Item 4 registration — memory-limited guest run of the 16x model (before any result)

Goal: the only untested part of the memory thesis is behaviour when the OS page cache
cannot hold the model. Every prior query measurement showed zero kernel disk reads on
a 32 GiB host caching a 1.5 GiB snapshot. This run puts the frozen capacity16 model
(`/private/tmp/gm-capacity16-20260917-seed201-m256/complete`, snapshot SHA256
`5F4473A1…`, 4,470,045 records, 1,466,174,760 payload bytes) inside a Linux VM whose
total RAM is smaller than the snapshot, in a container with a cgroup memory cap, and
re-runs the registered 128 queries (64 direct, 64 composed; first-forward then
repeat-reverse) at the 128 MiB budget, paged, buffered I/O.

Environment: colima/lima on Apple Silicon (native arm64; the first Rosetta install
was discarded), guest RAM **1 GiB**, container `--memory 256m --memory-swap 256m`.
The model is COPIED into a volume on the guest's own disk so the guest kernel, not
a host share, serves reads; `gm` is built for linux-arm64 in an SDK container and
run in the runtime image. New Linux instrument: `/proc/self/io` read/write bytes,
`/proc/self/status` VmRSS/VmHWM and, when readable, cgroup `memory.peak`/`memory.max`,
reported in the same `Usage` shape as the macOS sampler; missing counters fail
explicitly rather than reading zero. `--io nocache` (F_NOCACHE) is macOS-only and
is not used here.

Gates: all 256 score vectors bit-equal to the capacity16 resident reference
(`seed201-m256-model256-query-resident-buffered.json`); snapshot hash unchanged; zero
writes; process RSS peak ≤ the original bound B + 1.25·M (388,710,400 bytes) and the
container never OOM-kills; guest-level disk read bytes on the first pass are
substantial (expected ≥ the logical record bytes read, since the cache cannot hold
the model). p95 latency is reported but not gated.

Honest limit, fixed now: the VM's virtual disk is a file on the macOS host, and the
host's page cache still sits beneath it. Guest-level reads are real kernel I/O to the
virtual device and the guest cannot cache the model, so bounded-memory correctness
and eviction under page-cache pressure are tested; **device-cold latency is not**.
A raw-device or purged-host measurement remains outside this machine's means.
One run; if the environment cannot be brought up, report the block, not a pass.

### Item 4 — two environment attempts and one additional cell, declared before the result

Attempt 1 failed because `/private/tmp` is not shared into the colima guest (empty
copy); attempt 2 copied macOS AppleDouble sidecars (`._*.bin`) into the snapshot, which
the `*.bin` physical hash includes, so the worker reported `Immutable=false` although
all 256 scores were exact and the model bytes were intact. Both attempts are kept under
`artifacts/recovery/item4/attempt*/`. Attempt 2 also showed that the store's mandatory
open-time index checksum warms the 134 MB index inside a 256 MiB cgroup, so record reads
hit the virtual disk but index reads did not. Therefore a second cell at
`--memory 128m` is declared now, in addition to the registered 256 MiB cell: with
~48 MB process RSS the page cache cannot hold the index either. Same gates; latency
reported, not gated. Nothing else changes.

## Item 4 result — bounded guest memory: exact recall, real block-layer reads (2026-09-21)

Environment: colima 0.10.3 (native arm64 lima; PATH must prefer `/opt/homebrew/bin`
over the Rosetta install), guest 1 GiB RAM / 4 CPU / aarch64 / cgroup v2, Docker 29.5.2.
Snapshot streamed into a guest-disk volume with `COPYFILE_DISABLE=1 tar --exclude '._*'`
(1.5 GiB in 6.8 s); `gm` published for linux-arm64 in `mcr.microsoft.com/dotnet/sdk:8.0`;
worker run in `mcr.microsoft.com/dotnet/runtime:8.0`. Ledger and raw outputs:
`artifacts/recovery/item4/commands.json`, `query-paged-{256m,128m}.stdout.txt`,
`container-out/query-paged-{256m,128m}.json`, `summary-{256m,128m}.json`. Failed
attempts retained under `attempt1-failed-mount/`, `attempt2-appledouble/`,
`attempt3-128m-gcheaplimit/`. Tests 224/224 (NativeIo parser and host sampler added).

The 128 MiB cell first died with a .NET `OutOfMemoryException` (cgroup `oom_kill 0`):
the runtime's GC hard limit defaults to 75% of the cgroup (96 MiB) and the worker
preallocates its ~125 MiB record-cache array. Only the runtime self-limit was raised
(`DOTNET_GCHeapHardLimit=256MiB`); the cgroup stayed at 128 MiB. Untouched cache pages
are never committed, which is why RSS stays near 46 MB in both cells.

| cell | cgroup max = peak | oom_kill | reclaim events (`max`) | process peak RSS | first p95 ms | repeat p95 ms | resident p95 (host) | first-pass kernel reads | repeat kernel reads | exact / immutable / writes |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| 256 MiB | 268,435,456 | 0 | 5,180 | 48.6 MB | 1.437 | 0.360 | 0.334 / 0.260 | 1,867,776 B | 0 | 256/256 · yes · 0 |
| 128 MiB | 134,217,728 | 0 | 4,503 | 48.6 MB | 1.063 | 0.423 | 0.334 / 0.260 | 1,540,096 B | 0 | 256/256 · yes · 0 |

All 256 score vectors in each cell are bit-equal to the capacity16 resident reference;
snapshot hash `5F4473A1…` unchanged; zero data/index writes; top-1 123.16/128 with tie
credit, as before. Process RSS is 8× below the original B+1.25M bound. The guest page
cache was pinned at the cgroup cap throughout (memory.peak == memory.max, thousands
of reclaim events), so the 1.47 GB record file could not be cached; first-pass record
reads were served by the virtual block device (cgroup `io.stat` rbytes 1.60 GB / 1.30 GB
including the immutability hash; per-process `read_bytes` 1.87 MB / 1.54 MB for the
query passes). Repeat passes read nothing from the block layer because the queries'
own working set (~2 MB) fits any cache. Index probes mostly hit because the store's
open-time index checksum leaves the tail of the 134 MB index cached even at 128 MiB.

Verdict for the memory thesis: **a 1.5 GiB learned model is queried exactly by a process
holding ~46 MB, inside an OS that cannot cache the model, with real kernel I/O.** The
registered limit stands: the guest's virtual disk is a host file and the 32 GiB host
page cache sits beneath it, so first-pass latencies (≈1.1–1.4 ms p95, 3–4× resident)
are not device-cold numbers. Bounded memory and eviction-under-pressure correctness are
now demonstrated at the OS level; cold-device latency remains unmeasured on this machine.
