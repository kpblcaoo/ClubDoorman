using ClubDoorman.Effects.Channel;
using ClubDoorman.Models;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.ChannelModeration; // updated namespace after relocation
using ClubDoorman.Services.Moderation; // for IModerationService dependency
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
public class ChannelModerationEffectsBuilderTests
{
    private Message CreateMessage() => new()
    {
        Chat = new Chat { Id = -1, Type = ChatType.Supergroup, Title = "Chat" },
        SenderChat = new Chat { Id = -2, Type = ChatType.Channel, Title = "Channel" },
        Text = "hello"
    };

    private static ModerationResult Res(ModerationAction action) => new(action, action.ToString());

    [Test]
    public void Allow_Returns_LogOnly()
    {
        var bot = new Mock<ITelegramBotClientWrapper>();
        var ban = new Mock<IUserBanService>();
        var moderation = new Mock<IModerationService>();
        var b = new ChannelModerationEffectsBuilder(new LoggerFactory().CreateLogger<ChannelModerationEffectsBuilder>(), bot.Object, ban.Object, moderation.Object);
        var effects = b.BuildChannelEffects(CreateMessage(), Res(ModerationAction.Allow));
        Assert.That(effects.Length, Is.EqualTo(1));
    }

    [Test]
    public void Delete_Returns_DeleteEffect()
    {
        var bot = new Mock<ITelegramBotClientWrapper>();
        var ban = new Mock<IUserBanService>();
        var moderation = new Mock<IModerationService>();
        var b = new ChannelModerationEffectsBuilder(new LoggerFactory().CreateLogger<ChannelModerationEffectsBuilder>(), bot.Object, ban.Object, moderation.Object);
        var effects = b.BuildChannelEffects(CreateMessage(), Res(ModerationAction.Delete));
        Assert.That(effects.Length, Is.EqualTo(1));
    }

    [Test]
    public void Ban_Returns_BanEffect()
    {
        var bot = new Mock<ITelegramBotClientWrapper>();
        var ban = new Mock<IUserBanService>();
        var moderation = new Mock<IModerationService>();
        var b = new ChannelModerationEffectsBuilder(new LoggerFactory().CreateLogger<ChannelModerationEffectsBuilder>(), bot.Object, ban.Object, moderation.Object);
        var effects = b.BuildChannelEffects(CreateMessage(), Res(ModerationAction.Ban));
        Assert.That(effects.Length, Is.EqualTo(1));
    }

    [Test]
    public void Report_Returns_ReportEffect()
    {
        var bot = new Mock<ITelegramBotClientWrapper>();
        var ban = new Mock<IUserBanService>();
        var moderation = new Mock<IModerationService>();
        var b = new ChannelModerationEffectsBuilder(new LoggerFactory().CreateLogger<ChannelModerationEffectsBuilder>(), bot.Object, ban.Object, moderation.Object);
        var effects = b.BuildChannelEffects(CreateMessage(), Res(ModerationAction.Report));
        Assert.That(effects.Length, Is.EqualTo(1));
    }
}
