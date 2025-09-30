using ClubDoorman.Services.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;

namespace ClubDoorman.Services.RabbitMq;

/// <summary>
/// Placeholder publisher used until a real RabbitMQ client is wired in.
/// </summary>
public sealed class NullRabbitMqUpdatePublisher : IRabbitMqUpdatePublisher
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<NullRabbitMqUpdatePublisher> _logger;

    public NullRabbitMqUpdatePublisher(IOptions<RabbitMqOptions> options, ILogger<NullRabbitMqUpdatePublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
        if (_options.Enabled)
        {
            _logger.LogWarning("RabbitMQ ingestion is enabled but no concrete publisher is registered. Updates will continue inline.");
        }
    }

    public Task PublishAsync(Update update, UpdatePublishContext context, CancellationToken cancellationToken)
    {
        if (update == null) throw new ArgumentNullException(nameof(update));
        if (context == null) throw new ArgumentNullException(nameof(context));

        _logger.LogTrace("RabbitMQ disabled; skipping publish for update {UpdateId}.", update.Id);
        return Task.CompletedTask;
    }
}
