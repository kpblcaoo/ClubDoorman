using ClubDoorman.Models.Logging;
using ClubDoorman.Services.Logging;
using ClubDoorman.Features.Moderation;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services.Handlers.Pipeline.Steps;

/// <summary>
/// 120: AlreadyApprovedStep — если пользователь уже одобрен в чате, ранний allow.
/// Семантика: kind=already_approved, action=Allow, ruleCode=AlreadyApproved.
/// </summary>
public class AlreadyApprovedStep : IMessageStep
{
    private readonly IModerationFacade _moderationFacade;
    private readonly IModerationEventPublisher _events;
    private readonly ILogger<AlreadyApprovedStep> _logger;

    public int Order => 120;
    public string Name => nameof(AlreadyApprovedStep);

    public AlreadyApprovedStep(IModerationFacade moderationFacade, IModerationEventPublisher events, ILogger<AlreadyApprovedStep> logger)
    {
        _moderationFacade = moderationFacade;
        _events = events;
        _logger = logger;
    }

    public Task<StepResult> ExecuteAsync(MessageContext context, CancellationToken cancellationToken)
    {
        var msg = context.Message;
        var user = msg.From;
        if (user == null || user.IsBot) return Task.FromResult(StepResult.Continue());
        if (!_moderationFacade.IsUserApproved(user.Id, msg.Chat.Id)) return Task.FromResult(StepResult.Continue());
        _logger.LogDebug("[Pipeline] AlreadyApprovedStep skip moderation for user {UserId}", user.Id);
        context.AlreadyApprovedHandled = true;
        var resultObj = new { kind = "already_approved", action = "Allow", ruleCode = "AlreadyApproved" };
        context.UserResult = resultObj;
        context.UserResultHandled = true;
        _events.Publish(context.GmCorrelation, new ModerationEvent("already_approved", Action: "Allow", RuleCode: RuleCode.AlreadyApproved));
        return Task.FromResult(StepResult.StopOk("already-approved"));
    }
}
