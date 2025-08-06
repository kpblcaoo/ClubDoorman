using Microsoft.Extensions.Logging;
using ClubDoorman.Services.UserManagement;

namespace ClubDoorman.Services.UserManagement;

/// <summary>
/// Реализация сервиса для очистки пользователей из списков одобренных
/// </summary>
public class UserCleanupService : IUserCleanupService
{
    private readonly ApprovedUsersStorage _approvedUsersStorage;
    private readonly ILogger<UserCleanupService> _logger;
    
    public UserCleanupService(ApprovedUsersStorage approvedUsersStorage, ILogger<UserCleanupService> logger)
    {
        _approvedUsersStorage = approvedUsersStorage;
        _logger = logger;
    }
    
    /// <summary>
    /// Удаляет пользователя из всех списков одобренных
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="reason">Причина удаления</param>
    /// <returns>true, если пользователь был удален</returns>
    public bool RemoveUserFromAllApprovals(long userId, string reason)
    {
        try
        {
            var result = _approvedUsersStorage.RemoveAllApprovals(userId);
            if (result)
            {
                _logger.LogInformation("Пользователь {UserId} удален из всех списков одобренных. Причина: {Reason}", userId, reason);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении пользователя {UserId} из всех списков одобренных. Причина: {Reason}", userId, reason);
            return false;
        }
    }
    
    /// <summary>
    /// Удаляет пользователя из списка одобренных конкретной группы
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="groupId">ID группы</param>
    /// <param name="reason">Причина удаления</param>
    /// <returns>true, если пользователь был удален</returns>
    public bool RemoveUserFromGroupApproval(long userId, long groupId, string reason)
    {
        try
        {
            var result = _approvedUsersStorage.RemoveGroupApproval(userId, groupId);
            if (result)
            {
                _logger.LogInformation("Пользователь {UserId} удален из списка одобренных группы {GroupId}. Причина: {Reason}", userId, groupId, reason);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении пользователя {UserId} из списка одобренных группы {GroupId}. Причина: {Reason}", userId, groupId, reason);
            return false;
        }
    }
    
    /// <summary>
    /// Удаляет пользователя из глобального списка одобренных
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="reason">Причина удаления</param>
    /// <returns>true, если пользователь был удален</returns>
    public bool RemoveUserFromGlobalApproval(long userId, string reason)
    {
        try
        {
            var result = _approvedUsersStorage.RemoveGlobalApproval(userId);
            if (result)
            {
                _logger.LogInformation("Пользователь {UserId} удален из глобального списка одобренных. Причина: {Reason}", userId, reason);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении пользователя {UserId} из глобального списка одобренных. Причина: {Reason}", userId, reason);
            return false;
        }
    }
} 