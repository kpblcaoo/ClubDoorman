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
        services.AddSingleton<ICaptchaService, CaptchaService>();

        // Регистрируем зависимости, если они еще не зарегистрированы
        if (!services.Any(s => s.ServiceType == typeof(IViolationTracker)))
        {
            services.AddSingleton<IViolationTracker, ViolationTracker>();
        }

        if (!services.Any(s => s.ServiceType == typeof(IUserBanService)))
        {
            services.AddSingleton<IUserBanService, UserBanService>();
        }

        if (!services.Any(s => s.ServiceType == typeof(IUserFlowLogger)))
        {
            services.AddSingleton<IUserFlowLogger, UserFlowLogger>();
        }

        if (!services.Any(s => s.ServiceType == typeof(IStatisticsService)))
        {
            services.AddSingleton<IStatisticsService, StatisticsService>();
        }

        if (!services.Any(s => s.ServiceType == typeof(GlobalStatsManager)))
        {
            services.AddSingleton<GlobalStatsManager>();
        }

        if (!services.Any(s => s.ServiceType == typeof(IUserManager)))
        {
            services.AddSingleton<IUserManager, UserManager>();
        }

        if (!services.Any(s => s.ServiceType == typeof(IUserCleanupService)))
        {
            services.AddSingleton<IUserCleanupService, UserCleanupService>();
        }

        if (!services.Any(s => s.ServiceType == typeof(IChatLinkFormatter)))
        {
            services.AddSingleton<IChatLinkFormatter, ChatLinkFormatter>();
        }

        if (!services.Any(s => s.ServiceType == typeof(ApprovedUsersStorage)))
        {
            services.AddSingleton<ApprovedUsersStorage>();
        }

        return services;
    }
}