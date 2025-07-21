using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Json;
using ClubDoorman.Infrastructure;
using ClubDoorman.Infrastructure.ErrorHandling;

namespace ClubDoorman.Services;

internal sealed class UserManagerV2 : IUserManager
{
    private readonly ILogger<UserManagerV2> _logger;
    private readonly ApprovedUsersStorageV2 _approvedUsersStorage;
    private readonly IErrorHandlingMiddleware _errorMiddleware;
    private readonly ConcurrentDictionary<long, byte> _banlist = [];
    private readonly SemaphoreSlim _semaphore = new(1);
    private readonly HttpClient _clubHttpClient = new();
    private readonly HttpClient _httpClient = new();
    
    // Тестовый блэклист из переменной окружения DOORMAN_TEST_BLACKLIST_IDS
    private static readonly HashSet<long> _testBlacklist = LoadTestBlacklist();

    public UserManagerV2(ILogger<UserManagerV2> logger, ApprovedUsersStorageV2 approvedUsersStorage, IErrorHandlingMiddleware errorMiddleware)
    {
        _logger = logger;
        _approvedUsersStorage = approvedUsersStorage;
        _errorMiddleware = errorMiddleware;
        
        // Логируем состояние тестового блэклиста при создании UserManagerV2
        Console.WriteLine($"[DEBUG] UserManagerV2 создан: тестовый блэклист содержит {_testBlacklist.Count} ID(s): [{string.Join(", ", _testBlacklist)}]");
        
        if (Config.ClubServiceToken == null)
            _logger.LogWarning("DOORMAN_CLUB_SERVICE_TOKEN variable is not set, additional club checks disabled");
        else
            _clubHttpClient.DefaultRequestHeaders.Add("X-Service-Token", Config.ClubServiceToken);
    }

    public async Task RefreshBanlist()
    {
        await _errorMiddleware.ExecuteTelegramApiAsync(async () =>
        {
            await _semaphore.WaitAsync();
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var response = await _httpClient.GetFromJsonAsync<LolsBotApiResponse>(
                    "https://api.lols.bot/banlist", 
                    cts.Token
                );

                if (response?.banned_users != null && response.banned_users.Length > 0)
                {
                    _banlist.Clear();
                    foreach (var userId in response.banned_users)
                    {
                        _banlist.TryAdd(userId, 1);
                    }
                    _logger.LogInformation("Обновлен банлист из lols.bot: {Count} пользователей", response.banned_users.Length);
                }
                else
                {
                    _logger.LogWarning("Получен пустой банлист от lols.bot или ответ был null");
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }, "RefreshBanlist", null, null, CancellationToken.None);
    }

    /// <summary>
    /// Проверяет, одобрен ли пользователь
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="groupId">ID группы (для проверки группового одобрения)</param>
    /// <returns>true, если пользователь одобрен</returns>
    public bool Approved(long userId, long? groupId = null)
    {
        return _approvedUsersStorage.IsApproved(userId, groupId);
    }

    /// <summary>
    /// Одобряет пользователя в зависимости от настроек
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="groupId">ID группы (для группового одобрения)</param>
    public async ValueTask Approve(long userId, long? groupId = null)
    {
        if (Config.GlobalApprovalMode)
        {
            // Глобальный режим: одобряем глобально
            _approvedUsersStorage.ApproveUserGlobally(userId);
        }
        else
        {
            // Групповой режим: одобряем в конкретной группе
            if (groupId.HasValue)
            {
                _approvedUsersStorage.ApproveUserInGroup(userId, groupId.Value);
            }
            else
            {
                // Если группа не указана, одобряем глобально как fallback
                _approvedUsersStorage.ApproveUserGlobally(userId);
            }
        }
    }

    /// <summary>
    /// Удаляет одобрение пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="groupId">ID группы (для удаления группового одобрения)</param>
    /// <param name="removeAll">Удалить все одобрения пользователя</param>
    /// <returns>true, если одобрение было удалено</returns>
    public async Task<bool> RemoveApprovalAsync(long userId, long? groupId = null, bool removeAll = false)
    {
        return await _errorMiddleware.ExecuteWithErrorHandlingAsync(async () =>
        {
            if (removeAll)
            {
                return _approvedUsersStorage.RemoveAllApprovals(userId);
            }
            
            if (groupId.HasValue)
            {
                // Удаляем одобрение в конкретной группе
                return _approvedUsersStorage.RemoveGroupApproval(userId, groupId.Value);
            }
            else
            {
                // Удаляем глобальное одобрение
                return _approvedUsersStorage.RemoveGlobalApproval(userId);
            }
        }, new ErrorContext("RemoveApproval", $"Удаление одобрения пользователя {userId}", ErrorSeverity.Medium));
    }

    /// <summary>
    /// Удаляет одобрение пользователя (синхронная обертка для обратной совместимости)
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="groupId">ID группы (для удаления группового одобрения)</param>
    /// <param name="removeAll">Удалить все одобрения пользователя</param>
    /// <returns>true, если одобрение было удалено</returns>
    public bool RemoveApproval(long userId, long? groupId = null, bool removeAll = false)
    {
        return RemoveApprovalAsync(userId, groupId, removeAll).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Получает информацию об одобрении пользователя
    /// </summary>
    public (bool isGlobal, Dictionary<long, GroupApprovalInfo> groupApprovals) GetApprovalInfo(long userId)
    {
        var isGlobal = _approvedUsersStorage.IsGloballyApproved(userId);
        var groupApprovals = _approvedUsersStorage.GetUserGroupApprovals(userId);
        return (isGlobal, groupApprovals);
    }

    /// <summary>
    /// Получает статистику одобрений
    /// </summary>
    public (int globalCount, int groupCount, int totalGroupApprovals) GetApprovalStats()
    {
        return _approvedUsersStorage.GetApprovalStats();
    }

    /// <summary>
    /// Проверяет, находится ли пользователь в банлисте спамеров.
    /// Использует только кэшированный статический банлист из lols.bot без дополнительных HTTP запросов.
    /// </summary>
    /// <param name="userId">ID пользователя для проверки</param>
    /// <returns>true если пользователь в банлисте, false если нет</returns>
    public async ValueTask<bool> InBanlist(long userId)
    {
        Console.WriteLine($"[DEBUG] UserManagerV2.InBanlist: проверяем пользователя {userId} (тестовых ID: {_testBlacklist.Count})");
        _logger.LogDebug("InBanlist: проверяем пользователя {UserId} (тестовых ID: {TestCount})", userId, _testBlacklist.Count);
        
        // 1. Сначала проверяем тестовый блэклист
        if (_testBlacklist.Contains(userId))
        {
            Console.WriteLine($"[DEBUG] 🎯 НАЙДЕН в тестовом блэклисте: {userId}");
            _logger.LogWarning("🎯 Пользователь {UserId} найден в ТЕСТОВОМ блэклисте", userId);
            return true;
        }
        
        // 2. Проверяем кэшированный результат из статического банлиста
        if (_banlist.TryGetValue(userId, out var cachedResult))
        {
            var isBanned = cachedResult == 1;
            _logger.LogDebug("✅ Пользователь {UserId} найден в кэше: {Status}", userId, isBanned ? "ЗАБЛОКИРОВАН" : "НЕ заблокирован");
            return isBanned; // 1 = banned, 0 = not banned
        }
        
        // 3. Если пользователя нет в статическом банлисте - считаем НЕ заблокированным и кэшируем
        _banlist.TryAdd(userId, 0); // 0 = not banned
        _logger.LogDebug("✅ Пользователь {UserId} НЕ в банлисте lols.bot, кэшируем как незаблокированного", userId);
        return false;
    }

    public async ValueTask<string?> GetClubUsername(long userId)
    {
        if (Config.ClubServiceToken == null)
            return null;
        var url = $"{Config.ClubUrl}user/by_telegram_id/{userId}.json";
        try
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            var get = await _clubHttpClient.GetAsync(url, cts.Token);
            var response = await get.Content.ReadFromJsonAsync<ClubByTgIdResponse>(cancellationToken: cts.Token);
            var fullName = response?.user?.full_name;
            if (!string.IsNullOrEmpty(fullName))
                await Approve(userId); // Одобряем глобально, так как это клубный пользователь
            return fullName;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "GetClubUsername");
            return null;
        }
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable IDE1006 // Naming Styles

    private record LolsBotApiResponse(long[] banned_users);

    internal class ClubByTgIdResponse
    {
        public Error? error { get; set; }
        public User? user { get; set; }
    }

    internal class Error
    {
        public string code { get; set; }
        public Data data { get; set; }
        public object message { get; set; }
        public string title { get; set; }
    }

    internal class Data { }

    internal class User
    {
        public string avatar { get; set; }
        public string bio { get; set; }
        public string city { get; set; }
        public string company { get; set; }
        public string country { get; set; }
        public DateTime created_at { get; set; }
        public string? full_name { get; set; }
        public string id { get; set; }
        public bool is_active_member { get; set; }
        public DateTime membership_expires_at { get; set; }
        public DateTime membership_started_at { get; set; }
        public string moderation_status { get; set; }
        public string payment_status { get; set; }
        public string position { get; set; }
        public string slug { get; set; }
        public int upvotes { get; set; }
    }

    /// <summary>
    /// Загружает тестовый блэклист из переменной окружения DOORMAN_TEST_BLACKLIST_IDS
    /// Формат: "123456,789012,345678" (ID через запятую)
    /// </summary>
    private static HashSet<long> LoadTestBlacklist()
    {
        var testIds = Environment.GetEnvironmentVariable("DOORMAN_TEST_BLACKLIST_IDS");
        if (string.IsNullOrWhiteSpace(testIds))
        {
            Console.WriteLine("[DEBUG] DOORMAN_TEST_BLACKLIST_IDS не задана - тестовый блэклист пустой");
            return [];
        }

        var result = new HashSet<long>();
        var ids = testIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var idStr in ids)
        {
            if (long.TryParse(idStr.Trim(), out var id))
            {
                result.Add(id);
            }
            else
            {
                Console.WriteLine($"[WARNING] Некорректный ID в DOORMAN_TEST_BLACKLIST_IDS: '{idStr}'");
            }
        }
        
        Console.WriteLine($"[DEBUG] Загружен тестовый блэклист: {result.Count} ID(s) [{string.Join(", ", result)}]");
        return result;
    }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning restore IDE1006 // Naming Styles
} 