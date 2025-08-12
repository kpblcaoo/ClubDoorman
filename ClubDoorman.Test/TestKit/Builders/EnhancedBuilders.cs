using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Models;

namespace ClubDoorman.Test.TestKit.Builders;

/// <summary>
/// Расширенные билдеры для создания тестовых объектов с типовыми сценариями
/// </summary>
public static class EnhancedBuilders
{
    /// <summary>
    /// Создает builder для Update
    /// </summary>
    public static UpdateBuilder CreateUpdate() => new UpdateBuilder();

    /// <summary>
    /// Создает builder для сообщений с расширенными сценариями
    /// </summary>
    public static ScenarioMessageBuilder CreateScenarioMessage() => new ScenarioMessageBuilder();

    /// <summary>
    /// Создает builder для пользователей с расширенными сценариями
    /// </summary>
    public static ScenarioUserBuilder CreateScenarioUser() => new ScenarioUserBuilder();

    /// <summary>
    /// Создает builder для чатов с расширенными сценариями
    /// </summary>
    public static ScenarioChatBuilder CreateScenarioChat() => new ScenarioChatBuilder();

    /// <summary>
    /// Создает builder для CallbackQuery
    /// </summary>
    public static CallbackQueryBuilder CreateCallbackQuery() => new CallbackQueryBuilder();
}

/// <summary>
/// Builder для создания Update объектов
/// </summary>
public class UpdateBuilder
{
    private Update _update = new();

    /// <summary>
    /// Добавить сообщение в update
    /// </summary>
    public UpdateBuilder WithMessage(Message message)
    {
        _update.Message = message;
        return this;
    }

    /// <summary>
    /// Добавить callback query в update
    /// </summary>
    public UpdateBuilder WithCallbackQuery(CallbackQuery callbackQuery)
    {
        _update.CallbackQuery = callbackQuery;
        return this;
    }

    /// <summary>
    /// Добавить изменение участника чата
    /// </summary>
    public UpdateBuilder WithChatMemberUpdate(ChatMemberUpdated chatMember)
    {
        _update.ChatMember = chatMember;
        return this;
    }

    /// <summary>
    /// Установить ID update
    /// </summary>
    public UpdateBuilder WithId(int id)
    {
        _update.Id = id;
        return this;
    }

    public Update Build() => _update;
    public static implicit operator Update(UpdateBuilder builder) => builder.Build();
}

/// <summary>
/// Builder для сообщений с типовыми сценариями
/// </summary>
public class ScenarioMessageBuilder
{
    private Message _message;

    public ScenarioMessageBuilder()
    {
        _message = new Message
        {
            MessageId = 1000,
            Date = DateTime.UtcNow,
            Chat = EnhancedBuilders.CreateScenarioChat().AsGroup().Build(),
            From = EnhancedBuilders.CreateScenarioUser().AsRegularUser().Build(),
            Text = "Test message"
        };
    }

    /// <summary>
    /// Сообщение от участника из банлиста
    /// </summary>
    public ScenarioMessageBuilder FromBannedUser()
    {
        _message.From = EnhancedBuilders.CreateScenarioUser().AsBannedUser().Build();
        _message.Text = "I'm a banned user trying to send a message";
        return this;
    }

    /// <summary>
    /// Первое сообщение неодобренного пользователя
    /// </summary>
    public ScenarioMessageBuilder FromUnapprovedFirstTimeUser()
    {
        _message.From = EnhancedBuilders.CreateScenarioUser().AsFirstTimeUser().Build();
        _message.Text = "Hello, this is my first message!";
        return this;
    }

    /// <summary>
    /// Сообщение, пересланное с канала
    /// </summary>
    public ScenarioMessageBuilder ForwardedFromChannel()
    {
        var channel = EnhancedBuilders.CreateScenarioChat().AsChannel().Build();
        _message.ForwardOrigin = new MessageOriginChannel
        {
            Date = DateTime.UtcNow.AddMinutes(-5),
            Chat = channel
        };
        _message.Text = "This message was forwarded from a channel";
        return this;
    }

    /// <summary>
    /// Команда (начинающаяся с /)
    /// </summary>
    public ScenarioMessageBuilder AsCommand(string command = "/help")
    {
        _message.Text = command;
        _message.Entities = new MessageEntity[]
        {
            new MessageEntity { Type = MessageEntityType.BotCommand, Offset = 0, Length = command.Length }
        };
        return this;
    }

    /// <summary>
    /// Обычное текстовое сообщение
    /// </summary>
    public ScenarioMessageBuilder AsRegularText(string text = "Regular text message")
    {
        _message.Text = text;
        _message.Entities = null;
        return this;
    }

    /// <summary>
    /// Спам-сообщение
    /// </summary>
    public ScenarioMessageBuilder AsSpam()
    {
        _message.Text = "🎰 КАЗИНО! ВЫИГРЫШ 100000₽! ЖМИТЕ -> casino-spam.ru";
        return this;
    }

    /// <summary>
    /// Сообщение о присоединении нового пользователя
    /// </summary>
    public ScenarioMessageBuilder AsNewUserJoined(User? newUser = null)
    {
        newUser ??= EnhancedBuilders.CreateScenarioUser().AsFirstTimeUser().Build();
        _message.NewChatMembers = new[] { newUser };
        _message.From = newUser;
        _message.Text = null;
        return this;
    }

    /// <summary>
    /// Установить отправителя
    /// </summary>
    public ScenarioMessageBuilder FromUser(User user)
    {
        _message.From = user;
        return this;
    }

    /// <summary>
    /// Установить чат
    /// </summary>
    public ScenarioMessageBuilder InChat(Chat chat)
    {
        _message.Chat = chat;
        return this;
    }

    /// <summary>
    /// Установить текст
    /// </summary>
    public ScenarioMessageBuilder WithText(string text)
    {
        _message.Text = text;
        return this;
    }

    /// <summary>
    /// Установить ID сообщения
    /// </summary>
    public ScenarioMessageBuilder WithMessageId(int messageId)
    {
        _message.MessageId = messageId;
        return this;
    }

    /// <summary>
    /// В приватном чате
    /// </summary>
    public ScenarioMessageBuilder InPrivateChat()
    {
        _message.Chat = EnhancedBuilders.CreateScenarioChat().AsPrivate().Build();
        return this;
    }

    /// <summary>
    /// В групповом чате
    /// </summary>
    public ScenarioMessageBuilder InGroupChat()
    {
        _message.Chat = EnhancedBuilders.CreateScenarioChat().AsGroup().Build();
        return this;
    }

    public Message Build() => _message;
    public static implicit operator Message(ScenarioMessageBuilder builder) => builder.Build();
}

/// <summary>
/// Builder для пользователей с типовыми сценариями
/// </summary>
public class ScenarioUserBuilder
{
    private User _user;

    public ScenarioUserBuilder()
    {
        _user = new User
        {
            Id = 12345,
            IsBot = false,
            FirstName = "Test",
            LastName = "User",
            Username = "test_user"
        };
    }

    /// <summary>
    /// Пользователь из банлиста
    /// </summary>
    public ScenarioUserBuilder AsBannedUser()
    {
        _user.Id = 99999999; // ID пользователя из банлиста
        _user.FirstName = "Banned";
        _user.LastName = "Spammer";
        _user.Username = "banned_spammer";
        return this;
    }

    /// <summary>
    /// Новый пользователь (первый раз в чате)
    /// </summary>
    public ScenarioUserBuilder AsFirstTimeUser()
    {
        _user.Id = 11111111; // Новый ID
        _user.FirstName = "New";
        _user.LastName = "User";
        _user.Username = "new_user_" + DateTime.Now.Ticks % 10000;
        return this;
    }

    /// <summary>
    /// Обычный пользователь
    /// </summary>
    public ScenarioUserBuilder AsRegularUser()
    {
        _user.Id = 12345678;
        _user.FirstName = "Regular";
        _user.LastName = "User";
        _user.Username = "regular_user";
        return this;
    }

    /// <summary>
    /// Подозрительный пользователь
    /// </summary>
    public ScenarioUserBuilder AsSuspiciousUser()
    {
        _user.Id = 88888888;
        _user.FirstName = "Suspicious";
        _user.LastName = "User";
        _user.Username = "sus_user";
        return this;
    }

    /// <summary>
    /// Пользователь-бот
    /// </summary>
    public ScenarioUserBuilder AsBot()
    {
        _user.IsBot = true;
        _user.FirstName = "Test Bot";
        _user.Username = "test_bot";
        return this;
    }

    /// <summary>
    /// Анонимный пользователь (без username)
    /// </summary>
    public ScenarioUserBuilder AsAnonymous()
    {
        _user.Username = null;
        return this;
    }

    /// <summary>
    /// Установить ID
    /// </summary>
    public ScenarioUserBuilder WithId(long id)
    {
        _user.Id = id;
        return this;
    }

    /// <summary>
    /// Установить имя
    /// </summary>
    public ScenarioUserBuilder WithName(string firstName, string? lastName = null)
    {
        _user.FirstName = firstName;
        _user.LastName = lastName;
        return this;
    }

    /// <summary>
    /// Установить username
    /// </summary>
    public ScenarioUserBuilder WithUsername(string username)
    {
        _user.Username = username;
        return this;
    }

    public User Build() => _user;
    public static implicit operator User(ScenarioUserBuilder builder) => builder.Build();
}

/// <summary>
/// Builder для чатов с типовыми сценариями
/// </summary>
public class ScenarioChatBuilder
{
    private Chat _chat;

    public ScenarioChatBuilder()
    {
        _chat = new Chat
        {
            Id = -1001234567890,
            Type = ChatType.Group,
            Title = "Test Group"
        };
    }

    /// <summary>
    /// Групповой чат
    /// </summary>
    public ScenarioChatBuilder AsGroup()
    {
        _chat.Type = ChatType.Group;
        _chat.Title = "Test Group";
        return this;
    }

    /// <summary>
    /// Супергруппа
    /// </summary>
    public ScenarioChatBuilder AsSupergroup()
    {
        _chat.Type = ChatType.Supergroup;
        _chat.Title = "Test Supergroup";
        return this;
    }

    /// <summary>
    /// Канал
    /// </summary>
    public ScenarioChatBuilder AsChannel()
    {
        _chat.Type = ChatType.Channel;
        _chat.Title = "Test Channel";
        _chat.Username = "test_channel";
        return this;
    }

    /// <summary>
    /// Приватный чат
    /// </summary>
    public ScenarioChatBuilder AsPrivate()
    {
        _chat.Type = ChatType.Private;
        _chat.Title = null;
        _chat.FirstName = "Private";
        _chat.LastName = "Chat";
        return this;
    }

    /// <summary>
    /// Чат с включенным AI
    /// </summary>
    public ScenarioChatBuilder WithAiEnabled()
    {
        // ID будет использоваться в AppConfigFake.AiEnabledChats
        return this;
    }

    /// <summary>
    /// Чат с отключенной капчей
    /// </summary>
    public ScenarioChatBuilder WithNoCaptcha()
    {
        // ID будет использоваться в AppConfigFake.NoCaptchaGroups
        return this;
    }

    /// <summary>
    /// Отключенный чат
    /// </summary>
    public ScenarioChatBuilder AsDisabled()
    {
        // ID будет использоваться в AppConfigFake.DisabledChats
        return this;
    }

    /// <summary>
    /// Установить ID
    /// </summary>
    public ScenarioChatBuilder WithId(long id)
    {
        _chat.Id = id;
        return this;
    }

    /// <summary>
    /// Установить название
    /// </summary>
    public ScenarioChatBuilder WithTitle(string title)
    {
        _chat.Title = title;
        return this;
    }

    /// <summary>
    /// Установить username
    /// </summary>
    public ScenarioChatBuilder WithUsername(string username)
    {
        _chat.Username = username;
        return this;
    }

    public Chat Build() => _chat;
    public static implicit operator Chat(ScenarioChatBuilder builder) => builder.Build();
}

/// <summary>
/// Builder для CallbackQuery
/// </summary>
public class CallbackQueryBuilder
{
    private CallbackQuery _callbackQuery;

    public CallbackQueryBuilder()
    {
        _callbackQuery = new CallbackQuery
        {
            Id = "callback_123",
            From = EnhancedBuilders.CreateScenarioUser().AsRegularUser().Build(),
            Data = "test_callback"
        };
    }

    /// <summary>
    /// Callback для одобрения админом
    /// </summary>
    public CallbackQueryBuilder AsAdminApprove()
    {
        _callbackQuery.Data = "approve_user_12345";
        return this;
    }

    /// <summary>
    /// Callback для бана админом
    /// </summary>
    public CallbackQueryBuilder AsAdminBan()
    {
        _callbackQuery.Data = "ban_user_12345";
        return this;
    }

    /// <summary>
    /// Callback для пропуска админом
    /// </summary>
    public CallbackQueryBuilder AsAdminSkip()
    {
        _callbackQuery.Data = "skip_user_12345";
        return this;
    }

    /// <summary>
    /// Callback от пользователя (не админа)
    /// </summary>
    public CallbackQueryBuilder FromUser(User user)
    {
        _callbackQuery.From = user;
        return this;
    }

    /// <summary>
    /// Установить данные callback
    /// </summary>
    public CallbackQueryBuilder WithData(string data)
    {
        _callbackQuery.Data = data;
        return this;
    }

    /// <summary>
    /// Установить ID
    /// </summary>
    public CallbackQueryBuilder WithId(string id)
    {
        _callbackQuery.Id = id;
        return this;
    }

    /// <summary>
    /// Установить сообщение
    /// </summary>
    public CallbackQueryBuilder WithMessage(Message message)
    {
        _callbackQuery.Message = message;
        return this;
    }

    public CallbackQuery Build() => _callbackQuery;
    public static implicit operator CallbackQuery(CallbackQueryBuilder builder) => builder.Build();
}