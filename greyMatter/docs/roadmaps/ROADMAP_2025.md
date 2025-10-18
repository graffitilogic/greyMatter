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

## 📊 Current State Assessment (October 5, 2025)

### ⚠️ REALITY CHECK: See docs/roadmaps/ACTUAL_STATE.md for honest assessment

**TL;DR**: We have ONE working datasource (Tatoeba), save/load works for basic state, and lots of untested framework code. Claims of "Phase 2 completion" were premature.

### ✅ What Actually Works (Validated)

#### 1. **Single-Source Learning** (Tatoeba Only)
- **TatoebaLanguageTrainer**: Reads Tatoeba CSV, processes sentences, creates concepts
- **LanguageEphemeralBrain**: Creates neural clusters for word concepts
- **Save/Load**: SaveBrainStateAsync/LoadBrainStateAsync functional
- **Status**: ✅ **VALIDATED** - Can train on Tatoeba, save state, reload, continue

#### 2. **Fast Storage** (Binary Serialization)
- **FastStorageAdapter**: MessagePack-based binary storage
- **Performance**: 1,350x speedup proven (0.4s vs 540s for 5K vocabulary)
- **Status**: ✅ **VALIDATED** - Proven with multiple test runs
- **Gap**: No schema versioning, no corruption detection

#### 3. **LLM API Integration** (Ollama)
- **LLMTeacher**: Makes HTTP calls to Ollama, parses JSON responses
- **Interactive Mode**: User can ask questions during learning
- **Status**: ⚠️ **PROTOTYPE** - Works but guidance is mostly manual
- **Reality Check**: No proven impact on learning quality vs baseline

### 🏗️ What Exists But Is UNTESTED

#### 4. **Column Architecture** (Code Exists, Never Run)
- **WorkingMemory** (328 lines): ✅ Compiles, ❌ Never executed
- **MessageBus** (421 lines): ✅ Compiles, ❌ Never executed
- **AttentionSystem** (390 lines): ✅ Compiles, ❌ Never executed
- **ColumnBasedProcessor** (509 lines): ✅ Compiles, ❌ Never executed
- **Status**: 🏗️ **FRAMEWORK ONLY** - Zero test runs, completely unvalidated
- **Gap**: Need to actually run it, debug crashes, prove it works

#### 5. **Multi-Source Data** (1 of 8 Sources Proven)
- **Tatoeba**: ✅ Works
- **News/Wiki/Subtitles/etc**: ⚠️ Code exists, file paths unclear, untested
- **Status**: 🏗️ **MOSTLY THEORETICAL** - Framework exists but not validated
- **Gap**: Need to test each source with real files

#### 6. **Procedural Generation** (Framework, No Integration)
- **ProceduralCorticalColumnGenerator**: Template methods exist
- **Status**: 🏗️ **FRAMEWORK ONLY** - Never used in learning pipeline
- **Gap**: No regeneration flow, no efficiency measurement, not wired to anything

### ❌ What Doesn't Exist

#### 7. **Knowledge Quantification**
- **Current**: Vocabulary count only
- **Missing**: Retention tests, concept queries, relationship graphs
- **Status**: ❌ **NOT IMPLEMENTED**
- **Gap**: User requirement - "method to test, view or quantify accumulated knowledge"

#### 8. **Always-On Learning**
- **Current**: Manual start/stop, no daemon mode
- **Missing**: Background service, auto-restart, system integration
- **Status**: ❌ **NOT IMPLEMENTED**
- **Gap**: User requirement - "always on state where it is constantly learning"

#### 9. **Emergence Testing**
- **Current**: No tests, no baselines, no measurements
- **Missing**: Everything needed to validate emergence claims
- **Status**: ❌ **NOT IMPLEMENTED**
- **Gap**: Cannot test "complex interactions → emergence" hypothesis without working columns

#### 10. **Multi-Modal Input**
- **Current**: LanguageInput enum has 5 types, only Sentence used
- **Missing**: Audio processing, image processing, all non-text modalities
- **Status**: ❌ **NOT IMPLEMENTED**
- **Gap**: Claims of "input-agnostic design" are theoretical

## 🗺️ Honest Development Roadmap

**See docs/roadmaps/HONEST_PLAN.md for detailed week-by-week plan**

### Week 1: Foundation Validation (Oct 5-11) 🏗️ IN PROGRESS
**Goal**: Prove current system works - no new features, just validation

#### Tasks:
- [x] Reality check: Create ACTUAL_STATE.md (honest assessment)
- [x] Create HONEST_PLAN.md (week-by-week with deliverables)
- [ ] Test TatoebaLanguageTrainer end-to-end (1K sentences)
- [ ] Verify save/load with actual files created
- [ ] Audit NAS data: what sources actually exist?
- [ ] Test 3 data sources with real files
- [ ] Implement basic knowledge queries: QueryVocabulary(), GetRelatedConcepts()
- [ ] Document results: what works, what doesn't

#### Acceptance Criteria:
- ✅ 3 data sources proven working
- ✅ Save/load validated with test run
- ✅ Basic knowledge queries functional
- ✅ Honest documentation of results

---

### Week 2: Multi-Source Integration (Oct 12-18) ⏳ NOT STARTED
**Goal**: Real multi-source learning pipeline

**User Requirement**: "Functioning neural layer that can process a variety of data sources"

#### Tasks:
- [ ] Create IDataSource interface
- [ ] Implement for 3-5 proven sources
- [ ] Multi-source training in single session
- [ ] Track source attribution for concepts
- [ ] Enhanced knowledge queries (filter by source)

#### Acceptance Criteria:
- ✅ Brain learns from 3+ sources simultaneously
- ✅ Can query: "concepts from Wikipedia"
- ✅ Source breakdown in statistics

---

### Week 3: Column Architecture First Run (Oct 19-25) ⏳ NOT STARTED
**Goal**: Execute column code, prove it doesn't crash

#### Tasks:
- [ ] Create test runner: TrainWithColumnArchitectureAsync(50, 10)
- [ ] Execute column training for first time
- [ ] Debug crashes/errors
- [ ] Add message logging
- [ ] Verify column communication works
- [ ] Baseline comparison: columns vs traditional

#### Acceptance Criteria:
- ✅ Column training completes without crashing
- ✅ Evidence of inter-column communication
- ✅ Comparison data: columns vs no-columns
- ✅ Honest assessment of value added (if any)

---

### Week 4: Always-On Foundation (Oct 26-Nov 1) ⏳ NOT STARTED
**Goal**: Background learning service

**User Requirement**: "Eventually an always on state where it is constantly learning"

#### Tasks:
- [ ] ContinuousLearningService class
- [ ] Infinite learning loop with auto-save
- [ ] Graceful shutdown on signal
- [ ] Monitor script + restart on crash
- [ ] Test: 24h+ runtime

#### Acceptance Criteria:
- ✅ Service runs 24h+ without manual intervention
- ✅ Auto-saves work (no data loss)
- ✅ Survives system restart
- ✅ Control interface (pause/resume/stop)

---

### Month 2-3: See HONEST_PLAN.md

Topics: Knowledge representation, retention testing, performance characterization, procedural generation integration, emergence testing (if columns work)

---

## 🎯 Revised Success Metrics

### Technical Metrics (Honest):
- **Multi-Source**: ≥3 proven datasources working with real files
- **Save/Load**: Validated with test runs, not just "code exists"
- **Knowledge Query**: Can inspect brain state, query concepts, show relationships
- **Always-On**: Service runs 24h+ with auto-save and restart
- **Column Validation**: Runs without crashing, evidence of communication
- **Emergence**: TBD after columns proven working (no claims without evidence)

### Philosophical Metrics (Long-Term):
- **Procedural Efficiency**: Compare memory/compute vs equivalent static network
- **Emergent Behavior**: Observable behaviors not explicitly programmed (needs definition + tests)
- **Cognitive Complexity**: Multi-step reasoning, transfer learning (needs baselines)

---

## 📋 Immediate Next Steps (This Week)

### Monday (Oct 5) - DONE
- [x] Create ACTUAL_STATE.md (reality check)
- [x] Create HONEST_PLAN.md (detailed week-by-week)
- [x] Update ROADMAP_2025.md (remove inflated claims)
- [ ] Update README.md (realistic assessment)

### Tuesday-Wednesday (Oct 6-7)
- [ ] Audit /Volumes/jarvis/trainData contents
- [ ] Test TatoebaLanguageTrainer with 1K sentences
- [ ] Verify save/load creates actual files
- [ ] Document file locations and contents

### Thursday-Friday (Oct 8-9)
- [ ] Test 2-3 additional data sources
- [ ] Implement QueryVocabulary(word) → bool
- [ ] Implement GetRelatedConcepts(word) → list
- [ ] Create knowledge query demo
- [ ] Document Week 1 results

---

## 🔬 Research Questions - Revised Status

### Q1: "Can procedural generation overcome ML scale limitations?"

**Previous Claim**: Framework ready, integration imminent
**Current Reality**: ❌ **CANNOT ANSWER** - Generator exists but never used in learning
**Blocking Issues**:
1. ProceduralCorticalColumnGenerator not wired into any learning pipeline
2. No regeneration flow implemented
3. No efficiency measurements
4. Pure framework code

**Path to Answer**: Wire into learning, implement regeneration, measure % regenerated, compare to baseline

---

### Q2: "Can higher cognition emerge from interactions between short-lived structures?"

**Previous Claim**: Phase 2 Week 5 complete, ready for testing
**Current Reality**: ❌ **CANNOT ANSWER** - Column code exists but never executed
**Blocking Issues**:
1. Column architecture completely untested
2. No evidence columns communicate (never run)
3. No emergence tests defined
4. No baseline comparisons

**Path to Answer**: Run column code, prove it works, define emergence tests, measure against baseline

---

### Q3: "Is biological inspiration more efficient than brute force?"

**Previous Claim**: Storage efficiency proven, learning optimized
**Current Reality**: ⚠️ **PARTIAL** - Storage proven (1,350x), but learning efficiency unproven
**Evidence So Far**:
- ✅ FastStorageAdapter: 1,350x speedup validated
- ✅ Ephemeral clusters: O(active_concepts) memory scaling
- ❌ No head-to-head comparison with static network
- ❌ No transfer learning tests
- ❌ No efficiency measurements for "biological features"

**Path to Answer**: Define equivalent-task benchmark, compare greyMatter vs traditional network, measure memory/compute/quality

---

## 🚫 What We're Removing from Claims

### No Longer Claiming:
- ❌ "Phase 2 Week 5 COMPLETE" → 🏗️ "Phase 2: Code written, untested"
- ❌ "Production Ready" → ⚠️ "Experimental Prototype"
- ❌ "Revolutionary Implementation" → ⚠️ "Novel Approach (requires validation)"
- ❌ "Multi-Source Integration (8+ sources)" → ✅ "1 source proven, others untested"
- ❌ "Complex interactions produce emergence" → ⚠️ "Hypothesis (untested)"
- ❌ "Procedural generation active" → 🏗️ "Framework exists, no integration"

### Now Claiming (Validated):
- ✅ "Single-source learning works (Tatoeba)"
- ✅ "Save/load functional"
- ✅ "Fast binary storage (1,350x speedup)"
- ✅ "LLM API integration works"
- 🏗️ "Column architecture framework exists (untested)"
- 🏗️ "Multi-source data loaders exist (mostly untested)"

---

## 📝 Documentation Standards Going Forward

### Every "COMPLETE" Must Have:
1. ✅ Code exists and compiles
2. ✅ Executed at least 3 times successfully
3. ✅ Results documented with output/logs
4. ✅ Validated by independent test

### Status Levels (Strict):
- 🏗️ **Framework**: Code exists, compiles, never executed
- ⚠️ **Prototype**: Executed 1-2 times, may have issues
- ✅ **Validated**: Executed 3+ times, results consistent
- 🚀 **Production**: Used in real workflows, stable over time

### No More:
- Claiming "COMPLETE" because code compiles
- Confusing "framework exists" with "system works"
- Inflating "API integration" to "intelligent system"
- Theoretical features presented as working systems

---

**Last Updated**: October 5, 2025
**Next Review**: October 11, 2025 (after Week 1 validation complete)
**Philosophy**: Test everything. No claims without evidence. Build. Validate. Document. Repeat.

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
