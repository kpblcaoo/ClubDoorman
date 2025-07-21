namespace ClubDoorman.Services;

/// <summary>
/// Интерфейс для локализации капчи
/// </summary>
public interface ICaptchaLocalizer
{
    /// <summary>
    /// Получить локализованное описание эмодзи капчи
    /// </summary>
    /// <param name="emojiIndex">Индекс эмодзи в списке</param>
    /// <param name="chatId">ID чата для определения культуры</param>
    /// <returns>Локализованное описание</returns>
    string GetEmojiDescription(int emojiIndex, long chatId);
    
    /// <summary>
    /// Получить локализованное сообщение капчи
    /// </summary>
    /// <param name="userMention">HTML-ссылка на пользователя</param>
    /// <param name="emojiDescription">Описание эмодзи</param>
    /// <param name="chatId">ID чата для определения культуры</param>
    /// <returns>Локализованное сообщение капчи</returns>
    string GetCaptchaMessage(string userMention, string emojiDescription, long chatId);
    
    /// <summary>
    /// Получить локализованный плейсхолдер для рекламы
    /// </summary>
    /// <param name="chatId">ID чата для определения культуры</param>
    /// <returns>Локализованный плейсхолдер</returns>
    string GetAdPlaceholder(long chatId);
    
    /// <summary>
    /// Получить локализованное название для нового участника
    /// </summary>
    /// <param name="chatId">ID чата для определения культуры</param>
    /// <returns>Локализованное название</returns>
    string GetNewParticipantName(long chatId);
} 