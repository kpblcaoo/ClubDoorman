using ClubDoorman.Effects;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace ClubDoorman.Test.Integration.Effects;

[TestFixture]
public class EffectsConfigurationIntegrationTest
{
    private IServiceProvider _serviceProvider;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddClubDoorman();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Test]
    public void EffectsConfiguration_ShouldBeProperlyConfigured()
    {
        // Arrange & Act
        var effectsConfig = _serviceProvider.GetRequiredService<EffectsConfiguration>();

        // Assert
        Assert.That(effectsConfig.UseRealEffects, Is.True, "UseRealEffects should be true");
        Assert.That(effectsConfig.EnabledActions, Contains.Item("Delete"), "Delete should be in EnabledActions");
        Assert.That(effectsConfig.EnabledActions, Contains.Item("Report"), "Report should be in EnabledActions");
        Assert.That(effectsConfig.EnabledActions, Contains.Item("Ban"), "Ban should be in EnabledActions");
        Assert.That(effectsConfig.EnabledActions, Contains.Item("Allow"), "Allow should be in EnabledActions");
        Assert.That(effectsConfig.EnabledActions, Contains.Item("RequireManualReview"), "RequireManualReview should be in EnabledActions");
        Assert.That(effectsConfig.EnabledActions, Contains.Item("RequireAiAnalysis"), "RequireAiAnalysis should be in EnabledActions");
        Assert.That(effectsConfig.LegacyFallback, Is.True, "LegacyFallback should be true for safety");
        Assert.That(effectsConfig.LogComparison, Is.True, "LogComparison should be true");
    }

    [Test]
    public void EffectsConfiguration_ShouldEnableDeleteAndReportActions()
    {
        // Arrange
        var effectsConfig = _serviceProvider.GetRequiredService<EffectsConfiguration>();

        // Act & Assert
        Assert.That(effectsConfig.IsActionEnabled(ModerationAction.Delete), Is.True, "Delete action should be enabled");
        Assert.That(effectsConfig.IsActionEnabled(ModerationAction.Report), Is.True, "Report action should be enabled");
        Assert.That(effectsConfig.IsActionEnabled(ModerationAction.Ban), Is.True, "Ban action should be enabled");
        Assert.That(effectsConfig.IsActionEnabled(ModerationAction.Allow), Is.True, "Allow action should be enabled");
        Assert.That(effectsConfig.IsActionEnabled(ModerationAction.RequireManualReview), Is.True, "RequireManualReview action should be enabled");
        Assert.That(effectsConfig.IsActionEnabled(ModerationAction.RequireAiAnalysis), Is.True, "RequireAiAnalysis action should be enabled");
    }

    [Test]
    public void EffectBus_ShouldBeRealEffectBus()
    {
        // Arrange & Act
        var effectBus = _serviceProvider.GetRequiredService<IEffectBus>();

        // Assert
        Assert.That(effectBus, Is.TypeOf<EffectBus>(), "EffectBus should be real EffectBus, not LoggingEffectBus");
    }

    [Test]
    public void ModerationEffectsBuilder_ShouldBeRealBuilder()
    {
        // Arrange & Act
        var builder = _serviceProvider.GetRequiredService<IModerationEffectsBuilder>();

        // Assert
        Assert.That(builder, Is.TypeOf<ModerationEffectsBuilder>(), "Should be ModerationEffectsBuilder");
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
