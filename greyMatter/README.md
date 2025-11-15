# greyMatter - Procedural Neural Architecture 🧠

> **Status**: Active development - infrastructure working, neural realism needs work
> 
> **Current Reality**: Stable training pipeline with hash-table-based storage (see ARCHITECTURE_REALITY_CHECK.md)

## 🎯 What Actually Works (Nov 14, 2025)

**Infrastructure (Production-Ready)** ✅
- Long-term training stability: 10+ hours, no crashes, constant memory (20-25 MB)
- NAS integration: Hourly checkpoints save/load successfully
- Fast processing: ~470 concepts/sec sustained on real Tatoeba data
- Progressive curriculum: 4-phase learning pipeline operational
- Cluster partitioning: On-demand loading prevents memory bloat

**Neural Realism (Needs Work)** ⚠️
- ❌ Uses cluster_index.json for concept lookup (word list, not pattern matching)
- ❌ Fixed neuron bucket sizes (439 clusters with exactly 503 neurons - not biological)
- ❌ Deterministic neuron allocation (same concept → same count via hash)
- ❌ 1:1 concept→cluster mapping (no distributed representations)
- ✅ cluster_index.json kept as DEBUG SIDECAR for testing (not integrated into retrieval)

**See ARCHITECTURE_REALITY_CHECK.md for honest assessment and roadmap.**

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
- **[ARCHITECTURE_AUDIT.md](ARCHITECTURE_AUDIT.md)** - Architectural principles and cleanup plan
- **[TECHNICAL_DETAILS.md](TECHNICAL_DETAILS.md)** - Implementation details
- **[docs/QUICK_START.md](docs/QUICK_START.md)** - Getting started guide

### Project Status
- **Architecture**: Cerebro (procedural generation) ✅ Implemented
- **Training**: Production service with diverse NAS data ✅ Operational
- **Storage**: EnhancedBrainStorage + BinaryStorageManager ✅ Clean
- **Query System**: Knowledge inspection CLI ⏳ In progress

## 🚀 Quick Start

```bash
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

**Last Updated**: November 13, 2025
