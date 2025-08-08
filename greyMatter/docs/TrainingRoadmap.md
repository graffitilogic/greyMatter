# GreyMatter Training Roadmap (Prototype)

Purpose: Document a realistic path from current prototype to a measurable "toddler" learner without relying on large LLMs. This roadmap will evolve alongside the code.

## Current Reality (Aug 2025)
- Architecture scaffolding exists (HybridNeuron, BrainInJar, storage, clustering, consolidation).
- Trainers are largely hardcoded demonstrations, not data-driven curricula.
- Minimal ingestion/analysis exists; one streaming reader added for Simple English Wikipedia.
- No end-to-end task/evaluation loop yet.

## Principles
- Context over vocabulary lists; learn via short, concrete sentences first.
- Data-driven methodology; lessons are compiled from corpora.
- Incremental difficulty; spaced reinforcement; nightly consolidation.
- Measurable progress via simple tasks (cloze, micro-QA).
- License-safe, local-first datasets.

## Phases

### Phase 1 — Datasets & Ingestion
- Add dataset download automation for:
  - Project Gutenberg public-domain children’s books (IDs list).
  - Tatoeba simple English sentences (CC BY 2.0; attribution required).
  - WordNet 3.1 (relations for synonyms/hypernyms).
  - ConceptNet 5.8 edges (IsA/PartOf/UsedFor for grounding).
  - Brysbaert concreteness norms (for curriculum ordering).
- Implement readers in `Learning/`:
  - `GutenbergReader`, `TatoebaReader`, `WordNetReader`, `ConceptNetReader`.

### Phase 2 — Curriculum Compiler
- `CurriculumCompiler` that:
  - Scores sentences by length, concreteness, frequency, and pattern simplicity.
  - Builds staged queues: nouns/verbs → SVO → modifiers → questions → narratives.
  - Emits lesson items: sentence + focus concept(s) + lightweight targets.

### Phase 3 — Lightweight Linguistic Analysis
- Heuristics first (no heavy NLP):
  - Tokenize, lowercase, basic lemmatize (rules), suffix/prefix flags.
  - POS guesses via WordNet + rules.
  - Simple SVO pattern templates; pronoun/coplan patterns.
  - Co-occurrence windows to form distributional hints.

### Phase 4 — Feature Grounding
- Map each lesson item to features:
  - lexical: pos_guess, lemma, suffix flags
  - semantics: WordNet synsets/hypernyms; ConceptNet edges
  - concreteness: binned score
  - syntax: subject/verb/object flags if matched
  - frequency: moving frequency bins
- Feed via `BrainInJar.LearnConceptAsync` for word, pattern, and sentence-level bindings.
- Add spaced repetition schedule.

### Phase 5 — Training Loops & Evaluation
- Environmental learner (passive reading of staged items).
- Cloze trainer (self-masked tokens) with accuracy by POS/difficulty.
- Micro-QA (templated who/what/where from SVO sentences).
- Eval harness uses CBT subsets + in-house cloze.

### Phase 6 — Consolidation & Pruning
- Nightly replay of day buffer; prune weak synapses; strengthen frequent co-occurrences.
- Track concept mastery via `GetConceptMasteryLevelAsync`.

## Deliverables (near-term)
- [ ] Update README to reflect prototype status and roadmap link.
- [ ] Extend setup script with dataset downloads (Gutenberg/Tatoeba/WordNet/ConceptNet).
- [ ] Implement `CurriculumCompiler` + `EnvironmentalLearner` skeletons.
- [ ] Implement `EvalHarness` with basic cloze testing.
- [ ] Add attribution/LICENSING notes in `docs/Datasets.md`.

## Risks & Mitigations
- Data noise → use tight filters (length, vocab list, concreteness) for early stages.
- Feature sparsity → use ConceptNet/WordNet edges for scaffolding.
- Over-claiming → keep README conservative, track metrics in `docs/Results.md`.

## Notes
- CHILDES/TalkBank is excellent but has CC BY-NC-SA; use for research, don’t redistribute.
- Optional: Later add a tiny local Q&A model to generate augmentations and checks.
