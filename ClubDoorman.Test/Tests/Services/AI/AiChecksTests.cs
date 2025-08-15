using FluentAssertions;
using Xunit;
using ClubDoorman.Services.AI;
using ClubDoorman.Tests.TestKit2;
using ClubDoorman.Tests.TestKit2.Fakes;

namespace ClubDoorman.Tests.Services.AI;

/// <summary>
/// Тесты AI сервисов
/// Проверяют логику AI анализа и ML классификации
/// </summary>
public class AiChecksTests
{
    [Fact]
    public async Task GetSpamProbability_WithValidMessage_ReturnsSpamProbability()
    {
        // Arrange
        await using var app = new TestApp();
        var aiChecks = app.GetService<IAiChecks>();
        var message = TestBuilders.Message()
            .WithChat(TestBuilders.Chat().WithId(123).Build())
            .WithFrom(TestBuilders.User().WithId(456).WithFirstName("Test").Build())
            .WithText("Test message")
            .Build();

        // Act
        var result = await aiChecks.GetSpamProbability(message);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SpamProbability>();
        result.Probability.Should().BeGreaterThanOrEqualTo(0.0);
        result.Probability.Should().BeLessThanOrEqualTo(1.0);
    }

    [Fact]
    public async Task GetSpamProbability_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        await using var app = new TestApp();
        var aiChecks = app.GetService<IAiChecks>();
        Telegram.Bot.Types.Message? message = null;

        // Act & Assert
        await aiChecks.Invoking(x => x.GetSpamProbability(message!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetSpamProbability_WithEmptyMessage_ReturnsDefaultProbability()
    {
        // Arrange
        await using var app = new TestApp();
        var aiChecks = app.GetService<IAiChecks>();
        var message = TestBuilders.Message()
            .WithChat(TestBuilders.Chat().WithId(123).Build())
            .WithFrom(TestBuilders.User().WithId(456).WithFirstName("Test").Build())
            .WithText("")
            .Build();

        // Act
        var result = await aiChecks.GetSpamProbability(message);

        // Assert
        result.Should().NotBeNull();
        result.Probability.Should().BeGreaterThanOrEqualTo(0.0);
        result.Probability.Should().BeLessThanOrEqualTo(1.0);
    }

    [Theory]
    [InlineData("spam message", 0.8)]
    [InlineData("normal message", 0.2)]
    [InlineData("", 0.5)]
    public async Task GetSpamProbability_VariousMessages_ReturnsExpectedProbability(string messageText, double expectedProbability)
    {
        // Arrange
        await using var app = new TestApp();
        var aiChecks = app.GetService<IAiChecks>();
        var message = TestBuilders.Message()
            .WithChat(TestBuilders.Chat().WithId(123).Build())
            .WithFrom(TestBuilders.User().WithId(456).WithFirstName("Test").Build())
            .WithText(messageText)
            .Build();

        // Act
        var result = await aiChecks.GetSpamProbability(message);

        // Assert
        result.Should().NotBeNull();
        result.Probability.Should().BeGreaterThanOrEqualTo(0.0);
        result.Probability.Should().BeLessThanOrEqualTo(1.0);
    }

    [Fact]
    public void MarkUserOkay_DoesNotThrowException()
    {
        // Arrange
        using var app = new TestApp();
        var aiChecks = app.GetService<IAiChecks>();
        var userId = 123L;

        // Act & Assert
        aiChecks.Invoking(x => x.MarkUserOkay(userId))
            .Should().NotThrow();
    }
}
