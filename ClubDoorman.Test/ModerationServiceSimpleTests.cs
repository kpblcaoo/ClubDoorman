using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Models;
using ClubDoorman.Infrastructure;
using ClubDoorman.TestInfrastructure;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.UserManagement;

namespace ClubDoorman.Test;

[TestFixture]
public class ModerationServiceSimpleTests : TestBase
{
    // Прямое использование FakeModerationService для детерминированных сценариев
    private IModerationService _moderationService = null!;
    private FakeModerationService _fakePolicy = null!;
    private Mock<ILogger<IModerationService>> _mockLogger = null!;
    // Removed legacy _setup-dependent convenience properties after refactor to direct FakeModerationService usage

    [SetUp]
    public void SetUp()
    {
        Console.WriteLine("Setting up test (FakeModerationService + adapter)...");

        // Минимальные mocks для FakeModerationService
        var classifier = new Mock<ISpamHamClassifier>();
        var mimicry = new Mock<IMimicryClassifier>();
        var badMessage = new Mock<IBadMessageManager>();
        var userManager = new Mock<IUserManager>();
        var aiChecks = new Mock<IAiChecks>();
        var suspicious = new Mock<ISuspiciousUsersStorage>();
        var botWrapper = new Mock<ITelegramBotClientWrapper>();
        var messageService = new Mock<IMessageService>();
        var userBan = new Mock<IUserBanService>();
        var fakeLogger = new Mock<ILogger<FakeModerationService>>();

        _fakePolicy = new FakeModerationService(
            classifier.Object,
            mimicry.Object,
            badMessage.Object,
            userManager.Object,
            aiChecks.Object,
            suspicious.Object,
            botWrapper.Object,
            messageService.Object,
            userBan.Object,
            fakeLogger.Object);

        _moderationService = new ModerationServiceAdapter(_fakePolicy);
        _mockLogger = TK.CreateLoggerMock<IModerationService>();

        Console.WriteLine("Setup completed");
    }

    [Test]
    public async Task CheckUserName_WithNullUser_ThrowsArgumentNullException()
    {
        Console.WriteLine("Starting CheckUserName_WithNullUser_ThrowsArgumentNullException");

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _moderationService.CheckUserNameAsync(null!));

        Assert.That(exception.ParamName, Is.EqualTo("user"));
        Console.WriteLine("Completed CheckUserName_WithNullUser_ThrowsArgumentNullException");
    }

    [Test]
    public async Task CheckUserName_WithNormalName_ReturnsAllow()
    {
        Console.WriteLine("Starting CheckUserName_WithNormalName_ReturnsAllow");

        // Arrange
        var user = new User { FirstName = "John", LastName = "Doe" };
        _fakePolicy.SetResult(new ModerationResult(ModerationAction.Allow, "Имя пользователя корректно"));

        // Act
        var result = await _moderationService.CheckUserNameAsync(user);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
        Assert.That(result.Reason, Is.EqualTo("Имя пользователя корректно"));
        Console.WriteLine("Completed CheckUserName_WithNormalName_ReturnsAllow");
    }

    [Test]
    public async Task CheckUserName_WithLongName_ReturnsReport()
    {
        Console.WriteLine("Starting CheckUserName_WithLongName_ReturnsReport");

        // Arrange
        var user = new User { FirstName = new string('A', 50), LastName = "Doe" };
        _fakePolicy.SetResult(new ModerationResult(ModerationAction.Report, "Подозрительно длинное имя"));

        // Act
        var result = await _moderationService.CheckUserNameAsync(user);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Report));
        Assert.That(result.Reason, Does.Contain("Подозрительно длинное имя"));
        Console.WriteLine("Completed CheckUserName_WithLongName_ReturnsReport");
    }

    [Test]
    public async Task CheckUserName_WithExtremelyLongName_ReturnsBan()
    {
        Console.WriteLine("Starting CheckUserName_WithExtremelyLongName_ReturnsBan");

        // Arrange
        var user = new User { FirstName = new string('A', 100), LastName = "Doe" };
        _fakePolicy.SetResult(new ModerationResult(ModerationAction.Ban, "Экстремально длинное имя"));

        // Act
        var result = await _moderationService.CheckUserNameAsync(user);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
        Assert.That(result.Reason, Does.Contain("Экстремально длинное имя"));
        Console.WriteLine("Completed CheckUserName_WithExtremelyLongName_ReturnsBan");
    }

    [Test]
    public async Task CheckMessage_WithNullMessage_ThrowsArgumentNullException()
    {
        Console.WriteLine("Starting CheckMessage_WithNullMessage_ThrowsArgumentNullException");

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _moderationService.CheckMessageAsync(null!));

        Assert.That(exception.ParamName, Is.EqualTo("message"));
        Console.WriteLine("Completed CheckMessage_WithNullMessage_ThrowsArgumentNullException");
    }

    [Test]
    public async Task CheckMessage_WithBannedUser_ReturnsBan()
    {
        Console.WriteLine("Starting CheckMessage_WithBannedUser_ReturnsBan");

        // Arrange
        var user = new User { Id = 123, FirstName = "Test" };
        var chat = new Chat { Id = 456, Type = ChatType.Group };
        var message = new Message { From = user, Chat = chat, Text = "Hello" };
        _fakePolicy.SetResult(new ModerationResult(ModerationAction.Ban, "Пользователь в блэклисте спамеров"));

        // Act
        var result = await _moderationService.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
        Assert.That(result.Reason, Is.EqualTo("Пользователь в блэклисте спамеров"));
        Console.WriteLine("Completed CheckMessage_WithBannedUser_ReturnsBan");
    }
}