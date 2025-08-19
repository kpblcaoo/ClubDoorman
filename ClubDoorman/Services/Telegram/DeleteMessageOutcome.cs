namespace ClubDoorman.Services.Telegram;

/// <summary>
/// Classification of Telegram message deletion attempt outcome.
/// </summary>
public enum DeleteMessageOutcome
{
    Success = 0,
    NotFoundOrAlreadyDeleted = 1,
    BadRequest = 2,
    Forbidden = 3,
    RateLimited = 4,
    UnexpectedError = 5
}
