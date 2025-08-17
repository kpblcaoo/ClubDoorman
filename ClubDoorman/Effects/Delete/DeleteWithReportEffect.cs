using ClubDoorman.Services.Messaging;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Effects.Delete;

/// <summary>
/// Эффект для удаления сообщения с отчетом
/// Используется для общих случаев удаления (не ссылки/банальные приветствия)
/// <tags>effects, delete, moderation</tags>
/// </summary>
public class DeleteWithReportEffect : IEffect
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<DeleteWithReportEffect> _logger;
    private readonly Message _message;
    private readonly string _reason;
    private readonly bool _isSilentMode;

    public DeleteWithReportEffect(
        INotificationService notificationService,
        ILogger<DeleteWithReportEffect> logger,
        Message message,
        string reason,
        bool isSilentMode)
    {
        _notificationService = notificationService;
        _logger = logger;
        _message = message;
        _reason = reason;
        _isSilentMode = isSilentMode;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        await _notificationService.DeleteAndReportMessage(_message, _reason, _isSilentMode, ct);
    }
}
