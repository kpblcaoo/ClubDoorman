using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Core.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using ClubDoorman.Models;
using ClubDoorman.Effects;

namespace ClubDoorman.Features.Moderation;

/// <summary>
/// Фасад для функциональности модерации
/// <tags>moderation, facade, coordination, thin-layer</tags>
/// </summary>
public class ModerationFacade : IModerationFacade
{
    private readonly IModerationPolicy _moderationPolicy;
    private readonly ILogger<ModerationFacade> _logger;
    private readonly IModerationEffectsBuilder _moderationEffectsBuilder;
    private readonly IEffectBus _effectBus;


    public ModerationFacade(
        IModerationPolicy moderationPolicy,
        ILogger<ModerationFacade> logger,
        IModerationEffectsBuilder moderationEffectsBuilder,
        IEffectBus effectBus)
    {
        _moderationPolicy = moderationPolicy;
        _logger = logger;
        _moderationEffectsBuilder = moderationEffectsBuilder;
        _effectBus = effectBus;
    }

    public Task<ModerationResult> CheckMessageAsync(Message message)
    {
        return _moderationPolicy.CheckMessageAsync(message);
    }

    public Task<ModerationResult> CheckUserNameAsync(User user)
    {
        return _moderationPolicy.CheckUserNameAsync(user);
    }

    public Task IncrementGoodMessageCountAsync(User user, Chat chat, string messageText)
    {
        return _moderationPolicy.IncrementGoodMessageCountAsync(user, chat, messageText);
    }

    public bool IsUserApproved(long userId, long? chatId = null)
    {
        return _moderationPolicy.IsUserApproved(userId, chatId);
    }

    public bool SetAiDetectForSuspiciousUser(long userId, long chatId, bool enabled)
    {
        return _moderationPolicy.SetAiDetectForSuspiciousUser(userId, chatId, enabled);
    }

    public (int TotalSuspicious, int WithAiDetect, int GroupsCount) GetSuspiciousUsersStats()
    {
        return _moderationPolicy.GetSuspiciousUsersStats();
    }

    public List<(long UserId, long ChatId)> GetAiDetectUsers()
    {
        return _moderationPolicy.GetAiDetectUsers();
    }

    public Task<bool> CheckAiDetectAndNotifyAdminsAsync(User user, Chat chat, Message message)
    {
        return _moderationPolicy.CheckAiDetectAndNotifyAdminsAsync(user, chat, message);
    }

    public Task<bool> UnrestrictAndApproveUserAsync(long userId, long chatId)
    {
        return _moderationPolicy.UnrestrictAndApproveUserAsync(userId, chatId);
    }

    public void CleanupUserFromAllLists(long userId, long chatId)
    {
        _moderationPolicy.CleanupUserFromAllLists(userId, chatId);
    }

    public Task<bool> BanAndCleanupUserAsync(long userId, long chatId, int? messageIdToDelete = null)
    {
        return _moderationPolicy.BanAndCleanupUserAsync(userId, chatId, messageIdToDelete);
    }

    public Task ExecuteModerationActionAsync(Message message, ModerationResult result)
    {
        return _moderationPolicy.ExecuteModerationActionAsync(message, result);
    }

    /// <summary>
    /// Обрабатывает сообщение пользователя на основе результата модерации
    /// <tags>moderation, message-handling, action-execution</tags>
    /// </summary>
    /// <param name="message">Сообщение для обработки</param>
    /// <param name="user">Пользователь</param>
    /// <param name="chat">Чат</param>
    /// <param name="moderationResult">Результат модерации</param>
    /// <param name="isSilentMode">Тихий режим</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task HandleUserMessageAsync(
        Message message,
        User user,
        Chat chat,
        ModerationResult moderationResult,
        bool isSilentMode,
        CancellationToken cancellationToken)
    {
        var effects = _moderationEffectsBuilder.BuildEffects(message, moderationResult, isSilentMode);
        await _effectBus.ExecuteAsync(effects, cancellationToken);
    }


}