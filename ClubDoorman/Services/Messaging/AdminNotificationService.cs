using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Runtime.Caching;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Extensions;

namespace ClubDoorman.Services.Messaging;

/// <summary>
/// Сервис для отправки уведомлений админам и управления сообщениями
/// </summary>
public class AdminNotificationService : IAdminNotificationService
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IMessageService _messageService;
    private readonly IAppConfig _appConfig;
    private readonly ILogger<AdminNotificationService> _logger;

    public AdminNotificationService(
        ITelegramBotClientWrapper bot,
        IMessageService messageService,
        IAppConfig appConfig,
        ILogger<AdminNotificationService> logger)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task DeleteAndReportMessageAsync(Message message, string reason, bool isSilentMode, CancellationToken cancellationToken)
    {
        _logger.LogWarning("🚀 НОВЫЙ КОД DeleteAndReportMessage v2.0 для сообщения {MessageId} в чате {ChatId}", message.MessageId, message.Chat.Id);
        
        var user = message.From;
        var deletionMessagePart = $"{reason}";

        try
        {
            // ЭТАП 1: Пересылаем сообщение в админ-чат (делаем это первым, чтобы избежать race condition)
            var callbackDataBan = $"ban_{message.Chat.Id}_{user.Id}";
            MemoryCache.Default.Add(callbackDataBan, message, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(12) });
            
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    new InlineKeyboardButton("🤖 бан") { CallbackData = callbackDataBan },
                    new InlineKeyboardButton("😶 пропуск") { CallbackData = "noop" },
                    new InlineKeyboardButton("🥰 свой") { CallbackData = $"approve_{user.Id}" }
                }
            });

            var deletionData = new AutoBanNotificationData(
                user, 
                message.Chat, 
                deletionMessagePart, 
                reason, 
                message.MessageId, 
                LinkToMessage(message.Chat, message.MessageId)
            );
            
            // Получаем шаблон и форматируем сообщение
            var template = _messageService.GetTemplates().GetAdminTemplate(AdminNotificationType.AutoBan);
            var messageText = _messageService.GetTemplates().FormatNotificationTemplate(template, deletionData);
            
            // Добавляем префикс тихого режима если нужно
            if (isSilentMode)
            {
                messageText = $"🔇 <b>Тихий режим</b>\n\n{messageText}";
            }
            
            // Пытаемся переслать сообщение, но если не получается - отправляем без пересылки
            Message? forwardedMessage = null;
            try
            {
                _logger.LogDebug("🔍 ПЕРЕСЫЛКА: из чата {FromChatId} сообщение {MessageId} в админ-чат {AdminChatId}", message.Chat.Id, message.MessageId, _appConfig.AdminChatId);
                
                // Пересылаем сообщение и сохраняем ссылку на него
                forwardedMessage = await _bot.ForwardMessage(
                    new ChatId(_appConfig.AdminChatId),
                    message.Chat.Id,
                    message.MessageId,
                    cancellationToken: cancellationToken
                );
                
                _logger.LogDebug("✅ Сообщение успешно переслано, ждем 150мс перед отправкой уведомления");
                await Task.Delay(150, cancellationToken); // Задержка между пересылкой и уведомлением
                
                // Отправляем уведомление с кнопками как реплай на пересланное сообщение
                await _bot.SendMessage(
                    _appConfig.AdminChatId,
                    messageText,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard,
                    replyParameters: forwardedMessage,
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception forwardEx) when (forwardEx.Message.Contains("protected content") || forwardEx.Message.Contains("can't be forwarded"))
            {
                _logger.LogWarning("❌ Чат '{ChatTitle}' имеет защищенный контент, отправляем расширенное уведомление: {Error}", message.Chat.Title, forwardEx.Message);
                
                // Добавляем контент сообщения в уведомление, раз не можем переслать
                var extendedMessageText = $"{messageText}\n\n" +
                    $"📝 <b>Содержимое:</b>\n<code>{System.Net.WebUtility.HtmlEncode(message.Text?.Length > 500 ? message.Text[..500] + "..." : message.Text)}</code>";
                
                // Отправляем расширенное уведомление без пересылки
                await _bot.SendMessage(
                    _appConfig.AdminChatId,
                    extendedMessageText,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken
                );
            }
            
            _logger.LogDebug("Уведомление с кнопками успешно отправлено в админ-чат");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Не удалось отправить уведомление в админ-чат");
        }

        // ЭТАП 2: Увеличенная задержка для избежания race condition с Telegram API
        try
        {
            await Task.Delay(200, cancellationToken); // 200мс задержка (было 50мс)
            _logger.LogDebug("Выполнена задержка 200мс между пересылкой и предупреждением");
        }
        catch (OperationCanceledException)
        {
            // Игнорируем отмену операции
        }

        // ЭТАП 3: Отправляем предупреждение пользователю как реплай на оригинальное сообщение
        Message? warningMessage = null;
        if (!isSilentMode)
        {
            var warningKey = $"warning_{message.Chat.Id}_{user.Id}";
            var existingWarning = MemoryCache.Default.Get(warningKey);
            
            if (existingWarning == null)
            {
                try
                {
                    var warningData = new SimpleNotificationData(user, message.Chat, reason);
                    // Отправляем стандартное предупреждение новичку как реплай на сообщение, которое будет удалено
                    var replyParams = new ReplyParameters { MessageId = message.MessageId };
                    _logger.LogDebug("Отправляем предупреждение с реплаем на сообщение {MessageId} в чате {ChatId}", message.MessageId, message.Chat.Id);
                    
                    warningMessage = await _messageService.SendUserNotificationWithReplyAsync(
                        user, 
                        message.Chat, 
                        UserNotificationType.ModerationWarning, 
                        warningData, 
                        replyParams,
                        cancellationToken
                    );
                    
                    // Сохраняем ID предупреждающего сообщения в кэше (на 10 минут, чтобы не спамить)
                    MemoryCache.Default.Add(warningKey, warningMessage.MessageId, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(10) });
                    
                    DeleteMessageLater(warningMessage, TimeSpan.FromSeconds(40), cancellationToken);
                    _logger.LogDebug("Предупреждение отправлено пользователю и будет удалено через 40 секунд");
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Не удалось отправить предупреждение пользователю");
                }
            }
            else
            {
                _logger.LogDebug("Предупреждение пользователю {UserId} в чате {ChatId} уже было отправлено недавно, пропускаем", user.Id, message.Chat.Id);
            }
        }

        // ЭТАП 4: Увеличенная задержка перед удалением
        try
        {
            await Task.Delay(500, cancellationToken); // 500мс задержка для корректной обработки реплая
            _logger.LogDebug("Выполнена задержка 500мс между предупреждением и удалением");
        }
        catch (OperationCanceledException)
        {
            // Игнорируем отмену операции
        }
        
        // ЭТАП 5: Удаляем оригинальное сообщение
        try
        {
            _logger.LogDebug("Пытаемся удалить сообщение {MessageId} из чата {ChatId}", message.MessageId, message.Chat.Id);
            await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken: cancellationToken);
            deletionMessagePart += ", сообщение удалено.";
            _logger.LogDebug("Сообщение успешно удалено");
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to delete message {MessageId} from chat {ChatId}", message.MessageId, message.Chat.Id);
            deletionMessagePart += ", сообщение НЕ удалено (не хватило могущества?).";
        }
    }

    public async Task DontDeleteButReportMessageAsync(Message message, User user, bool isSilentMode, CancellationToken cancellationToken)
    {
        try
        {
            var suspiciousData = new SuspiciousMessageNotificationData(
                user, 
                message.Chat, 
                message.Text ?? message.Caption ?? "[медиа]", 
                message.MessageId
            );
            
            // Отправляем уведомление с кнопками для подозрительного сообщения (форвард делается внутри)
            await SendSuspiciousMessageWithButtonsAsync(message, user, suspiciousData, isSilentMode, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Ошибка при пересылке сообщения");
            var errorData = new ErrorNotificationData(
                new InvalidOperationException("Не удалось переслать подозрительное сообщение"),
                "Ошибка пересылки",
                user,
                message.Chat
            );
            await _messageService.SendAdminNotificationAsync(AdminNotificationType.SystemError, errorData, cancellationToken);
        }
    }
    
    public async Task SendSuspiciousMessageWithButtonsAsync(Message message, User user, SuspiciousMessageNotificationData data, bool isSilentMode, CancellationToken cancellationToken)
    {
        try
        {
            var template = _messageService.GetTemplates().GetAdminTemplate(AdminNotificationType.SuspiciousMessage);
            var messageText = _messageService.GetTemplates().FormatNotificationTemplate(template, data);
            
            // Добавляем префикс тихого режима если нужно
            if (isSilentMode)
            {
                messageText = $"🔇 **Тихий режим**\n\n{messageText}";
            }
            
            // Создаем кнопки реакции для админ-чата (стандартные кнопки: бан, пропуск, свой)
            var callbackDataBan = $"ban_{message.Chat.Id}_{user.Id}";
            MemoryCache.Default.Add(callbackDataBan, message, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(12) });
            
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    new InlineKeyboardButton("🤖 бан") { CallbackData = callbackDataBan },
                    new InlineKeyboardButton("😶 пропуск") { CallbackData = "noop" },
                    new InlineKeyboardButton("🥰 свой") { CallbackData = $"approve_{user.Id}" }
                }
            });
            
            // ФИКС: Пытаемся переслать сообщение, но если не получается - отправляем без пересылки
            Message? forward = null;
            try
            {
                // Сначала пересылаем оригинальное сообщение
                forward = await _bot.ForwardMessage(
                    new ChatId(_appConfig.AdminChatId),
                    message.Chat.Id,
                    message.MessageId,
                    cancellationToken: cancellationToken
                );
                
                // Отправляем уведомление с кнопками как ответ на форвард
                await _bot.SendMessage(
                    _appConfig.AdminChatId,
                    messageText,
                    parseMode: ParseMode.Html,
                    replyParameters: forward,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception forwardEx) when (forwardEx.Message.Contains("protected content") || forwardEx.Message.Contains("can't be forwarded"))
            {
                _logger.LogWarning("Сообщение имеет защищенный контент, отправляем уведомление без пересылки: {Error}", forwardEx.Message);
                
                // Отправляем уведомление без пересылки (просто как обычное сообщение)
                await _bot.SendMessage(
                    _appConfig.AdminChatId,
                    messageText,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken
                );
            }
            
            _logger.LogDebug("Отправлено подозрительное сообщение с кнопками для пользователя {User} в чате {Chat}", 
                Utils.FullName(user), message.Chat.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке подозрительного сообщения с кнопками");
            // Fallback: отправляем без кнопок
            await _messageService.SendAdminNotificationAsync(AdminNotificationType.SuspiciousMessage, data, cancellationToken);
        }
    }

    public void DeleteMessageLater(Message message, TimeSpan after = default, CancellationToken cancellationToken = default)
    {
        if (after == default)
            after = TimeSpan.FromMinutes(5);
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await Task.Delay(after, cancellationToken);
                    await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken: cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "DeleteMessage");
                }
            },
            cancellationToken
        );
    }

    private static string LinkToMessage(Chat chat, long messageId) =>
        chat.Type == ChatType.Supergroup ? LinkToSuperGroupMessage(chat, messageId)
        : chat.Username == null ? ""
        : LinkToGroupWithNameMessage(chat, messageId);

    private static string LinkToSuperGroupMessage(Chat chat, long messageId) => $"https://t.me/c/{chat.Id.ToString()[4..]}/{messageId}";

    private static string LinkToGroupWithNameMessage(Chat chat, long messageId) => $"https://t.me/{chat.Username}/{messageId}";
}