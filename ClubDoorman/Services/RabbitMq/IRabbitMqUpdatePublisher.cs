using Telegram.Bot.Types;

namespace ClubDoorman.Services.RabbitMq;

/// <summary>
/// Publishes Telegram updates into RabbitMQ for asynchronous pipeline processing.
/// </summary>
public interface IRabbitMqUpdatePublisher
{
    Task PublishAsync(Update update, UpdatePublishContext context, CancellationToken cancellationToken);
}
