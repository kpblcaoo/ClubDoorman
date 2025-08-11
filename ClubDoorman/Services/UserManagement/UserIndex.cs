using System.Runtime.Caching;

namespace ClubDoorman.Services.UserManagement;

/// <summary>
/// Сервис для индексации и поиска пользователей по кэшу
/// </summary>
public class UserIndex : IUserIndex
{
    /// <inheritdoc />
    public long? TryFindUserIdByUsername(string username)
    {
        if (string.IsNullOrEmpty(username))
            return null;
            
        // Поиск в MemoryCache по значениям, где username встречался
        foreach (var item in MemoryCache.Default)
        {
            // Ищем в значении, а не в ключе
            if (item.Value is string text && text.Contains(username, StringComparison.OrdinalIgnoreCase))
            {
                // Ключи вида chatId_userId
                var parts = item.Key.ToString()?.Split('_');
                if (parts?.Length == 2 && long.TryParse(parts[1], out var uid))
                    return uid;
            }
        }
        
        return null;
    }
    
    /// <inheritdoc />
    public void IndexUser(long chatId, long userId, string text)
    {
        if (string.IsNullOrEmpty(text))
            return;
            
        var key = $"{chatId}_{userId}";
        var policy = new CacheItemPolicy
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddHours(24) // Хранить 24 часа
        };
        
        MemoryCache.Default.Set(key, text, policy);
    }
}
