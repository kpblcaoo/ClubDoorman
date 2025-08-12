using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Services.AI;
using Telegram.Bot.Types;

namespace ClubDoorman.Tests.TestKit2.Fakes;

public sealed class FakeAiCascadeService : IAiCascadeService
{
    private readonly Queue<bool> _profileResults = new();
    private readonly Queue<Exception> _exceptions = new();
    
    public void EnqueueProfileResult(bool isRestricted) => _profileResults.Enqueue(isRestricted);
    public void EnqueueException(Exception exception) => _exceptions.Enqueue(exception);
    
    public async Task<bool> PerformAiProfileAnalysisAsync(Message message, User user, Chat chat, CancellationToken cancellationToken)
    {
        if (_exceptions.Count > 0)
            throw _exceptions.Dequeue();
            
        if (_profileResults.Count > 0)
            return _profileResults.Dequeue();
            
        return false; // Default: user is safe
    }
    
    public async Task HandleAiCascadeAnalysisAsync(Message message, User user, double mlScore, bool isSilentMode, CancellationToken cancellationToken)
    {
        if (_exceptions.Count > 0)
            throw _exceptions.Dequeue();
            
        // Default: do nothing
        await Task.CompletedTask;
    }
    
    public void Reset()
    {
        _profileResults.Clear();
        _exceptions.Clear();
    }
}
