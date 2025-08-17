using ClubDoorman.Effects.Allow;
using ClubDoorman.Features.Moderation;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Effects;

[TestFixture]
public class AllowEffectsTest
{
    private Mock<IModerationPolicy> _moderationPolicyMock;
    private Mock<ILogger<AllowMessageEffect>> _loggerMock;
    private Message _testMessage;
    private User _testUser;
    private Chat _testChat;

    [SetUp]
    public void Setup()
    {
        _moderationPolicyMock = new Mock<IModerationPolicy>();
        _loggerMock = new Mock<ILogger<AllowMessageEffect>>();
        
        _testUser = new User { Id = 456, Username = "testuser" };
        _testChat = new Chat { Id = 789, Title = "Test Chat" };
        _testMessage = new Message
        {
            Text = "test message",
            From = _testUser,
            Chat = _testChat
        };
    }

    [Test]
    public async Task AllowMessageEffect_WhenAiDetectNotBlocked_ShouldIncrementGoodMessageCount()
    {
        // Arrange
        var reason = "Test allow reason";
        _moderationPolicyMock.Setup(x => x.CheckAiDetectAndNotifyAdminsAsync(_testUser, _testChat, _testMessage))
            .ReturnsAsync(false); // AI детект не заблокировал

        var effect = new AllowMessageEffect(
            _moderationPolicyMock.Object,
            _loggerMock.Object,
            _testMessage,
            _testUser,
            _testChat,
            reason);

        // Act
        await effect.ExecuteAsync(CancellationToken.None);

        // Assert
        _moderationPolicyMock.Verify(
            x => x.CheckAiDetectAndNotifyAdminsAsync(_testUser, _testChat, _testMessage),
            Times.Once);
            
        _moderationPolicyMock.Verify(
            x => x.IncrementGoodMessageCountAsync(_testUser, _testChat, "test message"),
            Times.Once);
    }

    [Test]
    public async Task AllowMessageEffect_WhenAiDetectBlocked_ShouldNotIncrementGoodMessageCount()
    {
        // Arrange
        var reason = "Test allow reason";
        _moderationPolicyMock.Setup(x => x.CheckAiDetectAndNotifyAdminsAsync(_testUser, _testChat, _testMessage))
            .ReturnsAsync(true); // AI детект заблокировал

        var effect = new AllowMessageEffect(
            _moderationPolicyMock.Object,
            _loggerMock.Object,
            _testMessage,
            _testUser,
            _testChat,
            reason);

        // Act
        await effect.ExecuteAsync(CancellationToken.None);

        // Assert
        _moderationPolicyMock.Verify(
            x => x.CheckAiDetectAndNotifyAdminsAsync(_testUser, _testChat, _testMessage),
            Times.Once);
            
        _moderationPolicyMock.Verify(
            x => x.IncrementGoodMessageCountAsync(It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task AllowMessageEffect_WithCaption_ShouldUseCaptionAsMessageText()
    {
        // Arrange
        var reason = "Test allow reason";
        _testMessage.Text = null;
        _testMessage.Caption = "test caption";
        
        _moderationPolicyMock.Setup(x => x.CheckAiDetectAndNotifyAdminsAsync(_testUser, _testChat, _testMessage))
            .ReturnsAsync(false);

        var effect = new AllowMessageEffect(
            _moderationPolicyMock.Object,
            _loggerMock.Object,
            _testMessage,
            _testUser,
            _testChat,
            reason);

        // Act
        await effect.ExecuteAsync(CancellationToken.None);

        // Assert
        _moderationPolicyMock.Verify(
            x => x.IncrementGoodMessageCountAsync(_testUser, _testChat, "test caption"),
            Times.Once);
    }
}
