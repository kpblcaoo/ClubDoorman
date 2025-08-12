# TestKit v0 - Тестовая инфраструктура ClubDoorman

## 🎯 TestKit v0 - Новая архитектура тестирования

TestKit v0 - это комплексная система автоматизированного тестирования с поддержкой golden master тестов, фейковых реализаций и DI-харнесса для фиксации текущего поведения ClubDoorman.

### 🚀 Ключевые возможности TestKit v0:
- **Фейковые реализации** всех внешних зависимостей с транскриптами
- **DI-харнесс** для замены реализаций в тестах
- **Golden Master тесты** для фиксации поведения ключевых сценариев
- **Расширенные билдеры** для создания типовых тестовых сценариев
- **Детерминированные провайдеры** времени и случайных чисел

## 📦 Компоненты TestKit

### Core (Существующие)
- **TestKit.cs** - Основной класс с единым интерфейсом (674 строки)
- **TestCategories.cs** - Категории тестов для оптимизации CI/CD

### TestKit v0 (Новые компоненты)
- **Infrastructure/TestHostFactory.cs** - DI-харнесс для замены зависимостей фейками
- **Fakes/** - Фейковые реализации для детерминированных тестов
- **Builders/EnhancedBuilders.cs** - Билдеры для типовых сценариев
- **GoldenMaster/GoldenMasterTests.cs** - Golden master тесты ключевых сценариев

### Data Generation (Существующие)
- **TestKit.Bogus.cs** - Генерация реалистичных тестовых данных с помощью Bogus
- **TestKit.Builders.cs** - Fluent API для создания тестовых объектов (Test Data Builders pattern)

### Automation (Существующие)
- **TestKit.AutoFixture.cs** - Автоматическое создание объектов и моков с помощью AutoFixture
- **TestKit.Telegram.cs** - Улучшенная работа с Telegram объектами и решение проблемы MessageId
- **TestKit.Mocks.cs** - Централизованные моки для всех сервисов и компонентов

## 🚀 Быстрый старт TestKit v0

### Простой Golden Master тест
```csharp
[Test]
public async Task MyScenario_TestName_ExpectedBehavior()
{
    // Arrange - создаем тестовый хост с фейками
    var testHost = TestHostFactory.Create()
        .ConfigureAppConfig(config => config.WithAiEnabled(-1001234567890));

    // Создаем тестовые данные с помощью билдеров
    var message = EnhancedBuilders.CreateScenarioMessage()
        .AsSpam()
        .InGroupChat()
        .FromUser(EnhancedBuilders.CreateScenarioUser().AsFirstTimeUser())
        .Build();

    // Act - выполняем сценарий
    var result = await testHost.ExecuteMessageScenarioAsync(message);

    // Assert - проверяем результат
    Assert.That(result.Success, Is.True);
    Assert.That(testHost.BotClient.WasMessageDeleted(message.Chat, message.MessageId), Is.True);
    Assert.That(testHost.BotClient.WasUserBanned(message.Chat, message.From.Id), Is.True);
}
```

### Фейковые реализации
```csharp
// AppConfig фейк для управления флагами
var appConfig = new AppConfigFake()
    .WithAiEnabled(-1001234567890)
    .WithMimicrySettings(enabled: true, threshold: 0.8)
    .WithNoCaptcha(-1009876543210);

// BotClient фейк с транскриптом действий
var botClient = new BotClientFake();
await botClient.SendMessage(chatId, "Test message");
await botClient.BanChatMember(chatId, userId);

// Проверка транскрипта
Assert.That(botClient.WasMessageSent("Test message"), Is.True);
Assert.That(botClient.WasUserBanned(chatId, userId), Is.True);
var transcript = botClient.GetTranscript();

// Детерминированное время
var timeProvider = new TimeProviderFake();
timeProvider.SetTime(new DateTime(2024, 1, 1, 12, 0, 0));
timeProvider.AdvanceMinutes(30);

// Предсказуемая генерация случайных чисел
var randomProvider = new RandomProviderFake()
    .WithNextValues(1, 2, 3)
    .WithDoubleValues(0.5, 0.7, 0.9);
```

### Расширенные билдеры для типовых сценариев
```csharp
// Пользователи
var bannedUser = EnhancedBuilders.CreateScenarioUser().AsBannedUser().Build();
var firstTimeUser = EnhancedBuilders.CreateScenarioUser().AsFirstTimeUser().Build();
var suspiciousUser = EnhancedBuilders.CreateScenarioUser().AsSuspiciousUser().Build();

// Сообщения
var spamMessage = EnhancedBuilders.CreateScenarioMessage()
    .AsSpam()
    .InGroupChat()
    .FromUser(suspiciousUser)
    .Build();

var commandMessage = EnhancedBuilders.CreateScenarioMessage()
    .AsCommand("/help")
    .InPrivateChat()
    .Build();

var forwardedMessage = EnhancedBuilders.CreateScenarioMessage()
    .ForwardedFromChannel()
    .WithText("Forwarded content")
    .Build();

// Update объекты
var messageUpdate = EnhancedBuilders.CreateUpdate()
    .WithMessage(spamMessage)
    .Build();

var callbackUpdate = EnhancedBuilders.CreateUpdate()
    .WithCallbackQuery(
        EnhancedBuilders.CreateCallbackQuery()
            .AsAdminApprove()
            .FromUser(adminUser)
            .Build()
    )
    .Build();
```

### TestHostFactory - DI харнесс
```csharp
// Создание с автоматической регистрацией всех сервисов
var testHost = TestHostFactory.Create()
    .ConfigureAppConfig(config => 
    {
        config.AdminChatId = 123456789;
        config.SuspiciousDetectionEnabled = true;
        config.WithAiEnabled(-1001234567890);
    })
    .ConfigureBotClient(bot => 
    {
        bot.SetResponse("GetMe", new User { Id = 987654321, FirstName = "TestBot" });
    })
    .ConfigureTimeProvider(time => 
    {
        time.SetTime(new DateTime(2024, 1, 1));
    });

// Выполнение сценариев
var result = await testHost.ExecuteMessageScenarioAsync(message);
var callbackResult = await testHost.ExecuteCallbackScenarioAsync(callbackQuery);

// Доступ к фейкам для проверки
Assert.That(testHost.BotClient.GetTranscript().Count, Is.GreaterThan(0));
Assert.That(testHost.ApprovedUsersStorage.IsApproved(userId), Is.True);
```

## 📚 Документация
- **[INDEX.md](INDEX.md)** - Тегированный индекс всех генераторов и моков
- **[TestCategories.cs](TestCategories.cs)** - Категории тестов для CI/CD

## 🎭 Golden Master тесты

Golden Master тесты фиксируют текущее поведение системы для предотвращения регрессий при рефакторинге.

### Поддерживаемые сценарии:

1. **Ban-list сценарии**:
   - `BanList_UserInGroupChat` - участник из банлиста в группе → бан + уведомления
   - `BanList_UserInPrivateChat` - участник из банлиста в личке → логирование + уведомления

2. **Новые пользователи**:
   - `UnapprovedFirstUser_NewMessage` - первое сообщение → капча + блокировка
   - `NewUserJoin_FirstTimeJoin` - присоединение → процедура одобрения

3. **Режимы работы**:
   - `SilentMode_SpamMessage` - тихий режим → модерация без сообщений в чат

4. **Пересылки и команды**:
   - `ChannelForwarding_SuspiciousMessage` - пересылка с канала → проверка + удаление
   - `CommandVsMessage_Command` - команда → CommandRouter
   - `CommandVsMessage_RegularMessage` - обычное сообщение → модератор

### Структура снапшота:
```json
{
  "Success": true,
  "BotActionsCount": 3,
  "BotActionTypes": ["DeleteMessage", "BanChatMember", "SendMessage"],
  "BotActionsSummary": [
    "DeleteMessage(CHAT_ID,MESSAGE_ID)",
    "BanChatMember(CHAT_ID,USER_ID,TIMESTAMP)",
    "SendMessage(CHAT_ID,Admin notification)"
  ],
  "ErrorMessages": [],
  "LogMessagesCount": 2,
  "AdminNotificationsCount": 1,
  "ExecutionTimeMs": 150
}
```

### Обновление снапшотов:
1. При изменении поведения создается файл `*_updated.json`
2. Сравните изменения в файлах
3. Если изменения ожидаемы - замените оригинальный снапшот
4. Зафиксируйте изменения в git

### Добавление новых Golden Master тестов:
```csharp
[Test]
public async Task MyNewScenario_Description_ExpectedBehavior()
{
    // Arrange
    var testHost = TestHostFactory.CreateForCustomScenario();
    var input = EnhancedBuilders.CreateScenarioMessage().AsCustomScenario().Build();

    // Act
    var result = await testHost.ExecuteMessageScenarioAsync(input);

    // Assert & Record
    await AssertAndRecordGoldenMaster("MyNewScenario_Description", result);
}
```

## Основные методы

### Пользователи и чаты
```csharp
var user = TK.CreateValidUser();
var botUser = TK.CreateBotUser();
var groupChat = TK.CreateGroupChat();
var channel = TK.CreateChannel();
```

### Сообщения
```csharp
var validMessage = TK.CreateValidMessage();
var spamMessage = TK.CreateSpamMessage();
var textMessage = TK.CreateTextMessage(userId, chatId, "Hello");
var channelMessage = TK.CreateChannelMessage(senderChatId, chatId, "Channel post");
```

### Специализированные генераторы

#### Капча
```csharp
var captcha = TK.Specialized.Captcha.Bait();
var validCaptcha = TK.Specialized.Captcha.Valid();
var expiredCaptcha = TK.Specialized.Captcha.Expired();
var correctResult = TK.Specialized.Captcha.CorrectResult();
```

#### Модерация
```csharp
var allowResult = TK.Specialized.Moderation.Allow();
var deleteResult = TK.Specialized.Moderation.Delete();
var banResult = TK.Specialized.Moderation.Ban();
```

#### Админские действия
```csharp
var approveCallback = TK.Specialized.Admin.ApproveCallback();
var banCallback = TK.Specialized.Admin.BanCallback();
var notification = TK.Specialized.Admin.Notification();
```

#### Обновления чата
```csharp
var memberJoined = TK.Specialized.Updates.MemberJoined();
var memberBanned = TK.Specialized.Updates.MemberBanned();
var memberLeft = TK.Specialized.Updates.MemberLeft();
```

## Продвинутые возможности

### AutoFixture - Автоматическое создание объектов
```csharp
// Создание с автоматическими зависимостями
var service = TK.Create<IModerationService>();
var handler = TK.Create<MessageHandler>();

// Создание коллекций
var users = TK.CreateMany<User>(5);

// Создание с фикстурой для кастомизации
var (sut, fixture) = TK.CreateWithFixture<MessageHandler>();
```

### Builders - Fluent API
```csharp
// Создание объектов через builder pattern
var message = TestKitBuilders.CreateMessage()
    .WithText("Hello, world!")
    .FromUser(12345)
    .InChat(67890)
    .Build();

var user = TestKitBuilders.CreateUser()
    .WithId(12345)
    .WithUsername("testuser")
    .IsBot(false)
    .Build();
```

### Bogus - Реалистичные данные
```csharp
// Создание реалистичных данных
var realisticUser = TK.CreateUser(userId: 12345);
var realisticMessage = TK.CreateMessage();
var realisticChannel = TK.CreateRealisticChannel();
```

### Telegram - Специализированные сценарии
```csharp
// Полные сценарии
var (fakeClient, envelope, message, update) = TK.CreateFullScenario();
var (fakeClient, envelope, message, update) = TK.CreateSpamScenario();
var (fakeClient, envelope, message, update) = TK.CreateNewUserScenario();

// Создание envelope
var envelope = TK.CreateEnvelope(message, update);
```

### Mocks - Централизованные моки
```csharp
// Базовые моки
var mockService = TK.CreateMock<IMyService>();
var loggerMock = TK.CreateLoggerMock<MyClass>();
var nullLogger = TK.CreateNullLogger<MyClass>();

// Telegram моки
var botClient = TK.CreateMockBotClient();
var botWrapper = TK.CreateMockBotClientWrapper();
var testMessage = TK.CreateTestMessage("Hello");
var testUser = TK.CreateTestUser("testuser");
var testChat = TK.CreateTestChat("Test Group");
var testUpdate = TK.CreateTestUpdate();

// Сервисные моки
var moderationService = TK.CreateMockModerationService();
var captchaService = TK.CreateMockCaptchaService();
var userManager = TK.CreateMockUserManager();
var userBanService = TK.CreateMockUserBanService();
var violationTracker = TK.CreateMockViolationTracker();
var messageService = TK.CreateMockMessageService();
var statisticsService = TK.CreateMockStatisticsService();
var botPermissionsService = TK.CreateMockBotPermissionsService();

// AI моки
var aiChecks = TK.CreateMockAiChecks();
var spamAiChecks = TK.CreateSpamAiChecks();
var normalAiChecks = TK.CreateNormalAiChecks();
var errorAiChecks = TK.CreateErrorAiChecks();

// Специализированные моки
var (moderation, captcha, userBan, message, aiChecks) = TK.CreateMessageHandlerMocks();
var (captcha, statistics, moderation, message) = TK.CreateCallbackQueryHandlerMocks();
var banModeration = TK.CreateBanModerationService("Spam detected");
var deleteModeration = TK.CreateDeleteModerationService("Inappropriate content");
var successfulCaptcha = TK.CreateSuccessfulCaptchaService();
var failedCaptcha = TK.CreateFailedCaptchaService();
var approvedUserManager = TK.CreateApprovedUserManager();
var unapprovedUserManager = TK.CreateUnapprovedUserManager();
var banTriggeringTracker = TK.CreateBanTriggeringViolationTracker();
```

## Интеграционные тесты

### MessageHandler
```csharp
var factory = TK.CreateMessageHandlerFactory();
var handler = factory.CreateWithDefaults();

// Настройка сценария
factory.SetupModerationBanScenario("Спам сообщение");
var result = await handler.HandleUserMessageAsync(message);
```

### ModerationService
```csharp
var factory = TK.CreateModerationServiceFactory();
var service = factory.CreateWithDefaults();

var result = await service.CheckMessageAsync(message, user, chat);
```

## Лучшие практики

1. **Используйте `TK.CreateX()`** для всех тестовых объектов
2. **Смотрите в `TK.Specialized.*`** для специализированных сценариев
3. **Пишите тесты парами**: happy path + fail path
4. **Используйте фабрики** для интеграционных тестов
5. **Если метода нет** - добавьте прокси в `TestKit.cs`
6. **Для сложных объектов** используйте Builders или AutoFixture
7. **Для реалистичных данных** используйте Bogus
8. **Для моков используйте `TK.CreateMock*()`** - единый интерфейс для всех моков
9. **Используйте специализированные моки** для типичных сценариев (баны, капча, etc.)
10. **Все моки возвращают `Mock<T>`** - используйте `.Object` для получения объекта

## Решение проблемы размера TestKit.cs

TestKit.cs уже содержит 674 строки. Для предотвращения "раздувания":

### Стратегия разделения:
1. **Основные методы** остаются в `TestKit.cs`
2. **Специализированные** группируются в `TestKit.Specialized.*`
3. **Сложная логика** выносится в отдельные файлы:
   - `TestKit.AutoFixture.cs` - автоматическое создание
   - `TestKit.Builders.cs` - fluent API
   - `TestKit.Bogus.cs` - реалистичные данные
   - `TestKit.Telegram.cs` - Telegram-специфика

### Принципы добавления новых методов:
1. **Простые прокси** → `TestKit.cs`
2. **Специализированные** → `TestKit.Specialized.*`
3. **Сложная логика** → отдельный файл
4. **Доменно-специфичные** → соответствующий файл

## Добавление новых методов

Если нужного метода нет в TestKit:

1. **Простые прокси** в `TestKit.cs`:
```csharp
public static NewType CreateNewObject() => TestDataFactory.CreateNewObject();
```

2. **Специализированные** в `TestKit.Specialized.*`:
```csharp
public static class Specialized
{
    public static class NewCategory
    {
        public static NewType NewMethod() => TestDataFactory.CreateNewObject();
    }
}
```

3. **Сложная логика** в отдельном файле:
```csharp
// TestKit.NewFeature.cs
public static class TestKitNewFeature
{
    public static NewType CreateComplexObject() => // сложная логика
}
``` 