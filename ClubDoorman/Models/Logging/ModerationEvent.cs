namespace ClubDoorman.Models.Logging;

/// <summary>
/// Domain-level moderation event emitted by handlers. Golden Master and other consumers can project this form.
/// </summary>
public record ModerationEvent(
    string Kind,
    string? Action = null,
    RuleCode? RuleCode = null,
    int? Count = null,
    int? MessageId = null,
    string? Status = null,
    string? Extra = null
);
