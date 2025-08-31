namespace ClubDoorman.Services.Telegram;

/// <summary>
/// Rich result for a delete message operation.
/// </summary>
public sealed record DeleteMessageResult(long ChatId, int MessageId, DeleteMessageOutcome Outcome, long DurationMs, string? Error, string? RawError)
{
    public bool Success => Outcome == DeleteMessageOutcome.Success || Outcome == DeleteMessageOutcome.NotFoundOrAlreadyDeleted;
    public override string ToString() => $"chat={ChatId} msg={MessageId} outcome={Outcome} durMs={DurationMs}" + (Error != null ? $" error={Error}" : string.Empty);
}
