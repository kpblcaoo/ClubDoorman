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
    Boundary
}
