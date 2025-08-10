using Microsoft.Extensions.DependencyInjection;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Messaging;

namespace ClubDoorman.Services.Commands;

/// <summary>
/// Модуль для регистрации сервисов команд
/// </summary>
public static class CommandsModule
{
    /// <summary>
    /// Добавляет сервисы команд в DI контейнер
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <returns>Коллекция сервисов с добавленными сервисами команд</returns>
    public static IServiceCollection AddCommandsServices(this IServiceCollection services)
    {
        // Регистрируем CommandRouter
        services.AddSingleton<ICommandRouter, CommandRouter>();
        
        // Регистрируем старый CommandProcessingService для обратной совместимости
        services.AddSingleton<ICommandProcessingService, CommandProcessingService>();
        
        // Регистрируем все Command Handlers
        services.AddSingleton<ICommandHandler, StartCommandHandler>();
        services.AddSingleton<IStartCommandHandler, StartCommandHandler>();
        services.AddSingleton<ICommandHandler, SuspiciousCommandHandler>();
        services.AddSingleton<ISuspiciousCommandHandler, SuspiciousCommandHandler>();
        
        // Новые Command Handlers для админских команд
        services.AddSingleton<ICommandHandler, CheckCommandHandler>();
        services.AddSingleton<ICommandHandler, SpamCommandHandler>();
        services.AddSingleton<ICommandHandler, HamCommandHandler>();
        services.AddSingleton<ICommandHandler, StatsCommandHandler>();
        services.AddSingleton<ICommandHandler, StatsAliasCommandHandler>();
        services.AddSingleton<ICommandHandler, SayCommandHandler>();
        
        return services;
    }
} 