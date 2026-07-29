#!/bin/bash

# Production Scale Test for Phase 6B Procedural Neuron Regeneration
# Goal: Train large-scale model, save with procedural compression, validate accuracy

set -e

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
TEST_DIR="/tmp/phase6b_production_test_${TIMESTAMP}"
LOG_FILE="${TEST_DIR}/test_results.log"

echo "======================================================================"
echo "PHASE 6B PRODUCTION SCALE TEST"
echo "======================================================================"
echo "Test directory: ${TEST_DIR}"
echo "Start time: $(date)"
echo ""

mkdir -p "${TEST_DIR}"

# Test 1: Large training run with procedural save
echo "Step 1: Training large-scale model (30 minutes)..."
echo "  Target: 50K+ sentences, 100K+ neurons"
echo "  Procedural save: ENABLED"
echo ""

dotnet run --project greyMatter.csproj -- \
  --production-training \
  --duration 1800 \
  --procedural-save \
  --brain-path "${TEST_DIR}/brain" 2>&1 | tee "${LOG_FILE}"

# Extract metrics from training
echo ""
echo "======================================================================"
echo "TRAINING COMPLETE - ANALYZING RESULTS"
echo "======================================================================"

# Check brain size
BRAIN_SIZE=$(du -sh "${TEST_DIR}/brain" 2>/dev/null | cut -f1)
echo "Brain storage size: ${BRAIN_SIZE}"

# Count procedural files
PROCEDURAL_FILES=$(find "${TEST_DIR}/brain" -name "*_procedural.msgpack.gz" 2>/dev/null | wc -l | tr -d ' ')
STANDARD_FILES=$(find "${TEST_DIR}/brain" -name "*.msgpack.gz" ! -name "*_procedural*" 2>/dev/null | wc -l | tr -d ' ')

echo "Procedural neuron banks: ${PROCEDURAL_FILES}"
echo "Standard files: ${STANDARD_FILES}"

# Calculate compression ratio
if [ -d "${TEST_DIR}/brain/hierarchical" ]; then
  PROCEDURAL_SIZE=$(find "${TEST_DIR}/brain/hierarchical" -name "*_procedural.msgpack.gz" -exec du -b {} + 2>/dev/null | awk '{sum+=$1} END {print sum}')
  STANDARD_SIZE=$(find "${TEST_DIR}/brain/hierarchical" -name "neurons.bank_*.msgpack.gz" ! -name "*_procedural*" -exec du -b {} + 2>/dev/null | awk '{sum+=$1} END {print sum}')
  
  if [ -n "${PROCEDURAL_SIZE}" ] && [ -n "${STANDARD_SIZE}" ] && [ "${PROCEDURAL_SIZE}" -gt 0 ]; then
    COMPRESSION_RATIO=$(echo "scale=2; ${STANDARD_SIZE} / ${PROCEDURAL_SIZE}" | bc)
    SAVED_MB=$(echo "scale=2; (${STANDARD_SIZE} - ${PROCEDURAL_SIZE}) / 1048576" | bc)
    echo ""
    echo "COMPRESSION ANALYSIS:"
    echo "  Standard format: $(echo "scale=2; ${STANDARD_SIZE} / 1048576" | bc) MB"
    echo "  Procedural format: $(echo "scale=2; ${PROCEDURAL_SIZE} / 1048576" | bc) MB"
    echo "  Compression ratio: ${COMPRESSION_RATIO}x"
    echo "  Space saved: ${SAVED_MB} MB"
  fi
fi

# Extract neuron count from logs
NEURONS=$(grep -E "neurons|Neurons" "${LOG_FILE}" | tail -5)
echo ""
echo "NEURON METRICS:"
echo "${NEURONS}"

# Step 2: Load test - verify procedural load works
echo ""
echo "======================================================================"
echo "Step 2: LOAD TEST - Verifying procedural regeneration"
echo "======================================================================"

# Create a simple query test
cat > "${TEST_DIR}/query_test.txt" << 'QUERIES'
neural networks
machine learning
deep learning
artificial intelligence
natural language processing
QUERIES

echo "Running queries on loaded brain..."
dotnet run --project greyMatter.csproj -- \
  --cerebro-query \
  --brain-path "${TEST_DIR}/brain" \
  --input "${TEST_DIR}/query_test.txt" 2>&1 | tee -a "${LOG_FILE}"

echo ""
echo "======================================================================"
echo "TEST COMPLETE"
echo "======================================================================"
echo "End time: $(date)"
echo "Full logs: ${LOG_FILE}"
echo "Test directory: ${TEST_DIR}"
echo ""

# Final summary from logs
echo "FINAL SUMMARY:"
grep -E "(💾 Procedural save|Compression|neurons|Perfect|accuracy)" "${LOG_FILE}" | tail -20 || echo "Check ${LOG_FILE} for detailed metrics"

