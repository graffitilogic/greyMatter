# Project GreyMatter 
Neurobiologically-inspired experiments in novel machine learning patterns.

## Purpose
Discovery, experiments, and simulations of neural concepts that lean on biological ideas (dynamic neuron allocation, clustering, pruning, consolidation) rather than massive static parameter counts.

## Philosophy
Modern LLMs demonstrate remarkable capability through scale. This project explores a complementary path: emphasizing neurological structure, lazy loading, and storage-backed scale to pursue emergent properties.

This is a humble, self-taught programmer's attempt to combine known ideas in new ways and see what happens.

## Current Status (Prototype)
This repository is an early prototype. Some components are implemented, others are placeholders or demos:
- Implemented:
  - HybridNeuron with fatigue, dynamic thresholds, sparse connections (`Core/HybridNeuron.cs`)
  - BrainInJar orchestrator with lazy cluster loading and persistence (`Core/BrainInJar.cs`)
  - Storage layer and basic cluster/index persistence (`Storage/*`)
  - Demonstration trainers for language foundations (`Core/LanguageFoundationsTrainer.cs`, `Core/ComprehensiveLanguageTrainer.cs`) — currently hardcoded examples
  - Basic continuous processing scaffold (`Core/ContinuousProcessor.cs`)
- In progress:
  - Data ingestion readers (`Learning/*`) — initial Wikipedia stream reader added
  - Training methodology that compiles lessons from corpora (see roadmap)
  - Evaluation harness and realistic metrics

What this is NOT (yet):
- A complete language acquisition system
- A consciousness simulation
- Verified emergent scaling with measured benchmarks

## Near-Term Roadmap
A pragmatic plan to reach a “toddler” learner without large LLMs is documented here:
- See `greyMatter/docs/TrainingRoadmap.md`

High-level phases:
1) Datasets & ingestion (Gutenberg, Tatoeba, WordNet, ConceptNet, concreteness norms)
2) Curriculum compiler (stage simple → complex, spaced reinforcement)
3) Lightweight linguistic analysis (heuristics to extract SVO, POS guesses)
4) Feature grounding and training loops (environmental learner, cloze tasks)
5) Evaluation harness (cloze + micro-QA), nightly consolidation & pruning

## Running Demos (prototype)
- Build the solution and run `Program.cs` to explore demo modes. Expect experimental output.
- Optional datasets setup: `setup_learning_datasets.sh` downloads initial corpora into `learning_datasets/` (Simple English Wikipedia, CBT). These are for prototyping; curriculum compilation will follow.

## File Architecture (selected)
```
Core/
├── BrainInJar.cs
├── HybridNeuron.cs
├── ComprehensiveLanguageTrainer.cs
├── LanguageFoundationsTrainer.cs
├── ContinuousProcessor.cs
Learning/
├── WikipediaStreamReader.cs
Storage/
└── ...
```

## Contributing / Using
- Expect rapid iteration and breaking changes.
- Feedback, issues, and ideas are welcome.

## License and Data
- Code: choose a license that suits your usage (TBD here).
- External datasets have their own licenses (see docs). Do not commit large dumps to the repo.





