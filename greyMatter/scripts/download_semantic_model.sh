#!/bin/bash

# Download pre-trained sentence transformer model for semantic classification
# This script downloads a lightweight ONNX model suitable for local inference

echo "🧠 Downloading pre-trained semantic classification model..."
echo "==============================================="

# Use NAS paths for persistent storage
MODELS_DIR="/Volumes/jarvis/trainData/models"

# Create models directory on NAS
mkdir -p "${MODELS_DIR}"

# Model information
MODEL_NAME="all-MiniLM-L6-v2"
MODEL_URL="https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main/onnx/model.onnx"
TOKENIZER_URL="https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main/tokenizer.json"

echo "📦 Downloading ${MODEL_NAME} ONNX model..."
echo "   Source: HuggingFace sentence-transformers"
echo "   Size: ~90MB"
echo "   Purpose: Semantic text classification"
echo "   Target: ${MODELS_DIR}"
echo

# Download model
if command -v curl &> /dev/null; then
    echo "⬇️ Downloading model.onnx..."
    curl -L "${MODEL_URL}" -o "${MODELS_DIR}/sentence-transformer.onnx"
    
    echo "⬇️ Downloading tokenizer.json..."
    curl -L "${TOKENIZER_URL}" -o "${MODELS_DIR}/tokenizer.json"
elif command -v wget &> /dev/null; then
    echo "⬇️ Downloading model.onnx..."
    wget "${MODEL_URL}" -O "${MODELS_DIR}/sentence-transformer.onnx"
    
    echo "⬇️ Downloading tokenizer.json..."
    wget "${TOKENIZER_URL}" -O "${MODELS_DIR}/tokenizer.json"
else
    echo "❌ Error: Neither curl nor wget found. Please install one of them."
    echo "   Alternatively, manually download:"
    echo "   ${MODEL_URL}"
    echo "   ${TOKENIZER_URL}"
    exit 1
fi

echo
echo " Model download complete!"
echo "📁 Location: ${MODELS_DIR}/"
echo "🚀 Run: dotnet run -- --pretrained-demo"
echo
echo "ℹ️ Model Info:"
echo "   • Model: ${MODEL_NAME}"
echo "   • Input: Text strings"
echo "   • Output: 384-dimensional embeddings"
echo "   • Accuracy: High semantic understanding"
echo "   • Speed: Fast local inference"
