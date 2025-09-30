namespace ClubDoorman.Services.RabbitMq;

/// <summary>
/// Serializes and deserializes RabbitMQ update envelopes.
/// </summary>
public interface IRabbitMqEnvelopeSerializer
{
    byte[] Serialize(RabbitMqUpdateEnvelope envelope);
    RabbitMqUpdateEnvelope Deserialize(byte[] payload);
}
