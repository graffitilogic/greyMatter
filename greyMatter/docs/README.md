# greyMatter - Procedural Neural Architecture 🧠

**"A trillion-parameter model in a gigabyte of RAM."**

> **Status**: Active development - Synaptic novelty detection complete! ⚡
> 
> **Latest**: Biological graph traversal replaces broken pattern matching (Jan 2026)

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
- Lazy loading: Max 10 clusters in memory, unload after 30 min inactivity
- VQ-VAE clustering: Learned codebook groups similar patterns
- Hebbian synapses: "Neurons that fire together, wire together"
- Sparse connectivity: Only meaningful connections stored (>90% sparsity)

**Performance**
- Constant memory: 20-25 MB regardless of training duration
- Fast processing: ~470 concepts/sec on real data
- MessagePack storage: 60% smaller than JSON, 1,350x faster saves
- 10+ hour stability: No crashes, consistent performance

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

**User Guides:**
- **[SYNAPTIC_NOVELTY_DETECTION.md](SYNAPTIC_NOVELTY_DETECTION.md)** - How novelty detection works ⭐
- **[PRODUCTION_TRAINING_GUIDE.md](PRODUCTION_TRAINING_GUIDE.md)** - Running production training
- **[QUERY_GUIDE.md](QUERY_GUIDE.md)** - Testing and querying the brain

**Technical Details:**
- **[MASSIVE_DATASET_ACTIVATION.md](MASSIVE_DATASET_ACTIVATION.md)** - 571GB Wikipedia integration
- **[TECHNICAL_DETAILS.md](TECHNICAL_DETAILS.md)** - Architecture deep dive
- **ADPC_PHASE*.md** - Implementation history (archived)

## 🎯 Current Status

✅ **Complete & Working:**
- Biological novelty detection (synaptic graph traversal)
- Massive dataset training (571GB Wikipedia + 500GB books)
- Progressive curriculum with LLM teacher
- Procedural neural generation
- Lazy loading and efficient storage

🚀 **Ready For:**
- Production-scale training to build strong synaptic pathways
- Extended training runs (10+ hours proven stable)
- Real-world knowledge acquisition

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

**Last Updated**: January 14, 2026  
**Latest Achievement**: ✅ Synaptic novelty detection - biological graph traversal replaces broken pattern matching!
