# greyMatter - Procedural Neural Architecture 🧠

**"A trillion-parameter model in a gigabyte of RAM."**

> **Status**: Active development - Massive dataset activation complete
> 
> **Latest**:  571GB Wikipedia, books, and LLM teacher fully integrated (Nov 2025)

## 🎯 What Actually Works (Nov 2025)

**Massive Dataset Training Infrastructure**  **NEW**
- **571GB Wikipedia corpus**: DirectoryText format, recursive .txt loading
- **500GB book collections**: Narrative structures and storytelling patterns
- **LLM teacher integration**: Ollama deepseek-r1:1.5b generates content on-demand
- **Progressive curriculum**: Simple → News → Dialogue → Books → Wikipedia
- **Smart sampling**: 5000-sentence batches (never exhausts datasets)
- **LLM mixing**: Every 5th batch uses dynamic generation (6 rotating topics)
- **Fixed checkpoints**: NaN/Infinity sanitization prevents JSON crashes
- **Fixed queries**: Direct ConceptLabel lookup with case-insensitive matching
- **See**: MASSIVE_DATASET_ACTIVATION.md and PRODUCTION_TRAINING_GUIDE.md

**VQ-VAE Production Integration (ADPC-Net Phase 5)** 
- Cerebro uses VQ-VAE for all region ID generation (replaces LSH)
- Codebook learns during training (EMA updates with γ=0.99)
- Full persistence: Codebook saves/loads across training sessions
- Similar concepts cluster together (verified in tests)
- Deterministic assignments: Same pattern → same code
- **100% test passing**: All 6 Phase 5 validation tests pass
- **Perplexity growth**: 1.0 → 5.66 (codebook learns patterns)
- **Production ready**: Toggle support for LSH fallback

**VQ-VAE Codebook (ADPC-Net Phase 4)** 
- Learned vector quantization: 512-code codebook adapts to data
- Replaces fixed LSH with adaptive learned similarity
- EMA updates: Codebook continuously refines (γ=0.99)
- Perplexity tracking: 209/256 efficiency (81.6%)
- Commitment loss: Prevents encoder drift (β=0.25)
- **100% test passing**: All 6 Phase 4 validation tests pass
- **Utilization**: 94.9% (243/256 codes active)
- **Learned clustering**: 100% similar inputs → same code

**Sparse Synaptic Graph (ADPC-Net Phase 3)** 
- Hebbian learning: "Neurons that fire together, wire together"
- Sparse storage: Dictionary-based (O(E) not O(N²))
- Automatic pruning: Weak synapses removed below threshold
- Synaptic decay: Forgetting mechanism (0.99 default)
- **100% test passing**: All 6 Phase 3 validation tests pass
- **Sparsity**: >90% (only meaningful connections stored)

**Dynamic Neuron Generation (ADPC-Net Phase 2)** 
- Hypernetwork formula: `N = α*log(freq) + β*novelty + γ*complexity`
- Variable neuron counts: 5-500 per cluster (not fixed!)
- Pattern-driven allocation: Complex patterns get more neurons
- Deterministic generation: Same pattern → same neuron count
- **100% test passing**: All 6 Phase 2 validation tests pass
- **Observed range**: 82-97 neurons (vs Phase 1: all ~64)

**Pattern-Based Learning (ADPC-Net Phase 1)** 
- Feature encoding: 128-dim vectors (deterministic, reproducible)
- LSH partitioning: Efficient region-based clustering
- Novelty detection: Activation statistics track pattern familiarity
- Storage: Region mappings and activation stats persist correctly
- **100% test passing**: All 6 Phase 1 validation tests pass
- **No word list cheating**: Pattern similarity drives retrieval

**Infrastructure (Production-Ready)** 
- Long-term training stability: 10+ hours, no crashes, constant memory (20-25 MB)
- NAS integration: Hourly checkpoints save/load successfully
- Fast processing: ~470 concepts/sec sustained on real Tatoeba data
- Progressive curriculum: 4-phase learning pipeline operational
- Cluster partitioning: On-demand loading prevents memory bloat

**Neural Realism Progress**  **COMPLETE (All 5 Phases)**
-  **Pattern-based retrieval** (replaces word list lookup)
-  **Feature encoding** (128-dim vectors from text)
-  **LSH clustering** (locality-sensitive hashing for similarity)
-  **Novelty tracking** (activation statistics)
-  **Dynamic neuron allocation** (hypernetwork generation)
-  **Sparse synaptic graph** (Hebbian learning, pruning, decay)
-  **VQ-VAE codebook** (learned vector quantization, EMA updates)
-  **Production integration** (VQ-VAE in Cerebro training pipeline) **NEW**

**See ADPC_PHASE5_COMPLETE.md for Phase 5 details and test results.**

## 🏗️ Architecture

### Core Components

**Cerebro** (`Core/Cerebro.cs` - 1,398 lines)
- Procedural SBIJ orchestrator
- Lazy loading: Max 10 clusters loaded at once
- Procedurally generates neurons on-demand
- Unloads clusters after 30 minutes of inactivity
- STM → LTM consolidation

**EnhancedBrainStorage** (`Storage/EnhancedBrainStorage.cs`)
- Cerebro's persistence layer
- Partitioned cluster storage with gzip compression
- Lazy loading and efficient delta persistence
- Designed for procedural regeneration

**BinaryStorageManager** (`Storage/BinaryStorageManager.cs`)
- MessagePack serialization (2-10x compression vs JSON)
- Efficient binary format for neuron data
- Performance-optimized for large-scale persistence

### Training Pipeline

**ProductionTrainingService** (`Core/ProductionTrainingService.cs`)
- 24/7 continuous learning from NAS datasets
- Progressive 4-phase curriculum (children's stories → scientific papers)
- Automatic checkpoint management
- Diverse content formats: dialogue, narrative, technical, scientific

**TrainingDataProvider** (`Core/TrainingDataProvider.cs`)
- **571GB Wikipedia**: `/Volumes/jarvis/trainData/txtDump/cache/epub`
- **500GB Books**: `/Volumes/jarvis/trainData/books`
- **LLM-generated**: Dynamic content via Ollama (science, history, tech, nature, culture, philosophy)
- **685MB Tatoeba**: Full sentences for foundation training
- **39MB News**: Headlines and journalism
- **DirectoryText format**: Recursive .txt file loading for massive corpora
- No data copying - direct NAS access with smart sampling (5000 sentences/batch)

## 📖 Documentation

### Essential Reading
- **[MASSIVE_DATASET_ACTIVATION.md](MASSIVE_DATASET_ACTIVATION.md)** - 571GB Wikipedia + LLM integration ⭐ **NEW**
- **[PRODUCTION_TRAINING_GUIDE.md](PRODUCTION_TRAINING_GUIDE.md)** - Quick start for massive-scale training ⭐ **NEW**
- **[ADPC_PHASE5_COMPLETE.md](ADPC_PHASE5_COMPLETE.md)** - VQ-VAE production integration ⭐
- **[ADPC_PHASE4_COMPLETE.md](ADPC_PHASE4_COMPLETE.md)** - VQ-VAE codebook (learned vector quantization) ⭐
- **[ADPC_PHASE3_COMPLETE.md](ADPC_PHASE3_COMPLETE.md)** - Sparse synaptic graph (Hebbian learning) ⭐
- **[ADPC_PHASE2_COMPLETE.md](ADPC_PHASE2_COMPLETE.md)** - Dynamic neuron generation ⭐
- **[ADPC_PHASE1_COMPLETE.md](ADPC_PHASE1_COMPLETE.md)** - Pattern-based learning implementation ⭐
- **[TECHNICAL_DETAILS.md](TECHNICAL_DETAILS.md)** - Implementation details
- **[docs/QUICK_START.md](docs/QUICK_START.md)** - Getting started guide

### Project Status
- **Architecture**: Cerebro (procedural generation)  Implemented
- **Pattern Learning**: ADPC-Net Phase 1 (feature-based)  Complete & Validated
- **Dynamic Neurons**: ADPC-Net Phase 2 (hypernetwork)  Complete & Validated
- **Sparse Synapses**: ADPC-Net Phase 3 (Hebbian learning)  Complete & Validated
- **VQ-VAE Codebook**: ADPC-Net Phase 4 (learned quantization)  Complete & Validated
- **VQ-VAE Integration**: ADPC-Net Phase 5 (production)  Complete & Validated
- **Massive Datasets**: 571GB Wikipedia + Books + LLM  Activated **NEW**
- **Training**: Production service with progressive curriculum  Operational
- **Storage**: EnhancedBrainStorage + BinaryStorageManager  Clean
- **Query System**: Direct ConceptLabel lookup  Working
- **Next**: Scale testing with full 571GB corpus ⏳ In progress

## 🚀 Quick Start

```bash
# Test ADPC-Net Phase 1 (pattern-based learning)
dotnet run -- --adpc-test

# Test ADPC-Net Phase 2 (dynamic neuron generation)
dotnet run -- --adpc-phase2-test

# Test ADPC-Net Phase 3 (sparse synaptic graph)
dotnet run -- --adpc-phase3-test

# Test ADPC-Net Phase 4 (VQ-VAE codebook)
dotnet run -- --adpc-phase4-test

# Test ADPC-Net Phase 5 (VQ-VAE production integration) ⭐ NEW
dotnet run -- --adpc-phase5-test

# Production training (massive datasets - 571GB Wikipedia + Books + LLM)
dotnet run -- --production-training

# Knowledge query (with direct ConceptLabel lookup)
dotnet run -- CerebroQueryCLI.cs
# Query: red
# Query: technology

# Build project
dotnet build
```

## 🔬 Research Principles

**No Man's Sky Approach**
- Procedurally generate neural structures from concept seeds
- Render only what's needed for current scope
- Persist activation patterns, not complete structures
- Scale to millions of concepts without memory explosion

**Validation Standards**
- Test everything before claiming completion
- Measure memory usage, checkpoint sizes, training rates
- Validate biological alignment (overlapping clusters, STM→LTM)
- Evidence over claims

---

## 🤝 Contributing

This is experimental research at the intersection of neuroscience, procedural generation, and systems programming. All code is in active flux - expect breaking changes.

## 📜 License

MIT License - See LICENSE file for details

---

**Last Updated**: November 18, 2025  
**Latest Achievement**:  Massive dataset activation - 571GB Wikipedia, 500GB books, and LLM teacher fully integrated!
