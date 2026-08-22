# greyMatter

Neurobiologically-derived experiments in machine learning: a virtual neuron space far larger than
RAM, in which a cue materialises only the neurons and synapses inside its activation scope,
regenerates them procedurally from compact recipes, learns locally, persists only what learning
moved, and evicts.

The premise is that the gap between artificial and biological networks is more algorithmic than it
is a resource problem — so the interesting question is not "how many parameters" but "how little
needs to be real at any instant". Game-engine ideas do the work that biology does differently:
procedural generation, render-distance lazy loading, vector quantisation.

## Where to look

| | |
|---|---|
| **`Prompt.md`** | The specification. Short, and the authority the plan answers to. |
| **`plan.md`** | The implementation plan and its rules of engagement. Phase definitions, component specs, and the evaluation protocol. Start here. |
| **`RESULTS.md`** | Append-only findings log. Every number carries the command line that produced it. **Start at the P10.2 VERDICT at the end** — it summarises everything for a reader who will not read the rest. |
| **`src/GreyMatter.Poc/`** | The proof-of-concept. Deterministic, allocation-free in the hot path, data-oriented for an eventual CUDA port. |

## Running it

```bash
dotnet build src/GreyMatter.Poc/Poc.csproj -c Release
```

One binary, no shell scripts:

```bash
gm learn  --dataset tatoeba --sentences 50000 [--resume]
gm probe  --cue <word> [--topk 16]
gm eval   encoder-ceiling | recall | order | scale | assoc
          attribution | connectivity | shift
gm bench  substrate | store
gm stats
gm audit  --strings
gm config
```

Every parameter in `Config` is a `--kebab-case` flag, so any experiment's full configuration is
visible on the command line that produced it. Training data streams from `/Volumes/jarvis/trainData`
(`--local-sample` falls back to a built-in corpus).

## What works

The substrate thesis holds, and holds hard:

- **10⁷ virtual neurons served by a pool of 260.** Recall is flat to three decimal places across a
  385× reduction in working set — learned state lives in recipes, so RAM is a cache, not the store.
- **50,000 sentences unattended** in 29.8 minutes, working set pinned at its cap with zero
  truncations, checkpoint/resume within 1.25%.
- **Nothing readable on disk.** `gm audit --strings` is clean across 498 partitions and 340 MB of
  payload, verified by a structural MessagePack walk plus a corpus-vocabulary check — and by a test
  that plants a byte-packed word list and asserts the audit catches it.
- **Deterministic.** Same seed, bit-identical state; verified across an uncontrolled OS suspend
  mid-run.
- **Instruments that refuse.** The harness declines verdicts it cannot support — too few repeats,
  insufficient bigram support — because the project's predecessor lost months to results that did
  not survive their own controls.

## What does not

The system reliably knows *what it has seen and how often*. It does not know *what goes with what*,
and the campaign to change that has finished with a located boundary rather than a fix.

- **Frequency: complete.** ρ(mass, corpus frequency) → 1.00.
- **Order: real but weak.** Base-rate-corrected weights give `R_PMI` +0.18 against a null of +0.04,
  non-overlapping — short of the +0.15 `PMI_GAP` bar fixed before the experiment ran. The bar was
  not moved.
- **Pairwise association: absent.** `ASSOC_AUC` never exceeded 0.58 under any configuration.

The constraint was chased down one layer at a time and is *not* the synaptic budget (fixed at P7.1),
competition (P7.2), the learning rule (P9.1 — normalisation demonstrably carries the order signal),
or the k-WTA readout (P9.2R). It is **coverage**: ~84% of the pairs a cue should relate to have no
edge at all, and the coverage that exists is not preferentially related (16% of related pairs
non-zero against 16% of unrelated). Association is a population-mean effect — related pairs carry
3.4× the edge mass of unrelated ones — that does not survive to any per-pair readout.

Coverage is a property of hash-disjoint assemblies: **representation, not learning.** That is where
a v2 would start. See `RESULTS.md` § P10.2 VERDICT.

## House rules

Three that shape everything else, all in `plan.md` §0 and §6.1:

1. **Honest nulls are deliverables.** A refused verdict is a result.
2. **Thresholds are fixed before the experiment runs**, and are not adjusted to meet an outcome.
3. **Every claim needs its own null-controlled measurement.** Four published conclusions in this
   project were later corrected by direct measurement, every one of them inferred from a plausible
   pattern rather than isolated: a "lottery" that was specific-but-sparse, a 418× ratio that was
   12×, a sparsification effect that was normalisation, and two instruments "contradicting" each
   other that turned out to be right about different quantities. Nulls now print their own sample
   composition so the next one is visible where it is produced.
