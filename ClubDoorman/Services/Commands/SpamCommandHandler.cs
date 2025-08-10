using ClubDoorman.Infrastructure;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Telegram;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Services.Commands;

/// <summary>
/// Обработчик команды /spam для добавления сообщений в базу спама
/// </summary>
public class SpamCommandHandler : ICommandHandler
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ISpamHamClassifier _classifier;
    private readonly IBadMessageManager _badMessageManager;
    private readonly IMessageService _messageService;
    private readonly IAppConfig _appConfig;
    private readonly ILogger<SpamCommandHandler> _logger;

    public string CommandName => "spam";

    public SpamCommandHandler(
        ITelegramBotClientWrapper bot,
        ISpamHamClassifier classifier,
        IBadMessageManager badMessageManager,
        IMessageService messageService,
        IAppConfig appConfig,
        ILogger<SpamCommandHandler> logger)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        _badMessageManager = badMessageManager ?? throw new ArgumentNullException(nameof(badMessageManager));
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
            _logger.LogDebug("Команда /spam не из админ-чата: {ChatId}", message.Chat.Id);
            return;
        }

        // Проверяем, что есть реплай на сообщение
        if (message.ReplyToMessage == null)
        {
            _logger.LogDebug("Команда /spam без реплая на сообщение");
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
        _logger.LogDebug("Команда /spam: извлечен текст='{Text}' (длина={Length})", 
            string.IsNullOrWhiteSpace(text) ? "[ПУСТОЙ]" : text.Length > 100 ? text.Substring(0, 100) + "..." : text, 
            text?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("❌ Команда /spam не выполнена: текст сообщения пустой или отсутствует");
            await _messageService.SendUserNotificationAsync(message.From!, message.Chat, UserNotificationType.Warning, 
                new SimpleNotificationData(message.From!, message.Chat, "Сообщение не содержит текста"), 
                cancellationToken);
            return;
        }

        await HandleSpamCommandAsync(message, text, replyToMessage, cancellationToken);
    }

    private async Task HandleSpamCommandAsync(Message message, string text, Message replyToMessage, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔥 Обрабатываем команду /spam для текста: '{Text}'", text);
        await _classifier.AddSpam(text);
        await _badMessageManager.MarkAsBad(text);
        
        // Уведомление пользователю
        await _messageService.SendUserNotificationAsync(message.From!, message.Chat, UserNotificationType.Success, 
            new SimpleNotificationData(message.From!, message.Chat, "Сообщение добавлено как пример спама"), 
            cancellationToken);
            
        // Уведомление в админку о добавлении спам примера
        var adminData = new SimpleNotificationData(
            message.From!, 
            message.Chat, 
            $"Администратор {Utils.FullName(message.From!)} добавил сообщение как пример СПАМА:\n\n`{text.Substring(0, Math.Min(text.Length, 200))}{(text.Length > 200 ? "..." : "")}`"
        );
        await _messageService.SendAdminNotificationAsync(AdminNotificationType.SystemInfo, adminData, cancellationToken);
        
        _logger.LogInformation("✅ Команда /spam успешно выполнена");
    }
}