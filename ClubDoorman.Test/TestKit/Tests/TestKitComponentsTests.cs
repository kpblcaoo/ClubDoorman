using NUnit.Framework;
using ClubDoorman.Test.TestKit.Fakes;
using ClubDoorman.Test.TestKit.Builders;

namespace ClubDoorman.Test.TestKit.Tests;

/// <summary>
/// Простые юнит-тесты для проверки работоспособности TestKit v0 компонентов
/// </summary>
[TestFixture]
[Category("TestKit")]
public class TestKitComponentsTests
{
    [Test]
    public void AppConfigFake_ShouldReturnConfiguredValues()
    {
        // Arrange
        var appConfig = new AppConfigFake()
            .WithAiEnabled(-1001234567890)
            .WithMimicrySettings(enabled: true, threshold: 0.8)
            .WithNoCaptcha(-1009876543210);

        // Act & Assert
        Assert.That(appConfig.IsAiEnabledForChat(-1001234567890), Is.True);
        Assert.That(appConfig.SuspiciousDetectionEnabled, Is.True);
        Assert.That(appConfig.MimicryThreshold, Is.EqualTo(0.8));
        Assert.That(appConfig.NoCaptchaGroups.Contains(-1009876543210), Is.True);
        Assert.That(appConfig.BotApi, Is.EqualTo("fake:bot:token"));
    }

    [Test]
    public async Task BotClientFake_ShouldRecordActionsInTranscript()
    {
        // Arrange
        var botClient = new BotClientFake();
        var chatId = -1001234567890;
        var userId = 12345;
        var messageId = 100;

        // Act
        await botClient.SendMessage(chatId, "Test message");
        await botClient.DeleteMessage(chatId, messageId);
        await botClient.BanChatMember(chatId, userId);

        // Assert
        var transcript = botClient.GetTranscript();
        Assert.That(transcript.Count, Is.EqualTo(3));
        
        Assert.That(botClient.WasMessageSent("Test message"), Is.True);
        Assert.That(botClient.WasMessageDeleted(chatId, messageId), Is.True);
        Assert.That(botClient.WasUserBanned(chatId, userId), Is.True);

        // Check transcript details
        var sentMessages = botClient.GetSentMessages().ToList();
        var deleteActions = botClient.GetDeleteActions().ToList();
        var banActions = botClient.GetBanActions().ToList();
        
        Assert.That(sentMessages.Count, Is.EqualTo(1));
        Assert.That(deleteActions.Count, Is.EqualTo(1));
        Assert.That(banActions.Count, Is.EqualTo(1));
    }

    [Test]
    public void TimeProviderFake_ShouldProvideControlledTime()
    {
        // Arrange
        var timeProvider = new TimeProviderFake();
        var startTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        timeProvider.SetTime(startTime);
        var time1 = timeProvider.UtcNow;
        
        timeProvider.AdvanceMinutes(30);
        var time2 = timeProvider.UtcNow;
        
        timeProvider.AdvanceHours(1);
        var time3 = timeProvider.UtcNow;

        // Assert
        Assert.That(time1, Is.EqualTo(startTime));
        Assert.That(time2, Is.EqualTo(startTime.AddMinutes(30)));
        Assert.That(time3, Is.EqualTo(startTime.AddMinutes(30).AddHours(1)));
    }

    [Test]
    public void RandomProviderFake_ShouldProvidePredictableValues()
    {
        // Arrange
        var randomProvider = new RandomProviderFake()
            .WithNextValues(1, 2, 3)
            .WithDoubleValues(0.1, 0.5, 0.9);

        // Act & Assert
        Assert.That(randomProvider.Next(10), Is.EqualTo(1));
        Assert.That(randomProvider.Next(10), Is.EqualTo(2));
        Assert.That(randomProvider.Next(10), Is.EqualTo(3));
        
        Assert.That(randomProvider.NextDouble(), Is.EqualTo(0.1));
        Assert.That(randomProvider.NextDouble(), Is.EqualTo(0.5));
        Assert.That(randomProvider.NextDouble(), Is.EqualTo(0.9));
    }

    [Test]
    public void ApprovedUsersStorageFake_ShouldStoreAndRetrieveUsers()
    {
        // Arrange
        var storage = new ApprovedUsersStorageFake()
            .WithGlobalApprovals(111, 222)
            .WithGroupApprovals(-1001234567890, 333, 444);

        // Act & Assert
        Assert.That(storage.IsGloballyApproved(111), Is.True);
        Assert.That(storage.IsGloballyApproved(222), Is.True);
        Assert.That(storage.IsGloballyApproved(999), Is.False);
        
        Assert.That(storage.IsApprovedInGroup(333, -1001234567890), Is.True);
        Assert.That(storage.IsApprovedInGroup(444, -1001234567890), Is.True);
        Assert.That(storage.IsApprovedInGroup(555, -1001234567890), Is.False);
        
        Assert.That(storage.IsApproved(111), Is.True); // Global approval
        Assert.That(storage.IsApproved(333, -1001234567890), Is.True); // Group approval
        Assert.That(storage.IsApproved(999, -1001234567890), Is.False); // Not approved
    }

    [Test]
    public void SuspiciousUsersStorageFake_ShouldStoreAndRetrieveSuspiciousUsers()
    {
        // Arrange
        var storage = new SuspiciousUsersStorageFake()
            .WithSuspiciousUser(111, -1001234567890, "Test suspicious")
            .WithAiDetectUser(222, -1001234567890, "AI detection test");

        // Act & Assert
        Assert.That(storage.IsSuspicious(111, -1001234567890), Is.True);
        Assert.That(storage.IsSuspicious(222, -1001234567890), Is.True);
        Assert.That(storage.IsSuspicious(999, -1001234567890), Is.False);
        
        var suspiciousInfo = storage.GetSuspiciousInfo(111, -1001234567890);
        Assert.That(suspiciousInfo, Is.Not.Null);
        Assert.That(suspiciousInfo!.DetectionReason, Is.EqualTo("Test suspicious"));
        
        var aiDetectUsers = storage.GetAiDetectUsers();
        Assert.That(aiDetectUsers.Count, Is.EqualTo(1));
        Assert.That(aiDetectUsers[0], Is.EqualTo((222L, -1001234567890L)));
    }

    [Test]
    public void EnhancedBuilders_ShouldCreateScenarioObjects()
    {
        // Arrange & Act
        var bannedUser = EnhancedBuilders.CreateScenarioUser().AsBannedUser().Build();
        var firstTimeUser = EnhancedBuilders.CreateScenarioUser().AsFirstTimeUser().Build();
        
        var spamMessage = EnhancedBuilders.CreateScenarioMessage()
            .AsSpam()
            .FromUser(bannedUser)
            .InGroupChat()
            .Build();
        
        var commandMessage = EnhancedBuilders.CreateScenarioMessage()
            .AsCommand("/help")
            .FromUser(firstTimeUser)
            .InPrivateChat()
            .Build();
        
        var update = EnhancedBuilders.CreateUpdate()
            .WithMessage(spamMessage)
            .Build();

        // Assert
        Assert.That(bannedUser.Id, Is.EqualTo(99999999));
        Assert.That(bannedUser.FirstName, Is.EqualTo("Banned"));
        
        Assert.That(firstTimeUser.Id, Is.EqualTo(11111111));
        Assert.That(firstTimeUser.FirstName, Is.EqualTo("New"));
        
        Assert.That(spamMessage.Text, Contains.Substring("КАЗИНО"));
        Assert.That(spamMessage.From, Is.EqualTo(bannedUser));
        Assert.That(spamMessage.Chat.Type, Is.EqualTo(Telegram.Bot.Types.Enums.ChatType.Group));
        
        Assert.That(commandMessage.Text, Is.EqualTo("/help"));
        Assert.That(commandMessage.Entities, Is.Not.Null);
        Assert.That(commandMessage.Entities![0].Type, Is.EqualTo(Telegram.Bot.Types.Enums.MessageEntityType.BotCommand));
        
        Assert.That(update.Message, Is.EqualTo(spamMessage));
    }
}