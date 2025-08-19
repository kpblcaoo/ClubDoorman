using ClubDoorman.Models.Logging;

namespace ClubDoorman.Services.Logging;

/// <summary>
/// No-op publisher for tests / legacy constructions where semantic events are not needed.
/// </summary>
internal sealed class NullModerationEventPublisher : IModerationEventPublisher
{
    public static readonly NullModerationEventPublisher Instance = new();
    private NullModerationEventPublisher() { }
    public void Publish(string? correlationId, ModerationEvent evt) { }
}
