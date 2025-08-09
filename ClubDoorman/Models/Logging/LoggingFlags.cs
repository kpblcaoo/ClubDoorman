namespace ClubDoorman.Models.Logging;

/// <summary>
/// Настройки логирования для Golden Master и трейсов
/// </summary>
public class LoggingFlags
{
    /// <summary>
    /// Включить трейсинг событий
    /// </summary>
    public bool TraceEnabled { get; set; } = true;

    /// <summary>
    /// Включить сбор Golden Master данных
    /// </summary>
    public bool GoldenMasterEnabled { get; set; } = true;

    /// <summary>
    /// Коэффициент сэмплирования для Golden Master (0.1 = 10%)
    /// </summary>
    public double GoldenSampleRate { get; set; } = 0.1;
}