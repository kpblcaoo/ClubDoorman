using Microsoft.Extensions.DependencyInjection;

namespace ClubDoorman.Services.RabbitMq;

public static class RabbitMqModule
{
    public static IServiceCollection AddRabbitMqServices(this IServiceCollection services)
    {
        services.AddSingleton<IRabbitMqEnvelopeSerializer, RabbitMqEnvelopeSerializer>();
        services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
        services.AddSingleton<IRabbitMqUpdatePublisher, RabbitMqUpdatePublisher>();
        services.AddHostedService<RabbitMqPipelineConsumer>();
        return services;
    }
}
