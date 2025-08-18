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
        ("привет", RuleCode.Greeting)
    };

    public static RuleCode MapReason(string? reason, string? actionKind = null)
    {
        if (string.IsNullOrWhiteSpace(reason)) return RuleCode.Unknown;
        var norm = reason.Trim().ToLowerInvariant();
        foreach (var (needle, code) in Map)
        {
            if (norm.Contains(needle)) return code;
        }
        if (!string.IsNullOrEmpty(actionKind))
        {
            var ak = actionKind.ToLowerInvariant();
            if (ak.Contains("banlist")) return RuleCode.Banlist;
        }
        return RuleCode.Unknown;
    }
}
