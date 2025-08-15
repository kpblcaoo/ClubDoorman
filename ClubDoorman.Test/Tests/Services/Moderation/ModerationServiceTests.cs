using FluentAssertions;
using Xunit;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Tests.TestKit2;
using ClubDoorman.Tests.TestKit2.Fakes;

namespace ClubDoorman.Tests.Services.Moderation;

/// <summary>
/// Тесты бизнес-логики ModerationService
/// Проверяют реальную логику модерации
/// </summary>
public class ModerationServiceTests
{
    [Fact]
    public async Task CheckMessageAsync_UserInBanlist_ReturnsBanAction()
    {
        // Arrange
        await using var app = new TestApp();
        var moderationService = app.GetService<IModerationService>();
        var message = TestBuilders.Message()
            .WithChat(TestBuilders.Chat().WithId(123).Build())
            .WithFrom(TestBuilders.User().WithId(456).Build())
            .WithText("Hello world")
            .Build();

        // Act
        var result = await moderationService.CheckMessageAsync(message);

        // Assert
        result.Should().NotBeNull();
        result.Action.Should().Be(ModerationAction.Ban);
        result.Reason.Should().Contain("блэклисте");
    }

    [Fact]
    public async Task CheckMessageAsync_MessageWithButtons_ReturnsBanAction()
    {
        // Arrange
        await using var app = new TestApp();
        var moderationService = app.GetService<IModerationService>();
        var message = TestBuilders.Message()
            .WithChat(TestBuilders.Chat().WithId(123).Build())
            .WithFrom(TestBuilders.User().WithId(456).Build())
            .WithText("Hello")
            .Build();

        // Добавляем кнопки
        message.ReplyMarkup = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
        {
            new[] { new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton("Button 1") { CallbackData = "test" } }
        });

        // Act
        var result = await moderationService.CheckMessageAsync(message);

        // Assert
        result.Should().NotBeNull();
        result.Action.Should().Be(ModerationAction.Ban);
        result.Reason.Should().Contain("кнопками");
    }

    [Fact]
    public async Task CheckMessageAsync_StoryMessage_ReturnsDeleteAction()
    {
        // Arrange
        await using var app = new TestApp();
        var moderationService = app.GetService<IModerationService>();
        var message = TestBuilders.Message()
            .WithChat(TestBuilders.Chat().WithId(123).Build())
            .WithFrom(TestBuilders.User().WithId(456).Build())
            .WithText("Hello")
            .Build();

        // Добавляем Story
        message.Story = new Telegram.Bot.Types.Story { Id = 1 };

        // Act
        var result = await moderationService.CheckMessageAsync(message);

        // Assert
        result.Should().NotBeNull();
        result.Action.Should().Be(ModerationAction.Delete);
        result.Reason.Should().Contain("Story");
    }

    [Theory]
    [InlineData("spam message", ModerationAction.Ban)]
    [InlineData("normal message", ModerationAction.Allow)]
    [InlineData("", ModerationAction.Allow)]
    public async Task CheckMessageAsync_VariousMessages_ReturnsExpectedAction(string messageText, ModerationAction expectedAction)
    {
        // Arrange
        await using var app = new TestApp();
        var moderationService = app.GetService<IModerationService>();
        var message = TestBuilders.Message()
            .WithChat(TestBuilders.Chat().WithId(123).Build())
            .WithFrom(TestBuilders.User().WithId(456).Build())
            .WithText(messageText)
            .Build();

        // Act
        var result = await moderationService.CheckMessageAsync(message);

        // Assert
        result.Should().NotBeNull();
        result.Action.Should().Be(expectedAction);
    }
}
