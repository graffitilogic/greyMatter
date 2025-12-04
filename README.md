# greyMatter - Procedural Neural Architecture 🧠

**"A trillion-parameter model in a gigabyte of RAM."**

> **Status**: Active development - Massive dataset activation complete
> 
> **Latest**: ✅ 571GB Wikipedia, books, and LLM teacher fully integrated (Nov 2025)

## 🎯 What Actually Works (Dec 2025)

**Pattern-Based Neural Architecture** ✅ **Core Principle**
- **No queryable word lists**: Concepts exist as VQ activation patterns, not searchable labels
- **Pattern similarity retrieval**: Query by showing examples, system finds similar patterns in VQ space
- **Biological alignment**: Like human memory - "tip of tongue" from partial patterns, not dictionary lookup
- **VQ-VAE clustering**: 512-code learned codebook groups similar patterns automatically
- **Sparse activation**: Most neurons dormant, only relevant patterns activate (<1% active per query)
- **Procedural generation ready**: Foundation for regenerating neurons from VQ codes + weights

**Massive Dataset Training Infrastructure** ✅
- **571GB Wikipedia corpus**: DirectoryText format, recursive .txt loading
- **500GB book collections**: Narrative structures and storytelling patterns
- **LLM teacher integration**: Ollama deepseek-r1:1.5b generates content on-demand
- **Progressive curriculum**: Simple → News → Dialogue → Books → Wikipedia
- **Smart sampling**: 5000-sentence batches (never exhausts datasets)
- **LLM mixing**: Every 5th batch uses dynamic generation (6 rotating topics)
- **MessagePack checkpoints**: Binary serialization with NaN/Infinity sanitization

**VQ-VAE Production Integration (ADPC-Net Phase 5)** ✅
- Cerebro uses VQ-VAE for all region ID generation (replaces LSH)
- Codebook learns during training (EMA updates with γ=0.99)
- Full persistence: Codebook saves/loads across training sessions
- Similar concepts cluster together (verified in tests)
- Deterministic assignments: Same pattern → same code
- **100% test passing**: All 6 Phase 5 validation tests pass
- **Perplexity growth**: 1.0 → 5.66 (codebook learns patterns)
- **Production ready**: Toggle support for LSH fallback

**VQ-VAE Codebook (ADPC-Net Phase 4)** ✅
- Learned vector quantization: 512-code codebook adapts to data
- Replaces fixed LSH with adaptive learned similarity
- EMA updates: Codebook continuously refines (γ=0.99)
- Perplexity tracking: 209/256 efficiency (81.6%)
- Commitment loss: Prevents encoder drift (β=0.25)
- **100% test passing**: All 6 Phase 4 validation tests pass
- **Utilization**: 94.9% (243/256 codes active)
- **Learned clustering**: 100% similar inputs → same code

**Sparse Synaptic Graph (ADPC-Net Phase 3)** ✅
- Hebbian learning: "Neurons that fire together, wire together"
- Sparse storage: Dictionary-based (O(E) not O(N²))
- Automatic pruning: Weak synapses removed below threshold
- Synaptic decay: Forgetting mechanism (0.99 default)
- **100% test passing**: All 6 Phase 3 validation tests pass
- **Sparsity**: >90% (only meaningful connections stored)

**Dynamic Neuron Generation (ADPC-Net Phase 2)** ✅
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
- Long-term training stability: 10+ hours, no crashes
- NAS integration: Checkpoint persistence via MessagePack
- Pattern encoding: ~470 concepts/sec (CPU-bound, NAS I/O bottleneck)
- Progressive curriculum: 6-phase learning pipeline operational
- VQ-based clustering: Automatic pattern grouping, 24K+ clusters learned
- **Current bottleneck**: NAS I/O for checkpoint writes, not CPU
- **Next**: GPU port for VQ encoding after concept proven in .NET

**Neural Realism Progress** ✅ **COMPLETE (All 5 Phases)**
- ✅ **Pattern-based retrieval** (replaces word list lookup)
- ✅ **Feature encoding** (128-dim vectors from text)
- ✅ **LSH clustering** (locality-sensitive hashing for similarity)
- ✅ **Novelty tracking** (activation statistics)
- ✅ **Dynamic neuron allocation** (hypernetwork generation)
- ✅ **Sparse synaptic graph** (Hebbian learning, pruning, decay)
- ✅ **VQ-VAE codebook** (learned vector quantization, EMA updates)
- ✅ **Production integration** (VQ-VAE in Cerebro training pipeline) **NEW**

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
- **[PRODUCTION_TRAINING_GUIDE.md](PRODUCTION_TRAINING_GUIDE.md)** - Quick start for massive-scale training ⭐ **NEW**
- **[TECHNICAL_DETAILS.md](TECHNICAL_DETAILS.md)** - Implementation details


### Project Status
- **Architecture**: Cerebro (procedural generation foundation) ✅ Implemented
- **Pattern Learning**: ADPC-Net Phase 1 (feature-based) ✅ Complete & Validated
- **Dynamic Neurons**: ADPC-Net Phase 2 (hypernetwork) ✅ Complete & Validated
- **Sparse Synapses**: ADPC-Net Phase 3 (Hebbian learning) ✅ Complete & Validated
- **VQ-VAE Codebook**: ADPC-Net Phase 4 (learned quantization) ✅ Complete & Validated
- **VQ-VAE Integration**: ADPC-Net Phase 5 (production) ✅ Complete & Validated
- **Massive Datasets**: 571GB Wikipedia + Books + LLM ✅ Activated
- **Training**: Production service with progressive curriculum ✅ Operational
- **Storage**: MessagePack persistence with corruption recovery ✅ Working
- **Pattern Retrieval**: VQ similarity-based (no word list lookup) ✅ By design
- **Next Phase**: Procedural neuron regeneration (save VQ codes, regenerate structure) ⏳
- **Then**: GPU port (CUDA/C) after .NET prototype proven ⏳
- **Validation**: Generalization test (novel pattern combinations) 🔲 Planned

## 🚀 Quick Start

```bash
# Production training (massive datasets - 571GB Wikipedia + Books + LLM)
dotnet run -- --production-training

# Brain statistics (VQ clusters, neuron counts, pattern distribution)
dotnet run -- --cerebro-query stats

# Pattern similarity query (shows which VQ codes activate for input)
dotnet run -- --cerebro-query think "red apple"

# Build project
dotnet build
```

**Note**: This is a pattern-based system, not a word dictionary. Queries work by pattern similarity in VQ space, not keyword lookup. Think "Google image search by example" not "grep for exact word".

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

**Last Updated**: December 2, 2025  
**Latest Achievement**: ✅ Realigned to pattern-based biological architecture - removed word-list assumptions, embracing VQ similarity retrieval as core feature!
