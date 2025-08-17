# Полная архитектурная разведка (ветка: plans/arch-analysis, дата: 2025-08-18)

## Цели
- Независимый от предыдущих ревизий полный срез архитектуры: DI граф, эффекты, конфигурация, состояние, доменная логика.
- Установить точные количественные метрики для будущего контроля регрессий.

## 1. DI / Регистрация сервисов
- Всего AddSingleton<>: 72
- AddScoped<>: 0
- AddTransient<>: 0
=> Вся система построена на singleton-сервисах (включая stateful), повышая риск гонок и жёстких связей.

Единственное использование BuildServiceProvider(): Program.cs:87 (PostConfigure hack). Риск: двойное построение контейнера, возможные singleton-дубликаты вне root (антипаттерн). Требуется устранение позже.

Manual HttpClient: 1 (UserManager.RefreshBanlist) — заменить на IHttpClientFactory.

Feature vs Services разделение:
- Feature слои регистрируют фасады и policy (ModerationFeature: IModerationPolicy + IModerationFacade; UserJoinFeature аналогично).
- ModerationModule оставлен как адаптер (IModerationService -> IModerationPolicy) для legacy потребителей.

Политики:
- ModerationPolicy: 664 code LOC (файл 900 total) — главный концентратор правил.
- UserJoinPolicy: 133 code LOC.

Handlers слой: MessageHandler (387 code LOC) + другие (не детализированы здесь) управляют routing и делегированием.

## 2. Эффекты (Effects Pipeline)
LOC по файлам (сумма кода: 405):
- ChannelEffects.cs: 90 code LOC (ChannelAllow/Delete/Report/Ban/Unknown)
- AllowMessageEffect.cs: 38
- BanUserEffect.cs: 37
- RequireAiAnalysisEffect.cs: 33
- RequireManualReviewEffect.cs: 30
- DeleteWithReportEffect.cs: 29
- TrackViolationEffect.cs: 29
- ReportMessageEffect.cs: 29
- DeleteToLogEffect.cs: 26
- EffectBus.cs: 13
- FuncEffect helper: 11

IEffect occurrences: 20 (интерфейс внедрён точечно). Расширяемость: высока, т.к. эффекты мелкие и однотипные, но есть смешение доменной логики (TrackViolation, BanUser).

Риски:
- Эффекты смешивают разные уровни (audit vs business). Нужен слой нормализации (EffectKind/Category).
- ModerationEffectsBuilder (не включён в этот отчёт кодом) вероятно использует service locator: GetRequiredService внутри builder (подтвердить и выделить в TODO).

## 3. Конфигурация (Static Config vs Options)
Config.cs: 274 code LOC, 71 blank, 159 comment.
Всего прямых обращений к Config.* (исключая бинарники): 157.
AppConfig проксирует почти все поля -> двойная система.
Options зарегистрированы (AutoBanOptions, ViolationThresholdOptions, FeatureToggleOptions, ChatFilteringOptions) но НЕ потребляются бизнес-слоем напрямую.
Генератор ChatSettingsManager смешан внутрь Config.cs (ответственность хранения = нарушение SRP).

Критичные методы: IsChatAllowed, IsPrivateStartAllowed, IsAiEnabledForChat, IsMediaFilteringDisabledForChat — вызываются из Features/Moderation и Handlers.

## 4. Состояние (In-Memory)
ConcurrentDictionary:
- UserManager._banlist (long -> byte) — volatile ban cache.
- JoinedUserFlags._joinedUserFlags (string -> byte) — трекинг join flags.
- CaptchaService._captchaNeededUsers (string -> CaptchaInfo) — капча воркфлоу.
- StatisticsService._stats (long -> ChatStats) — агрегаты чата.

MemoryCache.Default usages (фрагменты):
- ViolationTracker: счётчики нарушений (TTL 24h) — ядро модерации.
- Buttons/Notification/LogChat/ServiceChatDispatcher: callback payloads (TTL 12h/10m) — UX/интерактив.
- AiChecks: AI результат/кэши (Set, Get) — latency оптимизация.
- BotPermissionsService: silent mode / права.
- UserIndex: хранит текстовые индексы username, перебирает весь MemoryCache (риск O(N)).
- ChatMemberHandler, CallbackQueryHandler: временные сообщения.

Проблемы:
- Нет унифицированного интерфейса (IStateStore) -> нельзя централизовать метрики, eviction, тестовые фейки.
- Смешанные TTL (10m, 12h, 24h) без конфиг-слоя.
- Потенциальные гонки: ConcurrentDictionary ок, но ChatSettingsManager пишет файл без lock.

## 5. Доменная логика / Моделирование
Главный концентратор правил: ModerationPolicy (664 code LOC) — симптомы god-class 2.0 после slimming handler.
Недостаток value objects: UserId/ChatId как long; violation counts как int.
ModerationResult (Action/Reason/Confidence) — хороший start, но нет расширяемого Metadata / Context.
AI профиль анализ возвращает bool (потеря деталей о score/threshold).
TrackViolationEffect смешивает подсчёт и эффект (side effect + бизнес правило).

## 6. Логирование и Scope
BeginScope найден: 1 (MessageHandler). Политики, эффекты, фасады — без scope; контекстные поля (ChatId/UserId/MessageId) повторяются текстом в логах вместо структурированного scope.
Риск: сложный корреляционный анализ, отсутствие requestId/chatId унификации.

## 7. Публичная поверхность (выборка)
Примеры public *Service классов (Handlers, Messaging, UserBan, Notifications, AI, etc.) — поверхностный рост API. Большинство как singleton. Нет явного разграничения Application vs Domain vs Infra namespace (всё под Services/ & Features/).

## 8. Policy слой
ModerationPolicy (664 code LOC) + UserJoinPolicy (133 code LOC) — Policy слой фактически инкапсулирует правила, но без модульности Rule-per-file. Требуется Rule decomposition (IIndividualRule interface) для тестируемости.

## 9. Ключевые количественные метрики (Baseline)
| Артефакт | Code LOC |
|----------|----------|
| MessageHandler | 387 |
| ModerationPolicy | 664 |
| UserJoinPolicy | 133 |
| Config.cs | 274 |
| Effects (сумма) | 405 |

DI Counts:
- Singleton registrations: 72
- Scoped: 0
- Transient: 0
- BuildServiceProvider misuse: 1
- Manual HttpClient: 1
- Config references: 157
- BeginScope usages: 1

## 10. Риск-матрица (обновлённая)
| Риск | Уровень | Причина |
|------|---------|---------|
| Все singleton (нет scoped) | High | Сложно вводить request-level state / тестировать изоляцию |
| ModerationPolicy размер | High | Высокий шанс регрессий при изменении правил |
| Static Config dual usage | High | Расхождение значений / трудность миграции (#45) |
| MemoryCache разброс TTL | Medium | Непредсказуемая память, нет централизованного мониторинга |
| BuildServiceProvider в Program | Medium | Дубли singleton, скрытые side-effects |
| Отсутствие scope контекстов | Medium | Сложно трассировать цепочку событий |
| ChatSettingsManager без lock | Medium | Потенциальная гонка при записи файла |
| Manual HttpClient | Low->Medium | Потенциальная resource leak / отсутствие pool tuning |
| Effect смешение (TrackViolation) | Medium | Трудно переиспользовать/тестировать отдельно подсчёт |

## 11. Приоритетный план (Refactor Roadmap v2)
1. Safety Layer
   - Убрать BuildServiceProvider() в PostConfigure (заменить на IHostedService init или ILogger injection через factory pattern).
   - Ввести LoggingScopeEnhancer (adapter) + добавить ChatId/UserId/MessageId.
2. Config Migration (Batch 1)
   - Suspicion + AI + Approval mode -> Options, parity tests.
   - Отделить ChatSettingsManager в `IChatSettingsStore` (singleton + lock).
3. State Abstraction
   - IViolationStore, ICaptchaGateStore, IUserBanCache, IUserJoinFlagsStore (wrap MemoryCache/ConcurrentDictionary).
   - Ввести ICacheClock (для тестируемости TTL).
4. Policy Decomposition
   - Выделить первые 3 правила (e.g. MediaFilterRule, SuspicionRule, LookAlikeRule) из ModerationPolicy.
5. Effects Purification
   - Вынести TrackViolation из эффекта в Rule decision stage (produce “IncrementViolation” domain event → effect executor преобразует).
6. Introduce Domain Context
   - ModerationContext (UserId, ChatId, TextSnippet, Flags, AiEligible).
7. DI Lifetime Rebalancing
   - Рассмотреть scoped для эффект-пайплайна (если появятся request-specific caches) — пока можно оставить singleton, но подготовить к изменению.
8. HttpClient Factory
   - IUserManager: внедрить IHttpClientFactory, добавить policy (timeout handler, retry optional).
9. Unified Cache Options
   - CacheOptions (ViolationTtl, CaptchaTtl, CallbackTtl) -> IOptions<CacheOptions>.
10. Logging / Telemetry
   - Ввести IEffectsMonitoringService (уже есть класс) -> агрегировать статистику (counts per effect).

## 12. Быстрые win’ы (Day 1)
- Удалить BuildServiceProvider hack.
- Добавить scope enrichment wrapper.
- Создать интерфейсы пустые обёртки над 2–3 ключевыми MemoryCache зонами (без поведения) — снизить прямые зависимостии.

## 13. Definition of Done для первых батчей
| Batch | DoD |
|-------|-----|
| Config Batch 1 | Паритет тест: старое == новое для Suspicion/AI; Config references уменьшены >= X |
| State Batch 1 | Тесты IViolationStore: increment/expiry; ViolationTracker больше не обращается напрямую к MemoryCache.Default |
| Policy Batch 1 | ModerationPolicy LOC -50 без изменения логов |

## 14. Следующие действия
1. Подтвердить формат и утвердить план.
2. Начать с Safety Layer (BuildServiceProvider removal + scope расширение) или с Config Batch 1 (если нужна конфиг чистка первее).

## 15. TODO (метки для будущего файла прогресса)
- [ ] Remove BuildServiceProvider call
- [ ] Introduce IChatSettingsStore
- [ ] Introduce IViolationStore
- [ ] SuspicionOptions parity tests
- [ ] Scope enrichment (ChatId/UserId/MessageId)
- [ ] ModerationPolicy rule extraction (first rule)

---
Документ сформирован автоматически на основе свежих grep/cloc/структурных сканов. Обновлять по завершению каждого батча.
