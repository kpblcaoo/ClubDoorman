using Microsoft.Extensions.DependencyInjection;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.LinkFormatting;

namespace ClubDoorman.Services.Captcha;

/// <summary>
/// Модуль для регистрации сервисов капчи
/// </summary>
public static class CaptchaModule
{
    /// <summary>
    /// Добавляет сервисы капчи в DI контейнер
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <returns>Коллекция сервисов с добавленными сервисами капчи</returns>
    public static IServiceCollection AddCaptchaServices(this IServiceCollection services)
    {
        // 1. Core captcha service
        services.AddSingleton<ICaptchaService, CaptchaService>();

        // 2. Low-level shared dependencies (state & trackers) — must be before services that consume them
        if (!services.Any(s => s.ServiceType == typeof(ApprovedUsersStorage)))
            services.AddSingleton<ApprovedUsersStorage>();

        if (!services.Any(s => s.ServiceType == typeof(IViolationTracker)))
            services.AddSingleton<IViolationTracker, ViolationTracker>();

        // 3. User management stack (UserManager uses ApprovedUsersStorage)
        if (!services.Any(s => s.ServiceType == typeof(IUserManager)))
            services.AddSingleton<IUserManager, UserManager>();

        if (!services.Any(s => s.ServiceType == typeof(IUserCleanupService)))
            services.AddSingleton<IUserCleanupService, UserCleanupService>();

        // 4. Logging / formatting helpers
        if (!services.Any(s => s.ServiceType == typeof(IUserFlowLogger)))
            services.AddSingleton<IUserFlowLogger, UserFlowLogger>();

        if (!services.Any(s => s.ServiceType == typeof(IChatLinkFormatter)))
            services.AddSingleton<IChatLinkFormatter, ChatLinkFormatter>();

        // 5. Statistics
        if (!services.Any(s => s.ServiceType == typeof(IStatisticsService)))
            services.AddSingleton<IStatisticsService, StatisticsService>();

        if (!services.Any(s => s.ServiceType == typeof(GlobalStatsManager)))
            services.AddSingleton<GlobalStatsManager>();

        // 6. High-level services depending on everything above
        if (!services.Any(s => s.ServiceType == typeof(IUserBanService)))
            services.AddSingleton<IUserBanService, UserBanService>();

        return services;
    }
}