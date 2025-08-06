using Microsoft.Extensions.DependencyInjection;
using ClubDoorman.Handlers;

namespace ClubDoorman.Services.Handlers;

/// <summary>
/// Модуль для регистрации Handlers сервисов
/// </summary>
public static class HandlersModule
{
    /// <summary>
    /// Добавляет Handlers сервисы в DI контейнер
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <returns>Коллекция сервисов для цепочки вызовов</returns>
    public static IServiceCollection AddHandlersServices(this IServiceCollection services)
    {
        services.AddSingleton<IUpdateDispatcher, UpdateDispatcher>();
        services.AddSingleton<IntroFlowService>();
        services.AddSingleton<IBotPermissionsService, BotPermissionsService>();
        services.AddSingleton<IUpdateHandler, MessageHandler>();
        services.AddSingleton<IMessageHandler, MessageHandler>();
        services.AddSingleton<MessageHandler>();
        services.AddSingleton<IUpdateHandler, CallbackQueryHandler>();
        services.AddSingleton<CallbackQueryHandler>();
        services.AddSingleton<IUpdateHandler, ChatMemberHandler>();
        services.AddSingleton<ChatMemberHandler>();
        
        return services;
    }
} 