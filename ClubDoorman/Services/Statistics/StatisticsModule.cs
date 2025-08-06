using Microsoft.Extensions.DependencyInjection;

namespace ClubDoorman.Services.Statistics;

/// <summary>
/// Модуль для регистрации сервисов статистики
/// </summary>
public static class StatisticsModule
{
    /// <summary>
    /// Добавляет сервисы статистики в DI контейнер
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <returns>Коллекция сервисов для цепочки вызовов</returns>
    public static IServiceCollection AddStatisticsServices(this IServiceCollection services)
    {
        services.AddSingleton<IStatisticsService, StatisticsService>();
        services.AddSingleton<GlobalStatsManager>();
        
        return services;
    }
} 