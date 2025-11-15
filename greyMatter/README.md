# greyMatter - Procedural Neural Architecture 🧠

> **Status**: Active development - ADPC-Net Phase 2 complete, dynamic neuron generation working
> 
> **Latest**: ✅ Hypernetwork-driven neuron allocation (Nov 14, 2025)

## 🎯 What Actually Works (Nov 14, 2025)

**Dynamic Neuron Generation (ADPC-Net Phase 2)** ✅ **NEW**
- Hypernetwork formula: `N = α*log(freq) + β*novelty + γ*complexity`
- Variable neuron counts: 5-500 per cluster (not fixed!)
- Pattern-driven allocation: Complex patterns get more neurons
- Deterministic generation: Same pattern → same neuron count
- **100% test passing**: All 6 Phase 2 validation tests pass
- **Observed range**: 82-97 neurons (vs Phase 1: all ~64)

**Pattern-Based Learning (ADPC-Net Phase 1)** ✅
- Feature encoding: 128-dim vectors (deterministic, reproducible)
- LSH partitioning: Efficient region-based clustering
- Novelty detection: Activation statistics track pattern familiarity
- Storage: Region mappings and activation stats persist correctly
- **100% test passing**: All 6 Phase 1 validation tests pass
- **No word list cheating**: Pattern similarity drives retrieval

**Infrastructure (Production-Ready)** ✅
- Long-term training stability: 10+ hours, no crashes, constant memory (20-25 MB)
- NAS integration: Hourly checkpoints save/load successfully
- Fast processing: ~470 concepts/sec sustained on real Tatoeba data
- Progressive curriculum: 4-phase learning pipeline operational
- Cluster partitioning: On-demand loading prevents memory bloat

**Neural Realism Progress** 🚧
- ✅ **Pattern-based retrieval** (replaces word list lookup)
- ✅ **Feature encoding** (128-dim vectors from text)
- ✅ **LSH clustering** (locality-sensitive hashing for similarity)
- ✅ **Novelty tracking** (activation statistics)
- ✅ **Dynamic neuron allocation** (hypernetwork generation) **NEW**
- ⏳ Distributed representations (Phase 3: sparse synaptic graph)

**See ADPC_PHASE1_COMPLETE.md for implementation details and test results.**

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
- Reads from NAS: `/Volumes/jarvis/trainData/`
- 685MB Tatoeba corpus (full sentences)
- Format-appropriate chunking (paragraphs vs lines vs sentences)
- No data copying - direct NAS access

## 📖 Documentation

### Essential Reading
- **[ADPC_PHASE1_COMPLETE.md](ADPC_PHASE1_COMPLETE.md)** - Pattern-based learning implementation ⭐ NEW
- **[ADPC_TESTING_SUMMARY.md](ADPC_TESTING_SUMMARY.md)** - Test results and bugs fixed ⭐ NEW
- **[ARCHITECTURE_AUDIT.md](ARCHITECTURE_AUDIT.md)** - Architectural principles and cleanup plan
- **[TECHNICAL_DETAILS.md](TECHNICAL_DETAILS.md)** - Implementation details
- **[docs/QUICK_START.md](docs/QUICK_START.md)** - Getting started guide

### Project Status
- **Architecture**: Cerebro (procedural generation) ✅ Implemented
- **Pattern Learning**: ADPC-Net Phase 1 (feature-based) ✅ Complete & Validated
- **Training**: Production service with diverse NAS data ✅ Operational
- **Storage**: EnhancedBrainStorage + BinaryStorageManager ✅ Clean
- **Query System**: Knowledge inspection CLI ⏳ In progress
- **Next**: Phase 2 - Hypernetwork neuron generation ⏳ Planned

## 🚀 Quick Start

```bash
# Test ADPC-Net Phase 1 (pattern-based learning)
dotnet run -- --adpc-test

# Test ADPC-Net Phase 2 (dynamic neuron generation)
dotnet run -- --adpc-phase2-test

# Production training (continuous learning)
dotnet run -- --production-training --duration 28800  # 8 hours

# Knowledge query
dotnet run -- --query stats
dotnet run -- --query associations <word>

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

**Last Updated**: November 14, 2025  
**Latest Achievement**: ✅ ADPC-Net Phase 2 complete - Hypernetwork dynamic neuron generation working!
