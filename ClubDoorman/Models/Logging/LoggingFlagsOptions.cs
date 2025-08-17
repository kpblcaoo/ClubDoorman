namespace ClubDoorman.Models.Logging;

/// <summary>
/// Конфигурация флагов для трассировки и Golden Master.
/// Значения подгружаются из секции LoggingFlags в appsettings.json / переменных окружения.
/// </summary>
public class LoggingFlagsOptions
{
    /// <summary>Включить событийный trace (категория ClubDoorman.Trace)</summary>
    public bool TraceEnabled { get; set; } = false;

    /// <summary>Включить запись Golden Master файлов</summary>
    public bool GoldenMasterEnabled { get; set; } = false;

    /// <summary>Доля (0..1) сообщений для выборки в Golden Master</summary>
    public double GoldenSampleRate { get; set; } = 0.1;
}
