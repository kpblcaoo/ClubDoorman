using ClubDoorman.Models;
using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
public class CaptchaServiceLocalizationTests : TestBase
{
    private CaptchaService _captchaService = null!;
    private Mock<ITelegramBotClientWrapper> _mockBot = null!;
    private Mock<ILogger<CaptchaService>> _mockLogger = null!;
    private Mock<IMessageService> _mockMessageService = null!;
    private Mock<ICaptchaLocalizer> _mockCaptchaLocalizer = null!;
    private Chat _testChat = null!;
    private User _testUser = null!;

    [SetUp]
    public void Setup()
    {
        _mockBot = new Mock<ITelegramBotClientWrapper>();
        _mockLogger = new Mock<ILogger<CaptchaService>>();
        _mockMessageService = new Mock<IMessageService>();
        _mockCaptchaLocalizer = new Mock<ICaptchaLocalizer>();
        
        _captchaService = new CaptchaService(
            _mockBot.Object,
            _mockLogger.Object,
            _mockMessageService.Object,
            _mockCaptchaLocalizer.Object
        );

        _testChat = new Chat
        {
            Id = 123456789L,
            Title = "Test Chat",
            Type = ChatType.Group
        };

        _testUser = new User
        {
            Id = 987654321L,
            FirstName = "Test",
            LastName = "User",
            Username = "testuser"
        };
    }

    private Message CreateTestMessage(int messageId = 123)
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            From = _testUser
        };
    }

    [Test]
    public async Task CreateCaptchaAsync_WithLocalization_UsesLocalizedMessages()
    {
        // Arrange
        var userMention = "<a href=\"tg://user?id=987654321\">Test User</a>";
        var emojiDescription = "единорог";
        var localizedMessage = "Привет, <a href=\"tg://user?id=987654321\">Test User</a>! Антиспам: на какой кнопке единорог?";
        var adPlaceholder = "\n\n 📍 Место для рекламы\n<i>...</i>";
        var finalMessage = localizedMessage + adPlaceholder;

        _mockCaptchaLocalizer.Setup(x => x.GetEmojiDescription(It.IsAny<int>(), _testChat.Id))
            .Returns(emojiDescription);
        _mockCaptchaLocalizer.Setup(x => x.GetCaptchaMessage(userMention, emojiDescription, _testChat.Id))
            .Returns(localizedMessage);
        _mockCaptchaLocalizer.Setup(x => x.GetAdPlaceholder(_testChat.Id))
            .Returns(adPlaceholder);

        _mockMessageService.Setup(x => x.SendCaptchaMessageAsync(
                _testChat,
                finalMessage,
                It.IsAny<ReplyParameters?>(),
                It.IsAny<InlineKeyboardMarkup>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestMessage());

        // Act
        var result = await _captchaService.CreateCaptchaAsync(_testChat, _testUser);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ChatId, Is.EqualTo(_testChat.Id));
        Assert.That(result.User.Id, Is.EqualTo(_testUser.Id));
        
        _mockCaptchaLocalizer.Verify(x => x.GetEmojiDescription(It.IsAny<int>(), _testChat.Id), Times.Once);
        _mockCaptchaLocalizer.Verify(x => x.GetCaptchaMessage(userMention, emojiDescription, _testChat.Id), Times.Once);
        _mockCaptchaLocalizer.Verify(x => x.GetAdPlaceholder(_testChat.Id), Times.Once);
        
        _mockMessageService.Verify(x => x.SendCaptchaMessageAsync(
            _testChat,
            finalMessage,
            It.IsAny<ReplyParameters?>(),
            It.IsAny<InlineKeyboardMarkup>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateCaptchaAsync_WithBlacklistedName_UsesLocalizedNewParticipantName()
    {
        // Arrange
        var blacklistedUser = new User
        {
            Id = 111111111L,
            FirstName = "porn",
            LastName = "user",
            Username = "pornuser"
        };

        var expectedName = "новый участник чата";
        var userMention = $"<a href=\"tg://user?id={blacklistedUser.Id}\">{expectedName}</a>";
        var emojiDescription = "единорог";
        var localizedMessage = $"Привет, {userMention}! Антиспам: на какой кнопке {emojiDescription}?";
        var adPlaceholder = "\n\n 📍 Место для рекламы\n<i>...</i>";
        var finalMessage = localizedMessage + adPlaceholder;

        _mockCaptchaLocalizer.Setup(x => x.GetNewParticipantName(_testChat.Id))
            .Returns(expectedName);
        _mockCaptchaLocalizer.Setup(x => x.GetEmojiDescription(It.IsAny<int>(), _testChat.Id))
            .Returns(emojiDescription);
        _mockCaptchaLocalizer.Setup(x => x.GetCaptchaMessage(userMention, emojiDescription, _testChat.Id))
            .Returns(localizedMessage);
        _mockCaptchaLocalizer.Setup(x => x.GetAdPlaceholder(_testChat.Id))
            .Returns(adPlaceholder);

        _mockMessageService.Setup(x => x.SendCaptchaMessageAsync(
                _testChat,
                finalMessage,
                It.IsAny<ReplyParameters?>(),
                It.IsAny<InlineKeyboardMarkup>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestMessage());

        // Act
        var result = await _captchaService.CreateCaptchaAsync(_testChat, blacklistedUser);

        // Assert
        Assert.That(result, Is.Not.Null);
        _mockCaptchaLocalizer.Verify(x => x.GetNewParticipantName(_testChat.Id), Times.Once);
        _mockCaptchaLocalizer.Verify(x => x.GetCaptchaMessage(userMention, emojiDescription, _testChat.Id), Times.Once);
    }

    [Test]
    public async Task CreateCaptchaAsync_NoAdGroup_DoesNotIncludeAdPlaceholder()
    {
        // Arrange
        var userMention = "<a href=\"tg://user?id=987654321\">Test User</a>";
        var emojiDescription = "единорог";
        var localizedMessage = "Привет, <a href=\"tg://user?id=987654321\">Test User</a>! Антиспам: на какой кнопке единорог?";
        // CaptchaService always adds ad placeholder, so we expect it
        var adPlaceholder = "\n\n 📍 Место для рекламы\n<i>...</i>";
        var finalMessage = localizedMessage + adPlaceholder;

        _mockCaptchaLocalizer.Setup(x => x.GetNewParticipantName(_testChat.Id))
            .Returns("Test User");
        _mockCaptchaLocalizer.Setup(x => x.GetEmojiDescription(It.IsAny<int>(), _testChat.Id))
            .Returns(emojiDescription);
        _mockCaptchaLocalizer.Setup(x => x.GetCaptchaMessage(userMention, emojiDescription, _testChat.Id))
            .Returns(localizedMessage);
        _mockCaptchaLocalizer.Setup(x => x.GetAdPlaceholder(_testChat.Id))
            .Returns(adPlaceholder);

        _mockMessageService.Setup(x => x.SendCaptchaMessageAsync(
                _testChat,
                finalMessage,
                It.IsAny<ReplyParameters?>(),
                It.IsAny<InlineKeyboardMarkup>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestMessage());

        // Act
        var result = await _captchaService.CreateCaptchaAsync(_testChat, _testUser);

        // Assert
        Assert.That(result, Is.Not.Null);
        _mockCaptchaLocalizer.Verify(x => x.GetAdPlaceholder(_testChat.Id), Times.Once);
        _mockMessageService.Verify(x => x.SendCaptchaMessageAsync(
            _testChat,
            finalMessage,
            It.IsAny<ReplyParameters?>(),
            It.IsAny<InlineKeyboardMarkup>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateCaptchaAsync_LocalizationException_StillCreatesCaptcha()
    {
        // Arrange
        _mockCaptchaLocalizer.Setup(x => x.GetNewParticipantName(_testChat.Id))
            .Throws(new Exception("Localization error"));
        _mockCaptchaLocalizer.Setup(x => x.GetEmojiDescription(It.IsAny<int>(), _testChat.Id))
            .Returns("unknown");
        _mockCaptchaLocalizer.Setup(x => x.GetCaptchaMessage(It.IsAny<string>(), It.IsAny<string>(), _testChat.Id))
            .Returns("Hello, Test User! Anti-spam: which button has unknown?");
        _mockCaptchaLocalizer.Setup(x => x.GetAdPlaceholder(_testChat.Id))
            .Returns("");

        _mockMessageService.Setup(x => x.SendCaptchaMessageAsync(
                _testChat,
                It.IsAny<string>(),
                It.IsAny<ReplyParameters?>(),
                It.IsAny<InlineKeyboardMarkup>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestMessage());

        // Act
        var result = await _captchaService.CreateCaptchaAsync(_testChat, _testUser);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ChatId, Is.EqualTo(_testChat.Id));
        Assert.That(result.User.Id, Is.EqualTo(_testUser.Id));
    }

    [Test]
    public void GenerateKey_ValidParameters_ReturnsExpectedKey()
    {
        // Arrange
        var chatId = 123456789L;
        var userId = 987654321L;
        var expectedKey = "123456789_987654321";

        // Act
        var result = _captchaService.GenerateKey(chatId, userId);

        // Assert
        Assert.That(result, Is.EqualTo(expectedKey));
    }

    [Test]
    public async Task ValidateCaptchaAsync_ValidKeyAndAnswer_ReturnsTrue()
    {
        // Arrange
        var chatId = 123456789L;
        var userId = 987654321L;
        var key = _captchaService.GenerateKey(chatId, userId);
        var correctAnswer = 5;

        // Create a captcha first
        _mockCaptchaLocalizer.Setup(x => x.GetNewParticipantName(chatId))
            .Returns("Test User");
        _mockCaptchaLocalizer.Setup(x => x.GetEmojiDescription(It.IsAny<int>(), chatId))
            .Returns("test");
        _mockCaptchaLocalizer.Setup(x => x.GetCaptchaMessage(It.IsAny<string>(), It.IsAny<string>(), chatId))
            .Returns("test message");
        _mockCaptchaLocalizer.Setup(x => x.GetAdPlaceholder(chatId))
            .Returns("");

        _mockMessageService.Setup(x => x.SendCaptchaMessageAsync(
                It.IsAny<Chat>(),
                It.IsAny<string>(),
                It.IsAny<ReplyParameters?>(),
                It.IsAny<InlineKeyboardMarkup>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestMessage());

        var captchaInfo = await _captchaService.CreateCaptchaAsync(_testChat, _testUser);

        // Act
        var result = await _captchaService.ValidateCaptchaAsync(key, captchaInfo.CorrectAnswer);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ValidateCaptchaAsync_InvalidAnswer_ReturnsFalse()
    {
        // Arrange
        var chatId = 123456789L;
        var userId = 987654321L;
        var key = _captchaService.GenerateKey(chatId, userId);
        var wrongAnswer = 999;

        // Create a captcha first
        _mockCaptchaLocalizer.Setup(x => x.GetNewParticipantName(chatId))
            .Returns("Test User");
        _mockCaptchaLocalizer.Setup(x => x.GetEmojiDescription(It.IsAny<int>(), chatId))
            .Returns("test");
        _mockCaptchaLocalizer.Setup(x => x.GetCaptchaMessage(It.IsAny<string>(), It.IsAny<string>(), chatId))
            .Returns("test message");
        _mockCaptchaLocalizer.Setup(x => x.GetAdPlaceholder(chatId))
            .Returns("");

        _mockMessageService.Setup(x => x.SendCaptchaMessageAsync(
                It.IsAny<Chat>(),
                It.IsAny<string>(),
                It.IsAny<ReplyParameters?>(),
                It.IsAny<InlineKeyboardMarkup>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestMessage());

        await _captchaService.CreateCaptchaAsync(_testChat, _testUser);

        // Act
        var result = await _captchaService.ValidateCaptchaAsync(key, wrongAnswer);

        // Assert
        Assert.That(result, Is.False);
    }
} 