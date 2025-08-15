using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Tests.TestKit2.Fakes;

namespace ClubDoorman.Tests.TestKit2.Core;

/// <summary>
/// Билдер для создания TestApp с настраиваемыми сервисами
/// </summary>
public class TestAppBuilder
{
    private readonly ServiceCollection _services = new();
    private FakeTelegramBotClientWrapper? _fakeTelegramBotClientWrapper;
    private FakeUserBanService? _fakeUserBanService;
    private EffectsSink? _effectsSink;

    public TestAppBuilder()
    {
        // Регистрируем базовые фейки
        _fakeTelegramBotClientWrapper = new FakeTelegramBotClientWrapper();
        _fakeUserBanService = new FakeUserBanService();
        _effectsSink = new EffectsSink();

        _services.AddSingleton<ITelegramBotClientWrapper>(_fakeTelegramBotClientWrapper);
        _services.AddSingleton<IUserBanService>(_fakeUserBanService);
        _services.AddSingleton<IEffectsSink>(_effectsSink);

        // Логгер
        _services.AddLogging(builder => builder.AddConsole());
    }

    /// <summary>
    /// Добавить кастомный фейк для ITelegramBotClientWrapper
    /// </summary>
    public TestAppBuilder WithTelegramClient(FakeTelegramBotClientWrapper telegramClient)
    {
        _fakeTelegramBotClientWrapper = telegramClient;
        _services.AddSingleton<ITelegramBotClientWrapper>(telegramClient);
        return this;
    }

    /// <summary>
    /// Добавить кастомный фейк для IUserBanService
    /// </summary>
    public TestAppBuilder WithUserBanService(FakeUserBanService userBanService)
    {
        _fakeUserBanService = userBanService;
        _services.AddSingleton<IUserBanService>(userBanService);
        return this;
    }

    /// <summary>
    /// Добавить кастомный EffectsSink
    /// </summary>
    public TestAppBuilder WithEffectsSink(EffectsSink effectsSink)
    {
        _effectsSink = effectsSink;
        _services.AddSingleton<IEffectsSink>(effectsSink);
        return this;
    }

    /// <summary>
    /// Добавить дополнительный сервис
    /// </summary>
    public TestAppBuilder WithService<TService, TImplementation>(TImplementation implementation)
        where TService : class
        where TImplementation : class, TService
    {
        _services.AddSingleton<TService>(implementation);
        return this;
    }

    /// <summary>
    /// Добавить дополнительный сервис с интерфейсом
    /// </summary>
    public TestAppBuilder WithService<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _services.AddSingleton<TService, TImplementation>();
        return this;
    }

    /// <summary>
    /// Построить TestApp
    /// </summary>
    public TestApp Build()
    {
        var serviceProvider = _services.BuildServiceProvider();
        return new TestApp();
    }
}
