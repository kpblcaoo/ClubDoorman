using ClubDoorman.Services.Messaging;
namespace ClubDoorman.Models.Notifications;

/// <summary>
/// Типы уведомлений для лог-чата
/// </summary>
public enum LogNotificationType
{
    /// <summary>
    /// Автобан по блэклисту lols.bot
    /// </summary>
    AutoBanBlacklist,

    /// <summary>
    /// Автобан из блэклиста
    /// </summary>
    AutoBanFromBlacklist,

    /// <summary>
    /// Автобан за известное спам-сообщение
    /// </summary>
    AutoBanKnownSpam,

    /// <summary>
    /// Бан за длинное имя
    /// </summary>
    BanForLongName,

    /// <summary>
    /// Бан канала
    /// </summary>
    BanChannel,

    /// <summary>
    /// Подозрительный пользователь
    /// </summary>
    SuspiciousUser,

    /// <summary>
    /// AI анализ профиля
    /// </summary>
    AiProfileAnalysis,

    /// <summary>
    /// Критическая ошибка
    /// </summary>
    CriticalError,

    /// <summary>
    /// Сообщение от канала
    /// </summary>
    ChannelMessage,

    /// <summary>
    /// Удаление сообщения за ссылки
    /// </summary>
    AutoBanTextMention,

    /// <summary>
    /// Автобан за повторные нарушения
    /// </summary>
    AutoBanRepeatedViolations,

    /// <summary>
    /// Бан пользователя из блэклиста
    /// </summary>
    BanBlacklistedUser,

    /// <summary>
    /// Автоматический бан
    /// </summary>
    AutoBan,

    /// <summary>
    /// Ручной бан
    /// </summary>
    ManualBan,

    /// <summary>
    /// Бан по профилю
    /// </summary>
    ProfileBan,

    /// <summary>
    /// Бан канала
    /// </summary>
    ChannelBan,

    /// <summary>
    /// Бан за неудачную капчу
    /// </summary>
    CaptchaBan,

    /// <summary>
    /// Бан за повторное нарушение
    /// </summary>
    RepeatedViolation
}