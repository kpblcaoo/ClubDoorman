namespace ClubDoorman.Services.RabbitMq;

/// <summary>
/// Metadata accompanying an update when it is sent to RabbitMQ.
/// </summary>
public sealed class UpdatePublishContext
{
    public UpdatePublishContext(Guid operationId, string? gmCorrelation, DateTimeOffset enqueuedAt)
    {
        OperationId = operationId;
        GmCorrelation = gmCorrelation;
        EnqueuedAt = enqueuedAt;
    }

    /// <summary>
    /// Correlates the update with the MessageHandler operation scope and logs.
    /// </summary>
    public Guid OperationId { get; }

    /// <summary>
    /// Golden Master correlation if recording is enabled.
    /// </summary>
    public string? GmCorrelation { get; }

    /// <summary>
    /// Timestamp used for queue latency calculations.
    /// </summary>
    public DateTimeOffset EnqueuedAt { get; }
}
