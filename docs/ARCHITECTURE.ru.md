# Архитектура ClubDoorman (Message Pipeline, Фаза 1)

> Статус: Фаза 1 (основной pipeline сообщений выделен и стабилен). Все тесты зелёные (929) после упрощения конструктора.

## 1. Общая схема
Входящие Telegram-обновления проходят через единый диспетчер (`IUpdateDispatcher`), который распределяет их между зарегистрированными обработчиками (`IUpdateHandler`). `MessageHandler` теперь — тонкий слой оркестрации: выполняет предварительные проверки (whitelist, отключённые чаты, тихий режим) и делегирует всю семантику в упорядоченный pipeline.

Golden Master (GM) фиксирует входные данные и публикует нормализованные события модерации, чтобы любые рефакторинги можно было валидировать по логам и событиям без переписывания тестов.

```
Update -> Dispatcher -> MessageHandler
  └─ prechecks (whitelist, disabled, silent)
      └─ IMessagePipeline.RunAsync(context)
           (упорядоченный список IMessageStep; остановка на первом обработанном результате)
                ↓ публикация ModerationEvents + GM-трассировка
```

## 2. Ответственность MessageHandler (после упрощения)
- Корреляция и запись входных данных через `IGoldenMasterRecorder`.
- Быстрый возврат при: неразрешённом чате, отключённом чате.
- Разрешение тихого режима (через сервис прав) и аннотирование контекста.
- Создание `MessageContext` и запуск pipeline.
- Сохранение ключевых лог-фраз для тестов на мутацию/золотой мастер.
- Нет прямой логики модерации, капчи, вступления, бана, AI или команд.
- Временно: владеет хелпером DeleteMessageLater (будет вынесен).

## 3. Модель pipeline
Кратко:
- `IMessagePipeline`: оркестрация шагов (`IMessageStep`) по порядку.
- `IMessageStep`: реализует `Order` (int) + `Task ExecuteAsync(MessageContext, CancellationToken)`.
- `MessageContext`: mutable bag (Update, Message, Chat/User, флаги, operation id, gm correlation, статус, результат модерации и др.).
- Семантика результата шага — через флаги контекста (`CommandHandled`, `NewMembersHandled`, `UserResultHandled` и др.).

### Текущий порядок шагов (Фаза 1)
| Order | Step | Назначение | Остановка |
|-------|------|------------|-----------|
| 10 | CommandStep | Обработка команд | При обработке команды |
| 15 | SystemOrBotMessageStep | Пропуск системных/бот-сообщений | Всегда, если system/bot |
| 20 | NewMembersStep | Обработка вступлений | При обработке новых участников |
| 30 | LeftMemberCleanupStep | Очистка после выхода | При обработке |
| 40 | ChannelMessageStep | Модерация каналов | При обработке |
| 50 | PrivateSkipStep | Пропуск приватных | При обработке |
| 100 | CaptchaPendingStep | Капча | По результату |
| 110 | BanlistCheckStep | Банлист/автобан | По результату |
| 120 | AlreadyApprovedStep | Пропуск одобренных | По результату |
| 130 | FirstMessageLogStep | Лог первого появления | Нет (только сайд-эффект) |
| 140 | ClubMemberSkipStep | Пропуск клубных | По результату |
| 200 | BaseModerationStep | ML/хейуристика | По результату (Allow/Delete/Ban/Report/RequireManualReview/RequireAiAnalysis) |
| 210 | AiProfileAnalysisStep | AI-анализ профиля | Не перекрывает финальные действия, только обогащение/эскалация |
| 220 | FinalModerationActionStep | Финальное действие (delete/ban/report) + события | Терминальный |

Все legacy-ветки удалены: нет fallback-команд, нет пост-пайплайн модерации в MessageHandler.

## 4. Golden Master и события
- `IGoldenMasterRecorder.TryRecordInput` помечает update через `gmCorrelation` (scope enrichment & log parity anchor).
- `IModerationEventPublisher` публикует нормализованные события (rule/action) из шагов, отвязывая тесты от внутренней ветвистости.
- Флаг логирования `LoggingFlagsOptions.TraceEnabled` может повысить уровень логов для файлового sink (диагностика) без засорения консоли.

## 5. DI: зависимости
Конструктор MessageHandler (упрощён):
```
(bot, appConfig, channelModeration, commandRouter,
 logger, botPermissions, goldenMaster, eventsPublisher,
 IOptions<LoggingFlagsOptions>, pipeline)
```
Контейнер регистрирует каждый шаг pipeline как singleton `IMessageStep`; `MessagePipeline` получает `IEnumerable<IMessageStep>` и сортирует.

Legacy-зависимости удалены из конструктора (остались только там, где реально нужны шагам/сервисам):
`IUserManager`, `IUserBanService`, `IUserJoinFacade`, `IModerationFacade`, `ICaptchaService`, `IUserFlowLogger`, `IForwardingService`, `IAiCascadeService`.

## 6. Тестирование и Golden Master
- 929 тестов зелёные; золотые/семантические тесты опираются на сохранённые лог-фразы (например, "MessageHandler получил сообщение").
- Все тесты используют pipeline; адаптер `HandleUserMessageAsync` временно сохранён для фабрик.
- Фабрика тестов строит реальный мини-пайплайн для проверки взаимодействия шагов.

## 7. Текущий технический долг / риски
| Зона | Долг | Влияние | Митигация |
|------|------|---------|-----------|
| Дублирование gating | Prechecks только в MessageHandler | Сложно тестировать отдельно | Вынести в IChatAccessGate |
| Inline-отложка | DeleteMessageLater — fire-and-forget Task | Сложно наблюдать/отменять, возможны флаки | Вынести в IDeletionScheduler |
| Разрастание фабрики | Остаются моки/настройки, не нужные pipeline | Шум, поддержка | Упростить, сделать билдеры |
| Связность шагов | Шаги предполагают сайд-эффекты предыдущих | Жёсткий порядок | Ввести контракты + валидатор |
| Логовый шум | Много debug-логов из шагов | Перегрузка CI-логов | Ввести уровни/категории/сэмплирование |

## 8. Дорожная карта (TODO)
### TODO 1: ChatAccessGate
- Вынести whitelist/disabled/silent из MessageHandler в IChatAccessGate.
- Метод: `ChatGateResult Evaluate(Message updateMessage, CancellationToken)` (Allowed, Disabled, NotWhitelisted, SilentMode, AdminChatOverride).
- Добавить pre-step pipeline (order < 10) или запускать до pipeline; публиковать события для наблюдаемости.
- Плюсы: можно тестировать gating отдельно, MessageHandler — только оркестрация.

### TODO 2: Сервис отложенного удаления
- Создать `IDeletionScheduler` с `ScheduleDeletion(Message, TimeSpan, CancellationToken)`.
- Использовать канал/Task.Delay с централизованным логированием и отменой (остановка host).
- Инъекция в шаги, где требуется удаление (сейчас только финальный шаг или хелпер).
- Для тестов — фейковый scheduler, фиксирующий intent.

### TODO 3: Упрощение фабрики тестов
- Ввести билдеры: BasicHandlerBuilder (только command/system), ModerationHandlerBuilder (100–220), спец-сценарии.
- Удалить legacy-методы, дублирующие ветвление до pipeline; оставить только setup для нужного шага.
- Глобальные моки — только там, где реально нужны; остальное — локально.
- Проверять, что используются только нужные pipeline-моки (анализатор/рефлексия).

### TODO 4: Контракты шагов
- Описать минимальные поля контекста, которые читает/пишет каждый шаг (таблица).
- Добавить debug-валидатор (в Trace-режиме), проверяющий инварианты (например, если UserResultHandled — ModerationResult не null).
- Упростит безопасную перестановку/вставку/удаление шагов.

### TODO 5: Логирование
- Ввести категорию/уровень логов для каждого шага (StepName, Order).
- Расширить LoggingFlagsOptions: PipelineTrace, ModerationTrace, AiTrace.
- Структурировать eventId для исходов модерации.

### TODO 6: Производительность
- Бенчмарк на волнах спама: измерить latency pipeline.
- Пулить временные объекты (ObjectPool<MessageContext>).
- Избегать лишних лямбда-аллоков в часто вызываемых шагах.

### TODO 7: Защита AI-анализа
- Backoff/circuit-breaker при повторных 401/5xx от OpenRouter.
- Кэшировать отрицательные результаты для предотвращения повторного анализа.

### TODO 8: Наблюдаемость
- Прометей-метрики: pipeline_step_executed_total{step="Command"}, action counts, skip reasons.
- Гистограммы latency по шагам.

### TODO 9: Эволюция Golden Master
- Анонимизация чувствительных данных перед логированием.
- Инструмент сравнения event stream между ветками.

### TODO 10: Устойчивость
- Таймаут/CTS для каждого шага (по умолчанию 2s) + агрегированный отчёт.
- Graceful degradation при недоступности AI/ML (event tag: degraded=true).

---

## 9. Гайд по контрибьюциям (pipeline)
1. Новый шаг — уникальный Order (10–220, <10 — для gating).
2. Документировать контракт в этой доке.
3. Минимальные unit-тесты для шага (happy + skip/edge).
4. Проверить, что golden/semantics тесты зелёные (стандартный suite).

## 10. Быстрые ссылки
- Код обработчика: `Services/Handlers/MessageHandler.cs`
- Ядро pipeline: `Services/Handlers/Pipeline/*`
- DI: `Infrastructure/ServiceCollectionExtensions.cs`
- Фабрика тестов: `ClubDoorman.Test/TestInfrastructure/MessageHandlerTestFactory.cs`

---
Последнее обновление: завершение фазы 1 (упрощение конструктора). Следующие шаги: (1) реализация этой доки (готово), (2) чистка фабрики тестов, (3) начало ChatAccessGate.
