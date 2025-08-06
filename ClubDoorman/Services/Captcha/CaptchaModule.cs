using Microsoft.Extensions.DependencyInjection;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Messaging;

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
        
        return services;
    }
} 