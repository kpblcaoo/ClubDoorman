namespace ClubDoorman.Services.UserManagement;

/// <summary>
/// Интерфейс для управления одобренными пользователями
/// </summary>
public interface IApprovedUsersStorage
{
    /// <summary>
    /// Проверяет, одобрен ли пользователь глобально
    /// </summary>
    bool IsGloballyApproved(long userId);

    /// <summary>
    /// Проверяет, одобрен ли пользователь в конкретной группе
    /// </summary>
    bool IsApprovedInGroup(long userId, long groupId);

    /// <summary>
    /// Проверяет, одобрен ли пользователь (глобально или в группе)
    /// </summary>
    bool IsApproved(long userId, long? groupId = null);

    /// <summary>
    /// Одобряет пользователя глобально
    /// </summary>
    void ApproveUserGlobally(long userId);

    /// <summary>
    /// Одобряет пользователя в конкретной группе
    /// </summary>
    void ApproveUserInGroup(long userId, long groupId);

    /// <summary>
    /// Удаляет пользователя из глобального списка одобренных
    /// </summary>
    bool RemoveGlobalApproval(long userId);

    /// <summary>
    /// Удаляет пользователя из списка одобренных конкретной группы
    /// </summary>
    bool RemoveGroupApproval(long userId, long groupId);

    /// <summary>
    /// Удаляет пользователя из всех списков одобренных (глобально и во всех группах)
    /// </summary>
    bool RemoveAllApprovals(long userId);

    /// <summary>
    /// Получает информацию об одобрении пользователя в группе
    /// </summary>
    GroupApprovalInfo? GetGroupApprovalInfo(long userId, long groupId);

    /// <summary>
    /// Получает все группы, в которых пользователь одобрен
    /// </summary>
    Dictionary<long, GroupApprovalInfo> GetUserGroupApprovals(long userId);

    /// <summary>
    /// Получает статистику одобрений
    /// </summary>
    (int globalCount, int groupCount, int totalGroupApprovals) GetApprovalStats();
}