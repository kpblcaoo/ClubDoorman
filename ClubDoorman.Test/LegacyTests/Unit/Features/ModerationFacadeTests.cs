using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Models;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.AI;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Unit.Features;

/// <summary>
/// Тесты для ModerationFacade
/// </summary>
[TestFixture]
public class ModerationFacadeTests
{
    private Mock<IModerationPolicy> _moderationPolicyMock;
    private Mock<IUserBanService> _userBanServiceMock;
    private Mock<IUserFlowLogger> _userFlowLoggerMock;
    private Mock<ILogger<ModerationFacade>> _loggerMock;
    private Mock<IMessageService> _messageServiceMock;
    private Mock<INotificationService> _notificationServiceMock;
    private Mock<IAiCascadeService> _aiCascadeServiceMock;
    private ModerationFacade _moderationFacade;

    [SetUp]
    public void SetUp()
    {
        _moderationPolicyMock = new Mock<IModerationPolicy>();
        _userBanServiceMock = new Mock<IUserBanService>();
        _userFlowLoggerMock = new Mock<IUserFlowLogger>();
        _loggerMock = new Mock<ILogger<ModerationFacade>>();
        _messageServiceMock = new Mock<IMessageService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _aiCascadeServiceMock = new Mock<IAiCascadeService>();

        _moderationFacade = new ModerationFacade(
            _moderationPolicyMock.Object,
            _userBanServiceMock.Object,
            _userFlowLoggerMock.Object,
            _loggerMock.Object,
            _messageServiceMock.Object,
            _notificationServiceMock.Object,
            _aiCascadeServiceMock.Object);
    }

    [Test]
    public async Task CheckMessageAsync_CallsModerationPolicy()
    {
        // Arrange
        var message = TK.CreateMessage();

        var expectedResult = new ModerationResult(ModerationAction.Allow, "Message is safe");
        _moderationPolicyMock.Setup(x => x.CheckMessageAsync(message))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _moderationFacade.CheckMessageAsync(message);

        // Assert
        result.Should().Be(expectedResult);
        _moderationPolicyMock.Verify(x => x.CheckMessageAsync(message), Times.Once);
    }

    [Test]
    public async Task CheckUserNameAsync_CallsModerationPolicy()
    {
        // Arrange
        var user = new User { Id = 123, FirstName = "Test", Username = "testuser" };
        var expectedResult = new ModerationResult(ModerationAction.Allow, "Username is safe");
        
        _moderationPolicyMock.Setup(x => x.CheckUserNameAsync(user))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _moderationFacade.CheckUserNameAsync(user);

        // Assert
        result.Should().Be(expectedResult);
        _moderationPolicyMock.Verify(x => x.CheckUserNameAsync(user), Times.Once);
    }

    [Test]
    public async Task HandleUserMessageAsync_AllowAction_CallsIncrementGoodMessageCount()
    {
        // Arrange
        var message = new Message
        {
            Text = "Good message",
            From = new User { Id = 123, FirstName = "Test" },
            Chat = new Chat { Id = -456, Type = ChatType.Group }
        };
        var user = message.From;
        var chat = message.Chat;
        var moderationResult = new ModerationResult(ModerationAction.Allow, "Message is safe");

        _moderationPolicyMock.Setup(x => x.CheckAiDetectAndNotifyAdminsAsync(user, chat, message))
            .ReturnsAsync(false);

        // Act
        await _moderationFacade.HandleUserMessageAsync(message, user, chat, moderationResult, false, CancellationToken.None);

        // Assert
        _moderationPolicyMock.Verify(x => x.IncrementGoodMessageCountAsync(user, chat, "Good message"), Times.Once);
    }

    [Test]
    public async Task HandleUserMessageAsync_AllowAction_AiDetectBlocked_DoesNotIncrementGoodMessageCount()
    {
        // Arrange
        var message = new Message
        {
            Text = "Suspicious message",
            From = new User { Id = 123, FirstName = "Test" },
            Chat = new Chat { Id = -456, Type = ChatType.Group }
        };
        var user = message.From;
        var chat = message.Chat;
        var moderationResult = new ModerationResult(ModerationAction.Allow, "Message is safe");

        _moderationPolicyMock.Setup(x => x.CheckAiDetectAndNotifyAdminsAsync(user, chat, message))
            .ReturnsAsync(true); // AI detect blocked the message

        // Act
        await _moderationFacade.HandleUserMessageAsync(message, user, chat, moderationResult, false, CancellationToken.None);

        // Assert
        _moderationPolicyMock.Verify(x => x.IncrementGoodMessageCountAsync(It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task HandleUserMessageAsync_BanAction_CallsAutoBan()
    {
        // Arrange
        var message = new Message
        {
            Text = "Spam message",
            From = new User { Id = 123, FirstName = "Spammer" },
            Chat = new Chat { Id = -456, Type = ChatType.Group }
        };
        var user = message.From;
        var chat = message.Chat;
        var moderationResult = new ModerationResult(ModerationAction.Ban, "Spam detected");

        // Act
        await _moderationFacade.HandleUserMessageAsync(message, user, chat, moderationResult, false, CancellationToken.None);

        // Assert
        _userFlowLoggerMock.Verify(x => x.LogUserBanned(user, chat, "Spam detected"), Times.Once);
        _userBanServiceMock.Verify(x => x.AutoBanAsync(message, "Spam detected", CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task ExecuteModerationActionAsync_CallsModerationPolicy()
    {
        // Arrange
        var message = new Message
        {
            Text = "Test message",
            From = new User { Id = 123, FirstName = "Test" },
            Chat = new Chat { Id = -456, Type = ChatType.Group }
        };
        var moderationResult = new ModerationResult(ModerationAction.Delete, "Inappropriate content");

        // Act
        await _moderationFacade.ExecuteModerationActionAsync(message, moderationResult);

        // Assert
        _moderationPolicyMock.Verify(x => x.ExecuteModerationActionAsync(message, moderationResult), Times.Once);
    }

    [Test]
    public async Task IncrementGoodMessageCountAsync_CallsModerationPolicy()
    {
        // Arrange
        var user = new User { Id = 123, FirstName = "Test" };
        var chat = new Chat { Id = -456, Type = ChatType.Group };
        var messageText = "Good message";

        // Act
        await _moderationFacade.IncrementGoodMessageCountAsync(user, chat, messageText);

        // Assert
        _moderationPolicyMock.Verify(x => x.IncrementGoodMessageCountAsync(user, chat, messageText), Times.Once);
    }

    [Test]
    public void IsUserApproved_CallsModerationPolicy()
    {
        // Arrange
        var userId = 123L;
        var chatId = -456L;
        _moderationPolicyMock.Setup(x => x.IsUserApproved(userId, chatId)).Returns(true);

        // Act
        var result = _moderationFacade.IsUserApproved(userId, chatId);

        // Assert
        result.Should().BeTrue();
        _moderationPolicyMock.Verify(x => x.IsUserApproved(userId, chatId), Times.Once);
    }
}
