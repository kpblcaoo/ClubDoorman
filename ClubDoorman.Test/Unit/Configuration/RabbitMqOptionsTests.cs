using System;
using ClubDoorman.Services.Core.Configuration;
using NUnit.Framework;

namespace ClubDoorman.Test.Unit.Configuration;

[TestFixture]
public class RabbitMqOptionsTests
{
    private static readonly string[] EnvKeys =
    [
        "DOORMAN_RABBITMQ__ENABLED",
        "DOORMAN_RABBITMQ__URI",
        "DOORMAN_RABBITMQ__INPUT_QUEUE",
        "DOORMAN_RABBITMQ__DLQ",
        "DOORMAN_RABBITMQ__PREFETCH",
        "DOORMAN_RABBITMQ__PUBLISH_TIMEOUT_SECONDS",
        "DOORMAN_RABBITMQ__EVENT_EXCHANGE"
    ];

    [TearDown]
    public void TearDown()
    {
        foreach (var key in EnvKeys)
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    [Test]
    public void LoadRabbitMqOptions_UsesDefaultsWhenEnvironmentMissing()
    {
        foreach (var key in EnvKeys)
        {
            Environment.SetEnvironmentVariable(key, null);
        }

        var options = ConfigurationHelper.LoadRabbitMqOptions();

        Assert.That(options.Enabled, Is.False);
        Assert.That(options.Uri, Is.Null);
        Assert.That(options.InputQueue, Is.EqualTo("spampyre.pipeline.input"));
        Assert.That(options.DeadLetterQueue, Is.EqualTo("spampyre.pipeline.dlq"));
        Assert.That(options.PrefetchCount, Is.EqualTo((ushort)50));
        Assert.That(options.PublishTimeoutSeconds, Is.EqualTo(5));
        Assert.That(options.EventExchange, Is.EqualTo("spampyre.events"));
    }

    [Test]
    public void LoadRabbitMqOptions_ReadsEnvironmentValues()
    {
        Environment.SetEnvironmentVariable("DOORMAN_RABBITMQ__ENABLED", "true");
        Environment.SetEnvironmentVariable("DOORMAN_RABBITMQ__URI", "amqps://user:pass@mq:5671/vhost");
        Environment.SetEnvironmentVariable("DOORMAN_RABBITMQ__INPUT_QUEUE", "custom.input");
        Environment.SetEnvironmentVariable("DOORMAN_RABBITMQ__DLQ", "custom.dlq");
        Environment.SetEnvironmentVariable("DOORMAN_RABBITMQ__PREFETCH", "200");
        Environment.SetEnvironmentVariable("DOORMAN_RABBITMQ__PUBLISH_TIMEOUT_SECONDS", "12");
        Environment.SetEnvironmentVariable("DOORMAN_RABBITMQ__EVENT_EXCHANGE", "custom.events");

        var options = ConfigurationHelper.LoadRabbitMqOptions();

        Assert.That(options.Enabled, Is.True);
        Assert.That(options.Uri, Is.EqualTo("amqps://user:pass@mq:5671/vhost"));
        Assert.That(options.InputQueue, Is.EqualTo("custom.input"));
        Assert.That(options.DeadLetterQueue, Is.EqualTo("custom.dlq"));
        Assert.That(options.PrefetchCount, Is.EqualTo((ushort)200));
        Assert.That(options.PublishTimeoutSeconds, Is.EqualTo(12));
        Assert.That(options.EventExchange, Is.EqualTo("custom.events"));
    }
}
