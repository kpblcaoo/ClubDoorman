using Serilog;
using Serilog.Events;
using ClubDoorman.Infrastructure;
using ClubDoorman.Services;
using ClubDoorman.Services.BanSystem;
using ClubDoorman.Handlers;
using ClubDoorman.Models.Logging;

using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.Commands;
using Telegram.Bot;
using DotNetEnv;
using ClubDoorman.Services.Messaging;

namespace ClubDoorman;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Загружаем переменные из .env файла если он существует
        var currentDir = Directory.GetCurrentDirectory();
        var envPath = Path.Combine(currentDir, ".env");
        
        if (File.Exists(envPath))
        {
            Console.WriteLine($"📄 Загружаем переменные из файла: {envPath}");
            Env.Load(envPath);
        }
        else
        {
            Console.WriteLine("📄 Файл .env не найден, используем переменные окружения");
            Console.WriteLine($"🔍 Искали в: {envPath}");
        }
        
        InitData();
        var host = Host.CreateDefaultBuilder(args)
            .UseSerilog(
                (_, _, config) =>
                {
                    // Создаем директорию для логов если её нет
                    var logsDir = "logs";
                    if (!Directory.Exists(logsDir))
                    {
                        Directory.CreateDirectory(logsDir);
                    }
                    
                    config
                        .MinimumLevel.Verbose()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                        .MinimumLevel.Override("System", LogEventLevel.Information)
                        .Enrich.FromLogContext()
                        .Enrich.WithProperty("Application", "ClubDoorman")
                        .WriteTo.Async(a => a.Console())
                        .WriteTo.Async(a => a.File(
                            path: Path.Combine(logsDir, "clubdoorman-.log"),
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 7,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                        ))
                        .WriteTo.Async(a => a.File(
                            path: Path.Combine(logsDir, "errors-.log"),
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 30,
                            restrictedToMinimumLevel: LogEventLevel.Error,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                        ))
                        .WriteTo.Async(a => a.File(
                            path: Path.Combine(logsDir, "system-.log"),
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 14,
                            restrictedToMinimumLevel: LogEventLevel.Information,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [System] {Message:lj}{NewLine}{Exception}"
                        ))
                        .WriteTo.Logger(lc => lc
                            .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("UserFlow"))
                            .WriteTo.Async(a => a.File(
                                path: Path.Combine(logsDir, "userflow-.log"),
                                rollingInterval: RollingInterval.Day,
                                retainedFileCountLimit: 7,
                                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [UserFlow] {Message:lj}{NewLine}{Exception}"
                            ))
                        );
                }
            )
            .ConfigureServices(services =>
            {
                // Регистрация конфигурации приложения
                services.AddConfigurationServices();

                // Telegram Bot Client - создаем после регистрации IAppConfig
                services.AddSingleton<TelegramBotClient>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("[DI] TelegramBotClient factory called");
                    var appConfig = provider.GetRequiredService<IAppConfig>();
                    logger.LogDebug("[DI] IAppConfig resolved: {AppConfigType}, BotApi: {BotApiPrefix}...", appConfig.GetType().Name, appConfig.BotApi != null ? appConfig.BotApi.Substring(0, Math.Min(appConfig.BotApi.Length, 10)) : "null");

                    // Проверяем конфигурацию бота
                    if (string.IsNullOrEmpty(appConfig.BotApi))
                    {
                        logger.LogError("[DI] DOORMAN_BOT_API is not set or is 'test-bot-token'.");
                        throw new InvalidOperationException(
                            "❌ Бот не может запуститься: DOORMAN_BOT_API не настроен или равен 'test-bot-token'. " +
                            "Установите переменную окружения DOORMAN_BOT_API с валидным токеном бота."
                        );
                    }

                    logger.LogDebug("[DI] 🤖 Starting bot with token: {BotApiPrefix}...", appConfig.BotApi.Substring(0, Math.Min(appConfig.BotApi.Length, 10)));

                    return new TelegramBotClient(appConfig.BotApi);
                });

                services.AddHostedService<Worker>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<Program>>();
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
                // Telegram Bot Client интерфейсы
                services.AddSingleton<ITelegramBotClient>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("[DI] ITelegramBotClient factory called");
                    return provider.GetRequiredService<TelegramBotClient>();
                });
                services.AddTelegramServices();
                services.AddStatisticsServices();
                services.AddAIServices();
                services.AddUserManagementServices();
                services.AddMessagingServices();
                
                // Классификаторы и менеджеры

                services.AddSingleton<IAiChecks>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("[DI] IAiChecks factory called");
                    return new AiChecks(
                        provider.GetRequiredService<ITelegramBotClientWrapper>(),
                        provider.GetRequiredService<ILogger<AiChecks>>(),
                        provider.GetRequiredService<IAppConfig>());
                });
                services.AddSingleton<GlobalStatsManager>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("[DI] GlobalStatsManager factory called");
                    return new GlobalStatsManager();
                });

                services.AddSingleton<IViolationTracker>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("[DI] IViolationTracker factory called");
                    return new ViolationTracker(provider.GetRequiredService<ILogger<ViolationTracker>>(), provider.GetRequiredService<IAppConfig>());
                });
                services.AddSingleton<IUserBanService>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("[DI] IUserBanService factory called");
                    return new UserBanService(
                        provider.GetRequiredService<ITelegramBotClientWrapper>(),
                        provider.GetRequiredService<IMessageService>(),
                        provider.GetRequiredService<IUserFlowLogger>(),
                        provider.GetRequiredService<ILogger<UserBanService>>(),
                        provider.GetRequiredService<IViolationTracker>(),
                        provider.GetRequiredService<IAppConfig>(),
                        provider.GetRequiredService<IStatisticsService>(),
                        provider.GetRequiredService<GlobalStatsManager>(),
                        provider.GetRequiredService<IUserManager>(),
                        provider.GetRequiredService<IUserCleanupService>()
                    );
                });
                

                
                // Новые сервисы
                services.AddSingleton<IUpdateDispatcher>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("[DI] IUpdateDispatcher factory called");
                    return new UpdateDispatcher(provider.GetServices<IUpdateHandler>(), provider.GetRequiredService<ILogger<UpdateDispatcher>>());
                });
                services.AddSingleton<IStatisticsService>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("[DI] IStatisticsService factory called");
                    return new StatisticsService(
                        provider.GetRequiredService<ITelegramBotClientWrapper>(),
                        provider.GetRequiredService<ILogger<StatisticsService>>(),
                        provider.GetRequiredService<IChatLinkFormatter>());
                });
                services.AddCaptchaServices();
                services.AddSingleton<IModerationService>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("[DI] IModerationService factory called");
                    return new ModerationService(
                        provider.GetRequiredService<ISpamHamClassifier>(),
                        provider.GetRequiredService<IMimicryClassifier>(),
                        provider.GetRequiredService<IBadMessageManager>(),
                        provider.GetRequiredService<IUserManager>(),
                        provider.GetRequiredService<IAiChecks>(),
                        provider.GetRequiredService<ISuspiciousUsersStorage>(),
                        provider.GetRequiredService<ITelegramBotClient>(),
                        provider.GetRequiredService<IMessageService>(),
                        provider.GetRequiredService<IUserBanService>(),
                        provider.GetRequiredService<IUserCleanupService>(),
                        provider.GetRequiredService<ILogger<ModerationService>>());
                });
                services.AddSingleton<IntroFlowService>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("[DI] IntroFlowService factory called");
                    return new IntroFlowService(
                        provider.GetRequiredService<ITelegramBotClientWrapper>(),
                        provider.GetRequiredService<ILogger<IntroFlowService>>(),
                        provider.GetRequiredService<ICaptchaService>(),
                        provider.GetRequiredService<IUserManager>(),
                        provider.GetRequiredService<IAiChecks>(),
                        provider.GetRequiredService<IStatisticsService>(),
                        provider.GetRequiredService<GlobalStatsManager>(),
                        provider.GetRequiredService<IModerationService>(),
                        provider.GetRequiredService<IMessageService>(),
                        provider.GetRequiredService<IUserBanService>(),
                        provider.GetRequiredService<IAppConfig>());
                });

                services.AddSingleton<IUserFlowLogger>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("[DI] IUserFlowLogger factory called");
                    return new UserFlowLogger(provider.GetRequiredService<ILogger<UserFlowLogger>>());
                });
                services.AddSingleton<IBotPermissionsService>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("[DI] IBotPermissionsService factory called");
                    return new BotPermissionsService(provider.GetRequiredService<ITelegramBotClientWrapper>(), provider.GetRequiredService<ILogger<BotPermissionsService>>());
                });

                // Централизованная система сообщений (перенесено в MessagingModule)
                services.Configure<LoggingConfiguration>(options => { });

                // Обработчики обновлений
                services.AddSingleton<IUpdateHandler>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("[DI] IUpdateHandler (MessageHandler) factory called");
                    return new MessageHandler(
                        provider.GetRequiredService<ITelegramBotClientWrapper>(),
                        provider.GetRequiredService<IModerationService>(),
                        provider.GetRequiredService<ICaptchaService>(),
                        provider.GetRequiredService<IUserManager>(),
                        provider.GetRequiredService<ISpamHamClassifier>(),
                        provider.GetRequiredService<IBadMessageManager>(),
                        provider.GetRequiredService<IAiChecks>(),
                        provider.GetRequiredService<GlobalStatsManager>(),
                        provider.GetRequiredService<IStatisticsService>(),
                        provider.GetRequiredService<IServiceProvider>(),
                        provider.GetRequiredService<IUserFlowLogger>(),
                        provider.GetRequiredService<IMessageService>(),
                        provider.GetRequiredService<IChatLinkFormatter>(),
                        provider.GetRequiredService<IBotPermissionsService>(),
                        provider.GetRequiredService<IAppConfig>(),
                        provider.GetRequiredService<IViolationTracker>(),
                        provider.GetRequiredService<ILogger<MessageHandler>>(),
                        provider.GetRequiredService<IUserBanService>());
                });
                services.AddSingleton<IUpdateHandler>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("[DI] IUpdateHandler (CallbackQueryHandler) factory called");
                    return new CallbackQueryHandler(
                        provider.GetRequiredService<ITelegramBotClientWrapper>(),
                        provider.GetRequiredService<ICaptchaService>(),
                        provider.GetRequiredService<IUserManager>(),
                        provider.GetRequiredService<IBadMessageManager>(),
                        provider.GetRequiredService<IStatisticsService>(),
                        provider.GetRequiredService<IAiChecks>(),
                        provider.GetRequiredService<IModerationService>(),
                        provider.GetRequiredService<IMessageService>(),
                        provider.GetRequiredService<IViolationTracker>(),
                        provider.GetRequiredService<IUserBanService>(),
                        provider.GetRequiredService<IServiceProvider>(),
                        provider.GetRequiredService<ILogger<CallbackQueryHandler>>());
                });
                services.AddSingleton<IUpdateHandler>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("[DI] IUpdateHandler (ChatMemberHandler) factory called");
                    return new ChatMemberHandler(
                        provider.GetRequiredService<ITelegramBotClientWrapper>(),
                        provider.GetRequiredService<IUserManager>(),
                        provider.GetRequiredService<ILogger<ChatMemberHandler>>(),
                        provider.GetRequiredService<IntroFlowService>(),
                        provider.GetRequiredService<IMessageService>(),
                        provider.GetRequiredService<IAppConfig>(),
                        provider.GetRequiredService<IUserCleanupService>());
                });

                // Новые прокси-сервисы для рефакторинга
                services.AddSingleton<IMessageHandler>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("[DI] IMessageHandler proxy factory called");
                    return provider.GetRequiredService<MessageHandler>();
                });
                services.AddSingleton<MessageHandler>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("[DI] MessageHandler factory called");
                    return new MessageHandler(
                        provider.GetRequiredService<ITelegramBotClientWrapper>(),
                        provider.GetRequiredService<IModerationService>(),
                        provider.GetRequiredService<ICaptchaService>(),
                        provider.GetRequiredService<IUserManager>(),
                        provider.GetRequiredService<ISpamHamClassifier>(),
                        provider.GetRequiredService<IBadMessageManager>(),
                        provider.GetRequiredService<IAiChecks>(),
                        provider.GetRequiredService<GlobalStatsManager>(),
                        provider.GetRequiredService<IStatisticsService>(),
                        provider.GetRequiredService<IServiceProvider>(),
                        provider.GetRequiredService<IUserFlowLogger>(),
                        provider.GetRequiredService<IMessageService>(),
                        provider.GetRequiredService<IChatLinkFormatter>(),
                        provider.GetRequiredService<IBotPermissionsService>(),
                        provider.GetRequiredService<IAppConfig>(),
                        provider.GetRequiredService<IViolationTracker>(),
                        provider.GetRequiredService<ILogger<MessageHandler>>(),
                        provider.GetRequiredService<IUserBanService>());
                });
                services.AddCommandsServices();
                        services.AddSingleton<IChannelModerationService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<Program>>();
            logger.LogDebug("[DI] IChannelModerationService factory called");
            return new ChannelModerationService(
                provider.GetRequiredService<ITelegramBotClientWrapper>(),
                provider.GetRequiredService<IModerationService>(),
                provider.GetRequiredService<IUserBanService>(),
                provider.GetRequiredService<ILogger<ChannelModerationService>>());
        });
                services.AddSingleton<IUserJoinService, UserJoinService>();

                // Регистрация сервиса лог-чата (перенесено в MessagingModule)

                // Логируем статус AI и Mimicry систем после полной инициализации
                services.PostConfigure<IAppConfig>(appConfig =>
                {
                    var serviceProvider = services.BuildServiceProvider();
                    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("[DI] PostConfigure: IAppConfig loaded. AI/Mimicry/Other system status will be logged here if needed.");
                });
            })
            .Build();

        // Логируем статус AI и Mimicry систем после полной инициализации
        var appConfig = host.Services.GetRequiredService<IAppConfig>();
        
        if (appConfig.OpenRouterApi != null)
        {
            Console.WriteLine("🤖 AI анализ: ВКЛЮЧЕН");
        }
        else
        {
            Console.WriteLine("🤖 AI анализ: ОТКЛЮЧЕН (DOORMAN_OPENROUTER_API не настроен)");
        }
        
        if (appConfig.SuspiciousDetectionEnabled)
        {
            Console.WriteLine($"🎭 Система мимикрии: ВКЛЮЧЕНА (порог: {appConfig.MimicryThreshold:F1})");
        }
        else
        {
            Console.WriteLine("🎭 Система мимикрии: ОТКЛЮЧЕНА (DOORMAN_SUSPICIOUS_DETECTION_ENABLE не установлен)");
        }
        
        // Информация о загруженных переменных окружения
        Console.WriteLine("📋 Загруженные переменные окружения:");
        Console.WriteLine($"   • DOORMAN_BOT_API: {(string.IsNullOrEmpty(appConfig.BotApi) ? "не найдено" : "найдено")}");
        Console.WriteLine($"   • DOORMAN_ADMIN_CHAT: {appConfig.AdminChatId}");
        Console.WriteLine($"   • DOORMAN_LOG_ADMIN_CHAT: {appConfig.LogAdminChatId}");
        Console.WriteLine($"   • DOORMAN_OPENROUTER_API: {(appConfig.OpenRouterApi != null ? "найдено" : "не найдено")}");
        Console.WriteLine($"   • DOORMAN_SUSPICIOUS_DETECTION_ENABLE: {appConfig.SuspiciousDetectionEnabled}");
        Console.WriteLine($"   • DOORMAN_MIMICRY_THRESHOLD: {appConfig.MimicryThreshold:F1}");
        Console.WriteLine($"   • DOORMAN_SUSPICIOUS_TO_APPROVED_COUNT: {appConfig.SuspiciousToApprovedMessageCount}");
        // Остальные свойства пока остаются в Config, будут перенесены в следующих группах
        Console.WriteLine($"   • DOORMAN_GLOBAL_APPROVAL_MODE: {Config.GlobalApprovalMode}");
        Console.WriteLine($"   • DOORMAN_BLACKLIST_AUTOBAN_DISABLE: {!Config.BlacklistAutoBan}");
        Console.WriteLine($"   • DOORMAN_CHANNELS_AUTOBAN_DISABLE: {!Config.ChannelAutoBan}");
        Console.WriteLine($"   • DOORMAN_BUTTON_AUTOBAN_DISABLE: {!Config.ButtonAutoBan}");
        Console.WriteLine($"   • DOORMAN_HIGH_CONFIDENCE_AUTOBAN_DISABLE: {!Config.HighConfidenceAutoBan}");
        Console.WriteLine($"   • DOORMAN_LOW_CONFIDENCE_HAM_ENABLE: {Config.LowConfidenceHamForward}");
        Console.WriteLine($"   • DOORMAN_APPROVE_BUTTON: {Config.ApproveButtonEnabled}");
        Console.WriteLine($"   • DOORMAN_DISABLE_MEDIA_FILTERING: {Config.DisableMediaFiltering}");
        Console.WriteLine($"   • DOORMAN_DELETE_FORWARDED_MESSAGES: {Config.DeleteForwardedMessages}");
        Console.WriteLine($"   • DOORMAN_PRIVATE_START_DISABLE: {!appConfig.IsPrivateStartAllowed()}");
        Console.WriteLine($"   • Отключенные чаты: {appConfig.DisabledChats.Count}");
        Console.WriteLine($"   • Белый список чатов: {appConfig.WhitelistChats.Count}");
        Console.WriteLine($"   • AI-включенные чаты: {appConfig.AiEnabledChats.Count}");
        Console.WriteLine($"   • Группы без VPN-рекламы: {appConfig.NoVpnAdGroups.Count}");
        Console.WriteLine($"   • Группы с отключенной капчей: {appConfig.NoCaptchaGroups.Count}");
        Console.WriteLine($"   • Чаты с отключенной фильтрацией медиа: {Config.MediaFilteringDisabledChats.Count}");

        await host.RunAsync();
    }

    private static void InitData()
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var dataInit = Path.Combine(basePath, "data_init");
        if (!Directory.Exists(dataInit))
            return;

        var data = Path.Combine(basePath, "data");
        if (!Directory.Exists(data))
            Directory.CreateDirectory(data);
        foreach (var sourceFullPath in Directory.EnumerateFiles(dataInit))
        {
            var file = Path.GetFileName(sourceFullPath);
            var target = Path.Combine(data, file);
            if (!File.Exists(target))
                File.Copy(sourceFullPath, target);
        }
    }
}
