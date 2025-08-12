using ClubDoorman.Services.Telegram;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman.Test.TestKit.Fakes;

/// <summary>
/// Запись действия, выполненного с ботом
/// </summary>
public record BotAction(string Action, object[] Parameters, DateTime Timestamp)
{
    public override string ToString()
    {
        var paramStr = string.Join(", ", Parameters.Select(p => p?.ToString() ?? "null"));
        return $"{Timestamp:HH:mm:ss.fff} {Action}({paramStr})";
    }
}

/// <summary>
/// Фальшивая реализация ITelegramBotClientWrapper для тестов
/// Сохраняет все вызовы в транскрипт для последующей проверки
/// </summary>
public class BotClientFake : ITelegramBotClientWrapper
{
    private readonly List<BotAction> _transcript = new();
    private readonly Dictionary<string, object> _responses = new();
    private int _messageIdCounter = 1000;

    public long BotId { get; set; } = 987654321;

    /// <summary>
    /// Получить все записанные действия
    /// </summary>
    public IReadOnlyList<BotAction> GetTranscript() => _transcript.AsReadOnly();

    /// <summary>
    /// Очистить транскрипт
    /// </summary>
    public void ClearTranscript() => _transcript.Clear();

    /// <summary>
    /// Настроить ответ для конкретного метода
    /// </summary>
    public void SetResponse<T>(string method, T response)
    {
        _responses[method] = response!;
    }

    /// <summary>
    /// Получить настроенный ответ или создать ответ по умолчанию
    /// </summary>
    private T GetResponse<T>(string method, Func<T> defaultFactory)
    {
        if (_responses.TryGetValue(method, out var response) && response is T typedResponse)
            return typedResponse;
        
        return defaultFactory();
    }

    // Основные методы отправки сообщений
    public Task<Message> SendMessageAsync(ChatId chatId, string text, ParseMode? parseMode = null, 
        ReplyParameters? replyParameters = null, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        var action = new BotAction("SendMessage", new object[] { chatId, text, parseMode ?? ParseMode.Html }, DateTime.UtcNow);
        _transcript.Add(action);

        return Task.FromResult(GetResponse("SendMessage", () => new Message
        {
            MessageId = ++_messageIdCounter,
            Chat = new Chat { Id = chatId },
            Text = text,
            Date = DateTime.UtcNow,
            From = new User { Id = BotId, IsBot = true, FirstName = "TestBot" }
        }));
    }

    public Task<Message> SendMessage(ChatId chatId, string text, ParseMode? parseMode = null, 
        ReplyParameters? replyParameters = null, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        return SendMessageAsync(chatId, text, parseMode, replyParameters, replyMarkup, cancellationToken);
    }

    // Методы бана
    public Task<bool> BanChatMemberAsync(ChatId chatId, long userId, DateTime? untilDate = null, 
        bool? revokeMessages = null, CancellationToken cancellationToken = default)
    {
        var action = new BotAction("BanChatMember", new object[] { chatId, userId, untilDate ?? DateTime.MaxValue }, DateTime.UtcNow);
        _transcript.Add(action);

        return Task.FromResult(GetResponse("BanChatMember", () => true));
    }

    public Task BanChatMember(ChatId chatId, long userId, DateTime? untilDate = null, 
        bool revokeMessages = false, CancellationToken cancellationToken = default)
    {
        var action = new BotAction("BanChatMember", new object[] { chatId, userId, untilDate ?? DateTime.MaxValue }, DateTime.UtcNow);
        _transcript.Add(action);
        return Task.CompletedTask;
    }

    public Task BanChatSenderChat(ChatId chatId, long senderChatId, CancellationToken cancellationToken = default)
    {
        var action = new BotAction("BanChatSenderChat", new object[] { chatId, senderChatId }, DateTime.UtcNow);
        _transcript.Add(action);
        return Task.CompletedTask;
    }

    // Методы удаления сообщений
    public Task<bool> DeleteMessageAsync(ChatId chatId, int messageId, CancellationToken cancellationToken = default)
    {
        var action = new BotAction("DeleteMessage", new object[] { chatId, messageId }, DateTime.UtcNow);
        _transcript.Add(action);

        return Task.FromResult(GetResponse("DeleteMessage", () => true));
    }

    public Task DeleteMessage(ChatId chatId, int messageId, CancellationToken cancellationToken = default)
    {
        var action = new BotAction("DeleteMessage", new object[] { chatId, messageId }, DateTime.UtcNow);
        _transcript.Add(action);
        return Task.CompletedTask;
    }

    // Методы редактирования
    public Task<Message> EditMessageText(ChatId chatId, int messageId, string text, ParseMode? parseMode = null, 
        ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        var action = new BotAction("EditMessageText", new object[] { chatId, messageId, text }, DateTime.UtcNow);
        _transcript.Add(action);

        return Task.FromResult(GetResponse("EditMessageText", () => new Message
        {
            MessageId = messageId,
            Chat = new Chat { Id = chatId },
            Text = text,
            Date = DateTime.UtcNow,
            From = new User { Id = BotId, IsBot = true, FirstName = "TestBot" }
        }));
    }

    public Task EditMessageReplyMarkup(ChatId chatId, int messageId, ReplyMarkup? replyMarkup = null, 
        CancellationToken cancellationToken = default)
    {
        var action = new BotAction("EditMessageReplyMarkup", new object[] { chatId, messageId, replyMarkup?.ToString() ?? "null" }, DateTime.UtcNow);
        _transcript.Add(action);
        return Task.CompletedTask;
    }

    // Остальные методы интерфейса
    public Task<bool> UnbanChatMemberAsync(ChatId chatId, long userId, bool? onlyIfBanned = null, CancellationToken cancellationToken = default)
    {
        var action = new BotAction("UnbanChatMember", new object[] { chatId, userId }, DateTime.UtcNow);
        _transcript.Add(action);
        return Task.FromResult(true);
    }

    public Task UnbanChatMember(ChatId chatId, long userId, bool? onlyIfBanned = null, CancellationToken cancellationToken = default)
    {
        var action = new BotAction("UnbanChatMember", new object[] { chatId, userId }, DateTime.UtcNow);
        _transcript.Add(action);
        return Task.CompletedTask;
    }

    public Task<User> GetMe(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetResponse("GetMe", () => new User
        {
            Id = BotId,
            IsBot = true,
            FirstName = "TestBot",
            Username = "test_bot"
        }));
    }

    public Task<Chat> GetChat(ChatId chatId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetResponse("GetChat", () => new Chat
        {
            Id = chatId,
            Type = ChatType.Group,
            Title = "Test Chat"
        }));
    }

    public Task<Chat> GetChatAsync(ChatId chatId, CancellationToken cancellationToken = default)
    {
        return GetChat(chatId, cancellationToken);
    }

    public Task<ChatFullInfo> GetChatFullInfo(ChatId chatId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetResponse("GetChatFullInfo", () => new ChatFullInfo
        {
            Id = chatId,
            Type = ChatType.Group,
            Title = "Test Chat"
        }));
    }

    public Task<Message> ForwardMessage(ChatId chatId, ChatId fromChatId, int messageId, CancellationToken cancellationToken = default)
    {
        var action = new BotAction("ForwardMessage", new object[] { chatId, fromChatId, messageId }, DateTime.UtcNow);
        _transcript.Add(action);

        return Task.FromResult(GetResponse("ForwardMessage", () => new Message
        {
            MessageId = ++_messageIdCounter,
            Chat = new Chat { Id = chatId },
            Date = DateTime.UtcNow,
            From = new User { Id = BotId, IsBot = true, FirstName = "TestBot" }
        }));
    }

    public Task RestrictChatMember(ChatId chatId, long userId, ChatPermissions permissions, DateTime? untilDate = null, CancellationToken cancellationToken = default)
    {
        var action = new BotAction("RestrictChatMember", new object[] { chatId, userId, permissions.ToString() }, DateTime.UtcNow);
        _transcript.Add(action);
        return Task.CompletedTask;
    }

    public Task<Message> SendPhoto(ChatId chatId, object photo, string? caption = null, ParseMode? parseMode = null, 
        ReplyParameters? replyParameters = null, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        var action = new BotAction("SendPhoto", new object[] { chatId, photo.ToString() ?? "photo", caption ?? "" }, DateTime.UtcNow);
        _transcript.Add(action);

        return Task.FromResult(GetResponse("SendPhoto", () => new Message
        {
            MessageId = ++_messageIdCounter,
            Chat = new Chat { Id = chatId },
            Caption = caption,
            Date = DateTime.UtcNow,
            From = new User { Id = BotId, IsBot = true, FirstName = "TestBot" }
        }));
    }

    public Task<int> GetChatMemberCount(ChatId chatId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetResponse("GetChatMemberCount", () => 100));
    }

    public Task<Update[]> GetUpdates(int? offset = null, int? limit = null, int? timeout = null, 
        IEnumerable<UpdateType>? allowedUpdates = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetResponse("GetUpdates", () => Array.Empty<Update>()));
    }

    public Task GetInfoAndDownloadFile(string fileId, Stream destination, CancellationToken cancellationToken = default)
    {
        var action = new BotAction("GetInfoAndDownloadFile", new object[] { fileId }, DateTime.UtcNow);
        _transcript.Add(action);
        return Task.CompletedTask;
    }

    public Task AnswerCallbackQuery(string callbackQueryId, string? text = null, bool? showAlert = null, 
        string? url = null, int? cacheTime = null, CancellationToken cancellationToken = default)
    {
        var action = new BotAction("AnswerCallbackQuery", new object[] { callbackQueryId, text ?? "" }, DateTime.UtcNow);
        _transcript.Add(action);
        return Task.CompletedTask;
    }

    public Task<ChatMember> GetChatMember(ChatId chatId, long userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetResponse("GetChatMember", () => new ChatMemberMember
        {
            User = new User { Id = userId, FirstName = "Test User" },
            Status = ChatMemberStatus.Member
        }));
    }

    public Task<ChatMember[]> GetChatAdministratorsAsync(ChatId chatId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetResponse("GetChatAdministrators", () => Array.Empty<ChatMember>()));
    }

    // Методы для удобства в тестах
    
    /// <summary>
    /// Проверить, было ли отправлено сообщение с указанным текстом
    /// </summary>
    public bool WasMessageSent(string text) => _transcript.Any(a => a.Action == "SendMessage" && a.Parameters.Contains(text));

    /// <summary>
    /// Проверить, было ли удалено сообщение
    /// </summary>
    public bool WasMessageDeleted(ChatId chatId, int messageId) => 
        _transcript.Any(a => a.Action == "DeleteMessage" && a.Parameters.Contains(chatId) && a.Parameters.Contains(messageId));

    /// <summary>
    /// Проверить, был ли забанен пользователь
    /// </summary>
    public bool WasUserBanned(ChatId chatId, long userId) => 
        _transcript.Any(a => a.Action == "BanChatMember" && a.Parameters.Contains(chatId) && a.Parameters.Contains(userId));

    /// <summary>
    /// Получить все отправленные сообщения
    /// </summary>
    public IEnumerable<BotAction> GetSentMessages() => _transcript.Where(a => a.Action == "SendMessage");

    /// <summary>
    /// Получить все действия бана
    /// </summary>
    public IEnumerable<BotAction> GetBanActions() => _transcript.Where(a => a.Action == "BanChatMember");

    /// <summary>
    /// Получить все действия удаления сообщений
    /// </summary>
    public IEnumerable<BotAction> GetDeleteActions() => _transcript.Where(a => a.Action == "DeleteMessage");
}