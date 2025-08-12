using ClubDoorman.Features.Moderation;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Infrastructure;
using ClubDoorman.Services;
using ClubDoorman.Models.Notifications;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Messaging;

namespace ClubDoorman.Features.AdminOps;

/// <summary>
/// Обработчик команд для управления подозрительными пользователями
/// </summary>
public class SuspiciousCommandHandler : ISuspiciousCommandHandler
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IModerationFacade _moderationService;
    private readonly IMessageService _messageService;
    private readonly ILogger<SuspiciousCommandHandler> _logger;
    private readonly IAppConfig _appConfig;

    public string CommandName => "suspicious";

    public SuspiciousCommandHandler(
        ITelegramBotClientWrapper bot, 
        IModerationFacade moderationService,
        IMessageService messageService,
        ILogger<SuspiciousCommandHandler> logger,
        IAppConfig appConfig)
    {
        _bot = bot;
        _moderationService = moderationService;
        _messageService = messageService;
        _logger = logger;
        _appConfig = appConfig;
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        // Проверяем, что команда пришла из админ-чата
        if (message.Chat.Id != _appConfig.AdminChatId && message.Chat.Id != _appConfig.LogAdminChatId)
            return;

        var commandParts = message.Text?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        if (commandParts.Length < 2)
        {
            await ShowHelp(message, cancellationToken);
            return;
        }

        var subCommand = commandParts[1].ToLower();
        
        try
        {
            switch (subCommand)
            {
                case "stats":
                    await HandleStatsCommand(message, cancellationToken);
                    break;
                    
                case "list":
                    await HandleListCommand(message, cancellationToken);
                    break;
                    
                case "help":
                default:
                    await ShowHelp(message, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке команды /suspicious {SubCommand}", subCommand);
            await _messageService.SendUserNotificationAsync(
                message.From!,
                message.Chat,
                UserNotificationType.Warning,
                new SimpleNotificationData(message.From!, message.Chat, "Произошла ошибка при выполнении команды"),
                cancellationToken
            );
        }
    }

    private async Task HandleStatsCommand(Message message, CancellationToken cancellationToken)
    {
        var stats = _moderationService.GetSuspiciousUsersStats();
        var aiDetectUsers = _moderationService.GetAiDetectUsers();
        
        var statusMessage = 
            $"*Статус системы подозрительных пользователей:*\n\n" +
            $"• Система включена: {(_appConfig.SuspiciousDetectionEnabled ? "✅" : "❌")}\n" +
            $"• Порог мимикрии: *{_appConfig.MimicryThreshold:F1}*\n" +
            $"• Сообщений для одобрения: *{_appConfig.SuspiciousToApprovedMessageCount}*\n\n" +
            $"*Статистика:*\n" +
            $"• Всего подозрительных: *{stats.TotalSuspicious}*\n" +
            $"• С AI детектом: *{stats.WithAiDetect}*\n" +
            $"• Групп: *{stats.GroupsCount}*\n\n" +
            $"*AI анализ:*\n" +
            $"• API настроен: {(_appConfig.OpenRouterApi != null ? "✅" : "❌")}\n" +
            $"• AI чаты включены: *{_appConfig.AiEnabledChats.Count}*\n\n" +
            $"*Команды:*\n" +
            $"• `/suspicious list` - список подозрительных\n" +
            $"• `/suspicious ai <on|off> <userId> <chatId>` - включить/выключить AI детект\n" +
            $"• `/suspicious approve <userId> <chatId>` - одобрить пользователя\n" +
            $"• `/suspicious ban <userId> <chatId>` - забанить пользователя\n" +
            $"• `/suspicious cleanup <userId> <chatId>` - очистить из всех списков";

        await _messageService.SendAdminNotificationAsync(
            AdminNotificationType.SystemInfo,
            new SimpleNotificationData(message.From!, message.Chat, statusMessage),
            cancellationToken
        );

        _logger.LogInformation("Отправлена статистика подозрительных пользователей в админ-чат");
    }

    private async Task HandleListCommand(Message message, CancellationToken cancellationToken)
    {
        var aiDetectUsers = _moderationService.GetAiDetectUsers();

        if (aiDetectUsers.Count == 0)
        {
            await _messageService.SendAdminNotificationAsync(
                AdminNotificationType.SystemInfo,
                new SimpleNotificationData(message.From!, message.Chat, "📝 *Список пользователей с AI детектом*\n\nНет пользователей с включенным AI детектом."),
                cancellationToken
            );
            return;
        }

        var listText = $"📝 *Пользователи с включенным AI детектом* ({aiDetectUsers.Count})\n\n";

        for (int i = 0; i < Math.Min(aiDetectUsers.Count, 10); i++) // Показываем максимум 10
        {
            var (userId, chatId) = aiDetectUsers[i];
            listText += $"{i + 1}. ID: `{userId}` в чате `{chatId}`\n";
        }

        if (aiDetectUsers.Count > 10)
        {
            listText += $"\n... и ещё {aiDetectUsers.Count - 10} пользователей";
        }

        await _messageService.SendAdminNotificationAsync(
            AdminNotificationType.SystemInfo,
            new SimpleNotificationData(message.From!, message.Chat, listText),
            cancellationToken
        );

        _logger.LogInformation("Отправлен список пользователей с AI детектом в админ-чат");
    }

    private async Task ShowHelp(Message message, CancellationToken cancellationToken)
    {
        var helpText = """
🔍 *Команды управления подозрительными пользователями*

/suspicious stats - показать статистику
/suspicious list - список пользователей с AI детектом  
/suspicious help - эта справка

*Описание системы:*
Система автоматически анализирует первые 3 сообщения новых пользователей на предмет шаблонности и мимикрии. Подозрительные пользователи переводятся в промежуточный статус и требуют дополнительных хороших сообщений для одобрения.

Для особо подозрительных пользователей администраторы могут включить AI детект, который будет пересылать все их сообщения в админ-чат для ручного анализа.
""";

        await _messageService.SendAdminNotificationAsync(
            AdminNotificationType.SystemInfo,
            new SimpleNotificationData(message.From!, message.Chat, helpText),
            cancellationToken
        );
    }
}