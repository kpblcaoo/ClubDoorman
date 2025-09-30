using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Dispatcher;
using ClubDoorman.Services.RabbitMq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;

namespace ClubDoorman.TestInfrastructure.RabbitMq;

/// <summary>
/// In-memory implementation of the RabbitMQ transport used by integration tests.
/// Replaces both the publisher and consumer so queue-enabled flows can be exercised without a broker.
/// </summary>
public sealed class InMemoryRabbitMqHarness : IRabbitMqUpdatePublisher, IHostedService, IDisposable
{
    private readonly IUpdateDispatcher _dispatcher;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<InMemoryRabbitMqHarness> _logger;
    private readonly Channel<RabbitMqUpdateEnvelope> _channel;
    private readonly List<RabbitMqUpdateEnvelope> _published = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _processingTask;
    private bool _disposed;

    public InMemoryRabbitMqHarness(
        IUpdateDispatcher dispatcher,
        IOptions<RabbitMqOptions> options,
        ILogger<InMemoryRabbitMqHarness> logger)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _channel = Channel.CreateUnbounded<RabbitMqUpdateEnvelope>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
    }

    /// <summary>
    /// Captured envelopes in publish order for assertions in tests.
    /// </summary>
    public IReadOnlyList<RabbitMqUpdateEnvelope> Published => _published;

    /// <summary>
    /// Waits for the next envelope published into the in-memory queue.
    /// </summary>
    public ValueTask<RabbitMqUpdateEnvelope> WaitForNextAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }

    public async Task PublishAsync(Update update, UpdatePublishContext context, CancellationToken cancellationToken)
    {
        if (update == null) throw new ArgumentNullException(nameof(update));
        if (context == null) throw new ArgumentNullException(nameof(context));
        cancellationToken.ThrowIfCancellationRequested();

        var envelope = new RabbitMqUpdateEnvelope
        {
            OperationId = context.OperationId,
            GmCorrelation = context.GmCorrelation,
            CreatedAt = context.EnqueuedAt,
            Update = update
        };

        lock (_published)
        {
            _published.Add(envelope);
        }

        if (!_options.Enabled)
        {
            _logger.LogTrace("RabbitMQ disabled in harness; skipping queue enqueue for update {UpdateId}", update.Id);
            return;
        }

        _logger.LogTrace("Enqueuing update {UpdateId} into in-memory RabbitMQ harness", update.Id);
        await _channel.Writer.WriteAsync(envelope, cancellationToken);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("In-memory RabbitMQ harness start skipped (feature disabled).");
            return Task.CompletedTask;
        }

        _processingTask = Task.Run(() => ProcessQueueAsync(_cts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var envelope in _channel.Reader.ReadAllAsync(cancellationToken))
            {
                if (envelope.Update == null)
                {
                    _logger.LogWarning("Envelope {OperationId} contained null update; skipping.", envelope.OperationId);
                    continue;
                }

                try
                {
                    await _dispatcher.DispatchAsync(envelope.Update, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogTrace("Dispatch cancelled for operation {OperationId}", envelope.OperationId);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "In-memory harness failed to dispatch update {UpdateId}", envelope.Update.Id);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogTrace("In-memory RabbitMQ harness processing cancelled.");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        _cts.Cancel();
        _channel.Writer.TryComplete();

        if (_processingTask != null)
        {
            await Task.WhenAny(_processingTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _channel.Writer.TryComplete();
        _cts.Dispose();
    }
}
