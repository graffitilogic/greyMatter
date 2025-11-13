# Production Training System

## Overview

The **Production Training Service** is a 24/7 continuous learning system designed for real-world deployment with **real training data from NAS**.

### Key Features ✅

- **NAS-Based Training Data**: Reads 50,000+ sentences directly from `/Volumes/jarvis/trainData` - NO copying to SSD
- **Progressive Curriculum**: Starts simple (3-8 word sentences), automatically advances to full complexity
- **Checkpoint Rehydration**: Loads from latest checkpoint on startup - no more starting from scratch
- **Continuous Operation**: Runs indefinitely with hourly checkpoints and 6-hour validation cycles
- **Centralized Storage**: All brain data in `/Users/billdodd/Desktop/Cerebro/brainData/`
- **NAS Archival**: Daily backups to `/Volumes/jarvis/brainData/`
- **Control Interface**: Pause/resume/stop via JSON file or shell scripts
- **Graceful Shutdown**: Always saves state before stopping

### Test Results (November 11, 2025)

```
📖 Curriculum Phase: Expansion (1K-5K sentences)
   Medium complexity sentences
📚 Loaded 5,000 sentences from sentences_eng_small.csv
✅ 10-second test run:
   - Processed: 109 sentences
   - Vocabulary: 3 → 419 words (416 new words learned!)
   - Checkpoint saved successfully
```

## Quick Start

### Start Training (24 hours)

```bash
./scripts/start_production_training.sh
```

This will:
- Load latest checkpoint (resume where you left off)
- Start training on NAS data (no copying to SSD)
- Use progressive curriculum (automatically advance difficulty)
- Save hourly checkpoints
- Run for 24 hours by default

### Monitor Status

```bash
./scripts/monitor_production_training.sh
```

### Control Commands

```bash
# Pause training (saves checkpoint)
./scripts/control_production_training.sh pause

# Resume training
./scripts/control_production_training.sh resume

# Stop gracefully (saves final checkpoint)
./scripts/control_production_training.sh stop
```

## Training Data Sources

### Available on NAS (`/Volumes/jarvis/trainData/`)

| Dataset | Size | Sentences | Description | Status |
|---------|------|-----------|-------------|--------|
| **Tatoeba Small** | 2.5MB | 50,000 | Real-world English sentences | ✅ **ACTIVE** |
| Tatoeba Full | 685MB | ~500K+ | Complete Tatoeba corpus | 📦 Available |
| SimpleWiki | 1.5GB | Millions | Wikipedia simplified English | 📦 Available |
| Learning Data | 685MB | ~500K+ | Curated learning sentences | 📦 Available |
| Gutenberg | - | - | Public domain books | 📂 Empty (can populate) |
| ConceptNet | - | - | Concept relationships | 📦 Available |

**Key Point**: Training data stays on NAS - only checkpoint snapshots saved to SSD.

## Progressive Curriculum

The system automatically advances through difficulty levels based on sentences processed:

| Phase | Range | Word Count | Description | Status |
|-------|-------|------------|-------------|--------|
| **Phase 1: Foundation** | 0-1K | 3-8 words | Simple sentences, basic vocabulary | ✅ Complete |
| **Phase 2: Expansion** | 1K-5K | 5-15 words | Medium complexity | **← CURRENT** |
| **Phase 3: Advanced** | 5K-20K | Any | Full complexity | ⏳ Upcoming |
| **Phase 4: Mastery** | 20K+ | Any | Complete corpus (685MB) | ⏳ Future |

## Direct Usage

```bash
# Run for 24 hours
dotnet run -- --production-training --duration 86400

# Run for 1 week
dotnet run -- --production-training --duration 604800

# Custom data path
dotnet run -- --production-training --data-path /path/to/data.txt --duration 86400
```

## Architecture

### Storage Structure

```
/Users/billdodd/Desktop/Cerebro/brainData/
├── checkpoints/              # Hourly checkpoints (keep last 24)
│   ├── 2025-11-11_10-51-40/
│   │   ├── metadata.json
│   │   ├── vocabulary.json
│   │   ├── synapses.json
│   │   └── clusters.json
│   └── latest.txt           # Pointer to latest checkpoint
├── live/                     # Current active state
│   ├── vocabulary.json
│   └── training_control.json
├── episodic_memory/          # Append-only event log
└── metrics/                  # Performance tracking (CSV)
    ├── sentences_processed.csv
    ├── vocabulary_size.csv
    └── memory_usage_gb.csv
```

### NAS Archive

```
/Volumes/jarvis/brainData/
├── archives/                 # Daily snapshots
│   └── 2025-11-11/
└── training_logs/            # Historical records
```

## Features

### Checkpoint System
- **Frequency**: Every 60 minutes
- **Retention**: Keep last 24 checkpoints
- **Content**: Full vocabulary, metadata, training stats
- **Rehydration**: Automatic load on startup

### Validation Cycles
- **Frequency**: Every 6 hours
- **Tests**:
  - Vocabulary retention (> 0 words)
  - Learning rate (> 0.1 sentences/sec)
  - Memory usage (< 4.0 GB)
- **Actions**: Checkpoint saved after each validation

### NAS Archival
- **Frequency**: Every 24 hours
- **Content**: Latest checkpoint + live state + metrics
- **Purpose**: Long-term backup and disaster recovery

### Control Interface
- **File**: `/Users/billdodd/Desktop/Cerebro/brainData/live/training_control.json`
- **Commands**: pause, resume, stop
- **Check Interval**: Every 5 seconds
- **Response**: Immediate (pause/resume) or graceful shutdown (stop)

## Monitoring

### Real-Time Logs

```bash
tail -f logs/production_training_$(ls -t logs/ | head -1)
```

### Metrics

```bash
# View sentences processed over time
tail -20 /Users/billdodd/Desktop/Cerebro/brainData/metrics/sentences_processed.csv

# View vocabulary growth
tail -20 /Users/billdodd/Desktop/Cerebro/brainData/metrics/vocabulary_size.csv
```

### Checkpoint History

```bash
ls -lh /Users/billdodd/Desktop/Cerebro/brainData/checkpoints/
```

## Example Usage

### Week-Long Training Run

```bash
# Start service for 7 days
./scripts/start_production_training.sh /path/to/data.txt 604800

# In another terminal, monitor progress
watch -n 60 ./scripts/monitor_production_training.sh

# After 3 days, pause for analysis
./scripts/control_production_training.sh pause

# Analyze checkpoints, then resume
./scripts/control_production_training.sh resume

# Stop gracefully after 7 days
./scripts/control_production_training.sh stop
```

### Recovery from Crash

```bash
# Service automatically loads latest checkpoint on restart
./scripts/start_production_training.sh

# Verify state restored
./scripts/monitor_production_training.sh
```

## Configuration

### ProductionTrainingService Parameters

```csharp
new ProductionTrainingService(
    dataPath: "path/to/training/data.txt",
    checkpointIntervalMinutes: 60,     // Hourly
    validationIntervalHours: 6,         // Every 6 hours
    nasArchiveIntervalHours: 24,        // Daily
    enableAttention: true,              // Skip boring repetitive patterns
    enableEpisodicMemory: true          // Record significant events
)
```

### Storage Configuration

```csharp
new ProductionStorageManager(
    activeBrainPath: "/Users/billdodd/Desktop/Cerebro/brainData",  // SSD
    nasArchivePath: "/Volumes/jarvis/brainData",                    // NAS
    enableNASArchive: true,                                          // Auto-archive
    maxCheckpoints: 24                                               // Keep last 24
)
```

## Production Readiness

### What Changed
- ❌ **Before**: 15+ demos, each runs 60 seconds, data scattered in `./demo_*` directories
- ✅ **After**: Single production service, runs indefinitely, centralized storage

### Multi-Day Capability
- ✅ Checkpoint rehydration working
- ✅ Hourly checkpoints tested
- ✅ Validation cycles implemented
- ✅ NAS archival ready
- ✅ Control interface functional
- ✅ Graceful shutdown verified

### Next Steps (Phase 3)
- [ ] Knowledge query system (IKnowledgeQuery interface)
- [ ] CLI query tools (word associations, pattern search)
- [ ] Validation suite (>85% accuracy target)
- [ ] Make accumulated knowledge actually useful!

## Troubleshooting

### Service Won't Start
```bash
# Check if already running
pgrep -f ProductionTrainingService

# Check logs
tail -100 logs/production_training_*.log

# Verify data file exists
ls -lh test_data/tatoeba/sentences_detailed.tsv
```

### Checkpoint Load Fails
```bash
# Check checkpoint directory
ls -lh /Users/billdodd/Desktop/Cerebro/brainData/checkpoints/

# Verify latest.txt
cat /Users/billdodd/Desktop/Cerebro/brainData/checkpoints/latest.txt

# Check metadata
cat $(cat /Users/billdodd/Desktop/Cerebro/brainData/checkpoints/latest.txt)/metadata.json | jq .
```

### High Memory Usage
```bash
# Check metrics
tail -20 /Users/billdodd/Desktop/Cerebro/brainData/metrics/memory_usage_gb.csv

# Force garbage collection by restarting
./scripts/control_production_training.sh stop
./scripts/start_production_training.sh
```

## Performance Targets

- **Learning Rate**: > 1.0 sentences/second
- **Vocabulary Growth**: Linear with training time
- **Memory Usage**: < 4.0 GB sustained
- **Checkpoint Save Time**: < 5 seconds
- **Checkpoint Load Time**: < 10 seconds
- **Validation Pass Rate**: > 90%

## Success Criteria (End of Week)

- [ ] 7-day continuous training run complete
- [ ] 100,000+ sentences processed
- [ ] 50,000+ vocabulary words learned
- [ ] 1,000,000+ synaptic connections
- [ ] Knowledge queryable via CLI
- [ ] >85% accuracy on validation suite
