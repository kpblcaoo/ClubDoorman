# Golden Master Pipeline (Phases 0–6)

This document describes the multilayer golden baseline system used to gate moderation behavior changes.

## Overview
Phases:
- Phase 0 (Raw v1): `golden/baseline/*.input.json` + `*.output.json`
- Phase 1 (Semantic Enrichment): ruleCode annotated in output
- Phase 2 (Manifest, Schema=1): `golden/manifest.json`
- Phase 3 (V2 Export, Schema=2): `golden/baseline_v2/*.v2.json`
- Phase 4 (Normalized, Schema=4): `golden/baseline_norm/*.norm.json`
- Phase 5 (Aggregates, Schema=5): `golden/aggregates.json`
- Phase 6 (CI Gate): Workflow `golden-validation.yml`

## Determinism & Privacy
| Aspect | Strategy |
|--------|----------|
| Correlation IDs | SHA256 of stable fields when `GoldenDeterministicIds=true` |
| UserId PII | Masked -> `Uxxxx` (last 4 digits mod 10000) in inputs/outputs |
| Usernames | Hashed prefix `u_<hash>` in snapshots |
| Manifest timestamp | Fixed via env `DOORMAN_GOLDEN_FIXED_TIMESTAMP` (ISO 8601 UTC) |
| Date folder | Forced via `GoldenFixedDateFolder=variantName` |
| Sampling | `GoldenSampleRate=1.0` for baseline harness |

## Fixed Timestamp
`GeneratedAtUtc` inside `manifest.json` previously caused spurious diffs. A deterministic timestamp is now injected by baseline harness:
```
DOORMAN_GOLDEN_FIXED_TIMESTAMP=2025-01-01T00:00:00Z
```
If this variable is unset, current UTC is used (discouraged for committed baselines).

## Updating Baseline
1. Run baseline harness:
```
dotnet run --project ClubDoorman.Baseline/ClubDoorman.Baseline.csproj -c Release
```
2. Inspect changes:
```
git diff ClubDoorman.Baseline/golden
```
3. Commit if intentional.

## CI Gate
Workflow regenerates artifacts and fails if any diff appears under `ClubDoorman.Baseline/golden`.
Golden tests (Manifest/V2/Norm/Agg) validate phase invariants.

## Future Roadmap
- Phase 7: Drop legacy v1 `*.output.json` post confidence window.
- Phase 8: Hash-chain integrity + mutation fuzz checks.
- Selective ignore / allowlist mechanism for fields if additional low-signal volatility appears.

## Quick Reference Environment Variables (baseline harness)
| Variable | Purpose |
|----------|---------|
| DOORMAN_GOLDEN_BASELINE | Enables baseline mode behaviors |
| DOORMAN_GOLDEN_FIXED_TIMESTAMP | Fixes manifest timestamp |
| DOORMAN_DISABLE_MEDIA_FILTERING | Produces optional media-off variant when set |
| DOORMAN_DATA_ROOT | Isolated temp data path per variant |
| DOORMAN_TEST_BLACKLIST_IDS | Test-only blacklist injection |

## Validation Invariants (abridged)
- Manifest IDs sequential, entries count matches raw snapshot pairs.
- V2 bi-directional parity of Action & RuleCode with manifest.
- Normalized (Schema=4) preserves Id/CorrelationId/ShortName and (Action, RuleCode).
- Aggregates counts & fingerprint derived strictly from manifest.

## Contribution Notes
When changing moderation logic:
- Re-run harness; verify semantic reason for diffs (Action / RuleCode).
- Avoid committing if only timestamp / environmental noise (should be stabilized already).
- If adding scenarios: extend seeder + short name map.

---
Maintainers: Update this doc alongside pipeline phase changes.
