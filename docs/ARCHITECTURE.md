# ClubDoorman Architecture (Message Pipeline Phase 1)

> Status: Phase 1 (core message pipeline extracted & stable). All tests green (929) after constructor slimming.

## 1. High-Level Overview
Incoming Telegram updates flow through a unified dispatcher (`IUpdateDispatcher`) which fans out to registered `IUpdateHandler` implementations. `MessageHandler` is now a thin orchestration layer: it performs pre‑pipeline chat gating (whitelist / disabled / silent mode resolution) and then delegates all semantic work to an ordered message pipeline.

Golden Master (GM) instrumentation records raw inputs and publishes normalized moderation events so refactors can be validated by log parity & event semantics without brittle test rewrites.

```
Update -> Dispatcher -> MessageHandler
  └─ prechecks (whitelist, disabled, silent)
      └─ IMessagePipeline.RunAsync(context)
           (ordered IMessageStep list; stops on handled result)
                ↓ emits ModerationEvents + GM traces
```

## 2. MessageHandler Responsibilities (Post-Slimming)
- Correlate & record update input via `IGoldenMasterRecorder`.
- Fast fail / return for: disallowed chat, disabled chat.
- Resolve silent mode (permissions service) and annotate context.
- Construct `MessageContext` & invoke pipeline.
- Bridge legacy logging expectations for mutation / golden tests (select log phrases preserved).
- NO direct moderation, captcha, join, ban, AI, or command logic remains here.
- Temporary: still owns `DeleteMessageLater` scheduling helper (to be extracted).

## 3. Pipeline Model
Interface summary:
- `IMessagePipeline`: orchestrates ordered execution of `IMessageStep` instances.
- `IMessageStep`: implements `Order` (int) + `Task ExecuteAsync(MessageContext, CancellationToken)`.
- `MessageContext`: mutable bag (Update, Message, Chat/User refs, flags, operation id, gm correlation, status markers, moderation result, etc.).
- `StepResult` semantics encoded through context flags (`CommandHandled`, `NewMembersHandled`, `UserResultHandled`, etc.).

### Current Step Ordering (Phase 1)
| Order | Step | Purpose | Stops |
|-------|------|---------|-------|
| 10 | CommandStep | Route and execute slash commands. | On handled command |
| 15 | SystemOrBotMessageStep | Skip system / bot messages w/ semantics. | Always if system/bot |
| 20 | NewMembersStep | Process joins (approval / logging). | On handled new members |
| 30 | LeftMemberCleanupStep | Clean artifacts when users leave. | On handled |
| 40 | ChannelMessageStep | Channel post moderation path. | On handled |
| 50 | PrivateSkipStep | Ignore private messages (configurable). | On handled |
| 100 | CaptchaPendingStep | Enforce captcha gate. | On result |
| 110 | BanlistCheckStep | Fast banlist / autoban. | On result |
| 120 | AlreadyApprovedStep | Bypass for approved users. | On result |
| 130 | FirstMessageLogStep | Log first appearance (user flow analytics). | Never (side effect only) |
| 140 | ClubMemberSkipStep | Skip club members (username heuristic). | On result |
| 200 | BaseModerationStep | Core ML / heuristic moderation (facade). | On result (Allow/Delete/Ban/Report/RequireManualReview/RequireAiAnalysis) |
| 210 | AiProfileAnalysisStep | Optional AI profile enrichment (OpenRouter). | Does not override terminal actions except enrichment / escalate |
| 220 | FinalModerationActionStep | Apply final action effects (delete/ban/report) + publish events. | Terminal |

All earlier “legacy branches” removed: no fallback command path and no post‑pipeline moderation logic in `MessageHandler`.

## 4. Golden Master & Events
- `IGoldenMasterRecorder.TryRecordInput` tags each update with `gmCorrelation` (scope enrichment & log parity anchor).
- `IModerationEventPublisher` emits normalized events (rule/action metadata) from steps, decoupling test assertions from internal branching.
- Logging flag `LoggingFlagsOptions.TraceEnabled` can elevate file sink verbosity (diagnostics) without overwhelming console.

## 5. Dependency Injection Snapshot
`MessageHandler` constructor (slim):
```
(bot, appConfig, channelModeration, commandRouter,
 logger, botPermissions, goldenMaster, eventsPublisher,
 IOptions<LoggingFlagsOptions>, pipeline)
```
Container registers every pipeline step as `IMessageStep` singleton; `MessagePipeline` receives `IEnumerable<IMessageStep>` and orders them.

Legacy dependencies dropped from the constructor (still present elsewhere if used by steps/services):
`IUserManager`, `IUserBanService`, `IUserJoinFacade`, `IModerationFacade`, `ICaptchaService`, `IUserFlowLogger`, `IForwardingService`, `IAiCascadeService`.

## 6. Testing & Golden Parity
- 929 tests green; golden / semantics tests rely on preserved log phrases (e.g., "MessageHandler получил сообщение").
- Pipeline path used in all updated tests; adapter `HandleUserMessageAsync` kept temporarily for factory helpers.
- Test factory builds a real miniature pipeline to ensure step interactions are covered.

## 7. Current Known Technical Debt / Risks
| Area | Debt | Impact | Mitigation |
|------|------|--------|------------|
| Gating duplication | Prechecks live solely in `MessageHandler` | Harder to unit test gating separately | Extract `IChatAccessGate` (see TODO) |
| Inline scheduling | `DeleteMessageLater` spins fire‑and‑forget Task | Hard to observe / cancel; test flakiness potential | Extract `IDeletionScheduler` |
| Test factory sprawl | Retains mocks & setup paths unused by pipeline | Noise & maintenance overhead | Prune + tiered builders |
| Step coupling | Steps implicitly assume previous step side-effects | Tight ordering constraints | Introduce explicit context contracts / validation step |
| Logging verbosity | Many debug logs from pipeline steps | Log churn in CI artifacts | Add structured log level map / sampling |

## 8. Phase 2+ Roadmap (Detailed TODO)
The following TODO entries will be used to drive upcoming refactors. (User requested inclusion of items 1–4 + added elaboration.)

### TODO 1: ChatAccessGate Abstraction
- Extract whitelist / disabled / silent mode resolution from `MessageHandler` into `IChatAccessGate`.
- Provide method: `ChatGateResult Evaluate(Message updateMessage, CancellationToken)` returning state (Allowed, Disabled, NotWhitelisted, SilentMode, AdminChatOverride).
- Add a pipeline pre-step (order < 10) or run before pipeline invocation; emit moderation events for skip reasons for observability.
- Benefits: unit test gating independently; simplify `MessageHandler.HandleAsync` to orchestration only.

### TODO 2: Deletion Scheduler Service
- Create `IDeletionScheduler` with `ScheduleDeletion(Message, TimeSpan, CancellationToken)`.
- Use a background channel or `Task.Delay` wrapped in central service with structured logging & cancellation (host stop).
- Inject into steps that schedule deletions (currently only used indirectly by final action or message handler helper).
- Provide deterministic test seam (fake scheduler capturing intents instead of sleeping).

### TODO 3: Test Factory Simplification
- Introduce layered builders: `BasicHandlerBuilder` (command + system steps), `ModerationHandlerBuilder` (adds 100–220 steps), specialized scenario helpers.
- Remove legacy scenario methods that mirror pre-pipeline branching (e.g., duplicated ban scenarios) in favor of explicit context setups for target step.
- Replace global mock fields with local constructs where only single scenario uses them (reduces retained state between tests).
- Enforce only mocks demanded by current pipeline: audit with Roslyn analyzer or reflection at test start.

### TODO 4: Step Contract Clarification
- Define minimal context fields each step reads/writes (contract doc table).
- Add optional debug validator step (runs in Trace mode) that inspects context invariants (e.g., if `UserResultHandled` then `ModerationResult` must be non-null).
- Facilitates safe reordering and insertion/removal of steps.

### TODO 5: Logging Strategy Refinement
- Introduce per-step log category enrichment (StepName, Order) for filtering.
- Provide `LoggingFlagsOptions` expansions: `PipelineTrace`, `ModerationTrace`, `AiTrace` to selectively elevate categories.
- Consider structured event IDs for moderation outcomes.

### TODO 6: Performance & Allocation Pass
- Benchmark high-throughput spam waves: measure pipeline latency distribution.
- Pool transient objects (MessageContext reuse via `ObjectPool<MessageContext>`).
- Avoid lambda allocations inside frequently called steps.

### TODO 7: AI Profile Analysis Guardrails
- Backoff & circuit-breaker if OpenRouter returns repeated 401/5xx.
- Cache negative results briefly to avoid re-analysis loops on same user.

### TODO 8: Observability Enhancements
- Expose Prometheus counters: `pipeline_step_executed_total{step="Command"}`, action counts, skip reasons.
- Latency histograms per step.

### TODO 9: Golden Master Evolution
- Add anonymization layer to sensitive user data before log capture.
- Differential event diffing tool: compare semantic event streams between branches.

### TODO 10: Hardening & Failure Modes
- Add timeout/CTS wrapping for each step (default 2s) with aggregated failure report.
- Graceful degradation path when AI or ML subsystems unavailable (explicit event tag: `degraded=true`).

---

## 9. Contribution Guidelines (Pipeline Changes)
1. Add new step with distinct `Order` (keep 10–220 gaps; reserve <10 for future gating).
2. Document contract additions in this file (context fields).
3. Provide minimal unit tests for step (happy + skip/edge).
4. Ensure golden / semantics tests remain green (run standard filtered suite).

## 10. Quick Reference
- Handler code: `Services/Handlers/MessageHandler.cs`
- Pipeline core: `Services/Handlers/Pipeline/*`
- DI registration: `Infrastructure/ServiceCollectionExtensions.cs`
- Test factory: `ClubDoorman.Test/TestInfrastructure/MessageHandlerTestFactory.cs`

---
Last updated: Phase 1 completion (constructor slimming). Next actionable items: (1) implement this doc (completed), (2) prune unused service provider mock scaffolding, (3) start ChatAccessGate extraction.
