using RabbitMQ.Client;

namespace ClubDoorman.Services.RabbitMq;

/// <summary>
/// Creates AMQP connections configured for the ClubDoorman ingestion pipeline.
/// </summary>
public interface IRabbitMqConnectionFactory
{
    IConnection CreateConnection();
}
