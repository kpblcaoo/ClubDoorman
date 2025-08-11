using Microsoft.Extensions.DependencyInjection;

namespace ClubDoorman.Features.UserJoin;

/// <summary>
/// Feature модуль для UserJoin функциональности
/// <tags>user-join, feature, di, registration</tags>
/// </summary>
public static class UserJoinFeature
{
    /// <summary>
    /// Регистрирует UserJoin сервисы в DI контейнере
    /// <tags>user-join, di, registration, services</tags>
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    public static IServiceCollection AddUserJoinFeature(this IServiceCollection services)
    {
        services.AddScoped<IUserJoinPolicy, UserJoinPolicy>();
        services.AddScoped<IUserJoinFacade, UserJoinFacade>();
        return services;
    }
}
