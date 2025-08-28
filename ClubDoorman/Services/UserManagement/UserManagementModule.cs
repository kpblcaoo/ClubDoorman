using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.Moderation;
using Microsoft.Extensions.DependencyInjection;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.Core;

namespace ClubDoorman.Services.UserManagement;

/// <summary>
/// Модуль для регистрации User Management сервисов
/// </summary>
public static class UserManagementModule
{
    /// <summary>
    /// Добавляет User Management сервисы в DI контейнер
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <returns>Коллекция сервисов для цепочки вызовов</returns>
    public static IServiceCollection AddUserManagementServices(this IServiceCollection services)
    {
        services.AddSingleton<IUserManager, UserManager>();
        services.AddSingleton<ApprovedUsersStorage>();
        services.AddSingleton<IUserCleanupService, UserCleanupService>();
        services.AddSingleton<IUserBanService, UserBanService>();
        services.AddSingleton<IUserFlowLogger, UserFlowLogger>();
        services.AddSingleton<IJoinedUserFlags, JoinedUserFlags>();
        services.AddSingleton<IUserIndex, UserIndex>();

        return services;
    }
}