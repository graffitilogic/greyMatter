# GreyMatter Training Quickstart Guide

## Overview

GreyMatter is an advanced language learning system that combines biological neural learning with semantic classification. This guide explains the available training options and their expected outcomes.

## Available Training Options

### 1. Enhanced Language Learning Runner (Phase 4) 🚀
**Command:** `dotnet run --project greyMatter.csproj -p:StartupObject=GreyMatter.EnhancedLearningRunner [target_vocab] [batch_size] [concurrency]`

**Purpose:** Large-scale vocabulary expansion using the enhanced language learning system.

**Parameters:**
- `target_vocab`: Target vocabulary size (default: 5000)
- `batch_size`: Words processed per batch (default: 500)
- `concurrency`: Maximum concurrent threads (default: 4)

**Expected Outcomes:**
- Scales vocabulary to target size
- Processes learning data from NAS storage
- Uses biological neural learning with semantic guidance
- Outputs progress and completion statistics

**Example:**
```bash
dotnet run --project greyMatter.csproj -p:StartupObject=GreyMatter.EnhancedLearningRunner 10000 1000 8
```

### 2. Hybrid Training Demo 🎯
**Command:** `dotnet run --project greyMatter.csproj HybridTrainingDemo.cs`

**Purpose:** Demonstrates hybrid training combining Cerebro's biological learning with semantic classification guidance.

**Expected Outcomes:**
- Initializes hybrid training system
- Demonstrates semantic classification capabilities
- Runs small-scale and medium-scale batch training
- Shows integration between biological and semantic learning
- Provides performance metrics and training statistics

### 3. Phase 2 Integration Demo 📚
**Command:** `dotnet run --project greyMatter.csproj Phase2IntegrationDemo.cs`

**Purpose:** Complete language understanding pipeline demonstration.

**Expected Outcomes:**
- Initializes integrated learning system
- Runs complete language understanding pipeline
- Demonstrates enhanced capabilities
- Performs performance evaluation
- Shows vocabulary growth and sentence learning metrics

### 4. Developmental Learning Demo 🌱
**Command:** `dotnet run --project greyMatter.csproj DevelopmentalLearningDemo.cs [library_path]`

**Purpose:** Simulates child-like learning progression from guided to autonomous learning.

**Parameters:**
- `library_path`: Path to digital library (defaults to NAS brain data)

**Expected Outcomes:**
- Sets up digital library for exploration
- Runs multiple learning sessions across developmental stages
- Shows progression from guided to autonomous learning
- Demonstrates resource collection and cataloging
- Tracks learning age and autonomy level

### 5. Tatoeba Hybrid Integration Demo 🌍
**Command:** `dotnet run --project greyMatter.csproj TatoebaHybridIntegrationDemo.cs`

**Purpose:** Real-world hybrid training using the Tatoeba dataset.

**Expected Outcomes:**
- Initializes integrated system with Tatoeba data
- Validates data access and preprocessing
- Runs baseline training comparison
- Performs hybrid training with real-world data
- Demonstrates semantic classification on authentic sentences

### 6. Phase 2 Quick Test ⚡
**Command:** `dotnet run --project greyMatter.csproj Phase2QuickTest.cs`

**Purpose:** Fast integration testing of the Phase 2 system.

**Expected Outcomes:**
- Quick initialization of integrated learning system
- Linguistic analysis testing
- Vocabulary integration testing
- Fast validation of core functionality
- Basic performance metrics

### 7. Reading Comprehension Demo 📖
**Command:** `dotnet run --project greyMatter.csproj ReadingComprehensionRunner.cs`

**Purpose:** Demonstrates advanced reading comprehension with episodic memory for narrative understanding and question answering.

**Expected Outcomes:**
- Initializes episodic memory system for temporal event storage
- Processes sample documents with entity extraction and topic analysis
- Demonstrates question answering with confidence scoring
- Shows interactive memory exploration capabilities
- Provides narrative chain building and context tracking
- Outputs comprehension metrics and answer accuracy statistics

**Features:**
- Episodic memory for storing and retrieving narrative events
- Text processing pipeline with semantic analysis
- Confidence-based question answering (up to 1.00 confidence)
- Interactive exploration of learned knowledge
- Persistent storage of comprehension data

## Data Storage and Persistence

### NAS Configuration
All training options use NAS storage for data persistence:
- **Brain Data:** `/Volumes/jarvis/brainData`
- **Training Data:** `/Volumes/jarvis/trainData`

### Persistence Status: ✅ FIXED
**Previous Issue:** Vocabulary did not accumulate across consecutive runs
**Solution:** Fixed persistence mechanisms including:
- Cerebro brain initialization with existing data
- SemanticStorageManager loading existing vocabulary
- Proper saving of learned knowledge and neural concepts
- Loading of previously learned words and relationships

**Current Behavior:** 
- ✅ Vocabulary accumulates across runs
- ✅ Neural concepts and relationships are preserved
- ✅ Learning builds upon previous sessions
- ✅ System loads existing data on startup

### Expected Accumulation
When running the same training option multiple times:
- **Reading Comprehension:** Episodic memory and narrative understanding accumulate
- **Enhanced Learning Runner:** Vocabulary grows incrementally
- **Hybrid Training Demo:** Neural connections strengthen
- **Phase 2 Integration:** Language patterns accumulate
- **Developmental Learning:** Knowledge builds progressively

## Performance Benchmarks

### Available Benchmark Options
- **Performance Benchmarks:** `dotnet run PerformanceBenchmarkRunner.cs`
- **Simple Performance Benchmark:** `dotnet run --project greyMatter.csproj SimpleBenchmarkRunner.cs`
- **MessagePack Performance Demo:** `dotnet run --project greyMatter.csproj MessagePackDemoRunner.cs`

## Getting Started

1. **Ensure NAS Storage:** Verify `/Volumes/jarvis/` is mounted
2. **Check Data:** Confirm training data exists in `/Volumes/jarvis/trainData/learning_data/`
3. **Run Initial Training:** Start with Phase 2 Quick Test for validation
4. **Try Reading Comprehension:** Run the Reading Comprehension Demo to see advanced narrative understanding
5. **Scale Up:** Use Enhanced Learning Runner for large-scale training

## Troubleshooting

### Common Issues
- **Data Path Not Found:** Ensure NAS is mounted and data exists
- **Compilation Errors:** Check namespace consistency (GreyMatter vs greyMatter)
- **Memory Issues:** Reduce batch size or concurrency for large vocabularies
- **Storage Errors:** Verify write permissions on NAS paths

### Validation Commands
```bash
# Check NAS mount
ls /Volumes/jarvis/

# Verify data exists
ls /Volumes/jarvis/trainData/learning_data/

# Test basic compilation
dotnet build greyMatter.csproj
```

## Expected Performance

- **Phase 2 Quick Test:** < 30 seconds
- **Reading Comprehension Demo:** 1-2 minutes
- **Enhanced Learning (5K vocab):** 2-5 minutes
- **Hybrid Training Demo:** 1-3 minutes
- **Developmental Learning:** 5-10 minutes
- **Tatoeba Integration:** 3-8 minutes (depending on dataset size)

Performance varies based on hardware, data size, and NAS I/O speed.
