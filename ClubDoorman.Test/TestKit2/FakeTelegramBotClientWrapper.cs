using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Services.Telegram;

namespace ClubDoorman.Tests.TestKit2;

/// <summary>
/// Фейк для ITelegramBotClientWrapper для тестирования
/// </summary>
public class FakeTelegramBotClientWrapper : ITelegramBotClientWrapper
{
    public List<SentMessage> SentMessages { get; } = new();
    public List<DeletedMessage> DeletedMessages { get; } = new();
    public List<BannedUser> BannedUsers { get; } = new();
    public List<UnbannedUser> UnbannedUsers { get; } = new();
    public List<string> OperationLog { get; } = new();
    
    public bool ShouldThrowException { get; set; } = false;
    public Exception? ExceptionToThrow { get; set; }
    public long BotId => 123456789;
    
    public Task<Message> SendMessageAsync(ChatId chatId, string text, ParseMode? parseMode = null, ReplyParameters? replyParameters = null, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");
            
        var message = new Message
        {
            Date = DateTime.UtcNow,
            Text = text,
            Chat = new Chat { Id = chatId.Identifier ?? 0 }
        };
        
        SentMessages.Add(new SentMessage(chatId.Identifier ?? 0, text));
        OperationLog.Add($"SendMessage: {chatId} - {text}");
        
        return Task.FromResult(message);
    }
    
    public Task<bool> BanChatMemberAsync(ChatId chatId, long userId, DateTime? untilDate = null, bool? revokeMessages = null, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");
            
        BannedUsers.Add(new BannedUser(chatId.Identifier ?? 0, userId, untilDate));
        OperationLog.Add($"BanUser: {chatId} - {userId}");
        
        return Task.FromResult(true);
    }
    
    public Task<bool> DeleteMessageAsync(ChatId chatId, int messageId, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");
            
        DeletedMessages.Add(new DeletedMessage(chatId.Identifier ?? 0, messageId));
        OperationLog.Add($"DeleteMessage: {chatId} - {messageId}");
        
        return Task.FromResult(true);
    }
    
    public Task<bool> UnbanChatMemberAsync(ChatId chatId, long userId, bool? onlyIfBanned = null, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");
            
        UnbannedUsers.Add(new UnbannedUser(chatId.Identifier ?? 0, userId));
        OperationLog.Add($"UnbanUser: {chatId} - {userId}");
        
        return Task.FromResult(true);
    }
    
    public Task<User> GetMe(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new User
        {
            Id = BotId,
            IsBot = true,
            FirstName = "TestBot",
            Username = "test_bot"
        });
    }
    
    // Остальные методы - заглушки
    public Task DeleteMessage(ChatId chatId, int messageId, CancellationToken cancellationToken = default) => DeleteMessageAsync(chatId, messageId, cancellationToken);
    public Task<Message> SendMessage(ChatId chatId, string text, ParseMode? parseMode = null, ReplyParameters? replyParameters = null, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default) => SendMessageAsync(chatId, text, parseMode, replyParameters, replyMarkup, cancellationToken);
    public Task<Chat> GetChat(ChatId chatId, CancellationToken cancellationToken = default) => Task.FromResult(new Chat { Id = chatId.Identifier ?? 0 });
    public Task<ChatFullInfo> GetChatFullInfo(ChatId chatId, CancellationToken cancellationToken = default) => Task.FromResult(new ChatFullInfo { Id = chatId.Identifier ?? 0 });
    public Task<Message> ForwardMessage(ChatId chatId, ChatId fromChatId, int messageId, CancellationToken cancellationToken = default)
    {
        var msg = (Message)Activator.CreateInstance(typeof(Message), nonPublic: true)!;
        typeof(Message).GetProperty("MessageId")!.SetValue(msg, messageId);
        return Task.FromResult(msg);
    }
    public Task BanChatMember(ChatId chatId, long userId, DateTime? untilDate = null, bool revokeMessages = false, CancellationToken cancellationToken = default) => BanChatMemberAsync(chatId, userId, untilDate, revokeMessages, cancellationToken);
    public Task BanChatSenderChat(ChatId chatId, long senderChatId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RestrictChatMember(ChatId chatId, long userId, ChatPermissions permissions, DateTime? untilDate = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<Message> SendPhoto(ChatId chatId, object photo, string? caption = null, ParseMode? parseMode = null, ReplyParameters? replyParameters = null, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        var msg = (Message)Activator.CreateInstance(typeof(Message), nonPublic: true)!;
        typeof(Message).GetProperty("MessageId")!.SetValue(msg, 1);
        return Task.FromResult(msg);
    }
    public Task<int> GetChatMemberCount(ChatId chatId, CancellationToken cancellationToken = default) => Task.FromResult(100);
    public Task<Update[]> GetUpdates(int? offset = null, int? limit = null, int? timeout = null, IEnumerable<UpdateType>? allowedUpdates = null, CancellationToken cancellationToken = default) => Task.FromResult(Array.Empty<Update>());
    public Task GetInfoAndDownloadFile(string fileId, Stream destination, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task AnswerCallbackQuery(string callbackQueryId, string? text = null, bool? showAlert = null, string? url = null, int? cacheTime = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task EditMessageReplyMarkup(ChatId chatId, int messageId, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<Message> EditMessageText(ChatId chatId, int messageId, string text, ParseMode? parseMode = null, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        var msg = (Message)Activator.CreateInstance(typeof(Message), nonPublic: true)!;
        typeof(Message).GetProperty("MessageId")!.SetValue(msg, messageId);
        return Task.FromResult(msg);
    }
    public Task UnbanChatMember(ChatId chatId, long userId, bool? onlyIfBanned = null, CancellationToken cancellationToken = default) => UnbanChatMemberAsync(chatId, userId, onlyIfBanned, cancellationToken);
    public Task<ChatMember> GetChatMember(ChatId chatId, long userId, CancellationToken cancellationToken = default) => Task.FromResult<ChatMember>(new ChatMemberMember { User = new User { Id = userId } });
    public Task<ChatMember[]> GetChatAdministratorsAsync(ChatId chatId, CancellationToken cancellationToken = default) => Task.FromResult(Array.Empty<ChatMember>());
    public Task<Chat> GetChatAsync(ChatId chatId, CancellationToken cancellationToken = default) => GetChat(chatId, cancellationToken);
}

// Вспомогательные классы для записи операций
public record SentMessage(long ChatId, string Text);
public record DeletedMessage(long ChatId, int MessageId);
public record BannedUser(long ChatId, long UserId, DateTime? UntilDate);
public record UnbannedUser(long ChatId, long UserId);
