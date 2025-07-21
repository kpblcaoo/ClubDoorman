using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using ClubDoorman.Infrastructure;
using ClubDoorman.Infrastructure.ErrorHandling;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services;

internal sealed class UserManager : IUserManager
{
    private readonly ILogger<UserManager> _logger;
    private readonly ApprovedUsersStorage _approvedUsersStorage;
    private readonly IErrorHandlingMiddleware _errorMiddleware;

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
                        _banlist.TryAdd(userId, 0);
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

    public UserManager(ILogger<UserManager> logger, ApprovedUsersStorage approvedUsersStorage, IErrorHandlingMiddleware errorMiddleware)
    {
        _logger = logger;
        _approvedUsersStorage = approvedUsersStorage;
        _errorMiddleware = errorMiddleware;
        
        // Логируем состояние тестового блэклиста при создании UserManager
        Console.WriteLine($"[DEBUG] UserManager создан: тестовый блэклист содержит {_testBlacklist.Count} ID(s): [{string.Join(", ", _testBlacklist)}]");
        
        if (Config.ClubServiceToken == null)
            _logger.LogWarning("DOORMAN_CLUB_SERVICE_TOKEN variable is not set, additional club checks disabled");
        else
            _clubHttpClient.DefaultRequestHeaders.Add("X-Service-Token", Config.ClubServiceToken);
    }

    private const string Path = "data/approved-users.txt";
    private readonly ConcurrentDictionary<long, byte> _banlist = [];
    private readonly SemaphoreSlim _semaphore = new(1);
    private readonly HashSet<long> _approved = File.ReadAllLines(Path).Select(long.Parse).ToHashSet();
    private readonly HttpClient _clubHttpClient = new();
    private readonly HttpClient _httpClient = new();
    
    // Тестовый блэклист из переменной окружения DOORMAN_TEST_BLACKLIST_IDS
    private static readonly HashSet<long> _testBlacklist = LoadTestBlacklist();

    public bool Approved(long userId, long? groupId = null) => _approvedUsersStorage.IsApproved(userId);

    public async ValueTask Approve(long userId, long? groupId = null)
    {
        _approvedUsersStorage.ApproveUser(userId);
    }

    public async Task<bool> RemoveApprovalAsync(long userId, long? groupId = null, bool removeAll = false)
    {
        return await _errorMiddleware.ExecuteWithErrorHandlingAsync(async () =>
        {
            return _approvedUsersStorage.RemoveApproval(userId);
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
    /// Проверяет, находится ли пользователь в банлисте
    /// </summary>
    /// <param name="userId">ID пользователя для проверки</param>
    /// <returns>true, если пользователь находится в банлисте</returns>
    /// <exception cref="UserManagementException">Выбрасывается при критических ошибках проверки</exception>
    public async ValueTask<bool> InBanlist(long userId)
    {
        // CODE QUALITY - Consider extracting userId validation to avoid duplication
        if (userId <= 0)
        {
            _logger.LogWarning("Попытка проверить некорректный ID пользователя: {UserId}", userId);
            return false;
        }

        Console.WriteLine($"[DEBUG] InBanlist: проверяем пользователя {userId} (тестовых ID: {_testBlacklist.Count})");
        _logger.LogDebug("InBanlist: проверяем пользователя {UserId} (тестовых ID: {TestCount})", userId, _testBlacklist.Count);
        
        // Сначала проверяем тестовый блэклист
        if (_testBlacklist.Contains(userId))
        {
            Console.WriteLine($"[DEBUG] 🎯 НАЙДЕН в тестовом блэклисте: {userId}");
            _logger.LogWarning("🎯 Пользователь {UserId} найден в ТЕСТОВОМ блэклисте", userId);
            return true;
        }
        
        // Проверяем локальный кэш
        if (_banlist.ContainsKey(userId))
        {
            _logger.LogDebug("Пользователь {UserId} найден в локальном кэше банлиста", userId);
            return true;
        }

        // Проверяем через API
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var result = await _httpClient.GetFromJsonAsync<LolsBotApiResponse>(
                $"https://api.lols.bot/account?id={userId}", 
                cts.Token
            );
            
            if (result == null)
            {
                _logger.LogWarning("Получен null ответ от LolsBot API для пользователя {UserId}", userId);
                return false;
            }

            // Проверяем, есть ли пользователь в списке забаненных
            if (result.banned_users != null && result.banned_users.Contains(userId))
            {
                _logger.LogInformation("Пользователь {UserId} найден в банлисте LolsBot", userId);
                _banlist.TryAdd(userId, 0);
                return true;
            }

            return false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Таймаут при проверке пользователя {UserId} в LolsBot API", userId);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Ошибка сети при проверке пользователя {UserId} в LolsBot API", userId);
            return false;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Ошибка парсинга JSON при проверке пользователя {UserId} в LolsBot API", userId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Неожиданная ошибка при проверке пользователя {UserId} в LolsBot API", userId);
            return false;
        }
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
            // cannot use _clubHttpClient.GetFromJsonAsync here because the response is not 200 OK at the time of writing for when user is not found
            var get = await _clubHttpClient.GetAsync(url, cts.Token);
            var response = await get.Content.ReadFromJsonAsync<ClubByTgIdResponse>(cancellationToken: cts.Token);
            var fullName = response?.user?.full_name;
            if (!string.IsNullOrEmpty(fullName))
                await Approve(userId);
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
