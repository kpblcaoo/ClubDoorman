using System;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Services.Core.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ClubDoorman.Services.RabbitMq;

/// <summary>
/// Base class for RabbitMQ consumers that deserialize envelopes and delegate handling downstream.
/// </summary>
public abstract class RabbitMqConsumerBackgroundService : BackgroundService
{
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly IRabbitMqEnvelopeSerializer _serializer;
    private readonly IOptions<RabbitMqOptions> _options;
    private readonly ILogger _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private CancellationToken _stoppingToken;
    private TaskCompletionSource<bool>? _completionSource;

    protected RabbitMqConsumerBackgroundService(
        IRabbitMqConnectionFactory connectionFactory,
        IRabbitMqEnvelopeSerializer serializer,
        IOptions<RabbitMqOptions> options,
        ILogger logger)
    {
        _connectionFactory = connectionFactory;
        _serializer = serializer;
        _options = options;
        _logger = logger;
    }

    protected RabbitMqOptions Options => _options.Value;

    protected virtual string QueueName => Options.InputQueue;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Options.Enabled)
        {
            _logger.LogInformation("RabbitMQ consumer {Consumer} disabled via configuration", GetType().Name);
            return Task.CompletedTask;
        }

        _stoppingToken = stoppingToken;
        _connection = _connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.BasicQos(0, Options.PrefetchCount, false);
        DeclareQueues(_channel);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnReceivedAsync;
        _channel.BasicConsume(QueueName, autoAck: false, consumer: consumer);

        _logger.LogInformation("RabbitMQ consumer {Consumer} started on queue {Queue}", GetType().Name, QueueName);
        _completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        stoppingToken.Register(static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true), _completionSource);
        return _completionSource.Task;
    }

    private void DeclareQueues(IModel channel)
    {
        channel.QueueDeclare(
            queue: Options.InputQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        if (!string.Equals(Options.DeadLetterQueue, Options.InputQueue, StringComparison.Ordinal))
        {
            channel.QueueDeclare(
                queue: Options.DeadLetterQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
        }
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel == null)
        {
            return;
        }

        try
        {
            var envelope = _serializer.Deserialize(args.Body.ToArray());
            await HandleMessageAsync(envelope, _stoppingToken);
            _channel.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("RabbitMQ consumer {Consumer} cancellation requested", GetType().Name);
            if (_channel.IsOpen)
            {
                _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ consumer {Consumer} failed to process message", GetType().Name);
            if (_channel.IsOpen)
            {
                _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
            }
        }
    }

    protected abstract Task HandleMessageAsync(RabbitMqUpdateEnvelope envelope, CancellationToken cancellationToken);

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _completionSource?.TrySetResult(true);
        try
        {
            _channel?.Close();
            _connection?.Close();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error while stopping RabbitMQ consumer {Consumer}", GetType().Name);
        }
        finally
        {
            _channel?.Dispose();
            _connection?.Dispose();
            _channel = null;
            _connection = null;
        }

        return base.StopAsync(cancellationToken);
    }
    
    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        _channel = null;
        _connection = null;
        base.Dispose();
    }
}
