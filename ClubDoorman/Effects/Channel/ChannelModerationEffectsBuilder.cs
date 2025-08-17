using ClubDoorman.Models;
using ClubDoorman.Services.Moderation;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

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

    public ChannelModerationEffectsBuilder(ILogger<ChannelModerationEffectsBuilder> logger)
    {
        _logger = logger;
    }

    public IEffect[] BuildChannelEffects(Message message, ModerationResult moderationResult)
    {
        var senderTitle = message.SenderChat?.Title ?? message.SenderChat?.Id.ToString() ?? "<unknown channel>";
        var reason = moderationResult.Reason ?? "<no-reason>";

        // Stage 1: возвращаем только логирующие эффекты для наблюдения порядка выполнения.
        return new IEffect[]
        {
            new FuncEffect(async ct =>
            {
                _logger.LogInformation("[ChannelEffects][STAGE1] Action={Action} Channel={Channel} Chat={Chat} Reason={Reason}",
                    moderationResult.Action, senderTitle, message.Chat.Title, reason);
                await Task.CompletedTask;
            })
        };
    }
}
