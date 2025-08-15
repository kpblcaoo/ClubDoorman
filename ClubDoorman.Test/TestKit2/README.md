# TestKit2

Упрощенный и удобный TestKit для тестирования ClubDoorman с использованием AutoFixture + AutoMoq.

## 🎯 Принципы

1. **Простота** - минимум кода для максимума функциональности
2. **AutoFixture + AutoMoq** - автоматическое создание моков и зависимостей
3. **Фейкаем только то, что нужно** - реальные сервисы где возможно
4. **Понятность** - очевидные имена и структура

## 🚀 Быстрый старт

### 1. Создание TestApp

```csharp
// Простое создание с AutoFixture
using var app = TestKit2.CreateApp();

// Доступ к фейкам
app.TelegramClient.SendMessageAsync(123, "Hello");
app.UserBanService.BanOperations.Count;
app.EffectsSink.Snapshot();
```

### 2. Создание объектов с AutoFixture

```csharp
// Любой объект с автозависимостями
var handler = TestKit2.CreateMessageHandler();
var service = TestKit2.Create<IModerationService>();
var user = TestKit2.Create<Telegram.Bot.Types.User>();

// Несколько объектов
var users = TestKit2.CreateMany<Telegram.Bot.Types.User>(3);

// С кастомизацией
var customUser = TestKit2.CreateWith<Telegram.Bot.Types.User>(u => 
{
    u.Id = 123;
    u.FirstName = "Custom";
});
```

### 3. Создание тестовых данных

```csharp
// MessageEnvelope для разных сценариев
var envelope = TestKit2.CreateEnvelope(text: "Normal message");
var spamEnvelope = TestKit2.CreateSpamEnvelope();
var commandEnvelope = TestKit2.CreateCommandEnvelope(command: "/start");
var newUserEnvelope = TestKit2.CreateNewUserEnvelope();

// Преобразование в Telegram типы
var message = TestKit2.CreateMessageFromEnvelope(envelope);
var update = TestKit2.CreateUpdateFromEnvelope(envelope);
```

## 📝 Примеры тестов

### Простой тест

```csharp
[Fact]
public async Task Test_SimpleMessage()
{
    // Arrange
    using var app = TestKit2.CreateApp();
    var envelope = TestKit2.CreateEnvelope(text: "Hello");
    var update = TestKit2.CreateUpdateFromEnvelope(envelope);
    
    // Act
    // await handler.HandleAsync(update, CancellationToken.None);
    
    // Assert
    update.Message.Should().NotBeNull();
    update.Message.Text.Should().Be("Hello");
}
```

### Тест с кастомизацией

```csharp
[Fact]
public async Task Test_CustomScenario()
{
    // Arrange
    using var app = TestKit2.CreateApp();
    var customUser = TestKit2.CreateWith<Telegram.Bot.Types.User>(u => 
    {
        u.Id = 12345;
        u.FirstName = "John";
        u.LastName = "Doe";
    });
    
    // Act & Assert
    customUser.Id.Should().Be(12345);
    customUser.FirstName.Should().Be("John");
}
```

### Тест с фейками

```csharp
[Fact]
public async Task Test_TelegramClient()
{
    // Arrange
    using var app = TestKit2.CreateApp();
    
    // Act
    await app.TelegramClient.SendMessageAsync(123, "Test message");
    
    // Assert
    app.TelegramClient.SentMessages.Should().HaveCount(1);
    app.TelegramClient.SentMessages[0].Text.Should().Be("Test message");
}
```

## 🔧 Фейки

### FakeTelegramBotClientWrapper
- Отслеживает отправленные сообщения (`SentMessages`)
- Отслеживает удаленные сообщения (`DeletedMessages`)
- Отслеживает забаненных пользователей (`BannedUsers`)
- Логирует операции (`OperationLog`)

### FakeUserBanService
- Отслеживает операции бана (`BanOperations`)
- Отслеживает удаления сообщений (`DeleteMessageOperations`)

### FakeBotPermissionsService
- Управляет правами бота
- Поддерживает тихий режим
- Проверяет права администратора

## 🎯 Преимущества

1. **Автоматические моки** - AutoFixture создает моки для всех зависимостей
2. **Минимум кода** - не нужно создавать множество фейков
3. **Гибкость** - легко кастомизировать через `CreateWith<T>()`
4. **Совместимость** - работает с существующими тестами
5. **Простота** - понятный API без сложной конфигурации

## 🔄 Миграция с TestKit

```csharp
// Старый способ
var handler = TK.CreateMessageHandler();
var message = TK.CreateMessage();

// Новый способ
var handler = TestKit2.CreateMessageHandler();
var envelope = TestKit2.CreateEnvelope();
var message = TestKit2.CreateMessageFromEnvelope(envelope);
```

## 📁 Структура

```
TestKit2/
├── Core/                           # Основные компоненты
│   ├── TestApp.cs                 # Главный контейнер
│   ├── TestAppBuilder.cs          # Билдер для TestApp
│   ├── TestDataFactory.cs         # Фабрика тестовых данных
│   ├── MessageEnvelope.cs         # Обертка для сообщений
│   └── EffectsSink.cs             # Запись эффектов
├── Fakes/                         # Фейковые сервисы
│   ├── FakeTelegramBotClientWrapper.cs
│   ├── FakeUserBanService.cs
│   └── FakeBotPermissionsService.cs
├── Scenarios/                     # Примеры тестов
│   ├── SimpleTests.cs
│   ├── ExampleUsage.cs
│   └── MessageHandlerIntegrationTest.cs
├── TestKit2.cs                    # Главный API
└── README.md                      # Документация
```

## ✅ Статус

**Готово к использованию**
- ✅ Использует AutoFixture + AutoMoq
- ✅ Минимальные фейки только для критичных сервисов
- ✅ Простой и понятный API
- ✅ Совместимость с существующими тестами
- ✅ Все тесты проходят (26/26)
- ✅ Проект компилируется без ошибок

## 🧪 Запуск тестов

```bash
# Все тесты TestKit2
dotnet test --filter "TestKit2"

# Конкретный тест
dotnet test --filter "TestKit2.Scenarios.SimpleTests"
```

## 💡 Советы

1. **Используйте `using var app = TestKit2.CreateApp()`** для автоматической очистки ресурсов
2. **Предпочитайте `CreateEnvelope()`** вместо прямого создания Message
3. **Используйте `CreateWith<T>()`** для кастомизации объектов
4. **Фейкайте только то, что действительно нужно** - AutoFixture создаст остальное
5. **Пишите простые тесты** - сложность не всегда означает качество
