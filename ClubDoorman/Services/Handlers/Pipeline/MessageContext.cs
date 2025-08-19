using Telegram.Bot.Types;

namespace ClubDoorman.Services.Handlers.Pipeline;

/// <summary>
/// Упрощённый контекст, будет расширяться на миграционных шагах.
/// </summary>
public class MessageContext
{
    public required Update Update { get; init; }
    public required Message Message { get; init; }
    public Guid OperationId { get; init; } = Guid.NewGuid();
    public string? GmCorrelation { get; init; }
    public bool CommandHandled { get; set; }
    public bool NewMembersHandled { get; set; } // set by NewMembersStep when it stops pipeline
    public bool LeftMemberCleanupHandled { get; set; }
    public bool ChannelMessageHandled { get; set; }
    public bool PrivateSkipHandled { get; set; }
    // Место для будущих производных данных (normalized text, flags, decision...).
}
