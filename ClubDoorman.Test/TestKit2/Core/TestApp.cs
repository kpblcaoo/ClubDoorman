using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Tests.TestKit2.Fakes;
using Microsoft.Extensions.Options;

namespace ClubDoorman.Tests.TestKit2.Core;

/// <summary>
/// Главный контейнер TestKit2 - только базовые фейки и DI
/// </summary>
public sealed class TestApp : IAsyncDisposable
{
    private readonly ServiceProvider _serviceProvider;
    
    // Основные фейки для удобного доступа
    public FakeTelegramBotClientWrapper TelegramClient { get; }
    public FakeUserBanService UserBanService { get; }
    public EffectsSink EffectsSink { get; }

    public TestApp()
    {
        var services = new ServiceCollection();
        
        // Базовые сервисы
        services.AddLogging(builder => builder.AddConsole());
        
        // Создаем базовые фейки
        TelegramClient = new FakeTelegramBotClientWrapper();
        UserBanService = new FakeUserBanService();
        EffectsSink = new EffectsSink();
        
        // Регистрируем базовые фейки
        services.AddSingleton<ITelegramBotClientWrapper>(TelegramClient);
        services.AddSingleton<IUserBanService>(UserBanService);
        services.AddSingleton<IEffectsSink>(EffectsSink);
        
        // Тестовая конфигурация
        services.AddSingleton<IAppConfig>(CreateTestConfig());
        
        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Получить сервис из DI контейнера
    /// </summary>
    public T GetService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();

    /// <summary>
    /// Зарегистрировать сервис
    /// </summary>
    public void RegisterService<T>(T implementation) where T : class
    {
        var services = new ServiceCollection();
        services.AddSingleton<T>(implementation);
        var newProvider = services.BuildServiceProvider();
        // TODO: Объединить с основным провайдером
    }

    /// <summary>
    /// Создать тестовую конфигурацию
    /// </summary>
    private static IAppConfig CreateTestConfig()
    {
        var config = new AppConfig(
            Microsoft.Extensions.Options.Options.Create(new AutoBanOptions()),
            Microsoft.Extensions.Options.Options.Create(new ViolationThresholdOptions()),
            Microsoft.Extensions.Options.Options.Create(new FeatureToggleOptions()),
            Microsoft.Extensions.Options.Options.Create(new ChatFilteringOptions())
        );
        
        return config;
    }

    public async ValueTask DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
    }
}
