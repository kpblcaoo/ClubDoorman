using Microsoft.Extensions.DependencyInjection;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Messaging;

namespace ClubDoorman.Features.AdminOps;

/// <summary>
/// Модуль для регистрации сервисов команд
/// </summary>
public static class CommandsModule
{
    /// <summary>
    /// Добавляет сервисы команд в DI контейнер (для обратной совместимости)
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <returns>Коллекция сервисов с добавленными сервисами команд</returns>
    public static IServiceCollection AddCommandsServices(this IServiceCollection services)
    {
        // Используем новую структуру AdminOps
        return services.AddAdminOpsFeature();
    }
} 