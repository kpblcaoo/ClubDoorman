using ClubDoorman.Services;
using ClubDoorman.Models.Notifications;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Infrastructure.ErrorHandling.ErrorHandlingStrategies;

/// <summary>
/// Стратегия уведомлений администраторов об ошибках
/// </summary>
public class NotificationStrategy : IErrorHandlingStrategy
{
    private readonly IMessageService _messageService;
    private readonly ILogger<NotificationStrategy> _logger;

    public string Name => "NotificationStrategy";
    public int Priority => 50; // Средний приоритет

    public NotificationStrategy(IMessageService messageService, ILogger<NotificationStrategy> logger)
    {
        _messageService = messageService;
        _logger = logger;
    }

    public bool CanHandle(Exception exception, ErrorContext context)
    {
        // Уведомляем только о высоких и критических ошибках
        return context.Severity >= ErrorSeverity.High;
    }

    public async Task<ErrorHandlingResult> HandleAsync(Exception exception, ErrorContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var notificationData = CreateNotificationData(exception, context);
            
            await _messageService.SendAdminNotificationAsync(
                AdminNotificationType.SystemError,
                notificationData,
                cancellationToken
            );

            _logger.LogDebug("Отправлено уведомление администратору об ошибке: {Operation}", context.Operation);

            return ErrorHandlingResult.Success(shouldContinue: true);
        }
        catch (Exception notificationException)
        {
            _logger.LogError(notificationException, "Не удалось отправить уведомление администратору об ошибке: {Operation}", context.Operation);
            return ErrorHandlingResult.Failure(shouldContinue: true);
        }
    }

    private static ErrorNotificationData CreateNotificationData(Exception exception, ErrorContext context)
    {
        var errorMessage = FormatErrorMessage(exception, context);
        
        return new ErrorNotificationData(
            exception,
            errorMessage,
            context.User,
            context.Chat
        );
    }

    private static string FormatErrorMessage(Exception exception, ErrorContext context)
    {
        var parts = new List<string>
        {
            $"Ошибка в операции: {context.Operation}"
        };

        if (!string.IsNullOrEmpty(context.Description))
        {
            parts.Add($"Описание: {context.Description}");
        }

        if (context.User != null)
        {
            parts.Add($"Пользователь: {context.User.Id} ({context.User.Username ?? context.User.FirstName})");
        }

        if (context.Chat != null)
        {
            parts.Add($"Чат: {context.Chat.Id} ({context.Chat.Title ?? context.Chat.Type.ToString()})");
        }

        if (context.Message != null)
        {
            parts.Add($"Сообщение ID: {context.Message.MessageId}");
        }

        parts.Add($"Исключение: {exception.GetType().Name}");
        parts.Add($"Сообщение: {exception.Message}");

        return string.Join("\n", parts);
    }
} 