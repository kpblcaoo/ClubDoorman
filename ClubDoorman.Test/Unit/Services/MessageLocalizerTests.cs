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
        _localizer = new MessageLocalizer(_mockLogger.Object);
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
        Assert.That(result, Does.Contain("Missing key"));
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
        Assert.That(result, Does.Contain("started"));
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
} 