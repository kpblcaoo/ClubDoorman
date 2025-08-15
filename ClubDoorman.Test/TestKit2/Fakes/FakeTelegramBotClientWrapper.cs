using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ClubDoorman.Services.Telegram;

namespace ClubDoorman.Tests.TestKit2.Fakes;

/// <summary>
/// Фейк для ITelegramBotClientWrapper
/// </summary>
public class FakeTelegramBotClientWrapper : ITelegramBotClientWrapper
{
    public List<SentMessage> SentMessages { get; } = new();
    public List<DeletedMessage> DeletedMessages { get; } = new();
    public List<BannedUser> BannedUsers { get; } = new();
    public List<UnbannedUser> UnbannedUsers { get; } = new();
    public List<EditedMessage> EditedMessages { get; } = new();
    public List<SentPhoto> SentPhotos { get; } = new();
    public List<RestrictedUser> RestrictedUsers { get; } = new();
    public List<string> OperationLog { get; } = new();

    public bool ShouldThrowException { get; set; } = false;
    public Exception? ExceptionToThrow { get; set; }
    public long BotId { get; set; } = 123456789;

    private int _nextMessageId = 1;

    public Task<Message> SendMessageAsync(
        ChatId chatId,
        string text,
        ParseMode? parseMode = null,
        ReplyParameters? replyParameters = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        var message = new Message
        {
            Chat = new Chat { Id = chatId.Identifier ?? 0, Type = ChatType.Group },
            From = new User { Id = BotId, IsBot = true, FirstName = "TestBot" },
            Text = text,
            Date = DateTime.UtcNow
        };
        // Устанавливаем MessageId через рефлексию, так как свойство только для чтения
        typeof(Message).GetProperty("MessageId")?.SetValue(message, _nextMessageId++);

        SentMessages.Add(new SentMessage(
            chatId.Identifier ?? 0,
            text,
            parseMode,
            replyMarkup,
            message
        ));
        
        OperationLog.Add($"SendMessageAsync: chatId={chatId.Identifier}, text={text}");
        return Task.FromResult(message);
    }

    public Task<bool> DeleteMessageAsync(
        ChatId chatId,
        int messageId,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        DeletedMessages.Add(new DeletedMessage(
            chatId.Identifier ?? 0,
            messageId
        ));

        OperationLog.Add($"DeleteMessageAsync: chatId={chatId.Identifier}, messageId={messageId}");
        return Task.FromResult(true);
    }

    public Task<bool> BanChatMemberAsync(
        ChatId chatId,
        long userId,
        DateTime? untilDate = null,
        bool? revokeMessages = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        BannedUsers.Add(new BannedUser(
            chatId.Identifier ?? 0,
            userId,
            untilDate,
            revokeMessages ?? false
        ));

        OperationLog.Add($"BanChatMemberAsync: chatId={chatId.Identifier}, userId={userId}");
        return Task.FromResult(true);
    }

    public Task<bool> UnbanChatMemberAsync(
        ChatId chatId,
        long userId,
        bool? onlyIfBanned = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        UnbannedUsers.Add(new UnbannedUser(
            chatId.Identifier ?? 0,
            userId,
            onlyIfBanned ?? false
        ));

        OperationLog.Add($"UnbanChatMemberAsync: chatId={chatId.Identifier}, userId={userId}");
        return Task.FromResult(true);
    }

    public Task<Message> EditMessageTextAsync(
        ChatId chatId,
        int messageId,
        string text,
        ParseMode? parseMode = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        var message = new Message
        {
            Chat = new Chat { Id = chatId.Identifier ?? 0, Type = ChatType.Group },
            From = new User { Id = BotId, IsBot = true, FirstName = "TestBot" },
            Text = text,
            Date = DateTime.UtcNow
        };
        // Устанавливаем MessageId через рефлексию
        typeof(Message).GetProperty("MessageId")?.SetValue(message, messageId);

        EditedMessages.Add(new EditedMessage(
            chatId.Identifier ?? 0,
            messageId,
            text,
            parseMode,
            replyMarkup,
            message
        ));

        OperationLog.Add($"EditMessageTextAsync: chatId={chatId.Identifier}, messageId={messageId}, text={text}");
        return Task.FromResult(message);
    }

    public Task<Message> SendPhotoAsync(
        ChatId chatId,
        InputFile photo,
        string? caption = null,
        ParseMode? parseMode = null,
        ReplyParameters? replyParameters = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        var message = new Message
        {
            Chat = new Chat { Id = chatId.Identifier ?? 0, Type = ChatType.Group },
            From = new User { Id = BotId, IsBot = true, FirstName = "TestBot" },
            Photo = new[] { new PhotoSize { FileId = "test_photo_id", Width = 100, Height = 100 } },
            Caption = caption,
            Date = DateTime.UtcNow
        };
        // Устанавливаем MessageId через рефлексию
        typeof(Message).GetProperty("MessageId")?.SetValue(message, _nextMessageId++);

        SentPhotos.Add(new SentPhoto(
            chatId.Identifier ?? 0,
            photo,
            caption,
            parseMode,
            replyMarkup,
            message
        ));

        OperationLog.Add($"SendPhotoAsync: chatId={chatId.Identifier}, caption={caption}");
        return Task.FromResult(message);
    }

    public Task<bool> RestrictChatMemberAsync(
        ChatId chatId,
        long userId,
        ChatPermissions permissions,
        DateTime? untilDate = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        RestrictedUsers.Add(new RestrictedUser(
            chatId.Identifier ?? 0,
            userId,
            permissions,
            untilDate
        ));

        OperationLog.Add($"RestrictChatMemberAsync: chatId={chatId.Identifier}, userId={userId}");
        return Task.FromResult(true);
    }

    // Дополнительные методы для полной реализации интерфейса
    public Task<User> GetMe(CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        return Task.FromResult(new User
        {
            Id = BotId,
            IsBot = true,
            FirstName = "TestBot",
            Username = "test_bot"
        });
    }

    public Task DeleteMessage(ChatId chatId, int messageId, CancellationToken cancellationToken = default)
    {
        return DeleteMessageAsync(chatId, messageId, cancellationToken);
    }

    public Task<Message> SendMessage(
        ChatId chatId,
        string text,
        ParseMode? parseMode = null,
        ReplyParameters? replyParameters = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        return SendMessageAsync(chatId, text, parseMode, replyParameters, replyMarkup, cancellationToken);
    }

    public Task<Chat> GetChat(ChatId chatId, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        return Task.FromResult(new Chat
        {
            Id = chatId.Identifier ?? 0,
            Type = ChatType.Group,
            Title = "Test Group"
        });
    }

    public Task<Chat> GetChatAsync(ChatId chatId, CancellationToken cancellationToken = default)
    {
        return GetChat(chatId, cancellationToken);
    }

    public Task<ChatFullInfo> GetChatFullInfo(ChatId chatId, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        return Task.FromResult(new ChatFullInfo
        {
            Id = chatId.Identifier ?? 0,
            Type = ChatType.Group,
            Title = "Test Group"
        });
    }

    public Task<Message> ForwardMessage(
        ChatId chatId,
        ChatId fromChatId,
        int messageId,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        var message = new Message
        {
            Chat = new Chat { Id = chatId.Identifier ?? 0, Type = ChatType.Group },
            From = new User { Id = BotId, IsBot = true, FirstName = "TestBot" },
            Date = DateTime.UtcNow
        };
        // Устанавливаем MessageId через рефлексию
        typeof(Message).GetProperty("MessageId")?.SetValue(message, _nextMessageId++);

        OperationLog.Add($"ForwardMessage: chatId={chatId.Identifier}, fromChatId={fromChatId.Identifier}, messageId={messageId}");
        return Task.FromResult(message);
    }

    public Task BanChatMember(
        ChatId chatId,
        long userId,
        DateTime? untilDate = null,
        bool revokeMessages = false,
        CancellationToken cancellationToken = default)
    {
        return BanChatMemberAsync(chatId, userId, untilDate, revokeMessages, cancellationToken);
    }

    public Task BanChatSenderChat(ChatId chatId, long senderChatId, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        OperationLog.Add($"BanChatSenderChat: chatId={chatId.Identifier}, senderChatId={senderChatId}");
        return Task.CompletedTask;
    }

    public Task RestrictChatMember(
        ChatId chatId,
        long userId,
        ChatPermissions permissions,
        DateTime? untilDate = null,
        CancellationToken cancellationToken = default)
    {
        return RestrictChatMemberAsync(chatId, userId, permissions, untilDate, cancellationToken);
    }

    public Task<Message> SendPhoto(
        ChatId chatId,
        object photo,
        string? caption = null,
        ParseMode? parseMode = null,
        ReplyParameters? replyParameters = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        var inputFile = photo as InputFile ?? new InputFileStream(new MemoryStream(), "test_photo");
        return SendPhotoAsync(chatId, inputFile, caption, parseMode, replyParameters, replyMarkup, cancellationToken);
    }

    public Task<int> GetChatMemberCount(ChatId chatId, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        return Task.FromResult(100);
    }

    public Task<Update[]> GetUpdates(
        int? offset = null,
        int? limit = null,
        int? timeout = null,
        IEnumerable<UpdateType>? allowedUpdates = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        return Task.FromResult(new Update[0]);
    }

    public Task GetInfoAndDownloadFile(string fileId, Stream destination, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        return Task.CompletedTask;
    }

    public Task AnswerCallbackQuery(
        string callbackQueryId,
        string? text = null,
        bool? showAlert = null,
        string? url = null,
        int? cacheTime = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        OperationLog.Add($"AnswerCallbackQuery: callbackQueryId={callbackQueryId}, text={text}");
        return Task.CompletedTask;
    }

    public Task EditMessageReplyMarkup(
        ChatId chatId,
        int messageId,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        OperationLog.Add($"EditMessageReplyMarkup: chatId={chatId.Identifier}, messageId={messageId}");
        return Task.CompletedTask;
    }

    public Task<Message> EditMessageText(
        ChatId chatId,
        int messageId,
        string text,
        ParseMode? parseMode = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        return EditMessageTextAsync(chatId, messageId, text, parseMode, replyMarkup, cancellationToken);
    }

    public Task UnbanChatMember(ChatId chatId, long userId, bool? onlyIfBanned = null, CancellationToken cancellationToken = default)
    {
        return UnbanChatMemberAsync(chatId, userId, onlyIfBanned, cancellationToken);
    }

    public Task<ChatMember> GetChatMember(ChatId chatId, long userId, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        var member = new ChatMemberMember
        {
            User = new User { Id = userId, FirstName = "Test", LastName = "User" }
        };
        return Task.FromResult<ChatMember>(member);
    }

    public Task<ChatMember[]> GetChatAdministratorsAsync(ChatId chatId, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        return Task.FromResult(new ChatMember[0]);
    }

    public void Clear()
    {
        SentMessages.Clear();
        DeletedMessages.Clear();
        BannedUsers.Clear();
        UnbannedUsers.Clear();
        EditedMessages.Clear();
        SentPhotos.Clear();
        RestrictedUsers.Clear();
        OperationLog.Clear();
    }
}

// Вспомогательные классы для отслеживания операций
public record SentMessage(long ChatId, string Text, ParseMode? ParseMode, ReplyMarkup? ReplyMarkup, Message Message);
public record DeletedMessage(long ChatId, int MessageId);
public record BannedUser(long ChatId, long UserId, DateTime? UntilDate, bool RevokeMessages);
public record UnbannedUser(long ChatId, long UserId, bool OnlyIfBanned);
public record EditedMessage(long ChatId, int MessageId, string Text, ParseMode? ParseMode, ReplyMarkup? ReplyMarkup, Message Message);
public record SentPhoto(long ChatId, InputFile Photo, string? Caption, ParseMode? ParseMode, ReplyMarkup? ReplyMarkup, Message Message);
public record RestrictedUser(long ChatId, long UserId, ChatPermissions Permissions, DateTime? UntilDate);
