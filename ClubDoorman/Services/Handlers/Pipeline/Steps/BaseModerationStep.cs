using ClubDoorman.Features.Moderation;
using ClubDoorman.Models.Logging;
using ClubDoorman.Services.Logging;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Models; // ModerationResult, ModerationAction
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services.Handlers.Pipeline.Steps;

/// <summary>
/// 200: BaseModerationStep — вызывает CheckMessageAsync и сохраняет ModerationResult в контекст.
/// Не публикует финальное событие (его сделает поздний шаг) и не останавливает конвейер.
/// Fail-safe: подставляет RequireManualReview при null или исключении.
/// </summary>
public class BaseModerationStep : IMessageStep
{
    private readonly IModerationFacade _moderationFacade;
    private readonly IUserFlowLogger _userFlowLogger;
    private readonly IModerationEventPublisher _events; // резерв для будущих метрик
    private readonly ILogger<BaseModerationStep> _logger;

    public int Order => 200;
    public string Name => nameof(BaseModerationStep);

    public BaseModerationStep(
        IModerationFacade moderationFacade,
        IUserFlowLogger userFlowLogger,
        IModerationEventPublisher events,
        ILogger<BaseModerationStep> logger)
    {
        _moderationFacade = moderationFacade;
        _userFlowLogger = userFlowLogger;
        _events = events;
        _logger = logger;
    }

    public async Task<StepResult> ExecuteAsync(MessageContext context, CancellationToken cancellationToken)
    {
        var user = context.User;
        if (user == null || user.IsBot) return StepResult.Continue();
        if (context.UserResultHandled) return StepResult.Continue(); // уже ранний выход (captcha/banlist/...)
        if (context.ModerationResult != null) return StepResult.Continue(); // уже выполнено
        try
        {
            _logger.LogTrace("[Pipeline] BaseModerationStep.CheckMessageAsync user={UserId} chat={ChatId}", user.Id, context.Chat.Id);
            var result = await _moderationFacade.CheckMessageAsync(context.Message);
            if (result == null)
            {
                _logger.LogWarning("BaseModerationStep: moderationResult == null, substituting RequireManualReview");
                result = new ModerationResult(ModerationAction.RequireManualReview, "Null moderation result", 0);
            }
            context.ModerationResult = result;
            _userFlowLogger.LogModerationResult(user, context.Chat, result.Action.ToString(), result.Reason, result.Confidence);
            _logger.LogDebug("[Pipeline] BaseModerationStep moderation result Action={Action} Reason={Reason} Conf={Conf}", result.Action, result.Reason, result.Confidence);
        }
        catch (Exception ex)
        {
            // Preserve legacy error log phrase so existing tests that verify 'Ошибка при модерации сообщения' still pass
            _logger.LogError(ex, "Ошибка при модерации сообщения. (pipeline) user={UserId} chat={ChatId} msg={MessageId}", user?.Id, context.Chat.Id, context.Message?.MessageId);
            _logger.LogError(ex, "BaseModerationStep exception, substituting RequireManualReview");
            context.ModerationResult = new ModerationResult(ModerationAction.RequireManualReview, "Ошибка модерации - требуется ручной анализ", 0);
        }
        return StepResult.Continue();
    }
}
