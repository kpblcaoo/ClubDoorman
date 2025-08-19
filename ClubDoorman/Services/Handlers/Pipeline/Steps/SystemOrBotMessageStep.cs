using ClubDoorman.Models.Logging;
using ClubDoorman.Services.Logging;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services.Handlers.Pipeline.Steps;

/// <summary>
/// 15: SystemOrBotMessageStep — эмитирует семантики для системных сообщений без пользователя и сообщений от ботов.
/// Semantics parity with legacy removed code:
///   system_no_user -> ruleCode=SystemNoUser
///   bot_message    -> ruleCode=BotMessage
/// Publishes ModerationEvent and stops pipeline.
/// </summary>
public class SystemOrBotMessageStep : IMessageStep
{
    private readonly IModerationEventPublisher _events;
    private readonly ILogger<SystemOrBotMessageStep> _logger;
    public int Order => 15;
    public string Name => nameof(SystemOrBotMessageStep);

    public SystemOrBotMessageStep(IModerationEventPublisher events, ILogger<SystemOrBotMessageStep> logger)
    {
        _events = events;
        _logger = logger;
    }

    public Task<StepResult> ExecuteAsync(MessageContext context, CancellationToken cancellationToken)
    {
        if (context.UserResultHandled) return Task.FromResult(StepResult.Continue());
        var msg = context.Message;
        var from = msg.From;
        if (from == null)
        {
            _logger.LogDebug("[Pipeline] SystemOrBotMessageStep system_no_user messageId={MessageId}", msg.MessageId);
            context.UserResultHandled = true;
            context.UserResult = new { kind = "system_no_user", ruleCode = "SystemNoUser" };
            _events.Publish(context.GmCorrelation, new ModerationEvent("system_no_user", Action: null, RuleCode: RuleCode.SystemNoUser));
            return Task.FromResult(StepResult.StopOk("system-no-user"));
        }
    if (from.IsBot && msg.LeftChatMember == null) // allow LeftMemberCleanupStep to handle system left-member messages
        {
            _logger.LogDebug("[Pipeline] SystemOrBotMessageStep bot_message userId={UserId} messageId={MessageId}", from.Id, msg.MessageId);
            context.UserResultHandled = true;
            context.UserResult = new { kind = "bot_message", ruleCode = "BotMessage" };
            _events.Publish(context.GmCorrelation, new ModerationEvent("bot_message", Action: null, RuleCode: RuleCode.BotMessage));
            return Task.FromResult(StepResult.StopOk("bot-message"));
        }
        return Task.FromResult(StepResult.Continue());
    }
}
