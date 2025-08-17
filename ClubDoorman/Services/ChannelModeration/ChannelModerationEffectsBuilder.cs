using System.Collections.Generic;
using ClubDoorman.Models;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using ClubDoorman.Effects;
using ClubDoorman.Effects.Channel; // effect classes live there

namespace ClubDoorman.Services.ChannelModeration;

/// <summary>
/// Интерфейс билдера эффектов для сообщений от каналов.
/// </summary>
public interface IChannelModerationEffectsBuilder
{
    IEffect[] BuildChannelEffects(Message message, ModerationResult moderationResult);
}

/// <summary>
/// Финальный билдер эффектов: проецирует ModerationResult.Action в конкретные side-effect эффекты.
/// </summary>
public class ChannelModerationEffectsBuilder : IChannelModerationEffectsBuilder
{
    private readonly ILogger<ChannelModerationEffectsBuilder> _logger;
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IUserBanService _banService;
    private readonly IModerationService _moderationService;

    public ChannelModerationEffectsBuilder(
        ILogger<ChannelModerationEffectsBuilder> logger,
        ITelegramBotClientWrapper bot,
        IUserBanService banService,
        IModerationService moderationService)
    {
        _logger = logger;
        _bot = bot;
        _banService = banService;
        _moderationService = moderationService;
    }

    public IEffect[] BuildChannelEffects(Message message, ModerationResult moderationResult)
    {
        var effects = new List<IEffect>();
        switch (moderationResult.Action)
        {
            case ModerationAction.Allow:
                // Полное воспроизведение legacy: лог + AI detect + increment good message (если применимо)
                effects.Add(new ChannelAllowEffect(_moderationService, _logger, message, moderationResult));
                break;
            case ModerationAction.Delete:
                effects.Add(new ChannelDeleteMessageEffect(_bot, message, moderationResult, _logger));
                break;
            case ModerationAction.Report:
                effects.Add(new ChannelReportMessageEffect(message, moderationResult, _logger));
                break;
            case ModerationAction.Ban:
                effects.Add(new ChannelBanEffect(_banService, message, moderationResult, _logger));
                break;
            default:
                effects.Add(new ChannelUnknownActionEffect(message, moderationResult, _logger));
                break;
        }
        return effects.ToArray();
    }
}
