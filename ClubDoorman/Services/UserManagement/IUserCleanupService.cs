namespace ClubDoorman.Services.UserManagement;

/// <summary>
/// Сервис для очистки пользователей из списков одобренных
/// </summary>
public interface IUserCleanupService
{
    /// <summary>
    /// Удаляет пользователя из всех списков одобренных
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="reason">Причина удаления</param>
    /// <returns>true, если пользователь был удален</returns>
    bool RemoveUserFromAllApprovals(long userId, string reason);

    /// <summary>
    /// Удаляет пользователя из списка одобренных конкретной группы
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="groupId">ID группы</param>
    /// <param name="reason">Причина удаления</param>
    /// <returns>true, если пользователь был удален</returns>
    bool RemoveUserFromGroupApproval(long userId, long groupId, string reason);

    /// <summary>
    /// Удаляет пользователя из глобального списка одобренных
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="reason">Причина удаления</param>
    /// <returns>true, если пользователь был удален</returns>
    bool RemoveUserFromGlobalApproval(long userId, string reason);
}