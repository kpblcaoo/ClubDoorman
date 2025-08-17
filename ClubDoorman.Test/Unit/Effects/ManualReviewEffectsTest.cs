using ClubDoorman.Effects.ManualReview;
using ClubDoorman.Services.Messaging;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Effects;

[TestFixture]
public class ManualReviewEffectsTest
{
    private Mock<INotificationService> _notificationServiceMock;
    private Mock<ILogger<RequireManualReviewEffect>> _loggerMock;
    private Message _testMessage;
    private User _testUser;

    [SetUp]
    public void Setup()
    {
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<RequireManualReviewEffect>>();
        
        _testUser = new User { Id = 456, Username = "testuser" };
        _testMessage = new Message
        {
            Text = "test message",
            From = _testUser
        };
    }

    [Test]
    public async Task RequireManualReviewEffect_ShouldCallNotificationService()
    {
        // Arrange
        var isSilentMode = false;
        var effect = new RequireManualReviewEffect(
            _notificationServiceMock.Object,
            _loggerMock.Object,
            _testMessage,
            _testUser,
            isSilentMode);

        // Act
        await effect.ExecuteAsync(CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(
            x => x.DontDeleteButReportMessage(_testMessage, _testUser, isSilentMode, CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task RequireManualReviewEffect_WithSilentMode_ShouldPassSilentModeToNotificationService()
    {
        // Arrange
        var isSilentMode = true;
        var effect = new RequireManualReviewEffect(
            _notificationServiceMock.Object,
            _loggerMock.Object,
            _testMessage,
            _testUser,
            isSilentMode);

        // Act
        await effect.ExecuteAsync(CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(
            x => x.DontDeleteButReportMessage(_testMessage, _testUser, isSilentMode, CancellationToken.None),
            Times.Once);
    }
}
