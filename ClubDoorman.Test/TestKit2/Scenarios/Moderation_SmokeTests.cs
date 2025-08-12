namespace ClubDoorman.Test.TestKit2.Scenarios;

public class Moderation_SmokeTests
{
    [Fact]
    public async Task Message_flow_produces_some_effect()
    {
        await using var app = new TestApp();
        var scenario = Scenario.With(app).GivenMessage(100, 200, "hello");
        await scenario.WhenHandled();
        var effects = scenario.ThenEffects();

        effects.Should().NotBeEmpty("message processing should produce at least one effect");
        effects.Should().Contain(e => e.Type == EffectType.IncrementGood, 
            "allowed message should increment good counter");
    }

    [Fact]
    public async Task Command_message_handled_without_errors()
    {
        await using var app = new TestApp();
        var scenario = Scenario.With(app).GivenMessage(100, 200, "/start");
        await scenario.WhenHandled();
        var effects = scenario.ThenEffects();

        // Commands might not produce effects but should not throw
        effects.Should().NotBeNull("effects collection should exist");
    }
}
