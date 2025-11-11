#!/bin/bash

# Production Training Service - Control Script
# Send pause/resume/stop commands

COMMAND="${1:-status}"
CONTROL_FILE="/Users/billdodd/Desktop/Cerebro/brainData/live/training_control.json"

echo "════════════════════════════════════════════════════════════════════"
echo "PRODUCTION TRAINING SERVICE - CONTROL"
echo "════════════════════════════════════════════════════════════════════"
echo ""

if [ "$COMMAND" == "pause" ]; then
    echo "⏸️  Sending PAUSE command..."
    cat > "$CONTROL_FILE" <<EOF
{
  "Command": "pause",
  "Timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "Reason": "Manual pause via control script"
}
EOF
    echo "✅ Pause command sent"
    echo "   Service will pause and save checkpoint"
    echo ""
    echo "To resume: ./scripts/control_production_training.sh resume"

elif [ "$COMMAND" == "resume" ]; then
    echo "▶️  Sending RESUME command..."
    cat > "$CONTROL_FILE" <<EOF
{
  "Command": "resume",
  "Timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "Reason": "Manual resume via control script"
}
EOF
    echo "✅ Resume command sent"
    echo "   Service will resume training"

elif [ "$COMMAND" == "stop" ]; then
    echo "🛑 Sending STOP command..."
    cat > "$CONTROL_FILE" <<EOF
{
  "Command": "stop",
  "Timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "Reason": "Manual stop via control script"
}
EOF
    echo "✅ Stop command sent"
    echo "   Service will save checkpoint and stop gracefully"
    echo ""
    echo "Waiting for service to stop..."
    sleep 5
    
    if pgrep -f "ProductionTrainingService" > /dev/null; then
        echo "⚠️  Service still running - may take a few seconds"
    else
        echo "✅ Service stopped"
    fi

else
    echo "Usage: $0 {pause|resume|stop}"
    echo ""
    echo "Commands:"
    echo "  pause  - Pause training and save checkpoint"
    echo "  resume - Resume training from checkpoint"
    echo "  stop   - Stop training gracefully"
    echo ""
    
    if [ -f "$CONTROL_FILE" ]; then
        echo "Current control state:"
        cat "$CONTROL_FILE" | jq .
    else
        echo "No control file found - service running normally"
    fi
    exit 1
fi

echo ""
