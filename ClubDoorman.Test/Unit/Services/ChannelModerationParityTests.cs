using System.Linq;
using ClubDoorman.Effects;
using ClubDoorman.Effects.Channel;
using ClubDoorman.Models;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Services;

/// <summary>
/// Temporary parity tests asserting that the channel effects builder produces side-effects
/// identical in intent to the legacy switch logic inside ChannelModerationService.
/// Category "parity" so they can be easily located & removed after migration is finalized.
/// </summary>
[TestFixture]
public class ChannelModerationParityTests
{
    private static Message CreateMessage()
    {
        return new Message
        {
            Chat = new Chat { Id = -100, Title = "TestChat" },
            SenderChat = new Chat { Id = -200, Title = "SenderChannel" },
            Text = "spam?"
        };
    }

    [TestCase(ModerationAction.Allow,    typeof(ChannelAllowEffect),         TestName = "Parity-Allow-HasAllowEffect")]
    [TestCase(ModerationAction.Delete,   typeof(ChannelDeleteMessageEffect), TestName = "Parity-Delete-HasDeleteEffect")]
    [TestCase(ModerationAction.Ban,      typeof(ChannelBanEffect),           TestName = "Parity-Ban-HasBanEffect")]
    [TestCase(ModerationAction.Report,   typeof(ChannelReportMessageEffect), TestName = "Parity-Report-HasReportEffect")]
    [Category("parity")]
    public void ChannelEffectsBuilder_Maps_Actions_To_Expected_SideEffect(ModerationAction action, System.Type? expectedSideEffectType)
    {
        // Arrange
        var bot = new Mock<ITelegramBotClientWrapper>(MockBehavior.Loose); // needed for Delete effect
        var ban = new Mock<IUserBanService>(MockBehavior.Loose);            // needed for Ban effect
        var logger = new LoggerFactory().CreateLogger<ChannelModerationEffectsBuilder>();
        var builder = new ChannelModerationEffectsBuilder(logger, bot.Object, ban.Object);
        var msg = CreateMessage();
        var result = new ModerationResult(action, "reason");

        // Act
        var effects = builder.BuildChannelEffects(msg, result);

        // Assert
    Assert.That(effects.Length, Is.EqualTo(1), "Exactly one effect per action in legacy parity stage");
    Assert.That(effects[0].GetType(), Is.EqualTo(expectedSideEffectType));
    }
}
