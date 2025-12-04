#!/bin/bash
# Test Production Training Improvements
# Shows curriculum phases and diverse data sources

echo "🧪 Testing Production Training Improvements"
echo "=========================================="
echo ""
echo "This test will run production training for 3 minutes"
echo "Watch for:"
echo "   Checkpoint at 10-minute interval (vs old 60-min)"
echo "   Curriculum phase transitions (diverse data sources)"
echo "   Shuffled batches (no endless cycling)"
echo ""
echo "Press Ctrl+C to stop..."
echo ""

# Run for 3 minutes
dotnet run -- --production-training --duration 180 2>&1 | \
  grep -E "(CURRICULUM|Loaded.*shuffled|Phase:|checkpoint|💾)" --color=always

echo ""
echo " Test complete! Check /Volumes/jarvis/brainData/checkpoints/ for saved state"
