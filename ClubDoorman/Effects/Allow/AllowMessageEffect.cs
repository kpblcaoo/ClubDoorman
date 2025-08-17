using ClubDoorman.Features.Moderation;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Effects.Allow;

/// <summary>
/// Эффект для разрешения сообщения
/// <tags>effects, allow, moderation</tags>
/// </summary>
public class AllowMessageEffect : IEffect
{
    private readonly IModerationPolicy _moderationPolicy;
    private readonly ILogger<AllowMessageEffect> _logger;
    private readonly Message _message;
    private readonly User _user;
    private readonly Chat _chat;
    private readonly string _reason;

    public AllowMessageEffect(
        IModerationPolicy moderationPolicy,
        ILogger<AllowMessageEffect> logger,
        Message message,
        User user,
        Chat chat,
        string reason)
    {
        _moderationPolicy = moderationPolicy;
        _logger = logger;
        _message = message;
        _user = user;
        _chat = chat;
        _reason = reason;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogDebug("Сообщение разрешено: {Reason}", _reason);
        var allowedMessageText = _message.Text ?? _message.Caption ?? "";

        // Проверяем AI детект для подозрительных пользователей
        var aiDetectBlocked = await _moderationPolicy.CheckAiDetectAndNotifyAdminsAsync(_user, _chat, _message);

        // Засчитываем хорошее сообщение только если пользователь не был заблокирован AI детектом
        if (!aiDetectBlocked)
        {
            await _moderationPolicy.IncrementGoodMessageCountAsync(_user, _chat, allowedMessageText);
        }
    }
}
