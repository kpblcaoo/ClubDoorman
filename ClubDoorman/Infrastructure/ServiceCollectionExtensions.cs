using ClubDoorman.Features.AdminOps;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Features.UserJoin;
using ClubDoorman.Services;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Dispatcher;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Services.LinkFormatting;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.TextProcessing;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.UserJoin;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Violation;
using ClubDoorman.Models.Logging;
using ClubDoorman.Effects;
using ClubDoorman.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace ClubDoorman.Infrastructure;

/// <summary>
/// Расширения для IServiceCollection для централизованной регистрации всех сервисов ClubDoorman
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет все сервисы ClubDoorman в DI контейнер
    /// Заменяет длинную цепочку вызовов Add*Services в Program.cs
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <returns>Коллекция сервисов для цепочки вызовов</returns>
    public static IServiceCollection AddClubDoorman(this IServiceCollection services)
    {
        // Регистрация конфигурации приложения (должна быть первой)
        services.AddConfigurationServices();

        // Регистрация основных сервисов в том же порядке, что и в Program.cs
        services.AddLinkFormattingServices();
        services.AddDispatcherServices();
        services.AddUserJoinServices();
        services.AddUserBanServices();
        services.AddUserJoinFeature();
        services.AddModerationFeature();
        services.AddModerationServices();
        services.AddChannelModerationServices();
        services.AddSuspiciousUsersServices();
        services.AddUserFlowServices();
        services.AddViolationServices();
        services.AddBadMessageServices();

        // Telegram Bot Client - создаем после регистрации IAppConfig
        services.AddSingleton<TelegramBotClient>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<TelegramBotClient>>();
            logger.LogDebug("[DI] TelegramBotClient factory called");
            var appConfig = provider.GetRequiredService<IAppConfig>();
            logger.LogDebug("[DI] IAppConfig resolved: {AppConfigType}, BotApi: {BotApiPrefix}...",
                appConfig.GetType().Name,
                appConfig.BotApi != null ? appConfig.BotApi.Substring(0, Math.Min(appConfig.BotApi.Length, 10)) : "null");

            // Проверяем конфигурацию бота
            if (string.IsNullOrEmpty(appConfig.BotApi))
            {
                logger.LogError("[DI] DOORMAN_BOT_API is not set or is 'test-bot-token'.");
                throw new InvalidOperationException(
                    "❌ Бот не может запуститься: DOORMAN_BOT_API не настроен или равен 'test-bot-token'. " +
                    "Установите переменную окружения DOORMAN_BOT_API с валидным токеном бота."
                );
            }

            logger.LogDebug("[DI] 🤖 Starting bot with token: {BotApiPrefix}...",
                appConfig.BotApi.Substring(0, Math.Min(appConfig.BotApi.Length, 10)));

            return new TelegramBotClient(appConfig.BotApi);
        });

        // Telegram Bot Client интерфейсы
        services.AddSingleton<ITelegramBotClient>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<ITelegramBotClient>>();
            logger.LogDebug("[DI] ITelegramBotClient factory called");
            return provider.GetRequiredService<TelegramBotClient>();
        });

        services.AddTelegramServices();
        services.AddStatisticsServices();
        services.AddAIServices();
        services.AddUserManagementServices();
        services.AddMessagingServices();
        
        // Регистрация инфраструктуры эффектов
        services.AddSingleton<EffectsConfiguration>(provider => new EffectsConfiguration
        {
            UseRealEffects = true, // Включаем реальные эффекты
            EnabledActions = new[] { "Delete" }, // Включаем только Delete Action
            LegacyFallback = false, // ВРЕМЕННО отключаем fallback для тестирования Delete Action
            LogComparison = true // Включено сравнение логов
        });
        services.AddSingleton<IEffectBus, EffectBus>();
        services.AddSingleton<LoggingModerationEffectsBuilder>();
        services.AddSingleton<RealModerationEffectsBuilder>();
        services.AddSingleton<IModerationEffectsBuilder, HybridModerationEffectsBuilder>();
        services.AddTextProcessingServices();
        services.AddCaptchaServices();
        services.AddHandlersServices();
        services.AddCommandsServices();
        services.AddAdminOpsFeature();

        // Регистрация Worker как HostedService
        services.AddHostedService<Worker>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<Worker>>();
            logger.LogDebug("[DI] Worker factory called");
            return new Worker(
                provider.GetRequiredService<ILogger<Worker>>(),
                provider.GetRequiredService<IUpdateDispatcher>(),
                provider.GetRequiredService<ICaptchaService>(),
                provider.GetRequiredService<IStatisticsService>(),
                provider.GetRequiredService<ISpamHamClassifier>(),
                provider.GetRequiredService<IUserManager>(),
                provider.GetRequiredService<IBadMessageManager>(),
                provider.GetRequiredService<IAiChecks>(),
                provider.GetRequiredService<IChatLinkFormatter>(),
                provider.GetRequiredService<ITelegramBotClientWrapper>(),
                provider.GetRequiredService<IMessageService>(),
                provider.GetRequiredService<IAppConfig>(),
                provider.GetRequiredService<IUserBanService>()
            );
        });

        // Конфигурация логирования (централизованная система сообщений перенесена в MessagingModule)
        services.Configure<LoggingConfiguration>(options => { });

        return services;
    }
}