using ClubDoorman.Models.Logging;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.Logging;
using ClubDoorman.Models; // ModerationAction
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services.Handlers.Pipeline.Steps;

/// <summary>
/// 210: AiProfileAnalysisStep — выполняет AI анализ профиля если ModerationResult.Allow.
/// При срабатывании ограничений публикует событие и завершает конвейер (ранний выход, без action).
/// </summary>
public class AiProfileAnalysisStep : IMessageStep
{
    private readonly IAiCascadeService _aiCascadeService;
    private readonly IModerationEventPublisher _events;
    private readonly ILogger<AiProfileAnalysisStep> _logger;

    public int Order => 210;
    public string Name => nameof(AiProfileAnalysisStep);

    public AiProfileAnalysisStep(
        IAiCascadeService aiCascadeService,
        IModerationEventPublisher events,
        ILogger<AiProfileAnalysisStep> logger)
    {
        _aiCascadeService = aiCascadeService;
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
        if (mod.Action != ModerationAction.Allow) return StepResult.Continue();

        _logger.LogTrace("[Pipeline] AiProfileAnalysisStep.PerformAiProfileAnalysisAsync user={UserId}", user.Id);
        bool restricted = false;
        try
        {
            restricted = await _aiCascadeService.PerformAiProfileAnalysisAsync(context.Message, user, context.Chat, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AiProfileAnalysisStep: exception during AI profile analysis (continuing)");
            restricted = false;
        }
        if (!restricted) return StepResult.Continue();
        context.AiProfileRestricted = true;
        var resultObj = new { kind = "ai_profile_restricted", ruleCode = "AiProfileRestricted" };
        context.UserResult = resultObj;
        context.UserResultHandled = true;
        _events.Publish(context.GmCorrelation, new ModerationEvent("ai_profile_restricted", Action: null, RuleCode: RuleCode.AiProfileRestricted));
        return StepResult.StopOk("ai_profile_restricted");
    }
}
