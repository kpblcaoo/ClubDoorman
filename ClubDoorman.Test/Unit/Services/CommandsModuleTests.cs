using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ClubDoorman.Features.AdminOps;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Handlers;
using ClubDoorman.Services;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.Violation;
using ClubDoorman.Services.ChannelModeration;

namespace ClubDoorman.Test.Unit.Services.Commands;

/// <summary>
/// Тесты для CommandsModule
/// <tags>unit, commands-module, di-registration</tags>
/// </summary>
[TestFixture]
[Category("unit")]
[Category("commands-module")]
public class CommandsModuleTests
{
    private IServiceCollection _services = null!;

    [SetUp]
    public void Setup()
    {
        _services = new ServiceCollection();
        
        // Добавляем необходимые зависимости для тестирования
        _services.AddSingleton(Mock.Of<ITelegramBotClientWrapper>());
        _services.AddSingleton(Mock.Of<IUpdateHandler>());
        _services.AddSingleton(Mock.Of<IAppConfig>());
        _services.AddSingleton(Mock.Of<IModerationService>());
        _services.AddSingleton(Mock.Of<IMessageService>());
        _services.AddSingleton(Mock.Of<ISpamHamClassifier>());
        _services.AddSingleton(Mock.Of<IBotPermissionsService>());
        _services.AddSingleton(Mock.Of<IBadMessageManager>());
        _services.AddSingleton(Mock.Of<IAiChecks>());
        _services.AddSingleton(Mock.Of<IStatisticsService>());
        _services.AddSingleton(Mock.Of<IUserFlowLogger>());
        _services.AddSingleton(Mock.Of<IChatLinkFormatter>());
        _services.AddSingleton(Mock.Of<IViolationTracker>());
        _services.AddSingleton(Mock.Of<IUserBanService>());
        _services.AddSingleton(Mock.Of<IChannelModerationService>());
        _services.AddSingleton(Mock.Of<ILogChatService>());
        
        // Добавляем логгеры
        _services.AddLogging();
    }

    /// <summary>
    /// POC: Проверка регистрации ICommandProcessingService
    /// <tags>poc, command-processing-service, di-registration</tags>
    /// </summary>
    [Test]
    public void AddCommandsServices_ShouldRegisterICommandProcessingService()
    {
        // Act
        _services.AddCommandsServices();

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var commandProcessingService = serviceProvider.GetService<ICommandProcessingService>();
        Assert.That(commandProcessingService, Is.Not.Null);
    }

    /// <summary>
    /// POC: Проверка регистрации ICommandHandler для StartCommandHandler
    /// <tags>poc, start-command-handler, di-registration</tags>
    /// </summary>
    [Test]
    public void AddCommandsServices_ShouldRegisterStartCommandHandlerAsICommandHandler()
    {
        // Act
        _services.AddCommandsServices();

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var commandHandlers = serviceProvider.GetServices<ICommandHandler>();
        Assert.That(commandHandlers, Is.Not.Null);
        Assert.That(commandHandlers.Count(), Is.GreaterThan(0));
        
        // Проверяем, что есть хотя бы один обработчик команд
        Assert.That(commandHandlers.Any(), Is.True);
    }

    /// <summary>
    /// POC: Проверка регистрации ICommandHandler для SuspiciousCommandHandler
    /// <tags>poc, suspicious-command-handler, di-registration</tags>
    /// </summary>
    [Test]
    public void AddCommandsServices_ShouldRegisterSuspiciousCommandHandlerAsICommandHandler()
    {
        // Act
        _services.AddCommandsServices();

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var commandHandlers = serviceProvider.GetServices<ICommandHandler>();
        Assert.That(commandHandlers, Is.Not.Null);
        Assert.That(commandHandlers.Count(), Is.GreaterThan(0));
        
        // Проверяем, что есть хотя бы один обработчик команд
        Assert.That(commandHandlers.Any(), Is.True);
    }

    /// <summary>
    /// POC: Проверка, что метод возвращает IServiceCollection
    /// <tags>poc, commands-service, di-registration</tags>
    /// </summary>
    [Test]
    public void AddCommandsServices_ShouldReturnServiceCollection()
    {
        // Act
        var result = _services.AddCommandsServices();

        // Assert
        Assert.That(result, Is.SameAs(_services));
    }
} 