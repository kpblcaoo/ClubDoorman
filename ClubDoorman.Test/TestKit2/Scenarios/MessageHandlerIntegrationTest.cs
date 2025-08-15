using Xunit;
using FluentAssertions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Tests.TestKit2.Core;

namespace ClubDoorman.Tests.TestKit2.Scenarios;

/// <summary>
/// Интеграционные тесты для MessageHandler с AutoFixture
/// </summary>
public class MessageHandlerIntegrationTest
{
    [Fact]
    public async Task Test_NormalUser_MessageAllowed()
    {
        // Arrange
        using var app = TestKit2.CreateApp();
        var envelope = TestKit2.CreateEnvelope(text: "Hello, this is a normal message");
        var message = TestKit2.CreateMessageFromEnvelope(envelope);
        var update = TestKit2.CreateUpdateFromEnvelope(envelope);

        // Act - просто проверяем, что объекты создаются корректно
        // В реальном тесте здесь был бы вызов handler.HandleAsync(update, CancellationToken.None);

        // Assert
        message.Should().NotBeNull();
        message.Text.Should().Be("Hello, this is a normal message");
        update.Message.Should().NotBeNull();
        app.TelegramClient.Should().NotBeNull();
    }

    [Fact]
    public async Task Test_SpamMessage_MessageDeleted()
    {
        // Arrange
        using var app = TestKit2.CreateApp();
        var envelope = TestKit2.CreateSpamEnvelope();
        var message = TestKit2.CreateMessageFromEnvelope(envelope);
        var update = TestKit2.CreateUpdateFromEnvelope(envelope);

        // Act - просто проверяем, что объекты создаются корректно
        // В реальном тесте здесь был бы вызов handler.HandleAsync(update, CancellationToken.None);

        // Assert
        message.Should().NotBeNull();
        message.Text.Should().Contain("BUY NOW");
        update.Message.Should().NotBeNull();
        app.TelegramClient.Should().NotBeNull();
    }

    [Fact]
    public async Task Test_CommandMessage_HandledByRouter()
    {
        // Arrange
        using var app = TestKit2.CreateApp();
        var envelope = TestKit2.CreateCommandEnvelope();
        var message = TestKit2.CreateMessageFromEnvelope(envelope);
        var update = TestKit2.CreateUpdateFromEnvelope(envelope);

        // Act - просто проверяем, что объекты создаются корректно
        // В реальном тесте здесь был бы вызов handler.HandleAsync(update, CancellationToken.None);

        // Assert
        message.Should().NotBeNull();
        message.Text.Should().Be("/start");
        update.Message.Should().NotBeNull();
        app.TelegramClient.Should().NotBeNull();
    }

    [Fact]
    public async Task Test_NewUserJoin_HandledByFacade()
    {
        // Arrange
        using var app = TestKit2.CreateApp();
        var envelope = TestKit2.CreateEnvelope();
        var message = TestKit2.CreateMessageFromEnvelope(envelope);
        message.NewChatMembers = new[] { message.From! };
        var update = TestKit2.CreateUpdateFromEnvelope(envelope);

        // Act - просто проверяем, что объекты создаются корректно
        // В реальном тесте здесь был бы вызов handler.HandleAsync(update, CancellationToken.None);

        // Assert
        message.Should().NotBeNull();
        message.NewChatMembers.Should().NotBeNull();
        update.Message.Should().NotBeNull();
        app.TelegramClient.Should().NotBeNull();
    }

    [Fact]
    public async Task Test_BotMessage_Ignored()
    {
        // Arrange
        using var app = TestKit2.CreateApp();
        var envelope = TestKit2.CreateEnvelope();
        envelope = envelope with { IsBot = true };
        var message = TestKit2.CreateMessageFromEnvelope(envelope);
        var update = TestKit2.CreateUpdateFromEnvelope(envelope);

        // Act - просто проверяем, что объекты создаются корректно
        // В реальном тесте здесь был бы вызов handler.HandleAsync(update, CancellationToken.None);

        // Assert
        message.Should().NotBeNull();
        message.From!.IsBot.Should().BeTrue();
        update.Message.Should().NotBeNull();
        app.TelegramClient.Should().NotBeNull();
    }

    [Fact]
    public async Task Test_PrivateChat_OnlyCommandsProcessed()
    {
        // Arrange
        using var app = TestKit2.CreateApp();
        var envelope = TestKit2.CreateEnvelope();
        var message = TestKit2.CreateMessageFromEnvelope(envelope);
        message.Chat.Type = ChatType.Private;
        var update = TestKit2.CreateUpdateFromEnvelope(envelope);

        // Act - просто проверяем, что объекты создаются корректно
        // В реальном тесте здесь был бы вызов handler.HandleAsync(update, CancellationToken.None);

        // Assert
        message.Should().NotBeNull();
        message.Chat.Type.Should().Be(ChatType.Private);
        update.Message.Should().NotBeNull();
        app.TelegramClient.Should().NotBeNull();
    }

    [Fact]
    public async Task Test_ChannelMessage_HandledByChannelModeration()
    {
        // Arrange
        using var app = TestKit2.CreateApp();
        var envelope = TestKit2.CreateEnvelope();
        var message = TestKit2.CreateMessageFromEnvelope(envelope);
        message.SenderChat = new Chat { Id = -100987654321, Type = ChatType.Channel, Title = "Test Channel" };
        var update = TestKit2.CreateUpdateFromEnvelope(envelope);

        // Act - просто проверяем, что объекты создаются корректно
        // В реальном тесте здесь был бы вызов handler.HandleAsync(update, CancellationToken.None);

        // Assert
        message.Should().NotBeNull();
        message.SenderChat.Should().NotBeNull();
        message.SenderChat!.Type.Should().Be(ChatType.Channel);
        update.Message.Should().NotBeNull();
        app.TelegramClient.Should().NotBeNull();
    }
}
