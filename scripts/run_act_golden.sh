#!/usr/bin/env bash
set -euo pipefail

if ! command -v act >/dev/null 2>&1; then
  echo "act not installed. Install: https://github.com/nektos/act" >&2
  exit 2
fi

# Use minimal runners to speed up local run
export ACT_ALLOW_SKIPPED_JOB=true

echo "[act-golden] Running golden-validation workflow locally via act (pull_request event)"
act pull_request -W .github/workflows/golden-validation.yml "$@"
