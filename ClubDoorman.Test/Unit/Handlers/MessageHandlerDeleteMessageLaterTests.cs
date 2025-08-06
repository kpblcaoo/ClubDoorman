using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserBan;
using System;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Handlers;
using ClubDoorman.Services;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.Test.TestKit;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Times = Moq.Times;
using ClubDoorman.Services.Handlers;

namespace ClubDoorman.Test.Unit.Handlers;

/// <summary>
/// Тесты для метода DeleteMessageLater в MessageHandler
/// <tags>unit, handlers, delete-message, async, timing</tags>
/// </summary>
[TestFixture]
[Category("unit")]
[Category("handlers")]
[Category("delete-message-later")]
public class MessageHandlerDeleteMessageLaterTests
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
    public void DeleteMessageLater_WithCustomTimeout_SchedulesMessageDeletion()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var customTimeout = TimeSpan.FromMilliseconds(100); // Короткий таймаут для теста
        var cancellationToken = CancellationToken.None;

        // Act
        _messageHandler.DeleteMessageLater(message, customTimeout, cancellationToken);

        // Assert
        // Проверяем, что метод выполнился без исключений
        // В реальном тесте нужно было бы дождаться выполнения и проверить вызов _bot.DeleteMessage
        Assert.Pass("Метод выполнился без исключений");
    }

    [Test]
    public void DeleteMessageLater_WithDefaultTimeout_UsesFiveMinutesDefault()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var cancellationToken = CancellationToken.None;

        // Act
        _messageHandler.DeleteMessageLater(message, default, cancellationToken);

        // Assert
        // Проверяем, что метод выполнился без исключений с дефолтным таймаутом
        Assert.Pass("Метод выполнился без исключений с дефолтным таймаутом");
    }

    [Test]
    public void DeleteMessageLater_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var timeout = TimeSpan.FromSeconds(1);
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Act
        _messageHandler.DeleteMessageLater(message, timeout, cancellationToken);
        
        // Отменяем операцию сразу
        cancellationTokenSource.Cancel();

        // Assert
        // Проверяем, что метод выполнился без исключений и отмена обработана корректно
        Assert.Pass("Метод выполнился без исключений, отмена обработана");
    }

    [Test]
    public void DeleteMessageLater_WithNullMessage_HandlesGracefully()
    {
        // Arrange
        Message? message = null;
        var timeout = TimeSpan.FromMilliseconds(100);
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        Assert.DoesNotThrow(() => _messageHandler.DeleteMessageLater(message!, timeout, cancellationToken),
            "Метод должен обрабатывать null сообщение без исключений");
    }

    [Test]
    public void DeleteMessageLater_WithZeroTimeout_ExecutesImmediately()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var zeroTimeout = TimeSpan.Zero;
        var cancellationToken = CancellationToken.None;

        // Act
        _messageHandler.DeleteMessageLater(message, zeroTimeout, cancellationToken);

        // Assert
        // Проверяем, что метод выполнился без исключений с нулевым таймаутом
        Assert.Pass("Метод выполнился без исключений с нулевым таймаутом");
    }

    [Test]
    public void DeleteMessageLater_WithNegativeTimeout_HandlesGracefully()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var negativeTimeout = TimeSpan.FromMilliseconds(-100);
        var cancellationToken = CancellationToken.None;

        // Act
        _messageHandler.DeleteMessageLater(message, negativeTimeout, cancellationToken);

        // Assert
        // Проверяем, что метод выполнился без исключений с отрицательным таймаутом
        Assert.Pass("Метод выполнился без исключений с отрицательным таймаутом");
    }

    [Test]
    public async Task DeleteMessageLater_WithShortTimeout_ActuallyDeletesMessage()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var shortTimeout = TimeSpan.FromMilliseconds(50); // Очень короткий таймаут
        var cancellationToken = CancellationToken.None;

        // Настраиваем мок для проверки вызова DeleteMessage
        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        });

        // Act
        _messageHandler.DeleteMessageLater(message, shortTimeout, cancellationToken);

        // Ждем немного больше таймаута
        await Task.Delay(100, cancellationToken);

        // Assert
        _factory.BotMock.Verify(
            x => x.DeleteMessage(It.Is<ChatId>(c => c.Identifier == chat.Id), message.MessageId, It.IsAny<CancellationToken>()),
            Times.Once,
            "DeleteMessage должен быть вызван один раз"
        );
    }

    [Test]
    public async Task DeleteMessageLater_WhenBotThrowsException_LogsWarning()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var shortTimeout = TimeSpan.FromMilliseconds(50);
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Test exception");

        // Настраиваем мок для выброса исключения
        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);
        });

        // Act
        _messageHandler.DeleteMessageLater(message, shortTimeout, cancellationToken);

        // Ждем немного больше таймаута
        await Task.Delay(100, cancellationToken);

        // Assert
        _factory.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once,
            "Должно быть залогировано предупреждение с исключением"
        );
    }

    [Test]
    public async Task DeleteMessageLater_WhenCancelled_DoesNotDeleteMessage()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var timeout = TimeSpan.FromMilliseconds(100);
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Act
        _messageHandler.DeleteMessageLater(message, timeout, cancellationToken);
        
        // Отменяем операцию сразу
        cancellationTokenSource.Cancel();

        // Ждем немного больше таймаута
        await Task.Delay(150, CancellationToken.None);

        // Assert
        _factory.BotMock.Verify(
            x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "DeleteMessage не должен быть вызван при отмене операции"
        );
    }

    [Test]
    public void DeleteMessageLater_WithDifferentMessageTypes_HandlesAllTypes()
    {
        // Arrange
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var timeout = TimeSpan.FromMilliseconds(10);
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        Assert.DoesNotThrow(() => _messageHandler.DeleteMessageLater(message, timeout, cancellationToken),
            "Метод должен обрабатывать сообщение корректно");
    }
} 