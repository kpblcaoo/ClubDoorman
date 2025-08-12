using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClubDoorman.Services.UserManagement;

namespace ClubDoorman.Tests.TestKit2.Fakes;

public sealed class FakeUserManager : IUserManager
{
    public bool Approved(long userId, long? groupId = null)
    {
        return true; // Default: user is approved
    }
    
    public ValueTask Approve(long userId, long? groupId = null)
    {
        return ValueTask.CompletedTask; // Default: success
    }
    
    public bool RemoveApproval(long userId, long? groupId = null, bool removeAll = false)
    {
        return true; // Default: success
    }
    
    public ValueTask<bool> InBanlist(long userId)
    {
        return ValueTask.FromResult(false); // Default: user is not banned
    }
    
    public ValueTask<string?> GetClubUsername(long userId)
    {
        return ValueTask.FromResult<string?>("fake_user"); // Default: fake username
    }
    
    public Task RefreshBanlist()
    {
        return Task.CompletedTask; // Default: success
    }
}
