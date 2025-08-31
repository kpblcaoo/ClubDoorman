using System;
using System.Linq;
using NUnit.Framework;
using ClubDoorman.Models.Logging;

namespace ClubDoorman.Test.Unit.Logging;

/// <summary>
/// Guard test: ensures RuleCode enum only grows by append and contains expected set.
/// If this fails, update Expected in a deliberate, reviewed change.
/// </summary>
[TestFixture]
public class RuleCodeWhitelistTests
{
    private static readonly string[] Expected = new[]
    {
        "Unknown",
        "StopWords",
        "Links",
        "TooManyEmojis",
        "Greeting",
        "Banlist",
        "MediaNoCaption",
        "MediaEarlyBlock",
        "Command",
        "ReplyLink",
        "MixedLinkStopWords",
        "EmojiEscalation",
        "BanEscalation",
        "Boundary",
        "Pass",
        "PrivateSkip",
        "NewMembers",
        "LeftMemberCleanup",
        "ChannelMessage",
        "SystemNoUser",
        "BotMessage",
        "CaptchaPending",
        "AlreadyApproved",
        "ClubMemberSkip",
        "AiProfileRestricted",
        "ModeratedGeneric",
        "CaptchaFail",
        "CaptchaSuccess",
        "ModeratedAllow",
        "ModeratedDelete",
        "ModeratedBan",
        "ModeratedReport"
    };

    [Test]
    public void RuleCode_List_Is_Stable_And_Appended()
    {
        var actual = Enum.GetNames(typeof(RuleCode));
        // 1. Exact sequence equality
        Assert.That(actual, Is.EqualTo(Expected), "RuleCode enum changed unexpectedly. If intentional, update Expected ordering (append only).");
        // 2. Ensure no reordering when new entries added at tail
        for (int i = 0; i < Expected.Length; i++)
        {
            Assert.That(actual[i], Is.EqualTo(Expected[i]), $"Mismatch at index {i}: expected {Expected[i]}, got {actual[i]}");
        }
    }
}
