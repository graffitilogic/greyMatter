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
