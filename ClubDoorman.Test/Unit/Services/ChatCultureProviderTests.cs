using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
public class ChatCultureProviderTests : TestBase
{
    private ChatCultureProvider _provider = null!;
    private Mock<ILogger<ChatCultureProvider>> _mockLogger = null!;
    private IMemoryCache _cache = null!;
    
    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<ChatCultureProvider>>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _provider = new ChatCultureProvider(_mockLogger.Object, _cache);
    }
    
    [TearDown]
    public void TearDown()
    {
        _cache?.Dispose();
    }
    
    [Test]
    public void GetDefaultCulture_ReturnsRussianByDefault()
    {
        // Act
        var culture = _provider.GetDefaultCulture();
        
        // Assert
        Assert.That(culture.Name, Is.EqualTo("ru"));
    }
    
    [Test]
    public void GetCultureForChat_WithoutOverride_ReturnsDefaultCulture()
    {
        // Arrange
        var chatId = 123456789L;
        
        // Act
        var culture = _provider.GetCultureForChat(chatId);
        
        // Assert
        Assert.That(culture.Name, Is.EqualTo("ru"));
    }
    
    [Test]
    public void SetCultureForChat_WithValidCulture_SetsCultureForChat()
    {
        // Arrange
        var chatId = 123456789L;
        var culture = new System.Globalization.CultureInfo("en");
        
        // Act
        _provider.SetCultureForChat(chatId, culture);
        var result = _provider.GetCultureForChat(chatId);
        
        // Assert
        Assert.That(result.Name, Is.EqualTo("en"));
    }
    
    [Test]
    public void RemoveChatCulture_ExistingChat_RemovesCulture()
    {
        // Arrange
        var chatId = 123456789L;
        var culture = new System.Globalization.CultureInfo("en");
        _provider.SetCultureForChat(chatId, culture);
        
        // Act
        _provider.RemoveChatCulture(chatId);
        var result = _provider.GetCultureForChat(chatId);
        
        // Assert
        Assert.That(result.Name, Is.EqualTo("ru")); // Возвращается к культуре по умолчанию
    }
    
    [Test]
    public void GetCultureForChat_WithCache_ReturnsCachedCulture()
    {
        // Arrange
        var chatId = 123456789L;
        var culture = new System.Globalization.CultureInfo("en");
        _provider.SetCultureForChat(chatId, culture);
        
        // Act - вызываем дважды
        var result1 = _provider.GetCultureForChat(chatId);
        var result2 = _provider.GetCultureForChat(chatId);
        
        // Assert
        Assert.That(result1.Name, Is.EqualTo("en"));
        Assert.That(result2.Name, Is.EqualTo("en"));
        Assert.That(result1, Is.SameAs(result2)); // Должен быть тот же объект из кэша
    }
    
    [Test]
    public void SetCultureForChat_UpdatesCache()
    {
        // Arrange
        var chatId = 123456789L;
        var culture1 = new System.Globalization.CultureInfo("en");
        var culture2 = new System.Globalization.CultureInfo("ru");
        
        // Act
        _provider.SetCultureForChat(chatId, culture1);
        var result1 = _provider.GetCultureForChat(chatId);
        _provider.SetCultureForChat(chatId, culture2);
        var result2 = _provider.GetCultureForChat(chatId);
        
        // Assert
        Assert.That(result1.Name, Is.EqualTo("en"));
        Assert.That(result2.Name, Is.EqualTo("ru"));
    }
} 