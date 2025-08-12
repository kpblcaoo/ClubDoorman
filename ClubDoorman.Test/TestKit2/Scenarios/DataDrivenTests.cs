using FluentAssertions;
using Xunit;

namespace ClubDoorman.Tests.TestKit2.Scenarios;

/// <summary>
/// Примеры data-driven тестов с [Theory]
/// Демонстрирует преимущества подхода:
/// - Меньше дублирования кода
/// - Легко добавлять новые test cases
/// - Четкая связь между входными данными и ожидаемым результатом
/// </summary>
public class DataDrivenTests
{
    [Theory]
    [InlineData("spam message", true)]
    [InlineData("hello world", false)]
    [InlineData("normal conversation", false)]
    [InlineData("buy now!", true)]
    public async Task Message_analysis_detects_spam_correctly(string messageText, bool expectedSpam)
    {
        // Arrange
        await using var app = new TestApp();
        var scenario = Scenario.With(app).GivenMessage(100, 200, messageText);
        
        // Act
        await scenario.WhenHandled();
        var effects = scenario.ThenEffects();
        
        // Assert
        if (expectedSpam)
        {
            effects.Should().Contain(e => e.Type == EffectType.Report || e.Type == EffectType.Delete, 
                $"spam message '{messageText}' should be reported or deleted");
        }
        else
        {
            effects.Should().Contain(e => e.Type == EffectType.IncrementGood, 
                $"normal message '{messageText}' should increment good counter");
        }
    }

    [Theory]
    [InlineData("/start", "start command")]
    [InlineData("/help", "help command")]
    [InlineData("/stats", "stats command")]
    public async Task Commands_are_handled_without_errors(string command, string description)
    {
        // Arrange
        await using var app = new TestApp();
        var scenario = Scenario.With(app).GivenMessage(100, 200, command);
        
        // Act
        await scenario.WhenHandled();
        var effects = scenario.ThenEffects();
        
        // Assert - команды могут не производить эффекты, но не должны падать
        effects.Should().NotBeNull($"{description} should not throw exceptions");
    }

    [Theory]
    [MemberData(nameof(GetUserScenarios))]
    public async Task Different_users_are_handled_correctly(long userId, string username, bool shouldBeAllowed)
    {
        // Arrange
        await using var app = new TestApp();
        var scenario = Scenario.With(app).GivenMessage(100, userId, "test message");
        
        // Act
        await scenario.WhenHandled();
        var effects = scenario.ThenEffects();
        
        // Assert
        if (shouldBeAllowed)
        {
            effects.Should().Contain(e => e.Type == EffectType.IncrementGood, 
                $"user {username} (ID: {userId}) should be allowed");
        }
        else
        {
            effects.Should().Contain(e => e.Type == EffectType.Ban, 
                $"user {username} (ID: {userId}) should be banned");
        }
    }

    public static IEnumerable<object[]> GetUserScenarios()
    {
        yield return new object[] { 123L, "normal_user", true };
        yield return new object[] { 456L, "suspicious_user", false };
        yield return new object[] { 789L, "banned_user", false };
    }
}
