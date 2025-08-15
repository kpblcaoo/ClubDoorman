using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Tests.TestKit2.Core;

/// <summary>
/// Фабрика для создания тестовых данных
/// </summary>
public static class TestData
{
    /// <summary>
    /// Создать сообщение
    /// </summary>
    public static Message CreateMessage(
        long chatId = -1001234567890,
        long userId = 123456789,
        string text = "Test message",
        string? username = "testuser",
        string? firstName = "Test",
        string? lastName = "User")
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Text = text,
            From = new User
            {
                Id = userId,
                IsBot = false,
                FirstName = firstName,
                LastName = lastName,
                Username = username
            },
            Chat = new Chat
            {
                Id = chatId,
                Type = ChatType.Group,
                Title = "Test Group"
            }
        };
    }

    /// <summary>
    /// Создать Update
    /// </summary>
    public static Update CreateUpdate(Message message) => new() { Message = message };

    /// <summary>
    /// Создать Update с параметрами
    /// </summary>
    public static Update CreateUpdate(
        long chatId = -1001234567890,
        long userId = 123456789,
        string text = "Test message")
    {
        return CreateUpdate(CreateMessage(chatId, userId, text));
    }

    /// <summary>
    /// Создать спам-сообщение
    /// </summary>
    public static Message CreateSpamMessage(
        long chatId = -1001234567890,
        long userId = 123456789)
    {
        return CreateMessage(chatId, userId, "BUY NOW!!! AMAZING OFFER!!!", "spammer", "Spam", "User");
    }

    /// <summary>
    /// Создать сообщение от нового пользователя
    /// </summary>
    public static Message CreateNewUserMessage(
        long chatId = -1001234567890,
        long userId = 123456789)
    {
        return CreateMessage(chatId, userId, "", "newuser", "New", "User");
    }

    /// <summary>
    /// Создать пользователя
    /// </summary>
    public static User CreateUser(
        long userId = 123456789,
        string? username = "testuser",
        string? firstName = "Test",
        string? lastName = "User",
        bool isBot = false)
    {
        return new User
        {
            Id = userId,
            IsBot = isBot,
            FirstName = firstName,
            LastName = lastName,
            Username = username
        };
    }

    /// <summary>
    /// Создать чат
    /// </summary>
    public static Chat CreateChat(
        long chatId = -1001234567890,
        ChatType type = ChatType.Group,
        string? title = "Test Group")
    {
        return new Chat
        {
            Id = chatId,
            Type = type,
            Title = title
        };
    }
}
