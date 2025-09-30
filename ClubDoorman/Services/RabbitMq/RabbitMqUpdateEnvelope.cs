using Telegram.Bot.Types;

namespace ClubDoorman.Services.RabbitMq;

/// <summary>
/// DTO that travels through RabbitMQ, wrapping a Telegram update with metadata.
/// </summary>
public sealed class RabbitMqUpdateEnvelope
{
    public Guid OperationId { get; init; }
    public string? GmCorrelation { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public Update Update { get; init; } = default!;
}
