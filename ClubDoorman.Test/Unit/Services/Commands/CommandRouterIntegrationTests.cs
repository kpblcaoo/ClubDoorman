using ClubDoorman.Services.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace ClubDoorman.Test.Unit.Services.Commands;

/// <summary>
/// Тест интеграции CommandRouter через DI 
/// <tags>integration, command-router, di</tags>
/// </summary>
[TestFixture]
[Category("integration")]
[Category("command-router")]
public class CommandRouterIntegrationTests
{
    /// <summary>
    /// Проверка успешной регистрации CommandRouter в DI контейнере
    /// <tags>di-registration, integration</tags>
    /// </summary>
    [Test]
    public void DI_CanResolve_CommandRouter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCommandsServices();

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var commandRouter = serviceProvider.GetService<ICommandRouter>();

        // Assert
        Assert.That(commandRouter, Is.Not.Null);
        Assert.That(commandRouter, Is.InstanceOf<CommandRouter>());
    }

    /// <summary>
    /// Проверка регистрации всех ожидаемых Command Handlers
    /// <tags>di-registration, command-handlers</tags>
    /// </summary>
    [Test]
    public void DI_CanResolve_AllCommandHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCommandsServices();

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var commandHandlers = serviceProvider.GetServices<ICommandHandler>();

        // Assert
        Assert.That(commandHandlers, Is.Not.Null);
        
        var handlersList = commandHandlers.ToList();
        Assert.That(handlersList.Count, Is.GreaterThanOrEqualTo(6), "Should have at least 6 command handlers registered");
        
        // Проверяем наличие основных команд по именам
        var commandNames = handlersList.Select(h => h.CommandName).ToList();
        Assert.That(commandNames, Contains.Item("start"));
        Assert.That(commandNames, Contains.Item("suspicious"));
        Assert.That(commandNames, Contains.Item("check"));
        Assert.That(commandNames, Contains.Item("spam"));
        Assert.That(commandNames, Contains.Item("ham"));
        Assert.That(commandNames, Contains.Item("stat"));
        Assert.That(commandNames, Contains.Item("stats"));
        Assert.That(commandNames, Contains.Item("say"));
    }

    /// <summary>
    /// Проверка совместимости старых интерфейсов
    /// <tags>backward-compatibility</tags>
    /// </summary>
    [Test]
    public void DI_BackwardCompatibility_OldInterfacesStillWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCommandsServices();

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var startCommandHandler = serviceProvider.GetService<IStartCommandHandler>();
        var suspiciousCommandHandler = serviceProvider.GetService<ISuspiciousCommandHandler>();
        var commandProcessingService = serviceProvider.GetService<ICommandProcessingService>();

        // Assert
        Assert.That(startCommandHandler, Is.Not.Null, "IStartCommandHandler should still be available");
        Assert.That(suspiciousCommandHandler, Is.Not.Null, "ISuspiciousCommandHandler should still be available");
        Assert.That(commandProcessingService, Is.Not.Null, "ICommandProcessingService should still be available");
    }
}