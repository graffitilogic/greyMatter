# Overnight Training Run Analysis
**Date**: November 11-12, 2025  
**Duration**: 8 hours (1:02 AM - 9:02 AM)  
**Status**: ⚠️ **PARTIALLY SUCCESSFUL** - Completed but with issues

---

## Executive Summary

The 8-hour overnight training run **completed successfully** and processed 11,304 sentences, but **vocabulary growth stalled** at 4,641 words around the 7,311 sentence mark. After that point, the system continued processing sentences but learned no new vocabulary for approximately 4,000 additional sentences.

---

## Key Metrics

### Final Statistics
- **Total Sentences Processed**: 11,304 (started from 2,313)
- **Session Sentences**: 8,991 new sentences
- **Final Vocabulary Size**: 4,641 words
- **Training Hours**: 8.0 hours
- **Checkpoints Saved**: 8 (hourly as expected)
- **Validations**: 0 passed / 1 attempted

### Checkpoint Progression
| Time | Sentences | Vocabulary | Training Hours | Notes |
|------|-----------|------------|----------------|-------|
| 19:01 (Nov 11) | 2,313 | 1,685 | 0.08 | Starting point |
| 02:02 (Nov 12) | 5,769 | 3,943 | 1.01 | Good growth |
| 05:03 (Nov 12) | 8,936 | 4,641 | 4.01 | Vocab plateaued |
| 09:02 (Nov 12) | 11,304 | 4,641 | 8.00 | **No new words** |

---

## Performance Analysis

### Training Rate Over Time
- **Initial**: 3.6-3.8 sent/sec (sentences 2,313 → ~3,000)
- **Mid-run**: ~1.0 sent/sec (sentences ~5,000 → 8,000)
- **Final hours**: **0.3 sent/sec** (sentences 8,000 → 11,304)

**Finding**: Processing rate degraded by **92%** over the run (3.7 → 0.3 sent/sec)

### Vocabulary Growth Pattern
```
Sentence Range    | New Words Learned | Rate
------------------|-------------------|----------------
2,313 → 5,000     | 2,258 words       | 0.84 words/sent
5,000 → 7,311     | 698 words         | 0.30 words/sent
7,311 → 11,304    | 0 words           | 0.00 words/sent ❌
```

**Critical Issue**: Vocabulary stopped growing at **sentence 7,311** despite processing 3,993 more sentences.

---

## What Worked ✅

1. **Checkpoint System**
   - All 8 hourly checkpoints saved correctly
   - Metadata accurate and consistent
   - Rehydration ready

2. **Training Stability**
   - No crashes or exceptions
   - Service ran for full 8 hours
   - Graceful shutdown

3. **Data Loading**
   - TrainingDataProvider functioning
   - Reading from NAS (no SSD copying)
   - 5,000 Tatoeba sentences loaded

4. **Progressive Curriculum**
   - Started in Phase 2 (Expansion: 1K-5K)
   - Stayed in Phase 2 throughout (correct for sentence count)

---

## What Went Wrong ❌

### 1. **Vocabulary Growth Plateau**
**Problem**: After sentence 7,311, no new words were learned despite processing 3,993 additional sentences.

**Possible Causes**:
- Tatoeba small dataset (5,000 sentences) may have limited unique vocabulary
- System may have learned all unique words in the loaded dataset
- Dataset is being re-processed after exhaustion (no shuffle/rotation)
- Vocabulary learning mechanism may have a bug/saturation point

### 2. **Processing Rate Degradation**
**Problem**: Training rate dropped from 3.7 to 0.3 sent/sec (92% slower)

**Possible Causes**:
- Memory leak or accumulation over time
- Growing data structures (episodic memory, synapses) slowing processing
- No memory cleanup between sentences
- Inefficient pattern matching at scale

### 3. **No Curriculum Advancement**
**Problem**: System stayed in Phase 2 despite processing 11,304 sentences (should advance to Phase 3 at 5,000)

**Possible Cause**:
- Curriculum advancement logic may be checking **session** sentences (8,991) not **total** (11,304)
- Phase 3 threshold is 5K-15K, system has 11,304 total → should be in Phase 3

### 4. **Validation Failure**
**Problem**: "Validations passed: 0/1" indicates validation was attempted but failed

**Impact**: Unknown - need to check validation logs

---

## Curriculum Analysis

### Expected vs Actual
| Phase | Sentence Range | Content Type | Expected | Actual |
|-------|----------------|--------------|----------|--------|
| 1 | 0-1K | Children's stories | Skip (started 2,313) | ✅ Skipped |
| 2 | 1K-5K | Tatoeba sentences | Load & train | ✅ Loaded |
| 3 | 5K-15K | Dialogue | Should advance | ❌ Never advanced |
| 4 | 15K+ | Scientific | N/A | N/A |

**Finding**: System never advanced to Phase 3 (dialogue) despite exceeding 5K sentences.

---

## Data Analysis

### Tatoeba Small Dataset Characteristics
- **Loaded**: 5,000 sentences from `sentences_eng_small.csv`
- **Scanned**: 5,656 lines total
- **Unique Vocabulary**: ~4,641 words (based on final count)

**Hypothesis**: The system learned all unique words in the Tatoeba small dataset by sentence 7,311, then continued re-processing the same 5,000 sentences without learning new vocabulary.

---

## Memory and Resource Usage

**From checkpoint metadata**:
- Memory Usage: ~2.2 GB (consistent across checkpoints)
- No evidence of memory leak in saved data
- Synapses count: 0 (interesting - why no synapse growth?)

---

## Maintenance Errors

**Observed**:
```
⚠️  Maintenance error: A task was canceled.
```

**Context**: Appears during checkpoint save at shutdown  
**Impact**: Minor - checkpoint still saved successfully  
**Action**: Investigate async task cancellation during shutdown

---

## Critical Findings Summary

### 🚨 Blockers
1. **Vocabulary plateau** at 4,641 words (sentence 7,311)
2. **Performance degradation** (92% slower by end)
3. **No curriculum advancement** to Phase 3

### ⚠️ Concerns
1. **No synapse growth** throughout training
2. Validation failure (0/1 passed)
3. Dataset exhaustion/re-processing

### ✅ Successes
1. 8-hour stability (no crashes)
2. Checkpoint system reliable
3. 11,304 sentences processed
4. 4,641 vocabulary learned (good initial growth)

---

## Recommendations

### Immediate Actions
1. **Investigate vocabulary plateau**:
   - Add logging to show when duplicate sentences are detected
   - Track unique vs re-processed sentences
   - Verify vocabulary learning logic doesn't have saturation bug

2. **Fix curriculum advancement**:
   - Check if using session vs total sentence count
   - Add explicit phase transition logging
   - Test Phase 3 (dialogue) dataset loading

3. **Debug performance degradation**:
   - Profile memory usage over time
   - Check episodic memory growth (subdirectories)
   - Add periodic garbage collection

4. **Enable diverse content**:
   - Rotate through multiple datasets (not just tatoeba_small)
   - Use children's stories → tatoeba → dialogue progression
   - Verify 50K sentences are available (not just 5K)

### Next Testing Session
1. Run 2-hour test with:
   - Explicit phase transitions
   - Dataset rotation logging
   - Memory profiling
2. Verify curriculum advances at 5K sentences
3. Confirm scientific dataset (Phase 4) loads at 15K

---

## Conclusion

The overnight run **successfully validated system stability** (8 hours, no crashes) but **revealed critical learning limitations**:
- Vocabulary learning stalls after consuming dataset
- Performance degrades significantly over time
- Curriculum doesn't advance as designed

**Next Priority**: Fix curriculum advancement and enable diverse content rotation before attempting multi-day runs.

**Phase 2 Status**: Functionally complete infrastructure, but learning loop needs debugging.
