using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Services.AI;
using Telegram.Bot.Types;

namespace ClubDoorman.Tests.TestKit2.Fakes;

public sealed class FakeSpamHamClassifier : ISpamHamClassifier
{
    private readonly Queue<(bool isSpam, float score)> _results = new();
    private readonly Queue<Exception> _exceptions = new();
    
    public void EnqueueResult(bool isSpam, float score = 0.8f) => _results.Enqueue((isSpam, score));
    public void EnqueueException(Exception exception) => _exceptions.Enqueue(exception);
    
    public async Task<(bool Spam, float Score)> IsSpam(string message)
    {
        if (_exceptions.Count > 0)
            throw _exceptions.Dequeue();
            
        if (_results.Count > 0)
            return _results.Dequeue();
            
        return (false, 0.5f); // Default: not spam
    }
    
    public async Task AddSpam(string message)
    {
        // Do nothing in fake
        await Task.CompletedTask;
    }
    
    public async Task AddHam(string message)
    {
        // Do nothing in fake
        await Task.CompletedTask;
    }
    
    public void Touch()
    {
        // Do nothing in fake
    }
    
    public void Reset()
    {
        _results.Clear();
        _exceptions.Clear();
    }
}
