using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClubDoorman.Services.Core.Configuration;

/// <summary>
/// Модуль для регистрации сервисов конфигурации
/// </summary>
public static class ConfigurationModule
{
    /// <summary>
    /// Добавляет сервисы конфигурации в DI контейнер
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <returns>Коллекция сервисов для цепочки вызовов</returns>
    public static IServiceCollection AddConfigurationServices(this IServiceCollection services)
    {
        // Регистрация основного интерфейса конфигурации
        services.AddSingleton<IAppConfig, AppConfig>();

        // Регистрация strongly-typed options из переменных окружения
        services.Configure<AutoBanOptions>(options =>
        {
            var loaded = ConfigurationHelper.LoadAutoBanOptions();
            options.BlacklistAutoBan = loaded.BlacklistAutoBan;
            options.ChannelAutoBan = loaded.ChannelAutoBan;
            options.LookAlikeAutoBan = loaded.LookAlikeAutoBan;
            options.ButtonAutoBan = loaded.ButtonAutoBan;
            options.HighConfidenceAutoBan = loaded.HighConfidenceAutoBan;
            options.BanFolderInviteUsers = loaded.BanFolderInviteUsers;
        });

        services.Configure<ViolationThresholdOptions>(options =>
        {
            var loaded = ConfigurationHelper.LoadViolationThresholdOptions();
            options.MlViolationsBeforeBan = loaded.MlViolationsBeforeBan;
            options.StopWordsViolationsBeforeBan = loaded.StopWordsViolationsBeforeBan;
            options.EmojiViolationsBeforeBan = loaded.EmojiViolationsBeforeBan;
            options.LookalikeViolationsBeforeBan = loaded.LookalikeViolationsBeforeBan;
            options.BoringGreetingsViolationsBeforeBan = loaded.BoringGreetingsViolationsBeforeBan;
            options.CaptchaViolationsBeforeBan = loaded.CaptchaViolationsBeforeBan;
        });

        services.Configure<FeatureToggleOptions>(options =>
        {
            var loaded = ConfigurationHelper.LoadFeatureToggleOptions();
            options.LowConfidenceHamForward = loaded.LowConfidenceHamForward;
            options.ApproveButtonEnabled = loaded.ApproveButtonEnabled;
            options.DeleteForwardedMessages = loaded.DeleteForwardedMessages;
            options.TextMentionFilterEnabled = loaded.TextMentionFilterEnabled;
            options.DisableWelcome = loaded.DisableWelcome;
            options.DisableMediaFiltering = loaded.DisableMediaFiltering;
            options.GlobalApprovalMode = loaded.GlobalApprovalMode;
            options.RepeatedViolationsBanToAdminChat = loaded.RepeatedViolationsBanToAdminChat;
        });

        services.Configure<ChatFilteringOptions>(options =>
        {
            var loaded = ConfigurationHelper.LoadChatFilteringOptions();
            options.MediaFilteringDisabledChats = loaded.MediaFilteringDisabledChats;
        });

        // Новые группы опций
        services.Configure<CoreOptions>(options =>
        {
            var loaded = ConfigurationHelper.LoadCoreOptions();
            options.BotApi = loaded.BotApi;
            options.AdminChatId = loaded.AdminChatId;
            options.LogAdminChatId = loaded.LogAdminChatId;
            options.ClubServiceToken = loaded.ClubServiceToken;
            options.ClubUrl = loaded.ClubUrl;
        });

        services.Configure<ChatAccessOptions>(options =>
        {
            var loaded = ConfigurationHelper.LoadChatAccessOptions();
            options.DisabledChats = loaded.DisabledChats;
            options.WhitelistChats = loaded.WhitelistChats;
            options.NoVpnAdGroups = loaded.NoVpnAdGroups;
            options.NoCaptchaGroups = loaded.NoCaptchaGroups;
        });

        services.Configure<AiOptions>(options =>
        {
            var loaded = ConfigurationHelper.LoadAiOptions();
            options.OpenRouterApi = loaded.OpenRouterApi;
            options.SuspiciousDetectionEnabled = loaded.SuspiciousDetectionEnabled;
            options.MimicryThreshold = loaded.MimicryThreshold;
            options.SuspiciousToApprovedMessageCount = loaded.SuspiciousToApprovedMessageCount;
            options.AiEnabledChats = loaded.AiEnabledChats;
        });

        services.Configure<TestHarnessOptions>(options =>
        {
            var loaded = ConfigurationHelper.LoadTestHarnessOptions();
            options.GoldenBaselineMode = loaded.GoldenBaselineMode;
            options.TestBlacklistUserIds = loaded.TestBlacklistUserIds;
        });

        return services;
    }
}