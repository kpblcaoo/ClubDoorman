using ClubDoorman.Infrastructure;
using ClubDoorman.Services;
using ClubDoorman.Models.Notifications;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Handlers.Commands;

/// <summary>
/// Обработчик команд локализации
/// </summary>
public class LocalizationCommandHandler : ICommandHandler
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ILogger<LocalizationCommandHandler> _logger;
    private readonly IMessageService _messageService;
    private readonly ILocalizationValidator _localizationValidator;

    public string CommandName => "localization";

    public LocalizationCommandHandler(
        ITelegramBotClientWrapper bot, 
        ILogger<LocalizationCommandHandler> logger, 
        IMessageService messageService,
        ILocalizationValidator localizationValidator)
    {
        _bot = bot;
        _logger = logger;
        _messageService = messageService;
        _localizationValidator = localizationValidator;
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Chat.Type != ChatType.Private)
            return;

        var args = message.Text!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var subCommand = args.Length > 1 ? args[1].ToLowerInvariant() : "help";

        switch (subCommand)
        {
            case "validate":
            case "check":
                await HandleValidateCommandAsync(message, cancellationToken);
                break;
            case "stats":
                await HandleStatsCommandAsync(message, cancellationToken);
                break;
            case "help":
            default:
                await HandleHelpCommandAsync(message, cancellationToken);
                break;
        }
    }

    private async Task HandleValidateCommandAsync(Message message, CancellationToken cancellationToken)
    {
        await _messageService.SendUserNotificationAsync(
            message.From!, 
            message.Chat, 
            UserNotificationType.SystemInfo, 
            new SimpleNotificationData(message.From!, message.Chat, "🔍 Starting localization validation..."), 
            cancellationToken
        );

        try
        {
            var result = _localizationValidator.ValidateAllResources();
            
            var response = new System.Text.StringBuilder();
            response.AppendLine("🔍 **Localization Validation Results**");
            response.AppendLine();
            
            if (result.IsValid)
            {
                response.AppendLine("✅ **All resources are valid!**");
            }
            else
            {
                response.AppendLine("❌ **Validation failed!**");
            }
            
            response.AppendLine();
            response.AppendLine("📊 **Statistics:**");
            foreach (var (key, value) in result.Statistics)
            {
                response.AppendLine($"• {key}: {value}");
            }
            
            if (result.Errors.Any())
            {
                response.AppendLine();
                response.AppendLine("❌ **Errors:**");
                foreach (var error in result.Errors.Take(10)) // Ограничиваем количество ошибок
                {
                    response.AppendLine($"• {error}");
                }
                if (result.Errors.Count > 10)
                {
                    response.AppendLine($"• ... and {result.Errors.Count - 10} more errors");
                }
            }
            
            if (result.Warnings.Any())
            {
                response.AppendLine();
                response.AppendLine("⚠️ **Warnings:**");
                foreach (var warning in result.Warnings.Take(10)) // Ограничиваем количество предупреждений
                {
                    response.AppendLine($"• {warning}");
                }
                if (result.Warnings.Count > 10)
                {
                    response.AppendLine($"• ... and {result.Warnings.Count - 10} more warnings");
                }
            }
            
            await _messageService.SendUserNotificationAsync(
                message.From!, 
                message.Chat, 
                UserNotificationType.SystemInfo, 
                new SimpleNotificationData(message.From!, message.Chat, response.ToString()), 
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during localization validation");
            await _messageService.SendUserNotificationAsync(
                message.From!, 
                message.Chat, 
                UserNotificationType.Warning, 
                new SimpleNotificationData(message.From!, message.Chat, $"❌ Error during validation: {ex.Message}"), 
                cancellationToken
            );
        }
    }

    private async Task HandleStatsCommandAsync(Message message, CancellationToken cancellationToken)
    {
        try
        {
            var result = _localizationValidator.ValidateAllResources();
            
            var response = new System.Text.StringBuilder();
            response.AppendLine("📊 **Localization Statistics**");
            response.AppendLine();
            
            foreach (var (key, value) in result.Statistics)
            {
                response.AppendLine($"• {key}: {value}");
            }
            
            response.AppendLine();
            response.AppendLine($"• Default Culture: {Config.DefaultCulture}");
            response.AppendLine($"• Validation Enabled: {Config.EnableLocalizationValidation}");
            
            await _messageService.SendUserNotificationAsync(
                message.From!, 
                message.Chat, 
                UserNotificationType.SystemInfo, 
                new SimpleNotificationData(message.From!, message.Chat, response.ToString()), 
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting localization stats");
            await _messageService.SendUserNotificationAsync(
                message.From!, 
                message.Chat, 
                UserNotificationType.Warning, 
                new SimpleNotificationData(message.From!, message.Chat, $"❌ Error getting stats: {ex.Message}"), 
                cancellationToken
            );
        }
    }

    private async Task HandleHelpCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var helpText = """
🔧 **Localization Commands**

Available commands:
• `/localization validate` - Validate all localization resources
• `/localization check` - Same as validate
• `/localization stats` - Show localization statistics
• `/localization help` - Show this help

Environment variables:
• `DOORMAN_DEFAULT_CULTURE` - Default culture (en/ru)
• `DOORMAN_LOCALIZATION_VALIDATION_ENABLE` - Enable validation
• `DOORMAN_CHAT_CULTURE_<CHAT_ID>` - Override culture for specific chat

Examples:
• `DOORMAN_DEFAULT_CULTURE=en`
• `DOORMAN_CHAT_CULTURE_123456789=ru`
""";

        await _messageService.SendUserNotificationAsync(
            message.From!, 
            message.Chat, 
            UserNotificationType.SystemInfo, 
            new SimpleNotificationData(message.From!, message.Chat, helpText), 
            cancellationToken
        );
    }
} 