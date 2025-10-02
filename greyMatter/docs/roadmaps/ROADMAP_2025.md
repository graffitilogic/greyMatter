# greyMatter Development Roadmap 2025
**Last Updated**: October 2, 2025

## 🎯 Central Question & Philosophy

**Core Philosophy**: Can higher level cognition emerge through complex interactions between specialized, short-lived neural structures rather than through massive parameter counts or computational brute force?

**Inspiration**: "If we can simulate entire galaxies through common-seed procedural functions (albeit at lower fidelity with limited local-scoping), why can't we overcome scale limitations with neural networks through similar concepts?"

**Desired End-State**: A trained ML system that:
- Uses dynamically scaled neural structures for learning and cognition
- Hydrates cortical columns procedurally during tasks with minimal persistence
- Regenerates structures on-demand during recognition/response
- Runs continuously (not dormant): re-evaluating, consolidating, testing ideas

## 📊 Current State Assessment (October 2025)

### ✅ Foundational Systems (Working)

#### 1. **Ephemeral Neural Clusters** (Core Architecture)
- **SimpleEphemeralBrain**: ~300 lines implementing shared neuron pools between related concepts
- **Venn Diagram Overlap**: Concepts share neurons (e.g., "cat" and "dog" share animal neurons)
- **On-Demand Activation**: O(active_concepts) memory scaling
- **Status**: ✅ **Proven Concept** - Production validated with batch learning

#### 2. **Procedural Cortical Column Generation** (Framework Exists)
- **ProceduralCorticalColumnGenerator**: Template-based column generation inspired by No Man's Sky
- **Column Types**: Phonetic, Semantic, Syntactic, Contextual, Episodic templates defined
- **Coordinate-Based Generation**: Semantic coordinates → consistent neural structures
- **Status**: ⚠️ **Framework Only** - Templates exist but not integrated into learning pipeline
- **Gap**: No active use in learn/recognize cycles; regeneration semantics undefined

#### 3. **LLM-Guided Learning System** (Revolutionary Implementation)
- **External LLM API**: Ollama endpoint with deepseek-r1:1.5b model
- **Intelligent Analysis**: Real-time learning state analysis and strategy recommendations
- **Multi-Source Integration**: 8+ data sources with LLM-guided selection
- **Interactive Commands**: `status`, `focus <topic>` for live guidance
- **Status**: ✅ **Production Ready** - Continuous learning sessions validated (126+ min runs)

#### 4. **Storage Architecture** (Migration In Progress)
- **FastStorageAdapter**: MessagePack implementation with 1,350x speedup validated
- **SemanticStorageManager**: Huth-inspired semantic domains (24 primary + 180+ secondary)
- **Biologically-Inspired Structure**: Hippocampus indexing + cortical columns
- **Status**: ⚠️ **Dual System** - FastStorageAdapter exists but legacy paths still active
- **Gap**: Need complete migration and compatibility shim for old storage

#### 5. **Training Infrastructure** (Consolidation Needed)
- **TrainingService**: Unified interface replacing 80+ legacy demos (✅ exists)
- **CerebroConfiguration**: Centralized config with CLI/env overrides (✅ working)
- **Multi-Source Data Provider**: Real dataset enforcement (✅ fail-fast implemented)
- **Status**: ⚠️ **Partial Adoption** - TrainingService exists but many legacy entry points remain
- **Gap**: 22 demo files in `/demos` still active; need systematic retirement

### 🛠️ Systems Needing Work

#### 6. **Biological Neural Features** (Stubs/Frameworks Only)
- **Working Memory**: Concept mentioned in docs; no implementation
- **Inter-Column Communication**: Framework classes exist; no message passing
- **Attention Mechanisms**: Mentioned in roadmaps; no gating implementation
- **Temporal Binding**: No time-based concept association system
- **Status**: ❌ **Not Implemented** - Critical gap for "emergence through interaction" goal

#### 7. **Continuous Cognition** (Partial Implementation)
- **ContinuousProcessor**: Exists with ethical drive system and background processing
- **EpisodicMemorySystem**: Framework for event storage exists
- **Status**: ⚠️ **Partial** - Background processing works but lacks cross-column integration

#### 8. **Evaluation & Validation** (Ad-Hoc)
- **Performance Validation**: CLI command exists via TrainingService
- **Learning Metrics**: Vocabulary growth, concept counts tracked
- **Status**: ⚠️ **Basic** - No standardized evaluation harness or retention tests
- **Gap**: Need repeatable benchmarks for retention, transfer, domain coverage

## 🗺️ Development Roadmap

### Phase 0: Foundation Cleanup (Now → 2 weeks) ⏳ IN PROGRESS
**Goal**: Single canonical path for learn/validate; clean error messages; reduced code duplication

#### Tasks:
- [x] Remove synthetic/static fallbacks (✅ MultiSourceLearningDataProvider hardened)
- [x] Normalize configuration (✅ CerebroConfiguration centralized)
- [x] Add dataset verification (✅ `--verify-training-data` CLI command)
- [ ] **Route all CLI commands through TrainingService** (IN PROGRESS)
  - Map `--continuous-learning`, `--performance-validation`, `--llm-teacher` to service methods
  - Retire legacy Program.cs entry points incrementally
- [ ] **Create demo retirement plan**
  - Audit 22 demo files: identify unique functionality vs. duplicates
  - Move unique functionality to TrainingService methods
  - Archive/delete redundant demos
- [ ] **Document canonical training flow**
  - Update README with single recommended path
  - Clear examples: CLI → TrainingService → Storage/Learning

#### Acceptance Criteria:
- ✅ One TrainingService interface for all training modes
- ✅ Data path issues fail fast with actionable messages
- [ ] ≤5 active demo files (vs. current 22)
- [ ] Build succeeds with zero errors (currently: 0 errors, 131 warnings ✅)

---

### Phase 1: Storage & Persistence (2-4 weeks)
**Goal**: Fast, reliable, versioned storage with clear migration path from legacy systems

#### Tasks:
1. **Complete FastStorageAdapter Migration**
   - Grep all `SemanticStorageManager` usage; replace with FastStorageAdapter
   - Create `LegacyStorageShim` for backward compatibility during transition
   - Add adapter selection flag in CerebroConfiguration
   
2. **Add Schema Versioning**
   - Implement `StorageSchemaVersion` in saved files
   - Add migration handlers for version mismatches
   - Version 1.0: Current FastStorageAdapter format
   
3. **Implement Integrity Checks**
   - Add checksum validation on load
   - Detect corrupted files and suggest recovery
   - Log storage health metrics
   
4. **Periodic Snapshots & Quick-Restore**
   - Auto-snapshot every N concepts learned
   - Keep last 3 snapshots for rollback
   - Restore from snapshot in <5 seconds for 50k concepts

#### Acceptance Criteria:
- Save/load completes in <5 seconds for 50-100k concepts
- Legacy storage reads convert automatically to new format
- Corruption detection catches ≥95% of file damage
- Performance validation task passes with new storage

---

### Phase 2: Procedural Neural Core (4-6 weeks) 🎯 CRITICAL
**Goal**: Demonstrate "No Man's Sky-style" neural generation with measurable reuse/regeneration

#### Tasks:
1. **Solidify Column Templates**
   - Review existing templates (phonetic, semantic, syntactic, contextual, episodic)
   - Add connection rules: how columns link to each other
   - Define regeneration semantics: what persists vs. what regenerates
   
2. **Implement Working Memory API**
   ```csharp
   public interface IWorkingMemory
   {
       Task<ActiveConcept> ActivateAsync(string conceptId);
       Task<void> DeactivateAsync(string conceptId);
       IEnumerable<ActiveConcept> GetActiveConcepts();
       Task<WorkingMemoryStats> GetStatsAsync();
   }
   ```
   - Track active concepts (like L1 cache)
   - LRU eviction when capacity exceeded
   - Integration with ephemeral brain activation
   
3. **Inter-Column Messaging Primitives**
   ```csharp
   public interface IColumnMessaging
   {
       Task SendMessageAsync(string fromColumn, string toColumn, Message message);
       Task<Message> ReceiveMessageAsync(string columnId, TimeSpan timeout);
       Task BroadcastAsync(string fromColumn, Message message);
   }
   ```
   - Simple fan-in/fan-out message passing
   - Asynchronous delivery with queues
   - Message types: activation, inhibition, query, response
   
4. **Basic Attention/Temporal Gating**
   ```csharp
   public class AttentionGate
   {
       double Calculate AttentionScore(Message message, BrainState state);
       bool ShouldPass(Message message, double threshold);
   }
   ```
   - Simple threshold-based gating
   - Attention driven by recency + relevance
   - Integration with working memory capacity
   
5. **Procedural Integration**
   - Wire ProceduralCorticalColumnGenerator into Cerebro learning pipeline
   - On concept learn: generate column → activate → persist minimal signature
   - On concept recall: regenerate column from signature → activate → use
   - Measure: % of data regenerated vs. loaded from disk

#### Acceptance Criteria:
- ✅ Column templates have clear regeneration rules (documented)
- ✅ Working memory API functional with LRU eviction
- ✅ Message passing between 2+ columns in demo
- ✅ Attention gate filters messages based on relevance threshold
- ✅ Measurable regeneration: e.g., 70% of neural structure regenerated, 30% persisted
- ✅ Unit tests: regeneration produces consistent structures from same seed

---

### Phase 3: LLM Teacher Maturation (Parallel, 3-5 weeks)
**Goal**: Reliable, measurable LLM guidance with clear contracts and telemetry

#### Tasks:
1. **Define Strict Contracts**
   ```csharp
   public interface ILLMTeacher
   {
       Task<TeacherResponse> AnalyzeLearningStateAsync(
           LearningContext context, 
           CancellationToken ct);
       
       Task<ConceptMapping> ProvideConceptualMappingAsync(
           string word, 
           List<string> context, 
           CancellationToken ct);
       
       Task<CurriculumGuidance> SuggestCurriculumAsync(
           LearningGoals goals, 
           CancellationToken ct);
   }
   ```
   - Clear input validation
   - Structured JSON response schema
   - Confidence scores for all recommendations
   
2. **Add Reliability Guards**
   - Retry policy: 3 attempts with exponential backoff
   - Timeout enforcement: 10s per request
   - Fallback strategy: use last known good strategy if LLM fails
   - Circuit breaker: pause LLM calls if 5 consecutive failures
   
3. **Structured Validation**
   - JSON schema validation on all LLM responses
   - Log malformed responses for analysis
   - Fallback to deterministic strategy on validation failure
   
4. **Telemetry Integration**
   ```csharp
   public class LLMTeacherMetrics
   {
       int TotalRequests;
       int SuccessfulResponses;
       int ValidationFailures;
       int Timeouts;
       double AverageResponseTime;
       Dictionary<string, int> StrategyRecommendations;
   }
   ```
   - Track success rates, latencies, failure modes
   - Log strategy recommendations and outcomes
   - A/B test: LLM-guided vs. deterministic learning
   
5. **Measure Impact**
   - Define benchmark: 1000-word vocabulary acquisition
   - Metrics: time to learn, retention after 24h, transfer to related concepts
   - Compare: LLM-guided vs. random sampling vs. deterministic curriculum
   - Target: ≥20% improvement in retention with LLM guidance

#### Acceptance Criteria:
- ✅ All LLM methods have timeout/retry/validation
- ✅ Telemetry captures request/response/strategy data
- ✅ Benchmark shows measurable improvement (≥20% retention boost)
- ✅ System gracefully handles LLM unavailability
- ✅ Documentation: LLM Teacher API reference with examples

---

### Phase 4: Data & Evaluation Harness (3-4 weeks)
**Goal**: Repeatable benchmarks; smoke tests for all data sources; standardized evaluation

#### Tasks:
1. **Canonicalize Data Formats**
   - Document expected format for each source (news, science, tech, wiki, subtitles, CBT, enhanced)
   - Add format validation at load time
   - Clear error messages for format violations
   
2. **Per-Source Smoke Tests**
   ```csharp
   public class DataSourceSmokeTests
   {
       [Test] void NewsLoader_ParsesHeadlines();
       [Test] void ScientificLoader_ParsesAbstracts();
       [Test] void TechnicalLoader_ParsesDocs();
       [Test] void WikiLoader_ParsesXML();
       [Test] void SubtitlesLoader_ParsesSRT();
       [Test] void CBTLoader_ParsesCBT();
       [Test] void EnhancedLoader_ParsesJSON();
   }
   ```
   - Each test loads 10 samples and validates structure
   - Checks: unique words, diversity, parsing errors
   - Runs in CI pipeline on data path changes
   
3. **Build Evaluation Harness**
   ```csharp
   public class EvaluationHarness
   {
       VocabularyGrowthMetrics MeasureGrowth(int trainingWords);
       RetentionMetrics MeasureRetention(TimeSpan delay);
       DomainCoverageMetrics MeasureDomainCoverage();
       TransferMetrics MeasureTransfer(string sourceDomain, string targetDomain);
   }
   ```
   - Vocabulary growth: words learned per minute, plateau detection
   - Retention: % recall after 1h, 24h, 7d delays
   - Domain coverage: distribution across 24 semantic domains
   - Transfer: learning in one domain improves performance in related domain
   
4. **Standardized Benchmark Suites**
   - **Tiny**: 100 words, 5min, quick validation
   - **Small**: 1000 words, 30min, core metrics
   - **Medium**: 10k words, 3h, domain coverage
   - **Large**: 50k words, 12h, scale + retention
   
5. **Repeatable Reporting**
   - JSON output format for all metrics
   - Time-series tracking: compare runs over time
   - Markdown summary report
   - Optional: charts/graphs for visualization

#### Acceptance Criteria:
- ✅ All data sources have passing smoke tests
- ✅ Evaluation harness runs on demand via CLI: `dotnet run -- --evaluate`
- ✅ Benchmark results reproducible within ±5% variance
- ✅ Metrics tracked over time in `/logs/evaluation/` directory
- ✅ Documentation: data format specs + evaluation metrics guide

---

### Phase 5: Scaling & Visualization (Future, 6-8 weeks)
**Goal**: Stable operation at 100k+ vocabulary; live visualization; multi-modal preparation

#### Tasks:
1. **Batch/Parallel Processing**
   - Parallelize concept cluster loading/saving
   - Batch neural activations across multiple concepts
   - Worker pool for concurrent column generation
   
2. **FMRI-Like Visualization**
   - Real-time heatmap of active cortical columns
   - Concept activation intensity over time
   - Inter-column message flow diagram
   - Web-based dashboard (optional: WebSockets + D3.js)
   
3. **Multi-Modal Preparation**
   - Define interfaces for audio, visual, sensor data
   - Text remains primary; other modalities follow later
   - Clean separation: `IModalityAdapter<TInput, TFeatures>`
   
4. **Scale Testing**
   - 100k vocabulary: stable memory, no crashes
   - 1M vocabulary: performance characterization
   - Long-running sessions: 48h+ continuous learning
   
5. **Interactive Status & Control**
   - CLI dashboard: live stats, active concepts, column usage
   - Manual intervention: pause, inspect, resume
   - Debugging hooks: breakpoints on concept activation

#### Acceptance Criteria:
- ✅ System runs stably with 100k+ vocabulary for 48h+
- ✅ Visualization shows real-time brain activity
- ✅ Multi-modal interfaces defined (even if not fully implemented)
- ✅ Interactive CLI dashboard functional
- ✅ Documentation: scaling guide + visualization examples

---

## 🎯 Key Gaps & Critical Path

### Critical Path to "Emergence Through Interaction" Goal:
1. **Working Memory + Inter-Column Communication** (Phase 2) 🔴 **HIGHEST PRIORITY**
   - Without this, no complex interactions between structures are possible
   - This is the foundation for emergence claims
   
2. **Procedural Regeneration** (Phase 2) 🔴 **HIGHEST PRIORITY**
   - Without measurable regeneration, system is just "lazy loading," not procedural generation
   - Must demonstrate: generate → persist minimal → regenerate consistently
   
3. **Standardized Evaluation** (Phase 4) 🟡 **MEDIUM PRIORITY**
   - Without repeatable metrics, can't validate emergence claims
   - Need before claiming "higher cognition"

### Open Research Questions:
1. **What % of neural structure can be regenerated vs. persisted?**
   - Target: ≥70% regenerated, ≤30% persisted
   - Current: Unknown (no regeneration pipeline active)
   
2. **Does inter-column communication enable emergent behaviors?**
   - Target: Observable behaviors not explicitly programmed
   - Current: No communication system to test
   
3. **Is procedural generation more efficient than static networks at equivalent capability?**
   - Target: Measure memory/compute for same task performance
   - Current: No equivalent-task benchmark defined

## 📈 Success Metrics

### Technical Metrics:
- **Storage Performance**: Save/load 50k concepts in <5s (Phase 1)
- **Regeneration Rate**: ≥70% of neural structure procedurally regenerated (Phase 2)
- **Message Throughput**: ≥1000 inter-column messages/sec (Phase 2)
- **LLM-Guided Improvement**: ≥20% retention boost vs. baseline (Phase 3)
- **Evaluation Reproducibility**: ≤5% variance across runs (Phase 4)
- **Scale Stability**: 100k vocabulary, 48h uptime (Phase 5)

### Philosophical Metrics (Long-Term):
- **Emergent Behavior**: Observable behaviors not explicitly programmed
- **Resource Efficiency**: Compare memory/compute vs. equivalent-capability static network
- **Cognitive Complexity**: Multi-step reasoning, analogy, transfer learning
- **Continuous Operation**: Self-guided learning, consolidation, exploration

## 📋 Immediate Next Steps (This Week)

1. **Complete Phase 0 Foundation Cleanup**
   - [ ] Route remaining CLI commands through TrainingService
   - [ ] Create demo retirement plan (audit 22 files → target ≤5)
   - [ ] Update README with canonical training flow
   
2. **Begin Phase 2 Planning**
   - [ ] Design working memory API (review biological inspiration)
   - [ ] Sketch inter-column message protocol
   - [ ] Define regeneration semantics for column templates
   
3. **Documentation**
   - [ ] Update TECHNICAL_DETAILS.md with this roadmap
   - [ ] Create CONTRIBUTING.md with development workflow
   - [ ] Add "Current State vs. Desired State" comparison diagram

---

## 🔬 Research Notes

### Biological Inspirations to Explore:
- **Hebbian Learning**: "Neurons that fire together, wire together"
- **Synaptic Pruning**: Remove weak connections over time
- **Sleep Consolidation**: Offline replay for memory strengthening
- **Predictive Coding**: Neural predictions vs. actual inputs
- **Global Workspace Theory**: Broadcasting of attended information

### Procedural Generation Analogies:
- **No Man's Sky**: Planets generated from coordinates + seed
- **Minecraft**: Terrain generated from coordinates + biome rules
- **greyMatter**: Neural columns generated from semantic coordinates + task requirements

### Key References:
- Huth et al. (2016): Semantic brain mapping
- HTM/Numenta: Sparse distributed representations
- Global Workspace Theory (Baars): Consciousness as broadcast mechanism
- Procedural content generation in games

---

**Last Updated**: October 2, 2025
**Next Review**: November 1, 2025 (after Phase 0 completion)
