# greyMatter

A C# experiment in learning and recall with a network whose learned state can exceed its
resident working memory. The intended mechanisms are sparse activation, procedural
regeneration, compact learned deviations, and loading/eviction driven by synaptic traversal.

**Status — 2026-09-09:** recovery stopped at R1's composed-retrieval gate after one permitted
learning correction. R0 passed; 148 tests pass. Memory virtualization is still unproven.
The subsequent T1 review found delayed threshold crossing and a mismatch between concept
links and neuronal simulation ticks. T2 then raised four-link retrieval from 3.4% to
31.5% at 12 ticks on one development seed; shuffled scored 3.4%. This remains below
the learning gate. Exact-replay numerical snapshots are available for further review.
T3 additionally found missing routes for 10/32 endpoints within 12 edges, and activation
loss on other existing routes. The next proposed design is an explicit assembly relay.
See the T1–T3 findings at the end of RESULTS.md; agents must not automatically start R2.

| Read | Purpose |
|---|---|
| [Prompt.md](Prompt.md) | Original purpose and constraints |
| [plan.md](plan.md) | Active recovery guide, terminal handoff pointer, historical plan below |
| [RESULTS.md](RESULTS.md) | Evidence log; start at Recovery R1 closeout near the end |
| [Recovery summary](artifacts/recovery/r1/summary.json) | Machine-readable final metrics |
| `src/GreyMatter.Poc/` | Implementation |
| `tests/GreyMatter.Poc.Tests/` | Tests, including recovery fixtures |

## What is measured

On a balanced synthetic symbol-sequence task, the experimental cue-member temporal
learning arm achieved the following mean top-1 retrieval over five fixed seeds:

| arm | direct association | composed association |
|---|---:|---:|
| Learned | 100.0% | 35.6% |
| Untrained | 3.1% | 3.1% |
| Shuffled training | 2.7% | 2.5% |
| Transition-count baseline | 100.0% | 100.0% |

Composed retrieval falls from 69.2% at two hops to 3.2% at three and 0.7% at four.
The registered mean gate was 80%; it failed. The experimental rule stays off by default.
The default learner achieved 100% direct retrieval on one development seed only.
These results establish synthetic direct association, not natural-language understanding.

The recovery also fixed two measured runtime defects: newly reached neurons no longer
propagate within the same logical step, and recall no longer updates fatigue/familiarity.

## What is not delivered yet

- Propagation still skips connected neurons that are not resident.
- Recipes remain in an in-memory dictionary; resume loads all stored partitions.
- Increasing the virtual ID range has not demonstrated additional useful neural capacity.
- `probe` still trains a fresh brain before showing neuron activations; saved-model
  related-material retrieval is planned but unimplemented.
- There is no demonstrated bounded-memory training/recall tradeoff or CUDA benefit.

The historical conclusions that the complete substrate thesis was proven and representation
was uniquely isolated as the cause of association failure are withdrawn. Historical numbers
remain in RESULTS.md with their limitations. Recognition/familiarity cannot substitute for
association, and a no-strings disk audit cannot establish absence of semantic memorization.

## Build and reproduce

.NET 8; no new dependencies were added during recovery.

```bash
dotnet build GreyMatter.sln -c Release
dotnet test GreyMatter.sln -c Release
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval recovery --mode fixtures
```

The registered learning evaluator supports development seed 100 or final seeds
101,102,103,104,105. Use a fresh result path; the evaluator refuses to overwrite artifacts.
Final reproduction (returns exit 1 when the gate fails):

```bash
dotnet run --project src/GreyMatter.Poc/Poc.csproj -c Release -- eval recovery --mode learning --seeds 101,102,103,104,105 --sequence-uses-cue-members true --output /tmp/greyMatter-r1-reproduction.json
```

This synthetic evaluator does not open a user brain or require the network corpus.
All query scores, effective configurations and corpus checksums are recorded in JSON.
A manually wired fixture is runtime evidence, never evidence of learning. See the guide's
stopping rules before launching new experiments.
