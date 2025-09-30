using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Dispatcher;
using ClubDoorman.Services.RabbitMq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.RabbitMq;

[TestFixture]
public class RabbitMqPipelineConsumerTests
{
    private static MethodInfo GetHandleMethod()
    {
        return typeof(RabbitMqPipelineConsumer).GetMethod("HandleMessageAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
    }

    private static RabbitMqPipelineConsumer CreateConsumer(Mock<IUpdateDispatcher> dispatcherMock)
    {
        var connectionFactory = new Mock<IRabbitMqConnectionFactory>(MockBehavior.Strict);
        var serializer = new Mock<IRabbitMqEnvelopeSerializer>(MockBehavior.Strict);
        var options = Options.Create(new RabbitMqOptions
        {
            Enabled = true,
            Uri = "amqp://guest:guest@localhost:5672/"
        });
        var logger = new Mock<ILogger<RabbitMqPipelineConsumer>>();
        return new RabbitMqPipelineConsumer(connectionFactory.Object, serializer.Object, options, dispatcherMock.Object, logger.Object);
    }

    [Test]
    public async Task HandleMessageAsync_DispatchesUpdate()
    {
        var dispatcherMock = new Mock<IUpdateDispatcher>();
        dispatcherMock.Setup(x => x.DispatchAsync(It.IsAny<Update>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var consumer = CreateConsumer(dispatcherMock);
        var envelope = new RabbitMqUpdateEnvelope
        {
            OperationId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            Update = new Update { Id = 777, Message = new Message { Chat = new Chat { Id = -100 } } }
        };

        var handleMethod = GetHandleMethod();
        await (Task)handleMethod.Invoke(consumer, new object[] { envelope, CancellationToken.None })!;

        dispatcherMock.Verify(x => x.DispatchAsync(envelope.Update, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleMessageAsync_NullUpdate_SkipsDispatch()
    {
        var dispatcherMock = new Mock<IUpdateDispatcher>(MockBehavior.Strict);
        var consumer = CreateConsumer(dispatcherMock);
        var envelope = new RabbitMqUpdateEnvelope
        {
            OperationId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            Update = null
        };

        var handleMethod = GetHandleMethod();
        await (Task)handleMethod.Invoke(consumer, new object[] { envelope, CancellationToken.None })!;
    }

    [Test]
    public void HandleMessageAsync_NullEnvelope_Throws()
    {
        var dispatcherMock = new Mock<IUpdateDispatcher>();
        var consumer = CreateConsumer(dispatcherMock);
        var handleMethod = GetHandleMethod();

    var task = (Task)handleMethod.Invoke(consumer, new object?[] { null, CancellationToken.None })!;
    Assert.ThrowsAsync<ArgumentNullException>(async () => await task);
    }
}
