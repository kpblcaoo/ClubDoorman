using ClubDoorman.Services.ChannelModeration;

namespace ClubDoorman.Test.TestInfrastructure.ChannelModeration;

internal sealed class TestChannelEffectsFlags : IChannelEffectsFlags
{
    public bool EffectsEnabled { get; init; }
    public bool ChannelAutoBanEnabled { get; init; }
}
