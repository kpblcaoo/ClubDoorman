using Xunit;
using FluentAssertions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Tests.TestKit2.Scenarios;

/// <summary>
/// Простые тесты для демонстрации возможностей TestKit2
/// </summary>
public class SimpleTests
{
    [Fact]
    public void Test_CreateEnvelope()
    {
        // Arrange & Act
        var envelope = TestKit2.CreateEnvelope(
            userId: 123456789,
            chatId: -1001234567890,
            text: "Test message",
            username: "testuser"
        );

        // Assert
        envelope.Should().NotBeNull();
        envelope.UserId.Should().Be(123456789);
        envelope.ChatId.Should().Be(-1001234567890);
        envelope.Text.Should().Be("Test message");
        envelope.Username.Should().Be("testuser");
    }

    [Fact]
    public void Test_CreateSpamEnvelope()
    {
        // Arrange & Act
        var envelope = TestKit2.CreateSpamEnvelope(
            userId: 999999999,
            chatId: -1009876543210
        );

        // Assert
        envelope.Should().NotBeNull();
        envelope.UserId.Should().Be(999999999);
        envelope.ChatId.Should().Be(-1009876543210);
        envelope.Text.Should().Contain("BUY NOW");
    }

    [Fact]
    public void Test_CreateMessageFromEnvelope()
    {
        // Arrange
        var envelope = TestKit2.CreateEnvelope(text: "Hello from envelope");

        // Act
        var message = TestKit2.CreateMessageFromEnvelope(envelope);

        // Assert
        message.Should().NotBeNull();
        message.Text.Should().Be("Hello from envelope");
        message.From.Should().NotBeNull();
        message.Chat.Should().NotBeNull();
    }

    [Fact]
    public void Test_CreateUpdateFromEnvelope()
    {
        // Arrange
        var envelope = TestKit2.CreateEnvelope(text: "Test update");

        // Act
        var update = TestKit2.CreateUpdateFromEnvelope(envelope);

        // Assert
        update.Should().NotBeNull();
        update.Message.Should().NotBeNull();
        update.Message.Text.Should().Be("Test update");
    }

    [Fact]
    public void Test_CreateCommandEnvelope()
    {
        // Arrange & Act
        var envelope = TestKit2.CreateCommandEnvelope(
            userId: 123456789,
            chatId: -1001234567890,
            command: "/ban"
        );

        // Assert
        envelope.Should().NotBeNull();
        envelope.Text.Should().Be("/ban");
        envelope.Username.Should().Be("admin");
    }

    [Fact]
    public void Test_CreateNewUserEnvelope()
    {
        // Arrange & Act
        var envelope = TestKit2.CreateNewUserEnvelope(
            userId: 111222333,
            chatId: -1001234567890
        );

        // Assert
        envelope.Should().NotBeNull();
        envelope.UserId.Should().Be(111222333);
        envelope.Username.Should().Be("newuser");
        envelope.FirstName.Should().Be("New");
        envelope.LastName.Should().Be("User");
    }

    [Fact]
    public void Test_CreateWithAutoFixture()
    {
        // Arrange & Act
        var user = TestKit2.Create<Telegram.Bot.Types.User>();
        var chat = TestKit2.Create<Telegram.Bot.Types.Chat>();

        // Assert
        user.Should().NotBeNull();
        chat.Should().NotBeNull();
    }

    [Fact]
    public void Test_CreateManyWithAutoFixture()
    {
        // Arrange & Act
        var users = TestKit2.CreateMany<Telegram.Bot.Types.User>(3);

        // Assert
        users.Should().HaveCount(3);
        users.Should().OnlyContain(u => u != null);
    }

    [Fact]
    public void Test_CreateWithCustomization()
    {
        // Arrange & Act
        var user = TestKit2.CreateWith<Telegram.Bot.Types.User>(u => 
        {
            u.Id = 12345;
            u.FirstName = "Custom";
            u.LastName = "User";
        });

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().Be(12345);
        user.FirstName.Should().Be("Custom");
        user.LastName.Should().Be("User");
    }
}
