using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services.Commands;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Test.TestKit;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Commands;

/// <summary>
/// Тесты для StatsCommandHandler - обработчика команды /stats
/// <tags>unit, commands, stats-command, statistics</tags>
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Commands")]
public class StatsCommandHandlerTests
{
    private Mock<ITelegramBotClientWrapper> _botMock;
    private Mock<IStatisticsService> _statisticsServiceMock;
    private Mock<IChatLinkFormatter> _chatLinkFormatterMock;
    private Mock<IMessageService> _messageServiceMock;
    private Mock<IAppConfig> _appConfigMock;
    private Mock<ILogger<StatsCommandHandler>> _loggerMock;
    private StatsCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _botMock = new Mock<ITelegramBotClientWrapper>();
        _statisticsServiceMock = new Mock<IStatisticsService>();
        _chatLinkFormatterMock = new Mock<IChatLinkFormatter>();
        _messageServiceMock = new Mock<IMessageService>();
        _appConfigMock = new Mock<IAppConfig>();
        _loggerMock = new Mock<ILogger<StatsCommandHandler>>();

        _handler = new StatsCommandHandler(
            _botMock.Object,
            _statisticsServiceMock.Object,
            _chatLinkFormatterMock.Object,
            _messageServiceMock.Object,
            _appConfigMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// Тест проверяет, что команда /stats от админа корректно обрабатывается
    /// <tags>stats-command, admin-command, notification</tags>
    /// </summary>
    [Test]
    public async Task HandleAsync_WithValidStatsFromAdmin_SendsNotificationToUser()
    {
        // Arrange
        var adminChatId = -1001234567890L;
        var message = TK.CreateStatsCommandMessage();
        message.Chat = new Chat { Id = adminChatId, Type = Telegram.Bot.Types.Enums.ChatType.Group };
        
        var stats = new Dictionary<long, ChatStats>
        {
            { 123456, new ChatStats("Test Chat") { BlacklistBanned = 5, StoppedCaptcha = 3 } }
        };

        _appConfigMock.Setup(x => x.AdminChatId).Returns(adminChatId);
        _statisticsServiceMock.Setup(x => x.GetAllStats()).Returns(stats);
        _chatLinkFormatterMock.Setup(x => x.GetChatLink(It.IsAny<long>(), It.IsAny<string>()))
            .Returns("Test Chat Link");

        // Act
        await _handler.HandleAsync(message, CancellationToken.None);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendUserNotificationAsync(
                message.From!,
                message.Chat,
                UserNotificationType.SystemInfo,
                It.IsAny<SimpleNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Должно быть отправлено уведомление с статистикой");
    }

    /// <summary>
    /// Тест проверяет, что команда /stats НЕ из админ-чата игнорируется
    /// <tags>stats-command, security, access-control</tags>
    /// </summary>
    [Test]
    public async Task HandleAsync_WithStatsFromNonAdminChat_DoesNotSendNotification()
    {
        // Arrange
        var adminChatId = -1001234567890L;
        var nonAdminChatId = -1009876543210L;
        var message = TK.CreateStatsCommandMessage();
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
    /// Тест проверяет обработку пустой статистики
    /// <tags>stats-command, empty-stats, edge-case</tags>
    /// </summary>
    [Test]
    public async Task HandleAsync_WithEmptyStats_SendsEmptyStatsMessage()
    {
        // Arrange
        var adminChatId = -1001234567890L;
        var message = TK.CreateStatsCommandMessage();
        message.Chat = new Chat { Id = adminChatId, Type = Telegram.Bot.Types.Enums.ChatType.Group };
        
        var emptyStats = new Dictionary<long, ChatStats>();

        _appConfigMock.Setup(x => x.AdminChatId).Returns(adminChatId);
        _statisticsServiceMock.Setup(x => x.GetAllStats()).Returns(emptyStats);

        // Act
        await _handler.HandleAsync(message, CancellationToken.None);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendUserNotificationAsync(
                message.From!,
                message.Chat,
                UserNotificationType.SystemInfo,
                It.Is<SimpleNotificationData>(d => d.Reason!.Contains("Ничего интересного не произошло")),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Должно быть отправлено уведомление о пустой статистике");
    }
}
