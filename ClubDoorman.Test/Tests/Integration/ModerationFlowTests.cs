using FluentAssertions;
using Xunit;
using ClubDoorman.Tests.TestKit2;
using ClubDoorman.Tests.TestKit2.Fakes;

namespace ClubDoorman.Tests.Integration;

/// <summary>
/// Интеграционные тесты полного flow модерации
/// Проверяют взаимодействие всех компонентов системы
/// </summary>
public class ModerationFlowTests
{
    [Fact]
    public async Task SpamMessage_ShouldBeDeleted()
    {
        // Arrange
        await using var app = new TestApp();
        var scenario = Scenario.With(app)
            .GivenMessage(123, 456, "spam message");

        // Act
        await scenario.WhenHandled();

        // Assert
        scenario.ThenEffects()
            .Should().Contain(e => e.Type == EffectType.Delete);
    }

    [Fact]
    public async Task NormalMessage_ShouldBeAllowed()
    {
        // Arrange
        await using var app = new TestApp();
        var scenario = Scenario.With(app)
            .GivenMessage(123, 456, "Hello, this is a normal message");

        // Act
        await scenario.WhenHandled();

        // Assert
        scenario.ThenEffects()
            .Should().NotContain(e => e.Type == EffectType.Delete);
    }

    [Fact]
    public async Task UserWithButtons_ShouldBeBanned()
    {
        // Arrange
        await using var app = new TestApp();
        var message = TestBuilders.Message()
            .WithChat(TestBuilders.Chat().WithId(123).Build())
            .WithFrom(TestBuilders.User().WithId(456).Build())
            .WithText("Message with buttons")
            .Build();

        // Добавляем кнопки
        message.ReplyMarkup = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
        {
            new[] { new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton("Click me") { CallbackData = "test" } }
        });

        var scenario = Scenario.With(app)
            .GivenUpdate(TestBuilders.Update().WithMessage(message).Build());

        // Act
        await scenario.WhenHandled();

        // Assert
        scenario.ThenEffects()
            .Should().Contain(e => e.Type == EffectType.Ban);
    }

    [Theory]
    [InlineData("spam", EffectType.Delete)]
    [InlineData("normal message", EffectType.IncrementGood)]
    [InlineData("", EffectType.IncrementGood)]
    public async Task VariousMessages_ShouldHaveExpectedEffects(string messageText, EffectType expectedEffect)
    {
        // Arrange
        await using var app = new TestApp();
        var scenario = Scenario.With(app)
            .GivenMessage(123, 456, messageText);

        // Act
        await scenario.WhenHandled();

        // Assert
        scenario.ThenEffects()
            .Should().Contain(e => e.Type == expectedEffect);
    }

    [Fact]
    public async Task AiAnalysis_ShouldBeTriggered()
    {
        // Arrange
        await using var app = new TestApp();
        var aiService = app.GetService<IAiCascadeService>() as FakeAiCascadeService;
        aiService!.EnqueueProfileResult(true); // Пользователь ограничен

        var scenario = Scenario.With(app)
            .GivenMessage(123, 456, "suspicious message");

        // Act
        await scenario.WhenHandled();

        // Assert
        scenario.ThenEffects()
            .Should().Contain(e => e.Type == EffectType.AiCascade);
    }

    [Fact]
    public async Task MultipleMessages_ShouldBeProcessedIndependently()
    {
        // Arrange
        await using var app = new TestApp();
        
        // Первое сообщение
        var scenario1 = Scenario.With(app)
            .GivenMessage(123, 456, "first message");
        await scenario1.WhenHandled();

        // Второе сообщение
        var scenario2 = Scenario.With(app)
            .GivenMessage(123, 789, "second message");
        await scenario2.WhenHandled();

        // Assert
        scenario1.ThenEffects().Should().NotBeEmpty();
        scenario2.ThenEffects().Should().NotBeEmpty();
    }
}
