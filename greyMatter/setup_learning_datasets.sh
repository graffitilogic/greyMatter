#!/bin/bash

# AI Preschool - Foundational Learning Datasets Setup
set -e

# Resolve training data root with preference order: env -> NAS (UNC or macOS) -> local fallback
if [ -n "$TRAINING_DATA_ROOT" ]; then
  ROOT="$TRAINING_DATA_ROOT"
else
  if [ -d "/Volumes/jarvis/trainData" ]; then
    ROOT="/Volumes/jarvis/trainData"
  elif [ -d "/Volumes/jarvis" ]; then
    ROOT="/Volumes/jarvis/trainData"
  else
    # UNC paths on macOS typically require mounts; allow user to pre-mount
    ROOT="$(pwd)/learning_datasets"
  fi
fi

mkdir -p "$ROOT"
cd "$ROOT"

echo "📚 Training datasets root: $ROOT"

# 1. Download Children's Book Test (CBT) dataset
echo "🧸 Downloading Children's Book Test dataset..."
if [ ! -f "CBTest.tgz" ]; then
  curl -L -o CBTest.tgz http://www.thespermwhale.com/jaseweston/babi/CBTest.tgz
  tar -xvzf CBTest.tgz
else
  echo "CBT dataset already downloaded."
fi

# 2. Simple English Wikipedia dump
echo "🌐 Downloading Simple English Wikipedia dump..."
if [ ! -f "simplewiki-latest-pages-articles-multistream.xml.bz2" ]; then
  curl -L -o simplewiki-latest-pages-articles-multistream.xml.bz2 https://dumps.wikimedia.org/simplewiki/latest/simplewiki-latest-pages-articles-multistream.xml.bz2
else
  echo "Simple English Wikipedia dump already downloaded."
fi

echo "✅ Learning datasets setup complete at $ROOT"
