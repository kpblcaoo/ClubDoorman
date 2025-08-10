using ClubDoorman.Services.Commands;
using ClubDoorman.Test.TestKit;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Services.Commands;

/// <summary>
/// Тесты для CommandRouter
/// <tags>unit, command-router, routing</tags>
/// </summary>
[TestFixture]
[Category("unit")]
[Category("command-router")]
public class CommandRouterTests
{
    private Mock<ILogger<CommandRouter>> _loggerMock = null!;
    private Mock<ICommandHandler> _commandHandler1Mock = null!;
    private Mock<ICommandHandler> _commandHandler2Mock = null!;
    private CommandRouter _commandRouter = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<CommandRouter>>();
        _commandHandler1Mock = new Mock<ICommandHandler>();
        _commandHandler1Mock.Setup(x => x.CommandName).Returns("test1");
        
        _commandHandler2Mock = new Mock<ICommandHandler>();
        _commandHandler2Mock.Setup(x => x.CommandName).Returns("test2");
        
        var handlers = new List<ICommandHandler> { _commandHandler1Mock.Object, _commandHandler2Mock.Object };
        _commandRouter = new CommandRouter(handlers, _loggerMock.Object);
    }

    /// <summary>
    /// Проверка успешной маршрутизации команды к соответствующему обработчику
    /// <tags>routing, success</tags>
    /// </summary>
    [Test]
    public async Task HandleCommandAsync_ValidCommand_RoutesToCorrectHandler()
    {
        // Arrange
        var message = TK.CreateMessage();
        message.Text = "/test1";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _commandRouter.HandleCommandAsync(message, cancellationToken);

        // Assert
        Assert.That(result, Is.True);
        _commandHandler1Mock.Verify(x => x.HandleAsync(message, cancellationToken), Times.Once);
        _commandHandler2Mock.Verify(x => x.HandleAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Проверка, что неизвестная команда не обрабатывается
    /// <tags>routing, unknown-command</tags>
    /// </summary>
    [Test]
    public async Task HandleCommandAsync_UnknownCommand_ReturnsFalse()
    {
        // Arrange
        var message = TK.CreateMessage();
        message.Text = "/unknown";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _commandRouter.HandleCommandAsync(message, cancellationToken);

        // Assert
        Assert.That(result, Is.False);
        _commandHandler1Mock.Verify(x => x.HandleAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Never);
        _commandHandler2Mock.Verify(x => x.HandleAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Проверка, что не-команда не обрабатывается
    /// <tags>routing, non-command</tags>
    /// </summary>
    [Test]
    public async Task HandleCommandAsync_NonCommand_ReturnsFalse()
    {
        // Arrange
        var message = TK.CreateMessage();
        message.Text = "regular message";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _commandRouter.HandleCommandAsync(message, cancellationToken);

        // Assert
        Assert.That(result, Is.False);
        _commandHandler1Mock.Verify(x => x.HandleAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Never);
        _commandHandler2Mock.Verify(x => x.HandleAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Проверка маршрутизации второй команды
    /// <tags>routing, second-handler</tags>
    /// </summary>
    [Test]
    public async Task HandleCommandAsync_SecondCommand_RoutesToSecondHandler()
    {
        // Arrange
        var message = TK.CreateMessage();
        message.Text = "/test2 with parameters";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _commandRouter.HandleCommandAsync(message, cancellationToken);

        // Assert
        Assert.That(result, Is.True);
        _commandHandler2Mock.Verify(x => x.HandleAsync(message, cancellationToken), Times.Once);
        _commandHandler1Mock.Verify(x => x.HandleAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}