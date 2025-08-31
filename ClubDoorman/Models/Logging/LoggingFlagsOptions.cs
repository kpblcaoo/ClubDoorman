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

    /// <summary>Базовый путь для записи Golden Master (по умолчанию "golden"). Позволяет изолировать тесты.</summary>
    public string GoldenBasePath { get; set; } = "golden";

    /// <summary>
    /// Включить детерминированные correlationId (без компонента времени/Guid) — используется для генерации baseline.
    /// По умолчанию выключено, чтобы рабочие снапшоты имели уникальные имена.
    /// </summary>
    public bool GoldenDeterministicIds { get; set; } = false;

    /// <summary>
    /// Если указано, вместо папки с датой будет использовано фиксированное имя (например, "baseline").
    /// Это позволяет хранить один стабильный baseline в репозитории без ежедневных подпапок.
    /// </summary>
    public string? GoldenFixedDateFolder { get; set; } = null;
}
