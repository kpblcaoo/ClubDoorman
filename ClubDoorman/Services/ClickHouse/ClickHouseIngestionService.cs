using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClubDoorman.Services.ClickHouse;

/// <summary>
/// Background service responsible for batching rows and writing them into ClickHouse.
/// </summary>
public sealed class ClickHouseIngestionService : BackgroundService, IClickHouseMessageSink
{
    private readonly ClickHouseOptions _options;
    private readonly IClickHouseIngestionClient _client;
    private readonly ILogger<ClickHouseIngestionService> _logger;
    private readonly Channel<ClickHouseMessageRecord> _channel;
    private long _droppedRows;

    public ClickHouseIngestionService(IOptions<ClickHouseOptions> options, IClickHouseIngestionClient client, ILogger<ClickHouseIngestionService> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _options.Normalize();
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var channelCapacity = Math.Max(_options.ChannelCapacity, _options.BatchSize);
        _channel = Channel.CreateBounded<ClickHouseMessageRecord>(new BoundedChannelOptions(channelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ValueTask<bool> TryEnqueueAsync(ClickHouseMessageRecord record, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return ValueTask.FromResult(true);
        }

        if (_channel.Writer.TryWrite(record))
        {
            return ValueTask.FromResult(true);
        }

        _logger.LogWarning("ClickHouse ingestion queue is full. Dropping row for chat {ChatId}, message {MessageId}.", record.ChatId, record.MessageId);
        Interlocked.Increment(ref _droppedRows);
        return ValueTask.FromResult(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("ClickHouse ingestion disabled. Background worker is idle.");
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // shutdown
            }
            return;
        }

        try
        {
            await _client.PingAsync(stoppingToken).ConfigureAwait(false);
            _logger.LogInformation("ClickHouse endpoint reachable.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ClickHouse ping failed at startup. Writes will be retried.");
        }

        _logger.LogInformation("ClickHouse ingestion worker started. BatchSize={BatchSize}, FlushInterval={FlushInterval}ms", _options.BatchSize, _options.FlushIntervalMilliseconds);
        var buffer = new List<ClickHouseMessageRecord>(_options.BatchSize);
        var flushInterval = TimeSpan.FromMilliseconds(_options.FlushIntervalMilliseconds);
        var nextFlushAt = DateTime.UtcNow + flushInterval;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                while (_channel.Reader.TryRead(out var record))
                {
                    buffer.Add(record);
                    if (buffer.Count >= _options.BatchSize)
                    {
                        await FlushAsync(buffer, stoppingToken).ConfigureAwait(false);
                        nextFlushAt = DateTime.UtcNow + flushInterval;
                    }
                }

                var delay = nextFlushAt - DateTime.UtcNow;
                if (delay <= TimeSpan.Zero)
                {
                    if (buffer.Count > 0)
                    {
                        await FlushAsync(buffer, stoppingToken).ConfigureAwait(false);
                    }
                    nextFlushAt = DateTime.UtcNow + flushInterval;
                    continue;
                }

                var waitTask = _channel.Reader.WaitToReadAsync(stoppingToken).AsTask();
                var delayTask = Task.Delay(delay, stoppingToken);
                var completed = await Task.WhenAny(waitTask, delayTask).ConfigureAwait(false);

                if (completed == waitTask)
                {
                    var hasItems = await waitTask.ConfigureAwait(false);
                    if (!hasItems)
                    {
                        break; // channel completed
                    }
                }
                else
                {
                    if (buffer.Count > 0)
                    {
                        await FlushAsync(buffer, stoppingToken).ConfigureAwait(false);
                    }
                    nextFlushAt = DateTime.UtcNow + flushInterval;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ClickHouse ingestion worker encountered an unexpected error.");
        }
        finally
        {
            _channel.Writer.TryComplete();
            if (buffer.Count > 0)
            {
                try
                {
                    await FlushAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to flush remaining ClickHouse rows during shutdown.");
                }
            }
            _logger.LogInformation("ClickHouse ingestion worker stopped.");
        }
    }

    private async Task FlushAsync(List<ClickHouseMessageRecord> buffer, CancellationToken cancellationToken)
    {
        if (buffer.Count == 0)
        {
            return;
        }

        var attempt = 0;
        var snapshot = buffer.ToArray();
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _client.InsertAsync(snapshot, cancellationToken).ConfigureAwait(false);
                buffer.Clear();
                _logger.LogDebug("ClickHouse flush completed for {Count} rows.", snapshot.Length);
                var dropped = Interlocked.Exchange(ref _droppedRows, 0);
                if (dropped > 0)
                {
                    _logger.LogWarning("ClickHouse dropped {Dropped} rows due to full queue in the previous window.", dropped);
                }
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                attempt++;
                _logger.LogError(ex, "Failed to write {Count} rows to ClickHouse (attempt {Attempt}/{MaxAttempts}).", snapshot.Length, attempt, _options.MaxRetryAttempts);
                if (attempt >= _options.MaxRetryAttempts)
                {
                    buffer.Clear();
                    _logger.LogWarning("Giving up on {Count} ClickHouse rows after {Attempts} attempts.", snapshot.Length, attempt);
                    return;
                }

                var delaySeconds = Math.Min(_options.RetryDelaySeconds * attempt, 30);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
            }
        }
    }
}
