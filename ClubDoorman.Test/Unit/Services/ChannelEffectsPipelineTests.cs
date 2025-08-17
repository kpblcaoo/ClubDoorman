using ClubDoorman.Effects.Channel;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models;
using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Effects; // IEffect, FuncEffect, IEffectBus
using NUnit.Framework;
using ClubDoorman.Test.TestInfrastructure.Logging;

namespace ClubDoorman.Test.Unit.Services;

/// <summary>
/// Tests for channel effects pipeline toggles (Stage 2/3)
/// </summary>
[TestFixture]
[Category("fast")]
[Category("critical")]
public class ChannelEffectsPipelineTests
{
    private Message CreateMessage(ModerationAction action)
    {
        return new Message
        {
            From = new User { Id = 1, FirstName = "Tester" },
            Chat = new Chat { Id = -1001, Title = "TestChat", Type = ChatType.Supergroup },
            SenderChat = new Chat { Id = -2002, Title = "TestChannel", Type = ChatType.Channel },
            Text = "Payload"
        };
    }

    private static ModerationResult CreateResult(ModerationAction action) => new ModerationResult(action, $"Reason-{action}");

    [Test]
    public async Task EffectsDisabled_UsesLegacyOnly()
    {
        var bot = new Mock<ITelegramBotClientWrapper>();
        var moderation = new Mock<IModerationService>();
        var userBan = new Mock<IUserBanService>();
    var logger = new Mock<ILogger<ChannelModerationService>>();
        var builder = new Mock<IChannelModerationEffectsBuilder>();
        var effectBus = new Mock<IEffectBus>();

        var msg = CreateMessage(ModerationAction.Allow);
        moderation.Setup(m => m.IsUserApproved(It.IsAny<long>(), It.IsAny<long>())).Returns(false);
        moderation.Setup(m => m.CheckMessageAsync(It.IsAny<Message>())).ReturnsAsync(CreateResult(ModerationAction.Allow));

    var flags = new Test.TestInfrastructure.ChannelModeration.TestChannelEffectsFlags { EffectsEnabled = false, DualRunEnabled = false, ChannelAutoBanEnabled = false };
    var service = new ChannelModerationService(bot.Object, moderation.Object, userBan.Object, logger.Object, flags, builder.Object, effectBus.Object);
    await service.HandleChannelMessageAsync(msg);

        builder.Verify(b => b.BuildChannelEffects(It.IsAny<Message>(), It.IsAny<ModerationResult>()), Times.Never);
        effectBus.Verify(b => b.ExecuteAsync(It.IsAny<IEffect[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task EffectsEnabled_BuildsAndExecutesEffects()
    {
        var bot = new Mock<ITelegramBotClientWrapper>();
        var moderation = new Mock<IModerationService>();
        var userBan = new Mock<IUserBanService>();
    var logger = new Mock<ILogger<ChannelModerationService>>();
        var builder = new Mock<IChannelModerationEffectsBuilder>();
        var effectBus = new Mock<IEffectBus>();

        var msg = CreateMessage(ModerationAction.Allow);
        var result = CreateResult(ModerationAction.Allow);
        moderation.Setup(m => m.CheckMessageAsync(It.IsAny<Message>())).ReturnsAsync(result);
        moderation.Setup(m => m.IsUserApproved(It.IsAny<long>(), It.IsAny<long>())).Returns(false);
        builder.Setup(b => b.BuildChannelEffects(msg, result)).Returns(new IEffect[] { new FuncEffect(ct => Task.CompletedTask) });

    var flags = new Test.TestInfrastructure.ChannelModeration.TestChannelEffectsFlags { EffectsEnabled = true, DualRunEnabled = false, ChannelAutoBanEnabled = false };
    var service = new ChannelModerationService(bot.Object, moderation.Object, userBan.Object, logger.Object, flags, builder.Object, effectBus.Object);
        await service.HandleChannelMessageAsync(msg);

        builder.Verify(b => b.BuildChannelEffects(msg, result), Times.Once);
        effectBus.Verify(b => b.ExecuteAsync(It.IsAny<IEffect[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DualRun_ExecutesLegacyAndEffects()
    {
        var bot = new Mock<ITelegramBotClientWrapper>();
        var moderation = new Mock<IModerationService>();
        var userBan = new Mock<IUserBanService>();
    var logger = new Mock<ILogger<ChannelModerationService>>();
        var builder = new Mock<IChannelModerationEffectsBuilder>();
        var effectBus = new Mock<IEffectBus>();

        var msg = CreateMessage(ModerationAction.Delete);
        var result = CreateResult(ModerationAction.Delete);
        moderation.SetupSequence(m => m.CheckMessageAsync(It.IsAny<Message>()))
            .ReturnsAsync(result) // effects path
            .ReturnsAsync(result); // legacy path inside ModerateChannelMessageContentAsync
        moderation.Setup(m => m.IsUserApproved(It.IsAny<long>(), It.IsAny<long>())).Returns(false);
        builder.Setup(b => b.BuildChannelEffects(msg, result)).Returns(new IEffect[] { new FuncEffect(ct => Task.CompletedTask) });

    var flags = new Test.TestInfrastructure.ChannelModeration.TestChannelEffectsFlags { EffectsEnabled = true, DualRunEnabled = true, ChannelAutoBanEnabled = false };
    var service = new ChannelModerationService(bot.Object, moderation.Object, userBan.Object, logger.Object, flags, builder.Object, effectBus.Object);
        await service.HandleChannelMessageAsync(msg);

        builder.Verify(b => b.BuildChannelEffects(msg, result), Times.Once);
        effectBus.Verify(b => b.ExecuteAsync(It.IsAny<IEffect[]>(), It.IsAny<CancellationToken>()), Times.Once);
        // Legacy path should have attempted delete
        bot.Verify(b => b.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DualRun_MismatchLogged_WhenActionsDiffer()
    {
        var bot = new Mock<ITelegramBotClientWrapper>();
        var moderation = new Mock<IModerationService>();
        var userBan = new Mock<IUserBanService>();
    var logger = new TestLogger<ChannelModerationService>();
        var builder = new Mock<IChannelModerationEffectsBuilder>();
        var effectBus = new Mock<IEffectBus>();

        var msg = CreateMessage(ModerationAction.Allow);
        var effectsResult = CreateResult(ModerationAction.Allow);
        var legacyResult = CreateResult(ModerationAction.Delete);

        // First call (effects path) -> Allow, second call (legacy) -> Delete
        moderation.SetupSequence(m => m.CheckMessageAsync(It.IsAny<Message>()))
            .ReturnsAsync(effectsResult)
            .ReturnsAsync(legacyResult);
        moderation.Setup(m => m.IsUserApproved(It.IsAny<long>(), It.IsAny<long>())).Returns(false);
        builder.Setup(b => b.BuildChannelEffects(msg, effectsResult)).Returns(new IEffect[] { new FuncEffect(ct => Task.CompletedTask) });

    var flags = new Test.TestInfrastructure.ChannelModeration.TestChannelEffectsFlags { EffectsEnabled = true, DualRunEnabled = true, ChannelAutoBanEnabled = false };
    var service = new ChannelModerationService(bot.Object, moderation.Object, userBan.Object, logger, flags, builder.Object, effectBus.Object);
        await service.HandleChannelMessageAsync(msg);

        // Verify both paths executed
        effectBus.Verify(b => b.ExecuteAsync(It.IsAny<IEffect[]>(), It.IsAny<CancellationToken>()), Times.Once);
        // Legacy delete action should have triggered delete
    bot.Verify(b => b.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

    var mismatch = logger.Records.FirstOrDefault(r => r.Level == LogLevel.Warning && r.Template == "[ChannelEffects][DualRun][Mismatch] LegacyAction={Legacy} EffectsAction={Effects} ChannelId={ChannelId} ChatId={ChatId}");
    Assert.That(mismatch, Is.Not.Null, "Expected mismatch warning log not found");
    Assert.That(mismatch!.Values["Legacy"]?.ToString(), Is.EqualTo(legacyResult.Action.ToString()));
    Assert.That(mismatch!.Values["Effects"]?.ToString(), Is.EqualTo(effectsResult.Action.ToString()));
    }
}
