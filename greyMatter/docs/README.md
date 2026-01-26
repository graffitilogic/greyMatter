# greyMatter - Procedural Neural Architecture 🧠

**"A trillion-parameter model in a gigabyte of RAM."**

> **Status**: Production-ready architecture - All memory management phases complete! 🚀
> 
> **Latest**: Phase 4 complete - LRU cluster eviction ensures bounded memory (Jan 25, 2026)

## 🎯 What Actually Works (January 2026)

**Biological Novelty Detection** ⭐
- Memory stored as synaptic connections between neurons (not hash lookups)
- Recognition through graph traversal of trained pathways
- Novelty emerges from cascade depth: trained concepts propagate deeply, garbage dies immediately
- Test: "neural networks" cascades through synapses, "qawsedrftg" activates nothing
- **See**: [SYNAPTIC_NOVELTY_DETECTION.md](SYNAPTIC_NOVELTY_DETECTION.md) for implementation details

**Massive-Scale Training**
- **571GB Wikipedia** + **500GB books** directly from NAS storage
- **LLM teacher** generates dynamic content (Ollama deepseek-r1:1.5b)
- Progressive curriculum: Basic → News → Dialogue → Books → Wikipedia
- Smart sampling: 5K sentence batches, never loads full datasets
- Checkpoints every 10 minutes with NaN/Infinity sanitization

**Neural Architecture**
- Procedural generation: Neurons created on-demand, not pre-allocated
- LRU cluster cache: Max 800 clusters, automatic eviction when full
- Background eviction: Removes idle clusters every 5 minutes (30 min idle threshold)
- VQ-VAE clustering: Learned codebook groups similar patterns
- Hebbian synapses: "Neurons that fire together, wire together"
- Sparse connectivity: Only meaningful connections stored (>90% sparsity)
- Partitioned synaptic storage: 256 partitions, handles 133M+ synapses without OOM

**Performance**
- Bounded memory: O(active_set) not O(total_data) - max 800 clusters in RAM
- Fast processing: ~470 concepts/sec on real data
- MessagePack storage: 60% smaller than JSON, 1,350x faster saves
- 10+ hour stability: No crashes, consistent performance
- Streaming synapse saves: 133M synapses in ~10 minutes (52% faster, no OOM)
- Automatic eviction: LRU cache prevents unbounded growth, ready for 24/7 training

## 🏗️ Architecture

### Core Components

**Cerebro** (`Core/Cerebro.cs` - 2,474 lines)
- Procedural SBIJ orchestrator
- LRU cluster cache: Max 800 clusters with automatic eviction
- Background eviction loop: Checks every 5 min, evicts after 30 min idle
- Procedurally generates neurons on-demand
- Graceful persistence before eviction
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

**User Guides:**
- **[SYNAPTIC_NOVELTY_DETECTION.md](SYNAPTIC_NOVELTY_DETECTION.md)** - How novelty detection works ⭐
- **[PRODUCTION_TRAINING_GUIDE.md](PRODUCTION_TRAINING_GUIDE.md)** - Running production training
- **[QUERY_GUIDE.md](QUERY_GUIDE.md)** - Testing and querying the brain

**Technical Details:**
- **[TECHNICAL_DETAILS.md](TECHNICAL_DETAILS.md)** - Complete architecture documentation
- **[SYNAPTIC_PROPAGATION_IMPLEMENTATION.md](SYNAPTIC_PROPAGATION_IMPLEMENTATION.md)** - Wave traversal implementation
- **[BIOLOGICAL_ALIGNMENT.md](BIOLOGICAL_ALIGNMENT.md)** - Biological fidelity principles

**Implementation History:**
- **[PHASE_6B_COMPLETION_SUMMARY.md](PHASE_6B_COMPLETION_SUMMARY.md)** - Procedural storage completion
- **[PRODUCTION_TRAINING_FIXES.md](PRODUCTION_TRAINING_FIXES.md)** - Production improvements

## 🎯 Current Status

✅ **Architecture Complete (Phases 1, 3, 4):**
- Phase 1: Fixed neuron-synapse ID mismatch (Jan 19)
- Phase 3: Partitioned synaptic storage - 256 partitions, 133M synapses, no OOM (Jan 23)
- Phase 4: LRU cluster eviction - bounded memory, automatic eviction (Jan 25)
- Biological novelty detection (synaptic graph traversal)
- Massive dataset training (571GB Wikipedia + 500GB books)
- Progressive curriculum with LLM teacher

🚀 **Ready For:**
- Extended validation: 8+ hour training runs
- Production-scale 24/7 training (memory bounded, automatic eviction)
- Real-world knowledge acquisition at scale

## 🚀 Quick Start

```bash
# Test novelty detection
dotnet run -- --cerebro-query think "neural networks"  # Lower novelty (trained)
dotnet run -- --cerebro-query think "qawsedrftg"       # High novelty (garbage)

# Production training (571GB Wikipedia + Books + LLM)
dotnet run -- --production-training                    # Run indefinitely (24/7 mode)
dotnet run -- --production-training --duration 3600    # Run for 1 hour (3600 seconds)
dotnet run -- --production-training --duration 7200    # Run for 2 hours
dotnet run -- --production-training --llm-teacher      # Enable LLM teacher (every 5th batch)

# Query trained knowledge
dotnet run -- --cerebro-query stats              # Show brain statistics
dotnet run -- --cerebro-query think "cat"        # Query a concept
dotnet run -- --cerebro-query clusters 50        # List top concepts

# Inspect brain state (fast, no loading)
dotnet run -- --inspect-brain

# Build
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

**Last Updated**: January 25, 2026  
**Latest Achievement**: ✅ Phase 4 complete - LRU cluster eviction ensures bounded memory for 24/7 training!

**Architecture Milestones:**
- Jan 19: Phase 1 - Fixed ID mismatch
- Jan 23: Phase 3 - Partitioned storage (133M synapses, 52% faster, no OOM)
- Jan 25: Phase 4 - LRU eviction (max 800 clusters, automatic eviction)
