#!/usr/bin/env bash
set -euo pipefail

# Local reproduction of the CI golden-validation workflow (items 1,2,5 requested)
if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet not found in PATH" >&2
  exit 1
fi

echo "[local-golden] Using dotnet version: $(dotnet --version)" >&2

echo "[local-golden] Restoring dependencies" >&2
dotnet restore

echo "[local-golden] Building (Release)" >&2
dotnet build --configuration Release --no-restore

echo "[local-golden] Running baseline harness (regenerates golden artifacts)" >&2
dotnet run --project ClubDoorman.Baseline/ClubDoorman.Baseline.csproj -c Release --no-launch-profile

echo "[local-golden] Checking for unintended golden diffs" >&2
if git status --porcelain -- ClubDoorman.Baseline/golden | grep -q .; then
  echo "[local-golden] ❌ Golden diffs detected:" >&2
  git --no-pager diff -- ClubDoorman.Baseline/golden || true
  exit 1
else
  echo "[local-golden] ✅ No golden diffs detected" >&2
fi

echo "[local-golden] Running golden tests (including determinism guards)" >&2
dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj \
  --no-build --no-restore \
  --filter "(Category=GoldenManifest|Category=GoldenV2|Category=GoldenNorm|Category=GoldenAgg)" \
  --verbosity minimal

echo "[local-golden] Success" >&2
