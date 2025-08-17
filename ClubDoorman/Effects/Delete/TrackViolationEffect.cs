using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Effects.Delete;

/// <summary>
/// Эффект для отслеживания нарушений пользователя
/// Отслеживает нарушения для повторных банов
/// <tags>effects, delete, moderation, violations</tags>
/// </summary>
public class TrackViolationEffect : IEffect
{
    private readonly IUserBanService _userBanService;
    private readonly ILogger<TrackViolationEffect> _logger;
    private readonly Message _message;
    private readonly User _user;
    private readonly string _reason;

    public TrackViolationEffect(
        IUserBanService userBanService,
        ILogger<TrackViolationEffect> logger,
        Message message,
        User user,
        string reason)
    {
        _userBanService = userBanService;
        _logger = logger;
        _message = message;
        _user = user;
        _reason = reason;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        await _userBanService.TrackViolationAndBanIfNeededAsync(_message, _user, _reason, ct);
    }
}
