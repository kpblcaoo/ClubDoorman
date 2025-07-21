using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
public class MessageTemplatesLocalizationTests : TestBase
{
    private MessageLocalizer _localizer = null!;
    private MessageTemplates _templates = null!;
    private Mock<ILogger<MessageLocalizer>> _mockLogger = null!;
    
    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<MessageLocalizer>>();
        var mockCultureProvider = new Mock<IChatCultureProvider>();
        mockCultureProvider.Setup(x => x.GetCultureForChat(It.IsAny<long>()))
            .Returns(new System.Globalization.CultureInfo("ru"));
        mockCultureProvider.Setup(x => x.GetDefaultCulture())
            .Returns(new System.Globalization.CultureInfo("ru"));
        
        _localizer = new MessageLocalizer(_mockLogger.Object, mockCultureProvider.Object);
        _templates = new MessageTemplates(_localizer);
    }
    
    [Test]
    public void GetLocalizedUserTemplate_WithLocalizer_ReturnsLocalizedMessage()
    {
        // Arrange
        var chatId = 123456789L;
        
        // Act
        var result = _templates.GetLocalizedUserTemplate(UserNotificationType.Welcome, chatId);
        
        // Assert
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Is.Not.EqualTo(_templates.GetUserTemplate(UserNotificationType.Welcome)));
    }
    
    [Test]
    public void GetLocalizedAdminTemplate_WithLocalizer_ReturnsLocalizedMessage()
    {
        // Arrange
        var chatId = 123456789L;
        
        // Act
        var result = _templates.GetLocalizedAdminTemplate(AdminNotificationType.AutoBan, chatId);
        
        // Assert
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Is.Not.EqualTo(_templates.GetAdminTemplate(AdminNotificationType.AutoBan)));
    }
    
    [Test]
    public void GetLocalizedLogTemplate_WithLocalizer_ReturnsLocalizedMessage()
    {
        // Arrange
        var chatId = 123456789L;
        
        // Act
        var result = _templates.GetLocalizedLogTemplate(LogNotificationType.AutoBanBlacklist, chatId);
        
        // Assert
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Is.Not.EqualTo(_templates.GetLogTemplate(LogNotificationType.AutoBanBlacklist)));
    }
    
    [Test]
    public void GetLocalizedTemplate_WithoutLocalizer_ReturnsOriginalTemplate()
    {
        // Arrange
        var templatesWithoutLocalizer = new MessageTemplates(null);
        var chatId = 123456789L;
        
        // Act
        var result = templatesWithoutLocalizer.GetLocalizedUserTemplate(UserNotificationType.Welcome, chatId);
        var original = templatesWithoutLocalizer.GetUserTemplate(UserNotificationType.Welcome);
        
        // Assert
        Assert.That(result, Is.EqualTo(original));
    }
    
    [Test]
    public void FormatNotificationTemplate_WithLocalizedTemplate_WorksCorrectly()
    {
        // Arrange
        var chatId = 123456789L;
        var user = new Telegram.Bot.Types.User
        {
            Id = 123,
            FirstName = "Test",
            LastName = "User",
            Username = "testuser"
        };
        var chat = new Telegram.Bot.Types.Chat
        {
            Id = chatId,
            Title = "Test Chat",
            Type = Telegram.Bot.Types.Enums.ChatType.Group
        };
        var data = new AutoBanNotificationData(user, chat, "Test ban type", "Test reason");
        
        // Act
        var template = _templates.GetLocalizedAdminTemplate(AdminNotificationType.AutoBan, chatId);
        var result = _templates.FormatNotificationTemplate(template, data);
        
        // Assert
        Assert.That(result, Is.Not.Empty);
        // Локализованный шаблон содержит индексированные плейсхолдеры, поэтому проверяем наличие шаблона
        // Проверяем русский шаблон, так как ChatCultureProvider настроен на русский
        Assert.That(result, Contains.Substring("Сообщение удалено:"));
        Assert.That(result, Contains.Substring("Юзер"));
        Assert.That(result, Contains.Substring("из чата"));
    }
} 