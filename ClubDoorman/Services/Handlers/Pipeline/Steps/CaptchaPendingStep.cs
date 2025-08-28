using ClubDoorman.Models.Logging;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.Logging;
using ClubDoorman.Services.Telegram;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services.Handlers.Pipeline.Steps;

/// <summary>
/// 100: CaptchaPendingStep — удаляет сообщения пользователей, которые ещё не прошли капчу.
/// Семантика: kind=captcha_pending, action=Delete, ruleCode=CaptchaPending.
/// </summary>
public class CaptchaPendingStep : IMessageStep
{
    private readonly ICaptchaService _captchaService;
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IModerationEventPublisher _events;
    private readonly ILogger<CaptchaPendingStep> _logger;

    public int Order => 100;
    public string Name => nameof(CaptchaPendingStep);

    public CaptchaPendingStep(ICaptchaService captchaService, ITelegramBotClientWrapper bot, IModerationEventPublisher events, ILogger<CaptchaPendingStep> logger)
    {
        _captchaService = captchaService;
        _bot = bot;
        _events = events;
        _logger = logger;
    }

    public async Task<StepResult> ExecuteAsync(MessageContext context, CancellationToken cancellationToken)
    {
        var msg = context.Message;
        var user = msg.From;
        if (user == null) return StepResult.Continue();
        var captchaKey = _captchaService.GenerateKey(msg.Chat.Id, user.Id);
        var info = _captchaService.GetCaptchaInfo(captchaKey);
        if (info == null) return StepResult.Continue();
        _logger.LogDebug("[Pipeline] CaptchaPendingStep deleting message {MessageId} from user {UserId} pending captcha", msg.MessageId, user.Id);
        try { await _bot.DeleteMessage(msg.Chat.Id, msg.MessageId, cancellationToken); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed deleting captcha pending message {MessageId}", msg.MessageId); }
        context.CaptchaPendingHandled = true;
        var resultObj = new { kind = "captcha_pending", action = "Delete", ruleCode = "CaptchaPending" };
        context.UserResult = resultObj;
        context.UserResultHandled = true;
        _events.Publish(context.GmCorrelation, new ModerationEvent("captcha_pending", Action: "Delete", RuleCode: RuleCode.CaptchaPending));
        return StepResult.StopOk("captcha-pending");
    }
}
