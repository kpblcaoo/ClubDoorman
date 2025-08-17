using System.Collections.Generic;
using ClubDoorman.Models;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using ClubDoorman.Effects;

namespace ClubDoorman.Effects.Channel;

/// <summary>
/// Интерфейс билдера эффектов для сообщений от каналов (этапная миграция).
/// </summary>
public interface IChannelModerationEffectsBuilder
{
    IEffect[] BuildChannelEffects(Message message, ModerationResult moderationResult);
}

/// <summary>
/// Билдер эффектов для каналов (Stage 1: только логирующие заглушки, без реальных side-effect операций).
/// На следующих этапах заглушки будут заменены на реальные эффекты удаления/бана и т.д.
/// </summary>
public class ChannelModerationEffectsBuilder : IChannelModerationEffectsBuilder
{
    private readonly ILogger<ChannelModerationEffectsBuilder> _logger;
    private readonly ITelegramBotClientWrapper? _bot;
    private readonly IUserBanService? _banService;
    private readonly IModerationService? _moderationService;

    public ChannelModerationEffectsBuilder(ILogger<ChannelModerationEffectsBuilder> logger, ITelegramBotClientWrapper? bot = null, IUserBanService? banService = null, IModerationService? moderationService = null)
    { _logger = logger; _bot = bot; _banService = banService; _moderationService = moderationService; }

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
                if (_bot != null)
                    effects.Add(new ChannelDeleteMessageEffect(_bot, message, moderationResult, _logger));
                break;
            case ModerationAction.Report:
                effects.Add(new ChannelReportMessageEffect(message, moderationResult, _logger));
                break;
            case ModerationAction.Ban:
                if (_banService != null)
                    effects.Add(new ChannelBanEffect(_banService, message, moderationResult, _logger));
                break;
            default:
                effects.Add(new ChannelUnknownActionEffect(message, moderationResult, _logger));
                break;
        }
        return effects.ToArray();
    }
}
