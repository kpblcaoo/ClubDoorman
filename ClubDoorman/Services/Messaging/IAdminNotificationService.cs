using Telegram.Bot.Types;

namespace ClubDoorman.Services.Messaging;

/// <summary>
/// Сервис для отправки уведомлений администраторам
/// </summary>
public interface IAdminNotificationService
{
    /// <summary>
    /// Удаляет сообщение и отправляет уведомление в админ-чат
    /// </summary>
    /// <param name="message">Сообщение для удаления</param>
    /// <param name="reason">Причина удаления</param>
    /// <param name="isSilentMode">Включен ли тихий режим</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task DeleteAndReportMessageAsync(Message message, string reason, bool isSilentMode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отправляет сообщение в лог-чат без удаления
    /// </summary>
    /// <param name="message">Сообщение</param>
    /// <param name="user">Пользователь</param>
    /// <param name="isSilentMode">Включен ли тихий режим</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task DontDeleteButReportMessageAsync(Message message, User user, bool isSilentMode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет сообщение и отправляет только в лог-чат (без предупреждения пользователю)
    /// </summary>
    /// <param name="message">Сообщение</param>
    /// <param name="reason">Причина удаления</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task DeleteAndReportToLogChatAsync(Message message, string reason, CancellationToken cancellationToken = default);
}