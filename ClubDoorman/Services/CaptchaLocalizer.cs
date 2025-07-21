using ClubDoorman.Infrastructure;

namespace ClubDoorman.Services;

/// <summary>
/// Реализация локализатора капчи
/// </summary>
public class CaptchaLocalizer : ICaptchaLocalizer
{
    private readonly IMessageLocalizer _messageLocalizer;
    private readonly IChatCultureProvider _cultureProvider;
    private readonly ILogger<CaptchaLocalizer> _logger;
    
    // Маппинг индексов эмодзи на ключи ресурсов
    private static readonly Dictionary<int, string> _emojiKeyMapping = new()
    {
        [0] = "CaptchaUnicorn",
        [1] = "CaptchaHammer", 
        [2] = "CaptchaCat",
        [3] = "CaptchaAnchor",
        [4] = "CaptchaDolphin",
        [5] = "CaptchaApple",
        [6] = "CaptchaBall",
        [7] = "CaptchaHorse",
        [8] = "CaptchaDuck",
        [9] = "CaptchaRaccoon",
        [10] = "CaptchaOwl",
        [11] = "CaptchaTurtle",
        [12] = "CaptchaCrab",
        [13] = "CaptchaBanana",
        [14] = "CaptchaWatermelon",
        [15] = "CaptchaClock",
        [16] = "CaptchaAirplane",
        [17] = "CaptchaKnife",
        [18] = "CaptchaTshirt",
        [19] = "CaptchaScissors",
        [20] = "CaptchaWhale",
        [21] = "CaptchaElephant",
        [22] = "CaptchaFlamingo",
        [23] = "CaptchaPopcorn",
        [24] = "CaptchaButterfly",
        [25] = "CaptchaCrown",
        [26] = "CaptchaSkull",
        [27] = "CaptchaBoomerang",
        [28] = "CaptchaEar"
    };
    
    public CaptchaLocalizer(
        IMessageLocalizer messageLocalizer,
        IChatCultureProvider cultureProvider,
        ILogger<CaptchaLocalizer> logger)
    {
        _messageLocalizer = messageLocalizer;
        _cultureProvider = cultureProvider;
        _logger = logger;
    }
    
    /// <summary>
    /// Получить локализованное описание эмодзи капчи
    /// </summary>
    public string GetEmojiDescription(int emojiIndex, long chatId)
    {
        if (!_emojiKeyMapping.TryGetValue(emojiIndex, out var key))
        {
            _logger.LogWarning("Unknown emoji index: {EmojiIndex}, using fallback", emojiIndex);
            return "unknown";
        }
        
        try
        {
            return _messageLocalizer.User(key, chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting localized emoji description for index {EmojiIndex}", emojiIndex);
            return "unknown";
        }
    }
    
    /// <summary>
    /// Получить локализованное сообщение капчи
    /// </summary>
    public string GetCaptchaMessage(string userMention, string emojiDescription, long chatId)
    {
        try
        {
            return _messageLocalizer.User("CaptchaMessage", chatId, userMention, emojiDescription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting localized captcha message");
            return $"Hello, {userMention}! Anti-spam: which button has {emojiDescription}?";
        }
    }
    
    /// <summary>
    /// Получить локализованный плейсхолдер для рекламы
    /// </summary>
    public string GetAdPlaceholder(long chatId)
    {
        try
        {
            return _messageLocalizer.User("CaptchaAdPlaceholder", chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting localized ad placeholder");
            return "\n\n 📍 Ad space\n<i>...</i>";
        }
    }
    
    /// <summary>
    /// Получить локализованное название для нового участника
    /// </summary>
    public string GetNewParticipantName(long chatId)
    {
        try
        {
            return _messageLocalizer.User("CaptchaNewParticipant", chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting localized new participant name");
            return "new chat participant";
        }
    }
} 