namespace ClubDoorman.Effects;

public interface IEffectBus
{
    Task ExecuteAsync(IEnumerable<IEffect> effects, CancellationToken ct);
}


public sealed class EffectBus : IEffectBus
{
    public async Task ExecuteAsync(IEnumerable<IEffect> effects, CancellationToken ct)
    {
        foreach (var e in effects)
            await e.ExecuteAsync(ct);
    }
}
