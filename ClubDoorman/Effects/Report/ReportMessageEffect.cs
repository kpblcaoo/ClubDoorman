using ClubDoorman.Services.Messaging;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Effects.Report;

/// <summary>
/// Эффект для отправки сообщения в админ-чат без удаления
/// Используется для Report и RequireManualReview действий
/// <tags>effects, report, moderation</tags>
/// </summary>
public class ReportMessageEffect : IEffect
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<ReportMessageEffect> _logger;
    private readonly Message _message;
    private readonly User _user;
    private readonly bool _isSilentMode;

    public ReportMessageEffect(
        INotificationService notificationService,
        ILogger<ReportMessageEffect> logger,
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
        await _notificationService.DontDeleteButReportMessage(_message, _user, _isSilentMode, ct);
    }
}
