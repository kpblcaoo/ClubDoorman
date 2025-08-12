using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Models;
using Telegram.Bot.Types;

namespace ClubDoorman.Tests.TestKit2.Fakes;

public sealed class FakeModerationService : IModerationService
{
    private readonly Queue<ModerationResult> _checkResults = new();
    private readonly Queue<Exception> _exceptions = new();
    
    public void EnqueueCheckResult(ModerationResult result) => _checkResults.Enqueue(result);
    public void EnqueueException(Exception exception) => _exceptions.Enqueue(exception);
    
    public async Task<ModerationResult> CheckMessageAsync(Message message)
    {
        if (_exceptions.Count > 0)
            throw _exceptions.Dequeue();
            
        if (_checkResults.Count > 0)
            return _checkResults.Dequeue();
            
        // Default: allow message
        return new ModerationResult(
            Action: ModerationAction.Allow,
            Reason: "Default fake result"
        );
    }
    
    public async Task<ModerationResult> CheckUserNameAsync(User user)
    {
        if (_exceptions.Count > 0)
            throw _exceptions.Dequeue();
            
        if (_checkResults.Count > 0)
            return _checkResults.Dequeue();
            
        return new ModerationResult(
            Action: ModerationAction.Allow,
            Reason: "Default fake username check"
        );
    }
    
    public async Task ExecuteModerationActionAsync(Message message, ModerationResult result)
    {
        if (_exceptions.Count > 0)
            throw _exceptions.Dequeue();
            
        // Default: do nothing
        await Task.CompletedTask;
    }
    
    public bool IsUserApproved(long userId, long? chatId = null)
    {
        return true; // Default: user is approved
    }
    
    public async Task IncrementGoodMessageCountAsync(User user, Chat chat, string messageText)
    {
        if (_exceptions.Count > 0)
            throw _exceptions.Dequeue();
            
        await Task.CompletedTask;
    }
    
    public bool SetAiDetectForSuspiciousUser(long userId, long chatId, bool enabled)
    {
        return true; // Default: success
    }
    
    public (int TotalSuspicious, int WithAiDetect, int GroupsCount) GetSuspiciousUsersStats()
    {
        return (0, 0, 0); // Default: no suspicious users
    }
    
    public List<(long UserId, long ChatId)> GetAiDetectUsers()
    {
        return new List<(long UserId, long ChatId)>(); // Default: empty list
    }
    
    public async Task<bool> CheckAiDetectAndNotifyAdminsAsync(User user, Chat chat, Message message)
    {
        if (_exceptions.Count > 0)
            throw _exceptions.Dequeue();
            
        return false; // Default: no AI detect needed
    }
    
    public async Task<bool> UnrestrictAndApproveUserAsync(long userId, long chatId)
    {
        if (_exceptions.Count > 0)
            throw _exceptions.Dequeue();
            
        return true; // Default: success
    }
    
    public void CleanupUserFromAllLists(long userId, long chatId)
    {
        // Default: do nothing
    }
    
    public async Task<bool> BanAndCleanupUserAsync(long userId, long chatId, int? messageIdToDelete = null)
    {
        if (_exceptions.Count > 0)
            throw _exceptions.Dequeue();
            
        return true; // Default: success
    }
    
    public void Reset()
    {
        _checkResults.Clear();
        _exceptions.Clear();
    }
}
