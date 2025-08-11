namespace ClubDoorman.Services.UserManagement;

/// <summary>
/// Интерфейс для управления флагами присоединившихся пользователей
/// </summary>
public interface IJoinedUserFlags
{
    /// <summary>
    /// Проверяет, присоединился ли пользователь к чату недавно
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <param name="userId">ID пользователя</param>
    /// <returns>true, если пользователь недавно присоединился</returns>
    bool IsUserRecentlyJoined(long chatId, long userId);
    
    /// <summary>
    /// Отмечает пользователя как недавно присоединившегося
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <param name="userId">ID пользователя</param>
    void MarkUserAsJoined(long chatId, long userId);
    
    /// <summary>
    /// Удаляет флаг о недавнем присоединении пользователя
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <param name="userId">ID пользователя</param>
    void RemoveJoinedFlag(long chatId, long userId);
}
