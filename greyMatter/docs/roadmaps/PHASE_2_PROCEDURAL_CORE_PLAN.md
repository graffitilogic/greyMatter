# Phase 2: Procedural Neural Core - Implementation Plan

**Status**: 🚀 IN PROGRESS  
**Started**: October 4, 2025  
**Target Completion**: 4-6 weeks  
**Goal**: Test "emergence through interaction" hypothesis with column-based architecture

---

## Vision Statement

**Build a biological-inspired neural system where intelligence emerges from interactions between specialized cortical columns, rather than being explicitly programmed.**

Inspired by:
- **No Man's Sky**: Procedural generation with coordinate-based consistency
- **Neocortex**: Cortical columns as fundamental processing units
- **HTM Theory**: Sparse distributed representations and temporal memory
- **Brain Hemispheres**: Specialized processing regions that collaborate

---

## Current State Analysis (Oct 4, 2025)

### ✅ What Exists and Works

**ProceduralCorticalColumnGenerator** (437 lines) - Strong foundation:

1. **5 Column Templates** (types of specialized processors):
   - `phonetic` - Sound-based patterns (vowel, consonant, phoneme)
   - `semantic` - Meaning-based patterns (category, relationship, property)
   - `syntactic` - Structure-based patterns (word order, dependencies)
   - `contextual` - Situation-based patterns (domain, register, tone)
   - `episodic` - Memory-based patterns (temporal, spatial, emotional)

2. **Coordinate-Based Generation** (like No Man's Sky):
   - `SemanticCoordinates` - Domain/Topic/Context positioning
   - Deterministic generation using SHA256 hashing
   - Same coordinates → same column structure

3. **Column Parameters**:
   - Size calculation (512-8192 neurons)
   - Sparsity calculation (1%-5% activation)
   - Complexity multiplier (based on domain/task)
   - Reuse/caching (30-min TTL)

4. **Pattern Generation**:
   - Feature patterns for each column type
   - Rule patterns with clustering (3-7 clusters)
   - Sparse distributed representations (SparsePattern)

5. **Demo Validation**:
   - `ProceduralGenerationDemo.cs` - Shows domain generation works
   - `HybridPersistenceManager` - Hot/cold storage decisions

### ⚠️ What's Missing (Critical Gaps)

1. **No Working Memory System** - Columns can't hold temporary state
2. **No Inter-Column Communication** - Columns are isolated, no messaging
3. **No Attention Mechanism** - No way to focus on relevant columns
4. **No Temporal Context** - No sequence/time tracking between activations
5. **No Connection Rules** - How do columns wire together?
6. **Not Integrated with Learning** - Cerebro/training systems don't use it
7. **No Feedback Loops** - Output doesn't reinforce internal structure
8. **No Evaluation Metrics** - Can't measure if "emergence" is happening

---

## Architecture Design

### Core Hypothesis to Test

**"Intelligence emerges when specialized neural columns interact through working memory, with attention directing information flow and temporal context enabling sequence learning."**

### Key Principles

1. **Specialization**: Each column type processes specific aspects (sound, meaning, structure)
2. **Interaction**: Columns communicate through shared working memory
3. **Attention**: Focus mechanism selects relevant columns for task
4. **Temporal Context**: Sequence information flows between processing steps
5. **Emergence**: Complex behaviors arise from simple column interactions

### System Components

```
┌─────────────────────────────────────────────────────────────┐
│                    Working Memory                           │
│  (Shared state, temporary activations, context)             │
└───────────────┬─────────────────────────────────────────────┘
                │
    ┌───────────┴────────────┐
    │   Attention System     │
    │  (Focus, routing)      │
    └───────────┬────────────┘
                │
┌───────────────┴───────────────────────────────────────────────┐
│              Cortical Column Network                          │
│                                                               │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐    │
│  │Phonetic  │  │Semantic  │  │Syntactic │  │Episodic  │    │
│  │Column    │◄─┤Column    │◄─┤Column    │◄─┤Column    │    │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘    │
│       ▲            ▲             ▲              ▲            │
│       └────────────┴─────────────┴──────────────┘            │
│                    │                                          │
│            ┌───────┴────────┐                                │
│            │ Message Bus    │                                │
│            └────────────────┘                                │
└───────────────────────────────────────────────────────────────┘
```

---

## Implementation Roadmap

### Step 1: Working Memory System (Week 1)

**Goal**: Columns can read/write temporary state

**New Classes**:
```csharp
public class WorkingMemory
{
    // Temporary activation patterns
    public Dictionary<string, SparsePattern> ActivePatterns { get; }
    
    // Decay over time
    public void DecayActivations(double decayRate = 0.1);
    
    // Query patterns by similarity
    public List<(string key, double similarity)> QuerySimilar(SparsePattern query, int topK = 5);
    
    // Context tracking
    public TemporalContext CurrentContext { get; set; }
}

public class TemporalContext
{
    public Queue<string> RecentActivations { get; } // Last N activations
    public DateTime Timestamp { get; set; }
    public string TaskPhase { get; set; } // "encoding", "processing", "retrieval"
}
```

**Integration Points**:
- Add `WorkingMemory` field to `ProceduralCorticalColumn`
- Update `GenerateColumnAsync` to initialize working memory
- Demo: Show pattern persistence and decay

**Success Criteria**:
- ✅ Columns can write patterns to working memory
- ✅ Patterns decay over time (exponential or linear)
- ✅ Can query similar patterns (cosine similarity)
- ✅ Demo shows working memory in action

---

### Step 2: Inter-Column Messaging (Week 2)

**Goal**: Columns can send/receive messages to collaborate

**New Classes**:
```csharp
public class ColumnMessage
{
    public string SenderId { get; set; }
    public string ReceiverId { get; set; }
    public MessageType Type { get; set; } // Excitatory, Inhibitory, Query, Response
    public SparsePattern Payload { get; set; }
    public double Strength { get; set; }
    public DateTime Timestamp { get; set; }
}

public class MessageBus
{
    private Dictionary<string, Queue<ColumnMessage>> _columnInboxes;
    
    // Send message to specific column
    public void SendMessage(ColumnMessage message);
    
    // Broadcast to columns of specific type
    public void Broadcast(string columnType, ColumnMessage message);
    
    // Get messages for column
    public List<ColumnMessage> GetMessages(string columnId, int maxCount = 10);
    
    // Clear old messages
    public void PurgeOldMessages(TimeSpan maxAge);
}

public enum MessageType
{
    Excitatory,  // Activate similar patterns
    Inhibitory,  // Suppress patterns
    Query,       // Request information
    Response,    // Answer query
    Forward      // Pass to next stage
}
```

**Connection Rules** (How columns talk):
```csharp
public class ColumnConnectionRules
{
    // Phonetic → Semantic (sound to meaning)
    // Semantic → Syntactic (meaning to structure)
    // Syntactic → Contextual (structure to context)
    // All → Episodic (for memory storage)
    
    public static List<string> GetDownstreamColumns(string columnType)
    {
        return columnType switch
        {
            "phonetic" => new List<string> { "semantic" },
            "semantic" => new List<string> { "syntactic", "episodic" },
            "syntactic" => new List<string> { "contextual", "episodic" },
            "contextual" => new List<string> { "episodic" },
            "episodic" => new List<string>(),
            _ => new List<string>()
        };
    }
}
```

**Integration Points**:
- Add `MessageBus` to ProceduralCorticalColumnGenerator
- Add `ProcessMessages()` method to columns
- Update demo to show message flow

**Success Criteria**:
- ✅ Columns can send messages to each other
- ✅ Message routing follows connection rules
- ✅ Demo shows phonetic → semantic → syntactic pipeline
- ✅ Can visualize message flow

---

### Step 3: Attention Mechanism (Week 3)

**Goal**: System focuses on relevant columns for current task

**New Classes**:
```csharp
public class AttentionSystem
{
    // Track column activation levels
    private Dictionary<string, double> _columnActivations;
    
    // Calculate attention scores
    public Dictionary<string, double> ComputeAttentionScores(
        WorkingMemory memory,
        TaskRequirements task);
    
    // Focus on top-K columns
    public List<string> FocusColumns(int topK = 5);
    
    // Boost/suppress columns
    public void ModulateColumn(string columnId, double factor);
    
    // Task-specific attention patterns
    private AttentionProfile _currentProfile;
}

public class AttentionProfile
{
    public Dictionary<string, double> ColumnTypeWeights { get; set; }
    // e.g., "comprehension" task: semantic=0.5, syntactic=0.3, contextual=0.2
}
```

**Integration Points**:
- Add `AttentionSystem` to ProceduralCorticalColumnGenerator
- Update column generation to respect attention scores
- Route messages through attention-weighted paths

**Success Criteria**:
- ✅ Can define task-specific attention profiles
- ✅ Relevant columns get higher activation
- ✅ Message routing prioritizes attended columns
- ✅ Demo shows attention shifting between tasks

---

### Step 4: Temporal Gating (Week 3-4)

**Goal**: Track sequences and temporal dependencies

**New Classes**:
```csharp
public class TemporalGate
{
    // Sequence tracking
    public Queue<ActivationEvent> SequenceHistory { get; }
    
    // Predict next activation
    public SparsePattern PredictNext(int stepsAhead = 1);
    
    // Detect sequences
    public List<ActivationSequence> DetectPatterns(int minLength = 3);
    
    // Context modulation
    public double ContextStrength(string columnId);
}

public class ActivationEvent
{
    public string ColumnId { get; set; }
    public SparsePattern Pattern { get; set; }
    public DateTime Timestamp { get; set; }
    public string TaskContext { get; set; }
}
```

**Integration Points**:
- Add temporal tracking to WorkingMemory
- Update column activation to record sequences
- Enable prediction-based messaging

**Success Criteria**:
- ✅ System records activation sequences
- ✅ Can predict next activation pattern
- ✅ Temporal context influences column selection
- ✅ Demo shows sequence learning

---

### Step 5: Integration with Cerebro/Training (Week 4-5)

**Goal**: Training systems use procedural columns

**Changes to Existing Code**:
```csharp
// TatoebaLanguageTrainer.cs
public class TatoebaLanguageTrainer
{
    private readonly ProceduralCorticalColumnGenerator _columnGenerator;
    private readonly WorkingMemory _workingMemory;
    private readonly MessageBus _messageBus;
    
    // Process sentence through column pipeline
    public async Task ProcessSentenceAsync(string sentence)
    {
        // 1. Phonetic column - sound patterns
        var phoneticColumn = await _columnGenerator.GenerateColumnAsync(...);
        var phoneticPattern = phoneticColumn.ProcessInput(sentence);
        _workingMemory.Store("phonetic", phoneticPattern);
        
        // 2. Semantic column - meaning extraction
        var semanticColumn = await _columnGenerator.GenerateColumnAsync(...);
        var semanticPattern = await semanticColumn.ProcessInputWithContext(
            sentence, 
            _workingMemory.Get("phonetic"));
        _workingMemory.Store("semantic", semanticPattern);
        
        // 3. Syntactic column - structure analysis
        // ... and so on
    }
}
```

**Integration Tasks**:
1. Add column generator to TatoebaLanguageTrainer
2. Create sentence → column pipeline
3. Store learned patterns in columns
4. Update TrainingService to support column-based learning

**Success Criteria**:
- ✅ Tatoeba training uses column pipeline
- ✅ Learning updates column patterns
- ✅ Performance comparable to current system
- ✅ Can visualize column activations during learning

---

### Step 6: Evaluation & Metrics (Week 5-6)

**Goal**: Measure if "emergence" is happening

**New Metrics**:
```csharp
public class EmergenceMetrics
{
    // Specialization - Are columns developing distinct functions?
    public double ColumnSpecialization { get; set; }
    
    // Interaction - Are columns collaborating effectively?
    public double InterColumnCorrelation { get; set; }
    
    // Emergence - Are complex behaviors appearing?
    public double NovelPatternGeneration { get; set; }
    
    // Efficiency - Is column system better than monolithic?
    public double ProcessingEfficiency { get; set; }
    
    // Generalization - Do columns generalize to new tasks?
    public double CrossTaskTransfer { get; set; }
}

public class EmergenceEvaluator
{
    public async Task<EmergenceMetrics> EvaluateSystemAsync(
        ProceduralCorticalColumnGenerator generator,
        WorkingMemory memory,
        List<TestTask> tasks);
}
```

**Evaluation Tasks**:
1. Measure column specialization (pattern divergence over time)
2. Measure interaction effectiveness (message success rate)
3. Detect novel pattern generation (unexpected combinations)
4. Compare column vs monolithic performance
5. Test cross-task transfer

**Success Criteria**:
- ✅ Can quantify specialization, interaction, emergence
- ✅ Column system outperforms baseline on some tasks
- ✅ Evidence of emergent behaviors
- ✅ Clear metrics for future improvement

---

## Success Criteria (Phase 2 Complete)

### Must Have ✅
1. **Working Memory** - Columns share temporary state
2. **Inter-Column Messages** - Columns communicate
3. **Connection Rules** - Clear pipeline: phonetic → semantic → syntactic → episodic
4. **Basic Attention** - Task-specific column focus
5. **Integration** - At least one trainer uses columns (TatoebaLanguageTrainer)
6. **Metrics** - Can measure emergence

### Nice to Have 🎯
1. **Temporal Prediction** - Sequence learning working
2. **Adaptive Connections** - Column wiring evolves with learning
3. **Visualization** - Can see column activations live
4. **Multiple Trainers** - Enhanced and Real learners use columns

### Evidence of Success 🎉
1. **Specialization** - Columns develop distinct activation patterns
2. **Collaboration** - Message flow shows meaningful information exchange
3. **Emergence** - System exhibits behaviors not explicitly programmed
4. **Performance** - Column system matches or exceeds current performance
5. **Generalization** - Columns transfer learning across tasks

---

## Risk Mitigation

### High Risk
1. **Performance Overhead** - Column coordination could be slow
   - **Mitigation**: Profile early, optimize hot paths, cache aggressively
   
2. **Complexity Explosion** - Too many interconnections
   - **Mitigation**: Start with simple pipeline, add complexity incrementally

3. **No Emergent Behavior** - Hypothesis might be wrong
   - **Mitigation**: Define clear tests early, iterate quickly, fail fast

### Medium Risk
1. **Integration Disruption** - Breaking existing trainers
   - **Mitigation**: Keep current trainers working, columns as opt-in initially

2. **Debugging Difficulty** - Hard to understand column interactions
   - **Mitigation**: Build visualization early, extensive logging

### Low Risk
1. **Storage Requirements** - Many columns = more storage
   - **Mitigation**: Column caching, hot/cold tiering already in place

---

## Timeline (6-Week Plan)

| Week | Focus | Deliverables |
|------|-------|--------------|
| 1 | Working Memory | WorkingMemory class, decay, similarity queries, demo |
| 2 | Inter-Column Messaging | MessageBus, ColumnMessage, connection rules, pipeline demo |
| 3 | Attention System | AttentionSystem, attention profiles, routing demo |
| 4 | Temporal Gating | TemporalGate, sequence tracking, prediction |
| 5 | Cerebro Integration | TatoebaLanguageTrainer uses columns, pipeline working |
| 6 | Evaluation & Metrics | EmergenceMetrics, evaluation harness, final results |

---

## Next Immediate Actions

1. ✅ Create this roadmap document (COMPLETE)
2. **Create WorkingMemory class** (Start Week 1)
3. Write unit tests for working memory
4. Update ProceduralGenerationDemo to show working memory
5. Commit progress: "Phase 2 Step 1: Working Memory foundation"

---

## References & Inspiration

- **Hierarchical Temporal Memory (HTM)** - Sparse distributed representations, temporal pooling
- **Neocortex** - Cortical columns, specialized regions, inter-column communication
- **No Man's Sky** - Procedural generation with coordinate consistency
- **Attention Mechanisms** - Transformer-style attention for routing
- **Dynamic Neural Modules** - Composing specialized networks

---

## Open Questions

1. **How much working memory?** - Size/capacity limits? Decay rates?
2. **Message bandwidth?** - How many messages per timestep?
3. **Attention granularity?** - Per-column or per-pattern?
4. **Learning rate?** - How fast should columns adapt?
5. **Column lifetime?** - When to retire unused columns?

These will be answered empirically as we build and test.

---

**Status**: Ready to begin Week 1 - Working Memory implementation
**Next**: Create `Core/WorkingMemory.cs` with pattern storage and decay
