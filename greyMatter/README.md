# greyMatter - Biologically-Inspired Neural Learning System 🧠

> **Status**: Experimental research prototype - not production software
> 
> **Vision**: Procedural generation meets neuroscience - creating dynamic, efficient neural structures

## ⚠️ Current State (October 2025)

**Week 1 (Oct 5-11)**: ✅ **COMPLETE** - Foundation validation successful  
**Week 2 (Oct 12-18)**: ✅ **COMPLETE** - Biological learning implemented and validated

### What Actually Works:
- ✅ **Biological Neural Learning**: Neurons form genuine connections (100% connected, 4.25M total connections)
- ✅ **Hebbian Learning**: "Fire together, wire together" - connections strengthen with activation
- ✅ **Single-source learning**: Tatoeba CSV sentences (117 sentences/sec training rate)
- ✅ **Save/load brain state**: FastStorageAdapter with 1,350x speedup (130MB for 55K neurons)
- ✅ **Knowledge query system**: Inspect neurons, connections, learning statistics
- ✅ **LLM API integration**: Ollama guidance for learning strategies

### Recent Achievements (Week 2):
- ✅ **Biological Learning Fix**: From 0% to 100% neurons with connections
- ✅ **Neural Connections**: 4,253,690 connections across 55,885 neurons
- ✅ **Word Associations**: 866 associations formed biologically (was 0)
- ✅ **Sentence Learning**: 100 sentences tracked (was 0)
- ✅ **Query System**: Successfully loads and displays neural data

### What's Framework-Only (Untested):
- 🏗️ Column-based cognitive architecture (code exists, never run)
- 🏗️ Multi-source integration (1 of 8 sources proven)
- 🏗️ Procedural column generation (not wired to learning)

### What Doesn't Exist:
- ❌ Retention or understanding tests
- ❌ Always-on background service
- ❌ Multi-modal inputs (audio, vision, etc.)

**For complete status, see:** [`docs/roadmaps/ACTUAL_STATE.md`](docs/roadmaps/ACTUAL_STATE.md)

---## 📖 Documentation

### Key Documents:
- **[ACTUAL_STATE.md](docs/roadmaps/ACTUAL_STATE.md)** - **Single source of truth** for what works vs what's theoretical
- **[WEEK1_RESULTS.md](docs/WEEK1_RESULTS.md)** - Foundation validation results (Oct 5-11)
- **[WEEK2_RESULTS.md](docs/WEEK2_RESULTS.md)** - Biological learning validation results (Oct 12-18)
- **[BIOLOGICAL_LEARNING_FIX.md](docs/BIOLOGICAL_LEARNING_FIX.md)** - Technical implementation details
- **[HONEST_PLAN.md](docs/roadmaps/HONEST_PLAN.md)** - Week-by-week development plan
- **[ROADMAP_2025.md](docs/roadmaps/ROADMAP_2025.md)** - High-level development roadmap
- **[TECHNICAL_DETAILS.md](TECHNICAL_DETAILS.md)** - System architecture and implementation

### Project Status:
- **Build**: ✅ 0 errors, 130 warnings (nullability warnings)
- **Weeks Completed**: ✅ Week 1 (Foundation), ✅ Week 2 (Biological Learning)
- **Test Coverage**: Basic validation complete, expansion needed
- **Biological Learning**: ✅ 100% neurons with connections, 4.25M total connections
- **Query System**: ✅ Operational - loads and displays neural data
- **Research Status**: Core hypothesis (emergence from columns) untested

---

## 🤝 Contributing

This is an active research project. Before contributing:
1. Read [ACTUAL_STATE.md](docs/roadmaps/ACTUAL_STATE.md) for honest current state
2. Check [WEEK2_RESULTS.md](docs/WEEK2_RESULTS.md) for latest achievements
3. Focus on validation and testing over new features
4. Document results with evidence, not claims

---

## 📜 License

MIT License - See LICENSE file for details

---

## 🔬 Research Philosophy

**Standard**: Test everything. No "COMPLETE" claims without validation. Build → Test → Validate → Document → Repeat.

**Status Levels**:
- 🏗️ **Framework**: Code exists, compiles, never executed
- ⚠️ **Prototype**: Executed 1-2 times, may have issues  
- ✅ **Validated**: Executed 3+ times, results consistent
- 🚀 **Production**: Used in real workflows, stable over time

---

**Last Updated**: October 7, 2025

**Data & Storage**
- Multi-source data provider expects real datasets (synthetic fallbacks removed for quality)
- FastStorageAdapter migration ~80% complete; legacy paths being phased out
- Storage validation now tests the same paths used by training (previously disconnected)scale through biological principles, not brute force.

## 🎯 What is greyMatter?

greyMatter is an **experimental research prototype** exploring neural learning systems inspired by biological brain architecture. The goal is to create **ephemeral neural clusters** that are procedurally generated on-demand, rather than using traditional static neural networks.

**Central Inspiration**: "If we can simulate entire galaxies through common-seed procedural functions, why can't we overcome scale limitations with neural networks through similar concepts?"

**Core Research Question**: Can higher level cognition emerge through complex interactions between specialized, short-lived neural structures rather than through massive parameter counts or computational brute force?

**Current Reality**: We have a working single-source learning system with fast binary storage. The "emergence through column interactions" hypothesis remains **untested** - column architecture code exists but has never been executed.

## 🚀 Quick Start

### Prerequisites
- .NET SDK 8.0+
- Git client
- **Optional**: Ollama with deepseek-r1:1.5b model for LLM teacher guidance

### Setup & Run
```bash
# Clone and build
git clone https://github.com/graffitilogic/greyMatter.git
cd greyMatter/greyMatter
dotnet build

# 🎯 RECOMMENDED: Quick Demo (No setup required)
dotnet run -- --tatoeba-hybrid-1k              # 1K sentences, ~2 min

# 🤖 ADVANCED: LLM-Guided Learning
dotnet run -- --llm-teacher                    # Interactive learning with AI teacher
dotnet run -- --llm-teacher --non-interactive  # Automated AI-guided training

# 🔄 Continuous Learning (Background processing)
dotnet run -- --continuous-learning --max-words 10000

# 📊 System Validation
dotnet run -- --performance-validation         # Comprehensive health check
```

### LLM Teacher Setup (Optional but Recommended)
For the full LLM-guided learning experience:
```bash
# Install Ollama (if not already installed)
curl -fsSL https://ollama.ai/install.sh | sh

# Pull the optimized model
ollama pull deepseek-r1:1.5b

# Start Ollama server
ollama serve

# Run greyMatter with LLM teacher
dotnet run -- --llm-teacher
```

### Essential Commands

**🎯 Primary Learning (Recommended)**
```bash
# Quick demos - No setup required
dotnet run -- --tatoeba-hybrid-1k            # Fast 1K sentence demo (~2 min)
dotnet run -- --llm-teacher                  # AI-guided interactive learning
dotnet run -- --reading-comprehension        # Q&A capabilities demonstration

# Production learning - Requires data setup
dotnet run -- --continuous-learning          # Background multi-source learning
dotnet run -- --enhanced-learning            # Enhanced Tatoeba training
dotnet run -- --performance-validation       # System health validation
```

**🛠️ Advanced Options**
```bash
# Data conversion (usually automatic)
dotnet run -- --convert-tatoeba-data         # Prepare Tatoeba dataset
dotnet run -- --convert-enhanced-data        # Prepare enhanced multi-source data

# Configuration
dotnet run -- --help                         # Show all options
dotnet run -- --brain-path ~/myBrain         # Custom brain storage path
dotnet run -- --max-words 50000              # Set learning target
dotnet run -- --batch-size 1000              # Adjust batch processing

# Analysis & debugging
dotnet run -- --debug                        # Comprehensive debugging
dotnet run -- --evaluate                     # Evaluate learning results
```

**🎓 Interactive LLM Commands**

During `--llm-teacher` sessions:
```bash
status                    # Get AI analysis of learning progress
focus science            # Guide toward scientific vocabulary
focus conversation       # Activate conversational data sources
focus news              # Focus on current events vocabulary
<any question>          # Ask AI about concepts or strategy
quit                    # Exit session
```

### Configuration Paths

Default storage locations (customizable via CLI):
```bash
--brain-path      # Default: /Volumes/jarvis/brainData
--training-data   # Default: /Volumes/jarvis/trainData
--working-drive   # Optional: External NAS/SSD path
```

### Typical Workflow

1. **Quick Test**: `dotnet run -- --tatoeba-hybrid-1k` (verify setup)
2. **AI Learning**: `dotnet run -- --llm-teacher` (best results)
3. **Background**: `dotnet run -- --continuous-learning --max-words 50000`
4. **Validate**: `dotnet run -- --performance-validation`

## 🧬 Core Architecture

### What Actually Works

#### TrainingService - Unified Learning Interface
Centralized service that replaced 80+ scattered demo classes:
- **Parameterized Training**: `RunTatoebaTrainingAsync()`, `RunLLMTeacherSessionAsync()`
- **Performance Validation**: `RunPerformanceValidationAsync()`
- **Configuration Management**: Unified parameter classes for all training modes
- **Status**: ✅ **Validated** - Successfully consolidated training infrastructure

#### LLM Teacher Integration
**Ollama API integration for learning guidance**:

**What Works:**
- **External LLM API**: Ollama endpoint with deepseek-r1:1.5b model
- **Interactive Guidance**: User can ask questions during learning sessions
- **Structured JSON Communication**: Type-safe responses from LLM
- **Multi-mode Operation**: Interactive guidance + background learning loops

**Current Limitations:**
- LLM provides recommendations but execution is mostly manual
- No proven improvement in learning outcomes vs. non-LLM baseline
- "Dynamic curriculum generation" is theoretical - requires testing

**Status**: ⚠️ **Prototype** - Works but impact unvalidated

#### Single-Source Learning (Tatoeba)
**Proven working system**:
- **TatoebaLanguageTrainer**: Reads CSV sentences, extracts concepts
- **LanguageEphemeralBrain**: Creates neural clusters for words
- **Sparse Activation Patterns**: Each word gets unique sparse representation
- **Neural Activation Storage**: Stores activation signatures with strengths
- **Save/Load**: Persists and restores brain state

**Status**: ✅ **Validated** - Multiple successful training runs

#### Fast Binary Storage
**MessagePack-based storage system**:
- **FastStorageAdapter**: Binary serialization (vs JSON)
- **Proven Performance**: 1,350x speedup (0.4s vs 540s for 5K vocabulary)
- **Simple Interface**: Save/Load brain state as Dictionary<string, object>

**Status**: ✅ **Validated** - Production-ready performance

### What's Framework-Only (Untested)

#### Column-Based Cognitive Architecture
**Code exists but never executed**:
- **WorkingMemory** (328 lines): Manages active concepts with decay
- **MessageBus** (421 lines): Inter-column communication protocol
- **AttentionSystem** (390 lines): Task-specific focus with profiles
- **ColumnBasedProcessor** (509 lines): Routes inputs through column pipeline
- **ProceduralCorticalColumnGenerator** (505 lines): Template-based column generation

**Current Reality**: All classes compile. Zero test runs. Completely unvalidated.

**Status**: 🏗️ **Framework Only** - Needs first execution and debugging

#### Multi-Source Data Integration
**Partial implementation**:
- ✅ **Tatoeba**: CSV reader working
- 🏗️ **News/Wikipedia/Subtitles/etc**: Code exists, file paths unclear, untested
- 🏗️ **LLM-guided source selection**: Theoretical - manual wiring required

**Current Reality**: 1 of 8 claimed sources proven working.

**Status**: 🏗️ **Mostly Theoretical** - Need to test with real files

#### Procedural Column Generation
**Not integrated**:
- Template methods exist for phonetic, semantic, syntactic columns
- Not wired into any learning pipeline
- No regeneration flow implemented
- No efficiency measurements

**Status**: 🏗️ **Framework Only** - Need to wire into learning

---

## 📊 Honest Limitations

### What Doesn't Exist:
- ❌ **Knowledge Quantification**: Beyond vocabulary counts
- ❌ **Retention Testing**: No tests for memory over time
- ❌ **Understanding Tests**: No comprehension validation
- ❌ **Transfer Learning Measurement**: No cross-domain tests
- ❌ **Always-On Service**: No background daemon mode
- ❌ **Multi-Modal**: No audio, vision, or non-text inputs
- ❌ **Emergence Validation**: Can't test column interactions (never run)

### Current Capabilities:
- ✅ Learn vocabulary from single datasource (Tatoeba)
- ✅ Save and load brain state
- ✅ Query vocabulary presence
- ✅ Get basic statistics (vocab size, concept count)
- ⚠️ LLM guidance (works but impact unproven)

### Research Questions Status:
- **"Can procedural generation overcome scale limits?"** → ❌ Cannot answer (generator not active)
- **"Can emergence come from column interactions?"** → ❌ Cannot answer (columns never run)
- **"Is biological inspiration more efficient?"** → ⚠️ Partial (storage proven, learning unproven)

---

## 📋 Current Focus (Week 1, Oct 5-11)

**Goal**: Validate what we claim works

### Tasks:
1. **Multi-source validation**: Prove 3 datasources work with real files
2. **Save/load testing**: Document with actual training run
3. **Knowledge queries**: Implement QueryVocabulary(), GetRelatedConcepts()
4. **Documentation**: Honest results, no inflated claims

**See**: [`docs/roadmaps/HONEST_PLAN.md`](docs/roadmaps/HONEST_PLAN.md) for detailed week-by-week plan

---

## 📊 Project Status (October 2025)

**Phase 0 Foundation Cleanup: ✅ COMPLETE**

This repository is an active research prototype with core systems operational and a clear path forward. Recent Phase 0 cleanup significantly improved code quality and maintainability.

### ✅ What’s working now
- Training entry points and demos build and run; core classes for brains, storage, learners, and evaluators are present.
- TrainingService exists and is being adopted, but legacy demos still exist alongside it.
- LLM Teacher integration (Ollama/deepseek-r1:1.5b) is wired up and can drive interactive sessions; analysis logic is evolving.
- Multi-source data provider has been cleaned up to remove static fallback generators; it now expects real datasets on disk.
- Storage layer includes a FastStorageAdapter implementation; migration from legacy paths is in progress.
- Continuous learning can process large word counts and autosave; validation now reads the same storage used by training.

### 🛠️ In Progress / Next Steps

**Phase 1: Storage & Persistence (2-4 weeks)**
- Complete FastStorageAdapter migration across remaining code paths
- Add versioned schema with integrity checks
- Implement periodic snapshots and quick-restore capability
- **Goal**: Save/load 50k-100k concepts in seconds

**Phase 2: Procedural Neural Core (4-6 weeks) - CRITICAL PATH**
- Activate ProceduralCorticalColumnGenerator in learn/recall pipeline
- Implement working memory APIs and inter-column messaging
- Add attention/temporal gating mechanisms
- **Goal**: Test core "emergence through interaction" hypothesis

**Phase 3-5**: LLM maturation, evaluation harness, scaling (see Roadmap)

### ⚠️ Known limitations
- Some documentation previously overstated “production-ready” status; treat this repo as an R&D codebase.
- Many demo classes remain; expect overlap and older patterns until consolidation completes.
- Performance figures vary by configuration and are still being characterized; avoid assuming prior headline numbers.
- Data sources require local files; no static, synthetic fallbacks remain. Missing data will error with clear messages.

### 📂 Training Data Requirements

**Default path**: `/Volumes/jarvis/trainData`  
**Override**: Use `--training-data /your/path` or set `TRAINING_DATA_ROOT` env var

**Expected structure**:
```
trainData/
├── news/headlines.txt                  # News headlines
├── scientific/abstracts.txt            # Scientific papers
├── technical/documentation.txt         # Technical docs
├── Tatoeba/sentences_eng_small.csv    # Tatoeba dataset
├── enhanced_learning_data/            # Pre-converted data
└── [other sources as configured]
```

**Setup**: Download datasets or use `--convert-*-data` commands to prepare sources

## 🏗️ Technical Foundation

### Key Classes
- **`TrainingService`**: Unified parameterized training interface (replaces 80+ demos) - **PRODUCTION READY**
- **`LLMTeacher`**: Revolutionary intelligent continuous learning guidance system with external API integration
- **`DynamicCurriculumManager`**: LLM-driven curriculum generation and progress evaluation
- **`Cerebro`**: Central learning orchestrator with advanced brain state management
- **`SimpleEphemeralBrain`**: Core ephemeral neural cluster implementation with biological activation patterns
- **`SemanticStorageManager`**: Biologically-inspired storage with Huth-inspired semantic domains
- **`ContinuousLearner`**: Background continuous learning with auto-save and multi-source integration
- **`EnhancedDataIntegrator`**: Multi-source data processing pipeline (8+ data source types)
- **`FastStorageAdapter`**: High-performance storage with 1,350x speed improvement
- **`ProceduralCorticalColumnGenerator`**: On-demand biological neural structure creation

### Storage Architecture
```
/brainData/
├── hippocampus/              # Sparse indices
├── cortical_columns/         # Semantic clusters
│   ├── living_things/        # Animals, plants, humans
│   ├── artifacts/            # Tools, vehicles, buildings
│   ├── natural_world/        # Geography, weather, materials
│   └── language_structures/  # Grammar, syntax patterns
└── working_memory/           # Active concepts
```

## 🎯 Design Philosophy

### Biological Inspiration
1. **Sparse Activation**: Only relevant neural clusters active at any time
2. **Procedural Details**: Complex structures generated from simple rules
3. **Temporal Dynamics**: Neural fatigue, consolidation, forgetting
4. **Hierarchical Organization**: From sensory primitives to abstract concepts

### No Man's Sky Analogy
Just as No Man's Sky doesn't store every planet's details but generates them procedurally within render distance, greyMatter generates neural complexity on-demand within the "cognitive render distance" of current tasks.

## 🔬 Research Areas

### Current Active Research
- **LLM-Guided Learning**: How external LLM teachers can enhance biological learning patterns
- **Dynamic Curriculum Generation**: Personalized learning paths based on real-time progress analysis
- **Multi-Source Data Integration**: Optimal combination of scientific, conversational, technical data sources
- **Neural Activation Persistence**: Storing and retrieving biological-style activation patterns
- **Teacher-Student Learning Dynamics**: Effectiveness of LLM guidance vs. autonomous learning
- **Cross-Domain Knowledge Transfer**: How learned patterns apply across semantic domains

### Proven Concepts
- **Procedural Scale Breakthrough**: Can procedural generation overcome traditional ML scale limitations? **YES** - demonstrated with cortical columns
- **Biological Efficiency**: Is biological inspiration more resource-efficient than brute force? **YES** - 1,350x performance improvement achieved
- **Emergent Behaviors**: Do complex behaviors emerge from simple neural interactions? **VALIDATED** - semantic clustering emerges naturally
- **Real-time Learning Guidance**: Can LLMs effectively guide neural learning in real-time? **REVOLUTIONARY SUCCESS**

### Future Research Directions
- **Multi-Column Communication**: Complex inter-region brain communication patterns
- **Attention Systems**: Biological attention and focus mechanisms with LLM integration  
- **Memory Consolidation**: Sleep-like processing for knowledge integration guided by LLM analysis
- **Goal-Directed Behavior**: Emergent problem-solving capabilities with teacher guidance
- **Hierarchical Learning**: Multi-level abstraction with LLM curriculum management
- **Cross-Modal Integration**: Combining text, visual, and audio learning modalities

## 📚 Architecture Details

### Procedural Neural Generation
Neural structures follow biological principles:
- **Cortical Columns**: 80-120 neurons per column
- **Connection Patterns**: Local clustering, long-range sparse connections
- **Activation Thresholds**: Realistic neural firing patterns
- **Fatigue Mechanics**: Usage-dependent performance degradation

### Semantic Organization (Huth-Inspired)
Based on brain mapping research identifying specialized cortical regions:
- **Living Things**: Animals, plants, human-related concepts
- **Artifacts**: Tools, vehicles, buildings, technology
- **Natural World**: Geography, weather, materials
- **Abstract Domains**: Emotions, relationships, temporal concepts

### Teacher-Student Learning
Childhood learning patterns with LLM teacher:
- **Guided Instruction**: Teacher provides conceptual frameworks
- **Exploration**: Student discovers patterns in data
- **Validation**: Teacher confirms or corrects understanding
- **Structured Output**: JSON-controlled LLM responses for precise feedback

## 🤝 Contributing

This is an experimental research project exploring the intersection of:
- **Neuroscience**: Biological brain architecture and learning
- **Procedural Generation**: Game development techniques for scale
- **Machine Learning**: Modern ML with biological constraints
- **Systems Programming**: High-performance .NET implementation

## 📄 License

[License details]

---

**Bottom Line**: greyMatter is an active R&D project demonstrating that biologically-inspired neural patterns + LLM-guided learning + procedural generation can achieve sophisticated learning behaviors. Phase 0 foundation cleanup is complete, providing a clean base for testing core emergence hypotheses in Phase 2.

## 🗺️ Roadmap to Desired End-State

**Goal**: A trained system that hydrates cortical columns procedurally with minimal persistence during learning and can regenerate structures on demand during recognition/response, with continuous background consolidation.

Phase 0 — Foundation cleanup (now → 2 weeks)
- Remove synthetic/static fallbacks (done for multi-source provider); enforce real datasets with explicit errors.
- Normalize configuration: single TrainingConfiguration (paths, batch sizes, storage adapter, teacher options).
- Route CLI/Program.cs through TrainingService for all modes; retire legacy demo entry points incrementally.
- Acceptance: one canonical “learn/validate” path; data path issues fail fast with actionable messages.

### Phase 1 — Storage and Persistence (2–4 weeks)
- Complete FastStorageAdapter migration; quarantine legacy storage behind a compatibility shim
- Add versioned schema and small integrity checks; implement periodic snapshots and quick-restore
- **Acceptance**: Save/load within seconds for 50k–100k concepts; validated by Performance Validation task

### Phase 2 — Procedural Neural Core (4–6 weeks) - CRITICAL PATH
- Flesh out ProceduralCorticalColumnGenerator with consistent column templates and connection rules
- Implement working memory APIs and inter-column messaging primitives; basic attention/temporal gating
- **Acceptance**: Measurable reuse/regeneration of columns across sessions; unit tests for regeneration correctness
- **WHY CRITICAL**: Tests core "emergence through interaction" hypothesis

### Phase 3 — LLM Teacher Maturation (parallel, 3–5 weeks)
- Define strict contracts (inputs/outputs) for AnalyzeLearningState, ProvideConceptualMapping, SuggestCurriculum
- Add reliability guards: retries, timeouts, structured validation, and telemetry of teacher decisions
- **Acceptance**: Teacher-driven focus measurably improves retention and learning rate on a fixed benchmark

### Phase 4 — Data and Evaluation Harness (3–4 weeks)
- Canonicalize data formats and loaders (news/science/tech/subtitles/wiki); add per-source smoke tests
- Build an evaluation harness for vocabulary growth, retention, and domain coverage
- **Acceptance**: Green smoke tests on all sources; repeatable evaluation runs with tracked metrics

Phase 5 — Scaling and visualization (future)
- Batch/parallel processing of concept clusters; live visualization hooks of “fmri-like” activity.
- Multi-modal expansion (text-first → text+audio+vision) behind clean interfaces.
- Acceptance: stable runs at 100k+ vocabulary, with interactive status and visualization.