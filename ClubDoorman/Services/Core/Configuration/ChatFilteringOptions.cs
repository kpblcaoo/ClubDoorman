namespace ClubDoorman.Services.Core.Configuration;

/// <summary>
/// Опции для фильтрации чатов
/// </summary>
public class ChatFilteringOptions
{
    /// <summary>
    /// Группы где фильтрация медиа отключена (независимо от глобальной настройки)
    /// </summary>
    public HashSet<long> MediaFilteringDisabledChats { get; set; } = new();
}