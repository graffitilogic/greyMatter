# Training Data Audit - October 5, 2025

## Data Infrastructure

**Training Data Location**: `/Volumes/jarvis/trainData/` (~0.5TB)
**Brain Storage Location**: `/Volumes/jarvis/brainData/` (empty, ready for use)
**Working Files Location**: `/Users/billdodd/Documents/Cerebro/` (local SSD for speed)

---

## Available Data Sources

### ✅ Validated Sources

#### 1. **Tatoeba Sentences** (WORKING)
- **Path**: `/Volumes/jarvis/trainData/Tatoeba/`
- **Files**: 
  - `sentences_eng_small.csv` (2.5MB) - ✅ Currently used in training
  - `sentences.csv` (685MB) - Full dataset available
- **Format**: CSV with sentence pairs
- **Status**: ✅ **Proven working** - TatoebaLanguageTrainer functional
- **Priority**: HIGH - This is our baseline

---

### 🏗️ Available But Untested

#### 2. **Enhanced Learning Data** (Pre-processed)
- **Path**: `/Volumes/jarvis/trainData/enhanced_learning_data/`
- **Files**:
  - `enhanced_word_database.json` (125MB)
  - `enhanced_learned_patterns.json` (48MB)
  - `enhanced_cooccurrence_matrix.json` (7.5MB)
  - `enhanced_data_stats.json` (2.9MB)
- **Format**: JSON (pre-processed data structures)
- **Status**: 🏗️ **Needs validation** - Loaders may exist
- **Priority**: MEDIUM - Structured data, potentially rich

#### 3. **SimpleWiki** (Wikipedia simplified)
- **Path**: `/Volumes/jarvis/trainData/SimpleWiki/`
- **Files**:
  - `simplewiki-latest-pages-articles-multistream.xml` (1.5GB)
  - Compressed: `.xml.bz2` (340MB)
- **Format**: MediaWiki XML dump
- **Status**: 🏗️ **Needs parser** - XML parsing required
- **Priority**: HIGH - Natural language, accessible vocabulary
- **Challenge**: Need XML parser for MediaWiki format

#### 4. **CBT (Children's Book Test)**
- **Path**: `/Volumes/jarvis/trainData/CBT/`
- **Files**:
  - `CBTest/` directory (extracted)
  - `CBTest.tgz` (115MB compressed)
- **Format**: Text files (children's literature)
- **Status**: 🏗️ **Needs exploration** - Unknown structure
- **Priority**: MEDIUM - Good for language fundamentals
- **Next**: Explore CBTest/ directory structure

#### 5. **WordNet** (Lexical database)
- **Path**: `/Volumes/jarvis/trainData/WordNet/`
- **Format**: Unknown (needs exploration)
- **Status**: 🏗️ **Needs exploration**
- **Priority**: MEDIUM - Word relationships, semantic networks

#### 6. **ConceptNet** (Common sense knowledge)
- **Path**: `/Volumes/jarvis/trainData/ConceptNet/`
- **Format**: Unknown (needs exploration)
- **Status**: 🏗️ **Needs exploration**
- **Priority**: MEDIUM - Conceptual relationships

---

### 📦 Large Archives (Unexplored)

#### 7. **txt-files.tar** (23GB)
- **Path**: `/Volumes/jarvis/trainData/txt-files.tar`
- **Size**: 23GB (+ 8.5GB zipped version)
- **Status**: ❌ **Not extracted** - Unknown contents
- **Priority**: LOW (for now) - Explore after other sources validated
- **Action**: Need to sample contents before extracting

---

## Week 1 Priority Sources

Based on file availability and likely usability, focus on these 3-5 sources for Week 1:

### Tier 1 (Must Validate)
1. ✅ **Tatoeba** - Already working, expand to full dataset
2. 🏗️ **Enhanced Learning Data** - Pre-processed, likely has loaders
3. 🏗️ **SimpleWiki** - Need XML parser, but high value

### Tier 2 (Should Validate)
4. 🏗️ **CBT** - Children's books, good fundamentals
5. 🏗️ **WordNet** or **ConceptNet** - One semantic database

---

## Implementation Plan

### Day 1: Audit Complete ✅
- [x] Identify available data sources
- [x] Document paths and formats
- [x] Prioritize based on accessibility

### Day 2: Baseline Test
- [ ] Run TatoebaLanguageTrainer with 1K sentences
- [ ] Save brain state to `/Volumes/jarvis/brainData/`
- [ ] Use `/Users/billdodd/Documents/Cerebro/` for working files
- [ ] Verify: load saved state, continue training

### Day 3: Multi-Source Testing
- [ ] Test enhanced_learning_data loaders (if they exist)
- [ ] Create SimpleWiki XML parser (or find existing)
- [ ] Test CBT data loading
- [ ] Prove: Brain can learn from 3+ sources in one session

### Day 4-5: Knowledge Queries
- [ ] Implement QueryVocabulary(word) → bool
- [ ] Implement GetRelatedConcepts(word) → List<string>
- [ ] Implement GetBrainStatistics() → stats object
- [ ] Create CLI query commands

---

## Data Access Notes

### NAS Mount
- Already mounted at `/Volumes/jarvis/`
- trainData: 0.5TB available
- brainData: Empty, ready for use

### Local Working Drive
- Path: `/Users/billdodd/Documents/Cerebro/`
- Purpose: Fast SSD for temporary/working files
- Strategy: Hot data local, cold data on NAS

### Storage Strategy
```
/Users/billdodd/Documents/Cerebro/   # Working files (SSD)
├── active_concepts/                 # Currently processing
├── temp_patterns/                   # Temporary neural patterns
└── session_state/                   # Current session data

/Volumes/jarvis/brainData/           # Long-term storage (NAS)
├── vocabulary/                      # Permanent vocabulary
├── neural_clusters/                 # Consolidated neural patterns
├── episodic_memories/               # Long-term memories
└── snapshots/                       # Periodic backups
```

---

## Long-Term Vision

**Goal**: System runs for weeks/months continuously
**Requirements**:
1. Multi-source data ingestion (working)
2. Robust save/load (working + tested)
3. Knowledge accumulation tracking (implement)
4. Always-on background service (Week 4)
5. Automatic consolidation (future)

**Why This Matters**: Can't test emergent intelligence without:
- Large-scale data diversity
- Long-term memory accumulation
- Complex data structures
- Continuous operation

---

## Next Actions

### Immediate (Today):
1. ✅ Data audit complete
2. Create test runner for baseline validation
3. Configure paths: working (local) + storage (NAS)

### This Week:
1. Validate Tatoeba end-to-end with NAS paths
2. Test 2-3 additional sources
3. Implement knowledge queries
4. Document actual results (no assumptions)

---

**Status**: Data audit complete. 0.5TB available with multiple rich sources. Ready to proceed with validation testing.
