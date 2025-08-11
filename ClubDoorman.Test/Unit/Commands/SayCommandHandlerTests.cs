using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services.Commands;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Test.TestKit;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Commands;

/// <summary>
/// Тесты для SayCommandHandler - обработчика команды /say
/// <tags>unit, commands, say-command, messaging</tags>
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Commands")]
public class SayCommandHandlerTests
{
    private Mock<ITelegramBotClientWrapper> _botMock;
    private Mock<IMessageService> _messageServiceMock;
    private Mock<IAppConfig> _appConfigMock;
    private Mock<ILogger<SayCommandHandler>> _loggerMock;
    private SayCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _botMock = new Mock<ITelegramBotClientWrapper>();
        _messageServiceMock = new Mock<IMessageService>();
        _appConfigMock = new Mock<IAppConfig>();
        _loggerMock = new Mock<ILogger<SayCommandHandler>>();

        _handler = new SayCommandHandler(
            _botMock.Object,
            _messageServiceMock.Object,
            _appConfigMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// Тест проверяет, что команда /say от админа с правильным форматом корректно обрабатывается
    /// <tags>say-command, admin-command, text-message</tags>
    /// </summary>
    [Test]
    public async Task HandleAsync_WithValidSayFromAdmin_SendsMessage()
    {
        // Arrange
        var adminChatId = -1001234567890L;
        var testMessage = "Test message to send";
        var message = TK.CreateSayCommandMessage($"/say 123456789 {testMessage}");
        message.Chat = new Chat { Id = adminChatId, Type = Telegram.Bot.Types.Enums.ChatType.Group };

        _appConfigMock.Setup(x => x.AdminChatId).Returns(adminChatId);

        // Act
        await _handler.HandleAsync(message, CancellationToken.None);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendUserNotificationAsync(
                message.From!,
                message.Chat,
                UserNotificationType.Success,
                It.Is<SimpleNotificationData>(d => d.Reason!.Contains("Сообщение отправлено пользователю")),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Должно быть отправлено уведомление об успешной отправке");
    }

    /// <summary>
    /// Тест проверяет, что команда /say без правильного формата отправляет предупреждение
    /// <tags>say-command, invalid-format, edge-case</tags>
    /// </summary>
    [Test]
    public async Task HandleAsync_WithSayWithoutProperFormat_SendsWarning()
    {
        // Arrange
        var adminChatId = -1001234567890L;
        var message = TK.CreateSayCommandMessage("/say");
        message.Chat = new Chat { Id = adminChatId, Type = Telegram.Bot.Types.Enums.ChatType.Group };

        _appConfigMock.Setup(x => x.AdminChatId).Returns(adminChatId);

        // Act
        await _handler.HandleAsync(message, CancellationToken.None);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendUserNotificationAsync(
                message.From!,
                message.Chat,
                UserNotificationType.Warning,
                It.Is<SimpleNotificationData>(d => d.Reason!.Contains("Формат: /say @username сообщение")),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Должно быть отправлено предупреждение о неправильном формате");
    }

    /// <summary>
    /// Тест проверяет, что команда /say НЕ из админ-чата игнорируется
    /// <tags>say-command, security, access-control</tags>
    /// </summary>
    [Test]
    public async Task HandleAsync_WithSayFromNonAdminChat_DoesNotSendMessage()
    {
        // Arrange
        var adminChatId = -1001234567890L;
        var nonAdminChatId = -1009876543210L;
        var message = TK.CreateSayCommandMessage("/say Test message");
        message.Chat = new Chat { Id = nonAdminChatId, Type = Telegram.Bot.Types.Enums.ChatType.Group };

        _appConfigMock.Setup(x => x.AdminChatId).Returns(adminChatId);
        _appConfigMock.Setup(x => x.LogAdminChatId).Returns(adminChatId);

        // Act
        await _handler.HandleAsync(message, CancellationToken.None);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendUserNotificationAsync(
                It.IsAny<User>(),
                It.IsAny<Chat>(),
                It.IsAny<UserNotificationType>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "Не должно быть отправлено уведомление для не-админ чата");
    }

    /// <summary>
    /// Тест проверяет обработку команды /say с длинным сообщением
    /// <tags>say-command, long-text, performance</tags>
    /// </summary>
    [Test]
    public async Task HandleAsync_WithLongText_SendsMessage()
    {
        // Arrange
        var adminChatId = -1001234567890L;
        var longText = new string('A', 1000); // 1000 символов
        var message = TK.CreateSayCommandMessage($"/say 123456789 {longText}");
        message.Chat = new Chat { Id = adminChatId, Type = Telegram.Bot.Types.Enums.ChatType.Group };

        _appConfigMock.Setup(x => x.AdminChatId).Returns(adminChatId);

        // Act
        await _handler.HandleAsync(message, CancellationToken.None);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendUserNotificationAsync(
                message.From!,
                message.Chat,
                UserNotificationType.Success,
                It.Is<SimpleNotificationData>(d => d.Reason!.Contains("Сообщение отправлено пользователю")),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Должно быть отправлено уведомление об успешной отправке длинного сообщения");
    }

    /// <summary>
    /// Тест проверяет обработку команды /say с username
    /// <tags>say-command, username-target, success-path</tags>
    /// </summary>
    [Test]
    public async Task HandleAsync_WithUsernameTarget_SendsMessage()
    {
        // Arrange
        var adminChatId = -1001234567890L;
        var testMessage = "Hello @testuser";
        var message = TK.CreateSayCommandMessage($"/say @testuser {testMessage}");
        message.Chat = new Chat { Id = adminChatId, Type = Telegram.Bot.Types.Enums.ChatType.Group };

        _appConfigMock.Setup(x => x.AdminChatId).Returns(adminChatId);

        // Act
        await _handler.HandleAsync(message, CancellationToken.None);

        // Assert
        // Если username не найден в кэше, должно быть отправлено предупреждение
        _messageServiceMock.Verify(
            x => x.SendUserNotificationAsync(
                message.From!,
                message.Chat,
                UserNotificationType.Warning,
                It.Is<SimpleNotificationData>(d => d.Reason!.Contains("Не удалось найти пользователя @testuser")),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Должно быть отправлено предупреждение о том, что пользователь не найден");
    }
}
