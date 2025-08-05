using ClubDoorman.Infrastructure;
using ClubDoorman.Models;
using ClubDoorman.Services.BanSystem;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Services;

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
        ILogger<ChannelModerationService> logger)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _moderationService = moderationService ?? throw new ArgumentNullException(nameof(moderationService));
        _userBanService = userBanService ?? throw new ArgumentNullException(nameof(userBanService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        // Проверяем, следует ли разрешить сообщение без модерации
        if (await ShouldAllowChannelMessageAsync(message, cancellationToken))
        {
            _logger.LogDebug("✅ Сообщение от канала {ChannelTitle} разрешено без модерации", senderChat.Title);
            return;
        }

        // Автобан каналов если включен
        if (Config.ChannelAutoBan)
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
            
            await ModerateChannelMessageContentAsync(message, cancellationToken);
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

    /// <summary>
    /// Модерирует содержимое сообщения от канала
    /// <tags>channel, moderation, content</tags>
    /// </summary>
    /// <param name="message">Сообщение от канала</param>
    /// <param name="cancellationToken">Токен отмены</param>
    private async Task ModerateChannelMessageContentAsync(Message message, CancellationToken cancellationToken = default)
    {
        try
        {
            var senderChat = message.SenderChat!;
            var chat = message.Chat;
            
            _logger.LogInformation("🔍 Модерируем содержимое сообщения от канала {ChannelTitle} в чате {ChatTitle}", 
                senderChat.Title, chat.Title);
            
            // Проверяем содержимое сообщения через ModerationService
            var moderationResult = await _moderationService.CheckMessageAsync(message);
            
            switch (moderationResult.Action)
            {
                case ModerationAction.Allow:
                    _logger.LogDebug("✅ Содержимое сообщения от канала {ChannelTitle} разрешено: {Reason}", 
                        senderChat.Title, moderationResult.Reason);
                    
                    // Засчитываем хорошее сообщение для одобрения пользователя
                    if (message.From != null)
                    {
                        var allowedMessageText = message.Text ?? message.Caption ?? "";
                        
                        // Проверяем AI детект для подозрительных пользователей
                        var aiDetectBlocked = await _moderationService.CheckAiDetectAndNotifyAdminsAsync(message.From, chat, message);
                        
                        // Засчитываем хорошее сообщение только если пользователь не был заблокирован AI детектом
                        if (!aiDetectBlocked)
                        {
                            await _moderationService.IncrementGoodMessageCountAsync(message.From, chat, allowedMessageText);
                        }
                    }
                    break;
                
                case ModerationAction.Delete:
                    _logger.LogInformation("🗑️ Удаляем сообщение от канала {ChannelTitle}: {Reason}", 
                        senderChat.Title, moderationResult.Reason);
                    await _bot.DeleteMessage(chat.Id, message.MessageId, cancellationToken);
                    break;
                
                case ModerationAction.Report:
                    _logger.LogInformation("📋 Отправляем сообщение от канала {ChannelTitle} в админ-чат: {Reason}", 
                        senderChat.Title, moderationResult.Reason);
                    // Здесь можно добавить отправку в админ-чат
                    break;
                
                case ModerationAction.Ban:
                    _logger.LogWarning("🚫 Баним канал {ChannelTitle} за содержимое: {Reason}", 
                        senderChat.Title, moderationResult.Reason);
                    await _userBanService.AutoBanChannelAsync(message, cancellationToken);
                    break;
                
                default:
                    _logger.LogInformation("❓ Неизвестное действие модерации для канала {ChannelTitle}: {Action}", 
                        senderChat.Title, moderationResult.Action);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка при модерации содержимого сообщения от канала {ChannelTitle}", 
                message.SenderChat?.Title);
        }
    }
} 