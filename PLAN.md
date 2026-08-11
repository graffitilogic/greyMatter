# PLAN — BNN fundamentals and their ANN counterparts

*Planning document, started 2026-08-10. Live and editable, unlike `RESULTS.md`
(terminal) and `REFOCUS.md` (experiment status). Purpose: get the conceptual map
right **before** proposing architecture, because the last cycle spent months
discovering by experiment something a page of arithmetic could have shown.*

**How to use this:** work through it section by section. Nothing here is a
commitment to build anything. Verdicts on Bill's starting list are marked
✅ correct / ⚠️ needs nuance / ❌ wrong, with the correction stated plainly.

---

## 0. Scope note — the part we're deferring, and the risk in deferring it

> *"Feature encodes through sensory cortices. I'm not sure I care to explore this
> at front. IO is fundamental and I'm not sure we need a biologically inspired
> ANN component to mimic it."*

**Reasonable to defer, but flagged loudly: this is exactly where greyMatter
died.** The VQ codebook *is* the sensory encoder, and the pigeonhole failure
(343 effective codes vs thousands of words) happened entirely inside it. Nothing
downstream could recover what the encoder discarded.

The encoder is not neutral plumbing. It determines what is *representable* at
all. Defer the biological realism, but treat the encoder's capacity as a
first-class design parameter, computed before anything is built on top of it.

**Standing rule for this round:** for any proposed representation, compute
capacity vs item count *first*.

---

## 1. Scale and architecture

> *BNN: massively scaled structurally through ultra-sparse activation, dynamic
> recurrence, local and time-based encoding. Billions of neurons with equally
> massive scale in synaptic connections.*

**✅ Correct.** Numbers for the record:

| | biological | large transformer |
|---|---|---|
| units | ~86B neurons (~16B cortical) | ~10⁶–10⁷ hidden units |
| connections | ~10¹⁴–10¹⁵ synapses | ~10¹¹–10¹² parameters |
| fan-out | ~1k–10k synapses/neuron (pyramidal up to ~30k) | ~10⁴–10⁵ params/unit |
| power | ~20 W | ~10⁶ W (training cluster) |

> *ANN: Scales horizontally — trillions of parameters, only thousands of neurons.*

**❌ The neuron count is wrong, and the error matters.** A GPT-3-scale model has
roughly **5 million** FFN hidden units (96 layers × ~49k units), not thousands.
The right mapping is **parameters ≈ synapses**, **hidden units ≈ neurons**.

So the real comparison is:

- **Connections:** brain ~10¹⁴, big model ~10¹² → brain leads by ~100×.
- **Units:** brain ~10¹¹, big model ~10⁷ → brain leads by ~10,000×.
- **Density per unit:** roughly *comparable*, even slightly favouring the model.

**The gap is in neuron count, not connection density.** That reframes the whole
"massive scale" intuition: transformers are not sparse-and-wide like cortex,
they are few-and-extremely-densely-connected. Which is the actual architectural
difference worth attacking.

Also: transformers scale in depth *and* width, not merely "horizontally."

### ⚠️ Missing from the list: dendritic computation

A cortical pyramidal neuron is **not** one ANN unit. Its dendritic branches
perform independent nonlinear integration — the neuron is closer to a small
multi-layer network. Work emulating a single cortical neuron with a deep
temporal CNN needed roughly 5–8 layers to match its input/output behaviour.

If a biological neuron ≈ a small network, the effective biological unit count is
higher still, and "billions of neurons" understates rather than overstates.

### ⚠️ Missing from the list: neuromodulation

Dopamine, acetylcholine, norepinephrine act as **global scalar signals** gating
plasticity — effectively a dynamic, context-dependent learning rate and a
"this mattered" tag. There is no clean ANN counterpart in standard training;
the nearest relatives are learning-rate schedules (crude, not context-sensitive)
and RL reward signals (closer in spirit).

This matters for greyMatter specifically: every threshold in it was a fixed
global constant, and that was the diagnosed root cause of both failures.
Neuromodulation is the biological answer to exactly that problem.

---

## 2. Sparsity

> *BNN: the 1% activation rule, sparse encoding. ANN: SAEs.*

**⚠️ The biology is roughly right; the ANN counterpart is wrong.**

**Biology:** sparse, yes, but "1%" is a metabolic estimate rather than a measured
constant. Cortical activity estimates range ~0.5–5% of neurons active at any
moment; some structures (hippocampal dentate gyrus) go well below 1%. The
metabolic argument is that ATP supply cannot support more than ~1% simultaneous
activity. Treat it as an order of magnitude, not a law.

**❌ SAEs are not the architectural counterpart.** A sparse autoencoder is an
*interpretability tool* — trained after the fact on a dense network's activations
to discover sparse features. It does not make the network sparse; it reveals that
a dense network's representation can be *decomposed* sparsely.

The genuine architectural counterparts to biological sparsity:

- **k-winner-take-all layers** — explicit top-k activation (closest match; this
  is what greyMatter's `MaxCoactivationGroup` top-16 cut already is)
- **Sparse MoE routing** — but coarse-grained: top-1 or top-2 of N experts, so
  ~10–25% active, not ~1%, and the unit of sparsity is a whole FFN block
- **ReLU activation sparsity** — emergent, roughly 50–90% zeros, not designed
- **HTM / Numenta-style sparse distributed representations** — designed for ~2%

**But SAEs point at something more important than sparsity**, covered in §5.

---

## 3. Spiking and temporal coding

> *BNN: spiking, temporal encoding. ANN: SNNs.*

**✅ Correct pairing.** Caveats worth holding:

- SNNs have **not** delivered competitive accuracy at scale. Their real advantage
  is energy efficiency on neuromorphic hardware (Loihi, SpiNNaker, TrueNorth).
- Training is hard because spikes are non-differentiable; surrogate-gradient
  methods are the standard workaround, and they largely amount to training an
  ANN and converting.
- **Open question:** is spike timing *computational* or *metabolic*? Spikes may
  be how biology sends signals cheaply over distance, with the computation living
  in rates and populations. If so, mimicking spikes is copying a transmission
  medium rather than an algorithm.

**greyMatter precedent:** there is no time axis, so sequence-level STDP (word
order as the temporal dimension) was a legitimate adaptation rather than a
compromise. It failed for capacity reasons, not for lack of real spike timing.

---

## 4. Feedback, prediction, and credit assignment

> *BNN: local feedback loops, higher cortical layers send predictions, lower
> respond with deltas. ANN: backpropagation.*

**⚠️ The biology is right — that is predictive coding — but the pairing conflates
two different things.**

Backprop is a **credit-assignment algorithm**. Predictive coding is a
**mechanism** which happens to compute approximately the same gradients using
only local signals. They are not opposites; predictive-coding networks can be
shown to approximate backprop under certain conditions.

Backprop's biological implausibility is specific:

1. **Weight transport** — requires the backward pass to use exactly the forward
   weights, transposed. No known mechanism does this.
2. **Global error signal** — needs an error computed at the output and shipped
   backward through the whole network.
3. **Non-local credit** — a synapse's update depends on quantities it has no
   access to.

Biologically motivated alternatives worth knowing: **feedback alignment**
(random fixed backward weights work surprisingly well, killing objection 1),
**target propagation**, **equilibrium propagation**, and **forward-forward**
(two forward passes, no backward pass at all).

**And the correspondence you're missing is the strongest one on this page:**
predictive coding at the *behavioural* level is **next-token prediction**. An LLM
is trained on prediction error against sensory input. That is the same objective
the cortex is theorised to run — implemented with a biologically implausible
optimiser, but the *learning signal itself* is the biological one.

So: the objective already matches. The mechanism does not.

---

## 5. Distributed representation — the important one

> *BNN: holographic concept storage. ANN: HDC.*

**⚠️ Good instinct, wrong word, and the correction is the most useful thing here.**

"Holographic" as a literal brain theory (Pribram's holonomic model) is largely
out of favour. What *is* supported:

- **Distributed population coding** — concepts encoded across many neurons, with
  graceful degradation. ✅ Real.
- **But also strikingly sparse, selective cells** — "concept cells" in medial
  temporal lobe responding to one individual across photographs, text, and
  spoken name. That is nearly the *opposite* of holographic.

Biology appears to use **sparse distributed coding**: not one neuron per concept,
not all neurons per concept, but a small subset — which is precisely the
assembly idea greyMatter was built on.

**HDC/VSA is a legitimate counterpart** and your instinct has real lineage —
Holographic Reduced Representations are literally named for it. High-dimensional
vectors, binding via circular convolution or XOR, superposition via addition,
graceful degradation under noise.

### The modern framing, and why it matters to us: superposition

A network can represent **more features than it has dimensions**, if features are
sparse. Features occupy nearly-orthogonal directions rather than dedicated ones,
and interference is tolerable precisely because few features are active at once.

This is what SAEs *discover* — which is the real reason they belong on this page,
just not in the sparsity row.

**This directly contradicts the conclusion I wrote in `RESULTS.md`.** See §6.

---

## 6. ⚠️ Retraction candidate: the pigeonhole argument was overstated

`RESULTS.md` currently claims:

> *"Procedural generation and per-item identity are in direct tension whenever
> the seed space is smaller than the item space."*

**That is only true for hard, single-code quantisation, which is what greyMatter
does — not for procedural generation in general.**

greyMatter assigns each item to **exactly one** of ~343 VQ codes. Capacity is
therefore 343 distinguishable items, full stop. Collisions are **total**: two
words sharing a code are perfectly indistinguishable downstream.

But a **sparse distributed code** assigns each item *k* of *n* codes. Capacity is
C(n, k):

| scheme | capacity |
|---|---|
| hard VQ, 512 codes, 1 active | 512 |
| sparse code, 512 codes, 4 active | ~2.8 × 10⁹ |
| sparse code, 512 codes, 8 active | ~10¹⁷ |

Same 512-entry codebook. Same "regenerate structure from a seed" principle.
**Capacity rises by 14 orders of magnitude**, and collisions become *partial and
graceful* rather than total — two similar items overlap in some codes and differ
in others, which is exactly what distributed representation buys.

**So the honest statement is narrower and more useful:**

> Procedural generation is in tension with per-item identity **when the seed is a
> single hard index**. It is not in tension when the seed is a sparse distributed
> code, because capacity is combinatorial rather than linear in codebook size.

Whether that rescues the thesis is an **open empirical question**, not something
this project tested. We tested one encoder and concluded about a whole class.

**This is Bill's suspicion, and I think he is right.** Action: soften the claim in
`RESULTS.md` to what was actually measured, and record the distributed-code
variant as untested rather than refuted.

---

## 6a. Bill's reframing: the recipe was single-lane, not procedural generation itself

*Added 2026-08-10, Bill. This supersedes the framing of §6 — it arrives at the
same place by a better route.*

> *"It isn't a failure of procedural generation per se but one of over-simplified
> single-lane neural state recipes… seed × timestamp × coordinates = planet,
> where the recipe picks from classes of pre-programmed properties. The model
> probably shouldn't be vanilla neuron → learning blender → persist weights.
> Maybe it should be [special types of neurons with specific start-values] →
> learning blender → persist weights → reconstituted on recall by concept vector,
> weights, constrained by spatial distance and other limiters."*

### Why this is right, and what biology calls it

**DNA is the actual existence proof for procedural generation of a brain.** The
genome is ~750 MB and specifies ~10¹⁴ synapses — a compression ratio no
architecture in this project came close to. It achieves it by specifying **cell
types and local wiring rules**, never per-synapse detail.

Cortex is not made of one kind of neuron:

- **~80% excitatory pyramidal**, ~20% inhibitory interneurons
- Interneurons split into functionally distinct classes: **PV** (fast-spiking,
  perisomatic — gain control and timing), **SST** (dendritic — gates which
  *inputs* reach the soma), **VIP** (inhibits SST — i.e. disinhibition, the
  substrate for attention and context)
- **Layer-specific roles**: L4 receives thalamic input, L2/3 does
  cortico-cortical, L5 projects subcortically, L6 feeds back to thalamus
- Transcriptomic surveys find ~100 distinct types in mouse cortex alone

greyMatter has **one** neuron type and **one** global inhibition threshold. The
biological answer to "everything thresholds against fixed global constants" is
not a better constant — it is *three different kinds of inhibition doing three
different jobs*.

### The capacity arithmetic — and why this is §6's fix in disguise

The single-lane recipe was `VqCode → prototype`. One index, ~343 values, capacity
343 items. Collisions total.

A **typed assembly** makes the *composition* the code. With `T` neuron types and
an assembly of `k` neurons, the number of distinguishable compositions is the
multiset count `C(T + k − 1, k)`:

| types T | assembly k | distinguishable compositions |
|---|---|---|
| 5 | 16 | ~15,500 |
| 10 | 16 | ~2.0 × 10⁷ |
| 20 | 16 | ~4.0 × 10⁹ |
| 20 | 32 | ~2.8 × 10¹³ |

**Twenty types and sixteen slots gives ~4 billion distinguishable assemblies** —
from a codebook far smaller than the one that failed.

This is the same mechanism as §6's sparse distributed code. There, identity lived
in *which k of n codes* were active; here it lives in *which types in what
proportion*. Both replace a linear index with a combinatorial composition. Bill's
route is better because it says *what the components should be* — cell types with
distinct intrinsic properties — rather than treating the code as an abstract
bit-pattern.

It also matches the NMS analogy properly. `seed × timestamp × coordinates` is a
**composed, injective** function: no two planets share a seed, so regenerating
from the seed loses nothing. greyMatter's `features → VqCode` was **many-to-one**.
That, precisely, is the defect — not procedural generation.

### ⚠️ The question that decides whether this works

**What assigns a neuron's type?**

- If type is derived from the **VQ code**, capacity does not increase at all. The
  collision has simply moved one level up: words sharing a code get identical type
  compositions, and we have rebuilt the same failure with more machinery. This is
  the trap, and it is easy to walk into.
- If type composition is derived from the **full pre-quantisation concept
  vector**, identity survives, because the collision never happens.

**Design rule, checkable before any code:** the recipe must be **injective over
the item set**. Whatever the tuple is — `(types, proportions, positions, concept
vector)` — it must uniquely identify the item. That is an arithmetic property,
verifiable on paper, and it is exactly what the last cycle failed to check.

### What this gives the untested leg

Typed neurons with spatial organisation finally give **scoped activation** (§8 Q3,
the never-tested third leg) something real to do. Cortical columns and topographic
maps are spatial; distance-limited activation is meaningful only when position
carries information. In the current design position is arbitrary, which is part of
why that leg was never testable.

---

## 6b. ENCODER CEILING measured, 2026-08-10 — both my predictions wrong

`dotnet run -- --encoder-ceiling --train 500`. No training, no brain.

### My hypothesis was falsified

I predicted the encoder would account for the measured separation, making
`RESULTS.md` a document about `FeatureEncoder` rather than about the thesis.

```
CEILING_AUC:    0.455        (chance)
CEILING_DPRIME: -0.03
```

**Raw encoder distance carries no information about trained vs control.** The
system's measured AUC of 0.94–1.00 is therefore *real separation that learning
produces*, not an artefact inherited from the input. The architecture is doing
work.

Why my framing was wrong: `max cosine to the trained set` tests whether trained
words form a **cluster** in encoder space. They do not — common English words
are not orthographically alike. But the system does not rely on clustering; each
trained word has a **dedicated assembly tuned to its own vector**. That is
memory, and memory beats clustering here.

### Identity survives the encoder intact — this is the important number

```
TOPK_COLLISIONS k= 4:   728/1,355 distinct (46.3% collide)
TOPK_COLLISIONS k= 8: 1,292/1,355 distinct ( 4.6% collide)
TOPK_COLLISIONS k=16: 1,352/1,355 distinct ( 0.2% collide)
TOPK_COLLISIONS k=32: 1,355/1,355 distinct ( 0.0% collide)
```

`k = 32` is exactly `ConceptFeatureDims` — the code the system *already* computes
and already feeds to training. **Zero collisions across the whole vocabulary.**

So the pigeonhole argument in `RESULTS.md` is not retracted, it is **relocated**:

| stage | identity |
|---|---|
| `FeatureEncoder` → 128-dim vector | **preserved** |
| top-32 dims (`BuildTrainingFeatures`) | **preserved — 0% collisions** |
| VQ quantise → 1 of ~343 codes | **destroyed — ≈3.9 words per code** |
| receptive field generated from that code | irrecoverable |

The information was never missing. It is discarded at a single, identifiable
step, and that step is the one the whole regeneration scheme is built on.

### The margin, and a hard design target

Section A also gives a number nothing before it did. With lossless storage a
trained cue matches itself at 1.000, and the strongest control sits at **0.741**
(`zxcvbnmasd` nearest `so`).

| | trained | strongest control | margin |
|---|---|---|---|
| raw encoder (lossless) | 1.000 | 0.741 | **+0.259** |
| measured system (VQ prototype) | 0.609 | 0.636 | **−0.027** |

**Procedural regeneration consumed the entire available margin and overshot.**
That makes the thesis question quantitative for the first time: *how much of a
0.259 identity margin can survive compression?*

And it settles a fairness question that has been open since the gate was added:
**rule 8 is achievable.** A lossless store passes it comfortably. The gate was
never demanding a distinction the input lacked.

> **Design constraint for any successor scheme:** regeneration must keep a
> trained cue's match above **0.741**. Checkable on paper, before code.

### What this makes the obvious next step

Do not generate the receptive field from the VQ code. Generate it from the
**top-32 dimension set**, which is already computed, already injective over the
vocabulary, and already the training input.

Storage arithmetic, for the capacity-first rule:

- top-32 set = 32 indices × 7 bits = **28 bytes per concept** (not per neuron)
- current assembly = 16 neurons × 68 B = **1,088 bytes**
- ≈ **39× compression, with identity preserved**

This is exactly Bill's §6a framing: a **composed, injective seed** rather than a
lossy single index. `seed × timestamp × coordinates`, not `seed`.

**Open question before building:** the top-32 set identifies a concept, but the
regenerated field must also *discriminate* — a novel word sharing 28 of 32 dims
must score measurably lower. Overlap-based scoring gives that for free
(28/32 vs 32/32), but it needs checking against the 0.741 constraint on paper
first.

---

## 7. ⚠️ Missing from the list: complementary learning systems

Biology does not use one memory system. **Hippocampus** does fast, sparse,
one-shot episodic binding; **neocortex** does slow, distributed, statistical
consolidation; replay transfers between them during rest and sleep.

The ML counterparts are partial: replay buffers in RL, and the general
observation that fast weights / slow weights schemes address the same
stability-plasticity trade-off.

**Relevant because greyMatter already has STM → LTM consolidation** — that piece
was better-motivated than it was given credit for, and it was never the thing
that failed.

---

## 8. Questions to step through together

Ordered so that the answer to each changes what the next one means.

1. **Is the target intelligence, or efficient storage/retrieval?** They pull in
   opposite directions and the last cycle never chose explicitly. Two of the
   three measured legs failed on *capacity*, which is a storage property.
2. **What assigns neuron type (§6a)?** This single question decides whether the
   typed-assembly design has real capacity or has merely relocated the
   collision. Answer it on paper before anything is built.
2a. **Does the typed/sparse composition variant deserve a real test?** Cheap to
   falsify: compose assemblies from types, re-run the fidelity control gate.
   **Arithmetic first** — how many items must be distinguishable, and what
   `T`, `k` does that need?
3. **If ANN units ≈ neurons and biology's edge is unit count, not density (§1),
   is "many tiny units" the actual architectural bet?** That is a different
   project from "compress a cortex onto a laptop."
4. **Is spiking worth adopting at all (§3)** — or is it a transmission medium we
   would be copying for its aesthetics?
5. **Which of the fixed global thresholds should become neuromodulatory (§1)?**
   This was the diagnosed root cause of both greyMatter failures and it is the
   cheapest biologically-motivated change available.
6. **What is the smallest experiment that could falsify the whole next
   direction?** Ask it before writing code this time.

---

## 9. Deliberately not decided yet

- Whether to continue greyMatter, fork it, or start clean.
- Whether the deliverable is research, a working system, or a written result.
- Any architecture. Nothing above commits to a build.
