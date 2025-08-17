using ClubDoorman.Services.Messaging;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Effects.ManualReview;

/// <summary>
/// Эффект для сообщений, требующих ручной проверки
/// <tags>effects, manual-review, moderation</tags>
/// </summary>
public class RequireManualReviewEffect : IEffect
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<RequireManualReviewEffect> _logger;
    private readonly Message _message;
    private readonly User _user;
    private readonly bool _isSilentMode;

    public RequireManualReviewEffect(
        INotificationService notificationService,
        ILogger<RequireManualReviewEffect> logger,
        Message message,
        User user,
        bool isSilentMode)
    {
        _notificationService = notificationService;
        _logger = logger;
        _message = message;
        _user = user;
        _isSilentMode = isSilentMode;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Требует ручной проверки: {Reason}", "RequireManualReview");
        await _notificationService.DontDeleteButReportMessage(_message, _user, _isSilentMode, ct);
    }
}
