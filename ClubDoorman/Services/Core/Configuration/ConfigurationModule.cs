using Microsoft.Extensions.DependencyInjection;

namespace ClubDoorman.Services.Core.Configuration;

/// <summary>
/// Модуль для регистрации сервисов конфигурации
/// </summary>
public static class ConfigurationModule
{
    /// <summary>
    /// Добавляет сервисы конфигурации в DI контейнер
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <returns>Коллекция сервисов для цепочки вызовов</returns>
    public static IServiceCollection AddConfigurationServices(this IServiceCollection services)
    {
        services.AddSingleton<IAppConfig, AppConfig>();
        services.AddSingleton<ILoggingFlagsConfig, LoggingFlagsConfig>();
        
        return services;
    }
} 