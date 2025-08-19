namespace ClubDoorman.Services.Core.Configuration;

/// <summary>
/// Настройки AI / подозрительных пользователей.
/// </summary>
public class AiOptions
{
    public string? OpenRouterApi { get; set; }
    public bool SuspiciousDetectionEnabled { get; set; }
    public double MimicryThreshold { get; set; } = 0.7;
    public int SuspiciousToApprovedMessageCount { get; set; } = 3;
    public HashSet<long> AiEnabledChats { get; set; } = new();
}
