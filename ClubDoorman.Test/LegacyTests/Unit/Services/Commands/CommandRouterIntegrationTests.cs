using ClubDoorman.Features.AdminOps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.Violation;
using ClubDoorman.Services.ChannelModeration;
using ClubDoorman.Services.LinkFormatting;
using ClubDoorman.Handlers;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services;

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
    /// Настройка зависимостей для тестирования командных обработчиков
    /// </summary>
    private void SetupDependencies(IServiceCollection services)
    {
        // Добавляем необходимые зависимости для тестирования
        services.AddSingleton(Mock.Of<ITelegramBotClientWrapper>());
        services.AddSingleton(Mock.Of<IMessageHandler>());
        services.AddSingleton(Mock.Of<IAppConfig>());
        services.AddSingleton(Mock.Of<IModerationService>());
        services.AddSingleton(Mock.Of<IMessageService>());
        services.AddSingleton(Mock.Of<ISpamHamClassifier>());
        services.AddSingleton(Mock.Of<IBotPermissionsService>());
        services.AddSingleton(Mock.Of<IBadMessageManager>());
        services.AddSingleton(Mock.Of<IAiChecks>());
        services.AddSingleton(Mock.Of<IStatisticsService>());
        services.AddSingleton(Mock.Of<IUserFlowLogger>());
        services.AddSingleton(Mock.Of<IChatLinkFormatter>());
        services.AddSingleton(Mock.Of<IViolationTracker>());
        services.AddSingleton(Mock.Of<IUserBanService>());
        services.AddSingleton(Mock.Of<IChannelModerationService>());
        services.AddSingleton(Mock.Of<ILogChatService>());
    }

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
        SetupDependencies(services);
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
        SetupDependencies(services);
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
        SetupDependencies(services);
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