#!/bin/bash

# Production Training Service - Start Script
# Starts 24/7 continuous training with checkpoint rehydration

set -e

echo "════════════════════════════════════════════════════════════════════"
echo "STARTING PRODUCTION TRAINING SERVICE"
echo "════════════════════════════════════════════════════════════════════"
echo ""

# Configuration
DATA_PATH="${1:-test_data/tatoeba/sentences_detailed.tsv}"
DURATION="${2:-86400}" # Default 24 hours
LOG_FILE="logs/production_training_$(date +%Y%m%d_%H%M%S).log"

# Create logs directory
mkdir -p logs

echo "Configuration:"
echo "  Data path: $DATA_PATH"
echo "  Duration: $DURATION seconds ($(($DURATION / 3600)) hours)"
echo "  Log file: $LOG_FILE"
echo ""

# Check if already running
if pgrep -f "ProductionTrainingService" > /dev/null; then
    echo "⚠️  Production training service already running!"
    echo ""
    echo "To stop: ./scripts/stop_production_training.sh"
    echo "To monitor: ./scripts/monitor_production_training.sh"
    exit 1
fi

echo "Starting service..."
echo ""

# Start production training service
dotnet run --project greyMatter.csproj -- \
    --production-training \
    --data-path "$DATA_PATH" \
    --duration "$DURATION" \
    2>&1 | tee "$LOG_FILE" &

SERVICE_PID=$!

echo "✅ Production training service started"
echo "   PID: $SERVICE_PID"
echo "   Log: $LOG_FILE"
echo ""
echo "Control commands:"
echo "  Monitor: ./scripts/monitor_production_training.sh"
echo "  Pause:   ./scripts/control_production_training.sh pause"
echo "  Resume:  ./scripts/control_production_training.sh resume"
echo "  Stop:    ./scripts/control_production_training.sh stop"
echo ""
echo "Training will run for $(($DURATION / 3600)) hours"
echo "Press Ctrl+C to detach (service continues in background)"
echo ""

# Save PID
echo $SERVICE_PID > logs/production_training.pid

# Wait for user to detach or service to complete
wait $SERVICE_PID
