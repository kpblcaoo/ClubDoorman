using ClubDoorman.Models.Logging;
using ClubDoorman.Services.Logging;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Models; // ModerationAction
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services.Handlers.Pipeline.Steps;

/// <summary>
/// 220: FinalModerationActionStep — вызывает HandleUserMessageAsync с результатом и формирует итоговое событие moderated.
/// Останавливает конвейер. RuleCode маппинг дублирует legacy поведение.
/// </summary>
public class FinalModerationActionStep : IMessageStep
{
    private readonly IModerationFacade _moderationFacade;
    private readonly IModerationEventPublisher _events;
    private readonly ILogger<FinalModerationActionStep> _logger;

    public int Order => 220;
    public string Name => nameof(FinalModerationActionStep);

    public FinalModerationActionStep(
        IModerationFacade moderationFacade,
        IModerationEventPublisher events,
        ILogger<FinalModerationActionStep> logger)
    {
        _moderationFacade = moderationFacade;
        _events = events;
        _logger = logger;
    }

    public async Task<StepResult> ExecuteAsync(MessageContext context, CancellationToken cancellationToken)
    {
        if (context.UserResultHandled) return StepResult.Continue();
        var user = context.User;
        if (user == null || user.IsBot) return StepResult.Continue();
        var mod = context.ModerationResult;
        if (mod == null) return StepResult.Continue();

        _logger.LogTrace("[Pipeline] FinalModerationActionStep.HandleUserMessageAsync user={UserId} action={Action}", user.Id, mod.Action);
        try
        {
            await _moderationFacade.HandleUserMessageAsync(context.Message, user, context.Chat, mod, context.IsSilentMode, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FinalModerationActionStep: exception in HandleUserMessageAsync (continuing but recording moderated_generic)");
        }
        var moderatedRule = mod.Action switch
        {
            ModerationAction.Allow => RuleCode.ModeratedAllow,
            ModerationAction.Delete => RuleCode.ModeratedDelete,
            ModerationAction.Ban => RuleCode.ModeratedBan,
            ModerationAction.Report => RuleCode.ModeratedReport,
            _ => RuleCode.ModeratedGeneric
        };
        var userResult = new { kind = "moderated", action = mod.Action.ToString(), reason = mod.Reason, ruleCode = moderatedRule.ToString() };
        context.UserResult = userResult;
        context.UserResultHandled = true;
        _events.Publish(context.GmCorrelation, new ModerationEvent("moderated", Action: mod.Action.ToString(), RuleCode: moderatedRule));
        return StepResult.StopOk("moderated");
    }
}
