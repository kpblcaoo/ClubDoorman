using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Scrutor;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.AI;
using ClubDoorman.Handlers;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.LinkFormatting;
using ClubDoorman.Services.Dispatcher;
using ClubDoorman.Services.UserJoin;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Features.UserJoin;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.Violation;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Tests.TestKit2.Fakes;

namespace ClubDoorman.Tests.TestKit2;

public sealed class TestApp : IAsyncDisposable
{
    private readonly ServiceProvider _provider;

    public TestApp(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();

        // Add basic logging
        services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Add configuration
        services.AddConfigurationServices();
        
        // Add core services that are needed for basic functionality
        services.AddSingleton<IEffectsSink, EffectsSink>();
        
        // Add fake services
        services.AddSingleton<FakeTelegramBotClientWrapper>();
        services.AddSingleton<ITelegramBotClientWrapper>(provider => 
            provider.GetRequiredService<FakeTelegramBotClientWrapper>());
            
        services.AddSingleton<IClock, FakeClock>();
        services.AddSingleton<IGuidProvider, DeterministicGuidProvider>();
        services.AddSingleton<IRandom, SeededRandom>();
        services.AddSingleton<IAiCascadeService, FakeAiCascadeService>();
        services.AddSingleton<ISpamHamClassifier, FakeSpamHamClassifier>();
        services.AddSingleton<ICaptchaService, FakeCaptchaService>();
        services.AddSingleton<IModerationService, FakeModerationService>();
        services.AddSingleton<IUserManager, FakeUserManager>();
        services.AddSingleton<IBadMessageManager, FakeBadMessageManager>();
        services.AddSingleton<IAiChecks, FakeAiChecks>();
        services.AddSingleton<GlobalStatsManager>();
        services.AddSingleton<IStatisticsService, FakeStatisticsService>();
        services.AddSingleton<IUserFlowLogger, FakeUserFlowLogger>();
        services.AddSingleton<IMessageService, FakeMessageService>();
        services.AddSingleton<IChatLinkFormatter, FakeChatLinkFormatter>();
        
        // Add HTTP client with fake handler
        var fakeHttpHandler = new FakeHttpMessageHandler();
        services.AddSingleton(fakeHttpHandler);
        services.AddHttpClient("default", client => { })
            .ConfigurePrimaryHttpMessageHandler(() => fakeHttpHandler);

        // Add minimal services for MessageHandler
        services.AddSingleton<IModerationService>(provider =>
        {
            return new ModerationServiceAdapter(
                provider.GetRequiredService<IModerationPolicy>());
        });

        // Add basic handlers
        services.AddSingleton<MessageHandler>();

        // Allow custom configuration
        configure?.Invoke(services);

        _provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = false, // Disable validation for testing
            ValidateScopes = false
        });
    }

    public MessageHandler Handler() => _provider.GetRequiredService<MessageHandler>();

    public T GetService<T>() where T : notnull => _provider.GetRequiredService<T>();

    public async ValueTask DisposeAsync()
    {
        await _provider.DisposeAsync();
    }
}
