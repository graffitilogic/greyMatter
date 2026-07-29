# greyMatter

Neurobiologically-inspired experiments in novel machine learning patterns.

## The Idea

Games like No Man's Sky render a seemingly limitless universe on commodity hardware
by generating content procedurally, only within the observer's scope, and persisting
only what changed. greyMatter applies the same principle to neural architecture:

- **Procedural generation** — neuron properties regenerate deterministically from
  compact VQ codes (a learned 512-entry codebook) instead of being stored in full.
- **Scoped activation** — only structures within the activation-distance of the
  current input are instantiated and computed; everything else stays dormant on disk.
- **Limited persistence** — only learned changes (Hebbian synaptic weights above
  budget thresholds) are persisted; the rest is regenerated on demand.

The falsifiable question driving the project: *can a procedurally regenerated
cortical region match a fully persisted one on recall fidelity, at a fraction of
the storage and resident memory?* See [REFOCUS.md](REFOCUS.md) for the current
plan, honest status, and metrics.

**Status (Jan 2026): experimental, mid-reboot.** The learning loop has a known
bug (no synapse creation in recent runs — see REFOCUS.md P1). Prior claims of
production readiness are retracted; nothing here is a product.

## Architecture (the load-bearing parts)

- `Core/Cerebro.cs` — orchestrator: cluster lifecycle, lazy loading, LRU eviction,
  cascade activation through the synaptic graph
- `Core/VectorQuantizer.cs` — VQ-VAE codebook (EMA updates); region IDs and the
  compact codes neurons regenerate from
- `Core/SparseSynapticGraph.cs` — Hebbian learning, pruning, decay; dictionary-based
  sparse storage
- `Core/NeuronHypernetwork.cs` — pattern-driven neuron allocation
  (`N = α·log(freq) + β·novelty + γ·complexity`)
- `Core/ProceduralNeuronData.cs` — compact storage format: VQ code + budgeted
  synapses (~90% smaller than full snapshots)
- `Storage/EnhancedBrainStorage.cs` — partitioned, streaming persistence
  (256 partitions, MessagePack)
- `Learning/` — Tatoeba/Wikipedia readers and training pipeline
- `Program.cs` — entry points: `--production-training`, `--cerebro-query`,
  `--inspect-brain`, plus procedural regen tests

## Running

```bash
cd greyMatter
dotnet build
dotnet run -- --production-training --dataset tatoeba_small --duration 600
dotnet run -- --cerebro-query stats
```

Brain state persists to the path configured in `CerebroConfiguration`
(default NAS path: `/Volumes/jarvis/brainData`).

## Repo Hygiene Notes

- This repo lives in Dropbox. Git operations from sandboxed tools can fail on
  cloud-only placeholder files — right-click the repo folder in Finder and choose
  **Make available offline** before heavy git surgery.
- Don't `rm -rf` brainData while training (macOS file locks); rename it aside instead.
- Historical experiments (ethics drives, goal systems, alternate brain
  implementations) were removed in the Jan 2026 refocus. They live in git history;
  archived design docs are in `greyMatter/docs/archive/`.
