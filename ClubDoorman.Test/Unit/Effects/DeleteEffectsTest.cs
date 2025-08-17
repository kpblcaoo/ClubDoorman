using ClubDoorman.Effects.Delete;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Effects;

[TestFixture]
public class DeleteEffectsTest
{
    private Mock<INotificationService> _notificationServiceMock;
    private Mock<IUserBanService> _userBanServiceMock;
    private Mock<ILogger<DeleteToLogEffect>> _logger1Mock;
    private Mock<ILogger<DeleteWithReportEffect>> _logger2Mock;
    private Mock<ILogger<TrackViolationEffect>> _logger3Mock;
    private Message _testMessage;
    private User _testUser;

    [SetUp]
    public void Setup()
    {
        _notificationServiceMock = new Mock<INotificationService>();
        _userBanServiceMock = new Mock<IUserBanService>();
        _logger1Mock = new Mock<ILogger<DeleteToLogEffect>>();
        _logger2Mock = new Mock<ILogger<DeleteWithReportEffect>>();
        _logger3Mock = new Mock<ILogger<TrackViolationEffect>>();

        _testMessage = new Message
        {
            Text = "test message",
            From = new User { Id = 456, Username = "testuser" },
            Chat = new Chat { Id = 789, Title = "Test Chat" }
        };
        // MessageId is read-only, so we can't set it directly

        _testUser = new User { Id = 456, Username = "testuser" };
    }

    [Test]
    public async Task DeleteToLogEffect_ShouldCallNotificationService()
    {
        // Arrange
        var effect = new DeleteToLogEffect(
            _notificationServiceMock.Object,
            _logger1Mock.Object,
            _testMessage,
            "Банальное приветствие");

        // Act
        await effect.ExecuteAsync(CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(
            x => x.DeleteAndReportToLogChat(_testMessage, "Банальное приветствие", CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task DeleteWithReportEffect_ShouldCallNotificationService()
    {
        // Arrange
        var effect = new DeleteWithReportEffect(
            _notificationServiceMock.Object,
            _logger2Mock.Object,
            _testMessage,
            "Спам сообщение",
            false);

        // Act
        await effect.ExecuteAsync(CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(
            x => x.DeleteAndReportMessage(_testMessage, "Спам сообщение", false, CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task TrackViolationEffect_ShouldCallUserBanService()
    {
        // Arrange
        var effect = new TrackViolationEffect(
            _userBanServiceMock.Object,
            _logger3Mock.Object,
            _testMessage,
            _testUser,
            "Нарушение правил");

        // Act
        await effect.ExecuteAsync(CancellationToken.None);

        // Assert
        _userBanServiceMock.Verify(
            x => x.TrackViolationAndBanIfNeededAsync(_testMessage, _testUser, "Нарушение правил", CancellationToken.None),
            Times.Once);
    }
}
