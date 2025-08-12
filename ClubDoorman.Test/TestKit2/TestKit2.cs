using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClubDoorman.Test.TestKit2;

/// <summary>
/// Новый TestKit2 для тестирования с новой архитектурой Features
/// Упрощенная версия без сложных зависимостей
/// 
/// РЕКОМЕНДАЦИИ ПО РАЗВИТИЮ:
/// 1. Per-test DI + IAsyncLifetime - уже реализовано в TestApp
/// 2. Data-driven тесты с [Theory] + InlineData/MemberData
/// 3. Параллельность по умолчанию (xUnit)
/// 4. FluentAssertions уже используется
/// 5. Меньше ритуалов - убрать TestFixture/SetUp/TearDown
/// 
/// ПРИНЦИПЫ:
/// - Каждый тест = свой TestApp
/// - await using var app = new TestApp();
/// - Scenario.With(app).GivenMessage().WhenHandled().ThenEffects()
/// - [Theory] для data-driven тестов
/// </summary>
public static class TestKit2
{
    // === БАЗОВЫЕ ФЕЙКИ ===
    
    public interface IClock
    {
        DateTime UtcNow { get; }
        Task Delay(TimeSpan delay, CancellationToken cancellationToken = default);
    }

    public sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; private set; } = DateTime.UtcNow;
        
        public void Advance(TimeSpan delta) => UtcNow += delta;
        public void SetTime(DateTime time) => UtcNow = time;
        
        public Task Delay(TimeSpan delay, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public interface IGuidProvider
    {
        Guid NewGuid();
    }

    public sealed class DeterministicGuidProvider : IGuidProvider
    {
        private int _counter = 0;
        public Guid NewGuid() => new Guid(_counter++, 0, 0, new byte[8]);
    }

    public interface IRandom
    {
        int Next(int minValue, int maxValue);
        double NextDouble();
    }

    public sealed class SeededRandom : IRandom
    {
        private readonly Random _random;
        
        public SeededRandom(int seed = 42) => _random = new Random(seed);
        
        public int Next(int minValue, int maxValue) => _random.Next(minValue, maxValue);
        public double NextDouble() => _random.NextDouble();
    }

    // === УТИЛИТЫ ДЛЯ СОЗДАНИЯ ТЕСТОВЫХ ОБЪЕКТОВ ===
    
    public static Message CreateMessage(long chatId, long userId, string text)
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = chatId, Type = ChatType.Supergroup },
            From = new User { Id = userId, Username = "testuser" },
            Text = text
        };
    }

    public static Update CreateUpdate(Message message)
    {
        return new Update { Message = message };
    }

    public static User CreateUser(long id, string? username = null)
    {
        return new User
        {
            Id = id,
            Username = username ?? $"user{id}",
            FirstName = "Test",
            IsBot = false
        };
    }

    public static Chat CreateChat(long id, string title = "Test Chat")
    {
        return new Chat
        {
            Id = id,
            Title = title,
            Type = ChatType.Supergroup
        };
    }

    // === DI УТИЛИТЫ ===
    
    public static IServiceCollection CreateTestServices()
    {
        var services = new ServiceCollection();
        
        // Базовые фейки
        services.AddSingleton<IClock, FakeClock>();
        services.AddSingleton<IGuidProvider, DeterministicGuidProvider>();
        services.AddSingleton<IRandom, SeededRandom>();
        
        return services;
    }
    
    /// <summary>
    /// Создает тестовый сервис-провайдер
    /// </summary>
    public static IServiceProvider CreateTestServiceProvider()
    {
        return CreateTestServices().BuildServiceProvider();
    }
    
    /// <summary>
    /// Очищает кэш между тестами
    /// </summary>
    public static void ClearMemoryCache()
    {
        // TODO: Implement cache clearing when needed
    }
    
    /// <summary>
    /// Создает тестовое сообщение с предустановленными параметрами
    /// </summary>
    public static Message CreateTestMessage(
        string text = "Test message",
        long chatId = 123456789,
        long userId = 987654321,
        string? username = "testuser")
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Text = text,
            Chat = new Chat { Id = chatId, Type = ChatType.Group, Title = "Test Chat" },
            From = new User { Id = userId, Username = username, FirstName = "Test", IsBot = false }
        };
    }
    
    /// <summary>
    /// Создает тестового пользователя
    /// </summary>
    public static User CreateTestUser(
        long userId = 987654321,
        string? username = "testuser",
        string firstName = "Test")
    {
        return new User
        {
            Id = userId,
            Username = username,
            FirstName = firstName,
            IsBot = false
        };
    }
    
    /// <summary>
    /// Создает тестовый чат
    /// </summary>
    public static Chat CreateTestChat(
        long chatId = 123456789,
        ChatType type = ChatType.Group,
        string title = "Test Chat")
    {
        return new Chat
        {
            Id = chatId,
            Type = type,
            Title = title
        };
    }
}
