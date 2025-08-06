using Telegram.Bot.Types;

namespace ClubDoorman.Services.Messaging;

/// <summary>
/// Сервис для работы с лог-чатом
/// </summary>
public interface ILogChatService
{
    /// <summary>
    /// Отправить уведомление в лог-чат с правильным форматированием
    /// </summary>
    Task SendLogNotificationAsync(Message message, string reason, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Обработать бан пользователя из лог-чата (без добавления в автобан)
    /// </summary>
    Task HandleLogBanAsync(long chatId, long userId, string adminName, CancellationToken cancellationToken = default);
} 