using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClubDoorman.Services.UserFlow;
using Telegram.Bot.Types;

namespace ClubDoorman.Tests.TestKit2.Fakes;

public sealed class FakeUserFlowLogger : IUserFlowLogger
{
    public void LogUserJoined(User user, Chat chat, string? joinReason = null)
    {
        // Default: do nothing
    }
    
    public void LogCaptchaShown(User user, Chat chat)
    {
        // Default: do nothing
    }
    
    public void LogCaptchaPassed(User user, Chat chat)
    {
        // Default: do nothing
    }
    
    public void LogCaptchaFailed(User user, Chat chat)
    {
        // Default: do nothing
    }
    
    public void LogWelcomeShown(User user, Chat chat)
    {
        // Default: do nothing
    }
    
    public void LogWelcomeRemoved(User user, Chat chat)
    {
        // Default: do nothing
    }
    
    public void LogFirstMessage(User user, Chat chat, string messageText)
    {
        // Default: do nothing
    }
    
    public void LogModerationStarted(User user, Chat chat, string messageText)
    {
        // Default: do nothing
    }
    
    public void LogSpamListCheck(User user, Chat chat, bool passed, string? reason = null)
    {
        // Default: do nothing
    }
    
    public void LogStopWordsCheck(User user, Chat chat, bool passed, string? reason = null)
    {
        // Default: do nothing
    }
    
    public void LogKnownSpamCheck(User user, Chat chat, bool passed, string? reason = null)
    {
        // Default: do nothing
    }
    
    public void LogMlAnalysis(User user, Chat chat, bool isSpam, double score, string? reason = null)
    {
        // Default: do nothing
    }
    
    public void LogModerationResult(User user, Chat chat, string action, string reason, double? confidence = null)
    {
        // Default: do nothing
    }
    
    public void LogUserApproved(User user, Chat chat, string reason)
    {
        // Default: do nothing
    }
    
    public void LogUserBanned(User user, Chat chat, string reason)
    {
        // Default: do nothing
    }
    
    public void LogUserRestricted(User user, Chat chat, string reason, TimeSpan? duration = null)
    {
        // Default: do nothing
    }
    
    public void LogUserRemovedFromApproved(User user, Chat chat, string reason)
    {
        // Default: do nothing
    }
    
    public void LogUserAddedToApproved(User user, Chat chat, string reason)
    {
        // Default: do nothing
    }
    
    public void LogUserMarkedAsSuspicious(User user, Chat chat, double mimicryScore, List<string> firstMessages)
    {
        // Default: do nothing
    }
    
    public void LogUserRemovedFromSuspicious(User user, Chat chat, string reason)
    {
        // Default: do nothing
    }
    
    public void LogAiProfileAnalysis(User user, Chat chat, double spamProbability, string reason)
    {
        // Default: do nothing
    }
    
    public void LogChannelMessage(Chat senderChat, Chat targetChat, string messageText)
    {
        // Default: do nothing
    }
    
    public void LogSystemError(Exception exception, string context, User? user = null, Chat? chat = null)
    {
        // Default: do nothing
    }
}
