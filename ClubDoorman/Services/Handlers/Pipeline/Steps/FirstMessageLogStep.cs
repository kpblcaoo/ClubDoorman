using ClubDoorman.Services.Logging;
using ClubDoorman.Services.UserFlow;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services.Handlers.Pipeline.Steps;

/// <summary>
/// 130: FirstMessageLogStep — логирует первое сообщение не одобренного пользователя.
/// Семантика: kind=system_no_user / bot_message / left_user_system cases already handled earlier; здесь только лог + pass-through.
/// Порождает событие first_message_log (Action: null) для Golden Master паритета (новое явное событие вместо скрытого).
/// </summary>
public class FirstMessageLogStep : IMessageStep
{
    private readonly IUserFlowLogger _userFlowLogger;
    private readonly IModerationEventPublisher _events;
    private readonly ILogger<FirstMessageLogStep> _logger;

    public int Order => 130;
    public string Name => nameof(FirstMessageLogStep);

    public FirstMessageLogStep(IUserFlowLogger userFlowLogger, IModerationEventPublisher events, ILogger<FirstMessageLogStep> logger)
    {
        _userFlowLogger = userFlowLogger;
        _events = events;
        _logger = logger;
    }

    public Task<StepResult> ExecuteAsync(MessageContext context, CancellationToken cancellationToken)
    {
        var msg = context.Message;
        var user = msg.From;
        if (user == null || user.IsBot) return Task.FromResult(StepResult.Continue());
        if (context.UserResultHandled) return Task.FromResult(StepResult.Continue()); // already exited earlier
        var text = msg.Text ?? msg.Caption ?? "[медиа/стикер/файл]";
        _logger.LogTrace("[Pipeline] FirstMessageLogStep logging first message for user {UserId}", user.Id);
        _userFlowLogger.LogFirstMessage(user, msg.Chat, text);
        _events.Publish(context.GmCorrelation, new Models.Logging.ModerationEvent("first_message_log", Action: null, RuleCode: null));
        return Task.FromResult(StepResult.Continue());
    }
}
