using Microsoft.Extensions.DependencyInjection;

namespace ClubDoorman.Services.ClickHouse;

/// <summary>
/// DI registration helpers for ClickHouse ingestion components.
/// </summary>
public static class ClickHouseModule
{
    public static IServiceCollection AddClickHouseServices(this IServiceCollection services)
    {
        services.AddSingleton<ClickHouseIngestionService>();
        services.AddSingleton<IClickHouseMessageSink>(sp => sp.GetRequiredService<ClickHouseIngestionService>());
        services.AddSingleton<IClickHouseIngestionClient, ClickHouseIngestionClient>();
        services.AddSingleton<NullClickHouseMessageSink>(_ => NullClickHouseMessageSink.Instance);
        services.AddHostedService(sp => sp.GetRequiredService<ClickHouseIngestionService>());
        return services;
    }
}
