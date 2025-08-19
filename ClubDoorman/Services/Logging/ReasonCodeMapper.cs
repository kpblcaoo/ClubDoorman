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
    // NOTE: previously mapped 'прошло все проверки' to Pass; removed so that in moderated context
    // it is classified via moderated precedence logic as ModeratedAllow. Other contexts without
    // 'moderated' kind will fall through and be inferred by actionKind if needed.
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
                if (ak0 == "private_skip") return RuleCode.PrivateSkip;
                if (ak0 == "new_members") return RuleCode.NewMembers;
                if (ak0 == "left_member_cleanup") return RuleCode.LeftMemberCleanup;
                if (ak0 == "channel_message") return RuleCode.ChannelMessage;
                if (ak0 == "system_no_user") return RuleCode.SystemNoUser;
                if (ak0 == "bot_message") return RuleCode.BotMessage;
                if (ak0 == "captcha_pending") return RuleCode.CaptchaPending;
                if (ak0 == "already_approved") return RuleCode.AlreadyApproved;
                if (ak0 == "club_member_skip") return RuleCode.ClubMemberSkip;
                if (ak0 == "ai_profile_restricted") return RuleCode.AiProfileRestricted;
                if (ak0 == "moderated") return RuleCode.ModeratedGeneric;
            }
        }

        // Moderated outcomes: give these precedence over generic substring map so that
        // текстовые причины ("прошло все проверки", "забан ...") мапятся в новые
        // специализированные RuleCode вместо Pass/Banlist.
        if (!string.IsNullOrEmpty(actionKind) && actionKind.Equals("moderated", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(norm))
            {
                // explicit banlist mention inside moderated context should always map to Banlist (override)
                if (norm.Contains("банлист"))
                    return RuleCode.Banlist;
                // allow
                if (norm.Contains("прошло все проверки") || norm.Contains("valid") || norm.Contains("ок"))
                    return RuleCode.ModeratedAllow;
                // delete
                if (norm.Contains("удал") || norm.Contains("delete"))
                    return RuleCode.ModeratedDelete;
                // ban (exclude explicit banlist reasons)
                if ((norm.Contains("бан") || norm.Contains("забан")) && !norm.Contains("банлист"))
                    return RuleCode.ModeratedBan;
                // report
                if (norm.Contains("репорт") || norm.Contains("жалоб"))
                    return RuleCode.ModeratedReport;
            }
            return RuleCode.ModeratedGeneric;
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
            if (ak == "private_skip") return RuleCode.PrivateSkip;
            if (ak == "new_members") return RuleCode.NewMembers;
            if (ak == "left_member_cleanup") return RuleCode.LeftMemberCleanup;
            if (ak == "channel_message") return RuleCode.ChannelMessage;
            if (ak == "system_no_user") return RuleCode.SystemNoUser;
            if (ak == "bot_message") return RuleCode.BotMessage;
            if (ak == "captcha_pending") return RuleCode.CaptchaPending;
            if (ak == "already_approved") return RuleCode.AlreadyApproved;
            if (ak == "club_member_skip") return RuleCode.ClubMemberSkip;
            if (ak == "ai_profile_restricted") return RuleCode.AiProfileRestricted;
            if (ak == "moderated")
            {
                // Attempt to refine generic moderated into specific outcome classes using textual hints.
                if (!string.IsNullOrEmpty(norm))
                {
                    if (norm.Contains("прошло все проверки") || norm.Contains("valid") || norm.Contains("ок")) return RuleCode.ModeratedAllow;
                    if (norm.Contains("удал") || norm.Contains("delete")) return RuleCode.ModeratedDelete;
                    if ((norm.Contains("бан") || norm.Contains("забан")) && !norm.Contains("банлист")) return RuleCode.ModeratedBan;
                    if (norm.Contains("репорт") || norm.Contains("жалоб")) return RuleCode.ModeratedReport;
                }
                return RuleCode.ModeratedGeneric;
            }
            if (ak.Contains("ban") && norm.Contains("лимит")) return RuleCode.BanEscalation;
            if (ak.Contains("ban") && !string.IsNullOrEmpty(reason) && reason.Contains("банлист", StringComparison.OrdinalIgnoreCase)) return RuleCode.Banlist;
            // removed legacy fallback mapping to Pass for 'прошло все проверки' under moderated kind
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
