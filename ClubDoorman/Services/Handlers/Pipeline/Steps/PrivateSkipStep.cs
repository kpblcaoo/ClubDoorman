using ClubDoorman.Models.Logging;
using ClubDoorman.Services.Logging;
using ClubDoorman.Services.Core.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Services.Handlers.Pipeline.Steps;

/// <summary>
/// Пропуск приватных чатов (не команды) с публикацией события private_skip.
/// </summary>
public class PrivateSkipStep : IMessageStep
{
    private readonly IModerationEventPublisher _events;
    private readonly ILogger<PrivateSkipStep> _logger;
    private readonly IAppConfig _appConfig; // может понадобиться для расширений (лог чатов, whitelist) в будущем

    public int Order => 50;
    public string Name => nameof(PrivateSkipStep);

    public PrivateSkipStep(IModerationEventPublisher events, IAppConfig appConfig, ILogger<PrivateSkipStep> logger)
    {
        _events = events;
        _appConfig = appConfig;
        _logger = logger;
    }

    public Task<StepResult> ExecuteAsync(MessageContext context, CancellationToken cancellationToken)
    {
        var msg = context.Message;
        if (msg.Chat.Type != ChatType.Private) return Task.FromResult(StepResult.Continue());
        if (msg.Text?.StartsWith("/") == true) return Task.FromResult(StepResult.Continue()); // обработают предыдущие шаги
        _logger.LogDebug("[Pipeline] PrivateSkipStep skipping non-command private message {MessageId}", msg.MessageId);
        context.PrivateSkipHandled = true;
        _events.Publish(context.GmCorrelation, new ModerationEvent("private_skip", Action: null, RuleCode: RuleCode.PrivateSkip));
        return Task.FromResult(StepResult.StopOk("private-skip"));
    }
}
