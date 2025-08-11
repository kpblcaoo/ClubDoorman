# Мажорный чеклист рефакторинга ClubDoorman (2025)

## Общий план

1. Перевести все сервисы и handler'ы на явные зависимости через конструктор (убрать сервис-локатор).
2. Вынести обработку команд в отдельный CommandRouter и специализированные CommandHandler'ы.
3. Разгрузить MessageHandler: вынести AI, уведомления, пересылки, кнопки и т.д. в отдельные сервисы.
4. Ввести отдельный индекс username → userId для MemoryCache (UserIndex), убрать O(n) обходы.
5. Внедрить логирование с BeginScope и корреляцией по ChatId/UserId/MsgId/TraceId.
6. Покрыть изменения интеграционными и unit-тестами.
7. Провести ревью и поэтапное слияние веток.

---

## Чеклист по этапам

- [ ] 01-explicit-deps: Все зависимости через конструктор, без IServiceProvider внутри handler'ов
- [ ] 02-command-router: CommandRouter и отдельные CommandHandler'ы, DI-регистрация
- [ ] 03-handler-slimming: Вынесение AI, уведомлений, пересылок, кнопок и т.д. в отдельные сервисы
- [ ] 04-cache-index: UserIndex для username → userId, отказ от O(n) MemoryCache
- [ ] 05-logging-scope: BeginScope и корреляция логов
- [ ] Интеграционные и unit-тесты для каждого этапа
- [ ] Документация по изменениям
- [ ] Финальное ревью и слияние

---

## Ветки для каждого этапа

- `refactor/explicit-deps`
- `refactor/command-router`
- `refactor/handler-slimming`
- `refactor/cache-index`
- `refactor/logging-scope`

(Ветки можно создавать по мере продвижения, не обязательно все сразу)

---

## Уже реализовано/частично реализовано

- StartCommandHandler и SuspiciousCommandHandler уже существуют как отдельные классы и регистрируются в DI, но вызываются через сервис-локатор, а не через CommandRouter.
- ILogChatService реализован и регистрируется, но используется через сервис-локатор.
- Для UserIndex, BeginScope, CommandRouter, INewUserGate, IModerationNotifier, IAiCascadeService — реализаций пока нет, только заделы в планах.

**Это нужно учесть при декомпозиции задач: часть кода уже есть, но требует доработки для чистой архитектуры.**
