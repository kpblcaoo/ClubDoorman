using ClubDoorman.Services.UserBan;
using ClubDoorman.Handlers;
using ClubDoorman.Services;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Test.TestKit;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using ClubDoorman.Features.AdminOps;

namespace ClubDoorman.Test.Unit.Services;

/// <summary>
/// Тесты для CommandProcessingService
/// <tags>unit, services, command-processing, proxy</tags>
/// </summary>
[TestFixture]
[Category("unit")]
[Category("services")]
[Category("command-processing")]
public class CommandProcessingServiceTests
{
    private Mock<ICommandRouter> _commandRouterMock = null!;
    private Mock<ILogger<CommandProcessingService>> _loggerMock = null!;
    private CommandProcessingService _service = null!;

    [SetUp]
    public void Setup()
    {
        _commandRouterMock = new Mock<ICommandRouter>();
        _loggerMock = new Mock<ILogger<CommandProcessingService>>();
        _service = new CommandProcessingService(_commandRouterMock.Object, _loggerMock.Object);
    }

    /// <summary>
    /// POC: Проверка проксирования вызова HandleCommandAsync
    /// <tags>poc, proxy, command-processing</tags>
    /// </summary>
    [Test]
    public async Task HandleCommandAsync_ValidMessage_ProxiesToMessageHandler()
    {
        // Arrange
        var message = TK.CreateMessage();
        var cancellationToken = CancellationToken.None;

        // Act
        await _service.HandleCommandAsync(message, cancellationToken);

        // Assert
        _commandRouterMock.Verify(x => x.HandleCommandAsync(message, cancellationToken), Times.Once);
    }
} 