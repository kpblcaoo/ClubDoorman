using FluentAssertions;
using Xunit;
using ClubDoorman.Services.Telegram;

namespace ClubDoorman.Tests.TestKit2.Scenarios;

public class SimpleSmokeTest
{
    [Fact]
    public async Task TestApp_can_be_created_and_disposed()
    {
        // Arrange & Act
        await using var app = new TestApp();
        
        // Assert
        app.Should().NotBeNull();
    }

    [Fact]
    public async Task EffectsSink_can_record_effects()
    {
        // Arrange
        await using var app = new TestApp();
        var effectsSink = app.GetService<IEffectsSink>();
        
        // Act
        effectsSink.Add(new Effect(EffectType.Delete, 123, 456, "test"));
        var effects = effectsSink.Snapshot();
        
        // Assert
        effects.Should().HaveCount(1);
        effects[0].Type.Should().Be(EffectType.Delete);
        effects[0].ChatId.Should().Be(123);
        effects[0].UserId.Should().Be(456);
        effects[0].Reason.Should().Be("test");
    }

    [Fact]
    public async Task FakeTelegramClient_can_be_created()
    {
        // Arrange & Act
        await using var app = new TestApp();
        var telegramClient = app.GetService<ITelegramBotClientWrapper>();
        
        // Assert
        telegramClient.Should().NotBeNull();
        telegramClient.Should().BeOfType<FakeTelegramBotClientWrapper>();
    }

    [Fact]
    public async Task TestBuilders_can_create_telegram_objects()
    {
        // Arrange & Act
        var message = TestBuilders.Message()
            .WithChat(TestBuilders.Chat().WithId(123).WithTitle("testchat").Build())
            .WithFrom(TestBuilders.User().WithId(456).WithUsername("testuser").Build())
            .WithText("test message")
            .Build();
        
        var user = TestBuilders.User().WithId(456).WithUsername("testuser").Build();
        var chat = TestBuilders.Chat().WithId(123).WithTitle("testchat").Build();
        
        // Assert
        message.Should().NotBeNull();
        message.Chat.Id.Should().Be(123);
        message.From.Id.Should().Be(456);
        message.Text.Should().Be("test message");
        
        user.Should().NotBeNull();
        user.Id.Should().Be(456);
        user.Username.Should().Be("testuser");
        
        chat.Should().NotBeNull();
        chat.Id.Should().Be(123);
        chat.Title.Should().Be("testchat");
    }
}
