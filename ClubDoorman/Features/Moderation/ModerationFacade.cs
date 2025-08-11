using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.TextProcessing;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using ClubDoorman.Models.Requests;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models;

namespace ClubDoorman.Features.Moderation;

/// <summary>
/// Фасад для функциональности модерации
/// <tags>moderation, facade, coordination, thin-layer</tags>
/// </summary>
public class ModerationFacade : IModerationFacade
{
    private readonly IModerationPolicy _moderationPolicy;

    public ModerationFacade(IModerationPolicy moderationPolicy)
    {
        _moderationPolicy = moderationPolicy;
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
}
