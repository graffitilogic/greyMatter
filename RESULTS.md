# greyMatter — What Was Actually Learned

*Terminal results document, 2026-08-10. Not a status doc — `REFOCUS.md` remains
the single live-state file (CLAUDE.md ground rule 1). This exists because two of
the thesis's three legs now have honest negative results, and those deserve a
clear statement rather than another queue item.*

---

## The question

Like *No Man's Sky* rendering a limitless universe on commodity hardware, build a
large virtual cortex from three ideas:

1. **Procedural generation** — reconstruct neural structure from a seed instead
   of storing it.
2. **Scoped activation** — only compute within a bounded activation distance.
3. **Limited persistence** — store only what cannot be regenerated.

Falsifiable form: *can a cortical region that is evicted and procedurally
regenerated match a fully-persisted region on recall fidelity, at a fraction of
the storage?*

## Verdict

| leg | status | one line |
|---|---|---|
| **Procedural generation** | **Negative, with a caveat worth keeping** | Regeneration reproduces assemblies faithfully, but what it reproduces is a prototype and carries no lexical identity |
| **Limited persistence** | **Positive but modest** | 2.7–5.1× measured, honest, well below early claims |
| **Scoped activation** | **Untested** | Never reached; depends on a synaptic graph that does not yet hold structure |

The headline result this project carried for months — *95.5% fidelity at
64 B/neuron, AUC ≈ 0.99* — is **retracted**. Details below.

---

## Leg 1 — Procedural generation of receptive fields

### What works

Neuron receptive fields are regenerated at load time from `(VqCode, identity)`
via a deterministic hash, with only learned *deviations* from the generated
baseline persisted. This machinery functions. When a run is measurable, fidelity
of the regenerated assembly against the pre-persistence original is
**95.5–97.2%**, at **72–160 bytes/neuron**, with 86–99% of weights regenerated
rather than stored.

That is a real engineering result: the procedural path reconstructs the assembly.

### Why it does not support the thesis

Recall is measured by `MatchQuality`, a cosine similarity against the receptive
field. Because the field is *generated from the VQ prototype*, **any** input
quantizing to code C matches code C's neurons — trained or not.

Consequence, measured directly: novel pseudo-words (`blorp`, `flendish`,
`thrumble`, `grastic`) that land in a trained region score **0.55–0.64**, while
weakly-trained real words score **0.55–0.67**. The distributions overlap in
exactly the region that matters.

The harness enforces a control gate (ground rule 8): if a control outscores a
trained cue, no fidelity number is reportable. Across **5 sweeps × 8 storage
budgets = 40 runs**:

- **6 runs reportable (15%)**
- passes scattered randomly across budgets — per-threshold pass rates 0/5 to 2/5
- **d′ = 1.76–2.01 across all 40 runs**, flat over a **400× range of storage**

Discrimination does not depend on the persistence budget at all. Storing more
individuated weights does not separate trained words from novel ones. (This is
the third independent restatement of the same finding; an earlier experiment
showed storing *zero* learned weights performed as well as storing all of them.)

### The retraction, precisely

**AUC = 1.000 is definitionally identical to passing the control gate.** AUC
1.000 means every trained cue outranks every control, which *is* the condition
`strongest control < weakest trained`. All 6 passing runs scored exactly 1.000;
all 34 failures scored below it.

Therefore **AUC 0.990 means the gate failed** — across ~104 trained×control
pairs it is one inverted pair. The banked *95.5% / AUC 0.990* headline was a
failing run. It was never valid under the project's own binding rule.

The deeper error was quoting a metric that could not fail: AUC measures
*average-case ranking* and is fully compatible with a control beating a trained
word. Separation and ranking were being treated as the same thing.

### Honest statement

> Procedurally regenerated receptive fields reproduce activation faithfully but
> are not lexically discriminative. The thesis asked whether a regenerated region
> can match a persisted one on *recall fidelity*. It reproduces the pattern; the
> pattern is not specific enough for the match to constitute recall. 85% of runs
> cannot report a fidelity number at all.

What discrimination exists is **regional, not lexical** — some controls are
silent only because their VQ code maps to no cluster at all.

---

## Leg 2 — The synaptic graph and sequence structure

The graph was meant to carry learned structure that the prototype cannot: which
words follow which. Sequence-level STDP potentiates `previous → current` and
depresses the reverse.

### Result: no order signal, and the graph degrades with scale

With a corrected estimator (pooled within-cue ranks, both arms scored over every
corpus successor, unreached = 0, base-rate-corrected association, 5 repeats):

```
R_PMI real −0.0657 [−0.0741..−0.0611]   shuffled −0.0962 [−0.1002..−0.0902]
PMI_GAP +0.0305        VERDICT: NO SIGNAL
```

Real correlation is negative. A positive gap over a negative correlation means
only that the shuffled control is worse.

More seriously, **the graph gets worse with more data**:

| corpus | true successors reached |
|---|---|
| 500 sentences | 199 / 822 (24.2%) |
| 20,000 sentences | 77 / 13,887 (0.6%) |

40× more data, and the **absolute** count fell. At 20,000 sentences the graph
reaches almost no true successor, so no order statistic computed on it can mean
anything in either direction.

### Cause: capacity, not policy

Per-neuron out-degree is capped at 64. Blocking creation at the cap is
first-come-first-served, so early partners hold slots permanently. Replacing
blocking with **synaptic competition** (displace the weakest existing synapse if
the candidate is stronger) worked exactly as designed — `displaced=5,825,016`,
creations blocked down 39% — and **did not help**: `reached` still collapsed to
0.6%. Every slot was overwritten ~8.6×. Blocking froze the graph; competition
made it never settle.

The arithmetic says why no eviction policy could have worked:

- ~37M creation attempts (155,402 learn events × up to 240 pairs)
- ~3.8M slots (59,000 neurons × 64)
- **≈10× oversubscribed**

And the flood's source: `passed = 9,309,604 (99.5%)`. The Hebbian co-activation
threshold is a fixed 0.12 against a mean match of 0.53, so it admits essentially
everything. All real selection happens downstream, in a top-16 cut. **The gate is
not a gate.**

---

## Leg 3 — Scoped activation

Never tested. It was gated behind a synaptic graph that demonstrably does not yet
carry structure, and testing "recall within distance *d*" over an unstructured
graph would measure nothing.

---

## What is true and measured

Not everything here is negative. These hold:

- **Procedural regeneration reconstructs assemblies** — 95.5–97.2% fidelity when
  measurable, 86–99% of weights regenerated rather than stored.
- **Compression is real but modest** — 2.7–5.1×, honest and reproducible. (Any
  older "90% compression" or "trillion-parameter" claim is false and retracted.)
  **Caveat found 2026-08-10:** `bytes_per_neuron` did not move when a genuinely
  persisted 4-byte field was added, so it is a formula with a hardcoded
  identity/meta constant rather than a measurement of what reaches disk. These
  figures are estimates and they under-report.
- **Allocation and assembly reuse work.** After fixing a chain of defects, neuron
  creation fell from **119.5 to 7.4 per sentence**, with assembly reuse at 100%
  from the first window on a resumed brain. Concept identity now survives a
  save/load round trip.
- **The storage/eviction machinery is sound** — partitioned MessagePack storage,
  LRU cluster eviction, and inline synaptic decay all behave as specified.

---

## The recurring architectural pattern

Both negative results have the same shape:

| failure | mechanism |
|---|---|
| Cannot separate trained words from novel ones in the same VQ region | Fixed cosine match against a prototype field |
| Synapse creation floods a 10× oversubscribed budget | Fixed co-activation threshold 0.12 vs mean match 0.53 |

**Everything thresholds against fixed global constants. Nothing adapts to its own
history.** Two independent subsystems failed for the same structural reason. That
convergence is the most useful thing the negative results produced.

---

## Why this was forced, not unlucky

One argument explains every negative result above, and it is arithmetic rather
than measurement.

The VQ codebook holds 512 codes at 67% utilisation — **~343 effective codes** —
against a corpus vocabulary in the low thousands. By pigeonhole, many words
necessarily share a code. The receptive field is *generated from that code*.
So **lexical identity is destroyed at quantisation time, before any learning
happens.** No amount of learned weight adjustment recovers it, because the
regeneration path never sees the word — only the code.

This is the tension the *No Man's Sky* analogy conceals. In that game the seed
**is** the identity: no two planets share a seed, so regenerating from the seed
loses nothing. Here the seed is shared across many words, so regeneration cannot
recover which word it was. Enlarging the codebook until codes ≈ vocabulary would
restore identity — and would make the code an index per word, i.e. storing the
word, at which point the compression is gone.

> **Procedural generation and per-item identity are in direct tension whenever
> the seed space is smaller than the item space.** Compression comes precisely
> from collisions, and collisions are precisely what destroys identity.

This is a structural property of the approach, not a defect in this
implementation. Any system regenerating item-specific structure from a shared
seed will hit it.

### ⚠️ Narrowed 2026-08-10 by direct measurement (`--encoder-ceiling`)

The statement above is correct but was applied too broadly. Measurement locates
the loss at one specific step rather than in the approach:

| stage | identity |
|---|---|
| `FeatureEncoder` → 128-dim vector | **preserved** |
| top-32 dims, the actual training input | **preserved — 0 collisions in 1,355 words** |
| VQ quantise → 1 of ~343 codes | **destroyed — ≈3.9 words per code** |

The encoder is not the bottleneck; it separates the entire vocabulary without
error. Identity is discarded at quantisation, by a *hard single-index* seed.
A composed or sparse seed of the same size does not have that property.

Two further corrections fall out:

- **The architecture was doing real work.** Raw encoder distance separates
  trained cues from controls at `AUC 0.455` — chance. The measured 0.94–1.00
  therefore comes from learning, not from the input.
- **Rule 8 was achievable, not unfair.** Losslessly stored, a trained cue matches
  at 1.000 against a strongest control of 0.741 — a margin of **+0.259**. The
  measured system runs at **−0.027**. Procedural regeneration consumed the whole
  margin. Any successor scheme must keep trained matches above **0.741**.

So the negative result stands for **this** implementation — hard VQ seeds cannot
carry lexical identity — and the general claim about procedural generation is
withdrawn to: *a lossy single-index seed destroys identity; whether a composed
injective seed preserves it is untested.*

## The one question that was open — now closed

*(Resolved 2026-08-10, W6. Retained because the reasoning is the useful part.)*

A naive fix — homeostatic thresholds that punish broadly-responsive neurons —
has a **reachable null that kills it**: the two populations needing separation
(weakly-trained words at 0.55–0.67, untrained words in a trained region at
0.55–0.64) occupy the same match range. Raising thresholds silences both, trading
false positives for false negatives while d′ stays put.

The signal that *is* discriminative is **activation history**. A neuron that
fired 400 times for its word is in a different state from one matching a novel
input for the first time — and the prototype field cannot represent that
difference, by construction.

`ActivationCount` is an `int`: **4 bytes, already persisted, already round-tripped
through regeneration, and never read by recall.**

That makes a clean, thesis-aligned question:

> **Can a few bytes of per-neuron activation history restore the lexical identity
> that a prototype-generated field provably cannot carry?**

- **If yes**, the thesis survives in modified form: procedural generation
  reconstructs *structure*, while a small persisted trace supplies *identity* —
  and the storage claim survives with it, since the trace is already inside the
  64 B floor.
- **If no**, the negative result is complete: procedural regeneration cannot
  support concept-specific recall, and the limitation is fundamental rather than
  a tuning problem.

Both outcomes are informative, and the null is reachable. That is the bar this
project's later experiments were held to, and the earlier ones were not.

### Answer: no. Measured, W6, 2026-08-10

`MeanFiringMatch` — a running mean of the match values that actually made each
neuron fire — was implemented, persisted, and round-tripped through regeneration.

```
FAMILIARITY: penalty trained=0.0046  control=0.0101  gap=+0.0055
d′ 1.75–2.09 (baseline 1.76–2.01)   gate 7/40 (baseline 6/40)
```

The mechanism worked **in the predicted direction** — controls were penalised
2.2× more than trained cues — and was **quantitatively irrelevant**: ~0.005
against a trained/control gap of 0.436, about 1% of the deciding scale. Learned
drift is real and negligible, consistent with the earlier finding that storing
zero learned weights performed as well as storing all of them.

No tuning fixes a 1% effect on a 44% gap. Combined with the pigeonhole argument
above, this closes the question: the identity a prototype cannot carry was
already destroyed at quantisation, and a few bytes of per-neuron history cannot
reconstruct what the seed never encoded.

**The negative result is complete.**

---

## How the measurements failed, and what it cost

The most transferable content here is methodological. Every rule in `CLAUDE.md`
was earned by a specific documented failure:

- **A pre-registered null is not enough — check it is *reachable*.** The first
  cascade experiment tested whether activation flows forward more than backward.
  Backward cross-concept edges were *structurally impossible* — the depression
  routine never creates a synapse — so the measured 0.98 forward share was a
  tautology. The test could not fail.
- **Perfect scores are alarming.** Every 100% this project produced was a broken
  measurement: a probe ignoring its input, two arms reading the same files,
  recall never touching the regenerated part.
- **No verdict from n=1 on a correlation-valued metric.** A single run reported
  "LEARNED ORDER" at +0.25; five repeats showed the null spanning nearly a full
  unit. It was one draw.
- **Instrument the decision, not the aggregate.** Four consecutive wrong theories
  were reasoned from summary metrics. One targeted counter on the actual decision
  settled it immediately.
- **A harness must be inert with respect to its subject.** Early fidelity runs
  silently folded their own training data into the brain they were measuring.
- **Controls gate validity, without a soft tier.** A "provisional" pass state
  crept in and let runs report numbers while controls beat trained words. That
  soft tier is how a control gets weakened without anyone deciding to weaken it.
- **A correct fix can expose a latent bug.** Replacing substring matching with
  exact matching was right — and it broke concept lookup, because substring
  matching had been accidentally masking a tag-joining defect for months.

Three of these produced retractions of results that had already been believed.

---

## Reproducing

```bash
cd greyMatter && dotnet build -c Release

# Synapse formation sanity
dotnet run -c Release -- --test-hebbian

# Fidelity + control gate (isolated scratch brain; aborts unless controls separate)
dotnet run -c Release -- --fidelity-test --train 500

# Storage sweep — run 5× and count reportable rows
./greyMatter/scripts/sweep_fidelity.sh

# Order / association, with shuffled-order null
dotnet run -c Release -- --cascade-stats --train 20000 --repeats 5 --cross-word off
```

All experiments default to a throwaway scratch brain and never touch
`/Volumes/jarvis/brainData`.
