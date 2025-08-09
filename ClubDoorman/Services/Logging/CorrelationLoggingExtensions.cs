using Microsoft.Extensions.Logging;
using ClubDoorman.Models.Logging;

namespace ClubDoorman.Services.Logging;

/// <summary>
/// Расширения для корреляционного логирования
/// </summary>
internal static class CorrelationLoggingExtensions
{
    /// <summary>
    /// Создает scope с корреляционными данными
    /// </summary>
    public static IDisposable BeginCorrelationScope(this ILogger logger, long? messageId, long? chatId, string? requestId = null)
    {
        var correlationData = new Dictionary<string, object?>
        {
            ["MessageId"] = messageId,
            ["ChatId"] = chatId,
            ["RequestId"] = requestId ?? Guid.NewGuid().ToString("N")[..8]
        };

        return logger.BeginScope(correlationData)!;
    }

    /// <summary>
    /// Логирует событие трейса если трейсинг включен
    /// </summary>
    public static void LogTrace(this ILogger logger, LoggingFlags flags, string eventName, object? data = null)
    {
        if (!flags.TraceEnabled) return;

        var traceLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger("ClubDoorman.Trace");

        if (data != null)
        {
            traceLogger.LogDebug("{EventName}: {@Data}", eventName, data);
        }
        else
        {
            traceLogger.LogDebug("{EventName}", eventName);
        }
    }

    /// <summary>
    /// Логирует событие трейса с простым сообщением
    /// </summary>
    public static void LogTraceEvent(this ILogger logger, LoggingFlags flags, string eventName, string message)
    {
        if (!flags.TraceEnabled) return;

        var traceLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger("ClubDoorman.Trace");

        traceLogger.LogDebug("{EventName}: {Message}", eventName, message);
    }
}