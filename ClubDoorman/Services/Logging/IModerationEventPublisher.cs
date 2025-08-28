using ClubDoorman.Models.Logging;

namespace ClubDoorman.Services.Logging;

/// <summary>
/// Abstraction for publishing moderation semantic events (decouples handlers from Golden Master recorder).
/// </summary>
public interface IModerationEventPublisher
{
    void Publish(string? correlationId, ModerationEvent evt);
}
