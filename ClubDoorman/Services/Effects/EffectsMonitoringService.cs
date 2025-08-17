using ClubDoorman.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services.Effects;

/// <summary>
/// Сервис мониторинга эффектов модерации
/// Отслеживает выполнение эффектов и сравнивает с ожидаемым поведением
/// <tags>effects, monitoring, verification</tags>
/// </summary>
public class EffectsMonitoringService
{
    private readonly EffectsConfiguration _config;
    private readonly ILogger<EffectsMonitoringService> _logger;

    public EffectsMonitoringService(
        EffectsConfiguration config,
        ILogger<EffectsMonitoringService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Записать информацию о выполнении эффекта
    /// </summary>
    public void LogEffectExecution(string actionName, bool success, TimeSpan duration)
    {
        if (!_config.LogComparison)
            return;

        var status = success ? "SUCCESS" : "FAILED";
        _logger.LogInformation("[EFFECTS_MONITOR] {Action}: {Status} in {Duration}ms",
            actionName, status, duration.TotalMilliseconds);
    }

    /// <summary>
    /// Сравнить логи до и после миграции
    /// </summary>
    public void CompareLogs(string actionName, string beforeLogs, string afterLogs)
    {
        if (!_config.LogComparison)
            return;

        var areIdentical = beforeLogs.Equals(afterLogs, StringComparison.OrdinalIgnoreCase);
        var status = areIdentical ? "IDENTICAL" : "DIFFERENT";

        _logger.LogInformation("[EFFECTS_MONITOR] {Action} logs comparison: {Status}",
            actionName, status);

        if (!areIdentical)
        {
            _logger.LogWarning("[EFFECTS_MONITOR] {Action} logs differ - potential regression detected",
                actionName);
        }
    }

    /// <summary>
    /// Проверить, что эффект выполнился корректно
    /// </summary>
    public bool ValidateEffectExecution(string actionName, object expectedResult, object actualResult)
    {
        if (!_config.LogComparison)
            return true;

        var isValid = Equals(expectedResult, actualResult);
        var status = isValid ? "VALID" : "INVALID";

        _logger.LogInformation("[EFFECTS_MONITOR] {Action} validation: {Status}",
            actionName, status);

        return isValid;
    }
}
