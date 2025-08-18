using ClubDoorman.Models.Logging;

namespace ClubDoorman.Services.Logging;

/// <summary>
/// Maps human reason strings (existing production texts) to stable RuleCode values.
/// Phase 1: heuristic substring mapping; can be tightened later.
/// </summary>
internal static class ReasonCodeMapper
{
    private static readonly (string needle, RuleCode code)[] Map =
    {
        ("ссылки запрещены", RuleCode.Links),
        ("многовато эмоджи", RuleCode.TooManyEmojis),
        ("медиа без подписи", RuleCode.MediaNoCaption),
        ("банлист", RuleCode.Banlist),
        ("забан", RuleCode.Banlist),
    ("привет", RuleCode.Greeting),
    ("стоп-слова", RuleCode.StopWords),
    ("первых трёх сообщениях нельзя отправлять", RuleCode.MediaEarlyBlock),
    ("команда обработана", RuleCode.Command),
    ("ответ с ссылкой", RuleCode.ReplyLink),
    ("ссылок https", RuleCode.Links),
    ("ссылки и стоп-слова", RuleCode.MixedLinkStopWords),
    ("лимит нарушений много эмодзи", RuleCode.EmojiEscalation),
    ("достигнут лимит нарушений", RuleCode.BanEscalation),
    ("прошло все проверки", RuleCode.Pass),
    };

    public static RuleCode MapReason(string? reason, string? actionKind = null)
    {
        string norm = string.Empty;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            norm = reason!.Trim().ToLowerInvariant();
        }
        else
        {
            // Try infer from actionKind even without textual reason
            if (!string.IsNullOrEmpty(actionKind))
            {
                var ak0 = actionKind.ToLowerInvariant();
                if (ak0.Contains("banlist")) return RuleCode.Banlist;
                if (ak0 == "command") return RuleCode.Command;
            }
        }
        foreach (var (needle, code) in Map)
        {
            if (norm.Contains(needle)) return code;
        }
        // Additional heuristics on kind
        if (!string.IsNullOrEmpty(actionKind))
        {
            var ak = actionKind.ToLowerInvariant();
            if (ak.Contains("banlist")) return RuleCode.Banlist;
            if (ak == "command") return RuleCode.Command;
            if (ak.Contains("ban") && norm.Contains("лимит")) return RuleCode.BanEscalation;
            if (ak.Contains("ban") && !string.IsNullOrEmpty(reason) && reason.Contains("банлист", StringComparison.OrdinalIgnoreCase)) return RuleCode.Banlist;
            if (ak.Contains("moderated") && norm.Contains("прошло все проверки")) return RuleCode.Pass;
        }
        if (!string.IsNullOrEmpty(actionKind))
        {
            var ak = actionKind.ToLowerInvariant();
            if (ak.Contains("banlist")) return RuleCode.Banlist;
            if (ak == "command") return RuleCode.Command;
        }
        return RuleCode.Unknown;
    }
}
