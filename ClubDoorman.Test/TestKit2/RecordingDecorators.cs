using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.AI;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using Telegram.Bot.Types;

namespace ClubDoorman.Tests.TestKit2;

public sealed class RecordingNotificationService : INotificationService
{
    private readonly INotificationService _inner;
    private readonly IEffectsSink _sink;

    public RecordingNotificationService(INotificationService inner, IEffectsSink sink)
    {
        _inner = inner;
        _sink = sink;
    }

    public async Task DeleteAndReportMessage(Message message, string reason, bool isSilentMode, CancellationToken cancellationToken)
    {
        _sink.Add(new Effect(EffectType.Delete, message.Chat.Id, message.From?.Id, reason, message.MessageId));
        await _inner.DeleteAndReportMessage(message, reason, isSilentMode, cancellationToken);
    }

    public async Task DeleteAndReportToLogChat(Message message, string reason, CancellationToken cancellationToken)
    {
        _sink.Add(new Effect(EffectType.LogChat, message.Chat.Id, message.From?.Id, reason, message.MessageId));
        await _inner.DeleteAndReportToLogChat(message, reason, cancellationToken);
    }

    public async Task DontDeleteButReportMessage(Message message, User user, bool isSilentMode, CancellationToken cancellationToken)
    {
        _sink.Add(new Effect(EffectType.Report, message.Chat.Id, user.Id, "suspicious"));
        await _inner.DontDeleteButReportMessage(message, user, isSilentMode, cancellationToken);
    }

    public async Task SendSuspiciousMessageWithButtons(Message message, User user, SuspiciousMessageNotificationData data, bool isSilentMode, CancellationToken cancellationToken)
    {
        _sink.Add(new Effect(EffectType.Report, message.Chat.Id, user.Id, "suspicious_with_buttons"));
        await _inner.SendSuspiciousMessageWithButtons(message, user, data, isSilentMode, cancellationToken);
    }
}

public sealed class RecordingUserBanService : IUserBanService
{
    private readonly IUserBanService _inner;
    private readonly IEffectsSink _sink;

    public RecordingUserBanService(IUserBanService inner, IEffectsSink sink)
    {
        _inner = inner;
        _sink = sink;
    }

    public async Task BanUserForLongNameAsync(Message? userJoinMessage, User user, string reason, TimeSpan? banDuration, CancellationToken cancellationToken)
    {
        var chatId = userJoinMessage?.Chat.Id ?? 0;
        _sink.Add(new Effect(EffectType.Ban, chatId, user.Id, reason));
        await _inner.BanUserForLongNameAsync(userJoinMessage, user, reason, banDuration, cancellationToken);
    }

    public async Task BanBlacklistedUserAsync(Message userJoinMessage, User user, CancellationToken cancellationToken)
    {
        _sink.Add(new Effect(EffectType.Ban, userJoinMessage.Chat.Id, user.Id, "blacklisted"));
        await _inner.BanBlacklistedUserAsync(userJoinMessage, user, cancellationToken);
    }

    public async Task AutoBanAsync(Message message, string reason, CancellationToken cancellationToken)
    {
        _sink.Add(new Effect(EffectType.Ban, message.Chat.Id, message.From?.Id, reason));
        await _inner.AutoBanAsync(message, reason, cancellationToken);
    }

    public async Task AutoBanChannelAsync(Message message, CancellationToken cancellationToken)
    {
        _sink.Add(new Effect(EffectType.Ban, message.Chat.Id, message.From?.Id, "channel"));
        await _inner.AutoBanChannelAsync(message, cancellationToken);
    }

    public async Task HandleBlacklistBanAsync(Message message, User user, Chat chat, CancellationToken cancellationToken)
    {
        _sink.Add(new Effect(EffectType.Ban, chat.Id, user.Id, "blacklist"));
        await _inner.HandleBlacklistBanAsync(message, user, chat, cancellationToken);
    }

    public async Task TrackViolationAndBanIfNeededAsync(Message message, User user, string reason, CancellationToken cancellationToken)
    {
        _sink.Add(new Effect(EffectType.Ban, message.Chat.Id, user.Id, reason));
        await _inner.TrackViolationAndBanIfNeededAsync(message, user, reason, cancellationToken);
    }

    public async Task BanUserAsync(Chat chat, User user, BanTypeEnum banType, string? customReason = null, Message? messageToDelete = null, CancellationToken cancellationToken = default)
    {
        _sink.Add(new Effect(EffectType.Ban, chat.Id, user.Id, customReason ?? banType.ToString()));
        await _inner.BanUserAsync(chat, user, banType, customReason, messageToDelete, cancellationToken);
    }

    public async Task BanUserAsync(Chat chat, User user, BanTypeEnum banType, string? customReason = null, long? messageIdToDelete = null, long? chatIdForMessage = null, CancellationToken cancellationToken = default)
    {
        _sink.Add(new Effect(EffectType.Ban, chat.Id, user.Id, customReason ?? banType.ToString()));
        await _inner.BanUserAsync(chat, user, banType, customReason, messageIdToDelete, chatIdForMessage, cancellationToken);
    }

    public async Task DeleteMessageByIdAsync(long chatId, int messageId)
    {
        _sink.Add(new Effect(EffectType.Delete, chatId, null, null, messageId));
        await _inner.DeleteMessageByIdAsync(chatId, messageId);
    }
}

public sealed class RecordingModerationService : IModerationService
{
    private readonly IModerationService _inner;
    private readonly IEffectsSink _sink;

    public RecordingModerationService(IModerationService inner, IEffectsSink sink)
    {
        _inner = inner;
        _sink = sink;
    }

    public async Task<ModerationResult> CheckMessageAsync(Message message)
    {
        var result = await _inner.CheckMessageAsync(message);
        if (result.Action == ModerationAction.Allow)
        {
            _sink.Add(new Effect(EffectType.IncrementGood, message.Chat.Id, message.From?.Id));
        }
        return result;
    }

    public async Task<ModerationResult> CheckUserNameAsync(User user)
    {
        return await _inner.CheckUserNameAsync(user);
    }

    public async Task ExecuteModerationActionAsync(Message message, ModerationResult result)
    {
        await _inner.ExecuteModerationActionAsync(message, result);
    }

    public bool IsUserApproved(long userId, long? chatId = null)
    {
        return _inner.IsUserApproved(userId, chatId);
    }

    public async Task IncrementGoodMessageCountAsync(User user, Chat chat, string messageText)
    {
        _sink.Add(new Effect(EffectType.IncrementGood, chat.Id, user.Id));
        await _inner.IncrementGoodMessageCountAsync(user, chat, messageText);
    }

    public bool SetAiDetectForSuspiciousUser(long userId, long chatId, bool enabled)
    {
        return _inner.SetAiDetectForSuspiciousUser(userId, chatId, enabled);
    }

    public (int TotalSuspicious, int WithAiDetect, int GroupsCount) GetSuspiciousUsersStats()
    {
        return _inner.GetSuspiciousUsersStats();
    }

    public List<(long UserId, long ChatId)> GetAiDetectUsers()
    {
        return _inner.GetAiDetectUsers();
    }

    public async Task<bool> CheckAiDetectAndNotifyAdminsAsync(User user, Chat chat, Message message)
    {
        return await _inner.CheckAiDetectAndNotifyAdminsAsync(user, chat, message);
    }

    public async Task<bool> UnrestrictAndApproveUserAsync(long userId, long chatId)
    {
        return await _inner.UnrestrictAndApproveUserAsync(userId, chatId);
    }

    public void CleanupUserFromAllLists(long userId, long chatId)
    {
        _inner.CleanupUserFromAllLists(userId, chatId);
    }

    public async Task<bool> BanAndCleanupUserAsync(long userId, long chatId, int? messageIdToDelete = null)
    {
        return await _inner.BanAndCleanupUserAsync(userId, chatId, messageIdToDelete);
    }
}

public sealed class RecordingAiCascadeService : IAiCascadeService
{
    private readonly IAiCascadeService _inner;
    private readonly IEffectsSink _sink;

    public RecordingAiCascadeService(IAiCascadeService inner, IEffectsSink sink)
    {
        _inner = inner;
        _sink = sink;
    }

    public async Task<bool> PerformAiProfileAnalysisAsync(Message message, User user, Chat chat, CancellationToken cancellationToken)
    {
        _sink.Add(new Effect(EffectType.AiCascade, chat.Id, user.Id, "profile_analysis"));
        return await _inner.PerformAiProfileAnalysisAsync(message, user, chat, cancellationToken);
    }

    public async Task HandleAiCascadeAnalysisAsync(Message message, User user, double mlScore, bool isSilentMode, CancellationToken cancellationToken)
    {
        _sink.Add(new Effect(EffectType.AiCascade, message.Chat.Id, user.Id, $"cascade_ml_score_{mlScore}"));
        await _inner.HandleAiCascadeAnalysisAsync(message, user, mlScore, isSilentMode, cancellationToken);
    }
}
