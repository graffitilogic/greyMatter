#!/bin/bash
# Monitor script for Continuous Learning Service
# Usage: ./monitor.sh [working_directory] [refresh_seconds]

WORKING_DIR=${1:-"./continuous_learning"}
REFRESH=${2:-5}
STATUS_FILE="$WORKING_DIR/status.json"

echo "🔍 CONTINUOUS LEARNING SERVICE MONITOR"
echo "Refreshing every $REFRESH seconds (Ctrl+C to stop)"
echo "═══════════════════════════════════════════════════════════"
echo ""

if [ ! -f "$STATUS_FILE" ]; then
    echo "❌ Status file not found: $STATUS_FILE"
    echo "Is the service running?"
    exit 1
fi

# Function to display status
display_status() {
    clear
    echo "🔍 CONTINUOUS LEARNING SERVICE MONITOR"
    echo "Refreshing every $REFRESH seconds (Ctrl+C to stop)"
    echo "Updated: $(date '+%Y-%m-%d %H:%M:%S')"
    echo "═══════════════════════════════════════════════════════════"
    echo ""
    
    if command -v jq &> /dev/null; then
        cat "$STATUS_FILE" | jq -r '
            "🟢 State: \(.State)",
            "⏰ Started: \(.StartTime)",
            "📝 Last Activity: \(.LastActivity)",
            "📊 Sentences: \(.SentencesProcessed | tonumber | floor)",
            "📚 Vocabulary: \(.VocabularySize | tonumber | floor)",
            "📁 Data Source: \(.CurrentDataSource)",
            "💬 Message: \(.Message)"
        '
    else
        cat "$STATUS_FILE" | python3 -c "
import sys, json
data = json.load(sys.stdin)
print(f\"🟢 State: {data['State']}\")
print(f\"⏰ Started: {data['StartTime']}\")
print(f\"📝 Last Activity: {data['LastActivity']}\")
print(f\"📊 Sentences: {int(data['SentencesProcessed']):,}\")
print(f\"📚 Vocabulary: {int(data['VocabularySize']):,}\")
print(f\"📁 Data Source: {data['CurrentDataSource']}\")
print(f\"💬 Message: {data['Message']}\")
"
    fi
    
    echo ""
    echo "═══════════════════════════════════════════════════════════"
    echo "Commands: ./control.sh {pause|resume|stop}"
}

# Main monitoring loop
while true; do
    if [ -f "$STATUS_FILE" ]; then
        display_status
    else
        echo "⚠️  Status file not found - service may have stopped"
        break
    fi
    sleep $REFRESH
done
