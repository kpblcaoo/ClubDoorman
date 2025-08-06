using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Handlers;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using ClubDoorman.Test.TestKit;
using NUnit.Framework;
using System.Reflection;
using Telegram.Bot.Types;
using Moq;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Handlers;

namespace ClubDoorman.Test.Unit.Handlers;

/// <summary>
/// Тесты для HandleStatsCommandAsync метода
/// <tags>unit, message-handler, stats-command, command-handling</tags>
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("MessageHandler")]
public class MessageHandlerStatsCommandTests
{
    private MessageHandler _messageHandler;

    [SetUp]
    public void Setup()
    {
        // Используем AutoFixture для автоматического создания всех зависимостей
        _messageHandler = TestKitAutoFixture.CreateMessageHandler();
    }

    /// <summary>
    /// Тест для HandleStatsCommandAsync с AutoFixture
    /// Проверяет, что метод выполняется без исключений
    /// <tags>autofixture, stats-command, basic-test</tags>
    /// </summary>
    [Test]
    public async Task HandleStatsCommandAsync_WithAutoFixture_ExecutesWithoutExceptions()
    {
        // Arrange
        var message = TK.CreateStatsCommandMessage();

        // Act & Assert - используем рефлексию для доступа к приватному методу
        var method = typeof(MessageHandler).GetMethod("HandleStatsCommandAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        Assert.DoesNotThrowAsync(async () => 
            await (Task)method!.Invoke(_messageHandler, new object[] { message, CancellationToken.None })!);
    }

    /// <summary>
    /// Тест для HandleStatsCommandAsync с кастомной статистикой
    /// Проверяет обработку статистики с реальными данными
    /// <tags>stats-command, custom-stats, integration</tags>
    /// </summary>
    [Test]
    public async Task HandleStatsCommandAsync_WithCustomStats_ProcessesStatisticsCorrectly()
    {
        // Arrange
        var (handler, fixture) = TestKitAutoFixture.CreateWithFixture<MessageHandler>();
        var message = TK.CreateStatsCommandMessage();

        // Создаем кастомную статистику
        var stats = new Dictionary<long, ChatStats>
        {
            { 123456, new ChatStats("Test Chat") { BlacklistBanned = 5, StoppedCaptcha = 3 } },
            { 789012, new ChatStats("Another Chat") { KnownBadMessage = 2, LongNameBanned = 1 } }
        };

        // Инжектируем статистику в AutoFixture
        TestKitAutoFixture.Inject(stats);

        // Act - используем рефлексию
        var method = typeof(MessageHandler).GetMethod("HandleStatsCommandAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        await (Task)method!.Invoke(handler, new object[] { message, CancellationToken.None })!;

        // Assert
        Assert.Pass("Метод обработал кастомную статистику без исключений");
    }

    /// <summary>
    /// Тест для HandleStatsCommandAsync с пустой статистикой
    /// Проверяет обработку случая, когда нет данных для отображения
    /// <tags>stats-command, empty-stats, edge-case</tags>
    /// </summary>
    [Test]
    public async Task HandleStatsCommandAsync_WithEmptyStats_HandlesEmptyData()
    {
        // Arrange
        var message = TK.CreateStatsCommandMessage();
        var emptyStats = new Dictionary<long, ChatStats>();

        // Создаем handler с настроенным моком статистики
        var statisticsServiceMock = TK.CreateMockStatisticsService();
        statisticsServiceMock.Setup(x => x.GetAllStats()).Returns(emptyStats);

        var handler = TK.CreateMessageHandlerBuilder()
            .Build();

        // Настраиваем статистику через рефлексию
        var statisticsServiceField = typeof(MessageHandler).GetField("_statisticsService", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        statisticsServiceField!.SetValue(handler, statisticsServiceMock.Object);

        // Act - используем рефлексию
        var method = typeof(MessageHandler).GetMethod("HandleStatsCommandAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        await (Task)method!.Invoke(handler, new object[] { message, CancellationToken.None })!;

        // Assert
        Assert.Pass("Метод обработал пустую статистику без исключений");
    }

    /// <summary>
    /// Тест для HandleStatsCommandAsync с исключением в статистике
    /// Проверяет обработку ошибок при получении статистики
    /// <tags>stats-command, exception-handling, error-scenario</tags>
    /// </summary>
    [Test]
    public async Task HandleStatsCommandAsync_WithStatisticsException_ThrowsException()
    {
        // Arrange
        var message = TK.CreateStatsCommandMessage();
        var statisticsServiceMock = TK.CreateMockStatisticsService();
        statisticsServiceMock.Setup(x => x.GetAllStats())
            .Throws(new Exception("Statistics service error"));

        var handler = TK.CreateMessageHandlerBuilder()
            .Build();

        // Настраиваем статистику через рефлексию
        var statisticsServiceField = typeof(MessageHandler).GetField("_statisticsService", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        statisticsServiceField!.SetValue(handler, statisticsServiceMock.Object);

        // Act & Assert - используем рефлексию
        var method = typeof(MessageHandler).GetMethod("HandleStatsCommandAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        Assert.ThrowsAsync<Exception>(async () => 
            await (Task)method!.Invoke(handler, new object[] { message, CancellationToken.None })!);
    }

    /// <summary>
    /// Тест для HandleStatsCommandAsync с проверкой уведомлений
    /// Проверяет, что уведомление отправляется пользователю
    /// <tags>stats-command, notification, message-service</tags>
    /// </summary>
    [Test]
    public async Task HandleStatsCommandAsync_WithValidStats_SendsNotificationToUser()
    {
        // Arrange
        var message = TK.CreateStatsCommandMessage();
        var stats = new Dictionary<long, ChatStats>
        {
            { 123456, new ChatStats("Test Chat") { BlacklistBanned = 5 } }
        };

        var statisticsServiceMock = TK.CreateMockStatisticsService();
        statisticsServiceMock.Setup(x => x.GetAllStats()).Returns(stats);

        var messageServiceMock = new Mock<IMessageService>();
        messageServiceMock.Setup(x => x.SendUserNotificationAsync(
            It.IsAny<User>(),
            It.IsAny<Chat>(),
            It.IsAny<UserNotificationType>(),
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = TK.CreateMessageHandlerBuilder()
            .Build();

        // Настраиваем зависимости через рефлексию
        var statisticsServiceField = typeof(MessageHandler).GetField("_statisticsService", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var messageServiceField = typeof(MessageHandler).GetField("_messageService", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        statisticsServiceField!.SetValue(handler, statisticsServiceMock.Object);
        messageServiceField!.SetValue(handler, messageServiceMock.Object);

        // Act - используем рефлексию
        var method = typeof(MessageHandler).GetMethod("HandleStatsCommandAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        await (Task)method!.Invoke(handler, new object[] { message, CancellationToken.None })!;

        // Assert
        messageServiceMock.Verify(
            x => x.SendUserNotificationAsync(
                message.From!,
                message.Chat,
                UserNotificationType.SystemInfo,
                It.IsAny<SimpleNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Должно быть отправлено уведомление пользователю");
    }

    /// <summary>
    /// Тест для HandleStatsCommandAsync с различными типами статистики
    /// Проверяет форматирование разных типов данных
    /// <tags>stats-command, formatting, different-stats-types</tags>
    /// </summary>
    [Test]
    public async Task HandleStatsCommandAsync_WithDifferentStatsTypes_FormatsCorrectly()
    {
        // Arrange
        var message = TK.CreateStatsCommandMessage();
        var stats = new Dictionary<long, ChatStats>
        {
            { 111111, new ChatStats("Chat 1") { BlacklistBanned = 10 } },
            { 222222, new ChatStats("Chat 2") { StoppedCaptcha = 5 } },
            { 333333, new ChatStats("Chat 3") { KnownBadMessage = 3 } },
            { 444444, new ChatStats("Chat 4") { LongNameBanned = 2 } }
        };

        var statisticsServiceMock = TK.CreateMockStatisticsService();
        statisticsServiceMock.Setup(x => x.GetAllStats()).Returns(stats);

        var messageServiceMock = new Mock<IMessageService>();
        messageServiceMock.Setup(x => x.SendUserNotificationAsync(
            It.IsAny<User>(),
            It.IsAny<Chat>(),
            It.IsAny<UserNotificationType>(),
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = TK.CreateMessageHandlerBuilder()
            .Build();

        // Настраиваем зависимости через рефлексию
        var statisticsServiceField = typeof(MessageHandler).GetField("_statisticsService", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var messageServiceField = typeof(MessageHandler).GetField("_messageService", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        statisticsServiceField!.SetValue(handler, statisticsServiceMock.Object);
        messageServiceField!.SetValue(handler, messageServiceMock.Object);

        // Act - используем рефлексию
        var method = typeof(MessageHandler).GetMethod("HandleStatsCommandAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        await (Task)method!.Invoke(handler, new object[] { message, CancellationToken.None })!;

        // Assert - проверяем, что уведомление было отправлено
        messageServiceMock.Verify(
            x => x.SendUserNotificationAsync(
                It.IsAny<User>(),
                It.IsAny<Chat>(),
                UserNotificationType.SystemInfo,
                It.IsAny<SimpleNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Должно быть отправлено уведомление с форматированной статистикой");
    }
} 