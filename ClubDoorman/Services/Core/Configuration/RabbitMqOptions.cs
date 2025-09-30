namespace ClubDoorman.Services.Core.Configuration;

/// <summary>
/// Strongly-typed settings for the RabbitMQ ingestion layer.
/// </summary>
public sealed class RabbitMqOptions
{
    /// <summary>
    /// Enables publishing Telegram updates into RabbitMQ instead of running the pipeline inline.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Connection URI in the form amqp[s]://user:pass@host:port/vhost.
    /// </summary>
    public string? Uri { get; set; }

    /// <summary>
    /// Queue that receives serialized Telegram updates before they hit the pipeline.
    /// </summary>
    public string InputQueue { get; set; } = "spampyre.pipeline.input";

    /// <summary>
    /// Queue for poisoned or repeatedly failed messages.
    /// </summary>
    public string DeadLetterQueue { get; set; } = "spampyre.pipeline.dlq";

    /// <summary>
    /// Prefetch limit for the consumer that forwards updates back to the pipeline.
    /// </summary>
    public ushort PrefetchCount { get; set; } = 50;

    /// <summary>
    /// Timeout in seconds for publish confirmations.
    /// </summary>
    public int PublishTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Exchange name used for future moderation events fan-out.
    /// </summary>
    public string EventExchange { get; set; } = "spampyre.events";
}
