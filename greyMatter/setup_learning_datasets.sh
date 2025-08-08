#!/bin/bash

# AI Preschool - Foundational Learning Datasets Setup
echo "📚 Setting up AI Preschool learning datasets..."

# Create resources directory
mkdir -p learning_datasets
cd learning_datasets

# 1. Download Children's Book Test (CBT) dataset from Facebook Research
echo "🧸 Downloading Children's Book Test dataset..."
if [ ! -f "CBTest.tgz" ]; then
    wget http://www.thespermwhale.com/jaseweston/babi/CBTest.tgz
    tar -xvzf CBTest.tgz
else
    echo "CBT dataset already downloaded."
fi

# 2. Download Simple English Wikipedia dump (a small, recent one is fine)
echo "🌐 Downloading Simple English Wikipedia dump..."
if [ ! -f "simplewiki-latest-pages-articles-multistream.xml.bz2" ]; then
    wget https://dumps.wikimedia.org/simplewiki/latest/simplewiki-latest-pages-articles-multistream.xml.bz2
else
    echo "Simple English Wikipedia dump already downloaded."
fi

echo "✅ Learning datasets setup complete."
