using ClubDoorman.Services.Telegram;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using System.Collections.Concurrent;

namespace ClubDoorman.Tests.TestKit2;

public sealed class FakeTelegramClient : ITelegramBotClientWrapper
{
    private readonly ConcurrentDictionary<long, Chat> _chats = new();
    private readonly ConcurrentDictionary<long, ChatFullInfo> _chatFullInfos = new();
    private readonly ConcurrentDictionary<long, User> _users = new();
    private readonly List<Update> _updates = new();
    private readonly List<Message> _messages = new();
    private readonly List<long> _bannedUsers = new();
    private readonly List<long> _bannedSenderChats = new();
    private readonly List<long> _restrictedUsers = new();
    private readonly User _botUser = new() { Id = 123, IsBot = true, FirstName = "TestBot" };
    public long BotId => _botUser.Id;

    private static long GetChatId(ChatId chatId)
    {
        if (chatId.Identifier is long l)
            return l;
        if (long.TryParse(chatId.ToString(), out var parsed))
            return parsed;
        throw new ArgumentException("ChatId must be convertible to long", nameof(chatId));
    }

    public Task<Message> SendMessageAsync(
        ChatId chatId,
        string text,
        ParseMode? parseMode = null,
        ReplyParameters? replyParameters = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        var msg = (Message)Activator.CreateInstance(typeof(Message), nonPublic: true)!;
        typeof(Message).GetProperty("MessageId")!.SetValue(msg, _messages.Count + 1);
        msg.Date = DateTime.UtcNow;
        msg.Chat = new Chat { Id = GetChatId(chatId), Type = ChatType.Supergroup };
        msg.Text = text;
        msg.From = _botUser;
        _messages.Add(msg);
        return Task.FromResult(msg);
    }

    public Task<bool> BanChatMemberAsync(
        ChatId chatId,
        long userId,
        DateTime? untilDate = null,
        bool? revokeMessages = null,
        CancellationToken cancellationToken = default)
    {
        _bannedUsers.Add(userId);
        return Task.FromResult(true);
    }

    public Task<bool> DeleteMessageAsync(
        ChatId chatId,
        int messageId,
        CancellationToken cancellationToken = default)
    {
        _messages.RemoveAll(m => m.MessageId == messageId && m.Chat.Id == GetChatId(chatId));
        return Task.FromResult(true);
    }

    public Task<bool> UnbanChatMemberAsync(
        ChatId chatId,
        long userId,
        bool? onlyIfBanned = null,
        CancellationToken cancellationToken = default)
    {
        _bannedUsers.Remove(userId);
        return Task.FromResult(true);
    }

    public Task<User> GetMe(CancellationToken cancellationToken = default)
        => Task.FromResult(_botUser);

    public Task DeleteMessage(ChatId chatId, int messageId, CancellationToken cancellationToken = default)
        => DeleteMessageAsync(chatId, messageId, cancellationToken);

    public Task<Message> SendMessage(
        ChatId chatId,
        string text,
        ParseMode? parseMode = null,
        ReplyParameters? replyParameters = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
        => SendMessageAsync(chatId, text, parseMode, replyParameters, replyMarkup, cancellationToken);

    public Task<Chat> GetChat(ChatId chatId, CancellationToken cancellationToken = default)
    {
        var id = GetChatId(chatId);
        if (_chats.TryGetValue(id, out var chat))
            return Task.FromResult(chat);
        var newChat = new Chat { Id = id, Type = ChatType.Supergroup };
        _chats[id] = newChat;
        return Task.FromResult(newChat);
    }

    public Task<ChatFullInfo> GetChatFullInfo(ChatId chatId, CancellationToken cancellationToken = default)
    {
        var id = GetChatId(chatId);
        if (_chatFullInfos.TryGetValue(id, out var info))
            return Task.FromResult(info);
        var newInfo = new ChatFullInfo { Id = id, Type = ChatType.Supergroup };
        _chatFullInfos[id] = newInfo;
        return Task.FromResult(newInfo);
    }

    public Task<Message> ForwardMessage(
        ChatId chatId,
        ChatId fromChatId,
        int messageId,
        CancellationToken cancellationToken = default)
    {
        var orig = _messages.FirstOrDefault(m => m.MessageId == messageId && m.Chat.Id == GetChatId(fromChatId));
        var msg = (Message)Activator.CreateInstance(typeof(Message), nonPublic: true)!;
        typeof(Message).GetProperty("MessageId")!.SetValue(msg, _messages.Count + 1);
        msg.Date = DateTime.UtcNow;
        msg.Chat = new Chat { Id = GetChatId(chatId), Type = ChatType.Supergroup };
        msg.Text = orig?.Text ?? "forwarded";
        msg.From = orig?.From ?? _botUser;
        _messages.Add(msg);
        return Task.FromResult(msg);
    }

    public Task BanChatMember(
        ChatId chatId,
        long userId,
        DateTime? untilDate = null,
        bool revokeMessages = false,
        CancellationToken cancellationToken = default)
    {
        _bannedUsers.Add(userId);
        return Task.CompletedTask;
    }

    public Task BanChatSenderChat(ChatId chatId, long senderChatId, CancellationToken cancellationToken = default)
    {
        _bannedSenderChats.Add(senderChatId);
        return Task.CompletedTask;
    }

    public Task RestrictChatMember(
        ChatId chatId,
        long userId,
        ChatPermissions permissions,
        DateTime? untilDate = null,
        CancellationToken cancellationToken = default)
    {
        _restrictedUsers.Add(userId);
        return Task.CompletedTask;
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
        var msg = (Message)Activator.CreateInstance(typeof(Message), nonPublic: true)!;
        typeof(Message).GetProperty("MessageId")!.SetValue(msg, _messages.Count + 1);
        msg.Date = DateTime.UtcNow;
        msg.Chat = new Chat { Id = GetChatId(chatId), Type = ChatType.Supergroup };
        msg.Caption = caption;
        msg.Photo = new[] { new PhotoSize { FileId = photo?.ToString() ?? "photoId" } };
        msg.From = _botUser;
        _messages.Add(msg);
        return Task.FromResult(msg);
    }

    public Task<int> GetChatMemberCount(ChatId chatId, CancellationToken cancellationToken = default)
        => Task.FromResult(_users.Count);

    public Task<Update[]> GetUpdates(
        int? offset = null,
        int? limit = null,
        int? timeout = null,
        IEnumerable<UpdateType>? allowedUpdates = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_updates.ToArray());

    public Task GetInfoAndDownloadFile(string fileId, Stream destination, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task AnswerCallbackQuery(string callbackQueryId, string? text = null, bool? showAlert = null, string? url = null, int? cacheTime = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task EditMessageReplyMarkup(ChatId chatId, int messageId, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<Message> EditMessageText(ChatId chatId, int messageId, string text, ParseMode? parseMode = null, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        var msg = _messages.FirstOrDefault(m => m.MessageId == messageId && m.Chat.Id == GetChatId(chatId));
        if (msg != null)
            msg.Text = text;
        else
        {
            msg = (Message)Activator.CreateInstance(typeof(Message), nonPublic: true)!;
            typeof(Message).GetProperty("MessageId")!.SetValue(msg, messageId);
            msg.Chat = new Chat { Id = GetChatId(chatId) };
            msg.Text = text;
        }
        return Task.FromResult(msg);
    }

    public Task UnbanChatMember(ChatId chatId, long userId, bool? onlyIfBanned = null, CancellationToken cancellationToken = default)
    {
        _bannedUsers.Remove(userId);
        return Task.CompletedTask;
    }

    public Task<ChatMember> GetChatMember(ChatId chatId, long userId, CancellationToken cancellationToken = default)
        => Task.FromResult<ChatMember>(new ChatMemberMember { User = new User { Id = userId } });

    public Task<ChatMember[]> GetChatAdministratorsAsync(ChatId chatId, CancellationToken cancellationToken = default)
        => Task.FromResult(new ChatMember[] { new ChatMemberAdministrator { User = _botUser } });

    public Task<Chat> GetChatAsync(ChatId chatId, CancellationToken cancellationToken = default)
        => GetChat(chatId, cancellationToken);
}
