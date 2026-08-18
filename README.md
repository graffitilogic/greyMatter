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
| **`plan.md`** | The implementation plan and its rules of engagement. Phase definitions, component specs, and the evaluation protocol. Start here. |
| **`RESULTS.md`** | Append-only findings log. Every number carries the command line that produced it. This is the real state of the project. |
| **`src/GreyMatter.Poc/`** | The proof-of-concept. Deterministic, allocation-free in the hot path, data-oriented for an eventual CUDA port. |
| **`greyMatter/`** | Legacy tree. Read-only reference quarry — components were ported out of it, never added to it. |

## Running it

```bash
dotnet build src/GreyMatter.Poc/Poc.csproj -c Release
```

One binary, no shell scripts:

```bash
gm learn  --dataset tatoeba --sentences 50000 [--resume]
gm probe  --cue <word> [--topk 16]
gm eval   encoder-ceiling | recall | order | scale | attribution | connectivity | shift
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

## What does not, yet

The system reliably knows *what it has seen and how often*. Teaching it *what goes with what* is the
open problem, and the current work is closing it.

Measured, in order: the synaptic budget was not the constraint (cross-population share 0% → 74.6%),
nor competition (displacement 0.000% → 8.472%), nor the substrate (co-occurring words are 60%
connected against a 15% null, with 418× the edge mass). The constraint is the **learning rule**:
Hebbian coactivation accumulates weight in proportion to co-occurrence count, with no term for the
target's base rate, so a frequent word wins every ranking simply by appearing more often.

Subtracting that base rate moves the order correlation from **−0.06 to +0.18** against a shuffled
null near zero, non-overlapping across five repeats — the first positive association result in the
project. It does not yet clear the bar fixed before the experiment ran, and the bar has not been
moved. See `RESULTS.md` § P8c.

## House rules

Three that shape everything else, all in `plan.md` §0 and §6.1:

1. **Honest nulls are deliverables.** A refused verdict is a result.
2. **Thresholds are fixed before the experiment runs**, and are not adjusted to meet an outcome.
3. **Every claim needs its own null-controlled measurement** — twice in this project a diagnosis
   inferred from a handful of examples turned out to be wrong when measured against a population.
