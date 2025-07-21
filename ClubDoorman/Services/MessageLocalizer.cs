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
    private readonly IChatCultureProvider _cultureProvider;
    
    public MessageLocalizer(ILogger<MessageLocalizer> logger, IChatCultureProvider cultureProvider)
    {
        _logger = logger;
        _cultureProvider = cultureProvider;
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
        return _cultureProvider.GetCultureForChat(chatId);
    }
    
    /// <summary>
    /// Получить культуру для админа
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <returns>Культура</returns>
    private CultureInfo GetCultureForAdmin(long chatId)
    {
        // Админы могут получать сообщения на английском для универсальности
        // Но можно переопределить через настройки чата
        return _cultureProvider.GetCultureForChat(chatId);
    }
    
    /// <summary>
    /// Получить локализованное сообщение для пользователя
    /// </summary>
    public string User(string key, long chatId, params object[] args)
    {
        var culture = GetCultureForChat(chatId);
        return User(key, culture, args);
    }
    
    /// <summary>
    /// Получить локализованное сообщение для пользователя с указанной культурой
    /// </summary>
    public string User(string key, CultureInfo culture, params object[] args)
    {
        try
        {
            var resourceManager = _resourceManagers["UserMessages"];
            var message = resourceManager.GetString(key, culture);
            
            if (string.IsNullOrEmpty(message))
            {
                _logger.LogWarning("User message key '{Key}' not found for culture '{Culture}'", key, culture.Name);
                // Fallback к GenericError
                var fallbackMessage = resourceManager.GetString("GenericError", culture) ?? "Something went wrong. Please try again later.";
                return fallbackMessage;
            }
            
            return args.Length > 0 ? string.Format(culture, message, args) : message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user message for key '{Key}'", key);
            return "Something went wrong. Please try again later.";
        }
    }
    
    /// <summary>
    /// Получить локализованное сообщение для админа
    /// </summary>
    public string Admin(string key, long chatId, params object[] args)
    {
        var culture = GetCultureForAdmin(chatId);
        return Admin(key, culture, args);
    }
    
    /// <summary>
    /// Получить локализованное сообщение для админа с указанной культурой
    /// </summary>
    public string Admin(string key, CultureInfo culture, params object[] args)
    {
        try
        {
            var resourceManager = _resourceManagers["AdminMessages"];
            var message = resourceManager.GetString(key, culture);
            
            if (string.IsNullOrEmpty(message))
            {
                _logger.LogWarning("Admin message key '{Key}' not found for culture '{Culture}'", key, culture.Name);
                return $"Missing key: {key}";
            }
            
            return args.Length > 0 ? string.Format(culture, message, args) : message;
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
        return System(key, _cultureProvider.GetDefaultCulture(), args);
    }
    
    /// <summary>
    /// Получить локализованное системное сообщение с указанной культурой
    /// </summary>
    public string System(string key, CultureInfo culture, params object[] args)
    {
        try
        {
            var resourceManager = _resourceManagers["SystemMessages"];
            var message = resourceManager.GetString(key, culture);
            
            if (string.IsNullOrEmpty(message))
            {
                _logger.LogWarning("System message key '{Key}' not found for culture '{Culture}'", key, culture.Name);
                return $"Missing key: {key}";
            }
            
            return args.Length > 0 ? string.Format(culture, message, args) : message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system message for key '{Key}'", key);
            return $"Error: {key}";
        }
    }
} 