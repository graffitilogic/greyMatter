#!/bin/bash

# AI Preschool - Foundational Learning Datasets Setup
set -e

# Resolve training data root with preference order: env -> NAS (macOS mount) -> local fallback
if [ -n "$TRAINING_DATA_ROOT" ]; then
  ROOT="$TRAINING_DATA_ROOT"
else
  if [ -d "/Volumes/jarvis/trainData" ]; then
    ROOT="/Volumes/jarvis/trainData"
  elif [ -d "/Volumes/jarvis" ]; then
    ROOT="/Volumes/jarvis/trainData"
  else
    ROOT="$(pwd)/learning_datasets"
  fi
fi

mkdir -p "$ROOT"
cd "$ROOT"

echo "📚 Training datasets root: $ROOT"

mkdir -p CBT SimpleWiki Tatoeba WordNet ConceptNet Gutenberg norms

# 1) Children's Book Test (CBT)
echo "🧸 Downloading Children's Book Test dataset..."
if [ ! -f "CBT/CBTest.tgz" ]; then
  curl -L -o CBT/CBTest.tgz http://www.thespermwhale.com/jaseweston/babi/CBTest.tgz || true
  if [ -f "CBT/CBTest.tgz" ]; then
    tar -xvzf CBT/CBTest.tgz -C CBT
  else
    echo "⚠️  CBT download failed (server sometimes down). Try again later."
  fi
else
  echo "CBT dataset already downloaded."
fi

# 2) Simple English Wikipedia dump
echo "🌐 Downloading Simple English Wikipedia dump..."
if [ ! -f "SimpleWiki/simplewiki-latest-pages-articles-multistream.xml.bz2" ]; then
  curl -L -o SimpleWiki/simplewiki-latest-pages-articles-multistream.xml.bz2 \
    https://dumps.wikimedia.org/simplewiki/latest/simplewiki-latest-pages-articles-multistream.xml.bz2 || true
else
  echo "Simple English Wikipedia dump already downloaded."
fi
# Decompress if needed
if [ -f "SimpleWiki/simplewiki-latest-pages-articles-multistream.xml.bz2" ] && [ ! -f "SimpleWiki/simplewiki-latest-pages-articles-multistream.xml" ]; then
  echo "🗜️  Decompressing Simple English Wikipedia (bunzip2 -k)..."
  bunzip2 -k SimpleWiki/simplewiki-latest-pages-articles-multistream.xml.bz2 || true
fi

# 3) Tatoeba sentences (English)
echo "🗣️  Downloading Tatoeba sentences..."
if [ ! -f "Tatoeba/sentences.csv" ]; then
  mkdir -p Tatoeba
  curl -L -o Tatoeba/sentences.tar.bz2 https://downloads.tatoeba.org/exports/sentences.tar.bz2 || true
  if [ -f "Tatoeba/sentences.tar.bz2" ]; then
    tar -xvjf Tatoeba/sentences.tar.bz2 -C Tatoeba
    # Optional: create a small English subset for quick starts
    awk -F'\t' '$2=="eng" {print $0}' Tatoeba/sentences.csv | head -n 50000 > Tatoeba/sentences_eng_small.csv || true
  else
    echo "⚠️  Tatoeba download failed."
  fi
else
  echo "Tatoeba sentences already present."
fi

# 4) WordNet 3.1 (dictionary database)
echo "🧩 Downloading WordNet (WNDB) 3.1..."
if [ ! -d "WordNet/WNdb-3.1" ]; then
  mkdir -p WordNet
  curl -L -o WordNet/WNdb-3.1.tar.gz https://wordnetcode.princeton.edu/3.1/WNdb-3.1.tar.gz || true
  if [ -f "WordNet/WNdb-3.1.tar.gz" ]; then
    tar -xvzf WordNet/WNdb-3.1.tar.gz -C WordNet
  else
    echo "⚠️  Could not fetch WordNet automatically. Place WNdb files into WordNet/."
  fi
else
  echo "WordNet already present."
fi

# 5) ConceptNet edges (English filtered later)
echo "🕸️  Downloading ConceptNet edges..."
if [ ! -f "ConceptNet/conceptnet-assertions-5.7.0.csv.gz" ]; then
  mkdir -p ConceptNet
  curl -L -o ConceptNet/conceptnet-assertions-5.7.0.csv.gz \
    https://conceptnet.s3.amazonaws.com/downloads/2018/edges/conceptnet-assertions-5.7.0.csv.gz || true
else
  echo "ConceptNet edges already present."
fi

# 6) Gutenberg (manual IDs) and Concreteness norms (manual)
if [ ! -f "Gutenberg/README.txt" ]; then
  cat > Gutenberg/README.txt << 'EOF'
Project Gutenberg downloads are best done via specific book IDs or curated lists.
Create a file 'gutenberg_ids.txt' here with one numeric ID per line (public domain works),
then run scripts/pg_download.sh to fetch them.
Suggested PD children's titles to consider (verify IDs): Alice in Wonderland, Peter Pan, Aesop's Fables.
EOF
fi

if [ ! -f "norms/README.txt" ]; then
  cat > norms/README.txt << 'EOF'
Brysbaert et al. concreteness norms require acceptance of terms.
Place the CSV as 'concreteness.csv' in this folder when available.
EOF
fi

# Create helper scripts folder with placeholder for Gutenberg downloader
mkdir -p scripts
if [ ! -f "scripts/pg_download.sh" ]; then
  cat > scripts/pg_download.sh << 'EOF'
#!/bin/bash
set -e
ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT_DIR/Gutenberg"

if [ ! -f gutenberg_ids.txt ]; then
  echo "Add IDs to Gutenberg/gutenberg_ids.txt (one per line)." && exit 1
fi

while IFS= read -r ID; do
  [ -z "$ID" ] && continue
  echo "📥 Downloading Gutenberg book ID $ID"
  # Try standard mirror path structure
  MIRROR_URL="https://www.gutenberg.org/files/${ID}/${ID}-0.txt"
  ALT_URL="https://www.gutenberg.org/files/${ID}/${ID}.txt"
  OUT="${ID}.txt"
  curl -L -f -o "$OUT" "$MIRROR_URL" || curl -L -f -o "$OUT" "$ALT_URL" || echo "⚠️  Failed to fetch $ID"
done < gutenberg_ids.txt

echo "✅ Gutenberg download complete."
EOF
  chmod +x scripts/pg_download.sh
fi

echo "✅ Learning datasets setup complete at $ROOT"
