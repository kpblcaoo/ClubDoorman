using System;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Handlers;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.Test.TestKit;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Times = Moq.Times;

namespace ClubDoorman.Test.Unit.Handlers;

/// <summary>
/// Тесты для метода SendSuspiciousMessageWithButtons в MessageHandler
/// <tags>unit, handlers, suspicious-message, admin-notifications, buttons</tags>
/// </summary>
[TestFixture]
[Category("unit")]
[Category("handlers")]
[Category("suspicious-message")]
public class MessageHandlerSendSuspiciousMessageTests
{
    private MessageHandlerTestFactory _factory = null!;
    private MessageHandler _messageHandler = null!;

    // Хелпер для создания ChatId
    private static ChatId Chat(long id) => new ChatId(id);

    [SetUp]
    public void Setup()
    {
        _factory = new MessageHandlerTestFactory();
        _messageHandler = _factory.CreateMessageHandler();
    }

    [Test]
    public async Task SendSuspiciousMessageWithButtons_WithValidData_SendsMessageWithButtons()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var data = new SuspiciousMessageNotificationData(user, chat, "Test message", message.MessageId);
        var isSilentMode = false;

        // Настраиваем моки для успешной отправки
        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.ForwardMessage(It.IsAny<ChatId>(), It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
        });

        _factory.WithAppConfigSetup(mock =>
        {
            mock.Setup(x => x.AdminChatId).Returns(12345L);
        });

        // Настраиваем моки для IMessageService
        _factory.WithMessageServiceSetup(mock =>
        {
            var templates = new MessageTemplates();
            mock.Setup(x => x.GetTemplates()).Returns(templates);
        });

        // Act
        await _messageHandler.SendSuspiciousMessageWithButtons(message, user, data, isSilentMode, CancellationToken.None);

        // Assert
        _factory.BotMock.Verify(
            x => x.ForwardMessage(
                Chat(12345L),
                chat.Id,
                message.MessageId,
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "Сообщение должно быть переслано в админ-чат"
        );

        _factory.BotMock.Verify(
            x => x.SendMessage(
                Chat(12345L),
                It.IsAny<string>(),
                ParseMode.Html,
                It.IsAny<ReplyParameters>(),
                It.IsAny<InlineKeyboardMarkup>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "Уведомление с кнопками должно быть отправлено"
        );
    }

    [Test]
    public async Task SendSuspiciousMessageWithButtons_WithSilentMode_AddsSilentModePrefix()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var data = new SuspiciousMessageNotificationData(user, chat, "Test message", message.MessageId);
        var isSilentMode = true;

        // Настраиваем моки для успешной отправки
        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.ForwardMessage(It.IsAny<ChatId>(), It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
        });

        _factory.WithAppConfigSetup(mock =>
        {
            mock.Setup(x => x.AdminChatId).Returns(12345L);
        });

        // Настраиваем моки для IMessageService
        _factory.WithMessageServiceSetup(mock =>
        {
            var templates = new MessageTemplates();
            mock.Setup(x => x.GetTemplates()).Returns(templates);
        });

        // Act
        await _messageHandler.SendSuspiciousMessageWithButtons(message, user, data, isSilentMode, CancellationToken.None);

        // Assert
        _factory.BotMock.Verify(
            x => x.SendMessage(
                Chat(12345L),
                It.Is<string>(text => text.Contains("🔇 **Тихий режим**")),
                ParseMode.Html,
                It.IsAny<ReplyParameters>(),
                It.IsAny<InlineKeyboardMarkup>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "Сообщение должно содержать префикс тихого режима"
        );
    }

    [Test]
    public async Task SendSuspiciousMessageWithButtons_WhenForwardFails_SendsMessageWithoutForward()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var data = new SuspiciousMessageNotificationData(user, chat, "Test message", message.MessageId);
        var isSilentMode = false;

        // Настраиваем мок для выброса исключения при пересылке
        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.ForwardMessage(It.IsAny<ChatId>(), It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("protected content"));
        });

        _factory.WithAppConfigSetup(mock =>
        {
            mock.Setup(x => x.AdminChatId).Returns(12345L);
        });

        // Настраиваем моки для IMessageService
        _factory.WithMessageServiceSetup(mock =>
        {
            var templates = new MessageTemplates();
            mock.Setup(x => x.GetTemplates()).Returns(templates);
        });

        // Act
        await _messageHandler.SendSuspiciousMessageWithButtons(message, user, data, isSilentMode, CancellationToken.None);

        // Assert
        _factory.BotMock.Verify(
            x => x.SendMessage(
                Chat(12345L),
                It.IsAny<string>(),
                ParseMode.Html,
                null,
                It.IsAny<InlineKeyboardMarkup>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "Сообщение должно быть отправлено без пересылки"
        );
    }

    [Test]
    public async Task SendSuspiciousMessageWithButtons_WhenBotThrowsException_SendsFallbackMessage()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var data = new SuspiciousMessageNotificationData(user, chat, "Test message", message.MessageId);
        var isSilentMode = false;

        // Настраиваем мок для выброса исключения
        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.ForwardMessage(It.IsAny<ChatId>(), It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));
        });

        _factory.WithAppConfigSetup(mock =>
        {
            mock.Setup(x => x.AdminChatId).Returns(12345L);
        });

        // Act
        await _messageHandler.SendSuspiciousMessageWithButtons(message, user, data, isSilentMode, CancellationToken.None);

        // Assert
        _factory.MessageServiceMock.Verify(
            x => x.SendAdminNotificationAsync(
                AdminNotificationType.SuspiciousMessage,
                data,
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "Должно быть отправлено fallback уведомление"
        );
    }

    [Test]
    public async Task SendSuspiciousMessageWithButtons_WithNullData_HandlesGracefully()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        SuspiciousMessageNotificationData? data = null;
        var isSilentMode = false;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => 
            await _messageHandler.SendSuspiciousMessageWithButtons(message, user, data!, isSilentMode, CancellationToken.None),
            "Метод должен обрабатывать null данные без исключений"
        );
    }

    [Test]
    public async Task SendSuspiciousMessageWithButtons_WithNullMessage_HandlesGracefully()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var data = new SuspiciousMessageNotificationData(user, chat, "Test message", message.MessageId);
        var isSilentMode = false;
        Message? nullMessage = null;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => 
            await _messageHandler.SendSuspiciousMessageWithButtons(nullMessage!, user, data, isSilentMode, CancellationToken.None),
            "Метод должен обрабатывать null сообщение без исключений"
        );
    }

    [Test]
    public async Task SendSuspiciousMessageWithButtons_WithNullUser_HandlesGracefully()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var data = new SuspiciousMessageNotificationData(user, chat, "Test message", message.MessageId);
        var isSilentMode = false;
        User? nullUser = null;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => 
            await _messageHandler.SendSuspiciousMessageWithButtons(message, nullUser!, data, isSilentMode, CancellationToken.None),
            "Метод должен обрабатывать null пользователя без исключений"
        );
    }

    [Test]
    public async Task SendSuspiciousMessageWithButtons_WithEmptyMessageText_HandlesCorrectly()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        message.Text = "";
        var data = new SuspiciousMessageNotificationData(user, chat, "", message.MessageId);
        var isSilentMode = false;

        // Настраиваем моки для успешной отправки
        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.ForwardMessage(It.IsAny<ChatId>(), It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
        });

        _factory.WithAppConfigSetup(mock =>
        {
            mock.Setup(x => x.AdminChatId).Returns(12345L);
        });

        // Настраиваем моки для IMessageService
        _factory.WithMessageServiceSetup(mock =>
        {
            var templates = new MessageTemplates();
            mock.Setup(x => x.GetTemplates()).Returns(templates);
        });

        // Act
        await _messageHandler.SendSuspiciousMessageWithButtons(message, user, data, isSilentMode, CancellationToken.None);

        // Assert
        _factory.BotMock.Verify(
            x => x.SendMessage(
                Chat(12345L),
                It.IsAny<string>(),
                ParseMode.Html,
                It.IsAny<ReplyParameters>(),
                It.IsAny<InlineKeyboardMarkup>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "Сообщение должно быть отправлено даже с пустым текстом"
        );
    }

    [Test]
    public async Task SendSuspiciousMessageWithButtons_WithLongMessageText_HandlesCorrectly()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var longText = new string('A', 1000);
        message.Text = longText;
        var data = new SuspiciousMessageNotificationData(user, chat, longText, message.MessageId);
        var isSilentMode = false;

        // Настраиваем моки для успешной отправки
        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.ForwardMessage(It.IsAny<ChatId>(), It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
        });

        _factory.WithAppConfigSetup(mock =>
        {
            mock.Setup(x => x.AdminChatId).Returns(12345L);
        });

        // Настраиваем моки для IMessageService
        _factory.WithMessageServiceSetup(mock =>
        {
            var templates = new MessageTemplates();
            mock.Setup(x => x.GetTemplates()).Returns(templates);
        });

        // Act
        await _messageHandler.SendSuspiciousMessageWithButtons(message, user, data, isSilentMode, CancellationToken.None);

        // Assert
        _factory.BotMock.Verify(
            x => x.SendMessage(
                Chat(12345L),
                It.IsAny<string>(),
                ParseMode.Html,
                It.IsAny<ReplyParameters>(),
                It.IsAny<InlineKeyboardMarkup>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "Сообщение должно быть отправлено даже с длинным текстом"
        );
    }

    [Test]
    public async Task SendSuspiciousMessageWithButtons_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var specialText = "Test message with *bold* _italic_ `code` and emoji 🚀";
        message.Text = specialText;
        var data = new SuspiciousMessageNotificationData(user, chat, specialText, message.MessageId);
        var isSilentMode = false;

        // Настраиваем моки для успешной отправки
        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.ForwardMessage(It.IsAny<ChatId>(), It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
        });

        _factory.WithAppConfigSetup(mock =>
        {
            mock.Setup(x => x.AdminChatId).Returns(12345L);
        });

        // Настраиваем моки для IMessageService
        _factory.WithMessageServiceSetup(mock =>
        {
            var templates = new MessageTemplates();
            mock.Setup(x => x.GetTemplates()).Returns(templates);
        });

        // Act
        await _messageHandler.SendSuspiciousMessageWithButtons(message, user, data, isSilentMode, CancellationToken.None);

        // Assert
        _factory.BotMock.Verify(
            x => x.SendMessage(
                Chat(12345L),
                It.IsAny<string>(),
                ParseMode.Html,
                It.IsAny<ReplyParameters>(),
                It.IsAny<InlineKeyboardMarkup>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "Сообщение должно быть отправлено с специальными символами"
        );
    }
} 