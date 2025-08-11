using ClubDoorman.Services.Commands;
using ClubDoorman.Test.TestKit;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Commands;

/// <summary>
/// Тесты для CommandRouter - маршрутизатора команд
/// <tags>unit, commands, router, command-routing</tags>
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Commands")]
public class CommandRouterTests
{
    private Mock<ILogger<CommandRouter>> _loggerMock;
    private Mock<ICommandHandler> _statsHandlerMock;
    private Mock<ICommandHandler> _sayHandlerMock;
    private CommandRouter _router;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<CommandRouter>>();
        _statsHandlerMock = new Mock<ICommandHandler>();
        _sayHandlerMock = new Mock<ICommandHandler>();

        _statsHandlerMock.Setup(x => x.CommandName).Returns("stat");
        _sayHandlerMock.Setup(x => x.CommandName).Returns("say");

        var handlers = new List<ICommandHandler> { _statsHandlerMock.Object, _sayHandlerMock.Object };
        _router = new CommandRouter(handlers, _loggerMock.Object);
    }

    /// <summary>
    /// Тест проверяет, что команда /stat правильно маршрутизируется к соответствующему обработчику
    /// <tags>command-routing, stat-command, success-path</tags>
    /// </summary>
    [Test]
    public async Task HandleCommandAsync_WithStatCommand_CallsStatsHandler()
    {
        // Arrange
        var message = TK.CreateMessage();
        message.Text = "/stat";

        // Act
        var result = await _router.HandleCommandAsync(message, CancellationToken.None);

        // Assert
        Assert.That(result, Is.True, "Команда должна быть обработана");
        _statsHandlerMock.Verify(
            x => x.HandleAsync(message, It.IsAny<CancellationToken>()),
            Times.Once,
            "StatsHandler должен быть вызван для команды /stat");
        _sayHandlerMock.Verify(
            x => x.HandleAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "SayHandler НЕ должен быть вызван для команды /stat");
    }

    /// <summary>
    /// Тест проверяет, что команда /stats НЕ маршрутизируется (поскольку зарегистрирован handler с именем "stat")
    /// <tags>command-routing, stats-command, routing-specifics</tags>
    /// </summary>
    [Test]
    public async Task HandleCommandAsync_WithStatsCommand_DoesNotRoute()
    {
        // Arrange
        var message = TK.CreateStatsCommandMessage(); // /stats

        // Act
        var result = await _router.HandleCommandAsync(message, CancellationToken.None);

        // Assert
        Assert.That(result, Is.False, "Команда /stats НЕ должна быть обработана CommandRouter (обработчик зарегистрирован с именем 'stat')");
        _statsHandlerMock.Verify(
            x => x.HandleAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "StatsHandler НЕ должен быть вызван для команды /stats через CommandRouter");
    }

    /// <summary>
    /// Тест проверяет, что команда /say правильно маршрутизируется к соответствующему обработчику
    /// <tags>command-routing, say-command, success-path</tags>
    /// </summary>
    [Test]
    public async Task HandleCommandAsync_WithSayCommand_CallsSayHandler()
    {
        // Arrange
        var message = TK.CreateSayCommandMessage();

        // Act
        var result = await _router.HandleCommandAsync(message, CancellationToken.None);

        // Assert
        Assert.That(result, Is.True, "Команда должна быть обработана");
        _sayHandlerMock.Verify(
            x => x.HandleAsync(message, It.IsAny<CancellationToken>()),
            Times.Once,
            "SayHandler должен быть вызван для команды /say");
        _statsHandlerMock.Verify(
            x => x.HandleAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "StatsHandler НЕ должен быть вызван для команды /say");
    }

    /// <summary>
    /// Тест проверяет, что неизвестная команда не обрабатывается
    /// <tags>command-routing, unknown-command, error-path</tags>
    /// </summary>
    [Test]
    public async Task HandleCommandAsync_WithUnknownCommand_ReturnsFalse()
    {
        // Arrange
        var message = TK.CreateMessage();
        message.Text = "/unknown";

        // Act
        var result = await _router.HandleCommandAsync(message, CancellationToken.None);

        // Assert
        Assert.That(result, Is.False, "Неизвестная команда НЕ должна быть обработана");
        _statsHandlerMock.Verify(
            x => x.HandleAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "StatsHandler НЕ должен быть вызван для неизвестной команды");
        _sayHandlerMock.Verify(
            x => x.HandleAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "SayHandler НЕ должен быть вызван для неизвестной команды");
    }

    /// <summary>
    /// Тест проверяет, что сообщение без команды не обрабатывается
    /// <tags>command-routing, non-command-message, edge-case</tags>
    /// </summary>
    [Test]
    public async Task HandleCommandAsync_WithNonCommandMessage_ReturnsFalse()
    {
        // Arrange
        var message = TK.CreateMessage();
        message.Text = "Обычное сообщение без команды";

        // Act
        var result = await _router.HandleCommandAsync(message, CancellationToken.None);

        // Assert
        Assert.That(result, Is.False, "Обычное сообщение НЕ должно быть обработано как команда");
        _statsHandlerMock.Verify(
            x => x.HandleAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "StatsHandler НЕ должен быть вызван для обычного сообщения");
        _sayHandlerMock.Verify(
            x => x.HandleAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "SayHandler НЕ должен быть вызван для обычного сообщения");
    }

    /// <summary>
    /// Тест проверяет, что null сообщение не обрабатывается
    /// <tags>command-routing, null-message, edge-case</tags>
    /// </summary>
    [Test]
    public async Task HandleCommandAsync_WithNullMessage_ReturnsFalse()
    {
        // Act
        var result = await _router.HandleCommandAsync(null!, CancellationToken.None);

        // Assert
        Assert.That(result, Is.False, "Null сообщение НЕ должно быть обработано");
        _statsHandlerMock.Verify(
            x => x.HandleAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "StatsHandler НЕ должен быть вызван для null сообщения");
        _sayHandlerMock.Verify(
            x => x.HandleAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "SayHandler НЕ должен быть вызван для null сообщения");
    }

    /// <summary>
    /// Тест проверяет обработку команды с дополнительными параметрами
    /// <tags>command-routing, command-with-params, success-path</tags>
    /// </summary>
    [Test]
    public async Task HandleCommandAsync_WithCommandAndParameters_CallsCorrectHandler()
    {
        // Arrange
        var message = TK.CreateMessage();
        message.Text = "/say Hello World";

        // Act
        var result = await _router.HandleCommandAsync(message, CancellationToken.None);

        // Assert
        Assert.That(result, Is.True, "Команда с параметрами должна быть обработана");
        _sayHandlerMock.Verify(
            x => x.HandleAsync(message, It.IsAny<CancellationToken>()),
            Times.Once,
            "SayHandler должен быть вызван для команды /say с параметрами");
    }

    /// <summary>
    /// Тест проверяет создание CommandRouter без обработчиков
    /// <tags>command-routing, initialization, edge-case</tags>
    /// </summary>
    [Test]
    public void Constructor_WithEmptyHandlers_CreatesRouterWithoutCommands()
    {
        // Arrange & Act
        var emptyRouter = new CommandRouter(new List<ICommandHandler>(), _loggerMock.Object);

        // Assert
        Assert.That(emptyRouter, Is.Not.Null, "CommandRouter должен быть создан даже без обработчиков");
    }

    /// <summary>
    /// Тест проверяет обработку команды в верхнем регистре
    /// <tags>command-routing, case-sensitivity, normalization</tags>
    /// </summary>
    [Test]
    public async Task HandleCommandAsync_WithUpperCaseCommand_CallsCorrectHandler()
    {
        // Arrange
        var message = TK.CreateMessage();
        message.Text = "/STAT";

        // Act
        var result = await _router.HandleCommandAsync(message, CancellationToken.None);

        // Assert
        Assert.That(result, Is.True, "Команда в верхнем регистре должна быть обработана");
        _statsHandlerMock.Verify(
            x => x.HandleAsync(message, It.IsAny<CancellationToken>()),
            Times.Once,
            "StatsHandler должен быть вызван для команды /STAT");
    }
}
