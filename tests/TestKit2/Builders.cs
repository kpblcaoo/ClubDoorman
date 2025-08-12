using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Tests.TestKit2;

public static class Builders
{
    public static MessageBuilder Message() => new();
    public static UpdateBuilder Update() => new();
    public static UserBuilder User() => new();
    public static ChatBuilder Chat() => new();
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
        MessageId = _messageId,
        Date = _date,
        Chat = _chat ?? Builders.Chat().Build(),
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
