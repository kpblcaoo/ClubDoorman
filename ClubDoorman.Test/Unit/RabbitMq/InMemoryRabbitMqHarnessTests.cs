using System;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Dispatcher;
using ClubDoorman.Services.RabbitMq;
using ClubDoorman.TestInfrastructure.RabbitMq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.RabbitMq;

[TestFixture]
public class InMemoryRabbitMqHarnessTests
{
    private static InMemoryRabbitMqHarness CreateHarness(bool enabled, Mock<IUpdateDispatcher>? dispatcherMock = null)
    {
        dispatcherMock ??= new Mock<IUpdateDispatcher>();
        var options = Options.Create(new RabbitMqOptions
        {
            Enabled = enabled
        });
        var logger = new Mock<ILogger<InMemoryRabbitMqHarness>>();
        return new InMemoryRabbitMqHarness(dispatcherMock.Object, options, logger.Object);
    }

    [Test]
    public async Task PublishAsync_Disabled_DoesNotEnqueue()
    {
        var dispatcherMock = new Mock<IUpdateDispatcher>(MockBehavior.Strict);
        var harness = CreateHarness(enabled: false, dispatcherMock);
        var context = new UpdatePublishContext(Guid.NewGuid(), null, DateTimeOffset.UtcNow);
        var update = new Update { Id = 42 };

        await harness.PublishAsync(update, context, CancellationToken.None);

        Assert.That(harness.Published, Has.Count.EqualTo(1));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        Assert.ThrowsAsync<OperationCanceledException>(async () => await harness.WaitForNextAsync(cts.Token));

        await harness.StopAsync(CancellationToken.None);
    }

    [Test]
    public async Task PublishAsync_Enabled_Dispatches()
    {
        var tcs = new TaskCompletionSource<Update>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatcherMock = new Mock<IUpdateDispatcher>();
        dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<Update>(), It.IsAny<CancellationToken>()))
            .Returns<Update, CancellationToken>((u, _) =>
            {
                tcs.TrySetResult(u);
                return Task.CompletedTask;
            });

    using var harness = CreateHarness(enabled: true, dispatcherMock);
        await harness.StartAsync(CancellationToken.None);

        var context = new UpdatePublishContext(Guid.NewGuid(), "gm-123", DateTimeOffset.UtcNow);
        var update = new Update { Id = 108, Message = new Message() };
        await harness.PublishAsync(update, context, CancellationToken.None);

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var dispatchedUpdate = await Task.Run(() => tcs.Task, timeoutCts.Token);

        Assert.That(dispatchedUpdate.Id, Is.EqualTo(108));
        Assert.That(harness.Published, Has.Count.EqualTo(1));

        await harness.StopAsync(CancellationToken.None);
        dispatcherMock.Verify(x => x.DispatchAsync(update, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
