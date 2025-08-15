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

        return services;
    }
}