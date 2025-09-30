using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Services.ClickHouse;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace ClubDoorman.Test.Unit.Services.ClickHouse;

[TestFixture]
public class ClickHouseIngestionServiceTests
{
    [Test]
    public async Task Enqueue_ReachesBatchSize_FlushesToClient()
    {
        var options = Options.Create(new ClickHouseOptions
        {
            Enabled = true,
            BatchSize = 2,
            ChannelCapacity = 10,
            FlushIntervalMilliseconds = 1_000,
            RetryDelaySeconds = 1,
            MaxRetryAttempts = 1,
            HttpTimeoutSeconds = 5
        });

        var fakeClient = new FakeClient();
        var service = new ClickHouseIngestionService(options, fakeClient, NullLogger<ClickHouseIngestionService>.Instance);

        await service.StartAsync(CancellationToken.None);

        var record1 = new ClickHouseMessageRecord(DateTime.UtcNow, DateTime.UtcNow, 1, "group", 1, 1, 0, 10, 0, 0, 0, "live");
        var record2 = new ClickHouseMessageRecord(DateTime.UtcNow, DateTime.UtcNow, 1, "group", 2, 1, 0, 5, 0, 0, 0, "live");

        var accepted1 = await service.TryEnqueueAsync(record1);
        var accepted2 = await service.TryEnqueueAsync(record2);

        Assert.That(accepted1, Is.True);
        Assert.That(accepted2, Is.True);

        var batch = await fakeClient.WaitForBatchAsync(TimeSpan.FromSeconds(2));
        Assert.That(batch.Count, Is.EqualTo(2));
        Assert.That(batch.Select(r => r.MessageId), Is.EquivalentTo(new[] { 1L, 2L }));

        await service.StopAsync(CancellationToken.None);
        service.Dispose();
    }

    [Test]
    public async Task TryEnqueue_Disabled_ReturnsTrue()
    {
        var options = Options.Create(new ClickHouseOptions { Enabled = false });
        var fakeClient = new FakeClient();
        var service = new ClickHouseIngestionService(options, fakeClient, NullLogger<ClickHouseIngestionService>.Instance);

        var result = await service.TryEnqueueAsync(new ClickHouseMessageRecord());

        Assert.That(result, Is.True);
        // no background start required; ensure insert never called
        Assert.That(fakeClient.Batches, Is.Empty);

        await service.StopAsync(CancellationToken.None);
        service.Dispose();
    }

    private sealed class FakeClient : IClickHouseIngestionClient
    {
        private readonly TaskCompletionSource<IReadOnlyList<ClickHouseMessageRecord>> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<IReadOnlyList<ClickHouseMessageRecord>> Batches { get; } = new();

        public Task InsertAsync(IReadOnlyList<ClickHouseMessageRecord> batch, CancellationToken cancellationToken)
        {
            Batches.Add(batch);
            _tcs.TrySetResult(batch);
            return Task.CompletedTask;
        }

        public Task PingAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task<IReadOnlyList<ClickHouseMessageRecord>> WaitForBatchAsync(TimeSpan timeout)
        {
            var completed = await Task.WhenAny(_tcs.Task, Task.Delay(timeout)).ConfigureAwait(false);
            if (completed != _tcs.Task)
            {
                throw new TimeoutException("ClickHouse batch was not delivered in time.");
            }

            return await _tcs.Task.ConfigureAwait(false);
        }
    }
}
