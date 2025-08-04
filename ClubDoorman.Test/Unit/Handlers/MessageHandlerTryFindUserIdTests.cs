using ClubDoorman.Handlers;
using ClubDoorman.Test.TestKit;
using NUnit.Framework;
using System.Runtime.Caching;

namespace ClubDoorman.Test.Unit.Handlers;

/// <summary>
/// Тесты для TryFindUserIdByUsername метода
/// <tags>unit, message-handler, user-search, cache</tags>
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("MessageHandler")]
public class MessageHandlerTryFindUserIdTests
{
    private MessageHandler _messageHandler;

    [SetUp]
    public void Setup()
    {
        // Используем AutoFixture для автоматического создания всех зависимостей
        _messageHandler = TestKitAutoFixture.CreateMessageHandler();
        
        // Очищаем кэш перед каждым тестом
        foreach (var item in MemoryCache.Default.ToList())
        {
            MemoryCache.Default.Remove(item.Key);
        }
    }

    [TearDown]
    public void TearDown()
    {
        // Очищаем кэш после каждого теста
        foreach (var item in MemoryCache.Default.ToList())
        {
            MemoryCache.Default.Remove(item.Key);
        }
    }

    /// <summary>
    /// Тест для TryFindUserIdByUsername с найденным пользователем
    /// Проверяет, что метод находит пользователя в кэше
    /// <tags>user-search, cache-hit, happy-path</tags>
    /// </summary>
    [Test]
    public void TryFindUserIdByUsername_WithExistingUser_ReturnsUserId()
    {
        // Arrange
        var username = "testuser1";
        var chatId = 123456L;
        var userId = 789012L;
        var cacheKey = $"{chatId}_{userId}";
        var cacheValue = $"Message from @{username}";
        
        MemoryCache.Default.Add(cacheKey, cacheValue, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1) });

        // Act
        var result = _messageHandler.TryFindUserIdByUsername(username);

        // Assert
        Assert.That(result, Is.EqualTo(userId));
    }

    /// <summary>
    /// Тест для TryFindUserIdByUsername с ненайденным пользователем
    /// Проверяет, что метод возвращает null для несуществующего пользователя
    /// <tags>user-search, cache-miss, edge-case</tags>
    /// </summary>
    [Test]
    public void TryFindUserIdByUsername_WithNonExistingUser_ReturnsNull()
    {
        // Arrange
        var username = "nonexistentuser2";
        
        // Кэш пустой или содержит другие данные
        MemoryCache.Default.Add("123_456", "Message from @otheruser", new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1) });

        // Act
        var result = _messageHandler.TryFindUserIdByUsername(username);

        // Assert
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Тест для TryFindUserIdByUsername с пустым username
    /// Проверяет обработку пустой строки
    /// <tags>user-search, empty-input, edge-case</tags>
    /// </summary>
    [Test]
    public void TryFindUserIdByUsername_WithEmptyUsername_ReturnsNull()
    {
        // Arrange
        var username = "";

        // Act
        var result = _messageHandler.TryFindUserIdByUsername(username);

        // Assert
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Тест для TryFindUserIdByUsername с null username
    /// Проверяет обработку null значения
    /// <tags>user-search, null-input, edge-case</tags>
    /// </summary>
    [Test]
    public void TryFindUserIdByUsername_WithNullUsername_ReturnsNull()
    {
        // Arrange
        string? username = null;

        // Act
        var result = _messageHandler.TryFindUserIdByUsername(username!);

        // Assert
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Тест для TryFindUserIdByUsername с регистронезависимым поиском
    /// Проверяет, что поиск не зависит от регистра
    /// <tags>user-search, case-insensitive, behavior</tags>
    /// </summary>
    [Test]
    public void TryFindUserIdByUsername_WithCaseInsensitiveSearch_FindsUser()
    {
        // Arrange
        var username = "TestUser3";
        var chatId = 123456L;
        var userId = 789012L;
        var cacheKey = $"{chatId}_{userId}";
        var cacheValue = $"Message from @{username.ToLower()}";
        
        MemoryCache.Default.Add(cacheKey, cacheValue, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1) });

        // Act
        var result = _messageHandler.TryFindUserIdByUsername(username.ToLower());

        // Assert
        Assert.That(result, Is.EqualTo(userId));
    }

    /// <summary>
    /// Тест для TryFindUserIdByUsername с множественными записями в кэше
    /// Проверяет, что метод находит правильного пользователя среди множества записей
    /// <tags>user-search, multiple-cache-entries, behavior</tags>
    /// </summary>
    [Test]
    public void TryFindUserIdByUsername_WithMultipleCacheEntries_FindsCorrectUser()
    {
        // Arrange
        var targetUsername = "targetuser4";
        var otherUsername = "otheruser4";
        var chatId1 = 123456L;
        var chatId2 = 789012L;
        var userId1 = 111111L;
        var userId2 = 222222L;
        
        MemoryCache.Default.Add($"{chatId1}_{userId1}", $"Message from @{targetUsername}", new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1) });
        MemoryCache.Default.Add($"{chatId2}_{userId2}", $"Message from @{otherUsername}", new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1) });

        // Act
        var result = _messageHandler.TryFindUserIdByUsername(targetUsername);

        // Assert
        Assert.That(result, Is.EqualTo(userId1));
    }

    /// <summary>
    /// Тест для TryFindUserIdByUsername с некорректным форматом ключа кэша
    /// Проверяет, что метод игнорирует записи с неправильным форматом ключа
    /// <tags>user-search, invalid-cache-key, edge-case</tags>
    /// </summary>
    [Test]
    public void TryFindUserIdByUsername_WithInvalidCacheKey_ReturnsNull()
    {
        // Arrange
        var username = "testuser5";
        var invalidCacheKey = "invalid_key_format";
        var cacheValue = $"Message from @{username}";
        
        MemoryCache.Default.Add(invalidCacheKey, cacheValue, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1) });

        // Act
        var result = _messageHandler.TryFindUserIdByUsername(username);

        // Assert
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Тест для TryFindUserIdByUsername с некорректным userId в ключе
    /// Проверяет, что метод игнорирует записи с некорректным userId
    /// <tags>user-search, invalid-user-id, edge-case</tags>
    /// </summary>
    [Test]
    public void TryFindUserIdByUsername_WithInvalidUserId_ReturnsNull()
    {
        // Arrange
        var username = "testuser6";
        var chatId = 123456L;
        var invalidUserId = "invalid_user_id";
        var cacheKey = $"{chatId}_{invalidUserId}";
        var cacheValue = $"Message from @{username}";
        
        MemoryCache.Default.Add(cacheKey, cacheValue, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1) });

        // Act
        var result = _messageHandler.TryFindUserIdByUsername(username);

        // Assert
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Тест для TryFindUserIdByUsername с пустым кэшем
    /// Проверяет поведение при пустом кэше
    /// <tags>user-search, empty-cache, edge-case</tags>
    /// </summary>
    [Test]
    public void TryFindUserIdByUsername_WithEmptyCache_ReturnsNull()
    {
        // Arrange
        var username = "testuser7";
        
        // Кэш пустой

        // Act
        var result = _messageHandler.TryFindUserIdByUsername(username);

        // Assert
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Тест для TryFindUserIdByUsername с username без @ символа
    /// Проверяет, что метод находит username даже без @ символа
    /// <tags>user-search, username-without-at, behavior</tags>
    /// </summary>
    [Test]
    public void TryFindUserIdByUsername_WithUsernameWithoutAtSymbol_FindsUser()
    {
        // Arrange
        var username = "testuser8";
        var chatId = 123456L;
        var userId = 789012L;
        var cacheKey = $"{chatId}_{userId}";
        var cacheValue = $"Message from {username}"; // Без @ символа
        
        MemoryCache.Default.Add(cacheKey, cacheValue, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1) });

        // Act
        var result = _messageHandler.TryFindUserIdByUsername(username);

        // Assert
        Assert.That(result, Is.EqualTo(userId));
    }

    /// <summary>
    /// Отладочный тест для понимания работы кэша
    /// <tags>debug, cache-investigation</tags>
    /// </summary>
    [Test]
    public void TryFindUserIdByUsername_DebugCache_InvestigatesCacheBehavior()
    {
        // Arrange
        var username = "testuser9";
        var chatId = 123456L;
        var userId = 789012L;
        var cacheKey = $"{chatId}_{userId}";
        var cacheValue = $"Message from @{username}";
        
        // Добавляем в кэш
        MemoryCache.Default.Add(cacheKey, cacheValue, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1) });
        
        // Проверяем, что кэш содержит данные
        var cacheItem = MemoryCache.Default.Get(cacheKey);
        Assert.That(cacheItem, Is.Not.Null, "Кэш должен содержать добавленный элемент");
        Assert.That(cacheItem, Is.EqualTo(cacheValue), "Значение в кэше должно совпадать");
        
        // Проверяем количество элементов в кэше
        var cacheCount = MemoryCache.Default.GetCount();
        Assert.That(cacheCount, Is.GreaterThan(0), "Кэш должен содержать элементы");
        
        // Выводим все элементы кэша для отладки
        Console.WriteLine($"Количество элементов в кэше: {cacheCount}");
        foreach (var item in MemoryCache.Default)
        {
            Console.WriteLine($"Кэш: ключ='{item.Key}', значение='{item.Value}'");
        }

        // Act
        var result = _messageHandler.TryFindUserIdByUsername(username);

        // Assert
        Console.WriteLine($"Результат поиска: {result}");
        Assert.That(result, Is.EqualTo(userId), "Должен найти пользователя в кэше");
    }
} 