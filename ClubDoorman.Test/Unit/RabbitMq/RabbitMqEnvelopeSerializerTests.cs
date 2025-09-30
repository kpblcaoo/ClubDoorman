using System;
using ClubDoorman.Services.RabbitMq;
using Newtonsoft.Json;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Unit.RabbitMq;

[TestFixture]
public class RabbitMqEnvelopeSerializerTests
{
    private readonly RabbitMqEnvelopeSerializer _serializer = new();

    [Test]
    public void SerializeDeserialize_RoundTripsEnvelope()
    {
        var now = DateTimeOffset.UtcNow;
        var update = new Update
        {
            Id = 42,
            Message = new Message
            {
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = -100123, Title = "Test Chat", Type = ChatType.Supergroup },
                From = new User { Id = 777, FirstName = "Tester", Username = "tester" },
                Text = "Hello from tests"
            }
        };

        var envelope = new RabbitMqUpdateEnvelope
        {
            OperationId = Guid.NewGuid(),
            GmCorrelation = "gm123",
            CreatedAt = now,
            Update = update
        };

        var payload = _serializer.Serialize(envelope);
        var restored = _serializer.Deserialize(payload);

        Assert.That(restored.OperationId, Is.EqualTo(envelope.OperationId));
        Assert.That(restored.GmCorrelation, Is.EqualTo("gm123"));
        Assert.That(restored.CreatedAt.ToUnixTimeSeconds(), Is.EqualTo(now.ToUnixTimeSeconds()));
        Assert.That(restored.Update.Id, Is.EqualTo(update.Id));
        Assert.That(restored.Update.Message!.Text, Is.EqualTo("Hello from tests"));
        Assert.That(restored.Update.Message!.Chat!.Title, Is.EqualTo("Test Chat"));
    }

    [Test]
    public void Deserialize_InvalidPayload_Throws()
    {
        Assert.Throws<ArgumentException>(() => _serializer.Deserialize(Array.Empty<byte>()));
        Assert.Throws<JsonReaderException>(() => _serializer.Deserialize("not json"u8.ToArray()));
    }
}
