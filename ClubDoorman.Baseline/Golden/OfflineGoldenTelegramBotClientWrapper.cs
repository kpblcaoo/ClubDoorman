using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ClubDoorman.Services.Telegram;

namespace ClubDoorman.Baseline.Golden;

public class BaselineOfflineTelegramBotClientWrapper : ITelegramBotClientWrapper
{
    public long BotId => 9999990000;

    private static Message MakeMessage(long chatId, string text)
    {
        var chat = new Chat { Id = chatId, Type = ChatType.Supergroup, Title = "GoldenBaselineChat" };
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = chat,
            From = new User { Id = 900000001, IsBot = false, Username = "baseline_user", FirstName = "Baseline", LastName = "User" },
            Text = text
        };
    }

    public Task<Message> SendMessageAsync(ChatId chatId, string text, ParseMode? parseMode = null, ReplyParameters? replyParameters = null, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
        => Task.FromResult(MakeMessage(chatId.Identifier ?? 0, text));
    public Task<bool> BanChatMemberAsync(ChatId chatId, long userId, DateTime? untilDate = null, bool? revokeMessages = null, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> DeleteMessageAsync(ChatId chatId, int messageId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<DeleteMessageResult> DeleteMessageWithOutcomeAsync(ChatId chatId, int messageId, CancellationToken cancellationToken = default)
        => Task.FromResult(new DeleteMessageResult(chatId.Identifier ?? 0, messageId, DeleteMessageOutcome.Success, 0, null, null));
    public Task<bool> UnbanChatMemberAsync(ChatId chatId, long userId, bool? onlyIfBanned = null, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<User> GetMe(CancellationToken cancellationToken = default) => Task.FromResult(new User { Id = BotId, IsBot = true, Username = "offline_golden_bot" });
    public Task DeleteMessage(ChatId chatId, int messageId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<Message> SendMessage(ChatId chatId, string text, ParseMode? parseMode = null, ReplyParameters? replyParameters = null, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default) => Task.FromResult(MakeMessage(chatId.Identifier ?? 0, text));
    public Task<Chat> GetChat(ChatId chatId, CancellationToken cancellationToken = default) => Task.FromResult(new Chat { Id = chatId.Identifier ?? 0, Type = ChatType.Supergroup, Title = "GoldenBaselineChat" });
    public Task<ChatFullInfo> GetChatFullInfo(ChatId chatId, CancellationToken cancellationToken = default) => Task.FromResult(new ChatFullInfo { Id = chatId.Identifier ?? 0, Type = ChatType.Supergroup });
    public Task<Message> ForwardMessage(ChatId chatId, ChatId fromChatId, int messageId, CancellationToken cancellationToken = default) => Task.FromResult(MakeMessage(chatId.Identifier ?? 0, "Forwarded"));
    public Task BanChatMember(ChatId chatId, long userId, DateTime? untilDate = null, bool revokeMessages = false, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task BanChatSenderChat(ChatId chatId, long senderChatId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RestrictChatMember(ChatId chatId, long userId, ChatPermissions permissions, DateTime? untilDate = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<Message> SendPhoto(ChatId chatId, object photo, string? caption = null, ParseMode? parseMode = null, ReplyParameters? replyParameters = null, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default) => Task.FromResult(MakeMessage(chatId.Identifier ?? 0, caption ?? "photo"));
    public Task<int> GetChatMemberCount(ChatId chatId, CancellationToken cancellationToken = default) => Task.FromResult(1);
    public Task<Update[]> GetUpdates(int? offset = null, int? limit = null, int? timeout = null, IEnumerable<UpdateType>? allowedUpdates = null, CancellationToken cancellationToken = default) => Task.FromResult(Array.Empty<Update>());
    public Task GetInfoAndDownloadFile(string fileId, Stream destination, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task AnswerCallbackQuery(string callbackQueryId, string? text = null, bool? showAlert = null, string? url = null, int? cacheTime = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task EditMessageReplyMarkup(ChatId chatId, int messageId, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<Message> EditMessageText(ChatId chatId, int messageId, string text, ParseMode? parseMode = null, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default) => Task.FromResult(MakeMessage(chatId.Identifier ?? 0, text));
    public Task UnbanChatMember(ChatId chatId, long userId, bool? onlyIfBanned = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<ChatMember> GetChatMember(ChatId chatId, long userId, CancellationToken cancellationToken = default) => Task.FromResult<ChatMember>(new ChatMemberMember { User = new User { Id = userId, IsBot = false, FirstName = "Baseline", LastName = "User", Username = userId == 900000001 ? "baseline_user" : "user" + userId } });
    public Task<ChatMember[]> GetChatAdministratorsAsync(ChatId chatId, CancellationToken cancellationToken = default) => Task.FromResult(new ChatMember[] { new ChatMemberAdministrator { User = new User { Id = BotId, IsBot = true } } });
    public Task<Chat> GetChatAsync(ChatId chatId, CancellationToken cancellationToken = default) => GetChat(chatId, cancellationToken);
}
