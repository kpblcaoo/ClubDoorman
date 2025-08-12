namespace ClubDoorman.Services.Core.Configuration;

/// <summary>
/// Помощник для загрузки конфигурации из переменных окружения
/// </summary>
public static class ConfigurationHelper
{
    /// <summary>
    /// Получает булево значение из переменной окружения
    /// </summary>
    public static bool GetEnvironmentBool(string envName, bool defaultValue = false)
    {
        var env = Environment.GetEnvironmentVariable(envName);
        if (env == null)
            return defaultValue;
        if (int.TryParse(env, out var num) && num == 1)
            return true;
        if (bool.TryParse(env, out var b) && b)
            return true;
        return defaultValue;
    }

    /// <summary>
    /// Получает целое число из переменной окружения
    /// </summary>
    public static int GetEnvironmentInt(string envName, int defaultValue)
    {
        var env = Environment.GetEnvironmentVariable(envName);
        if (env == null)
            return defaultValue;
        if (int.TryParse(env, out var result))
            return result;
        return defaultValue;
    }

    /// <summary>
    /// Получает список чатов из переменной окружения
    /// </summary>
    public static HashSet<long> GetEnvironmentChatList(string envName)
    {
        var chatsStr = Environment.GetEnvironmentVariable(envName);
        if (string.IsNullOrEmpty(chatsStr))
            return new HashSet<long>();
        
        return chatsStr
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => long.TryParse(x.Trim(), out var id) ? id : (long?)null)
            .Where(x => x.HasValue)
            .Select(x => x.Value)
            .ToHashSet();
    }

    /// <summary>
    /// Загружает опции автобанов из переменных окружения
    /// </summary>
    public static AutoBanOptions LoadAutoBanOptions()
    {
        return new AutoBanOptions
        {
            BlacklistAutoBan = !GetEnvironmentBool("DOORMAN_BLACKLIST_AUTOBAN_DISABLE"),
            ChannelAutoBan = !GetEnvironmentBool("DOORMAN_CHANNELS_AUTOBAN_DISABLE"),
            LookAlikeAutoBan = !GetEnvironmentBool("DOORMAN_LOOKALIKE_AUTOBAN_DISABLE"),
            ButtonAutoBan = !GetEnvironmentBool("DOORMAN_BUTTON_AUTOBAN_DISABLE"),
            HighConfidenceAutoBan = !GetEnvironmentBool("DOORMAN_HIGH_CONFIDENCE_AUTOBAN_DISABLE"),
            BanFolderInviteUsers = GetEnvironmentBool("DOORMAN_BAN_FOLDER_INVITE_USERS")
        };
    }

    /// <summary>
    /// Загружает опции пороговых значений из переменных окружения
    /// </summary>
    public static ViolationThresholdOptions LoadViolationThresholdOptions()
    {
        return new ViolationThresholdOptions
        {
            MlViolationsBeforeBan = GetEnvironmentInt("DOORMAN_ML_VIOLATIONS_BEFORE_BAN", 0),
            StopWordsViolationsBeforeBan = GetEnvironmentInt("DOORMAN_STOP_WORDS_VIOLATIONS_BEFORE_BAN", 0),
            EmojiViolationsBeforeBan = GetEnvironmentInt("DOORMAN_EMOJI_VIOLATIONS_BEFORE_BAN", 0),
            LookalikeViolationsBeforeBan = GetEnvironmentInt("DOORMAN_LOOKALIKE_VIOLATIONS_BEFORE_BAN", 0),
            BoringGreetingsViolationsBeforeBan = GetEnvironmentInt("DOORMAN_BORING_GREETINGS_VIOLATIONS_BEFORE_BAN", 0),
            CaptchaViolationsBeforeBan = GetEnvironmentInt("DOORMAN_CAPTCHA_VIOLATIONS_BEFORE_BAN", 0)
        };
    }

    /// <summary>
    /// Загружает опции переключателей функций из переменных окружения
    /// </summary>
    public static FeatureToggleOptions LoadFeatureToggleOptions()
    {
        return new FeatureToggleOptions
        {
            LowConfidenceHamForward = GetEnvironmentBool("DOORMAN_LOW_CONFIDENCE_HAM_ENABLE"),
            ApproveButtonEnabled = GetEnvironmentBool("DOORMAN_APPROVE_BUTTON"),
            DeleteForwardedMessages = GetEnvironmentBool("DOORMAN_DELETE_FORWARDED_MESSAGES"),
            TextMentionFilterEnabled = GetEnvironmentBool("DOORMAN_TEXT_MENTION_FILTER_ENABLE"),
            DisableWelcome = GetEnvironmentBool("DOORMAN_DISABLE_WELCOME"),
            DisableMediaFiltering = GetEnvironmentBool("DOORMAN_DISABLE_MEDIA_FILTERING"),
            GlobalApprovalMode = !GetEnvironmentBool("DOORMAN_GROUP_APPROVAL_MODE"),
            RepeatedViolationsBanToAdminChat = GetEnvironmentBool("DOORMAN_REPEATED_VIOLATIONS_BAN_TO_ADMIN_CHAT")
        };
    }

    /// <summary>
    /// Загружает опции фильтрации чатов из переменных окружения
    /// </summary>
    public static ChatFilteringOptions LoadChatFilteringOptions()
    {
        return new ChatFilteringOptions
        {
            MediaFilteringDisabledChats = GetEnvironmentChatList("DOORMAN_MEDIA_FILTERING_DISABLED_CHATS")
        };
    }
}