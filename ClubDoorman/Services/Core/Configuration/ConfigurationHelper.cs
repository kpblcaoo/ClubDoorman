using ClubDoorman.Services.ClickHouse;

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
            .Select(x => x!.Value)
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

    /// <summary>
    /// Загружает настройки RabbitMQ для конвейера сообщений.
    /// </summary>
    public static RabbitMqOptions LoadRabbitMqOptions()
    {
        var prefetch = GetEnvironmentInt("DOORMAN_RABBITMQ__PREFETCH", 50);
        if (prefetch <= 0)
        {
            prefetch = 50;
        }
        if (prefetch > ushort.MaxValue)
        {
            prefetch = ushort.MaxValue;
        }

        var publishTimeout = GetEnvironmentInt("DOORMAN_RABBITMQ__PUBLISH_TIMEOUT_SECONDS", 5);
        if (publishTimeout <= 0)
        {
            publishTimeout = 5;
        }

        return new RabbitMqOptions
        {
            Enabled = GetEnvironmentBool("DOORMAN_RABBITMQ__ENABLED"),
            Uri = Environment.GetEnvironmentVariable("DOORMAN_RABBITMQ__URI"),
            InputQueue = Environment.GetEnvironmentVariable("DOORMAN_RABBITMQ__INPUT_QUEUE") ?? "spampyre.pipeline.input",
            DeadLetterQueue = Environment.GetEnvironmentVariable("DOORMAN_RABBITMQ__DLQ") ?? "spampyre.pipeline.dlq",
            PrefetchCount = (ushort)prefetch,
            PublishTimeoutSeconds = publishTimeout,
            EventExchange = Environment.GetEnvironmentVariable("DOORMAN_RABBITMQ__EVENT_EXCHANGE") ?? "spampyre.events"
        };
    }

    /// <summary>
    /// Загружает настройки ClickHouse для аналитики.
    /// </summary>
    public static ClickHouseOptions LoadClickHouseOptions()
    {
        var options = new ClickHouseOptions
        {
            Enabled = GetEnvironmentBool("DOORMAN_CLICKHOUSE__ENABLED"),
            Url = Environment.GetEnvironmentVariable("DOORMAN_CLICKHOUSE__URL"),
            Database = Environment.GetEnvironmentVariable("DOORMAN_CLICKHOUSE__DATABASE") ?? "tg",
            RawTable = Environment.GetEnvironmentVariable("DOORMAN_CLICKHOUSE__RAW_TABLE") ?? "tg.messages_raw",
            IngestSource = Environment.GetEnvironmentVariable("DOORMAN_CLICKHOUSE__INGEST_SOURCE") ?? "live",
            BatchSize = GetEnvironmentInt("DOORMAN_CLICKHOUSE__BATCH_SIZE", 500),
            FlushIntervalMilliseconds = GetEnvironmentInt("DOORMAN_CLICKHOUSE__FLUSH_MS", 500),
            ChannelCapacity = GetEnvironmentInt("DOORMAN_CLICKHOUSE__CHANNEL_CAPACITY", 5000),
            MaxRetryAttempts = GetEnvironmentInt("DOORMAN_CLICKHOUSE__MAX_RETRY", 3),
            RetryDelaySeconds = GetEnvironmentInt("DOORMAN_CLICKHOUSE__RETRY_DELAY", 2),
            HttpTimeoutSeconds = GetEnvironmentInt("DOORMAN_CLICKHOUSE__HTTP_TIMEOUT", 10),
            Username = Environment.GetEnvironmentVariable("DOORMAN_CLICKHOUSE__USERNAME"),
            Password = Environment.GetEnvironmentVariable("DOORMAN_CLICKHOUSE__PASSWORD"),
            IncludePrivateChats = GetEnvironmentBool("DOORMAN_CLICKHOUSE__INCLUDE_PRIVATE")
        };

        options.Normalize();
        return options;
    }

    /// <summary>
    /// Загружает базовые core опции
    /// </summary>
    public static CoreOptions LoadCoreOptions()
    {
        // Bot API: если значение test-bot-token или пусто -> null чтобы позже отловить
        var botToken = Environment.GetEnvironmentVariable("DOORMAN_BOT_API");
        if (string.IsNullOrEmpty(botToken) || botToken == "test-bot-token")
            botToken = string.Empty; // сохраняем пустую строку как сигнал отсутствия

        var adminChat = Environment.GetEnvironmentVariable("DOORMAN_ADMIN_CHAT");
        long adminChatId = 123456789;
        if (!string.IsNullOrEmpty(adminChat) && long.TryParse(adminChat, out var parsedAdmin))
            adminChatId = parsedAdmin;

        var logChatVar = Environment.GetEnvironmentVariable("DOORMAN_LOG_ADMIN_CHAT");
        long logAdminChatId = 0;
        if (!string.IsNullOrEmpty(logChatVar) && long.TryParse(logChatVar, out var parsedLog))
            logAdminChatId = parsedLog;

        var clubToken = Environment.GetEnvironmentVariable("DOORMAN_CLUB_SERVICE_TOKEN");
        var clubUrl = Environment.GetEnvironmentVariable("DOORMAN_CLUB_URL") ?? "https://vas3k.club/";
        if (!clubUrl.EndsWith('/'))
            clubUrl += '/';
        if (!Uri.IsWellFormedUriString(clubUrl, UriKind.Absolute))
            clubUrl = "https://vas3k.club/";

        return new CoreOptions
        {
            BotApi = botToken,
            AdminChatId = adminChatId,
            LogAdminChatId = logAdminChatId,
            ClubServiceToken = clubToken,
            ClubUrl = clubUrl
        };
    }

    /// <summary>
    /// Загружает настройки доступа к чатам
    /// </summary>
    public static ChatAccessOptions LoadChatAccessOptions()
    {
        return new ChatAccessOptions
        {
            DisabledChats = GetEnvironmentChatList("DOORMAN_DISABLED_CHATS"),
            WhitelistChats = GetEnvironmentChatList("DOORMAN_WHITELIST"),
            NoVpnAdGroups = GetEnvironmentChatList("NO_VPN_AD_GROUPS"),
            NoCaptchaGroups = GetEnvironmentChatList("DOORMAN_NO_CAPTCHA_GROUPS")
        };
    }

    /// <summary>
    /// Загружает AI / suspicious настройки
    /// </summary>
    public static AiOptions LoadAiOptions()
    {
        var apiKey = Environment.GetEnvironmentVariable("DOORMAN_OPENROUTER_API");
        if (string.IsNullOrEmpty(apiKey) || apiKey == "test-api-key")
            apiKey = null;

        double mimicryThreshold = 0.7;
        var mimicryStr = Environment.GetEnvironmentVariable("DOORMAN_MIMICRY_THRESHOLD");
        if (!string.IsNullOrEmpty(mimicryStr) && double.TryParse(mimicryStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedThreshold))
            mimicryThreshold = parsedThreshold;

        int suspiciousCount = GetEnvironmentInt("DOORMAN_SUSPICIOUS_TO_APPROVED_COUNT", 3);

        return new AiOptions
        {
            OpenRouterApi = apiKey,
            SuspiciousDetectionEnabled = GetEnvironmentBool("DOORMAN_SUSPICIOUS_DETECTION_ENABLE"),
            MimicryThreshold = mimicryThreshold,
            SuspiciousToApprovedMessageCount = suspiciousCount,
            AiEnabledChats = GetEnvironmentChatList("DOORMAN_AI_ENABLED_CHATS")
        };
    }

    /// <summary>
    /// Загружает настройки тестового / golden режима
    /// </summary>
    public static TestHarnessOptions LoadTestHarnessOptions()
    {
        var golden = Environment.GetEnvironmentVariable("DOORMAN_GOLDEN_BASELINE") == "1";
        var raw = Environment.GetEnvironmentVariable("DOORMAN_TEST_BLACKLIST_IDS");
        var set = new HashSet<long>();
        if (!string.IsNullOrWhiteSpace(raw))
        {
            foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (long.TryParse(part.Trim(), out var id))
                    set.Add(id);
            }
        }
        return new TestHarnessOptions
        {
            GoldenBaselineMode = golden,
            TestBlacklistUserIds = set
        };
    }
}