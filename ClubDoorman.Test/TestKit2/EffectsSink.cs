namespace ClubDoorman.Tests.TestKit2;

public enum EffectType { Delete, Report, Ban, Warn, IncrementGood, AiCascade, LogChat }

public record Effect(EffectType Type, long ChatId, long? UserId = null, string? Reason = null, int? MsgId = null);

public interface IEffectsSink { void Add(Effect e); IReadOnlyList<Effect> Snapshot(); }

public sealed class EffectsSink : IEffectsSink {
    private readonly List<Effect> _list = new();
    public void Add(Effect e) => _list.Add(e);
    public IReadOnlyList<Effect> Snapshot() => _list.ToArray();
}
