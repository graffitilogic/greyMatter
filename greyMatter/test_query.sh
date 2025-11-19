#!/bin/bash

# Test script for querying trained Cerebro brain
# Usage: ./test_query.sh

echo "════════════════════════════════════════════════════════════════════════════════"
echo "CEREBRO QUERY TEST - Testing Knowledge Retrieval"
echo "════════════════════════════════════════════════════════════════════════════════"
echo ""

# Show brain statistics
echo "📊 Step 1: Check brain statistics"
echo "────────────────────────────────────────────────────────────────────────────────"
dotnet run -- --cerebro-query stats
echo ""

# Show learned clusters
echo "🗂️  Step 2: View top 30 learned concepts"
echo "────────────────────────────────────────────────────────────────────────────────"
dotnet run -- --cerebro-query clusters 30
echo ""

# Query specific concepts
echo "🔍 Step 3: Query specific concepts"
echo "────────────────────────────────────────────────────────────────────────────────"

concepts=("cat" "dog" "education" "science" "love" "computer" "language" "news" "government" "technology")

for concept in "${concepts[@]}"; do
    echo ""
    echo ">>> Querying: $concept"
    dotnet run -- --cerebro-query think "$concept"
done

echo ""
echo "════════════════════════════════════════════════════════════════════════════════"
echo " Query test complete!"
echo "════════════════════════════════════════════════════════════════════════════════"
