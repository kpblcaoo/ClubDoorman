namespace ClubDoorman.Services;

/// <summary>
/// Интерфейс для инжектируемой конфигурации приложения
/// Заменяет статические свойства Config.cs для лучшего тестирования
/// </summary>
public interface IAppConfig
{
    /// <summary>
    /// API токен OpenRouter для AI проверок
    /// </summary>
    string? OpenRouterApi { get; }
    
    /// <summary>
    /// Включена ли система обнаружения подозрительных пользователей
    /// </summary>
    bool SuspiciousDetectionEnabled { get; }
    
    /// <summary>
    /// Порог для классификации мимикрии (0.0 - 1.0)
    /// </summary>
    double MimicryThreshold { get; }
    
    /// <summary>
    /// Количество сообщений для перевода из подозрительных в одобренные
    /// </summary>
    int SuspiciousToApprovedMessageCount { get; }
    
    /// <summary>
    /// ID админского чата
    /// </summary>
    long AdminChatId { get; }
    
    /// <summary>
    /// Чат для логирования спама
    /// </summary>
    long LogAdminChatId { get; }
    
    /// <summary>
    /// API токен бота Telegram
    /// </summary>
    string BotApi { get; }
    
    /// <summary>
    /// Автоматически банить пользователей из черного списка
    /// </summary>
    bool BlacklistAutoBan { get; }
    
    /// <summary>
    /// Автоматически банить каналы
    /// </summary>
    bool ChannelAutoBan { get; }
    
    /// <summary>
    /// Автоматически банить пользователей с похожими именами
    /// </summary>
    bool LookAlikeAutoBan { get; }
    
    /// <summary>
    /// Пересылать сообщения с низкой уверенностью в ham
    /// </summary>
    bool LowConfidenceHamForward { get; }
    
    /// <summary>
    /// Включить кнопку одобрения
    /// </summary>
    bool ApproveButtonEnabled { get; }
    
    /// <summary>
    /// Автоматически банить пользователей с кнопками
    /// </summary>
    bool ButtonAutoBan { get; }
    
    /// <summary>
    /// Автоматически банить при высокой уверенности
    /// </summary>
    bool HighConfidenceAutoBan { get; }
    
    /// <summary>
    /// Глобальный режим одобрения
    /// </summary>
    bool GlobalApprovalMode { get; }
    
    /// <summary>
    /// Отключить фильтрацию медиа
    /// </summary>
    bool DisableMediaFiltering { get; }
    
    /// <summary>
    /// Удалять пересланные сообщения
    /// </summary>
    bool DeleteForwardedMessages { get; }
    
    /// <summary>
    /// Отключенные чаты
    /// </summary>
    HashSet<long> DisabledChats { get; }
    
    /// <summary>
    /// Whitelist групп
    /// </summary>
    HashSet<long> WhitelistChats { get; }
    
    /// <summary>
    /// Группы без VPN-рекламы
    /// </summary>
    HashSet<long> NoVpnAdGroups { get; }
    
    /// <summary>
    /// Группы с отключенной капчей
    /// </summary>
    HashSet<long> NoCaptchaGroups { get; }
    
    /// <summary>
    /// AI-включенные чаты
    /// </summary>
    HashSet<long> AiEnabledChats { get; }
    
    /// <summary>
    /// Чаты с отключенной фильтрацией медиа
    /// </summary>
    HashSet<long> MediaFilteringDisabledChats { get; }
    
    /// <summary>
    /// Проверить, разрешен ли чат
    /// </summary>
    bool IsChatAllowed(long chatId);
    
    /// <summary>
    /// Проверить, разрешен ли приватный старт
    /// </summary>
    bool IsPrivateStartAllowed();
    
    /// <summary>
    /// Проверить, включен ли AI для чата
    /// </summary>
    bool IsAiEnabledForChat(long chatId);
    
    /// <summary>
    /// Проверить, отключена ли фильтрация медиа для чата
    /// </summary>
    bool IsMediaFilteringDisabledForChat(long chatId);
} 