#!/bin/bash

# Monitor Cerebro training progress
echo "════════════════════════════════════════════════════════════════"
echo "Cerebro Training Monitor - $(date)"
echo "════════════════════════════════════════════════════════════════"
echo ""

# Check if process is running
if ps aux | grep -q "[d]otnet run.*production-training"; then
    echo " Training process is RUNNING"
    ps aux | grep "[d]otnet run.*production-training" | awk '{print "   PID: " $2 "  CPU: " $3 "%  MEM: " $4 "%"}'
else
    echo "❌ Training process NOT running"
fi

echo ""
echo "📊 Recent Training Progress:"
echo "────────────────────────────────────────────────────────────────"
tail -20 /tmp/cerebro_extended_test.log | grep -E "(📊|🎓|💾|⚠️)"
echo ""

echo "💾 NAS Storage Status:"
echo "────────────────────────────────────────────────────────────────"
echo "Brain Data: $(du -sh /Volumes/jarvis/brainData 2>/dev/null | awk '{print $1}')"
echo "Checkpoints: $(find /Volumes/jarvis/brainData/checkpoints -type f 2>/dev/null | wc -l | tr -d ' ') files"
echo "Metrics: $(find /Volumes/jarvis/brainData/metrics -type f 2>/dev/null | wc -l | tr -d ' ') files"
echo ""

echo "🧠 Memory Usage:"
echo "────────────────────────────────────────────────────────────────"
if ps aux | grep -q "[d]otnet run.*production-training"; then
    PID=$(ps aux | grep "[d]otnet run.*production-training" | awk '{print $2}')
    echo "Process Memory: $(ps -p $PID -o rss= | awk '{printf "%.2f GB", $1/1024/1024}')"
fi
echo "System Memory: $(vm_stat | grep "Pages free" | awk '{printf "%.2f GB free", $3 * 4096 / 1024 / 1024 / 1024}')"
echo ""

echo "⏱️  Uptime: $(tail -100 /tmp/cerebro_extended_test.log | grep "elapsed" | tail -1 | grep -oE 'elapsed [0-9]+m [0-9]+s' || echo 'N/A')"
echo ""
