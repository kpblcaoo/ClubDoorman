using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

namespace ClubDoorman.Services.Telegram;

/// <summary>
/// Модуль для регистрации сервисов Telegram
/// </summary>
public static class TelegramModule
{
    /// <summary>
    /// Добавляет сервисы Telegram в DI контейнер
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <returns>Коллекция сервисов для цепочки вызовов</returns>
    public static IServiceCollection AddTelegramServices(this IServiceCollection services)
    {
        services.AddSingleton<ITelegramBotClientWrapper, TelegramBotClientWrapper>();

        return services;
    }
}