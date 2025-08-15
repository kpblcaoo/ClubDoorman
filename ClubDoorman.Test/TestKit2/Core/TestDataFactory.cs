using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Tests.TestKit2.Core;

/// <summary>
/// Фабрика для создания тестовых данных с поддержкой MessageEnvelope
/// </summary>
public static class TestDataFactory
{
    private static int _nextMessageId = 1;

    /// <summary>
    /// Сбросить счетчик MessageId
    /// </summary>
    public static void ResetMessageIdCounter()
    {
        _nextMessageId = 1;
    }

    /// <summary>
    /// Установить следующий MessageId
    /// </summary>
    public static void SetNextMessageId(int messageId)
    {
        _nextMessageId = messageId;
    }

    /// <summary>
    /// Создать Message с правильным MessageId
    /// </summary>
    public static Message CreateValidMessageWithId(int messageId)
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Text = "Test message",
            From = new User
            {
                Id = 123456789,
                IsBot = false,
                FirstName = "Test",
                LastName = "User",
                Username = "testuser"
            },
            Chat = new Chat
            {
                Id = -1001234567890,
                Type = ChatType.Group,
                Title = "Test Group"
            }
        };
    }

    /// <summary>
    /// Создать Message из MessageEnvelope
    /// </summary>
    public static Message CreateMessageFromEnvelope(MessageEnvelope envelope)
    {
        return new Message
        {
            Date = envelope.Date ?? DateTime.UtcNow,
            Text = envelope.Text,
            From = new User
            {
                Id = envelope.UserId,
                IsBot = envelope.IsBot,
                FirstName = envelope.FirstName ?? "Test",
                LastName = envelope.LastName ?? "User",
                Username = envelope.Username
            },
            Chat = new Chat
            {
                Id = envelope.ChatId,
                Type = ChatType.Group,
                Title = envelope.ChatTitle ?? "Test Group",
                Username = envelope.ChatUsername
            }
        };
    }

    /// <summary>
    /// Создать Update из MessageEnvelope
    /// </summary>
    public static Update CreateUpdateFromEnvelope(MessageEnvelope envelope)
    {
        return new Update
        {
            Message = CreateMessageFromEnvelope(envelope)
        };
    }

    /// <summary>
    /// Создать MessageEnvelope с автоматическим MessageId
    /// </summary>
    public static MessageEnvelope CreateEnvelope(
        long userId = 123456789,
        long chatId = -1001234567890,
        string text = "Test message",
        string? username = "testuser",
        string? firstName = "Test",
        string? lastName = "User")
    {
        return new MessageEnvelope(
            MessageId: _nextMessageId++,
            UserId: userId,
            ChatId: chatId,
            Text: text,
            Username: username,
            FirstName: firstName,
            LastName: lastName,
            IsBot: false,
            ChatTitle: "Test Group",
            Date: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Создать спам MessageEnvelope
    /// </summary>
    public static MessageEnvelope CreateSpamEnvelope(
        long userId = 123456789,
        long chatId = -1001234567890)
    {
        return new MessageEnvelope(
            MessageId: _nextMessageId++,
            UserId: userId,
            ChatId: chatId,
            Text: "BUY NOW!!! AMAZING OFFER!!!",
            Username: "spammer",
            FirstName: "Spam",
            LastName: "User",
            IsBot: false,
            ChatTitle: "Test Group",
            Date: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Создать команду MessageEnvelope
    /// </summary>
    public static MessageEnvelope CreateCommandEnvelope(
        long userId = 123456789,
        long chatId = -1001234567890,
        string command = "/start")
    {
        return new MessageEnvelope(
            MessageId: _nextMessageId++,
            UserId: userId,
            ChatId: chatId,
            Text: command,
            Username: "admin",
            FirstName: "Admin",
            LastName: "User",
            IsBot: false,
            ChatTitle: "Test Group",
            Date: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Создать нового пользователя MessageEnvelope
    /// </summary>
    public static MessageEnvelope CreateNewUserEnvelope(
        long userId = 123456789,
        long chatId = -1001234567890)
    {
        return new MessageEnvelope(
            MessageId: _nextMessageId++,
            UserId: userId,
            ChatId: chatId,
            Text: "",
            Username: "newuser",
            FirstName: "New",
            LastName: "User",
            IsBot: false,
            ChatTitle: "Test Group",
            Date: DateTime.UtcNow
        );
    }
}
