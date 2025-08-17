using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Test.TestInfrastructure.Logging;

/// <summary>
/// In-memory test logger capturing structured logging state for assertions.
/// </summary>
/// <typeparam name="T">Category type.</typeparam>
internal sealed class TestLogger<T> : ILogger<T>
{
    private readonly ConcurrentQueue<LogRecord> _records = new();
    public IReadOnlyCollection<LogRecord> Records => _records.ToArray();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NoopScope.Instance;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        string? template = null;
        Dictionary<string, object?> values = new();
        if (state is IEnumerable<KeyValuePair<string, object>> kvps)
        {
            foreach (var kv in kvps)
            {
                if (kv.Key == "{OriginalFormat}" || kv.Key == "OriginalFormat")
                    template = kv.Value?.ToString();
                else
                    values[kv.Key] = kv.Value;
            }
        }
        _records.Enqueue(new LogRecord(logLevel, eventId, template, message, values, exception));
    }

    public sealed record LogRecord(LogLevel Level, EventId EventId, string? Template, string Message, Dictionary<string, object?> Values, Exception? Exception);

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new();
        public void Dispose() { }
    }
}
