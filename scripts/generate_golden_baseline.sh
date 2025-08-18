#!/usr/bin/env bash
set -euo pipefail

# Script: generate_golden_baseline.sh
# Purpose: Generate (or refresh) deterministic Golden Master baseline snapshots.
# Usage: ./scripts/generate_golden_baseline.sh [--clean]
# Requires: dotnet 9 SDK, project restored & built.

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BASELINE_DIR="${ROOT_DIR}/golden/baseline"
APP_DLL="${ROOT_DIR}/ClubDoorman/bin/Release/net9.0/ClubDoorman.dll"

CLEAN=0
if [[ "${1:-}" == "--clean" ]]; then
  CLEAN=1
fi

if [[ $CLEAN -eq 1 && -d "$BASELINE_DIR" ]]; then
  echo "[baseline] Cleaning existing baseline directory: $BASELINE_DIR" >&2
  rm -rf "$BASELINE_DIR"
fi

export DOORMAN_BOT_API="https://api.telegram.org" # safe placeholder
export DOORMAN_ADMIN_CHAT="123456789"
export LoggingFlags__GoldenMasterEnabled=true
export LoggingFlags__GoldenSampleRate=1.0
export LoggingFlags__GoldenBasePath="golden"
export LoggingFlags__GoldenDeterministicIds=true
export LoggingFlags__GoldenFixedDateFolder="baseline"

if [[ ! -f "$APP_DLL" ]]; then
  echo "[baseline] Building project (Release)..." >&2
  (cd "$ROOT_DIR" && dotnet restore && dotnet build --configuration Release --no-restore) >/dev/null
fi

echo "[baseline] Running application briefly to capture baseline..." >&2
# Run for a short fixed time; the worker likely polls long-running. Use timeout to stop.
# We rely on startup instrumentation / synthetic test harness later for richer baseline.
TO=8
command -v timeout >/dev/null || TO=0
if [[ $TO -gt 0 ]]; then
  timeout ${TO}s dotnet "$APP_DLL" || true
else
  dotnet "$APP_DLL" &
  PID=$!
  sleep 8
  kill $PID || true
fi

if [[ -d "$BASELINE_DIR" ]]; then
  COUNT=$(find "$BASELINE_DIR" -maxdepth 1 -name '*.input.json' | wc -l | tr -d ' ')
  echo "[baseline] Generated $COUNT input snapshot(s) under golden/baseline" >&2
else
  echo "[baseline] No baseline directory created (check flags or runtime)" >&2
  exit 1
fi

echo "[baseline] Done." >&2
