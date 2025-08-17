using ClubDoorman.Infrastructure;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Messaging;
using Microsoft.Extensions.Options;

/// <summary>
/// Реализация конфигурации приложения
/// Переносит логику из статического класса Config.cs для лучшей тестируемости
/// </summary>
public class AppConfig : IAppConfig
{
    private readonly IOptions<AutoBanOptions> _autoBanOptions;
    private readonly IOptions<ViolationThresholdOptions> _violationThresholdOptions;
    private readonly IOptions<FeatureToggleOptions> _featureToggleOptions;
    private readonly IOptions<ChatFilteringOptions> _chatFilteringOptions;

    public AppConfig(
        IOptions<AutoBanOptions> autoBanOptions,
        IOptions<ViolationThresholdOptions> violationThresholdOptions,
        IOptions<FeatureToggleOptions> featureToggleOptions,
        IOptions<ChatFilteringOptions> chatFilteringOptions)
    {
        _autoBanOptions = autoBanOptions;
        _violationThresholdOptions = violationThresholdOptions;
        _featureToggleOptions = featureToggleOptions;
        _chatFilteringOptions = chatFilteringOptions;
    Effects = new EffectsConfiguration();
    }

    /// <summary>
    /// API токен для OpenRouter
    /// </summary>
    public string? OpenRouterApi => Config.OpenRouterApi;

    /// <summary>
    /// Включено ли обнаружение подозрительных пользователей
    /// </summary>
    public bool SuspiciousDetectionEnabled => Config.SuspiciousDetectionEnabled;

    /// <summary>
    /// Порог мимикрии для обнаружения подозрительных пользователей
    /// </summary>
    public double MimicryThreshold => Config.MimicryThreshold;

    /// <summary>
    /// Количество сообщений для перехода из подозрительных в одобренные
    /// </summary>
    public int SuspiciousToApprovedMessageCount => Config.SuspiciousToApprovedMessageCount;

    /// <summary>
    /// ID админского чата
    /// </summary>
    public long AdminChatId => Config.AdminChatId;

    /// <summary>
    /// ID чата для логирования
    /// </summary>
    public long LogAdminChatId => Config.LogAdminChatId;

    /// <summary>
    /// Список чатов с включенным AI
    /// </summary>
    public HashSet<long> AiEnabledChats => Config.AiEnabledChats;

    /// <summary>
    /// Включен ли AI для конкретного чата
    /// </summary>
    public bool IsAiEnabledForChat(long chatId) => Config.IsAiEnabledForChat(chatId);

    /// <summary>
    /// Разрешён ли чат для работы бота
    /// </summary>
    public bool IsChatAllowed(long chatId) => Config.IsChatAllowed(chatId);

    /// <summary>
    /// Разрешён ли приватный старт
    /// </summary>
    public bool IsPrivateStartAllowed() => Config.IsPrivateStartAllowed();

    /// <summary>
    /// API токен бота Telegram
    /// </summary>
    public string BotApi => Config.BotApi;

    /// <summary>
    /// Токен сервиса клуба
    /// </summary>
    public string? ClubServiceToken => Config.ClubServiceToken;

    /// <summary>
    /// URL клуба
    /// </summary>
    public string ClubUrl => Config.ClubUrl;

    /// <summary>
    /// Отключенные чаты
    /// </summary>
    public HashSet<long> DisabledChats => Config.DisabledChats;

    /// <summary>
    /// Whitelist групп - если указан, бот работает только в этих группах
    /// </summary>
    public HashSet<long> WhitelistChats => Config.WhitelistChats;

    /// <summary>
    /// Группы, где не показывать рекламу
    /// </summary>
    public HashSet<long> NoVpnAdGroups => Config.NoVpnAdGroups;

    /// <summary>
    /// Группы, в которых отключена капча
    /// </summary>
    public HashSet<long> NoCaptchaGroups => Config.NoCaptchaGroups;

    /// <summary>
    /// Включен ли фильтр ссылок
    /// </summary>
    public bool TextMentionFilterEnabled => Config.TextMentionFilterEnabled;

    /// <summary>
    /// Автоматически банить пользователей, входящих через папки
    /// </summary>
    public bool BanFolderInviteUsers => Config.BanFolderInviteUsers;

    /// <summary>
    /// Количество повторных нарушений ML фильтра перед баном
    /// </summary>
    public int MlViolationsBeforeBan => Config.MlViolationsBeforeBan;

    /// <summary>
    /// Количество повторных нарушений стоп-слов перед баном
    /// </summary>
    public int StopWordsViolationsBeforeBan => Config.StopWordsViolationsBeforeBan;

    /// <summary>
    /// Количество повторных нарушений эмодзи перед баном
    /// </summary>
    public int EmojiViolationsBeforeBan => Config.EmojiViolationsBeforeBan;

    /// <summary>
    /// Количество повторных нарушений lookalike символов перед баном
    /// </summary>
    public int LookalikeViolationsBeforeBan => Config.LookalikeViolationsBeforeBan;

    /// <summary>
    /// Количество повторных нарушений банальных приветствий перед баном
    /// </summary>
    public int BoringGreetingsViolationsBeforeBan => Config.BoringGreetingsViolationsBeforeBan;

    /// <summary>
    /// Количество непройденных капч перед баном
    /// </summary>
    public int CaptchaViolationsBeforeBan => Config.CaptchaViolationsBeforeBan;

    /// <summary>
    /// Отправлять уведомления о банах за повторные нарушения в админ-чат вместо лог-чата
    /// </summary>
    public bool RepeatedViolationsBanToAdminChat => Config.RepeatedViolationsBanToAdminChat;

    // === НОВЫЕ СВОЙСТВА ИЗ STRONGLY-TYPED OPTIONS ===

    /// <summary>
    /// Автоматически банить пользователей из черного списка
    /// </summary>
    public bool BlacklistAutoBan => _autoBanOptions.Value.BlacklistAutoBan;

    /// <summary>
    /// Автоматически банить каналы
    /// </summary>
    public bool ChannelAutoBan => _autoBanOptions.Value.ChannelAutoBan;

    /// <summary>
    /// Включить effects-пайплайн для сообщений от каналов
    /// </summary>
    public bool ChannelEffectsEnabled => Config.ChannelEffectsEnabled;

    /// <summary>
    /// Автоматически банить пользователей с похожими именами
    /// </summary>
    public bool LookAlikeAutoBan => _autoBanOptions.Value.LookAlikeAutoBan;

    /// <summary>
    /// Автоматически банить по кнопкам
    /// </summary>
    public bool ButtonAutoBan => _autoBanOptions.Value.ButtonAutoBan;

    /// <summary>
    /// Автоматически банить при высокой уверенности
    /// </summary>
    public bool HighConfidenceAutoBan => _autoBanOptions.Value.HighConfidenceAutoBan;

    /// <summary>
    /// Пересылать сообщения с низкой уверенностью в ham
    /// </summary>
    public bool LowConfidenceHamForward => _featureToggleOptions.Value.LowConfidenceHamForward;

    /// <summary>
    /// Включить кнопку одобрения
    /// </summary>
    public bool ApproveButtonEnabled => _featureToggleOptions.Value.ApproveButtonEnabled;

    /// <summary>
    /// Удаление пересланных сообщений от новичков
    /// </summary>
    public bool DeleteForwardedMessages => _featureToggleOptions.Value.DeleteForwardedMessages;

    /// <summary>
    /// Отключить приветственные сообщения
    /// </summary>
    public bool DisableWelcome => _featureToggleOptions.Value.DisableWelcome;

    /// <summary>
    /// Отключить фильтрацию картинок/видео/документов глобально
    /// </summary>
    public bool DisableMediaFiltering => _featureToggleOptions.Value.DisableMediaFiltering;

    /// <summary>
    /// Режим автоодобрения пользователей (true = глобальный, false = групповой)
    /// </summary>
    public bool GlobalApprovalMode => _featureToggleOptions.Value.GlobalApprovalMode;

    /// <summary>
    /// Группы где фильтрация медиа отключена
    /// </summary>
    public HashSet<long> MediaFilteringDisabledChats => _chatFilteringOptions.Value.MediaFilteringDisabledChats;

    /// <summary>
    /// Проверяет, отключена ли фильтрация медиа для данного чата
    /// </summary>
    public bool IsMediaFilteringDisabledForChat(long chatId)
    {
        // Если глобально отключено - фильтрация отключена везде
        if (DisableMediaFiltering)
            return true;

        // Если чат в списке исключений - фильтрация отключена
        return MediaFilteringDisabledChats.Contains(chatId);
    }

    // === КОНФИГУРАЦИЯ ЭФФЕКТОВ МОДЕРАЦИИ ===

    /// <summary>
    /// Конфигурация эффектов модерации
    /// </summary>
    public EffectsConfiguration Effects { get; }

    public AppConfig(
        IOptions<AutoBanOptions> autoBanOptions,
        IOptions<ViolationThresholdOptions> violationThresholdOptions,
        IOptions<FeatureToggleOptions> featureToggleOptions,
        IOptions<ChatFilteringOptions> chatFilteringOptions,
        EffectsConfiguration effectsConfiguration) // Добавляем инъекцию
    {
        _autoBanOptions = autoBanOptions;
        _violationThresholdOptions = violationThresholdOptions;
        _featureToggleOptions = featureToggleOptions;
        _chatFilteringOptions = chatFilteringOptions;
        Effects = effectsConfiguration; // Используем DI конфигурацию
    }
}