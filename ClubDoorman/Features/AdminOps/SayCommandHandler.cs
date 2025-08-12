using System.Runtime.Caching;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Telegram;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Features.AdminOps;

/// <summary>
/// Обработчик команды /say для отправки сообщений пользователям
/// </summary>
public class SayCommandHandler : ICommandHandler
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IMessageService _messageService;
    private readonly IAppConfig _appConfig;
    private readonly ILogger<SayCommandHandler> _logger;

    public string CommandName => "say";

    public SayCommandHandler(
        ITelegramBotClientWrapper bot,
        IMessageService messageService,
        IAppConfig appConfig,
        ILogger<SayCommandHandler> logger)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        // Проверяем, что команда пришла из админ-чата
        var isAdminChat = message.Chat.Id == _appConfig.AdminChatId || message.Chat.Id == _appConfig.LogAdminChatId;
        if (!isAdminChat)
        {
            _logger.LogDebug("Команда /say не из админ-чата: {ChatId}", message.Chat.Id);
            return;
        }

        await HandleSayCommandAsync(message, cancellationToken);
    }

    internal async Task HandleSayCommandAsync(Message message, CancellationToken cancellationToken)
    {
        if (message?.Text == null)
        {
            await _messageService.SendUserNotificationAsync(
                message?.From!,
                message?.Chat!,
                UserNotificationType.Warning,
                new SimpleNotificationData(message?.From!, message?.Chat!, "Сообщение не может быть null"),
                cancellationToken
            );
            return;
        }
        
        var parts = message.Text.Split(' ', 3);
        if (parts.Length < 3)
        {
            await _messageService.SendUserNotificationAsync(
                message.From!,
                message.Chat,
                UserNotificationType.Warning,
                new SimpleNotificationData(message.From!, message.Chat, "Формат: /say @username сообщение или /say user_id сообщение"),
                cancellationToken
            );
            return;
        }
        
        var target = parts[1];
        var textToSend = parts[2];
        long? userId = null;
        
        if (target.StartsWith("@"))
        {
            // Пробуем найти userId по username среди недавних пользователей (по кэшу)
            userId = TryFindUserIdByUsername(target.Substring(1));
        }
        else if (long.TryParse(target, out var id))
        {
            userId = id;
        }
        
        if (userId == null)
        {
            await _messageService.SendUserNotificationAsync(
                message.From!,
                message.Chat,
                UserNotificationType.Warning,
                new SimpleNotificationData(message.From!, message.Chat, $"Не удалось найти пользователя {target}. Сообщение не отправлено."),
                cancellationToken
            );
            return;
        }

        try
        {
            await _bot.SendMessage(userId.Value, textToSend, parseMode: ParseMode.Markdown);
            await _messageService.SendUserNotificationAsync(
                message.From!,
                message.Chat,
                UserNotificationType.Success,
                new SimpleNotificationData(message.From!, message.Chat, $"Сообщение отправлено пользователю {target}"),
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            await _messageService.SendUserNotificationAsync(
                message.From!,
                message.Chat,
                UserNotificationType.Warning,
                new SimpleNotificationData(message.From!, message.Chat, $"Не удалось доставить сообщение пользователю {target}: {ex.Message}"),
                cancellationToken
            );
        }
    }

    // Вспомогательная функция для поиска userId по username среди недавних пользователей (по кэшу)
    internal long? TryFindUserIdByUsername(string username)
    {
        if (string.IsNullOrEmpty(username))
            return null;
            
        // Можно использовать MemoryCache или другой кэш, если он есть
        // Здесь пример с MemoryCache: ищем по значениям, где username встречался
        foreach (var item in MemoryCache.Default)
        {
            // Ищем в значении, а не в ключе
            if (item.Value is string text && text.Contains(username, StringComparison.OrdinalIgnoreCase))
            {
                // Ключи вида chatId_userId
                var parts = item.Key.ToString().Split('_');
                if (parts.Length == 2 && long.TryParse(parts[1], out var uid))
                    return uid;
            }
        }
        
        return null;
    }
}