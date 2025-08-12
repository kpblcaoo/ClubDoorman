using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClubDoorman.Services.BadMessage;

namespace ClubDoorman.Tests.TestKit2.Fakes;

public sealed class FakeBadMessageManager : IBadMessageManager
{
    private readonly HashSet<string> _badMessages = new();
    
    public bool KnownBadMessage(string message)
    {
        return _badMessages.Contains(message);
    }
    
    public ValueTask MarkAsBad(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            _badMessages.Add(message);
        }
        return ValueTask.CompletedTask;
    }
    
    public void Reset()
    {
        _badMessages.Clear();
    }
}
