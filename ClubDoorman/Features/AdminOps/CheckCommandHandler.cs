using ClubDoorman.Infrastructure;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.TextProcessing;
using ClubDoorman.Services.Telegram;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Features.AdminOps;

/// <summary>
/// Обработчик команды /check для проверки сообщений на спам
/// </summary>
public class CheckCommandHandler : ICommandHandler
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ISpamHamClassifier _classifier;
    private readonly IMessageService _messageService;
    private readonly IBotPermissionsService _botPermissionsService;
    private readonly IAppConfig _appConfig;
    private readonly ILogger<CheckCommandHandler> _logger;

    public string CommandName => "check";

    public CheckCommandHandler(
        ITelegramBotClientWrapper bot,
        ISpamHamClassifier classifier,
        IMessageService messageService,
        IBotPermissionsService botPermissionsService,
        IAppConfig appConfig,
        ILogger<CheckCommandHandler> logger)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _botPermissionsService = botPermissionsService ?? throw new ArgumentNullException(nameof(botPermissionsService));
        _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[DEBUG] CheckCommandHandler.HandleAsync вызван: ChatId={message.Chat.Id}, AdminChatId={_appConfig.AdminChatId}, LogAdminChatId={_appConfig.LogAdminChatId}");

        // Проверяем, что команда пришла из админ-чата
        var isAdminChat = message.Chat.Id == _appConfig.AdminChatId || message.Chat.Id == _appConfig.LogAdminChatId;
        if (!isAdminChat)
        {
            Console.WriteLine($"[DEBUG] CheckCommandHandler: команда не из админ-чата: {message.Chat.Id}");
            _logger.LogDebug("Команда /check не из админ-чата: {ChatId}", message.Chat.Id);
            return;
        }

        Console.WriteLine($"[DEBUG] CheckCommandHandler: admin chat check passed");

        // Проверяем, что есть реплай на сообщение
        if (message.ReplyToMessage == null)
        {
            Console.WriteLine($"[DEBUG] CheckCommandHandler: нет ReplyToMessage");
            _logger.LogDebug("Команда /check без реплая на сообщение");
            return;
        }

        Console.WriteLine($"[DEBUG] CheckCommandHandler: ReplyToMessage найдено");

        var replyToMessage = message.ReplyToMessage;

        Console.WriteLine($"[DEBUG] CheckCommandHandler: проверяем ReplyToMessage.From?.Id={replyToMessage.From?.Id} vs Bot.BotId={_bot.BotId}");

        // Проверяем, что реплай не на сообщение бота (кроме форвардов)
        if (replyToMessage.From?.Id == _bot.BotId && replyToMessage.ForwardDate == null)
        {
            Console.WriteLine($"[DEBUG] CheckCommandHandler: ReplyToMessage от бота, отправляем предупреждение");
            await _messageService.SendUserNotificationAsync(message.From!, message.Chat, UserNotificationType.Warning,
                new SimpleNotificationData(message.From!, message.Chat, "Реплай на сообщение бота"),
                cancellationToken);
            return;
        }

        Console.WriteLine($"[DEBUG] CheckCommandHandler: bot message check passed, извлекаем текст");

        var text = replyToMessage.Text ?? replyToMessage.Caption;
        _logger.LogDebug("Команда /check: извлечен текст='{Text}' (длина={Length})",
            string.IsNullOrWhiteSpace(text) ? "[ПУСТОЙ]" : text.Length > 100 ? text.Substring(0, 100) + "..." : text,
            text?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("❌ Команда /check не выполнена: текст сообщения пустой или отсутствует");
            await _messageService.SendUserNotificationAsync(message.From!, message.Chat, UserNotificationType.Warning,
                new SimpleNotificationData(message.From!, message.Chat, "Сообщение не содержит текста"),
                cancellationToken);
            return;
        }

        await HandleCheckCommandAsync(message, text, replyToMessage, cancellationToken);
    }

    private async Task HandleCheckCommandAsync(Message message, string text, Message replyToMessage, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[DEBUG] CheckCommandHandler.HandleCheckCommandAsync вызван, text='{text}'");

        // Проверяем права администратора ПОЛЬЗОВАТЕЛЯ, а не бота
        try
        {
            Console.WriteLine($"[DEBUG] CheckCommandHandler: проверяем права пользователя {message.From?.Id} в чате {message.Chat.Id}");

            // Получаем список администраторов чата
            var chatAdmins = await _bot.GetChatAdministratorsAsync(message.Chat.Id, cancellationToken);
            var isUserAdmin = chatAdmins.Any(admin => admin.User.Id == message.From!.Id);

            Console.WriteLine($"[DEBUG] CheckCommandHandler: isUserAdmin={isUserAdmin}");
            if (!isUserAdmin)
            {
                Console.WriteLine($"[DEBUG] CheckCommandHandler: пользователь не админ, отправляем сообщение об ошибке");
                await _messageService.SendUserNotificationAsync(message.From!, message.Chat, UserNotificationType.Warning,
                    new SimpleNotificationData(message.From!, message.Chat, "Доступ запрещен - требуются права администратора"),
                    cancellationToken);
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] CheckCommandHandler: исключение при проверке прав: {ex.Message}");
            _logger.LogWarning(ex, "Не удалось проверить права администратора в чате {ChatId}", message.Chat.Id);
            await _messageService.SendUserNotificationAsync(message.From!, message.Chat, UserNotificationType.Warning,
                new SimpleNotificationData(message.From!, message.Chat, "Ошибка проверки прав доступа"),
                cancellationToken);
            return;
        }

        Console.WriteLine($"[DEBUG] CheckCommandHandler: начинаем анализ текста");
        var emojis = SimpleFilters.TooManyEmojis(text);
        var normalized = TextProcessor.NormalizeText(text);
        var lookalike = SimpleFilters.FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(normalized);
        var hasStopWords = SimpleFilters.HasStopWords(normalized);
        var (spam, score) = await _classifier.IsSpam(normalized);
        var lookAlikeMsg = lookalike.Count == 0 ? "отсутствуют" : string.Join(", ", lookalike);
        var msg =
            $"*Результат проверки:*\n"
            + $"• Много эмодзи: *{emojis}*\n"
            + $"• Найдены стоп-слова: *{hasStopWords}*\n"
            + $"• Маскирующиеся слова: *{lookAlikeMsg}*\n"
            + $"• ML классификатор: спам *{spam}*, скор *{score}*\n\n"
            + $"_Если простые фильтры отработали, то в датасет добавлять не нужно_";
        Console.WriteLine($"[DEBUG] CheckCommandHandler: отправляем результат: {msg}");
        await _messageService.SendUserNotificationAsync(message.From!, message.Chat, UserNotificationType.SystemInfo,
            new SimpleNotificationData(message.From!, message.Chat, msg),
            cancellationToken);
        Console.WriteLine($"[DEBUG] CheckCommandHandler: результат отправлен");
    }
}