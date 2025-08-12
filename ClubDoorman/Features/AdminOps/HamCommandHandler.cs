using ClubDoorman.Infrastructure;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Telegram;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Features.AdminOps;

/// <summary>
/// Обработчик команды /ham для добавления сообщений в базу НЕ-спама
/// </summary>
public class HamCommandHandler : ICommandHandler
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ISpamHamClassifier _classifier;
    private readonly IMessageService _messageService;
    private readonly IAppConfig _appConfig;
    private readonly ILogger<HamCommandHandler> _logger;

    public string CommandName => "ham";

    public HamCommandHandler(
        ITelegramBotClientWrapper bot,
        ISpamHamClassifier classifier,
        IMessageService messageService,
        IAppConfig appConfig,
        ILogger<HamCommandHandler> logger)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
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
            _logger.LogDebug("Команда /ham не из админ-чата: {ChatId}", message.Chat.Id);
            return;
        }

        // Проверяем, что есть реплай на сообщение
        if (message.ReplyToMessage == null)
        {
            _logger.LogDebug("Команда /ham без реплая на сообщение");
            return;
        }

        var replyToMessage = message.ReplyToMessage;
        
        // Проверяем, что реплай не на сообщение бота (кроме форвардов)
        if (replyToMessage.From?.Id == _bot.BotId && replyToMessage.ForwardDate == null)
        {
            await _messageService.SendUserNotificationAsync(message.From!, message.Chat, UserNotificationType.Warning, 
                new SimpleNotificationData(message.From!, message.Chat, "Реплай на сообщение бота"), 
                cancellationToken);
            return;
        }

        var text = replyToMessage.Text ?? replyToMessage.Caption;
        _logger.LogDebug("Команда /ham: извлечен текст='{Text}' (длина={Length})", 
            string.IsNullOrWhiteSpace(text) ? "[ПУСТОЙ]" : text.Length > 100 ? text.Substring(0, 100) + "..." : text, 
            text?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("❌ Команда /ham не выполнена: текст сообщения пустой или отсутствует");
            await _messageService.SendUserNotificationAsync(message.From!, message.Chat, UserNotificationType.Warning, 
                new SimpleNotificationData(message.From!, message.Chat, "Сообщение не содержит текста"), 
                cancellationToken);
            return;
        }

        await HandleHamCommandAsync(message, text, replyToMessage, cancellationToken);
    }

    private async Task HandleHamCommandAsync(Message message, string text, Message replyToMessage, CancellationToken cancellationToken)
    {
        _logger.LogInformation("✅ Обрабатываем команду /ham для текста: '{Text}'", text);
        await _classifier.AddHam(text);
        
        // Уведомление пользователю (исправляем Markdown ошибку)
        await _messageService.SendUserNotificationAsync(message.From!, message.Chat, UserNotificationType.Success, 
            new SimpleNotificationData(message.From!, message.Chat, "Сообщение добавлено как пример НЕ\\-спама"), 
            cancellationToken);
            
        // Уведомление в админку о добавлении НЕ-спам примера
        var adminData = new SimpleNotificationData(
            message.From!, 
            message.Chat, 
            $"Администратор {Utils.FullName(message.From!)} добавил сообщение как пример НЕ-спама:\n\n`{text.Substring(0, Math.Min(text.Length, 200))}{(text.Length > 200 ? "..." : "")}`"
        );
        await _messageService.SendAdminNotificationAsync(AdminNotificationType.SystemInfo, adminData, cancellationToken);
        
        _logger.LogInformation("✅ Команда /ham успешно выполнена");
    }
}