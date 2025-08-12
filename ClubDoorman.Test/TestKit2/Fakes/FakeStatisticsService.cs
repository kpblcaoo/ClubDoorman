using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Models;

namespace ClubDoorman.Tests.TestKit2.Fakes;

public sealed class FakeStatisticsService : IStatisticsService
{
    private readonly Dictionary<long, ChatStats> _stats = new();
    
    public void IncrementCaptcha(long chatId)
    {
        if (!_stats.ContainsKey(chatId))
            _stats[chatId] = new ChatStats("Fake Chat");
        _stats[chatId].StoppedCaptcha++;
    }
    
    public void IncrementBlacklistBan(long chatId)
    {
        if (!_stats.ContainsKey(chatId))
            _stats[chatId] = new ChatStats("Fake Chat");
        _stats[chatId].BlacklistBanned++;
    }
    
    public void IncrementKnownBadMessage(long chatId)
    {
        if (!_stats.ContainsKey(chatId))
            _stats[chatId] = new ChatStats("Fake Chat");
        _stats[chatId].KnownBadMessage++;
    }
    
    public void IncrementLongNameBan(long chatId)
    {
        if (!_stats.ContainsKey(chatId))
            _stats[chatId] = new ChatStats("Fake Chat");
        _stats[chatId].LongNameBanned++;
    }
    
    public IDictionary<long, ChatStats> GetAllStats()
    {
        return new Dictionary<long, ChatStats>(_stats);
    }
    
    public void ClearStats()
    {
        _stats.Clear();
    }
    
    public Task<string> GenerateReportAsync()
    {
        return Task.FromResult("Fake statistics report");
    }
    
    public void Reset()
    {
        _stats.Clear();
    }
}
