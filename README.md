# greyMatter

A C# experiment in learning and recall through sparse activation, procedural neuron
identities, numeric learned state, and connection-driven loading and eviction.

**Status — 2026-09-20: recovery campaign closed at R6; post-closeout comparison complete.**
Local-text learn/probe/audit works, and exact paging plus bounded training are demonstrated
on learned state. The R5 relay learner scored MRR **0.570** with **61/128** zero-output
cues; a diagnostic showed each token keeps only ~4 successors (slot cost) and forgetting
is clocked by other-successor count. Sparse wiring (D1) lifted it to **0.610 / 56 zeros**.
A decay-free **count policy through the identical substrate** (same records, packed store,
bounded cache, exact paging) scores **0.907 / 10 zeros at 4.55 MiB versus 37 MiB**. On this
task the neural learning layer subtracts value; the substrate is the deliverable, and the
count policy is now the default. Moving record checksums to the disk boundary halved relay
training time bit-exactly. Inside a Linux guest with a 128 MiB cgroup, the 1.5 GiB synthetic
model is queried exactly by a ~46 MB process with real block-layer reads (host cache still
underneath, so not device-cold). The three "seeds" are isomorphic relabelings, not
replication. **224 tests pass.**

The R6 closeout profile attributed about **54% of training scope time to record
serialization/checksum work**; hashing once per disk write/load instead of per access
cut that to 19% and training wall time from 8.6 s to 4.6 s with identical output. File-call time
is not pure disk wait; instrumentation and OS caching limit interpretation. Current
evidence does **not justify a CUDA port**. Further research requires a new directive.

## Use the saved-model utility

.NET 8; no new dependencies. Plain text expects one sentence per line. Tatoeba format
expects `id<TAB>language<TAB>text` and accepts English rows. Learning makes one pass;
model directories are immutable and existing paths are refused.

```bash
dotnet build GreyMatter.sln -c Release
dotnet test GreyMatter.sln -c Release

dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll learn \
  --model ./my-model --source ./sentences.txt --format text --seed 201 --budget-mib 128

dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll probe \
  --model ./my-model --cue coffee --candidates ./candidates.txt --hops 1

dotnet src/GreyMatter.Poc/bin/Release/net8.0/gm.dll audit --model ./my-model
```

`learn` accepts `--policy count-baseline` (default since 2026-09-21: one record per token,
directed counts, no decay), `source-local` (the R5 relay learner) or `sparse-relay` (D1); the
policy is persisted in the model and reported by `probe`/`audit`. Candidates are an external file with one token per line, at most 4096 unique tokens.
Probe uses saved encoder settings, requires no training source, and performs no
learning. It reports every candidate's activation score, ties and a `NoOutput` flag.
This is closed-candidate retrieval, not generation; ordinal ordering of zero scores
is not an answer. Hops 1–4 and memory budgets 128/256 MiB are supported. Peak RSS may
be unavailable through .NET on macOS (reported null); experiments use native timing.

Tokenization is versioned NFKC/lowercase letter/digit runs. A stable hash supplies
numeric identities. Under the default count policy each token is one numeric record
holding up to 32 successor counts; under the relay policies eight deterministic neurons
carry each token's signal and observed adjacent tokens update source-local synapses. Sentence boundaries reset
learning context. Text labels, candidate lists and source passages stay outside the
numeric model. The audit validates schema/checksums and rejects extra model files;
it cannot establish absence of semantic memorization in numerical weights.

Commands without `--model` retain the older experimental runtime, including its
train-before-probe behavior. Use the explicit named-model commands above for this
utility. Append/resume training for these text models is not implemented.

## What has been measured

| Check | Result |
|---|---|
| Real text |40,006 training sentences, 327,074 tokens; roughly 37 MiB model storage |
| Runtime memory |Training peak below 97 MiB; paged evaluation below 71 MiB |
| Real-data recall |128 fixed supported cues, 32 candidates; three fixed encoder seeds (isomorphic) |
| Post-closeout comparison |relay 0.570 → sparse relay 0.610 → **count policy 0.907** MRR on the same frozen task; 37 → 4.55 MiB |
| Larger synthetic state |4.47 million records, 33.55 million edges; 1.50 GiB snapshot |
| Capacity comparison |288 MiB training peak vs 1.71 GiB resident reference; exact paged scores |
| Synthetic quality at that size |Direct 100%, multi-hop 92.4%, including zero-output ties |

Graph storage is larger than raw source text; the 37 MiB figure is not a compression
ratio or a linear forecast. The real-data task deliberately selects supported
next-token associations and excludes normalized duplicate sentences across its split.
It does not measure open-ended meaning or unseen-relation reasoning. Seeds share
the same corpus; they are not independent data replications.

Earlier correctness tests demonstrate exact recall through repeated eviction with
one- and eight-record caches. The larger synthetic workload demonstrates bounded
training with actual eviction. Current real-data query working sets fit the cache.
OS caching assists measured latency on the host; a memory-capped guest run demonstrates
exact recall with the model far larger than the OS can cache, but cold-device latency
remains unmeasured on this machine. The historical full R4 grid remains
incomplete; later bounded passes do not rewrite its recorded failures. CUDA is deferred.

## Evidence and continuation

| Read | Purpose |
|---|---|
| [Prompt.md](Prompt.md) | Original purpose and constraints |
| [plan.md](plan.md) | Agent guide and closed-campaign boundary |
| [RESULTS.md](RESULTS.md) | Append-only evidence; latest R6 four-part verdict |
| [R6 closeout](artifacts/recovery/r6/summary.json) | Profile, exact replay, four verdicts and GPU decision |
| [R5 summary](artifacts/recovery/r5/summary.json) | Real-data scores, controls, intervals, storage and memory |
| [Capacity check](artifacts/recovery/capacity16/summary.json) | Measured  >1 GiB resident model and exact paging |
| [Resource follow-up](artifacts/recovery/resource-pressure/summary.json) | Cache pressure and index-I/O costs |
| [Packed storage](artifacts/recovery/packed/summary.json) | Storage allocation reduction and restart exactness |
| [Source-local retention](artifacts/recovery/retention-policy/summary.json) | Retention, replacement and branching tests |
| [R3](artifacts/recovery/r3/summary.json) | Exact recall under repeated eviction |
| `src/GreyMatter.Poc/Utility/` | Saved-model text boundary and commands |
| `tests/GreyMatter.Poc.Tests/` | Correctness and regression tests |

R5 scripts and exact command ledgers live in `artifacts/recovery/r5/`. They refuse
result/model overwrite. The three measured models remain at
`/private/tmp/gm-r5-20260917/model-201` (also 202/203); these are scratch locations.
Use a chosen persistent directory when training a model you intend to retain.
