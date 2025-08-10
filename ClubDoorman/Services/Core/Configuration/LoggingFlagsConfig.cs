using Microsoft.Extensions.Configuration;

namespace ClubDoorman.Services.Core.Configuration;

/// <summary>
/// Конфигурация флагов логирования для Golden Master и трассировки
/// </summary>
public interface ILoggingFlagsConfig
{
    /// <summary>
    /// Включено ли событийное трейсинг логирование
    /// </summary>
    bool TraceEnabled { get; }
    
    /// <summary>
    /// Включена ли запись Golden Master данных
    /// </summary>
    bool GoldenMasterEnabled { get; }
    
    /// <summary>
    /// Частота сэмплирования для Golden Master (0.0 - 1.0)
    /// </summary>
    double GoldenSampleRate { get; }
}

/// <summary>
/// Реализация конфигурации флагов логирования, загружаемая из appsettings.json
/// </summary>
public class LoggingFlagsConfig : ILoggingFlagsConfig
{
    public bool TraceEnabled { get; }
    public bool GoldenMasterEnabled { get; }
    public double GoldenSampleRate { get; }

    public LoggingFlagsConfig(IConfiguration configuration)
    {
        var section = configuration.GetSection("LoggingFlags");
        TraceEnabled = section.GetValue<bool>("TraceEnabled", false);
        GoldenMasterEnabled = section.GetValue<bool>("GoldenMasterEnabled", false);
        GoldenSampleRate = section.GetValue<double>("GoldenSampleRate", 0.1);
    }
}