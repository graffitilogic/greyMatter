# greyMatter: Production System Plan
**Created**: November 10, 2025  
**Purpose**: Stop the demo circus, build a real production system

---

## 🚨 Current Problems (User Feedback)

### 1. Storage Chaos
**Problem**: Data scattered everywhere instead of centralized storage
- ❌ `./continuous_learning_demo/checkpoints`
- ❌ `./attention_showcase_memory`
- ❌ `./demo_episodic_memory`
- ❌ `./episodic_memory`

**Reality Check**:
- NAS (`/Volumes/jarvis/brainData`): **Empty** (last modified Nov 7)
- Desktop (`/Users/billdodd/Desktop/Cerebro/brainData`): **Sept 18** (2 months old!)
- **Nothing is being persisted long-term**

**Root Cause**: Every demo hardcodes its own temporary directory

### 2. Demo-Focused vs Production-Focused
**Problem**: Loosely organized cluster of demos, not a cohesive system

**Current State**:
- 15+ demo files
- Each runs 30-60 seconds
- Data thrown away after each run
- No continuity between runs
- No long-term training
- No production deployment path

**What's Missing**:
- ❌ Multi-day training runs
- ❌ Checkpoint validation
- ❌ Knowledge persistence
- ❌ Rehydration on restart
- ❌ Production monitoring
- ❌ Automated testing at checkpoints

### 3. Knowledge Utility & Measurability
**Problem**: Even if we accumulate knowledge, we can't:
- Query it effectively
- Measure its quality
- Validate retention over time
- Use it for inference
- Compare before/after

---

## ✅ Production System Requirements

### 1. Centralized Storage Architecture

#### Primary Storage Hierarchy:
```
/Users/billdodd/Desktop/Cerebro/brainData/  (Fast SSD - active work)
├── live/                                    # Current training state
│   ├── vocabulary.json                      # 18KB (fast writes)
│   ├── synapses.json                        # 257MB (periodic flush)
│   ├── cluster_index.json                   # 122KB
│   └── neurons/                             # Active neuron pools
│
├── checkpoints/                             # Hourly snapshots
│   ├── 2025-11-10_14-00/                   # Timestamped
│   │   ├── vocabulary.json
│   │   ├── synapses.json
│   │   └── metadata.json                    # Stats, timestamp, sentence count
│   └── latest -> 2025-11-10_14-00          # Symlink
│
├── episodic_memory/                         # Week 7 episodic events
│   └── episodes.jsonl                       # Append-only log
│
└── metrics/                                 # Performance tracking
    ├── training_rate.csv                    # Sentences/sec over time
    ├── vocabulary_growth.csv                # Words learned vs time
    └── memory_usage.csv                     # RAM/disk over time

/Volumes/jarvis/brainData/                   (NAS - long-term archive)
├── archives/                                # Daily backups
│   ├── 2025-11-10/                         # Full snapshot
│   └── 2025-11-09/
│
└── training_logs/                           # Historical records
    └── 2025-11/
        ├── nov_10.log
        └── nov_09.log
```

#### Storage Strategy:
1. **Active work**: Fast SSD (`Desktop/Cerebro/brainData`)
2. **Hourly checkpoints**: SSD (keep last 24)
3. **Daily archives**: NAS (keep all)
4. **Async offload**: Background thread copies to NAS every 6 hours

### 2. Production Training Pipeline

#### Long-Term Training Service:
```bash
# Start production training
./scripts/start_production_training.sh

Configuration:
  - Duration: 7 days (24/7 operation)
  - Data source: /Volumes/jarvis/trainData/Tatoeba/sentences.csv
  - Checkpoint interval: 1 hour
  - Validation interval: 6 hours
  - Brain data: /Users/billdodd/Desktop/Cerebro/brainData
  - Archive: /Volumes/jarvis/brainData
  
Training phases:
  1. Load latest checkpoint (or start fresh)
  2. Train continuously with auto-save
  3. Hourly checkpoint + metrics
  4. 6-hour validation test
  5. Daily archive to NAS
  
Status:
  - Monitor: ./scripts/monitor_training.sh
  - Pause: ./scripts/control_training.sh --pause
  - Resume: ./scripts/control_training.sh --resume
  - Stop: ./scripts/control_training.sh --stop
```

#### Validation at Checkpoints:
Every 6 hours, automatically run:
1. **Retention test**: Sample 1000 previously learned sentences
   - Success: Can recall vocabulary
   - Measure: Accuracy percentage

2. **Generalization test**: New unseen sentences with known words
   - Success: Forms correct associations
   - Measure: Pattern detection rate

3. **Knowledge query test**: Ask specific questions
   - "What words are associated with 'quantum'?"
   - "What patterns involve 'learning'?"
   - Measure: Relevance of responses

4. **Performance regression**: Compare to baseline
   - Memory usage within bounds?
   - Processing speed maintained?
   - Checkpoint size reasonable?

### 3. Knowledge Persistence & Rehydration

#### On Startup:
```csharp
// Production mode: Always load from checkpoint
var brain = new LanguageEphemeralBrain();
var storage = new SemanticStorageManager(
    brainDataPath: "/Users/billdodd/Desktop/Cerebro/brainData"
);

// Load latest checkpoint
var checkpoint = storage.LoadLatestCheckpoint();
if (checkpoint != null)
{
    brain.LoadVocabulary(checkpoint.Vocabulary);
    brain.LoadSynapses(checkpoint.Synapses);
    brain.LoadClusters(checkpoint.Clusters);
    
    Console.WriteLine($"✅ Rehydrated from checkpoint: {checkpoint.Timestamp}");
    Console.WriteLine($"   Vocabulary: {checkpoint.VocabularySize:N0} words");
    Console.WriteLine($"   Synapses: {checkpoint.SynapseCount:N0} connections");
    Console.WriteLine($"   Training time: {checkpoint.TotalHours:F1} hours");
}

// Continue training from where we left off
await trainingService.ContinueTrainingAsync();
```

#### On Checkpoint:
```csharp
// Every hour during training
await storage.SaveCheckpointAsync(new Checkpoint
{
    Timestamp = DateTime.Now,
    Vocabulary = brain.GetVocabulary(),
    Synapses = brain.GetSynapses(),
    Clusters = brain.GetClusters(),
    Metadata = new CheckpointMetadata
    {
        SentencesProcessed = sentenceCount,
        TrainingHours = totalHours,
        VocabularySize = brain.VocabularySize,
        AverageTrainingRate = sentencesPerSecond,
        MemoryUsageGB = Process.GetCurrentProcess().WorkingSet64 / 1e9
    }
});

// Archive to NAS every 6 hours
if (hoursSinceLastArchive >= 6)
{
    await storage.ArchiveToNASAsync(nasPath: "/Volumes/jarvis/brainData");
}
```

### 4. Knowledge Querying & Measurement

#### Query API:
```csharp
public interface IKnowledgeQuery
{
    // Basic queries
    Task<List<string>> GetAssociatedWords(string word);
    Task<List<Pattern>> GetPatterns(string word);
    Task<double> GetAssociationStrength(string word1, string word2);
    
    // Advanced queries
    Task<KnowledgeGraph> GetSubgraph(string centerWord, int depth);
    Task<List<Concept>> GetConcepts(string topic);
    Task<List<Episode>> GetEpisodicMemory(DateTime start, DateTime end);
    
    // Metrics
    Task<KnowledgeMetrics> GetKnowledgeMetrics();
}

public class KnowledgeMetrics
{
    public int VocabularySize { get; set; }
    public int TotalConnections { get; set; }
    public double AverageConnectivityDegree { get; set; }
    public int ConceptCount { get; set; }
    public int EpisodesRecorded { get; set; }
    public double KnowledgeDensity { get; set; }  // connections / vocabulary
}
```

#### Testing Knowledge Quality:
```bash
# Run knowledge validation suite
./scripts/test_knowledge.sh --checkpoint latest

Tests:
  1. Vocabulary recall (1000 random words)
  2. Association accuracy (500 word pairs)
  3. Pattern recognition (100 new sentences)
  4. Episodic memory (query temporal context)
  5. Knowledge graph connectivity (measure centrality)

Results:
  ✅ Vocabulary recall: 98.7% (987/1000)
  ✅ Association accuracy: 94.2% (471/500)
  ✅ Pattern recognition: 89.1% (89/100)
  ✅ Episodic recall: 100% (all episodes queryable)
  ✅ Graph connectivity: 4.7 avg degree (healthy)
  
Overall: PASS (89.7% average accuracy)
```

---

## 📋 Implementation Plan

### Phase 1: Storage Unification (Days 1-2)
**Goal**: Single source of truth for all brain data

**Tasks**:
1. Create `ProductionStorageManager` class
   - Enforces path conventions
   - Manages checkpoint lifecycle
   - Handles NAS archival
   
2. Update all classes to use centralized paths
   - `ContinuousLearningService`
   - `IntegratedTrainer`
   - `EpisodicMemorySystem`
   - Remove all hardcoded `./demo_*` paths

3. Migration script
   - Move existing data to proper locations
   - Validate data integrity
   - Clean up scattered demo directories

**Acceptance Criteria**:
- ✅ All data writes to `/Users/billdodd/Desktop/Cerebro/brainData`
- ✅ Daily archives to `/Volumes/jarvis/brainData`
- ✅ No more `./anything` relative paths in production code
- ✅ Migration script tested and run

### Phase 2: Production Training Service (Days 3-4)
**Goal**: 24/7 learning with automatic validation

**Tasks**:
1. Create `ProductionTrainingService` class
   - Loads from latest checkpoint
   - Runs continuous training
   - Hourly checkpoints
   - 6-hour validation cycles
   - Daily NAS archival

2. Create control scripts
   - `start_production_training.sh`
   - `monitor_training.sh`
   - `control_training.sh` (pause/resume/stop)
   - `test_knowledge.sh`

3. Implement automatic validation
   - Retention tests
   - Generalization tests
   - Performance regression detection

**Acceptance Criteria**:
- ✅ Service runs for 24+ hours without intervention
- ✅ Checkpoints saved every hour
- ✅ Validation runs every 6 hours
- ✅ Data archived to NAS daily
- ✅ Can restart and resume from checkpoint

### Phase 3: Knowledge Query System (Days 5-6)
**Goal**: Make accumulated knowledge useful

**Tasks**:
1. Implement `IKnowledgeQuery` interface
   - Word associations
   - Pattern queries
   - Episodic memory search
   - Knowledge metrics

2. Create query CLI tool
   - `./scripts/query_brain.sh "quantum"`
   - `./scripts/brain_stats.sh`
   - `./scripts/visualize_knowledge.sh`

3. Knowledge validation suite
   - Automated testing of knowledge quality
   - Benchmark against known-good datasets
   - Track quality metrics over time

**Acceptance Criteria**:
- ✅ Can query learned associations
- ✅ Can retrieve episodic memories
- ✅ Can generate knowledge graph visualizations
- ✅ Validation suite passes with >85% accuracy

### Phase 4: Production Monitoring (Day 7)
**Goal**: Visibility into long-term operation

**Tasks**:
1. Metrics collection
   - Training rate (sentences/sec)
   - Vocabulary growth
   - Memory usage
   - Checkpoint sizes

2. Dashboard creation
   - Real-time training status
   - Historical performance charts
   - Knowledge growth visualization
   - System health indicators

3. Alerting
   - Training rate degradation
   - Memory leaks
   - Checkpoint failures
   - NAS connectivity issues

**Acceptance Criteria**:
- ✅ Metrics logged every 5 minutes
- ✅ Dashboard shows real-time status
- ✅ Alerts trigger on anomalies
- ✅ Historical data queryable

---

## 🎯 Success Criteria (End of Week)

### Storage:
- ✅ Single centralized brain data location
- ✅ Hourly checkpoints working
- ✅ Daily NAS archival working
- ✅ No data scattered in demo directories

### Training:
- ✅ 7-day continuous training run completed
- ✅ 168 hourly checkpoints created
- ✅ 28 validation cycles passed
- ✅ Can restart and resume from any checkpoint

### Knowledge:
- ✅ 100K+ sentences processed
- ✅ 50K+ vocabulary learned
- ✅ 1M+ synaptic connections
- ✅ >85% validation accuracy maintained

### Production:
- ✅ Service runs as launchd daemon
- ✅ Auto-restarts on failure
- ✅ Monitoring dashboard operational
- ✅ Query tools functional

### Measurability:
- ✅ Knowledge metrics calculated hourly
- ✅ Quality tests pass at all checkpoints
- ✅ Historical trends visualized
- ✅ Regression detection working

---

## 💣 What Gets Deprecated

### Demos to Archive:
- ❌ `AttentionShowcase.cs` → Archive to `demos/archive/`
- ❌ `Week7ContinuousDemo.cs` → Archive
- ❌ `AttentionEpisodicDemo.cs` → Archive
- ❌ `ContinuousLearningDemo.cs` → Replaced by production service

### Keep Only:
- ✅ `ProductionTrainingService.cs` (the real deal)
- ✅ Integration tests (automated validation)
- ✅ Benchmark tests (performance regression)

### Philosophy Change:
**Before**: "Let's build a demo to show X works"  
**After**: "Does this make the production system better?"

---

## 📊 Week 1 Deliverable

At the end of this week, you should be able to:

```bash
# Start 7-day training
./scripts/start_production_training.sh --days 7

# Check status anytime
./scripts/monitor_training.sh
# Output:
# Day 3 of 7 - 72 hours elapsed
# Processed: 2,147,483 sentences
# Vocabulary: 47,329 words (growing)
# Training rate: 28.4 sent/sec (stable)
# Last checkpoint: 23 minutes ago (✅)
# Last validation: 2.3 hours ago (✅ 89.2% accuracy)
# Memory usage: 4.2GB (healthy)
# NAS archive: 6 hours ago (✅)

# Query what was learned
./scripts/query_brain.sh "quantum"
# Associated words: physics (0.92), entanglement (0.87), mechanics (0.83)...
# Patterns: 47 patterns involving "quantum"
# Episodes: First seen Day 1 at 14:23, reinforced 127 times

# Test knowledge quality
./scripts/test_knowledge.sh
# Running validation suite...
# ✅ Vocabulary recall: 98.7%
# ✅ Association accuracy: 94.2%
# ✅ Pattern recognition: 89.1%
# Overall: PASS (94.0% accuracy)

# View dashboard
open monitoring/dashboard.html
# Shows graphs of vocabulary growth, training rate, memory, etc.
```

**This is what a production system looks like.**

---

## Next Steps

1. **User approval**: Do these priorities align with your goals?
2. **Implementation**: Start Phase 1 (Storage Unification) immediately
3. **Timeline**: Complete all 4 phases in 7 days
4. **Outcome**: Real production system, not demo circus

**Ready to build something real?** 🚀
