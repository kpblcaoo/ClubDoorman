using ClubDoorman.Infrastructure;

namespace ClubDoorman.Services;

/// <summary>
/// Реализация инжектируемой конфигурации приложения
/// Переносит логику из статического Config.cs для лучшего тестирования
/// </summary>
public class AppConfig : IAppConfig
{
    public string? OpenRouterApi { get; }
    public bool SuspiciousDetectionEnabled { get; }
    public double MimicryThreshold { get; }
    public int SuspiciousToApprovedMessageCount { get; }
    public long AdminChatId { get; }
    public long LogAdminChatId { get; }
    public string BotApi { get; }
    public bool BlacklistAutoBan { get; }
    public bool ChannelAutoBan { get; }
    public bool LookAlikeAutoBan { get; }
    public bool LowConfidenceHamForward { get; }
    public bool ApproveButtonEnabled { get; }
    public bool ButtonAutoBan { get; }
    public bool HighConfidenceAutoBan { get; }
    public bool GlobalApprovalMode { get; }
    public bool DisableMediaFiltering { get; }
    public bool DeleteForwardedMessages { get; }
    public HashSet<long> DisabledChats { get; }
    public HashSet<long> WhitelistChats { get; }
    public HashSet<long> NoVpnAdGroups { get; }
    public HashSet<long> NoCaptchaGroups { get; }
    public HashSet<long> AiEnabledChats { get; }
    public HashSet<long> MediaFilteringDisabledChats { get; }

    public AppConfig()
    {
        // Инициализируем все свойства из статического Config
        OpenRouterApi = Config.OpenRouterApi;
        SuspiciousDetectionEnabled = Config.SuspiciousDetectionEnabled;
        MimicryThreshold = Config.MimicryThreshold;
        SuspiciousToApprovedMessageCount = Config.SuspiciousToApprovedMessageCount;
        AdminChatId = Config.AdminChatId;
        LogAdminChatId = Config.LogAdminChatId;
        BotApi = Config.BotApi;
        BlacklistAutoBan = Config.BlacklistAutoBan;
        ChannelAutoBan = Config.ChannelAutoBan;
        LookAlikeAutoBan = Config.LookAlikeAutoBan;
        LowConfidenceHamForward = Config.LowConfidenceHamForward;
        ApproveButtonEnabled = Config.ApproveButtonEnabled;
        ButtonAutoBan = Config.ButtonAutoBan;
        HighConfidenceAutoBan = Config.HighConfidenceAutoBan;
        GlobalApprovalMode = Config.GlobalApprovalMode;
        DisableMediaFiltering = Config.DisableMediaFiltering;
        DeleteForwardedMessages = Config.DeleteForwardedMessages;
        DisabledChats = Config.DisabledChats;
        WhitelistChats = Config.WhitelistChats;
        NoVpnAdGroups = Config.NoVpnAdGroups;
        NoCaptchaGroups = Config.NoCaptchaGroups;
        AiEnabledChats = Config.AiEnabledChats;
        MediaFilteringDisabledChats = Config.MediaFilteringDisabledChats;
    }

    public bool IsChatAllowed(long chatId) => Config.IsChatAllowed(chatId);
    public bool IsPrivateStartAllowed() => Config.IsPrivateStartAllowed();
    public bool IsAiEnabledForChat(long chatId) => Config.IsAiEnabledForChat(chatId);
    public bool IsMediaFilteringDisabledForChat(long chatId) => Config.IsMediaFilteringDisabledForChat(chatId);
} 