namespace ClubDoorman.Services.Core.Configuration;

/// <summary>
/// Опции для настройки автоматических банов
/// </summary>
public class AutoBanOptions
{
    /// <summary>
    /// Автоматически банить пользователей из черного списка
    /// </summary>
    public bool BlacklistAutoBan { get; set; } = true;

    /// <summary>
    /// Автоматически банить каналы
    /// </summary>
    public bool ChannelAutoBan { get; set; } = true;

    /// <summary>
    /// Автоматически банить пользователей с похожими именами
    /// </summary>
    public bool LookAlikeAutoBan { get; set; } = true;

    /// <summary>
    /// Автоматически банить по кнопкам
    /// </summary>
    public bool ButtonAutoBan { get; set; } = true;

    /// <summary>
    /// Автоматически банить при высокой уверенности
    /// </summary>
    public bool HighConfidenceAutoBan { get; set; } = true;

    /// <summary>
    /// Автоматически банить пользователей, входящих через папки
    /// </summary>
    public bool BanFolderInviteUsers { get; set; } = false;
}