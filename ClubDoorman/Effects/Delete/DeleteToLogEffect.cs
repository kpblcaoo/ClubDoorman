using ClubDoorman.Services.Messaging;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Effects.Delete;

/// <summary>
/// Эффект для удаления сообщения и отправки в лог-чат
/// Используется для ссылок и банальных приветствий без предупреждения пользователю
/// <tags>effects, delete, moderation</tags>
/// </summary>
public class DeleteToLogEffect : IEffect
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<DeleteToLogEffect> _logger;
    private readonly Message _message;
    private readonly string _reason;

    public DeleteToLogEffect(
        INotificationService notificationService,
        ILogger<DeleteToLogEffect> logger,
        Message message,
        string reason)
    {
        _notificationService = notificationService;
        _logger = logger;
        _message = message;
        _reason = reason;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        await _notificationService.DeleteAndReportToLogChat(_message, _reason, ct);
    }
}
