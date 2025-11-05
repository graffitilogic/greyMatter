# greyMatter Documentation Index
**Last Updated**: October 7, 2025

---

## 📚 Documentation Structure

This index explains the purpose and relationship between all documentation files.

---

## 🎯 Single Source of Truth

### **[ACTUAL_STATE.md](roadmaps/ACTUAL_STATE.md)** - **START HERE**
**Purpose**: Current system status - what works NOW vs what's theoretical  
**Updated**: After each major milestone (Week 1, Week 2, etc)  
**Contains**:
- ✅ What actually works (validated)
- 🏗️ What exists but is untested (framework only)
- ❌ What doesn't exist yet
- Current capabilities and limitations

**Use this to**: Understand the current state of the system at any time

---

## 📊 Historical Validation Records

### **[WEEK1_RESULTS.md](WEEK1_RESULTS.md)** - Foundation Validation
**Date**: October 5-11, 2025  
**Purpose**: Permanent record of Week 1 validation testing  
**Contains**:
- Day-by-day test results with timing data
- Baseline training validation (1K sentences)
- Multi-source framework validation
- Knowledge query system implementation
- Complete test outputs and evidence

**Use this to**: Reference Week 1 validation methodology and results

### **[WEEK2_RESULTS.md](WEEK2_RESULTS.md)** - Biological Learning Implementation
**Date**: October 12-18, 2025  
**Purpose**: Permanent record of biological learning fix and validation  
**Contains**:
- Before/after comparison (0% → 100% connections)
- Root cause analysis and solution implementation
- Simple test results (3 sentences, 823 neurons)
- Full pipeline results (100 sentences, 55,885 neurons)
- Query system validation
- Complete metrics and evidence

**Use this to**: Reference biological learning implementation and validation

---

## 🔧 Technical Deep Dives

### **[BIOLOGICAL_LEARNING_FIX.md](BIOLOGICAL_LEARNING_FIX.md)** - Implementation Details
**Purpose**: Technical reference for biological learning implementation  
**Contains**:
- Code changes with before/after comparisons
- Hebbian learning implementation
- Connection formation algorithms
- Export/import fixes
- Line-by-line code explanations

**Use this to**: Understand the technical implementation of biological learning

### **[TECHNICAL_DETAILS.md](../TECHNICAL_DETAILS.md)** - System Architecture
**Purpose**: Comprehensive technical documentation of the entire system  
**Contains**:
- Core component descriptions
- Storage architecture
- Neural architecture implementation
- Performance optimization details
- Learning pipeline explanation
- LLM integration architecture

**Use this to**: Understand overall system design and architecture

---

## 📋 Planning Documents

### **[HONEST_PLAN.md](roadmaps/HONEST_PLAN.md)** - Week-by-Week Development Plan
**Purpose**: Detailed weekly plan with specific tasks and deliverables  
**Updated**: Weekly as work progresses  
**Contains**:
- Week 1: ✅ Foundation validation - COMPLETE
- Week 2: ✅ Biological learning - COMPLETE
- Week 3: Multi-source integration - PENDING
- Week 4: Column architecture validation - PENDING
- Week 5: Always-on foundation - PENDING
- Success metrics for each week
- Honest progress tracking

**Use this to**: See detailed weekly tasks and current progress

### **[ROADMAP_2025.md](roadmaps/ROADMAP_2025.md)** - High-Level Development Roadmap
**Purpose**: Long-term vision and research questions  
**Contains**:
- Central research questions
- Multi-month phase planning
- Philosophical metrics
- Research status for key hypotheses

**Use this to**: Understand long-term vision and research goals

---

## 📁 Reference Documents

### **[DATA_AUDIT.md](DATA_AUDIT.md)** - Training Data Inventory
**Date**: October 5, 2025  
**Purpose**: Catalog of available training data sources  
**Contains**:
- Validated sources (Tatoeba)
- Available but untested sources (Wikipedia, CBT, etc)
- File paths and formats
- Data infrastructure details

**Use this to**: Find what training data is available and where

---

## 🗂️ Documentation Organization

```
docs/
├── DOCUMENTATION_INDEX.md          ← YOU ARE HERE (navigation guide)
│
├── 🎯 Single Source of Truth
│   └── roadmaps/
│       └── ACTUAL_STATE.md         ← Current system status
│
├── 📊 Historical Records
│   ├── WEEK1_RESULTS.md            ← Foundation validation evidence
│   └── WEEK2_RESULTS.md            ← Biological learning validation evidence
│
├── 🔧 Technical References
│   ├── BIOLOGICAL_LEARNING_FIX.md  ← Implementation details
│   └── ../TECHNICAL_DETAILS.md     ← System architecture
│
├── 📋 Planning
│   └── roadmaps/
│       ├── HONEST_PLAN.md          ← Weekly development plan
│       └── ROADMAP_2025.md         ← Long-term roadmap
│
└── 📁 References
    └── DATA_AUDIT.md               ← Training data inventory
```

---

## 🚀 Quick Start Guide

### New to the Project?
1. **Start**: [ACTUAL_STATE.md](roadmaps/ACTUAL_STATE.md) - Understand what works now
2. **Read**: [WEEK2_RESULTS.md](WEEK2_RESULTS.md) - See latest achievements
3. **Check**: [HONEST_PLAN.md](roadmaps/HONEST_PLAN.md) - See current priorities

### Want to Understand the Code?
1. **Architecture**: [TECHNICAL_DETAILS.md](../TECHNICAL_DETAILS.md)
2. **Recent Changes**: [BIOLOGICAL_LEARNING_FIX.md](BIOLOGICAL_LEARNING_FIX.md)
3. **Validation**: [WEEK1_RESULTS.md](WEEK1_RESULTS.md) + [WEEK2_RESULTS.md](WEEK2_RESULTS.md)

### Looking for Training Data?
1. **Data Sources**: [DATA_AUDIT.md](DATA_AUDIT.md)

### Want to Contribute?
1. **Current Status**: [ACTUAL_STATE.md](roadmaps/ACTUAL_STATE.md)
2. **Current Tasks**: [HONEST_PLAN.md](roadmaps/HONEST_PLAN.md)
3. **Validation Examples**: [WEEK1_RESULTS.md](WEEK1_RESULTS.md) + [WEEK2_RESULTS.md](WEEK2_RESULTS.md)

---

## 📝 Update Schedule

- **ACTUAL_STATE.md**: After each major milestone (Week 1, 2, 3, etc)
- **WEEK[N]_RESULTS.md**: Created at end of each week with validation evidence
- **HONEST_PLAN.md**: Updated weekly with progress and next tasks
- **ROADMAP_2025.md**: Updated monthly or when research questions change
- **TECHNICAL_DETAILS.md**: Updated when major architectural changes occur
- **DATA_AUDIT.md**: Updated when new data sources added/removed

---

## ✅ Documentation Principles

1. **No Duplication**: Each doc has a specific purpose
2. **Single Source of Truth**: ACTUAL_STATE.md for current status
3. **Historical Records**: WEEK[N]_RESULTS.md never change (permanent record)
4. **Technical References**: Implementation details separate from status
5. **Honest Assessment**: Reality over claims, evidence over promises

---

**Last Updated**: October 7, 2025  
**Next Review**: After Week 3 completion
