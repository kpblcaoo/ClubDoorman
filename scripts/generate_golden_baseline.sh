#!/usr/bin/env bash
set -euo pipefail

# Script: generate_golden_baseline.sh
# Purpose: Run isolated baseline harness project (ClubDoorman.Baseline) to create deterministic Golden Master snapshots.
# Usage: ./scripts/generate_golden_baseline.sh [--clean]

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BASELINE_DIR="${ROOT_DIR}/golden/baseline"
HARNESS_PROJ="${ROOT_DIR}/ClubDoorman.Baseline/ClubDoorman.Baseline.csproj"

CLEAN=0
SAFE_MODE=0

for arg in "$@"; do
  case "$arg" in
    --clean) CLEAN=1 ;;
    --safe) SAFE_MODE=1 ;;
    --duration=*) DURATION="${arg#*=}" ;;
  esac
done

if [[ $CLEAN -eq 1 && -d "$BASELINE_DIR" ]]; then
  echo "[baseline] Cleaning existing baseline directory: $BASELINE_DIR" >&2
  rm -rf "$BASELINE_DIR"
fi

# Подгружаем .env если есть (разрешаем пользователю задать реальный токен)
if [[ -f "${ROOT_DIR}/ClubDoorman/.env" ]]; then
  # shellcheck disable=SC2046
  export $(grep -v '^#' "${ROOT_DIR}/ClubDoorman/.env" | grep -E '^[A-Za-z0-9_]+=') || true
fi

if [[ $SAFE_MODE -eq 1 || -z "${DOORMAN_BOT_API:-}" ]]; then
  export DOORMAN_BOT_API="123456:TESTTOKENPLACEHOLDER"
fi
export DOORMAN_ADMIN_CHAT="${DOORMAN_ADMIN_CHAT:-123456789}"

echo "[baseline] Building harness..." >&2
(cd "$ROOT_DIR" && dotnet restore >/dev/null && dotnet build "$HARNESS_PROJ" -c Release --no-restore >/dev/null)

echo "[baseline] Running harness..." >&2
(cd "$ROOT_DIR" && dotnet run -c Release -p "$HARNESS_PROJ")

if [[ -d "$BASELINE_DIR" ]]; then
  COUNT=$(find "$BASELINE_DIR" -maxdepth 1 -name '*.input.json' 2>/dev/null | wc -l | tr -d ' ')
  if [[ "$COUNT" == "0" ]]; then
    echo "[baseline] Baseline directory exists but contains 0 input snapshots. Возможно за время окна не пришло ни одного апдейта."
    echo "[baseline] Советы:"
    echo "  • Отправьте несколько сообщений боту/в группу и перезапустите скрипт"
    echo "  • Или добавьте позже синтетический сидер (TODO)"
  else
    echo "[baseline] Generated $COUNT input snapshot(s) under golden/baseline" >&2
  fi
else
  echo "[baseline] No baseline directory created (check flags or runtime)" >&2
  exit 1
fi

echo "[baseline] Done." >&2
