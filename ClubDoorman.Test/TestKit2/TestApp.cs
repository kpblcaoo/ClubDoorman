using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Tests.TestKit2.Fakes;
using ClubDoorman.Tests.TestKit2.Core;
using AutoFixture;

namespace ClubDoorman.Tests.TestKit2;

public sealed class TestApp : IDisposable, IAsyncDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly FakeTelegramBotClientWrapper? _fakeTelegramBotClientWrapper;
    private readonly FakeUserBanService? _fakeUserBanService;
    private readonly EffectsSink? _effectsSink;
    private readonly IFixture _fixture;

    // Конструктор для билдера
    internal TestApp(
        ServiceProvider serviceProvider,
        FakeTelegramBotClientWrapper? fakeTelegramBotClientWrapper,
        FakeUserBanService? fakeUserBanService,
        EffectsSink? effectsSink)
    {
        _serviceProvider = serviceProvider;
        _fakeTelegramBotClientWrapper = fakeTelegramBotClientWrapper;
        _fakeUserBanService = fakeUserBanService;
        _effectsSink = effectsSink;
        _fixture = new Fixture();
    }

    // Конструктор с AutoFixture
    public TestApp(IFixture fixture)
    {
        _fixture = fixture;
        var services = new ServiceCollection();

        // Регистрируем только базовые фейки
        _fakeTelegramBotClientWrapper = new FakeTelegramBotClientWrapper();
        _fakeUserBanService = new FakeUserBanService();
        _effectsSink = new EffectsSink();

        services.AddSingleton<ITelegramBotClientWrapper>(_fakeTelegramBotClientWrapper);
        services.AddSingleton<IUserBanService>(_fakeUserBanService);
        services.AddSingleton<IEffectsSink>(_effectsSink);

        // Логгер
        services.AddLogging(builder => builder.AddConsole());

        _serviceProvider = services.BuildServiceProvider();
    }

    // Старый конструктор для обратной совместимости
    public TestApp() : this(new Fixture())
    {
    }

    // Статический метод для создания с билдером
    public static TestAppBuilder CreateBuilder() => new TestAppBuilder();

    public FakeTelegramBotClientWrapper? TelegramClient => _fakeTelegramBotClientWrapper;
    public FakeUserBanService? UserBanService => _fakeUserBanService;
    public EffectsSink? EffectsSink => _effectsSink;

    // Совместимость со старыми тестами
    public T GetService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();

    /// <summary>
    /// Создать любой объект с автозависимостями
    /// </summary>
    public T Create<T>() => _fixture.Create<T>();

    /// <summary>
    /// Создать объект с кастомизацией
    /// </summary>
    public T CreateWith<T>(Action<T> customization) where T : class
    {
        var obj = Create<T>();
        customization(obj);
        return obj;
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
    }
}
