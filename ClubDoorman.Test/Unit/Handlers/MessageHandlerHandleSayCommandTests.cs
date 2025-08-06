using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserBan;
using System;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Handlers;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.Test.TestKit;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Times = Moq.Times;
using ClubDoorman.Services.Handlers;

namespace ClubDoorman.Test.Unit.Handlers;

/// <summary>
/// Тесты для метода HandleSayCommandAsync в MessageHandler
/// <tags>unit, handlers, say-command, admin-commands, messaging</tags>
/// </summary>
[TestFixture]
[Category("unit")]
[Category("handlers")]
[Category("say-command")]
public class MessageHandlerHandleSayCommandTests
{
    private MessageHandlerTestFactory _factory = null!;
    private MessageHandler _messageHandler = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new MessageHandlerTestFactory();
        _messageHandler = _factory.CreateMessageHandler();
    }

    [Test]
    public async Task HandleSayCommandAsync_WithInsufficientParts_SendsWarningMessage()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        message.Text = "/say @username"; // Недостаточно частей

        // Act
        await _messageHandler.HandleSayCommandAsync(message, CancellationToken.None);

        // Assert
        _factory.MessageServiceMock.Verify(
            x => x.SendUserNotificationAsync(
                user,
                chat,
                UserNotificationType.Warning,
                It.Is<SimpleNotificationData>(d => d.Reason!.Contains("Формат: /say")),
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "Должно быть отправлено предупреждение о неправильном формате"
        );
    }

    [Test]
    public async Task HandleSayCommandAsync_WithUsername_AttemptsToFindUserAndSendMessage()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        message.Text = "/say 12345 Привет!"; // Используем userId вместо username

        // Настраиваем мок для успешной отправки
        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.SendMessage(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
        });

        // Act
        await _messageHandler.HandleSayCommandAsync(message, CancellationToken.None);

        // Assert
        _factory.MessageServiceMock.Verify(
            x => x.SendUserNotificationAsync(
                user,
                chat,
                UserNotificationType.Success,
                It.Is<SimpleNotificationData>(d => d.Reason!.Contains("Сообщение отправлено")),
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "Должно быть отправлено уведомление об успешной отправке"
        );
    }

    [Test]
    public async Task HandleSayCommandAsync_WithUserId_AttemptsToSendMessage()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        message.Text = "/say 12345 Привет!";

        // Настраиваем мок для успешной отправки
        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.SendMessage(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
        });

        // Act
        await _messageHandler.HandleSayCommandAsync(message, CancellationToken.None);

        // Assert
        _factory.BotMock.Verify(
            x => x.SendMessage(It.Is<ChatId>(c => c.Identifier == 12345L), "Привет!", ParseMode.Markdown, null, null, It.IsAny<CancellationToken>()),
            Times.Once,
            "Должно быть отправлено сообщение пользователю"
        );
    }

    [Test]
    public async Task HandleSayCommandAsync_WithInvalidUsername_SendsWarningMessage()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        message.Text = "/say @nonexistentuser Привет!";

        // Act
        await _messageHandler.HandleSayCommandAsync(message, CancellationToken.None);

        // Assert
        _factory.MessageServiceMock.Verify(
            x => x.SendUserNotificationAsync(
                user,
                chat,
                UserNotificationType.Warning,
                It.Is<SimpleNotificationData>(d => d.Reason!.Contains("Не удалось найти пользователя")),
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "Должно быть отправлено предупреждение о ненайденном пользователе"
        );
    }

    [Test]
    public async Task HandleSayCommandAsync_WithInvalidUserId_SendsWarningMessage()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        message.Text = "/say invalid_id Привет!";

        // Act
        await _messageHandler.HandleSayCommandAsync(message, CancellationToken.None);

        // Assert
        _factory.MessageServiceMock.Verify(
            x => x.SendUserNotificationAsync(
                user,
                chat,
                UserNotificationType.Warning,
                It.Is<SimpleNotificationData>(d => d.Reason!.Contains("Не удалось найти пользователя")),
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "Должно быть отправлено предупреждение о ненайденном пользователе"
        );
    }

    [Test]
    public async Task HandleSayCommandAsync_WhenBotThrowsException_SendsErrorMessage()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        message.Text = "/say 12345 Привет!";
        var expectedException = new InvalidOperationException("Test exception");

        // Настраиваем мок для выброса исключения
        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.SendMessage(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), null, null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);
        });

        // Act
        await _messageHandler.HandleSayCommandAsync(message, CancellationToken.None);

        // Assert
        _factory.MessageServiceMock.Verify(
            x => x.SendUserNotificationAsync(
                user,
                chat,
                UserNotificationType.Warning,
                It.Is<SimpleNotificationData>(d => d.Reason!.Contains("Не удалось доставить сообщение")),
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "Должно быть отправлено предупреждение об ошибке доставки"
        );
    }

    [Test]
    public async Task HandleSayCommandAsync_WithEmptyMessage_HandlesGracefully()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        message.Text = "/say 12345 "; // Пустое сообщение

        // Act
        await _messageHandler.HandleSayCommandAsync(message, CancellationToken.None);

        // Assert
        _factory.BotMock.Verify(
            x => x.SendMessage(It.Is<ChatId>(c => c.Identifier == 12345L), "", ParseMode.Markdown, null, null, It.IsAny<CancellationToken>()),
            Times.Once,
            "Должно быть отправлено пустое сообщение"
        );
    }

    [Test]
    public async Task HandleSayCommandAsync_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        message.Text = "/say 12345 Привет! *bold* _italic_ `code`";

        // Настраиваем мок для успешной отправки
        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.SendMessage(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
        });

        // Act
        await _messageHandler.HandleSayCommandAsync(message, CancellationToken.None);

        // Assert
        _factory.BotMock.Verify(
            x => x.SendMessage(It.Is<ChatId>(c => c.Identifier == 12345L), "Привет! *bold* _italic_ `code`", ParseMode.Markdown, null, null, It.IsAny<CancellationToken>()),
            Times.Once,
            "Должно быть отправлено сообщение с Markdown разметкой"
        );
    }

    [Test]
    public async Task HandleSayCommandAsync_WithLongMessage_HandlesCorrectly()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var longText = new string('A', 1000); // Длинное сообщение
        message.Text = $"/say 12345 {longText}";

        // Настраиваем мок для успешной отправки
        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.SendMessage(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
        });

        // Act
        await _messageHandler.HandleSayCommandAsync(message, CancellationToken.None);

        // Assert
        _factory.BotMock.Verify(
            x => x.SendMessage(It.Is<ChatId>(c => c.Identifier == 12345L), longText, ParseMode.Markdown, null, null, It.IsAny<CancellationToken>()),
            Times.Once,
            "Должно быть отправлено длинное сообщение"
        );
    }

    [Test]
    public async Task HandleSayCommandAsync_WithNullMessage_HandlesGracefully()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        message.Text = null;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => 
            await _messageHandler.HandleSayCommandAsync(message, CancellationToken.None),
            "Метод должен обрабатывать null сообщение без исключений"
        );
    }

    [Test]
    public async Task HandleSayCommandAsync_WithNullUser_HandlesGracefully()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        message.From = null;
        message.Text = "/say 12345 Привет!";

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => 
            await _messageHandler.HandleSayCommandAsync(message, CancellationToken.None),
            "Метод должен обрабатывать null пользователя без исключений"
        );
    }
} 