using Microsoft.Extensions.DependencyInjection;

namespace ClubDoorman.Services.Messaging;

/// <summary>
/// Модуль для регистрации сервисов мессенджинга
/// </summary>
public static class MessagingModule
{
    /// <summary>
    /// Добавляет все сервисы мессенджинга в DI контейнер
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <returns>Коллекция сервисов с добавленными сервисами мессенджинга</returns>
    public static IServiceCollection AddMessagingServices(this IServiceCollection services)
    {
        // Централизованная система сообщений
        services.AddSingleton<MessageTemplates>();
        services.AddSingleton<ILoggingConfigurationService, LoggingConfigurationService>();
        services.AddSingleton<IServiceChatDispatcher, ServiceChatDispatcher>();
        services.AddSingleton<IMessageService, MessageService>();
        services.AddSingleton<IChatLinkFormatter, ChatLinkFormatter>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ILogChatService, LogChatService>();
        
        return services;
    }
} 