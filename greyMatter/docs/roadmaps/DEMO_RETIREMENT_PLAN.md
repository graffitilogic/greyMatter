# Demo Retirement Plan & CLI Consolidation
**Date**: October 2, 2025  
**Status**: Planning Phase

## 🎯 Goals
1. Route all CLI commands through TrainingService where appropriate
2. Reduce demo files from 22 → 5 (retain only essential demos)
3. Update README with canonical training flow

---

## 📊 Current State Analysis

### CLI Commands in Program.cs (Discovered: 30+)

#### ✅ Already Using TrainingService
- `--llm-teacher` → TrainingService.RunLLMTeacherSessionAsync()
- `--performance-validation` → TrainingService.RunPerformanceValidationAsync()
- `--enhanced-learning` → TrainingService.RunTatoebaTrainingAsync()
- `--language-random-sample` → TrainingService.RunTatoebaTrainingAsync()
- `--reading-comprehension` → TrainingService.RunReadingComprehensionAsync()

#### 🟡 Should Route Through TrainingService (High Priority)
- `--continuous-learning` → TrainingService.RunContinuousLearningAsync() (NEW METHOD)
- `--convert-tatoeba-data` → TrainingService.ConvertTatoebaDataAsync() (NEW METHOD)
- `--convert-enhanced-data` → TrainingService.ConvertEnhancedDataAsync() (NEW METHOD)

#### 🟢 Legacy Test/Debug Commands (Consider Retiring or Grouping)
- `--test-sparse` / `--sparse-encoding` → Already shows deprecation message
- `--neuron-analysis` / `--analyze-neurons` → Keep as debug tool
- `--auto-save-test` / `--test-auto-save` → Retire (functionality in continuous learning)
- `--analyze-patterns` → Retire or move to evaluation
- `--evaluate` / `--eval-training` → Keep (evaluation harness)
- `--debug` / `--comprehensive-debug` → Keep as debug tool
- `--diag` / `--quick-diag` → Consolidate with --debug
- `--validate-learning` / `--learning-validation` → Already handled by evaluation
- `--diagnostic` / `--analyze-growth` → Consolidate into --debug
- `--test-optimized` / `--test-efficiency` → Retire (covered by performance-validation)
- `--test-hybrid-optimization` / `--verify-optimization` → Retire
- `--test-training` / `--test-results` → Retire

#### 🔵 Tatoeba Variants (Consolidate)
Currently have 10+ variants:
- `--tatoeba-hybrid` / `--hybrid-tatoeba`
- `--tatoeba-hybrid-full` / `--hybrid-full-scale`
- `--tatoeba-hybrid-random` / `--hybrid-random`
- `--tatoeba-hybrid-debug` / `--hybrid-debug`
- `--tatoeba-hybrid-1k` / `--hybrid-1k` ✅ **KEEP** (quick demo)
- `--tatoeba-hybrid-10k` / `--hybrid-10k`
- `--tatoeba-hybrid-100k` / `--hybrid-100k`
- `--tatoeba-hybrid-complete` / `--hybrid-complete`

**Recommendation**: Keep only `--tatoeba-hybrid-1k` as quick demo. Others route through `--enhanced-learning --max-words N`

#### ⚪ Special Purpose (Keep)
- `--verify-training-data` / `--verify-data` → ✅ Essential utility
- `--procedural-demo` / `--phase2-demo` → ⚠️ Disabled, awaiting Phase 2 implementation

---

## 📁 Demo File Analysis (22 Files)

### ✅ Keep (Essential - 5 files)

1. **LLMTeacherDemo.cs** - Core LLM teacher functionality demo
   - **Why**: Showcases revolutionary LLM-guided learning
   - **Status**: Production-ready, well-maintained
   - **Usage**: `--llm-teacher`

2. **TatoebaHybridIntegrationDemo.cs** - Quick 1k sentence demo
   - **Why**: Fast onboarding demo (<5 min runtime)
   - **Status**: Well-documented, minimal dependencies
   - **Usage**: `--tatoeba-hybrid-1k`
   - **Action**: Keep 1k method only; retire other variants

3. **ProceduralGenerationDemo.cs** - Phase 2 procedural columns
   - **Why**: Demonstrates core "No Man's Sky" concept (currently disabled)
   - **Status**: Framework demo awaiting Phase 2 integration
   - **Usage**: `--procedural-demo` (disabled until Phase 2)
   - **Action**: Keep for future Phase 2 work

4. **ReadingComprehensionDemo.cs** - Q&A capabilities
   - **Why**: Demonstrates episodic memory and comprehension
   - **Status**: Functional, unique capability
   - **Usage**: `--reading-comprehension`

5. **EnhancedContinuousLearningDemo.cs** - Background learning demo
   - **Why**: Shows continuous learning with multi-source integration
   - **Status**: Production-ready
   - **Usage**: Internal demo for `--continuous-learning`

### 🔴 Retire (Redundant/Obsolete - 12 files)

6. **SimpleEphemeralDemo.cs** - Basic ephemeral brain demo
   - **Why Retire**: Functionality covered by TatoebaHybridIntegrationDemo
   - **Migration Path**: None needed (basic concept well-documented)

7. **EnhancedEphemeralDemo.cs** - Enhanced ephemeral demo
   - **Why Retire**: Superseded by continuous learning and LLM teacher
   - **Migration Path**: Use `--llm-teacher` instead

8. **ScaleDemo.cs** - 100k scale demonstration
   - **Why Retire**: Functionality covered by `--enhanced-learning --max-words 100000`
   - **Migration Path**: TrainingService with --max-words flag

9. **ComprehensiveDemo.cs** - Kitchen sink demo
   - **Why Retire**: Too broad, duplicates other demos
   - **Migration Path**: Use specific focused demos

10. **TextLearningDemo.cs** - Basic text learning
    - **Why Retire**: Covered by Tatoeba demos
    - **Migration Path**: Use `--tatoeba-hybrid-1k`

11. **LanguageFoundationsDemo.cs** - Language foundations
    - **Why Retire**: Superseded by LLM teacher and Tatoeba training
    - **Migration Path**: Use `--llm-teacher`

12. **DevelopmentalLearningDemo.cs** - Developmental learning
    - **Why Retire**: Concepts integrated into continuous learning
    - **Migration Path**: Use `--continuous-learning`

13. **InteractiveConversation.cs** - Conversation demo
    - **Why Retire**: Experimental, not production-ready
    - **Migration Path**: None (future feature)

14. **Phase2QuickTest.cs** - Phase 2 quick test
    - **Why Retire**: Test file, not demo
    - **Migration Path**: Convert to unit test if needed

15. **TestHybridSystem.cs** - Hybrid system test
    - **Why Retire**: Test file, not demo
    - **Migration Path**: Functionality in TrainingService tests

16. **IterativeGrowthTest.cs** - Growth test
    - **Why Retire**: Test file, should be in /tests
    - **Migration Path**: Move to test suite

17. **QuickClassifierTest.cs** - Classifier test
    - **Why Retire**: Test file, not demo
    - **Migration Path**: Move to test suite

### ⚠️ Archive (Specialized - 5 files)

Move to `/demos/archive/` or `/demos/specialized/` subdirectory:

18. **PreTrainedSemanticDemo.cs** - Pre-trained semantic classifier
    - **Why Archive**: Specialized ONNX demo, not core workflow
    - **Future**: May be useful for multi-modal Phase 5

19. **SemanticDomainTest.cs** - Semantic domain testing
    - **Why Archive**: Diagnostic tool, not end-user demo
    - **Future**: Useful for storage validation

20. **DebugClassifierTest.cs** - Debug classifier
    - **Why Archive**: Debug utility, specialized use
    - **Future**: Keep for troubleshooting

21. **ConceptStorageTest.cs** - Storage testing
    - **Why Archive**: Test utility, not demo
    - **Future**: Convert to unit test

22. **benchmarks/** folder - Performance benchmarks
    - **Why Archive**: Specialized performance testing
    - **Future**: Integrate into `--performance-validation`

---

## 🚀 Implementation Plan

### Phase 1: Add Missing TrainingService Methods (2-3 hours)

#### 1.1 Add Continuous Learning Method
```csharp
public async Task<TrainingResult> RunContinuousLearningAsync(
    int maxWords = 5000,
    int batchSize = 100,
    bool autoSave = true)
{
    // Wrap EnhancedContinuousLearner with TrainingService interface
    // Use centralized config for paths
}
```

#### 1.2 Add Data Conversion Methods
```csharp
public async Task ConvertTatoebaDataAsync(string inputPath, string outputPath)
{
    // Wrap TatoebaDataConverter with TrainingService interface
}

public async Task ConvertEnhancedDataAsync(string inputPath, string outputPath)
{
    // Wrap EnhancedDataConverter with TrainingService interface
}
```

### Phase 2: Route CLI Commands Through TrainingService (2-3 hours)

Update Program.cs to route these commands:
- `--continuous-learning` → TrainingService.RunContinuousLearningAsync()
- `--convert-tatoeba-data` → TrainingService.ConvertTatoebaDataAsync()
- `--convert-enhanced-data` → TrainingService.ConvertEnhancedDataAsync()

Consolidate Tatoeba variants:
- Keep only `--tatoeba-hybrid-1k` as quick demo
- Add message for other variants: "Use --enhanced-learning --max-words N instead"

### Phase 3: Retire/Archive Demo Files (1-2 hours)

1. **Create `/demos/archive/` directory**
2. **Move archived demos** (5 files) to archive
3. **Delete retired demos** (12 files) - commit before delete for safety
4. **Update remaining demos** (5 files):
   - Add deprecation notices if needed
   - Ensure all use TrainingService or have clear purpose
   - Update inline documentation

### Phase 4: Update Documentation (2-3 hours)

#### 4.1 Update README.md
- Replace "Key Commands" section with streamlined version
- Add "Quick Start" section with 3 primary commands
- Add "Advanced Usage" section for specialized commands
- Remove references to retired demos

#### 4.2 Create COMMANDS.md
- Comprehensive CLI reference
- Organized by category (Learning, Evaluation, Debug, Utilities)
- Include examples and expected output

#### 4.3 Update ROADMAP_2025.md
- Mark "Foundation cleanup" as complete
- Update Phase 0 acceptance criteria

---

## 📋 Streamlined CLI Commands (Target State)

### Primary Commands (3)
```bash
# 1. LLM-Guided Learning (RECOMMENDED)
dotnet run -- --llm-teacher

# 2. Quick Demo (1k sentences, <5 min)
dotnet run -- --tatoeba-hybrid-1k

# 3. Production Training
dotnet run -- --enhanced-learning --max-words 10000
```

### Advanced Commands (8)
```bash
# Evaluation & Validation
dotnet run -- --performance-validation    # Comprehensive system validation
dotnet run -- --evaluate                  # Learning evaluation metrics
dotnet run -- --reading-comprehension     # Q&A capabilities test

# Background Learning
dotnet run -- --continuous-learning       # Continuous background learning

# Data Utilities
dotnet run -- --verify-training-data      # Check dataset presence
dotnet run -- --convert-tatoeba-data      # Convert Tatoeba format
dotnet run -- --convert-enhanced-data     # Convert enhanced data format

# Debug & Analysis
dotnet run -- --debug                     # Comprehensive debugging
```

### Configuration Flags (Apply to any command)
```bash
--brain-path <path>          # Custom brain storage location
--max-words <number>         # Vocabulary size limit
--batch-size <number>        # Batch processing size
-td <path>                   # Training data root override
```

---

## ✅ Acceptance Criteria

### CLI Consolidation
- [ ] ≤15 active CLI commands (down from 30+)
- [ ] All learning commands route through TrainingService
- [ ] Tatoeba variants consolidated (10 → 1)
- [ ] Clear deprecation messages for obsolete commands

### Demo Retirement
- [ ] ≤5 demo files in /demos (down from 22)
- [ ] Archived demos in /demos/archive or /demos/specialized
- [ ] No broken references in remaining code
- [ ] Build succeeds with zero errors

### Documentation
- [ ] README "Key Commands" section updated
- [ ] Quick Start guide with 3 primary commands
- [ ] COMMANDS.md created with comprehensive reference
- [ ] All code examples in README use current commands

---

## 🎯 Next Steps (This Week)

1. **Monday**: Implement TrainingService methods (continuous learning, data conversion)
2. **Tuesday**: Route CLI commands through TrainingService
3. **Wednesday**: Retire/archive demo files, update references
4. **Thursday**: Update README and create COMMANDS.md
5. **Friday**: Build verification, acceptance criteria check, commit

---

**Estimated Total Time**: 8-12 hours  
**Risk Level**: Low (mostly cleanup, well-scoped changes)  
**Dependencies**: None (can proceed immediately)
