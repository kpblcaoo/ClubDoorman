using ClubDoorman.Models;

namespace ClubDoorman.Infrastructure;

/// <summary>
/// Конфигурация для управления эффектами модерации
/// <tags>configuration, effects, moderation</tags>
/// </summary>
public class EffectsConfiguration
{
    /// <summary>
    /// Включить использование реальных эффектов вместо логгер-заглушек
    /// </summary>
    public bool UseRealEffects { get; set; } = false;

    /// <summary>
    /// Список включенных действий для эффектов
    /// </summary>
    public string[] EnabledActions { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Включить fallback на старую логику для безопасности
    /// </summary>
    public bool LegacyFallback { get; set; } = true;

    /// <summary>
    /// Включить сравнение логов для верификации
    /// </summary>
    public bool LogComparison { get; set; } = true;

    /// <summary>
    /// Проверить, включен ли эффект для конкретного действия
    /// </summary>
    public bool IsActionEnabled(string actionName)
    {
        return UseRealEffects && EnabledActions.Contains(actionName);
    }

    /// <summary>
    /// Проверить, включен ли эффект для конкретного действия
    /// </summary>
    public bool IsActionEnabled(ModerationAction action)
    {
        return IsActionEnabled(action.ToString());
    }
}
