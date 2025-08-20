using ClubDoorman.Features.AdminOps;
using ClubDoorman.Services.Logging; // for IModerationEventPublisher
using ClubDoorman.Models.Logging; // ModerationEvent, RuleCode
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Services.Handlers.Pipeline.Steps;

/// <summary>
/// Шаг обработки команд ( /command ). Повторяет текущую семантику MessageHandler для ранней остановки.
/// </summary>
public class CommandStep : IMessageStep
{
    private readonly ICommandRouter _commandRouter;
    private readonly IModerationEventPublisher _events;
    private readonly ILogger<CommandStep> _logger;

    public int Order => 10; // Ранний шаг
    public string Name => nameof(CommandStep);

    public CommandStep(ICommandRouter commandRouter, IModerationEventPublisher events, ILogger<CommandStep> logger)
    {
        _commandRouter = commandRouter;
        _events = events;
        _logger = logger;
    }

    public async Task<StepResult> ExecuteAsync(MessageContext context, CancellationToken cancellationToken)
    {
    var message = context.Message;
    if (message.Text == null || !message.Text.StartsWith("/"))
            return StepResult.Continue();

        _logger.LogDebug("[Pipeline] CommandStep handling command {Command}", message.Text);
        try
        {
            var handled = await _commandRouter.HandleCommandAsync(message, cancellationToken);
            if (handled)
            {
                context.CommandHandled = true;
                _events.Publish(context.GmCorrelation, new ModerationEvent("command", Action: "Allow", RuleCode: RuleCode.Command, MessageId: message.MessageId));
                return StepResult.StopOk("command-handled");
            }
            return StepResult.Continue();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка в CommandStep для команды {Command}", message.Text);
            return StepResult.Fail(ex, "command-step-exception");
        }
    }
}
