namespace ClubDoorman.Services.Core.Configuration;

/// <summary>
/// Доступы и списки чатов.
/// </summary>
public class ChatAccessOptions
{
    public HashSet<long> DisabledChats { get; set; } = new();
    public HashSet<long> WhitelistChats { get; set; } = new();
    public HashSet<long> NoVpnAdGroups { get; set; } = new();
    public HashSet<long> NoCaptchaGroups { get; set; } = new();
}
