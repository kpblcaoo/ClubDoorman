using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Handlers;
using ClubDoorman.Infrastructure;
using ClubDoorman.Services;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Services.Telegram;

namespace ClubDoorman.Test.Unit.Services;

/// <summary>
/// Тесты для ChannelModerationService
/// <tags>channel, moderation, tests</tags>
/// </summary>
public class ChannelModerationServiceTests
{
    private Mock<ITelegramBotClientWrapper> _botMock = null!;
    private Mock<IModerationService> _moderationServiceMock = null!;
    private Mock<IUserBanService> _userBanServiceMock = null!;
    private Mock<ILogger<ChannelModerationService>> _loggerMock = null!;
    private ChannelModerationService _service = null!;

        [SetUp]
    public void Setup()
    {
        _botMock = new Mock<ITelegramBotClientWrapper>();
        _moderationServiceMock = new Mock<IModerationService>();
        _userBanServiceMock = new Mock<IUserBanService>();
        _loggerMock = new Mock<ILogger<ChannelModerationService>>();

        _service = new ChannelModerationService(
            _botMock.Object,
            _moderationServiceMock.Object,
            _userBanServiceMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task IsChannelOwnerAsync_WhenUserIsOwner_ShouldReturnTrue()
    {
        // Arrange
        var message = CreateTestMessage();
        var channelAdmins = new[]
        {
            new ChatMemberOwner { User = new User { Id = 123 } }
        };
        
        _botMock.Setup(x => x.GetChatAdministratorsAsync(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(channelAdmins);

        // Act
        var result = await _service.IsChannelOwnerAsync(message);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsChannelOwnerAsync_WhenUserIsNotOwner_ShouldReturnFalse()
    {
        // Arrange
        var message = CreateTestMessage();
        var channelAdmins = new[]
        {
            new ChatMemberOwner { User = new User { Id = 456 } }
        };
        
        _botMock.Setup(x => x.GetChatAdministratorsAsync(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(channelAdmins);

        // Act
        var result = await _service.IsChannelOwnerAsync(message);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ShouldAllowChannelMessageAsync_WhenUserIsOwner_ShouldReturnTrue()
    {
        // Arrange
        var message = CreateTestMessage();
        var channelAdmins = new[]
        {
            new ChatMemberOwner { User = new User { Id = 123 } }
        };
        
        _botMock.Setup(x => x.GetChatAdministratorsAsync(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(channelAdmins);
        _botMock.Setup(x => x.GetChatAsync(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Chat { Id = 1, Type = ChatType.Supergroup });

        // Act
        var result = await _service.ShouldAllowChannelMessageAsync(message);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ShouldAllowChannelMessageAsync_WhenChannelDiscussion_ShouldReturnTrue()
    {
        // Arrange
        var message = CreateTestMessage();
        message.IsAutomaticForward = true;
        
        _botMock.Setup(x => x.GetChatAsync(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Chat { Id = 1, Type = ChatType.Supergroup });

        // Act
        var result = await _service.ShouldAllowChannelMessageAsync(message);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ShouldAllowChannelMessageAsync_WhenUnknownChannel_ShouldReturnFalse()
    {
        // Arrange
        var message = CreateTestMessage();
        var channelAdmins = new[]
        {
            new ChatMemberOwner { User = new User { Id = 456 } }
        };
        
        _botMock.Setup(x => x.GetChatAdministratorsAsync(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(channelAdmins);
        _botMock.Setup(x => x.GetChatAsync(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Chat { Id = 1, Type = ChatType.Supergroup });

        // Act
        var result = await _service.ShouldAllowChannelMessageAsync(message);

        // Assert
        Assert.That(result, Is.False);
    }

    private static Message CreateTestMessage()
    {
        return new Message
        {
            From = new User { Id = 123, FirstName = "Test", Username = "testuser" },
            Chat = new Chat { Id = 1, Title = "Test Chat", Type = ChatType.Supergroup },
            SenderChat = new Chat { Id = 2, Title = "Test Channel", Type = ChatType.Channel },
            Text = "Test message"
        };
    }
} 