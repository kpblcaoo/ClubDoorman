namespace ClubDoorman.Services.UserManagement;

/// <summary>
/// Интерфейс для поиска пользователей по индексу/кэшу
/// </summary>
public interface IUserIndex
{
    /// <summary>
    /// Пытается найти ID пользователя по username среди недавних пользователей
    /// </summary>
    /// <param name="username">Username для поиска</param>
    /// <returns>ID пользователя или null, если не найден</returns>
    long? TryFindUserIdByUsername(string username);

    /// <summary>
    /// Добавляет информацию о пользователе в индекс
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <param name="userId">ID пользователя</param>
    /// <param name="text">Текст, связанный с пользователем</param>
    void IndexUser(long chatId, long userId, string text);
}
