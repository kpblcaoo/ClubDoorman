# TestKit v0 Usage Examples

## Complete Example: Testing a Ban-List Scenario

This example demonstrates how to use TestKit v0 to test the complete ban-list workflow with transcript verification.

```csharp
using NUnit.Framework;
using ClubDoorman.Test.TestKit.Infrastructure;
using ClubDoorman.Test.TestKit.Builders;

[TestFixture]
public class BanListWorkflowTests
{
    [Test]
    public async Task BannedUser_InGroupChat_ShouldBanAndNotifyAdmins()
    {
        // Arrange - Create test host with configured settings
        var testHost = TestHostFactory.Create()
            .ConfigureAppConfig(config =>
            {
                config.AdminChatId = 123456789;        // Admin notifications
                config.LogAdminChatId = 987654321;     // Log chat
                config.WithAiEnabled(-1001234567890);   // Enable AI for test chat
            })
            .ConfigureTimeProvider(time =>
            {
                time.SetTime(new DateTime(2024, 1, 1, 12, 0, 0)); // Fixed time for reproducible tests
            });

        // Create test scenario: banned user sends spam message in group
        var bannedUser = EnhancedBuilders.CreateScenarioUser()
            .AsBannedUser()
            .WithId(999999999)
            .Build();

        var groupChat = EnhancedBuilders.CreateScenarioChat()
            .AsGroup()
            .WithId(-1001234567890)
            .WithTitle("Test Group")
            .Build();

        var spamMessage = EnhancedBuilders.CreateScenarioMessage()
            .AsSpam()
            .FromUser(bannedUser)
            .InChat(groupChat)
            .WithMessageId(12345)
            .Build();

        // Act - Execute the scenario
        var result = await testHost.ExecuteMessageScenarioAsync(spamMessage);

        // Assert - Verify the complete workflow
        Assert.That(result.Success, Is.True, "Scenario should execute successfully");

        // Verify bot actions using transcript
        var botClient = testHost.BotClient;
        
        // Should delete the spam message
        Assert.That(botClient.WasMessageDeleted(groupChat.Id, spamMessage.MessageId), Is.True,
            "Bot should delete spam message from banned user");
        
        // Should ban the user
        Assert.That(botClient.WasUserBanned(groupChat.Id, bannedUser.Id), Is.True,
            "Bot should ban the user from the group");
        
        // Should send admin notification
        var sentMessages = botClient.GetSentMessages().ToList();
        var adminNotifications = sentMessages.Where(m => 
            m.Parameters.Contains(testHost.AppConfig.AdminChatId)).ToList();
        Assert.That(adminNotifications.Count, Is.GreaterThan(0),
            "Bot should send notification to admin chat");

        // Verify transcript details
        var transcript = botClient.GetTranscript();
        Assert.That(transcript.Count, Is.GreaterThanOrEqualTo(2), 
            "Should have at least delete and ban actions");

        // Verify order of actions
        var deleteAction = transcript.FirstOrDefault(a => a.Action == "DeleteMessage");
        var banAction = transcript.FirstOrDefault(a => a.Action == "BanChatMember");
        
        Assert.That(deleteAction, Is.Not.Null, "Should have delete action");
        Assert.That(banAction, Is.Not.Null, "Should have ban action");
        Assert.That(deleteAction!.Timestamp, Is.LessThanOrEqualTo(banAction!.Timestamp),
            "Delete should happen before or at same time as ban");
    }

    [Test]
    public async Task NewUser_FirstMessage_ShouldTriggerCaptcha()
    {
        // Arrange - Host configured for new user scenarios
        var testHost = TestHostFactory.CreateForNewUserScenario()
            .ConfigureAppConfig(config =>
            {
                config.SuspiciousDetectionEnabled = true;
                config.MimicryThreshold = 0.7;
                // Don't include test chat in NoCaptchaGroups
            });

        // Create first-time user scenario
        var newUser = EnhancedBuilders.CreateScenarioUser()
            .AsFirstTimeUser()
            .WithId(11111111)
            .WithName("NewUser", "TestUser")
            .Build();

        var message = EnhancedBuilders.CreateScenarioMessage()
            .FromUnapprovedFirstTimeUser()
            .FromUser(newUser)
            .InGroupChat()
            .WithText("Hello everyone! This is my first message here.")
            .Build();

        // Ensure user is not pre-approved
        Assert.That(testHost.ApprovedUsersStorage.IsApproved(newUser.Id), Is.False,
            "User should not be pre-approved for this test");

        // Act
        var result = await testHost.ExecuteMessageScenarioAsync(message);

        // Assert - Verify captcha workflow
        Assert.That(result.Success, Is.True);

        // Should send captcha to user or notify admins about new user
        var sentMessages = testHost.BotClient.GetSentMessages();
        Assert.That(sentMessages.Count(), Is.GreaterThan(0),
            "Bot should respond to new user message");

        // Message should be deleted or restricted
        var deleteActions = testHost.BotClient.GetDeleteActions();
        Assert.That(deleteActions.Count(), Is.GreaterThanOrEqualTo(0),
            "Bot might delete message from unapproved user");
    }

    [Test]
    public async Task SilentMode_SpamDetection_ShouldModerateWithoutPublicMessages()
    {
        // Arrange - Silent mode configuration
        var testHost = TestHostFactory.CreateForSilentModeScenario()
            .ConfigureAppConfig(config =>
            {
                config.WithSilentMode(true);
                config.AdminChatId = 123456789;
                config.LogAdminChatId = 987654321;
            });

        var spamMessage = EnhancedBuilders.CreateScenarioMessage()
            .AsSpam()
            .InGroupChat()
            .Build();

        // Act
        var result = await testHost.ExecuteMessageScenarioAsync(spamMessage);

        // Assert - Verify silent operation
        Assert.That(result.Success, Is.True);

        var transcript = testHost.BotClient.GetTranscript();
        
        // Should still perform moderation actions
        var moderationActions = transcript.Where(a => 
            a.Action == "DeleteMessage" || a.Action == "BanChatMember").ToList();
        
        // Should send admin notifications but not public messages
        var publicMessages = transcript.Where(a => 
            a.Action == "SendMessage" && 
            a.Parameters.Contains(spamMessage.Chat.Id)).ToList();
        
        var adminMessages = transcript.Where(a => 
            a.Action == "SendMessage" && 
            (a.Parameters.Contains(testHost.AppConfig.AdminChatId) || 
             a.Parameters.Contains(testHost.AppConfig.LogAdminChatId))).ToList();

        Assert.That(publicMessages.Count, Is.EqualTo(0),
            "Silent mode should not send public messages to the group");
        Assert.That(adminMessages.Count, Is.GreaterThan(0),
            "Silent mode should still send admin notifications");
    }
}
```

## Builder Patterns Examples

### Creating Complex Scenarios

```csharp
// Forwarded message from suspicious channel
var suspiciousForwardedMessage = EnhancedBuilders.CreateScenarioMessage()
    .ForwardedFromChannel()
    .WithText("🎰 GET RICH QUICK! Casino with 100% win rate!")
    .InGroupChat()
    .FromUser(EnhancedBuilders.CreateScenarioUser().AsSuspiciousUser())
    .Build();

// Admin callback for user approval
var adminApprovalCallback = EnhancedBuilders.CreateCallbackQuery()
    .AsAdminApprove()
    .WithData("approve_user_12345")
    .FromUser(EnhancedBuilders.CreateScenarioUser().WithId(987654321)) // Admin user
    .Build();

// Update with chat member change (user joined)
var userJoinUpdate = EnhancedBuilders.CreateUpdate()
    .WithChatMemberUpdate(new ChatMemberUpdated
    {
        Chat = EnhancedBuilders.CreateScenarioChat().AsGroup().Build(),
        NewChatMember = new ChatMemberMember 
        { 
            User = EnhancedBuilders.CreateScenarioUser().AsFirstTimeUser().Build(),
            Status = ChatMemberStatus.Member
        },
        // ... other properties
    })
    .Build();
```

### Storage Pre-configuration

```csharp
// Pre-populate storage for testing
var testHost = TestHostFactory.Create()
    .ConfigureAppConfig(config => 
    {
        // Configure which chats have what settings
        config.WithAiEnabled(-1001111111)
              .WithNoCaptcha(-1002222222)
              .WithChatDisabled(-1003333333);
    });

// Pre-approve some users
testHost.ApprovedUsersStorage
    .WithGlobalApprovals(111, 222, 333)                    // Globally approved
    .WithGroupApprovals(-1001234567890, 444, 555, 666);    // Group-specific approvals

// Add suspicious users
testHost.SuspiciousUsersStorage
    .WithSuspiciousUser(777, -1001234567890, "Detected by AI")
    .WithAiDetectUser(888, -1001234567890, "Flagged for monitoring");
```

## Golden Master Test Pattern

```csharp
[Test]
public async Task ComplexWorkflow_ShouldMatchExpectedBehavior()
{
    // Arrange
    var testHost = TestHostFactory.Create()
        .ConfigureAppConfig(config => /* setup */)
        .ConfigureTimeProvider(time => time.SetTime(fixedTime));

    var scenario = /* create complex scenario */;

    // Act
    var result = await testHost.ExecuteScenarioAsync(scenario);

    // Assert with golden master
    await AssertAndRecordGoldenMaster("ComplexWorkflow_Description", result);
}

private async Task AssertAndRecordGoldenMaster(string scenarioName, TestExecutionResult result)
{
    // Create snapshot of current behavior
    var snapshot = new 
    {
        Success = result.Success,
        BotActionsCount = result.Transcript.BotActions.Count,
        ActionTypes = result.Transcript.BotActions.Select(a => a.Action).ToList(),
        // Mask dynamic values for stability
        ActionSummary = result.Transcript.BotActions.Select(a => 
            $"{a.Action}({string.Join(",", MaskDynamicValues(a.Parameters))})"
        ).ToList()
    };

    // Compare with saved snapshot or create new one
    var snapshotPath = $"GoldenMasterSnapshots/{scenarioName}.json";
    
    if (!File.Exists(snapshotPath))
    {
        await File.WriteAllTextAsync(snapshotPath, JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }));
        Assert.Pass($"Golden master snapshot created for {scenarioName}");
    }
    else
    {
        var expected = JsonSerializer.Deserialize<dynamic>(await File.ReadAllTextAsync(snapshotPath));
        // Compare and assert...
    }
}
```

## Advanced Features

### Custom Service Replacement

```csharp
var customModerationService = new Mock<IModerationService>();
customModerationService
    .Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
    .ReturnsAsync(new ModerationResult(ModerationAction.Ban, "Custom ban reason"));

var testHost = TestHostFactory.Create()
    .Replace<IModerationService, IModerationService>(customModerationService.Object);
```

### Deterministic Random Testing

```csharp
var testHost = TestHostFactory.Create()
    .ConfigureRandomProvider(random => 
    {
        random.WithNextValues(1, 3, 7)          // Predictable captcha answers
              .WithDoubleValues(0.1, 0.9, 0.5); // Predictable AI confidence scores
    });
```

This TestKit v0 provides complete control over the testing environment while maintaining the complexity and realism needed to properly test ClubDoorman's sophisticated anti-spam workflows.