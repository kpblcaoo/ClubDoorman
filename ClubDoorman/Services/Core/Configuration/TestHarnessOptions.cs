namespace ClubDoorman.Services.Core.Configuration;

/// <summary>
/// Опции тестового / golden режима. Выделены отдельно чтобы изолировать небоевую функциональность.
/// </summary>
public class TestHarnessOptions
{
    /// <summary>
    /// Включен ли golden baseline режим (ускоряет ML/AI и подменяет TelegramBotClient).
    /// Источник: DOORMAN_GOLDEN_BASELINE == "1".
    /// </summary>
    public bool GoldenBaselineMode { get; set; }

    /// <summary>
    /// Тестовый блэклист ID пользователей (используется только в тестах / golden пайплайне).
    /// Источник: DOORMAN_TEST_BLACKLIST_IDS = "123,456".
    /// </summary>
    public HashSet<long> TestBlacklistUserIds { get; set; } = new();
}
