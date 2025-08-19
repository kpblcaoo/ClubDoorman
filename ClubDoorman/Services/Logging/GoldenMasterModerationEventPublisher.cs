using ClubDoorman.Models.Logging;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services.Logging;

/// <summary>
/// Adapter: ModerationEvent -> GoldenMasterRecorder.TryRecordOutput.
/// </summary>
public sealed class GoldenMasterModerationEventPublisher : IModerationEventPublisher
{
    private readonly IGoldenMasterRecorder _recorder;
    private readonly ILogger<GoldenMasterModerationEventPublisher> _logger;
    public GoldenMasterModerationEventPublisher(IGoldenMasterRecorder recorder, ILogger<GoldenMasterModerationEventPublisher> logger)
    {
        _recorder = recorder;
        _logger = logger;
    }

    public void Publish(string? correlationId, ModerationEvent evt)
    {
        if (correlationId == null)
        {
            _logger.LogTrace("GM Event skipped (no correlation): {Kind}", evt.Kind);
            return; // no active recording
        }
        try
        {
            _logger.LogTrace("GM Event publish: corr={Correlation} kind={Kind} action={Action} rule={Rule}", correlationId, evt.Kind, evt.Action, evt.RuleCode);
            var payload = new
            {
                kind = evt.Kind,
                action = evt.Action,
                ruleCode = evt.RuleCode?.ToString(),
                count = evt.Count,
                messageId = evt.MessageId,
                status = evt.Status,
                extra = evt.Extra
            };
            _logger.LogTrace("GM Event payload prepared: {Payload}", System.Text.Json.JsonSerializer.Serialize(payload));
            _recorder.TryRecordOutput(correlationId, payload);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "GoldenMasterModerationEventPublisher: publish failed");
        }
    }
}
