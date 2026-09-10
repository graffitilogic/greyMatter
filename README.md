# greyMatter

A C# experiment in learning and recall using sparse activation, procedural neuron
identities, compact learned state, and eventually connection-driven loading and eviction.

**Status — 2026-09-09:** the new protected assembly relay passes its registered synthetic
learning gates. **155 tests pass.** The next deliverable is a bounded disk-backed store;
network execution larger than RAM remains unproven.

| Read | Purpose |
|---|---|
| [Prompt.md](Prompt.md) | Original purpose and constraints |
| [plan.md](plan.md) | Active agent guide and current phase |
| [RESULTS.md](RESULTS.md) | Evidence log; start at the A1 closeout near the end |
| [A1 summary](artifacts/recovery/a1/summary.json) | Final synthetic metrics |
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

## What remains

- The relay is fully resident. Its current backing store retains recipes in RAM.
- Bounded storage, learned networks larger than RAM, realistic branching, and useful
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

Follow the R2 memory-accounting contract in plan.md before extending the implementation.
