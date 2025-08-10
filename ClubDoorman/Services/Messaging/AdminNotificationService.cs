using System.Runtime.Caching;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services.Messaging;

/// <summary>
/// Сервис для отправки уведомлений администраторам
/// </summary>
public class AdminNotificationService : IAdminNotificationService
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IMessageService _messageService;
    private readonly ILogChatService _logChatService;
    private readonly IAppConfig _appConfig;
    private readonly ILogger<AdminNotificationService> _logger;

    public AdminNotificationService(
        ITelegramBotClientWrapper bot,
        IMessageService messageService,
        ILogChatService logChatService,
        IAppConfig appConfig,
        ILogger<AdminNotificationService> logger)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _logChatService = logChatService ?? throw new ArgumentNullException(nameof(logChatService));
        _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Удаляет сообщение и отправляет уведомление в админ-чат
    /// </summary>
    public async Task DeleteAndReportMessageAsync(Message message, string reason, bool isSilentMode, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("🚀 НОВЫЙ КОД DeleteAndReportMessage v2.0 для сообщения {MessageId} в чате {ChatId}", message.MessageId, message.Chat.Id);
        
        var user = message.From;
        var deletionMessagePart = $"{reason}";

        try
        {
            // ЭТАП 1: Пересылаем сообщение в админ-чат (делаем это первым, чтобы избежать race condition)
            var callbackDataBan = $"ban_{message.Chat.Id}_{user.Id}";
            MemoryCache.Default.Add(callbackDataBan, message, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(12) });
            
            var keyboard = CreateAdminKeyboard(callbackDataBan, user.Id);
            var deletionData = CreateAutoBanNotificationData(user, message, deletionMessagePart, reason);
            
            // Получаем шаблон и форматируем сообщение
            var template = _messageService.GetTemplates().GetAdminTemplate(AdminNotificationType.AutoBan);
            var messageText = _messageService.GetTemplates().FormatNotificationTemplate(template, deletionData);
            
            // Добавляем префикс тихого режима если нужно
            if (isSilentMode)
            {
                messageText = $"🔇 <b>Тихий режим</b>\n\n{messageText}";
            }
            
            await ForwardMessageToAdminChatAsync(message, messageText, keyboard, cancellationToken);
            
            _logger.LogDebug("Уведомление с кнопками успешно отправлено в админ-чат");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Не удалось отправить уведомление в админ-чат");
        }

        // ЭТАП 2: Задержка для избежания race condition с Telegram API
        try
        {
            await Task.Delay(200, cancellationToken); // 200мс задержка
            await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken);
            _logger.LogInformation("🗑️ Сообщение {MessageId} удалено из чата {ChatId}", message.MessageId, message.Chat.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ Не удалось удалить сообщение {MessageId} из чата {ChatId}", message.MessageId, message.Chat.Id);
        }

        // ЭТАП 3: Отправляем уведомление пользователю
        await SendUserNotificationAsync(message, user, cancellationToken);
    }

    /// <summary>
    /// Отправляет сообщение в лог-чат без удаления
    /// </summary>
    public async Task DontDeleteButReportMessageAsync(Message message, User user, bool isSilentMode, CancellationToken cancellationToken = default)
    {
        // Используем сервис лог-чата для отправки уведомления
        await _logChatService.SendLogNotificationAsync(message, "🔍 Требует внимания - подозрительная активность", cancellationToken);

        _logger.LogDebug("Выполнена задержка 50мс между пересылкой и предупреждением");
        await Task.Delay(50, cancellationToken);
    }

    /// <summary>
    /// Удаляет сообщение и отправляет только в лог-чат (без предупреждения пользователю)
    /// </summary>
    public async Task DeleteAndReportToLogChatAsync(Message message, string reason, CancellationToken cancellationToken = default)
    {
        // Используем сервис лог-чата для отправки уведомления
        await _logChatService.SendLogNotificationAsync(message, reason, cancellationToken);
        
        try
        {
            await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken);
            _logger.LogInformation("🗑️ Сообщение удалено: {Reason}", reason);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось удалить сообщение");
        }
    }

    private InlineKeyboardMarkup CreateAdminKeyboard(string callbackDataBan, long userId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                new InlineKeyboardButton("🤖 бан") { CallbackData = callbackDataBan },
                new InlineKeyboardButton("😶 пропуск") { CallbackData = "noop" },
                new InlineKeyboardButton("🥰 свой") { CallbackData = $"approve_{userId}" }
            }
        });
    }

    private AutoBanNotificationData CreateAutoBanNotificationData(User user, Message message, string deletionMessagePart, string reason)
    {
        return new AutoBanNotificationData(
            user, 
            message.Chat, 
            deletionMessagePart, 
            reason, 
            message.MessageId, 
            LinkToMessage(message.Chat, message.MessageId)
        );
    }

    private string LinkToMessage(Chat chat, int messageId)
    {
        if (chat.Username != null)
        {
            return $"https://t.me/{chat.Username}/{messageId}";
        }

        var chatIdString = chat.Id.ToString();
        if (chatIdString.StartsWith("-100"))
        {
            chatIdString = chatIdString.Substring(4);
        }
        return $"https://t.me/c/{chatIdString}/{messageId}";
    }

    private async Task ForwardMessageToAdminChatAsync(Message message, string messageText, InlineKeyboardMarkup keyboard, CancellationToken cancellationToken)
    {
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
    }

    private async Task SendUserNotificationAsync(Message message, User user, CancellationToken cancellationToken)
    {
        try
        {
            Message? notificationMessage = null;
            
            try
            {
                notificationMessage = await _messageService.SendUserNotificationWithReplyAsync(user, message.Chat, UserNotificationType.MessageDeleted, 
                    message, cancellationToken: cancellationToken);
                _logger.LogDebug("Уведомление пользователю отправлено, ID сообщения: {MessageId}", notificationMessage?.MessageId);
                
                // Удаляем уведомление через 10 секунд
                if (notificationMessage != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(10000, cancellationToken);
                            await _bot.DeleteMessage(message.Chat.Id, notificationMessage.MessageId, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Не удалось удалить уведомление пользователю");
                        }
                    }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось отправить уведомление пользователю {UserId}", user.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке уведомления пользователю");
        }
    }
}