using ClubDoorman.Infrastructure;
using Microsoft.Extensions.Caching.Memory;

namespace ClubDoorman.Services;

/// <summary>
/// Реализация провайдера культуры чата
/// </summary>
public class ChatCultureProvider : IChatCultureProvider
{
    private readonly ILogger<ChatCultureProvider> _logger;
    private readonly IMemoryCache _cache;
    private readonly Dictionary<long, System.Globalization.CultureInfo> _chatCultures;
    private readonly System.Globalization.CultureInfo _defaultCulture;
    
    public ChatCultureProvider(ILogger<ChatCultureProvider> logger, IMemoryCache cache)
    {
        _logger = logger;
        _cache = cache;
        _chatCultures = new Dictionary<long, System.Globalization.CultureInfo>();
        
        // Определяем культуру по умолчанию из Config
        _defaultCulture = new System.Globalization.CultureInfo(Config.DefaultCulture);
        
        _logger.LogInformation("ChatCultureProvider initialized with default culture: {Culture}", _defaultCulture.Name);
        
        // Загружаем настройки чатов из переменных окружения
        LoadChatCulturesFromEnvironment();
    }
    
    /// <summary>
    /// Получить культуру для чата
    /// </summary>
    public System.Globalization.CultureInfo GetCultureForChat(long chatId)
    {
        // Сначала проверяем кэш
        var cacheKey = $"chat_culture_{chatId}";
        if (_cache.TryGetValue(cacheKey, out System.Globalization.CultureInfo? cachedCulture))
        {
            return cachedCulture;
        }
        
        // Проверяем настройки чата
        if (_chatCultures.TryGetValue(chatId, out var culture))
        {
            _cache.Set(cacheKey, culture, TimeSpan.FromHours(1));
            return culture;
        }
        
        // Возвращаем культуру по умолчанию
        _cache.Set(cacheKey, _defaultCulture, TimeSpan.FromHours(1));
        return _defaultCulture;
    }
    
    /// <summary>
    /// Получить культуру по умолчанию
    /// </summary>
    public System.Globalization.CultureInfo GetDefaultCulture()
    {
        return _defaultCulture;
    }
    
    /// <summary>
    /// Установить культуру для чата
    /// </summary>
    public void SetCultureForChat(long chatId, System.Globalization.CultureInfo culture)
    {
        _chatCultures[chatId] = culture;
        
        // Обновляем кэш
        var cacheKey = $"chat_culture_{chatId}";
        _cache.Set(cacheKey, culture, TimeSpan.FromHours(1));
        
        _logger.LogInformation("Set culture {Culture} for chat {ChatId}", culture.Name, chatId);
    }
    
    /// <summary>
    /// Удалить настройку культуры для чата
    /// </summary>
    public void RemoveChatCulture(long chatId)
    {
        if (_chatCultures.Remove(chatId))
        {
            // Удаляем из кэша
            var cacheKey = $"chat_culture_{chatId}";
            _cache.Remove(cacheKey);
            
            _logger.LogInformation("Removed custom culture for chat {ChatId}", chatId);
        }
    }
    
    /// <summary>
    /// Загрузить настройки чатов из переменных окружения
    /// </summary>
    private void LoadChatCulturesFromEnvironment()
    {
        // Формат: DOORMAN_CHAT_CULTURE_123456789=en
        var chatCultureVars = Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .Where(kvp => kvp.Key?.ToString()?.StartsWith("DOORMAN_CHAT_CULTURE_") == true);
        
        foreach (var kvp in chatCultureVars)
        {
            var key = kvp.Key?.ToString();
            var value = kvp.Value?.ToString();
            
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                continue;
            
            // Извлекаем ID чата из ключа
            var chatIdStr = key.Replace("DOORMAN_CHAT_CULTURE_", "");
            if (!long.TryParse(chatIdStr, out var chatId))
            {
                _logger.LogWarning("Invalid chat ID in environment variable: {Key}", key);
                continue;
            }
            
            // Парсим культуру
            var culture = value.ToLowerInvariant() switch
            {
                "en" or "en-us" or "en-gb" => new System.Globalization.CultureInfo("en"),
                "ru" or "ru-ru" => new System.Globalization.CultureInfo("ru"),
                _ => null
            };
            
            if (culture != null)
            {
                _chatCultures[chatId] = culture;
                _logger.LogInformation("Loaded culture {Culture} for chat {ChatId} from environment", culture.Name, chatId);
            }
            else
            {
                _logger.LogWarning("Invalid culture value '{Value}' for chat {ChatId}", value, chatId);
            }
        }
        
        _logger.LogInformation("Loaded {Count} chat culture settings from environment", _chatCultures.Count);
    }
} 