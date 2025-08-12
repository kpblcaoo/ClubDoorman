# TestKit2 - Как использовать

## Быстрый старт

### Простой тест
```csharp
[Fact]
public async Task Basic_test()
{
    // Arrange
    await using var app = new TestApp();
    
    // Act
    var result = app.GetService<IMyService>().DoSomething();
    
    // Assert
    result.Should().Be("expected");
}
```

### Тест с эффектами
```csharp
[Fact]
public async Task Test_with_effects()
{
    // Arrange
    await using var app = new TestApp();
    var scenario = Scenario.With(app)
        .GivenMessage(123, 456, "spam message");
    
    // Act
    await scenario.WhenHandled();
    
    // Assert
    scenario.ThenEffects()
        .Should().Contain(e => e.Type == EffectType.Delete);
}
```

### Data-driven тест
```csharp
[Theory]
[InlineData("spam", true)]
[InlineData("hello", false)]
public async Task Spam_detection_test(string message, bool expectedSpam)
{
    await using var app = new TestApp();
    var scenario = Scenario.With(app)
        .GivenMessage(123, 456, message);
    
    await scenario.WhenHandled();
    
    var effects = scenario.ThenEffects();
    if (expectedSpam)
    {
        effects.Should().Contain(e => e.Type == EffectType.Delete);
    }
    else
    {
        effects.Should().Contain(e => e.Type == EffectType.IncrementGood);
    }
}
```

## Фейки

### Настройка времени
```csharp
[Fact]
public async Task Time_based_test()
{
    await using var app = new TestApp();
    var clock = app.GetService<IClock>() as FakeClock;
    
    clock!.SetTime(DateTime.UtcNow.AddHours(1));
    // или
    clock.Advance(TimeSpan.FromMinutes(30));
}
```

### Настройка AI/ML
```csharp
[Fact]
public async Task AI_analysis_test()
{
    await using var app = new TestApp();
    var aiService = app.GetService<IAiCascadeService>() as FakeAiCascadeService;
    var classifier = app.GetService<ISpamHamClassifier>() as FakeSpamHamClassifier;
    
    // Настройка AI
    aiService!.EnqueueProfileResult(true); // пользователь ограничен
    
    // Настройка ML
    classifier!.EnqueueResult(true, 0.9f); // спам с высокой уверенностью
}
```

### Настройка HTTP
```csharp
[Fact]
public async Task HTTP_test()
{
    await using var app = new TestApp();
    var httpHandler = app.GetService<FakeHttpMessageHandler>();
    
    httpHandler!.SetupResponse("https://api.example.com/data", 
        new HttpResponseMessage(HttpStatusCode.OK) 
        { 
            Content = new StringContent("{\"result\":\"ok\"}") 
        });
}
```

## Builders

### Создание сообщений
```csharp
var message = TestBuilders.Message()
    .WithChat(TestBuilders.Chat().WithId(123).WithTitle("Test Chat").Build())
    .WithFrom(TestBuilders.User().WithId(456).WithUsername("testuser").Build())
    .WithText("Hello world")
    .Build();
```

### Готовые пресеты
```csharp
var joinUpdate = TestBuilders.NewMemberJoin(123, 456, "newuser");
var forwardedUpdate = TestBuilders.ForwardedMessage(123, 456, 789, "forwarded text");
var commandUpdate = TestBuilders.PrivateChatCommand(456, "/start");
```

## Effects

### Типы эффектов
- `EffectType.Delete` - удаление сообщения
- `EffectType.Report` - жалоба на сообщение
- `EffectType.Ban` - бан пользователя
- `EffectType.Warn` - предупреждение
- `EffectType.IncrementGood` - инкремент хороших сообщений
- `EffectType.AiCascade` - AI анализ
- `EffectType.LogChat` - логирование в чат

### Проверка эффектов
```csharp
var effects = scenario.ThenEffects();

// Проверка конкретного эффекта
effects.Should().Contain(e => 
    e.Type == EffectType.Delete && 
    e.ChatId == 123 && 
    e.Reason.Contains("spam"));

// Проверка отсутствия эффекта
effects.Should().NotContain(e => e.Type == EffectType.Ban);

// Подсчет эффектов
effects.Count(e => e.Type == EffectType.Delete).Should().Be(1);
```

## Конфигурация

### Кастомная конфигурация
```csharp
await using var app = new TestApp(services =>
{
    // Добавить дополнительные сервисы
    services.AddSingleton<IMyCustomService, FakeMyCustomService>();
    
    // Заменить существующие сервисы
    services.Replace(ServiceDescriptor.Singleton<IMyService, FakeMyService>());
});
```

## Лучшие практики

### ✅ Правильно
```csharp
[Fact]
public async Task Good_test()
{
    await using var app = new TestApp();
    var scenario = Scenario.With(app)
        .GivenMessage(123, 456, "test");
    
    await scenario.WhenHandled();
    
    scenario.ThenEffects()
        .Should().Contain(e => e.Type == EffectType.Delete);
}
```

### ❌ Неправильно
```csharp
[Fact]
public async Task Bad_test()
{
    // НЕ создавайте объекты напрямую
    var message = new Message { Text = "test" }; // ❌
    
    // НЕ используйте моки напрямую
    var mock = new Mock<IMyService>(); // ❌
    
    // НЕ проверяйте внутренние вызовы
    mock.Verify(x => x.InternalMethod(), Times.Once); // ❌
}
```

## Отладка

### Логирование
```csharp
await using var app = new TestApp(services =>
{
    services.AddLogging(builder => 
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Debug);
    });
});
```

### Проверка состояния
```csharp
var effectsSink = app.GetService<IEffectsSink>();
var effects = effectsSink.Snapshot();
Console.WriteLine($"Effects: {string.Join(", ", effects)}");
```

## Миграция с LegacyTests

### Старый способ
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

### Новый способ
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
