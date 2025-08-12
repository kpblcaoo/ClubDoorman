using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClubDoorman.Services.AI;
using Telegram.Bot.Types;

namespace ClubDoorman.Tests.TestKit2.Fakes;

public sealed class FakeAiChecks : IAiChecks
{
    public void MarkUserOkay(long userId)
    {
        // Default: do nothing
    }
    
    public ValueTask<SpamPhotoBio> GetAttentionBaitProbability(User user, Func<string, Task>? ifChanged = default)
    {
        return ValueTask.FromResult(new SpamPhotoBio(
            new SpamProbability { Probability = 0.0, Reason = "Fake result" },
            new byte[0],
            "Fake bio"
        ));
    }
    
    public ValueTask<SpamPhotoBio> GetAttentionBaitProbability(User user, string? messageText, Func<string, Task>? ifChanged = default)
    {
        return ValueTask.FromResult(new SpamPhotoBio(
            new SpamProbability { Probability = 0.0, Reason = "Fake result" },
            new byte[0],
            "Fake bio"
        ));
    }
    
    public ValueTask<SpamProbability> GetSpamProbability(Message message)
    {
        return ValueTask.FromResult(new SpamProbability
        {
            Probability = 0.0,
            Reason = "Fake result"
        });
    }
    
    public ValueTask<SpamProbability> GetSuspiciousUserSpamProbability(
        Message message, 
        User user, 
        List<string> firstMessages, 
        double mimicryScore)
    {
        return ValueTask.FromResult(new SpamProbability
        {
            Probability = 0.0,
            Reason = "Fake result"
        });
    }
    
    public ValueTask<SpamProbability> GetCascadeAnalysisProbability(
        Message message, 
        User user, 
        double mlScore, 
        bool mlSpamDecision)
    {
        return ValueTask.FromResult(new SpamProbability
        {
            Probability = 0.0,
            Reason = "Fake result"
        });
    }
}
