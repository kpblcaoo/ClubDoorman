# Golden Master Evolution Roadmap

This document captures the phased plan to evolve the current synthetic Golden Master (GM) baseline for ClubDoorman.

## Legend
- Complexity: S (small), M (medium), L (large)
- Invasiveness (prod impact): Low / Med / High (affects production runtime behavior vs. test-only infra)
- Confidence: Estimated probability of smooth delivery

## Phase 1 – Semantic Core (RuleCode + mediaKind + noise pruning)
Introduce a stable semantic layer so snapshots depend on codes, not brittle free‑text.

Tasks:
1. Enum `RuleCode` (StopWords, Links, TooManyEmojis, Greeting, Banlist, MediaNoCaption, MediaEarlyBlock, Command, ReplyLink, MixedLinkStopWords, EmojiEscalation, BanEscalation, Boundary, Unknown).
2. Map existing moderation reasons -> `RuleCode` inside a translator (non-breaking; keep original text for UI/logs).
3. Extend snapshot serialization with `mediaKind` (photo|video|sticker|document|null) when message has no text.
4. Remove or null-out ML raw `Confidence` (currently noisy / negative) from snapshot v2.

Complexity: M  | Invasiveness: Low  | Confidence: 95%
Risks: Miss a reason -> Unknown. Mitigation: Fallback + warning log.

## Phase 2 – Manifest
Single source-of-truth enumerating expected scenarios.

Tasks:
1. Create `golden/manifest.json` with array of `{id, shortName, expectedAction, ruleCode}`.
2. Seeder regenerates manifest deterministically.
3. Add NUnit test verifying: (a) every manifest entry has matching output snapshot; (b) no extra orphan snapshots.

Complexity: S | Invasiveness: Low | Confidence: 98%
Risk: Human forgets to update manifest. Mitigation: test fail.

## Phase 3 – Snapshot DTO v2
Parallel new schema before deprecating v1.

Tasks:
1. Define `GoldenSnapshotV2` (InputCore, OutputCore) with only semantic fields.
2. Recorder writes both v1 (legacy) and v2 (new) if flag enabled.
3. Add `SCHEMA_VERSION` file (value: 2).

Complexity: M | Invasiveness: Med (writer changes) | Confidence: 90%
Risks: Dual format linger. Mitigation: timebox (# of PRs) then remove v1.

## Phase 4 – User Identity Normalization
Reduce snapshot churn when ids/logins shift.

Tasks:
1. Alias real numeric user IDs to stable `User#N` sequence per run.
2. Store alias map only runtime (not in repo) to avoid leaking real IDs.

Complexity: S | Invasiveness: Low | Confidence: 97%
Risk: Legacy snapshots mismatch. Mitigation: perform with v2 adoption.

## Phase 5 – Reason Codes Decouple UI Text
Make future text refactors snapshot-safe.

Tasks:
1. Introduce mapping `RuleCode -> LocalizedText` (central dictionary).
2. Snapshots store only `ruleCode` + optional `humanReason` (non-compared).
3. Replace scattering of hardcoded reason strings with calls to reason provider.

Complexity: M | Invasiveness: Med | Confidence: 85%
Risk: Missed migration leaves stale literal. Mitigation: grep known phrases.

## Phase 6 – Invariant Tests (Non-GM)
Shift stable logical properties out of GM to reduce scenario proliferation.

Invariants:
- Never Ban in private chat.
- Link detection priority over stop-words when both present.
- Violation counters reset after Ban.
- Emoji escalation threshold enforced.

Complexity: S | Invasiveness: Low | Confidence: 99%
Risk: Overlap with GM. Mitigation: Document boundary (what GM covers vs invariants).

## Phase 7 – Boundary Aggregation
Combine related micro-scenarios into compound flows.

Tasks:
1. Merge emoji 9/10/11 into one scenario with sequential messages.
2. Remove redundant snapshots; update manifest.

Complexity: S | Invasiveness: Low | Confidence: 95%
Risk: Loss of fine-grained diff clarity. Mitigation: Keep internal message index in scenario output.

## Phase 8 – CI Enforcement
Fail fast on unintended drift.

Tasks:
1. Workflow: run baseline → assert clean git diff in golden path.
2. On diff: fail + upload patch artifact, apply label `baseline-update`.
3. Set deterministic culture/env (`DOTNET_CLI_UI_LANGUAGE=en-US`, explicit locale for serializer).

Complexity: M | Invasiveness: Low | Confidence: 90%
Risk: Flaky locale differences. Mitigation: force culture at process start.

## Phase 9 – Remove v1 Format
Cleanup after confidence in v2.

Tasks:
1. Stop writing legacy snapshots.
2. Delete historical v1 files (one PR with manifest unchanged).
3. Bump `SCHEMA_VERSION` if breaking change occurs.

Complexity: S | Invasiveness: Low | Confidence: 95%
Risk: Forgotten consumer of v1. Mitigation: Repo-wide search before removal.

## Phase 10 – (Optional) Verify / ApprovalTests Pilot
Adopt external snapshot tooling only if added value needed.

Tasks:
1. One pilot test using Verify with scrubbers mirroring RuleCode normalization.
2. Evaluate UX (diff viewer, update flow) vs custom.
3. Decide to expand or keep custom.

Complexity: M | Invasiveness: Med | Confidence: 70%
Risk: Dual snapshot systems complexity. Mitigation: keep pilot isolated.

## Phase 11 – Property-Based Tests (Selective)
Broaden edge exploration via generated inputs.

Targets:
- Generated emoji densities.
- Mixed textual noise vs link injection.
- Randomized order of benign messages ensuring no spurious Ban.

Complexity: M/L | Invasiveness: Low | Confidence: 80%
Risk: Flaky seeds. Mitigation: capture failing seed; limit iterations.

## Phase 12 – Documentation & Governance
Codify maintenance rules.

Artifacts:
- `docs/GOLDEN_BASELINE.md` (purpose, schema, update workflow, manifest rules)
- CONTRIBUTING section for updating snapshots.
- Changelog section enumerating semantic changes (RuleCode additions, schema bumps).

Complexity: S | Invasiveness: None | Confidence: 100%
Risk: Staleness. Mitigation: PR template checkbox referencing doc updated.

---
## Summary Table

| Phase | Name                         | Cmplx | Inv | Conf | Primary Risk |
|------:|------------------------------|:-----:|:---:|:----:|--------------|
| 1 | Semantic Core                | M | Low | 95% | Missing mapping |
| 2 | Manifest                     | S | Low | 98% | Drift vs files |
| 3 | Snapshot v2 DTO              | M | Med | 90% | Dual format linger |
| 4 | ID Normalization             | S | Low | 97% | Legacy mismatch |
| 5 | Reason Codes Refactor        | M | Med | 85% | Missed literals |
| 6 | Invariant Tests              | S | Low | 99% | Overlap noise |
| 7 | Boundary Aggregation         | S | Low | 95% | Lost granularity |
| 8 | CI Enforcement               | M | Low | 90% | Locale flakiness |
| 9 | Remove v1                    | S | Low | 95% | Hidden dependency |
| 10 | Verify Pilot                | M | Med | 70% | Dual maintenance |
| 11 | Property Tests              | M/L | Low | 80% | Flaky seeds |
| 12 | Documentation & Governance  | S | None | 100% | Doc staleness |

---
## Quick Wins (Recommended First)
1. Phase 1 + Phase 2 (semantic + manifest) → immediate stability & diff clarity.
2. Phase 6 (pull invariant logic out of GM) → slows scenario growth.
3. Phase 3+4 (v2 + normalization) once existing snapshots stable for 1–2 PRs.

---
## Acceptance Criteria (Key Early Phases)
- P1: Each snapshot has non-null RuleCode; zero negative/meaningless confidence values.
- P2: Manifest test fails on missing/extra snapshot.
- P3: Both v1 & v2 written; SCHEMA_VERSION=2 file present.
- P4: Raw numeric user IDs absent from v2 input snapshots.

---
## Risk Mitigation Matrix (Condensed)
| Risk | Phase | Mitigation |
|------|-------|-----------|
| Unknown Rule | 1 | Log + fallback Unknown; audit before merge |
| Drift (files) | 2/8 | CI diff gating |
| Dual format drag | 3 | Timebox removal milestone |
| Missed refactor points | 5 | Grep known phrases pre-merge |
| Flaky gen tests | 11 | Capture seed + iteration cap |
| Doc staleness | 12 | PR checklist |

---
## Future Extensions (Optional)
- Golden diff summarizer bot posting compact rule change summary on PR.
- Rule coverage metric (percentage of RuleCodes exercised by baseline).
- Mutation tests over moderation rules (ensuring GM catches injected faults).

---
## Ownership & Governance
- Primary Maintainer: (assign) `@moderation-core`.
- Update Flow: Change → run baseline → update manifest → CI green → human review sign-off (label `baseline-approved`).

End of roadmap.
