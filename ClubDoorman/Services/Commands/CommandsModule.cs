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
        services.AddSingleton<ICommandProcessingService, CommandProcessingService>();
        services.AddSingleton<ICommandHandler, StartCommandHandler>();
        services.AddSingleton<IStartCommandHandler, StartCommandHandler>();
        services.AddSingleton<ICommandHandler, SuspiciousCommandHandler>();
        services.AddSingleton<ISuspiciousCommandHandler, SuspiciousCommandHandler>();
        
        return services;
    }
} 