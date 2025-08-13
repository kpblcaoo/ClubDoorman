using System.Text;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.LinkFormatting;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.Telegram;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Features.AdminOps;

/// <summary>
/// Обработчик команды /stat и /stats для отображения статистики по группам
/// </summary>
public class StatsCommandHandler : ICommandHandler
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IStatisticsService _statisticsService;
    private readonly IChatLinkFormatter _chatLinkFormatter;
    private readonly IMessageService _messageService;
    private readonly IAppConfig _appConfig;
    private readonly ILogger<StatsCommandHandler> _logger;

    public string CommandName => "stats"; // Основная команда, также обрабатывает "stat"

    public StatsCommandHandler(
        ITelegramBotClientWrapper bot,
        IStatisticsService statisticsService,
        IChatLinkFormatter chatLinkFormatter,
        IMessageService messageService,
        IAppConfig appConfig,
        ILogger<StatsCommandHandler> logger)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _chatLinkFormatter = chatLinkFormatter ?? throw new ArgumentNullException(nameof(chatLinkFormatter));
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
            _logger.LogDebug("Команда /stats не из админ-чата: {ChatId}", message.Chat.Id);
            return;
        }

        var commandText = message.Text?.Split(' ')[0].ToLower();
        var command = commandText?.StartsWith("/") == true ? commandText.Substring(1) : commandText;

        // Обрабатываем как /stat, так и /stats
        if (command != "stat" && command != "stats")
        {
            _logger.LogDebug("Неподдерживаемая команда для StatsCommandHandler: {Command}", command);
            return;
        }

        await HandleStatsCommandAsync(message, cancellationToken);
    }

    private async Task HandleStatsCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var report = _statisticsService.GetAllStats();
        var sb = new StringBuilder();
        sb.AppendLine("📊 *Статистика по группам:*\n");
        
        if (report == null || !report.Any())
        {
            sb.AppendLine("Ничего интересного не произошло 🎉");
            await _messageService.SendUserNotificationAsync(
                message.From!,
                message.Chat,
                UserNotificationType.SystemInfo,
                new SimpleNotificationData(message.From!, message.Chat, sb.ToString()),
                cancellationToken
            );
            return;
        }
        
        foreach (var (chatId, stats) in report.OrderBy(x => x.Value.ChatTitle))
        {
            var sum = stats.KnownBadMessage + stats.BlacklistBanned + stats.StoppedCaptcha + stats.LongNameBanned;
            if (sum == 0) continue;
            Chat? chat = null;
            try { chat = await _bot.GetChat(chatId); } catch { }
            sb.AppendLine();
            if (chat != null)
                sb.AppendLine($"{_chatLinkFormatter.GetChatLink(chat)} (`{chat.Id}`) [{ChatSettingsManager.GetChatType(chat.Id)}]:");
            else
                sb.AppendLine($"{_chatLinkFormatter.GetChatLink(chatId, stats.ChatTitle)} (`{chatId}`) [{ChatSettingsManager.GetChatType(chatId)}]:");
            sb.AppendLine($"▫️ Всего блокировок: *{sum}*");
            if (stats.BlacklistBanned > 0)
                sb.AppendLine($"▫️ По блеклистам: *{stats.BlacklistBanned}*");
            if (stats.StoppedCaptcha > 0)
                sb.AppendLine($"▫️ Остановлено капчей: *{stats.StoppedCaptcha}*");
            if (stats.LongNameBanned > 0)
                sb.AppendLine($"▫️ За длинные имена: *{stats.LongNameBanned}*");
            if (stats.KnownBadMessage > 0)
                sb.AppendLine($"▫️ Известные спам-сообщения: *{stats.KnownBadMessage}*");
        }
        
        await _messageService.SendUserNotificationAsync(
            message.From!,
            message.Chat,
            UserNotificationType.SystemInfo,
            new SimpleNotificationData(message.From!, message.Chat, sb.ToString()),
            cancellationToken
        );
    }
}