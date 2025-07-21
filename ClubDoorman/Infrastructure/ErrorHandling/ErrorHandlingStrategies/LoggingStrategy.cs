using Microsoft.Extensions.Logging;

namespace ClubDoorman.Infrastructure.ErrorHandling.ErrorHandlingStrategies;

/// <summary>
/// Стратегия логирования ошибок
/// </summary>
public class LoggingStrategy : IErrorHandlingStrategy
{
    private readonly ILogger<LoggingStrategy> _logger;

    public string Name => "LoggingStrategy";
    public int Priority => 100; // Высокий приоритет - логируем всегда

    public LoggingStrategy(ILogger<LoggingStrategy> logger)
    {
        _logger = logger;
    }

    public bool CanHandle(Exception exception, ErrorContext context)
    {
        // Логируем все ошибки
        return true;
    }

    public async Task<ErrorHandlingResult> HandleAsync(Exception exception, ErrorContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var logLevel = GetLogLevel(context.Severity);
            var message = FormatLogMessage(exception, context);

            _logger.Log(logLevel, exception, message);

            // Добавляем дополнительные данные в лог
            if (context.AdditionalData.Any())
            {
                _logger.Log(logLevel, "Дополнительные данные: {@AdditionalData}", context.AdditionalData);
            }

            return ErrorHandlingResult.Success(shouldContinue: true);
        }
        catch (Exception logException)
        {
            // Если не удалось залогировать, выводим в консоль
            Console.WriteLine($"Ошибка при логировании: {logException.Message}");
            return ErrorHandlingResult.Failure(shouldContinue: true);
        }
    }

    private static LogLevel GetLogLevel(ErrorSeverity severity)
    {
        return severity switch
        {
            ErrorSeverity.Low => LogLevel.Debug,
            ErrorSeverity.Medium => LogLevel.Warning,
            ErrorSeverity.High => LogLevel.Error,
            ErrorSeverity.Critical => LogLevel.Critical,
            _ => LogLevel.Warning
        };
    }

    private static string FormatLogMessage(Exception exception, ErrorContext context)
    {
        var parts = new List<string>
        {
            $"Ошибка в операции '{context.Operation}'"
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

        parts.Add($"Исключение: {exception.GetType().Name}: {exception.Message}");

        return string.Join(" | ", parts);
    }
} 