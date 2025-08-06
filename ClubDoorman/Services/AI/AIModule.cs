using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Moderation;
using Microsoft.Extensions.DependencyInjection;

namespace ClubDoorman.Services.AI;

/// <summary>
/// Модуль для регистрации AI сервисов
/// </summary>
public static class AIModule
{
    /// <summary>
    /// Добавляет AI сервисы в DI контейнер
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <returns>Коллекция сервисов для цепочки вызовов</returns>
    public static IServiceCollection AddAIServices(this IServiceCollection services)
    {
        services.AddSingleton<IAiChecks, AiChecks>();
        services.AddSingleton<ISpamHamClassifier, SpamHamClassifier>();
        services.AddSingleton<IMimicryClassifier, MimicryClassifier>();
        services.AddSingleton<IBadMessageManager, BadMessageManager>();
        services.AddSingleton<ISuspiciousUsersStorage, SuspiciousUsersStorage>();
        
        return services;
    }
} 