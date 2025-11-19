#!/bin/bash

# Enhanced Data Integration Script for GreyMatter
# This script processes all available learning sources in /Volumes/jarvis/trainData

echo "🧠 GreyMatter Enhanced Data Integration"
echo "======================================"

# Check if training data directory exists
if [ ! -d "/Volumes/jarvis/trainData" ]; then
    echo "❌ Training data directory not found at /Volumes/jarvis/trainData"
    echo "Please ensure the external drive is mounted."
    exit 1
fi

echo "📂 Found training data directory"

# Navigate to project directory
cd "/Users/billdodd/Library/CloudStorage/Dropbox/dev/gl/greyMatter/greyMatter"

echo "🏗️ Building project..."
dotnet build

if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi

echo " Build successful"

# Run the enhanced data integration
echo "🚀 Starting enhanced data integration..."
dotnet run --project greyMatter.csproj -- EnhancedDataIntegrationRunner

echo "🎉 Enhanced data integration complete!"
echo ""
echo "📊 Integration Summary:"
echo "• Processed SimpleWiki articles for encyclopedic knowledge"
echo "• Integrated news headlines for current events"
echo "• Added scientific abstracts for technical understanding"
echo "• Incorporated children's literature for narrative patterns"
echo "• Learned idioms and expressions for colloquial language"
echo "• Processed technical documentation for formal writing"
echo "• Added social media posts for conversational patterns"
echo "• Integrated open subtitles for spoken dialogue"
echo ""
echo "🧠 Your GreyMatter system now has access to diverse, biologically-encoded learning sources!"
