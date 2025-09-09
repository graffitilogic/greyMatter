# greyMatter - Biologically-Inspired Neural Learning System 🧠

> **Vision**: Procedural generation meets neuroscience - creating dynamic, efficient neural structures that scale through biological principles, not brute force.

## 🎯 What is greyMatter?

greyMatter is an experimental neural learning system inspired by biological brain architecture. Instead of traditional static neural networks, it creates **ephemeral neural clusters** that are procedurally generated on-demand. The central much like how No Man's Sky renders infinite worlds within the player's "render distance."

**Core Philosophy / Question**: Can higher level cognition emerge through complex interactions between specialized, short-lived neural structures rather than through massive parameter counts or computational brute force.

## 🚀 Quick Start

### Prerequisites
- .NET SDK 8.0+
- Git client

### Setup & Run
```bash
# Clone and build
git clone https://github.com/graffitilogic/greyMatter.git
cd greyMatter/greyMatter
dotnet build

# Quick demo (1k sentences)
dotnet run -- --tatoeba-hybrid-1k

# LLM-Guided Learning (auto-converts data if needed)
dotnet run -- --llm-teacher

# Enhanced learning with TrainingService (requires data prep)
dotnet run -- --enhanced-learning --brain-path ~/brainData --max-words 5000

# Performance validation
dotnet run -- --performance-validation
```

### Key Commands
```bash
# Learning & Training (TrainingService-based)
dotnet run -- --enhanced-learning --brain-path ~/brainData --max-words 5000
dotnet run -- --llm-teacher                   # LLM-guided learning (auto-converts data)
dotnet run -- --performance-validation        # System performance tests

# Manual Data Preparation (optional - auto-done if needed)
dotnet run -- --convert-tatoeba-data          # Prepare Tatoeba dataset
dotnet run -- --convert-enhanced-data         # Prepare enhanced training data

# Legacy Learning Commands  
dotnet run -- --tatoeba-hybrid-1k
dotnet run -- --tatoeba-hybrid-complete
dotnet run -- --language-random-sample 1000

# Analysis & Debugging  
dotnet run -- --reading-comprehension
dotnet run -- --debug
dotnet run -- --evaluate
```

## 🧬 Core Architecture

### TrainingService - Unified Learning Interface (NEW!)
Centralized service that replaced 80+ scattered demo classes:
- **Parameterized Training**: `RunTatoebaTrainingAsync()`, `RunLLMTeacherSessionAsync()`
- **Performance Validation**: `RunPerformanceValidationAsync()`
- **Configuration Management**: Unified parameter classes for all training modes
- **Result Tracking**: Standardized training results and metrics

### LLM-Guided Continuous Learning System
**True continuous learning**: LLM provides learning guidance with automatic data preparation:
- **Auto-Data Conversion**: Automatically converts Tatoeba data if not already prepared
- **Interactive Mode**: Real-time LLM guidance with background learning
- **Guided Training**: LLM develops learning strategy and executes with available datasets
- **Multi-Source Potential**: Framework exists for scientific abstracts, social media, technical docs
- **Status Commands**: `status`, `focus <topic>` commands for live interaction

**✅ Now Truly Continuous**: No separate data prep step required - handles conversion automatically

**LLM Teacher Configuration:**
```bash
# External Ollama API endpoint
http://192.168.69.138:11434/api/chat

# Model: deepseek-r1:1.5b
# Optimized for structured learning guidance and JSON responses
```

### Ephemeral Neural Clusters
- **Dynamic Allocation**: Neural structures created on-demand for specific cognitive tasks
- **Shared Neurons**: Concepts share neural resources (like Venn diagrams)
- **FMRI-like Visualization**: Real-time brain activity visualization
- **Biological Fatigue**: Neurons experience realistic usage patterns

### Procedural Generation
- **Cortical Columns**: Specialized processing units generated as needed
- **Minimal Persistence**: Only essential patterns stored, details procedurally regenerated
- **Semantic Clustering**: Related concepts grouped in biological-style "brain regions"

## 📊 Current Status (December 2024)

### ✅ Working Components
- **TrainingService**: Unified training interface replacing 80+ demo classes
- **True Continuous Learning**: LLM-guided learning with automatic data conversion
- **Data Processing**: Multi-source conversion (TatoebaDataConverter, EnhancedDataIntegrator)
- **Simple Brain**: SimpleEphemeralBrain with shared neurons
- **Storage System**: SemanticStorageManager with Huth-inspired semantic domains
- **Performance Optimization**: FastStorageAdapter (1,350x speedup demonstrated)
- **Background Processing**: ContinuousLearner with integrated data preparation
- **Visualization**: Brain scan visualization tools

### 🚀 Recent Major Updates
- **Architecture Refactor**: 84+ demo classes → TrainingService with parameterized methods
- **True Continuous Learning**: Integrated data prep into learning pipeline (no separate step)
- **LLM Teacher Integration**: Learning guidance system with external LLM
- **Documentation Consolidation**: 42 scattered .md files → 3 organized documents
- **Zero Compilation Errors**: Production-ready build status achieved

### ⚠️ Known Limitations
- **Save Performance**: 35+ minutes for 5K vocabulary (optimization available with FastStorageAdapter)
- **Scale Testing**: Framework exists, large-scale validation pending  
- **Cross-Column Communication**: Framework only, not fully operational

### 🎯 Honest Performance Metrics
- **Processing**: 8-10 concepts/second
- **Load Time**: ~3 minutes for 100MB brain state
- **Storage Optimization**: 1,350x improvement with FastStorageAdapter
- **Memory Efficiency**: O(active_concepts) scaling achieved

## 🏗️ Technical Foundation

### Key Classes
- **`TrainingService`**: Unified parameterized training interface (replaces 80+ demos)
- **`Cerebro`**: Central learning orchestrator
- **`SimpleEphemeralBrain`**: Core ephemeral neural cluster implementation
- **`SemanticStorageManager`**: Biologically-inspired storage with semantic domains
- **`ContinuousLearner`**: Background continuous learning with integrated data preparation
- **`EnhancedDataIntegrator`**: Multi-source data processing and conversion pipeline
- **`LLMTeacher`**: Intelligent continuous learning guidance system (auto-converts data)
- **`ProceduralCorticalColumnGenerator`**: On-demand neural structure creation

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

### Current Experiments
- **Dynamic Scale**: Can procedural generation overcome traditional ML scale limitations?
- **Emergence**: Do complex behaviors emerge from simple neural interactions?
- **Efficiency**: Is biological inspiration more resource-efficient than brute force?
- **Teacher Integration**: How can LLMs enhance biological learning patterns?

### Future Directions
- **Multi-Column Communication**: Complex inter-region brain communication
- **Attention Systems**: Biological attention and focus mechanisms  
- **Memory Consolidation**: Sleep-like processing for knowledge integration
- **Goal-Directed Behavior**: Emergent problem-solving capabilities

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

**Bottom Line**: greyMatter is a proof-of-concept that biological inspiration + procedural generation might achieve human-level cognition more efficiently than massive parameter models. We're experimenting with ideas, not claiming production readiness.