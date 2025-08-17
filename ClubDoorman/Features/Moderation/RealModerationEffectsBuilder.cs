using ClubDoorman.Effects;
using ClubDoorman.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Features.Moderation;

/// <summary>
/// Реальный билдер эффектов модерации
/// Будет заполняться по мере миграции действий
/// <tags>effects, moderation, builder, real</tags>
/// </summary>
public class RealModerationEffectsBuilder : IModerationEffectsBuilder
{
    private readonly ILogger<RealModerationEffectsBuilder> _logger;

    public RealModerationEffectsBuilder(ILogger<RealModerationEffectsBuilder> logger)
    {
        _logger = logger;
    }

    public IEffect[] BuildEffects(Message message, ModerationResult result, bool isSilentMode)
    {
        _logger.LogDebug("Real effects not implemented yet for action: {Action}", result.Action);
        
        // Пока возвращаем пустой массив - эффекты будут добавляться по мере миграции
        return Array.Empty<IEffect>();
    }
}
