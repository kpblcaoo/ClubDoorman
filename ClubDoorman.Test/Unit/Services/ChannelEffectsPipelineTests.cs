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
    public async Task AutoBanEnabled_SkipsEffectsPipeline()
    {
        var bot = new Mock<ITelegramBotClientWrapper>();
        var moderation = new Mock<IModerationService>();
        var userBan = new Mock<IUserBanService>();
    var logger = new Mock<ILogger<ChannelModerationService>>();
        var builder = new Mock<IChannelModerationEffectsBuilder>();
        var effectBus = new Mock<IEffectBus>();

    var msg = CreateMessage(ModerationAction.Allow);
    // Simulate pure channel post: From is null so approved-user bypass won't trigger
    msg.From = null;
    // Ensure senderChat differs from chat (already) and not auto-forward to avoid early returns
    msg.IsAutomaticForward = false;
        var result = CreateResult(ModerationAction.Allow);
        moderation.Setup(m => m.CheckMessageAsync(It.IsAny<Message>())).ReturnsAsync(result);
    moderation.Setup(m => m.IsUserApproved(It.IsAny<long>(), It.IsAny<long>())).Returns(false);
        builder.Setup(b => b.BuildChannelEffects(It.IsAny<Message>(), It.IsAny<ModerationResult>()))
            .Returns(new IEffect[] { new FuncEffect(ct => Task.CompletedTask) });

        // Arrange admin list empty so owner check fails
        bot.Setup(b => b.GetChatAdministratorsAsync(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ChatMember>());
        var service = new ChannelModerationService(bot.Object, moderation.Object, userBan.Object, logger.Object, builder.Object, effectBus.Object);
    await service.HandleChannelMessageAsync(msg);

    builder.Verify(b => b.BuildChannelEffects(It.IsAny<Message>(), It.IsAny<ModerationResult>()), Times.Never, "With ChannelAutoBan enabled effects pipeline should be skipped");
    effectBus.Verify(b => b.ExecuteAsync(It.IsAny<IEffect[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // DualRun и legacy сценарии удалены – оставлены только актуальные тесты effects-enabled
}
