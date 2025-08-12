using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Tests.TestKit2;

public static class TestBuilders
{
    public static MessageBuilder Message() => new();
    public static UpdateBuilder Update() => new();
    public static UserBuilder User() => new();
    public static ChatBuilder Chat() => new();
    
    // Специализированные методы для создания конкретных типов Update'ов
    public static Update NewMemberJoin(long chatId, long userId, string? username = null)
    {
        return new Update
        {
            Message = new Message
            {
                Chat = new Chat { Id = chatId, Type = ChatType.Supergroup },
                NewChatMembers = new[] { new User { Id = userId, Username = username } },
                Date = DateTime.UtcNow
            }
        };
    }

    public static Update ForwardedMessage(long chatId, long fromUserId, long forwardFromId, string? text = null)
    {
        return new Update
        {
            Message = new Message
            {
                Chat = new Chat { Id = chatId, Type = ChatType.Supergroup },
                From = new User { Id = fromUserId },
                Text = text,
                Date = DateTime.UtcNow
            }
        };
    }

    public static Update ChannelPost(long chatId, string? text = null)
    {
        return new Update
        {
            ChannelPost = new Message
            {
                Chat = new Chat { Id = chatId, Type = ChatType.Channel },
                Text = text,
                Date = DateTime.UtcNow
            }
        };
    }

    public static Update EditedMessage(long chatId, long userId, string? newText = null)
    {
        return new Update
        {
            EditedMessage = new Message
            {
                Chat = new Chat { Id = chatId, Type = ChatType.Supergroup },
                From = new User { Id = userId },
                Text = newText,
                Date = DateTime.UtcNow
            }
        };
    }

    public static Update MediaWithCaption(long chatId, long userId, string? caption = null)
    {
        return new Update
        {
            Message = new Message
            {
                Chat = new Chat { Id = chatId, Type = ChatType.Supergroup },
                From = new User { Id = userId },
                Caption = caption,
                Photo = new[] { new PhotoSize { FileId = "photoId" } },
                Date = DateTime.UtcNow
            }
        };
    }

    public static Update PrivateChatCommand(long userId, string command)
    {
        return new Update
        {
            Message = new Message
            {
                Chat = new Chat { Id = userId, Type = ChatType.Private },
                From = new User { Id = userId },
                Text = command,
                Entities = new[] { new MessageEntity { Type = MessageEntityType.BotCommand, Offset = 0, Length = command.Length } },
                Date = DateTime.UtcNow
            }
        };
    }
}

public sealed class MessageBuilder
{
    private int _messageId = 1;
    private DateTime _date = DateTime.UtcNow;
    private Chat? _chat;
    private User? _from;
    private string? _text;
    private string? _caption;

    public MessageBuilder WithId(int id) { _messageId = id; return this; }
    public MessageBuilder WithDate(DateTime date) { _date = date; return this; }
    public MessageBuilder WithChat(Chat chat) { _chat = chat; return this; }
    public MessageBuilder WithFrom(User user) { _from = user; return this; }
    public MessageBuilder WithText(string text) { _text = text; return this; }
    public MessageBuilder WithCaption(string caption) { _caption = caption; return this; }

    public Message Build() => new()
    {
        Date = _date,
                    Chat = _chat ?? TestBuilders.Chat().Build(),
        From = _from,
        Text = _text,
        Caption = _caption
    };
}

public sealed class UpdateBuilder
{
    private int _id = 1;
    private Message? _message;

    public UpdateBuilder WithId(int id) { _id = id; return this; }
    public UpdateBuilder WithMessage(Message message) { _message = message; return this; }

    public Update Build() => new()
    {
        Id = _id,
        Message = _message
    };
}

public sealed class UserBuilder
{
    private long _id = 123;
    private bool _isBot = false;
    private string _firstName = "TestUser";
    private string? _lastName;
    private string? _username;

    public UserBuilder WithId(long id) { _id = id; return this; }
    public UserBuilder WithFirstName(string firstName) { _firstName = firstName; return this; }
    public UserBuilder WithLastName(string lastName) { _lastName = lastName; return this; }
    public UserBuilder WithUsername(string username) { _username = username; return this; }
    public UserBuilder AsBot() { _isBot = true; return this; }

    public User Build() => new()
    {
        Id = _id,
        IsBot = _isBot,
        FirstName = _firstName,
        LastName = _lastName,
        Username = _username
    };
}

public sealed class ChatBuilder
{
    private long _id = 100;
    private ChatType _type = ChatType.Supergroup;
    private string? _title;

    public ChatBuilder WithId(long id) { _id = id; return this; }
    public ChatBuilder WithType(ChatType type) { _type = type; return this; }
    public ChatBuilder WithTitle(string title) { _title = title; return this; }

    public Chat Build() => new()
    {
        Id = _id,
        Type = _type,
        Title = _title
    };
}
