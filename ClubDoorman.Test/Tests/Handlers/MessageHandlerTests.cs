using FluentAssertions;
using Xunit;
using ClubDoorman.Handlers;
using ClubDoorman.Tests.TestKit2;
using ClubDoorman.Tests.TestKit2.Fakes;

namespace ClubDoorman.Tests.Handlers;

/// <summary>
/// Тесты обработчиков сообщений
/// Проверяют логику обработки входящих сообщений
/// </summary>
public class MessageHandlerTests
{
    [Fact]
    public async Task HandleUpdateAsync_WithValidMessage_ProcessesSuccessfully()
    {
        // Arrange
        await using var app = new TestApp();
        var messageHandler = app.Handler();
        var update = TestBuilders.Update()
            .WithMessage(TestBuilders.Message()
                .WithChat(TestBuilders.Chat().WithId(123).Build())
                .WithFrom(TestBuilders.User().WithId(456).Build())
                .WithText("Hello world")
                .Build())
            .Build();

        // Act
        await messageHandler.HandleUpdateAsync(update);

        // Assert
        // Проверяем, что обработка прошла без исключений
        // В реальном тесте здесь были бы проверки эффектов
    }

    [Fact]
    public async Task HandleUpdateAsync_WithNullUpdate_ThrowsArgumentNullException()
    {
        // Arrange
        await using var app = new TestApp();
        var messageHandler = app.Handler();
        Telegram.Bot.Types.Update? update = null;

        // Act & Assert
        await messageHandler.Invoking(x => x.HandleUpdateAsync(update!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task HandleUpdateAsync_WithEmptyMessage_ProcessesSuccessfully()
    {
        // Arrange
        await using var app = new TestApp();
        var messageHandler = app.Handler();
        var update = TestBuilders.Update()
            .WithMessage(TestBuilders.Message()
                .WithChat(TestBuilders.Chat().WithId(123).Build())
                .WithFrom(TestBuilders.User().WithId(456).Build())
                .WithText("")
                .Build())
            .Build();

        // Act
        await messageHandler.HandleUpdateAsync(update);

        // Assert
        // Проверяем, что обработка прошла без исключений
    }

    [Theory]
    [InlineData("spam message")]
    [InlineData("normal message")]
    [InlineData("")]
    [InlineData("команда /start")]
    public async Task HandleUpdateAsync_VariousMessages_ProcessesSuccessfully(string messageText)
    {
        // Arrange
        await using var app = new TestApp();
        var messageHandler = app.Handler();
        var update = TestBuilders.Update()
            .WithMessage(TestBuilders.Message()
                .WithChat(TestBuilders.Chat().WithId(123).Build())
                .WithFrom(TestBuilders.User().WithId(456).Build())
                .WithText(messageText)
                .Build())
            .Build();

        // Act
        await messageHandler.HandleUpdateAsync(update);

        // Assert
        // Проверяем, что обработка прошла без исключений
    }

    [Fact]
    public async Task HandleUpdateAsync_WithCallbackQuery_ProcessesSuccessfully()
    {
        // Arrange
        await using var app = new TestApp();
        var messageHandler = app.Handler();
        var update = TestBuilders.Update()
            .WithCallbackQuery(TestBuilders.CallbackQuery()
                .WithFrom(TestBuilders.User().WithId(456).Build())
                .WithData("test_callback")
                .Build())
            .Build();

        // Act
        await messageHandler.HandleUpdateAsync(update);

        // Assert
        // Проверяем, что обработка прошла без исключений
    }
}
