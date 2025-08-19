using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services.Handlers.Pipeline;

/// <summary>
/// Последовательно выполняет шаги. Пока не интегрирован в MessageHandler (Phase 1 scaffold).
/// </summary>
public class MessagePipeline : IMessagePipeline
{
    private readonly IReadOnlyList<IMessageStep> _steps;
    private readonly ILogger<MessagePipeline> _logger;

    public MessagePipeline(IEnumerable<IMessageStep> steps, ILogger<MessagePipeline> logger)
    {
        _steps = steps.OrderBy(s => s.Order).ToList();
        _logger = logger;
    }

    public async Task RunAsync(MessageContext ctx, CancellationToken cancellationToken)
    {
        foreach (var step in _steps)
        {
            StepResult result;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                result = await step.ExecuteAsync(ctx, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in step {Step}", step.Name);
                result = StepResult.Fail(ex);
            }
            finally { }

            sw.Stop();
            _logger.LogTrace("[Pipeline] Step {Step} completed in {Elapsed}ms (stop={Stop}, failed={Failed})", step.Name, sw.ElapsedMilliseconds, result.Stop, result.Failed);
            if (result.Failed || result.Stop) break;
        }
    }
}
