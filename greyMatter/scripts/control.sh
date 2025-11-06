#!/bin/bash
# Control script for Continuous Learning Service
# Usage: ./control.sh {pause|resume|stop} [working_directory]

COMMAND=$1
WORKING_DIR=${2:-"./continuous_learning"}
CONTROL_FILE="$WORKING_DIR/control.json"

if [ -z "$COMMAND" ]; then
    echo "Usage: $0 {pause|resume|stop} [working_directory]"
    echo ""
    echo "Commands:"
    echo "  pause  - Pause the learning service"
    echo "  resume - Resume the learning service"
    echo "  stop   - Stop the learning service gracefully"
    echo ""
    echo "Default working directory: ./continuous_learning"
    exit 1
fi

case "$COMMAND" in
    pause|resume|stop)
        TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
        echo "Sending $COMMAND command to service..."
        echo "{\"Command\":\"$COMMAND\",\"Timestamp\":\"$TIMESTAMP\"}" > "$CONTROL_FILE"
        echo "✅ Command sent: $COMMAND"
        echo "📁 Control file: $CONTROL_FILE"
        ;;
    *)
        echo "❌ Unknown command: $COMMAND"
        echo "Valid commands: pause, resume, stop"
        exit 1
        ;;
esac
