# greyMatter

A C# experiment in learning and recall using sparse activation, procedural neuron
identities, compact learned state, and connection-driven loading and eviction.

**Status — 2026-09-16:** packed storage preserves exact learning, saved-policy
restart and recall. The same 8,192-chain model now occupies **120.7 MiB instead of
3.19 GiB** on disk—a 96.3% reduction—with all 128 supported recall queries correct.
**203 tests pass.** Warm-cache query p95 is effectively unchanged, but index lookup
increases logical read traffic. R4 remains stopped; larger-than-memory capacity
and real-data utility are unproven.

| Read | Purpose |
|---|---|
| [Prompt.md](Prompt.md) | Original purpose and constraints |
| [plan.md](plan.md) | Active agent guide and current phase |
| [RESULTS.md](RESULTS.md) | Evidence log; start at the R4 development checkpoint near the end |
| [A1 summary](artifacts/recovery/a1/summary.json) | Final synthetic metrics |
| [R2 summary](artifacts/recovery/r2/summary.json) | Storage and restart correctness |
| [R3 summary](artifacts/recovery/r3/summary.json) | Exact paged recall and traversal trace |
| [R4 checkpoint](artifacts/recovery/r4/summary.json) | Development measurements and runtime blocker |
| [R4 deferred correction](artifacts/recovery/r4-deferred/summary.json) | Exactness checks and first scale-cell stop |
| [Retention diagnostic](artifacts/recovery/retention/summary.json) | Forgetting trace and unchanged-encoding recall comparison |
| [Source-local retention](artifacts/recovery/retention-policy/summary.json) | Five-seed recall, replacement and branching evaluation |
| [Persisted-policy integration](artifacts/recovery/policy-integration/summary.json) | Saved policy, separate-process continuation and larger recall test |
| [Packed storage](artifacts/recovery/packed/summary.json) | Exactness, disk allocation and paired lookup costs |
| `src/GreyMatter.Poc/` | Implementation |
| `tests/GreyMatter.Poc.Tests/` | Correctness and regression tests |

## What works

The experimental AssemblyRelay uses eight deterministic neurons as an assembly's common
entry/exit cohort. Observed adjacent training cues learn numerical synaptic weights
between these cohorts. Relay connection slots are isolated from within-cue reinforcement.
Each tick distributes source activation by relative outgoing weight into a fresh next
activation set. Runtime receives codes, never the evaluator's answer labels.

On five fresh seeds, each with 128 direct and 128 composed retrieval queries:

| arm | direct top-1 | composed top-1 |
|---|---:|---:|
| Learned protected relay | 100.0% | 100.0% |
| Untrained | 3.1% | 3.1% |
| Shuffled training | 4.1% | 3.1% |
| Transition-count baseline | 100.0% | 100.0% |

All observed links and intermediate relay junctions were connected. All numeric recall
snapshots replayed scores exactly. Learned graphs contained 1,278–1,280 unique relay
neurons and 8,192 synapses, representing 128 observed pair relations. This is evidence
of usable synthetic associative routing, not additional independent neural complexity.
The simple transition baseline also passes; no advantage over it is claimed.

The new numeric storage path reproduced resident learning exactly with 2,048 records
and an eight-record cache, including a mid-sequence checkpoint resumed in a fresh
process. Its 6,976-byte cache reservation excludes learner/checkpoint scratch, the
external encoder, runtime overhead and OS file caching. This is storage correctness,
not a whole-process memory or large-network recall result.

Paged recall now reproduces all frozen A1 scores with one- and eight-record caches,
in both query orders, without modifying model files. A four-hop query performed 287
loads through a one-record cache and retained its answer. Traversal separately reserves
530,944 bytes of scratch; the small record cache is not the entire application memory.

## What remains

- Exact paged recall is an experimental evaluation path. The existing default CLI
  utility has not yet been connected to it.
- Whole-process memory bounds, learned networks larger than RAM, realistic branching, and useful
  resource/quality tradeoffs are unproven. CUDA is deferred.
- The existing `learn`/`probe` commands still use the older runtime; `probe` trains a
  fresh brain. Saved-model related-material retrieval from local data is a later phase.
- The old R1 failure remains recorded. A1 is a separate experimental model, not a
  retrospective pass or a default behavior change.

The earlier claim that the complete substrate thesis was proven is withdrawn. Addressable
neuron IDs are not demonstrated useful capacity. A no-strings audit is not evidence of
absence of semantic memorization.

## Build and reproduce

.NET 8; no new dependencies.

```bash
dotnet build GreyMatter.sln -c Release
dotnet test GreyMatter.sln -c Release
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval recovery --mode relay --seeds 100 --output /tmp/gm-relay-dev/run.json
```

Final reproduction uses `--seeds 201,202,203,204,205` and a fresh output directory.
The evaluator refuses to overwrite snapshots. It generates its synthetic training data
and does not open a user brain or require the network corpus. JSON records configurations,
corpus checksums, per-query scores and activation traces. Numerical graph snapshots
require the AssemblyRelay runtime; they are recall-only, not training-resume checkpoints.

Reproduce R2 with `eval recovery --mode storage --records 128 --output /tmp/gm-r2-dev/result.json`
and a fresh directory. The R2 closeout in RESULTS.md provides the separate-process
resume command and storage/encoder limitations. Reproduce R3 with
`eval recovery --mode paging --seeds 100 --output /tmp/gm-r3-dev/result.json`; it reads
the frozen A1 artifacts. Follow the R4 contract in plan.md before making resource or
capacity claims; the default learn/probe utility is still unchanged.
