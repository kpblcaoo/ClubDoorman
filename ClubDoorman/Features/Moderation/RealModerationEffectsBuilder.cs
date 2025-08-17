using ClubDoorman.Effects;
using ClubDoorman.Effects.Delete;
using ClubDoorman.Models;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using System.Collections.Generic;

namespace ClubDoorman.Features.Moderation;

/// <summary>
/// Реальный билдер эффектов модерации
/// Будет заполняться по мере миграции действий
/// <tags>effects, moderation, builder, real</tags>
/// </summary>
public class RealModerationEffectsBuilder : IModerationEffectsBuilder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RealModerationEffectsBuilder> _logger;

    public RealModerationEffectsBuilder(
        IServiceProvider serviceProvider,
        ILogger<RealModerationEffectsBuilder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IEffect[] BuildEffects(Message message, ModerationResult result, bool isSilentMode)
    {
        var effects = new List<IEffect>();

        switch (result.Action)
        {
            case ModerationAction.Delete:
                _logger.LogInformation("Удаление сообщения: {Reason}", result.Reason);

                if (result.Reason.Contains("Ссылки запрещены") || result.Reason.Contains("Банальное приветствие"))
                {
                    effects.Add(new DeleteToLogEffect(
                        _serviceProvider.GetRequiredService<INotificationService>(),
                        _serviceProvider.GetRequiredService<ILogger<DeleteToLogEffect>>(),
                        message,
                        result.Reason));
                }
                else
                {
                    effects.Add(new DeleteWithReportEffect(
                        _serviceProvider.GetRequiredService<INotificationService>(),
                        _serviceProvider.GetRequiredService<ILogger<DeleteWithReportEffect>>(),
                        message,
                        result.Reason,
                        isSilentMode));
                }

                effects.Add(new TrackViolationEffect(
                    _serviceProvider.GetRequiredService<IUserBanService>(),
                    _serviceProvider.GetRequiredService<ILogger<TrackViolationEffect>>(),
                    message,
                    message.From!,
                    result.Reason));
                break;

            default:
                _logger.LogDebug("Real effects not implemented yet for action: {Action}", result.Action);
                break;
        }

        return effects.ToArray();
    }
}
