# Worklog: 01-explicit-deps

## TODO
- [ ] Найти все использования IServiceProvider/GetService/GetRequiredService в handler'ах и сервисах
- [ ] Добавить зависимости в конструкторы
- [ ] Обновить DI-регистрации
- [ ] Удалить сервис-локатор из кода
- [ ] Прогнать тесты
- [ ] Описать изменения

## Log
- StartCommandHandler, SuspiciousCommandHandler, ILogChatService уже реализованы, но используются через локатор — требуется доработка.
- ...
