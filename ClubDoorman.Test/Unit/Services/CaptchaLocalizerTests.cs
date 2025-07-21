using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("unit")]
[Category("services")]
[Category("localization")]
public class CaptchaLocalizerTests : TestBase
{
    private ICaptchaLocalizer _captchaLocalizer = null!;
    private Mock<IMessageLocalizer> _mockMessageLocalizer = null!;
    private Mock<IChatCultureProvider> _mockCultureProvider = null!;
    private Mock<ILogger<CaptchaLocalizer>> _mockLogger = null!;

    [SetUp]
    public void Setup()
    {
        _mockMessageLocalizer = new Mock<IMessageLocalizer>();
        _mockCultureProvider = new Mock<IChatCultureProvider>();
        _mockLogger = new Mock<ILogger<CaptchaLocalizer>>();
        
        _captchaLocalizer = new CaptchaLocalizer(
            _mockMessageLocalizer.Object,
            _mockCultureProvider.Object,
            _mockLogger.Object
        );
    }

    [Test]
    public void GetEmojiDescription_ValidIndex_ReturnsLocalizedDescription()
    {
        // Arrange
        var chatId = 123456789L;
        var emojiIndex = 0; // unicorn
        var expectedDescription = "единорог";
        
        _mockMessageLocalizer.Setup(x => x.User("CaptchaUnicorn", chatId))
            .Returns(expectedDescription);

        // Act
        var result = _captchaLocalizer.GetEmojiDescription(emojiIndex, chatId);

        // Assert
        Assert.That(result, Is.EqualTo(expectedDescription));
        _mockMessageLocalizer.Verify(x => x.User("CaptchaUnicorn", chatId), Times.Once);
    }

    [Test]
    public void GetEmojiDescription_InvalidIndex_ReturnsUnknown()
    {
        // Arrange
        var chatId = 123456789L;
        var emojiIndex = 999; // invalid index

        // Act
        var result = _captchaLocalizer.GetEmojiDescription(emojiIndex, chatId);

        // Assert
        Assert.That(result, Is.EqualTo("unknown"));
        _mockMessageLocalizer.Verify(x => x.User(It.IsAny<string>(), It.IsAny<long>()), Times.Never);
    }

    [Test]
    public void GetEmojiDescription_Exception_ReturnsUnknown()
    {
        // Arrange
        var chatId = 123456789L;
        var emojiIndex = 0;
        
        _mockMessageLocalizer.Setup(x => x.User("CaptchaUnicorn", chatId))
            .Throws(new Exception("Test exception"));

        // Act
        var result = _captchaLocalizer.GetEmojiDescription(emojiIndex, chatId);

        // Assert
        Assert.That(result, Is.EqualTo("unknown"));
    }

    [Test]
    public void GetCaptchaMessage_ValidParameters_ReturnsLocalizedMessage()
    {
        // Arrange
        var chatId = 123456789L;
        var userMention = "<a href=\"tg://user?id=123\">Test User</a>";
        var emojiDescription = "единорог";
        var expectedMessage = "Привет, <a href=\"tg://user?id=123\">Test User</a>! Антиспам: на какой кнопке единорог?";
        
        _mockMessageLocalizer.Setup(x => x.User("CaptchaMessage", chatId, userMention, emojiDescription))
            .Returns(expectedMessage);

        // Act
        var result = _captchaLocalizer.GetCaptchaMessage(userMention, emojiDescription, chatId);

        // Assert
        Assert.That(result, Is.EqualTo(expectedMessage));
        _mockMessageLocalizer.Verify(x => x.User("CaptchaMessage", chatId, userMention, emojiDescription), Times.Once);
    }

    [Test]
    public void GetCaptchaMessage_Exception_ReturnsFallbackMessage()
    {
        // Arrange
        var chatId = 123456789L;
        var userMention = "<a href=\"tg://user?id=123\">Test User</a>";
        var emojiDescription = "единорог";
        
        _mockMessageLocalizer.Setup(x => x.User("CaptchaMessage", chatId, userMention, emojiDescription))
            .Throws(new Exception("Test exception"));

        // Act
        var result = _captchaLocalizer.GetCaptchaMessage(userMention, emojiDescription, chatId);

        // Assert
        Assert.That(result, Is.EqualTo($"Hello, {userMention}! Anti-spam: which button has {emojiDescription}?"));
    }

    [Test]
    public void GetAdPlaceholder_ValidChatId_ReturnsLocalizedPlaceholder()
    {
        // Arrange
        var chatId = 123456789L;
        var expectedPlaceholder = "\n\n 📍 Место для рекламы\n<i>...</i>";
        
        _mockMessageLocalizer.Setup(x => x.User("CaptchaAdPlaceholder", chatId))
            .Returns(expectedPlaceholder);

        // Act
        var result = _captchaLocalizer.GetAdPlaceholder(chatId);

        // Assert
        Assert.That(result, Is.EqualTo(expectedPlaceholder));
        _mockMessageLocalizer.Verify(x => x.User("CaptchaAdPlaceholder", chatId), Times.Once);
    }

    [Test]
    public void GetAdPlaceholder_Exception_ReturnsFallbackPlaceholder()
    {
        // Arrange
        var chatId = 123456789L;
        
        _mockMessageLocalizer.Setup(x => x.User("CaptchaAdPlaceholder", chatId))
            .Throws(new Exception("Test exception"));

        // Act
        var result = _captchaLocalizer.GetAdPlaceholder(chatId);

        // Assert
        Assert.That(result, Is.EqualTo("\n\n 📍 Ad space\n<i>...</i>"));
    }

    [Test]
    public void GetNewParticipantName_ValidChatId_ReturnsLocalizedName()
    {
        // Arrange
        var chatId = 123456789L;
        var expectedName = "новый участник чата";
        
        _mockMessageLocalizer.Setup(x => x.User("CaptchaNewParticipant", chatId))
            .Returns(expectedName);

        // Act
        var result = _captchaLocalizer.GetNewParticipantName(chatId);

        // Assert
        Assert.That(result, Is.EqualTo(expectedName));
        _mockMessageLocalizer.Verify(x => x.User("CaptchaNewParticipant", chatId), Times.Once);
    }

    [Test]
    public void GetNewParticipantName_Exception_ReturnsFallbackName()
    {
        // Arrange
        var chatId = 123456789L;
        
        _mockMessageLocalizer.Setup(x => x.User("CaptchaNewParticipant", chatId))
            .Throws(new Exception("Test exception"));

        // Act
        var result = _captchaLocalizer.GetNewParticipantName(chatId);

        // Assert
        Assert.That(result, Is.EqualTo("new chat participant"));
    }

    [Test]
    public void GetEmojiDescription_AllValidIndices_ReturnCorrectKeys()
    {
        // Arrange
        var chatId = 123456789L;
        var testCases = new[]
        {
            (0, "CaptchaUnicorn"),
            (1, "CaptchaHammer"),
            (2, "CaptchaCat"),
            (3, "CaptchaAnchor"),
            (4, "CaptchaDolphin"),
            (5, "CaptchaApple"),
            (6, "CaptchaBall"),
            (7, "CaptchaHorse"),
            (8, "CaptchaDuck"),
            (9, "CaptchaRaccoon"),
            (10, "CaptchaOwl"),
            (11, "CaptchaTurtle"),
            (12, "CaptchaCrab"),
            (13, "CaptchaBanana"),
            (14, "CaptchaWatermelon"),
            (15, "CaptchaClock"),
            (16, "CaptchaAirplane"),
            (17, "CaptchaKnife"),
            (18, "CaptchaTshirt"),
            (19, "CaptchaScissors"),
            (20, "CaptchaWhale"),
            (21, "CaptchaElephant"),
            (22, "CaptchaFlamingo"),
            (23, "CaptchaPopcorn"),
            (24, "CaptchaButterfly"),
            (25, "CaptchaCrown"),
            (26, "CaptchaSkull"),
            (27, "CaptchaBoomerang"),
            (28, "CaptchaEar")
        };

        foreach (var (index, expectedKey) in testCases)
        {
            _mockMessageLocalizer.Setup(x => x.User(expectedKey, chatId))
                .Returns($"test_{index}");
        }

        // Act & Assert
        foreach (var (index, expectedKey) in testCases)
        {
            var result = _captchaLocalizer.GetEmojiDescription(index, chatId);
            Assert.That(result, Is.EqualTo($"test_{index}"), $"Failed for index {index}");
            _mockMessageLocalizer.Verify(x => x.User(expectedKey, chatId), Times.Once);
        }
    }
} 