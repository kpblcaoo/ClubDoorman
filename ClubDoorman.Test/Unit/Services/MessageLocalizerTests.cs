using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.Test.Unit.Services;

/// <summary>
/// Тесты для локализатора сообщений
/// </summary>
public class MessageLocalizerTests : TestBase
{
    private IMessageLocalizer _localizer;
    private Mock<ILogger<MessageLocalizer>> _mockLogger;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<MessageLocalizer>>();
        var mockCultureProvider = new Mock<IChatCultureProvider>();
        mockCultureProvider.Setup(x => x.GetCultureForChat(It.IsAny<long>()))
            .Returns(new System.Globalization.CultureInfo("ru"));
        mockCultureProvider.Setup(x => x.GetDefaultCulture())
            .Returns(new System.Globalization.CultureInfo("ru"));
        
        _localizer = new MessageLocalizer(_mockLogger.Object, mockCultureProvider.Object);
    }

    [Test]
    public void User_ValidKey_ReturnsLocalizedMessage()
    {
        // Arrange
        var chatId = 123456789L;
        var key = "CaptchaPrompt";
        var args = new object[] { "ABC123" };

        // Act
        var result = _localizer.User(key, chatId, args);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Does.Contain("ABC123"));
    }

    [Test]
    public void User_InvalidKey_ReturnsFallbackMessage()
    {
        // Arrange
        var chatId = 123456789L;
        var key = "NonExistentKey";

        // Act
        var result = _localizer.User(key, chatId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Does.Contain("Something went wrong"));
    }

    [Test]
    public void Admin_ValidKey_ReturnsLocalizedMessage()
    {
        // Arrange
        var chatId = 123456789L;
        var key = "AutoBanBlacklist";
        var args = new object[] { "TestUser", "TestChat", "TestLink" };

        // Act
        var result = _localizer.Admin(key, chatId, args);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Does.Contain("TestUser"));
        Assert.That(result, Does.Contain("TestChat"));
    }

    [Test]
    public void System_ValidKey_ReturnsLocalizedMessage()
    {
        // Arrange
        var key = "BotStarted";

        // Act
        var result = _localizer.System(key);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Does.Contain("Бот"));
    }

    [Test]
    public void User_WithFormatting_FormatsCorrectly()
    {
        // Arrange
        var chatId = 123456789L;
        var key = "Success";
        var args = new object[] { "Operation completed" };

        // Act
        var result = _localizer.User(key, chatId, args);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Does.Contain("Operation completed"));
    }

    [Test]
    public void Admin_WithFormatting_FormatsCorrectly()
    {
        // Arrange
        var chatId = 123456789L;
        var key = "BanForLongName";
        var args = new object[] { "Auto-ban", "TestChat", "Name too long" };

        // Act
        var result = _localizer.Admin(key, chatId, args);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Does.Contain("Auto-ban"));
        Assert.That(result, Does.Contain("TestChat"));
        Assert.That(result, Does.Contain("Name too long"));
    }

    [Test]
    public void System_WithFormatting_FormatsCorrectly()
    {
        // Arrange
        var key = "NewUserJoined";
        var args = new object[] { "TestUser", 123L, "testuser", "TestGroup", 456L };

        // Act
        var result = _localizer.System(key, args);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Does.Contain("TestUser"));
        Assert.That(result, Does.Contain("TestGroup"));
    }
    
    [Test]
    public void User_WithCulture_ReturnsLocalizedMessage()
    {
        // Arrange
        var key = "CaptchaPrompt";
        var culture = new System.Globalization.CultureInfo("ru");
        var args = new object[] { "ABC123" };

        // Act
        var result = _localizer.User(key, culture, args);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Does.Contain("ABC123"));
    }
    
    [Test]
    public void Admin_WithCulture_ReturnsLocalizedMessage()
    {
        // Arrange
        var key = "AutoBanBlacklist";
        var culture = new System.Globalization.CultureInfo("en");
        var args = new object[] { "TestUser", "TestChat", "TestLink" };

        // Act
        var result = _localizer.Admin(key, culture, args);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Does.Contain("TestUser"));
        Assert.That(result, Does.Contain("TestChat"));
    }
    
    [Test]
    public void System_WithCulture_ReturnsLocalizedMessage()
    {
        // Arrange
        var key = "BotStarted";
        var culture = new System.Globalization.CultureInfo("en");

        // Act
        var result = _localizer.System(key, culture);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Does.Contain("started"));
    }
    
    [Test]
    public void User_MissingKey_ReturnsGenericError()
    {
        // Arrange
        var key = "NonExistentKey";
        var culture = new System.Globalization.CultureInfo("en");

        // Act
        var result = _localizer.User(key, culture);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Does.Contain("Something went wrong"));
    }
} 