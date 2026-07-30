#!/bin/bash
# P3 thesis curve: fidelity vs persistence budget.
#
# Each run trains an isolated scratch brain from nothing, so runs are
# independent and your brainData is never touched.
#
# Thresholds are deliberately FINE at the low end. The first sweep used
# 0.25/1/4/16 and looked flat (84.0/84.7/81.2/81.6) — but learning only moves a
# weight a little from its prototype, so almost every deviation already sits
# below 0.25 and the coarse range was sampling one point of the curve four times.
#
# Usage: sh scripts/sweep_fidelity.sh [repeats]
set -u
REPEATS="${1:-1}"
THRESHOLDS="0.02 0.05 0.1 0.25 0.5 1.0 2.0 8.0"

echo "threshold,repeat,fidelity,regenerated,stored_per_neuron,bytes_per_neuron,auc,dprime"
for t in $THRESHOLDS; do
  for r in $(seq 1 "$REPEATS"); do
    out=$(dotnet run -c Release -- --fidelity-test --deviation-threshold "$t" 2>/dev/null)

    fid=$(echo "$out"  | grep 'REGENERATION FIDELITY' | grep -oE '[0-9]+\.[0-9]+%' | head -1 | tr -d '%')
    regen=$(echo "$out" | grep '^PROCEDURAL:' | grep -oE 'regenerated=[0-9.]+%' | grep -oE '[0-9.]+')
    stored=$(echo "$out"| grep '^PROCEDURAL:' | grep -oE 'stored=[0-9.]+' | grep -oE '[0-9.]+')
    bytes=$(echo "$out" | grep '^PROCEDURAL:' | grep -oE 'bytes=[0-9]+' | grep -oE '[0-9]+')
    auc=$(echo "$out"   | grep '^DISCRIMINATION:' | grep -oE 'AUC=[0-9.]+' | grep -oE '[0-9.]+')
    dp=$(echo "$out"    | grep '^DISCRIMINATION:' | grep -oE "d.=[-0-9.]+" | grep -oE '[-0-9.]+$')

    echo "$t,$r,${fid:-NA},${regen:-NA},${stored:-NA},${bytes:-NA},${auc:-NA},${dp:-NA}"
  done
done
