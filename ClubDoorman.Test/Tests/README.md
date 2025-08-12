# Тесты ClubDoorman

## Структура тестов

Тесты организованы по принципу **дублирования структуры основного проекта** с фокусом на типы тестов:

```
Tests/
├── Services/                    # Тесты сервисов (дублирует ClubDoorman/Services/)
│   ├── AI/                     # AI сервисы
│   │   └── AiChecksTests.cs
│   ├── Moderation/             # Модерация
│   │   └── ModerationServiceTests.cs
│   ├── UserManagement/         # Управление пользователями
│   ├── Captcha/                # Капча
│   ├── Telegram/               # Telegram API
│   └── ...
├── Handlers/                   # Тесты обработчиков (дублирует ClubDoorman/Handlers/)
│   └── MessageHandlerTests.cs
├── Features/                   # Тесты фич (дублирует ClubDoorman/Features/)
├── Infrastructure/             # Тесты инфраструктуры
├── Integration/                # Интеграционные тесты
│   └── ModerationFlowTests.cs
└── Unit/                       # Модульные тесты
```

## Принципы организации

### 1. **Дублирование структуры проекта**
- Тесты находятся в папках, соответствующих структуре основного проекта
- Легко найти тесты для конкретного компонента
- Новая функциональность → новые тесты в соответствующей папке

### 2. **Разделение по типам тестов**
- **Unit** - тестирование отдельных компонентов
- **Integration** - тестирование взаимодействия компонентов
- **Services** - тестирование бизнес-логики сервисов
- **Handlers** - тестирование обработчиков событий

### 3. **Использование TestKit2**
- Все тесты используют современную инфраструктуру TestKit2
- Per-test DI с `IAsyncDisposable`
- Data-driven тесты с `[Theory]`
- FluentAssertions для читаемых проверок

## Примеры тестов

### Unit тест сервиса
```csharp
[Fact]
public async Task CheckMessageAsync_UserInBanlist_ReturnsBanAction()
{
    await using var app = new TestApp();
    var service = app.GetService<IModerationService>();
    var message = TestBuilders.Message()
        .WithChat(TestBuilders.Chat().WithId(123).Build())
        .WithFrom(TestBuilders.User().WithId(456).Build())
        .WithText("Hello world")
        .Build();

    var result = await service.CheckMessageAsync(message);

    result.Should().NotBeNull();
    result.Action.Should().Be(ModerationAction.Ban);
}
```

### Data-driven тест
```csharp
[Theory]
[InlineData("spam", ModerationAction.Ban)]
[InlineData("normal message", ModerationAction.Allow)]
public async Task CheckMessageAsync_VariousMessages_ReturnsExpectedAction(
    string messageText, ModerationAction expectedAction)
{
    await using var app = new TestApp();
    var service = app.GetService<IModerationService>();
    var message = TestBuilders.Message()
        .WithText(messageText)
        .Build();

    var result = await service.CheckMessageAsync(message);

    result.Action.Should().Be(expectedAction);
}
```

### Интеграционный тест
```csharp
[Fact]
public async Task SpamMessage_ShouldBeDeleted()
{
    await using var app = new TestApp();
    var scenario = Scenario.With(app)
        .GivenMessage(123, 456, "spam message");

    await scenario.WhenHandled();

    scenario.ThenEffects()
        .Should().Contain(e => e.Type == EffectType.Delete);
}
```

## Миграция с LegacyTests

### Старый подход (LegacyTests)
```csharp
[Test]
public void Old_way()
{
    var mock = new Mock<IMyService>();
    mock.Setup(x => x.DoSomething()).Returns("result");
    
    var service = new MyService(mock.Object);
    var result = service.Process();
    
    Assert.AreEqual("expected", result);
    mock.Verify(x => x.DoSomething(), Times.Once);
}
```

### Новый подход (TestKit2)
```csharp
[Fact]
public async Task New_way()
{
    await using var app = new TestApp();
    var scenario = Scenario.With(app)
        .GivenMessage(123, 456, "test");
    
    await scenario.WhenHandled();
    
    scenario.ThenEffects()
        .Should().Contain(e => e.Type == EffectType.Delete);
}
```

## Лучшие практики

### ✅ Правильно
- Используйте `TestApp` для DI
- Используйте `TestBuilders` для создания данных
- Используйте `Scenario` для интеграционных тестов
- Используйте `[Theory]` для data-driven тестов
- Используйте FluentAssertions для проверок

### ❌ Неправильно
- Не создавайте объекты напрямую (`new Message()`)
- Не используйте моки напрямую в тестах
- Не проверяйте внутренние вызовы методов
- Не создавайте сложные setup'ы в каждом тесте

## Запуск тестов

### Все тесты
```bash
dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj
```

### Конкретная категория
```bash
dotnet test --filter "Category=Integration"
```

### Конкретный тест
```bash
dotnet test --filter "FullyQualifiedName~ModerationServiceTests"
```

## Добавление новых тестов

1. **Определите тип теста** (Unit/Integration/Services/Handlers)
2. **Выберите папку** в соответствии со структурой проекта
3. **Создайте файл** с именем `{ComponentName}Tests.cs`
4. **Используйте TestKit2** для инфраструктуры
5. **Следуйте принципам** из этого README

## Связь с TestKit2

Все тесты используют инфраструктуру из `TestKit2/`:
- `TestApp` - DI контейнер
- `TestBuilders` - создание тестовых данных
- `Scenario` - DSL для сценариев
- `Fakes/` - фейки для внешних зависимостей

Подробнее см. `TestKit2/HOWTO.md`
