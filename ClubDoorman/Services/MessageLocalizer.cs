using System.Globalization;
using System.Reflection;
using ClubDoorman.Infrastructure;

namespace ClubDoorman.Services;

/// <summary>
/// Реализация локализатора сообщений
/// </summary>
public class MessageLocalizer : IMessageLocalizer
{
    private readonly ILogger<MessageLocalizer> _logger;
    private readonly Dictionary<string, System.Resources.ResourceManager> _resourceManagers;
    
    public MessageLocalizer(ILogger<MessageLocalizer> logger)
    {
        _logger = logger;
        _resourceManagers = new Dictionary<string, System.Resources.ResourceManager>
        {
            ["UserMessages"] = new System.Resources.ResourceManager("ClubDoorman.Resources.UserMessages", Assembly.GetExecutingAssembly()),
            ["AdminMessages"] = new System.Resources.ResourceManager("ClubDoorman.Resources.AdminMessages", Assembly.GetExecutingAssembly()),
            ["SystemMessages"] = new System.Resources.ResourceManager("ClubDoorman.Resources.SystemMessages", Assembly.GetExecutingAssembly())
        };
    }
    
    /// <summary>
    /// Получить культуру для чата
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <returns>Культура</returns>
    private CultureInfo GetCultureForChat(long chatId)
    {
        // Простая логика: пока все чаты на русском, кроме специальных
        // В будущем можно добавить настройку языка по чату
        if (chatId == Config.AdminChatId || chatId == Config.LogAdminChatId)
        {
            // Админские чаты могут быть на английском
            return new CultureInfo("en");
        }
        
        // По умолчанию русский
        return new CultureInfo("ru");
    }
    
    /// <summary>
    /// Получить культуру для админа
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <returns>Культура</returns>
    private CultureInfo GetCultureForAdmin(long chatId)
    {
        // Админы могут получать сообщения на английском для универсальности
        return new CultureInfo("en");
    }
    
    /// <summary>
    /// Получить локализованное сообщение для пользователя
    /// </summary>
    public string User(string key, long chatId, params object[] args)
    {
        try
        {
            var culture = GetCultureForChat(chatId);
            var resourceManager = _resourceManagers["UserMessages"];
            var message = resourceManager.GetString(key, culture);
            
            if (string.IsNullOrEmpty(message))
            {
                _logger.LogWarning("User message key '{Key}' not found for culture '{Culture}'", key, culture.Name);
                // Fallback на английский
                message = resourceManager.GetString(key, new CultureInfo("en")) ?? $"Missing key: {key}";
            }
            
            return args.Length > 0 ? string.Format(message, args) : message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user message for key '{Key}'", key);
            return $"Error: {key}";
        }
    }
    
    /// <summary>
    /// Получить локализованное сообщение для админа
    /// </summary>
    public string Admin(string key, long chatId, params object[] args)
    {
        try
        {
            var culture = GetCultureForAdmin(chatId);
            var resourceManager = _resourceManagers["AdminMessages"];
            var message = resourceManager.GetString(key, culture);
            
            if (string.IsNullOrEmpty(message))
            {
                _logger.LogWarning("Admin message key '{Key}' not found for culture '{Culture}'", key, culture.Name);
                // Fallback на английский
                message = resourceManager.GetString(key, new CultureInfo("en")) ?? $"Missing key: {key}";
            }
            
            return args.Length > 0 ? string.Format(message, args) : message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin message for key '{Key}'", key);
            return $"Error: {key}";
        }
    }
    
    /// <summary>
    /// Получить локализованное системное сообщение
    /// </summary>
    public string System(string key, params object[] args)
    {
        try
        {
            // Системные сообщения по умолчанию на английском
            var culture = new CultureInfo("en");
            var resourceManager = _resourceManagers["SystemMessages"];
            var message = resourceManager.GetString(key, culture);
            
            if (string.IsNullOrEmpty(message))
            {
                _logger.LogWarning("System message key '{Key}' not found for culture '{Culture}'", key, culture.Name);
                return $"Missing key: {key}";
            }
            
            return args.Length > 0 ? string.Format(message, args) : message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system message for key '{Key}'", key);
            return $"Error: {key}";
        }
    }
} 