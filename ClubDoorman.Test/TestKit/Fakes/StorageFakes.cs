using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.SuspiciousUsers;
using ClubDoorman.Models;

namespace ClubDoorman.Test.TestKit.Fakes;

/// <summary>
/// Фальшивая in-memory реализация IApprovedUsersStorage для тестов
/// </summary>
public class ApprovedUsersStorageFake : IApprovedUsersStorage
{
    private readonly HashSet<long> _globalApprovedUsers = new();
    private readonly Dictionary<long, Dictionary<long, GroupApprovalInfo>> _groupApprovedUsers = new();
    private readonly object _lock = new();

    public bool IsGloballyApproved(long userId)
    {
        lock (_lock)
        {
            return _globalApprovedUsers.Contains(userId);
        }
    }

    public bool IsApprovedInGroup(long userId, long groupId)
    {
        lock (_lock)
        {
            return _groupApprovedUsers.TryGetValue(groupId, out var groupUsers) && 
                   groupUsers.ContainsKey(userId);
        }
    }

    public bool IsApproved(long userId, long? groupId = null)
    {
        lock (_lock)
        {
            // Сначала проверяем глобальное одобрение
            if (_globalApprovedUsers.Contains(userId))
                return true;

            // Если указана группа, проверяем одобрение в группе
            if (groupId.HasValue)
            {
                return IsApprovedInGroup(userId, groupId.Value);
            }

            return false;
        }
    }

    public void ApproveUserGlobally(long userId)
    {
        lock (_lock)
        {
            _globalApprovedUsers.Add(userId);
        }
    }

    public void ApproveUserInGroup(long userId, long groupId)
    {
        lock (_lock)
        {
            if (!_groupApprovedUsers.ContainsKey(groupId))
            {
                _groupApprovedUsers[groupId] = new Dictionary<long, GroupApprovalInfo>();
            }

            _groupApprovedUsers[groupId][userId] = new GroupApprovalInfo(DateTime.UtcNow);
        }
    }

    public bool RemoveGlobalApproval(long userId)
    {
        lock (_lock)
        {
            return _globalApprovedUsers.Remove(userId);
        }
    }

    public bool RemoveGroupApproval(long userId, long groupId)
    {
        lock (_lock)
        {
            return _groupApprovedUsers.TryGetValue(groupId, out var groupUsers) && 
                   groupUsers.Remove(userId);
        }
    }

    public bool RemoveAllApprovals(long userId)
    {
        lock (_lock)
        {
            bool globalRemoved = _globalApprovedUsers.Remove(userId);
            bool groupsRemoved = false;
            
            foreach (var groupUsers in _groupApprovedUsers.Values)
            {
                if (groupUsers.Remove(userId))
                    groupsRemoved = true;
            }
            
            return globalRemoved || groupsRemoved;
        }
    }

    public GroupApprovalInfo? GetGroupApprovalInfo(long userId, long groupId)
    {
        lock (_lock)
        {
            if (_groupApprovedUsers.TryGetValue(groupId, out var groupUsers) &&
                groupUsers.TryGetValue(userId, out var info))
            {
                return info;
            }
            return null;
        }
    }

    public Dictionary<long, GroupApprovalInfo> GetUserGroupApprovals(long userId)
    {
        lock (_lock)
        {
            var result = new Dictionary<long, GroupApprovalInfo>();
            foreach (var (groupId, groupUsers) in _groupApprovedUsers)
            {
                if (groupUsers.TryGetValue(userId, out var info))
                {
                    result[groupId] = info;
                }
            }
            return result;
        }
    }

    public (int globalCount, int groupCount, int totalGroupApprovals) GetApprovalStats()
    {
        lock (_lock)
        {
            var globalCount = _globalApprovedUsers.Count;
            var groupCount = _groupApprovedUsers.Count;
            var totalGroupApprovals = _groupApprovedUsers.Values.Sum(g => g.Count);
            
            return (globalCount, groupCount, totalGroupApprovals);
        }
    }

    // Методы для удобства в тестах
    
    /// <summary>
    /// Очистить все данные
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _globalApprovedUsers.Clear();
            _groupApprovedUsers.Clear();
        }
    }

    /// <summary>
    /// Предварительно заполнить данными для тестов
    /// </summary>
    public ApprovedUsersStorageFake WithGlobalApprovals(params long[] userIds)
    {
        foreach (var userId in userIds)
            ApproveUserGlobally(userId);
        return this;
    }

    /// <summary>
    /// Предварительно заполнить групповыми одобрениями для тестов
    /// </summary>
    public ApprovedUsersStorageFake WithGroupApprovals(long groupId, params long[] userIds)
    {
        foreach (var userId in userIds)
            ApproveUserInGroup(userId, groupId);
        return this;
    }
}

/// <summary>
/// Фальшивая in-memory реализация ISuspiciousUsersStorage для тестов
/// </summary>
public class SuspiciousUsersStorageFake : ISuspiciousUsersStorage
{
    private readonly Dictionary<(long UserId, long ChatId), SuspiciousUserInfo> _suspiciousUsers = new();
    private readonly object _lock = new();

    public bool IsSuspicious(long userId, long chatId)
    {
        lock (_lock)
        {
            return _suspiciousUsers.ContainsKey((userId, chatId));
        }
    }

    public bool AddSuspicious(long userId, long chatId, SuspiciousUserInfo info)
    {
        lock (_lock)
        {
            var key = (userId, chatId);
            bool isNew = !_suspiciousUsers.ContainsKey(key);
            _suspiciousUsers[key] = info;
            return isNew;
        }
    }

    public bool RemoveSuspicious(long userId, long chatId)
    {
        lock (_lock)
        {
            return _suspiciousUsers.Remove((userId, chatId));
        }
    }

    public bool UpdateMessageCount(long userId, long chatId, int messageCount)
    {
        lock (_lock)
        {
            var key = (userId, chatId);
            if (_suspiciousUsers.TryGetValue(key, out var info))
            {
                // Создаем новый объект с обновленным счетчиком
                var updatedInfo = new SuspiciousUserInfo(
                    info.FirstSeenAt,
                    messageCount,
                    info.IsAiDetectEnabled,
                    info.DetectionReason,
                    info.LastMessageAt
                );
                _suspiciousUsers[key] = updatedInfo;
                return true;
            }
            return false;
        }
    }

    public bool SetAiDetectEnabled(long userId, long chatId, bool enabled)
    {
        lock (_lock)
        {
            var key = (userId, chatId);
            if (_suspiciousUsers.TryGetValue(key, out var info))
            {
                // Создаем новый объект с обновленной настройкой AI
                var updatedInfo = new SuspiciousUserInfo(
                    info.FirstSeenAt,
                    info.MessageCount,
                    enabled,
                    info.DetectionReason,
                    info.LastMessageAt
                );
                _suspiciousUsers[key] = updatedInfo;
                return true;
            }
            return false;
        }
    }

    public SuspiciousUserInfo? GetSuspiciousInfo(long userId, long chatId)
    {
        lock (_lock)
        {
            _suspiciousUsers.TryGetValue((userId, chatId), out var info);
            return info;
        }
    }

    public SuspiciousUserInfo? GetSuspiciousUser(long userId, long chatId)
    {
        return GetSuspiciousInfo(userId, chatId);
    }

    public List<(long UserId, long ChatId)> GetAiDetectUsers()
    {
        lock (_lock)
        {
            return _suspiciousUsers
                .Where(kvp => kvp.Value.IsAiDetectEnabled)
                .Select(kvp => kvp.Key)
                .ToList();
        }
    }

    public (int TotalSuspicious, int WithAiDetect, int GroupsCount) GetStats()
    {
        lock (_lock)
        {
            var totalSuspicious = _suspiciousUsers.Count;
            var withAiDetect = _suspiciousUsers.Count(kvp => kvp.Value.IsAiDetectEnabled);
            var groupsCount = _suspiciousUsers.Select(kvp => kvp.Key.ChatId).Distinct().Count();
            
            return (totalSuspicious, withAiDetect, groupsCount);
        }
    }

    // Методы для удобства в тестах
    
    /// <summary>
    /// Очистить все данные
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _suspiciousUsers.Clear();
        }
    }

    /// <summary>
    /// Добавить подозрительного пользователя для тестов
    /// </summary>
    public SuspiciousUsersStorageFake WithSuspiciousUser(long userId, long chatId, 
        string reason = "Test suspicious user", bool aiDetectEnabled = false)
    {
        var info = new SuspiciousUserInfo(
            DateTime.UtcNow,
            1,
            aiDetectEnabled,
            reason,
            DateTime.UtcNow
        );
        AddSuspicious(userId, chatId, info);
        return this;
    }

    /// <summary>
    /// Добавить пользователя с включенной AI детекцией
    /// </summary>
    public SuspiciousUsersStorageFake WithAiDetectUser(long userId, long chatId, string reason = "AI detection enabled")
    {
        return WithSuspiciousUser(userId, chatId, reason, true);
    }
}