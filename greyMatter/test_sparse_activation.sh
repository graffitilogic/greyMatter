#!/bin/bash
# Test Phase 6A sparse activation instrumentation
# Train on 100 sentences, then run queries to measure activation %

echo "🧪 Testing Phase 6A Sparse Activation Instrumentation"
echo "=================================================="
echo ""
echo "This test will:"
echo "  1. Train on 100 simple sentences"
echo "  2. Run 14 queries to measure sparse activation %"
echo "  3. Save checkpoint to show biological alignment metrics"
echo ""

# Run the dedicated test program
dotnet run --project greyMatter.csproj -- --test-sparse-activation

echo ""
echo "=================================================="
echo "✅ Test complete. Review output above for:"
echo "   ⚡ Sparse Activation lines during queries"
echo "   📊 BIOLOGICAL ALIGNMENT METRICS section at save"
echo "   Target: <2% activation (biological cortex equivalent)"
echo ""
