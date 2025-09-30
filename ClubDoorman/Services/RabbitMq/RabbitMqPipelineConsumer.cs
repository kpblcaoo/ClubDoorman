using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Dispatcher;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClubDoorman.Services.RabbitMq;

/// <summary>
/// Hosted service that consumes serialized Telegram updates from RabbitMQ and forwards them to the pipeline dispatcher.
/// </summary>
public sealed class RabbitMqPipelineConsumer : RabbitMqConsumerBackgroundService
{
    private readonly IUpdateDispatcher _dispatcher;
    private readonly ILogger<RabbitMqPipelineConsumer> _logger;

    public RabbitMqPipelineConsumer(
        IRabbitMqConnectionFactory connectionFactory,
        IRabbitMqEnvelopeSerializer serializer,
        IOptions<RabbitMqOptions> options,
        IUpdateDispatcher dispatcher,
        ILogger<RabbitMqPipelineConsumer> logger)
        : base(connectionFactory, serializer, options, logger)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task HandleMessageAsync(RabbitMqUpdateEnvelope envelope, CancellationToken cancellationToken)
    {
        if (envelope == null)
        {
            throw new ArgumentNullException(nameof(envelope));
        }

        if (envelope.Update == null)
        {
            _logger.LogWarning("RabbitMQ envelope {OperationId} contained no update payload; dropping message", envelope.OperationId);
            return;
        }

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["opId"] = envelope.OperationId,
            ["gmCorrelation"] = envelope.GmCorrelation,
            ["updateType"] = envelope.Update.Type.ToString(),
            ["chatId"] = envelope.Update.Message?.Chat.Id ?? envelope.Update.EditedMessage?.Chat.Id,
            ["userId"] = envelope.Update.Message?.From?.Id ?? envelope.Update.EditedMessage?.From?.Id
        });

        if (envelope.CreatedAt != default)
        {
            var latency = DateTimeOffset.UtcNow - envelope.CreatedAt;
            _logger.LogTrace("RabbitMQ pipeline latency {LatencyMs} ms for update {UpdateId}", latency.TotalMilliseconds, envelope.Update.Id);
        }

        await _dispatcher.DispatchAsync(envelope.Update, cancellationToken);
    }
}
