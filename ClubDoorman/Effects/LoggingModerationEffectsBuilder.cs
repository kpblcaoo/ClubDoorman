using Telegram.Bot.Types;
using ClubDoorman.Models;

namespace ClubDoorman.Effects;

public interface IModerationEffectsBuilder
{
    IEffect[] BuildEffects(Message message, ModerationResult result, bool isSilentMode);
}

public class LoggingModerationEffectsBuilder : IModerationEffectsBuilder
{
    private readonly ILogger<LoggingModerationEffectsBuilder> _logger;

    public LoggingModerationEffectsBuilder(ILogger<LoggingModerationEffectsBuilder> logger)
    {
        _logger = logger;
    }

    public IEffect[] BuildEffects(Message message, ModerationResult result, bool isSilentMode)
    {
        return new IEffect[]
        {
            new FuncEffect(ct =>
            {
                _logger.LogInformation("Запрошен эффект: {Action} для сообщения {Text}", result.Action, message.Text);
                return Task.CompletedTask;
            })
        };
    }
}