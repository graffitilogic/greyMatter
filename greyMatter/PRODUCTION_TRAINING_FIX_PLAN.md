# Production Training Fix Plan
**Date**: November 12, 2025  
**Status**: READY FOR IMPLEMENTATION

---

## Problems Identified

### 🚨 Critical Issues
1. **Dataset Exhaustion**: `LoadTrainingData()` called once at startup, never reloads
2. **No Curriculum Progression**: Phase advancement checked once, never re-evaluated
3. **Performance Degradation**: 3.7 → 0.3 sent/sec (92% slower over 8 hours)
4. **NAS Archive Unused**: 24-hour archive interval never triggers

### ⚠️ Secondary Issues
5. **No Synapse Growth**: 0 synapses throughout training (learning not persisting?)
6. **Vocabulary Plateau**: Stops learning at 4,641 words (sentence 7,311)
7. **Dataset Cycling**: Re-processes same 5,000 sentences infinitely

---

## Fix Strategy

### Priority 1: Dynamic Curriculum & Dataset Rotation
**Goal**: Enable true progressive learning with diverse content

**Implementation**:
1. Check curriculum phase **every N sentences** (not just startup)
2. Reload training data when phase changes
3. Add dataset rotation within phases (prevent exhaustion)
4. Log phase transitions explicitly

**Code Changes**:
- `ProductionTrainingService.cs`: Add periodic curriculum checking
- Track last phase, reload when it changes
- Add sentence counter for curriculum checks

### Priority 2: Dataset Rotation & Exhaustion Prevention
**Goal**: Prevent vocabulary plateau from dataset exhaustion

**Implementation**:
1. Load multiple dataset batches (not all 5,000 at once)
2. Track which sentences have been processed
3. Reload fresh batches when exhausted
4. Add shuffle/rotation logic

**Code Changes**:
- Modify `RunTrainingLoopAsync()` to reload data periodically
- Add batch size parameter (e.g., 1,000 sentences per batch)
- Track processed sentence hashes to avoid exact duplicates

### Priority 3: NAS Archive Implementation
**Goal**: Actually use the 24-hour archive feature

**Implementation**:
1. Track last archive time
2. Copy full checkpoint to NAS every 24 hours
3. Add verification that archive succeeded
4. Cleanup old archives (keep last 30 days)

**Code Changes**:
- `RunMaintenanceLoopAsync()`: Add archive interval check
- Call `_storageManager.ArchiveCheckpoint()` at 24-hour mark
- Add logging for archive operations

### Priority 4: Performance Optimization
**Goal**: Maintain stable processing rate

**Implementation**:
1. Add periodic garbage collection
2. Limit episodic memory growth (prune old memories)
3. Profile memory usage every checkpoint
4. Add memory threshold alerts

**Code Changes**:
- Add `GC.Collect()` after checkpoint saves
- Implement episodic memory pruning (keep last 10K events)
- Track memory deltas between checkpoints

---

## Implementation Plan

### Phase A: Critical Fixes (2-3 hours)

#### Fix 1: Dynamic Curriculum Checking
**File**: `Core/ProductionTrainingService.cs`

**Changes**:
```csharp
// Add fields
private TrainingPhase? _currentPhase;
private int _lastCurriculumCheck = 0;
private const int CURRICULUM_CHECK_INTERVAL = 100; // Check every 100 sentences

// In RunTrainingLoopAsync(), after processing sentence:
if (_totalSentencesProcessed - _lastCurriculumCheck >= CURRICULUM_CHECK_INTERVAL)
{
    CheckAndAdvanceCurriculum();
    _lastCurriculumCheck = _totalSentencesProcessed;
}

// New method:
private void CheckAndAdvanceCurriculum()
{
    var curriculum = _dataProvider.GetProgressiveCurriculum(_datasetKey);
    var newPhase = curriculum.GetPhaseForSentenceCount(_totalSentencesProcessed);
    
    if (_currentPhase == null || newPhase.DatasetKey != _currentPhase.DatasetKey)
    {
        Console.WriteLine($"\n🎓 CURRICULUM ADVANCEMENT!");
        Console.WriteLine($"   Previous: {_currentPhase?.Name ?? "None"}");
        Console.WriteLine($"   New Phase: {newPhase.Name}");
        Console.WriteLine($"   Dataset: {newPhase.DatasetKey}");
        
        _currentPhase = newPhase;
        ReloadTrainingData();
    }
}

// New method:
private void ReloadTrainingData()
{
    Console.WriteLine($"📚 Reloading training data for phase: {_currentPhase?.Name}");
    _trainingSentences = LoadTrainingData();
    _sentenceIndex = 0; // Reset index
    Console.WriteLine($"✅ Loaded {_trainingSentences.Count:N0} sentences");
}
```

#### Fix 2: Batch-Based Dataset Loading
**File**: `Core/ProductionTrainingService.cs`

**Changes**:
```csharp
// Add fields
private List<string> _trainingSentences = new();
private int _sentenceIndex = 0;
private const int RELOAD_BATCH_SIZE = 1000;

// In RunTrainingLoopAsync():
if (_sentenceIndex >= _trainingSentences.Count)
{
    // Reached end of current batch, reload
    Console.WriteLine($"📚 Reloading training batch (processed {_sentenceIndex} sentences)");
    ReloadTrainingData();
}

var sentence = _trainingSentences[_sentenceIndex];
// ... process sentence ...
_sentenceIndex++;
```

#### Fix 3: NAS Archive Implementation
**File**: `Core/ProductionTrainingService.cs`

**Changes**:
```csharp
// Add field
private DateTime _lastNASArchive = DateTime.MinValue;

// In RunMaintenanceLoopAsync(), add check:
if ((DateTime.Now - _lastNASArchive).TotalHours >= _nasArchiveIntervalHours)
{
    Console.WriteLine($"\n💾 24-Hour NAS Archive Starting...");
    try
    {
        var checkpoint = await SaveCheckpointAsync("nas_archive");
        _storageManager.ArchiveCheckpoint(checkpoint);
        _lastNASArchive = DateTime.Now;
        Console.WriteLine($"✅ NAS archive completed: {checkpoint}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  NAS archive failed: {ex.Message}");
    }
}
```

#### Fix 4: Memory Management
**File**: `Core/ProductionTrainingService.cs`

**Changes**:
```csharp
// After checkpoint save in RunMaintenanceLoopAsync():
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();
Console.WriteLine($"♻️  Garbage collection completed");
```

### Phase B: Enhanced Logging (1 hour)

**Add detailed logging**:
- Dataset reloads: which dataset, how many sentences
- Curriculum transitions: old → new phase
- Memory usage: before/after GC
- NAS archives: success/failure, file size
- Vocabulary growth: new words per batch

### Phase C: Validation (1 hour)

**Test scenarios**:
1. 2-hour run with curriculum advancement (should hit Phase 3)
2. Verify NAS archive after 24 hours (or test with 5-min interval)
3. Check vocabulary continues growing across phases
4. Verify memory stays stable

---

## Expected Outcomes After Fixes

### Training Behavior
- ✅ Curriculum advances at 5K, 15K sentence marks
- ✅ Loads children's stories → tatoeba → dialogue → scientific
- ✅ Vocabulary grows continuously (no plateau)
- ✅ Processing rate stays stable (no 92% degradation)

### Data Management
- ✅ NAS archives every 24 hours
- ✅ Dataset rotates to prevent exhaustion
- ✅ Memory usage stays bounded

### Logging Output
```
📊 Training: 5,000 total | Vocab: 3,500
🎓 CURRICULUM ADVANCEMENT!
   Previous: Expansion (Tatoeba)
   New Phase: Advanced (Dialogue)
   Dataset: dialogue
📚 Reloading training data for phase: Advanced
✅ Loaded 50,000 sentences (dialogue lines)

📊 Training: 5,100 total | Vocab: 3,520 (20 new!)
...
💾 Checkpoint saved: 03:02:15 (hourly)
♻️  Garbage collection completed
...
💾 24-Hour NAS Archive Starting...
✅ NAS archive completed: /Volumes/jarvis/brainData/archives/2025-11-13_03-02-15
```

---

## Code Review Checklist

Before implementation:
- [ ] Review curriculum phase thresholds (1K, 5K, 15K, 15K+)
- [ ] Verify all 8 dataset types are registered
- [ ] Check TrainingDataProvider has shuffle logic
- [ ] Confirm StorageManager has ArchiveCheckpoint method
- [ ] Test curriculum advancement logic standalone

After implementation:
- [ ] Run 2-hour test, verify phase advancement
- [ ] Check vocabulary growth is continuous
- [ ] Verify NAS archive (or test with short interval)
- [ ] Profile memory usage
- [ ] Review all console logs for clarity

---

## Rollback Plan

If fixes cause issues:
1. Git branch: `fix/production-training-dynamic-curriculum`
2. Tag before changes: `before-training-fixes`
3. Revert: `git checkout before-training-fixes`
4. All fixes are in `ProductionTrainingService.cs` - single file rollback

---

## Timeline

**Estimated Time**: 4-5 hours total
- Implement fixes: 2-3 hours
- Enhanced logging: 1 hour
- Testing & validation: 1 hour

**Recommended Schedule**:
1. Implement Fix 1 & 2 (curriculum + dataset reload)
2. Test 30-minute run, verify curriculum advances
3. Implement Fix 3 & 4 (NAS archive + memory)
4. Test 2-hour run, verify all features
5. If successful, launch overnight run

---

## Success Metrics

After fixes, overnight run should show:
- **Vocabulary**: Continuous growth (no plateau)
- **Curriculum**: Advances through phases 2 → 3 → 4
- **Performance**: Stable 2-3 sent/sec throughout
- **NAS Archive**: 1 archive created
- **Memory**: Stable ~2-3 GB (no growth)
- **Datasets**: Multiple loaded (tatoeba, dialogue, scientific)

**Status**: Ready to implement. Awaiting approval to proceed.
