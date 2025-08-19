using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Infrastructure;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Models;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Messaging;
// Builder now resides in the same namespace folder (no .Effects subfolder)
using ClubDoorman.Effects;

namespace ClubDoorman.Services.ChannelModeration;

/// <summary>
/// Сервис для модерации каналов
/// <tags>channel, moderation, proxy</tags>
/// </summary>
public class ChannelModerationService : IChannelModerationService
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IModerationService _moderationService;
    private readonly IUserBanService _userBanService;
    private readonly ILogger<ChannelModerationService> _logger;
    private readonly IChannelModerationEffectsBuilder? _channelEffectsBuilder;
    private readonly IEffectBus? _effectBus;
    private readonly IAppConfig _appConfig;

    /// <summary>
    /// Создает экземпляр ChannelModerationService
    /// <tags>channel, constructor, dependency-injection</tags>
    /// </summary>
    /// <param name="bot">Клиент Telegram бота</param>
    /// <param name="moderationService">Сервис модерации</param>
    /// <param name="userBanService">Сервис управления банами</param>
    /// <param name="logger">Логгер</param>
    public ChannelModerationService(
        ITelegramBotClientWrapper bot,
        IModerationService moderationService,
        IUserBanService userBanService,
        ILogger<ChannelModerationService> logger,
        IChannelModerationEffectsBuilder? channelEffectsBuilder = null,
        IEffectBus? effectBus = null,
        IAppConfig? appConfig = null)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _moderationService = moderationService ?? throw new ArgumentNullException(nameof(moderationService));
        _userBanService = userBanService ?? throw new ArgumentNullException(nameof(userBanService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _channelEffectsBuilder = channelEffectsBuilder; // может быть null на ранних этапах миграции
        _effectBus = effectBus; // может быть null если эффекты отключены
        _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
    }

    /// <summary>
    /// Обрабатывает сообщение от канала
    /// <tags>channel, moderation, proxy</tags>
    /// </summary>
    /// <param name="message">Сообщение от канала</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task HandleChannelMessageAsync(Message message, CancellationToken cancellationToken = default)
    {
        var chat = message.Chat;
        var senderChat = message.SenderChat!;

        _logger.LogDebug("🔍 Обрабатываем сообщение от канала {ChannelTitle} в чате {ChatTitle}",
            senderChat.Title, chat.Title);

        // Разрешаем сообщения от самого чата
        if (senderChat.Id == chat.Id)
        {
            _logger.LogDebug("✅ Канал {ChannelTitle} является самим чатом - разрешаем", senderChat.Title);
            return;
        }

        // Разрешаем в announcement чатах
        if (ChatSettingsManager.GetChatType(chat.Id) == "announcement")
        {
            _logger.LogDebug("✅ Чат {ChatTitle} является announcement - разрешаем", chat.Title);
            return;
        }

        // Проверяем, следует ли разрешить сообщение без модерации (legacy bypass – сохраняем без изменений логики)
        if (await ShouldAllowChannelMessageAsync(message, cancellationToken))
        {
            _logger.LogDebug("✅ Сообщение от канала {ChannelTitle} разрешено без модерации", senderChat.Title);
            return; // В legacy не выполнялись side-effects / модерация для таких сообщений
        }

        // Автобан каналов если включен
    var channelAutoBan = _appConfig.ChannelAutoBan;
        if (channelAutoBan)
        {
            _logger.LogInformation("🚫 Автобан канала {ChannelTitle} включен - баним", senderChat.Title);
            await _userBanService.AutoBanChannelAsync(message, cancellationToken);
        }
        else
        {
            // Проверяем, одобрен ли пользователь (если есть информация о пользователе)
            if (message.From != null && _moderationService.IsUserApproved(message.From.Id, chat.Id))
            {
                _logger.LogDebug("✅ Пользователь {UserId} уже одобрен в чате {ChatId}, пропускаем модерацию канала {ChannelTitle}",
                    message.From.Id, chat.Id, senderChat.Title);
                return;
            }

            // Модерация содержимого для неизвестных каналов
            _logger.LogInformation("🔍 Модерация содержимого сообщения от канала {ChannelTitle} в чате {ChatTitle}",
                senderChat.Title, chat.Title);

            // Модерация содержимого (теперь только через effects pipeline; legacy switch удалён)
            _logger.LogInformation("🔍 Модерируем (effects) сообщение от канала {ChannelTitle} в чате {ChatTitle}",
                senderChat.Title, chat.Title);
            if (_channelEffectsBuilder == null || _effectBus == null)
            {
                _logger.LogError("[ChannelEffects] Builder или EffectBus не зарегистрированы – действие проигнорировано (legacy switch удалён)");
                return;
            }
            try
            {
                var moderationResult = await _moderationService.CheckMessageAsync(message);
                var effects = _channelEffectsBuilder.BuildChannelEffects(message, moderationResult);
                _logger.LogInformation("[ChannelEffects] Executing {Count} effect(s) (Action={Action})", effects.Length, moderationResult.Action);
                await _effectBus.ExecuteAsync(effects, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ChannelEffects] Ошибка при модерации канала (legacy path недоступен)");
            }
        }
    }

    /// <summary>
    /// Проверяет, является ли отправитель владельцем канала
    /// <tags>channel, owner, validation</tags>
    /// </summary>
    /// <param name="message">Сообщение от канала</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если отправитель является владельцем канала</returns>
    public async Task<bool> IsChannelOwnerAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.From == null)
        {
            _logger.LogDebug("❌ Сообщение без отправителя - не может быть владельцем канала");
            return false;
        }

        try
        {
            var senderChat = message.SenderChat!;
            var channelAdmins = await _bot.GetChatAdministratorsAsync(senderChat.Id, cancellationToken);

            var isOwner = channelAdmins.Any(admin =>
                admin.User.Id == message.From.Id &&
                admin.Status == ChatMemberStatus.Creator);

            if (isOwner)
            {
                _logger.LogDebug("✅ Пользователь {UserId} является владельцем канала {ChannelTitle}",
                    message.From.Id, senderChat.Title);
            }
            else
            {
                _logger.LogDebug("❌ Пользователь {UserId} не является владельцем канала {ChannelTitle}",
                    message.From.Id, senderChat.Title);
            }

            return isOwner;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Не удалось проверить владельца канала {ChannelId} - разрешаем сообщение", message.SenderChat?.Id);
            return false; // Если не можем проверить - проверяем дальше
        }
    }

    /// <summary>
    /// Проверяет, является ли чат обсуждением данного канала
    /// <tags>channel, discussion, linked</tags>
    /// </summary>
    /// <param name="message">Сообщение от канала</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если чат является обсуждением канала</returns>
    public async Task<bool> IsChannelDiscussionAsync(Message message, CancellationToken cancellationToken = default)
    {
        try
        {
            var chat = message.Chat;
            var senderChat = message.SenderChat!;

            // Проверяем, является ли это обсуждением канала
            if (chat.Type == ChatType.Supergroup && message.IsAutomaticForward)
            {
                _logger.LogDebug("✅ Чат {ChatTitle} является обсуждением канала {ChannelTitle}",
                    chat.Title, senderChat.Title);
                return true;
            }

            // Проверяем связанный чат через ChatFullInfo
            try
            {
                var chatFullInfo = await _bot.GetChatFullInfo(chat.Id, cancellationToken);
                if (chatFullInfo.LinkedChatId == senderChat.Id)
                {
                    _logger.LogDebug("✅ Чат {ChatTitle} связан с каналом {ChannelTitle}",
                        chat.Title, senderChat.Title);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Не удалось получить ChatFullInfo для чата {ChatId}", chat.Id);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Не удалось проверить связь чата {ChatId} с каналом {ChannelId}",
                message.Chat.Id, message.SenderChat?.Id);
            return false;
        }
    }

    /// <summary>
    /// Проверяет, следует ли разрешить сообщение от канала без модерации
    /// <tags>channel, moderation, allow</tags>
    /// </summary>
    /// <param name="message">Сообщение от канала</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если сообщение следует разрешить без модерации</returns>
    public async Task<bool> ShouldAllowChannelMessageAsync(Message message, CancellationToken cancellationToken = default)
    {
        // 1. Проверяем, является ли чат обсуждением канала
        if (await IsChannelDiscussionAsync(message, cancellationToken))
        {
            return true;
        }

        // 2. Проверяем, является ли отправитель владельцем канала
        if (await IsChannelOwnerAsync(message, cancellationToken))
        {
            return true;
        }

        return false;
    }

}