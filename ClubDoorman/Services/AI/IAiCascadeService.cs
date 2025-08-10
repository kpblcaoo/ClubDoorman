using Telegram.Bot.Types;

namespace ClubDoorman.Services.AI;

/// <summary>
/// Сервис для выполнения каскадного AI анализа сообщений
/// </summary>
public interface IAiCascadeService
{
    /// <summary>
    /// Выполняет AI анализ профиля пользователя при первом сообщении
    /// </summary>
    /// <param name="message">Сообщение</param>
    /// <param name="user">Пользователь</param>
    /// <param name="chat">Чат</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true если пользователь получил ограничения</returns>
    Task<bool> PerformAiProfileAnalysisAsync(Message message, User user, Chat chat, CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполняет каскадный AI анализ на основе ML оценки
    /// </summary>
    /// <param name="message">Сообщение</param>
    /// <param name="user">Пользователь</param>
    /// <param name="mlScore">Оценка ML классификатора</param>
    /// <param name="isSilentMode">Включен ли тихий режим</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task HandleAiCascadeAnalysisAsync(Message message, User user, double mlScore, bool isSilentMode, CancellationToken cancellationToken = default);
}