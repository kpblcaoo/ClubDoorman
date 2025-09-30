using System;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Services.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Telegram.Bot.Types;

namespace ClubDoorman.Services.RabbitMq;

/// <summary>
/// Publishes Telegram updates into the configured RabbitMQ queue.
/// </summary>
public sealed class RabbitMqUpdatePublisher : IRabbitMqUpdatePublisher, IDisposable
{
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly IRabbitMqEnvelopeSerializer _serializer;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqUpdatePublisher> _logger;
    private readonly object _sync = new();
    private IConnection? _connection;
    private bool _queueDeclared;
    private bool _disposed;

    public RabbitMqUpdatePublisher(
        IRabbitMqConnectionFactory connectionFactory,
        IRabbitMqEnvelopeSerializer serializer,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqUpdatePublisher> logger)
    {
        _connectionFactory = connectionFactory;
        _serializer = serializer;
        _logger = logger;
        _options = options.Value;
    }

    public Task PublishAsync(Update update, UpdatePublishContext context, CancellationToken cancellationToken)
    {
        if (update == null) throw new ArgumentNullException(nameof(update));
        if (context == null) throw new ArgumentNullException(nameof(context));
        cancellationToken.ThrowIfCancellationRequested();

        if (!_options.Enabled)
        {
            _logger.LogTrace("RabbitMQ disabled; skipping publish for update {UpdateId}", update.Id);
            return Task.CompletedTask;
        }

        var envelope = new RabbitMqUpdateEnvelope
        {
            OperationId = context.OperationId,
            GmCorrelation = context.GmCorrelation,
            CreatedAt = context.EnqueuedAt,
            Update = update
        };

        var body = _serializer.Serialize(envelope);

        using var channel = GetConnection().CreateModel();
        EnsureQueue(channel);
        if (_options.PublishTimeoutSeconds > 0)
        {
            channel.ConfirmSelect();
        }

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = context.OperationId.ToString();
        properties.CorrelationId = context.GmCorrelation;
        properties.Timestamp = new AmqpTimestamp(context.EnqueuedAt.ToUnixTimeSeconds());
        properties.Type = "telegram.update";

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: _options.InputQueue,
            mandatory: false,
            basicProperties: properties,
            body: body);

        if (_options.PublishTimeoutSeconds > 0)
        {
            var timeout = TimeSpan.FromSeconds(_options.PublishTimeoutSeconds);
            if (!channel.WaitForConfirms(timeout))
            {
                _logger.LogWarning("RabbitMQ publish confirmation timed out for update {UpdateId} after {Timeout}s", update.Id, timeout.TotalSeconds);
                throw new TimeoutException($"RabbitMQ publish confirmation timed out after {timeout.TotalSeconds} seconds.");
            }
        }

        _logger.LogTrace("Published update {UpdateId} to RabbitMQ queue {Queue}", update.Id, _options.InputQueue);
        return Task.CompletedTask;
    }

    private IConnection GetConnection()
    {
        if (_connection != null && _connection.IsOpen)
        {
            return _connection;
        }

        lock (_sync)
        {
            if (_connection != null)
            {
                try
                {
                    if (_connection.IsOpen)
                    {
                        return _connection;
                    }
                    _connection.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while disposing closed RabbitMQ connection");
                }
            }

            _connection = _connectionFactory.CreateConnection();
            _queueDeclared = false;
            return _connection;
        }
    }

    private void EnsureQueue(IModel channel)
    {
        if (_queueDeclared) return;
        lock (_sync)
        {
            if (_queueDeclared) return;
            channel.QueueDeclare(
                queue: _options.InputQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            if (!string.Equals(_options.DeadLetterQueue, _options.InputQueue, StringComparison.Ordinal))
            {
                channel.QueueDeclare(
                    queue: _options.DeadLetterQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
            }
            _queueDeclared = true;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error while disposing RabbitMQ connection");
            }
        }
    }
}
