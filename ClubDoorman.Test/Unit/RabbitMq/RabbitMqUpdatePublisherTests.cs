using System;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.RabbitMq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.RabbitMq;

[TestFixture]
public class RabbitMqUpdatePublisherTests
{
    private static RabbitMqOptions EnabledOptions => new()
    {
        Enabled = true,
        Uri = "amqp://guest:guest@localhost:5672/",
        InputQueue = "spampyre.pipeline.input",
        DeadLetterQueue = "spampyre.pipeline.dlq",
        PrefetchCount = 10,
        PublishTimeoutSeconds = 5
    };

    private static RabbitMqOptions DisabledOptions => new()
    {
        Enabled = false
    };

    [Test]
    public async Task PublishAsync_SkipsWhenDisabled()
    {
        var connectionFactory = new Mock<IRabbitMqConnectionFactory>(MockBehavior.Strict);
        var serializer = new Mock<IRabbitMqEnvelopeSerializer>(MockBehavior.Strict);
        var logger = new Mock<ILogger<RabbitMqUpdatePublisher>>();
        var publisher = new RabbitMqUpdatePublisher(connectionFactory.Object, serializer.Object, Options.Create(DisabledOptions), logger.Object);

        await publisher.PublishAsync(new Update { Id = 1 }, new UpdatePublishContext(Guid.NewGuid(), null, DateTimeOffset.UtcNow), CancellationToken.None);

        connectionFactory.VerifyNoOtherCalls();
    }

    [Test]
    public async Task PublishAsync_DeclaresQueueAndPublishes()
    {
        var options = EnabledOptions;

        var channel = new Mock<IModel>();
        channel.Setup(c => c.CreateBasicProperties()).Returns(new Mock<IBasicProperties>().Object);
        channel.Setup(c => c.WaitForConfirms(It.IsAny<TimeSpan>())).Returns(true);

        var connection = new Mock<IConnection>();
        connection.Setup(c => c.IsOpen).Returns(true);
        connection.Setup(c => c.CreateModel()).Returns(channel.Object);

        var connectionFactory = new Mock<IRabbitMqConnectionFactory>();
        connectionFactory.Setup(f => f.CreateConnection()).Returns(connection.Object);

        var serializer = new Mock<IRabbitMqEnvelopeSerializer>();
        serializer.Setup(s => s.Serialize(It.IsAny<RabbitMqUpdateEnvelope>())).Returns(new byte[] { 1, 2, 3 });

        var logger = new Mock<ILogger<RabbitMqUpdatePublisher>>();

        var publisher = new RabbitMqUpdatePublisher(connectionFactory.Object, serializer.Object, Options.Create(options), logger.Object);

        var context = new UpdatePublishContext(Guid.NewGuid(), "gm-1", DateTimeOffset.UtcNow);
        await publisher.PublishAsync(new Update { Id = 42 }, context, CancellationToken.None);

        connectionFactory.Verify(f => f.CreateConnection(), Times.Once);
        connection.Verify(c => c.CreateModel(), Times.Once);
    channel.Verify(c => c.QueueDeclare(options.InputQueue, true, false, false, null), Times.Once);
    channel.Verify(c => c.QueueDeclare(options.DeadLetterQueue, true, false, false, null), Times.Once);
        channel.Verify(c => c.ConfirmSelect(), Times.Once);
    channel.Verify(c => c.BasicPublish(string.Empty, options.InputQueue, false, It.IsAny<IBasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>()), Times.Once);
        channel.Verify(c => c.WaitForConfirms(It.IsAny<TimeSpan>()), Times.Once);
        channel.Verify(c => c.Dispose(), Times.Once);
    }

    [Test]
    public void PublishAsync_WaitForConfirmsTimeout_Throws()
    {
        var options = EnabledOptions;
        options.PublishTimeoutSeconds = 1;

        var channel = new Mock<IModel>();
        channel.Setup(c => c.CreateBasicProperties()).Returns(new Mock<IBasicProperties>().Object);
        channel.Setup(c => c.WaitForConfirms(It.IsAny<TimeSpan>())).Returns(false);

        var connection = new Mock<IConnection>();
        connection.Setup(c => c.IsOpen).Returns(true);
        connection.Setup(c => c.CreateModel()).Returns(channel.Object);

        var connectionFactory = new Mock<IRabbitMqConnectionFactory>();
        connectionFactory.Setup(f => f.CreateConnection()).Returns(connection.Object);

        var serializer = new Mock<IRabbitMqEnvelopeSerializer>();
        serializer.Setup(s => s.Serialize(It.IsAny<RabbitMqUpdateEnvelope>())).Returns(new byte[] { 1 });

        var logger = new Mock<ILogger<RabbitMqUpdatePublisher>>();

        var publisher = new RabbitMqUpdatePublisher(connectionFactory.Object, serializer.Object, Options.Create(options), logger.Object);

        var context = new UpdatePublishContext(Guid.NewGuid(), null, DateTimeOffset.UtcNow);

        Assert.ThrowsAsync<TimeoutException>(async () => await publisher.PublishAsync(new Update { Id = 1 }, context, CancellationToken.None));
    }
}
