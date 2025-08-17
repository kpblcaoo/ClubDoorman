using ClubDoorman.Infrastructure;

namespace ClubDoorman.Services.ChannelModeration;

/// <summary>
/// Production implementation reading static Config values.
/// </summary>
internal sealed class ChannelEffectsFlags : IChannelEffectsFlags
{
    public bool EffectsEnabled => Config.ChannelEffectsEnabled;
    public bool ChannelAutoBanEnabled => Config.ChannelAutoBan;
}
