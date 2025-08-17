using ClubDoorman.Effects;
using ClubDoorman.Models;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Effects.Channel;

// Allow: воспроизводим legacy блок: лог + AI detect + increment
internal sealed class ChannelAllowEffect : IEffect
{
    private readonly IModerationService? _moderationService;
    private readonly ILogger _logger;
    private readonly Message _message;
    private readonly ModerationResult _result;
    public ChannelAllowEffect(IModerationService? moderationService, ILogger logger, Message message, ModerationResult result)
    { _moderationService = moderationService; _logger = logger; _message = message; _result = result; }
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var senderTitle = _message.SenderChat?.Title;
        _logger.LogDebug("✅ Содержимое сообщения от канала {ChannelTitle} разрешено: {Reason}", senderTitle, _result.Reason);
        if (_message.From != null && _moderationService != null)
        {
            var chat = _message.Chat;
            var allowedMessageText = _message.Text ?? _message.Caption ?? "";
            var aiDetectBlocked = await _moderationService.CheckAiDetectAndNotifyAdminsAsync(_message.From, chat, _message);
            if (!aiDetectBlocked)
            {
                await _moderationService.IncrementGoodMessageCountAsync(_message.From, chat, allowedMessageText);
            }
        }
    }
}

internal sealed class ChannelDeleteMessageEffect : IEffect
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly Message _message;
    private readonly ModerationResult _result;
    private readonly ILogger _logger;
    public ChannelDeleteMessageEffect(ITelegramBotClientWrapper bot, Message message, ModerationResult result, ILogger logger)
    { _bot = bot; _message = message; _result = result; _logger = logger; }
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var senderTitle = _message.SenderChat?.Title;
        _logger.LogInformation("🗑️ Удаляем сообщение от канала {ChannelTitle}: {Reason}", senderTitle, _result.Reason);
        await _bot.DeleteMessage(_message.Chat.Id, _message.MessageId, ct);
    }
}

internal sealed class ChannelReportMessageEffect : IEffect
{
    private readonly Message _message;
    private readonly ModerationResult _result;
    private readonly ILogger _logger;
    public ChannelReportMessageEffect(Message message, ModerationResult result, ILogger logger)
    { _message = message; _result = result; _logger = logger; }
    public Task ExecuteAsync(CancellationToken ct)
    {
        var senderTitle = _message.SenderChat?.Title;
        _logger.LogInformation("📋 Отправляем сообщение от канала {ChannelTitle} в админ-чат: {Reason}", senderTitle, _result.Reason);
        // Placeholder: в legacy комментарий "можно добавить отправку"
        return Task.CompletedTask;
    }
}

internal sealed class ChannelBanEffect : IEffect
{
    private readonly IUserBanService _banService;
    private readonly Message _message;
    private readonly ModerationResult _result;
    private readonly ILogger _logger;
    public ChannelBanEffect(IUserBanService banService, Message message, ModerationResult result, ILogger logger)
    { _banService = banService; _message = message; _result = result; _logger = logger; }
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var senderTitle = _message.SenderChat?.Title;
        _logger.LogWarning("🚫 Баним канал {ChannelTitle} за содержимое: {Reason}", senderTitle, _result.Reason);
        await _banService.AutoBanChannelAsync(_message, ct);
    }
}

internal sealed class ChannelUnknownActionEffect : IEffect
{
    private readonly Message _message;
    private readonly ModerationResult _result;
    private readonly ILogger _logger;
    public ChannelUnknownActionEffect(Message message, ModerationResult result, ILogger logger)
    { _message = message; _result = result; _logger = logger; }
    public Task ExecuteAsync(CancellationToken ct)
    {
        var senderTitle = _message.SenderChat?.Title;
        _logger.LogInformation("❓ Неизвестное действие модерации для канала {ChannelTitle}: {Action}", senderTitle, _result.Action);
        return Task.CompletedTask;
    }
}
