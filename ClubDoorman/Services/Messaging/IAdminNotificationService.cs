using Telegram.Bot.Types;
using ClubDoorman.Models.Notifications;

namespace ClubDoorman.Services.Messaging;

/// <summary>
/// Сервис для отправки уведомлений админам и управления сообщениями
/// </summary>
public interface IAdminNotificationService
{
    /// <summary>
    /// Удаляет сообщение и отправляет уведомление админам
    /// </summary>
    /// <param name="message">Сообщение для удаления</param>
    /// <param name="reason">Причина удаления</param>
    /// <param name="isSilentMode">Тихий режим</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task DeleteAndReportMessageAsync(Message message, string reason, bool isSilentMode, CancellationToken cancellationToken);

    /// <summary>
    /// Отправляет сообщение в ручную проверку без удаления
    /// </summary>
    /// <param name="message">Сообщение</param>
    /// <param name="user">Пользователь</param>
    /// <param name="isSilentMode">Тихий режим</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task DontDeleteButReportMessageAsync(Message message, User user, bool isSilentMode, CancellationToken cancellationToken);

    /// <summary>
    /// Отправляет подозрительное сообщение с кнопками
    /// </summary>
    /// <param name="message">Сообщение</param>
    /// <param name="user">Пользователь</param>
    /// <param name="data">Данные уведомления</param>
    /// <param name="isSilentMode">Тихий режим</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task SendSuspiciousMessageWithButtonsAsync(Message message, User user, SuspiciousMessageNotificationData data, bool isSilentMode, CancellationToken cancellationToken);

    /// <summary>
    /// Удаляет сообщение позже
    /// </summary>
    /// <param name="messageToDelete">Сообщение для удаления</param>
    /// <param name="delay">Задержка</param>
    /// <param name="cancellationToken">Токен отмены</param>
    void DeleteMessageLater(Message messageToDelete, TimeSpan delay, CancellationToken cancellationToken);
}