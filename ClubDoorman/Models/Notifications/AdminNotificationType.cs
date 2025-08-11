namespace ClubDoorman.Models.Notifications;

/// <summary>
/// Типы уведомлений для администраторов
/// </summary>
public enum AdminNotificationType
{
    /// <summary>
    /// AI анализ профиля
    /// </summary>
    AiProfileAnalysis,
    
    /// <summary>
    /// AI обнаружение с автоудалением
    /// </summary>
    AiDetectAutoDelete,
    
    /// <summary>
    /// AI обнаружение подозрительного контента
    /// </summary>
    AiDetectSuspicious,
    
    /// <summary>
    /// Автоматический бан
    /// </summary>
    AutoBan,
    
    /// <summary>
    /// Автобан по чёрному списку
    /// </summary>
    AutoBanBlacklist,
    
    /// <summary>
    /// Автобан из чёрного списка
    /// </summary>
    AutoBanFromBlacklist,
    
    /// <summary>
    /// Бан канала
    /// </summary>
    BanChannel,
    
    /// <summary>
    /// Бан за длинное имя
    /// </summary>
    BanForLongName,
    
    /// <summary>
    /// Ошибка канала
    /// </summary>
    ChannelError,
    
    /// <summary>
    /// Сообщение канала
    /// </summary>
    ChannelMessage,
    
    /// <summary>
    /// Ошибка модерации
    /// </summary>
    ModerationError,
    
    /// <summary>
    /// Попытка бана в приватном чате
    /// </summary>
    PrivateChatBanAttempt,
    
    /// <summary>
    /// Удалён из одобренных
    /// </summary>
    RemovedFromApproved,
    
    /// <summary>
    /// Тихий режим
    /// </summary>
    SilentMode,
    
    /// <summary>
    /// Успех
    /// </summary>
    Success,
    
    /// <summary>
    /// Подозрительное сообщение
    /// </summary>
    SuspiciousMessage,
    
    /// <summary>
    /// Подозрительный пользователь
    /// </summary>
    SuspiciousUser,
    
    /// <summary>
    /// Системная ошибка
    /// </summary>
    SystemError,
    
    /// <summary>
    /// Системная информация
    /// </summary>
    SystemInfo,
    
    /// <summary>
    /// Пользователь одобрен
    /// </summary>
    UserApproved,
    
    /// <summary>
    /// Очистка пользователя
    /// </summary>
    UserCleanup,
    
    /// <summary>
    /// Пользователь удалён из одобренных
    /// </summary>
    UserRemovedFromApproved,
    
    /// <summary>
    /// Пользователь ограничен
    /// </summary>
    UserRestricted,
    
    /// <summary>
    /// Предупреждение
    /// </summary>
    Warning
}