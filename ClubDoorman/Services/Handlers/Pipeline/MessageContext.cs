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
    // Место для будущих производных данных (normalized text, flags, decision...).
}
