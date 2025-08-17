namespace ClubDoorman.Services.ChannelModeration;

/// <summary>
/// Temporary (migration) feature flags for channel effects pipeline.
/// Remove after full rollout.
/// </summary>
public interface IChannelEffectsFlags
{
    bool EffectsEnabled { get; }
    bool ChannelAutoBanEnabled { get; }
}
