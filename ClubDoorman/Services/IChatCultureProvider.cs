namespace ClubDoorman.Services;

/// <summary>
/// Интерфейс для провайдера культуры чата
/// </summary>
public interface IChatCultureProvider
{
    /// <summary>
    /// Получить культуру для чата
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <returns>Культура для чата</returns>
    System.Globalization.CultureInfo GetCultureForChat(long chatId);
    
    /// <summary>
    /// Получить культуру по умолчанию
    /// </summary>
    /// <returns>Культура по умолчанию</returns>
    System.Globalization.CultureInfo GetDefaultCulture();
    
    /// <summary>
    /// Установить культуру для чата
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <param name="culture">Культура</param>
    void SetCultureForChat(long chatId, System.Globalization.CultureInfo culture);
    
    /// <summary>
    /// Удалить настройку культуры для чата
    /// </summary>
    /// <param name="chatId">ID чата</param>
    void RemoveChatCulture(long chatId);
} 