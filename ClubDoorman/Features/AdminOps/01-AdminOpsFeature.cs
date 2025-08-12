using Microsoft.Extensions.DependencyInjection;

namespace ClubDoorman.Features.AdminOps;

/// <summary>
/// Регистрация зависимостей для фичи AdminOps
/// </summary>
public static class AdminOpsFeature
{
    /// <summary>
    /// Регистрирует все сервисы для админских операций
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    public static IServiceCollection AddAdminOpsFeature(this IServiceCollection services)
    {
        // Регистрируем все CommandHandler'ы как ICommandHandler
        services.AddSingleton<ICommandHandler, SpamCommandHandler>();
        services.AddSingleton<ICommandHandler, HamCommandHandler>();
        services.AddSingleton<ICommandHandler, CheckCommandHandler>();
        services.AddSingleton<ICommandHandler, StatsCommandHandler>();
        services.AddSingleton<ICommandHandler, SayCommandHandler>();
        services.AddSingleton<ICommandHandler, StatsAliasCommandHandler>();
        services.AddSingleton<ICommandHandler, SuspiciousCommandHandler>();
        services.AddSingleton<ICommandHandler, StartCommandHandler>();

        // Регистрируем дополнительные интерфейсы для обратной совместимости
        services.AddSingleton<IStartCommandHandler, StartCommandHandler>();
        services.AddSingleton<ISuspiciousCommandHandler, SuspiciousCommandHandler>();
        services.AddSingleton<StatsCommandHandler>(); // Для StatsAliasCommandHandler

        // Регистрируем CommandRouter
        services.AddSingleton<ICommandRouter, CommandRouter>();

        // Регистрируем CommandProcessingService для обратной совместимости
        services.AddSingleton<ICommandProcessingService, CommandProcessingService>();

        // Регистрируем фасад
        services.AddSingleton<IAdminOpsFacade, AdminOpsFacade>();

        return services;
    }
}
