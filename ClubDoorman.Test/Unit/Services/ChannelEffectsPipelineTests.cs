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
    public async Task EffectsPipeline_Executes()
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

    // DualRun и legacy сценарии удалены – оставлены только актуальные тесты effects-enabled
}
