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

            // Summary at Debug; full JSON at Trace
            _logger.LogDebug("GM Event corr={Correlation} kind={Kind} action={Action} rule={Rule} count={Count} msg={MessageId}",
                correlationId, evt.Kind, evt.Action, evt.RuleCode, evt.Count, evt.MessageId);
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("GM Event payload: {Payload}", System.Text.Json.JsonSerializer.Serialize(payload));
            }
            _recorder.TryRecordOutput(correlationId, payload);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "GoldenMasterModerationEventPublisher: publish failed");
        }
    }
}
