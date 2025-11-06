#!/bin/bash
# Status display script for Continuous Learning Service
# Usage: ./status.sh [working_directory]

WORKING_DIR=${1:-"./continuous_learning"}
STATUS_FILE="$WORKING_DIR/status.json"

if [ ! -f "$STATUS_FILE" ]; then
    echo "❌ Status file not found: $STATUS_FILE"
    echo "Is the service running?"
    exit 1
fi

echo "📊 CONTINUOUS LEARNING SERVICE STATUS"
echo "═══════════════════════════════════════════════════════════"
echo ""

# Pretty print JSON using jq if available, otherwise just cat
if command -v jq &> /dev/null; then
    cat "$STATUS_FILE" | jq -r '
        "State: \(.State)",
        "Started: \(.StartTime)",
        "Last Activity: \(.LastActivity)",
        "Sentences Processed: \(.SentencesProcessed | tonumber | floor | tostring | gsub("(?<a>[0-9])(?<b>([0-9]{3})+(?![0-9]))"; "\(.a),\(.b)"))",
        "Vocabulary Size: \(.VocabularySize | tonumber | floor | tostring | gsub("(?<a>[0-9])(?<b>([0-9]{3})+(?![0-9]))"; "\(.a),\(.b)"))",
        "Current Data Source: \(.CurrentDataSource)",
        "Message: \(.Message)"
    '
else
    # Fallback without jq
    cat "$STATUS_FILE" | python3 -c "
import sys, json
data = json.load(sys.stdin)
print(f\"State: {data['State']}\")
print(f\"Started: {data['StartTime']}\")
print(f\"Last Activity: {data['LastActivity']}\")
print(f\"Sentences Processed: {int(data['SentencesProcessed']):,}\")
print(f\"Vocabulary Size: {int(data['VocabularySize']):,}\")
print(f\"Current Data Source: {data['CurrentDataSource']}\")
print(f\"Message: {data['Message']}\")
"
fi

echo ""
echo "═══════════════════════════════════════════════════════════"
echo "📁 Status file: $STATUS_FILE"
