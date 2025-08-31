# Worklog: 01-explicit-deps

## COMPLETED
- [x] Найти все использования IServiceProvider/GetService/GetRequiredService в handler'ах и сервисах
- [x] Добавить зависимости в конструкторы
- [x] Обновить DI-регистрации (все сервисы уже зарегистрированы в модулях)
- [x] Удалить сервис-локатор из кода
- [x] Основной проект компилируется без ошибок
- [ ] Прогнать тесты (тестовая инфраструктура нуждается в доработке)
- [x] Описать изменения

## TODO
- [ ] Завершить исправление тестовой инфраструктуры (10 файлов нуждаются в обновлении)
- [ ] Проверить работоспособность через интеграционные тесты

## Log
- ✅ StartCommandHandler, SuspiciousCommandHandler, ILogChatService больше не используются через локатор
- ✅ MessageHandler.cs: удалены 4 использования сервис-локатора
  - Конструктор: `serviceProvider.GetService<IChannelModerationService>()` → прямая зависимость
  - HandleCommandAsync: `_serviceProvider.GetRequiredService<StartCommandHandler>()` → прямое поле _startCommandHandler
  - HandleCommandAsync: `_serviceProvider.GetRequiredService<SuspiciousCommandHandler>()` → прямое поле _suspiciousCommandHandler  
  - DeleteAndReportToLogChat: `_serviceProvider.GetRequiredService<ILogChatService>()` → прямое поле _logChatService
- ✅ CallbackQueryHandler.cs: удалено 1 использование сервис-локатора
  - HandleLogBanUser: `_serviceProvider.GetRequiredService<ILogChatService>()` → прямое поле _logChatService
- ✅ Все зависимости теперь явно объявлены в конструкторах
- ✅ Основная цель достигнута: нет ни одного обращения к IServiceProvider внутри handler'ов и сервисов
- ✅ Все изменения бизнес-логики отсутствуют - тела методов остались идентичными
- ⚠️ Тестовая инфраструктура частично обновлена, остается 10 ошибок компиляции в тестах
