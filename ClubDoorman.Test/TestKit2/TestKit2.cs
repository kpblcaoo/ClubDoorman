using ClubDoorman.Tests.TestKit2.Core;
using ClubDoorman.Tests.TestKit2.Fakes;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.UserBan;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using AutoFixture;
using AutoFixture.AutoMoq;

namespace ClubDoorman.Tests.TestKit2;

/// <summary>
/// Главный API для TestKit2 - использует AutoFixture + AutoMoq
/// </summary>
public static class TestKit2
{
    private static readonly IFixture _fixture = CreateFixture();

    /// <summary>
    /// Создать настроенный AutoFixture для TestKit2
    /// </summary>
    public static IFixture CreateFixture()
    {
        var fixture = new Fixture()
            .Customize(new AutoMoqCustomization { ConfigureMembers = true });

        // Убираем ThrowingRecursionBehavior и добавляем OmitOnRecursionBehavior
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Кастомизируем только то, что действительно нужно фейкать
        fixture.Customize<ITelegramBotClientWrapper>(composer => composer
            .FromFactory(() => new FakeTelegramBotClientWrapper()));

        fixture.Customize<IUserBanService>(composer => composer
            .FromFactory(() => new FakeUserBanService()));

        return fixture;
    }

    /// <summary>
    /// Создать базовый TestApp с AutoFixture
    /// </summary>
    public static TestApp CreateApp()
    {
        return new TestApp(_fixture);
    }

    /// <summary>
    /// Создать TestApp с билдером
    /// </summary>
    public static TestAppBuilder CreateAppBuilder()
    {
        return TestApp.CreateBuilder();
    }

    /// <summary>
    /// Создать любой объект с автозависимостями
    /// </summary>
    public static T Create<T>()
    {
        return _fixture.Create<T>();
    }

    /// <summary>
    /// Создать несколько объектов
    /// </summary>
    public static IEnumerable<T> CreateMany<T>(int count = 3)
    {
        return _fixture.CreateMany<T>(count);
    }

    /// <summary>
    /// Создать объект с кастомизацией
    /// </summary>
    public static T CreateWith<T>(Action<T> customization) where T : class
    {
        var obj = Create<T>();
        customization(obj);
        return obj;
    }

    /// <summary>
    /// Создать MessageHandler с автозависимостями
    /// </summary>
    public static MessageHandler CreateMessageHandler()
    {
        return _fixture.Create<MessageHandler>();
    }

    /// <summary>
    /// Создать MessageEnvelope
    /// </summary>
    public static MessageEnvelope CreateEnvelope(
        long userId = 123456789,
        long chatId = -1001234567890,
        string text = "Test message",
        string? username = "testuser",
        string? firstName = "Test",
        string? lastName = "User")
    {
        return TestDataFactory.CreateEnvelope(userId, chatId, text, username, firstName, lastName);
    }

    /// <summary>
    /// Создать спам MessageEnvelope
    /// </summary>
    public static MessageEnvelope CreateSpamEnvelope(
        long userId = 123456789,
        long chatId = -1001234567890)
    {
        return TestDataFactory.CreateSpamEnvelope(userId, chatId);
    }

    /// <summary>
    /// Создать команду MessageEnvelope
    /// </summary>
    public static MessageEnvelope CreateCommandEnvelope(
        long userId = 123456789,
        long chatId = -1001234567890,
        string command = "/start")
    {
        return TestDataFactory.CreateCommandEnvelope(userId, chatId, command);
    }

    /// <summary>
    /// Создать нового пользователя MessageEnvelope
    /// </summary>
    public static MessageEnvelope CreateNewUserEnvelope(
        long userId = 123456789,
        long chatId = -1001234567890)
    {
        return TestDataFactory.CreateNewUserEnvelope(userId, chatId);
    }

    /// <summary>
    /// Создать Message из MessageEnvelope
    /// </summary>
    public static Message CreateMessageFromEnvelope(MessageEnvelope envelope)
    {
        return TestDataFactory.CreateMessageFromEnvelope(envelope);
    }

    /// <summary>
    /// Создать Update из MessageEnvelope
    /// </summary>
    public static Update CreateUpdateFromEnvelope(MessageEnvelope envelope)
    {
        return TestDataFactory.CreateUpdateFromEnvelope(envelope);
    }

    /// <summary>
    /// Сбросить счетчик MessageId
    /// </summary>
    public static void ResetMessageIdCounter()
    {
        TestDataFactory.ResetMessageIdCounter();
    }

    /// <summary>
    /// Установить следующий MessageId
    /// </summary>
    public static void SetNextMessageId(int messageId)
    {
        TestDataFactory.SetNextMessageId(messageId);
    }

    // Устаревшие методы для обратной совместимости
    [Obsolete("Используйте CreateEnvelope вместо CreateMessage")]
    public static Message CreateMessage(
        long chatId = -1001234567890,
        long userId = 123456789,
        string text = "Test message",
        string? username = "testuser",
        string? firstName = "Test",
        string? lastName = "User")
    {
        var envelope = CreateEnvelope(userId, chatId, text, username, firstName, lastName);
        return CreateMessageFromEnvelope(envelope);
    }

    [Obsolete("Используйте CreateUpdateFromEnvelope")]
    public static Update CreateUpdate(Message message) => new() { Message = message };

    [Obsolete("Используйте CreateSpamEnvelope")]
    public static Message CreateSpamMessage(
        long chatId = -1001234567890,
        long userId = 123456789)
    {
        var envelope = CreateSpamEnvelope(userId, chatId);
        return CreateMessageFromEnvelope(envelope);
    }

    [Obsolete("Используйте CreateNewUserEnvelope")]
    public static Message CreateNewUserMessage(
        long chatId = -1001234567890,
        long userId = 123456789)
    {
        var envelope = CreateNewUserEnvelope(userId, chatId);
        return CreateMessageFromEnvelope(envelope);
    }

    [Obsolete("Используйте CreateCommandEnvelope")]
    public static Message CreateCommand(
        long chatId = -1001234567890,
        long userId = 123456789,
        string command = "/start")
    {
        var envelope = CreateCommandEnvelope(userId, chatId, command);
        return CreateMessageFromEnvelope(envelope);
    }
}
