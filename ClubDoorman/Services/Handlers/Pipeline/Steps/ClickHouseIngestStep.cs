using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Services.ClickHouse;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClubDoorman.Services.Handlers.Pipeline.Steps;

/// <summary>
/// Enqueues processed messages for ClickHouse analytics ingestion.
/// </summary>
public sealed class ClickHouseIngestStep : IMessageStep
{
    private readonly IClickHouseMessageSink _sink;
    private readonly IOptions<ClickHouseOptions> _options;
    private readonly ILogger<ClickHouseIngestStep> _logger;

    public ClickHouseIngestStep(IClickHouseMessageSink sink, IOptions<ClickHouseOptions> options, ILogger<ClickHouseIngestStep> logger)
    {
        _sink = sink;
        _options = options;
        _logger = logger;
    }

    public int Order => 135;

    public string Name => nameof(ClickHouseIngestStep);

    public async Task<StepResult> ExecuteAsync(MessageContext context, CancellationToken cancellationToken)
    {
        var opts = _options.Value;
        if (!opts.Enabled)
        {
            return StepResult.Continue();
        }

        if (context.Message == null)
        {
            return StepResult.Continue();
        }

        if (!ClickHouseMessageRecordFactory.TryCreate(context.Message, opts, out var record))
        {
            return StepResult.Continue();
        }

        var accepted = await _sink.TryEnqueueAsync(record, cancellationToken).ConfigureAwait(false);
        if (!accepted)
        {
            _logger.LogWarning("ClickHouse queue overflow while processing chat {ChatId} message {MessageId}.", record.ChatId, record.MessageId);
        }

        return StepResult.Continue();
    }
}
