namespace ClubDoorman.Services;

/// <summary>
/// Интерфейс для локализации сообщений
/// </summary>
public interface IMessageLocalizer
{
    /// <summary>
    /// Получить локализованное сообщение для пользователя
    /// </summary>
    /// <param name="key">Ключ сообщения</param>
    /// <param name="chatId">ID чата для определения языка</param>
    /// <param name="args">Аргументы для форматирования</param>
    /// <returns>Локализованное сообщение</returns>
    string User(string key, long chatId, params object[] args);
    
    /// <summary>
    /// Получить локализованное сообщение для админа
    /// </summary>
    /// <param name="key">Ключ сообщения</param>
    /// <param name="chatId">ID чата для определения языка</param>
    /// <param name="args">Аргументы для форматирования</param>
    /// <returns>Локализованное сообщение</returns>
    string Admin(string key, long chatId, params object[] args);
    
    /// <summary>
    /// Получить локализованное системное сообщение
    /// </summary>
    /// <param name="key">Ключ сообщения</param>
    /// <param name="args">Аргументы для форматирования</param>
    /// <returns>Локализованное сообщение</returns>
    string System(string key, params object[] args);
} 