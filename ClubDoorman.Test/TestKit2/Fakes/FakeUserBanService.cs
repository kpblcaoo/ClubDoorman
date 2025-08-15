using ClubDoorman.Services.UserBan;
using Telegram.Bot.Types;

namespace ClubDoorman.Tests.TestKit2.Fakes;

/// <summary>
/// Фейк для IUserBanService
/// </summary>
public class FakeUserBanService : IUserBanService
{
    public List<BanOperation> BanOperations { get; } = new();
    public List<DeleteMessageOperation> DeleteMessageOperations { get; } = new();

    public bool ShouldThrowException { get; set; } = false;
    public Exception? ExceptionToThrow { get; set; }

    public Task BanUserForLongNameAsync(Message? userJoinMessage, User user, string reason, TimeSpan? banDuration, CancellationToken cancellationToken)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        BanOperations.Add(new BanOperation(user.Id, reason, banDuration));
        return Task.CompletedTask;
    }

    public Task DeleteMessageByIdAsync(ChatId chatId, int messageId, CancellationToken cancellationToken)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        DeleteMessageOperations.Add(new DeleteMessageOperation(chatId, messageId));
        return Task.CompletedTask;
    }

    // Остальные методы - пустые реализации
    public Task BanBlacklistedUserAsync(Message userJoinMessage, User user, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task AutoBanAsync(Message message, string reason, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task AutoBanChannelAsync(Message message, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task HandleBlacklistBanAsync(Message message, User user, Chat chat, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task TrackViolationAndBanIfNeededAsync(Message message, User user, string reason, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task BanUserAsync(Chat chat, User user, BanTypeEnum banType, string? customReason = null, Message? messageToDelete = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task BanUserAsync(Chat chat, User user, BanTypeEnum banType, string? customReason = null, long? messageIdToDelete = null, long? chatIdForMessage = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DeleteMessageByIdAsync(long chatId, int messageId) => Task.CompletedTask;
}

public record BanOperation(long UserId, string Reason, TimeSpan? Duration);
public record DeleteMessageOperation(ChatId ChatId, int MessageId);
