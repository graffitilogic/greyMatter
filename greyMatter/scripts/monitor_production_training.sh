#!/bin/bash

# Production Training Service - Monitor Script
# Shows real-time status and metrics

CONTROL_FILE="/Users/billdodd/Desktop/Cerebro/brainData/live/training_control.json"
METRICS_DIR="/Users/billdodd/Desktop/Cerebro/brainData/metrics"
CHECKPOINT_DIR="/Users/billdodd/Desktop/Cerebro/brainData/checkpoints"

echo "════════════════════════════════════════════════════════════════════"
echo "PRODUCTION TRAINING SERVICE - MONITOR"
echo "════════════════════════════════════════════════════════════════════"
echo ""

# Check if running
if ! pgrep -f "ProductionTrainingService" > /dev/null; then
    echo "⚠️  Production training service is NOT running"
    echo ""
    echo "To start: ./scripts/start_production_training.sh"
    exit 1
fi

echo "✅ Service is running"
echo ""

# Show latest metrics
if [ -f "$METRICS_DIR/sentences_processed.csv" ]; then
    echo "📊 Latest Metrics:"
    echo "─────────────────────────────────────────────────────────────────"
    
    SENTENCES=$(tail -1 "$METRICS_DIR/sentences_processed.csv" | cut -d',' -f2)
    VOCAB=$(tail -1 "$METRICS_DIR/vocabulary_size.csv" | cut -d',' -f2)
    TIMESTAMP=$(tail -1 "$METRICS_DIR/sentences_processed.csv" | cut -d',' -f1)
    
    echo "  Last update: $TIMESTAMP"
    echo "  Sentences processed: $(printf "%'d" $SENTENCES)"
    echo "  Vocabulary size: $(printf "%'d" $VOCAB)"
    
    if [ -f "$METRICS_DIR/memory_usage_gb.csv" ]; then
        MEMORY=$(tail -1 "$METRICS_DIR/memory_usage_gb.csv" | cut -d',' -f2)
        echo "  Memory usage: ${MEMORY} GB"
    fi
    echo ""
fi

# Show checkpoint info
if [ -f "$CHECKPOINT_DIR/latest.txt" ]; then
    LATEST_CHECKPOINT=$(cat "$CHECKPOINT_DIR/latest.txt")
    if [ -f "$LATEST_CHECKPOINT/metadata.json" ]; then
        echo "💾 Latest Checkpoint:"
        echo "─────────────────────────────────────────────────────────────────"
        cat "$LATEST_CHECKPOINT/metadata.json" | jq -r '
            "  Timestamp: \(.Timestamp)",
            "  Sentences: \(.SentencesProcessed)",
            "  Vocabulary: \(.VocabularySize)",
            "  Training hours: \(.TrainingHours | tonumber | round)"
        '
        echo ""
    fi
fi

# Show control status
if [ -f "$CONTROL_FILE" ]; then
    echo "🎛️  Control Status:"
    echo "─────────────────────────────────────────────────────────────────"
    cat "$CONTROL_FILE" | jq -r '
        "  Command: \(.Command)",
        "  Timestamp: \(.Timestamp)",
        "  Reason: \(.Reason)"
    '
    echo ""
fi

echo "Control commands:"
echo "  ./scripts/control_production_training.sh pause"
echo "  ./scripts/control_production_training.sh resume"
echo "  ./scripts/control_production_training.sh stop"
echo ""

# Tail recent log
LOG_FILE=$(ls -t logs/production_training_*.log 2>/dev/null | head -1)
if [ -n "$LOG_FILE" ]; then
    echo "📋 Recent Log (last 10 lines from $LOG_FILE):"
    echo "─────────────────────────────────────────────────────────────────"
    tail -10 "$LOG_FILE"
fi
