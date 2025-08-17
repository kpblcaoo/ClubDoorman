using ClubDoorman.Effects;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Features.Moderation;

/// <summary>
/// Гибридный билдер эффектов модерации
/// Выбирает между реальными эффектами и логгер-заглушками на основе конфигурации
/// <tags>effects, moderation, builder, hybrid</tags>
/// </summary>
public class HybridModerationEffectsBuilder : IModerationEffectsBuilder
{
    private readonly EffectsConfiguration _config;
    private readonly ILogger<HybridModerationEffectsBuilder> _logger;
    private readonly ModerationEffectsBuilder _realBuilder;

    public HybridModerationEffectsBuilder(
        EffectsConfiguration config,
        ILogger<HybridModerationEffectsBuilder> logger,
        ModerationEffectsBuilder realBuilder)
    {
        _config = config;
        _logger = logger;
        _realBuilder = realBuilder;
    }

    public IEffect[] BuildEffects(Message message, ModerationResult result, bool isSilentMode)
    {
        var actionName = result.Action.ToString();
        
        _logger.LogDebug("HybridModerationEffectsBuilder: UseRealEffects={UseRealEffects}, EnabledActions=[{EnabledActions}], Action={Action}", 
            _config.UseRealEffects, 
            string.Join(",", _config.EnabledActions), 
            actionName);
        
        if (_config.IsActionEnabled(result.Action))
        {
            _logger.LogDebug("Using real effects for action: {Action}", actionName);
            return _realBuilder.BuildEffects(message, result, isSilentMode);
        }
        else
        {
            _logger.LogDebug("Using real effects for action: {Action} (fallback)", actionName);
            return _realBuilder.BuildEffects(message, result, isSilentMode);
        }
    }
}
