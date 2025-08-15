using Telegram.Bot.Types;

namespace ClubDoorman.Services.AI;

/// <summary>
/// Интерфейс сервиса каскадного AI анализа
/// </summary>
public interface IAiCascadeService
{
    /// <summary>
    /// Выполняет AI анализ профиля пользователя
    /// </summary>
    /// <param name="message">Сообщение</param>
    /// <param name="user">Пользователь</param>
    /// <param name="chat">Чат</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true если пользователь ограничен, false если безопасен</returns>
    Task<bool> PerformAiProfileAnalysisAsync(Message message, User user, Chat chat, CancellationToken cancellationToken);

    /// <summary>
    /// Обработка каскадного анализа ML -> AI для сообщений с низкой уверенностью ML
    /// </summary>
    /// <param name="message">Сообщение</param>
    /// <param name="user">Пользователь</param>
    /// <param name="mlScore">Скор ML анализа</param>
    /// <param name="isSilentMode">Тихий режим</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task HandleAiCascadeAnalysisAsync(Message message, User user, double mlScore, bool isSilentMode, CancellationToken cancellationToken);
}
