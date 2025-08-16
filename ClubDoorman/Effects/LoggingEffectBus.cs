namespace ClubDoorman.Effects;




public sealed class LoggingEffectBus : IEffectBus
{
    private readonly ILogger<LoggingEffectBus> _logger;

    public LoggingEffectBus(ILogger<LoggingEffectBus> logger)
        => _logger = logger;

    public async Task ExecuteAsync(IEnumerable<IEffect> effects, CancellationToken ct)
    {
        foreach (var effect in effects)
        {
            _logger.LogInformation("[EFFECT] Would run: {Effect}", effect.GetType().Name);
        }

        await Task.CompletedTask;
    }
}
// This class is a no-op implementation of IEffectBus that logs the effects instead of executing them.
// It can be used for debugging or testing purposes to see which effects would be executed without actually running them.