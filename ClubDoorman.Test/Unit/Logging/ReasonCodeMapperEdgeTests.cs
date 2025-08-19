using NUnit.Framework;
using ClubDoorman.Services.Logging;
using ClubDoorman.Models.Logging;

namespace ClubDoorman.Test.Unit.Logging;

[TestFixture]
public class ReasonCodeMapperEdgeTests
{
    [TestCase("ПРОШЛО ВСЕ ПРОВЕРКИ", "moderated", RuleCode.ModeratedAllow)]
    [TestCase("прошло все проверки", "moderated", RuleCode.ModeratedAllow)]
    [TestCase("Забан за спам ссылки", "moderated", RuleCode.ModeratedBan)]
    [TestCase("забан за СПАМ ССЫЛКИ", "moderated", RuleCode.ModeratedBan)]
    [TestCase("репорт подозрение", "moderated", RuleCode.ModeratedReport)]
    [TestCase("удалено по подозрению на спам", "moderated", RuleCode.ModeratedDelete)]
    [TestCase("банлист нарушение", "moderated", RuleCode.Banlist)] // explicit banlist substring should override moderated-ban
    [TestCase("", "command", RuleCode.Command)]
    [TestCase(null, "command", RuleCode.Command)]
    [TestCase(null, "captcha_pending", RuleCode.CaptchaPending)]
    [TestCase(null, "ai_profile_restricted", RuleCode.AiProfileRestricted)]
    [TestCase("что-то неизвестное", "moderated", RuleCode.ModeratedGeneric)]
    public void Maps_As_Expected(string? reason, string? kind, RuleCode expected)
    {
        var actual = ReasonCodeMapper.MapReason(reason, kind);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void BanPhrase_With_Banlist_Wins()
    {
        var reason = "забан пользователь в банлист"; // contains both бан and банлист
        var code = ReasonCodeMapper.MapReason(reason, "moderated");
        Assert.That(code, Is.EqualTo(RuleCode.Banlist));
    }
}
