namespace ClubDoorman.Tests.TestKit2.Core;

/// <summary>
/// Типы эффектов для тестирования
/// </summary>
public enum EffectType
{
    Delete = 0,
    Report = 1,
    Ban = 2,
    Approve = 3,
    IncrementGood = 4,
    LogChat = 5,
    AiCascade = 6
}

/// <summary>
/// Эффект для тестирования
/// </summary>
public record Effect(EffectType Type, long ChatId, long UserId, string? Reason = null, int? MessageId = null);

/// <summary>
/// Интерфейс для записи эффектов в тестах
/// </summary>
public interface IEffectsSink
{
    void Add(Effect effect);
    IReadOnlyList<Effect> Snapshot();
    void Clear();
}

/// <summary>
/// Реализация IEffectsSink для тестов
/// </summary>
public class EffectsSink : IEffectsSink
{
    private readonly List<Effect> _effects = new();

    public void Add(Effect effect)
    {
        _effects.Add(effect);
    }

    public IReadOnlyList<Effect> Snapshot()
    {
        return _effects.ToList().AsReadOnly();
    }

    public void Clear()
    {
        _effects.Clear();
    }
}
