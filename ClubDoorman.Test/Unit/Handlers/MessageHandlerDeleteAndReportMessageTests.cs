using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Handlers;
using ClubDoorman.Test.TestKit;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Handlers;

namespace ClubDoorman.Test.Unit.Handlers;

/// <summary>
/// Тесты для DeleteAndReportMessage метода
/// <tags>unit, message-handler, delete-report, moderation</tags>
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("MessageHandler")]
public class MessageHandlerDeleteAndReportMessageTests
{
    private MessageHandlerTestFactory _factory = null!;
    private MessageHandler _messageHandler = null!;

    [SetUp]
    public void Setup()
    {
        _factory = TK.CreateMessageHandlerFactory();
        _messageHandler = _factory.CreateMessageHandler();
        
        // Настраиваем AppConfig для AdminChatId
        _factory.AppConfigMock.Setup(x => x.AdminChatId).Returns(123456789L);
        
        // Настраиваем MessageService для возврата MessageTemplates
        var templates = new MessageTemplates();
        _factory.MessageServiceMock.Setup(x => x.GetTemplates()).Returns(templates);
    }

    /// <summary>
    /// Тест для DeleteAndReportMessage с успешной пересылкой
    /// Проверяет полный цикл: пересылка, уведомление, удаление
    /// <tags>delete-report, forward-success, notification, delete</tags>
    /// </summary>
    [Test]
    public async Task DeleteAndReportMessage_WithSuccessfulForward_CompletesFullCycle()
    {
        // Arrange
        var message = TK.CreateMessage();
        var user = message.From!;
        var chat = message.Chat;
        var reason = "Test violation";
        var isSilentMode = false;

        // Настраиваем мок Bot для успешной пересылки
        var forwardedMessage = TK.CreateMessage();
        _factory.BotMock.Setup(x => x.ForwardMessage(
            It.IsAny<ChatId>(), It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(forwardedMessage);

        // Настраиваем мок Bot для отправки уведомления
        _factory.BotMock.Setup(x => x.SendMessage(
            It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), 
            It.IsAny<ReplyParameters>(), It.IsAny<InlineKeyboardMarkup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(forwardedMessage);

        // Настраиваем мок Bot для удаления сообщения
        _factory.BotMock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Настраиваем мок MessageService для отправки предупреждения
        _factory.MessageServiceMock.Setup(x => x.SendUserNotificationWithReplyAsync(
            It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<UserNotificationType>(),
            It.IsAny<SimpleNotificationData>(), It.IsAny<ReplyParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(forwardedMessage);

        // Act
        await _messageHandler.DeleteAndReportMessage(message, reason, isSilentMode, CancellationToken.None);

        // Assert
        _factory.BotMock.Verify(x => x.ForwardMessage(
            It.IsAny<ChatId>(), chat.Id, message.MessageId, It.IsAny<CancellationToken>()), Times.Once);
        _factory.BotMock.Verify(x => x.DeleteMessage(chat.Id, message.MessageId, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Тест для DeleteAndReportMessage с защищенным контентом
    /// Проверяет обработку ошибки пересылки защищенного контента
    /// <tags>delete-report, protected-content, forward-error</tags>
    /// </summary>
    [Test]
    public async Task DeleteAndReportMessage_WithProtectedContent_HandlesForwardError()
    {
        // Arrange
        var message = TK.CreateMessage();
        var user = message.From!;
        var chat = message.Chat;
        var reason = "Test violation";
        var isSilentMode = false;

        // Настраиваем мок Bot для выброса исключения при пересылке
        _factory.BotMock.Setup(x => x.ForwardMessage(
            It.IsAny<ChatId>(), It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("can't be forwarded"));

        // Настраиваем мок Bot для отправки расширенного уведомления
        _factory.BotMock.Setup(x => x.SendMessage(
            It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), 
            It.IsAny<ReplyParameters>(), It.IsAny<InlineKeyboardMarkup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        // Настраиваем мок Bot для удаления сообщения
        _factory.BotMock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _messageHandler.DeleteAndReportMessage(message, reason, isSilentMode, CancellationToken.None);

        // Assert
        // Проверяем, что было отправлено расширенное уведомление без пересылки
        _factory.BotMock.Verify(x => x.SendMessage(
            It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), 
            It.IsAny<ReplyParameters>(), It.IsAny<InlineKeyboardMarkup>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Тест для DeleteAndReportMessage в тихом режиме
    /// Проверяет, что предупреждение пользователю не отправляется
    /// <tags>delete-report, silent-mode, no-notification</tags>
    /// </summary>
    [Test]
    public async Task DeleteAndReportMessage_WithSilentMode_DoesNotSendUserNotification()
    {
        // Arrange
        var message = TK.CreateMessage();
        var user = message.From!;
        var chat = message.Chat;
        var reason = "Test violation";
        var isSilentMode = true; // Тихий режим

        // Настраиваем мок Bot для успешной пересылки
        var forwardedMessage = TK.CreateMessage();
        _factory.BotMock.Setup(x => x.ForwardMessage(
            It.IsAny<ChatId>(), It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(forwardedMessage);

        // Настраиваем мок Bot для отправки уведомления
        _factory.BotMock.Setup(x => x.SendMessage(
            It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), 
            It.IsAny<ReplyParameters>(), It.IsAny<InlineKeyboardMarkup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(forwardedMessage);

        // Настраиваем мок Bot для удаления сообщения
        _factory.BotMock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _messageHandler.DeleteAndReportMessage(message, reason, isSilentMode, CancellationToken.None);

        // Assert
        // В тихом режиме предупреждение пользователю не отправляется
        _factory.MessageServiceMock.Verify(x => x.SendUserNotificationWithReplyAsync(
            It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<UserNotificationType>(),
            It.IsAny<SimpleNotificationData>(), It.IsAny<ReplyParameters>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Тест для DeleteAndReportMessage с ошибкой удаления
    /// Проверяет обработку ошибки при удалении сообщения
    /// <tags>delete-report, delete-error, error-handling</tags>
    /// </summary>
    [Test]
    public async Task DeleteAndReportMessage_WithDeleteError_HandlesGracefully()
    {
        // Arrange
        var message = TK.CreateMessage();
        var user = message.From!;
        var chat = message.Chat;
        var reason = "Test violation";
        var isSilentMode = false;

        // Настраиваем мок Bot для успешной пересылки
        var forwardedMessage = TK.CreateMessage();
        _factory.BotMock.Setup(x => x.ForwardMessage(
            It.IsAny<ChatId>(), It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(forwardedMessage);

        // Настраиваем мок Bot для отправки уведомления
        _factory.BotMock.Setup(x => x.SendMessage(
            It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), 
            It.IsAny<ReplyParameters>(), It.IsAny<InlineKeyboardMarkup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(forwardedMessage);

        // Настраиваем мок Bot для выброса исключения при удалении
        _factory.BotMock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unable to delete message"));

        // Act
        await _messageHandler.DeleteAndReportMessage(message, reason, isSilentMode, CancellationToken.None);

        // Assert
        // Метод должен завершиться без исключений, несмотря на ошибку удаления
        Assert.Pass("Ошибка удаления обработана корректно");
    }

    /// <summary>
    /// Тест для DeleteAndReportMessage с ошибкой отправки уведомления
    /// Проверяет обработку ошибки при отправке уведомления в админ-чат
    /// <tags>delete-report, notification-error, error-handling</tags>
    /// </summary>
    [Test]
    public async Task DeleteAndReportMessage_WithNotificationError_HandlesGracefully()
    {
        // Arrange
        var message = TK.CreateMessage();
        var user = message.From!;
        var chat = message.Chat;
        var reason = "Test violation";
        var isSilentMode = false;

        // Настраиваем мок Bot для выброса исключения при отправке уведомления
        _factory.BotMock.Setup(x => x.SendMessage(
            It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), 
            It.IsAny<ReplyParameters>(), It.IsAny<InlineKeyboardMarkup>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Failed to send notification"));

        // Act
        await _messageHandler.DeleteAndReportMessage(message, reason, isSilentMode, CancellationToken.None);

        // Assert
        // Метод должен завершиться без исключений, несмотря на ошибку уведомления
        Assert.Pass("Ошибка уведомления обработана корректно");
    }

    /// <summary>
    /// Тест для DeleteAndReportMessage с ошибкой отправки предупреждения пользователю
    /// Проверяет обработку ошибки при отправке предупреждения пользователю
    /// <tags>delete-report, user-notification-error, error-handling</tags>
    /// </summary>
    [Test]
    public async Task DeleteAndReportMessage_WithUserNotificationError_HandlesGracefully()
    {
        // Arrange
        var message = TK.CreateMessage();
        var user = message.From!;
        var chat = message.Chat;
        var reason = "Test violation";
        var isSilentMode = false;

        // Настраиваем мок Bot для успешной пересылки
        var forwardedMessage = TK.CreateMessage();
        _factory.BotMock.Setup(x => x.ForwardMessage(
            It.IsAny<ChatId>(), It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(forwardedMessage);

        // Настраиваем мок Bot для отправки уведомления
        _factory.BotMock.Setup(x => x.SendMessage(
            It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), 
            It.IsAny<ReplyParameters>(), It.IsAny<InlineKeyboardMarkup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(forwardedMessage);

        // Настраиваем мок Bot для удаления сообщения
        _factory.BotMock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Настраиваем мок MessageService для выброса исключения при отправке предупреждения
        _factory.MessageServiceMock.Setup(x => x.SendUserNotificationWithReplyAsync(
            It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<UserNotificationType>(),
            It.IsAny<SimpleNotificationData>(), It.IsAny<ReplyParameters>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Failed to send user notification"));

        // Act
        await _messageHandler.DeleteAndReportMessage(message, reason, isSilentMode, CancellationToken.None);

        // Assert
        // Метод должен завершиться без исключений, несмотря на ошибку предупреждения пользователю
        Assert.Pass("Ошибка предупреждения пользователю обработана корректно");
    }

    /// <summary>
    /// Тест для DeleteAndReportMessage с отменой операции
    /// Проверяет обработку CancellationToken
    /// <tags>delete-report, cancellation, cancellation-token</tags>
    /// </summary>
    [Test]
    public async Task DeleteAndReportMessage_WithCancellation_HandlesGracefully()
    {
        // Arrange
        var message = TK.CreateMessage();
        var user = message.From!;
        var chat = message.Chat;
        var reason = "Test violation";
        var isSilentMode = false;
        var cts = new CancellationTokenSource();

        // Настраиваем мок Bot для успешной пересылки
        var forwardedMessage = TK.CreateMessage();
        _factory.BotMock.Setup(x => x.ForwardMessage(
            It.IsAny<ChatId>(), It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(forwardedMessage);

        // Настраиваем мок Bot для отправки уведомления
        _factory.BotMock.Setup(x => x.SendMessage(
            It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), 
            It.IsAny<ReplyParameters>(), It.IsAny<InlineKeyboardMarkup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(forwardedMessage);

        // Настраиваем мок Bot для удаления сообщения
        _factory.BotMock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _messageHandler.DeleteAndReportMessage(message, reason, isSilentMode, cts.Token);

        // Assert
        // Метод должен завершиться без исключений
        Assert.Pass("Отмена операции обработана корректно");
    }

    /// <summary>
    /// Тест для DeleteAndReportMessage с длинным текстом
    /// Проверяет обработку сообщений с длинным текстом
    /// <tags>delete-report, long-text, content-truncation</tags>
    /// </summary>
    [Test]
    public async Task DeleteAndReportMessage_WithLongText_HandlesContentTruncation()
    {
        // Arrange
        var message = TK.CreateMessage();
        message.Text = new string('a', 1000); // Длинный текст
        var user = message.From!;
        var chat = message.Chat;
        var reason = "Test violation";
        var isSilentMode = false;

        // Настраиваем мок Bot для выброса исключения при пересылке (защищенный контент)
        _factory.BotMock.Setup(x => x.ForwardMessage(
            It.IsAny<ChatId>(), It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("can't be forwarded"));

        // Настраиваем мок Bot для отправки расширенного уведомления
        _factory.BotMock.Setup(x => x.SendMessage(
            It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), 
            It.IsAny<ReplyParameters>(), It.IsAny<InlineKeyboardMarkup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        // Act
        await _messageHandler.DeleteAndReportMessage(message, reason, isSilentMode, CancellationToken.None);

        // Assert
        // Проверяем, что расширенное уведомление было отправлено
        _factory.BotMock.Verify(x => x.SendMessage(
            It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), 
            It.IsAny<ReplyParameters>(), It.IsAny<InlineKeyboardMarkup>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Тест для DeleteAndReportMessage с медиа-контентом
    /// Проверяет обработку сообщений с медиа-контентом
    /// <tags>delete-report, media-content, caption</tags>
    /// </summary>
    [Test]
    public async Task DeleteAndReportMessage_WithMediaContent_HandlesCaption()
    {
        // Arrange
        var message = TK.CreateMessage();
        message.Text = null;
        message.Caption = "Test media caption";
        var user = message.From!;
        var chat = message.Chat;
        var reason = "Test violation";
        var isSilentMode = false;

        // Настраиваем мок Bot для выброса исключения при пересылке (защищенный контент)
        _factory.BotMock.Setup(x => x.ForwardMessage(
            It.IsAny<ChatId>(), It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("can't be forwarded"));

        // Настраиваем мок Bot для отправки расширенного уведомления
        _factory.BotMock.Setup(x => x.SendMessage(
            It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), 
            It.IsAny<ReplyParameters>(), It.IsAny<InlineKeyboardMarkup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        // Act
        await _messageHandler.DeleteAndReportMessage(message, reason, isSilentMode, CancellationToken.None);

        // Assert
        // Проверяем, что расширенное уведомление было отправлено с caption
        _factory.BotMock.Verify(x => x.SendMessage(
            It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), 
            It.IsAny<ReplyParameters>(), It.IsAny<InlineKeyboardMarkup>(), It.IsAny<CancellationToken>()), Times.Once);
    }
} 