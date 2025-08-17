using ClubDoorman.Effects.Ban;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.UserFlow;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Effects;

[TestFixture]
public class BanEffectsTest
{
    private Mock<IUserBanService> _userBanServiceMock;
    private Mock<IUserFlowLogger> _userFlowLoggerMock;
    private Mock<ILogger<BanUserEffect>> _loggerMock;
    private Message _testMessage;
    private User _testUser;
    private Chat _testChat;

    [SetUp]
    public void Setup()
    {
        _userBanServiceMock = new Mock<IUserBanService>();
        _userFlowLoggerMock = new Mock<IUserFlowLogger>();
        _loggerMock = new Mock<ILogger<BanUserEffect>>();
        
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
    public async Task BanUserEffect_ShouldCallUserFlowLoggerAndUserBanService()
    {
        // Arrange
        var reason = "Test ban reason";
        var effect = new BanUserEffect(
            _userBanServiceMock.Object,
            _userFlowLoggerMock.Object,
            _loggerMock.Object,
            _testMessage,
            _testUser,
            _testChat,
            reason);

        // Act
        await effect.ExecuteAsync(CancellationToken.None);

        // Assert
        _userFlowLoggerMock.Verify(
            x => x.LogUserBanned(_testUser, _testChat, reason),
            Times.Once);
            
        _userBanServiceMock.Verify(
            x => x.AutoBanAsync(_testMessage, reason, CancellationToken.None),
            Times.Once);
    }
}
