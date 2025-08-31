using ClubDoorman.Effects.Report;
using ClubDoorman.Services.Messaging;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Effects;

[TestFixture]
public class ReportEffectsTest
{
    private Mock<INotificationService> _notificationServiceMock;
    private Mock<ILogger<ReportMessageEffect>> _loggerMock;
    private Message _testMessage;
    private User _testUser;

    [SetUp]
    public void Setup()
    {
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<ReportMessageEffect>>();

        _testUser = new User { Id = 456, Username = "testuser" };
        _testMessage = new Message
        {
            Text = "test message",
            From = _testUser,
            Chat = new Chat { Id = 789, Title = "Test Chat" }
        };
    }

    [Test]
    public async Task ReportMessageEffect_ShouldCallNotificationService()
    {
        // Arrange
        var effect = new ReportMessageEffect(
            _notificationServiceMock.Object,
            _loggerMock.Object,
            _testMessage,
            _testUser,
            false);

        // Act
        await effect.ExecuteAsync(CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(
            x => x.DontDeleteButReportMessage(_testMessage, _testUser, false, CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task ReportMessageEffect_WithSilentMode_ShouldPassSilentModeToService()
    {
        // Arrange
        var effect = new ReportMessageEffect(
            _notificationServiceMock.Object,
            _loggerMock.Object,
            _testMessage,
            _testUser,
            true);

        // Act
        await effect.ExecuteAsync(CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(
            x => x.DontDeleteButReportMessage(_testMessage, _testUser, true, CancellationToken.None),
            Times.Once);
    }
}
