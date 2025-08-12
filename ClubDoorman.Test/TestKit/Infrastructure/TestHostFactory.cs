using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Core;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Test.TestKit.Fakes;
using ClubDoorman.Services.Dispatcher;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.TestKit.Infrastructure;

/// <summary>
/// Транскрипт тестового выполнения, содержащий все побочные эффекты
/// </summary>
public class TestTranscript
{
    public List<BotAction> BotActions { get; } = new();
    public List<string> LogMessages { get; } = new();
    public List<string> AdminNotifications { get; } = new();
    public List<string> ErrorMessages { get; } = new();
    
    /// <summary>
    /// Очистить транскрипт
    /// </summary>
    public void Clear()
    {
        BotActions.Clear();
        LogMessages.Clear();
        AdminNotifications.Clear();
        ErrorMessages.Clear();
    }

    /// <summary>
    /// Получить сводку транскрипта
    /// </summary>
    public string GetSummary()
    {
        return $"Bot Actions: {BotActions.Count}, Log Messages: {LogMessages.Count}, " +
               $"Admin Notifications: {AdminNotifications.Count}, Errors: {ErrorMessages.Count}";
    }
}

/// <summary>
/// Результат выполнения тестового сценария
/// </summary>
public class TestExecutionResult
{
    public TestTranscript Transcript { get; }
    public bool Success { get; }
    public Exception? Exception { get; }
    public TimeSpan ExecutionTime { get; }

    public TestExecutionResult(TestTranscript transcript, bool success, Exception? exception, TimeSpan executionTime)
    {
        Transcript = transcript;
        Success = success;
        Exception = exception;
        ExecutionTime = executionTime;
    }
}

/// <summary>
/// Фабрика для создания тестового хоста с поддержкой замены зависимостей фейками
/// </summary>
public class TestHostFactory
{
    private readonly ServiceCollection _services = new();
    private readonly Dictionary<Type, object> _fakeReplacements = new();
    private IServiceProvider? _serviceProvider;
    
    // Фейковые реализации
    public AppConfigFake AppConfig { get; private set; } = new();
    public BotClientFake BotClient { get; private set; } = new();
    public TimeProviderFake TimeProvider { get; private set; } = new();
    public RandomProviderFake RandomProvider { get; private set; } = new();
    public ApprovedUsersStorageFake ApprovedUsersStorage { get; private set; } = new();
    public SuspiciousUsersStorageFake SuspiciousUsersStorage { get; private set; } = new();

    /// <summary>
    /// Создать TestHostFactory с регистрацией всех необходимых сервисов
    /// </summary>
    public static TestHostFactory Create()
    {
        var factory = new TestHostFactory();
        factory.RegisterServices();
        return factory;
    }

    /// <summary>
    /// Регистрировать все сервисы как в основном приложении
    /// </summary>
    private void RegisterServices()
    {
        // Регистрируем все сервисы как в Program.cs
        _services.AddConfigurationServices();
        _services.AddLinkFormattingServices();
        _services.AddDispatcherServices();
        _services.AddUserJoinServices();
        _services.AddUserBanServices();
        _services.AddUserJoinFeature();
        _services.AddModerationFeature();
        _services.AddModerationServices();
        _services.AddChannelModerationServices();
        _services.AddSuspiciousUsersServices();
        _services.AddUserFlowServices();
        _services.AddViolationServices();
        _services.AddBadMessageServices();
        _services.AddTelegramServices();
        _services.AddStatisticsServices();
        _services.AddAIServices();
        _services.AddUserManagementServices();
        _services.AddMessagingServices();
        _services.AddTextProcessingServices();
        _services.AddCaptchaServices();
        _services.AddHandlersServices();
        _services.AddCommandsServices();

        // Регистрируем логгеры как null loggers для тестов
        _services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Сохраняем фейки для замены
        _fakeReplacements[typeof(IAppConfig)] = AppConfig;
        _fakeReplacements[typeof(ITelegramBotClientWrapper)] = BotClient;
        _fakeReplacements[typeof(ITimeProvider)] = TimeProvider;
        _fakeReplacements[typeof(IRandomProvider)] = RandomProvider;
        _fakeReplacements[typeof(IApprovedUsersStorage)] = ApprovedUsersStorage;
        _fakeReplacements[typeof(ISuspiciousUsersStorage)] = SuspiciousUsersStorage;
    }

    /// <summary>
    /// Заменить конкретную реализацию фейком
    /// </summary>
    public TestHostFactory Replace<TInterface, TImplementation>(TImplementation implementation)
        where TImplementation : class, TInterface
    {
        _fakeReplacements[typeof(TInterface)] = implementation;
        return this;
    }

    /// <summary>
    /// Настроить AppConfig
    /// </summary>
    public TestHostFactory ConfigureAppConfig(Action<AppConfigFake> configure)
    {
        configure(AppConfig);
        return this;
    }

    /// <summary>
    /// Настроить BotClient
    /// </summary>
    public TestHostFactory ConfigureBotClient(Action<BotClientFake> configure)
    {
        configure(BotClient);
        return this;
    }

    /// <summary>
    /// Настроить TimeProvider
    /// </summary>
    public TestHostFactory ConfigureTimeProvider(Action<TimeProviderFake> configure)
    {
        configure(TimeProvider);
        return this;
    }

    /// <summary>
    /// Построить ServiceProvider с заменами
    /// </summary>
    public IServiceProvider Build()
    {
        if (_serviceProvider != null)
            return _serviceProvider;

        // Заменяем реализации фейками
        foreach (var (interfaceType, implementation) in _fakeReplacements)
        {
            // Удаляем существующую регистрацию
            var existing = _services.FirstOrDefault(x => x.ServiceType == interfaceType);
            if (existing != null)
                _services.Remove(existing);

            // Добавляем фейковую реализацию
            _services.AddSingleton(interfaceType, implementation);
        }

        _serviceProvider = _services.BuildServiceProvider();
        return _serviceProvider;
    }

    /// <summary>
    /// Получить сервис из контейнера
    /// </summary>
    public T GetService<T>() where T : notnull
    {
        var provider = Build();
        return provider.GetRequiredService<T>();
    }

    /// <summary>
    /// Выполнить тестовый сценарий с Update
    /// </summary>
    public async Task<TestExecutionResult> ExecuteScenarioAsync(Update update)
    {
        var transcript = new TestTranscript();
        var startTime = DateTime.UtcNow;
        Exception? exception = null;
        bool success = false;

        try
        {
            // Очищаем транскрипты
            BotClient.ClearTranscript();
            
            // Получаем диспетчер и выполняем обновление
            var dispatcher = GetService<IUpdateDispatcher>();
            await dispatcher.DispatchAsync(update);
            
            // Собираем транскрипт
            transcript.BotActions.AddRange(BotClient.GetTranscript());
            
            success = true;
        }
        catch (Exception ex)
        {
            exception = ex;
            transcript.ErrorMessages.Add(ex.Message);
        }

        var executionTime = DateTime.UtcNow - startTime;
        return new TestExecutionResult(transcript, success, exception, executionTime);
    }

    /// <summary>
    /// Выполнить тестовый сценарий с конкретным сообщением
    /// </summary>
    public async Task<TestExecutionResult> ExecuteMessageScenarioAsync(Message message)
    {
        var update = new Update { Message = message };
        return await ExecuteScenarioAsync(update);
    }

    /// <summary>
    /// Выполнить тестовый сценарий с CallbackQuery
    /// </summary>
    public async Task<TestExecutionResult> ExecuteCallbackScenarioAsync(CallbackQuery callbackQuery)
    {
        var update = new Update { CallbackQuery = callbackQuery };
        return await ExecuteScenarioAsync(update);
    }

    /// <summary>
    /// Создать тестовый хост с предустановленными настройками для тестирования банлиста
    /// </summary>
    public static TestHostFactory CreateForBanListScenario()
    {
        return Create()
            .ConfigureAppConfig(config =>
            {
                config.WithAiEnabled(-1001234567890); // Тестовый чат
            });
    }

    /// <summary>
    /// Создать тестовый хост с предустановленными настройками для тестирования тихого режима
    /// </summary>
    public static TestHostFactory CreateForSilentModeScenario()
    {
        return Create()
            .ConfigureAppConfig(config =>
            {
                config.WithSilentMode(true);
            });
    }

    /// <summary>
    /// Создать тестовый хост с предустановленными настройками для тестирования новых пользователей
    /// </summary>
    public static TestHostFactory CreateForNewUserScenario()
    {
        return Create()
            .ConfigureAppConfig(config =>
            {
                // Настройки для тестирования новых пользователей
                config.SuspiciousDetectionEnabled = true;
                config.MimicryThreshold = 0.7;
            });
    }

    /// <summary>
    /// Освободить ресурсы
    /// </summary>
    public void Dispose()
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}