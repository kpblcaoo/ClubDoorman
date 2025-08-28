#!/usr/bin/env bash
set -euo pipefail
# Phase 0 baseline freeze script.
# Generates ClubDoorman.Baseline/golden/freeze-manifest.json with sha256 hashes of current golden snapshot files.
# Usage: ./scripts/baseline_freeze.sh
# Optional env: FREEZE_OUTPUT (override output path), FREEZE_VERBOSE=1 for extra logs.

if [[ ${FREEZE_VERBOSE:-0} == 1 ]]; then
  set -x
fi

REPO_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || echo "$(pwd)")"
GOLDEN_ROOT="$REPO_ROOT/ClubDoorman.Baseline/golden"
OUTPUT_PATH="${FREEZE_OUTPUT:-$GOLDEN_ROOT/freeze-manifest.json}"

if [[ ! -d "$GOLDEN_ROOT" ]]; then
  echo "[freeze] Golden root not found: $GOLDEN_ROOT" >&2
  exit 1
fi

# Collect snapshot files (exclude existing manifest if present)
mapfile -t FILES < <(find "$GOLDEN_ROOT" -type f -name '*.json' ! -name 'freeze-manifest.json' | LC_ALL=C sort)

if [[ ${#FILES[@]} -eq 0 ]]; then
  echo "[freeze] No snapshot json files found under $GOLDEN_ROOT" >&2
  exit 2
fi

TMP_JSON="$(mktemp)"
{
  echo "[";
  first=1
  for f in "${FILES[@]}"; do
    rel="${f#$REPO_ROOT/}"
    hash=$(sha256sum "$f" | awk '{print $1}')
    if [[ $first -eq 0 ]]; then echo ","; fi
    first=0
    printf '  {"path":"%s","sha256":"%s"}' "$rel" "$hash"
  done
  echo; echo "]";
} > "$TMP_JSON"

mv "$TMP_JSON" "$OUTPUT_PATH"

count=${#FILES[@]}
sha_all=$(sha256sum "$OUTPUT_PATH" | awk '{print $1}')
echo "[freeze] Wrote manifest for $count files -> $OUTPUT_PATH (manifest sha256=$sha_all)"
