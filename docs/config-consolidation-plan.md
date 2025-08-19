# Консолидация конфигурации (Config.cs -> strongly-typed options)

Цель: Полностью убрать статический класс `Config` и перевести все обращения на `IAppConfig` + `IOptions<T>`.

## Этапы

1. Инвентаризация свойств `Config.cs` и их использования.
2. Создание новых `Options` классов для оставшихся групп настроек:
   - CoreOptions (BotApi, AdminChatId, LogAdminChatId, ClubServiceToken, ClubUrl)
   - ChatAccessOptions (DisabledChats, WhitelistChats, NoVpnAdGroups, NoCaptchaGroups)
   - AiOptions (OpenRouterApi, AiEnabledChats, SuspiciousDetectionEnabled, MimicryThreshold, SuspiciousToApprovedMessageCount)
   - ViolationThresholdOptions (уже есть)
   - FeatureToggleOptions (уже есть)
   - AutoBanOptions (уже есть)
   - ChatFilteringOptions (MediaFilteringDisabledChats + DisableMediaFiltering в FeatureToggleOptions)
3. Маппинг env переменных -> sections/appsettings.json с `BinderOptions.BindNonPublicProperties = true` если нужно.
4. Расширение `AddConfigurationServices()` для регистрации новых options и построения `AppConfig` без прямых ссылок на `Config`.
5. Итеративная замена обращений `Config.*` в сервисах на `IAppConfig`.
6. Удаление неиспользуемых методов из `Config.cs` по мере миграции (PR-ами малыми порциями).
7. Добавление тестов на маппинг конфигурации (пример: подменяем `IOptions<T>` через `Options.Create()` и проверяем поведение сервисов).
8. Полное удаление `Config.cs`, обновление документации и README.

## Приоритет очередности замены

1. Примитивные поля (BotApi, AdminChatId, LogAdminChatId).
2. Коллекции (DisabledChats, WhitelistChats, NoVpnAdGroups, NoCaptchaGroups).
3. AI / Suspicious (OpenRouterApi, SuspiciousDetectionEnabled, MimicryThreshold, SuspiciousToApprovedMessageCount, AiEnabledChats + методы IsAiEnabledForChat, IsChatAllowed, IsPrivateStartAllowed).
4. Violation thresholds (уже есть, только отвязать от статического).
5. Остаток: TextMentionFilterEnabled, BanFolderInviteUsers (распределить в уже существующие options), RepeatedViolationsBanToAdminChat (уже в FeatureToggleOptions), DeleteForwardedMessages.

## Контрольные точки

| Stage | Цель | Метрика |
|-------|------|---------|
| S1 | Добавлены новые Option-классы | Компилируется, тесты зелёные |
| S2 | AppConfig не использует `Config` для X% полей | grep 'Config.' уменьшается |
| S3 | Все обращения к `Config` вне `AppConfig` устранены | grep возвращает 0 за исключением самого файла Config.cs |
| S4 | `Config` удалён | build ok |

## Обратная совместимость

До финального удаления `Config.cs` переменные окружения остаются источником истины. Новые options будут считывать из тех же env напрямую через `Configure<Options>(...)` без потребности в appsettings.* пока не будет создан единый конфиг файл.

## Риски

- Ошибки парсинга коллекций (рекомендуется вынести общий парсер строк -> HashSet<long>).
- Несинхронное поведение (сейчас `Config` иммутабелен после старта; options по умолчанию тоже иммутабельны — ок).
- Тесты, которые не поднимают env переменные — нужно внедрить фабрики с `Options.Create()`.

## Следующий шаг (в этом PR)

Добавить новые Option-классы CoreOptions, ChatAccessOptions, AiOptions и адаптировать `AppConfig` для их использования параллельно со старым `Config` (feature parity). Затем начать замену прямых обращений (UserManager, CaptchaService и др.).

---

Документ обновляется по мере прогресса.
