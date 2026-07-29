#!/bin/bash

# Quick Production Test - 5 minutes, high intensity
set -e

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
TEST_DIR="/tmp/phase6b_prod_${TIMESTAMP}"
mkdir -p "${TEST_DIR}"

echo "======================================================================"
echo "PHASE 6B PRODUCTION SCALE TEST - 5 MINUTE INTENSIVE"
echo "======================================================================"
echo "Test directory: ${TEST_DIR}"
echo "Start: $(date)"
echo ""

# Run intensive 5-minute training with procedural save
echo "Training with procedural compression (5 minutes)..."
dotnet run -- \
  --production-training \
  --duration 300 \
  --procedural-save \
  --brain-path "${TEST_DIR}/brain" \
  2>&1 | tee "${TEST_DIR}/training.log" &

TRAIN_PID=$!
echo "Training started (PID: ${TRAIN_PID})"

# Wait for training to complete
wait ${TRAIN_PID}

echo ""
echo "======================================================================"
echo "ANALYZING RESULTS"
echo "======================================================================"

# Brain size
if [ -d "${TEST_DIR}/brain" ]; then
  TOTAL_SIZE=$(du -sh "${TEST_DIR}/brain" | cut -f1)
  echo "Total brain size: ${TOTAL_SIZE}"
  
  # Count files
  PROC_COUNT=$(find "${TEST_DIR}/brain" -name "*_procedural.msgpack.gz" 2>/dev/null | wc -l | tr -d ' ')
  STD_COUNT=$(find "${TEST_DIR}/brain" -name "neurons.bank_*.msgpack.gz" ! -name "*_procedural*" 2>/dev/null | wc -l | tr -d ' ')
  
  echo "Procedural banks: ${PROC_COUNT}"
  echo "Standard banks: ${STD_COUNT}"
  
  # Size comparison
  if [ -d "${TEST_DIR}/brain/hierarchical" ]; then
    PROC_SIZE=$(find "${TEST_DIR}/brain/hierarchical" -name "*_procedural.msgpack.gz" 2>/dev/null -exec du -b {} + | awk '{sum+=$1} END {print sum}')
    STD_SIZE=$(find "${TEST_DIR}/brain/hierarchical" -name "neurons.bank_*.msgpack.gz" ! -name "*_procedural*" 2>/dev/null -exec du -b {} + | awk '{sum+=$1} END {print sum}')
    
    if [ -n "${PROC_SIZE}" ] && [ "${PROC_SIZE}" -gt 0 ] && [ -n "${STD_SIZE}" ]; then
      PROC_MB=$(echo "scale=2; ${PROC_SIZE} / 1048576" | bc)
      STD_MB=$(echo "scale=2; ${STD_SIZE} / 1048576" | bc)
      RATIO=$(echo "scale=2; ${STD_SIZE} / ${PROC_SIZE}" | bc)
      SAVED_MB=$(echo "scale=2; (${STD_SIZE} - ${PROC_SIZE}) / 1048576" | bc)
      
      echo ""
      echo "COMPRESSION METRICS:"
      echo "  Standard format:   ${STD_MB} MB"
      echo "  Procedural format: ${PROC_MB} MB"
      echo "  Compression ratio: ${RATIO}x"
      echo "  Space saved:       ${SAVED_MB} MB ($(echo "scale=1; 100 * (${STD_SIZE} - ${PROC_SIZE}) / ${STD_SIZE}" | bc)%)"
    fi
  fi
fi

# Extract key metrics from training log
echo ""
echo "TRAINING METRICS:"
grep -E "(💾 Procedural save|neurons in|Compression|ratio)" "${TEST_DIR}/training.log" | tail -10

echo ""
echo "======================================================================"
echo "TEST COMPLETE - $(date)"
echo "======================================================================"
echo "Results in: ${TEST_DIR}"

