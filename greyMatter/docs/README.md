# greyMatter Documentation

Project overview and honest status: see the root [README.md](../../README.md) and
[REFOCUS.md](../../REFOCUS.md) — REFOCUS.md is the single source of truth for
current status, plan, and metrics.

## Active Documents

- [TECHNICAL_DETAILS.md](TECHNICAL_DETAILS.md) — architecture reference: two-format
  storage, procedural regeneration, cluster reuse, wave traversal
- [SYNAPTIC_NOVELTY_DETECTION.md](SYNAPTIC_NOVELTY_DETECTION.md) — novelty from
  cascade depth through the synaptic graph
- [BIOLOGICAL_ALIGNMENT.md](BIOLOGICAL_ALIGNMENT.md) — biological fidelity principles
- [PRODUCTION_TRAINING_GUIDE.md](PRODUCTION_TRAINING_GUIDE.md) — running training
- [QUERY_GUIDE.md](QUERY_GUIDE.md) — querying and inspecting the brain

## Archive

`archive/` holds superseded roadmaps, phase summaries, and fix write-ups from the
2025–Jan 2026 era. They are historical record, not current status. Several contain
unverified claims (see REFOCUS.md ground rules: no claim without a command).

## Quick Reference

```bash
cd greyMatter
dotnet build
dotnet run -- --production-training --dataset tatoeba_small --duration 600
dotnet run -- --cerebro-query stats
dotnet run -- --inspect-brain
```
