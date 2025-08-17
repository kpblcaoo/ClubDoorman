using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Effects;
using ClubDoorman.Effects.Channel;
using ClubDoorman.Models;
using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("parity")]
public class ChannelModerationDualRunParityTests
{
    private (ChannelModerationService svc, Mock<IModerationService> mod, Mock<ITelegramBotClientWrapper> bot, Mock<IUserBanService> ban, TestChannelEffectsFlags flags, IEffectBus bus) Create(bool dualRun)
    {
        var bot = new Mock<ITelegramBotClientWrapper>(MockBehavior.Strict);
        var mod = new Mock<IModerationService>(MockBehavior.Strict);
        var ban = new Mock<IUserBanService>(MockBehavior.Strict);
        var logger = LoggerFactory.Create(b => b.AddFilter(_=>true).SetMinimumLevel(LogLevel.Debug)).CreateLogger<ChannelModerationService>();
        var builderLogger = LoggerFactory.Create(b => b.AddFilter(_=>true)).CreateLogger<ChannelModerationEffectsBuilder>();
        var effectsBuilder = new ChannelModerationEffectsBuilder(builderLogger, bot.Object, ban.Object, mod.Object);
        var flags = new TestChannelEffectsFlags { EffectsEnabled = true, DualRunEnabled = dualRun, ChannelAutoBanEnabled = false };
        var bus = new EffectBus();
        var svc = new ChannelModerationService(bot.Object, mod.Object, ban.Object, logger, flags, effectsBuilder, bus);
        return (svc, mod, bot, ban, flags, bus);
    }

    private static Message Msg() => new() { Chat = new Chat { Id = -1, Title = "Chat" }, SenderChat = new Chat { Id = -2, Title = "Chan" } };

    [Test]
    public async Task DualRun_And_SingleRun_ProduceSameModerationAction()
    {
        var message = Msg();
        var result = new ModerationResult(ModerationAction.Delete, "r");

        // single-run
        var (svc1, mod1, bot1, ban1, _, _) = Create(false);
        mod1.Setup(m => m.CheckMessageAsync(message)).ReturnsAsync(result);
        bot1.Setup(b => b.DeleteMessage(message.Chat.Id, It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await svc1.HandleChannelMessageAsync(message);

        // dual-run
        var message2 = Msg();
        var (svc2, mod2, bot2, ban2, _, _) = Create(true);
        // expect two identical calls (effects path + legacy path)
        mod2.SetupSequence(m => m.CheckMessageAsync(message2))
            .ReturnsAsync(result)
            .ReturnsAsync(result);
    bot2.Setup(b => b.DeleteMessage(message2.Chat.Id, It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await svc2.HandleChannelMessageAsync(message2);

    mod2.Verify(m => m.CheckMessageAsync(It.IsAny<Message>()), Times.Exactly(2));
    bot2.Verify(b => b.DeleteMessage(message2.Chat.Id, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once, "Delete should execute only via effects path in dual-run");
    }
}

internal sealed class TestChannelEffectsFlags : IChannelEffectsFlags
{
    public bool EffectsEnabled { get; set; }
    public bool DualRunEnabled { get; set; }
    public bool ChannelAutoBanEnabled { get; set; }
}
