using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scrutor;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.AI;
using ClubDoorman.Handlers;
using ClubDoorman.Infrastructure.Configuration;
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

namespace ClubDoorman.Tests.TestKit2;

public sealed class TestApp : IAsyncDisposable
{
    private readonly ServiceProvider _provider;

    public TestApp(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();

        // Mirror production DI from Program.cs
        services.AddConfigurationServices();
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
        services.AddTelegramServices();
        services.AddStatisticsServices();
        services.AddAIServices();
        services.AddUserManagementServices();
        services.AddMessagingServices();

        // Register test infrastructure
        services.AddSingleton<IEffectsSink, EffectsSink>();
        
        // Replace real Telegram client with fake
        services.Replace(ServiceDescriptor.Singleton<ITelegramBotClientWrapper, FakeTelegramClient>());

        // Decorate services with recording decorators
        services.Decorate<INotificationService, RecordingNotificationService>();
        services.Decorate<IUserBanService, RecordingUserBanService>();
        services.Decorate<IModerationService, RecordingModerationService>();
        services.Decorate<IAiCascadeService, RecordingAiCascadeService>();

        // Allow custom configuration
        configure?.Invoke(services);

        _provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }

    public MessageHandler Handler() => _provider.GetRequiredService<MessageHandler>();

    public T GetService<T>() where T : notnull => _provider.GetRequiredService<T>();

    public async ValueTask DisposeAsync()
    {
        await _provider.DisposeAsync();
    }
}
