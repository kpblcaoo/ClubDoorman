namespace ClubDoorman.Models.Logging;

/// <summary>
/// Stable semantic codes for moderation rules (Golden Master Phase 1).
/// Keep ordering stable; append new values at end to avoid churn.
/// </summary>
public enum RuleCode
{
    Unknown = 0,
    StopWords,
    Links,
    TooManyEmojis,
    Greeting,
    Banlist,
    MediaNoCaption,
    MediaEarlyBlock,
    Command,
    ReplyLink,
    MixedLinkStopWords,
    EmojiEscalation,
    BanEscalation,
    Boundary,
    Pass,
    // Added for early path semantics (golden hardening)
    PrivateSkip,
    NewMembers,
    LeftMemberCleanup,
    ChannelMessage,
    // Additional early / system semantics (Phase 2 hardening)
    SystemNoUser,
    BotMessage,
    CaptchaPending,
    AlreadyApproved,
    ClubMemberSkip,
    AiProfileRestricted,
    ModeratedGeneric,
    // Captcha lifecycle (future expansion)
    CaptchaFail,
    CaptchaSuccess,
    // Split moderated outcomes (appended at end for stability)
    ModeratedAllow,
    ModeratedDelete,
    ModeratedBan,
    ModeratedReport
}
