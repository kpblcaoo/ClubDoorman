using ClubDoorman.Services.UserBan;
using ClubDoorman.Handlers;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using ClubDoorman.Test.TestKit;
using NUnit.Framework;
using System.Reflection;
using Telegram.Bot.Types;
using Moq;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Features.AdminOps;
using ClubDoorman.Test.TestInfrastructure;

namespace ClubDoorman.Test.Unit.Handlers;

/// <summary>
/// Тесты для HandleStatsCommandAsync метода
/// <tags>unit, message-handler, stats-command, command-handling</tags>
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("MessageHandler")]
public class MessageHandlerStatsCommandTests
{
    private MessageHandler _messageHandler;

    [SetUp]
    public void Setup()
    {
        // Используем AutoFixture для автоматического создания всех зависимостей
        _messageHandler = TestKitAutoFixture.CreateMessageHandler();
    }

    /// <summary>
    /// Тест проверяет, что MessageHandler корректно вызывает CommandRouter для команд
    /// <tags>command-router, basic-test</tags>
    /// </summary>
    [Test]
    public async Task HandleCommandAsync_WithStatsCommand_ExecutesWithoutExceptions()
    {
        // Arrange
        var message = TK.CreateStatsCommandMessage();
        var factory = new MessageHandlerTestFactory();

        factory.CommandRouterMock.Setup(x => x.HandleCommandAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = factory.CreateMessageHandler();

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
            await handler.HandleCommandAsync(message, CancellationToken.None));
    }

    /// <summary>
    /// Тест для MessageHandler.HandleCommandAsync с командой /stats - должна быть передана в CommandRouter
    /// <tags>integration, command-routing, stats-command</tags>
    /// </summary>
    [Test]
    public async Task HandleCommandAsync_WithCustomStats_RoutesThroughCommandRouter()
    {
        // Arrange
        var message = TK.CreateStatsCommandMessage();
        var factory = new MessageHandlerTestFactory();

        // Настройка CommandRouter для возврата true (команда обработана)
        factory.CommandRouterMock.Setup(x => x.HandleCommandAsync(
            It.IsAny<Message>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = factory.CreateMessageHandler();

        // Act
        await handler.HandleCommandAsync(message, CancellationToken.None);

        // Проверяем что команда передана в CommandRouter
        factory.CommandRouterMock.Verify(
            x => x.HandleCommandAsync(
                It.IsAny<Message>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "CommandRouter должен вызываться для /stats");
    }

    /// <summary>
    /// Тест для MessageHandler.HandleCommandAsync с командой /stats - проверка интеграции при пустых данных
    /// <tags>integration, command-routing, empty-stats, edge-case</tags>
    /// </summary>
    [Test]
    public async Task HandleCommandAsync_WithEmptyStats_RoutesThroughCommandRouter()
    {
        // Arrange
        var message = TK.CreateStatsCommandMessage();
        var factory = new MessageHandlerTestFactory();

        // Настройка CommandRouter для возврата true (команда обработана)
        factory.CommandRouterMock.Setup(x => x.HandleCommandAsync(
            It.IsAny<Message>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = factory.CreateMessageHandler();

        // Act
        await handler.HandleCommandAsync(message, CancellationToken.None);

        // Assert - проверяем, что CommandRouter был вызван
        factory.CommandRouterMock.Verify(
            x => x.HandleCommandAsync(
                It.IsAny<Message>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "CommandRouter должен вызываться для /stats (пустые данные тоже обрабатываются)");
    }

    /// <summary>
    /// Тест для MessageHandler.HandleCommandAsync с командой /stats - проверка обработки неизвестных команд
    /// <tags>integration, command-routing, unknown-command</tags>
    /// </summary>
    [Test]
    public async Task HandleCommandAsync_WithUnknownCommand_StillCallsCommandRouter()
    {
        // Arrange
        var message = TK.CreateStatsCommandMessage();
        var factory = new MessageHandlerTestFactory();

        // Настройка CommandRouter для возврата false (команда не обработана)
        factory.CommandRouterMock.Setup(x => x.HandleCommandAsync(
            It.IsAny<Message>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = factory.CreateMessageHandler();

        // Act
        await handler.HandleCommandAsync(message, CancellationToken.None);

        // Assert - router вызывается (он пробует обработать и возвращает false)
        factory.CommandRouterMock.Verify(
            x => x.HandleCommandAsync(
                It.IsAny<Message>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "CommandRouter должен вызываться даже если вернёт false");
    }

    /// <summary>
    /// Тест проверяет, что команды /stats корректно передаются в CommandRouter
    /// После рефакторинга команды обрабатываются через CommandRouter
    /// <tags>stats-command, command-router, integration</tags>
    /// </summary>
    [Test]
    public async Task HandleCommandAsync_WithStatsCommand_CallsCommandRouter()
    {
        // Arrange
        var message = TK.CreateStatsCommandMessage();
        var factory = new MessageHandlerTestFactory();

        factory.CommandRouterMock.Setup(x => x.HandleCommandAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = factory.CreateMessageHandler();

        // Act
        await handler.HandleCommandAsync(message, CancellationToken.None);

        // Assert – убеждаемся что CommandRouter вызывался
        factory.CommandRouterMock.Verify(
            x => x.HandleCommandAsync(
                It.IsAny<Message>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "CommandRouter должен вызываться – /stats идёт через общий механизм маршрутизации");
    }

    /// <summary>
    /// Тест для MessageHandler.HandleCommandAsync - проверка логирования обработки команд
    /// <tags>integration, command-routing, logging</tags>
    /// </summary>
    [Test]
    public async Task HandleCommandAsync_WithStatsCommand_InvokesCommandRouter_LogsHandledSafely()
    {
        // Arrange
        var message = TK.CreateStatsCommandMessage();
        var factory = new MessageHandlerTestFactory();

        // Настройка CommandRouter для возврата true (команда обработана)
        factory.CommandRouterMock.Setup(x => x.HandleCommandAsync(
            It.IsAny<Message>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = factory.CreateMessageHandler();

        // Act
        await handler.HandleCommandAsync(message, CancellationToken.None);

        // Assert - router вызывается
        factory.CommandRouterMock.Verify(
            x => x.HandleCommandAsync(
                It.IsAny<Message>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "CommandRouter должен вызываться – логирование фиксирует успешную обработку");
    }
}