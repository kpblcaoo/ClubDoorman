namespace ClubDoorman.Services;

/// <summary>
/// Интерфейс для управления состоянием пользователей в системе
/// </summary>
public interface IUserStateManager
{
    /// <summary>
    /// Сбрасывает пользователя из всех внутренних списков и кэшей.
    /// Используется как при бане, так и при одобрении пользователя.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    void CleanupUserFromAllLists(long userId, long chatId);
} 