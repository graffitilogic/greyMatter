# greyMatter - Procedural Neural Architecture 🧠

> **Status**: Active development - production training phase
> 
> **Architecture**: Procedural generation meets neuroscience - No Man's Sky for neural networks

## 🎯 Core Philosophy

**Procedural Generation Over Static Storage**
- Don't serialize the universe - generate it on-demand from seed functions
- Lazy load neural clusters (max 10 at a time)
- Persist only activation deltas, weights, and patterns
- Rehydrate by procedurally regenerating structure + overlaying learned weights

**Biological Alignment**
- Neurons participate in multiple overlapping clusters (like fMRI activation patterns)
- STM → LTM consolidation (experience-driven permanent learning)
- Adaptive concept capacities (concepts grow based on activation frequency)
- Cluster unloading after inactivity (biological memory pruning)

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
