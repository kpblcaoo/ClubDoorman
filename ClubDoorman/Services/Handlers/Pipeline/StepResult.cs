namespace ClubDoorman.Services.Handlers.Pipeline;

/// <summary>
/// Результат выполнения шага пайплайна.
/// </summary>
public sealed record StepResult(bool Stop, bool Failed = false, Exception? Error = null, string? Reason = null)
{
    public static StepResult Continue() => new(false);
    public static StepResult StopOk(string? reason = null) => new(true, false, null, reason);
    public static StepResult Fail(Exception ex, string? reason = null) => new(true, true, ex, reason ?? ex.Message);
}
