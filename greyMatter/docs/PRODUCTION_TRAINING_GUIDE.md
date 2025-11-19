# Production Training Quick Start

##  All Systems Ready

All training infrastructure is now activated:
- 571GB Wikipedia corpus
- 500GB book collections
- LLM teacher (Ollama deepseek-r1:1.5b)
- Progressive curriculum (simple → encyclopedic)
- Fixed checkpoints (NaN/Infinity sanitization)
- Fixed query system (direct ConceptLabel lookup)

---

## Start Production Training

### Option 1: Default Progressive Curriculum (Recommended)
```bash
cd /Users/billdodd/Library/CloudStorage/Dropbox/dev/gl/greyMatter/greyMatter
dotnet run --project greyMatter.csproj -- --production-training
```

**What happens**:
- Phase 1 (0-1K): Simple Tatoeba sentences
- Phase 2 (1K-5K): News headlines
- Phase 3 (5K-10K): Conversational dialogue
- Phase 4 (10K-20K): 📚 **Book corpus activated**
- Phase 5 (20K-50K): 📖 **Wikipedia chunks activated**
- Phase 6 (50K+): 🌍 **Full 571GB Wikipedia activated**
- Every 5th batch: 🤖 **LLM-generated content** (science, history, tech, nature, culture, philosophy)

### Option 2: Test Short Run (5 minutes)
```bash
# Quick test to verify all datasets work
dotnet run --project greyMatter.csproj -- --production-training --max-sentences 500
```

### Option 3: Jump to Wikipedia Phase
```bash
# Manually set sentence count to 50000 to activate Phase 6
# (Edit ProductionTrainingService.cs line ~173: _totalSentencesProcessed = 50000;)
dotnet run --project greyMatter.csproj -- --production-training
```

---

## Monitor Training

### Watch Progress
```bash
# In separate terminal - monitor training output
tail -f logs/production_training.log
```

### Check Checkpoints
```bash
# Verify checkpoints are saving (every 10 minutes)
ls -lh /Volumes/jarvis/brainData/checkpoints/ | tail -10
```

### Query Learned Knowledge
```bash
# Test query system during or after training
dotnet run --project greyMatter.csproj CerebroQueryCLI.cs
# Query: red
# Query: technology
# Query: philosophy
```

---

## Expected Output

### Phase Transitions
```
📖 Curriculum Phase: Foundation (0-1K sentences)
   Short simple sentences - basic vocabulary and grammar
📂 Loading dataset: tatoeba_small
 Loaded 1,000 fresh sentences from 'tatoeba_small' (batch #1, shuffled)

...

🎓 CURRICULUM ADVANCING
   From: Foundation (0-1K sentences)
   To: Expansion (1K-5K sentences)
   Sentences: 1,000
📖 Curriculum Phase: Expansion (1K-5K sentences)
   News headlines - current events and journalism vocabulary
📂 Loading dataset: news
 Loaded 5,000 fresh sentences from 'news' (batch #2, shuffled)

...

🎓 CURRICULUM ADVANCING
   From: Dialogue (5K-10K sentences)
   To: Narrative (10K-20K sentences)  ← 📚 BOOKS START HERE
   Sentences: 10,000
📖 Curriculum Phase: Narrative (10K-20K sentences)
   Book corpus - narrative structures, complex storytelling
📂 Loading dataset: books_corpus
 Loaded 5,000 fresh sentences from 'books_corpus' (batch #10, shuffled)

...

🎓 CURRICULUM ADVANCING
   From: Narrative (10K-20K sentences)
   To: Encyclopedic (20K-50K sentences)  ← 📖 WIKIPEDIA CHUNKS START HERE
   Sentences: 20,000
📖 Curriculum Phase: Encyclopedic (20K-50K sentences)
   Wikipedia chunks - encyclopedic knowledge, technical vocabulary
📂 Loading dataset: wikipedia_chunked
 Loaded 30,000 fresh sentences from 'wikipedia_chunked' (batch #20, shuffled)

...

🎓 CURRICULUM ADVANCING
   From: Encyclopedic (20K-50K sentences)
   To: Full Corpus (50K+ sentences)  ← 🌍 571GB WIKIPEDIA STARTS HERE
   Sentences: 50,000
📖 Curriculum Phase: Full Corpus (50K+ sentences)
   Full Wikipedia (571GB+) - comprehensive world knowledge
📂 Loading dataset: wikipedia_full
📁 Scanning directory: /Volumes/jarvis/trainData/txtDump/cache/epub
📄 Loading from 1247 .txt files...
 Loaded 25,000 fresh sentences from 'wikipedia_full' (batch #40, shuffled)
```

### LLM Generation (Every 5th Batch)
```
🤖 Generating diverse content via LLM (batch #5)...
   Topic: science, Difficulty: beginner
✨ Generated 1,000 LLM sentences on 'science' (beginner)

...

🤖 Generating diverse content via LLM (batch #10)...
   Topic: history, Difficulty: intermediate
✨ Generated 1,000 LLM sentences on 'history' (intermediate)

...

🤖 Generating diverse content via LLM (batch #50)...
   Topic: philosophy, Difficulty: advanced
✨ Generated 1,000 LLM sentences on 'philosophy' (advanced)
```

### Checkpoint Saves
```
💾 Saving checkpoint to /Volumes/jarvis/brainData/checkpoints/brain_checkpoint_20241117_143022.json
   Neurons: 1,847,293 | Clusters: 12,847 | Size: 487.3 MB
 Checkpoint saved successfully (took 12.3s)
```

---

## Verify Infrastructure

### Check NAS Mounts
```bash
# Verify training data accessible
ls -lh /Volumes/jarvis/trainData/
# Should see: txtDump/, books/, epub/, news/, dialogue/

# Check Wikipedia corpus
ls -lh /Volumes/jarvis/trainData/txtDump/cache/epub/ | wc -l
# Should see: ~1000+ .txt files

# Check brain data storage
ls -lh /Volumes/jarvis/brainData/
# Should see: checkpoints/, partitions/
```

### Check LLM Teacher (Optional)
```bash
# Test Ollama connection
curl http://192.168.69.138:11434/api/tags
# Should return: {"models":[{"name":"deepseek-r1:1.5b",...}]}

# Generate test content
curl http://192.168.69.138:11434/api/chat -d '{
  "model": "deepseek-r1:1.5b",
  "messages": [{"role": "user", "content": "Explain photosynthesis in 2 sentences."}],
  "stream": false
}'
```

---

## Performance Expectations

### Training Speed
- **Phase 1-3** (0-10K): ~50-100 sentences/sec (simple datasets)
- **Phase 4** (10K-20K): ~30-50 sentences/sec (books - larger files)
- **Phase 5** (20K-50K): ~20-40 sentences/sec (Wikipedia chunks)
- **Phase 6** (50K+): ~10-30 sentences/sec (571GB Wikipedia - massive files)
- **LLM batches**: ~5-10 sentences/sec (network latency + generation time)

### Memory Usage
- **Phase 1-3**: 2-4 GB RAM
- **Phase 4-5**: 4-8 GB RAM
- **Phase 6**: 8-16 GB RAM (loading large Wikipedia files)
- **Neuron count**: ~30-50 neurons per sentence (with 83-85% reuse)

### Disk Usage
- **Checkpoints**: 400-600 MB per checkpoint (every 10 min)
- **Partitions**: 50-100 MB per 10K sentences
- **Estimated 24h**: 30-50 GB total storage

---

## Troubleshooting

### Issue: "Failed to load dataset 'wikipedia_full'"
```bash
# Check NAS mount
mount | grep jarvis
# If not mounted: mount -t nfs 192.168.69.138:/volume1/jarvis /Volumes/jarvis

# Check directory exists
ls -lh /Volumes/jarvis/trainData/txtDump/cache/epub/
```

### Issue: "LLM generation failed"
```bash
# Test LLM connection
curl http://192.168.69.138:11434/api/tags

# If fails, training will automatically fall back to static datasets
# Check LLM server logs on NAS
```

### Issue: Checkpoint save fails
```bash
# Check disk space
df -h /Volumes/jarvis/brainData/

# Check write permissions
touch /Volumes/jarvis/brainData/checkpoints/test.txt && rm /Volumes/jarvis/brainData/checkpoints/test.txt
```

### Issue: Query returns "not found"
```bash
# Verify checkpoint has ConceptLabel data
# (Old checkpoints before Nov 18 may have empty labels)
# Solution: Train for at least 1000 sentences to build new checkpoint
```

---

## Success Metrics

After 1-hour run, you should see:

 **Curriculum Progression**:
- Started: Phase 1 (tatoeba_small)
- Advanced through: Phase 2 (news), Phase 3 (dialogue)
- Reached: Phase 4 (books_corpus) or beyond

 **Dataset Diversity**:
- Multiple dataset loads visible in logs
- LLM generation every 5th batch (if available)
- No repeated dataset cycling

 **Checkpoint Reliability**:
- 6 checkpoints saved (every 10 min)
- No JSON serialization errors
- File sizes 400-600 MB each

 **Query Functionality**:
- Can query common words ("the", "red", "blue", "school")
- Can query learned concepts from diverse sources
- Case-insensitive matching works

 **Neuron Reuse**:
- 83-85% reuse rate maintained
- New neuron rate 0.1-0.3% per batch
- Total neurons ~30-50 per sentence

---

## Stop Training

```bash
# Graceful shutdown (CTRL+C)
^C

# Wait for final checkpoint save
💾 Saving final checkpoint before shutdown...
 Checkpoint saved successfully
👋 Production training stopped gracefully
```

---

**Status**: 🚀 Ready to unleash the full power of 571GB+ training corpus!
