using System.Collections.Concurrent;

namespace ClubDoorman.Services.UserManagement;

/// <summary>
/// Сервис для управления флагами недавно присоединившихся пользователей
/// </summary>
public class JoinedUserFlags : IJoinedUserFlags
{
    // Флаги присоединившихся пользователей (временные)
    private readonly ConcurrentDictionary<string, byte> _joinedUserFlags = new();
    
    /// <inheritdoc />
    public bool IsUserRecentlyJoined(long chatId, long userId)
    {
        var joinKey = $"joined_{chatId}_{userId}";
        return _joinedUserFlags.ContainsKey(joinKey);
    }
    
    /// <inheritdoc />
    public void MarkUserAsJoined(long chatId, long userId)
    {
        var joinKey = $"joined_{chatId}_{userId}";
        _joinedUserFlags.TryAdd(joinKey, 1);
        
        // Автоматическое удаление через 15 секунд
        _ = Task.Run(async () => 
        { 
            await Task.Delay(15000); 
            RemoveJoinedFlag(chatId, userId);
        });
    }
    
    /// <inheritdoc />
    public void RemoveJoinedFlag(long chatId, long userId)
    {
        var joinKey = $"joined_{chatId}_{userId}";
        _joinedUserFlags.TryRemove(joinKey, out _);
    }
}
