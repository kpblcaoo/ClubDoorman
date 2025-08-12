using ClubDoorman.Services.Core.Configuration;

namespace ClubDoorman.Test.TestKit.Fakes;

/// <summary>
/// Фальшивая реализация IAppConfig для тестов
/// Позволяет управлять всеми конфигурационными флагами в тестах
/// </summary>
public class AppConfigFake : IAppConfig
{
    public string? OpenRouterApi { get; set; }
    public bool SuspiciousDetectionEnabled { get; set; }
    public double MimicryThreshold { get; set; } = 0.7;
    public int SuspiciousToApprovedMessageCount { get; set; } = 5;
    public long AdminChatId { get; set; } = 123456789;
    public long LogAdminChatId { get; set; } = 987654321;
    public HashSet<long> AiEnabledChats { get; set; } = new();
    public string BotApi { get; set; } = "fake:bot:token";
    public string? ClubServiceToken { get; set; }
    public string ClubUrl { get; set; } = "https://fake-club.example.com";
    public HashSet<long> DisabledChats { get; set; } = new();
    public HashSet<long> WhitelistChats { get; set; } = new();
    public HashSet<long> NoVpnAdGroups { get; set; } = new();
    public HashSet<long> NoCaptchaGroups { get; set; } = new();
    public bool TextMentionFilterEnabled { get; set; } = true;
    public bool BanFolderInviteUsers { get; set; } = false;
    public int MlViolationsBeforeBan { get; set; } = 3;
    public int StopWordsViolationsBeforeBan { get; set; } = 3;
    public int EmojiViolationsBeforeBan { get; set; } = 3;
    public int LookalikeViolationsBeforeBan { get; set; } = 3;
    public int BoringGreetingsViolationsBeforeBan { get; set; } = 3;
    public int CaptchaViolationsBeforeBan { get; set; } = 3;
    public bool RepeatedViolationsBanToAdminChat { get; set; } = false;

    /// <summary>
    /// Создает AppConfigFake с настройками по умолчанию для тестов
    /// </summary>
    public AppConfigFake()
    {
        // Настройки по умолчанию для тестов
        SuspiciousDetectionEnabled = true;
        MimicryThreshold = 0.7;
        SuspiciousToApprovedMessageCount = 5;
    }

    /// <summary>
    /// Создает AppConfigFake с кастомными настройками
    /// </summary>
    public static AppConfigFake Create(Action<AppConfigFake>? configure = null)
    {
        var config = new AppConfigFake();
        configure?.Invoke(config);
        return config;
    }

    // Методы интерфейса
    public bool IsAiEnabledForChat(long chatId) => AiEnabledChats.Contains(chatId);
    
    public bool IsChatAllowed(long chatId)
    {
        // Если есть whitelist, проверяем его
        if (WhitelistChats.Count > 0)
            return WhitelistChats.Contains(chatId);
        
        // Иначе проверяем, что чат не отключен
        return !DisabledChats.Contains(chatId);
    }
    
    public bool IsPrivateStartAllowed() => true; // По умолчанию разрешен в тестах

    // Методы для удобства настройки в тестах
    public AppConfigFake WithAiEnabled(long chatId)
    {
        AiEnabledChats.Add(chatId);
        return this;
    }

    public AppConfigFake WithChatDisabled(long chatId)
    {
        DisabledChats.Add(chatId);
        return this;
    }

    public AppConfigFake WithNoCaptcha(long chatId)
    {
        NoCaptchaGroups.Add(chatId);
        return this;
    }

    public AppConfigFake WithSilentMode(bool enabled = true)
    {
        // В будущем можно добавить флаг SilentMode если потребуется
        return this;
    }

    public AppConfigFake WithOpenRouterApi(string? apiKey)
    {
        OpenRouterApi = apiKey;
        return this;
    }

    public AppConfigFake WithMimicrySettings(bool enabled, double threshold = 0.7)
    {
        SuspiciousDetectionEnabled = enabled;
        MimicryThreshold = threshold;
        return this;
    }
}