using ClubDoorman.Services.ChannelModeration;

namespace ClubDoorman.Test.TestInfrastructure.ChannelModeration;

internal sealed class TestChannelEffectsFlags : IChannelEffectsFlags
{
    public bool ChannelAutoBanEnabled { get; init; }
}
