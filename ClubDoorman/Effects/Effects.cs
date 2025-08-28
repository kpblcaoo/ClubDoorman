namespace ClubDoorman.Effects;

public interface IEffect
{
    Task ExecuteAsync(CancellationToken ct);
}

/// универсальный контейнер для "выполни вот этот кусок"
public sealed class FuncEffect : IEffect
{
    private readonly Func<CancellationToken, Task> _run;
    public FuncEffect(Func<CancellationToken, Task> run) => _run = run;
    public Task ExecuteAsync(CancellationToken ct) => _run(ct);
}