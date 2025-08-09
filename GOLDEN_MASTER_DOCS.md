# Golden Master и Трейсинг

Этот документ описывает функциональность Golden Master и трейсинга, добавленную в ClubDoorman для обеспечения стабильности при рефакторинге.

## Обзор

Golden Master (GM) и трейсинг предоставляют "страховку поведения" перед дальнейшими правками кода:
- **Golden Master**: записывает входные и выходные данные в канонизированном JSON формате
- **Трейсинг**: логирует ключевые события в обработке с корреляционными ID

## Конфигурация

Добавьте в `appsettings.json`:

```json
{
  "LoggingFlags": {
    "TraceEnabled": true,
    "GoldenMasterEnabled": true,
    "GoldenSampleRate": 0.1
  }
}
```

### Параметры конфигурации

- `TraceEnabled` (bool): Включить трейсинг событий
- `GoldenMasterEnabled` (bool): Включить сбор Golden Master данных
- `GoldenSampleRate` (double): Коэффициент сэмплирования (0.1 = 10%)

## Логирование

### Serilog конфигурация

Трейсы записываются в отдельный файл `logs/trace-.json` с категорией `ClubDoorman.Trace`:

```csharp
.WriteTo.Logger(lc => lc
    .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("SourceContext") && 
        e.Properties["SourceContext"].ToString().Contains("ClubDoorman.Trace"))
    .MinimumLevel.Debug()
    .WriteTo.Async(a => a.File(
        new Serilog.Formatting.Compact.CompactJsonFormatter(),
        path: Path.Combine(logsDir, "trace-.json"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7
    ))
)
```

## Golden Master

### Структура файлов

Golden Master файлы сохраняются в структуре:
```
golden/
  2024-01-15/
    MessageHandler/
      12345.json
      67890.json
    ChatMemberHandler/
      -100123456789.json
    CallbackQueryHandler/
      callback_hash_123.json
```

### Формат данных

Каждый файл содержит канонизированный JSON:

```json
{
  "input": {
    "update": { /* Telegram Update объект */ },
    "timestamp": "2024-01-01T00:00:00Z"
  },
  "output": {
    "success": true,
    "timestamp": "2024-01-01T00:00:00Z"
  },
  "handlerName": "MessageHandler",
  "messageId": 12345,
  "timestamp": "2024-01-01T00:00:00Z"
}
```

### Канонизация данных

Для стабильного сравнения применяется канонизация:

1. **Временные метки**: нормализованы к `2024-01-01T00:00:00Z`
2. **GUID**: заменены на `00000000-0000-0000-0000-000000000000`
3. **PII данные**: замаскированы:
   - Телефоны → `PHONE_MASKED`
   - Токены → `BOT_TOKEN_MASKED`
   - Username → `@u***`
4. **Числа**: округлены до 3 знаков после запятой
5. **Коллекции**: отсортированы для стабильного порядка

## Трейсинг

### Корреляционные ID

Каждый запрос получает корреляционный scope с:
- `MessageId`: ID сообщения
- `ChatId`: ID чата
- `RequestId`: уникальный ID запроса

### Трейс-события

Ключевые точки трейсинга:

#### MessageHandler
- `Routed->MessageHandler`
- `MessageHandler->Command`
- `MessageHandler->NewMembers`
- `MessageHandler->Channel`
- `MessageHandler->UserMessage`
- `MessageHandler->Completed/Error`

#### ChatMemberHandler
- `Routed->ChatMemberHandler`
- `ChatMemberHandler->NewMember`
- `ChatMemberHandler->IntroFlow`
- `ChatMemberHandler->FolderBan`
- `ChatMemberHandler->Restricted`
- `ChatMemberHandler->RemovedFromApproved`
- `ChatMemberHandler->Completed/Error`

#### CallbackQueryHandler
- `Routed->CallbackQueryHandler`
- `CallbackQueryHandler->AdminCallback`
- `CallbackQueryHandler->UserCallback`
- `CallbackQueryHandler->Completed/Error`

## Использование в тестах

### Интеграционный тест

```csharp
[Test]
public async Task MessageHandler_WithGoldenMaster_RecordsData()
{
    // Arrange
    var loggingFlags = LoggingFlagsTestHelper.CreateMockLoggingFlags(
        goldenMasterEnabled: true, sampleRate: 1.0);
    
    var handler = CreateMessageHandler(loggingFlags);
    var update = CreateTestUpdate();
    
    // Act
    await handler.HandleAsync(update);
    
    // Assert
    var goldenFile = Path.Combine("golden", "...", "MessageHandler", "12345.json");
    var content = await File.ReadAllTextAsync(goldenFile);
    // Проверяем содержимое...
}
```

### Проверка Golden Master

```csharp
var expectedJson = await GoldenMasterRecorder.ReadAsync("MessageHandler", 12345);
var actualJson = JsonCanonicalizer.Canonicalize(actualResult);
actualJson.Should().Be(expectedJson);
```

## Очистка данных

### Автоматическая ротация

- Логи трейсов: хранятся 7 дней
- Golden Master файлы: нет автоматической очистки (настраивается отдельно)

### Ручная очистка

```bash
# Очистка старых Golden Master файлов
rm -rf golden/2024-01-*

# Очистка трейс-логов
rm logs/trace-*.json
```

## Производительность

### Отключение в продакшене

Для отключения без перекомпиляции:

```json
{
  "LoggingFlags": {
    "TraceEnabled": false,
    "GoldenMasterEnabled": false,
    "GoldenSampleRate": 0.0
  }
}
```

### Сэмплирование

Golden Master использует детерминированное сэмплирование на основе `messageId`:
- `messageId % 100 < (sampleRate * 100)`
- Гарантирует стабильную выборку для одинаковых сообщений

## Безопасность

### Защита PII

Автоматическое маскирование:
- Номера телефонов
- Токены ботов
- Имена пользователей (частично)
- Другие потенциально чувствительные данные

### Конфигурация доступа

Убедитесь, что директории `golden/` и `logs/` имеют правильные права доступа в продакшене.