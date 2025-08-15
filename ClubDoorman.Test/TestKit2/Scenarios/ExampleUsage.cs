using Xunit;
using FluentAssertions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Tests.TestKit2.Scenarios;

/// <summary>
/// Примеры использования TestKit2
/// </summary>
public class ExampleUsage
{
    [Fact]
    public void Example_CreateBasicObjects()
    {
        // Создание базовых объектов с AutoFixture
        var user = TestKit2.Create<Telegram.Bot.Types.User>();
        var chat = TestKit2.Create<Telegram.Bot.Types.Chat>();
        var message = TestKit2.Create<Telegram.Bot.Types.Message>();

        // Проверки
        user.Should().NotBeNull();
        chat.Should().NotBeNull();
        message.Should().NotBeNull();
    }

    [Fact]
    public void Example_CreateEnvelopes()
    {
        // Создание различных типов MessageEnvelope
        var normalEnvelope = TestKit2.CreateEnvelope(text: "Normal message");
        var spamEnvelope = TestKit2.CreateSpamEnvelope();
        var commandEnvelope = TestKit2.CreateCommandEnvelope(command: "/start");
        var newUserEnvelope = TestKit2.CreateNewUserEnvelope();

        // Проверки
        normalEnvelope.Text.Should().Be("Normal message");
        spamEnvelope.Text.Should().Contain("BUY NOW");
        commandEnvelope.Text.Should().Be("/start");
        newUserEnvelope.Username.Should().Be("newuser");
    }

    [Fact]
    public void Example_CreateMessageHandler()
    {
        // Создание MessageHandler с автозависимостями
        var handler = TestKit2.CreateMessageHandler();

        // Проверки
        handler.Should().NotBeNull();
    }

    [Fact]
    public void Example_CreateWithCustomization()
    {
        // Создание объекта с кастомизацией
        var customUser = TestKit2.CreateWith<Telegram.Bot.Types.User>(u => 
        {
            u.Id = 12345;
            u.FirstName = "John";
            u.LastName = "Doe";
            u.Username = "johndoe";
        });

        // Проверки
        customUser.Id.Should().Be(12345);
        customUser.FirstName.Should().Be("John");
        customUser.LastName.Should().Be("Doe");
        customUser.Username.Should().Be("johndoe");
    }

    [Fact]
    public void Example_CreateManyObjects()
    {
        // Создание нескольких объектов
        var users = TestKit2.CreateMany<Telegram.Bot.Types.User>(5);
        var chats = TestKit2.CreateMany<Telegram.Bot.Types.Chat>(3);

        // Проверки
        users.Should().HaveCount(5);
        chats.Should().HaveCount(3);
    }

    [Fact]
    public void Example_ConvertEnvelopeToTelegramTypes()
    {
        // Создание MessageEnvelope
        var envelope = TestKit2.CreateEnvelope(
            userId: 123456789,
            chatId: -1001234567890,
            text: "Hello from envelope"
        );

        // Преобразование в Telegram типы
        var message = TestKit2.CreateMessageFromEnvelope(envelope);
        var update = TestKit2.CreateUpdateFromEnvelope(envelope);

        // Проверки
        message.Text.Should().Be("Hello from envelope");
        message.From!.Id.Should().Be(123456789);
        message.Chat.Id.Should().Be(-1001234567890);
        
        update.Message.Should().NotBeNull();
    }

    [Fact]
    public void Example_TestAppUsage()
    {
        // Создание TestApp
        using var app = TestKit2.CreateApp();

        // Доступ к фейкам
        app.TelegramClient.Should().NotBeNull();
        app.UserBanService.Should().NotBeNull();
        app.EffectsSink.Should().NotBeNull();

        // Создание объектов через TestApp
        var user = app.Create<Telegram.Bot.Types.User>();
        var service = app.Create<object>();

        // Проверки
        user.Should().NotBeNull();
        service.Should().NotBeNull();
    }

    [Fact]
    public void Example_IntegrationTest()
    {
        // Пример интеграционного теста
        using var app = TestKit2.CreateApp();
        var handler = TestKit2.CreateMessageHandler();
        var envelope = TestKit2.CreateEnvelope(text: "Test message");
        var update = TestKit2.CreateUpdateFromEnvelope(envelope);

        // Act (в реальном тесте здесь был бы вызов handler.HandleAsync)
        // await handler.HandleAsync(update, CancellationToken.None);

        // Assert
        app.TelegramClient.Should().NotBeNull();
        envelope.Text.Should().Be("Test message");
        update.Message.Should().NotBeNull();
    }
}
